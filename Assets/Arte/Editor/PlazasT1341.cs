using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1341: plazas de barrio armadas con instancias anidadas de los prefabs de T-1340 (árboles AR, banca, kiosco y jardinera PZ).
    /// Crea Plaza_Kiosco, Plaza_Esquina y Plaza_Banqueta en Assets/Arte/Zonas/PZ/Plazas/ y las captura de arriba y a la altura de los ojos.
    /// </summary>
    public static class PlazasT1341
    {
        public const string RutaArbolChico = "Assets/Arte/Zonas/AR/AR01_Arbol.prefab";
        public const string RutaArbolGrande = "Assets/Arte/Zonas/AR/AR02_Arbol.prefab";
        public const string RutaBanca = "Assets/Arte/Zonas/PZ/PZ01_Banca.prefab";
        public const string RutaKiosco = "Assets/Arte/Zonas/PZ/PZ02_Kiosco.prefab";
        public const string RutaJardinera = "Assets/Arte/Zonas/PZ/PZ03_Jardinera.prefab";
        public const string CarpetaPlazas = "Assets/Arte/Zonas/PZ/Plazas/";
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string RutaInforme = "Logs/t1341-plazas.txt";

        public const string PlazaKiosco = "Plaza_Kiosco";
        public const string PlazaEsquina = "Plaza_Esquina";
        public const string PlazaBanqueta = "Plaza_Banqueta";
        public static readonly string[] Plazas = { PlazaKiosco, PlazaEsquina, PlazaBanqueta };

        public const string GrupoArboles = "Arboles";
        public const string GrupoBancas = "Bancas";
        public const string GrupoJardineras = "Jardineras";
        public const string GrupoKiosco = "Kiosco";

        /// <summary>Grados que se suman a la rotación de cada banca: se corrige tras ver la captura (el frente del modelo es desconocido).</summary>
        public const float GiroBanca = 180f; // el frente del PZ01 mira a -Z (captura T-1340-PZ01_Banca)
        /// <summary>Grados que se suman a la rotación del kiosco: la escalera mira a un lado desconocido.</summary>
        public const float GiroKiosco = 0f;

        // Plaza_Kiosco (~24x24 m)
        private const int BancasKiosco = 8;
        private const float RadioBancasKiosco = 6.5f;
        /// <summary>Desfase (°) del círculo de bancas para no poner ninguna justo frente a la escalera del kiosco.</summary>
        private const float DesfaseBancasKiosco = 22.5f;
        private const float GradosVuelta = 360f;
        private const float EsquinaArbolKiosco = 8f;
        private const float EntradaMediaKiosco = 11f;
        private const float BordeJardineraKiosco = 11.2f;
        private const float DesplazamientoJardineraKiosco = 5.5f;

        // Plaza_Esquina (~10x10 m)
        private static readonly Vector3 ArbolEsquina = new Vector3(-3f, 0f, 3f);
        private static readonly Vector3 EsquinaLibre = new Vector3(3.5f, 0f, 3.5f);
        private static readonly Vector3[] BancasEsquina = { new Vector3(-4f, 0f, -2f), new Vector3(-1f, 0f, -4f) };
        // x, y, z y giro Y
        private static readonly Vector4[] JardinerasEsquina = { new Vector4(4.4f, 0f, -3f, 90f), new Vector4(-1f, 0f, 4.4f, 0f) };

        // Plaza_Banqueta (15 m a lo largo de X)
        private static readonly float[] ArbolesBanquetaX = { -6f, 0f, 6f }; // la jardinera del AR01 mide 3 m
        private static readonly float[] BancasBanquetaX = { -3f, 3f };
        private const float YawMiraCalle = 180f; // mirar hacia -Z

        // Capturas
        private const float AlturaOjos = 1.6f;
        private const float CampoVisionOjos = 60f;
        private static readonly Vector3 DireccionArriba = new Vector3(1f, 1.1f, -1f);

        /// <summary>Una pieza de plaza: instancia del prefab en un grupo, con posición y giro Y (°) locales a la plaza.</summary>
        public sealed class Pieza
        {
            public readonly string Grupo;
            public readonly string Ruta;
            public readonly Vector3 Posicion;
            public readonly float Yaw;

            public Pieza(string grupo, string ruta, Vector3 posicion, float yaw)
            {
                Grupo = grupo;
                Ruta = ruta;
                Posicion = posicion;
                Yaw = yaw;
            }
        }

        /// <summary>Giro Y (°) de una banca en <paramref name="posicion"/> para que su frente mire a <paramref name="objetivo"/>, con GiroBanca.</summary>
        public static float YawBancaHacia(Vector3 posicion, Vector3 objetivo)
        {
            Vector3 d = objetivo - posicion;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + GiroBanca;
        }

        public static List<Pieza> PiezasDe(string plaza)
        {
            List<Pieza> piezas = new List<Pieza>();
            switch (plaza)
            {
                case PlazaKiosco:
                    piezas.Add(new Pieza(GrupoKiosco, RutaKiosco, Vector3.zero, GiroKiosco));
                    for (int i = 0; i < BancasKiosco; i++)
                    {
                        float grados = DesfaseBancasKiosco + i * GradosVuelta / BancasKiosco;
                        float rad = grados * Mathf.Deg2Rad;
                        Vector3 p = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * RadioBancasKiosco;
                        piezas.Add(new Pieza(GrupoBancas, RutaBanca, p, YawBancaHacia(p, Vector3.zero)));
                    }
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        foreach (float sz in new[] { -1f, 1f })
                        {
                            piezas.Add(new Pieza(GrupoArboles, RutaArbolGrande, new Vector3(sx, 0f, sz) * EsquinaArbolKiosco, 0f));
                        }
                    }
                    piezas.Add(new Pieza(GrupoArboles, RutaArbolChico, new Vector3(0f, 0f, EntradaMediaKiosco), 0f));
                    piezas.Add(new Pieza(GrupoArboles, RutaArbolChico, new Vector3(0f, 0f, -EntradaMediaKiosco), 0f));
                    piezas.Add(new Pieza(GrupoArboles, RutaArbolChico, new Vector3(EntradaMediaKiosco, 0f, 0f), 0f));
                    piezas.Add(new Pieza(GrupoArboles, RutaArbolChico, new Vector3(-EntradaMediaKiosco, 0f, 0f), 0f));
                    // una jardinera por lado, entre el árbol de la esquina y el de la entrada media, alineada con el borde
                    piezas.Add(new Pieza(GrupoJardineras, RutaJardinera, new Vector3(DesplazamientoJardineraKiosco, 0f, BordeJardineraKiosco), 0f));
                    piezas.Add(new Pieza(GrupoJardineras, RutaJardinera, new Vector3(BordeJardineraKiosco, 0f, -DesplazamientoJardineraKiosco), 90f));
                    piezas.Add(new Pieza(GrupoJardineras, RutaJardinera, new Vector3(-DesplazamientoJardineraKiosco, 0f, -BordeJardineraKiosco), 0f));
                    piezas.Add(new Pieza(GrupoJardineras, RutaJardinera, new Vector3(-BordeJardineraKiosco, 0f, DesplazamientoJardineraKiosco), 90f));
                    break;
                case PlazaEsquina:
                    piezas.Add(new Pieza(GrupoArboles, RutaArbolGrande, ArbolEsquina, 0f));
                    foreach (Vector3 p in BancasEsquina)
                    {
                        piezas.Add(new Pieza(GrupoBancas, RutaBanca, p, YawBancaHacia(p, EsquinaLibre)));
                    }
                    foreach (Vector4 j in JardinerasEsquina)
                    {
                        piezas.Add(new Pieza(GrupoJardineras, RutaJardinera, new Vector3(j.x, j.y, j.z), j.w));
                    }
                    break;
                case PlazaBanqueta:
                    foreach (float x in ArbolesBanquetaX)
                    {
                        piezas.Add(new Pieza(GrupoArboles, RutaArbolChico, new Vector3(x, 0f, 0f), 0f));
                    }
                    foreach (float x in BancasBanquetaX)
                    {
                        piezas.Add(new Pieza(GrupoBancas, RutaBanca, new Vector3(x, 0f, 0f), YawMiraCalle + GiroBanca));
                    }
                    break;
            }
            return piezas;
        }

        public static string RutaPrefab(string plaza)
        {
            return CarpetaPlazas + plaza + ".prefab";
        }

        [MenuItem("Mecha Cyberpunk/T-1341/Crear plazas")]
        public static void Crear()
        {
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            StringBuilder informe = new StringBuilder();
            Directory.CreateDirectory(CarpetaPlazas);
            foreach (string plaza in Plazas)
            {
                informe.AppendLine(CrearPlaza(plaza));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Arte] T-1341 plazas:\n" + informe);
        }

        private static string CrearPlaza(string plaza)
        {
            List<Pieza> piezas = PiezasDe(plaza);
            HashSet<string> faltan = new HashSet<string>();
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
            foreach (Pieza pieza in piezas)
            {
                if (prefabs.ContainsKey(pieza.Ruta) || faltan.Contains(pieza.Ruta))
                {
                    continue;
                }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pieza.Ruta);
                if (prefab == null)
                {
                    faltan.Add(pieza.Ruta);
                }
                else
                {
                    prefabs[pieza.Ruta] = prefab;
                }
            }
            if (faltan.Count > 0)
            {
                return plaza + "\tNO CREADA, faltan prefabs: " + string.Join(", ", faltan);
            }

            GameObject raiz = new GameObject(plaza);
            Dictionary<string, Transform> grupos = new Dictionary<string, Transform>();
            Dictionary<string, int> cuentas = new Dictionary<string, int>();
            foreach (Pieza pieza in piezas)
            {
                if (!grupos.TryGetValue(pieza.Grupo, out Transform grupo))
                {
                    grupo = new GameObject(pieza.Grupo).transform;
                    grupo.SetParent(raiz.transform, false);
                    grupos[pieza.Grupo] = grupo;
                    cuentas[pieza.Grupo] = 0;
                }
                cuentas[pieza.Grupo]++;
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[pieza.Ruta]);
                instancia.name = Path.GetFileNameWithoutExtension(pieza.Ruta) + "_" + cuentas[pieza.Grupo].ToString("00");
                instancia.transform.SetParent(grupo, false);
                instancia.transform.localPosition = pieza.Posicion;
                instancia.transform.localRotation = Quaternion.Euler(0f, pieza.Yaw, 0f);
            }

            Bounds b = ArteT250.Limites(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(plaza));
            Object.DestroyImmediate(raiz);
            List<string> resumen = new List<string>();
            foreach (KeyValuePair<string, int> c in cuentas)
            {
                resumen.Add(c.Key + " " + c.Value);
            }
            return string.Format("{0}\tpiezas {1} ({2})\tlimites {3:F1}x{4:F1}x{5:F1} m", plaza, piezas.Count, string.Join(", ", resumen), b.size.x, b.size.y, b.size.z);
        }

        // ------------------------------------------------------------------ capturas

        [MenuItem("Mecha Cyberpunk/T-1341/Capturar plazas")]
        public static void Capturar()
        {
            GameObject jugadorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador);
            foreach (string plaza in Plazas)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(plaza));
                if (prefab == null)
                {
                    Debug.LogWarning("[Arte] T-1341: falta " + RutaPrefab(plaza) + ", sin captura.");
                    continue;
                }
                Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                PonerAtardecer();
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                instancia.transform.position = Vector3.zero;
                Bounds b = ArteT250.Limites(instancia);

                GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                suelo.name = "SueloCaptura";
                suelo.transform.position = new Vector3(b.center.x, 0f, b.center.z);
                suelo.transform.localScale = new Vector3(b.size.x * 0.3f + 4f, 1f, b.size.z * 0.3f + 4f);

                DatosOjos(plaza, b, out Vector3 entrada, out Vector3 mira, out Vector3 lugarJugador);
                if (jugadorPrefab != null)
                {
                    GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(jugadorPrefab, escena);
                    jugador.transform.position = lugarJugador;
                    jugador.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(mira - lugarJugador, Vector3.up));
                }

                GameObject camaraObjeto = new GameObject("CamaraCaptura");
                camaraObjeto.tag = "MainCamera";
                Camera camara = camaraObjeto.AddComponent<Camera>();
                ArteT250.Encuadrar(camara, b, DireccionArriba);
                ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "T-1341-" + plaza + "-arriba.png"));

                camara.fieldOfView = CampoVisionOjos;
                camara.transform.position = entrada + Vector3.up * AlturaOjos;
                camara.transform.LookAt(new Vector3(mira.x, AlturaOjos, mira.z));
                ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "T-1341-" + plaza + "-ojos.png"));
            }
        }

        /// <summary>Entrada (donde se ponen los ojos), punto al que se mira y lugar del jugador, según la plaza.</summary>
        private static void DatosOjos(string plaza, Bounds b, out Vector3 entrada, out Vector3 mira, out Vector3 jugador)
        {
            mira = Vector3.zero;
            switch (plaza)
            {
                case PlazaKiosco:
                    entrada = new Vector3(-4f, 0f, -EntradaMediaKiosco - 2f); // a un lado del AR01 de la entrada, no detrás de su tronco
                    jugador = new Vector3(-2.5f, 0f, -EntradaMediaKiosco + 1f);
                    break;
                case PlazaEsquina:
                    entrada = EsquinaLibre + new Vector3(1.5f, 0f, 1.5f);
                    jugador = EsquinaLibre + new Vector3(1.5f, 0f, -0.5f);
                    break;
                default:
                    entrada = new Vector3(0f, 0f, -b.extents.z - 6f);
                    jugador = new Vector3(1f, 0f, -b.extents.z - 3.5f);
                    break;
            }
        }

        private static void PonerAtardecer()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.33f, 0.4f);
            GameObject luz = new GameObject("Sol");
            Light sol = luz.AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.color = new Color(1f, 0.7f, 0.45f);
            sol.intensity = 1.1f;
            sol.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(20f, 215f, 0f);
        }

        /// <summary>Batchmode: crea las plazas y las captura.</summary>
        public static void CrearYCapturar()
        {
            Crear();
            Capturar();
        }
    }
}
