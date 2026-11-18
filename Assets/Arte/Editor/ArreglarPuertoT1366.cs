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
        public const float HuecoBarcoMin = 1f, HuecoBarcoMax = 3f, YMuelle = -1f, YMar = -3f, MuelleSur = -250f, X0Muelle = 115f, X1Muelle = 330f, AnchoMuelle = 12f;
        public const float DesplazamientoEscaleraFila = -2f;
        // Grúas: posición original del generador, alto libre que debe quedar sobre las calles y holgura al arroyo (m).
        public const float XGruaBase = 175f, PasoGruas = 45f, ZGruaBase = -244f, AltoLibreCalle = 4.5f, HolguraArroyo = 0f;
        public const float DesfaseZGrua = -2.4f, /* 2.4 m al sur: el larguero norte no toca el arroyo de Calle_2 */ SeparacionGruas = 56f, ExclusionX0 = 293f, ExclusionX1 = 307f;
        public const float PasoBusquedaZ = 0.25f, RangoBusquedaZ = 14f, RangoBusquedaX = 6f;

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
                DespejarArroyos(raiz.transform);
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

        // Un BoxCollider por pila (fila x columna) en el propio Bloque; center y size salen del espacio LOCAL del Bloque (8 vértices de cada contenedor).
        private static void SincronizarColliders(Transform bloque, List<Pila> pilas)
        {
            List<BoxCollider> cajas = bloque.GetComponents<BoxCollider>().ToList();
            for (int i = 0; i < pilas.Count; i++)
            {
                Bounds local = default;
                bool primero = true;
                foreach (Transform cont in pilas[i].Contenedores)
                {
                    foreach (MeshFilter mf in cont.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null)
                        {
                            continue;
                        }
                        Bounds m = mf.sharedMesh.bounds;
                        for (int k = 0; k < 8; k++)
                        {
                            Vector3 e = m.extents;
                            Vector3 esquina = m.center + new Vector3((k & 1) == 0 ? -e.x : e.x, (k & 2) == 0 ? -e.y : e.y, (k & 4) == 0 ? -e.z : e.z);
                            Vector3 enBloque = bloque.InverseTransformPoint(mf.transform.TransformPoint(esquina));
                            if (primero)
                            {
                                local = new Bounds(enBloque, Vector3.zero);
                                primero = false;
                            }
                            else
                            {
                                local.Encapsulate(enBloque);
                            }
                        }
                    }
                }
                BoxCollider bc = i < cajas.Count ? cajas[i] : bloque.gameObject.AddComponent<BoxCollider>();
                bc.center = local.center;
                bc.size = local.size;
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
            List<Rect> cajasObstaculo = CajasObstaculo(raiz);
            List<Vector2> postes = PosicionesPostes(raiz);
            float zCentro = ZEjeCalle4(raiz) + DesfaseZGrua;
            int indiceGrua = 0, gruasSinSolucion = 0, activas = 0;
            float centroPrevio = float.MinValue;
            foreach (Transform g in gruas)
            {
                ActivarColliders(g);
                // Posición original del generador (idempotente): sin giro, centrada en (175 + 45 i, -244), patas en y = -1.
                g.gameObject.SetActive(true);
                g.rotation = Quaternion.identity;
                Bounds b0 = LimitesRenderers(g);
                g.position += new Vector3(XGruaBase + PasoGruas * indiceGrua - b0.center.x, YMuelle - b0.min.y, ZGruaBase - b0.center.z);
                indiceGrua++;
                // Caben las que dejen su caja en x dentro del muelle sur, fuera de 293..307 (tramo norte-sur de Calle_2) y a >= 56 m de la anterior.
                float medio = b0.extents.z;
                float minimo = X0Muelle + medio + 0.2f, maximo = Mathf.Min(X1Muelle - medio - 0.2f, ExclusionX0 - medio - 0.2f);
                int caben = maximo >= minimo ? Mathf.FloorToInt((maximo - minimo) / SeparacionGruas) + 1 : 0;
                if (activas >= caben)
                {
                    g.gameObject.SetActive(false);
                    informe.AppendLine("Grúa " + indiceGrua + ": DESACTIVADA (SetActive false): en x del muelle sur solo caben " + caben + " grúas separadas " + SeparacionGruas + " m y fuera de x " + ExclusionX0 + ".." + ExclusionX1);
                    continue;
                }
                float nominal = caben > 1 ? minimo + (maximo - minimo) * activas / (caben - 1) : minimo;
                float desde = Mathf.Max(minimo, centroPrevio == float.MinValue ? minimo : centroPrevio + SeparacionGruas);
                string resultado = UbicarGrua(g, zCentro, nominal, desde, maximo, cajasObstaculo, postes, out bool sinSolucion, out float centroX);
                centroPrevio = centroX;
                activas++;
                gruasSinSolucion += sinSolucion ? 1 : 0;
                informe.AppendLine("Grúa " + indiceGrua + ": " + resultado);
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
            informe.AppendLine("Grúas: " + gruas.Count + " con collider de malla; " + ajustadasY + " bajadas/subidas hasta pisar el muelle (y " + YMuelle + "), " + gruasSinSolucion + " con conflictos; " + bitasMovidas + " bitas movidas fuera de las patas");
        }

        private struct PuntoGrua
        {
            public Vector2 Desde; // desde el centro de la grúa, sin girar
            public bool Pie;
        }

        private static List<Rect> CajasObstaculo(Transform raiz)
        {
            List<Rect> r = new List<Rect>();
            Transform padre = raiz.Find("Contenedores");
            if (padre == null)
            {
                return r;
            }
            foreach (Transform bloque in padre)
            {
                List<Pila> pilas = bloque.name.StartsWith("Bloque_") ? PilasDe(bloque) : new List<Pila>();
                if (pilas.Count == 0)
                {
                    continue;
                }
                Bounds b = LimitesRenderers(pilas[0].Contenedores[0]);
                foreach (Pila p in pilas)
                {
                    foreach (Transform c in p.Contenedores)
                    {
                        b.Encapsulate(LimitesRenderers(c));
                    }
                }
                r.Add(new Rect(b.min.x - 1f, b.min.z - 1f, b.size.x + 2f, b.size.z + 2f));
            }
            return r;
        }

        private static List<Vector2> PosicionesPostes(Transform raiz)
        {
            Transform padre = raiz.Find("Postes");
            return padre == null ? new List<Vector2>() : padre.Cast<Transform>().Where(t => t.name.StartsWith("Poste")).Select(t => new Vector2(t.position.x, t.position.z)).ToList();
        }

        /// <summary>
        /// Coloca la grúa con sus dos largueros bajos (a lo largo de z local, 16 m libres entre ellos) paralelos a Calle_4: giro de 90 o 270 grados
        /// (el de menos conflictos) y centro en z = eje de Calle_4 - 2 m, para que la calle pase entera entre los largueros. Solo se ajusta x
        /// (±6 m del nominal, dentro de [desde, hasta]) para que ningún punto bajo toque un poste, un bloque ni el arroyo real (eje ± ancho/2) de una calle.
        /// Los pies del lado del patio se hunden hasta 1 m: aceptado.
        /// </summary>
        private static string UbicarGrua(Transform g, float zCentro, float nominal, float desde, float hasta, List<Rect> obstaculos, List<Vector2> postes,
            out bool conConflictos, out float centroFinal)
        {
            Bounds b = LimitesRenderers(g);
            Vector2 centro = new Vector2(b.center.x, b.center.z);
            HashSet<long> vistos = new HashSet<long>();
            List<PuntoGrua> puntos = new List<PuntoGrua>();
            foreach (Vector3 w in VerticesBajos(g, AltoLibreCalle))
            {
                bool pie = w.y <= YMuelle + 1f;
                long clave = (Mathf.RoundToInt(w.x * 4f) * 1000003L + Mathf.RoundToInt(w.z * 4f)) * 2 + (pie ? 1 : 0);
                if (vistos.Add(clave))
                {
                    puntos.Add(new PuntoGrua { Desde = new Vector2(w.x, w.z) - centro, Pie = pie });
                }
            }
            List<(Vector3[] eje, float radio)> arroyos = g.root.GetComponentsInChildren<CalleZona>(true)
                .Select(c => (c.Puntos().ToArray(), c.Ancho * 0.5f + HolguraArroyo)).ToList();
            float[] giros = { 90f, 270f };
            int menosConflictos = int.MaxValue, mejorArroyo = 0;
            float mejorCosto = float.MaxValue, mejorYaw = 90f, mejorX = Mathf.Clamp(nominal, desde, Mathf.Max(desde, hasta));
            foreach (float yaw in giros)
            {
                float c = Mathf.Cos(yaw * Mathf.Deg2Rad), s = Mathf.Sin(yaw * Mathf.Deg2Rad);
                for (float x = desde; x <= hasta + 0.001f; x += 0.5f)
                {
                    float dx = x - nominal;
                    int conflictos = 0, enArroyo = 0;
                    foreach (PuntoGrua p in puntos)
                    {
                        Vector2 w = new Vector2(x, zCentro) + new Vector2(p.Desde.x * c + p.Desde.y * s, -p.Desde.x * s + p.Desde.y * c);
                        Vector3 w3 = new Vector3(w.x, 0f, w.y);
                        bool calle = arroyos.Any(a => DistanciaPlanoACalle(w3, a.eje) < a.radio);
                        bool mal = calle || obstaculos.Any(r => r.Contains(w)) || postes.Any(q => (q - w).sqrMagnitude < 0.64f);
                        conflictos += mal ? 1 : 0;
                        enArroyo += calle ? 1 : 0;
                    }
                    float costo = Mathf.Abs(dx) + (yaw == 90f ? 0f : 0.1f);
                    if (conflictos < menosConflictos || (conflictos == menosConflictos && costo < mejorCosto))
                    {
                        menosConflictos = conflictos;
                        mejorArroyo = enArroyo;
                        mejorCosto = costo;
                        mejorYaw = yaw;
                        mejorX = x;
                    }
                }
            }
            conConflictos = menosConflictos != 0;
            centroFinal = mejorX;
            g.RotateAround(new Vector3(centro.x, 0f, centro.y), Vector3.up, mejorYaw);
            g.position += new Vector3(mejorX - centro.x, 0f, zCentro - centro.y);
            // Hueco libre entre largueros a 1 m de alto, medido sobre la malla ya colocada.
            List<Vector3> pies = VerticesBajos(g, YMuelle + 1f);
            List<float> norte = pies.Where(v => v.z > zCentro).Select(v => v.z).ToList();
            List<float> sur = pies.Where(v => v.z < zCentro).Select(v => v.z).ToList();
            string hueco = norte.Count > 0 && sur.Count > 0 ? (norte.Min() - sur.Max()).ToString("F2") + " m" : "no medible";
            Bounds fin = LimitesRenderers(g);
            return "x " + mejorX.ToString("F1") + ", z " + zCentro.ToString("F1") + ", giro " + mejorYaw + ", hueco libre entre largueros a 1 m de alto " + hueco
                + ", vértices bajos en un arroyo: " + mejorArroyo + ", en poste o bloque: " + (menosConflictos == int.MaxValue ? 0 : menosConflictos - mejorArroyo)
                + "; caja x " + fin.min.x.ToString("F1") + ".." + fin.max.x.ToString("F1") + ", z " + fin.min.z.ToString("F1") + ".." + fin.max.z.ToString("F1");
        }

        /// <summary>z del tramo largo de Calle_4 (el paralelo al muelle).</summary>
        private static float ZEjeCalle4(Transform raiz)
        {
            Transform c4 = raiz.Find("Calle_4");
            CalleZona cz = c4 != null ? c4.GetComponent<CalleZona>() : null;
            if (cz == null)
            {
                return -232f;
            }
            List<Vector3> pts = cz.Puntos();
            float mejor = 0f, z = -232f;
            for (int i = 1; i < pts.Count; i++)
            {
                float dx = Mathf.Abs(pts[i].x - pts[i - 1].x);
                if (dx > mejor)
                {
                    mejor = dx;
                    z = (pts[i].z + pts[i - 1].z) * 0.5f;
                }
            }
            return z;
        }

        /// <summary>Vértices del collider de malla (o de la malla visible si no hay) por debajo de una altura, en mundo.</summary>
        public static List<Vector3> VerticesBajos(Transform raiz, float yMaxima)
        {
            List<Vector3> res = new List<Vector3>();
            List<(Transform t, Mesh m)> mallas = raiz.GetComponentsInChildren<MeshCollider>(true).Where(c => c.sharedMesh != null).Select(c => (c.transform, c.sharedMesh)).ToList();
            if (mallas.Count == 0)
            {
                mallas = raiz.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Select(f => (f.transform, f.sharedMesh)).ToList();
            }
            foreach ((Transform t, Mesh m) in mallas)
            {
                foreach (Vector3 v in m.vertices)
                {
                    Vector3 w = t.TransformPoint(v);
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
            // Los extras son duplicados de un original del Puerto con sus mismos componentes (el tambo lleva BarrilExplosivo): no se instancia el prefab pelón.
            Transform pallet = PlantillaProp(raiz, "PU06_Pallets", false), tambo = PlantillaProp(raiz, "C16_Tambo", true);
            if (tambo == null || pallet == null)
            {
                informe.AppendLine("Bodegas: no hay un C16_Tambo con BarrilExplosivo o un PU06_Pallets original en el Puerto para duplicar: sin extras");
            }
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
                // T-1376: sin extras en las puertas (quedaban dentro de la caja de la bodega y se encimaban); el grupo viejo ya se borró arriba.
                if (!PonerExtrasBodegas || pallet == null || tambo == null)
                {
                    continue;
                }
                Transform grupo = new GameObject(marca).transform;
                grupo.SetParent(padre, false);
                PonerCopia(pallet, grupo, frente + normal * 1.8f + lado * 11f, 90f, 1.2f);
                PonerCopia(pallet, grupo, frente + normal * 1.8f + lado * 12.4f, 85f, 1.0f);
                PonerCopia(tambo, grupo, frente + normal * 1.3f - lado * 11f, 0f, 1.0f);
                PonerCopia(tambo, grupo, frente + normal * 1.3f - lado * 12f, 0f, 1.0f);
                PonerCopia(tambo, grupo, frente + normal * 2.2f - lado * 11.5f, 0f, 1.0f);
                extras += 5;
            }
            informe.AppendLine("Bodegas: " + bodegas.Count + " con collider de malla; " + extras + " pallets y tambos extra en las puertas (a una calle de x=160 quedan a >= " + DespejeCalle + " m del eje)");
        }

        /// <summary>T-1376: los pallets y tambos extra en las puertas de las bodegas se apagaron: se encimaban con la bodega.</summary>
        private const bool PonerExtrasBodegas = false;

        /// <summary>Un original del Puerto (no extra) con ese nombre; con <paramref name="conBarril"/> exige un componente BarrilExplosivo y un collider.</summary>
        private static Transform PlantillaProp(Transform raiz, string codigo, bool conBarril)
        {
            return raiz.GetComponentsInChildren<Transform>(true)
                .Where(x => x.name.StartsWith(codigo) && PrefabUtility.GetNearestPrefabInstanceRoot(x) == x.gameObject && !EsExtra(x))
                .FirstOrDefault(x => !conBarril || (x.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "BarrilExplosivo")
                    && x.GetComponentsInChildren<Collider>(true).Length > 0));
        }

        private static bool EsExtra(Transform x)
        {
            for (Transform p = x.parent; p != null; p = p.parent)
            {
                if (p.name.StartsWith("Extra_"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Duplica la plantilla (con todos sus componentes) y la pone con la base centrada en <paramref name="pie"/>, a la altura <paramref name="alto"/>.</summary>
        private static void PonerCopia(Transform plantilla, Transform padre, Vector3 pie, float yaw, float alto)
        {
            if (plantilla == null)
            {
                return;
            }
            GameObject go = Object.Instantiate(plantilla.gameObject, padre);
            go.name = plantilla.gameObject.name;
            go.SetActive(true);
            go.transform.rotation = Quaternion.identity;
            Bounds b = LimitesRenderers(go.transform);
            go.transform.localScale *= b.size.y > 1e-4f ? alto / b.size.y : 1f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            b = LimitesRenderers(go.transform);
            go.transform.position += new Vector3(pie.x - b.center.x, pie.y - b.min.y, pie.z - b.center.z);
            ActivarColliders(go.transform);
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

        // ---------------------------------------------------------------- Colliders fuera del arroyo de las calles

        public const float ArroyoAlturaMinima = 0.3f, ArroyoAlturaMaxima = 3f, EmpujeMaximoProp = 12f, EmpujeMaximoBodegaNorte = 14f, MargenArroyoBodega = 0.5f, PatioMargen = 0.5f;

        /// <summary>Puntos en planta (x, z) donde vigilar un collider: esquinas, aristas y centro de una caja; rejilla de 1 m para una malla.</summary>
        public static List<Vector2> MuestrasPlanta(Collider c, Bounds b)
        {
            List<Vector2> m = new List<Vector2>();
            if (c is BoxCollider bc)
            {
                Vector3 e = bc.size * 0.5f;
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        Vector3 w = c.transform.TransformPoint(bc.center + new Vector3(e.x * i, 0f, e.z * j));
                        m.Add(new Vector2(w.x, w.z));
                    }
                }
                return m;
            }
            for (float x = b.min.x; x <= b.max.x + 0.001f; x += 1f)
            {
                for (float z = b.min.z; z <= b.max.z + 0.001f; z += 1f)
                {
                    m.Add(new Vector2(x, z));
                }
            }
            return m;
        }

        private static float ProfundidadEnArroyo(Vector2 w, List<(Vector3[] eje, float radio)> arroyos, out Vector2 haciaAfuera)
        {
            float peor = 0f;
            haciaAfuera = Vector2.zero;
            foreach ((Vector3[] eje, float radio) in arroyos)
            {
                for (int i = 1; i < eje.Length; i++)
                {
                    Vector2 a = new Vector2(eje[i - 1].x, eje[i - 1].z), d = new Vector2(eje[i].x, eje[i].z) - a;
                    float k = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(w - a, d) / d.sqrMagnitude);
                    Vector2 cerca = a + d * k;
                    float prof = radio - Vector2.Distance(w, cerca);
                    if (prof > peor)
                    {
                        peor = prof;
                        Vector2 fuera = w - cerca;
                        haciaAfuera = fuera.sqrMagnitude < 1e-6f ? new Vector2(-d.y, d.x).normalized : fuera.normalized;
                    }
                }
            }
            return peor;
        }

        private sealed class Entorno
        {
            public List<(Vector3[] eje, float radio)> Arroyos;      // radio real
            public List<(Vector3[] eje, float radio)> ArroyosMargen; // radio + 0.5 m
            public List<Rect> Bloques;
            public Rect Patio;
            public List<Vector2> Postes;
            public Transform Raiz;
        }

        /// <summary>Puntos en planta de los colliders (entre 0.3 y 3 m de alto) de un objeto.</summary>
        private static List<Vector2> MuestrasDe(Transform t)
        {
            List<Vector2> m = new List<Vector2>();
            foreach (Collider c in t.GetComponentsInChildren<Collider>(true).Where(x => !x.isTrigger))
            {
                if (LimitesCollider(c, out Bounds b) && b.max.y >= ArroyoAlturaMinima && b.min.y <= ArroyoAlturaMaxima)
                {
                    m.AddRange(MuestrasPlanta(c, b));
                }
            }
            return m;
        }

        private static Rect RectDe(IEnumerable<Vector2> m)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (Vector2 p in m)
            {
                x0 = Mathf.Min(x0, p.x);
                x1 = Mathf.Max(x1, p.x);
                z0 = Mathf.Min(z0, p.y);
                z1 = Mathf.Max(z1, p.y);
            }
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        private static Rect Expandir(Rect r, float m)
        {
            return Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
        }

        /// <summary>Rectángulos de lo que no se puede encimar: las demás bodegas, casetas y rejas (cajas de sus renderers).</summary>
        private static List<Rect> OtrosRects(Transform raiz, Transform yo)
        {
            List<Rect> r = new List<Rect>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                bool cuenta = NombresBodegas.Any(n => t.name.StartsWith(n)) || t.name.StartsWith("LO04_Caseta") || t.name.StartsWith("DS01_RejaPuas");
                if (!cuenta || t == yo || (yo != null && t.IsChildOf(yo)) || PrefabUtility.GetNearestPrefabInstanceRoot(t) != t.gameObject)
                {
                    continue;
                }
                Bounds b = LimitesRenderers(t);
                r.Add(Expandir(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z), 0.3f));
            }
            return r;
        }

        /// <summary>Si el objeto, con esos puntos en planta, queda dentro del patio, fuera de arroyos (+0.5 m), bloques, postes y otras piezas.</summary>
        private static bool Cabe(Entorno e, Transform yo, List<Vector2> puntos)
        {
            if (puntos.Count == 0)
            {
                return true;
            }
            Rect huella = RectDe(puntos);
            Rect patio = Expandir(e.Patio, -PatioMargen);
            if (huella.xMin < patio.xMin || huella.xMax > patio.xMax || huella.yMin < patio.yMin || huella.yMax > patio.yMax)
            {
                return false;
            }
            foreach (Vector2 p in puntos)
            {
                if (ProfundidadEnArroyo(p, e.ArroyosMargen, out _) > 0f)
                {
                    return false;
                }
            }
            if (e.Bloques.Any(r => r.Overlaps(huella)) || OtrosRects(e.Raiz, yo).Any(r => r.Overlaps(huella)))
            {
                return false;
            }
            Rect poste = Expandir(huella, 0.8f);
            return !e.Postes.Any(q => poste.Contains(q));
        }

        private static List<Vector2> Mover(List<Vector2> puntos, Vector2 centro, float giro, Vector2 delta)
        {
            float c = Mathf.Cos(giro * Mathf.Deg2Rad), s = Mathf.Sin(giro * Mathf.Deg2Rad);
            return puntos.Select(p =>
            {
                Vector2 r = p - centro;
                return centro + new Vector2(r.x * c + r.y * s, -r.x * s + r.y * c) + delta;
            }).ToList();
        }

        /// <summary>
        /// Despeja el arroyo real (eje ± ancho/2) entre 0.3 y 3 m de alto:
        /// - las bodegas que lo invaden se mueven hacia el lado opuesto a la calle (hasta 14 m), con sus detalles (neón, reflector, pallets, tambos, extras),
        ///   y si no cabe se prueba girarlas 90°; si de plano no caben se dejan donde estaban CON collider y se anota "NO CABE";
        /// - el montacargas que invade una calle se pone junto a la bodega más cercana, fuera de toda calle, o se deja con collider y se anota "NO CABE";
        /// - los demás props (pallets, huacales, tambos) se apartan hacia afuera (hasta 12 m) y, si no caben, se apagan sus colliders.
        /// </summary>
        private static void DespejarArroyos(Transform raiz)
        {
            Entorno e = new Entorno
            {
                Raiz = raiz,
                Arroyos = raiz.GetComponentsInChildren<CalleZona>(true).Select(c => (c.Puntos().ToArray(), c.Ancho * 0.5f + HolguraArroyo)).ToList(),
                Bloques = CajasObstaculo(raiz),
                Postes = PosicionesPostes(raiz),
            };
            e.ArroyosMargen = e.Arroyos.Select(a => (a.eje, a.radio + MargenArroyoBodega)).ToList();
            Transform patio = raiz.Find("Patio");
            Bounds bp = patio != null ? LimitesRenderers(patio) : new Bounds(new Vector3(216.5f, 0f, -141.5f), new Vector3(203f, 3f, 193f));
            e.Patio = Rect.MinMaxRect(bp.min.x, bp.min.z, bp.max.x, bp.max.z);

            Transform padreBodegas = raiz.Find("Bodegas");
            List<Transform> bodegas = padreBodegas == null ? new List<Transform>()
                : padreBodegas.Cast<Transform>().Where(t => NombresBodegas.Any(n => t.name.StartsWith(n))).ToList();
            List<(Transform t, Rect r)> huellasOriginales = bodegas.Select(b => (b, Expandir(RectDe(MuestrasDe(b)), 0f))).ToList();
            int bodegasMovidas = 0, noCaben = 0;
            foreach (Transform b in bodegas)
            {
                List<Vector2> puntos = MuestrasDe(b);
                if (puntos.Count == 0 || !puntos.Any(p => ProfundidadEnArroyo(p, e.Arroyos, out _) > 0f))
                {
                    continue;
                }
                Vector2 centro = RectDe(puntos).center;
                Vector2 cerca = PuntoMasCercanoEnEje(centro, e.Arroyos, out _);
                Vector2 lejos = centro - cerca;
                Vector2 dir = Mathf.Abs(lejos.x) > Mathf.Abs(lejos.y) ? new Vector2(Mathf.Sign(lejos.x), 0f) : new Vector2(0f, Mathf.Sign(lejos.y == 0f ? 1f : lejos.y));
                bool hecho = false;
                foreach (float giro in new[] { 0f, 90f })
                {
                    for (float s = 0f; s <= EmpujeMaximoBodegaNorte && !hecho; s += 0.25f)
                    {
                        if (giro == 0f && s == 0f)
                        {
                            continue;
                        }
                        if (Cabe(e, b, Mover(puntos, centro, giro, dir * s)))
                        {
                            MoverConDetalles(b, padreBodegas, huellasOriginales, centro, giro, dir * s);
                            informe.AppendLine("Bodega " + b.name + " movida " + s.ToString("F2") + " m hacia " + (dir.y > 0f ? "el norte" : dir.y < 0f ? "el sur" : dir.x > 0f ? "el este" : "el oeste")
                                + (giro != 0f ? " y girada 90 grados" : "") + " con sus detalles, fuera del arroyo + " + MargenArroyoBodega + " m");
                            hecho = true;
                            bodegasMovidas++;
                        }
                    }
                    if (hecho)
                    {
                        break;
                    }
                }
                if (!hecho)
                {
                    noCaben++;
                    ActivarColliders(b);
                    informe.AppendLine("NO CABE: bodega " + b.name + " en " + b.position.ToString("F1") + " invade el arroyo y no hay sitio dentro del patio (hasta " + EmpujeMaximoBodegaNorte
                        + " m, ni girada 90 grados): queda donde estaba CON collider");
                }
            }

            int montMovidos = 0, montNoCaben = 0, movidos = 0, apagados = 0, revisados = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("PU05_Montacargas")).ToList())
            {
                if (PrefabUtility.GetNearestPrefabInstanceRoot(t) != t.gameObject)
                {
                    continue;
                }
                List<Vector2> puntos = MuestrasDe(t);
                if (puntos.Count == 0 || !puntos.Any(p => ProfundidadEnArroyo(p, e.Arroyos, out _) > 0f))
                {
                    continue;
                }
                Vector2 centro = RectDe(puntos).center;
                Rect cerca = bodegas.Count == 0 ? new Rect(centro, Vector2.zero)
                    : bodegas.Select(b => RectDe(MuestrasDe(b))).OrderBy(r => DistanciaARect(centro, r)).First();
                Vector2? mejor = null;
                float mejorD = float.MaxValue;
                foreach (float margen in new[] { 3f, 5f, 8f })
                {
                    Rect anillo = Expandir(cerca, margen);
                    List<Vector2> candidatos = new List<Vector2>();
                    for (float x = anillo.xMin; x <= anillo.xMax; x += 2f)
                    {
                        candidatos.Add(new Vector2(x, anillo.yMin));
                        candidatos.Add(new Vector2(x, anillo.yMax));
                    }
                    for (float z = anillo.yMin; z <= anillo.yMax; z += 2f)
                    {
                        candidatos.Add(new Vector2(anillo.xMin, z));
                        candidatos.Add(new Vector2(anillo.xMax, z));
                    }
                    foreach (Vector2 cand in candidatos)
                    {
                        Vector2 delta = cand - centro;
                        if (delta.magnitude < mejorD && Cabe(e, t, Mover(puntos, centro, 0f, delta)))
                        {
                            mejor = delta;
                            mejorD = delta.magnitude;
                        }
                    }
                    if (mejor.HasValue)
                    {
                        break;
                    }
                }
                ActivarColliders(t);
                if (mejor.HasValue)
                {
                    t.position += new Vector3(mejor.Value.x, 0f, mejor.Value.y);
                    montMovidos++;
                }
                else
                {
                    montNoCaben++;
                    informe.AppendLine("NO CABE: montacargas en " + t.position.ToString("F1") + " invade una calle y no hay sitio junto a la bodega más cercana: queda donde estaba CON collider");
                }
            }

            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (!PropsConCollider.Any(n => t.name.StartsWith(n)) || t.name.StartsWith("PU05_Montacargas") || PrefabUtility.GetNearestPrefabInstanceRoot(t) != t.gameObject)
                {
                    continue;
                }
                revisados++;
                float empujado = 0f;
                bool fallo = false;
                for (int vuelta = 0; vuelta < 6; vuelta++)
                {
                    float peor = 0f;
                    Vector2 dir = Vector2.zero;
                    foreach (Vector2 m in MuestrasDe(t))
                    {
                        float p = ProfundidadEnArroyo(m, e.Arroyos, out Vector2 fuera);
                        if (p > peor)
                        {
                            peor = p;
                            dir = fuera;
                        }
                    }
                    if (peor <= 0f)
                    {
                        break;
                    }
                    empujado += peor + 0.05f;
                    if (empujado > EmpujeMaximoProp)
                    {
                        fallo = true;
                        break;
                    }
                    t.position += new Vector3(dir.x, 0f, dir.y) * (peor + 0.05f);
                    movidos += vuelta == 0 ? 1 : 0;
                }
                if (!fallo && empujado > 0f)
                {
                    Bounds caja = LimitesRenderers(t);
                    fallo = e.Bloques.Any(r => r.Overlaps(Rect.MinMaxRect(caja.min.x, caja.min.z, caja.max.x, caja.max.z)));
                }
                if (fallo)
                {
                    foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
                    {
                        c.enabled = false;
                    }
                    apagados++;
                    informe.AppendLine("Arroyo: " + t.name + " en " + t.position.ToString("F1") + " (pallet, huacal o tambo) no cabe fuera del arroyo: colliders apagados");
                }
            }
            informe.AppendLine("Arroyos: " + bodegas.Count + " bodegas (" + bodegasMovidas + " movidas, " + noCaben + " NO CABE), montacargas (" + montMovidos + " movidos, " + montNoCaben + " NO CABE), "
                + revisados + " pallets, huacales y tambos revisados, " + movidos + " apartados, " + apagados + " con colliders apagados");
        }

        private static float DistanciaARect(Vector2 p, Rect r)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), dz = Mathf.Max(0f, Mathf.Max(r.yMin - p.y, p.y - r.yMax));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Vector2 PuntoMasCercanoEnEje(Vector2 w, List<(Vector3[] eje, float radio)> arroyos, out float distancia)
        {
            float mejor = float.MaxValue;
            Vector2 res = w;
            foreach ((Vector3[] eje, float radio) in arroyos)
            {
                for (int i = 1; i < eje.Length; i++)
                {
                    Vector2 a = new Vector2(eje[i - 1].x, eje[i - 1].z), d = new Vector2(eje[i].x, eje[i].z) - a;
                    float k = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(w - a, d) / d.sqrMagnitude);
                    Vector2 cerca = a + d * k;
                    float dist = Vector2.Distance(w, cerca);
                    if (dist < mejor)
                    {
                        mejor = dist;
                        res = cerca;
                    }
                }
            }
            distancia = mejor;
            return res;
        }

        /// <summary>Mueve la bodega y los objetos de "Bodegas" que son de ella (la bodega más cercana por su huella original): neón, letrero, reflector, pallets, tambos y extras.</summary>
        private static void MoverConDetalles(Transform bodega, Transform padre, List<(Transform t, Rect r)> huellas, Vector2 centro, float giro, Vector2 delta)
        {
            List<Transform> partes = new List<Transform> { bodega };
            if (padre != null && huellas.Count > 0)
            {
                foreach (Transform h in padre.Cast<Transform>())
                {
                    if (h == bodega || NombresBodegas.Any(n => h.name.StartsWith(n)) || h.name.StartsWith("PU05_Montacargas"))
                    {
                        continue;
                    }
                    Vector2 p = new Vector2(h.position.x, h.position.z);
                    Transform suya = huellas.OrderBy(x => DistanciaARect(p, x.r)).First().t;
                    if (suya == bodega)
                    {
                        partes.Add(h);
                    }
                }
            }
            foreach (Transform p in partes)
            {
                if (giro != 0f)
                {
                    p.RotateAround(new Vector3(centro.x, 0f, centro.y), Vector3.up, giro);
                }
                p.position += new Vector3(delta.x, 0f, delta.y);
            }
        }

        /// <summary>Revisión final con física: OverlapBox cada 2 m a lo largo de cada calle (ancho/2 - 0.3, de 0.3 a 3 m de alto); lista cada collider hallado.</summary>
        private static void RevisarArroyos(GameObject inst)
        {
            Physics.SyncTransforms();
            HashSet<Collider> hallados = new HashSet<Collider>();
            foreach (CalleZona calle in inst.GetComponentsInChildren<CalleZona>(true))
            {
                List<Vector3> pts = calle.Puntos();
                for (int i = 1; i < pts.Count; i++)
                {
                    Vector3 a = pts[i - 1], d = pts[i] - pts[i - 1];
                    float largo = d.magnitude;
                    if (largo < 0.01f)
                    {
                        continue;
                    }
                    Quaternion giro = Quaternion.LookRotation(d / largo);
                    for (float s = 1f; s < largo; s += 2f)
                    {
                        Vector3 centro = a + d / largo * s + Vector3.up * ((ArroyoAlturaMinima + ArroyoAlturaMaxima) * 0.5f);
                        Vector3 mitad = new Vector3(calle.Ancho * 0.5f - 0.3f, (ArroyoAlturaMaxima - ArroyoAlturaMinima) * 0.5f, 1f);
                        foreach (Collider c in Physics.OverlapBox(centro, mitad, giro, ~0, QueryTriggerInteraction.Ignore))
                        {
                            hallados.Add(c);
                        }
                    }
                }
            }
            informe.AppendLine("Colliders en el arroyo (OverlapBox cada 2 m, 0.3 a 3 m de alto): " + hallados.Count + " (los angostos de la orilla, como postes, no se listan)");
            List<(Vector3[] eje, float radio)> ejes = inst.GetComponentsInChildren<CalleZona>(true).Select(c => (c.Puntos().ToArray(), c.Ancho * 0.5f)).ToList();
            int ignorados = 0;
            foreach (Collider c in hallados.OrderBy(x => x.bounds.center.x).ThenBy(x => x.bounds.center.z))
            {
                // Postes y similares: miden menos de 1 m de ancho y están en la orilla (a más de ancho/2 - 1 m del eje): no son estorbo del carril.
                if (Mathf.Min(c.bounds.size.x, c.bounds.size.z) < 1f && ejes.All(a => DistanciaPlanoACalle(c.bounds.center, a.eje) >= a.radio - 1f))
                {
                    ignorados++;
                    continue;
                }
                string ruta = c.name;
                for (Transform p = c.transform.parent; p != null && p != inst.transform; p = p.parent)
                {
                    ruta = p.name + "/" + ruta;
                }
                informe.AppendLine("  " + ruta + " centro " + c.bounds.center.ToString("F1") + " tam " + c.bounds.size.ToString("F1"));
            }
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
            RevisarArroyos(inst);
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
