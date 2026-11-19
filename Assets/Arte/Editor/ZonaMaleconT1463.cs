using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1463: el Malecón igual a K11 y A11, con vida. Corre DESPUÉS de T1384, T1393, T1400, T1404 y T1413 (mismo prefab T1354_Malecon; todo lo nuevo cuelga de
    /// "T1463_Malecon", que se borra y rehace y vuelve a encender lo que apagó).
    /// 1) Marcas de cámara Vista19b_K11 y Vista19b_A11 (hijos Mira y Campo_&lt;grados&gt;). K11 va en la banqueta del mar a x = -135, donde la avenida ya curva a la derecha.
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
        private const float LateralFila = 12.4f;
        private const float XFilaDesde = -300f;
        private const float XFilaHasta = 105f;
        private const float RadioCamara = 2.2f;
        private const int TopeSpots = 28;
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
        private static readonly Vector3 CamaraA11 = new Vector3(150f, 40f, -300f);
        private static readonly Vector3 MiraA11 = new Vector3(90f, 0f, -275f);
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

        /// <summary>K11: en el paseo, a 9.6 m del eje de la avenida (la fila de palmeras queda a la izquierda y el barandal más allá), a 1.9 m de alto, mirando al oeste donde la avenida ya curva a la derecha.</summary>
        public static Vector3 CamaraK11 => PuntoLateral(-135f, 9.6f, AlturaOjos);

        public static Vector3 MiraK11 => PuntoLateral(-215f, 3f, 0.6f);

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
            List<Bounds> ocupado = OcupadoPaseo(raiz);
            List<Vector3> borde = ColocarT1354Curva.BordeMar(-330f, 175f);
            float largo = ColocarT1354Curva.Longitud(borde);
            // Un farol queda 6 m delante de la cámara de K11 (como en la referencia); los demás cada 15 m. Las palmeras van a medio camino entre dos faroles.
            float dRef = DistanciaEnX(borde, CamaraK11.x - 6f);
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
                    if (Plano(pos, camK) < RadioCamara) { omitidos++; continue; }
                    GameObject go = esPalmera
                        ? ZonaMaleconT1393.Colocar(palmera, grupo, pos, Quaternion.Euler(0f, (float)obra.Azar.NextDouble() * 360f, 0f), 1.1f + (float)obra.Azar.NextDouble() * 0.3f, ocupado, "AR03_Palmera_" + nP)
                        : ZonaMaleconT1393.Colocar(farol, grupo, pos, Quaternion.LookRotation(mar), 1f, ocupado, "ML01_Farol_" + nF);
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
                KitS2c.Luz(f.go.transform, f.cabeza, LightType.Spot, KitS2c.Sodio, ColocarS2oLuz.IntensidadPoste * 1.6f, ColocarS2oLuz.RangoPoste * 1.25f,
                    ColocarS2oLuz.AnguloPoste, (f.haciaAvenida * 0.25f + Vector3.down).normalized);
                spots++;
            }
            Informe.AppendLine("Fila nueva: " + nP + " palmeras y " + nF + " faroles cada " + PasoFila + " m a " + LateralFila + " m del eje (" + spots + " con spot de sodio sin sombras, el resto solo halo); "
                + omitidos + " omitidos por choque o por la cámara.");
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
            foreach (float dx in new[] { 4f, 8f, 12f, 16f, 22f, 30f, 40f, 55f })
            {
                sitios.Add((cam.x - dx, LateralBarandalGente - (float)azar.NextDouble() * 0.5f));
            }
            foreach (float dx in new[] { 10f, 24f, 38f, 58f, 80f })
            {
                sitios.Add((cam.x - dx, LateralBanquetaGente));
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
                bool alBarandal = lateral > 0f;
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
            List<Bounds> ocupado = OcupadoPaseo(raiz);
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
                if (ZonaMaleconT1393.Colocar(p, grupo, suelo, Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f), 1f, ocupado, p.name + "_" + n) != null) { n++; }
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
                    nGente++;
                }
            }
            Debug.Log("[Arte] T-1463 PoblarFoto: " + nCarros + " carros con luces y " + nGente + " personas caminando (congeladas a media zancada) junto a " + centro.ToString("F0") + ".");
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
            Transform faro = raiz.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == "ML04_Faro_Lejano");
            Renderer haz = raiz.GetComponentsInChildren<Renderer>(false).FirstOrDefault(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("T1413_HazFaroLejano", StringComparison.Ordinal));
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
            Transform faro = raiz.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == "ML04_Faro_Lejano");
            if (faro != null)
            {
                Bounds fb = ZonaMaleconT1393.CajaDe(faro.gameObject);
                Vector3 lampara = new Vector3(fb.center.x, fb.max.y, fb.center.z);
                bool vf = Proyectar(CamaraA11, MiraA11, CampoA11, lampara, out float fx, out float fy);
                Informe.AppendLine("A11 faro: pantalla x " + fx.ToString("F2") + ", y " + fy.ToString("F2") + " (esperado x < -0.1 e y > 0).");
                Exigir(vf && fx < -0.1f && fx > -1f && fy > 0f && fy < 1f, "A11: el faro no cae arriba a la izquierda.");
            }
            Vector3 playa = PuntoLateral(110f, 24f, ColocarT1354Curva.YPiePlaya);
            bool vp = Proyectar(CamaraA11, MiraA11, CampoA11, playa, out float px, out float py);
            Informe.AppendLine("A11 playa (x 110, lateral 24): pantalla x " + px.ToString("F2") + ", y " + py.ToString("F2") + " (esperado y < -0.1).");
            Exigir(vp && py < -0.1f && Mathf.Abs(px) <= 1f, "A11: la playa no cae abajo.");
            List<float> sxA = new List<float>();
            foreach ((Transform t, Bounds b) in Edificios(raiz))
            {
                if (EsDelFrente(b, out _) && EnA11(b) && Proyectar(CamaraA11, MiraA11, CampoA11, b.center, out float sx, out _)) { sxA.Add(sx); }
            }
            int aLaDerA = sxA.Count(s => s > 0f);
            Informe.AppendLine("A11 fachadas del frente en cuadro: " + sxA.Count + ", " + aLaDerA + " a la derecha del centro.");
            Exigir(sxA.Count >= 3 && aLaDerA >= sxA.Count * 0.7f, "A11: las fachadas no caen a la derecha.");
            Vector3 muelle = PuntoLateral(92f, 20f, 0f);
            Proyectar(CamaraA11, MiraA11, CampoA11, muelle, out float mx, out float my);
            Informe.AppendLine("A11 muelle (x 92): pantalla x " + mx.ToString("F2") + ", y " + my.ToString("F2") + " (en la referencia va arriba al centro).");
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
