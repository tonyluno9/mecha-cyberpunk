using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-530 (plan de la noche 3-nov, paso 1: escala, colisiones y barrido). Idempotente sobre Ciudad_Prologo:
    /// - Afueras: las 6 salidas de la ciudad siguen 100 m de calle con banquetas, rayas y cuadras de relleno a los lados y una calle
    ///   cruzada al fondo; sin retenes, sin "ALTO · CONTROL CORPORATIVO" y sin muros en las salidas. Un muro invisible cierra cada calle al fondo.
    /// - Tianguis: el arco sale del cruce (su pilar quedaba en medio de la calle), los puestos que flotaban encima de otros bajan al piso,
    ///   los puestos traslapados se quedan uno (orden: hileras de T-512, modelos de T-503, mesas de T-407), sin hologramas ni proyectores
    ///   de relleno, y sin las dos barreras junto a los licuados y el pastor.
    /// - Barrido general: cajas, tambos, huacales y puestos que flotan bajan al piso; las cámaras tiradas en el suelo se apagan.
    /// - Letreros planos de lugares (texto sobre la puerta: EL MERCADO 24 HORAS ABIERTO, etc.) apagados.
    /// - Neón Alto: los dos tramos de autopista elevada (N02) que atravesaban edificios quedan como puentes del ancho de la calle.
    /// - Robots de Nova (guardias) a 2.2 m; cámara del hombro más cerca.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT530.Colocar
    /// </summary>
    public static class ColocarT530
    {
        public const string NombreRaiz = "T530_Afueras";
        public const float LargoAfueras = 100f;
        public const float AnchoCalle = 14f;
        public const float AnchoBanqueta = 3f;
        public const float AlturaRobotNova = 2.2f;
        private const float AltoBanqueta = 0.2f;
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";

        /// <summary>Salidas de la ciudad: punto en la orilla (centro de la calle) y dirección hacia afuera.</summary>
        public static readonly (Vector3 punto, Vector3 dir)[] Salidas =
        {
            (new Vector3(135f, 0f, 0f), Vector3.right),
            (new Vector3(-135f, 0f, 0f), Vector3.left),
            (new Vector3(-47f, 0f, 87f), Vector3.forward),
            (new Vector3(47f, 0f, 87f), Vector3.forward),
            (new Vector3(-47f, 0f, -87f), Vector3.back),
            (new Vector3(47f, 0f, -87f), Vector3.back),
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-530/Escala y barrido")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();

            Afueras();
            Tianguis();
            Physics.SyncTransforms();
            Flotantes();
            // Segunda vuelta: lo que estaba encima de algo que bajó (cajas sobre las mesas del Tianguis).
            Physics.SyncTransforms();
            Flotantes();
            Letreros();
            PuentesNeon();
            RobotsNova();
            Camara();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t530-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-530 listo:\n" + Informe);
        }

        // ---------------------------------------------------------------- Afueras

        private static void Afueras()
        {
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;

            // Retenes, obra y avisos de las salidas: fuera.
            Transform finales = GameObject.Find("Barrio")?.transform.Find("Finales");
            foreach (string n in new[] { "Final_Reten", "Final_Obra", "Final_Reten_Norte_Oeste", "Final_Reten_Norte_Este", "Final_Obra_Sur" })
            {
                Transform t = finales?.Find(n);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }
            Transform muroNorte = finales?.Find("Final_Autopista/MuroContencion");
            if (muroNorte != null)
            {
                muroNorte.gameObject.SetActive(false);
            }
            Informe.AppendLine("Retenes, obra, avisos ALTO y muros de las orillas apagados");

            Material asfalto = Mat("Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat");
            Material banqueta = MatEscena("T506_Barrio_Banqueta");
            Material raya = MatEscena("Barrio_Marca");
            Material muro = MatEscena("T506_Barrio_ConcretoOscuro");
            Material[] fachadas = Directory.GetFiles("Assets/Arte/Exteriores/Materiales", "T507_T506_Barrio_*.mat")
                .Where(p => Regex.IsMatch(Path.GetFileName(p), "_(Edif|Pobre)_[A-D]_\\d")).Select(Mat).Where(m => m != null).ToArray();
            GameObject[] ventanas = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => t.name == "Ventanas" && t.GetComponent<MeshRenderer>() != null && t.parent != null && t.parent.name.StartsWith("Lado_"))
                .Select(t => t.gameObject).Take(6).ToArray();

            // Muros de la orilla con huecos en las salidas (detrás de los edificios: tapan el vacío desde las azoteas).
            Transform orillas = Grupo(raiz, "Orillas");
            float mitadX = 135f;
            float mitadZ = 88f;
            MuroConHuecos(orillas, muro, new Vector3(mitadX, 0f, 0f), Vector3.forward, mitadZ, new[] { 0f });
            MuroConHuecos(orillas, muro, new Vector3(-mitadX, 0f, 0f), Vector3.forward, mitadZ, new[] { 0f });
            MuroConHuecos(orillas, muro, new Vector3(0f, 0f, mitadZ), Vector3.right, mitadX, new[] { -47f, 47f });
            MuroConHuecos(orillas, muro, new Vector3(0f, 0f, -mitadZ), Vector3.right, mitadX, new[] { -47f, 47f });

            System.Random azar = new System.Random(530);
            int cuadras = 0;
            for (int i = 0; i < Salidas.Length; i++)
            {
                cuadras += Corredor(Grupo(raiz, "Salida_" + i), Salidas[i].punto, Salidas[i].dir, asfalto, banqueta, raya, fachadas, ventanas, azar);
            }
            TorresFuera();
            Informe.AppendLine("Afueras: " + Salidas.Length + " calles de " + LargoAfueras + " m con " + cuadras + " edificios de relleno");
        }

        /// <summary>Las torres de fondo de Neón Alto (T-505) que caen dentro de una calle de las afueras se empujan hacia afuera.</summary>
        private static void TorresFuera()
        {
            Transform torres = GameObject.Find("T505_NeonAlto")?.transform.Find("Torres");
            if (torres == null)
            {
                return;
            }
            int movidas = 0;
            foreach (Transform torre in torres)
            {
                if (!Caja(torre, out Bounds b))
                {
                    continue;
                }
                bool movida = false;
                for (int intento = 0; intento < 20; intento++)
                {
                    int dentro = -1;
                    for (int i = 0; i < Salidas.Length && dentro < 0; i++)
                    {
                        dentro = DentroDeAfueras(b, Salidas[i].punto, Salidas[i].dir) ? i : -1;
                    }
                    if (dentro < 0)
                    {
                        break;
                    }
                    Vector3 paso = Salidas[dentro].dir * 20f;
                    torre.position += paso;
                    b.center += paso;
                    movida = true;
                }
                movidas += movida ? 1 : 0;
            }
            Informe.AppendLine("Torres de fondo movidas fuera de las afueras: " + movidas);
        }

        private static bool DentroDeAfueras(Bounds b, Vector3 origen, Vector3 dir)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            const float Margen = 5f;
            float largo = LargoAfueras + AnchoCalle + 40f + Margen;
            Vector3[] esquinas =
            {
                new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.max.z),
            };
            float minD = esquinas.Min(e => Vector3.Dot(e - origen, dir));
            float maxD = esquinas.Max(e => Vector3.Dot(e - origen, dir));
            float minL = esquinas.Min(e => Vector3.Dot(e - origen, lado));
            float maxL = esquinas.Max(e => Vector3.Dot(e - origen, lado));
            return maxD > -Margen && minD < largo && maxL > -(70f + Margen) && minL < 70f + Margen;
        }

        private static int Corredor(Transform padre, Vector3 origen, Vector3 dir, Material asfalto, Material banqueta, Material raya,
            Material[] fachadas, GameObject[] ventanas, System.Random azar)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            Quaternion giro = Quaternion.LookRotation(dir);
            float mitad = AnchoCalle * 0.5f;
            float largo = LargoAfueras;
            Vector3 centro = origen + dir * (largo * 0.5f);

            // Piso: calle y banquetas (la calle cruzada del fondo es un poco más ancha que la calle).
            Caja(padre, "Calle", asfalto, centro + Vector3.down * 0.5f, giro, new Vector3(AnchoCalle, 1f, largo + 1f), true);
            for (int s = -1; s <= 1; s += 2)
            {
                Caja(padre, "Banqueta", banqueta, centro + lado * s * (mitad + AnchoBanqueta * 0.5f) + Vector3.up * (AltoBanqueta * 0.5f - 0.5f),
                    giro, new Vector3(AnchoBanqueta, AltoBanqueta + 1f, largo), true);
            }
            Vector3 cruce = origen + dir * (largo + mitad);
            Caja(padre, "CalleCruzada", asfalto, cruce + Vector3.down * 0.5f, giro, new Vector3(120f, 1f, AnchoCalle), true);
            for (float d = 4f; d < largo; d += 8f)
            {
                Caja(padre, "Raya", raya, origen + dir * d + Vector3.up * 0.005f, giro, new Vector3(0.3f, 0.01f, 3f), false);
            }

            // Cuadras de relleno a los dos lados, pegadas unas con otras (no se ve el vacío entre ellas).
            int n = 0;
            for (int s = -1; s <= 1; s += 2)
            {
                float d = 0.5f;
                while (d < largo)
                {
                    float ancho = Mathf.Min(10f + (float)azar.NextDouble() * 14f, largo - d);
                    if (ancho < 4f)
                    {
                        break;
                    }
                    float alto = 9f + (float)azar.NextDouble() * (d > largo * 0.5f ? 30f : 16f);
                    float fondo = 22f;
                    Vector3 c = origen + dir * (d + ancho * 0.5f) + lado * s * (mitad + AnchoBanqueta + fondo * 0.5f) + Vector3.up * (alto * 0.5f);
                    Edificio(padre, fachadas, ventanas, azar, c, giro, new Vector3(fondo, alto, ancho), lado * -s, ancho);
                    n++;
                    d += ancho;
                }
            }
            // Al fondo: cuadra que cierra la vista después de la calle cruzada y muro invisible para que nadie pase.
            float altoFondo = 28f + (float)azar.NextDouble() * 20f;
            Edificio(padre, fachadas, ventanas, azar, cruce + dir * (mitad + 15f) + Vector3.up * (altoFondo * 0.5f), giro,
                new Vector3(120f, altoFondo, 30f), -dir, 120f);
            n++;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 c = cruce + lado * s * 40f + dir * -(mitad + 11f) + Vector3.up * 12f;
                Edificio(padre, fachadas, ventanas, azar, c, giro, new Vector3(40f - mitad - AnchoBanqueta, 24f, 22f), dir, 30f);
                n++;
            }
            for (int s = -1; s <= 1; s += 2)
            {
                // Remate de la calle cruzada a cada lado.
                Edificio(padre, fachadas, ventanas, azar, cruce + lado * s * 64f + Vector3.up * 13f, giro, new Vector3(8f, 26f, AnchoCalle + 30f), lado * -s, AnchoCalle);
                n++;
            }
            GameObject limite = new GameObject("LimiteInvisible");
            limite.transform.SetParent(padre, false);
            limite.transform.SetPositionAndRotation(cruce + dir * (mitad - 0.5f) + Vector3.up * 10f, giro);
            limite.AddComponent<BoxCollider>().size = new Vector3(120f, 20f, 1f);
            foreach (int s in new[] { -1, 1 })
            {
                GameObject ladoInv = new GameObject("LimiteInvisible");
                ladoInv.transform.SetParent(padre, false);
                ladoInv.transform.SetPositionAndRotation(cruce + lado * s * 45f + Vector3.up * 10f, giro);
                ladoInv.AddComponent<BoxCollider>().size = new Vector3(1f, 20f, AnchoCalle + 2f);
            }
            return n;
        }

        private static void Edificio(Transform padre, Material[] fachadas, GameObject[] ventanas, System.Random azar, Vector3 centro,
            Quaternion giro, Vector3 tam, Vector3 frente, float anchoFrente)
        {
            Material m = fachadas.Length > 0 ? fachadas[azar.Next(fachadas.Length)] : null;
            GameObject cuerpo = Caja(padre, "Edificio", m, centro, giro, tam, true);
            if (ventanas.Length == 0)
            {
                return;
            }
            // Ventanas en la cara hacia la calle (copia de las de la ciudad: se prenden de noche igual).
            float pisoBase = centro.y - tam.y * 0.5f;
            float alto = tam.y - 4f;
            if (alto < 3f)
            {
                return;
            }
            Vector3 cara = new Vector3(centro.x, pisoBase + 4f + alto * 0.5f, centro.z) + frente.normalized * (Vector3.Dot(tam, Abs(Quaternion.Inverse(giro) * frente)) * 0.5f + 0.03f);
            GameObject v = Object.Instantiate(ventanas[azar.Next(ventanas.Length)], padre);
            v.name = "Ventanas";
            v.transform.SetPositionAndRotation(cara, Quaternion.LookRotation(-frente));
            v.transform.localScale = new Vector3(anchoFrente - 1f, alto, 1f);
            // VentanasFachada (Mundo): repetición del mosaico de 12 m al tamaño del quad, como en la ciudad.
            foreach (MonoBehaviour mb in v.GetComponents<MonoBehaviour>().Where(c => c != null && c.GetType().Name == "VentanasFachada"))
            {
                SerializedObject so = new SerializedObject(mb);
                so.FindProperty("repeticion").vector4Value = new Vector4((anchoFrente - 1f) / 12f, alto / 12f, (float)azar.NextDouble(), 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
                mb.enabled = false;
                mb.enabled = true;
            }
            cuerpo.isStatic = true;
        }

        private static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        private static void MuroConHuecos(Transform padre, Material m, Vector3 centro, Vector3 eje, float mitad, float[] huecos)
        {
            const float Hueco = AnchoCalle * 0.5f + AnchoBanqueta;
            List<float> cortes = new List<float> { -mitad };
            foreach (float h in huecos.OrderBy(h => h))
            {
                cortes.Add(h - Hueco);
                cortes.Add(h + Hueco);
            }
            cortes.Add(mitad);
            for (int i = 0; i + 1 < cortes.Count; i += 2)
            {
                float a = cortes[i];
                float b = cortes[i + 1];
                if (b - a < 0.5f)
                {
                    continue;
                }
                Vector3 c = centro + eje * ((a + b) * 0.5f) + Vector3.up * 3f;
                Caja(padre, "Muro", m, c, Quaternion.LookRotation(eje), new Vector3(2f, 6f, b - a), true);
            }
        }

        // ---------------------------------------------------------------- Tianguis

        private static void Tianguis()
        {
            // Arco fuera del cruce: su pilar quedaba en medio de la calle que cruza (x = -47).
            Transform arco = GameObject.Find("GTA_T407")?.transform.Find("Tianguis/ArcoTianguis");
            if (arco != null)
            {
                arco.position = new Vector3(-36f, arco.position.y, arco.position.z);
                Informe.AppendLine("Arco del Tianguis a x = -36 (fuera del cruce)");
            }

            // Hologramas y proyectores de relleno.
            int apagados = 0;
            foreach (string ruta in new[] { "T503_Tianguis/Hologramas", "T503_Tianguis/HologramasPuestos" })
            {
                apagados += Apagar(ruta);
            }
            foreach (Transform t in Todos().Where(t => t.name.StartsWith("CY01_ProyectorHolo")))
            {
                t.gameObject.SetActive(false);
                apagados++;
            }
            Informe.AppendLine("Hologramas y proyectores de relleno apagados: " + apagados);

            // Las dos barreras entre los licuados y el pastor (orillas del Tianguis junto al cruce oeste).
            int barreras = 0;
            foreach (Transform t in Todos().Where(t => t.name.StartsWith("C18_BarreraCalle_Noche") && t.position.x < -30f && t.position.x > -60f && t.position.z > 0f))
            {
                t.gameObject.SetActive(false);
                barreras++;
            }
            Informe.AppendLine("Barreras del Tianguis quitadas: " + barreras);

            // Puestos de T-512 que quedaron encima de otros: al piso.
            Transform t512 = GameObject.Find("T512_LuzZonas")?.transform.Find("Tianguis");
            int bajados = 0;
            if (t512 != null)
            {
                foreach (Transform p in t512)
                {
                    if (p.position.y > 0.05f)
                    {
                        p.position = new Vector3(p.position.x, 0f, p.position.z);
                        bajados++;
                    }
                }
            }
            Informe.AppendLine("Puestos de T-512 bajados al piso: " + bajados);

            // Traslapes: se queda el de más prioridad.
            List<(Transform t, int prioridad)> unidades = new List<(Transform, int)>();
            if (t512 != null)
            {
                unidades.AddRange(t512.Cast<Transform>().Select(t => (t, 4)));
            }
            Transform t503 = GameObject.Find("T503_Tianguis")?.transform.Find("Puestos");
            if (t503 != null)
            {
                unidades.AddRange(t503.Cast<Transform>().Where(t => t.gameObject.activeSelf).Select(t => (t, 3)));
            }
            Transform noche = GameObject.Find("Barrio")?.transform.Find("ArteNoche/Tianguis");
            if (noche != null)
            {
                unidades.AddRange(noche.Cast<Transform>().Select(t => (t, t.name.StartsWith("C13") ? 3 : 1)));
            }
            Transform t407 = GameObject.Find("GTA_T407")?.transform.Find("Tianguis/Puestos");
            if (t407 != null)
            {
                unidades.AddRange(t407.Cast<Transform>().Select(t => (t, 2)));
            }
            Transform t506 = GameObject.Find("T506_SinGreybox")?.transform.Find("Puestos");
            if (t506 != null)
            {
                unidades.AddRange(t506.Cast<Transform>().Where(t => Mathf.Abs(t.position.z) < 11f && Mathf.Abs(t.position.x) < 46f).Select(t => (t, 0)));
            }
            List<Bounds> puestas = new List<Bounds>();
            List<Transform> quedan = new List<Transform>();
            int quitados = 0;
            foreach ((Transform t, int prioridad) in unidades.OrderByDescending(u => u.prioridad))
            {
                if (!t.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (!Caja(t, out Bounds b))
                {
                    continue;
                }
                // Las cajas de T-506 encima de la mesa de su puesto de T-407 no cuentan como traslape con su mesa.
                bool choca = false;
                for (int i = 0; i < puestas.Count; i++)
                {
                    if (prioridad == 0 && quedan[i].IsChildOf(t407 != null ? t407 : quedan[i]) && Encima(b, puestas[i]))
                    {
                        continue;
                    }
                    if (TraslapeXZ(b, puestas[i], 0.15f))
                    {
                        choca = true;
                        break;
                    }
                }
                if (choca)
                {
                    t.gameObject.SetActive(false);
                    quitados++;
                    continue;
                }
                puestas.Add(b);
                quedan.Add(t);
            }
            // Cajas de T-506 que se quedaron sin la mesa de su puesto (apagado arriba): fuera también.
            if (t506 != null && t407 != null)
            {
                foreach (Transform caja in t506.Cast<Transform>().Where(c => c.gameObject.activeSelf && Mathf.Abs(c.position.z) < 11f && c.position.y > 0.6f))
                {
                    Transform mesa = t407.Cast<Transform>().FirstOrDefault(p => Mathf.Abs(p.position.x - caja.position.x) < 2f && Mathf.Abs(p.position.z - caja.position.z) < 1.2f);
                    if (mesa == null || !mesa.gameObject.activeSelf)
                    {
                        caja.gameObject.SetActive(false);
                        quitados++;
                    }
                }
            }
            Informe.AppendLine("Puestos y cajas traslapados quitados en El Tianguis: " + quitados);
        }

        private static bool Encima(Bounds arriba, Bounds abajo)
        {
            return arriba.min.y >= abajo.max.y - 0.3f;
        }

        private static bool TraslapeXZ(Bounds a, Bounds b, float margen)
        {
            return a.min.x + margen < b.max.x && b.min.x + margen < a.max.x && a.min.z + margen < b.max.z && b.min.z + margen < a.max.z;
        }

        // ---------------------------------------------------------------- Flotantes

        private static readonly Regex Apoyados = new Regex(
            "^(Puesto_[0-9]|C16_Tambo|C17_Huacales|C11_Basurero|L04_Llantas|L01_Contenedor|C19_Moto|CY06_Expendedora|N03a|N03b|N06|C12_PuestoTacos|C13_PuestoCiber|Tambo|Huacal|Barril|Caja_Calle|Cono)");

        private static void Flotantes()
        {
            int bajados = 0;
            int camaras = 0;
            StringBuilder lista = new StringBuilder();
            foreach (Transform t in Todos().ToList())
            {
                if (!t.gameObject.activeInHierarchy || t.root.name == NombreRaiz)
                {
                    continue;
                }
                if (t.name.StartsWith("CY08"))
                {
                    if (Caja(t, out Bounds bc) && bc.max.y < 1.2f)
                    {
                        t.gameObject.SetActive(false);
                        camaras++;
                        lista.AppendLine("  cámara en el suelo: " + InspeccionT530.Ruta(t));
                    }
                    continue;
                }
                if (!Apoyados.IsMatch(t.name) || (t.parent != null && Apoyados.IsMatch(t.parent.name)))
                {
                    continue;
                }
                if (!Caja(t, out Bounds b) || b.min.y > 4f)
                {
                    continue;
                }
                float soporte = Soporte(t, b);
                float hueco = b.min.y - soporte;
                if (hueco > 0.12f && hueco < 4f)
                {
                    t.position += Vector3.down * hueco;
                    bajados++;
                    lista.AppendLine("  " + InspeccionT530.Ruta(t) + " bajó " + hueco.ToString("F2") + " m");
                }
            }
            Informe.AppendLine("Objetos flotando bajados al piso: " + bajados + "; cámaras en el suelo apagadas: " + camaras);
            Informe.Append(lista);
        }

        /// <summary>Altura de lo que hay debajo del centro y de las 4 esquinas (colliders), sin contar el propio objeto.</summary>
        private static float Soporte(Transform t, Bounds b)
        {
            float mejor = float.NegativeInfinity;
            Vector3[] puntos =
            {
                b.center,
                new Vector3(b.min.x + 0.1f, 0f, b.min.z + 0.1f), new Vector3(b.max.x - 0.1f, 0f, b.min.z + 0.1f),
                new Vector3(b.min.x + 0.1f, 0f, b.max.z - 0.1f), new Vector3(b.max.x - 0.1f, 0f, b.max.z - 0.1f),
            };
            foreach (Vector3 p in puntos)
            {
                Vector3 desde = new Vector3(p.x, b.min.y + 0.1f, p.z);
                foreach (RaycastHit h in Physics.RaycastAll(desde, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(t))
                    {
                        continue;
                    }
                    mejor = Mathf.Max(mejor, h.point.y);
                }
            }
            return float.IsNegativeInfinity(mejor) ? b.min.y : mejor;
        }

        // ---------------------------------------------------------------- Letreros, puentes, robots, cámara

        private static void Letreros()
        {
            int n = 0;
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != null && m.GetType().Name == "LetreroLugar"))
            {
                m.gameObject.SetActive(false);
                n++;
            }
            foreach (Transform t in Todos().Where(t => t.name == "AvisoCanvas" && t.parent != null && t.parent.name == "Cerrado"))
            {
                t.gameObject.SetActive(false);
                n++;
            }
            Informe.AppendLine("Letreros planos de lugares apagados: " + n);
        }

        private static void PuentesNeon()
        {
            int n = 0;
            foreach (Transform t in Todos().Where(t => t.name == "N02_T505"))
            {
                // El modelo mide 24 m a escala 1; la calle de Neón Alto (x = 40 a 54) más sus banquetas cabe en 0.9.
                t.localScale = new Vector3(0.9f, t.localScale.y, t.localScale.z);
                foreach (Transform tira in t.Cast<Transform>().Where(h => h.name == "Tira_Neon"))
                {
                    // Las tiras de neón medían 76 m: ahora del largo del puente (21 m).
                    tira.localScale = new Vector3(21f / 0.9f, tira.localScale.y, tira.localScale.z);
                }
                n++;
            }
            Informe.AppendLine("Puentes de Neón Alto acortados al ancho de la calle: " + n);
        }

        private static void RobotsNova()
        {
            Transform guardias = GameObject.Find("T510_Guardias")?.transform;
            if (guardias == null)
            {
                return;
            }
            int n = 0;
            foreach (Transform g in guardias)
            {
                Renderer[] rs = g.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0)
                {
                    continue;
                }
                g.localScale = Vector3.one;
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                if (b.size.y > 0.5f)
                {
                    g.localScale = Vector3.one * (AlturaRobotNova / b.size.y);
                    n++;
                }
            }
            Informe.AppendLine("Robots de Nova a " + AlturaRobotNova + " m: " + n);
        }

        private static void Camara()
        {
            Object config = AssetDatabase.LoadMainAssetAtPath(RutaCamara);
            if (config == null)
            {
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("distanciaNormal").floatValue = 2.6f;
            so.FindProperty("alturaNormal").floatValue = 1.6f;
            so.FindProperty("lateralNormal").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            Informe.AppendLine("Cámara del hombro: 2.6 m atrás (antes 3.2), 1.6 m de alto, 0.5 m al lado");
        }

        // ---------------------------------------------------------------- Utilidades

        private static IEnumerable<Transform> Todos()
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
        }

        private static int Apagar(string ruta)
        {
            int i = ruta.IndexOf('/');
            Transform t = GameObject.Find(ruta.Substring(0, i))?.transform.Find(ruta.Substring(i + 1));
            if (t == null || !t.gameObject.activeSelf)
            {
                return 0;
            }
            t.gameObject.SetActive(false);
            return 1;
        }

        private static bool Caja(Transform t, out Bounds b)
        {
            b = default;
            bool hay = false;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer)
                {
                    continue;
                }
                if (!hay)
                {
                    b = r.bounds;
                    hay = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return hay;
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(padre, false);
            return g;
        }

        private static GameObject Caja(Transform padre, string nombre, Material m, Vector3 centro, Quaternion giro, Vector3 tam, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(centro, giro);
            go.transform.localScale = tam;
            if (m != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            if (!collider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            go.isStatic = true;
            return go;
        }

        private static Material Mat(string ruta)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(ruta);
        }

        private static Material MatEscena(string nombre)
        {
            return Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.name == nombre);
        }
    }
}
