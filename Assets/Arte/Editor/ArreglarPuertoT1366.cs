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
    /// T-1366: arregla el prefab del Puerto (T1357_Puerto) según el barrido (T-1360) y los recorridos (T-1361) SIN rehacerlo con
    /// ColocarT1357Puerto: abre el prefab con LoadPrefabContents y lo guarda. Idempotente: cada paso calcula el estado final absoluto.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ArreglarPuertoT1366.Aplicar (y .FotosAntes / .FotosDespues).
    /// </summary>
    public static class ArreglarPuertoT1366
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string RutaInforme = "Logs/t1366-arreglos.txt";
        public const string CarpetaFotos = "docs/capturas/bugs-18nov-B";

        // Asfalto de las calles: cada Tramo sube 3 mm sobre el anterior para que ningún par comparta plano; las rayas van por encima de todos.
        public const float TopeAsfaltoBase = 0.08f, PasoAsfalto = 0.003f, YRayasBase = 0.11f, PasoRayas = 0.003f;
        // Los postes dejan de ser una columna de 7 m donde alguien "cabe parado" sin salida: el collider solo llega a esta altura (m).
        public const float AlturaColliderPoste = 2.5f;
        // Cualquier prop con collider (pallet, tambo, montacargas) queda al menos a esta distancia (m) de la línea central de una calle.
        public const float DespejeCalle = 2.8f;
        // Contenedor separado de otro de la misma fila a lo más esto (m); los de una pila quedan apoyados con esta tolerancia.
        public const float HuecoFilaMaximo = 0.3f, ToleranciaApoyo = 0.02f;
        // El barco queda a 1 a 3 m del muelle (m).
        public const float HuecoBarcoMin = 1f, HuecoBarcoMax = 3f, YMuelle = -1f, YMar = -3f, MuelleSur = -250f;
        public const float DesplazamientoEscaleraFila = -2f;

        private static readonly string[] PropsConCollider = { "PU06_Pallets", "C17_Huacales", "C16_Tambo", "PU05_Montacargas" };
        private static readonly string[] NombresBodegas = { "PU03_Bodega", "PU04_Bodega" };
        private static readonly StringBuilder informe = new StringBuilder();

        // ---------------------------------------------------------------- Lógica de medida (sin física; sirve a las pruebas)

        /// <summary>Caja en mundo de los renderers de una raíz, calculada con las mallas (no necesita escena ni física).</summary>
        public static Bounds LimitesRenderers(Transform raiz)
        {
            bool primero = true;
            Bounds total = new Bounds(raiz.position, Vector3.zero);
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null)
                {
                    continue;
                }
                Bounds b = CajaEnMundo(mf.transform, mf.sharedMesh.bounds);
                if (primero)
                {
                    total = b;
                    primero = false;
                }
                else
                {
                    total.Encapsulate(b);
                }
            }
            return total;
        }

        /// <summary>Caja en mundo de un collider de caja o de malla (sin usar Collider.bounds, que en un prefab sin escena sale vacío).</summary>
        public static bool LimitesCollider(Collider c, out Bounds caja)
        {
            if (c is BoxCollider bc)
            {
                caja = CajaEnMundo(c.transform, new Bounds(bc.center, bc.size));
                return true;
            }
            if (c is MeshCollider mc && mc.sharedMesh != null)
            {
                caja = CajaEnMundo(c.transform, mc.sharedMesh.bounds);
                return true;
            }
            caja = default;
            return false;
        }

        public static Bounds CajaEnMundo(Transform t, Bounds local)
        {
            Vector3 e = local.extents;
            Bounds r = new Bounds(t.TransformPoint(local.center + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                Vector3 s = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                r.Encapsulate(t.TransformPoint(local.center + s));
            }
            return r;
        }

        /// <summary>Pilas de contenedores de un bloque: columna (0 oeste, 1 este), fila (z) y los contenedores de abajo hacia arriba.</summary>
        public sealed class Pila
        {
            public int Columna;
            public float Z;
            public List<Transform> Contenedores = new List<Transform>();
        }

        public static List<Pila> PilasDe(Transform bloque)
        {
            List<Transform> todos = bloque.Cast<Transform>().Where(t => t.name.StartsWith("PT01_Contenedor")).ToList();
            if (todos.Count == 0)
            {
                return new List<Pila>();
            }
            List<(Transform t, Bounds b)> cajas = todos.Select(t => (t, LimitesRenderers(t))).ToList();
            float xMedio = cajas.Average(c => c.b.center.x);
            List<Pila> pilas = new List<Pila>();
            foreach (var c in cajas.OrderBy(c => c.b.center.z).ThenBy(c => c.b.center.y))
            {
                int col = c.b.center.x >= xMedio ? 1 : 0;
                Pila p = pilas.FirstOrDefault(q => q.Columna == col && Mathf.Abs(q.Z - c.b.center.z) < 0.5f);
                if (p == null)
                {
                    p = new Pila { Columna = col, Z = c.b.center.z };
                    pilas.Add(p);
                }
                p.Contenedores.Add(c.t);
            }
            foreach (Pila p in pilas)
            {
                p.Contenedores = p.Contenedores.OrderBy(t => LimitesRenderers(t).center.y).ToList();
            }
            return pilas.OrderBy(p => p.Z).ThenBy(p => p.Columna).ToList();
        }

        // ---------------------------------------------------------------- Aplicar

        public static void Aplicar()
        {
            informe.Clear();
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                informe.AppendLine("T-1366 arreglos del Puerto");
                Contenedores(raiz.transform);
                Escaleras(raiz.transform);
                GruasYBarco(raiz.transform);
                Calles(raiz.transform);
                Postes(raiz.transform);
                PropsSueltos(raiz.transform);
                Bodegas(raiz.transform);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Barrido();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[ArreglarPuertoT1366] " + informe);
        }

        // 1 y 2: pilas apoyadas y alineadas, filas juntas, colliders de pila sincronizados, LOD recalculado.
        private static void Contenedores(Transform raiz)
        {
            Transform padre = raiz.Find("Contenedores");
            if (padre == null)
            {
                informe.AppendLine("Contenedores: no hay grupo Contenedores");
                return;
            }
            int movidos = 0, apagados = 0, bloques = 0;
            float huecoMaximo = 0f;
            foreach (Transform bloque in padre)
            {
                if (!bloque.name.StartsWith("Bloque_"))
                {
                    continue;
                }
                bloques++;
                List<Pila> pilas = PilasDe(bloque);
                foreach (Pila pila in pilas)
                {
                    Bounds abajo = LimitesRenderers(pila.Contenedores[0]);
                    float baseY = 0f; // el patio tiene su cara superior en y = 0
                    float techo = baseY;
                    for (int k = 0; k < pila.Contenedores.Count; k++)
                    {
                        Transform t = pila.Contenedores[k];
                        Bounds b = LimitesRenderers(t);
                        Vector3 d = new Vector3(k == 0 ? 0f : abajo.center.x - b.center.x, techo - b.min.y, k == 0 ? 0f : abajo.center.z - b.center.z);
                        if (d.magnitude > 0.001f)
                        {
                            t.position += d;
                            movidos++;
                        }
                        techo = LimitesRenderers(t).max.y;
                        // Los de abajo no necesitan collider propio: lo cubre el de la pila del bloque (evita colliders encimados).
                        bool cubierto = k < pila.Contenedores.Count - 1;
                        foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
                        {
                            if (c.enabled == cubierto)
                            {
                                c.enabled = !cubierto;
                                apagados += cubierto ? 1 : 0;
                            }
                        }
                    }
                }
                // Huecos entre filas contiguas (mismo bloque y columna).
                foreach (IGrouping<int, Pila> col in pilas.GroupBy(p => p.Columna))
                {
                    List<Pila> orden = col.OrderBy(p => p.Z).ToList();
                    for (int i = 1; i < orden.Count; i++)
                    {
                        Bounds a = LimitesRenderers(orden[i - 1].Contenedores[0]), b = LimitesRenderers(orden[i].Contenedores[0]);
                        huecoMaximo = Mathf.Max(huecoMaximo, b.min.z - a.max.z);
                    }
                }
                SincronizarColliders(bloque, pilas);
                RecalcularLod(bloque, pilas);
            }
            informe.AppendLine("Contenedores: " + bloques + " bloques; " + movidos + " contenedores reapoyados o alineados; " + apagados
                + " colliders de contenedores tapados apagados (los cubre el de su pila); mayor hueco entre filas contiguas " + huecoMaximo.ToString("F3") + " m");
        }

        // Un BoxCollider por pila (fila x columna) en el propio Bloque, del tamaño de la pila.
        private static void SincronizarColliders(Transform bloque, List<Pila> pilas)
        {
            List<BoxCollider> cajas = bloque.GetComponents<BoxCollider>().ToList();
            for (int i = 0; i < pilas.Count; i++)
            {
                Bounds b = LimitesRenderers(pilas[i].Contenedores[0]);
                for (int k = 1; k < pilas[i].Contenedores.Count; k++)
                {
                    b.Encapsulate(LimitesRenderers(pilas[i].Contenedores[k]));
                }
                BoxCollider bc = i < cajas.Count ? cajas[i] : bloque.gameObject.AddComponent<BoxCollider>();
                bc.center = bloque.InverseTransformPoint(b.center);
                bc.size = b.size;
            }
            for (int i = pilas.Count; i < cajas.Count; i++)
            {
                Object.DestroyImmediate(cajas[i], true);
            }
        }

        private static void RecalcularLod(Transform bloque, List<Pila> pilas)
        {
            LODGroup lg = bloque.GetComponent<LODGroup>();
            Renderer[] rend = pilas.SelectMany(p => p.Contenedores).SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).ToArray();
            if (rend.Length == 0)
            {
                return;
            }
            if (lg == null)
            {
                lg = bloque.gameObject.AddComponent<LODGroup>();
            }
            lg.SetLODs(new[] { new LOD(0.012f, rend) });
            lg.RecalculateBounds();
        }

        // 3: cada bloque se sube desde el suelo (Escalera_Suelo) y cada subida de fila tiene su Escalera_Fila, más hacia afuera del centro.
        private static void Escaleras(Transform raiz)
        {
            Transform padre = raiz.Find("Contenedores");
            int bloques = 0, sinSuelo = 0, filasSinEscalera = 0, movidas = 0;
            foreach (Transform bloque in padre)
            {
                if (!bloque.name.StartsWith("Bloque_"))
                {
                    continue;
                }
                bloques++;
                List<Pila> pilas = PilasDe(bloque);
                PuntoTrepar suelo = bloque.GetComponentsInChildren<PuntoTrepar>(true).FirstOrDefault(p => p.name.StartsWith("Escalera_Suelo"));
                if (suelo == null)
                {
                    sinSuelo++;
                }
                float xOeste = pilas.Where(p => p.Columna == 0).Select(p => LimitesRenderers(p.Contenedores[0]).center.x).DefaultIfEmpty(0f).Average();
                foreach (PuntoTrepar fila in bloque.GetComponentsInChildren<PuntoTrepar>(true).Where(p => p.name.StartsWith("Escalera_Fila")))
                {
                    Vector3 p = fila.transform.position;
                    float x = xOeste + DesplazamientoEscaleraFila;
                    if (Mathf.Abs(p.x - x) > 0.001f)
                    {
                        fila.transform.position = new Vector3(x, p.y, p.z);
                        movidas++;
                    }
                }
                // Cada fila con más pisos que la anterior necesita una Escalera_Fila a la altura de la anterior.
                List<float> alturas = pilas.Where(p => p.Columna == 0).OrderBy(p => p.Z).Select(p => LimitesRenderers(p.Contenedores[p.Contenedores.Count - 1]).max.y).ToList();
                for (int f = 0; f + 1 < alturas.Count; f++)
                {
                    if (alturas[f + 1] - alturas[f] > 0.5f
                        && !bloque.GetComponentsInChildren<PuntoTrepar>(true).Any(p => p.name.StartsWith("Escalera_Fila") && Mathf.Abs(p.transform.position.y - alturas[f]) < 0.8f))
                    {
                        filasSinEscalera++;
                    }
                }
            }
            informe.AppendLine("Escaleras: " + bloques + " bloques, " + sinSuelo + " sin Escalera_Suelo, " + filasSinEscalera + " subidas de fila sin Escalera_Fila, "
                + movidas + " Escalera_Fila movidas hacia el borde oeste (para que 'bajar caminando hacia afuera' alcance el vacío).");
            informe.AppendLine("Escalera visual: no hay modelo de escalera de mano de 2.6 m (L03_EscaleraEmergencia mide 4.5 x 3.5 x 4.8 m y taparía el pasillo): solo PuntoTrepar, ver docs/assets-faltan.md.");
        }

        // 4: colliders de grúas y barco, patas sobre el muelle, sin bitas debajo, barco flotando y separado.
        private static void GruasYBarco(Transform raiz)
        {
            Transform muelle = raiz.Find("Muelle");
            if (muelle == null)
            {
                informe.AppendLine("Muelle: no hay grupo Muelle");
                return;
            }
            List<Transform> gruas = muelle.Cast<Transform>().Where(t => t.name.StartsWith("PU01_Grua")).ToList();
            List<Transform> bitas = muelle.Cast<Transform>().Where(t => t.name.StartsWith("PT02_BitaAmarre")).ToList();
            int ajustadasY = 0, bitasMovidas = 0;
            foreach (Transform g in gruas)
            {
                ActivarColliders(g);
                float minY = LimitesRenderers(g).min.y;
                if (Mathf.Abs(minY - YMuelle) > 0.005f)
                {
                    g.position += Vector3.up * (YMuelle - minY);
                    ajustadasY++;
                }
                List<Vector3> bajos = VerticesBajos(g, YMuelle + 1.5f);
                foreach (Transform bita in bitas)
                {
                    Vector3 p = bita.position;
                    int intentos = 0;
                    while (bajos.Any(v => (new Vector2(v.x, v.z) - new Vector2(p.x, p.z)).sqrMagnitude < 1f) && intentos++ < 40)
                    {
                        p.x += 0.5f;
                    }
                    if (intentos > 0)
                    {
                        bita.position = p;
                        bitasMovidas++;
                    }
                }
            }
            Transform barco = muelle.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("PU02_Barco"));
            if (barco != null)
            {
                ActivarColliders(barco);
                Bounds b = LimitesRenderers(barco);
                float hueco = MuelleSur - b.max.z;
                if (hueco < HuecoBarcoMin || hueco > HuecoBarcoMax)
                {
                    barco.position += Vector3.forward * ((MuelleSur - (HuecoBarcoMin + HuecoBarcoMax) * 0.5f) - b.max.z);
                }
                b = LimitesRenderers(barco);
                informe.AppendLine("Barco: collider de malla activo; quilla en y " + b.min.y.ToString("F2") + ", cubierta en y " + b.max.y.ToString("F2")
                    + " (mar en y " + YMar + "); hueco al muelle " + (MuelleSur - b.max.z).ToString("F2") + " m");
            }
            informe.AppendLine("Grúas: " + gruas.Count + " con collider de malla; " + ajustadasY + " bajadas/subidas hasta pisar el muelle (y " + YMuelle + "); " + bitasMovidas + " bitas movidas fuera de las patas");
        }

        private static List<Vector3> VerticesBajos(Transform raiz, float yMaxima)
        {
            List<Vector3> res = new List<Vector3>();
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null)
                {
                    continue;
                }
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    Vector3 w = mf.transform.TransformPoint(v);
                    if (w.y <= yMaxima)
                    {
                        res.Add(w);
                    }
                }
            }
            return res;
        }

        private static void ActivarColliders(Transform t)
        {
            foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
            {
                if (!c.enabled)
                {
                    c.enabled = true;
                }
            }
        }

        // 6: Tramo y Linea sin planos repetidos.
        private static void Calles(Transform raiz)
        {
            int tramo = 0, rayas = 0, n = 0;
            foreach (Transform calle in raiz)
            {
                if (!calle.name.StartsWith("Calle_"))
                {
                    continue;
                }
                n++;
                foreach (Transform t in calle)
                {
                    Vector3 p = t.position;
                    if (t.name == "Tramo")
                    {
                        p.y = TopeAsfaltoBase + PasoAsfalto * tramo - 0.04f;
                        tramo++;
                    }
                    else if (t.name == "Linea")
                    {
                        p.y = YRayasBase + PasoRayas * (n - 1);
                        rayas++;
                    }
                    else
                    {
                        continue;
                    }
                    t.position = p;
                }
            }
            informe.AppendLine("Calles: " + tramo + " Tramo a alturas distintas (cada uno 3 mm sobre el anterior), " + rayas + " rayas sobre todos los asfaltos");
            informe.AppendLine("Calle_4 y demas CalleZona: sin cambios (sus extremos son uniones con otras zonas). El atoro del carro en (164.1, 0, -166) era de la prueba de circuito, que une calles con una recta por los bloques.");
        }

        // 8: los postes ya no son una columna de 7 m con la punta "parada" sin salida.
        private static void Postes(Transform raiz)
        {
            Transform padre = raiz.Find("Postes");
            if (padre == null)
            {
                return;
            }
            int cortados = 0;
            foreach (BoxCollider bc in padre.GetComponentsInChildren<BoxCollider>(true))
            {
                Bounds w = CajaEnMundo(bc.transform, new Bounds(bc.center, bc.size));
                if (w.size.y <= AlturaColliderPoste + 0.01f)
                {
                    continue;
                }
                float ys = Mathf.Abs(bc.transform.lossyScale.y) > 1e-4f ? Mathf.Abs(bc.transform.lossyScale.y) : 1f;
                float nueva = AlturaColliderPoste / ys;
                float baseLocal = bc.center.y - bc.size.y * 0.5f;
                bc.size = new Vector3(bc.size.x, nueva, bc.size.z);
                bc.center = new Vector3(bc.center.x, baseLocal + nueva * 0.5f, bc.center.z);
                cortados++;
            }
            informe.AppendLine("Postes: " + cortados + " colliders recortados a " + AlturaColliderPoste + " m (antes la punta a 7 m era un sitio donde cabe parado sin salida).");
        }

        // 5 y 7: colliders de pallets, huacales, tambos y montacargas, fuera del eje de las calles.
        private static void PropsSueltos(Transform raiz)
        {
            List<Vector3[]> ejes = raiz.GetComponentsInChildren<CalleZona>(true).Select(c => c.Puntos().ToArray()).ToList();
            int activos = 0, movidos = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (!PropsConCollider.Any(n => t.name.StartsWith(n)))
                {
                    continue;
                }
                if (PrefabUtility.GetNearestPrefabInstanceRoot(t) != t.gameObject)
                {
                    continue;
                }
                Bounds b = LimitesRenderers(t);
                float centro = Mathf.Min(ejes.Select(e => DistanciaPlanoACalle(b.center, e)).DefaultIfEmpty(100f).Min());
                float medio = Mathf.Max(b.extents.x, b.extents.z);
                if (centro < DespejeCalle + medio)
                {
                    Vector3 dir = DireccionHaciaAfuera(b.center, ejes);
                    t.position += dir * (DespejeCalle + medio + 0.2f - centro);
                    movidos++;
                }
                ActivarColliders(t);
                activos++;
            }
            informe.AppendLine("Pallets, huacales, tambos y montacargas con collider: " + activos + " (" + movidos + " apartados del eje de una calle para no frenar carros)");
        }

        private static Vector3 DireccionHaciaAfuera(Vector3 p, List<Vector3[]> ejes)
        {
            float mejor = float.MaxValue;
            Vector3 dir = Vector3.right;
            foreach (Vector3[] e in ejes)
            {
                for (int i = 1; i < e.Length; i++)
                {
                    Vector2 a = new Vector2(e[i - 1].x, e[i - 1].z), bb = new Vector2(e[i].x, e[i].z), q = new Vector2(p.x, p.z);
                    Vector2 d = bb - a;
                    float k = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, d) / d.sqrMagnitude);
                    Vector2 cerca = a + d * k;
                    float dist = Vector2.Distance(q, cerca);
                    if (dist < mejor)
                    {
                        mejor = dist;
                        Vector2 fuera = q - cerca;
                        dir = fuera.sqrMagnitude < 1e-6f ? new Vector3(1f, 0f, 0f) : new Vector3(fuera.x, 0f, fuera.y).normalized;
                    }
                }
            }
            return dir;
        }

        public static float DistanciaPlanoACalle(Vector3 p, Vector3[] calle)
        {
            float m = float.MaxValue;
            Vector2 q = new Vector2(p.x, p.z);
            for (int i = 1; i < calle.Length; i++)
            {
                Vector2 a = new Vector2(calle[i - 1].x, calle[i - 1].z), b = new Vector2(calle[i].x, calle[i].z);
                Vector2 d = b - a;
                float k = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, d) / d.sqrMagnitude);
                m = Mathf.Min(m, Vector2.Distance(q, a + d * k));
            }
            return m;
        }

        // 5 y 9: bodegas con collider de malla, y más pallets y tambos en sus puertas (look de K15).
        private static void Bodegas(Transform raiz)
        {
            Transform padre = raiz.Find("Bodegas");
            if (padre == null)
            {
                return;
            }
            GameObject pallet = Buscar("PU06_Pallets"), tambo = Buscar("C16_Tambo");
            List<Transform> bodegas = padre.Cast<Transform>().Where(t => NombresBodegas.Any(n => t.name.StartsWith(n))).ToList();
            int extras = 0;
            foreach (Transform b in bodegas)
            {
                ActivarColliders(b);
                Bounds caja = LimitesRenderers(b);
                bool oeste = caja.center.x < 150f; // las del oeste miran al este; las del norte miran al sur
                Vector3 normal = oeste ? Vector3.right : Vector3.back;
                Vector3 lado = Vector3.Cross(Vector3.up, normal);
                Vector3 frente = oeste ? new Vector3(caja.max.x, 0f, caja.center.z) : new Vector3(caja.center.x, 0f, caja.min.z);
                string marca = "Extra_" + b.name;
                foreach (Transform viejo in padre.Cast<Transform>().Where(t => t.name.StartsWith(marca)).ToList())
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                if (pallet == null || tambo == null)
                {
                    continue;
                }
                Transform grupo = new GameObject(marca).transform;
                grupo.SetParent(padre, false);
                Poner(pallet, grupo, frente + normal * 1.8f + lado * 11f, 90f, 1.2f);
                Poner(pallet, grupo, frente + normal * 1.8f + lado * 12.4f, 85f, 1.0f);
                Poner(tambo, grupo, frente + normal * 1.3f - lado * 11f, 0f, 1.0f);
                Poner(tambo, grupo, frente + normal * 1.3f - lado * 12f, 0f, 1.0f);
                Poner(tambo, grupo, frente + normal * 2.2f - lado * 11.5f, 0f, 1.0f);
                extras += 5;
            }
            informe.AppendLine("Bodegas: " + bodegas.Count + " con collider de malla; " + extras + " pallets y tambos extra en las puertas (a una calle de x=160 quedan a >= " + DespejeCalle + " m del eje)");
        }

        private static GameObject Buscar(string codigo)
        {
            return AssetDatabase.FindAssets(codigo + " t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(r => { string n = Path.GetFileNameWithoutExtension(r); return n == codigo || n.StartsWith(codigo + "_"); })
                .OrderBy(r => Path.GetFileNameWithoutExtension(r) == codigo ? 0 : 1)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
        }

        private static void Poner(GameObject prefab, Transform padre, Vector3 pie, float yaw, float alto)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Bounds b = LimitesRenderers(go.transform);
            go.transform.localScale = Vector3.one * (b.size.y > 1e-4f ? alto / b.size.y : 1f);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            b = LimitesRenderers(go.transform);
            go.transform.position += new Vector3(pie.x - b.center.x, pie.y - b.min.y, pie.z - b.center.z);
        }

        // ---------------------------------------------------------------- Barrido final

        private static void Barrido()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Physics.SyncTransforms();
            IReadOnlyList<BarridoZonasT1360.Hallazgo> res = BarridoZonasT1360.BarrerZona(inst);
            informe.AppendLine();
            informe.AppendLine("Barrido T-1360 despues (grupos / total):");
            foreach (IGrouping<string, BarridoZonasT1360.Hallazgo> g in res.GroupBy(h => h.tipo).OrderBy(g => g.Key))
            {
                informe.AppendLine("  " + g.Key + ": " + g.Count() + " / " + g.Sum(h => h.cantidad));
            }
            informe.AppendLine("  total de grupos: " + res.Count);
            Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
            long tris = 0;
            foreach (MeshFilter mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Renderer>() != null)
                {
                    tris += mf.sharedMesh.triangles.Length / 3;
                }
            }
            informe.AppendLine("Renderers " + rs.Length + ", triangulos " + tris);
        }

        // ---------------------------------------------------------------- Fotos

        private struct Toma
        {
            public string Arreglo;
            public int N;
            public string Ruta; // objeto del prefab (ruta con /) o vacío para usar Punto
            public Vector3 Punto;
            public float Distancia;
        }

        private static readonly Toma[] Tomas =
        {
            new Toma { Arreglo = "contenedores", N = 1, Ruta = "Contenedores/Bloque_1", Distancia = 20f },
            new Toma { Arreglo = "contenedores", N = 2, Ruta = "Contenedores/Bloque_21", Distancia = 20f },
            new Toma { Arreglo = "escaleras", N = 1, Ruta = "Contenedores/Bloque_7/Escalera_Suelo", Distancia = 9f },
            new Toma { Arreglo = "escaleras", N = 2, Ruta = "Contenedores/Bloque_14/Escalera_Suelo", Distancia = 9f },
            new Toma { Arreglo = "gruas", N = 1, Punto = new Vector3(175f, 0f, -244f), Distancia = 20f },
            new Toma { Arreglo = "barco", N = 1, Punto = new Vector3(250f, -1f, -248f), Distancia = 30f },
            new Toma { Arreglo = "colliders", N = 1, Ruta = "Bodegas/PU03_Bodega", Distancia = 20f },
            new Toma { Arreglo = "colliders", N = 2, Punto = new Vector3(206f, 0f, -106f), Distancia = 15f },
            new Toma { Arreglo = "parpadeo", N = 1, Punto = new Vector3(160f, 0.08f, -215f), Distancia = 14f },
            new Toma { Arreglo = "parpadeo", N = 2, Punto = new Vector3(300f, 0.08f, -80f), Distancia = 14f },
            new Toma { Arreglo = "carro", N = 1, Punto = new Vector3(164.1f, 0f, -166f), Distancia = 14f },
            new Toma { Arreglo = "atrapado", N = 1, Punto = new Vector3(208.075f, 3f, -144.825f), Distancia = 14f },
            new Toma { Arreglo = "atrapado", N = 2, Punto = new Vector3(294.775f, 3f, -127f), Distancia = 14f },
        };

        public static void FotosAntes() { Fotos("antes"); }

        public static void FotosDespues() { Fotos("despues"); }

        private static void Fotos(string sufijo)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            ArteT250.PonerLuz();
            Physics.SyncTransforms();
            GameObject go = new GameObject("CamaraT1366");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            try
            {
                foreach (Toma t in Tomas)
                {
                    Vector3 mira = t.Punto;
                    if (!string.IsNullOrEmpty(t.Ruta))
                    {
                        Transform o = inst.transform.Find(t.Ruta);
                        if (o == null)
                        {
                            Debug.LogWarning("[ArreglarPuertoT1366] No hay " + t.Ruta);
                            continue;
                        }
                        mira = LimitesRenderers(o).center;
                    }
                    Vector3 pos = default;
                    bool libre = false;
                    foreach (float escala in new[] { 1f, 0.8f, 1.3f })
                    {
                        for (int k = 0; k < 8 && !libre; k++)
                        {
                            float ang = (45f + k * 45f) * Mathf.Deg2Rad;
                            Vector3 c = mira + new Vector3(Mathf.Cos(ang) * t.Distancia * escala, t.Distancia * 0.45f * escala, Mathf.Sin(ang) * t.Distancia * escala);
                            if (!Physics.CheckSphere(c, 0.5f, ~0, QueryTriggerInteraction.Ignore) && !Physics.Linecast(mira + Vector3.up * 0.5f, c, ~0, QueryTriggerInteraction.Ignore))
                            {
                                pos = c;
                                libre = true;
                            }
                        }
                        if (libre)
                        {
                            break;
                        }
                    }
                    if (!libre)
                    {
                        pos = mira + new Vector3(0.7f, 0.6f, 0.7f) * t.Distancia;
                    }
                    cam.transform.position = pos;
                    cam.transform.rotation = Quaternion.LookRotation(mira - pos);
                    ArteT250.Guardar(cam, Path.Combine(CarpetaFotos, "t1366-" + t.Arreglo + "-" + t.N + "-" + sufijo + ".png"));
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
