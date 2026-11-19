using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1384: el Malecón idéntico a K11, K12 y A11. Edita el prefab T1354_Malecon (no rehace la zona): quita las palmeras que tapan la cámara de K11,
    /// cambia parte del frente de fachadas del kit por edificios ML05 (a y b mezclados), pone hieleras ML07, carros estacionados, el skyline SK01 al otro lado
    /// del mar sobre una orilla lejana, y un material de mar azul petróleo con más reflejo. Todo lo nuevo cuelga de "T1384_Malecon" (se borra y rehace).
    /// Lo viejo que se cambia se apaga (SetActive false), no se borra, para poder correr dos veces. Los assets que no existan se anotan y se sigue.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaMaleconT1384.Aplicar (luego .Validar, .AuditarEscena y, en otra corrida, .Fotos).
    /// </summary>
    public static class ZonaMaleconT1384
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1354_Malecon.prefab";
        public const string NombreRaiz = "T1384_Malecon";

        /// <summary>Variante principal de ML05 y ML07 y la alterna (se mezclan para que no se repita el edificio).</summary>
        public const string Variante = "_a";
        public const string VarianteAlterna = "_b";
        private const string RutaML05 = "Assets/Arte/Zonas/ML/ML05_EdificioMalecon";
        private const string RutaML07 = "Assets/Arte/Zonas/ML/ML07_Hielera";
        private const string RutaSkyline = "Assets/Arte/Fondos/SK01_Skyline.prefab";
        private static readonly string[] Carros =
        {
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V02.prefab", "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab", "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V12.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V15.prefab",
        };

        private static readonly Vector3 CamaraK11 = new Vector3(40f, 1.95f, -235.6f);
        private const float RadioSinPalmera = 6.5f;
        private const float ProbabilidadML05 = 0.5f;
        private const int MaximoML05 = 30;
        private const float AlturaSkyline = 170f;
        private const float ZSkyline = -700f;
        private const float YOrillaLejana = -2.2f;
        private const float TopeFlotante = 0.7f;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static readonly Dictionary<string, Bounds> medidas = new Dictionary<string, Bounds>();

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1384/Malecon")]
        public static void Aplicar()
        {
            Informe.Clear();
            medidas.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                int luzAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                Transform vieja = raiz.Find(NombreRaiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                ReactivarCasas(raiz);
                Transform mio = new GameObject(NombreRaiz).transform;
                mio.SetParent(raiz, false);

                QuitarPalmerasDeLaCamara(raiz);
                List<Bounds> ocupado = Ocupado(raiz);
                PonerML05(raiz, mio);
                PonerHieleras(raiz, mio, ocupado);
                PonerCarros(mio, ocupado);
                PonerSkyline(raiz, mio);
                MarConReflejos(raiz);
                BajarFlotantes(raiz);
                MarcarEstaticos(mio);

                Informe.AppendLine("Luces antes " + luzAntes + ", después " + raiz.GetComponentsInChildren<Light>(true).Length + " (no se agregó ninguna).");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1384-aplicar");
        }

        private static void ReactivarCasas(Transform raiz)
        {
            Transform fachadas = raiz.Find("Fachadas");
            if (fachadas == null)
            {
                return;
            }
            foreach (Transform c in fachadas)
            {
                if (!c.gameObject.activeSelf && c.name.StartsWith("Casa_", StringComparison.Ordinal))
                {
                    c.gameObject.SetActive(true);
                }
            }
        }

        // ================================================================ Palmeras que tapan K11

        private static void QuitarPalmerasDeLaCamara(Transform raiz)
        {
            Transform paseo = raiz.Find("Paseo");
            if (paseo == null)
            {
                Informe.AppendLine("PENDIENTE: no hay Paseo");
                return;
            }
            List<GameObject> quitar = new List<GameObject>();
            foreach (Transform t in paseo)
            {
                bool palmera = t.name.StartsWith("AR03_Palmera", StringComparison.Ordinal);
                bool farol = t.name.StartsWith("ML01_Farol", StringComparison.Ordinal);
                if (!palmera && !farol)
                {
                    continue;
                }
                Bounds b = CajaDe(t.gameObject);
                float d = Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(CamaraK11.x, CamaraK11.z));
                if ((palmera && d < RadioSinPalmera) || (farol && d < 1.5f))
                {
                    quitar.Add(t.gameObject);
                }
            }
            foreach (GameObject g in quitar)
            {
                Informe.AppendLine("Quitada de la cámara K11: " + g.name + " en " + g.transform.position);
                Object.DestroyImmediate(g);
            }
            Informe.AppendLine("Quitadas cerca de la cámara K11: " + quitar.Count);
        }

        // ================================================================ Edificios ML05

        private static void PonerML05(Transform raiz, Transform mio)
        {
            GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + Variante + ".prefab");
            GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + VarianteAlterna + ".prefab");
            if (a == null && b == null)
            {
                Informe.AppendLine("FALTA: ML05_EdificioMalecon_a/_b (T-1382). Se dejan las fachadas del kit.");
                return;
            }
            if (a == null) { a = b; }
            if (b == null) { b = a; }
            Informe.AppendLine("ML05: variante principal " + Variante + " y alterna " + VarianteAlterna + " mezcladas para variar la silueta (revisar imageassets/modelos-nuevos).");
            Transform fachadas = raiz.Find("Fachadas");
            if (fachadas == null)
            {
                return;
            }
            Transform grupo = new GameObject("ML05").transform;
            grupo.SetParent(mio, false);
            System.Random azar = new System.Random(1384);
            int puestos = 0;
            foreach (Transform casa in fachadas.Cast<Transform>().ToList())
            {
                if (puestos >= MaximoML05 || !casa.gameObject.activeSelf || !casa.name.StartsWith("Casa_", StringComparison.Ordinal))
                {
                    continue;
                }
                Bounds cb = CajaDe(casa.gameObject);
                float z = ColocarT1354Curva.ZCentro(cb.center.x);
                float haciaCiudad = cb.center.z - z;
                // Solo el frente de la avenida (centro a ~14.5 m de la línea); la hilera de fondo queda a ~25 m.
                if (haciaCiudad < 12.5f || haciaCiudad > 18.5f || azar.NextDouble() > ProbabilidadML05)
                {
                    continue;
                }
                GameObject prefab = puestos % 2 == 0 ? a : b;
                Bounds mb = Medida(prefab);
                if (mb.size.x < 1f)
                {
                    continue;
                }
                float escala = Mathf.Clamp(Mathf.Min(cb.size.x, 6.4f) / mb.size.x, 0.7f, 1.6f);
                float dz = (ColocarT1354Curva.ZCentro(cb.center.x + 1f) - ColocarT1354Curva.ZCentro(cb.center.x - 1f)) * 0.5f;
                Vector3 tangente = new Vector3(1f, 0f, dz).normalized;
                Vector3 mar = ColocarT1354Curva.HaciaElMar(tangente);
                Vector3 linea = new Vector3(cb.center.x, 0f, z);
                // Frente a 11 m de la línea (LateralCiudad = -11) y el cuerpo hacia la ciudad.
                Vector3 centro = linea + mar * (ColocarT1354Curva.LateralCiudad - mb.size.z * escala * 0.5f);
                Quaternion giro = Quaternion.LookRotation(-mar);
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
                go.name = prefab.name + "_" + puestos;
                go.transform.rotation = giro;
                go.transform.localScale = Vector3.one * escala;
                Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
                Vector3 destino = new Vector3(centro.x, ColocarT1354Curva.YBanqueta, centro.z);
                go.transform.position = destino - giro * ancla;
                Renderer[] rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0 && go.GetComponent<LODGroup>() == null)
                {
                    LODGroup lod = go.AddComponent<LODGroup>();
                    lod.SetLODs(new[] { new LOD(0.012f, rs) });
                    lod.RecalculateBounds();
                }
                casa.gameObject.SetActive(false);
                Informe.AppendLine("ML05 " + go.name + " reemplaza " + casa.name + " (escala " + escala.ToString("F2") + ", ancho de la casa " + cb.size.x.ToString("F1") + ")");
                puestos++;
            }
            Informe.AppendLine("ML05 colocados: " + puestos);
        }

        // ================================================================ Hieleras y carros

        private static List<Bounds> Ocupado(Transform raiz)
        {
            List<Bounds> lista = new List<Bounds>();
            foreach (string grupo in new[] { "Paseo", "Postes", "Muelle" })
            {
                Transform g = raiz.Find(grupo);
                if (g == null)
                {
                    continue;
                }
                foreach (Transform t in g)
                {
                    if (t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null)
                    {
                        lista.Add(CajaDe(t.gameObject));
                    }
                }
            }
            return lista;
        }

        private static bool Libre(Bounds b, List<Bounds> ocupado)
        {
            Bounds g = b;
            g.Expand(0.4f);
            return !ocupado.Any(o => o.Intersects(g));
        }

        private static void PonerHieleras(Transform raiz, Transform mio, List<Bounds> ocupado)
        {
            GameObject h = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML07 + Variante + ".prefab");
            GameObject h2 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML07 + VarianteAlterna + ".prefab");
            if (h == null && h2 == null)
            {
                Informe.AppendLine("FALTA: ML07_Hielera_a/_b (T-1382). Se quedan los Hueco_Hielera.");
                return;
            }
            if (h == null) { h = h2; }
            if (h2 == null) { h2 = h; }
            Transform grupo = new GameObject("ML07").transform;
            grupo.SetParent(mio, false);
            int n = 0;
            // 1) En cada Hueco_Hielera del puesto de mariscos.
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false).Where(x => x.name == "Hueco_Hielera").ToList())
            {
                Vector3 p = t.position;
                p.y = ColocarT1354Curva.YBanqueta;
                if (Colocar(n % 2 == 0 ? h : h2, grupo, p, Quaternion.Euler(0f, 90f * (n % 4), 0f), 1f, ocupado, "Hielera_" + n) != null)
                {
                    n++;
                }
            }
            // 2) Hieleras sueltas junto al barandal cada ~55 m (lateral 13.4), fuera del puesto, las bocas de las uniones y el muelle.
            List<Vector3> borde = ColocarT1354Curva.BordeMar(-330f, 175f);
            float largo = ColocarT1354Curva.Longitud(borde);
            foreach (float d in ColocarT1354Curva.Cada(largo, 55f, 31f))
            {
                Vector3 p = ColocarT1354Curva.PuntoEn(borde, d, out Vector3 t);
                Vector3 mar = ColocarT1354Curva.HaciaElMar(t);
                p += mar * 13.4f;
                p.y = ColocarT1354Curva.YBanqueta;
                if (Mathf.Abs(Mathf.Abs(p.x) - 47f) < 10f || p.x > 100f || Mathf.Abs(p.x - 78f) < 12f)
                {
                    continue;
                }
                if (Colocar(n % 2 == 0 ? h : h2, grupo, p, Quaternion.LookRotation(-mar), 1f, ocupado, "HielerasSuelta_" + n) != null)
                {
                    n++;
                }
            }
            Informe.AppendLine("ML07 hieleras: " + n);
        }

        private static void PonerCarros(Transform mio, List<Bounds> ocupado)
        {
            Transform grupo = new GameObject("Carros").transform;
            grupo.SetParent(mio, false);
            List<Vector3> avenida = ColocarT1354Curva.Avenida();
            float largo = ColocarT1354Curva.Longitud(avenida);
            int n = 0;
            foreach (float d in ColocarT1354Curva.Cada(largo, 62f, 24f))
            {
                if (n >= Carros.Length * 2)
                {
                    break;
                }
                Vector3 p = ColocarT1354Curva.PuntoEn(avenida, d, out Vector3 t);
                if (Mathf.Abs(Mathf.Abs(p.x) - 47f) < 16f || p.x > 100f)
                {
                    continue;
                }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Carros[n % Carros.Length]);
                if (prefab == null)
                {
                    Informe.AppendLine("FALTA carro " + Carros[n % Carros.Length]);
                    continue;
                }
                Vector3 mar = ColocarT1354Curva.HaciaElMar(t);
                // Pegado a las fachadas, sobre la banqueta de 3 m del lado de la ciudad (lateral -9.5), paralelo a la calle; fuera del carril (-8).
                Vector3 sitio = p - mar * 9.5f;
                sitio.y = ColocarT1354Curva.YBanqueta;
                GameObject car = Colocar(prefab, grupo, sitio, Quaternion.LookRotation(t.normalized), 1f, ocupado, "Carro_" + n);
                if (car != null)
                {
                    foreach (MonoBehaviour m in car.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        m.enabled = false;
                    }
                    foreach (Rigidbody rb in car.GetComponentsInChildren<Rigidbody>(true))
                    {
                        rb.isKinematic = true;
                    }
                    n++;
                }
            }
            Informe.AppendLine("Carros estacionados (inertes, scripts apagados): " + n);
        }

        /// <summary>Instancia un prefab con su base en <paramref name="suelo"/>, centrado en su caja; si choca con algo ocupado no lo pone (devuelve null).</summary>
        private static GameObject Colocar(GameObject prefab, Transform padre, Vector3 suelo, Quaternion giro, float escala, List<Bounds> ocupado, string nombre)
        {
            Bounds mb = Medida(prefab);
            Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
            Vector3 pos = suelo - giro * ancla;
            Vector3 medio = suelo + Vector3.up * (mb.size.y * escala * 0.5f);
            Vector3 tam = giro * (mb.size * escala);
            Bounds ocupa = new Bounds(medio, new Vector3(Mathf.Abs(tam.x), Mathf.Abs(tam.y), Mathf.Abs(tam.z)));
            if (!Libre(ocupa, ocupado))
            {
                Informe.AppendLine("Omitido por choque: " + nombre + " en " + suelo);
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.rotation = giro;
            go.transform.localScale = Vector3.one * escala;
            go.transform.position = pos;
            ocupado.Add(ocupa);
            return go;
        }

        // ================================================================ Skyline y otra orilla

        private static void PonerSkyline(Transform raiz, Transform mio)
        {
            GameObject sk = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkyline);
            if (sk == null)
            {
                Informe.AppendLine("FALTA: SK01_Skyline (T-1382). Sin skyline ni otra orilla.");
                return;
            }
            Transform grupo = new GameObject("Skyline").transform;
            grupo.SetParent(mio, false);
            // Otra orilla: una losa baja y oscura (copia del FondoMar, misma malla) 0.8 m sobre el agua, de x -560 a 200 y z -750 a -690.
            Transform fondo = raiz.Find("Piso/FondoMar");
            if (fondo != null)
            {
                GameObject orilla = Object.Instantiate(fondo.gameObject, grupo);
                orilla.name = "OtraOrilla";
                foreach (Collider c in orilla.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(c);
                }
                orilla.transform.position = new Vector3(-180f, YOrillaLejana - 0.1f, ZSkyline - 20f);
                orilla.transform.localScale = new Vector3(760f / 1400f, 1f, 60f / 638f);
                MeshRenderer mr = orilla.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = KitS2c.Mat("T1384_OtraOrilla", () => KitS2c.Lit(new Color(0.03f, 0.035f, 0.05f), 0.1f));
                }
            }
            else
            {
                Informe.AppendLine("PENDIENTE: no hay Piso/FondoMar para la otra orilla; el skyline queda sin orilla.");
            }
            Bounds sb = Medida(sk);
            if (sb.size.y < 1f)
            {
                Informe.AppendLine("PENDIENTE: SK01 sin renderers medibles.");
                return;
            }
            float escala = AlturaSkyline / sb.size.y;
            float ancho = sb.size.x * escala;
            int cuantos = Mathf.Clamp(Mathf.CeilToInt(760f / Mathf.Max(ancho * 0.92f, 1f)), 1, 12);
            float x0 = -560f + ancho * 0.5f;
            for (int i = 0; i < cuantos; i++)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(sk, grupo);
                go.name = "SK01_" + i;
                Quaternion giro = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f);
                go.transform.rotation = giro;
                go.transform.localScale = Vector3.one * escala;
                Vector3 ancla = new Vector3(sb.center.x, sb.min.y, sb.center.z) * escala;
                Vector3 destino = new Vector3(x0 + i * ancho * 0.92f, YOrillaLejana, ZSkyline);
                go.transform.position = destino - giro * ancla;
            }
            Informe.AppendLine("Skyline: " + cuantos + " copias de SK01 a " + escala.ToString("F2") + "x (alto " + AlturaSkyline + " m, ancho de cada una " + ancho.ToString("F0") + " m) en z=" + ZSkyline + ", sobre OtraOrilla (y " + YOrillaLejana + ").");
        }

        // ================================================================ Mar

        private static void MarConReflejos(Transform raiz)
        {
            string ruta = KitS2c.Carpeta + "/T1384_MarMalecon.mat";
            Material origen = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Mar.mat");
            if (origen == null)
            {
                Informe.AppendLine("FALTA: Mar.mat de T-1351; el mar queda como estaba.");
                return;
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(origen) { name = "T1384_MarMalecon" };
                AssetDatabase.CreateAsset(m, ruta);
            }
            // Azul petróleo oscuro, más espejo (fresnel y reflejo de la sonda) y estelas de luz más anchas y fuertes para las luces de la otra orilla.
            SetColor(m, "_ColorProfundo", new Color(0.008f, 0.045f, 0.06f, 1f));
            SetColor(m, "_ColorSuperficie", new Color(0.03f, 0.14f, 0.17f, 1f));
            SetColor(m, "_ColorCielo", new Color(0.05f, 0.09f, 0.18f, 1f));
            SetFloat(m, "_Reflejo", 1.5f);
            SetFloat(m, "_FresnelMin", 0.14f);
            SetFloat(m, "_FresnelPotencia", 3f);
            SetFloat(m, "_Rugosidad", 0.1f);
            SetFloat(m, "_GananciaLuces", 9f);
            SetFloat(m, "_AnchoEstela", 0.06f);
            SetFloat(m, "_AlturaOla", 0.1f);
            EditorUtility.SetDirty(m);
            int n = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Mar" && t.parent != null && t.parent.name == "Mar")
                {
                    MeshRenderer mr = t.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = m;
                        n++;
                    }
                }
            }
            Informe.AppendLine("Mar: material T1384_MarMalecon (copia de Mar.mat; el Puerto sigue con el suyo) puesto en " + n + " plano(s).");
        }

        private static void SetColor(Material m, string p, Color c)
        {
            if (m.HasProperty(p))
            {
                m.SetColor(p, c);
            }
        }

        private static void SetFloat(Material m, string p, float v)
        {
            if (m.HasProperty(p))
            {
                m.SetFloat(p, v);
            }
        }

        // ================================================================ Flotantes y estáticos

        private static bool Excluido(Transform t)
        {
            for (Transform x = t; x != null; x = x.parent)
            {
                string n = x.name;
                if (n.StartsWith("Luz", StringComparison.Ordinal) || n.StartsWith("Halo", StringComparison.Ordinal) || n.StartsWith("Haz", StringComparison.Ordinal)
                    || n == "Cables" || n.StartsWith("Cable", StringComparison.Ordinal) || n.StartsWith("Neon", StringComparison.Ordinal) || n == "Letrero"
                    || n == "Skyline" || n == "Postes" || n == "Fachadas" || n == "Mar" || n == "ML05" || n.StartsWith("Dron", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Lo que cuelga de Paseo con el punto más bajo a más de 0.5 m sobre la banqueta se baja hasta la banqueta (las escaleras se saltan).</summary>
        private static void BajarFlotantes(Transform raiz)
        {
            Transform paseo = raiz.Find("Paseo");
            if (paseo == null)
            {
                return;
            }
            foreach (Transform t in paseo)
            {
                if (!t.gameObject.activeInHierarchy || t.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal) || t.name.StartsWith("Hueco_", StringComparison.Ordinal))
                {
                    continue;
                }
                Bounds b = CajaDe(t.gameObject);
                if (b.size == Vector3.zero)
                {
                    continue;
                }
                if (b.min.y > ColocarT1354Curva.YBanqueta + 0.5f)
                {
                    float baja = b.min.y - ColocarT1354Curva.YBanqueta;
                    t.position += Vector3.down * baja;
                    Informe.AppendLine("Flotante bajado " + baja.ToString("F2") + " m: " + t.name);
                }
            }
        }

        private static void MarcarEstaticos(Transform mio)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in mio.GetComponentsInChildren<Transform>(true))
            {
                if (t == mio || t.name.StartsWith("Carro_", StringComparison.Ordinal) || t.GetComponentInParent<Rigidbody>() != null || t.GetComponent<Light>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T-1384/Validar")]
        public static void Validar()
        {
            Informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mio = raiz.Find(NombreRaiz);
                Informe.AppendLine("Luces en la zona: " + raiz.GetComponentsInChildren<Light>(true).Length + " (T-1384 no agrega ninguna)");
                Informe.AppendLine("Objetos en T1384_Malecon: " + (mio != null ? mio.GetComponentsInChildren<Transform>(true).Length : 0) + " (tope 3000)");
                // 1) Flotantes: renderer con el punto más bajo a más de 0.5 m sobre el piso y que no es luz, cable, letrero, dron ni skyline.
                int flotan = 0;
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (Excluido(r.transform))
                    {
                        continue;
                    }
                    Transform p = r.transform;
                    while (p.parent != null && p.parent != raiz)
                    {
                        p = p.parent;
                    }
                    // Costa y Muelle se miden contra su propio suelo (arena y agua), abajo.
                    if (p.name == "Costa" || p.name == "Muelle" || p.name == "Piso" || p.name == "Calles")
                    {
                        continue;
                    }
                    if (r.bounds.min.y > TopeFlotante)
                    {
                        flotan++;
                        Informe.AppendLine("FLOTA: " + Ruta(r.transform, raiz) + " min.y=" + r.bounds.min.y.ToString("F2"));
                    }
                }
                foreach (string g in new[] { "Costa", "Muelle" })
                {
                    Transform gt = raiz.Find(g);
                    if (gt == null)
                    {
                        continue;
                    }
                    foreach (Renderer r in gt.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!Excluido(r.transform) && r.bounds.min.y > TopeFlotante)
                        {
                            flotan++;
                            Informe.AppendLine("FLOTA (" + g + "): " + Ruta(r.transform, raiz) + " min.y=" + r.bounds.min.y.ToString("F2"));
                        }
                    }
                }
                // Casas del kit y ML05: la base sobre la banqueta (0.2 ± 0.5 m).
                foreach (Transform c in raiz.GetComponentsInChildren<Transform>(false).Where(x => x.parent != null && (x.parent.name == "Fachadas" || x.parent.name == "ML05")))
                {
                    Bounds b = CajaDe(c.gameObject);
                    if (b.size == Vector3.zero)
                    {
                        continue;
                    }
                    if (b.min.y > 0.7f || b.min.y < -0.4f)
                    {
                        flotan++;
                        Informe.AppendLine("FLOTA/ENTERRADA: " + Ruta(c, raiz) + " min.y=" + b.min.y.ToString("F2"));
                    }
                }
                Informe.AppendLine("Flotantes o enterrados: " + flotan);

                // 2) Cruces entre lo nuevo (carros, hieleras) y lo existente (paseo, postes, muelle); cajas alineadas a los ejes: es una señal.
                int cruces = 0;
                int carril = 0;
                if (mio != null)
                {
                    List<(string nombre, Bounds caja)> otros = new List<(string, Bounds)>();
                    foreach (string g in new[] { "Paseo", "Postes", "Muelle" })
                    {
                        Transform gt = raiz.Find(g);
                        if (gt == null)
                        {
                            continue;
                        }
                        foreach (Transform t in gt)
                        {
                            if (t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null)
                            {
                                otros.Add((g + "/" + t.name, CajaDe(t.gameObject)));
                            }
                        }
                    }
                    foreach (Transform t in mio.GetComponentsInChildren<Transform>(false).Where(x => x.parent != null && (x.parent.name == "Carros" || x.parent.name == "ML07")))
                    {
                        Bounds b = CajaDe(t.gameObject);
                        foreach ((string nombre, Bounds caja) in otros)
                        {
                            if (b.Intersects(caja))
                            {
                                cruces++;
                                Informe.AppendLine("CRUCE: " + t.name + " con " + nombre);
                            }
                        }
                        // 3) Carros en el carril: el arroyo va de -8 a 8 de la línea de la avenida.
                        if (t.parent.name == "Carros")
                        {
                            float zc = ColocarT1354Curva.ZCentro(b.center.x);
                            if (b.max.z < zc + 8f && b.min.z > zc - 8f)
                            {
                                carril++;
                                Informe.AppendLine("EN EL CARRIL: " + t.name);
                            }
                        }
                    }
                }
                Informe.AppendLine("Cruces: " + cruces);
                Informe.AppendLine("En el carril: " + carril);
                Informe.AppendLine("Tamaño del prefab (MB): " + (new FileInfo(RutaPrefab).Length / 1048576f).ToString("F1") + " (tope 60)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1384-validar");
        }

        /// <summary>
        /// Renderers de toda la escena entre el Malecón y el horizonte (x -340..180, z -900..-205) con el punto más bajo a más de 0.5 m: el rectángulo
        /// oscuro que flota sobre el mar en la captura se busca aquí (puede ser del fondo T513 o de la otra orilla de la escena, no del prefab).
        /// Escribe Logs/t1384-escena.txt. No guarda la escena.
        /// </summary>
        public static void AuditarEscena()
        {
            Informe.Clear();
            ColocarS2.Abrir();
            Bounds zona = new Bounds(new Vector3(-80f, 100f, -552f), new Vector3(520f, 600f, 695f));
            int n = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || !r.bounds.Intersects(zona) || r.bounds.min.y < 0.5f)
                {
                    continue;
                }
                string ruta = Ruta(r.transform, null);
                if (ruta.Contains("/Fachadas/") || ruta.Contains("/Cables") || ruta.Contains("/Postes/") || ruta.Contains("Neon") || ruta.Contains("Skyline"))
                {
                    continue;
                }
                n++;
                Informe.AppendLine("ALTO: " + ruta + " min.y=" + r.bounds.min.y.ToString("F1") + " max.y=" + r.bounds.max.y.ToString("F1") + " centro=" + r.bounds.center + " tam=" + r.bounds.size);
            }
            Informe.AppendLine("Total: " + n);
            Escribir("t1384-escena");
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T-1384/Fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                // K11: igual que en la T-1354 (a 1.95 m, en la banqueta del mar, mirando al oeste con ~8 grados hacia la ciudad); sin palmera a 6.5 m a la redonda.
                ("k11", "K11_Malecon.png", CamaraK11, new Vector3(-59f, 1.95f, -221.7f), 60f),
                ("k12", "K12_Malecon_Muelle.png", new Vector3(70f, 2.2f, -236f), new Vector3(149f, 2.2f, -250f), 75f),
                ("a11", "A11_Malecon_Aerea.png", new Vector3(-20f, 40f, -200f), new Vector3(-95f, 0f, -250f), 70f),
            };
            Directory.CreateDirectory("docs/capturas/t-1384");
            foreach (var t in tomas)
            {
                string juego = "Logs/t1384-" + t.nombre + ".png";
                ColocarS2cLook.Foto(t.desde, t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/t-1384/malecon-" + t.nombre + ".png");
            }
        }

        // ================================================================ Ayudas

        private static string Ruta(Transform t, Transform hasta)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null && p != hasta; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }

        /// <summary>Caja de los renderers activos de un objeto; sin renderers, un punto en su posición.</summary>
        private static Bounds CajaDe(GameObject go)
        {
            Bounds b = new Bounds();
            bool hay = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false))
            {
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
            return hay ? b : new Bounds(go.transform.position, Vector3.zero);
        }

        /// <summary>Caja de un prefab con su pivote en el origen sin girar ni escalar (se mide una instancia temporal).</summary>
        private static Bounds Medida(GameObject prefab)
        {
            if (prefab == null)
            {
                return new Bounds();
            }
            if (medidas.TryGetValue(prefab.name, out Bounds g))
            {
                return g;
            }
            GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            tmp.transform.localScale = Vector3.one;
            Renderer[] rs = tmp.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length == 0 ? new Bounds() : rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            Object.DestroyImmediate(tmp);
            medidas[prefab.name] = b;
            return b;
        }

        private static void Escribir(string nombre)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/" + nombre + ".txt", Informe.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] " + nombre + ":\n" + Informe);
        }
    }
}
