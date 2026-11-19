using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1462, paso 1: diagnóstico de lo que se ve mal desde arriba. Con Ciudad_Prologo abierta, para las 4 cámaras aéreas de Vista19b (A11, A13, A15, A18)
    /// y una rejilla de alturas (40, 80, 150, 250 m) sobre el centro de cada zona nueva, mirando a 45 grados hacia abajo en 4 rumbos, lista los renderers
    /// dentro del frustum que sean sospechosos:
    ///  [A] bounds enormes (más de 60 m en algún eje) o planos y grandes (paneles, cartas, muros de fondo)
    ///  [B] flotan (la base del bounds a más de 4 m sobre lo que hay debajo: raycast y bounds de otros renderers)
    ///  [C] material, shader, malla u objeto con nombre de fondo lejano (skyline, SK01, T513, T1363, Horizonte, fondo, lejano) o shader sin niebla / unlit
    ///  [D] color casi blanco o azul liso y grande en pantalla.
    /// El porcentaje de pantalla es el del rectángulo del bounds proyectado (no descuenta lo que lo tape).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.Correr19nov.Correr -pasos AereasT1462.Diagnosticar
    /// Escribe Logs/t1462-diagnostico.txt y las fotos Logs/t1462-diag-*.png (candidatos grandes pintados de colores; leyenda en el txt). No guarda la escena.
    /// </summary>
    public static class AereasT1462
    {
        public const string RutaDiagnostico = "Logs/t1462-diagnostico.txt";
        public const string PrefijoFoto = "Logs/t1462-diag-";
        private const float AspectoFoto = 1376f / 768f;
        private const float CampoRejilla = 60f;
        private const float LejosDiag = 1500f;
        private const float MinPctCandidatoFoto = 1f;
        private const int TopeLista = 200;
        private static readonly float[] Alturas = { 40f, 80f, 150f, 250f };
        private static readonly string[] Palabras = { "skyline", "sk01", "t513", "t1363", "horizonte", "fondo", "lejano", "sinniebla", "unlit" };

        private static readonly (string clave, string raiz)[] Zonas =
        {
            ("Malecon", "T1354_Malecon"), ("Cerro", "T1355_Cerro"), ("Autopista", "T1356_Autopista"), ("Puerto", "T1357_Puerto"), ("Orillas", "T1358_Orillas"),
        };

        private struct Cam
        {
            public string Nombre, Zona;
            public Vector3 Desde, Hacia;
            public float Campo;
            public bool Foto;
        }

        private sealed class Datos
        {
            public Renderer R;
            public Bounds B;
            public string Flags = "";
            public float MaxPct;
            public string CamMax = "";
            public int Camaras;
            public bool FlotaCalculado, Flota;
            public float Hueco;
        }

        public static void Diagnosticar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Directory.CreateDirectory("Logs");
            var log = new StringBuilder("T-1462 diagnóstico aéreo " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");
            Dictionary<Renderer, Datos> res = Analizar(log, out List<Cam> camaras, out Dictionary<string, List<Datos>> porCam);
            EscribirLista(log, res);
            File.WriteAllText(RutaDiagnostico, log.ToString());
            try
            {
                Fotos(camaras, porCam, log);
            }
            finally
            {
                File.WriteAllText(RutaDiagnostico, log.ToString());
            }
            Debug.Log("[Arte] " + log);
        }

        // ------------------------------------------------------------------ paso 2: arreglos

        public const string RutaAplicar = "Logs/t1462-aplicar.txt";
        public const string RutaValidar = "Logs/t1462-validar.txt";
        public const string RaizAereas = "T1462_Aereas";
        public const float UmbralFondoT513 = 25f;
        public const float UmbralBrumaAltura = 14f;
        public const float UmbralSkyline = 100f;
        public const float UmbralCiudadRespaldo = 25f;
        private const float AlturaFotoAerea = 25f;

        /// <summary>
        /// Arregla lo que se ve roto desde arriba. Idempotente; guarda la escena.
        ///  - T513_Fondo (paneles pintados: torres y bloques negros que flotan en la niebla): Tramo_3 apagado (era la aguja oscura sobre el mar en K11) y el resto
        ///    se apaga por altura (OcultarPorAltura, umbral 25 m).
        ///  - T1399_Bruma/Bruma_* (planos BrumaAltura a 10 m sobre la calle: desde arriba son la mancha blanca de A18): se apagan por altura (14 m).
        ///  - Skyline SK01 del Malecón (arcos aditivos a 495 m): se apagan por encima de 100 m, donde se ven como un muro de luz colgado.
        ///  - NieblaPorAltura en el objeto del CicloDiaNoche.
        /// Los componentes de altura viven en la raíz de escena T1462_Aereas, que Aplicar borra y rehace.
        /// </summary>
        public static void Aplicar()
        {
            ColocarS2.Abrir();
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var log = new StringBuilder("T-1462 Aplicar " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");
            OcultarPorAltura.AplicarATodos(float.NegativeInfinity);

            foreach (GameObject viejo in todos.Where(t => t != null && t.parent == null && t.name == RaizAereas).Select(t => t.gameObject).ToList())
            {
                Object.DestroyImmediate(viejo);
            }
            var raiz = new GameObject(RaizAereas);

            // Fondo T513.
            Transform t513 = todos.FirstOrDefault(t => t != null && t.parent == null && t.name == "T513_Fondo");
            if (t513 == null)
            {
                log.AppendLine("FALTA T513_Fondo en la escena");
            }
            else
            {
                Transform t3 = t513.Find("Tramo_3_fondo-suburbios");
                if (t3 != null)
                {
                    t3.gameObject.SetActive(false);
                    log.AppendLine("Tramo_3_fondo-suburbios apagado (aguja oscura sobre el mar en K11)");
                }
                CrearGrupo(raiz.transform, "Fondo_T513", t513.GetComponentsInChildren<Renderer>(true), UmbralFondoT513, log);
            }

            // Bruma de altura.
            Transform bruma = todos.FirstOrDefault(t => t != null && t.parent == null && t.name == "T1399_Bruma");
            if (bruma == null)
            {
                log.AppendLine("FALTA T1399_Bruma en la escena");
            }
            else
            {
                Renderer[] planos = bruma.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.name.StartsWith("Bruma_", StringComparison.Ordinal)).ToArray();
                CrearGrupo(raiz.transform, "BrumaAltura", planos, UmbralBrumaAltura, log);
            }

            // Skyline del Malecón.
            Transform malecon = todos.FirstOrDefault(t => t != null && t.parent == null && t.name == "T1354_Malecon");
            Transform sky = malecon == null ? null : malecon.Find("T1393_Malecon/Skyline");
            if (sky == null)
            {
                log.AppendLine("FALTA T1354_Malecon/T1393_Malecon/Skyline");
            }
            else
            {
                CrearGrupo(raiz.transform, "SkylineSK01", sky.GetComponentsInChildren<Renderer>(true), UmbralSkyline, log);
            }

            // Piso de ciudad lejana: bajo todo, solo se ve con la cámara por encima de 25 m (a nivel de calle no existe).
            CrearCiudadRespaldo(raiz.transform, log);

            // Niebla por altura.
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            if (ciclo == null)
            {
                log.AppendLine("FALTA CicloDiaNoche en la escena");
            }
            else if (ciclo.GetComponent<NieblaPorAltura>() == null)
            {
                ciclo.gameObject.AddComponent<NieblaPorAltura>();
                log.AppendLine("NieblaPorAltura agregado a " + ciclo.name);
            }
            else
            {
                log.AppendLine("NieblaPorAltura ya estaba en " + ciclo.name);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaAplicar, log.ToString());
            ColocarS2.Informe.Clear();
            ColocarS2.Informe.Append(log);
            ColocarS2.Guardar("t1462-aplicar-escena");
            Debug.Log("[Arte] " + log);
        }

        /// <summary>
        /// Para las aéreas: los 30 renderers visibles (sin oclusión) que más espacio ocupan en el tercio de arriba de la pantalla (lo que queda sobre el horizonte), con su ruta,
        /// malla, material y distancia. Ayuda a nombrar las cajas o bloques que se ven en el cielo.
        /// </summary>
        private static void ListarArriba(StringBuilder log, Cam c)
        {
            OcultarPorAltura.AplicarATodos(c.Desde.y);
            var go = new GameObject("T1462_CamArriba") { hideFlags = HideFlags.HideAndDontSave };
            Camera cam = go.AddComponent<Camera>();
            cam.aspect = AspectoFoto;
            cam.fieldOfView = c.Campo;
            cam.farClipPlane = LejosDiag;
            cam.transform.position = c.Desde;
            cam.transform.LookAt(c.Hacia);
            Plane[] planos = GeometryUtility.CalculateFrustumPlanes(cam);
            var lista = new List<(Renderer r, float area, float dist)>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer) || !GeometryUtility.TestPlanesAABB(planos, r.bounds))
                {
                    continue;
                }
                Bounds b = r.bounds;
                Vector3 v = cam.WorldToViewportPoint(b.center);
                if (v.z <= 0f || v.y < 0.62f)
                {
                    continue;
                }
                float area = PorcentajePantalla(cam, b);
                if (area >= 0.3f)
                {
                    lista.Add((r, area, Vector3.Distance(c.Desde, b.center)));
                }
            }
            log.AppendLine();
            log.AppendLine("== ARRIBA en " + c.Nombre + " (centro del renderer en el tercio superior, % de bounds)");
            foreach ((Renderer r, float area, float dist) in lista.OrderByDescending(x => x.area).Take(30))
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                string mats = string.Join("+", r.sharedMaterials.Where(m => m != null).Select(m => m.name + "{" + (m.shader != null ? m.shader.name : "?") + "}"));
                log.AppendLine("  " + area.ToString("0.0", CultureInfo.InvariantCulture) + "% a " + dist.ToString("0") + " m | " + RutaDe(r.transform) + " | malla " + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "?") + " | " + mats + " | tamaño " + V(r.bounds.size) + " centro " + V(r.bounds.center));
            }
            Object.DestroyImmediate(go);
        }

        public const string RutaMaterialRespaldo = "Assets/Arte/Mundo/Look/T1462_CiudadRespaldo.mat";
        public const string RutaVentanasLejanas = "Assets/Arte/Mundo/Zonas/T1363_VentanasLejanas.png";
        private const float YCiudadRespaldo = -8f;
        private const float LadoCiudadRespaldo = 4000f;
        private const float MetrosPorMosaico = 96f;
        private static readonly Vector3 CentroCiudadRespaldo = new Vector3(-60f, YCiudadRespaldo, -100f);

        /// <summary>
        /// Lo que queda fuera del mapa visto desde arriba era el azul liso del fondo. Un plano enorme con la textura de ventanas lejanas (T1363: pared oscura con ventanas
        /// cálidas, frías y magenta) bajo todo (y -8: bajo el mar y el piso de respaldo) lo hace leer como ciudad continua a oscuras con luces. Plano con material, sin collider ni sombras.
        /// </summary>
        private static void CrearCiudadRespaldo(Transform padre, StringBuilder log)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaVentanasLejanas);
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (tex == null || sh == null)
            {
                log.AppendLine("FALTA la textura " + RutaVentanasLejanas + " o el shader URP Unlit: sin CiudadRespaldo");
                return;
            }
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialRespaldo);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, RutaMaterialRespaldo);
            }
            mat.shader = sh;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(1.1f, 1.1f, 1.1f, 1f));
            mat.SetTextureScale("_BaseMap", Vector2.one * (LadoCiudadRespaldo / MetrosPorMosaico));
            EditorUtility.SetDirty(mat);
            GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plano.name = "CiudadRespaldo";
            Object.DestroyImmediate(plano.GetComponent<Collider>());
            plano.transform.SetParent(padre, false);
            plano.transform.SetPositionAndRotation(CentroCiudadRespaldo, Quaternion.Euler(90f, 0f, 0f));
            plano.transform.localScale = new Vector3(LadoCiudadRespaldo, LadoCiudadRespaldo, 1f);
            var r = plano.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            CrearGrupo(padre, "CiudadRespaldo_Arriba", new Renderer[] { r }, UmbralCiudadRespaldo, log, true);
            log.AppendLine("CiudadRespaldo: plano de " + LadoCiudadRespaldo.ToString("0") + " m a y " + YCiudadRespaldo.ToString("0") + ", mosaico de " + MetrosPorMosaico.ToString("0") + " m, visible sobre " + UmbralCiudadRespaldo.ToString("0") + " m");
        }

        private static void CrearGrupo(Transform padre, string nombre, Renderer[] renderers, float umbral, StringBuilder log, bool soloArriba = false)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            OcultarPorAltura o = go.AddComponent<OcultarPorAltura>();
            var so = new SerializedObject(o);
            SerializedProperty lista = so.FindProperty("renderers");
            lista.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
            so.FindProperty("umbral").floatValue = umbral;
            so.FindProperty("soloArriba").boolValue = soloArriba;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("Grupo " + nombre + ": " + renderers.Length + " renderers, se apagan sobre " + umbral.ToString("0") + " m");
        }

        /// <summary>
        /// Repite el diagnóstico con los arreglos puestos (cada cámara aplica OcultarPorAltura a su altura) y falla en voz alta si en cuadro queda:
        ///  - un renderer de T513_Fondo o BrumaAltura encendido en una cámara por encima de su umbral, o Tramo_3 activo;
        ///  - un sospechoso C (fondo, skyline) o D (blanco/azul liso) de más de 1 % de pantalla, salvo lo permitido (halos, haces, cables);
        ///  - la niebla por altura fuera de lo pedido (calle 1, 40-80 m entre 0.35 y 0.45, 150 m en 0.2 o menos).
        /// Los A y B (calles grandes, cables, letreros colgados) se cuentan pero no fallan: son legítimos. Escribe Logs/t1462-validar.txt. No guarda la escena.
        /// </summary>
        public static void Validar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Directory.CreateDirectory("Logs");
            var log = new StringBuilder("T-1462 Validar " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");
            int fallas = 0;
            OcultarPorAltura.AplicarATodos(float.NegativeInfinity);

            Transform t513 = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t != null && t.parent == null && t.name == "T513_Fondo");
            Transform t3 = t513 == null ? null : t513.Find("Tramo_3_fondo-suburbios");
            if (t3 != null && t3.gameObject.activeSelf)
            {
                fallas++;
                log.AppendLine("FALLA: Tramo_3_fondo-suburbios sigue activo");
            }
            OcultarPorAltura[] comps = Object.FindObjectsByType<OcultarPorAltura>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            log.AppendLine("componentes OcultarPorAltura: " + comps.Length + " (" + string.Join(", ", comps.Select(c => c.name + ":" + c.Cantidad + "@" + c.Umbral.ToString("0"))) + ")");
            if (comps.Length < 4)
            {
                fallas++;
                log.AppendLine("FALLA: faltan componentes OcultarPorAltura (corre Aplicar)");
            }
            if (Object.FindFirstObjectByType<NieblaPorAltura>() == null)
            {
                fallas++;
                log.AppendLine("FALLA: no hay NieblaPorAltura en la escena");
            }
            float f0 = CalculoNieblaAltura.Factor(0f), f40 = CalculoNieblaAltura.Factor(40f), f80 = CalculoNieblaAltura.Factor(80f), f150 = CalculoNieblaAltura.Factor(150f);
            log.AppendLine("niebla: factor 0 m " + f0.ToString("0.00") + ", 40 m " + f40.ToString("0.00") + ", 80 m " + f80.ToString("0.00") + ", 150 m " + f150.ToString("0.00"));
            if (f0 < 0.999f || f40 < 0.35f || f40 > 0.45f || f80 < 0.35f || f80 > 0.45f || f150 > 0.2f)
            {
                fallas++;
                log.AppendLine("FALLA: factores de niebla fuera de lo pedido");
            }

            Dictionary<Renderer, Datos> res = Analizar(log, out List<Cam> camaras, out Dictionary<string, List<Datos>> porCam, true);
            string[] permitidos = { "Halos", "HazTorre", "Haz", "FaroLejano", "Cables", "Cable", "Bruma_", "DerramesNeon", "Estelas", "T1464_Autopista", "PisoRespaldo", "Skyline", "CiudadRespaldo" };
            var camY = camaras.ToDictionary(c => c.Nombre, c => c.Desde.y);
            int sospechososC = 0;
            foreach (KeyValuePair<string, List<Datos>> kv in porCam)
            {
                float y = camY[kv.Key];
                OcultarPorAltura.AplicarATodos(y);
                var cam = camaras.First(c => c.Nombre == kv.Key);
                var go = new GameObject("T1462_CamVal") { hideFlags = HideFlags.HideAndDontSave };
                Camera cm = go.AddComponent<Camera>();
                cm.aspect = AspectoFoto;
                cm.fieldOfView = cam.Campo;
                cm.farClipPlane = LejosDiag;
                cm.transform.position = cam.Desde;
                cm.transform.LookAt(cam.Hacia);
                foreach (Datos d in kv.Value)
                {
                    if (d.R == null || !d.R.enabled)
                    {
                        continue;
                    }
                    string ruta = RutaDe(d.R.transform);
                    float pct = PorcentajePantalla(cm, d.B);
                    bool esT513 = ruta.StartsWith("T513_Fondo", StringComparison.Ordinal);
                    bool esBruma = d.R.name.StartsWith("Bruma_", StringComparison.Ordinal);
                    bool esSky = ruta.Contains("/Skyline/");
                    if (esT513 && y > UmbralFondoT513 || esBruma && y > UmbralBrumaAltura || esSky && y > UmbralSkyline)
                    {
                        fallas++;
                        log.AppendLine("FALLA: " + kv.Key + " (y " + y.ToString("0") + ") ve encendido " + ruta);
                        continue;
                    }
                    if ((d.Flags.Contains('C') || d.Flags.Contains('D')) && pct >= 1f && !permitidos.Any(p => ruta.Contains(p)))
                    {
                        sospechososC++;
                        fallas++;
                        log.AppendLine("FALLA: sospechoso [" + d.Flags + "] " + pct.ToString("0.0") + "% en " + kv.Key + ": " + ruta);
                    }
                }
                Object.DestroyImmediate(go);
            }
            foreach (Cam c in camaras.Where(x => x.Nombre.StartsWith("A1", StringComparison.Ordinal)))
            {
                ListarArriba(log, c);
            }
            OcultarPorAltura.AplicarATodos(float.NegativeInfinity);
            log.AppendLine("sospechosos C/D no permitidos: " + sospechososC + "; sospechosos totales (incluye A/B legítimos): " + res.Count);
            log.AppendLine(fallas == 0 ? "RESULTADO: OK" : "RESULTADO: " + fallas + " FALLAS");
            File.WriteAllText(RutaValidar, log.ToString());
            if (fallas > 0)
            {
                Debug.LogError("[Arte] T-1462 Validar: " + fallas + " fallas. Ver " + RutaValidar + "\n" + log);
            }
            else
            {
                Debug.Log("[Arte] " + log);
            }
        }

        // ------------------------------------------------------------------ análisis

        private static Dictionary<Renderer, Datos> Analizar(StringBuilder log, out List<Cam> camaras, out Dictionary<string, List<Datos>> porCam, bool conAltura = false)
        {
            List<Renderer> rend = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r != null && r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer) && r.gameObject.activeInHierarchy).ToList();
            var datos = new Datos[rend.Count];
            for (int i = 0; i < rend.Count; i++)
            {
                datos[i] = new Datos { R = rend[i], B = rend[i].bounds };
            }
            log.AppendLine("renderers activos: " + rend.Count);

            var rejilla = new Rejilla(datos);
            camaras = ListaCamaras(rend, log);
            porCam = new Dictionary<string, List<Datos>>();
            var res = new Dictionary<Renderer, Datos>();
            var go = new GameObject("T1462_CamDiag") { hideFlags = HideFlags.HideAndDontSave };
            Camera cam = go.AddComponent<Camera>();
            cam.aspect = AspectoFoto;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = LejosDiag;
            try
            {
                foreach (Cam c in camaras)
                {
                    cam.fieldOfView = c.Campo;
                    cam.transform.position = c.Desde;
                    cam.transform.LookAt(c.Hacia);
                    Plane[] planos = GeometryUtility.CalculateFrustumPlanes(cam);
                    if (conAltura)
                    {
                        OcultarPorAltura.AplicarATodos(c.Desde.y);
                    }
                    var lista = new List<Datos>();
                    int dentro = 0;
                    foreach (Datos d in datos)
                    {
                        if (d.R == null || (conAltura && !d.R.enabled) || !GeometryUtility.TestPlanesAABB(planos, d.B))
                        {
                            continue;
                        }
                        dentro++;
                        float pct = PorcentajePantalla(cam, d.B);
                        string flags = Sospechas(d, pct, rejilla);
                        if (flags.Length == 0)
                        {
                            continue;
                        }
                        if (!res.TryGetValue(d.R, out Datos acum))
                        {
                            res[d.R] = d;
                            acum = d;
                        }
                        foreach (char f in flags)
                        {
                            if (acum.Flags.IndexOf(f) < 0)
                            {
                                acum.Flags += f;
                            }
                        }
                        acum.Camaras++;
                        if (pct >= acum.MaxPct)
                        {
                            acum.MaxPct = pct;
                            acum.CamMax = c.Nombre;
                        }
                        lista.Add(d);
                    }
                    porCam[c.Nombre] = lista;
                    log.AppendLine("camara " + c.Nombre + " (" + c.Zona + ") desde " + V(c.Desde) + " hacia " + V(c.Hacia) + ": " + dentro + " renderers en cuadro, " + lista.Count + " sospechosos");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                if (conAltura)
                {
                    OcultarPorAltura.AplicarATodos(float.NegativeInfinity);
                }
            }
            return res;
        }

        private static List<Cam> ListaCamaras(List<Renderer> rend, StringBuilder log)
        {
            var l = new List<Cam>();
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // Las 4 de Vista19b, resueltas como lo hace Vista19b.Fotos.
            AgregarVista(l, todos, "A11", "Malecon", ZonaMaleconT1413.CamaraA11, ZonaMaleconT1413.MiraA11, ZonaMaleconT1413.CampoA11, null);
            AgregarVista(l, todos, "A13", "Cerro", new Vector3(-388f, 66f, -28f), new Vector3(-336f, 9f, 2f), 60f, "T1414_Cerro");
            AgregarVista(l, todos, "A15", "Puerto", new Vector3(326f, 66f, -60f), new Vector3(240f, 0f, -205f), 62f, "T1414_Puerto");
            AgregarVista(l, todos, "A18", "Autopista", new Vector3(-205f, 46f, 150f), new Vector3(-335f, 12f, 238f), 62f, "T1414_Autopista");

            // Rejilla: centro = mediana de los centros de los renderers bajo la raíz de la zona (los paneles lejanos no la desvían).
            foreach ((string clave, string nombreRaiz) in Zonas)
            {
                Transform raiz = todos.FirstOrDefault(t => t != null && t.parent == null && t.name.StartsWith(nombreRaiz, StringComparison.Ordinal))
                    ?? todos.FirstOrDefault(t => t != null && t.name.StartsWith(nombreRaiz, StringComparison.Ordinal));
                if (raiz == null)
                {
                    log.AppendLine("zona " + clave + ": no encontré la raíz " + nombreRaiz + " en la escena (sin rejilla)");
                    continue;
                }
                List<Vector3> centros = rend.Where(r => r.transform.IsChildOf(raiz)).Select(r => r.bounds.center).ToList();
                if (centros.Count == 0)
                {
                    log.AppendLine("zona " + clave + ": " + nombreRaiz + " sin renderers");
                    continue;
                }
                float mx = Mediana(centros.Select(c => c.x)), mz = Mediana(centros.Select(c => c.z));
                float suelo = Mediana(centros.Select(c => c.y).Where(y => y < 15f).DefaultIfEmpty(0f));
                var centro = new Vector3(mx, suelo, mz);
                log.AppendLine("zona " + clave + ": raíz " + RutaDe(raiz) + ", " + centros.Count + " renderers, centro " + V(centro));
                string[] rumbos = { "N", "E", "S", "O" };
                Vector3[] dirs = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
                foreach (float h in Alturas)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        l.Add(new Cam
                        {
                            Nombre = clave + "-h" + h.ToString("0") + "-" + rumbos[k],
                            Zona = clave,
                            Desde = centro - dirs[k] * h + Vector3.up * h,
                            Hacia = centro,
                            Campo = CampoRejilla,
                            // Foto: 80 m rumbo N y 150 m rumbo S por zona (el resto solo cuenta).
                            Foto = (Mathf.Approximately(h, 80f) && k == 0) || (Mathf.Approximately(h, 150f) && k == 2),
                        });
                    }
                }
            }
            return l;
        }

        private static void AgregarVista(List<Cam> l, Transform[] todos, string clave, string zona, Vector3 desde, Vector3 hacia, float campo, string raizMarca)
        {
            string origen = "constante";
            Transform marca = todos.FirstOrDefault(x => x != null && x.name == Vista19b.PrefijoMarca + clave);
            if (marca != null && marca.Find("Mira") != null)
            {
                desde = marca.position;
                hacia = marca.Find("Mira").position;
                foreach (Transform h in marca)
                {
                    if (h.name.StartsWith("Campo_", StringComparison.Ordinal) && float.TryParse(h.name.Substring(6), NumberStyles.Float, CultureInfo.InvariantCulture, out float c))
                    {
                        campo = c;
                    }
                }
                origen = "marca";
            }
            else if (raizMarca != null)
            {
                Transform r = todos.FirstOrDefault(x => x != null && x.name == raizMarca);
                Transform c = r != null ? r.Find(AereasT1414.NombreCamara) : null;
                if (c != null && c.Find(AereasT1414.NombreMira) != null)
                {
                    desde = c.position;
                    hacia = c.Find(AereasT1414.NombreMira).position;
                    origen = raizMarca + "/" + AereasT1414.NombreCamara;
                }
            }
            l.Add(new Cam { Nombre = clave + "(" + origen + ")", Zona = zona, Desde = desde, Hacia = hacia, Campo = campo, Foto = true });
        }

        // ------------------------------------------------------------------ sospechas

        private static string Sospechas(Datos d, float pct, Rejilla rejilla)
        {
            var f = new StringBuilder();
            Vector3 s = d.B.size;
            if (s.x > 60f || s.y > 60f || s.z > 60f)
            {
                f.Append('A');
            }
            else if (s.y < 3f && s.x > 30f && s.z > 30f && d.B.min.y > 1.5f)
            {
                f.Append('A');
            }
            if (d.B.min.y > 4f)
            {
                if (!d.FlotaCalculado)
                {
                    d.FlotaCalculado = true;
                    float soporte = rejilla.Soporte(d);
                    d.Hueco = d.B.min.y - soporte;
                    d.Flota = d.Hueco > 4f;
                }
                if (d.Flota)
                {
                    f.Append('B');
                }
            }
            if (pct >= 0.2f && NombreSospechoso(d.R))
            {
                f.Append('C');
            }
            if (pct >= 1.5f && ColorLiso(d.R))
            {
                f.Append('D');
            }
            return f.ToString();
        }

        private static string TextoNombres(Renderer r)
        {
            var sb = new StringBuilder();
            Transform t = r.transform;
            for (int i = 0; i < 6 && t != null; i++, t = t.parent)
            {
                sb.Append(t.name).Append('|');
            }
            MeshFilter mf = r.GetComponent<MeshFilter>();
            Mesh m = mf != null ? mf.sharedMesh : (r as SkinnedMeshRenderer)?.sharedMesh;
            if (m != null)
            {
                sb.Append(m.name).Append('|');
            }
            foreach (Material mat in r.sharedMaterials)
            {
                if (mat != null)
                {
                    sb.Append(mat.name).Append('|');
                    if (mat.shader != null)
                    {
                        sb.Append(mat.shader.name).Append('|');
                    }
                }
            }
            return sb.ToString();
        }

        private static bool NombreSospechoso(Renderer r)
        {
            string txt = TextoNombres(r).ToLowerInvariant();
            return Palabras.Any(p => txt.Contains(p));
        }

        private static bool ColorLiso(Renderer r)
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null)
                {
                    continue;
                }
                Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.gray;
                bool sinTextura = !m.HasProperty("_BaseMap") || m.GetTexture("_BaseMap") == null;
                bool casiBlanco = Mathf.Min(c.r, Mathf.Min(c.g, c.b)) > 0.85f;
                bool azulLiso = c.b > 0.35f && c.b > c.r * 1.6f && c.b > c.g * 1.25f;
                if ((casiBlanco || azulLiso) && sinTextura)
                {
                    return true;
                }
                // Con textura: blanco o azul de tinte tan fuerte que la textura casi no se nota.
                if (!sinTextura && (Mathf.Min(c.r, Mathf.Min(c.g, c.b)) > 1.2f || (azulLiso && c.b > 0.8f)))
                {
                    return true;
                }
            }
            return false;
        }

        private static float PorcentajePantalla(Camera cam, Bounds b)
        {
            Vector3 mn = b.min, mx = b.max;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            bool alguno = false;
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z);
                Vector3 v = cam.WorldToViewportPoint(p);
                if (v.z <= 0.01f)
                {
                    continue;
                }
                alguno = true;
                x0 = Mathf.Min(x0, v.x);
                x1 = Mathf.Max(x1, v.x);
                y0 = Mathf.Min(y0, v.y);
                y1 = Mathf.Max(y1, v.y);
            }
            if (!alguno)
            {
                return 0f;
            }
            float w = Mathf.Min(1f, x1) - Mathf.Max(0f, x0);
            float h = Mathf.Min(1f, y1) - Mathf.Max(0f, y0);
            return w > 0f && h > 0f ? w * h * 100f : 0f;
        }

        // Soporte debajo de un renderer: raycast hacia abajo y bounds de otros renderers bajo su centro.
        private sealed class Rejilla
        {
            private const float Celda = 25f;
            private readonly Dictionary<long, List<Datos>> _celdas = new Dictionary<long, List<Datos>>();
            private readonly List<Datos> _enormes = new List<Datos>();

            public Rejilla(Datos[] todos)
            {
                foreach (Datos d in todos)
                {
                    if (d.B.size.x > 200f || d.B.size.z > 200f)
                    {
                        _enormes.Add(d);
                        continue;
                    }
                    int x0 = Mathf.FloorToInt(d.B.min.x / Celda), x1 = Mathf.FloorToInt(d.B.max.x / Celda);
                    int z0 = Mathf.FloorToInt(d.B.min.z / Celda), z1 = Mathf.FloorToInt(d.B.max.z / Celda);
                    for (int x = x0; x <= x1; x++)
                    {
                        for (int z = z0; z <= z1; z++)
                        {
                            long k = Clave(x, z);
                            if (!_celdas.TryGetValue(k, out List<Datos> l))
                            {
                                l = new List<Datos>();
                                _celdas[k] = l;
                            }
                            l.Add(d);
                        }
                    }
                }
            }

            private static long Clave(int x, int z) => ((long)x << 32) ^ (uint)z;

            public float Soporte(Datos d)
            {
                float mejor = float.NegativeInfinity;
                Vector3 c = d.B.center;
                float y0 = d.B.min.y;
                foreach (RaycastHit h in Physics.RaycastAll(new Vector3(c.x, y0 + 0.05f, c.z), Vector3.down, 2000f))
                {
                    if (h.collider != null && !h.collider.transform.IsChildOf(d.R.transform) && !d.R.transform.IsChildOf(h.collider.transform))
                    {
                        mejor = Mathf.Max(mejor, h.point.y);
                    }
                }
                var vistos = new List<Datos>(_enormes);
                if (_celdas.TryGetValue(Clave(Mathf.FloorToInt(c.x / Celda), Mathf.FloorToInt(c.z / Celda)), out List<Datos> cel))
                {
                    vistos.AddRange(cel);
                }
                foreach (Datos o in vistos)
                {
                    if (o == d || o.R == null)
                    {
                        continue;
                    }
                    Bounds b = o.B;
                    if (c.x < b.min.x || c.x > b.max.x || c.z < b.min.z || c.z > b.max.z)
                    {
                        continue;
                    }
                    if (b.max.y <= y0 + 0.5f)
                    {
                        mejor = Mathf.Max(mejor, b.max.y);
                    }
                }
                return float.IsNegativeInfinity(mejor) ? -1000f : mejor;
            }
        }

        // ------------------------------------------------------------------ informe

        private static void EscribirLista(StringBuilder log, Dictionary<Renderer, Datos> res)
        {
            List<Datos> orden = res.Values.OrderByDescending(d => d.MaxPct).ToList();
            log.AppendLine();
            log.AppendLine("== RESUMEN: sospechosos distintos " + orden.Count + " (A=enorme/plano, B=flota, C=nombre/shader de fondo, D=blanco/azul liso)");
            foreach (char f in "ABCD")
            {
                log.AppendLine("  [" + f + "] " + orden.Count(d => d.Flags.IndexOf(f) >= 0));
            }
            log.AppendLine("== LISTA (por % de pantalla máximo, top " + TopeLista + ")");
            int n = 0;
            foreach (Datos d in orden.Take(TopeLista))
            {
                Renderer r = d.R;
                if (r == null)
                {
                    continue;
                }
                n++;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                Mesh m = mf != null ? mf.sharedMesh : (r as SkinnedMeshRenderer)?.sharedMesh;
                string mats = string.Join(" + ", r.sharedMaterials.Where(x => x != null).Select(x => x.name + " {" + (x.shader != null ? x.shader.name : "?") + "}"));
                log.AppendLine("#" + n + " [" + d.Flags + "] " + d.MaxPct.ToString("0.00", CultureInfo.InvariantCulture) + "% en " + d.CamMax + " (en " + d.Camaras + " camaras)");
                log.AppendLine("   ruta: " + RutaDe(r.transform));
                log.AppendLine("   prefab: " + PrefabDe(r.transform) + " | raiz de escena: " + r.transform.root.name);
                log.AppendLine("   malla: " + (m != null ? m.name : "(ninguna)") + " | material: " + mats);
                log.AppendLine("   centro " + V(d.B.center) + " tamaño " + V(d.B.size) + (d.Flags.Contains('B') ? " | hueco bajo la base " + d.Hueco.ToString("0.0", CultureInfo.InvariantCulture) + " m" : ""));
            }
        }

        // ------------------------------------------------------------------ fotos

        private static readonly Color[] Paleta =
        {
            new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), new Color(1f, 1f, 0f), new Color(1f, 0f, 1f), new Color(0f, 1f, 1f),
            new Color(1f, 0.5f, 0f), new Color(0.6f, 0.2f, 1f), new Color(0.5f, 1f, 0.5f), new Color(1f, 0.5f, 0.7f), new Color(0.5f, 0.5f, 0f),
        };

        private static void Fotos(List<Cam> camaras, Dictionary<string, List<Datos>> porCam, StringBuilder log)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null)
            {
                log.AppendLine("fotos: no encontré el shader URP Unlit, sin fotos");
                return;
            }
            log.AppendLine();
            log.AppendLine("== FOTOS (candidatos de más de " + MinPctCandidatoFoto + "% pintados de color; letra de la leyenda = color en la foto)");
            var viejos = new Dictionary<Renderer, Material[]>();
            var mats = new List<Material>();
            try
            {
                foreach (Cam c in camaras.Where(x => x.Foto))
                {
                    var cs = new GameObject("T1462_CamPct") { hideFlags = HideFlags.HideAndDontSave };
                    Camera cam = cs.AddComponent<Camera>();
                    cam.aspect = AspectoFoto;
                    cam.fieldOfView = c.Campo;
                    cam.transform.position = c.Desde;
                    cam.transform.LookAt(c.Hacia);
                    cam.farClipPlane = LejosDiag;
                    List<(Datos d, float pct)> grandes = (porCam.TryGetValue(c.Nombre, out List<Datos> l) ? l : new List<Datos>())
                        .Select(d => (d, pct: PorcentajePantalla(cam, d.B))).Where(x => x.pct >= MinPctCandidatoFoto).OrderByDescending(x => x.pct).Take(Paleta.Length).ToList();
                    Object.DestroyImmediate(cs);
                    string archivo = PrefijoFoto + c.Nombre.Replace("(", "-").Replace(")", "") + ".png";
                    log.AppendLine("foto " + archivo + " (" + grandes.Count + " pintados)");
                    for (int i = 0; i < grandes.Count; i++)
                    {
                        Renderer r = grandes[i].d.R;
                        if (!viejos.ContainsKey(r))
                        {
                            viejos[r] = r.sharedMaterials;
                        }
                        var m = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                        m.SetColor("_BaseColor", Paleta[i]);
                        mats.Add(m);
                        var arr = new Material[r.sharedMaterials.Length];
                        for (int k = 0; k < arr.Length; k++)
                        {
                            arr[k] = m;
                        }
                        r.sharedMaterials = arr;
                        log.AppendLine("   color " + i + " (r=" + Paleta[i].r + " g=" + Paleta[i].g + " b=" + Paleta[i].b + "): " + grandes[i].pct.ToString("0.0", CultureInfo.InvariantCulture) + "% [" + grandes[i].d.Flags + "] " + RutaDe(r.transform));
                    }
                    try
                    {
                        ColocarS2cLook.Foto(c.Desde, c.Hacia, archivo, c.Campo);
                    }
                    catch (Exception e)
                    {
                        log.AppendLine("   falló la foto: " + e.Message);
                    }
                    foreach (KeyValuePair<Renderer, Material[]> kv in viejos)
                    {
                        if (kv.Key != null)
                        {
                            kv.Key.sharedMaterials = kv.Value;
                        }
                    }
                    viejos.Clear();
                }
            }
            finally
            {
                foreach (KeyValuePair<Renderer, Material[]> kv in viejos)
                {
                    if (kv.Key != null)
                    {
                        kv.Key.sharedMaterials = kv.Value;
                    }
                }
                foreach (Material m in mats)
                {
                    Object.DestroyImmediate(m);
                }
            }
        }

        // ------------------------------------------------------------------ ayudas

        private static float Mediana(IEnumerable<float> v)
        {
            List<float> l = v.OrderBy(x => x).ToList();
            return l.Count == 0 ? 0f : l[l.Count / 2];
        }

        private static string V(Vector3 v) =>
            v.x.ToString("0.0", CultureInfo.InvariantCulture) + ";" + v.y.ToString("0.0", CultureInfo.InvariantCulture) + ";" + v.z.ToString("0.0", CultureInfo.InvariantCulture);

        private static string RutaDe(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }

        private static string PrefabDe(Transform t)
        {
            GameObject raiz = PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
            return raiz == null ? "(objeto de escena, no es prefab)" : raiz.name + " <- " + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
        }
    }
}
