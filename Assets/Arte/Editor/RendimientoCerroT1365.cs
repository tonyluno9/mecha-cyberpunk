using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1365: rendimiento de la Colonia del Cerro. <see cref="Aplicar"/> pone un LODGroup por casa (LOD0 todo, LOD1 solo la silueta barata),
    /// LODGroups de un solo nivel en los detalles pesados, apaga sombras de lo chico y de las fachadas, y quita sombras y alcance de más a las luces.
    /// <see cref="Combinar"/> (aparte, no se llama desde Aplicar) junta por celda los Cuerpo y las azoteas en una malla por material.
    /// Se llaman con -executeMethod MechaCyberpunk.Arte.Editor.RendimientoCerroT1365.Aplicar (o .Combinar). Aplicar es idempotente.
    /// </summary>
    public static class RendimientoCerroT1365
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        public const string RutaInforme = "Logs/t1365-rendimiento.txt";
        public const string RutaCombinado = "Assets/_Proyecto/Mundo/Datos/Zonas/T1365_Cerro_Combinado.asset";
        public const string NombreCombinado = "Combinado_T1365";

        /// <summary>Altura en pantalla (fracción de la pantalla) por debajo de la cual una casa pasa de LOD0 (todo) a LOD1 (silueta).</summary>
        public const float AltoCasaLod0 = 0.12f;
        /// <summary>Altura en pantalla por debajo de la cual la casa deja de dibujarse.</summary>
        public const float AltoCasaCulled = 0.012f;
        /// <summary>Altura en pantalla por debajo de la cual se apagan los detalles de azotea (tendederos, lavaderos).</summary>
        public const float AltoDetalleCulled = 0.04f;
        /// <summary>Altura en pantalla por debajo de la cual se apagan los carros viejos.</summary>
        public const float AltoCarroCulled = 0.03f;
        /// <summary>Tamaño en metros por debajo del cual un renderer no proyecta sombra.</summary>
        public const float TamanoMinimoSombra = 1.5f;
        /// <summary>Una pieza de azotea (tinaco, base) entra en LOD1 solo si tiene tantos triángulos o menos en su LOD0.</summary>
        public const int TriangulosMaxPiezaLod1 = 3000;
        public const float AlcanceMaximoLuz = 18f;
        public const int MaxLucesConSombra = 4;
        private const float LadoCelda = 40f;

        // ================================================================ Nombres

        public static bool EsCasaKit(string nombre)
        {
            return nombre.StartsWith("Casa_");
        }

        public static bool EsCasaTripo(string nombre)
        {
            return nombre.StartsWith("CC0") && nombre.Contains("CasaCerro");
        }

        private static bool EsDetallePesado(string nombre)
        {
            return nombre.StartsWith("CC04_") || nombre.StartsWith("CC05_") || nombre.Contains("Carro") || nombre.Contains("Aire")
                || nombre.Contains("Antena") || nombre.Contains("Tinaco");
        }

        // ================================================================ Triángulos y Mesh LOD

        /// <summary>
        /// Triángulos de la malla en el nivel de Mesh LOD pedido (0 = el más detallado, -1 = el más bajo). Sin Mesh LOD (lodCount menor o igual a 1) es la malla entera.
        /// Con Mesh LOD, GetIndexCount(sub) sin nivel cuenta todos los niveles juntos: por eso se pide el nivel explícito.
        /// </summary>
        public static long Triangulos(Mesh malla, int nivel)
        {
            if (malla == null)
            {
                return 0;
            }
            int niveles = malla.lodCount;
            long suma = 0;
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                suma += niveles > 1 ? malla.GetIndexCount(s, nivel < 0 ? niveles - 1 : Mathf.Min(nivel, niveles - 1)) : malla.GetIndexCount(s);
            }
            return suma / 3;
        }

        public static long TriangulosRenderer(Renderer r, int nivel)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            return mf == null ? 0 : Triangulos(mf.sharedMesh, nivel);
        }

        // ================================================================ Aplicar

        private class Cuenta
        {
            public int lodGroupsCasa;
            public int lodGroupsTripo;
            public int lodGroupsDetalle;
            public int lodGroupsInternosApagados;
            public int renderersLod0;
            public int renderersLod1;
            public long triLod0;
            public long triLod1;
            public int sombrasApagadas;
            public int sombrasEncendidas;
            public int lucesConSombraQuitada;
            public int lucesRangoBajado;
            public int lucesTotal;
            public int piezasLod1Excluidas;
            public readonly Dictionary<string, int> exclusiones = new Dictionary<string, int>();
        }

        public static void Aplicar()
        {
            Cuenta c = new Cuenta();
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            string informeMallas;
            try
            {
                Transform casas = raiz.transform.Find("Casas");
                Transform detalles = raiz.transform.Find("Detalles");
                Transform carros = raiz.transform.Find("Carros");
                if (casas != null)
                {
                    foreach (Transform hijo in casas.Cast<Transform>().ToList())
                    {
                        if (EsCasaKit(hijo.name))
                        {
                            AplicarCasaKit(hijo, c);
                        }
                        else if (EsCasaTripo(hijo.name))
                        {
                            AplicarCasaTripo(hijo, c);
                        }
                    }
                }
                AplicarDetalles(detalles, AltoDetalleCulled, c);
                AplicarDetalles(carros, AltoCarroCulled, c);
                AplicarLuces(raiz, c);
                informeMallas = InformeMallas(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            EscribirInforme(c, informeMallas);
            Debug.Log("[Arte] T-1365: aplicado; informe en " + RutaInforme);
        }

        private static void AplicarCasaKit(Transform casa, Cuenta c)
        {
            Renderer[] todos = casa.GetComponentsInChildren<Renderer>(true).Where(EsMalla).ToArray();
            if (todos.Length == 0)
            {
                return;
            }
            List<Renderer> lod1 = new List<Renderer>();
            foreach (Renderer r in todos)
            {
                if (EntraEnLod1(r, casa, c))
                {
                    lod1.Add(r);
                }
                // Solo el Cuerpo proyecta sombra: ya da la misma silueta que las fachadas y ahorra dibujarlas en el mapa de sombras.
                bool proyecta = r.gameObject.name == "Cuerpo";
                FijarSombra(r, proyecta, c);
            }
            DesactivarLodGroupsInternos(casa, c);
            LODGroup g = ObtenerLodGroup(casa.gameObject);
            g.SetLODs(new[] { new LOD(AltoCasaLod0, todos), new LOD(AltoCasaCulled, lod1.ToArray()) });
            g.RecalculateBounds();
            c.lodGroupsCasa++;
            c.renderersLod0 += todos.Length;
            c.renderersLod1 += lod1.Count;
            c.triLod0 += todos.Sum(r => TriangulosRenderer(r, 0));
            c.triLod1 += lod1.Sum(r => TriangulosRenderer(r, 0));
        }

        private static void AplicarCasaTripo(Transform casa, Cuenta c)
        {
            Renderer[] todos = casa.GetComponentsInChildren<Renderer>(true).Where(EsMalla).ToArray();
            if (todos.Length == 0)
            {
                return;
            }
            foreach (Renderer r in todos)
            {
                // La casa grande proyecta sombra; lo que le agrega T-1364 (Extra_*) solo si mide 1.5 m o más.
                bool extra = EsHijoExtra(r.transform, casa);
                FijarSombra(r, !extra || MedidaMayor(r) >= TamanoMinimoSombra, c);
            }
            DesactivarLodGroupsInternos(casa, c);
            LODGroup g = ObtenerLodGroup(casa.gameObject);
            g.SetLODs(new[] { new LOD(AltoCasaCulled, todos) });
            g.RecalculateBounds();
            c.lodGroupsTripo++;
            c.renderersLod0 += todos.Length;
            c.triLod0 += todos.Sum(r => TriangulosRenderer(r, 0));
        }

        private static void AplicarDetalles(Transform padre, float alturaCulled, Cuenta c)
        {
            if (padre == null)
            {
                return;
            }
            foreach (Transform hijo in padre.Cast<Transform>().ToList())
            {
                if (!EsDetallePesado(hijo.name))
                {
                    continue;
                }
                Renderer[] rs = hijo.GetComponentsInChildren<Renderer>(true).Where(EsMalla).ToArray();
                if (rs.Length == 0)
                {
                    continue;
                }
                foreach (Renderer r in rs)
                {
                    FijarSombra(r, MedidaMayor(r) >= TamanoMinimoSombra, c);
                }
                DesactivarLodGroupsInternos(hijo, c);
                LODGroup g = ObtenerLodGroup(hijo.gameObject);
                g.SetLODs(new[] { new LOD(alturaCulled, rs) });
                g.RecalculateBounds();
                c.lodGroupsDetalle++;
            }
        }

        private static bool EntraEnLod1(Renderer r, Transform casa, Cuenta c)
        {
            string n = r.gameObject.name;
            if (n == "Cuerpo" || n == "AzoteaT1318" || n == "VentanaCalida")
            {
                return true;
            }
            for (Transform t = r.transform; t != null && t != casa; t = t.parent)
            {
                bool tinaco = t.name == "BaseTinaco" || t.name.StartsWith("C09_Tinaco") || t.name.Contains("TK01");
                bool cuarto = t.name.StartsWith("Extra_") && (t.name.Contains("Cuarto") || t.name.Contains("Tinaco"));
                if (!tinaco && !cuarto)
                {
                    continue;
                }
                // Un cuarto de Tripo pesa decenas de miles de triángulos y Mesh LOD no se puede forzar: afuera de LOD1 (se anota).
                if (TriangulosRenderer(r, 0) <= TriangulosMaxPiezaLod1)
                {
                    return true;
                }
                c.piezasLod1Excluidas++;
                c.exclusiones[t.name] = c.exclusiones.TryGetValue(t.name, out int k) ? k + 1 : 1;
                return false;
            }
            return false;
        }

        private static bool EsMalla(Renderer r)
        {
            return r is MeshRenderer && r.GetComponent<MeshFilter>() != null;
        }

        private static bool EsHijoExtra(Transform t, Transform raiz)
        {
            for (; t != null && t != raiz; t = t.parent)
            {
                if (t.name.StartsWith("Extra_"))
                {
                    return true;
                }
            }
            return false;
        }

        private static float MedidaMayor(Renderer r)
        {
            Vector3 s = r.bounds.size;
            return Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }

        private static void FijarSombra(Renderer r, bool proyecta, Cuenta c)
        {
            ShadowCastingMode modo = proyecta ? ShadowCastingMode.On : ShadowCastingMode.Off;
            // VentanaCalida y otros ya vienen sin sombra: no los encendemos.
            if (proyecta && r.shadowCastingMode == ShadowCastingMode.Off)
            {
                modo = ShadowCastingMode.Off;
            }
            if (modo == ShadowCastingMode.Off)
            {
                if (r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    c.sombrasApagadas++;
                }
            }
            else
            {
                c.sombrasEncendidas++;
            }
            r.shadowCastingMode = modo;
        }

        private static LODGroup ObtenerLodGroup(GameObject go)
        {
            LODGroup g = go.GetComponent<LODGroup>();
            if (g == null)
            {
                g = go.AddComponent<LODGroup>();
            }
            g.fadeMode = LODFadeMode.None;
            g.animateCrossFading = false;
            return g;
        }

        /// <summary>Un LODGroup que venga dentro de una pieza (de su prefab) compite con el nuestro por los renderers: se apaga.</summary>
        private static void DesactivarLodGroupsInternos(Transform raiz, Cuenta c)
        {
            foreach (LODGroup g in raiz.GetComponentsInChildren<LODGroup>(true))
            {
                if (g.gameObject != raiz.gameObject && g.enabled)
                {
                    g.enabled = false;
                    c.lodGroupsInternosApagados++;
                }
            }
        }

        private static void AplicarLuces(GameObject raiz, Cuenta c)
        {
            foreach (Light l in raiz.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional)
                {
                    continue;
                }
                c.lucesTotal++;
                if (l.shadows != LightShadows.None)
                {
                    l.shadows = LightShadows.None;
                    c.lucesConSombraQuitada++;
                }
                if (l.range > AlcanceMaximoLuz)
                {
                    l.range = AlcanceMaximoLuz;
                    c.lucesRangoBajado++;
                }
            }
        }

        // ================================================================ Informe

        private static string InformeMallas(GameObject raiz)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<Mesh, int> usos = new Dictionary<Mesh, int>();
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null)
                {
                    usos[mf.sharedMesh] = usos.TryGetValue(mf.sharedMesh, out int k) ? k + 1 : 1;
                }
            }
            int conLods = usos.Keys.Count(m => m.lodCount > 1);
            sb.AppendLine("Mallas distintas en el prefab: " + usos.Count + ", con Mesh LOD (lodCount mayor que 1): " + conLods);
            sb.AppendLine("Las 25 mallas con más triángulos (nombre, usos, lodCount, triángulos LOD0, triángulos del LOD más bajo):");
            foreach (KeyValuePair<Mesh, int> kv in usos.OrderByDescending(p => Triangulos(p.Key, 0) * p.Value).Take(25))
            {
                sb.AppendLine("  " + kv.Key.name + "\t" + kv.Value + "\t" + kv.Key.lodCount + "\t" + Triangulos(kv.Key, 0) + "\t" + Triangulos(kv.Key, -1));
            }
            return sb.ToString();
        }

        private static void EscribirInforme(Cuenta c, string mallas)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1365 rendimiento del Cerro (" + RutaPrefab + ")");
            sb.AppendLine();
            sb.AppendLine("LODGroups: " + c.lodGroupsCasa + " casas del kit (LOD0 + LOD1), " + c.lodGroupsTripo + " casas de Tripo (un solo LOD culled a " + AltoCasaCulled + "), "
                + c.lodGroupsDetalle + " detalles pesados (un solo LOD).");
            sb.AppendLine("Umbrales (altura en pantalla): kit LOD0 hasta " + AltoCasaLod0 + ", LOD1 hasta " + AltoCasaCulled + ", culled por debajo; Tripo culled a " + AltoCasaCulled
                + "; detalles " + AltoDetalleCulled + "; carros " + AltoCarroCulled + ".");
            sb.AppendLine("LODGroups internos de piezas apagados: " + c.lodGroupsInternosApagados);
            sb.AppendLine();
            sb.AppendLine("Renderers de casas del kit: LOD0 " + c.renderersLod0 + " (incluye casas de Tripo), LOD1 " + c.renderersLod1);
            double pct = c.triLod0 == 0 ? 0 : 100.0 * c.triLod1 / c.triLod0;
            sb.AppendLine("Triángulos (LOD0 de cada malla): LOD0 " + c.triLod0 + ", LOD1 " + c.triLod1 + " (" + pct.ToString("0.00") + " % del LOD0)");
            sb.AppendLine("Piezas de azotea que no entraron en LOD1 por pesadas: " + c.piezasLod1Excluidas + " (" + string.Join(", ", c.exclusiones.Select(k => k.Key + " x" + k.Value)) + ")");
            sb.AppendLine();
            sb.AppendLine("Sombras apagadas: " + c.sombrasApagadas + " renderers; con sombra encendida en casas y detalles: " + c.sombrasEncendidas);
            sb.AppendLine("Luces (sin contar direccionales): " + c.lucesTotal + "; sombras quitadas a " + c.lucesConSombraQuitada + "; alcance bajado a " + AlcanceMaximoLuz + " m en " + c.lucesRangoBajado);
            sb.AppendLine();
            sb.Append(mallas);
            string completa = Path.GetFullPath(RutaInforme);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
        }

        // ================================================================ Combinar (aparte)

        /// <summary>
        /// Plan B si Aplicar no alcanza: junta los Cuerpo y las AzoteaT1318 de las casas del kit en una malla por celda de 40 x 40 m y por material.
        /// Los renderers originales se apagan (enabled = false) sin borrarlos, para que sus colliders y la jerarquía se queden.
        /// Las mallas van en <see cref="RutaCombinado"/>. Se puede repetir: deshace lo anterior antes de armar.
        /// </summary>
        public static void Combinar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            int grupos = 0;
            int originales = 0;
            try
            {
                Transform previo = raiz.transform.Find(NombreCombinado);
                if (previo != null)
                {
                    Object.DestroyImmediate(previo.gameObject);
                }
                Transform casas = raiz.transform.Find("Casas");
                Dictionary<(int, int, Material), List<Renderer>> celdas = new Dictionary<(int, int, Material), List<Renderer>>();
                if (casas != null)
                {
                    foreach (Transform casa in casas.Cast<Transform>().Where(t => EsCasaKit(t.name)))
                    {
                        foreach (Renderer r in casa.GetComponentsInChildren<Renderer>(true))
                        {
                            if ((r.gameObject.name != "Cuerpo" && r.gameObject.name != "AzoteaT1318") || !EsMalla(r) || r.sharedMaterial == null)
                            {
                                continue;
                            }
                            r.enabled = true; // vuelve a empezar desde el estado de Aplicar
                            Vector3 p = raiz.transform.InverseTransformPoint(r.bounds.center);
                            (int, int, Material) clave = (Mathf.FloorToInt(p.x / LadoCelda), Mathf.FloorToInt(p.z / LadoCelda), r.sharedMaterial);
                            if (!celdas.TryGetValue(clave, out List<Renderer> lista))
                            {
                                lista = new List<Renderer>();
                                celdas[clave] = lista;
                            }
                            lista.Add(r);
                        }
                    }
                }
                if (AssetDatabase.LoadAssetAtPath<Object>(RutaCombinado) != null)
                {
                    AssetDatabase.DeleteAsset(RutaCombinado);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(RutaCombinado)));
                GameObject contenedor = new GameObject(NombreCombinado);
                contenedor.transform.SetParent(raiz.transform, false);
                const StaticEditorFlags banderas = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic
                    | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic;
                GameObjectUtility.SetStaticEditorFlags(contenedor, banderas);
                List<Mesh> mallas = new List<Mesh>();
                foreach (KeyValuePair<(int, int, Material), List<Renderer>> kv in celdas.OrderBy(k => k.Key.Item1).ThenBy(k => k.Key.Item2).ThenBy(k => k.Key.Item3.name))
                {
                    List<CombineInstance> cis = new List<CombineInstance>();
                    foreach (Renderer r in kv.Value)
                    {
                        cis.Add(new CombineInstance
                        {
                            mesh = r.GetComponent<MeshFilter>().sharedMesh,
                            transform = raiz.transform.worldToLocalMatrix * r.transform.localToWorldMatrix,
                        });
                    }
                    Mesh m = new Mesh { name = "T1365_" + kv.Key.Item3.name + "_" + kv.Key.Item1 + "_" + kv.Key.Item2, indexFormat = IndexFormat.UInt32 };
                    m.CombineMeshes(cis.ToArray(), true, true);
                    m.RecalculateBounds();
                    mallas.Add(m);
                    GameObject go = new GameObject(m.name);
                    go.transform.SetParent(contenedor.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = m;
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = kv.Key.Item3;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    GameObjectUtility.SetStaticEditorFlags(go, banderas);
                    foreach (Renderer r in kv.Value)
                    {
                        r.enabled = false;
                        originales++;
                    }
                    grupos++;
                }
                if (mallas.Count > 0)
                {
                    AssetDatabase.CreateAsset(mallas[0], RutaCombinado);
                    for (int i = 1; i < mallas.Count; i++)
                    {
                        AssetDatabase.AddObjectToAsset(mallas[i], RutaCombinado);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-1365: Combinar: " + grupos + " mallas combinadas, " + originales + " renderers originales apagados, mallas en " + RutaCombinado);
        }
    }
}
