using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-531 (plan de la noche 3-nov, paso 1: tu habitación y los lugares de Tino y el Doc). Idempotente sobre Ciudad_Prologo
    /// (parte de la escena de main sin T-531: correr una vez después de T-530):
    /// - Habitación: sin la estación del mecha (T02) ni lo demás del taller; queda un banco de trabajo. LA GÜERA sale a la banqueta.
    ///   Sin el letrero amarillo vacío sobre la puerta; el tablero de ENCARGOS se lee desde la calle.
    /// - Locales nuevos tallados en la planta baja de dos edificios de la calle principal de Suburbios (cuarto de 8 x 6 m, 3.6 m de alto,
    ///   frente abierto de 7 m): CIC de Tino junto al MECÁNICO (lado norte) e I+D+I del Doc (lado sur, lejos de El Mercado).
    ///   El puesto CIC (mostrador de la tienda) y Tino se mueven al CIC; la clínica (camilla, mostrador de implantes) y el Doc, al I+D+I.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT531.Colocar
    /// </summary>
    public static class ColocarT531
    {
        public const string NombreRaiz = "T531_Locales";
        public const float AltoLocal = 3.6f;
        public const float FondoLocal = 6f;
        private const float Piso = 0.2f;
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        /// <summary>Local: nombre, edificio a tallar, x de los dos módulos de fachada que ocupa, z del frente y hacia dónde mira (calle).</summary>
        public static readonly (string nombre, string edificio, float xMin, float xMax, float zFrente, float normalZ)[] Locales =
        {
            ("CIC", "Barrio/Manzana_1/Edificios/Edificio_7", -77.35f, -69.25f, 10f, -1f),
            ("IDI", "Barrio/Manzana_4/Edificios/Edificio_41", -104.05f, -95.95f, -10f, 1f),
        };

        public static readonly Color LuzCic = new Color(1f, 0.78f, 0.5f);
        public static readonly Color LuzIdi = new Color(0.35f, 1f, 0.92f);

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-531/Habitación, Tino y el Doc")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Habitacion();
            if (GameObject.Find(NombreRaiz) == null)
            {
                Transform raiz = new GameObject(NombreRaiz).transform;
                foreach (var l in Locales)
                {
                    Tallar(raiz, l.nombre, l.edificio, l.xMin, l.xMax, l.zFrente, l.normalZ);
                }
                MoverCic();
                MoverClinica();
            }
            else
            {
                Informe.AppendLine("Locales ya tallados (sin cambios)");
            }
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t531-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-531 listo:\n" + Informe);
        }

        // ---------------------------------------------------------------- Habitación

        private static void Habitacion()
        {
            Transform casa = GameObject.Find("Barrio")?.transform.Find("Manzana_4/Lugar_Casa");
            Transform taller = casa?.Find("Interior/Taller");
            int quitados = 0;
            if (taller != null)
            {
                foreach (Transform t in taller)
                {
                    bool queda = t.name == "Banco" || t.name == "Banco_Visual_T250";
                    if (!queda && t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        quitados++;
                    }
                }
                // Del banco largo de tres módulos T03 queda uno.
                Transform bancos = taller.Find("Banco_Visual_T250");
                if (bancos != null)
                {
                    foreach (Transform b in bancos.Cast<Transform>().Where(b => Mathf.Abs(b.position.z - (-73.5f)) > 0.2f))
                    {
                        b.gameObject.SetActive(false);
                    }
                }
                Transform banco = taller.Find("Banco");
                if (banco != null && banco.GetComponent<Renderer>() != null)
                {
                    banco.GetComponent<Renderer>().enabled = false;
                    banco.localScale = new Vector3(banco.localScale.x, banco.localScale.y, Mathf.Min(banco.localScale.z, 1.6f));
                }
            }
            Informe.AppendLine("Habitación: " + quitados + " piezas del taller quitadas (T02 incluida); queda un banco de trabajo");

            GameObject guera = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject).FirstOrDefault(g => g.name.StartsWith("Reclutable_LA G") && g.transform.parent != null && g.transform.parent.name == "GTA_T405");
            if (guera != null)
            {
                guera.transform.position = new Vector3(-55.5f, Piso, -86f);
                guera.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Informe.AppendLine("LA GÜERA sale de la habitación a la banqueta (-55.5, -86)");
            }

            Transform letrero = casa?.Find("Letrero");
            if (letrero != null)
            {
                letrero.gameObject.SetActive(false);
            }
            Transform tablero = GameObject.Find("GTA_T426")?.transform.Find("TableroEncargos");
            if (tablero != null)
            {
                // El texto se leía al revés desde la calle: el frente del tablero mira a la calle (+x).
                Vector3 e = tablero.eulerAngles;
                tablero.rotation = Quaternion.Euler(e.x, -90f, e.z);
                Informe.AppendLine("Tablero de ENCARGOS al derecho (mira a la calle); letrero amarillo vacío apagado");
            }
        }

        // ---------------------------------------------------------------- Locales

        private static void Tallar(Transform raiz, string nombre, string rutaEdificio, float xMin, float xMax, float zFrente, float normalZ)
        {
            Transform edificio = Buscar(rutaEdificio);
            if (edificio == null)
            {
                Informe.AppendLine("FALTA " + rutaEdificio);
                return;
            }
            Transform local = new GameObject(nombre).transform;
            local.SetParent(raiz, false);
            float zFondo = zFrente - normalZ * FondoLocal;
            Bounds hueco = new Bounds();
            hueco.SetMinMax(new Vector3(xMin, Piso - 0.5f, Mathf.Min(zFrente, zFondo)), new Vector3(xMax, Piso + AltoLocal, Mathf.Max(zFrente, zFondo)));

            // Cuerpo del edificio: se parte en cajas alrededor del hueco (misma malla, material y collider).
            Transform cuerpo = edificio.Find("Cuerpo");
            Material fachada = cuerpo != null ? cuerpo.GetComponent<Renderer>().sharedMaterial : null;
            int piezas = 0;
            if (cuerpo != null && cuerpo.gameObject.activeSelf)
            {
                Bounds c = cuerpo.GetComponent<Renderer>().bounds;
                foreach (Bounds parte in Restar(c, hueco))
                {
                    GameObject p = Object.Instantiate(cuerpo.gameObject, edificio);
                    p.name = "Cuerpo_T531";
                    p.transform.rotation = Quaternion.identity;
                    p.transform.position = parte.center;
                    p.transform.localScale = parte.size;
                    piezas++;
                }
                cuerpo.gameObject.SetActive(false);
            }

            // Fachada y adornos que tapan el hueco (módulos de planta baja, rellenos, letreros, toldos, gente, macetas).
            int tapas = 0;
            Bounds tapa = new Bounds();
            tapa.SetMinMax(new Vector3(xMin + 0.3f, Piso - 0.1f, Mathf.Min(zFrente, zFondo) - (normalZ < 0 ? 1.5f : 0f)),
                new Vector3(xMax - 0.3f, Piso + AltoLocal + 2.4f, Mathf.Max(zFrente, zFondo) + (normalZ > 0 ? 1.5f : 0f)));
            foreach (Transform t in Todos().ToList())
            {
                if (!t.gameObject.activeInHierarchy || t.IsChildOf(raiz) || t.name == "Cuerpo_T531" || t.GetComponent<Light>() != null)
                {
                    continue;
                }
                if (t.name.StartsWith("Fachada_Modular") && tapa.Contains(new Vector3(t.position.x, Piso + 1f, zFrente + normalZ * -0.1f)))
                {
                    t.gameObject.SetActive(false);
                    tapas++;
                    continue;
                }
                Renderer r = t.GetComponent<Renderer>();
                if (r == null || r is ParticleSystemRenderer || t.root.name == "Barrio" && t.name == "Asfalto")
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (tapa.Contains(b.center) && b.size.x < 9f && b.min.y < Piso + AltoLocal && !t.name.StartsWith("Banqueta"))
                {
                    t.gameObject.SetActive(false);
                    tapas++;
                }
            }

            // Cuarto: piso, techo, muros por dentro y frente con pilastras y dintel.
            Material piso = MatEscena(nombre == "IDI" ? "T506_MercadoInt_Piso" : "T506_Casa_Piso");
            Material muro = MatEscena(nombre == "IDI" ? "T506_CasinoInt_Muro" : "T506_MercadoInt_Muro");
            Material techo = MatEscena("T506_Casa_Techo");
            float cx = (xMin + xMax) * 0.5f;
            float ancho = xMax - xMin;
            float czCuarto = (zFrente + zFondo) * 0.5f;
            Caja(local, "Piso", piso, new Vector3(cx, Piso + 0.02f, czCuarto), new Vector3(ancho, 0.04f, FondoLocal), true);
            Caja(local, "Techo", techo, new Vector3(cx, Piso + AltoLocal - 0.05f, czCuarto), new Vector3(ancho, 0.1f, FondoLocal), false);
            Caja(local, "MuroFondo", muro, new Vector3(cx, Piso + AltoLocal * 0.5f, zFondo + normalZ * 0.05f), new Vector3(ancho, AltoLocal, 0.1f), false);
            Caja(local, "MuroIzq", muro, new Vector3(xMin + 0.05f, Piso + AltoLocal * 0.5f, czCuarto), new Vector3(0.1f, AltoLocal, FondoLocal), false);
            Caja(local, "MuroDer", muro, new Vector3(xMax - 0.05f, Piso + AltoLocal * 0.5f, czCuarto), new Vector3(0.1f, AltoLocal, FondoLocal), false);
            const float Pilastra = 0.5f;
            const float AltoAbertura = 2.7f;
            Caja(local, "Pilastra", fachada, new Vector3(xMin + Pilastra * 0.5f, Piso + AltoLocal * 0.5f, zFrente - normalZ * 0.2f), new Vector3(Pilastra, AltoLocal, 0.4f), true);
            Caja(local, "Pilastra", fachada, new Vector3(xMax - Pilastra * 0.5f, Piso + AltoLocal * 0.5f, zFrente - normalZ * 0.2f), new Vector3(Pilastra, AltoLocal, 0.4f), true);
            Caja(local, "Dintel", fachada, new Vector3(cx, Piso + (AltoAbertura + AltoLocal) * 0.5f, zFrente - normalZ * 0.2f), new Vector3(ancho, AltoLocal - AltoAbertura, 0.4f), true);

            GameObject luz = new GameObject("Luz_" + nombre);
            luz.transform.SetParent(local, false);
            luz.transform.position = new Vector3(cx, Piso + AltoLocal - 0.4f, czCuarto);
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = nombre == "IDI" ? LuzIdi : LuzCic;
            l.intensity = 6f;
            l.range = 9f;
            l.shadows = LightShadows.None;
            Informe.AppendLine("Local " + nombre + ": cuerpo partido en " + piezas + " cajas, " + tapas + " piezas de fachada quitadas, cuarto " + ancho.ToString("F1") + " x " + FondoLocal + " m");
        }

        /// <summary>Caja 'c' menos el hueco 'h' (alineadas a los ejes): hasta 6 cajas.</summary>
        public static List<Bounds> Restar(Bounds c, Bounds h)
        {
            List<Bounds> partes = new List<Bounds>();
            Vector3 hmin = Vector3.Max(h.min, c.min);
            Vector3 hmax = Vector3.Min(h.max, c.max);
            if (hmin.x >= hmax.x || hmin.y >= hmax.y || hmin.z >= hmax.z)
            {
                partes.Add(c);
                return partes;
            }
            void Agregar(Vector3 a, Vector3 b)
            {
                if (b.x - a.x > 0.01f && b.y - a.y > 0.01f && b.z - a.z > 0.01f)
                {
                    Bounds x = new Bounds();
                    x.SetMinMax(a, b);
                    partes.Add(x);
                }
            }
            Agregar(c.min, new Vector3(hmin.x, c.max.y, c.max.z));
            Agregar(new Vector3(hmax.x, c.min.y, c.min.z), c.max);
            Agregar(new Vector3(hmin.x, c.min.y, c.min.z), new Vector3(hmax.x, hmin.y, c.max.z));
            Agregar(new Vector3(hmin.x, hmax.y, c.min.z), new Vector3(hmax.x, c.max.y, c.max.z));
            Agregar(new Vector3(hmin.x, hmin.y, c.min.z), new Vector3(hmax.x, hmax.y, hmin.z));
            Agregar(new Vector3(hmin.x, hmin.y, hmax.z), new Vector3(hmax.x, hmax.y, c.max.z));
            return partes;
        }

        private static void MoverCic()
        {
            (string nombre, string edificio, float xMin, float xMax, float zFrente, float normalZ) l = Locales[0];
            // El puesto CIC miraba a -x (a la calle de tu casa); en el local mira a la calle principal (-z): giro de -90°.
            Vector3 origen = new Vector3(-44.5f, 0f, -77f);
            Vector3 destino = new Vector3((l.xMin + l.xMax) * 0.5f, Piso + 0.04f, l.zFrente - l.normalZ * 4.4f);
            Quaternion giro = Quaternion.Euler(0f, -90f, 0f);
            Transform cic = GameObject.Find("GTA_T417")?.transform.Find("Lugar_CIC");
            Transform tino = GameObject.Find("GTA_T417")?.transform.Find("Apoyo_TINO");
            int n = Mover(new[] { cic, tino }, origen, destino, giro);
            if (cic != null)
            {
                foreach (Transform t in cic.Cast<Transform>().Where(t => t.name.StartsWith("Letrero")))
                {
                    t.gameObject.SetActive(false);
                }
            }
            Informe.AppendLine("Puesto CIC y Tino al local CIC (" + n + " objetos); su letrero plano apagado (el 3D llega en el paso 2)");
        }

        private static void MoverClinica()
        {
            (string nombre, string edificio, float xMin, float xMax, float zFrente, float normalZ) l = Locales[1];
            Transform mercado = GameObject.Find("Barrio")?.transform.Find("Manzana_2/Lugar_MercadoNegro/Interior");
            Transform clinica = mercado?.Find("Clinica");
            Transform luz = mercado?.Find("Luces/LuzFriaClinica");
            Transform joel = GameObject.Find("GTA_T417")?.transform.Find("Apoyo_JOEL");
            if (clinica == null)
            {
                Informe.AppendLine("FALTA la clínica en El Mercado");
                return;
            }
            // La clínica era un rincón de El Mercado que se abría hacia +x; en el I+D+I se abre hacia la calle (+z): giro de -90°.
            Vector3 origen = new Vector3(-34.9f, Piso, 28.3f);
            Vector3 destino = new Vector3((l.xMin + l.xMax) * 0.5f + 1.5f, Piso + 0.04f, l.zFrente - l.normalZ * 2.5f);
            Quaternion giro = Quaternion.Euler(0f, -90f, 0f);
            int n = Mover(new[] { clinica, luz, joel }, origen, destino, giro);
            // La clínica y su luz son parte del prefab de El Mercado: no se pueden cambiar de padre; se mueven de lugar.
            foreach (string pared in new[] { "TabiqueIzquierdo", "TabiqueDerecho", "Dintel", "PisoBaldosa" })
            {
                clinica.Find(pared)?.gameObject.SetActive(false);
            }
            if (joel != null)
            {
                joel.position = new Vector3(joel.position.x, Piso + 0.04f, joel.position.z);
            }
            Informe.AppendLine("Clínica y el Doc (JOEL) de El Mercado al I+D+I (" + n + " objetos)");
        }

        private static int Mover(Transform[] grupo, Vector3 origen, Vector3 destino, Quaternion giro)
        {
            int n = 0;
            foreach (Transform g in grupo.Where(g => g != null))
            {
                List<Transform> hijos = g.GetComponent<Renderer>() == null && g.GetComponent<Collider>() == null && g.position == Vector3.zero
                    ? g.Cast<Transform>().ToList() : new List<Transform> { g };
                foreach (Transform t in hijos)
                {
                    t.SetPositionAndRotation(destino + giro * (t.position - origen), giro * t.rotation);
                    n++;
                }
            }
            return n;
        }

        // ---------------------------------------------------------------- Utilidades

        private static Transform Buscar(string ruta)
        {
            int i = ruta.IndexOf('/');
            return GameObject.Find(ruta.Substring(0, i))?.transform.Find(ruta.Substring(i + 1));
        }

        private static IEnumerable<Transform> Todos()
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
        }

        private static void Caja(Transform padre, string nombre, Material m, Vector3 centro, Vector3 tam, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.position = centro;
            go.transform.localScale = tam;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            if (m != null)
            {
                r.sharedMaterial = m;
            }
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (!collider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            go.isStatic = true;
        }

        private static Material MatEscena(string nombre)
        {
            return Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.name == nombre);
        }
    }
}
