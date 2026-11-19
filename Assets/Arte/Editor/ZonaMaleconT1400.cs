using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1400: Malecón, ronda 3. Corre DESPUÉS de ZonaMaleconT1393.Aplicar (edita el mismo prefab T1354_Malecon; todo lo nuevo cuelga de "T1400_Malecon").
    /// 1) ML05 frente al mar: grupos de 1 a 4 casas vecinas del frente de la avenida se apagan (no se borran) y se reemplazan por un ML05 (_a/_b alternados)
    ///    que ocupa el ancho del grupo, escala de 0.8 a 1.2, sin encimarse con las casas que quedan.
    /// 2) Paseo: palmeras y faroles de sodio cada 12 a 15 m pegados al barandal, a más de 6.5 m de las cámaras K11 y K12.
    /// 3) Banqueta de la ciudad: puestos (tacos, ciber), hieleras ML07, botes de basura y carros estacionados junto a la guarnición.
    /// Aparte: QuitarColumna (barrido de renderers en el cono de K11 en la escena), Validar y Fotos.
    /// Orden: T1384.Aplicar, T1393.Aplicar, T1400.Aplicar, T1400.Validar, T1400.QuitarColumna, y en corrida aparte T1400.Fotos.
    /// Si se vuelve a correr T1393.Aplicar, hay que volver a correr T1400.Aplicar después (T1393 reactiva todas las casas).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaMaleconT1400.Aplicar
    /// </summary>
    public static class ZonaMaleconT1400
    {
        public const string RutaPrefab = ZonaMaleconT1393.RutaPrefab;
        public const string NombreRaiz = "T1400_Malecon";
        private const string NombreApagados = "T1400_Apagados";

        private const string RutaML05 = "Assets/Arte/Zonas/ML/ML05_EdificioMalecon";
        private const string RutaPalmera = "Assets/Arte/Zonas/AR/AR03_Palmera.prefab";
        private const string RutaFarol = "Assets/Arte/Zonas/ML/ML01_Farol.prefab";
        private const string RutaHielera = "Assets/Arte/Zonas/ML/ML07_Hielera";
        private const string CarpetaCarros = "Assets/Arte/Vehiculos/Prefabs/";

        // ML05
        private const float EscalaMinimaML05 = 0.8f;
        private const float EscalaMaximaML05 = 1.2f;
        private const float AjusteAncho = 0.97f;
        private const float HuecoMaximoEntreCasas = 2f;
        private const float EncimeMaximo = 0.25f;
        private const int MinimoML05 = 10;
        private const int MaximoML05 = 16;
        private const int MinimoML05EnK11 = 3;
        private const float FrenteMinimo = 12.5f;
        private const float FrenteMaximo = 18.5f;

        // Paseo
        private const float PasoPaseo = 13.5f;
        private const float LateralPalmera = 14.7f;
        private const float LateralFarol = 15.1f;
        private const float RadioSinPalmera = 6.5f;
        private const int TopeSpotsFarol = 8;
        private const int TopeLucesNeon = 6;

        // Banqueta de la ciudad (laterales: negativo hacia la ciudad)
        private const float LateralBote = -8.7f;
        private const float LateralCarro = -8.45f;
        private const float LateralMuro = ColocarT1354Curva.LateralCiudad;
        private const float FondoMaximoPuesto = 1.9f;
        private const float AnchoMaximoPuesto = 4f;

        // K11 (foto): vertical 60 grados y 16:9
        private const float CampoK11 = 60f;
        private const float AspectoK11 = 16f / 9f;

        private static readonly Vector3 MiraK11 = new Vector3(-59f, 7f, -221.7f);
        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1400/Malecon ronda 3")]
        public static void Aplicar()
        {
            Informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                DeshacerAnterior(raiz);
                Transform mio = new GameObject(NombreRaiz).transform;
                mio.SetParent(raiz, false);
                Transform apagados = new GameObject(NombreApagados).transform;
                apagados.SetParent(mio, false);
                if (raiz.Find(ZonaMaleconT1393.NombreRaiz) == null)
                {
                    Informe.AppendLine("AVISO: no hay " + ZonaMaleconT1393.NombreRaiz + "; corre antes ZonaMaleconT1393.Aplicar.");
                }

                int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                KitS2c.Obra obra = KitS2c.Empezar("T1400_Extras", 1400, mio);
                PonerML05(obra, raiz, mio, apagados);
                PonerPaseo(obra, raiz, mio);
                PonerBanqueta(raiz, mio);
                KitS2c.Terminar(obra);
                MarcarEstaticos(mio);
                Informe.AppendLine("Luces antes " + lucesAntes + ", después " + raiz.GetComponentsInChildren<Light>(true).Length + "; nuevas de T-1400: " + mio.GetComponentsInChildren<Light>(true).Length
                    + " (spots de farol <= " + TopeSpotsFarol + ", luces de neón <= " + TopeLucesNeon + ").");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1400-aplicar");
        }

        /// <summary>Borra la raíz de T-1400 y vuelve a encender lo que apagó (casas y letreros), anotado por su camino de índices.</summary>
        private static void DeshacerAnterior(Transform raiz)
        {
            Transform vieja = raiz.Find(NombreRaiz);
            if (vieja == null)
            {
                return;
            }
            Transform ap = vieja.Find(NombreApagados);
            int n = 0;
            if (ap != null)
            {
                foreach (Transform marca in ap)
                {
                    string[] partes = marca.name.Split('|');
                    if (partes.Length != 2)
                    {
                        continue;
                    }
                    Transform t = raiz;
                    foreach (string s in partes[0].Split('/'))
                    {
                        if (t == null || !int.TryParse(s, out int k) || k < 0 || k >= t.childCount)
                        {
                            t = null;
                            break;
                        }
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
            Informe.AppendLine("Deshecho lo anterior de T-1400: " + n + " objetos vueltos a encender.");
        }

        private static string Camino(Transform raiz, Transform t)
        {
            List<string> idx = new List<string>();
            for (Transform c = t; c != raiz && c != null; c = c.parent)
            {
                idx.Add(c.GetSiblingIndex().ToString());
            }
            idx.Reverse();
            return string.Join("/", idx);
        }

        private static void Apagar(Transform raiz, Transform apagados, Transform t)
        {
            if (!t.gameObject.activeSelf)
            {
                return;
            }
            string marca = Camino(raiz, t) + "|" + t.name;
            t.gameObject.SetActive(false);
            new GameObject(marca).transform.SetParent(apagados, false);
        }

        // ================================================================ Geometría de la avenida

        private static void Marco(float x, out Vector3 linea, out Vector3 tang, out Vector3 mar)
        {
            float z = ColocarT1354Curva.ZCentro(x);
            float dz = (ColocarT1354Curva.ZCentro(x + 1f) - ColocarT1354Curva.ZCentro(x - 1f)) * 0.5f;
            linea = new Vector3(x, 0f, z);
            tang = new Vector3(1f, 0f, dz).normalized;
            mar = ColocarT1354Curva.HaciaElMar(tang);
        }

        private static bool EnCuadroK11(Vector3 p)
        {
            Vector3 d = p - ZonaMaleconT1393.CamaraK11;
            Vector3 eje = (MiraK11 - ZonaMaleconT1393.CamaraK11).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, eje).normalized;
            Vector3 arriba = Vector3.Cross(eje, derecha);
            float f = Vector3.Dot(d, eje);
            if (f < 1f || f > 400f)
            {
                return false;
            }
            float tv = Mathf.Tan(CampoK11 * 0.5f * Mathf.Deg2Rad);
            return Mathf.Abs(Vector3.Dot(d, derecha)) <= f * tv * AspectoK11 && Mathf.Abs(Vector3.Dot(d, arriba)) <= f * tv;
        }

        // ================================================================ 1. ML05 por grupos

        private sealed class Plan
        {
            public List<int> Miembros;
            public GameObject Prefab;
            public float Escala;
            public Vector3 Posicion;
            public Quaternion Giro;
            public Bounds Caja;
            public float X0, X1;
        }

        private static void PonerML05(KitS2c.Obra obra, Transform raiz, Transform mio, Transform apagados)
        {
            Transform fachadas = raiz.Find("Fachadas");
            GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + ZonaMaleconT1393.Variante + ".prefab");
            GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(RutaML05 + ZonaMaleconT1393.VarianteAlterna + ".prefab");
            if (fachadas == null || (a == null && b == null))
            {
                Informe.AppendLine("FALTA: Fachadas o ML05_EdificioMalecon. Sin ML05 de grupo.");
                return;
            }
            if (a == null) { a = b; }
            if (b == null) { b = a; }

            List<Transform> casas = fachadas.Cast<Transform>().Where(x => x.name.StartsWith("Casa_", StringComparison.Ordinal) && x.gameObject.activeSelf).ToList();
            List<Bounds> cajas = casas.Select(x => ZonaMaleconT1393.CajaDe(x.gameObject)).ToList();
            List<float> fondo = cajas.Select(c => c.center.z - ColocarT1354Curva.ZCentro(c.center.x)).ToList();
            List<int> frente = Enumerable.Range(0, casas.Count).Where(i => fondo[i] >= FrenteMinimo && fondo[i] <= FrenteMaximo).OrderBy(i => cajas[i].center.x).ToList();

            // ML05 que dejó T-1393 (cuentan como ocupados).
            List<Bounds> ocupados = new List<Bounds>();
            Transform ml93 = raiz.Find(ZonaMaleconT1393.NombreRaiz + "/ML05");
            if (ml93 != null)
            {
                foreach (Transform h in ml93) { ocupados.Add(ZonaMaleconT1393.CajaDe(h.gameObject)); }
            }

            // Planes posibles, de oeste a este: grupos de 3, 2, 4 y 1 casas vecinas.
            List<Plan> planes = new List<Plan>();
            int paso = 0;
            int p = 0;
            while (p < frente.Count)
            {
                Plan hecho = null;
                foreach (int tam in new[] { 3, 2, 4, 1 })
                {
                    if (p + tam > frente.Count) { continue; }
                    List<int> g = frente.GetRange(p, tam);
                    bool contiguo = true;
                    for (int k = 1; k < g.Count && contiguo; k++)
                    {
                        contiguo = cajas[g[k]].min.x - cajas[g[k - 1]].max.x < HuecoMaximoEntreCasas;
                    }
                    if (!contiguo) { continue; }
                    GameObject prefab = paso % 2 == 0 ? a : b;
                    Plan plan = Calcular(prefab, g, cajas, frente);
                    if (plan != null)
                    {
                        hecho = plan;
                        paso++;
                        break;
                    }
                }
                if (hecho != null)
                {
                    planes.Add(hecho);
                    p += hecho.Miembros.Count;
                }
                else
                {
                    p++;
                }
            }

            // Si salen demasiados, se dejan algunas casas del kit para variar (semilla fija).
            System.Random azar = new System.Random(1400);
            while (planes.Count > MaximoML05)
            {
                planes.RemoveAt(azar.Next(planes.Count));
            }
            Informe.AppendLine("ML05 de grupo: " + planes.Count + " planes sobre " + frente.Count + " casas del frente (casas activas " + casas.Count + ").");

            Transform grupo = new GameObject("ML05").transform;
            grupo.SetParent(mio, false);
            Transform fach = new GameObject("Fachadas").transform;
            fach.SetParent(mio, false);
            int n = 0;
            int luces = 0;
            Color[] colores = { KitS2c.Turquesa, KitS2c.Magenta, KitS2c.Sodio };
            foreach (Plan plan in planes.OrderBy(q => ZonaMaleconT1393.DistanciaACamaras(q.Caja.center)))
            {
                Bounds chica = plan.Caja;
                chica.Expand(-EncimeMaximo * 2f);
                if (ocupados.Any(o => o.Intersects(chica)))
                {
                    Informe.AppendLine("ML05 omitido en x " + plan.X0.ToString("F0") + ": se encimaba con otro ML05.");
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(plan.Prefab, grupo);
                go.name = plan.Prefab.name + "_" + n + "_" + string.Join("_", plan.Miembros.Select(i => casas[i].name));
                go.transform.rotation = plan.Giro;
                go.transform.localScale = Vector3.one * plan.Escala;
                go.transform.position = plan.Posicion;
                Renderer[] rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0 && go.GetComponent<LODGroup>() == null)
                {
                    LODGroup lod = go.AddComponent<LODGroup>();
                    lod.SetLODs(new[] { new LOD(0.012f, rs) });
                    lod.RecalculateBounds();
                }
                Bounds caja = ZonaMaleconT1393.CajaDe(go);
                ocupados.Add(caja);
                foreach (int i in plan.Miembros)
                {
                    Apagar(raiz, apagados, casas[i]);
                }
                // Letreros y aires de las casas quitadas (los de T-1384 y T-1393) quedarían flotando: se apagan.
                int off = ApagarFachadasDentro(raiz, apagados, plan.X0 - 0.5f, plan.X1 + 0.5f);
                // Un letrero de neón y un aire acondicionado nuevos sobre el ML05.
                Marco(caja.center.x, out Vector3 linea, out Vector3 tang, out Vector3 mar);
                Vector3 muro = linea + mar * (LateralMuro - 0.05f);
                Color color = colores[n % colores.Length];
                Vector3 pNeon = muro + tang * (plan.Caja.size.x * 0.3f) + Vector3.up * (color == KitS2c.Sodio ? 2.8f : 3.8f);
                int hijos = fach.childCount;
                KitS2c.Neon(obra, fach, pNeon, mar, tang, color, color != KitS2c.Sodio);
                if (fach.childCount > hijos)
                {
                    Transform neon = fach.GetChild(fach.childCount - 1);
                    ZonaMaleconT1393.Brillar(neon, color);
                    Light luz = neon.GetComponentInChildren<Light>(true);
                    if (luz != null)
                    {
                        if (luces < TopeLucesNeon) { luz.intensity = 3f; luz.range = 8f; luces++; }
                        else { Object.DestroyImmediate(luz.gameObject); }
                    }
                }
                GameObject ac = KitS2c.Prefab("C10_AireAcondicionado");
                if (ac != null)
                {
                    ZonaMaleconT1393.PonerAnclado(ac, fach, muro - tang * (plan.Caja.size.x * 0.28f) + Vector3.up * 3.4f, mar, 1f, "AireAcondicionado", false);
                }
                Informe.AppendLine("ML05 " + go.name + ": escala " + plan.Escala.ToString("F2") + ", ancho " + caja.size.x.ToString("F1") + " m para " + (plan.X1 - plan.X0).ToString("F1")
                    + " m de grupo (" + plan.Miembros.Count + " casas), " + off + " letreros viejos apagados" + (EnCuadroK11(caja.center) ? ", EN CUADRO K11" : ""));
                n++;
            }
            int enK11 = grupo.Cast<Transform>().Count(t => EnCuadroK11(ZonaMaleconT1393.CajaDe(t.gameObject).center));
            Informe.AppendLine("ML05 colocados por T-1400: " + n + " (más " + (ml93 != null ? ml93.childCount : 0) + " de T-1393); en el cuadro de K11: " + enK11 + ".");
        }

        private static Plan Calcular(GameObject prefab, List<int> g, List<Bounds> cajas, List<int> frente)
        {
            Bounds mb = ZonaMaleconT1393.Medida(prefab);
            if (mb.size.x < 1f)
            {
                return null;
            }
            float x0 = g.Min(i => cajas[i].min.x);
            float x1 = g.Max(i => cajas[i].max.x);
            float ancho = x1 - x0;
            float escala = ancho * AjusteAncho / mb.size.x;
            if (escala < EscalaMinimaML05 || escala > EscalaMaximaML05)
            {
                return null;
            }
            float cx = (x0 + x1) * 0.5f;
            Marco(cx, out Vector3 linea, out _, out Vector3 mar);
            Quaternion giro = Quaternion.LookRotation(-mar);
            Vector3 centro = linea + mar * (LateralMuro - mb.size.z * escala * 0.5f);
            Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
            Vector3 pos = new Vector3(centro.x, ColocarT1354Curva.YBanqueta, centro.z) - giro * ancla;
            Vector3 tam = giro * (mb.size * escala);
            Bounds caja = new Bounds(new Vector3(centro.x, ColocarT1354Curva.YBanqueta + mb.size.y * escala * 0.5f, centro.z), new Vector3(Mathf.Abs(tam.x), Mathf.Abs(tam.y), Mathf.Abs(tam.z)));
            Bounds chica = caja;
            chica.Expand(-EncimeMaximo * 2f);
            // Sin encimarse con las casas del frente que no son del grupo.
            foreach (int i in frente)
            {
                if (!g.Contains(i) && cajas[i].Intersects(chica))
                {
                    return null;
                }
            }
            return new Plan { Miembros = new List<int>(g), Prefab = prefab, Escala = escala, Posicion = pos, Giro = giro, Caja = caja, X0 = x0, X1 = x1 };
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

        // ================================================================ 2. Paseo junto al barandal

        private static List<Bounds> OcupadoPaseo(Transform raiz)
        {
            List<Bounds> lista = ZonaMaleconT1393.Ocupado(raiz);
            Transform p93 = raiz.Find(ZonaMaleconT1393.NombreRaiz + "/Paseo");
            if (p93 != null)
            {
                foreach (Transform t in p93)
                {
                    if (t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null) { lista.Add(ZonaMaleconT1393.CajaDe(t.gameObject)); }
                }
            }
            return lista;
        }

        private static float DistanciaK11K12(Vector3 p)
        {
            Vector2 q = new Vector2(p.x, p.z);
            return Mathf.Min(Vector2.Distance(q, new Vector2(ZonaMaleconT1393.CamaraK11.x, ZonaMaleconT1393.CamaraK11.z)),
                Vector2.Distance(q, new Vector2(ZonaMaleconT1393.CamaraK12.x, ZonaMaleconT1393.CamaraK12.z)));
        }

        private static bool SinCalle(float x)
        {
            return Mathf.Abs(Mathf.Abs(x) - 47f) < 10f || x > 100f;
        }

        private static void PonerPaseo(KitS2c.Obra obra, Transform raiz, Transform mio)
        {
            GameObject palmera = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPalmera);
            GameObject farol = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFarol);
            if (palmera == null || farol == null)
            {
                Informe.AppendLine("FALTA: AR03_Palmera o ML01_Farol. Sin paseo.");
                return;
            }
            Transform grupo = new GameObject("Paseo").transform;
            grupo.SetParent(mio, false);
            List<Bounds> ocupado = OcupadoPaseo(raiz);
            List<Vector3> borde = ColocarT1354Curva.BordeMar(-330f, 175f);
            float largo = ColocarT1354Curva.Longitud(borde);
            List<(bool esPalmera, Vector3 p, Quaternion giro)> candidatos = new List<(bool, Vector3, Quaternion)>();
            int i = 0;
            for (float d = PasoPaseo * 0.5f; d <= largo; d += PasoPaseo, i++)
            {
                Vector3 pt = ColocarT1354Curva.PuntoEn(borde, d, out _);
                // Lateral medido desde el eje de la avenida bajo el punto.
                Marco(pt.x, out Vector3 linea, out _, out Vector3 marAv);
                bool pal = i % 2 == 0;
                Vector3 pos = linea + marAv * (pal ? LateralPalmera : LateralFarol);
                pos.y = ColocarT1354Curva.YBanqueta;
                if (SinCalle(pos.x)) { continue; }
                candidatos.Add((pal, pos, Quaternion.LookRotation(marAv)));
            }
            int nP = 0, nF = 0, spots = 0;
            foreach (var c in candidatos.OrderBy(x => ZonaMaleconT1393.DistanciaACamaras(x.p)))
            {
                if (DistanciaK11K12(c.p) < RadioSinPalmera) { continue; }
                GameObject prefab = c.esPalmera ? palmera : farol;
                float escala = c.esPalmera ? 1f + (float)obra.Azar.NextDouble() * 0.3f : 1f;
                Quaternion giro = c.esPalmera ? Quaternion.Euler(0f, (float)obra.Azar.NextDouble() * 360f, 0f) : c.giro;
                GameObject go = ZonaMaleconT1393.Colocar(prefab, grupo, c.p, giro, escala, ocupado, (c.esPalmera ? "AR03_Palmera_" : "ML01_Farol_") + (nP + nF));
                if (go == null) { continue; }
                if (c.esPalmera) { nP++; continue; }
                nF++;
                Bounds fb = ZonaMaleconT1393.Medida(farol);
                Vector3 cabeza = c.p + Vector3.up * fb.size.y * 0.92f;
                KitS2c.Halo(obra, go.transform, cabeza, ColocarT1354Curva.YBanqueta, KitS2c.Sodio);
                if (spots < TopeSpotsFarol)
                {
                    Vector3 haciaAvenida = -(c.giro * Vector3.forward);
                    KitS2c.Luz(go.transform, cabeza, LightType.Spot, KitS2c.Sodio, ColocarS2oLuz.IntensidadPoste * 1.6f, ColocarS2oLuz.RangoPoste * 1.25f,
                        ColocarS2oLuz.AnguloPoste, (haciaAvenida * 0.25f + Vector3.down).normalized);
                    spots++;
                }
            }
            Informe.AppendLine("Paseo al barandal: " + nP + " palmeras, " + nF + " faroles (" + spots + " con spot de sodio sin sombras, el resto solo halo), cada " + PasoPaseo
                + " m; a más de " + RadioSinPalmera + " m de K11 y K12. No hay modelo de banca en el proyecto: sin bancas.");
        }

        // ================================================================ 3. Banqueta de la ciudad

        private static void PonerBanqueta(Transform raiz, Transform mio)
        {
            Transform grupo = new GameObject("Banqueta").transform;
            grupo.SetParent(mio, false);
            List<Bounds> ocupado = OcupadoPaseo(raiz);
            System.Random azar = new System.Random(14001);

            // Puestos contra el muro, con su hielera y su basurero al lado.
            GameObject[] puestos = { KitS2c.Prefab("C12_PuestoTacos"), KitS2c.Prefab("C13_PuestoCiber") };
            GameObject hA = AssetDatabase.LoadAssetAtPath<GameObject>(RutaHielera + "_a.prefab");
            GameObject hB = AssetDatabase.LoadAssetAtPath<GameObject>(RutaHielera + "_b.prefab");
            GameObject basurero = KitS2c.Prefab("C11_Basurero");
            GameObject bote = KitS2c.Prefab("BT01_BoteBasura");
            int nPuesto = 0, nHielera = 0, nBasurero = 0, nBote = 0, nCarro = 0;
            int k = 0;
            for (float x = -285f + (float)azar.NextDouble() * 10f; x < 150f; x += 50f + (float)azar.NextDouble() * 20f, k++)
            {
                if (SinCalle(x) || ZonaMaleconT1393.DistanciaACamaras(new Vector3(x, 0f, ColocarT1354Curva.ZCentro(x) + 10f)) < 4f) { continue; }
                GameObject prefab = puestos[k % 2];
                if (prefab == null) { continue; }
                Marco(x, out Vector3 linea, out Vector3 tang, out Vector3 mar);
                Bounds mb = ZonaMaleconT1393.Medida(prefab);
                float e = Mathf.Min(1f, FondoMaximoPuesto / Mathf.Max(0.1f, mb.size.z), AnchoMaximoPuesto / Mathf.Max(0.1f, mb.size.x));
                if (e < 0.6f)
                {
                    Informe.AppendLine("Puesto " + prefab.name + " omitido: mide " + mb.size + " y no cabe en la banqueta (escala " + e.ToString("F2") + " < 0.6).");
                    continue;
                }
                Vector3 muro = linea + mar * (LateralMuro + 0.02f);
                muro.y = ColocarT1354Curva.YBanqueta;
                GameObject go = ZonaMaleconT1393.PonerAnclado(prefab, grupo, muro, mar, e, prefab.name, false);
                if (go == null) { continue; }
                Bounds cb = ZonaMaleconT1393.CajaDe(go);
                Bounds cg = cb;
                cg.Expand(0.3f);
                if (ocupado.Any(o => o.Intersects(cg)))
                {
                    Object.DestroyImmediate(go);
                    continue;
                }
                ocupado.Add(cb);
                nPuesto++;
                // Hielera y basurero a cada lado del puesto, contra el muro.
                GameObject h = (nPuesto % 2 == 0) ? hA : hB;
                if (h != null)
                {
                    Vector3 sitio = muro + tang * (cb.extents.x + 0.8f);
                    GameObject gh = ZonaMaleconT1393.PonerAnclado(h, grupo, sitio, mar, 1f, h.name, false);
                    if (gh != null && !Choca(gh, ocupado)) { nHielera++; } else if (gh != null) { Object.DestroyImmediate(gh); }
                }
                if (basurero != null)
                {
                    Vector3 sitio = muro - tang * (cb.extents.x + 0.7f);
                    GameObject gb = ZonaMaleconT1393.PonerAnclado(basurero, grupo, sitio, mar, 1f, basurero.name, false);
                    if (gb != null && !Choca(gb, ocupado)) { nBasurero++; } else if (gb != null) { Object.DestroyImmediate(gb); }
                }
            }

            // Botes de basura junto a la guarnición.
            if (bote != null)
            {
                for (float x = -290f + (float)azar.NextDouble() * 8f; x < 150f; x += 24f + (float)azar.NextDouble() * 8f)
                {
                    if (SinCalle(x)) { continue; }
                    Marco(x, out Vector3 linea, out _, out Vector3 mar);
                    Vector3 suelo = linea + mar * LateralBote;
                    suelo.y = ColocarT1354Curva.YBanqueta;
                    if (ZonaMaleconT1393.DistanciaACamaras(suelo) < 3f) { continue; }
                    if (ZonaMaleconT1393.Colocar(bote, grupo, suelo, Quaternion.LookRotation(mar), 1f, ocupado, "BT01_BoteBasura_" + nBote) != null) { nBote++; }
                }
            }

            // Carros estacionados junto a la guarnición (mitad sobre la banqueta, sin tocar el carril de circulación).
            List<GameObject> carros = Enumerable.Range(2, 7).Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCarros + "V0" + n + ".prefab")).Where(c => c != null).ToList();
            if (carros.Count == 0)
            {
                Informe.AppendLine("FALTA: ningún carro V02..V08 en Assets/Arte/Vehiculos/Prefabs. Sin carros estacionados.");
            }
            else
            {
                int ci = 0;
                for (float x = -280f + (float)azar.NextDouble() * 12f; x < 140f; x += 38f + (float)azar.NextDouble() * 16f)
                {
                    if (SinCalle(x)) { continue; }
                    GameObject prefab = carros[ci++ % carros.Count];
                    Bounds mb = ZonaMaleconT1393.Medida(prefab);
                    float largoCarro = Mathf.Max(mb.size.x, mb.size.z);
                    float anchoCarro = Mathf.Min(mb.size.x, mb.size.z);
                    if (largoCarro < 3f || largoCarro > 6.5f || anchoCarro > 2.4f)
                    {
                        Informe.AppendLine("Carro " + prefab.name + " omitido: mide " + mb.size + " (fuera de 3 a 6.5 m de largo).");
                        continue;
                    }
                    Marco(x, out Vector3 linea, out Vector3 tang, out Vector3 mar);
                    Vector3 suelo = linea + mar * LateralCarro;
                    suelo.y = ColocarT1354Curva.YBanqueta;
                    if (ZonaMaleconT1393.DistanciaACamaras(suelo) < 6f) { continue; }
                    Vector3 rumbo = azar.NextDouble() < 0.5 ? tang : -tang;
                    Quaternion giro = Quaternion.LookRotation(rumbo);
                    if (mb.size.x > mb.size.z) { giro *= Quaternion.Euler(0f, -90f, 0f); }
                    GameObject go = ZonaMaleconT1393.Colocar(prefab, grupo, suelo, giro, 1f, ocupado, "Estacionado_" + prefab.name + "_" + nCarro);
                    if (go == null) { continue; }
                    foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; }
                    foreach (MonoBehaviour m in go.GetComponentsInChildren<MonoBehaviour>(true)) { m.enabled = false; }
                    nCarro++;
                }
            }
            Informe.AppendLine("Banqueta de la ciudad: " + nPuesto + " puestos, " + nHielera + " hieleras ML07, " + nBasurero + " basureros, " + nBote + " botes, " + nCarro
                + " carros estacionados (sin colliders ni scripts). Gente: la pone el tráfico.");
        }

        private static bool Choca(GameObject go, List<Bounds> ocupado)
        {
            Bounds b = ZonaMaleconT1393.CajaDe(go);
            Bounds g = b;
            g.Expand(0.2f);
            if (ocupado.Any(o => o.Intersects(g))) { return true; }
            ocupado.Add(b);
            return false;
        }

        private static void MarcarEstaticos(Transform mio)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in mio.GetComponentsInChildren<Transform>(true))
            {
                if (t == mio || t.GetComponent<Light>() != null || t.name == NombreApagados || (t.parent != null && t.parent.name == NombreApagados)) { continue; }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T-1400/Validar")]
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
                    Escribir("t1400-validar");
                    return;
                }
                int problemas = 0;

                // ML05
                Transform ml = mio.Find("ML05");
                List<Transform> hijos = ml == null ? new List<Transform>() : ml.Cast<Transform>().ToList();
                List<Bounds> cajas = hijos.Select(h => ZonaMaleconT1393.CajaDe(h.gameObject)).ToList();
                Transform fachadas = raiz.Find("Fachadas");
                List<Bounds> casasActivas = fachadas == null ? new List<Bounds>() : fachadas.Cast<Transform>().Where(t => t.name.StartsWith("Casa_", StringComparison.Ordinal) && t.gameObject.activeSelf)
                    .Select(t => ZonaMaleconT1393.CajaDe(t.gameObject)).ToList();
                if (hijos.Count < MinimoML05) { problemas++; Informe.AppendLine("ERROR: solo " + hijos.Count + " ML05 de grupo (mínimo " + MinimoML05 + ")."); }
                int enK11 = 0;
                for (int i = 0; i < hijos.Count; i++)
                {
                    Transform h = hijos[i];
                    float e = h.localScale.x;
                    if (e < EscalaMinimaML05 - 0.001f || e > EscalaMaximaML05 + 0.001f) { problemas++; Informe.AppendLine("ERROR: escala " + e.ToString("F2") + " fuera de 0.8 a 1.2: " + h.name); }
                    Bounds ch = cajas[i];
                    ch.Expand(-EncimeMaximo * 2f);
                    for (int j = i + 1; j < hijos.Count; j++)
                    {
                        if (ch.Intersects(cajas[j])) { problemas++; Informe.AppendLine("ENCIMADO: " + h.name + " con " + hijos[j].name); }
                    }
                    foreach (Bounds c in casasActivas)
                    {
                        float lat = c.center.z - ColocarT1354Curva.ZCentro(c.center.x);
                        if (lat >= FrenteMinimo && lat <= FrenteMaximo && ch.Intersects(c)) { problemas++; Informe.AppendLine("ENCIMADO con una casa del kit: " + h.name); break; }
                    }
                    if (cajas[i].min.y > 0.7f || cajas[i].min.y < -0.4f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + h.name + " min.y=" + cajas[i].min.y.ToString("F2")); }
                    // El frente (-Z local) mira a la avenida.
                    Marco(cajas[i].center.x, out Vector3 linea, out _, out _);
                    Vector3 haciaAvenida = (linea - new Vector3(cajas[i].center.x, 0f, cajas[i].center.z)).normalized;
                    float mira = Vector3.Dot(h.rotation * Vector3.back, haciaAvenida);
                    if (mira < 0.85f) { problemas++; Informe.AppendLine("DE ESPALDAS a la avenida (" + mira.ToString("F2") + "): " + h.name); }
                    if (EnCuadroK11(cajas[i].center)) { enK11++; }
                }
                Informe.AppendLine("ML05 de grupo: " + hijos.Count + "; en el cuadro de K11: " + enK11);
                if (enK11 < MinimoML05EnK11) { problemas++; Informe.AppendLine("ERROR: menos de " + MinimoML05EnK11 + " ML05 dentro del cuadro de K11."); }
                Transform ap = mio.Find(NombreApagados);
                Informe.AppendLine("Objetos apagados (casas y letreros viejos, siguen en el prefab): " + (ap == null ? 0 : ap.childCount));
                if (fachadas != null)
                {
                    int apagadas = fachadas.Cast<Transform>().Count(t => t.name.StartsWith("Casa_", StringComparison.Ordinal) && !t.gameObject.activeSelf);
                    Informe.AppendLine("Casas del kit apagadas en total: " + apagadas + "; activas: " + casasActivas.Count);
                }

                // Paseo
                Transform paseo = mio.Find("Paseo");
                int pal = 0, far = 0;
                if (paseo != null)
                {
                    foreach (Transform t in paseo)
                    {
                        Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                        float lat = ColocarT1354Curva.ZCentro(b.center.x) - b.center.z;
                        if (b.min.y > 0.7f || b.min.y < -0.5f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + t.name + " min.y=" + b.min.y.ToString("F2")); }
                        if (-lat < 12f) { problemas++; Informe.AppendLine("LEJOS DEL BARANDAL (lateral " + (-lat).ToString("F1") + "): " + t.name); }
                        if (DistanciaK11K12(b.center) < RadioSinPalmera) { problemas++; Informe.AppendLine("TAPA LA CÁMARA: " + t.name); }
                        if (t.name.StartsWith("AR03_Palmera", StringComparison.Ordinal)) { pal++; } else { far++; }
                    }
                }
                Informe.AppendLine("Paseo: " + pal + " palmeras, " + far + " faroles");
                if (pal < 8 || far < 8) { problemas++; Informe.AppendLine("ERROR: pocas palmeras o faroles en el paseo."); }

                // Banqueta de la ciudad
                Transform banq = mio.Find("Banqueta");
                int carros = 0, botes = 0, puestos = 0, hieleras = 0;
                if (banq != null)
                {
                    foreach (Transform t in banq)
                    {
                        Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                        float z = ColocarT1354Curva.ZCentro(b.center.x);
                        if (b.min.y > 0.7f || b.min.y < -0.5f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + t.name + " min.y=" + b.min.y.ToString("F2")); }
                        if (t.name.StartsWith("Estacionado_", StringComparison.Ordinal))
                        {
                            carros++;
                            if (b.center.z - b.extents.z - z < 7.0f) { problemas++; Informe.AppendLine("CARRO EN EL CARRIL: " + t.name + " borde a " + (b.center.z - b.extents.z - z).ToString("F2") + " m del eje"); }
                        }
                        else if (t.name.StartsWith("BT01", StringComparison.Ordinal)) { botes++; }
                        else if (t.name.StartsWith("ML07", StringComparison.Ordinal)) { hieleras++; }
                        else if (t.name.StartsWith("C12", StringComparison.Ordinal) || t.name.StartsWith("C13", StringComparison.Ordinal)) { puestos++; }
                        if (b.center.z - b.extents.z - z < 6.9f) { problemas++; Informe.AppendLine("EN EL CARRIL: " + t.name); }
                    }
                }
                Informe.AppendLine("Banqueta: " + puestos + " puestos, " + hieleras + " hieleras, " + botes + " botes, " + carros + " carros estacionados");
                if (puestos < 3 || botes < 5 || carros < 4) { problemas++; Informe.AppendLine("ERROR: pocos puestos, botes o carros en la banqueta."); }

                Informe.AppendLine("Luces de la zona: " + raiz.GetComponentsInChildren<Light>(true).Length + "; nuevas de T-1400: " + mio.GetComponentsInChildren<Light>(true).Length);
                Informe.AppendLine("Objetos en " + NombreRaiz + ": " + mio.GetComponentsInChildren<Transform>(true).Length + " (tope 3000)");
                Informe.AppendLine("Problemas: " + problemas);
                Informe.AppendLine("Tamaño del prefab (MB): " + (new FileInfo(RutaPrefab).Length / 1048576f).ToString("F1") + " (tope 60)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1400-validar");
        }

        // ================================================================ Columna oscura del cuadro de K11

        private static readonly string[] SeQuedan = { "Farol", "Poste", "Palmera", "Cable", "Faro", "Skyline", "SK01", "ML05", "Autopista", "Antena" };

        /// <summary>
        /// Barrido de renderers de la escena en el cono de la cámara K11 (30 a 600 m): altos y delgados (alto >= 12 m y >= 2.5 veces su ancho).
        /// Los que no son farol, poste, palmera, faro, skyline ni ML05 se apagan (renderer.enabled = false) y se guarda la escena. Lista completa en Logs/t1400-columna.txt.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1400/Quitar columna K11")]
        public static void QuitarColumna()
        {
            Informe.Clear();
            ColocarS2.Abrir();
            Vector3 cam = ZonaMaleconT1393.CamaraK11;
            Vector3 eje = MiraK11 - cam;
            Vector3 ejePlano = new Vector3(eje.x, 0f, eje.z).normalized;
            int quitadas = 0, vistas = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) { continue; }
                Bounds b = r.bounds;
                Vector3 d = b.center - cam;
                Vector3 dPlano = new Vector3(d.x, 0f, d.z);
                float dist = dPlano.magnitude;
                if (dist < 30f || dist > 600f) { continue; }
                float ang = Vector3.Angle(ejePlano, dPlano.normalized);
                if (ang > 50f) { continue; }
                float ancho = Mathf.Max(b.size.x, b.size.z);
                if (b.size.y < 12f || b.size.y < 2.5f * ancho) { continue; }
                float elevMin = Mathf.Atan2(b.min.y - cam.y, dist) * Mathf.Rad2Deg;
                if (elevMin > 40f) { continue; }
                vistas++;
                string ruta = Ruta(r.transform);
                bool queda = SeQuedan.Any(s => ruta.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
                Informe.AppendLine((queda ? "SE QUEDA   " : "QUITADA    ") + ruta + " | centro " + b.center + " tamaño " + b.size + " | a " + dist.ToString("F0") + " m, " + ang.ToString("F0") + " grados del eje");
                if (!queda)
                {
                    r.enabled = false;
                    EditorUtility.SetDirty(r);
                    quitadas++;
                }
            }
            Informe.AppendLine("Candidatas altas y delgadas en el cono de K11: " + vistas + "; apagadas: " + quitadas + ". Para rehacer, vuelve a encender el renderer indicado.");
            if (quitadas > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            Escribir("t1400-columna");
        }

        private static string Ruta(Transform t)
        {
            List<string> partes = new List<string>();
            for (Transform c = t; c != null; c = c.parent) { partes.Add(c.name); }
            partes.Reverse();
            return string.Join("/", partes);
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T-1400/Fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                // K11: en la banqueta del mar, mirando al oeste: barandal y paseo a la izquierda, avenida curva al centro, fachadas a la derecha, skyline sobre el mar.
                ("k11", "K11_Malecon.png", ZonaMaleconT1393.CamaraK11, MiraK11, CampoK11),
                ("k12", "K12_Malecon_Muelle.png", ZonaMaleconT1393.CamaraK12, new Vector3(20f, 2.2f, ColocarT1354Curva.ZCentro(20f) - 3f), 75f),
            };
            Directory.CreateDirectory("docs/capturas/t-1400");
            foreach (var t in tomas)
            {
                string juego = "Logs/t1400-" + t.nombre + ".png";
                ColocarS2cLook.Foto(t.desde, t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/t-1400/malecon-" + t.nombre + ".png");
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
