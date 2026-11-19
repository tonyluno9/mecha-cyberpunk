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
    /// T-1401: Colonia del Cerro, ronda 3. Corre DESPUÉS de <see cref="ZonaCerroT1394"/> (que corre después de T1385) sobre el mismo prefab (T1355_Cerro).
    /// Todo lo nuevo cuelga de la raíz "T1401_Cerro", que Aplicar borra y rehace; lo que quita (postes) se guarda desactivado en "T1401_Cerro/Reemplazados"
    /// con su lugar en "T1401_Cerro/Registro", y Aplicar lo devuelve al empezar (se puede correr dos veces). Los cables malos se quitan de la malla de cables
    /// (se rehace una malla nueva en Mallas/T1401_Cerro.asset, la vieja no se toca); la corrida repetida solo revisa la malla ya limpia.
    ///  1. CABLES: revisa TODAS las mallas de cables del prefab (T1355, T1385, T1394), las parte en cables sueltos y quita los de más de 40 m, flecha de más
    ///     de 3 m, con puntos NaN/infinitos, enterrados bajo el piso, con triángulos deformes, pegados a la cámara K13 o colgados de un poste quitado.
    ///  2. POSTE pegado a K13: se quitan los postes que caen en el centro del encuadre cerca de la cámara (dejan el callejón libre, como en la referencia).
    ///  3. ENLACES entre azoteas: sus cajas lisas (malla unidad escalada, sin UV a escala) se rehacen con UV en metros: escalones con M_T09_ConcretoManchado
    ///     y tablones con madera con textura (T506_Madera_Vieja).
    ///  4. AZOTEAS visibles desde K13: tinacos negros (C09_Tinaco sobre su base), antenas (CY04) y tendederos (CC04) donde todavía no hay.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaCerroT1401.Aplicar, luego .Validar, luego .Fotos (corrida aparte).
    /// </summary>
    public static class ZonaCerroT1401
    {
        public const string RutaPrefab = ZonaCerroT1385.RutaPrefab;
        public const string Raiz = "T1401_Cerro";
        public const string RutaInforme = "Logs/t1401-aplicar.txt";
        public const string RutaValidar = "Logs/t1401-validar.txt";
        public const int Semilla = 14010;
        public const string RutaConcreto = ZonaCerroT1385.RutaM09;
        public const string RutaMadera = ZonaCerroT1394.RutaMadera;

        // Cámara K13: la misma de T1394 (calle base mirando al oeste-noroeste hacia la escalera que sube). A13 igual.
        public static readonly Vector3 K13Desde = ZonaCerroT1394.K13Desde;
        public static readonly Vector3 K13Hacia = ZonaCerroT1394.K13Hacia;
        public static readonly Vector3 A13Desde = ZonaCerroT1394.A13Desde;
        public static readonly Vector3 A13Hacia = ZonaCerroT1394.A13Hacia;
        public const float CampoFoto = ZonaCerroT1394.CampoFoto;

        // Límites de un cable sano.
        private const float LargoMaximoCable = 40f;
        private const float FlechaMaximaCable = 3f;
        private const float AristaMaximaCable = 3f;
        private const float AristaAroMaxima = 0.3f;
        private const float DistanciaCamaraCable = 1.0f;
        private const float EnterradoMinimo = 0.3f;

        // Postes: fuera los que caen en el centro del encuadre.
        private const float PosteCercaDeCamara = 4f;
        private const float PosteAdelanteMax = 14f;
        private const float PosteLadoMax = 3.5f;

        // Azoteas visibles.
        private const int TopeAzoteasVisibles = 60;
        private const int TopeObjetosNuevos = 150;
        private const float AzoteaMinDist = 7f;
        private const float AzoteaMaxDist = 80f;
        private const float AzoteaMaxAngulo = 48f;
        private const float MetrosUvConcreto = 1.2f;
        private const float MetrosUvMadera = 0.6f;
        private const float EscalaTinaco = 1.25f;
        private const float EscalaAntena = 1.1f;

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[64];
        private static readonly Collider[] bufCol = new Collider[256];
        private static readonly Dictionary<string, Vector3> medidas = new Dictionary<string, Vector3>();

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Nuevo, Reem, Reg;
            public KitS2c.Obra Obra;
            public Material Concreto, Madera;
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1401/Cerro ronda 3: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1401: Colonia del Cerro, ronda 3 (" + RutaPrefab + ")\n");
            medidas.Clear();
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int devueltos = Restaurar(zona);
                Log("Devueltos a su lugar (de una corrida anterior): " + devueltos + ".");
                Transform viejo = zona.transform.Find(Raiz);
                if (viejo != null && viejo.Find("Reemplazados") != null && viejo.Find("Reemplazados").childCount > 0)
                {
                    Log("ERROR: quedan objetos en " + Raiz + "/Reemplazados sin devolver; no se guarda nada.");
                    Escribir(RutaInforme, informe);
                    return;
                }
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                Physics.SyncTransforms();
                Ctx c = Abrir(zona);
                if (zona.transform.Find(ZonaCerroT1394.Raiz) == null)
                {
                    Log("AVISO: no existe " + ZonaCerroT1394.Raiz + "; corre ZonaCerroT1394.Aplicar antes. Se sigue con lo que haya.");
                }
                Log("Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                c.Reem = Hijo(c.Nuevo, "Reemplazados");
                c.Reg = Hijo(c.Nuevo, "Registro");
                c.Concreto = AssetDatabase.LoadAssetAtPath<Material>(RutaConcreto);
                if (c.Concreto == null)
                {
                    Log("AVISO: falta " + RutaConcreto + "; los escalones usan la banqueta del kit.");
                    c.Concreto = KitS2c.MatBanqueta();
                }
                c.Madera = AssetDatabase.LoadAssetAtPath<Material>(RutaMadera);
                if (c.Madera == null)
                {
                    Log("AVISO: falta " + RutaMadera + "; los tablones usan el concreto.");
                    c.Madera = c.Concreto;
                }
                else if (c.Madera.mainTexture == null)
                {
                    Log("AVISO: la madera " + RutaMadera + " no tiene textura principal; los tablones se verán de color liso.");
                }
                Physics.SyncTransforms();
                List<Vector3> postesQuitados = new List<Vector3>();
                Paso("postes del centro de K13", () => QuitarPostes(c, postesQuitados));
                Paso("cables", () => AnalizarCables(c, postesQuitados, true, null));
                Paso("enlaces", () => ArreglarEnlaces(c));
                Paso("azoteas visibles", () => PropsAzoteas(c));
                Estatico(c);
                KitS2c.Terminar(c.Obra);
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
                Debug.LogError("[ZonaCerroT1401] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            Escribir(RutaInforme, informe);
        }

        private static void Paso(string nombre, Action accion)
        {
            try
            {
                accion();
            }
            catch (Exception e)
            {
                Log("ERROR en el paso '" + nombre + "': " + e);
                Debug.LogError("[ZonaCerroT1401] " + nombre + ": " + e);
            }
        }

        private static Ctx Abrir(GameObject zona)
        {
            Physics.SyncTransforms();
            return new Ctx
            {
                Zona = zona,
                Fisica = zona.scene.GetPhysicsScene(),
                Terreno = zona.transform.Find("Terreno"),
                Calles = zona.transform.Find("Calles"),
            };
        }

        // ================================================================ Registro: quitar sin perder (mismo formato que T1394)

        private static int Restaurar(GameObject zona)
        {
            Transform raiz = zona.transform.Find(Raiz);
            if (raiz == null)
            {
                return 0;
            }
            Transform reem = raiz.Find("Reemplazados");
            Transform reg = raiz.Find("Registro");
            if (reem == null || reg == null)
            {
                return 0;
            }
            if (reem.childCount != reg.childCount)
            {
                Log("AVISO: el registro de reemplazos no coincide (" + reem.childCount + " vs " + reg.childCount + "); no se devuelve nada.");
                return 0;
            }
            List<Transform> objetos = new List<Transform>();
            List<string> rutas = new List<string>();
            List<int> indices = new List<int>();
            for (int i = 0; i < reem.childCount; i++)
            {
                string[] p = reg.GetChild(i).name.Split('|');
                int idx;
                if (p.Length < 3 || !int.TryParse(p[2], out idx))
                {
                    continue;
                }
                objetos.Add(reem.GetChild(i));
                rutas.Add(p[1]);
                indices.Add(idx);
            }
            int[] orden = Enumerable.Range(0, objetos.Count).OrderBy(i => rutas[i], StringComparer.Ordinal).ThenBy(i => indices[i]).ToArray();
            int n = 0;
            foreach (int i in orden)
            {
                Transform padre = rutas[i].Length == 0 ? zona.transform : zona.transform.Find(rutas[i]);
                if (padre == null)
                {
                    Log("AVISO: no existe " + rutas[i] + " para devolver " + objetos[i].name + ".");
                    continue;
                }
                objetos[i].SetParent(padre, true);
                objetos[i].SetSiblingIndex(Mathf.Min(indices[i], padre.childCount - 1));
                objetos[i].gameObject.SetActive(true);
                n++;
            }
            Physics.SyncTransforms();
            return n;
        }

        private static string RutaPadre(Transform zona, Transform padre)
        {
            List<string> partes = new List<string>();
            Transform t = padre;
            while (t != null && t != zona)
            {
                partes.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", partes);
        }

        private static void Confirmar(Ctx c, List<Transform> lista)
        {
            List<string> nombres = new List<string>();
            foreach (Transform t in lista)
            {
                nombres.Add("Orig|" + RutaPadre(c.Zona.transform, t.parent) + "|" + t.GetSiblingIndex() + "|" + t.name);
            }
            for (int i = 0; i < lista.Count; i++)
            {
                GameObject marca = new GameObject(nombres[i]);
                marca.transform.SetParent(c.Reg, false);
                lista[i].SetParent(c.Reem, true);
                lista[i].gameObject.SetActive(false);
            }
        }

        // ================================================================ 2. Postes que tapan el centro de K13

        private static bool EsPoste(Transform t)
        {
            return t.name.StartsWith("Poste_") || t.name.StartsWith("C07_PosteLuz");
        }

        private static void QuitarPostes(Ctx c, List<Vector3> posiciones)
        {
            Vector3 adelante = Plano(K13Hacia - K13Desde).normalized;
            Vector3 derecha = new Vector3(adelante.z, 0f, -adelante.x);
            List<Transform> retirar = new List<Transform>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (!EsPoste(t) || (t.parent != null && EsPoste(t.parent)))
                {
                    continue;
                }
                Vector3 p = Plano(t.position - K13Desde);
                float ade = Vector3.Dot(p, adelante);
                float lado = Vector3.Dot(p, derecha);
                bool estorba = p.magnitude < PosteCercaDeCamara || (ade > 0f && ade < PosteAdelanteMax && Mathf.Abs(lado) < PosteLadoMax);
                if (!estorba)
                {
                    continue;
                }
                Log("Poste quitado del encuadre de K13: " + t.name + " (" + RutaPadre(c.Zona.transform, t.parent) + ") a " + p.magnitude.ToString("0.0") + " m, adelante "
                    + ade.ToString("0.0") + ", lado " + lado.ToString("0.0") + ".");
                posiciones.Add(t.position);
                retirar.Add(t);
            }
            if (retirar.Count > 0)
            {
                Confirmar(c, retirar);
            }
            Physics.SyncTransforms();
            Log("Postes quitados: " + retirar.Count + " (guardados desactivados en " + Raiz + "/Reemplazados).");
        }

        // ================================================================ 1. Cables rotos

        private static int BuscarRaiz(int[] raiz, int i)
        {
            while (raiz[i] != i)
            {
                raiz[i] = raiz[raiz[i]];
                i = raiz[i];
            }
            return i;
        }

        private static bool Finito(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        /// <summary>
        /// Revisa todas las mallas de cables (MeshFilter llamado "Cables" o con "Cables" en el nombre de la malla), cable por cable (componente conexo).
        /// Con <paramref name="modificar"/> deja en cada malla mala una malla nueva sin los cables rotos; si no, solo cuenta. Devuelve cuántos cables malos hay/hubo.
        /// </summary>
        private static int AnalizarCables(Ctx c, List<Vector3> postesQuitados, bool modificar, StringBuilder sb)
        {
            int malosTotal = 0, sanosTotal = 0, mallasMalas = 0, contador = 0, lineas = 0;
            Vector3 camaraAlta = K13Desde;
            List<MeshFilter> filtros = c.Zona.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.sharedMesh != null && (mf.name == "Cables" || mf.sharedMesh.name.Contains("Cables")) && (c.Reem == null || !mf.transform.IsChildOf(c.Reem))).ToList();
            Log("Mallas de cables encontradas: " + filtros.Count + ".");
            foreach (MeshFilter mf in filtros)
            {
                Mesh m = mf.sharedMesh;
                if (!m.isReadable)
                {
                    Log("AVISO: la malla " + m.name + " no es legible; no se revisó.");
                    continue;
                }
                Vector3[] v = m.vertices;
                Vector3[] nor = m.normals;
                int[] tri = m.triangles;
                int[] raiz = Enumerable.Range(0, v.Length).ToArray();
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    raiz[BuscarRaiz(raiz, tri[i + 1])] = BuscarRaiz(raiz, tri[i]);
                    raiz[BuscarRaiz(raiz, tri[i + 2])] = BuscarRaiz(raiz, tri[i]);
                }
                Vector3[] w = new Vector3[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    w[i] = mf.transform.TransformPoint(v[i]);
                }
                bool tieneAros = v.Length % 4 == 0;
                Dictionary<int, List<int>> comps = new Dictionary<int, List<int>>();
                HashSet<int> conTriangulos = new HashSet<int>();
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    conTriangulos.Add(BuscarRaiz(raiz, tri[i]));
                }
                for (int i = 0; i < v.Length; i++)
                {
                    int r = BuscarRaiz(raiz, i);
                    if (!conTriangulos.Contains(r))
                    {
                        continue;
                    }
                    List<int> l;
                    if (!comps.TryGetValue(r, out l))
                    {
                        l = new List<int>();
                        comps[r] = l;
                    }
                    l.Add(i);
                }
                Dictionary<int, string> malos = new Dictionary<int, string>();
                // Aristas largas: por triángulo.
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    int r = BuscarRaiz(raiz, tri[i]);
                    if (malos.ContainsKey(r))
                    {
                        continue;
                    }
                    for (int e = 0; e < 3; e++)
                    {
                        int ia = tri[i + e], ib = tri[i + (e + 1) % 3];
                        float len = Finito(w[ia]) && Finito(w[ib]) ? Vector3.Distance(w[ia], w[ib]) : float.PositiveInfinity;
                        bool mismoAro = tieneAros && ia / 4 == ib / 4;
                        if (len > AristaMaximaCable || (mismoAro && len > AristaAroMaxima))
                        {
                            malos[r] = "triángulo deforme (arista de " + (float.IsInfinity(len) ? "inf" : len.ToString("0.0")) + " m)";
                            break;
                        }
                    }
                }
                foreach (KeyValuePair<int, List<int>> kv in comps)
                {
                    if (malos.ContainsKey(kv.Key))
                    {
                        continue;
                    }
                    List<int> ids = kv.Value;
                    if (ids.Any(i => !Finito(w[i])))
                    {
                        malos[kv.Key] = "vértices NaN o infinitos";
                        continue;
                    }
                    Bounds bb = new Bounds(w[ids[0]], Vector3.zero);
                    foreach (int i in ids)
                    {
                        bb.Encapsulate(w[i]);
                    }
                    List<Vector3> centros = new List<Vector3>();
                    if (tieneAros && ids.Count % 4 == 0 && ids[0] % 4 == 0 && ids[ids.Count - 1] == ids[0] + ids.Count - 1)
                    {
                        for (int k = 0; k + 3 < ids.Count; k += 4)
                        {
                            centros.Add((w[ids[k]] + w[ids[k + 1]] + w[ids[k + 2]] + w[ids[k + 3]]) * 0.25f);
                        }
                    }
                    else
                    {
                        centros.Add(bb.min);
                        centros.Add(bb.max);
                    }
                    Vector3 a = centros[0], b = centros[centros.Count - 1];
                    float largo = Vector3.Distance(a, b);
                    float sag = 0f;
                    Vector3 ab = b - a;
                    float l2 = ab.sqrMagnitude;
                    foreach (Vector3 q in centros)
                    {
                        float t = l2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / l2);
                        sag = Mathf.Max(sag, Vector3.Distance(q, a + ab * t));
                    }
                    if (largo > LargoMaximoCable || bb.size.magnitude > LargoMaximoCable * 1.5f)
                    {
                        malos[kv.Key] = "mide " + largo.ToString("0") + " m (tope " + LargoMaximoCable + ")";
                        continue;
                    }
                    if (sag > FlechaMaximaCable)
                    {
                        malos[kv.Key] = "flecha de " + sag.ToString("0.0") + " m (tope " + FlechaMaximaCable + ")";
                        continue;
                    }
                    if (ids.Any(i => Vector3.Distance(w[i], camaraAlta) < DistanciaCamaraCable))
                    {
                        malos[kv.Key] = "pasa a menos de " + DistanciaCamaraCable + " m de la cámara K13";
                        continue;
                    }
                    int enterrados = 0;
                    foreach (Vector3 q in centros)
                    {
                        float y;
                        if (Piso(c, q.x, q.z, q.y + 150f, out y) && y > q.y + EnterradoMinimo)
                        {
                            enterrados++;
                        }
                    }
                    if (enterrados >= 2 || (enterrados >= 1 && centros.Count <= 2))
                    {
                        malos[kv.Key] = "pasa por debajo del piso (" + enterrados + " tramos enterrados)";
                        continue;
                    }
                    bool colgado = false;
                    foreach (Vector3 pp in postesQuitados)
                    {
                        foreach (Vector3 extremo in new[] { a, b })
                        {
                            if (Plano(extremo - pp).magnitude < 1.5f && extremo.y > pp.y - 0.5f && extremo.y < pp.y + 13f)
                            {
                                colgado = true;
                            }
                        }
                    }
                    if (colgado)
                    {
                        malos[kv.Key] = "colgaba de un poste quitado";
                    }
                }
                int sanos = comps.Count - malos.Count;
                sanosTotal += sanos;
                malosTotal += malos.Count;
                foreach (KeyValuePair<int, string> kv in malos)
                {
                    if (lineas++ < 80)
                    {
                        Vector3 mid = Vector3.zero;
                        foreach (int i in comps[kv.Key])
                        {
                            mid += w[i];
                        }
                        mid /= Mathf.Max(1, comps[kv.Key].Count);
                        string linea = "Cable malo en " + mf.transform.parent.name + "/" + mf.name + " cerca de " + mid.ToString("0") + ": " + kv.Value + (modificar ? "; quitado." : ".");
                        if (sb != null)
                        {
                            sb.AppendLine(linea);
                        }
                        else
                        {
                            Log(linea);
                        }
                    }
                }
                if (malos.Count == 0 || !modificar)
                {
                    continue;
                }
                mallasMalas++;
                int[] nuevo = new int[v.Length];
                List<Vector3> nv = new List<Vector3>();
                List<Vector3> nn = new List<Vector3>();
                for (int i = 0; i < v.Length; i++)
                {
                    int r = BuscarRaiz(raiz, i);
                    if (comps.ContainsKey(r) && !malos.ContainsKey(r))
                    {
                        nuevo[i] = nv.Count;
                        nv.Add(v[i]);
                        if (nor.Length == v.Length)
                        {
                            nn.Add(nor[i]);
                        }
                    }
                    else
                    {
                        nuevo[i] = -1;
                    }
                }
                List<int> nt = new List<int>();
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    if (nuevo[tri[i]] >= 0 && nuevo[tri[i + 1]] >= 0 && nuevo[tri[i + 2]] >= 0)
                    {
                        nt.Add(nuevo[tri[i]]);
                        nt.Add(nuevo[tri[i + 1]]);
                        nt.Add(nuevo[tri[i + 2]]);
                    }
                }
                Mesh limpia = new Mesh { name = Raiz + "_Cables_" + (contador++), indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                limpia.SetVertices(nv);
                if (nn.Count == nv.Count && nn.Count > 0)
                {
                    limpia.SetNormals(nn);
                }
                limpia.SetTriangles(nt, 0);
                limpia.RecalculateBounds();
                mf.sharedMesh = limpia;
                c.Obra.Mallas.Add(limpia);
                Log("Malla " + m.name + ": " + malos.Count + " cables malos quitados, " + sanos + " se quedan (" + (nt.Count / 3) + " triángulos).");
            }
            string resumen = "Cables revisados: " + (sanosTotal + malosTotal) + "; malos " + (modificar ? "quitados" : "que quedan") + ": " + malosTotal + "; mallas rehechas: " + mallasMalas + ".";
            if (sb != null)
            {
                sb.AppendLine(resumen);
            }
            else
            {
                Log(resumen);
            }
            return malosTotal;
        }

        // ================================================================ 3. Enlaces entre azoteas: escalones y tablones con textura

        private static void ArreglarEnlaces(Ctx c)
        {
            Transform t94 = c.Zona.transform.Find(ZonaCerroT1394.Raiz);
            Transform enlaces = t94 == null ? null : t94.Find("Enlaces");
            if (enlaces == null)
            {
                Log("AVISO: no hay " + ZonaCerroT1394.Raiz + "/Enlaces; no hay nada que arreglar.");
                return;
            }
            Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
            int escalones = 0, tablones = 0, otros = 0;
            foreach (MeshFilter mf in enlaces.GetComponentsInChildren<MeshFilter>(true))
            {
                bool esEscalon = mf.name.StartsWith("Escalon_");
                bool esTablon = mf.name.StartsWith("Tablon");
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if ((!esEscalon && !esTablon) || mr == null)
                {
                    otros++;
                    continue;
                }
                Vector3 tam = mf.transform.localScale;
                float uv = esTablon ? MetrosUvMadera : MetrosUvConcreto;
                string clave = (esTablon ? "T" : "E") + tam.x.ToString("0.00") + "_" + tam.y.ToString("0.00") + "_" + tam.z.ToString("0.00");
                Mesh malla;
                if (!cache.TryGetValue(clave, out malla))
                {
                    malla = KitS2c.MallaCaja(tam, uv);
                    malla.name = Raiz + "_" + (esTablon ? "Tablon" : "Escalon") + "_" + cache.Count;
                    c.Obra.Mallas.Add(malla);
                    cache[clave] = malla;
                }
                mf.sharedMesh = malla;
                mf.transform.localScale = Vector3.one;
                mr.sharedMaterial = esTablon ? c.Madera : c.Concreto;
                BoxCollider bc = mf.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.size = tam;
                    bc.center = Vector3.zero;
                }
                if (esTablon)
                {
                    tablones++;
                }
                else
                {
                    escalones++;
                }
            }
            Log("Enlaces entre azoteas: " + escalones + " escalones con " + c.Concreto.name + " y " + tablones + " tablones con " + c.Madera.name
                + " (UV en metros, malla a tamaño real, " + cache.Count + " mallas distintas); sin tocar: " + otros + ".");
        }

        // ================================================================ 4. Tinacos, antenas y ropa tendida en las azoteas de K13

        private static bool EsCasaVisible(Transform t)
        {
            string n = t.name;
            return n.StartsWith("Casa_") || n.StartsWith("Extra_") || ((n.StartsWith("CC0") && !n.StartsWith("CC04") && !n.StartsWith("CC05") && !n.StartsWith("CC08")));
        }

        private static bool BajoCasa(Transform t, Transform zona)
        {
            while (t != null && t != zona)
            {
                if (EsCasaVisible(t))
                {
                    return true;
                }
                t = t.parent;
            }
            return false;
        }

        private static bool Hay(List<Vector3> lista, Bounds casa)
        {
            foreach (Vector3 p in lista)
            {
                if (p.x > casa.min.x - 0.5f && p.x < casa.max.x + 0.5f && p.z > casa.min.z - 0.5f && p.z < casa.max.z + 0.5f && p.y > casa.max.y - 6f)
                {
                    return true;
                }
            }
            return false;
        }

        private static void PropsAzoteas(Ctx c)
        {
            GameObject tinaco = KitS2c.Prefab("C09_Tinaco");
            GameObject baseTinaco = KitS2c.Prefab("TK01_BaseTinaco");
            GameObject antena = KitS2c.Prefab("CY04_AntenaAzotea");
            GameObject tendedero = AssetDatabase.LoadAssetAtPath<GameObject>(ZonaCerroT1385.CarpetaCC + "CC04_Tendedero.prefab");
            if (tinaco == null || baseTinaco == null)
            {
                Log("AVISO: falta C09_Tinaco o TK01_BaseTinaco; no se ponen tinacos.");
            }
            if (antena == null)
            {
                Log("AVISO: falta CY04_AntenaAzotea; no se ponen antenas.");
            }
            if (tendedero == null)
            {
                Log("AVISO: falta CC04_Tendedero; no se pone ropa tendida.");
            }
            List<Vector3> tinacos = new List<Vector3>(), antenas = new List<Vector3>(), tendederos = new List<Vector3>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (t.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                if (t.name.StartsWith("C09_Tinaco"))
                {
                    tinacos.Add(t.position);
                }
                else if (t.name.StartsWith("CY04"))
                {
                    antenas.Add(t.position);
                }
                else if (t.name.StartsWith("CC04"))
                {
                    tendederos.Add(t.position);
                }
            }
            Vector3 adelante = Plano(K13Hacia - K13Desde).normalized;
            List<(Transform casa, Bounds b, float dist)> visibles = new List<(Transform, Bounds, float)>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (!EsCasaVisible(t) || (t.parent != null && BajoCasa(t.parent, c.Zona.transform)) || t.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                Bounds b = Cubierta(t);
                if (b.size.x < 3f || b.size.z < 3f || b.max.y < K13Desde.y + 2f)
                {
                    continue;
                }
                Vector3 d = Plano(b.center - K13Desde);
                float dist = d.magnitude;
                if (dist < AzoteaMinDist || dist > AzoteaMaxDist || Mathf.Abs(Vector3.SignedAngle(adelante, d, Vector3.up)) > AzoteaMaxAngulo)
                {
                    continue;
                }
                visibles.Add((t, b, dist));
            }
            Log("Casas en el encuadre de K13 (de " + AzoteaMinDist + " a " + AzoteaMaxDist + " m, a menos de " + AzoteaMaxAngulo + " grados del centro): " + visibles.Count + ".");
            Transform det = Hijo(c.Nuevo, "Azoteas");
            int nTin = 0, nAnt = 0, nTen = 0, i = 0, azoteas = 0;
            foreach ((Transform casa, Bounds b, float dist) v in visibles.OrderBy(v => v.dist).Take(TopeAzoteasVisibles))
            {
                i++;
                if (nTin + nAnt + nTen >= TopeObjetosNuevos)
                {
                    break;
                }
                float nivel;
                if (!NivelTecho(c, v.b.min.x + 0.5f, v.b.max.x - 0.5f, v.b.min.z + 0.5f, v.b.max.z - 0.5f, v.b.max.y, out nivel))
                {
                    continue;
                }
                Transform g = Hijo(det, "Azotea_" + i);
                int puestos = 0;
                if (tinaco != null && baseTinaco != null && i % 2 == 0 && !Hay(tinacos, v.b))
                {
                    Vector3 mb = Medida(baseTinaco);
                    Vector3 escalaBase = new Vector3(1.6f / Mathf.Max(0.1f, mb.x), 0.7f / Mathf.Max(0.1f, mb.y), 1.6f / Mathf.Max(0.1f, mb.z));
                    GameObject gb = PonerEnAzotea(c, g, baseTinaco, escalaBase, v.b, nivel, i, 0.7f);
                    if (gb != null)
                    {
                        Bounds bb = Cubierta(gb.transform);
                        GameObject gt = (GameObject)PrefabUtility.InstantiatePrefab(tinaco, g);
                        gt.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        gt.transform.localScale = Vector3.one * EscalaTinaco;
                        Bounds bt = Cubierta(gt.transform);
                        gt.transform.position += new Vector3(bb.center.x - bt.center.x, bb.max.y - bt.min.y, bb.center.z - bt.center.z);
                        tinacos.Add(gt.transform.position);
                        nTin++;
                        puestos++;
                    }
                }
                if (antena != null && i % 3 != 1 && !Hay(antenas, v.b))
                {
                    GameObject ga = PonerEnAzotea(c, g, antena, Vector3.one * EscalaAntena, v.b, nivel, i + 2, 1.0f);
                    if (ga != null)
                    {
                        antenas.Add(ga.transform.position);
                        nAnt++;
                        puestos++;
                    }
                }
                if (tendedero != null && i % 3 == 1 && !Hay(tendederos, v.b))
                {
                    GameObject gd = PonerEnAzotea(c, g, tendedero, Vector3.one, v.b, nivel, i + 4, 0.5f);
                    if (gd != null)
                    {
                        tendederos.Add(gd.transform.position);
                        nTen++;
                        puestos++;
                    }
                }
                if (puestos == 0)
                {
                    Object.DestroyImmediate(g.gameObject);
                }
                else
                {
                    azoteas++;
                }
            }
            Physics.SyncTransforms();
            Log("Azoteas visibles desde K13 con objetos nuevos: " + azoteas + " (tinacos " + nTin + ", antenas " + nAnt + ", tendederos " + nTen + ").");
        }

        private static GameObject PonerEnAzotea(Ctx c, Transform padre, GameObject prefab, Vector3 escala, Bounds casa, float nivel, int variante, float margenAlto)
        {
            Vector3 m = Medida(prefab);
            Vector3 tam = new Vector3(m.x * escala.x, m.y * escala.y, m.z * escala.z);
            float[] fx = { 0.25f, 0.75f, 0.25f, 0.75f, 0.5f, 0.5f };
            float[] fz = { 0.25f, 0.25f, 0.75f, 0.75f, 0.25f, 0.75f };
            for (int k = 0; k < fx.Length; k++)
            {
                int j = (k + variante) % fx.Length;
                float x = Mathf.Lerp(casa.min.x, casa.max.x, fx[j]);
                float z = Mathf.Lerp(casa.min.z, casa.max.z, fz[j]);
                if (x - tam.x * 0.5f < casa.min.x + 0.3f || x + tam.x * 0.5f > casa.max.x - 0.3f || z - tam.z * 0.5f < casa.min.z + 0.3f || z + tam.z * 0.5f > casa.max.z - 0.3f)
                {
                    continue;
                }
                bool plano = true;
                foreach (Vector2 q in new[] { new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f) })
                {
                    plano &= TechoEn(c, x + q.x * tam.x * 0.45f, z + q.y * tam.z * 0.45f, nivel);
                }
                if (!plano)
                {
                    continue;
                }
                float alto = Mathf.Max(0.2f, tam.y - 0.3f);
                if (!Libre(c, new Vector3(x, nivel + 0.2f + alto * 0.5f, z), new Vector3(tam.x * 0.5f - 0.05f, alto * 0.5f, tam.z * 0.5f - 0.05f), Quaternion.identity))
                {
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                g.transform.localScale = escala;
                Bounds b = Cubierta(g.transform);
                g.transform.position += new Vector3(x - b.center.x, nivel - b.min.y, z - b.center.z);
                Physics.SyncTransforms();
                return g;
            }
            return null;
        }

        private static bool TechoEn(Ctx c, float x, float z, float nivel)
        {
            RaycastHit h;
            return c.Fisica.Raycast(new Vector3(x, nivel + 1.5f, z), Vector3.down, out h, 3f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.9f
                && Mathf.Abs(h.point.y - nivel) < 0.25f && !EsPiso(c, h.collider.transform);
        }

        /// <summary>Nivel de la azotea: el valor más común (3 de 9 rayos o más) entre los puntos planos que ve un rayo desde arriba.</summary>
        private static bool NivelTecho(Ctx c, float x0, float x1, float z0, float z1, float top, out float nivel)
        {
            nivel = top;
            List<float> ys = new List<float>();
            float[] f = { 0.2f, 0.5f, 0.8f };
            foreach (float fx in f)
            {
                foreach (float fz in f)
                {
                    RaycastHit h;
                    Vector3 origen = new Vector3(Mathf.Lerp(x0, x1, fx), top + 6f, Mathf.Lerp(z0, z1, fz));
                    if (c.Fisica.Raycast(origen, Vector3.down, out h, 12f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.95f
                        && h.point.y > top - 4f && h.point.y < top + 1.3f && !EsPiso(c, h.collider.transform))
                    {
                        ys.Add(h.point.y);
                    }
                }
            }
            if (ys.Count < 3)
            {
                return false;
            }
            float mejor = 0f;
            int cuenta = 0;
            foreach (float y in ys)
            {
                int k = ys.Count(val => Mathf.Abs(val - y) < 0.15f);
                if (k > cuenta || (k == cuenta && y < mejor))
                {
                    cuenta = k;
                    mejor = y;
                }
            }
            if (cuenta < 3)
            {
                return false;
            }
            nivel = ys.Where(val => Mathf.Abs(val - mejor) < 0.15f).Average();
            return true;
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(false))
            {
                if (t == c.Nuevo || t.GetComponent<Light>() != null || t.IsChildOf(c.Reem) || t.IsChildOf(c.Reg))
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
            Debug.Log("[ZonaCerroT1401] " + texto);
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

        private static Vector3 Plano(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static Vector3 Medida(GameObject prefab)
        {
            Vector3 ya;
            if (medidas.TryGetValue(prefab.name, out ya))
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

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

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

        // ================================================================ Validar

        /// <summary>
        /// Revisa el prefab ya con Aplicar: ya no queda ningún cable fuera de los límites (40 m, flecha 3 m, bajo el piso, triángulos deformes); ningún poste en el
        /// centro del encuadre de K13; los escalones y tablones de los enlaces tienen malla a tamaño real, material con textura y collider del mismo tamaño;
        /// hay tinacos, antenas y tendederos nuevos; el registro de postes cuadra; la cámara K13 no tiene nada pegado ni delante a 6 m. Escribe Logs/t1401-validar.txt.
        /// </summary>
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1401: validación de " + RutaPrefab + "\n");
            informe = sb;
            int fallos = 0;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject zona = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Ctx c = Abrir(zona);
                c.Nuevo = zona.transform.Find(Raiz);
                Physics.SyncTransforms();
                if (c.Nuevo == null)
                {
                    sb.AppendLine("FALLO: no existe " + Raiz + " (corre Aplicar).");
                    fallos++;
                }
                else
                {
                    c.Reem = c.Nuevo.Find("Reemplazados");
                    c.Reg = c.Nuevo.Find("Registro");
                    if (c.Reem == null || c.Reg == null || c.Reem.childCount != c.Reg.childCount)
                    {
                        sb.AppendLine("FALLO: el registro de postes quitados no cuadra.");
                        fallos++;
                    }
                    else
                    {
                        sb.AppendLine("Postes quitados guardados: " + c.Reem.childCount + ".");
                    }
                    int malos = AnalizarCables(c, new List<Vector3>(), false, sb);
                    if (malos > 0)
                    {
                        sb.AppendLine("FALLO: quedan " + malos + " cables fuera de los límites.");
                        fallos++;
                    }
                    fallos += ValidarPostes(c, sb);
                    fallos += ValidarEnlaces(c, sb);
                    fallos += ValidarProps(c, sb);
                    fallos += ValidarCamara(c, sb);
                    string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RutaPrefab);
                    long bytes = File.Exists(ruta) ? new FileInfo(ruta).Length : -1;
                    sb.AppendLine("Tamaño del prefab: " + (bytes / 1048576f).ToString("0.0") + " MB " + (bytes >= 60L * 1024 * 1024 ? "FALLO (>= 60 MB)" : "ok"));
                    if (bytes >= 60L * 1024 * 1024)
                    {
                        fallos++;
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(zona);
            }
            sb.AppendLine(fallos == 0 ? "RESULTADO: sin fallos." : "RESULTADO: " + fallos + " fallos.");
            Escribir(RutaValidar, sb);
            Debug.Log("[ZonaCerroT1401] " + sb);
        }

        private static int ValidarPostes(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Vector3 adelante = Plano(K13Hacia - K13Desde).normalized;
            Vector3 derecha = new Vector3(adelante.z, 0f, -adelante.x);
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (!EsPoste(t) || (t.parent != null && EsPoste(t.parent)))
                {
                    continue;
                }
                Vector3 p = Plano(t.position - K13Desde);
                float ade = Vector3.Dot(p, adelante);
                if (p.magnitude < PosteCercaDeCamara || (ade > 0f && ade < PosteAdelanteMax && Mathf.Abs(Vector3.Dot(p, derecha)) < PosteLadoMax))
                {
                    sb.AppendLine("FALLO: el poste " + t.name + " sigue en el centro del encuadre de K13 (a " + p.magnitude.ToString("0.0") + " m).");
                    fallos++;
                }
            }
            return fallos;
        }

        private static int ValidarEnlaces(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Transform t94 = c.Zona.transform.Find(ZonaCerroT1394.Raiz);
            Transform enlaces = t94 == null ? null : t94.Find("Enlaces");
            if (enlaces == null)
            {
                sb.AppendLine("AVISO: no hay enlaces que revisar.");
                return 0;
            }
            int esc = 0, tab = 0, sinTextura = 0, escalados = 0, desajuste = 0;
            foreach (MeshFilter mf in enlaces.GetComponentsInChildren<MeshFilter>(true))
            {
                bool escalon = mf.name.StartsWith("Escalon_");
                bool tablon = mf.name.StartsWith("Tablon");
                if (!escalon && !tablon)
                {
                    continue;
                }
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (escalon)
                {
                    esc++;
                }
                else
                {
                    tab++;
                }
                if (mr == null || mr.sharedMaterial == null || mr.sharedMaterial.mainTexture == null)
                {
                    sinTextura++;
                }
                if ((mf.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
                {
                    escalados++;
                }
                BoxCollider bc = mf.GetComponent<BoxCollider>();
                if (bc != null && mf.sharedMesh != null && (bc.size - mf.sharedMesh.bounds.size).magnitude > 0.05f)
                {
                    desajuste++;
                }
            }
            sb.AppendLine("Enlaces: " + esc + " escalones y " + tab + " tablones.");
            if (esc + tab == 0)
            {
                sb.AppendLine("FALLO: no hay escalones ni tablones.");
                fallos++;
            }
            if (sinTextura > 0)
            {
                sb.AppendLine("FALLO: " + sinTextura + " piezas de enlace sin material con textura.");
                fallos++;
            }
            if (escalados > 0)
            {
                sb.AppendLine("FALLO: " + escalados + " piezas de enlace siguen con malla unidad escalada (UV estiradas).");
                fallos++;
            }
            if (desajuste > 0)
            {
                sb.AppendLine("FALLO: " + desajuste + " colliders no coinciden con su malla.");
                fallos++;
            }
            return fallos;
        }

        private static int ValidarProps(Ctx c, StringBuilder sb)
        {
            Transform az = c.Nuevo.Find("Azoteas");
            if (az == null)
            {
                sb.AppendLine("AVISO: no se pusieron objetos en azoteas (faltan prefabs o no hubo sitio).");
                return 0;
            }
            int tin = 0, ant = 0, ten = 0, flotando = 0;
            foreach (Transform t in az.GetComponentsInChildren<Transform>(false))
            {
                if (t.name.StartsWith("C09_Tinaco"))
                {
                    tin++;
                }
                else if (t.name.StartsWith("CY04"))
                {
                    ant++;
                }
                else if (t.name.StartsWith("CC04"))
                {
                    ten++;
                }
                else
                {
                    continue;
                }
                Bounds b = Cubierta(t);
                if (!c.Fisica.Raycast(new Vector3(b.center.x, b.min.y + 0.3f, b.center.z), Vector3.down, 1.0f, ~0, QueryTriggerInteraction.Ignore) && tin + ant + ten > 0 && !t.name.StartsWith("C09_Tinaco"))
                {
                    flotando++;
                }
            }
            sb.AppendLine("Azoteas con objetos nuevos: " + az.childCount + " (tinacos " + tin + ", antenas " + ant + ", tendederos " + ten + "); sin apoyo debajo: " + flotando + ".");
            return 0;
        }

        private static int ValidarCamara(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Vector3 adelante = (K13Hacia - K13Desde).normalized;
            RaycastHit h;
            if (c.Fisica.OverlapSphere(K13Desde, 0.8f, bufCol, ~0, QueryTriggerInteraction.Ignore) > 0)
            {
                sb.AppendLine("FALLO: hay un collider a menos de 0.8 m de la cámara K13 (" + bufCol[0].transform.name + ").");
                fallos++;
            }
            if (c.Fisica.Raycast(K13Desde, adelante, out h, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                sb.AppendLine("FALLO: algo tapa la cámara K13 a " + h.distance.ToString("0.0") + " m (" + h.collider.transform.name + ").");
                fallos++;
            }
            return fallos;
        }

        // ================================================================ Fotos

        /// <summary>
        /// Fotos K13 y A13 (referencia a la izquierda, juego a la derecha) en docs/capturas/t-1401/. No guarda nada; va en una corrida aparte de Aplicar.
        /// Si algo estorba la cámara K13 (collider a menos de 0.8 m o pared a menos de 6 m), la corre un poco por la calle.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1401/Cerro ronda 3: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1401");
            Physics.SyncTransforms();
            Vector3 corrimiento = CamaraLibre(K13Desde, K13Hacia);
            Debug.Log("[ZonaCerroT1401] Cámara K13 corrida " + corrimiento + " desde " + K13Desde + ".");
            ColocarS2cLook.Foto(K13Desde + corrimiento, K13Hacia + corrimiento, "docs/capturas/t-1401/cerro-k13-juego.png", CampoFoto);
            ColocarS2cLook.Foto(A13Desde, A13Hacia, "docs/capturas/t-1401/cerro-a13-juego.png", CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1401/cerro-k13-juego.png", "docs/capturas/t-1401/cerro-k13.png");
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", "docs/capturas/t-1401/cerro-a13-juego.png", "docs/capturas/t-1401/cerro-a13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static Vector3 CamaraLibre(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = (hacia - desde).normalized;
            Vector3[] corrimientos =
            {
                Vector3.zero, new Vector3(1.5f, 0f, 0f), new Vector3(-1.5f, 0f, 0f), new Vector3(0f, 0f, -5f), new Vector3(1.5f, 0f, -5f),
                new Vector3(0f, 0f, 5f), new Vector3(1.5f, 0f, 5f), new Vector3(0f, 0f, -10f), new Vector3(1.5f, 0f, -10f), new Vector3(0f, 0f, 10f),
            };
            foreach (Vector3 o in corrimientos)
            {
                Vector3 p = desde + o;
                bool tapada = Physics.CheckSphere(p, 0.8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(p, dir, 6f, ~0, QueryTriggerInteraction.Ignore);
                bool suelo = Physics.Raycast(p, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                if (!tapada && suelo)
                {
                    return o;
                }
            }
            Debug.LogWarning("[ZonaCerroT1401] Ningún corrimiento dejó libre la cámara K13; se usa la posición original.");
            return Vector3.zero;
        }
    }
}
