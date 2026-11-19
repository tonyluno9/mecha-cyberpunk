using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1385: la Colonia del Cerro más cerca de K13 y A13. Trabaja sobre el prefab T1355_Cerro (NO corre el generador ColocarT1355Cerro):
    /// todo lo nuevo cuelga de la raíz "T1385_Cerro" que Aplicar borra y rehace (se puede correr dos veces).
    /// Qué hace, en orden:
    ///  1. Libera la salida del túnel de la autopista: quita del terreno (Terreno/Ladera) los triángulos que suben dentro de la caja de la boca.
    ///  2. Muros de contención (CC08) en la calle, bajo las casas que están levantadas sobre la banqueta.
    ///  3. Casas nuevas CC06 y CC07 en los lotes libres a los lados de las calles, con su muro, su escalera de servicio (PuntoTrepar) y detalles de azotea.
    ///  4. Postes con transformador y lámpara, cables, letreros de neón chicos, carros estacionados, botes de basura y macetas.
    ///  5. Material de calle: M_T09_ConcretoManchado en banquetas y escaleras, M_T07_AsfaltoMojado en el arroyo.
    /// Las fotos K13 y A13 se toman con las cámaras de este archivo (<see cref="K13Desde"/>, <see cref="A13Desde"/>).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaCerroT1385.Aplicar, luego .Validar, luego .Fotos (corrida aparte).
    /// </summary>
    public static class ZonaCerroT1385
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        public const string Raiz = "T1385_Cerro";
        public const string RutaInforme = "Logs/t1385-aplicar.txt";
        public const string RutaValidar = "Logs/t1385-validar.txt";
        public const string RutaMallaLadera = "Assets/_Proyecto/Mundo/Datos/Zonas/T1385_Cerro_Ladera.asset";
        public const int Semilla = 13850;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLucesNuevas = 40;

        // Variantes elegidas mirando imageassets/modelos-nuevos/nuevos-18nov/<Código>_a.png y _b.png contra K13/A13:
        // CC06_b: casa de bloque con escalera exterior y ventana (la _a es obra negra con lona, sin escalera);
        // CC07_a: edificio verde menta de tres pisos con cortina metálica, rejas, aire, tinaco y ropa tendida (la _b trae una plataforma ancha que come lote);
        // CC08_a: muro de piedra naranja con escalera y barandal rojo, como los muros de K13 (la _b es de tubos y láminas, más industrial).
        public const string Variante06 = "_b";
        public const string Variante07 = "_a";
        public const string Variante08 = "_a";
        public const string CarpetaCC = "Assets/Arte/Zonas/CC/";

        public const string RutaM07 = "Assets/Arte/Materiales/Calle18nov/M_T07_AsfaltoMojado.mat";
        public const string RutaM09 = "Assets/Arte/Materiales/Calle18nov/M_T09_ConcretoManchado.mat";
        private static readonly string[] CarrosViejos =
        {
            "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab", "Assets/Arte/Vehiculos/Prefabs/V03.prefab",
            "Assets/Arte/Vehiculos/Prefabs/V05.prefab", "Assets/Arte/Vehiculos/Prefabs/V07.prefab",
        };

        // Cámaras de las fotos. K13: pierna 2 (x -360, sube hacia el sur 5%), parado antes de la escalera del z = 120 (que queda a la izquierda,
        // como en la referencia) mirando calle arriba. A13: desde el sureste, alto, sobre la ladera.
        public static readonly Vector3 K13Desde = new Vector3(-358.8f, 13.45f, 140f);
        public static readonly Vector3 K13Hacia = new Vector3(-361f, 15.6f, 95f);
        public static readonly Vector3 A13Desde = new Vector3(-300f, 48f, -100f);
        public static readonly Vector3 A13Hacia = new Vector3(-385f, 16f, 45f);

        // Caja de la boca del túnel que debe quedar libre (x -368..-352, z 185..193) más el tramo donde el terreno entra al tubo (hasta z 222).
        private static readonly Vector3 TunelMin = new Vector3(-370f, 10.3f, 183f);
        private static readonly Vector3 TunelMax = new Vector3(-350f, 40f, 222f);

        private const float FachadaDesdeEje = 6.5f;
        private const float AltoBanqueta = 0.2f;
        private const float SeparacionLotes = 9.5f;
        private const int TopeCasas = 16;
        private const int TopeMuros = 70;
        private const int TopeNeones = 6;
        private const int TopePostes = 8;
        private const int TopeCarros = 5;

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[256];
        private static readonly Collider[] bufCol = new Collider[256];

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Casas, Nuevo;
            public readonly List<List<Vector3>> Ejes = new List<List<Vector3>>();
            public KitS2c.Obra Obra;
            public int LucesAntes;
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1385/Cerro: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1385: Colonia del Cerro (" + RutaPrefab + ")\n");
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Ctx c = Abrir(zona, true);
                c.LucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                Log("Luces antes: " + c.LucesAntes + ". Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                LiberarTunel(c);
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                Physics.SyncTransforms();
                MurosBajoCasas(c);
                CasasNuevas(c);
                Postes(c);
                Carros(c);
                Menudo(c);
                Materiales(c);
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                Log("Luces después: " + luces + " (nuevas " + (luces - c.LucesAntes) + ", tope " + TopeLucesNuevas + ").");
                if (luces - c.LucesAntes > TopeLucesNuevas)
                {
                    Log("AVISO: se pasó del tope de luces nuevas.");
                }
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(zona, RutaPrefab, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log("Tamaño del prefab después: " + Megas(RutaPrefab) + " MB (tope 60).");
            }
            catch (Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[ZonaCerroT1385] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            Escribir(RutaInforme, informe);
        }

        private static Ctx Abrir(GameObject zona, bool borrarNuevo = true)
        {
            Physics.SyncTransforms();
            Ctx c = new Ctx
            {
                Zona = zona,
                Fisica = zona.scene.GetPhysicsScene(),
                Terreno = zona.transform.Find("Terreno"),
                Calles = zona.transform.Find("Calles"),
                Casas = zona.transform.Find("Casas"),
            };
            Transform viejo = zona.transform.Find(Raiz);
            if (viejo != null && borrarNuevo)
            {
                Object.DestroyImmediate(viejo.gameObject);
                Physics.SyncTransforms();
            }
            foreach (CalleZona cz in zona.GetComponentsInChildren<CalleZona>(false))
            {
                c.Ejes.Add(cz.Puntos());
            }
            Log("Calles de zona: " + c.Ejes.Count + ", casas: " + (c.Casas == null ? 0 : c.Casas.childCount) + ".");
            return c;
        }

        // ================================================================ 1. Túnel

        private static void LiberarTunel(Ctx c)
        {
            Transform ladera = c.Terreno == null ? null : c.Terreno.Find("Ladera");
            MeshFilter mf = ladera == null ? null : ladera.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Log("Túnel: no hay Terreno/Ladera, no se toca.");
                return;
            }
            Mesh origen = mf.sharedMesh;
            Mesh copia = Object.Instantiate(origen);
            copia.name = "T1385_Cerro_Ladera";
            Vector3[] v = origen.vertices;
            int quitados = 0;
            for (int s = 0; s < origen.subMeshCount; s++)
            {
                int[] t = origen.GetTriangles(s);
                List<int> queda = new List<int>(t.Length);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 cen = ladera.TransformPoint((v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f);
                    bool dentro = cen.x > TunelMin.x && cen.x < TunelMax.x && cen.z > TunelMin.z && cen.z < TunelMax.z && cen.y > TunelMin.y && cen.y < TunelMax.y;
                    if (dentro)
                    {
                        quitados++;
                    }
                    else
                    {
                        queda.Add(t[i]);
                        queda.Add(t[i + 1]);
                        queda.Add(t[i + 2]);
                    }
                }
                copia.SetTriangles(queda, s);
            }
            copia.RecalculateBounds();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaMallaLadera));
            AssetDatabase.DeleteAsset(RutaMallaLadera);
            AssetDatabase.CreateAsset(copia, RutaMallaLadera);
            mf.sharedMesh = copia;
            MeshCollider mc = ladera.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = copia;
            }
            Log("Túnel: " + quitados + " triángulos de Terreno/Ladera quitados en la caja " + TunelMin + " a " + TunelMax + " (la boca x -368..-352, z 185..193 queda libre).");
        }

        // ================================================================ 2. Muros bajo casas levantadas

        private static void MurosBajoCasas(Ctx c)
        {
            GameObject muro = Cargar("CC08_MuroContencion" + Variante08);
            if (muro == null || c.Casas == null)
            {
                Log("Muros: falta CC08_MuroContencion" + Variante08 + " (o no hay casas); no se ponen.");
                return;
            }
            Transform padre = Hijo(c.Nuevo, "Muros");
            List<VariarCerroT1364.CasaT> casas = VariarCerroT1364.Leer(c.Zona.transform);
            int puestos = 0, saltados = 0;
            foreach (VariarCerroT1364.CasaT casa in casas)
            {
                if (puestos >= TopeMuros)
                {
                    break;
                }
                float zc = (casa.Z0 + casa.Z1) * 0.5f;
                float xCalle = casa.Frente + casa.Normal * 1.0f;
                if (!Piso(c, xCalle, zc, casa.Base + 6f, out float ySuelo))
                {
                    saltados++;
                    continue;
                }
                float alto = casa.Base - ySuelo;
                if (alto < 0.6f || alto > 4.5f)
                {
                    continue;
                }
                float xMuro = casa.Frente + casa.Normal * 0.24f;
                if (PonerMuro(c, padre, muro, new Vector3(xMuro, ySuelo, zc), new Vector3(casa.Normal, 0f, 0f), casa.Ancho, alto + 0.1f))
                {
                    puestos++;
                }
                else
                {
                    saltados++;
                }
            }
            Log("Muros CC08 bajo casas levantadas: " + puestos + " puestos, " + saltados + " saltados (estorbaba algo o no había piso).");
        }

        /// <summary>
        /// Una hilera de CC08 (a escala pareja, sin deformar) de ancho <paramref name="ancho"/> que cubre del suelo a <paramref name="alto"/> m,
        /// con la cara hacia -<paramref name="afuera"/> contraria a la casa (la casa está en -afuera). Devuelve false si no cupo.
        /// </summary>
        private static bool PonerMuro(Ctx c, Transform padre, GameObject prefab, Vector3 pie, Vector3 afuera, float ancho, float alto)
        {
            Vector3 m = Medida(prefab);
            if (m.x < 0.1f || m.y < 0.1f || m.z < 0.1f)
            {
                return false;
            }
            float e = Mathf.Clamp(alto / m.y, 0.5f, 1.2f);
            int pisos = Mathf.Max(1, Mathf.CeilToInt(alto / (m.y * e) - 0.05f));
            int tramos = Mathf.Max(1, Mathf.RoundToInt(ancho / (m.x * e)));
            float paso = ancho / tramos;
            Vector3 lateral = new Vector3(-afuera.z, 0f, afuera.x);
            // Caja libre antes de poner nada: la franja de 0.5 m delante de la fachada, del suelo a la parte alta del muro.
            Vector3 centro = pie + afuera * 0.0f + Vector3.up * (alto * 0.5f + 0.05f);
            Vector3 mitad = new Vector3(Mathf.Abs(afuera.x) > 0.5f ? 0.2f : ancho * 0.5f - 0.1f, alto * 0.5f - 0.05f, Mathf.Abs(afuera.x) > 0.5f ? ancho * 0.5f - 0.1f : 0.2f);
            if (!Libre(c, centro, mitad, Quaternion.identity))
            {
                return false;
            }
            for (int i = 0; i < tramos; i++)
            {
                for (int p = 0; p < pisos; p++)
                {
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                    g.name = "CC08_Muro";
                    g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(-afuera));
                    g.transform.localScale = new Vector3(paso / m.x, alto / pisos / m.y, e);
                    Bounds b = Cubierta(g.transform);
                    Vector3 objetivo = pie + lateral * ((i + 0.5f) * paso - ancho * 0.5f) + Vector3.up * (p * alto / pisos) - afuera * (b.size.x > b.size.z ? b.size.z : b.size.x) * 0.0f;
                    g.transform.position += new Vector3(objetivo.x - b.center.x, objetivo.y - b.min.y - (p == 0 ? 0.3f : 0f), objetivo.z - b.center.z);
                    Colisionar(g);
                }
            }
            return true;
        }

        // ================================================================ 3. Casas nuevas

        private static void CasasNuevas(Ctx c)
        {
            GameObject cc06 = Cargar("CC06_CasaCerro" + Variante06);
            GameObject cc07 = Cargar("CC07_CasaCerro" + Variante07);
            GameObject muro = Cargar("CC08_MuroContencion" + Variante08);
            if (cc06 == null && cc07 == null)
            {
                Log("Casas nuevas: faltan CC06 y CC07 (llegan en T-1382); no se ponen.");
                return;
            }
            Transform padre = Hijo(c.Nuevo, "Casas");
            Transform muros = Hijo(c.Nuevo, "MurosCasas");
            int n = 0, rechazadas = 0, neones = 0;
            foreach (List<Vector3> eje in c.Ejes)
            {
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    float acumulado = SeparacionLotes;
                    for (int i = 1; i + 1 < eje.Count && n < TopeCasas; i++)
                    {
                        acumulado += Plano(eje[i] - eje[i - 1]).magnitude;
                        Vector3 t = Plano(eje[i + 1] - eje[i - 1]);
                        if (acumulado < SeparacionLotes || t.sqrMagnitude < 0.01f || Mathf.Abs(t.normalized.x) > 0.05f)
                        {
                            continue; // solo tramos rectos de norte a sur: las fachadas quedan en x constante
                        }
                        acumulado = 0f;
                        Vector3 afuera = new Vector3(lado, 0f, 0f);
                        GameObject prefab = n % 3 == 2 && cc06 != null ? cc06 : (cc07 != null ? cc07 : cc06);
                        if (n % 3 != 2 && cc07 == null)
                        {
                            prefab = cc06;
                        }
                        if (IntentarCasa(c, padre, muros, muro, prefab, eje[i], afuera, n, ref neones))
                        {
                            n++;
                        }
                        else
                        {
                            rechazadas++;
                        }
                    }
                }
            }
            Log("Casas nuevas CC06/CC07: " + n + " puestas, " + rechazadas + " lotes rechazados (ocupados, sin piso parejo o colgando).");
        }

        private static bool IntentarCasa(Ctx c, Transform padre, Transform muros, GameObject muro, GameObject prefab, Vector3 pCalle, Vector3 afuera, int numero, ref int neones)
        {
            Vector3 m = Medida(prefab);
            if (m.x < 0.1f || m.y < 0.1f || m.z < 0.1f)
            {
                return false;
            }
            float objetivoAncho = 6.2f;
            float e = Mathf.Clamp(objetivoAncho / Mathf.Max(m.x, m.z), 0.4f, 3f);
            float ancho = m.x * e, fondo = m.z * e, alto = m.y * e;
            if (alto > 12.5f)
            {
                e *= 12.5f / alto;
                ancho = m.x * e;
                fondo = m.z * e;
                alto = m.y * e;
            }
            float yBase = pCalle.y + AltoBanqueta;
            Vector3 fachada = new Vector3(pCalle.x + afuera.x * FachadaDesdeEje, yBase, pCalle.z);
            Vector3 centro = fachada + afuera * (fondo * 0.5f);
            // Piso parejo: la banqueta al frente y la ladera atrás deben estar entre -0.8 y +0.6 m de la base.
            Vector3 lateral = new Vector3(0f, 0f, 1f);
            foreach (Vector3 q in new[] { fachada + afuera * 1.0f, centro + lateral * ancho * 0.5f, centro - lateral * ancho * 0.5f, centro + afuera * fondo * 0.45f })
            {
                if (!Piso(c, q.x, q.z, yBase + 10f, out float gy) || gy < yBase - 0.8f || gy > yBase + 0.6f)
                {
                    return false;
                }
            }
            Vector3 mitad = new Vector3(fondo * 0.5f + 0.3f, (alto - 0.6f) * 0.5f, ancho * 0.5f + 0.4f);
            if (!Libre(c, new Vector3(centro.x, yBase + 0.6f + mitad.y, centro.z), mitad, Quaternion.identity))
            {
                return false;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = prefab.name + "_" + numero;
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(-afuera));
            g.transform.localScale = Vector3.one * e;
            Bounds b = Cubierta(g.transform);
            g.transform.position += new Vector3(centro.x - b.center.x, yBase - b.min.y, centro.z - b.center.z);
            b = Cubierta(g.transform);
            Colisionar(g);
            Physics.SyncTransforms();
            // Muro de contención en el frente si la ladera queda por debajo del piso (no deja la casa colgando).
            if (Piso(c, fachada.x + afuera.x * 0.3f, fachada.z, yBase + 10f, out float gf) && yBase - gf > 0.5f && muro != null)
            {
                PonerMuro(c, muros, muro, new Vector3(fachada.x - afuera.x * 0.0f + afuera.x * 0.24f, gf, fachada.z), afuera, Mathf.Min(ancho, 6f), yBase - gf + 0.1f);
            }
            // Escalera de servicio a la azotea (parkour): PuntoTrepar al pie de la fachada, con riel y peldaños visibles.
            AccesoTecho(c, padre, fachada, afuera, yBase, b.max.y, ancho, numero);
            DetalleAzotea(c, g.transform, centro, afuera, b.max.y, ancho, fondo, numero);
            if (neones < TopeNeones && numero % 2 == 0)
            {
                Color color = neones % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa;
                Vector3 p = new Vector3(fachada.x, yBase + Mathf.Min(2.6f, alto * 0.5f), fachada.z - ancho * 0.3f);
                KitS2c.Neon(c.Obra, c.Nuevo, p, -afuera, Vector3.forward, color, true);
                neones++;
            }
            return true;
        }

        private static void AccesoTecho(Ctx c, Transform padre, Vector3 fachada, Vector3 afuera, float yBase, float yTecho, float ancho, int numero)
        {
            float alto = yTecho - yBase;
            Vector3 pos = new Vector3(fachada.x + afuera.x * 0.05f, yBase, fachada.z + ancho * 0.5f - 0.5f);
            GameObject go = new GameObject("Acceso_T1385_" + numero);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-afuera));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
            Material mat = KitS2c.MatBanqueta();
            foreach (float lado in new[] { -0.25f, 0.25f })
            {
                KitS2c.Losa(c.Obra, go.transform, "Riel", new Vector3(fachada.x + afuera.x * 0.15f, yBase + alto * 0.5f, pos.z + lado), Quaternion.identity, new Vector3(0.06f, alto, 0.06f), mat, 1f, false);
            }
            for (float y = 0.4f; y < alto; y += 0.5f)
            {
                KitS2c.Losa(c.Obra, go.transform, "Peldano", new Vector3(fachada.x + afuera.x * 0.15f, yBase + y, pos.z), Quaternion.identity, new Vector3(0.05f, 0.05f, 0.5f), mat, 1f, false);
            }
        }

        private static void DetalleAzotea(Ctx c, Transform casa, Vector3 centro, Vector3 afuera, float yTecho, float ancho, float fondo, int numero)
        {
            // Solo las casas sin tinaco propio: CC06 (obra negra) lleva tinaco, antena y macetas; CC07 ya trae su tinaco y su tendedero.
            if (!casa.name.StartsWith("CC06"))
            {
                return;
            }
            Transform d = Hijo(c.Nuevo, "DetalleAzotea_" + numero);
            Vector3 atras = new Vector3(centro.x + afuera.x * fondo * 0.18f, yTecho, centro.z - ancho * 0.15f);
            if (KitS2c.Prefab("TK01_BaseTinaco") != null && KitS2c.Prefab("C09_Tinaco") != null)
            {
                KitS2c.Medida mt = KitS2c.Medir("TK01_BaseTinaco");
                KitS2c.Pieza(d, "TK01_BaseTinaco", atras, Quaternion.identity, new Vector3(1.5f / mt.Caja.size.x, 0.7f / mt.Caja.size.y, 1f));
                KitS2c.Pieza(d, "C09_Tinaco", atras + Vector3.up * 0.7f, Quaternion.identity, Vector3.one * 1.1f);
            }
            if (KitS2c.Prefab("CY04_AntenaAzotea") != null)
            {
                KitS2c.Pieza(d, "CY04_AntenaAzotea", atras + new Vector3(-afuera.x * 1.2f, 0f, 1.6f), Quaternion.identity, Vector3.one * 0.8f);
            }
        }

        // ================================================================ 4. Postes, carros, menudo

        private static void Postes(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Postes");
            List<Vector3> puntas = new List<Vector3>();
            int n = 0;
            foreach (Transform casa in c.Nuevo.Find("Casas") == null ? new List<Transform>() : c.Nuevo.Find("Casas").Cast<Transform>().ToList())
            {
                if (n >= TopePostes || !casa.name.StartsWith("CC0"))
                {
                    continue;
                }
                Bounds b = Cubierta(casa);
                float lado = b.center.x > MasCercano(c, b.center).x ? 1f : -1f; // hacia qué lado de la calle está la casa
                Vector3 eje = MasCercano(c, b.center);
                Vector3 pie = new Vector3(eje.x + (-lado) * (CalleZona_AnchoMedio(c) - 0.1f), eje.y + AltoBanqueta, b.center.z + 3.5f);
                // El poste va en el borde exterior de la banqueta, del lado de la casa.
                pie.x = eje.x + lado * (CalleZona_AnchoMedio(c) + 0.9f);
                if (!Libre(c, pie + Vector3.up * 3f, new Vector3(0.6f, 2.5f, 0.6f), Quaternion.identity) || DistanciaAEjes(c, pie) < 4.6f)
                {
                    continue;
                }
                Vector3 punta = KitS2c.Poste(c.Obra, padre, pie, new Vector3(-lado, 0f, 0f), pie.y - AltoBanqueta, n % 2 == 0);
                puntas.Add(punta);
                for (int k = 0; k < 3; k++)
                {
                    KitS2c.Cable(c.Obra, punta + Vector3.down * (0.3f + k * 0.22f), new Vector3(b.center.x - lado * (b.size.x * 0.4f), b.max.y, b.center.z), c.Obra.Rango(0.4f, 0.9f), 0.02f);
                }
                n++;
            }
            for (int i = 0; i + 1 < puntas.Count; i++)
            {
                if (Vector3.Distance(puntas[i], puntas[i + 1]) < 30f)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        KitS2c.Cable(c.Obra, puntas[i] + Vector3.down * (k * 0.22f), puntas[i + 1] + Vector3.down * (k * 0.22f), c.Obra.Rango(0.6f, 1f));
                    }
                }
            }
            Log("Postes nuevos con lámpara: " + n + " (con cables a azoteas y entre ellos).");
        }

        private static float CalleZona_AnchoMedio(Ctx c)
        {
            return 4.5f + 1.5f * 0f; // el arroyo es de 9 m: mitad 4.5; la banqueta (1.5 m) va después
        }

        private static void Carros(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Carros");
            List<GameObject> prefabs = CarrosViejos.Select(r => AssetDatabase.LoadAssetAtPath<GameObject>(r)).Where(p => p != null).ToList();
            if (prefabs.Count == 0)
            {
                Log("Carros: no hay prefabs de carro viejo.");
                return;
            }
            int n = 0;
            int paso = 0;
            foreach (List<Vector3> eje in c.Ejes)
            {
                for (int i = 6; i + 6 < eje.Count && n < TopeCarros; i += 14)
                {
                    Vector3 t = Plano(eje[i + 1] - eje[i - 1]);
                    if (t.sqrMagnitude < 0.01f || Mathf.Abs(t.normalized.x) > 0.05f)
                    {
                        continue;
                    }
                    t.Normalize();
                    float lado = (paso++ % 2 == 0) ? 1f : -1f;
                    Vector3 izq = new Vector3(-t.z, 0f, t.x);
                    Vector3 pos = eje[i] + izq * lado * (4.5f - 1.6f) + Vector3.up * 0.05f;
                    Quaternion giro = Quaternion.LookRotation(lado > 0f ? t : -t);
                    if (!Libre(c, pos + Vector3.up * 1f, new Vector3(1.1f, 0.6f, 2.3f), giro))
                    {
                        continue;
                    }
                    GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[n % prefabs.Count], padre);
                    car.name = "CarroViejo_T1385_" + n;
                    car.transform.SetPositionAndRotation(pos, giro);
                    n++;
                    Physics.SyncTransforms();
                }
            }
            Log("Carros viejos estacionados al borde (no en el carril): " + n + ".");
        }

        /// <summary>Botes de basura, bolsas y macetas pegados a las fachadas de las casas nuevas, fuera de las calles.</summary>
        private static void Menudo(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Menudo");
            Transform nuevas = c.Nuevo.Find("Casas");
            if (nuevas == null)
            {
                return;
            }
            string[] codigos = { "BT01_BoteBasura", "BS01a_Basura", "C20d_Macetas" };
            int n = 0;
            foreach (Transform casa in nuevas.Cast<Transform>().Where(t => t.name.StartsWith("CC0")).ToList())
            {
                Bounds b = Cubierta(casa);
                Vector3 eje = MasCercano(c, b.center);
                float lado = b.center.x > eje.x ? 1f : -1f;
                string codigo = codigos[n % codigos.Length];
                if (KitS2c.Prefab(codigo) == null)
                {
                    continue;
                }
                float x = eje.x + lado * (FachadaDesdeEje - 0.55f);
                Vector3 pos = new Vector3(x, eje.y + AltoBanqueta, b.center.z - b.size.z * 0.5f - 0.4f);
                if (!Piso(c, pos.x, pos.z, pos.y + 2f, out float y) || Mathf.Abs(y - pos.y) > 0.3f || DistanciaAEjes(c, pos) < 4.9f
                    || !Libre(c, pos + Vector3.up * 0.5f, new Vector3(0.4f, 0.4f, 0.4f), Quaternion.identity))
                {
                    continue;
                }
                KitS2c.Pieza(padre, codigo, new Vector3(pos.x, y, pos.z), Quaternion.Euler(0f, n * 70f, 0f), Vector3.one);
                n++;
            }
            Log("Botes, basura y macetas en las banquetas: " + n + ".");
        }

        // ================================================================ 5. Materiales y estático

        private static void Materiales(Ctx c)
        {
            Material m07 = AssetDatabase.LoadAssetAtPath<Material>(RutaM07);
            Material m09 = AssetDatabase.LoadAssetAtPath<Material>(RutaM09);
            if (m07 == null || m09 == null)
            {
                Log("Materiales: falta " + (m07 == null ? RutaM07 : "") + " " + (m09 == null ? RutaM09 : "") + " (llegan con T-1382); no se cambia nada.");
                return;
            }
            int arroyos = 0, banquetas = 0, escaleras = 0;
            if (c.Calles != null)
            {
                foreach (MeshRenderer r in c.Calles.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.name == "Arroyo")
                    {
                        r.sharedMaterial = m07;
                        arroyos++;
                    }
                    else if (r.name == "Banquetas")
                    {
                        r.sharedMaterial = m09;
                        banquetas++;
                    }
                }
            }
            Transform esc = c.Zona.transform.Find("Escaleras");
            if (esc != null)
            {
                foreach (MeshRenderer r in esc.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.name.StartsWith("CE01_EscaleraCerro_") && r.GetComponent<MeshFilter>() != null && r.transform.parent == esc)
                    {
                        r.sharedMaterial = m09;
                        escaleras++;
                    }
                }
            }
            Log("Materiales: M_T07 en " + arroyos + " arroyos, M_T09 en " + banquetas + " banquetas y " + escaleras + " tramos de escalera.");
        }

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t == c.Nuevo || t.name.StartsWith("CarroViejo_") || t.IsChildOf(c.Nuevo.Find("Carros") ?? c.Nuevo) && t.parent != null && t.parent.name == "Carros" || t.GetComponent<Light>() != null || t.GetComponent<Rigidbody>() != null)
                {
                    continue;
                }
                Transform arriba = t;
                bool enCarro = false;
                while (arriba != null && arriba != c.Nuevo)
                {
                    if (arriba.name.StartsWith("CarroViejo_"))
                    {
                        enCarro = true;
                    }
                    arriba = arriba.parent;
                }
                if (enCarro)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
                n++;
            }
            Log("Objetos marcados Static: " + n + ".");
        }

        // ================================================================ Ayudas

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ZonaCerroT1385] " + texto);
        }

        private static void Escribir(string ruta, StringBuilder sb)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
        }

        private static string Megas(string ruta)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            return File.Exists(completa) ? (new FileInfo(completa).Length / 1048576f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "?";
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static GameObject Cargar(string nombre)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCC + nombre + ".prefab");
            if (p == null)
            {
                Log("FALTA " + CarpetaCC + nombre + ".prefab (llega con T-1382).");
            }
            return p;
        }

        private static Vector3 Plano(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static readonly Dictionary<string, Vector3> medidas = new Dictionary<string, Vector3>();

        /// <summary>Tamaño del prefab sin girar ni escalar (por sus renderers).</summary>
        private static Vector3 Medida(GameObject prefab)
        {
            if (medidas.TryGetValue(prefab.name, out Vector3 ya))
            {
                return ya;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Vector3 tam = Cubierta(g.transform).size;
            Object.DestroyImmediate(g);
            medidas[prefab.name] = tam;
            return tam;
        }

        private static Bounds Cubierta(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
            {
                b.Encapsulate(rs[i].bounds);
            }
            return b;
        }

        /// <summary>Si el modelo no trae collider, cada malla visible recibe uno igual a ella (sin muros invisibles); así las azoteas se pisan.</summary>
        private static void Colisionar(GameObject g)
        {
            if (g.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                return;
            }
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Renderer>() != null)
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
        }

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

        /// <summary>Piso (terreno, arroyo o banqueta) en x, z: el más alto bajo <paramref name="yDesde"/>.</summary>
        private static bool Piso(Ctx c, float x, float z, float yDesde, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, yDesde, z), Vector3.down, bufRayos, 300f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (EsPiso(c, bufRayos[i].collider.transform) && bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                    ok = true;
                }
            }
            return ok;
        }

        /// <summary>True si la caja no toca ningún collider que no sea el piso (terreno y calles).</summary>
        private static bool Libre(Ctx c, Vector3 centro, Vector3 mitad, Quaternion giro)
        {
            int n = c.Fisica.OverlapBox(centro, mitad, bufCol, giro, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!EsPiso(c, bufCol[i].transform))
                {
                    return false;
                }
            }
            return true;
        }

        private static Vector3 MasCercano(Ctx c, Vector3 p)
        {
            Vector3 mejor = p;
            float d = float.MaxValue;
            foreach (List<Vector3> eje in c.Ejes)
            {
                foreach (Vector3 q in eje)
                {
                    float dd = Plano(q - p).sqrMagnitude;
                    if (dd < d)
                    {
                        d = dd;
                        mejor = q;
                    }
                }
            }
            return mejor;
        }

        /// <summary>Distancia en planta de p al eje de calle más cercano (a los segmentos, no solo a las muestras).</summary>
        private static float DistanciaAEjes(Ctx c, Vector3 p)
        {
            float mejor = float.MaxValue;
            foreach (List<Vector3> eje in c.Ejes)
            {
                for (int i = 0; i + 1 < eje.Count; i++)
                {
                    Vector3 a = Plano(eje[i]), b = Plano(eje[i + 1]), ab = b - a;
                    float l2 = ab.sqrMagnitude;
                    float t = l2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(Plano(p) - a, ab) / l2);
                    mejor = Mathf.Min(mejor, (Plano(p) - (a + ab * t)).magnitude);
                }
            }
            return mejor;
        }

        // ================================================================ Validar

        /// <summary>
        /// Revisa el prefab: lo nuevo no flota ni se entierra, nada nuevo en el carril (menos carros al borde), cada casa nueva tiene collider en el techo
        /// y un PuntoTrepar con salida sobre el techo, la boca del túnel está libre y el prefab pesa menos de 60 MB. Escribe Logs/t1385-validar.txt.
        /// </summary>
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1385: validación de " + RutaPrefab + "\n");
            int fallos = 0;
            informe = sb;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject zona = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Ctx c = Abrir(zona, false);
                c.Nuevo = zona.transform.Find(Raiz);
                if (c.Nuevo == null)
                {
                    sb.AppendLine("FALLO: no existe " + Raiz + " (corre Aplicar).");
                    fallos++;
                }
                else
                {
                    foreach (Transform casa in (c.Nuevo.Find("Casas") ?? c.Nuevo).Cast<Transform>().Where(t => t.name.StartsWith("CC0")))
                    {
                        Bounds b = Cubierta(casa);
                        if (casa.GetComponentsInChildren<Collider>(true).Length == 0)
                        {
                            sb.AppendLine("FALLO: " + casa.name + " no tiene collider.");
                            fallos++;
                        }
                        if (!Piso(c, b.center.x, b.center.z, b.min.y + 3f, out float y) || Mathf.Abs(y - b.min.y) > 1.2f)
                        {
                            sb.AppendLine("FALLO: " + casa.name + " flota o está enterrada (base " + b.min.y.ToString("0.00") + ", piso " + (y == float.MinValue ? "?" : y.ToString("0.00")) + ").");
                            fallos++;
                        }
                        float d = DistanciaAEjes(c, new Vector3(b.center.x, 0f, b.center.z)) - b.extents.x;
                        if (d < 5.9f)
                        {
                            sb.AppendLine("FALLO: " + casa.name + " invade la calle (a " + d.ToString("0.00") + " m del eje).");
                            fallos++;
                        }
                        // Techo pisable: rayo hacia abajo desde arriba cae sobre un collider de la casa a la altura del techo.
                        int n = c.Fisica.Raycast(new Vector3(b.center.x, b.max.y + 2f, b.center.z), Vector3.down, bufRayos, 30f, ~0, QueryTriggerInteraction.Ignore);
                        bool techo = false;
                        for (int i = 0; i < n; i++)
                        {
                            techo |= bufRayos[i].collider.transform.IsChildOf(casa) && bufRayos[i].point.y > b.max.y - 3f;
                        }
                        if (!techo)
                        {
                            sb.AppendLine("AVISO: " + casa.name + " no tiene collider arriba en el centro de la azotea (puede ser por un cuarto o tinaco).");
                        }
                    }
                    PuntoTrepar[] puntos = c.Nuevo.GetComponentsInChildren<PuntoTrepar>(true);
                    foreach (PuntoTrepar p in puntos)
                    {
                        Vector3 s = p.Salida;
                        if (c.Fisica.Raycast(s + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 1.5f, ~0, QueryTriggerInteraction.Ignore) == false)
                        {
                            sb.AppendLine("FALLO: " + p.name + ": no hay techo bajo la salida " + s);
                            fallos++;
                        }
                    }
                    sb.AppendLine("PuntoTrepar nuevos: " + puntos.Length + ".");
                    foreach (Transform t in c.Nuevo.Find("Carros") == null ? new List<Transform>() : c.Nuevo.Find("Carros").Cast<Transform>().ToList())
                    {
                        float d = DistanciaAEjes(c, t.position);
                        if (d > 4.3f || d < 1.2f)
                        {
                            sb.AppendLine("AVISO: " + t.name + " a " + d.ToString("0.00") + " m del eje (borde esperado 2.3 a 3.6).");
                        }
                    }
                }
                Physics.SyncTransforms();
                // Boca del túnel: nada del Cerro dentro de la caja x -368..-352, z 185..193, y 10.3..16 (salvo el piso de la calle, que va más abajo).
                Vector3 centroT = new Vector3(-360f, 13.2f, 189f);
                int nt = c.Fisica.OverlapBox(centroT, new Vector3(8f, 2.7f, 4f), bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < nt; i++)
                {
                    sb.AppendLine("FALLO: la boca del túnel está ocupada por " + bufCol[i].transform.name + " (" + bufCol[i].transform.parent?.name + ").");
                    fallos++;
                }
                sb.AppendLine("Triángulos del terreno en la caja del túnel: revisar en la escena con la autopista (aquí solo el Cerro).");
                string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RutaPrefab);
                long bytes = File.Exists(ruta) ? new FileInfo(ruta).Length : -1;
                sb.AppendLine("Tamaño del prefab: " + (bytes / 1048576f).ToString("0.0") + " MB " + (bytes >= TopeBytesPrefab ? "FALLO (>= 60 MB)" : "ok"));
                if (bytes >= TopeBytesPrefab)
                {
                    fallos++;
                }
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                sb.AppendLine("Luces del prefab: " + luces + " (las nuevas del Raíz: " + (c.Nuevo == null ? 0 : c.Nuevo.GetComponentsInChildren<Light>(true).Length) + ", tope " + TopeLucesNuevas + ").");
                if (c.Nuevo != null && c.Nuevo.GetComponentsInChildren<Light>(true).Length > TopeLucesNuevas)
                {
                    fallos++;
                }
                if (c.Nuevo != null)
                {
                    sb.AppendLine("Objetos nuevos: " + c.Nuevo.GetComponentsInChildren<Transform>(true).Length + " (tope ~3000).");
                }
            }
            finally
            {
                Object.DestroyImmediate(zona);
            }
            sb.AppendLine(fallos == 0 ? "RESULTADO: sin fallos." : "RESULTADO: " + fallos + " fallos.");
            Escribir(RutaValidar, sb);
            Debug.Log("[ZonaCerroT1385] " + sb);
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T1385/Cerro: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1385");
            ColocarS2cLook.Foto(K13Desde, K13Hacia, "docs/capturas/t-1385/cerro-k13-juego.png", 66f);
            ColocarS2cLook.Foto(A13Desde, A13Hacia, "docs/capturas/t-1385/cerro-a13-juego.png", 66f);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1385/cerro-k13-juego.png", "docs/capturas/t-1385/cerro-k13.png");
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", "docs/capturas/t-1385/cerro-a13-juego.png", "docs/capturas/t-1385/cerro-a13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }
    }
}
