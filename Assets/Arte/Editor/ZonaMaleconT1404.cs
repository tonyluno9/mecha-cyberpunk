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
    /// T-1404: Malecón, ronda 4. Corre DESPUÉS de ZonaMaleconT1400.Aplicar (mismo prefab T1354_Malecon; todo lo nuevo cuelga de "T1404_Malecon", que se borra y rehace).
    /// 1) Faro del muelle (ML04_Faro, T-1363): se prende. El modelo es una sola malla, así que la linterna se hace con un halo cálido corto bajo la lámpara; un haz de HaloLuz
    ///    que gira (GiroHazFaro) con su Spot, una luz de punto cálida y dos luces rojas de aviso con su halo. El haz viejo del faro (HazFaro, tenue) se apaga y queda anotado.
    /// 2) La columna alta y oscura de la izquierda de K11 no es el faro (el faro está en x ~ 168 y K11 mira al oeste desde x = 40): Aplicar y IdentificarEnEscena hacen un barrido de
    ///    renderers altos y delgados en el cono de K11 y anotan su ruta y su posición en pantalla (Logs/t1404-columna-*.txt).
    /// 3) ML05: si menos de 2 ML05 (T-1393 y T-1400) entran en el cuadro de K11, los grupos de 1 a 3 casas del frente entre 15 y 90 m de la cámara y en cuadro se reemplazan por ML05.
    /// Orden: T1384, T1393, T1400.Aplicar, T1404.Aplicar, T1404.Validar, y en corrida aparte T1404.Fotos. Si se vuelve a correr T1400.Aplicar, correr T1404.Aplicar después.
    /// </summary>
    public static class ZonaMaleconT1404
    {
        public const string RutaPrefab = ZonaMaleconT1393.RutaPrefab;
        public const string NombreRaiz = "T1404_Malecon";
        private const string NombreApagados = "T1404_Apagados";
        private const string RutaML05 = "Assets/Arte/Zonas/ML/ML05_EdificioMalecon";

        private const float EscalaMinima = 0.6f;
        private const float EscalaMaxima = 1.4f;
        private const float AjusteAncho = 0.97f;
        private const float HuecoMaximo = 2f;
        private const float EncimeMaximo = 0.25f;
        private const float DistanciaMin = 15f;
        private const float DistanciaMax = 90f;
        private const float FrenteMinimo = 12.5f;
        private const float FrenteMaximo = 18.5f;
        private const float LateralMuro = ColocarT1354Curva.LateralCiudad;
        private const float CampoK11 = 60f;
        private const float AspectoK11 = 16f / 9f;
        private static readonly Vector3 MiraK11 = new Vector3(-59f, 7f, -221.7f);
        private static readonly Color Calido = new Color(1f, 0.92f, 0.65f);
        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Utilidades

        private static bool EnCuadro(Vector3 p, float margen = 1f)
        {
            Vector3 cam = ZonaMaleconT1393.CamaraK11;
            Vector3 d = p - cam;
            Vector3 eje = (MiraK11 - cam).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, eje).normalized;
            Vector3 arriba = Vector3.Cross(eje, derecha);
            float f = Vector3.Dot(d, eje);
            if (f < 1f || f > 400f)
            {
                return false;
            }
            float tv = Mathf.Tan(CampoK11 * 0.5f * Mathf.Deg2Rad) * margen;
            return Mathf.Abs(Vector3.Dot(d, derecha)) <= f * tv * AspectoK11 && Mathf.Abs(Vector3.Dot(d, arriba)) <= f * tv;
        }

        private static bool CajaEnCuadro(Bounds b)
        {
            return EnCuadro(b.center) || EnCuadro(new Vector3(b.min.x, b.center.y, b.min.z)) || EnCuadro(new Vector3(b.max.x, b.center.y, b.max.z))
                || EnCuadro(new Vector3(b.min.x, b.center.y, b.max.z)) || EnCuadro(new Vector3(b.max.x, b.center.y, b.min.z));
        }

        private static void Marco(float x, out Vector3 linea, out Vector3 tang, out Vector3 mar)
        {
            float z = ColocarT1354Curva.ZCentro(x);
            float dz = (ColocarT1354Curva.ZCentro(x + 1f) - ColocarT1354Curva.ZCentro(x - 1f)) * 0.5f;
            linea = new Vector3(x, 0f, z);
            tang = new Vector3(1f, 0f, dz).normalized;
            mar = ColocarT1354Curva.HaciaElMar(tang);
        }

        private static string Ruta(Transform t)
        {
            List<string> partes = new List<string>();
            for (Transform c = t; c != null; c = c.parent) { partes.Add(c.name); }
            partes.Reverse();
            return string.Join("/", partes);
        }

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

        /// <summary>Vuelve a encender lo que apagó la corrida anterior (por camino de índices y nombre) y borra la raíz.</summary>
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
            Informe.AppendLine("Deshecho lo anterior de T-1404: " + n + " objetos vueltos a encender.");
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1404/Malecon ronda 4 (aplicar)")]
        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1404 Malecón (ronda 4)");
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
                KitS2c.Obra obra = KitS2c.Empezar("T1404_Extras", 1404, mio);
                PrenderFaro(obra, raiz, mio);
                Barrido(raiz.GetComponentsInChildren<Renderer>(false), "prefab");
                PonerML05(raiz, mio, apagados);
                KitS2c.Terminar(obra);
                Informe.AppendLine("Luces antes " + lucesAntes + ", después " + raiz.GetComponentsInChildren<Light>(true).Length + "; nuevas de T-1404: " + mio.GetComponentsInChildren<Light>(true).Length + ".");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1404-malecon-aplicar");
        }

        // ================================================================ 1. Faro

        private static void PrenderFaro(KitS2c.Obra obra, Transform raiz, Transform mio)
        {
            List<Transform> faros = raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("ML04_Faro", StringComparison.Ordinal)
                && (t.parent == null || !t.parent.name.StartsWith("ML04_Faro", StringComparison.Ordinal)) && t.GetComponentInChildren<Renderer>(true) != null).ToList();
            if (faros.Count == 0)
            {
                Informe.AppendLine("FARO: no hay ML04_Faro en el prefab; sin faro que prender.");
                return;
            }
            Transform grupo = new GameObject("Faro").transform;
            grupo.SetParent(mio, false);
            int k = 0;
            foreach (Transform faro in faros)
            {
                Bounds b = ZonaMaleconT1393.CajaDe(faro.gameObject);
                if (b.size == Vector3.zero) { continue; }
                Vector3 lampara = new Vector3(b.center.x, b.min.y + b.size.y * 0.88f, b.center.z);
                // El haz viejo (tenue) de T-1354 se apaga: lo reemplaza el de T-1404.
                Transform viejo = faro.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "HazFaro");
                if (viejo != null && viejo.gameObject.activeSelf)
                {
                    viejo.gameObject.SetActive(false);
                    new GameObject("VIEJO|" + Camino(raiz, viejo)).transform.SetParent(grupo, false);
                    Informe.AppendLine("FARO " + Ruta(faro) + ": haz viejo HazFaro apagado (se reactiva rehaciendo T-1354, no por T-1404).");
                }
                Transform p = new GameObject("Faro_" + k).transform;
                p.SetParent(grupo, false);
                Informe.AppendLine("FARO " + Ruta(faro) + " en " + b.center.ToString("F1") + ", alto " + b.size.y.ToString("F1") + " m, lámpara en y " + lampara.y.ToString("F1") + ".");

                // Linterna: halo cálido corto bajo la lámpara (el modelo es una sola malla y no se puede hacer emisiva solo arriba).
                KitS2c.Halo(obra, p, lampara + Vector3.up * 1.5f, lampara.y - 6f, KitS2c.Sodio);
                // Luz de punto cálida, sin sombras.
                Light punto = KitS2c.Luz(p, lampara, LightType.Point, Calido, 900f, 45f, 0f);
                punto.name = "LuzLinterna";

                // Haz que gira con su Spot.
                GameObject pivote = new GameObject("HazFaro_T1404");
                pivote.transform.SetParent(p, false);
                pivote.transform.position = lampara;
                pivote.AddComponent<MechaCyberpunk.Mundo.Ambiente.GiroHazFaro>();
                GameObject cono = new GameObject("Haz");
                cono.transform.SetParent(pivote.transform, false);
                cono.transform.localRotation = Quaternion.Euler(2f, 250f, 0f);
                Mesh malla = MallaCono(140f, 1.5f, 12f);
                malla.name = "T1404_HazFaro_" + k;
                obra.Mallas.Add(malla);
                cono.AddComponent<MeshFilter>().sharedMesh = malla;
                MeshRenderer mr = cono.AddComponent<MeshRenderer>();
                mr.sharedMaterial = MatHalo("T1404_HazFaro", Calido, 0.4f, 1.2f, 1.3f);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                Light spot = KitS2c.Luz(cono.transform, lampara, LightType.Spot, Calido, 4000f, 140f, 28f, cono.transform.forward);
                spot.name = "LuzHaz";

                // Luces rojas de aviso: dos, a los lados de la baranda, con su halo rojo corto.
                Vector3[] lados = { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f) };
                for (int i = 0; i < lados.Length; i++)
                {
                    Vector3 pos = lampara + lados[i] * 1.8f - Vector3.up * 1.2f;
                    Light rojo = KitS2c.Luz(p, pos, LightType.Point, new Color(1f, 0.1f, 0.08f), 70f, 12f, 0f);
                    rojo.name = "LuzAviso_" + i;
                    GameObject h = new GameObject("HaloAviso_" + i);
                    h.transform.SetParent(p, false);
                    h.transform.position = pos;
                    Mesh mh = MallaCono(2.2f, 0.05f, 1.1f);
                    mh.name = "T1404_HaloAviso_" + k + "_" + i;
                    obra.Mallas.Add(mh);
                    h.transform.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
                    h.AddComponent<MeshFilter>().sharedMesh = mh;
                    MeshRenderer hr = h.AddComponent<MeshRenderer>();
                    hr.sharedMaterial = MatHalo("T1404_HaloAviso", new Color(1f, 0.12f, 0.08f), 0.6f, 1.2f, 1.3f);
                    hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    hr.receiveShadows = false;
                }
                k++;
            }
            Informe.AppendLine("FARO: " + k + " faro(s) prendidos (halo de linterna, punto cálido, haz giratorio con spot, 2 avisos rojos).");
        }

        private static Material MatHalo(string nombre, Color color, float intensidad, float borde, float caida)
        {
            return KitS2c.Mat(nombre, () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/HaloLuz"));
                m.SetColor("_Color", color);
                m.SetFloat("_Intensidad", intensidad);
                m.SetFloat("_Borde", borde);
                m.SetFloat("_Caida", caida);
                m.SetFloat("_Suave", 0.3f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                return m;
            });
        }

        /// <summary>Cono abierto que sale de la punta (z = 0) y se abre hasta radioFin en z = largo; la v de la UV va de 0 a 1.</summary>
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

        // ================================================================ 2. Columna del cono de K11

        /// <summary>Renderers altos y delgados (alto >= 12 m y >= 2.5 veces el ancho) en el cono de K11, a 30-600 m: se anotan con su ruta y su posición en pantalla. No cambia nada.</summary>
        private static void Barrido(IEnumerable<Renderer> renderers, string donde)
        {
            Vector3 cam = ZonaMaleconT1393.CamaraK11;
            Vector3 eje = (MiraK11 - cam).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, eje).normalized;
            Vector3 arriba = Vector3.Cross(eje, derecha);
            float tv = Mathf.Tan(CampoK11 * 0.5f * Mathf.Deg2Rad);
            int vistas = 0;
            foreach (Renderer r in renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) { continue; }
                Bounds b = r.bounds;
                Vector3 d = b.center - cam;
                float f = Vector3.Dot(d, eje);
                float dist = new Vector2(d.x, d.z).magnitude;
                if (dist < 30f || dist > 600f || f < 1f) { continue; }
                float ancho = Mathf.Max(b.size.x, b.size.z);
                if (b.size.y < 12f || b.size.y < 2.5f * ancho) { continue; }
                float sx = Vector3.Dot(d, derecha) / (f * tv * AspectoK11);
                float sy = Vector3.Dot(d, arriba) / (f * tv);
                if (Mathf.Abs(sx) > 1.1f) { continue; }
                vistas++;
                Informe.AppendLine("COLUMNA (" + donde + ") " + Ruta(r.transform) + " | centro " + b.center.ToString("F0") + " tamaño " + b.size.ToString("F0") + " | a " + dist.ToString("F0")
                    + " m | pantalla x " + sx.ToString("F2") + " (-1 izquierda, 1 derecha), y " + sy.ToString("F2"));
            }
            Informe.AppendLine("Barrido de K11 (" + donde + "): " + vistas + " candidatas. La columna oscura sobre el mar a la izquierda de K11 debe tener pantalla x entre -0.9 y -0.3.");
        }

        [MenuItem("Mecha Cyberpunk/T-1404/Malecon ronda 4 (identificar columna en escena)")]
        public static void IdentificarEnEscena()
        {
            Informe.Clear();
            ColocarS2.Abrir();
            Barrido(Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None), "escena");
            Escribir("t1404-columna-escena");
        }

        // ================================================================ 3. ML05 en el cuadro de K11

        private static void PonerML05(Transform raiz, Transform mio, Transform apagados)
        {
            List<Bounds> existentes = new List<Bounds>();
            foreach (string ruta in new[] { ZonaMaleconT1393.NombreRaiz + "/ML05", ZonaMaleconT1400.NombreRaiz + "/ML05" })
            {
                Transform g = raiz.Find(ruta);
                if (g == null) { continue; }
                foreach (Transform h in g)
                {
                    if (h.gameObject.activeInHierarchy) { existentes.Add(ZonaMaleconT1393.CajaDe(h.gameObject)); }
                }
            }
            int enCuadro = existentes.Count(CajaEnCuadro);
            Informe.AppendLine("ML05 existentes (T-1393 y T-1400): " + existentes.Count + "; en el cuadro de K11: " + enCuadro + ".");
            foreach (Bounds b in existentes.Where(CajaEnCuadro))
            {
                Informe.AppendLine("  en cuadro: centro " + b.center.ToString("F0") + ", distancia " + Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(ZonaMaleconT1393.CamaraK11.x, ZonaMaleconT1393.CamaraK11.z)).ToString("F0") + " m");
            }
            if (enCuadro >= 2)
            {
                Informe.AppendLine("ML05: ya hay " + enCuadro + " en el cuadro; no se cambia ninguna casa.");
                return;
            }
            Transform fachadas = raiz.Find("Fachadas");
            GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + ZonaMaleconT1393.Variante + ".prefab");
            GameObject b2 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + ZonaMaleconT1393.VarianteAlterna + ".prefab");
            if (fachadas == null || (a == null && b2 == null))
            {
                Informe.AppendLine("FALTA: Fachadas o ML05_EdificioMalecon. Sin cambios.");
                return;
            }
            if (a == null) { a = b2; }
            if (b2 == null) { b2 = a; }

            Vector2 cam = new Vector2(ZonaMaleconT1393.CamaraK11.x, ZonaMaleconT1393.CamaraK11.z);
            List<Transform> casas = fachadas.Cast<Transform>().Where(x => x.name.StartsWith("Casa_", StringComparison.Ordinal) && x.gameObject.activeSelf).ToList();
            List<Bounds> cajas = casas.Select(x => ZonaMaleconT1393.CajaDe(x.gameObject)).ToList();
            List<float> fondo = cajas.Select(c => c.center.z - ColocarT1354Curva.ZCentro(c.center.x)).ToList();
            List<int> frente = Enumerable.Range(0, casas.Count).Where(i => fondo[i] >= FrenteMinimo && fondo[i] <= FrenteMaximo).OrderBy(i => cajas[i].center.x).ToList();
            List<int> candidatas = frente.Where(i =>
            {
                float d = Vector2.Distance(new Vector2(cajas[i].center.x, cajas[i].center.z), cam);
                return d >= DistanciaMin && d <= DistanciaMax && CajaEnCuadro(cajas[i]);
            }).ToList();
            Informe.AppendLine("Casas del frente en cuadro entre " + DistanciaMin + " y " + DistanciaMax + " m de K11: " + candidatas.Count + " (de " + frente.Count + " del frente).");

            Transform grupo = new GameObject("ML05").transform;
            grupo.SetParent(mio, false);
            Transform fach = new GameObject("Fachadas").transform;
            fach.SetParent(mio, false);
            List<Bounds> ocupados = new List<Bounds>(existentes);
            int p = 0, n = 0;
            while (p < candidatas.Count)
            {
                bool hecho = false;
                foreach (int tam in new[] { 3, 2, 1 })
                {
                    if (p + tam > candidatas.Count) { continue; }
                    List<int> g = candidatas.GetRange(p, tam);
                    bool contiguo = true;
                    for (int q = 1; q < g.Count && contiguo; q++)
                    {
                        contiguo = cajas[g[q]].min.x - cajas[g[q - 1]].max.x < HuecoMaximo;
                    }
                    if (!contiguo) { continue; }
                    GameObject prefab = n % 2 == 0 ? a : b2;
                    Bounds mb = ZonaMaleconT1393.Medida(prefab);
                    if (mb.size.x < 1f) { continue; }
                    float x0 = g.Min(i => cajas[i].min.x), x1 = g.Max(i => cajas[i].max.x);
                    float escala = (x1 - x0) * AjusteAncho / mb.size.x;
                    if (escala < EscalaMinima || escala > EscalaMaxima) { continue; }
                    Marco((x0 + x1) * 0.5f, out Vector3 linea, out _, out Vector3 mar);
                    Quaternion giro = Quaternion.LookRotation(-mar);
                    Vector3 centro = linea + mar * (LateralMuro - mb.size.z * escala * 0.5f);
                    Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
                    Vector3 pos = new Vector3(centro.x, ColocarT1354Curva.YBanqueta, centro.z) - giro * ancla;
                    Vector3 tamano = giro * (mb.size * escala);
                    Bounds caja = new Bounds(new Vector3(centro.x, ColocarT1354Curva.YBanqueta + mb.size.y * escala * 0.5f, centro.z), new Vector3(Mathf.Abs(tamano.x), Mathf.Abs(tamano.y), Mathf.Abs(tamano.z)));
                    Bounds chica = caja;
                    chica.Expand(-EncimeMaximo * 2f);
                    if (ocupados.Any(o => o.Intersects(chica)) || frente.Any(i => !g.Contains(i) && cajas[i].Intersects(chica)))
                    {
                        Informe.AppendLine("ML05 omitido en x " + x0.ToString("F0") + ": se encimaba con otro edificio o con una casa que se queda.");
                        continue;
                    }
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
                    go.name = prefab.name + "_T1404_" + n;
                    go.transform.rotation = giro;
                    go.transform.localScale = Vector3.one * escala;
                    go.transform.position = pos;
                    Renderer[] rs = go.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0 && go.GetComponent<LODGroup>() == null)
                    {
                        LODGroup lod = go.AddComponent<LODGroup>();
                        lod.SetLODs(new[] { new LOD(0.012f, rs) });
                        lod.RecalculateBounds();
                    }
                    ocupados.Add(ZonaMaleconT1393.CajaDe(go));
                    foreach (int i in g) { Apagar(raiz, apagados, casas[i]); }
                    int off = ApagarFachadasDentro(raiz, apagados, x0 - 0.5f, x1 + 0.5f);
                    Informe.AppendLine("ML05 " + go.name + " reemplaza " + string.Join(",", g.Select(i => casas[i].name)) + " (escala " + escala.ToString("F2") + ", ancho del grupo " + (x1 - x0).ToString("F1")
                        + " m), " + off + " letreros viejos apagados" + (CajaEnCuadro(caja) ? ", EN CUADRO K11" : ""));
                    n++;
                    p += tam;
                    hecho = true;
                    break;
                }
                if (!hecho) { p++; }
            }
            foreach (Transform t in grupo.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            }
            Informe.AppendLine("ML05 nuevos de T-1404: " + n + ".");
        }

        /// <summary>Apaga los hijos no-casa de Fachadas (T-1384) y de T1393_Malecon/Fachadas que quedan sobre el muro en el rango de x.</summary>
        private static int ApagarFachadasDentro(Transform raiz, Transform apagados, float x0, float x1)
        {
            int n = 0;
            foreach (string ruta in new[] { "Fachadas", ZonaMaleconT1393.NombreRaiz + "/Fachadas" })
            {
                Transform g = raiz.Find(ruta);
                if (g == null) { continue; }
                foreach (Transform t in g.Cast<Transform>().ToList())
                {
                    if (!t.gameObject.activeSelf || t.name.StartsWith("Casa_", StringComparison.Ordinal)) { continue; }
                    Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                    if (b.size == Vector3.zero) { continue; }
                    float lateral = b.center.z - ColocarT1354Curva.ZCentro(b.center.x);
                    if (b.center.x >= x0 && b.center.x <= x1 && lateral >= 8.5f && lateral <= 15f && b.min.y < 9f)
                    {
                        Apagar(raiz, apagados, t);
                        n++;
                    }
                }
            }
            return n;
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T-1404/Malecon ronda 4 (validar)")]
        public static void Validar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1404 validar Malecón");
            int fallos = 0;
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mio = raiz.Find(NombreRaiz);
                if (mio == null)
                {
                    Informe.AppendLine("FALLO: no existe " + NombreRaiz);
                    fallos++;
                }
                else
                {
                    Light[] luces = mio.GetComponentsInChildren<Light>(true);
                    Informe.AppendLine("Luces nuevas: " + luces.Length);
                    foreach (Light l in luces)
                    {
                        if (l.shadows != LightShadows.None)
                        {
                            Informe.AppendLine("FALLO: luz con sombras " + l.name);
                            fallos++;
                        }
                    }
                    int pivotes = mio.GetComponentsInChildren<MechaCyberpunk.Mundo.Ambiente.GiroHazFaro>(true).Length;
                    Informe.AppendLine("Haces giratorios: " + pivotes);
                    if (pivotes == 0)
                    {
                        Informe.AppendLine("FALLO: no hay haz giratorio.");
                        fallos++;
                    }
                    Transform ml = mio.Find("ML05");
                    List<Bounds> cajas = new List<Bounds>();
                    foreach (string ruta in new[] { ZonaMaleconT1393.NombreRaiz + "/ML05", ZonaMaleconT1400.NombreRaiz + "/ML05", NombreRaiz + "/ML05" })
                    {
                        Transform g = raiz.Find(ruta);
                        if (g == null) { continue; }
                        foreach (Transform h in g)
                        {
                            if (h.gameObject.activeInHierarchy) { cajas.Add(ZonaMaleconT1393.CajaDe(h.gameObject)); }
                        }
                    }
                    for (int i = 0; i < cajas.Count; i++)
                    {
                        Bounds c = cajas[i];
                        c.Expand(-EncimeMaximo * 2f);
                        for (int j = i + 1; j < cajas.Count; j++)
                        {
                            if (c.Intersects(cajas[j]))
                            {
                                Informe.AppendLine("CHOQUE entre ML05 en " + cajas[i].center.ToString("F0") + " y " + cajas[j].center.ToString("F0"));
                                fallos++;
                            }
                        }
                        if (Mathf.Abs(cajas[i].min.y - ColocarT1354Curva.YBanqueta) > 0.5f)
                        {
                            Informe.AppendLine("ALTURA ML05 en " + cajas[i].center.ToString("F0") + ": base en y " + cajas[i].min.y.ToString("F2"));
                            fallos++;
                        }
                    }
                    Informe.AppendLine("ML05 totales activos: " + cajas.Count + ", nuevos de T-1404: " + (ml == null ? 0 : ml.childCount) + ", en cuadro de K11: " + cajas.Count(CajaEnCuadro));
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Informe.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Escribir("t1404-malecon-validar");
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T-1404/Malecon ronda 4 (fotos)")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            Directory.CreateDirectory("docs/capturas/t-1404");
            Directory.CreateDirectory("Logs");
            List<(string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)> tomas = new List<(string, string, Vector3, Vector3, float)>
            {
                ("k11", "K11_Malecon.png", ZonaMaleconT1393.CamaraK11, MiraK11, CampoK11),
            };
            Transform faro = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith("ML04_Faro", StringComparison.Ordinal));
            if (faro != null)
            {
                Vector3 f = faro.position;
                float x = f.x - 35f;
                tomas.Add(("faro", "K11_Malecon.png", new Vector3(x, 2.2f, ColocarT1354Curva.ZCentro(x) - 13f), f + Vector3.up * 16f, 60f));
            }
            foreach (var t in tomas)
            {
                string juego = "Logs/t1404-malecon-" + t.nombre + "-juego.png";
                ColocarS2cLook.Foto(t.desde, t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/t-1404/malecon-" + t.nombre + ".png");
            }
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
