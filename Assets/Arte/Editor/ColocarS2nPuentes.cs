using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-809 (noche 6-nov, sesión 2): los dos puentes elevados de Neón Alto (N02 de T-505, sobre la avenida x = 40..54, en z = 38 y z = 72; el
    /// de z = 72 es el que mira a Nova) estaban atravesados entre dos edificios sin llegar a nada y sin colisión. Ahora se suben: cada uno lleva
    /// una escalera de acero por la banqueta (la de z = 38 por la banqueta oeste hacia el norte, la de z = 72 por la este hacia el sur), que
    /// pasa por encima del pretil del puente con un descanso y baja a la cubierta con seis escalones. Escalones de 0.25 m (rampa de 36°) y de
    /// 0.28 m (41°) para el CharacterController del jugador (paso 0.35, pendiente 45°). Colisión: rampas invisibles bajo los escalones,
    /// cubierta, pretiles, topes en las puntas y barandales; postes de apoyo hasta la banqueta y tira de neón bajo el pasamanos. Los adornos
    /// de la banqueta que quedaban dentro de la escalera se apagan. Cada puente trae su camino (hijos "Camino/P0..P5") para la prueba.
    /// No toca Barrio.prefab. Idempotente: rehace la raíz S2n_Puentes.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nPuentes.Colocar (fotos: ColocarS2nPuentes.Fotos)
    /// </summary>
    public static class ColocarS2nPuentes
    {
        public const string NombreRaiz = "S2n_Puentes";
        /// <summary>Alto (m) de la cubierta del N02 sobre su base (medido con rayos sobre la malla) y de la banqueta.</summary>
        public const float AltoCubierta = 8.39f;
        public const float AltoBanqueta = 0.2f;
        /// <summary>Medio ancho (m) de la cubierta (de pretil a pretil) y hasta dónde llegan los pretiles con su borde.</summary>
        private const float MedioAnchoCubierta = 3.5f;
        private const float MedioAnchoPretil = 4.6f;
        private const float AltoPretil = 9.95f;
        /// <summary>El descanso pasa sobre el pretil: su piso queda a esta altura.</summary>
        private const float AltoDescanso = 10.1f;
        private const float PuntaOeste = 37f;
        private const float PuntaEste = 57.8f;
        private const float CentroAvenida = 47f;
        private const float GrosorEscalon = 0.06f;
        private const float AltoBarandal = 1.05f;
        private const float AltoTopeInvisible = 1.3f;
        private const float SeparacionPostes = 1.6f;
        private const float SeparacionApoyos = 3.2f;
        private const float GrosorRampa = 0.3f;
        private const float PasilloAlPie = 2f;
        /// <summary>Adornos que no se apagan sino que se recorren (las pruebas de T-505 cuentan los cargadores de la entrada del casino).</summary>
        private static readonly string[] SeRecorren = { "CY10_CargadorCalle" };
        private static readonly string[] Subgrupos = { "Casino" };

        /// <summary>Un puente: su z, el lado de la escalera (x0..x1 en la banqueta) y hacia dónde baja (+1 norte, -1 sur).</summary>
        public static readonly (string nombre, float z, float x0, float x1, int signo)[] Puentes =
        {
            ("Puente_Sur", 38f, 37.15f, 38.55f, 1),
            ("Puente_Nova", 72f, 55.75f, 57f, -1),
        };

        private const float HuellaFuera = 0.34f;
        private const int EscalonesFuera = 39;
        private const float HuellaDentro = 0.32f;
        private const int EscalonesDentro = 6;

        /// <summary>Grupos de adornos de la banqueta y cuántos niveles abajo del grupo está cada adorno (ArteNoche los agrupa por calle).</summary>
        private static readonly (string grupo, int nivel)[] AdornosQueSeApagan =
            { ("T505_NeonAlto/Piezas", 1), ("Barrio/ArteNoche", 2), ("Barrio/Ambientacion", 1) };

        [MenuItem("Mecha Cyberpunk/S2n/9 Puentes y pasarelas")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Material acero = Cargar("Assets/Arte/Exteriores/Materiales/T506_Final_Acero.mat");
            Material barandal = Cargar("Assets/Arte/Exteriores/Materiales/T508_Barandal.mat");
            Material[] neones = { Cargar("Assets/Arte/Exteriores/Materiales/Luz_Verde.mat"), Cargar("Assets/Arte/Exteriores/Materiales/Luz_Magenta.mat") };
            int i = 0;
            foreach (var p in Puentes)
            {
                Armar(raiz, p.nombre, p.z, p.x0, p.x1, p.signo, acero, barandal, neones[i % neones.Length]);
                i++;
            }
            ColocarS2.Guardar("s2n-puentes");
        }

        public static void ColocarYFotos()
        {
            Colocar();
            Fotos();
        }

        private static Material Cargar(string ruta)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                Debug.LogError("[Arte] T-809: falta " + ruta);
            }
            return m;
        }

        private static void Armar(Transform raiz, string nombre, float z, float x0, float x1, int s, Material acero, Material barandal, Material neon)
        {
            Transform puente = new GameObject(nombre).transform;
            puente.SetParent(raiz, false);
            Transform visual = Grupo(puente, "Visual");
            Transform colision = Grupo(puente, "Colision");
            float xc = (x0 + x1) * 0.5f;

            // 1. Cubierta: piso, pretiles y topes en las puntas (el N02 no tiene colisión).
            Solido(colision, "Cubierta", PuntaOeste, PuntaEste, AltoCubierta - 0.4f, AltoCubierta, z - MedioAnchoCubierta, z + MedioAnchoCubierta);
            Solido(colision, "Pretil_Sur", PuntaOeste, PuntaEste, AltoCubierta, AltoPretil, z - MedioAnchoPretil, z - MedioAnchoCubierta);
            Solido(colision, "Pretil_Norte", PuntaOeste, PuntaEste, AltoCubierta, AltoPretil, z + MedioAnchoCubierta, z + MedioAnchoPretil);
            Solido(colision, "Tope_Oeste", PuntaOeste - 0.3f, PuntaOeste, AltoCubierta, AltoCubierta + AltoTopeInvisible + 1f, z - MedioAnchoPretil, z + MedioAnchoPretil);
            Solido(colision, "Tope_Este", PuntaEste, PuntaEste + 0.3f, AltoCubierta, AltoCubierta + AltoTopeInvisible + 1f, z - MedioAnchoPretil, z + MedioAnchoPretil);

            // 2. Descanso sobre el pretil (de la orilla de la cubierta a fuera del borde).
            float zDentro = z + s * (MedioAnchoCubierta - 0.1f);
            float zFuera = z + s * (MedioAnchoPretil + 0.7f);
            Caja(visual, "Descanso", acero, x0, x1, AltoDescanso - 0.12f, AltoDescanso, Min(zDentro, zFuera), Max(zDentro, zFuera), true);
            Barandales(visual, colision, barandal, neon, x0, x1, zDentro, AltoDescanso, zFuera, AltoDescanso);

            // 3. Escalones de adentro: del descanso a la cubierta.
            float zCubierta = zDentro - s * EscalonesDentro * HuellaDentro;
            Tramo(visual, colision, acero, barandal, neon, x0, x1, zDentro, AltoDescanso, zCubierta, AltoCubierta, EscalonesDentro, -s, false);

            // 4. Escalera de afuera: del descanso a la banqueta, con postes de apoyo.
            float zBanqueta = zFuera + s * EscalonesFuera * HuellaFuera;
            Tramo(visual, colision, acero, barandal, neon, x0, x1, zFuera, AltoDescanso, zBanqueta, AltoBanqueta, EscalonesFuera, s, true);
            // Más un pasillo al pie de la escalera para llegar a ella.
            ApagarAdornos(x0, x1, Min(zFuera, zBanqueta + s * PasilloAlPie), Max(zFuera, zBanqueta + s * PasilloAlPie));

            // 5. Camino para la prueba: banqueta, pie de la escalera, descanso (afuera y adentro), pie de los escalones de adentro, centro del puente.
            Transform camino = Grupo(puente, "Camino");
            Punto(camino, "P0", new Vector3(xc, AltoBanqueta, zBanqueta + s * PasilloAlPie));
            Punto(camino, "P1", new Vector3(xc, AltoBanqueta, zBanqueta));
            Punto(camino, "P2", new Vector3(xc, AltoDescanso, zFuera));
            Punto(camino, "P3", new Vector3(xc, AltoDescanso, zDentro));
            Punto(camino, "P4", new Vector3(xc, AltoCubierta, zCubierta - s * 0.6f));
            Punto(camino, "P5", new Vector3(CentroAvenida, AltoCubierta, z));
            ColocarS2.Informe.AppendLine(nombre + ": cubierta a " + AltoCubierta + " m, escalera x " + x0 + ".." + x1 + ", de z " + zBanqueta.ToString("F1")
                + " (banqueta) a " + zFuera.ToString("F1") + " (descanso a " + AltoDescanso + " m) y " + zCubierta.ToString("F1") + " (cubierta)");
        }

        /// <summary>Tramo de escalones de (zA, yA) a (zB, yB): escalones, largueros, barandales y una rampa invisible bajo las narices.</summary>
        private static void Tramo(Transform visual, Transform colision, Material acero, Material barandal, Material neon, float x0, float x1,
            float zA, float yA, float zB, float yB, int escalones, int s, bool apoyos)
        {
            float huella = Mathf.Abs(zB - zA) / escalones;
            float contrahuella = (yA - yB) / escalones;
            for (int k = 1; k <= escalones; k++)
            {
                // El escalón k está a k huellas del descanso y una contrahuella más abajo cada vez.
                float y = yA - k * contrahuella;
                float zi = zA + s * (k - 1) * huella;
                float zf = zA + s * k * huella;
                Caja(visual, "Escalon", acero, x0 + 0.05f, x1 - 0.05f, y - GrosorEscalon, y, Min(zi, zf), Max(zi, zf), false);
            }
            // Largueros a los dos lados, inclinados.
            Viga(visual, "Larguero", acero, new Vector3(x0 + 0.04f, yA - 0.15f, zA), new Vector3(x0 + 0.04f, yB - 0.15f, zB), 0.08f, 0.3f);
            Viga(visual, "Larguero", acero, new Vector3(x1 - 0.04f, yA - 0.15f, zA), new Vector3(x1 - 0.04f, yB - 0.15f, zB), 0.08f, 0.3f);
            // Rampa invisible: su cara de arriba pasa por las narices de los escalones (la pendiente queda bajo los 45° del jugador).
            Rampa(colision, x0, x1, new Vector3(0f, yA, zA), new Vector3(0f, yB, zB));
            Barandales(visual, colision, barandal, neon, x0, x1, zA, yA, zB, yB);
            if (!apoyos)
            {
                return;
            }
            float largo = Mathf.Abs(zB - zA);
            for (float d = SeparacionApoyos; d < largo - 1f; d += SeparacionApoyos)
            {
                float t = d / largo;
                float y = Mathf.Lerp(yA, yB, t) - 0.3f;
                float zz = Mathf.Lerp(zA, zB, t);
                foreach (float x in new[] { x0 + 0.06f, x1 - 0.06f })
                {
                    Caja(visual, "Apoyo", acero, x - 0.06f, x + 0.06f, AltoBanqueta, y, zz - 0.06f, zz + 0.06f, false);
                }
            }
        }

        /// <summary>Barandal a los dos lados de (zA, yA)-(zB, yB): postes, pasamanos con neón y un tope invisible que no deja caer.</summary>
        private static void Barandales(Transform visual, Transform colision, Material barandal, Material neon, float x0, float x1, float zA, float yA, float zB, float yB)
        {
            foreach (float x in new[] { x0 + 0.03f, x1 - 0.03f })
            {
                Vector3 a = new Vector3(x, yA + AltoBarandal, zA);
                Vector3 b = new Vector3(x, yB + AltoBarandal, zB);
                Viga(visual, "Pasamanos", barandal, a, b, 0.05f, 0.05f);
                Viga(visual, "Neon", neon, a - Vector3.up * 0.08f, b - Vector3.up * 0.08f, 0.02f, 0.02f, false);
                Viga(visual, "Travesano", barandal, a - Vector3.up * 0.5f, b - Vector3.up * 0.5f, 0.03f, 0.03f);
                float largo = Mathf.Abs(zB - zA);
                int postes = Mathf.Max(1, Mathf.CeilToInt(largo / SeparacionPostes));
                for (int k = 0; k <= postes; k++)
                {
                    float t = (float)k / postes;
                    float y = Mathf.Lerp(yA, yB, t);
                    float zz = Mathf.Lerp(zA, zB, t);
                    Caja(visual, "Poste", barandal, x - 0.025f, x + 0.025f, y, y + AltoBarandal, zz - 0.025f, zz + 0.025f, false);
                }
                // Tope invisible: una tabla delgada inclinada, alta, por el lado del barandal.
                GameObject tope = new GameObject("Tope");
                tope.transform.SetParent(colision, false);
                Vector3 centro = (a + b) * 0.5f - Vector3.up * (AltoBarandal - AltoTopeInvisible * 0.5f);
                tope.transform.position = centro;
                tope.transform.rotation = Quaternion.LookRotation(new Vector3(0f, b.y - a.y, b.z - a.z).normalized, Vector3.up);
                BoxCollider c = tope.AddComponent<BoxCollider>();
                c.size = new Vector3(0.1f, AltoTopeInvisible * Mathf.Cos(Mathf.Atan2(Mathf.Abs(b.y - a.y), Mathf.Max(0.001f, Mathf.Abs(b.z - a.z)))), Vector3.Distance(a, b));
            }
        }

        private static void Rampa(Transform colision, float x0, float x1, Vector3 a, Vector3 b)
        {
            GameObject rampa = new GameObject("Rampa");
            rampa.transform.SetParent(colision, false);
            Vector3 dir = (b - a).normalized;
            Quaternion giro = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 normal = giro * Vector3.up;
            rampa.transform.SetPositionAndRotation(new Vector3((x0 + x1) * 0.5f, 0f, 0f) + (a + b) * 0.5f - normal * (GrosorRampa * 0.5f), giro);
            BoxCollider c = rampa.AddComponent<BoxCollider>();
            // Un poco más larga que el tramo para que empalme con el descanso y con el piso.
            c.size = new Vector3(x1 - x0, GrosorRampa, Vector3.Distance(a, b) + 0.4f);
        }

        private static void ApagarAdornos(float x0, float x1, float z0, float z1)
        {
            Bounds volumen = new Bounds();
            volumen.SetMinMax(new Vector3(x0 - 0.2f, AltoBanqueta + 0.05f, z0), new Vector3(x1 + 0.2f, AltoDescanso + 2f, z1));
            HashSet<GameObject> apagados = new HashSet<GameObject>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.bounds.Intersects(volumen))
                {
                    continue;
                }
                string ruta = ColocarS2.Ruta(r.transform);
                var grupo = AdornosQueSeApagan.FirstOrDefault(g => ruta.StartsWith(g.grupo + "/"));
                if (grupo.grupo == null)
                {
                    continue;
                }
                // El adorno entero: el objeto que está 'nivel' niveles abajo del grupo (uno más si cae en un subgrupo, como Piezas/Casino).
                int profundidad = grupo.grupo.Split('/').Length + grupo.nivel;
                Transform adorno = AncestroA(r.transform, profundidad);
                if (Subgrupos.Contains(adorno.name) && adorno != r.transform)
                {
                    adorno = AncestroA(r.transform, profundidad + 1);
                }
                if (!apagados.Add(adorno.gameObject))
                {
                    continue;
                }
                if (SeRecorren.Any(adorno.name.StartsWith))
                {
                    // Los cargadores son parte de la entrada del casino (T-505): se recorren hacia la calle, fuera de la escalera.
                    Bounds b = Limites(adorno);
                    bool escaleraAlOeste = (x0 + x1) * 0.5f < CentroAvenida;
                    float dx = escaleraAlOeste ? x1 + 0.15f - b.min.x : x0 - 0.15f - b.max.x;
                    adorno.position += new Vector3(dx, 0f, 0f);
                    ColocarS2.Informe.AppendLine("  adorno recorrido " + dx.ToString("F2") + " m hacia la calle (estorbaba la escalera): " + ColocarS2.Ruta(adorno));
                    continue;
                }
                adorno.gameObject.SetActive(false);
                ColocarS2.Informe.AppendLine("  adorno apagado (estorbaba la escalera): " + ColocarS2.Ruta(adorno));
            }
        }

        private static Transform AncestroA(Transform t, int profundidad)
        {
            while (ColocarS2.Ruta(t).Split('/').Length > profundidad)
            {
                t = t.parent;
            }
            return t;
        }

        private static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ---------------------------------------------------------------- Piezas

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(padre, false);
            return g;
        }

        private static void Punto(Transform padre, string nombre, Vector3 p)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.position = p;
        }

        private static float Min(float a, float b) => Mathf.Min(a, b);
        private static float Max(float a, float b) => Mathf.Max(a, b);

        private static void Solido(Transform padre, string nombre, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            g.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            g.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        }

        /// <summary>Caja con su malla; con colisión solo si se pide (los escalones van sin ella: los sube la rampa).</summary>
        private static GameObject Caja(Transform padre, string nombre, Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool conColision)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = nombre;
            c.transform.SetParent(padre, false);
            c.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            c.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            c.GetComponent<Renderer>().sharedMaterial = m;
            if (!conColision)
            {
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
            GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic);
            return c;
        }

        private static void Viga(Transform padre, string nombre, Material m, Vector3 a, Vector3 b, float ancho, float alto, bool sombra = true)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = nombre;
            c.transform.SetParent(padre, false);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.position = (a + b) * 0.5f;
            c.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            c.transform.localScale = new Vector3(ancho, alto, Vector3.Distance(a, b));
            Renderer r = c.GetComponent<Renderer>();
            r.sharedMaterial = m;
            if (!sombra)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic);
        }

        // ---------------------------------------------------------------- Capturas

        /// <summary>Desde la calle y la vista desde arriba de cada puente, de día, en docs/capturas/s2n-puentes-*.png. No guarda la escena.</summary>
        [MenuItem("Mecha Cyberpunk/S2n/Fotos de los puentes")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>(FindObjectsInactive.Include);
            ciclo?.Aplicar(13f);
            const string P = "docs/capturas/s2n-puentes-";
            ColocarS2.Foto(new Vector3(44f, 1.7f, 63f), new Vector3(38f, 5f, 46f), P + "sur-calle.png", 70f);
            ColocarS2.Foto(new Vector3(50f, 1.7f, 46f), new Vector3(56f, 5f, 64f), P + "nova-calle.png", 70f);
            ColocarS2.Foto(new Vector3(CentroAvenida, AltoCubierta + 1.9f, 39.5f), new Vector3(CentroAvenida, 3f, 90f), P + "sur-arriba.png", 70f, 1000f);
            ColocarS2.Foto(new Vector3(CentroAvenida, AltoCubierta + 1.9f, 70.5f), new Vector3(CentroAvenida, 3f, 0f), P + "nova-arriba.png", 70f, 1000f);
            ColocarS2.Foto(new Vector3(38f, AltoDescanso + 1.7f, 44f), new Vector3(46f, 8f, 37f), P + "descanso.png", 70f);
            ColocarS2.LadoALado("docs/referencias-ciudad/neon-alto-noche.png", P + "sur-calle.png", P + "referencia.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }
    }
}
