using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1404 (Autopista elevada, ronda 4): corre DESPUÉS de ZonaAutopistaT1402.Aplicar. Edita el prefab T1356_Autopista y pone lo nuevo bajo "T1404_Autopista" (se borra y rehace).
    /// (1) Chatarra AU03 (la de T-1386 y la de T-1402): cada montón se reduce para caber en ~4.5 m de largo x ~2 m de alto (escala uniforme, base en y = 0, mismo centro) y se quitan los
    /// que quedan a menos de 12 m de alguna cámara K18. (2) Luces de sodio colgadas del tablero (spots chicos sin sombras, 6 como máximo) con un charco debajo de cada una.
    /// La cámara K18 vuelve a la calle baja de la ronda 2, girada hacia el norte para que entren el tablero y la rampa (ver Fotos). Es idempotente.
    /// Métodos: Aplicar (Logs/t1404-autopista-aplicar.txt), Validar (Logs/t1404-autopista-validar.txt), Fotos (corrida aparte, docs/capturas/t-1404/).
    /// </summary>
    public static class ZonaAutopistaT1404
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1404_Autopista";

        private const float LargoChatarra = 4.5f;
        private const float AltoChatarra = 2f;
        private const float RadioSinChatarra = 12f;
        private const int MaxLuces = 6;
        private const float YCharco = 0.04f;
        private static readonly StringBuilder informe = new StringBuilder();

        /// <summary>Cámaras de K18 (calle baja, ronda 2 girada al norte): nombre, posición, mira y campo vertical.</summary>
        private static readonly (string nombre, Vector3 desde, Vector3 hacia, float campo)[] CamarasK18 =
        {
            ("autopista-k18", new Vector3(-148f, 2.5f, 216.5f), new Vector3(-275f, 11f, 258f), 68f),
            ("autopista-k18b", new Vector3(-172f, 4f, 217f), new Vector3(-285f, 12f, 258f), 72f),
            ("autopista-k18c", new Vector3(-165f, 6f, 217f), new Vector3(-300f, 12f, 262f), 70f),
        };

        // ---------------------------------------------------------------- Utilidades

        private static Bounds? Caja(GameObject go, bool soloVisible)
        {
            Bounds? caja = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (soloVisible && (!r.enabled || r.name.StartsWith("Halo")))
                {
                    continue;
                }
                if (caja.HasValue)
                {
                    Bounds w = caja.Value;
                    w.Encapsulate(r.bounds);
                    caja = w;
                }
                else
                {
                    caja = r.bounds;
                }
            }
            return caja;
        }

        private static bool EsMonton(Transform t)
        {
            return t.name.StartsWith("AU03_") && t.parent != null && (t.parent.name == "Chatarra_AU03" || t.parent.name == "Chatarra");
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        /// <summary>Cajas de todo lo visible del prefab (menos lo de T1404, pisos grandes, fondo, cerro, halos, cables, charcos y líneas).</summary>
        private static List<Bounds> Ocupados(Transform raiz, Transform mia)
        {
            Transform acantilado = raiz.Find("Acantilado");
            Transform fondo = raiz.Find("Fondo");
            List<Bounds> lista = new List<Bounds>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || (mia != null && r.transform.IsChildOf(mia)))
                {
                    continue;
                }
                if (r.name == "Cables" || r.name.StartsWith("Halo") || r.name.StartsWith("Haz_") || r.name.StartsWith("Charco") || r.name.StartsWith("Linea"))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.x > 60f || b.size.z > 60f || b.size.y < 0.4f || (acantilado != null && r.transform.IsChildOf(acantilado)) || (fondo != null && r.transform.IsChildOf(fondo)))
                {
                    continue;
                }
                lista.Add(b);
            }
            return lista;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1404/Autopista ronda 4 (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            informe.AppendLine("T-1404 Autopista (ronda 4)");
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform vieja = raiz.Find(NombreRaiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                AjustarChatarra(raiz);
                KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1404, raiz);
                PonerLuces(o, raiz);
                KitS2c.Terminar(o);
                int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                informe.AppendLine("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (nuevas " + (lucesDespues - lucesAntes) + ", tope " + MaxLuces + ").");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1404-autopista-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1404 autopista aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Chatarra

        /// <summary>
        /// Cada montón AU03 (hijos de Chatarra_AU03 de T-1386 y de Chatarra de T-1402): si está a menos de 12 m de una cámara K18 se quita; si no, se escala de forma uniforme
        /// con f = min(1, 4.5 / largo mayor, 2 / alto) (solo si f &lt; 0.98, así se puede correr dos veces), manteniendo el centro en planta y la base en y = 0.
        /// </summary>
        private static void AjustarChatarra(Transform raiz)
        {
            List<Transform> montones = raiz.GetComponentsInChildren<Transform>(true).Where(EsMonton).ToList();
            int quitados = 0, escalados = 0, sinCambio = 0;
            foreach (Transform m in montones)
            {
                Bounds? c0 = Caja(m.gameObject, true);
                if (!c0.HasValue)
                {
                    continue;
                }
                Vector3 centro = c0.Value.center;
                bool cerca = CamarasK18.Any(c => DistanciaPlana(c.desde, centro) < RadioSinChatarra);
                if (cerca)
                {
                    informe.AppendLine("AU03 " + m.name + " (" + m.parent.name + ") quitado: a menos de " + RadioSinChatarra + " m de una cámara K18, en " + centro.ToString("F1"));
                    Object.DestroyImmediate(m.gameObject);
                    quitados++;
                    continue;
                }
                float largo = Mathf.Max(c0.Value.size.x, c0.Value.size.z);
                float f = Mathf.Min(1f, LargoChatarra / Mathf.Max(0.1f, largo), AltoChatarra / Mathf.Max(0.1f, c0.Value.size.y));
                if (f >= 0.98f)
                {
                    sinCambio++;
                    continue;
                }
                m.localScale *= f;
                Bounds? c1 = Caja(m.gameObject, true);
                if (c1.HasValue)
                {
                    m.position += new Vector3(centro.x - c1.Value.center.x, -c1.Value.min.y, centro.z - c1.Value.center.z);
                }
                escalados++;
                informe.AppendLine("AU03 " + m.name + ": de " + c0.Value.size.ToString("F1") + " a " + (c1.HasValue ? c1.Value.size.ToString("F1") : "?") + " (factor " + f.ToString("F2") + ").");
            }
            informe.AppendLine("Chatarra AU03: " + montones.Count + " montones revisados, " + escalados + " reducidos, " + quitados + " quitados por estar cerca de K18, " + sinCambio + " ya de buen tamaño.");
        }

        // ---------------------------------------------------------------- Luces y charcos bajo el tablero

        /// <summary>Base de la losa del tablero principal en (x, z): el mínimo y de los renderers de Tablero/Principal que cubren ese punto y no son columnas (min y &gt; 3).</summary>
        private static float BajoLosa(Transform raiz, float x, float z)
        {
            Transform tab = raiz.Find("Tablero/Principal");
            float m = float.MaxValue;
            if (tab != null)
            {
                foreach (Renderer r in tab.GetComponentsInChildren<Renderer>(false))
                {
                    Bounds b = r.bounds;
                    if (b.min.y > 3f && x >= b.min.x - 0.5f && x <= b.max.x + 0.5f && z >= b.min.z - 0.5f && z <= b.max.z + 0.5f)
                    {
                        m = Mathf.Min(m, b.min.y);
                    }
                }
            }
            return m == float.MaxValue ? -1f : m;
        }

        private static void PonerLuces(KitS2c.Obra o, Transform raiz)
        {
            Transform luces = new GameObject("Luces").transform;
            luces.SetParent(o.Raiz, false);
            Transform charcos = new GameObject("Charcos").transform;
            charcos.SetParent(o.Raiz, false);
            List<Vector3> columnas = T.Columnas(T.Principal());
            List<Bounds> ocupados = Ocupados(raiz, o.Raiz);
            float[] xs = { -128f, -150f, -178f, -205f, -232f, -258f, -285f, -118f };
            int n = 0;
            foreach (float x in xs)
            {
                if (n >= MaxLuces)
                {
                    break;
                }
                const float z = 244f;
                if (columnas.Any(q => Mathf.Abs(q.x - x) < 3.5f && Mathf.Abs(q.z - z) < 4f))
                {
                    informe.AppendLine("Luz en x " + x + ": junto a una columna, se omite.");
                    continue;
                }
                float y = BajoLosa(raiz, x, z);
                if (y < 0f)
                {
                    informe.AppendLine("Luz en x " + x + ": no hay losa del tablero encima, se omite.");
                    continue;
                }
                Light l = KitS2c.Luz(luces, new Vector3(x, y - 0.35f, z), LightType.Spot, KitS2c.Sodio, KitS2c.IntensidadPoste * 0.9f, 14f, 105f, Vector3.down);
                l.gameObject.name = "LuzSodio_" + n;
                float largo = o.Rango(2.6f, 3.6f), ancho = o.Rango(1.8f, 2.4f);
                Vector3 c = new Vector3(x, YCharco, z);
                Bounds caja = new Bounds(new Vector3(c.x, 0.3f, c.z), new Vector3(largo + 0.4f, 0.5f, largo + 0.4f));
                if (!ocupados.Any(b => b.Intersects(caja)))
                {
                    KitS2c.Charco(o, charcos, c, largo, ancho, o.Rango(0f, 180f));
                }
                informe.AppendLine("Luz de sodio " + n + " en " + l.transform.position.ToString("F1") + " (losa en y " + y.ToString("F1") + "), sin sombras.");
                n++;
            }
            informe.AppendLine("Luces de sodio bajo el tablero: " + n + " con " + charcos.childCount + " charcos.");
        }

        // ---------------------------------------------------------------- Validar

        [MenuItem("Mecha Cyberpunk/T-1404/Autopista ronda 4 (validar)")]
        public static void Validar()
        {
            StringBuilder s = new StringBuilder("T-1404 validar Autopista\n");
            int fallos = 0;
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mia = raiz.Find(NombreRaiz);
                if (mia == null)
                {
                    s.AppendLine("FALLO: no existe " + NombreRaiz);
                    fallos++;
                }
                else
                {
                    Light[] luces = mia.GetComponentsInChildren<Light>(true);
                    s.AppendLine("Luces nuevas: " + luces.Length + " (tope " + MaxLuces + ")");
                    fallos += luces.Length > MaxLuces ? 1 : 0;
                    foreach (Light l in luces)
                    {
                        if (l.shadows != LightShadows.None || l.type != LightType.Spot)
                        {
                            s.AppendLine("FALLO: luz " + l.name + " con sombras o que no es spot.");
                            fallos++;
                        }
                    }
                    foreach (Transform m in raiz.GetComponentsInChildren<Transform>(true).Where(EsMonton))
                    {
                        Bounds? c = Caja(m.gameObject, true);
                        if (!c.HasValue)
                        {
                            continue;
                        }
                        float largo = Mathf.Max(c.Value.size.x, c.Value.size.z);
                        if (largo > LargoChatarra + 0.3f || c.Value.size.y > AltoChatarra + 0.3f)
                        {
                            s.AppendLine("TAMAÑO " + m.name + ": " + c.Value.size.ToString("F1"));
                            fallos++;
                        }
                        if (Mathf.Abs(c.Value.min.y) > 0.15f)
                        {
                            s.AppendLine("ALTURA " + m.name + ": base en y=" + c.Value.min.y.ToString("F2"));
                            fallos++;
                        }
                        foreach ((string nombre, Vector3 desde, Vector3 hacia, float campo) cam in CamarasK18)
                        {
                            if (DistanciaPlana(cam.desde, c.Value.center) < RadioSinChatarra)
                            {
                                s.AppendLine("CERCA de " + cam.nombre + ": " + m.name);
                                fallos++;
                            }
                        }
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            s.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1404-autopista-validar.txt", s.ToString());
            Debug.Log("[Arte] T-1404 autopista validar:\n" + s);
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>
        /// K18 desde la calle baja (como la ronda 2) girada hacia el norte: casas a la izquierda, tablero y rampa a la derecha, boca del túnel al fondo.
        /// Tres alturas: 2.5 m (k18), 4 m (k18b) y 6 m (k18c). No guarda nada; corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1404/Autopista ronda 4 (fotos)")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1404");
            Directory.CreateDirectory("Logs");
            foreach ((string nombre, Vector3 desde, Vector3 hacia, float campo) c in CamarasK18)
            {
                string juego = "Logs/t1404-" + c.nombre + "-juego.png";
                ColocarS2cLook.Foto(c.desde, c.hacia, juego, c.campo);
                ColocarS2.LadoALado("imageassets/referencias/look/K18_AutopistaElevada.png", juego, "docs/capturas/t-1404/" + c.nombre + ".png");
            }
        }
    }
}
