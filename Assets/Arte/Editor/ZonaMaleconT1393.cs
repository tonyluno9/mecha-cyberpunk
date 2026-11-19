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
    /// T-1393: Malecón, ronda 2. Corre DESPUÉS de ZonaMaleconT1384.Aplicar (edita el mismo prefab T1354_Malecon; todo lo nuevo cuelga de "T1393_Malecon").
    /// 1) Skyline: quita la "OtraOrilla" vieja (torres a 400-480 m, el rectángulo oscuro) y la losa/skyline plano de T-1384; pone el arco SK01 (malla
    ///    curva de 900 m de radio) a 0.55x (radio 495 m) centrado en la cámara K11, dos copias espejadas que cubren de oeste a este por el sur.
    /// 2) ML05 rehechos para que quepan en el ancho de su casa (sin encimarse ni dejar huecos).
    /// 3) Fachadas: letreros de neón en bandera (magenta, turquesa) y de pared (ámbar) con emisión, aires acondicionados, toldos y cables de los postes.
    /// 4) Paseo: palmeras y faroles de sodio extra entre los que ya había (sin tapar la cámara K11 ni la K12).
    /// Métodos: Aplicar, Validar y, en corrida aparte, Fotos. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaMaleconT1393.Aplicar
    /// </summary>
    public static class ZonaMaleconT1393
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1354_Malecon.prefab";
        public const string Raiz1384 = "T1384_Malecon";
        public const string NombreRaiz = "T1393_Malecon";

        public const string Variante = "_a";
        public const string VarianteAlterna = "_b";
        private const string RutaML05 = "Assets/Arte/Zonas/ML/ML05_EdificioMalecon";
        private const string RutaSkyline = "Assets/Arte/Fondos/SK01_Skyline.prefab";
        private const string RutaPalmera = "Assets/Arte/Zonas/AR/AR03_Palmera.prefab";
        private const string RutaFarol = "Assets/Arte/Zonas/ML/ML01_Farol.prefab";

        private static readonly Vector3 CamaraK11 = new Vector3(40f, 1.95f, -235.6f);
        private const float RadioSinPalmera = 6.5f;

        // Skyline: el arco SK01 mide 900 m de radio y 180 m de alto, 120 grados, con el centro en el origen del prefab.
        private const float EscalaRadio = 0.55f;
        private const float EscalaAlto = 0.9f;
        private const float YBaseSkyline = -2.5f;
        private const float AlfaOesteA = 245f;
        private const float AlfaCentroB = 365f;
        private const float RadioMinimoSkyline = 350f;
        private const float RadioMaximoSkyline = 550f;

        private const float EscalaMinimaML05 = 0.45f;
        private const float EscalaMaximaML05 = 1.3f;
        private const float EncimeMaximo = 0.25f;

        private const int TopeLucesNeon = 22;
        private const int TopeLucesFarol = 16;
        private const int TopeLucesNuevas = 40;
        private const int TopeCables = 140;
        private const float LateralPaseo = 12f;
        private const float TopeFlotante = 0.7f;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static readonly Dictionary<string, Bounds> medidas = new Dictionary<string, Bounds>();

        private static Vector3 CamaraK12 => new Vector3(104f, 2.3f, ColocarT1354Curva.ZCentro(104f) - 10f);
        private static Vector3 MiraK12 => new Vector3(20f, 2.2f, ColocarT1354Curva.ZCentro(20f) - 3f);

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1393/Malecon ronda 2")]
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
                Transform mio = new GameObject(NombreRaiz).transform;
                mio.SetParent(raiz, false);
                Transform t1384 = raiz.Find(Raiz1384);
                if (t1384 == null)
                {
                    Informe.AppendLine("AVISO: no hay " + Raiz1384 + "; corre antes ZonaMaleconT1384.Aplicar.");
                }

                KitS2c.Obra obra = KitS2c.Empezar("T1393_Extras", 1393, mio);
                QuitarSkylineViejo(raiz, t1384);
                PonerSkyline(mio);
                List<(Transform t, Bounds caja)> frentes = RehacerML05(raiz, t1384, mio);
                List<Vector3> puntosNeon = PonerFachadas(obra, raiz, frentes);
                PonerPaseo(obra, raiz, mio);
                PonerCables(obra, raiz, puntosNeon);
                KitS2c.Terminar(obra);
                MarcarEstaticos(mio);

                int luzDespues = raiz.GetComponentsInChildren<Light>(true).Length;
                int nuevas = mio.GetComponentsInChildren<Light>(true).Length;
                Informe.AppendLine("Luces antes " + luzAntes + ", después " + luzDespues + "; nuevas de T-1393: " + nuevas + " (tope " + TopeLucesNuevas + ").");
                if (nuevas > TopeLucesNuevas)
                {
                    Informe.AppendLine("ERROR: más luces nuevas del tope.");
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1393-aplicar");
        }

        // ================================================================ Skyline

        private static void QuitarSkylineViejo(Transform raiz, Transform t1384)
        {
            if (t1384 != null)
            {
                Transform sk = t1384.Find("Skyline");
                if (sk != null)
                {
                    Informe.AppendLine("Quitado " + Raiz1384 + "/Skyline (losa oscura OtraOrilla y SK01 plano a z -700: era el rectángulo del K11 y quedaba fuera de cuadro).");
                    Object.DestroyImmediate(sk.gameObject);
                }
            }
            Transform orilla = raiz.Find("OtraOrilla");
            if (orilla != null && orilla.gameObject.activeSelf)
            {
                orilla.gameObject.SetActive(false);
                Informe.AppendLine("Apagada la OtraOrilla vieja de T-1354 (22 torres a z -590..-660, 355-425 m, la mayoría fuera del cuadro de K11 y sin leerse).");
            }
        }

        private static void PonerSkyline(Transform mio)
        {
            GameObject sk = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkyline);
            if (sk == null)
            {
                Informe.AppendLine("FALTA: SK01_Skyline. Sin skyline.");
                return;
            }
            Renderer rBase = sk.GetComponentInChildren<Renderer>(true);
            Material origen = rBase != null ? rBase.sharedMaterial : null;
            Material mat = origen;
            if (origen != null)
            {
                mat = KitS2c.Mat("T1393_Skyline", () =>
                {
                    Material m = new Material(origen);
                    // Más claro de noche que el SK01 pelón (0.2): las torres con pantallas deben leerse como en K11.
                    if (m.HasProperty("_BrilloNoche")) { m.SetFloat("_BrilloNoche", 1.1f); }
                    if (m.HasProperty("_BrumaNoche")) { m.SetFloat("_BrumaNoche", 0.12f); }
                    return m;
                });
            }
            Transform grupo = new GameObject("Skyline").transform;
            grupo.SetParent(mio, false);
            Vector3 pivote = new Vector3(CamaraK11.x, YBaseSkyline, CamaraK11.z);
            float radio = 900f * EscalaRadio;
            // El centro del arco mira a +Z local: yaw = 90 - alfa (alfa medido desde +X hacia +Z). A: sur-oeste; B (espejo): sur-este.
            (string nombre, float alfa, bool espejo)[] copias = { ("SK01_Oeste", AlfaOesteA, false), ("SK01_Este", AlfaCentroB, true) };
            foreach (var c in copias)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(sk, grupo);
                go.name = c.nombre;
                go.transform.SetPositionAndRotation(pivote, Quaternion.Euler(0f, 90f - c.alfa, 0f));
                go.transform.localScale = new Vector3(c.espejo ? -EscalaRadio : EscalaRadio, EscalaAlto, EscalaRadio);
                if (mat != null)
                {
                    foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] ms = new Material[r.sharedMaterials.Length];
                        for (int i = 0; i < ms.Length; i++) { ms[i] = mat; }
                        r.sharedMaterials = ms;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        r.receiveShadows = false;
                    }
                }
            }
            Informe.AppendLine("Skyline: 2 arcos SK01 (oeste y espejo este) con radio " + radio.ToString("F0") + " m, alto " + (180f * EscalaAlto).ToString("F0")
                + " m, centrados en " + pivote + " (K11); cubren alfa 185 a 425 grados por el sur. Material T1393_Skyline (brillo de noche 1.1).");
        }

        // ================================================================ ML05

        private static List<(Transform t, Bounds caja)> RehacerML05(Transform raiz, Transform t1384, Transform mio)
        {
            List<(Transform, Bounds)> frentes = new List<(Transform, Bounds)>();
            Transform fachadas = raiz.Find("Fachadas");
            if (t1384 != null)
            {
                Transform ml = t1384.Find("ML05");
                if (ml != null)
                {
                    Object.DestroyImmediate(ml.gameObject);
                    Informe.AppendLine("Quitado " + Raiz1384 + "/ML05 (todos a escala 0.70: más anchos que su casa); se rehacen ajustados al ancho.");
                }
            }
            if (fachadas == null)
            {
                return frentes;
            }
            foreach (Transform c in fachadas)
            {
                if (!c.gameObject.activeSelf && c.name.StartsWith("Casa_", StringComparison.Ordinal))
                {
                    c.gameObject.SetActive(true);
                }
            }
            GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + Variante + ".prefab");
            GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + VarianteAlterna + ".prefab");
            if (a == null && b == null)
            {
                Informe.AppendLine("FALTA: ML05_EdificioMalecon_a/_b. Se quedan las fachadas del kit.");
                return frentes;
            }
            if (a == null) { a = b; }
            if (b == null) { b = a; }
            Transform grupo = new GameObject("ML05").transform;
            grupo.SetParent(mio, false);
            List<Transform> casas = fachadas.Cast<Transform>().Where(x => x.name.StartsWith("Casa_", StringComparison.Ordinal) && x.gameObject.activeSelf).ToList();
            List<Bounds> cajasCasas = casas.Select(x => CajaDe(x.gameObject)).ToList();
            List<Bounds> puestos = new List<Bounds>();
            System.Random azar = new System.Random(1393);
            int n = 0;
            int omitidos = 0;
            for (int i = 0; i < casas.Count; i++)
            {
                Transform casa = casas[i];
                Bounds cb = cajasCasas[i];
                float z = ColocarT1354Curva.ZCentro(cb.center.x);
                float haciaCiudad = cb.center.z - z;
                if (haciaCiudad < 12.5f || haciaCiudad > 18.5f)
                {
                    continue;
                }
                frentes.Add((casa, cb));
                if (azar.NextDouble() > 0.5)
                {
                    continue;
                }
                GameObject prefab = n % 2 == 0 ? a : b;
                Bounds mb = Medida(prefab);
                if (mb.size.x < 1f)
                {
                    continue;
                }
                float escala = Mathf.Min(cb.size.x * 0.97f, 8f) / mb.size.x;
                if (escala < EscalaMinimaML05)
                {
                    omitidos++;
                    Informe.AppendLine("ML05 omitido en " + casa.name + ": para caber en " + cb.size.x.ToString("F1") + " m habría que escalar a " + escala.ToString("F2") + " (< " + EscalaMinimaML05 + "); se queda la casa del kit.");
                    continue;
                }
                escala = Mathf.Min(escala, EscalaMaximaML05);
                float dz = (ColocarT1354Curva.ZCentro(cb.center.x + 1f) - ColocarT1354Curva.ZCentro(cb.center.x - 1f)) * 0.5f;
                Vector3 tangente = new Vector3(1f, 0f, dz).normalized;
                Vector3 mar = ColocarT1354Curva.HaciaElMar(tangente);
                Vector3 linea = new Vector3(cb.center.x, 0f, z);
                Vector3 centro = linea + mar * (ColocarT1354Curva.LateralCiudad - mb.size.z * escala * 0.5f);
                Quaternion giro = Quaternion.LookRotation(-mar);
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
                go.name = prefab.name + "_" + n + "_" + casa.name;
                go.transform.rotation = giro;
                go.transform.localScale = Vector3.one * escala;
                Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
                go.transform.position = new Vector3(centro.x, ColocarT1354Curva.YBanqueta, centro.z) - giro * ancla;
                Bounds caja = CajaDe(go);
                Bounds chica = caja;
                chica.Expand(-EncimeMaximo * 2f);
                bool choca = puestos.Any(o => o.Intersects(chica));
                for (int k = 0; k < casas.Count && !choca; k++)
                {
                    choca = k != i && cajasCasas[k].Intersects(chica);
                }
                if (choca)
                {
                    omitidos++;
                    Informe.AppendLine("ML05 omitido en " + casa.name + ": se encimaba con un vecino (caja " + caja.size + ").");
                    Object.DestroyImmediate(go);
                    continue;
                }
                Renderer[] rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0 && go.GetComponent<LODGroup>() == null)
                {
                    LODGroup lod = go.AddComponent<LODGroup>();
                    lod.SetLODs(new[] { new LOD(0.012f, rs) });
                    lod.RecalculateBounds();
                }
                casa.gameObject.SetActive(false);
                puestos.Add(caja);
                frentes.RemoveAll(f => f.Item1 == casa);
                frentes.Add((go.transform, caja));
                Informe.AppendLine("ML05 " + go.name + " (escala " + escala.ToString("F2") + ", ancho " + caja.size.x.ToString("F1") + " en casa de " + cb.size.x.ToString("F1") + ")");
                n++;
            }
            Informe.AppendLine("ML05 colocados: " + n + ", omitidos: " + omitidos + ". Frente a la avenida (el modelo mira a -Z local, giro hacia el mar como el kit); revisar en la foto.");
            return frentes;
        }

        // ================================================================ Fachadas: neones, aires y toldos

        private static float DistanciaACamaras(Vector3 p)
        {
            Vector2 q = new Vector2(p.x, p.z);
            float d = Vector2.Distance(q, new Vector2(CamaraK11.x, CamaraK11.z));
            d = Mathf.Min(d, Vector2.Distance(q, new Vector2(CamaraK12.x, CamaraK12.z)));
            d = Mathf.Min(d, Vector2.Distance(q, new Vector2(-60f, -225f)));
            return d;
        }

        private static List<Vector3> PonerFachadas(KitS2c.Obra obra, Transform raiz, List<(Transform t, Bounds caja)> frentes)
        {
            List<Vector3> puntos = new List<Vector3>();
            Transform grupo = new GameObject("Fachadas").transform;
            grupo.SetParent(obra.Raiz.parent, false);
            GameObject ac = KitS2c.Prefab("C10_AireAcondicionado");
            GameObject toldo = KitS2c.Prefab("TL01_Toldo");
            List<Bounds> neonesViejos = raiz.GetComponentsInChildren<Transform>(false).Where(t => t.name == "Neon" && t.parent != null && t.parent.name == "Fachadas")
                .Select(t => CajaDe(t.gameObject)).ToList();
            Color[] colores = { KitS2c.Magenta, KitS2c.Turquesa, KitS2c.Sodio };
            int nNeon = 0, nAc = 0, nToldo = 0, luces = 0;
            System.Random azar = new System.Random(13930);
            foreach ((Transform t, Bounds cb) in frentes.OrderBy(f => DistanciaACamaras(f.caja.center)))
            {
                float z = ColocarT1354Curva.ZCentro(cb.center.x);
                float dz = (ColocarT1354Curva.ZCentro(cb.center.x + 1f) - ColocarT1354Curva.ZCentro(cb.center.x - 1f)) * 0.5f;
                Vector3 tang = new Vector3(1f, 0f, dz).normalized;
                Vector3 mar = ColocarT1354Curva.HaciaElMar(tang);
                Vector3 linea = new Vector3(cb.center.x, 0f, z);
                float ext = Mathf.Abs(mar.x) * cb.extents.x + Mathf.Abs(mar.z) * cb.extents.z;
                float frente = Vector3.Dot(cb.center - linea, mar) + ext;
                if (Mathf.Abs(frente - ColocarT1354Curva.LateralCiudad) > 2.5f)
                {
                    continue;
                }
                float ancho = Mathf.Max(2.5f, cb.size.x);
                Vector3 muro = linea + mar * frente;
                // Neón: bandera magenta/turquesa o de pared ámbar, a un lado del frente.
                Color color = colores[nNeon % colores.Length];
                Vector3 pNeon = muro + tang * (ancho * 0.32f) + Vector3.up * (color == KitS2c.Sodio ? 2.7f : 3.6f + (float)azar.NextDouble() * 1.2f);
                bool choca = neonesViejos.Any(o => Mathf.Abs(o.center.x - pNeon.x) < 2.2f && Mathf.Abs(o.center.z - pNeon.z) < 2.2f && Mathf.Abs(o.center.y - pNeon.y) < 2f);
                if (!choca)
                {
                    int hijos = grupo.childCount;
                    KitS2c.Neon(obra, grupo, pNeon, mar, tang, color, color != KitS2c.Sodio);
                    if (grupo.childCount > hijos)
                    {
                        Transform neon = grupo.GetChild(grupo.childCount - 1);
                        Brillar(neon, color);
                        Light luz = neon.GetComponentInChildren<Light>(true);
                        if (luz != null)
                        {
                            if (luces < TopeLucesNeon)
                            {
                                luz.intensity = 3f;
                                luz.range = 8f;
                                luces++;
                            }
                            else
                            {
                                Object.DestroyImmediate(luz.gameObject);
                            }
                        }
                        puntos.Add(pNeon);
                        nNeon++;
                    }
                }
                // Aire acondicionado al otro lado del frente, a 3.3 m.
                if (ac != null && azar.NextDouble() < 0.8)
                {
                    Vector3 pAc = muro - tang * (ancho * 0.3f) + Vector3.up * 3.3f;
                    if (PonerAnclado(ac, grupo, pAc, mar, 1f, "AireAcondicionado", false) != null) { nAc++; }
                }
                // Toldo sobre la puerta de la planta baja en ~40 %.
                if (toldo != null && azar.NextDouble() < 0.4)
                {
                    Vector3 pT = muro + tang * (ancho * -0.05f) + Vector3.up * 2.9f;
                    Bounds tb = Medida(toldo);
                    float e = Mathf.Clamp(Mathf.Min(3.2f, ancho * 0.6f) / Mathf.Max(0.5f, tb.size.x), 0.4f, 1.4f);
                    if (PonerAnclado(toldo, grupo, pT, mar, e, "Toldo", true) != null) { nToldo++; }
                }
            }
            Informe.AppendLine("Fachadas: " + nNeon + " neones (" + luces + " con luz, magenta/turquesa en bandera y ámbar de pared, con emisión), " + nAc + " aires, " + nToldo + " toldos.");
            return puntos;
        }

        /// <summary>Instancia un prefab del kit con su cara de atrás (+Z local) contra el muro en <paramref name="punto"/> y la cara al frente mirando a <paramref name="normal"/>.</summary>
        private static GameObject PonerAnclado(GameObject prefab, Transform padre, Vector3 punto, Vector3 normal, float escala, string nombre, bool desdeArriba)
        {
            Bounds mb = Medida(prefab);
            if (mb.size.x < 0.05f)
            {
                return null;
            }
            Quaternion giro = Quaternion.LookRotation(-normal);
            Vector3 ancla = new Vector3(mb.center.x, desdeArriba ? mb.max.y : mb.min.y, mb.max.z) * escala;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.rotation = giro;
            go.transform.localScale = Vector3.one * escala;
            go.transform.position = punto - giro * ancla;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
            return go;
        }

        /// <summary>Material emisivo (RealtimeEmissive) en los renderers del letrero: copia del material del kit con la emisión del color del neón.</summary>
        private static void Brillar(Transform neon, Color color)
        {
            string clave = color == KitS2c.Magenta ? "Magenta" : color == KitS2c.Turquesa ? "Turquesa" : "Ambar";
            foreach (Renderer r in neon.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material src = ms[i];
                    if (src == null)
                    {
                        continue;
                    }
                    string nombre = "T1393_" + src.name.Replace(" ", "_").Replace("/", "_").Replace("(Instance)", "") + "_" + clave;
                    Color emision = color * 2.4f;
                    ms[i] = KitS2c.Mat(nombre, () =>
                    {
                        Material m = new Material(src);
                        m.EnableKeyword("_EMISSION");
                        if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", emision); }
                        Texture baseTex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                        if (baseTex != null && m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", baseTex); }
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        return m;
                    });
                }
                r.sharedMaterials = ms;
            }
        }

        // ================================================================ Paseo: palmeras y faroles

        private static void PonerPaseo(KitS2c.Obra obra, Transform raiz, Transform mio)
        {
            GameObject palmera = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPalmera);
            GameObject farol = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFarol);
            if (palmera == null || farol == null)
            {
                Informe.AppendLine("FALTA: " + (palmera == null ? "AR03_Palmera " : "") + (farol == null ? "ML01_Farol" : "") + ". Sin paseo extra.");
                return;
            }
            Transform grupo = new GameObject("Paseo").transform;
            grupo.SetParent(mio, false);
            List<Bounds> ocupado = Ocupado(raiz);
            List<Vector3> borde = ColocarT1354Curva.BordeMar(-330f, 175f);
            float largo = ColocarT1354Curva.Longitud(borde);
            List<(float d, bool esPalmera, Vector3 p, Quaternion giro)> candidatos = new List<(float, bool, Vector3, Quaternion)>();
            int i = 0;
            for (float d = 12f; d <= largo; d += 12f, i++)
            {
                Vector3 p = ColocarT1354Curva.PuntoEn(borde, d, out Vector3 t);
                Vector3 mar = ColocarT1354Curva.HaciaElMar(t);
                p += mar * LateralPaseo;
                p.y = ColocarT1354Curva.YBanqueta;
                if (Mathf.Abs(Mathf.Abs(p.x) - 47f) < 10f || p.x > 100f)
                {
                    continue;
                }
                candidatos.Add((d, i % 4 < 2, p, Quaternion.LookRotation(mar)));
            }
            int nP = 0, nF = 0, luces = 0;
            foreach (var c in candidatos.OrderBy(x => DistanciaACamaras(x.p)))
            {
                float dCamara = Mathf.Min(Vector2.Distance(new Vector2(c.p.x, c.p.z), new Vector2(CamaraK11.x, CamaraK11.z)),
                    Vector2.Distance(new Vector2(c.p.x, c.p.z), new Vector2(CamaraK12.x, CamaraK12.z)));
                if (c.esPalmera && dCamara < RadioSinPalmera)
                {
                    continue;
                }
                if (!c.esPalmera && dCamara < 1.5f)
                {
                    continue;
                }
                GameObject prefab = c.esPalmera ? palmera : farol;
                float escala = c.esPalmera ? 0.9f + (float)obra.Azar.NextDouble() * 0.25f : 1f;
                Quaternion giro = c.esPalmera ? Quaternion.Euler(0f, (float)obra.Azar.NextDouble() * 360f, 0f) : c.giro;
                GameObject go = Colocar(prefab, grupo, c.p, giro, escala, ocupado, (c.esPalmera ? "AR03_Palmera_" : "ML01_Farol_") + (nP + nF));
                if (go == null)
                {
                    continue;
                }
                if (c.esPalmera)
                {
                    nP++;
                    continue;
                }
                nF++;
                Bounds fb = Medida(prefab);
                Vector3 cabeza = c.p + Vector3.up * fb.size.y * 0.92f;
                KitS2c.Halo(obra, go.transform, cabeza, ColocarT1354Curva.YBanqueta, KitS2c.Sodio);
                if (luces < TopeLucesFarol)
                {
                    Vector3 haciaAvenida = -(c.giro * Vector3.forward);
                    KitS2c.Luz(go.transform, cabeza, LightType.Spot, KitS2c.Sodio, ColocarS2oLuz.IntensidadPoste * 1.6f, ColocarS2oLuz.RangoPoste * 1.25f,
                        ColocarS2oLuz.AnguloPoste, (haciaAvenida * 0.25f + Vector3.down).normalized);
                    luces++;
                }
            }
            Informe.AppendLine("Paseo extra: " + nP + " palmeras, " + nF + " faroles (" + luces + " con spot de sodio sin sombras, el resto solo halo); fuera de " + RadioSinPalmera + " m de K11 y K12.");
        }

        private static List<Bounds> Ocupado(Transform raiz)
        {
            List<Bounds> lista = new List<Bounds>();
            foreach (string grupo in new[] { "Paseo", "Postes", "Muelle", Raiz1384 })
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

        private static GameObject Colocar(GameObject prefab, Transform padre, Vector3 suelo, Quaternion giro, float escala, List<Bounds> ocupado, string nombre)
        {
            Bounds mb = Medida(prefab);
            Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
            Vector3 medio = suelo + Vector3.up * (mb.size.y * escala * 0.5f);
            Vector3 tam = giro * (mb.size * escala);
            Bounds ocupa = new Bounds(medio, new Vector3(Mathf.Abs(tam.x), Mathf.Abs(tam.y), Mathf.Abs(tam.z)));
            Bounds g = ocupa;
            g.Expand(0.4f);
            if (ocupado.Any(o => o.Intersects(g)))
            {
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.rotation = giro;
            go.transform.localScale = Vector3.one * escala;
            go.transform.position = suelo - giro * ancla;
            ocupado.Add(ocupa);
            return go;
        }

        // ================================================================ Cables

        private static void PonerCables(KitS2c.Obra obra, Transform raiz, List<Vector3> puntosNeon)
        {
            Transform postes = raiz.Find("Postes");
            if (postes == null)
            {
                Informe.AppendLine("PENDIENTE: no hay Postes; sin cables nuevos.");
                return;
            }
            List<Bounds> lista = postes.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null)
                .Select(t => CajaDe(t.gameObject)).OrderBy(b => b.center.x).ToList();
            int n = 0;
            for (int i = 0; i + 1 < lista.Count && n < TopeCables; i++)
            {
                Vector3 a = new Vector3(lista[i].center.x, lista[i].max.y - 0.4f, lista[i].center.z);
                Vector3 b = new Vector3(lista[i + 1].center.x, lista[i + 1].max.y - 0.4f, lista[i + 1].center.z);
                float d = Vector3.Distance(a, b);
                if (d < 6f || d > 60f)
                {
                    continue;
                }
                for (int k = 0; k < 3; k++)
                {
                    KitS2c.Cable(obra, a + Vector3.down * (0.5f * k), b + Vector3.down * (0.5f * k), 0.6f + 0.2f * k);
                    n++;
                }
            }
            // De cada poste hacia el letrero de neón más cercano de la acera de enfrente.
            foreach (Bounds pb in lista)
            {
                if (n >= TopeCables || puntosNeon.Count == 0)
                {
                    break;
                }
                Vector3 a = new Vector3(pb.center.x, pb.max.y - 0.8f, pb.center.z);
                Vector3 mejor = puntosNeon.OrderBy(p => Vector3.Distance(p, a)).First();
                float d = Vector3.Distance(a, mejor);
                if (d > 4f && d < 24f)
                {
                    KitS2c.Cable(obra, a, mejor + Vector3.up * 0.3f, 0.5f);
                    n++;
                }
            }
            Informe.AppendLine("Cables nuevos: " + n + " (postes entre sí en tres hilos y postes a letreros).");
        }

        // ================================================================ Estáticos

        private static void MarcarEstaticos(Transform mio)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in mio.GetComponentsInChildren<Transform>(true))
            {
                if (t == mio || t.GetComponent<Light>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T-1393/Validar")]
        public static void Validar()
        {
            Informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mio = raiz.Find(NombreRaiz);
                if (mio == null)
                {
                    Informe.AppendLine("ERROR: no hay " + NombreRaiz + "; corre Aplicar.");
                    Escribir("t1393-validar");
                    return;
                }
                int problemas = 0;
                float radioSkyline = 900f * EscalaRadio;
                if (radioSkyline < RadioMinimoSkyline || radioSkyline > RadioMaximoSkyline) { problemas++; Informe.AppendLine("ERROR: radio del skyline " + radioSkyline + " fuera de 350 a 550 m."); }
                int luces = mio.GetComponentsInChildren<Light>(true).Length;
                Informe.AppendLine("Luces nuevas: " + luces + " (tope " + TopeLucesNuevas + ")");
                Informe.AppendLine("Luces de la zona: " + raiz.GetComponentsInChildren<Light>(true).Length);
                Informe.AppendLine("Objetos en " + NombreRaiz + ": " + mio.GetComponentsInChildren<Transform>(true).Length + " (tope 3000)");
                if (luces > TopeLucesNuevas) { problemas++; Informe.AppendLine("ERROR: demasiadas luces."); }

                // Otra orilla vieja apagada y sin restos activos del skyline plano.
                Transform orilla = raiz.Find("OtraOrilla");
                if (orilla != null && orilla.gameObject.activeSelf) { problemas++; Informe.AppendLine("ERROR: la OtraOrilla vieja sigue activa."); }
                Transform t1384 = raiz.Find(Raiz1384);
                if (t1384 != null && t1384.Find("Skyline") != null) { problemas++; Informe.AppendLine("ERROR: sigue " + Raiz1384 + "/Skyline."); }

                // Skyline: radio dentro de 350 a 550 m de K11 y todo bajo los 600 m de la cámara de fotos.
                Transform sk = mio.Find("Skyline");
                if (sk != null)
                {
                    foreach (Renderer r in sk.GetComponentsInChildren<Renderer>(true))
                    {
                        Bounds b = r.bounds;
                        Vector3 c = CamaraK11;
                        float cerca = float.MaxValue, lejos = 0f;
                        foreach (Vector3 q in Esquinas(b))
                        {
                            float d = Vector2.Distance(new Vector2(q.x, q.z), new Vector2(c.x, c.z));
                            cerca = Mathf.Min(cerca, d);
                            lejos = Mathf.Max(lejos, d);
                        }
                        Informe.AppendLine("Skyline " + r.transform.name + ": distancia a K11 de " + cerca.ToString("F0") + " a " + lejos.ToString("F0") + " m, alto " + b.size.y.ToString("F0") + " m, base y " + b.min.y.ToString("F1"));
                        if (lejos > 600f) { problemas++; Informe.AppendLine("ERROR: pasa de 600 m (far clip)."); }
                    }
                }
                else { problemas++; Informe.AppendLine("ERROR: no hay Skyline."); }
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.name.IndexOf("Mar", StringComparison.Ordinal) >= 0 && r.transform.parent != null)
                    {
                        Bounds b = r.bounds;
                        if (b.size.x > 300f) { Informe.AppendLine("Mar/fondo " + r.transform.parent.name + "/" + r.name + ": x " + b.min.x.ToString("F0") + ".." + b.max.x.ToString("F0") + ", z " + b.min.z.ToString("F0") + ".." + b.max.z.ToString("F0")); }
                    }
                }

                // ML05: sin encimarse, sin huecos, frente a la avenida y base en la banqueta.
                Transform ml = mio.Find("ML05");
                if (ml != null)
                {
                    List<Transform> hijos = ml.Cast<Transform>().ToList();
                    List<Bounds> cajas = hijos.Select(h => CajaDe(h.gameObject)).ToList();
                    Transform fachadas = raiz.Find("Fachadas");
                    List<Bounds> casas = fachadas == null ? new List<Bounds>() : fachadas.Cast<Transform>().Where(t => t.name.StartsWith("Casa_", StringComparison.Ordinal) && t.gameObject.activeSelf)
                        .Select(t => CajaDe(t.gameObject)).ToList();
                    for (int i = 0; i < hijos.Count; i++)
                    {
                        Bounds ch = cajas[i];
                        ch.Expand(-EncimeMaximo * 2f);
                        for (int j = i + 1; j < hijos.Count; j++)
                        {
                            if (ch.Intersects(cajas[j])) { problemas++; Informe.AppendLine("ENCIMADO: " + hijos[i].name + " con " + hijos[j].name); }
                        }
                        foreach (Bounds c in casas)
                        {
                            if (ch.Intersects(c)) { problemas++; Informe.AppendLine("ENCIMADO con una casa del kit: " + hijos[i].name); break; }
                        }
                        if (cajas[i].min.y > 0.7f || cajas[i].min.y < -0.4f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + hijos[i].name + " min.y=" + cajas[i].min.y.ToString("F2")); }
                        // Hueco: la casa que reemplaza (por nombre) debe medir parecido de ancho.
                        int ix = hijos[i].name.IndexOf("Casa_", StringComparison.Ordinal);
                        if (ix >= 0 && fachadas != null)
                        {
                            Transform casa = fachadas.Find(hijos[i].name.Substring(ix));
                            if (casa != null)
                            {
                                Bounds cb = CajaTodo(casa.gameObject);
                                float dif = cajas[i].size.x - cb.size.x;
                                if (Mathf.Abs(dif) > 0.8f) { problemas++; Informe.AppendLine("HUECO/SOBRA de ancho " + dif.ToString("F2") + " m: " + hijos[i].name); }
                            }
                        }
                    }
                    Informe.AppendLine("ML05: " + hijos.Count);
                }

                // Letreros, aires y toldos: contra el muro, a altura razonable.
                Transform fach = mio.Find("Fachadas");
                int neones = 0;
                if (fach != null)
                {
                    foreach (Transform t in fach)
                    {
                        Bounds b = CajaDe(t.gameObject);
                        if (b.size == Vector3.zero) { continue; }
                        if (t.name == "Neon") { neones++; }
                        if (b.min.y < 1.9f || b.min.y > 7f) { problemas++; Informe.AppendLine("ALTURA rara: " + t.name + " min.y=" + b.min.y.ToString("F2")); }
                        float z = ColocarT1354Curva.ZCentro(b.center.x);
                        if (b.center.z - z < 8.5f) { problemas++; Informe.AppendLine("EN EL CARRIL o banqueta: " + t.name + " en " + b.center); }
                    }
                }
                Informe.AppendLine("Neones nuevos: " + neones);

                // Paseo extra: sobre la banqueta, fuera del carril.
                Transform paseo = mio.Find("Paseo");
                if (paseo != null)
                {
                    foreach (Transform t in paseo)
                    {
                        Bounds b = CajaDe(t.gameObject);
                        float z = ColocarT1354Curva.ZCentro(b.center.x);
                        if (b.min.y > TopeFlotante || b.min.y < -0.5f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + t.name + " min.y=" + b.min.y.ToString("F2")); }
                        if (z - b.center.z < 9f) { problemas++; Informe.AppendLine("MUY CERCA DEL CARRIL: " + t.name); }
                        if (t.name.StartsWith("AR03_Palmera", StringComparison.Ordinal))
                        {
                            foreach (Vector3 cam in new[] { CamaraK11, CamaraK12 })
                            {
                                if (Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(cam.x, cam.z)) < RadioSinPalmera) { problemas++; Informe.AppendLine("PALMERA TAPA LA CÁMARA: " + t.name); }
                            }
                        }
                    }
                }
                Informe.AppendLine("Problemas: " + problemas);
                Informe.AppendLine("Tamaño del prefab (MB): " + (new FileInfo(RutaPrefab).Length / 1048576f).ToString("F1") + " (tope 60)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1393-validar");
        }

        private static IEnumerable<Vector3> Esquinas(Bounds b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            }
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T-1393/Fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                // K11: en la banqueta del mar, mirando al oeste (el mar queda a la izquierda), como la T-1384.
                ("k11", "K11_Malecon.png", CamaraK11, new Vector3(-59f, 7f, -221.7f), 60f),
                // K12: junto al puesto, mirando al oeste por la avenida con el muelle y el mar a la izquierda (en la ronda 1 miraba al este y el mar quedaba a la derecha).
                ("k12", "K12_Malecon_Muelle.png", CamaraK12, MiraK12, 75f),
                ("a11", "A11_Malecon_Aerea.png", new Vector3(-20f, 40f, -200f), new Vector3(-95f, 0f, -250f), 70f),
            };
            Directory.CreateDirectory("docs/capturas/t-1393");
            foreach (var t in tomas)
            {
                string juego = "Logs/t1393-" + t.nombre + ".png";
                ColocarS2cLook.Foto(t.desde, t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/t-1393/malecon-" + t.nombre + ".png");
            }
        }

        // ================================================================ Ayudas

        private static Bounds CajaDe(GameObject go)
        {
            Bounds b = new Bounds();
            bool hay = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false))
            {
                if (!hay) { b = r.bounds; hay = true; }
                else { b.Encapsulate(r.bounds); }
            }
            return hay ? b : new Bounds(go.transform.position, Vector3.zero);
        }

        /// <summary>Como CajaDe pero cuenta también los renderers de objetos apagados (la casa que ML05 reemplazó).</summary>
        private static Bounds CajaTodo(GameObject go)
        {
            Bounds b = new Bounds();
            bool hay = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!hay) { b = r.bounds; hay = true; }
                else { b.Encapsulate(r.bounds); }
            }
            return hay ? b : new Bounds(go.transform.position, Vector3.zero);
        }

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
