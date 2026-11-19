using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1463: el Malecón igual a K11 y A11, con vida. Corre DESPUÉS de T1384, T1393, T1400, T1404 y T1413 (mismo prefab T1354_Malecon; todo lo nuevo cuelga de
    /// "T1463_Malecon", que se borra y rehace y vuelve a encender lo que apagó).
    /// 1) Marcas de cámara Vista19b_K11 y Vista19b_A11 (hijos Mira y Campo_&lt;grados&gt;). K11 va en la banqueta del mar a x = -150, donde la avenida ya curva a la derecha.
    /// 2) La fila del paseo se rehace: un farol ML01 cada 15 m y entre cada dos faroles una palmera AR03 (también cada 15 m); las palmeras y faroles viejos del paseo se apagan.
    /// 3) Letrero de neón (con emisión, sin Light) en cada edificio del frente que cae en el cuadro de K11 o de A11 y no tiene.
    /// 4) Gente de decorado (Visual_NP07..NP14 con PoseQuieta en estado Idle) en el paseo y la banqueta; basura y cajas en el paseo.
    /// 5) PoblarFoto(Camera) pone carros con luces en los carriles y gente caminando para la foto; QuitarFoto() los quita. No se guardan.
    /// Orden: T1463.Aplicar, T1463.Validar. En la foto: ColocarS2.Abrir, PoblarFoto(cámara), foto, QuitarFoto.
    /// </summary>
    public static class ZonaMaleconT1463
    {
        public const string RutaPrefab = ZonaMaleconT1393.RutaPrefab;
        public const string NombreRaiz = "T1463_Malecon";
        public const string NombreFoto = "T1463_Foto";
        private const string NombreApagados = "T1463_Apagados";
        private const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        private const string CarpetaCarros = "Assets/Arte/Vehiculos/Prefabs/";
        private const string RutaPalmera = "Assets/Arte/Zonas/AR/AR03_Palmera.prefab";
        private const string RutaFarol = "Assets/Arte/Zonas/ML/ML01_Farol.prefab";

        // Fila del paseo
        private const float PasoFila = 15f;
        private const float LateralFila = 11.2f;
        private const float XFilaDesde = -300f;
        private const float XFilaHasta = 105f;
        private const float RadioCamara = 2.2f;
        private const float RadioPalmeraCamara = 6f;
        private const int TopeSpots = 36;
        private const int CupoLuces = 40;

        // Gente
        private const float LateralBarandalGente = 14.7f;
        private const float LateralBanquetaGente = -9.3f;
        private const float PasoGente = 22f;

        // Cámaras
        private const float AlturaOjos = 1.9f;
        private const float CampoK11 = 60f;
        private const float CampoA11 = 70f;
        private const float Aspecto = 16f / 9f;
        private static readonly Vector3 CamaraA11 = new Vector3(-125f, 28f, -255f);
        private static readonly Vector3 MiraA11 = new Vector3(-165f, 0f, -238f);
        private static readonly Vector2 CentroIslote = new Vector2(-200f, -285f);
        private const string NombreFaroA11 = "ML04_Faro_T1463";
        private static readonly string[] NombresVisuales =
        {
            "Visual_NP07_Pescador_a", "Visual_NP08_Turista_a", "Visual_NP09_Corredor_a", "Visual_NP10_Elotero_a", "Visual_NP11_Estibador_a", "Visual_NP12_Abuela_a",
            "Visual_NP13_Repartidor_a", "Visual_NP14_Oficinista_a", "Visual_NP07_Pescador_b", "Visual_NP08_Turista_b", "Visual_NP11_Estibador_b", "Visual_NP13_Repartidor_b",
            "Visual_NP14_Oficinista_b",
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Geometría de las cámaras

        private static void Marco(float x, out Vector3 linea, out Vector3 tang, out Vector3 mar)
        {
            float z = ColocarT1354Curva.ZCentro(x);
            float dz = (ColocarT1354Curva.ZCentro(x + 1f) - ColocarT1354Curva.ZCentro(x - 1f)) * 0.5f;
            linea = new Vector3(x, 0f, z);
            tang = new Vector3(1f, 0f, dz).normalized;
            mar = ColocarT1354Curva.HaciaElMar(tang);
        }

        private static Vector3 PuntoLateral(float x, float lateral, float y)
        {
            Marco(x, out Vector3 linea, out _, out Vector3 mar);
            Vector3 p = linea + mar * lateral;
            p.y = y;
            return p;
        }

        /// <summary>K11: en el paseo, a 9.6 m del eje de la avenida (la fila de palmeras queda a la izquierda y el barandal más allá), a 1.9 m de alto, mirando recto al oeste: la avenida que sigue curva a la derecha en pantalla.</summary>
        public static Vector3 CamaraK11 => PuntoLateral(-150f, 8f, AlturaOjos);

        public static Vector3 MiraK11 => new Vector3(CamaraK11.x - 80f, 0.2f, CamaraK11.z);

        private static bool Proyectar(Vector3 cam, Vector3 mira, float campo, Vector3 p, out float sx, out float sy)
        {
            Vector3 eje = (mira - cam).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, eje).normalized;
            Vector3 arriba = Vector3.Cross(eje, derecha);
            Vector3 d = p - cam;
            float f = Vector3.Dot(d, eje);
            sx = 0f;
            sy = 0f;
            if (f < 0.5f)
            {
                return false;
            }
            float tv = Mathf.Tan(campo * 0.5f * Mathf.Deg2Rad);
            sx = Vector3.Dot(d, derecha) / (f * tv * Aspecto);
            sy = Vector3.Dot(d, arriba) / (f * tv);
            return true;
        }

        private static bool EnCuadro(Vector3 cam, Vector3 mira, float campo, Vector3 p)
        {
            return Proyectar(cam, mira, campo, p, out float sx, out float sy) && Mathf.Abs(sx) <= 1f && Mathf.Abs(sy) <= 1f;
        }

        private static bool CajaEnCuadro(Vector3 cam, Vector3 mira, float campo, Bounds b)
        {
            return EnCuadro(cam, mira, campo, b.center) || ZonaMaleconT1393.Esquinas(b).Any(e => EnCuadro(cam, mira, campo, e));
        }

        private static bool EnK11(Bounds b)
        {
            return Vector3.Distance(b.center, CamaraK11) <= 170f && CajaEnCuadro(CamaraK11, MiraK11, CampoK11, b);
        }

        private static bool EnA11(Bounds b)
        {
            return Vector3.Distance(b.center, CamaraA11) <= 300f && CajaEnCuadro(CamaraA11, MiraA11, CampoA11, b);
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        // ================================================================ Utilidades

        private static string Camino(Transform raiz, Transform t)
        {
            List<string> idx = new List<string>();
            for (Transform c = t; c != raiz && c != null; c = c.parent) { idx.Add(c.GetSiblingIndex().ToString()); }
            idx.Reverse();
            return string.Join("/", idx);
        }

        private static void Apagar(Transform raiz, Transform apagados, Transform t)
        {
            if (!t.gameObject.activeSelf) { return; }
            string marca = Camino(raiz, t) + "|" + t.name;
            t.gameObject.SetActive(false);
            new GameObject(marca).transform.SetParent(apagados, false);
        }

        private static void DeshacerAnterior(Transform raiz)
        {
            Transform vieja = raiz.Find(NombreRaiz);
            if (vieja == null) { return; }
            int n = 0;
            Transform ap = vieja.Find(NombreApagados);
            if (ap != null)
            {
                foreach (Transform marca in ap)
                {
                    string[] partes = marca.name.Split('|');
                    if (partes.Length != 2) { continue; }
                    Transform t = raiz;
                    foreach (string s in partes[0].Split('/'))
                    {
                        if (t == null || !int.TryParse(s, out int k) || k < 0 || k >= t.childCount) { t = null; break; }
                        t = t.GetChild(k);
                    }
                    if (t != null && t.name == partes[1] && !t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(true);
                        n++;
                    }
                }
            }
            Object.DestroyImmediate(vieja.gameObject);
            Informe.AppendLine("Deshecho lo anterior de T-1463: " + n + " objetos vueltos a encender.");
        }

        private static GameObject Cargar(string codigo)
        {
            string guid = AssetDatabase.FindAssets(codigo + " t:Prefab").FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == codigo);
            if (guid == null)
            {
                guid = AssetDatabase.FindAssets(codigo + " t:Model").FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == codigo);
            }
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static List<GameObject> CargarVisuales()
        {
            List<GameObject> lista = new List<GameObject>();
            foreach (string n in NombresVisuales)
            {
                GameObject v = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + n + ".prefab");
                if (v != null) { lista.Add(v); }
            }
            return lista;
        }

        private static float DistanciaEnX(IList<Vector3> linea, float x)
        {
            float d = 0f;
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                float tramo = Vector3.Distance(linea[i], linea[i + 1]);
                if (linea[i + 1].x >= x)
                {
                    float dx = linea[i + 1].x - linea[i].x;
                    return d + (dx < 1e-4f ? 0f : Mathf.Clamp01((x - linea[i].x) / dx) * tramo);
                }
                d += tramo;
            }
            return d;
        }

        private static float LateralDe(Vector3 p)
        {
            Marco(p.x, out Vector3 linea, out _, out Vector3 mar);
            return Vector3.Dot(p - linea, mar);
        }

        private static List<Bounds> OcupadoPaseo(Transform raiz)
        {
            List<Bounds> lista = ZonaMaleconT1393.Ocupado(raiz);
            foreach (string ruta in new[] { ZonaMaleconT1393.NombreRaiz + "/Paseo", ZonaMaleconT1400.NombreRaiz + "/Paseo", ZonaMaleconT1413.NombreRaiz + "/Puestos", ZonaMaleconT1413.NombreRaiz + "/Muelle", "Puestos" })
            {
                Transform g = raiz.Find(ruta);
                if (g == null) { continue; }
                foreach (Transform t in g)
                {
                    if (t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null) { lista.Add(ZonaMaleconT1393.CajaDe(t.gameObject)); }
                }
            }
            return lista;
        }

        /// <summary>Ocupado sin las losas, el barandal ni las piezas largas del paseo: solo cosas de pie de menos de 10 m.</summary>
        private static List<Bounds> OcupadoDePie(Transform raiz)
        {
            return OcupadoPaseo(raiz).Where(b => b.size.x <= 10f && b.size.z <= 10f && b.size.y >= 0.6f).ToList();
        }

        /// <summary>Como Colocar de T-1393 pero la huella que se revisa es la del pie (no la copa ni los brazos): cabe junto al barandal.</summary>
        private static GameObject PonerSimple(GameObject prefab, Transform padre, Vector3 suelo, Quaternion giro, float escala, List<Bounds> ocupado, float huella, string nombre)
        {
            Bounds mb = ZonaMaleconT1393.Medida(prefab);
            Bounds pie = new Bounds(suelo + Vector3.up * 1.2f, new Vector3(huella, 2.4f, huella));
            if (ocupado.Any(o => o.Intersects(pie))) { return null; }
            Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.rotation = giro;
            go.transform.localScale = Vector3.one * escala;
            go.transform.position = suelo - giro * ancla;
            ocupado.Add(pie);
            return go;
        }

        private static void MarcarEstaticos(Transform mio)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in mio.GetComponentsInChildren<Transform>(true))
            {
                // La gente tiene Animator y SkinnedMesh: no se marca estática.
                if (t == mio || t.GetComponent<Light>() != null || t.name == NombreApagados || (t.parent != null && t.parent.name == NombreApagados)
                    || t.GetComponentInParent<PoseQuieta>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1463/Malecon (aplicar)")]
        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1463 Malecón (K11 y A11 con vida)");
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                DeshacerAnterior(raiz);
                Transform mio = new GameObject(NombreRaiz).transform;
                mio.SetParent(raiz, false);
                Transform apagados = new GameObject(NombreApagados).transform;
                apagados.SetParent(mio, false);
                int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                KitS2c.Obra obra = KitS2c.Empezar("T1463_Extras", 1463, mio);
                PonerMarca(mio, "Vista19b_K11", CamaraK11, MiraK11, CampoK11);
                PonerMarca(mio, "Vista19b_A11", CamaraA11, MiraA11, CampoA11);
                ApagarFilaVieja(raiz, apagados);
                PonerFila(obra, raiz, mio);
                ArreglarFarosExistentes(raiz);
                PonerFaroA11(obra, mio);
                PonerArena(raiz);
                PonerLetreros(raiz, mio);
                PonerGente(raiz, mio, obra);
                PonerBasura(raiz, mio);
                InformarCosta(raiz);
                MarcarEstaticos(mio);
                KitS2c.Terminar(obra);
                int nuevas = mio.GetComponentsInChildren<Light>(true).Length;
                Informe.AppendLine("Luces antes " + lucesAntes + ", después " + raiz.GetComponentsInChildren<Light>(true).Length + "; nuevas de T-1463: " + nuevas + " (cupo " + CupoLuces + ").");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1463-malecon-aplicar");
        }

        private static void PonerMarca(Transform mio, string nombre, Vector3 desde, Vector3 mira, float campo)
        {
            GameObject m = new GameObject(nombre);
            m.transform.SetParent(mio, false);
            m.transform.position = desde;
            GameObject ojo = new GameObject("Mira");
            ojo.transform.SetParent(m.transform, false);
            ojo.transform.position = mira;
            GameObject c = new GameObject("Campo_" + campo.ToString(CultureInfo.InvariantCulture));
            c.transform.SetParent(m.transform, false);
            Informe.AppendLine("Marca " + nombre + ": desde " + desde.ToString("F1") + " hacia " + mira.ToString("F1") + ", campo " + campo + ".");
        }

        // ================================================================ Fila de palmeras y faroles

        private static bool EsFilaVieja(Transform t)
        {
            return t.name.StartsWith("AR03_Palmera", StringComparison.Ordinal) || t.name.StartsWith("ML01_Farol", StringComparison.Ordinal);
        }

        private static void ApagarFilaVieja(Transform raiz, Transform apagados)
        {
            Transform mio = raiz.Find(NombreRaiz);
            int n = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).ToList())
            {
                if (!EsFilaVieja(t) || !t.gameObject.activeSelf || t.IsChildOf(mio)) { continue; }
                if (t.parent != null && EsFilaVieja(t.parent)) { continue; }
                Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                Vector3 c = b.size == Vector3.zero ? t.position : b.center;
                float lateral = LateralDe(c);
                if (c.x < XFilaDesde - 30f || c.x > XFilaHasta || lateral < 5f || lateral > 18f) { continue; }
                Apagar(raiz, apagados, t);
                n++;
            }
            Informe.AppendLine("Fila vieja: " + n + " palmeras y faroles del paseo (x de " + (XFilaDesde - 30f) + " a " + XFilaHasta + ") apagados; los nuevos los reemplazan.");
        }

        private static void PonerFila(KitS2c.Obra obra, Transform raiz, Transform mio)
        {
            GameObject palmera = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPalmera);
            GameObject farol = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFarol);
            if (palmera == null || farol == null)
            {
                Informe.AppendLine("FALTA: AR03_Palmera o ML01_Farol. Sin fila.");
                return;
            }
            Transform grupo = new GameObject("Fila").transform;
            grupo.SetParent(mio, false);
            List<Bounds> ocupado = OcupadoDePie(raiz);
            List<Vector3> borde = ColocarT1354Curva.BordeMar(-330f, 175f);
            float largo = ColocarT1354Curva.Longitud(borde);
            // Un farol queda 6 m delante de la cámara de K11 (como en la referencia); los demás cada 15 m. Las palmeras van a medio camino entre dos faroles.
            float dRef = DistanciaEnX(borde, CamaraK11.x - 9f);
            Vector3 camK = CamaraK11;
            Vector3 camA = new Vector3(CamaraA11.x, 0f, CamaraA11.z);
            List<(GameObject go, Vector3 cabeza, Vector3 haciaAvenida)> faroles = new List<(GameObject, Vector3, Vector3)>();
            int nP = 0, nF = 0, omitidos = 0;
            Bounds fb = ZonaMaleconT1393.Medida(farol);
            for (int k = -60; k <= 60; k++)
            {
                for (int tipo = 0; tipo < 2; tipo++)
                {
                    bool esPalmera = tipo == 1;
                    float d = dRef + PasoFila * k + (esPalmera ? PasoFila * 0.5f : 0f);
                    if (d < 0f || d > largo) { continue; }
                    Vector3 pt = ColocarT1354Curva.PuntoEn(borde, d, out _);
                    if (pt.x < XFilaDesde || pt.x > XFilaHasta) { continue; }
                    Marco(pt.x, out Vector3 linea, out _, out Vector3 mar);
                    Vector3 pos = linea + mar * LateralFila;
                    pos.y = ColocarT1354Curva.YBanqueta;
                    if (Plano(pos, camK) < (esPalmera ? RadioPalmeraCamara : RadioCamara)) { omitidos++; continue; }
                    GameObject go = esPalmera
                        ? PonerSimple(palmera, grupo, pos, Quaternion.Euler(0f, (float)obra.Azar.NextDouble() * 360f, 0f), 1.1f + (float)obra.Azar.NextDouble() * 0.3f, ocupado, 1.2f, "AR03_Palmera_" + nP)
                        : PonerSimple(farol, grupo, pos, Quaternion.LookRotation(mar), 1f, ocupado, 1f, "ML01_Farol_" + nF);
                    if (go == null) { omitidos++; continue; }
                    if (esPalmera) { nP++; continue; }
                    nF++;
                    Vector3 cabeza = pos + Vector3.up * fb.size.y * 0.92f;
                    KitS2c.Halo(obra, go.transform, cabeza, ColocarT1354Curva.YBanqueta, KitS2c.Sodio);
                    faroles.Add((go, cabeza, -mar));
                }
            }
            int spots = 0;
            foreach (var f in faroles.OrderBy(x => Mathf.Min(Plano(x.cabeza, camK), Plano(x.cabeza, camA))))
            {
                if (spots >= TopeSpots) { break; }
                KitS2c.Luz(f.go.transform, f.cabeza, LightType.Spot, KitS2c.Sodio, ColocarS2oLuz.IntensidadPoste * 3.2f, ColocarS2oLuz.RangoPoste * 1.25f,
                    ColocarS2oLuz.AnguloPoste, (f.haciaAvenida * 0.25f + Vector3.down).normalized);
                spots++;
            }
            PonerCharcosDeLuz(obra, mio, faroles.Select(x => (x.go.transform.position, x.haciaAvenida)).ToList());
            Informe.AppendLine("Fila nueva: " + nP + " palmeras y " + nF + " faroles cada " + PasoFila + " m a " + LateralFila + " m del eje (" + spots + " con spot de sodio sin sombras, el resto solo halo); "
                + omitidos + " omitidos por choque o por la cámara.");
        }

        // ================================================================ Charcos de luz y arena

        private static Texture2D TexCharcoLuz()
        {
            const string ruta = KitS2c.CarpetaTexturas + "/T1463_CharcoLuz.png";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (t != null) { return t; }
            Directory.CreateDirectory(KitS2c.CarpetaTexturas);
            const int n = 64;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                    float v = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static Material MatCharcoLuz()
        {
            return KitS2c.Mat("T1463_CharcoLuz", () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                m.SetTexture("_BaseMap", TexCharcoLuz());
                m.SetColor("_BaseColor", KitS2c.Sodio * 0.55f);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
                m.SetFloat("_SrcBlend", 1f);
                m.SetFloat("_DstBlend", 1f);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
        }

        /// <summary>Un plano aditivo con degradado naranja sobre el piso bajo cada farol (corrido hacia la avenida), sin Light: el charco de luz de la referencia.</summary>
        private static void PonerCharcosDeLuz(KitS2c.Obra obra, Transform mio, List<(Vector3 pie, Vector3 haciaAvenida)> faroles)
        {
            Transform grupo = new GameObject("CharcosDeLuz").transform;
            grupo.SetParent(mio, false);
            Material mat = MatCharcoLuz();
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            int n = 0;
            foreach ((Vector3 pie, Vector3 haciaAvenida) in faroles)
            {
                GameObject q = new GameObject("CharcoLuz_" + n++);
                q.transform.SetParent(grupo, false);
                q.transform.position = new Vector3(pie.x, ColocarT1354Curva.YBanqueta + 0.03f, pie.z) + haciaAvenida * 1.6f;
                q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(9f, 9f, 1f);
                q.AddComponent<MeshFilter>().sharedMesh = quad;
                MeshRenderer mr = q.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            Informe.AppendLine("Charcos de luz: " + n + " planos aditivos naranjas de 9 m bajo los faroles (sin Light).");
        }

        /// <summary>La playa de T-1354 pasa de T1354_ArenaClara a T1463_Arena: arena grisácea mojada (0.35, 0.33, 0.30, suavidad 0.35).</summary>
        private static void PonerArena(Transform raiz)
        {
            Material arena = KitS2c.Mat("T1463_Arena", () => KitS2c.Lit(new Color(0.35f, 0.33f, 0.30f), 0.35f));
            int n = 0;
            Transform mio = raiz.Find(NombreRaiz);
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Playa" || t.IsChildOf(mio)) { continue; }
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = arena;
                    n++;
                }
            }
            Informe.AppendLine("Arena: " + n + " renderers de la playa con el material T1463_Arena (falta la textura de arena de playa: assets que faltan).");
        }

        // ================================================================ Poses en el editor

        private static AnimationClip ClipDeEstado(Animator a, string estado)
        {
            RuntimeAnimatorController rc = a.runtimeAnimatorController;
            if (rc == null) { return null; }
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(rc));
            if (ctrl != null && ctrl.layers.Length > 0)
            {
                foreach (ChildAnimatorState cs in ctrl.layers[0].stateMachine.states)
                {
                    if (cs.state.name == estado && cs.state.motion is AnimationClip c) { return c; }
                }
            }
            return rc.animationClips.FirstOrDefault(c => c.name.IndexOf(estado, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>El Animator no corre en el editor: se muestrea el clip del estado a la fase dada para que el personaje no quede en T-pose.</summary>
        private static bool PoseEnEditor(GameObject go, string estado, float fase)
        {
            Animator a = go.GetComponentInChildren<Animator>(true);
            if (a == null) { return false; }
            AnimationClip clip = ClipDeEstado(a, estado);
            if (clip == null) { return false; }
            clip.SampleAnimation(a.gameObject, Mathf.Repeat(fase, 1f) * clip.length);
            return true;
        }

        /// <summary>Muestrea todos los PoseQuieta de la escena abierta (la gente fija del paseo) para la foto.</summary>
        private static int MuestrearPoses()
        {
            int n = 0;
            foreach (PoseQuieta p in Object.FindObjectsByType<PoseQuieta>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (PoseEnEditor(p.gameObject, p.Estado, p.Fase)) { n++; }
            }
            return n;
        }

        /// <summary>Ancho de los brazos (en el eje x del personaje) entre su alto: una T-pose da cerca de 1 o más; una pose natural queda abajo de 0.75.</summary>
        private static float AnchoSobreAlto(GameObject go)
        {
            Bounds b = new Bounds();
            bool hay = false;
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                Mesh m = new Mesh();
                smr.BakeMesh(m, true);
                Matrix4x4 aMundo = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (Vector3 v in m.vertices)
                {
                    Vector3 p = go.transform.InverseTransformDirection(aMundo.MultiplyPoint3x4(v) - go.transform.position);
                    if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; }
                    else { b.Encapsulate(p); }
                }
                Object.DestroyImmediate(m);
            }
            return hay && b.size.y > 0.1f ? b.size.x / b.size.y : 0f;
        }

        private static void ValidarPoses()
        {
            List<GameObject> visuales = CargarVisuales();
            Exigir(visuales.Count > 0, "no hay Visual_NPxx para revisar las poses.");
            foreach (GameObject v in visuales)
            {
                GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(v);
                try
                {
                    float antes = AnchoSobreAlto(tmp);
                    bool idle = PoseEnEditor(tmp, "Idle", 0.3f);
                    float despues = AnchoSobreAlto(tmp);
                    bool camina = PoseEnEditor(tmp, "Caminar", 0.3f);
                    float caminando = AnchoSobreAlto(tmp);
                    Informe.AppendLine("Pose " + v.name + ": ancho/alto sin muestrear " + antes.ToString("F2") + ", Idle " + (idle ? despues.ToString("F2") : "SIN CLIP") + ", Caminar " + (camina ? caminando.ToString("F2") : "SIN CLIP"));
                    Exigir(idle && camina, v.name + ": no se encontró el clip Idle o Caminar para muestrear.");
                    Exigir(!idle || despues < 0.75f, v.name + ": con el clip Idle sigue con los brazos abiertos (ancho/alto " + despues.ToString("F2") + ").");
                    Exigir(!camina || caminando < 0.85f, v.name + ": con el clip Caminar sigue con los brazos abiertos (ancho/alto " + caminando.ToString("F2") + ").");
                }
                finally
                {
                    Object.DestroyImmediate(tmp);
                }
            }
        }
        // ================================================================ Faros existentes (K12): sin mancha blanca

        private static Material Suavizar(Material src, float factorEmision, float topeBase)
        {
            return KitS2c.Mat("T1463_Suave_" + src.name.Replace(" ", "_").Replace("(Instance)", ""), () =>
            {
                Material m = new Material(src);
                if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", m.GetColor("_EmissionColor") * factorEmision); }
                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (mx > topeBase) { c = new Color(c.r * topeBase / mx, c.g * topeBase / mx, c.b * topeBase / mx, c.a); }
                    m.SetColor("_BaseColor", c);
                }
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
        }

        /// <summary>
        /// El faro lejano de T-1413 flotaba 5 m sobre el mar y salía como mancha blanca en K12: se baja sobre sus rocas (base 0.3 m dentro de la cima), se achica a escala 0.7,
        /// se le baja la emisión y el blanco, su haz se acorta y se apaga el halo. También se baja la intensidad de los haces de T-1413 y de T-1404.
        /// </summary>
        private static void ArreglarFarosExistentes(Transform raiz)
        {
            Transform grupo = raiz.Find(ZonaMaleconT1413.NombreRaiz + "/FaroLejano");
            if (grupo == null) { Informe.AppendLine("Faro lejano de T-1413: no existe (nada que arreglar)."); }
            else
            {
                Transform faro = grupo.Find("ML04_Faro_Lejano");
                Transform haz = grupo.Find("Haz");
                float cima = float.NegativeInfinity;
                foreach (Transform t in grupo)
                {
                    if (!t.name.StartsWith("ML05_Roca", StringComparison.Ordinal)) { continue; }
                    Bounds rb = ZonaMaleconT1393.CajaDe(t.gameObject);
                    if (rb.size != Vector3.zero) { cima = Mathf.Max(cima, rb.max.y); }
                }
                if (faro != null && !float.IsNegativeInfinity(cima))
                {
                    Bounds antes = ZonaMaleconT1393.CajaDe(faro.gameObject);
                    faro.localScale = Vector3.one * 0.7f;
                    Bounds b = ZonaMaleconT1393.CajaDe(faro.gameObject);
                    faro.position += new Vector3(antes.center.x - b.center.x, (cima - 0.3f) - b.min.y, antes.center.z - b.center.z);
                    b = ZonaMaleconT1393.CajaDe(faro.gameObject);
                    foreach (Renderer r in faro.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] ms = r.sharedMaterials;
                        for (int i = 0; i < ms.Length; i++) { if (ms[i] != null) { ms[i] = Suavizar(ms[i], 0.3f, 0.6f); } }
                        r.sharedMaterials = ms;
                    }
                    if (haz != null)
                    {
                        haz.position = new Vector3(b.center.x, b.min.y + b.size.y * 0.88f, b.center.z);
                        haz.localScale = Vector3.one * 0.45f;
                    }
                    Informe.AppendLine("Faro lejano de T-1413: base en y " + b.min.y.ToString("F1") + " sobre la cima de las rocas (y " + cima.ToString("F1") + "), alto " + b.size.y.ToString("F1") + " m, emisión x0.3, haz a escala 0.45.");
                }
                else
                {
                    Informe.AppendLine("Faro lejano de T-1413: faltan el faro o sus rocas; sin cambios.");
                }
                Transform luz = grupo.Find("Luz");
                if (luz != null) { luz.gameObject.SetActive(false); }
            }
            int bajados = 0;
            foreach (string nombre in new[] { "T1413_HazFaroLejano", "T1404_HazFaro" })
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    Material m = r.sharedMaterial;
                    if (m != null && m.name.StartsWith(nombre, StringComparison.Ordinal) && m.HasProperty("_Intensidad") && m.GetFloat("_Intensidad") > 0.16f)
                    {
                        m.SetFloat("_Intensidad", 0.15f);
                        EditorUtility.SetDirty(m);
                        bajados++;
                    }
                }
            }
            Informe.AppendLine("Haces de faro con la intensidad bajada a 0.15: " + bajados + " renderers.");
        }
        // ================================================================ Faro de A11

        private static Mesh MallaCono(float largo, float radioInicio, float radioFin)
        {
            const int lados = 20;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            for (int i = 0; i <= lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                Vector3 r = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                v.Add(r * radioInicio);
                v.Add(r * radioFin + Vector3.forward * largo);
                n.Add(r);
                n.Add(r);
                uv.Add(new Vector2((float)i / lados, 0f));
                uv.Add(new Vector2((float)i / lados, 1f));
            }
            for (int i = 0; i < lados; i++)
            {
                int a = i * 2;
                t.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            Mesh m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Faro sobre un islote de rocas ML05_Escollera a la vista de A11 (arriba a la izquierda), con halo y un haz (malla emisiva, sin Light) que barre el mar hacia el este.</summary>
        private static void PonerFaroA11(KitS2c.Obra obra, Transform mio)
        {
            GameObject faro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Zonas/ML/ML04_Faro.prefab");
            GameObject roca = AssetDatabase.LoadAssetAtPath<GameObject>(CorrerZonasT1350.RutaModulo("ML05_Escollera"));
            if (faro == null || roca == null)
            {
                Informe.AppendLine("FALTA ML04_Faro o ML05_Escollera: sin faro de A11.");
                return;
            }
            Transform grupo = new GameObject("FaroA11").transform;
            grupo.SetParent(mio, false);
            const float escalaRoca = 1.2f;
            const float escalaFaro = 0.6f;
            Bounds rb = ZonaMaleconT1393.Medida(roca);
            float alto = Mathf.Max(0.5f, rb.size.y) * escalaRoca;
            float fondo = ColocarT1354Curva.YFondoPlaya - 0.4f;
            float meta = ColocarT1354Curva.YAgua + 1.2f;
            int capas = Mathf.Clamp(Mathf.CeilToInt((meta - fondo) / (alto * 0.65f)) + 1, 3, 9);
            System.Random azar = new System.Random(14635);
            float cima = fondo;
            int piezas = 0;
            for (int c = 0; c < capas; c++)
            {
                float radio = c == capas - 1 ? 0f : escalaRoca * 2.4f * (1f - (float)c / (capas - 1));
                int cuantas = c == capas - 1 ? 1 : Mathf.Max(3, Mathf.RoundToInt(radio * 1.4f));
                float y = fondo + alto * 0.65f * c;
                for (int k = 0; k < cuantas; k++)
                {
                    float ang = k * Mathf.PI * 2f / cuantas + c;
                    Vector3 p = new Vector3(CentroIslote.x + Mathf.Cos(ang) * radio, y, CentroIslote.y + Mathf.Sin(ang) * radio);
                    PonerSimple(roca, grupo, p, Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f), escalaRoca, new List<Bounds>(), 0.1f, "ML05_Roca_" + c + "_" + k);
                    piezas++;
                }
                cima = y + alto * 0.75f;
            }
            GameObject f = PonerSimple(faro, grupo, new Vector3(CentroIslote.x, cima, CentroIslote.y), Quaternion.identity, escalaFaro, new List<Bounds>(), 0.1f, NombreFaroA11);
            Bounds fb = ZonaMaleconT1393.CajaDe(f);
            Vector3 lampara = new Vector3(fb.center.x, fb.min.y + fb.size.y * 0.88f, fb.center.z);
            KitS2c.Halo(obra, grupo, lampara + Vector3.up * 1.5f, lampara.y - 6f, KitS2c.Sodio);
            Shader sh = Shader.Find("MechaCyberpunk/Mundo/HaloLuz");
            if (sh == null) { Informe.AppendLine("FALTA el shader HaloLuz: sin haz."); return; }
            Material mat = KitS2c.Mat("T1463_HazFaro", () =>
            {
                Material m = new Material(sh);
                m.SetColor("_Color", KitS2c.Calido);
                m.SetFloat("_Intensidad", 0.5f);
                m.SetFloat("_Borde", 1.2f);
                m.SetFloat("_Caida", 1.3f);
                m.SetFloat("_Suave", 0.3f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                return m;
            });
            Mesh malla = MallaCono(70f, 0.6f, 6f);
            malla.name = "T1463_HazFaro";
            obra.Mallas.Add(malla);
            GameObject haz = new GameObject("Haz");
            haz.transform.SetParent(grupo, false);
            haz.transform.position = lampara;
            haz.transform.rotation = Quaternion.LookRotation(new Vector3(1f, -0.01f, -0.45f));
            haz.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = haz.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Informe.AppendLine("Faro de A11: islote de " + piezas + " rocas en " + CentroIslote + ", lámpara en y " + lampara.y.ToString("F1") + ", haz de malla sin Light hacia el este.");
        }

        // ================================================================ Letreros de neón

        private static List<(Transform t, Bounds b)> Edificios(Transform raiz)
        {
            List<(Transform, Bounds)> lista = new List<(Transform, Bounds)>();
            foreach (string ruta in new[] { ZonaMaleconT1393.Raiz1384 + "/ML05", ZonaMaleconT1393.NombreRaiz + "/ML05", ZonaMaleconT1400.NombreRaiz + "/ML05", ZonaMaleconT1404.NombreRaiz + "/ML05" })
            {
                Transform g = raiz.Find(ruta);
                if (g == null) { continue; }
                foreach (Transform h in g)
                {
                    if (h.gameObject.activeInHierarchy && h.GetComponentInChildren<Renderer>() != null) { lista.Add((h, ZonaMaleconT1393.CajaDe(h.gameObject))); }
                }
            }
            Transform fachadas = raiz.Find("Fachadas");
            if (fachadas != null)
            {
                foreach (Transform h in fachadas)
                {
                    if (h.name.StartsWith("Casa_", StringComparison.Ordinal) && h.gameObject.activeInHierarchy && h.GetComponentInChildren<Renderer>() != null) { lista.Add((h, ZonaMaleconT1393.CajaDe(h.gameObject))); }
                }
            }
            return lista;
        }

        /// <summary>Solo los del frente (su cara al lado de la ciudad queda a unos 11 m del eje).</summary>
        private static bool EsDelFrente(Bounds b, out float frente)
        {
            Marco(b.center.x, out Vector3 linea, out _, out Vector3 mar);
            float ext = Mathf.Abs(mar.x) * b.extents.x + Mathf.Abs(mar.z) * b.extents.z;
            frente = Vector3.Dot(b.center - linea, mar) + ext;
            return Mathf.Abs(frente - ColocarT1354Curva.LateralCiudad) <= 3f;
        }

        private static List<Bounds> Neones(Transform raiz)
        {
            List<Bounds> lista = new List<Bounds>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false))
            {
                bool esNeon = t.name == "Neon" || t.name.StartsWith("NE01", StringComparison.Ordinal) || t.name.StartsWith("NE02", StringComparison.Ordinal) || t.name.StartsWith("Letrero_T1463", StringComparison.Ordinal);
                if (!esNeon || t.GetComponentInChildren<Renderer>() == null) { continue; }
                // Un "Neon" con hijo "Letrero" o un NE0x suelto: se cuenta una vez (el más alto de la jerarquía de neones).
                if (t.parent != null && (t.parent.name == "Neon" || t.parent.name.StartsWith("NE0", StringComparison.Ordinal))) { continue; }
                lista.Add(ZonaMaleconT1393.CajaDe(t.gameObject));
            }
            return lista;
        }

        private static bool TieneNeon(Bounds edificio, List<Bounds> neones)
        {
            Bounds g = edificio;
            g.Expand(new Vector3(3f, 0f, 3f));
            return neones.Any(n => n.center.x >= g.min.x && n.center.x <= g.max.x && n.center.z >= g.min.z && n.center.z <= g.max.z && n.center.y > 1f && n.center.y < 14f);
        }

        private static void PonerLetreros(Transform raiz, Transform mio)
        {
            Transform grupo = new GameObject("Letreros").transform;
            grupo.SetParent(mio, false);
            GameObject[] prefabs = { KitS2c.Prefab(KitS2c.NeonMagenta), KitS2c.Prefab(KitS2c.NeonTurquesa), KitS2c.Prefab(KitS2c.NeonPared) };
            Color[] colores = { KitS2c.Magenta, KitS2c.Turquesa, KitS2c.Sodio };
            List<Bounds> neones = Neones(raiz);
            System.Random azar = new System.Random(14631);
            int enCuadro = 0, ya = 0, nuevos = 0, k = 0;
            foreach ((Transform t, Bounds b) in Edificios(raiz).OrderBy(e => e.b.center.x))
            {
                if (!EsDelFrente(b, out float frente) || !(EnK11(b) || EnA11(b))) { continue; }
                enCuadro++;
                if (TieneNeon(b, neones)) { ya++; continue; }
                int i = k++ % 3;
                if (prefabs[i] == null) { continue; }
                Marco(b.center.x, out Vector3 linea, out Vector3 tang, out Vector3 mar);
                Bounds mb = ZonaMaleconT1393.Medida(prefabs[i]);
                float escala = Mathf.Clamp(1.6f / Mathf.Max(0.3f, mb.size.y), 0.6f, 2f);
                float alto = (i == 2 ? 2.7f : 3.6f) + (float)azar.NextDouble() * 1.0f;
                Vector3 punto = linea + mar * (frente + 0.02f) + tang * (b.extents.x * 0.35f) + Vector3.up * alto;
                GameObject go = ZonaMaleconT1393.PonerAnclado(prefabs[i], grupo, punto, mar, escala, "Letrero_T1463_" + nuevos, false);
                if (go == null) { continue; }
                ZonaMaleconT1393.Brillar(go.transform, colores[i]);
                neones.Add(ZonaMaleconT1393.CajaDe(go));
                nuevos++;
            }
            Informe.AppendLine("Letreros: " + enCuadro + " edificios del frente en cuadro de K11 o A11, " + ya + " ya tenían, " + nuevos + " nuevos (emisivos, sin Light).");
        }

        // ================================================================ Gente y basura

        private static GameObject PonerPersona(GameObject visual, Transform padre, Vector3 pos, Vector3 haciaDonde, string nombre, System.Random azar, bool congelada)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(visual, padre);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(haciaDonde);
            PoseQuieta pose = go.AddComponent<PoseQuieta>();
            pose.Definir("Idle", (float)azar.NextDouble(), congelada);
            return go;
        }

        private static void PonerGente(Transform raiz, Transform mio, KitS2c.Obra obra)
        {
            List<GameObject> visuales = CargarVisuales();
            if (visuales.Count == 0)
            {
                Informe.AppendLine("FALTA: ningún Visual_NPxx en " + CarpetaVisuales + ". Sin gente de decorado.");
                return;
            }
            Transform grupo = new GameObject("Gente").transform;
            grupo.SetParent(mio, false);
            System.Random azar = new System.Random(14632);
            Vector3 cam = CamaraK11;
            int n = 0, enK11 = 0;
            List<(float x, float lateral)> sitios = new List<(float, float)>();
            // Gente de K11: junto al barandal, cerca de la cámara, y unos en la banqueta de enfrente.
            int ip = 0; foreach (float dx in new[] { 3f, 6f, 10f, 13f, 17f, 21f, 26f, 31f, 37f, 44f, 52f, 61f, 72f, 85f })
            {
                sitios.Add((cam.x - dx, ip++ % 2 == 0 ? LateralBarandalGente - (float)azar.NextDouble() * 0.3f : 13.3f + (float)azar.NextDouble() * 0.3f));
            }
            int ib = 0;
            foreach (float dx in new[] { 6f, 10f, 14f, 18f, 23f, 28f, 34f, 41f, 49f, 58f, 68f, 80f, 95f, 110f })
            {
                sitios.Add((cam.x - dx, LateralBanquetaGente - (ib++ % 2) * 1.3f));
            }
            for (float x = XFilaDesde + 6f; x <= 100f; x += PasoGente)
            {
                sitios.Add((x + (float)azar.NextDouble() * 6f, LateralBarandalGente - (float)azar.NextDouble() * 0.4f));
                sitios.Add((x + 8f + (float)azar.NextDouble() * 6f, LateralBanquetaGente));
            }
            foreach ((float x, float lateral) in sitios)
            {
                Marco(x, out Vector3 linea, out Vector3 tang, out Vector3 mar);
                Vector3 pos = linea + mar * lateral;
                pos.y = ColocarT1354Curva.YBanqueta;
                if (Plano(pos, cam) < 1.5f || Mathf.Abs(Mathf.Abs(x) - 47f) < 3f) { continue; }
                bool alBarandal = lateral > 14f;
                Vector3 mira = alBarandal ? mar : (azar.NextDouble() < 0.5 ? tang : -tang);
                GameObject v = visuales[n % visuales.Count];
                PonerPersona(v, grupo, pos, mira, (alBarandal ? "Barandal_" : "Banqueta_") + n + "_" + v.name, azar, false);
                if (EnK11(new Bounds(pos + Vector3.up, Vector3.one))) { enK11++; }
                n++;
            }
            Informe.AppendLine("Gente de decorado: " + n + " personas NP07..NP14 en Idle (PoseQuieta), " + enK11 + " en el cuadro de K11. Recargados en el barandal: sin clip propio (Idle mirando al mar).");
        }

        private static void PonerBasura(Transform raiz, Transform mio)
        {
            Transform grupo = new GameObject("Basura").transform;
            grupo.SetParent(mio, false);
            List<Bounds> ocupado = OcupadoDePie(raiz);
            string[] codigos = { "BS01a_Basura", "BS01b_Basura", "BS01c_Basura", "C17_Huacales", "BS01d_Basura", "C16_Tambo", "BS01e_Basura", "BT01_BoteBasura" };
            List<GameObject> prefabs = codigos.Select(Cargar).ToList();
            System.Random azar = new System.Random(14633);
            int n = 0;
            int k = 0;
            for (float x = XFilaDesde + 10f; x <= 100f; x += 18f + (float)azar.NextDouble() * 8f, k++)
            {
                GameObject p = prefabs[k % prefabs.Count];
                if (p == null || Mathf.Abs(Mathf.Abs(x) - 47f) < 4f) { continue; }
                Marco(x, out Vector3 linea, out _, out Vector3 mar);
                Vector3 suelo = linea + mar * (LateralBarandalGente + 0.5f);
                suelo.y = ColocarT1354Curva.YBanqueta;
                if (Plano(suelo, CamaraK11) < 2f) { continue; }
                Bounds pm = ZonaMaleconT1393.Medida(p);
                if (PonerSimple(p, grupo, suelo, Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f), 1f, ocupado, Mathf.Clamp(Mathf.Max(pm.size.x, pm.size.z), 0.4f, 1.5f), p.name + "_" + n) != null) { n++; }
            }
            Informe.AppendLine("Basura y cajas en el paseo: " + n + " (BS01, huacales, tambos, botes). Bancas: no hay modelo de banca en el proyecto (anotado en assets que faltan).");
        }

        private static void InformarCosta(Transform raiz)
        {
            int escaleras = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal) && (t.parent == null || !t.parent.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal)));
            int rocas = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("ML05_Escollera", StringComparison.Ordinal));
            Informe.AppendLine("Costa existente (T-1354/T-1363): " + escaleras + " escaleras a la playa y " + rocas + " rocas de escollera ML05. No se tocan.");
        }

        // ================================================================ Ayudante de foto

        /// <summary>
        /// Para las fotos Vista19b: pone en la escena abierta (no en el prefab) carros con faros y calaveras encendidos en los dos carriles y gente caminando en el paseo y la banqueta,
        /// alrededor de lo que ve la cámara. Todo cuelga de "T1463_Foto"; QuitarFoto lo borra. Llamar con la escena abierta (ColocarS2.Abrir) y antes de ColocarS2cLook.Foto.
        /// </summary>
        public static void PoblarFoto(Camera camara)
        {
            QuitarFoto();
            int muestreados = MuestrearPoses();
            GameObject raiz = new GameObject(NombreFoto);
            Vector3 cam = camara.transform.position;
            Vector3 fwd = camara.transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            bool aerea = cam.y > 15f;
            Vector3 centro = aerea ? cam + fwd * (Mathf.Max(20f, cam.y * 1.5f)) : cam;
            float[] carros = aerea ? new[] { -70f, -35f, 0f, 35f, 70f, 105f } : new[] { 12f, 26f, 44f, 66f, 92f };
            float[] gente = aerea ? new[] { -60f, -20f, 20f, 60f, 100f } : new[] { 6f, 10f, 15f, 21f, 29f, 38f, 50f, 64f, 80f };
            System.Random azar = new System.Random(14634);
            List<GameObject> modelos = Enumerable.Range(2, 7).Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCarros + "V0" + n + ".prefab")).Where(c => c != null).ToList();
            int nCarros = 0, nGente = 0;
            for (int i = 0; i < carros.Length && modelos.Count > 0; i++)
            {
                Vector3 p = centro + fwd * carros[i];
                Marco(p.x, out Vector3 linea, out Vector3 tang, out _);
                bool haciaEste = i % 2 == 0; // carril del mar (+2 m) va al este; el de la ciudad (-2 m) al oeste
                Vector3 mar = ColocarT1354Curva.HaciaElMar(tang);
                Vector3 suelo = linea + mar * (haciaEste ? 2f : -2f);
                suelo.y = ColocarT1354Curva.YArroyo;
                GameObject prefab = modelos[i % modelos.Count];
                Bounds mb = ZonaMaleconT1393.Medida(prefab);
                Quaternion giro = Quaternion.LookRotation(haciaEste ? tang : -tang);
                if (mb.size.x > mb.size.z) { giro *= Quaternion.Euler(0f, -90f, 0f); }
                Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z);
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                go.name = "CarroFoto_" + prefab.name + "_" + i;
                go.transform.rotation = giro;
                go.transform.position = suelo - giro * ancla;
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; }
                foreach (MonoBehaviour m in go.GetComponentsInChildren<MonoBehaviour>(true)) { m.enabled = false; }
                foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; }
                foreach (Light l in go.GetComponentsInChildren<Light>(true))
                {
                    l.gameObject.SetActive(true);
                    l.enabled = true;
                }
                nCarros++;
            }
            List<GameObject> visuales = CargarVisuales();
            for (int i = 0; i < gente.Length && visuales.Count > 0; i++)
            {
                Vector3 p = centro + fwd * gente[i];
                Marco(p.x, out Vector3 linea, out Vector3 tang, out Vector3 mar);
                foreach (float lateral in new[] { LateralBarandalGente - 1.2f, LateralBanquetaGente })
                {
                    Vector3 pos = linea + mar * (lateral + (float)azar.NextDouble() * 0.8f);
                    pos.y = ColocarT1354Curva.YBanqueta;
                    GameObject v = visuales[(i * 2 + (lateral > 0f ? 0 : 1)) % visuales.Count];
                    Vector3 rumbo = ((i + (lateral > 0f ? 1 : 0)) % 2 == 0) ? tang : -tang;
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(v, raiz.transform);
                    go.name = "PersonaFoto_" + v.name + "_" + nGente;
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.LookRotation(rumbo);
                    PoseQuieta pose = go.AddComponent<PoseQuieta>();
                    pose.Definir("Caminar", (float)azar.NextDouble(), true);
                    pose.Aplicar();
                    PoseEnEditor(go, "Caminar", pose.Fase);
                    nGente++;
                }
            }
            Debug.Log("[Arte] T-1463 PoblarFoto: " + muestreados + " personas fijas muestreadas en Idle; " + nCarros + " carros con luces y " + nGente + " personas caminando (congeladas a media zancada) junto a " + centro.ToString("F0") + ".");
        }

        public static void QuitarFoto()
        {
            GameObject vieja = GameObject.Find(NombreFoto);
            if (vieja != null) { Object.DestroyImmediate(vieja); }
        }

        // ================================================================ Validar

        private static int fallos;

        private static void Exigir(bool ok, string mensaje)
        {
            if (ok) { return; }
            fallos++;
            Informe.AppendLine("FALLO: " + mensaje);
        }

        [MenuItem("Mecha Cyberpunk/T-1463/Malecon (validar)")]
        public static void Validar()
        {
            Informe.Clear();
            fallos = 0;
            Informe.AppendLine("T-1463 validar Malecón");
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mio = raiz.Find(NombreRaiz);
                Exigir(mio != null, "no existe " + NombreRaiz + "; corre Aplicar.");
                if (mio != null)
                {
                    ValidarMarcas(mio);
                    ValidarFila(raiz, mio);
                    ValidarLetreros(raiz);
                    ValidarVida(raiz, mio);
                    ValidarLuces(mio);
                    ValidarProyecciones(raiz, mio);
                    ValidarJuegoEnPrefab(raiz);
                    ValidarPoses();
                    ValidarArena(raiz);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Informe.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Escribir("t1463-validar");
            if (fallos > 0) { Debug.LogError("[Arte] T-1463 Validar: " + fallos + " fallos (ver Logs/t1463-validar.txt)"); }
        }

        private static void ValidarArena(Transform raiz)
        {
            Transform mio = raiz.Find(NombreRaiz);
            Renderer[] playa = raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Playa" && !t.IsChildOf(mio)).SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).ToArray();
            int conArena = playa.Count(r => r.sharedMaterial != null && r.sharedMaterial.name == "T1463_Arena");
            Informe.AppendLine("Playa: " + playa.Length + " renderers, " + conArena + " con T1463_Arena.");
            Exigir(playa.Length > 0 && conArena == playa.Length, "la playa no tiene el material T1463_Arena.");
        }

        private static void ValidarMarcas(Transform mio)
        {
            foreach (string n in new[] { "Vista19b_K11", "Vista19b_A11" })
            {
                Transform m = mio.Find(n);
                Exigir(m != null && m.Find("Mira") != null && m.Cast<Transform>().Any(h => h.name.StartsWith("Campo_", StringComparison.Ordinal)), "marca " + n + " incompleta (falta Mira o Campo_).");
            }
        }

        private static List<Transform> Hijos(Transform g, string prefijo)
        {
            return g == null ? new List<Transform>() : g.Cast<Transform>().Where(t => t.name.StartsWith(prefijo, StringComparison.Ordinal)).ToList();
        }

        private static void ValidarFila(Transform raiz, Transform mio)
        {
            Transform fila = mio.Find("Fila");
            List<Transform> palmeras = Hijos(fila, "AR03_Palmera");
            List<Transform> faroles = Hijos(fila, "ML01_Farol");
            Informe.AppendLine("Fila: " + palmeras.Count + " palmeras y " + faroles.Count + " faroles.");
            Exigir(palmeras.Count >= 10 && faroles.Count >= 10, "la fila tiene menos de 10 palmeras o 10 faroles.");
            // Espaciado medido entre faroles consecutivos que se ven juntos en K11.
            List<Vector3> pf = faroles.Select(t => ZonaMaleconT1393.CajaDe(t.gameObject).center).OrderBy(p => p.x).ToList();
            List<float> pasos = new List<float>();
            for (int i = 0; i + 1 < pf.Count; i++)
            {
                if (EnCuadro(CamaraK11, MiraK11, CampoK11, pf[i]) && EnCuadro(CamaraK11, MiraK11, CampoK11, pf[i + 1])) { pasos.Add(Plano(pf[i], pf[i + 1])); }
            }
            Informe.AppendLine("Espaciado entre faroles consecutivos en cuadro de K11: " + pasos.Count + " pares, mínimo " + (pasos.Count > 0 ? pasos.Min().ToString("F1") : "-")
                + " m, máximo " + (pasos.Count > 0 ? pasos.Max().ToString("F1") : "-") + " m (pedido " + PasoFila + ").");
            Exigir(pasos.Count >= 3, "menos de 3 pares de faroles consecutivos en cuadro de K11.");
            Exigir(pasos.Count == 0 || (pasos.Min() >= PasoFila - 1.5f && pasos.Max() <= PasoFila + 2.5f), "espaciado de faroles fuera de " + (PasoFila - 1.5f) + " a " + (PasoFila + 2.5f) + " m.");
            // Duplicados viejos.
            int viejos = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false))
            {
                if (!EsFilaVieja(t) || t.IsChildOf(mio) || (t.parent != null && EsFilaVieja(t.parent))) { continue; }
                Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                Vector3 c = b.size == Vector3.zero ? t.position : b.center;
                float lateral = LateralDe(c);
                if (c.x >= XFilaDesde - 30f && c.x <= XFilaHasta && lateral >= 5f && lateral <= 18f) { viejos++; Informe.AppendLine("  vieja activa: " + t.name + " en " + c.ToString("F1")); }
            }
            Exigir(viejos == 0, viejos + " palmeras o faroles viejos del paseo siguen activos (duplicados).");
            // Costa.
            int escaleras = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal) && (t.parent == null || !t.parent.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal)));
            int rocas = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("ML05_Escollera", StringComparison.Ordinal));
            int puestos = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("ML03_PuestoMariscos", StringComparison.Ordinal));
            Informe.AppendLine("Costa: " + escaleras + " escaleras, " + rocas + " rocas de escollera, " + puestos + " puestos ML03.");
            Exigir(escaleras >= 2, "menos de 2 escaleras a la playa.");
            Exigir(rocas >= 6, "menos de 6 rocas de escollera.");
            Exigir(puestos >= 1, "no hay puesto ML03.");
            Transform faro = raiz.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == NombreFaroA11);
            Renderer haz = raiz.GetComponentsInChildren<Renderer>(false).FirstOrDefault(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("T1463_HazFaro", StringComparison.Ordinal));
            Informe.AppendLine("Faro lejano: " + (faro != null ? "sí" : "NO") + ", haz sin Light: " + (haz != null ? "sí (" + haz.name + ")" : "NO"));
            Exigir(faro != null && haz != null, "falta el faro lejano o su haz (T-1413).");
        }

        private static void ValidarLetreros(Transform raiz)
        {
            List<Bounds> neones = Neones(raiz);
            int enCuadro = 0, sin = 0;
            foreach ((Transform t, Bounds b) in Edificios(raiz))
            {
                if (!EsDelFrente(b, out _) || !(EnK11(b) || EnA11(b))) { continue; }
                enCuadro++;
                if (!TieneNeon(b, neones))
                {
                    sin++;
                    Informe.AppendLine("  sin letrero: " + t.name + " en " + b.center.ToString("F0"));
                }
            }
            Informe.AppendLine("Fachadas del frente en cuadro: " + enCuadro + ", sin letrero: " + sin + " (neones contados en el prefab: " + neones.Count + ").");
            Exigir(enCuadro >= 3, "menos de 3 fachadas del frente en cuadro.");
            Exigir(sin == 0, sin + " fachadas en cuadro sin letrero.");
        }

        private static void ValidarVida(Transform raiz, Transform mio)
        {
            PoseQuieta[] gente = mio.GetComponentsInChildren<PoseQuieta>(true);
            int enK11 = gente.Count(g => EnCuadro(CamaraK11, MiraK11, CampoK11, g.transform.position + Vector3.up));
            Informe.AppendLine("Gente: " + gente.Length + " personas, " + enK11 + " en el cuadro de K11.");
            Exigir(gente.Length >= 20, "menos de 20 personas de decorado.");
            Exigir(enK11 >= 6, "menos de 6 personas en el cuadro de K11.");
            int estacionados = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("Estacionado_", StringComparison.Ordinal) || (t.parent != null && t.parent.name == "Carros" && t.name.StartsWith("Carro_", StringComparison.Ordinal)));
            Informe.AppendLine("Carros estacionados (T-1384 y T-1400): " + estacionados + ". Carros en carriles: solo en la foto, con PoblarFoto.");
            Exigir(estacionados >= 3, "menos de 3 carros estacionados.");
            int basura = Hijos(mio.Find("Basura"), "").Count;
            Informe.AppendLine("Basura, cajas y botes en el paseo: " + basura + ".");
            Exigir(basura >= 5, "menos de 5 objetos de basura o cajas.");
        }

        private static void ValidarLuces(Transform mio)
        {
            Light[] luces = mio.GetComponentsInChildren<Light>(true);
            Informe.AppendLine("Luces nuevas: " + luces.Length + " (cupo " + CupoLuces + ").");
            Exigir(luces.Length <= CupoLuces, "pasa el cupo de luces: " + luces.Length);
            foreach (Light l in luces)
            {
                Exigir(l.shadows == LightShadows.None, "luz con sombras: " + l.name);
            }
        }

        private static void ValidarProyecciones(Transform raiz, Transform mio)
        {
            Vector3 cam = CamaraK11, mira = MiraK11;
            // K11: fila a la izquierda del centro, fachadas a la derecha, curva al centro-derecha al fondo.
            List<Transform> fila = Hijos(mio.Find("Fila"), "AR03_Palmera").Concat(Hijos(mio.Find("Fila"), "ML01_Farol")).ToList();
            List<float> sxFila = new List<float>();
            foreach (Transform t in fila)
            {
                Vector3 c = ZonaMaleconT1393.CajaDe(t.gameObject).center;
                float d = Plano(c, cam);
                if (d >= 6f && d <= 80f && Proyectar(cam, mira, CampoK11, c, out float sx, out float sy) && Mathf.Abs(sx) <= 1f && Mathf.Abs(sy) <= 1f) { sxFila.Add(sx); }
            }
            int aLaIzq = sxFila.Count(s => s < 0.2f);
            Informe.AppendLine("K11 fila (6 a 80 m): " + sxFila.Count + " en cuadro, " + aLaIzq + " con pantalla x < 0.2; x entre " + (sxFila.Count > 0 ? sxFila.Min().ToString("F2") + " y " + sxFila.Max().ToString("F2") : "-") + ".");
            Exigir(sxFila.Count >= 6 && aLaIzq >= sxFila.Count * 0.7f, "K11: la fila de palmeras y faroles no cae a la izquierda del centro.");
            List<float> sxFach = new List<float>();
            foreach ((Transform t, Bounds b) in Edificios(raiz))
            {
                if (EsDelFrente(b, out _) && Plano(b.center, cam) <= 70f && Proyectar(cam, mira, CampoK11, b.center, out float sx, out float sy) && Mathf.Abs(sx) <= 1.2f) { sxFach.Add(sx); }
            }
            int aLaDer = sxFach.Count(s => s > 0.15f);
            Informe.AppendLine("K11 fachadas del frente a menos de 70 m: " + sxFach.Count + ", " + aLaDer + " con pantalla x > 0.15.");
            Exigir(sxFach.Count >= 2 && aLaDer >= sxFach.Count * 0.8f, "K11: las fachadas no caen a la derecha.");
            Vector3 lejos = new Vector3(-250f, 0f, ColocarT1354Curva.ZCentro(-250f));
            bool ok = Proyectar(cam, mira, CampoK11, lejos, out float cx, out float cy);
            Informe.AppendLine("K11 curva: el eje de la avenida en x = -250 cae en pantalla x " + cx.ToString("F2") + ", y " + cy.ToString("F2") + " (esperado x entre 0.05 y 0.95, y entre -0.3 y 0.2).");
            Exigir(ok && cx >= 0.05f && cx <= 0.95f && cy >= -0.3f && cy <= 0.2f, "K11: la curva no cae al centro-derecha al fondo.");
            Vector3 vecino = PuntoLateral(-190f, 0f, 0f);
            Proyectar(cam, mira, CampoK11, vecino, out float vx, out _);
            Informe.AppendLine("K11 eje en x = -190 (el tramo medio): pantalla x " + vx.ToString("F2") + ". Barandal (lateral 15.6) a 12 m adelante: ver foto.");

            // A11: faro arriba a la izquierda, playa abajo a la izquierda, fachadas a la derecha.
            Transform faro = raiz.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == NombreFaroA11);
            if (faro != null)
            {
                Bounds fb = ZonaMaleconT1393.CajaDe(faro.gameObject);
                Vector3 lampara = new Vector3(fb.center.x, fb.max.y, fb.center.z);
                bool vf = Proyectar(CamaraA11, MiraA11, CampoA11, lampara, out float fx, out float fy);
                Informe.AppendLine("A11 faro: pantalla x " + fx.ToString("F2") + ", y " + fy.ToString("F2") + " (esperado x < -0.1 e y > 0).");
                Exigir(vf && fx < -0.1f && fx > -1f && fy > 0f && fy < 1f, "A11: el faro no cae arriba a la izquierda.");
            }
            Vector3 playa = PuntoLateral(-150f, 26f, -3f);
            bool vp = Proyectar(CamaraA11, MiraA11, CampoA11, playa, out float px, out float py);
            Informe.AppendLine("A11 playa (x -150, lateral 26): pantalla x " + px.ToString("F2") + ", y " + py.ToString("F2") + " (esperado y < -0.1).");
            Exigir(vp && py < -0.1f && Mathf.Abs(px) <= 1f, "A11: la playa no cae abajo.");
            List<float> sxA = new List<float>();
            foreach ((Transform t, Bounds b) in Edificios(raiz))
            {
                if (EsDelFrente(b, out _) && EnA11(b) && Proyectar(CamaraA11, MiraA11, CampoA11, b.center, out float sx, out _)) { sxA.Add(sx); }
            }
            int aLaDerA = sxA.Count(s => s > 0f);
            int escalerasA11 = raiz.GetComponentsInChildren<Transform>(false).Count(t => t.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal) && (t.parent == null || !t.parent.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal)) && EnCuadro(CamaraA11, MiraA11, CampoA11, ZonaMaleconT1393.CajaDe(t.gameObject).center)); Informe.AppendLine("A11 escaleras a la playa en cuadro: " + escalerasA11 + " (pedido 2 o más)."); Exigir(escalerasA11 >= 2, "A11: menos de 2 escaleras a la playa en cuadro."); Informe.AppendLine("A11 fachadas del frente en cuadro: " + sxA.Count + ", " + aLaDerA + " a la derecha del centro.");
            Exigir(sxA.Count >= 3 && aLaDerA >= sxA.Count * 0.6f, "A11: las fachadas no caen a la derecha.");
            foreach (float x in new[] { -110f, -170f, -230f, -290f })
            {
                Proyectar(CamaraA11, MiraA11, CampoA11, new Vector3(x, 0f, ColocarT1354Curva.ZCentro(x)), out float ex, out float ey);
                Informe.AppendLine("A11 eje de la avenida en x = " + x + ": pantalla x " + ex.ToString("F2") + ", y " + ey.ToString("F2") + ".");
            }
        }

        private static void ValidarJuegoEnPrefab(Transform raiz)
        {
            CalleZona[] calles = raiz.GetComponentsInChildren<CalleZona>(true);
            int conBanqueta = calles.Count(c => c.Banquetas && !c.Autopista && c.Cantidad >= 2);
            Informe.AppendLine("Juego (peatones): CalleZona en el prefab: " + calles.Length + ", con banquetas y sin autopista: " + conBanqueta + " (GrupoPeatones saca un peatón por ruta de banqueta, hasta 16 en todas las zonas; el visual sale de ConfigPeatonesZona_Base, zona Malecon, con 10 NP).");
            Exigir(conBanqueta >= 1, "ninguna CalleZona con banquetas en el Malecón: el juego no saca peatones por zona aquí.");
        }

        /// <summary>Revisa en la escena que el tráfico y los peatones de zona sí tienen al Malecón (ZonaTrafico llamada Malecon y CalleZona con banquetas).</summary>
        [MenuItem("Mecha Cyberpunk/T-1463/Malecon (validar juego en la escena)")]
        public static void ValidarJuego()
        {
            Informe.Clear();
            fallos = 0;
            Informe.AppendLine("T-1463 validar juego (tráfico y peatones del Malecón en la escena)");
            ColocarS2.Abrir();
            MechaCyberpunk.Trafico.Calle.ZonaTrafico[] zonas = Object.FindObjectsByType<MechaCyberpunk.Trafico.Calle.ZonaTrafico>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            MechaCyberpunk.Trafico.Calle.ZonaTrafico malecon = zonas.FirstOrDefault(z => z.Nombre == "Malecon");
            Informe.AppendLine("ZonaTrafico Malecon: " + (malecon != null ? "sí, en " + malecon.transform.position.ToString("F0") + ", tamaño " + malecon.Tamano.ToString("F0") : "NO") + " (zonas en escena: " + zonas.Length + ").");
            Exigir(malecon != null, "no hay ZonaTrafico llamada Malecon en la escena: el tráfico por zona no sacaría carros del Malecón.");
            if (malecon != null)
            {
                Exigir(malecon.Contiene(PuntoLateral(-135f, 0f, 0f)) && malecon.Contiene(PuntoLateral(100f, 0f, 0f)), "la ZonaTrafico Malecon no cubre la avenida de x = -135 a x = 100.");
            }
            CalleZona[] calles = Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int enZona = calles.Count(c => c.Banquetas && !c.Autopista && c.Cantidad >= 2 && c.Cantidad > 0 && c.Punto(0).z < -195f);
            Informe.AppendLine("CalleZona con banquetas al sur de z = -195 (Malecón): " + enZona + " de " + calles.Length + ".");
            Exigir(enZona >= 1, "ninguna CalleZona con banquetas en el Malecón en la escena.");
            Escribir("t1463-validar-juego");
        }

        // ================================================================ Salida

        private static void Escribir(string nombre)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/" + nombre + ".txt", Informe.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] " + nombre + ":\n" + Informe);
        }
    }
}
