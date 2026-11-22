using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T24 / T-1644: los carros estacionados de Ciudad_Prologo (raíz GTA_T1630_SoloPatrullas, Estacionado_*) cambiaron de malla y de tamaño con el modelo de Tripo.
    /// Revisar (no cambia nada) mide cada uno: altura sobre el piso (flota o se hunde), llantas sobre la banqueta (piso más alto que la calle bajo una esquina) y encimados entre sí o con
    /// otros objetos. Ajustar corrige: lo baja o lo sube al piso, lo aleja de la banqueta hacia la calle y, si se enciman dos, recorre el segundo a lo largo de la calle; si no cabe, lo apaga.
    /// Idempotente. Informe en Logs/t1644-estacionados.txt. Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.EstacionadosTripoT1644.Revisar (o .Ajustar).
    /// </summary>
    public static class EstacionadosTripoT1644
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "GTA_T1630_SoloPatrullas";
        public const string Prefijo = "Estacionado_";
        public const string RutaInforme = "Logs/t1644-estacionados.txt";

        private const int CapaIgnorar = 2;
        private const float AlturaRayo = 1.5f;
        private const float AlcanceRayo = 4f;
        private const float HolguraSobreElSuelo = 0.02f;
        private const float ToleranciaAltura = 0.06f;
        private const float AlturaDeBordillo = 0.1f;
        private const float PasoAlejar = 0.1f;
        private const float MaximoAlejar = 1.2f;
        private const float MargenEntreCarros = 0.3f;
        private const float AlturaDeSuelo = 0.35f;
        private const float MargenObjetos = 0.05f;
        private const float MaximoRecorrer = 3f;

        [MenuItem("Mecha Cyberpunk/T-1644/Revisar estacionados")]
        public static void Revisar() => Ejecutar(false);

        [MenuItem("Mecha Cyberpunk/T-1644/Ajustar estacionados")]
        public static void Ajustar() => Ejecutar(true);

        private static void Ejecutar(bool corregir)
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("T-1644 estacionados (" + (corregir ? "Ajustar" : "Revisar") + ")");
            GameObject raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz);
            if (raiz == null)
            {
                informe.AppendLine("FALTA la raíz " + NombreRaiz);
                Escribir(informe);
                return;
            }
            List<Transform> carros = raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith(Prefijo)).ToList();
            informe.AppendLine("Carros: " + carros.Count + " (activos " + carros.Count(c => c.gameObject.activeInHierarchy) + ")");
            Physics.SyncTransforms();
            int malos = 0;
            int cambiados = 0;
            List<Bounds> puestos = new List<Bounds>();
            foreach (Transform carro in carros)
            {
                if (!carro.gameObject.activeInHierarchy)
                {
                    continue;
                }
                List<string> problemas = new List<string>();
                Transform[] partes = carro.GetComponentsInChildren<Transform>(true);
                int[] capas = partes.Select(t => t.gameObject.layer).ToArray();
                foreach (Transform t in partes)
                {
                    t.gameObject.layer = CapaIgnorar;
                }
                Physics.SyncTransforms();
                Vector3 adelante = carro.forward;
                adelante.y = 0f;
                adelante.Normalize();
                Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
                bool cambio = false;

                // 1) Altura: el fondo de la malla sobre el piso de la calle.
                Bounds b = Medidas(carro.gameObject);
                float suelo = Suelo(b.center, b.min.y);
                if (float.IsNaN(suelo))
                {
                    problemas.Add("sin piso debajo");
                }
                else
                {
                    float hueco = b.min.y - (suelo + HolguraSobreElSuelo);
                    if (Mathf.Abs(hueco) > ToleranciaAltura)
                    {
                        problemas.Add((hueco > 0f ? "flota " : "hundido ") + Mathf.Abs(hueco).ToString("0.00") + " m");
                        if (corregir)
                        {
                            carro.position -= Vector3.up * hueco;
                            cambio = true;
                            Physics.SyncTransforms();
                            b = Medidas(carro.gameObject);
                        }
                    }

                    // 2) Banqueta: bajo las esquinas, piso más alto que la calle. Se aleja hacia el lado contrario.
                    float lado = LadoDeBanqueta(b, adelante, derecha, suelo);
                    if (lado != 0f)
                    {
                        problemas.Add("sobre la banqueta (" + (lado > 0f ? "derecha" : "izquierda") + ")");
                        if (corregir)
                        {
                            float movido = 0f;
                            while (movido < MaximoAlejar && LadoDeBanqueta(b, adelante, derecha, suelo) != 0f)
                            {
                                carro.position -= derecha * (lado * PasoAlejar);
                                movido += PasoAlejar;
                                Physics.SyncTransforms();
                                b = Medidas(carro.gameObject);
                            }
                            cambio = true;
                            problemas.Add("alejado " + movido.ToString("0.0") + " m");
                        }
                    }
                }

                // 3) Encimado con un carro ya revisado: se recorre a lo largo de la calle.
                int vecino = Encimado(b, puestos);
                if (vecino >= 0)
                {
                    problemas.Add("encimado con otro estacionado");
                    if (corregir)
                    {
                        bool libre = false;
                        Vector3 inicio = carro.position;
                        for (float d = PasoAlejar; d <= MaximoRecorrer && !libre; d += PasoAlejar)
                        {
                            foreach (float signo in new[] { 1f, -1f })
                            {
                                carro.position = inicio + adelante * (signo * d);
                                Physics.SyncTransforms();
                                b = Medidas(carro.gameObject);
                                if (Encimado(b, puestos) < 0 && Objetos(b, carro, suelo).Count == 0)
                                {
                                    libre = true;
                                    problemas.Add("recorrido " + (signo * d).ToString("0.0") + " m");
                                    break;
                                }
                            }
                        }
                        if (!libre)
                        {
                            carro.position = inicio;
                            carro.gameObject.SetActive(false);
                            problemas.Add("APAGADO: no cabe");
                        }
                        cambio = true;
                    }
                }

                // 4) Otros objetos (postes, bancas, puestos) dentro de la caja del carro.
                if (carro.gameObject.activeSelf)
                {
                    List<string> estorbos = Objetos(b, carro, float.IsNaN(suelo) ? b.min.y : suelo);
                    if (estorbos.Count > 0)
                    {
                        problemas.Add("toca " + string.Join(", ", estorbos.Distinct().Take(4)));
                    }
                    puestos.Add(b);
                }

                for (int k = 0; k < partes.Length; k++)
                {
                    partes[k].gameObject.layer = capas[k];
                }
                Physics.SyncTransforms();
                if (problemas.Count > 0)
                {
                    malos++;
                }
                if (cambio)
                {
                    cambiados++;
                }
                informe.AppendLine(carro.name + "\tpos " + carro.position.ToString("0.00") + "\tcaja " + b.size.ToString("0.00") + "\t" + (problemas.Count == 0 ? "bien" : string.Join("; ", problemas)));
            }
            informe.AppendLine("Con algo que revisar: " + malos + ". Cambiados: " + cambiados + ".");
            if (corregir && cambiados > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                informe.AppendLine("Escena guardada: " + (new FileInfo(RutaEscena).Length / 1048576f).ToString("0.0") + " MB.");
            }
            Escribir(informe);
        }

        /// <summary>Hoja de 3 x 2 fotos de estacionados (V02, V03, V18, V16, V01a y una fila) en docs/capturas/_tareas/T24/estacionados.png. Corrida aparte.</summary>
        [MenuItem("Mecha Cyberpunk/T-1644/Fotos estacionados")]
        public static void Fotos()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            string[] nombres = { "Estacionado_3_V02", "Estacionado_42_V03", "Estacionado_32_V18", "Estacionado_11_V16", "Estacionado_1_V01a", "Estacionado_36_V22" };
            List<Transform> todos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name.StartsWith(Prefijo)).ToList();
            const int Ancho = 800;
            const int Alto = 500;
            Texture2D hoja = new Texture2D(Ancho * 3, Alto * 2, TextureFormat.RGB24, false);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            GameObject camObjeto = new GameObject("CamFotosT1644");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;
            for (int i = 0; i < nombres.Length; i++)
            {
                Transform carro = todos.FirstOrDefault(t => t.name == nombres[i]);
                if (carro == null)
                {
                    continue;
                }
                Vector3 adelante = carro.forward;
                Vector3 derecha = carro.right;
                // Desde la calle (el lado contrario a la banqueta lo da el punto de subida, a la izquierda): de frente-lado y un poco arriba, para ver bordillo y vecinos.
                float lejos = carro.name.Contains("V18") ? 13f : 8f;
                camObjeto.transform.position = carro.position + adelante * lejos * 0.8f - derecha * lejos * 0.6f + Vector3.up * 4f;
                camObjeto.transform.LookAt(carro.position + Vector3.up * 0.8f);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0f, 0f, Ancho, Alto), (i % 3) * Ancho, (1 - i / 3) * Alto);
            }
            hoja.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory(CarpetaFotos);
            File.WriteAllBytes(CarpetaFotos + "/estacionados.png", hoja.EncodeToPNG());
            Object.DestroyImmediate(hoja);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObjeto);
        }

        public const string CarpetaFotos = "docs/capturas/_tareas/T24";

        private static float Suelo(Vector3 punto, float fondo)
        {
            if (Physics.Raycast(new Vector3(punto.x, fondo + AlturaRayo, punto.z), Vector3.down, out RaycastHit hit, AlcanceRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }
            return float.NaN;
        }

        // +1 si la banqueta queda bajo las esquinas de la derecha, -1 si de la izquierda, 0 si las cuatro esquinas están en la calle.
        private static float LadoDeBanqueta(Bounds b, Vector3 adelante, Vector3 derecha, float suelo)
        {
            float mitadLargo = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(adelante.x), 0f, Mathf.Abs(adelante.z))));
            float mitadAncho = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(derecha.x), 0f, Mathf.Abs(derecha.z))));
            float resultado = 0f;
            foreach (float lado in new[] { 1f, -1f })
            {
                foreach (float punta in new[] { 0.8f, -0.8f })
                {
                    Vector3 p = b.center + derecha * (lado * mitadAncho * 0.9f) + adelante * (punta * mitadLargo);
                    float s = Suelo(p, suelo);
                    if (!float.IsNaN(s) && s > suelo + AlturaDeBordillo)
                    {
                        resultado = lado;
                    }
                }
            }
            return resultado;
        }

        private static int Encimado(Bounds b, List<Bounds> puestos)
        {
            for (int i = 0; i < puestos.Count; i++)
            {
                Bounds otro = puestos[i];
                otro.Expand(new Vector3(MargenEntreCarros, 0f, MargenEntreCarros) * 2f);
                if (otro.Intersects(b))
                {
                    return i;
                }
            }
            return -1;
        }

        private static List<string> Objetos(Bounds b, Transform carro, float suelo)
        {
            List<string> nombres = new List<string>();
            Vector3 semi = new Vector3(b.extents.x - MargenObjetos, (b.max.y - (suelo + AlturaDeSuelo)) * 0.5f, b.extents.z - MargenObjetos);
            Vector3 centro = new Vector3(b.center.x, (b.max.y + suelo + AlturaDeSuelo) * 0.5f, b.center.z);
            foreach (Collider otro in Physics.OverlapBox(centro, semi, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (otro.transform.IsChildOf(carro) || otro.bounds.max.y <= suelo + AlturaDeSuelo || otro.transform.name.StartsWith(Prefijo) || EsEstacionado(otro.transform))
                {
                    continue;
                }
                nombres.Add(otro.name);
            }
            return nombres;
        }

        private static bool EsEstacionado(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name.StartsWith(Prefijo))
                {
                    return true;
                }
            }
            return false;
        }

        // Alto: de las mallas (el fondo visible). Largo y ancho: de las cajas de choque, porque V02 y V03 vienen en diagonal dentro de su FBX y la caja de su malla girada sale más grande que el carro.
        private static Bounds Medidas(GameObject objeto)
        {
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(objeto.transform.position, Vector3.zero);
            }
            Bounds visual = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                visual.Encapsulate(r.bounds);
            }
            Collider[] cajas = objeto.GetComponentsInChildren<BoxCollider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            if (cajas.Length == 0)
            {
                return visual;
            }
            Bounds choque = cajas[0].bounds;
            foreach (Collider c in cajas)
            {
                choque.Encapsulate(c.bounds);
            }
            Vector3 min = new Vector3(choque.min.x, visual.min.y, choque.min.z);
            Vector3 max = new Vector3(choque.max.x, visual.max.y, choque.max.z);
            Bounds b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        private static void Escribir(StringBuilder informe)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Vehiculos] T-1644 estacionados:\n" + informe);
        }
    }
}
