using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1386: deja la Autopista elevada como K18 y A18. Edita el prefab Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab (no la escena) y pone todo lo
    /// nuevo bajo el hijo raíz "T1386_Autopista", que Aplicar borra y rehace (se puede correr dos veces). NO corras ColocarT1356Autopista.Aplicar:
    /// rehace la zona y borra los arreglos de T-1360 a T-1377.
    /// Qué hace: barreras AU02 en los bordes del tablero y la rampa (en lugar del guardarraíl AU05, que se apaga; el collider del parapeto sigue cerrando
    /// el borde); chatarra AU03 bajo el puente; casas de 2 y 3 pisos con letreros, cables, semáforo y carros en la calle baja de K18; torre con reflectores
    /// al fondo; roca del cerro más clara. Métodos: Aplicar, Validar y Fotos (las fotos, en una corrida de Unity aparte).
    /// </summary>
    public static class ZonaAutopistaT1386
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1386_Autopista";

        /// <summary>Barrera AU02: la _a es la losa lisa con franjas, la que más se parece a las barreras lisas de K18 y se repite sin que se note.</summary>
        public const string VarianteBarrera = "_a";
        /// <summary>Chatarra AU03: la _a (dos carros chocados, llantas y carrito) para los montones; la _b se mezcla cada tercer montón.</summary>
        public const string VarianteChatarra = "_a";
        public const string VarianteChatarraAlterna = "_b";

        private const string RutaBarrera = "Assets/Arte/Zonas/AU/AU02_BarreraConcreto";
        private const string RutaChatarra = "Assets/Arte/Zonas/AU/AU03_ChatarraPuente";
        private const string RutaTorres = "Assets/Arte/Exteriores/Prefabs/";
        private const string RutaRocaBase = "Assets/Arte/Mundo/S2c/T1356_Roca.mat";
        private const float MedioTablero = 7.2f;
        private const float AltoBarrera = 0.9f;
        private const float AnchoMaxBarrera = 0.7f;
        private const float ZCalleBaja = 216f;
        private const float ZFilaSur = 210f;
        private const int MaxObjetosNuevos = 3000;
        private const float PisoFoto = 3.2f;

        private static readonly StringBuilder informe = new StringBuilder();

        private sealed class Ocupado
        {
            public Bounds Caja;
            public Transform Quien;
            public bool Desechable;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1386/Autopista (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                if (raiz.position != Vector3.zero || raiz.rotation != Quaternion.identity || raiz.localScale != Vector3.one)
                {
                    informe.AppendLine("AVISO: la raíz del prefab no está en el origen sin girar; las posiciones se toman en mundo.");
                }
                int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1386, raiz);

                Transform casas = Hijo(o.Raiz, "Casas");
                Transform barreras = Hijo(o.Raiz, "Barreras_AU02");
                Transform chatarra = Hijo(o.Raiz, "Chatarra_AU03");
                Transform calle = Hijo(o.Raiz, "CalleBaja");
                Transform fondo = Hijo(o.Raiz, "Fondo");

                CasasDeK18(o, raiz, casas);
                List<Ocupado> ocupados = Ocupados(raiz, o.Raiz);
                Barreras(o, raiz, barreras);
                ChatarraBajoPuente(o, raiz, chatarra, ocupados);
                CalleBajaK18(o, raiz, calle, ocupados);
                TorreConReflectores(o, raiz, fondo);
                RocaDelCerro(raiz);

                KitS2c.Terminar(o);
                int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                int objetos = o.Raiz.GetComponentsInChildren<Transform>(true).Length;
                informe.AppendLine("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (suben " + (lucesDespues - lucesAntes) + ", tope 40)");
                informe.AppendLine("Objetos nuevos bajo " + NombreRaiz + ": " + objetos + " (tope " + MaxObjetosNuevos + ")");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1386-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1386 aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Utilidades

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static GameObject Cargar(string codigo, string variante)
        {
            string ruta = codigo + variante + ".prefab";
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (p == null)
            {
                informe.AppendLine("FALTA " + ruta + " (T-1382 no ha llegado): se omite lo que lo usa.");
            }
            return p;
        }

        /// <summary>Caja de los renderers del prefab (sin girar, escala 1) y centro respecto al pivote. Se instancia bajo <paramref name="padre"/> y se borra.</summary>
        private static void Medir(GameObject prefab, Transform padre, out Vector3 tam, out Vector3 centro)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(new Vector3(0f, -5000f, 0f), Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                tam = Vector3.one;
                centro = Vector3.zero;
            }
            else
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                tam = b.size;
                centro = b.center - go.transform.position;
            }
            Object.DestroyImmediate(go);
        }

        private static GameObject Poner(Transform padre, GameObject prefab, Vector3 pos, Quaternion giro, Vector3 escala, bool conCollider)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(pos, giro);
            go.transform.localScale = escala;
            if (!conCollider)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
            }
            return go;
        }

        private static bool Encuentra(Transform raiz, string ruta, out Transform t)
        {
            t = raiz.Find(ruta);
            if (t == null)
            {
                informe.AppendLine("No existe " + ruta + " en el prefab (nada que tocar ahí).");
            }
            return t != null;
        }

        private static Bounds? CajaDe(GameObject go)
        {
            Bounds? caja = null;
            foreach (Bounds b in go.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Concat(go.GetComponentsInChildren<Collider>(true).Select(c => c.bounds)))
            {
                if (caja.HasValue)
                {
                    Bounds w = caja.Value;
                    w.Encapsulate(b);
                    caja = w;
                }
                else
                {
                    caja = b;
                }
            }
            return caja;
        }

        /// <summary>Cajas de todo lo visible que ya hay en el prefab (menos lo de T1386, los pisos grandes y el fondo), para no encimar nada.</summary>
        private static List<Ocupado> Ocupados(Transform raiz, Transform mia)
        {
            Transform chatarraVieja = raiz.Find("Bajo/Chatarra");
            List<Ocupado> lista = new List<Ocupado>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.gameObject.activeInHierarchy || r.transform.IsChildOf(mia))
                {
                    continue;
                }
                Bounds b = r.bounds;
                bool piso = b.size.x > 60f || b.size.z > 60f || b.size.y < 0.4f;
                if (piso || r.transform.IsChildOf(raiz.Find("Acantilado") ?? mia) || r.transform.IsChildOf(raiz.Find("Fondo") ?? mia))
                {
                    continue;
                }
                Transform quien = null;
                bool desechable = false;
                if (chatarraVieja != null && r.transform.IsChildOf(chatarraVieja))
                {
                    Transform t = r.transform;
                    while (t.parent != chatarraVieja)
                    {
                        t = t.parent;
                    }
                    quien = t;
                    desechable = !t.name.StartsWith("CarroViejo");
                }
                lista.Add(new Ocupado { Caja = b, Quien = quien, Desechable = desechable });
            }
            return lista;
        }

        /// <summary>True si la caja no choca con nada; lo que choca y es chatarra vieja (de Bajo/Chatarra, no los carros) se quita.</summary>
        private static bool LibreOQuitar(Bounds caja, List<Ocupado> ocupados, bool quitar, ref int quitados)
        {
            List<Ocupado> choca = ocupados.Where(c => c.Caja.Intersects(caja)).ToList();
            if (choca.Any(c => !c.Desechable))
            {
                return false;
            }
            if (quitar)
            {
                foreach (Transform q in choca.Select(c => c.Quien).Distinct().Where(q => q != null).ToList())
                {
                    Object.DestroyImmediate(q.gameObject);
                    quitados++;
                }
                ocupados.RemoveAll(c => c.Quien == null ? false : !c.Quien);
                ocupados.RemoveAll(c => choca.Contains(c));
            }
            return true;
        }

        private static Vector3 Derecha(Vector3 tg)
        {
            return new Vector3(tg.z, 0f, -tg.x);
        }

        /// <summary>True si el punto cae sobre el tablero de la otra línea y a su misma altura (ahí no hay borde).</summary>
        private static bool EnOtroTablero(Vector3 punto, float y, List<Vector3> otraDensa)
        {
            return otraDensa.Any(q => Mathf.Abs(q.y - y) < 0.6f && T.Planta(q - punto).magnitude < MedioTablero - 0.2f);
        }

        // ---------------------------------------------------------------- Barreras AU02

        /// <summary>
        /// Barreras de concreto AU02 pegadas al borde exterior del tablero (principal y rampa norte), en los mismos sitios del guardarraíl AU05, que se
        /// apaga (solo tenía visuales, sin collider). El parapeto invisible con collider sigue cerrando el borde, así que no hay muros invisibles nuevos
        /// ni borde abierto.
        /// </summary>
        private static void Barreras(KitS2c.Obra o, Transform raiz, Transform padre)
        {
            GameObject prefab = Cargar(RutaBarrera, VarianteBarrera);
            if (prefab == null)
            {
                return;
            }
            Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
            bool largoX = tam.x >= tam.z;
            float largo = largoX ? tam.x : tam.z;
            float ancho = largoX ? tam.z : tam.x;
            float esc = Mathf.Min(AltoBarrera / Mathf.Max(0.05f, tam.y), AnchoMaxBarrera / Mathf.Max(0.05f, ancho));
            esc = Mathf.Clamp(esc, 0.3f, 3f);
            float paso = Mathf.Max(0.5f, largo * esc);
            informe.AppendLine("AU02" + VarianteBarrera + ": caja " + tam + ", escala " + esc.ToString("0.00") + ", paso " + paso.ToString("0.00") + " m");

            List<Vector3> principal = T.Principal();
            List<Vector3> rampa = T.RampaNorte();
            List<List<Vector3>> lineas = new List<List<Vector3>> { principal, rampa };
            int puestas = 0;
            for (int l = 0; l < lineas.Count; l++)
            {
                List<Vector3> linea = lineas[l];
                List<Vector3> otraDensa = T.Resamplear(lineas[(l + 1) % lineas.Count], 1f);
                float total = T.Longitud(linea);
                int n = Mathf.Max(1, Mathf.RoundToInt(total / paso));
                float pasoReal = total / n;
                float escLargo = pasoReal / paso;
                for (int k = 0; k < n; k++)
                {
                    T.PuntoEn(linea, total * (k + 0.5f) / n, out Vector3 pt, out Vector3 tg);
                    Vector3 der = Derecha(tg);
                    foreach (float lado in new[] { -1f, 1f })
                    {
                        Vector3 afuera = der * lado;
                        if (EnOtroTablero(pt + afuera * (MedioTablero - 0.35f), pt.y, otraDensa))
                        {
                            continue;
                        }
                        Quaternion giro = largoX ? Quaternion.LookRotation(Vector3.Cross(tg, Vector3.up)) : Quaternion.LookRotation(tg);
                        Vector3 escala = largoX ? new Vector3(esc * escLargo, esc, esc) : new Vector3(esc, esc, esc * escLargo);
                        Vector3 centroMundo = pt + afuera * (MedioTablero - ancho * esc * 0.5f) + Vector3.up * (tam.y * esc * 0.5f);
                        Vector3 desplazado = giro * Vector3.Scale(centro, escala);
                        GameObject b = Poner(padre, prefab, centroMundo - desplazado, giro, escala, false);
                        b.name = "AU02_" + puestas++;
                        GameObjectUtility.SetStaticEditorFlags(b, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    }
                }
            }
            informe.AppendLine("AU02: " + puestas + " barreras puestas");
            if (Encuentra(raiz, "Guardarrail", out Transform guardarrail))
            {
                guardarrail.gameObject.SetActive(false);
                informe.AppendLine("Se apagó T1356_Autopista/Guardarrail (AU05 solo visual, sin collider): lo reemplazan las AU02.");
            }
        }

        // ---------------------------------------------------------------- Chatarra AU03

        private static void ChatarraBajoPuente(KitS2c.Obra o, Transform raiz, Transform padre, List<Ocupado> ocupados)
        {
            GameObject a = Cargar(RutaChatarra, VarianteChatarra);
            GameObject b = Cargar(RutaChatarra, VarianteChatarraAlterna);
            if (a == null)
            {
                return;
            }
            List<Vector3> columnas = T.Columnas(T.Principal());
            columnas.AddRange(T.Columnas(T.RampaNorte()));
            List<Vector3> rampa = T.RampaNorte();
            int puestos = 0;
            int quitados = 0;
            const int meta = 18;
            for (int intento = 0; intento < 900 && puestos < meta; intento++)
            {
                GameObject prefab = puestos % 3 == 2 && b != null ? b : a;
                Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
                float yaw = 90f * o.Azar.Next(4);
                bool gira = yaw % 180f > 1f;
                Vector3 huella = gira ? new Vector3(tam.z, tam.y, tam.x) : tam;
                Vector3 c = new Vector3(o.Rango(-310f, 235f), 0f, o.Rango(239f, 257f));
                if (columnas.Any(q => Mathf.Abs(q.x - c.x) < 4f + huella.x * 0.5f && Mathf.Abs(q.z - c.z) < 3f + huella.z * 0.5f))
                {
                    continue;
                }
                if (rampa.Any(q => T.Planta(q - c).magnitude < 14f))
                {
                    continue;
                }
                if (c.z - huella.z * 0.5f < 236f || c.z + huella.z * 0.5f > 259f)
                {
                    continue;
                }
                Bounds caja = new Bounds(new Vector3(c.x, huella.y * 0.5f, c.z), huella);
                if (!LibreOQuitar(caja, ocupados, true, ref quitados))
                {
                    continue;
                }
                Quaternion giro = Quaternion.Euler(0f, yaw, 0f);
                Vector3 pos = new Vector3(c.x, 0f, c.z) - giro * new Vector3(centro.x, centro.y - tam.y * 0.5f, centro.z);
                GameObject go = Poner(padre, prefab, pos, giro, Vector3.one, true);
                go.name = "AU03_" + puestos;
                ocupados.Add(new Ocupado { Caja = caja, Quien = go.transform, Desechable = false });
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                puestos++;
            }
            informe.AppendLine("AU03: " + puestos + " montones bajo el tablero (z 236 a 259), " + quitados + " piezas viejas de Bajo/Chatarra quitadas por estar debajo");
        }

        // ---------------------------------------------------------------- Calle baja de K18

        /// <summary>
        /// Cambia las casas de la fila sur de la calle z = 216 (x -290 a -190, las que se ven a la izquierda en K18) por casas de 2 y 3 pisos con letreros.
        /// Quita por nombre las Casa_N de T1356_Autopista/Bajo/Casitas cuyo frente está en z = 210 en ese rango y pone el frente nuevo exacto sobre su huella.
        /// </summary>
        private static void CasasDeK18(KitS2c.Obra o, Transform raiz, Transform padre)
        {
            if (!Encuentra(raiz, "Bajo/Casitas", out Transform casitas))
            {
                return;
            }
            List<Transform> viejas = new List<Transform>();
            foreach (Transform c in casitas)
            {
                if (c.name.StartsWith("Casa_") && Mathf.Abs(c.position.z - ZFilaSur) < 0.5f && c.position.x > -290f && c.position.x < -190f)
                {
                    viejas.Add(c);
                }
            }
            if (viejas.Count == 0)
            {
                informe.AppendLine("Casas: no hay Casa_N en z=210, x -290 a -190; no se cambia la fila.");
                return;
            }
            float xa = float.MaxValue;
            float xb = float.MinValue;
            foreach (Transform c in viejas)
            {
                Bounds? caja = CajaDe(c.gameObject);
                if (caja.HasValue)
                {
                    xa = Mathf.Min(xa, caja.Value.min.x);
                    xb = Mathf.Max(xb, caja.Value.max.x);
                }
            }
            informe.AppendLine("Casas: se quitan " + viejas.Count + " casas viejas de Bajo/Casitas (" + string.Join(", ", viejas.Select(v => v.name)) + "), huella x " + xa.ToString("0.0") + " a " + xb.ToString("0.0"));
            foreach (Transform c in viejas)
            {
                Object.DestroyImmediate(c.gameObject);
            }
            System.Func<int, KitS2c.Estilo> estilo = i =>
            {
                KitS2c.Estilo e = KitS2c.EstiloAlAzar(o);
                e.Pisos = o.Chance(0.55f) ? 3 : 2;
                e.Neon = o.Chance(0.6f) ? (o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa) : (Color?)null;
                e.NeonBandera = true;
                e.Tinaco = true;
                return e;
            };
            KitS2c.Frente(o, padre, new Vector3(xa, 0f, ZFilaSur), new Vector3(xb, 0f, ZFilaSur), Vector3.forward, 6f, estilo);
            informe.AppendLine("Casas nuevas: " + o.Casas + " de 2 y 3 pisos con letreros");
        }

        private static void CalleBajaK18(KitS2c.Obra o, Transform raiz, Transform padre, List<Ocupado> ocupados)
        {
            // Cables entre los postes de la fila sur y a la fila norte (cuelgan sobre la calle, a 6 m, sin collider).
            if (Encuentra(raiz, "Bajo/Postes", out Transform postes))
            {
                List<Vector3> puntas = new List<Vector3>();
                foreach (Transform p in postes)
                {
                    if (p.name.StartsWith("Poste_") && Mathf.Abs(p.position.z - 211f) < 0.6f && p.position.x > -296f && p.position.x < -130f)
                    {
                        puntas.Add(p.position + Vector3.up * 6.75f);
                    }
                }
                puntas = puntas.OrderBy(p => p.x).ToList();
                for (int i = 0; i < puntas.Count; i++)
                {
                    if (i + 1 < puntas.Count)
                    {
                        KitS2c.Cable(o, puntas[i] - Vector3.up * 0.7f, puntas[i + 1] - Vector3.up * 0.7f, 0.7f);
                    }
                    if (i % 2 == 0)
                    {
                        KitS2c.Cable(o, puntas[i] - Vector3.up * 0.3f, new Vector3(puntas[i].x + 3f, 6.2f, 222.05f), 1.3f);
                    }
                }
                informe.AppendLine("Cables: " + puntas.Count + " postes unidos y con tiro a la fila norte");
            }

            // Semáforo en la banqueta sur (esquina del cruce hacia el este), de cara a la cámara de K18.
            int quitados = 0;
            for (float x = -205f; x > -250f; x -= 4f)
            {
                Bounds caja = new Bounds(new Vector3(x, 2f, 211.8f), new Vector3(1.2f, 4f, 1.2f));
                if (LibreOQuitar(caja, ocupados, false, ref quitados))
                {
                    GameObject s = ColocarT910.Semaforo(o, padre, new Vector3(x, 0f, 211.8f), Vector3.right, false);
                    s.name = "Semaforo_K18";
                    ocupados.Add(new Ocupado { Caja = caja });
                    informe.AppendLine("Semáforo en x=" + x + " (usa el ayudante ColocarT910.Semaforo, que trae sus lentes y su luz)");
                    break;
                }
            }

            // Carros estacionados en la orilla sur de la calle (z 212.5 a 214.3), a lo largo de x.
            string[] carros = { "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab", "Assets/Arte/Vehiculos/Prefabs/V01b_CarroPro.prefab" };
            int puestos = 0;
            for (float x = -280f; x < -195f && puestos < 6; x += o.Rango(12f, 20f))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(carros[puestos % carros.Length]);
                if (prefab == null)
                {
                    informe.AppendLine("FALTA " + carros[puestos % carros.Length]);
                    break;
                }
                Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
                Quaternion giro = Quaternion.Euler(0f, o.Chance(0.5f) ? 90f : -90f, 0f);
                float largo = Mathf.Max(tam.x, tam.z);
                float ancho = Mathf.Min(tam.x, tam.z);
                float z = 212.4f + ancho * 0.5f + 0.2f;
                Bounds caja = new Bounds(new Vector3(x, tam.y * 0.5f, z), new Vector3(largo + 0.4f, tam.y, ancho + 0.4f));
                if (!LibreOQuitar(caja, ocupados, false, ref quitados))
                {
                    continue;
                }
                Vector3 pos = new Vector3(x, 0.02f, z) - giro * new Vector3(centro.x, centro.y - tam.y * 0.5f, centro.z);
                GameObject c = Poner(padre, prefab, pos, giro, Vector3.one, true);
                c.name = "CarroEstacionado_" + puestos;
                ocupados.Add(new Ocupado { Caja = caja });
                puestos++;
            }
            informe.AppendLine("Carros estacionados en la calle baja: " + puestos);
        }

        // ---------------------------------------------------------------- Torre con reflectores y roca

        private static void TorreConReflectores(KitS2c.Obra o, Transform raiz, Transform padre)
        {
            GameObject t20 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTorres + "T20.prefab");
            GameObject t21 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTorres + "T21.prefab");
            GameObject t23 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTorres + "T23.prefab");
            if (t20 == null || t21 == null || t23 == null)
            {
                informe.AppendLine("FALTA T20, T21 o T23 en " + RutaTorres + ": no hay torre de reflectores.");
                return;
            }
            // Las torres del fondo que estorban en el hueco de la torre alta (x < -280) se apagan por nombre.
            if (Encuentra(raiz, "Fondo", out Transform fondo))
            {
                foreach (Transform t in fondo)
                {
                    if (t.name.StartsWith("Torre_") && t.position.x < -280f && t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        informe.AppendLine("Se apagó T1356_Autopista/Fondo/" + t.name + " para dejar sitio a la torre de reflectores.");
                    }
                }
            }
            Transform torre = Hijo(padre, "TorreReflectores");
            torre.position = new Vector3(-330f, 0f, 312f);
            const float ancho = 13f;
            const int pisos = 24;
            float y = Capa(torre, t20, "Base", 0f, 12f, ancho);
            for (int i = 0; i < pisos; i++)
            {
                y = Capa(torre, t21, "Piso", y, 6f, ancho);
            }
            y = Capa(torre, t23, "Remate", y, 12f, ancho);
            // Tres haces de luz hacia arriba y afuera: conos de halo (malla del kit, sin luces) con la punta en el remate.
            Vector3 cima = torre.position + Vector3.up * y;
            Vector3[] direcciones = { new Vector3(-0.35f, 1f, 0.05f), new Vector3(0.35f, 1f, 0.1f), new Vector3(0f, 1f, -0.4f) };
            const float largoHaz = 110f;
            for (int i = 0; i < direcciones.Length; i++)
            {
                Vector3 d = direcciones[i].normalized;
                KitS2c.Halo(o, padre, cima + Vector3.up * 40f, cima.y, KitS2c.Fria);
                Transform haz = padre.Find("Halo");
                if (haz == null)
                {
                    break;
                }
                haz.name = "Haz_" + i;
                haz.rotation = Quaternion.FromToRotation(Vector3.up, -d);
                haz.position = cima + d * largoHaz;
                haz.localScale = new Vector3(largoHaz * 0.2f, largoHaz, largoHaz * 0.2f);
            }
            informe.AppendLine("Torre de reflectores en (-330, 0, 312), alto " + y.ToString("0") + " m, 3 haces (sin luces nuevas)");
        }

        private static float Capa(Transform torre, GameObject prefab, string nombre, float y, float alto, float ancho)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, torre);
            g.name = nombre;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            g.transform.localPosition = Vector3.zero;
            Bounds b = g.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
            g.transform.localScale = new Vector3(ancho / Mathf.Max(0.01f, b.size.x), alto / Mathf.Max(0.01f, b.size.y), ancho / Mathf.Max(0.01f, b.size.z));
            // Se centra la caja en x,z del torre y se apoya en y.
            g.transform.localPosition = Vector3.zero;
            Bounds nb = g.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
            g.transform.position += new Vector3(torre.position.x - nb.center.x, torre.position.y + y - nb.min.y, torre.position.z - nb.center.z);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (Collider c in g.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return y + alto;
        }

        /// <summary>El acantilado ya tiene roca con textura (T1356_Roca, UV de mundo); se aclara para que el cerro no sea una silueta negra y se lea el relieve.</summary>
        private static void RocaDelCerro(Transform raiz)
        {
            if (!Encuentra(raiz, "Acantilado/Acantilado", out Transform acantilado))
            {
                return;
            }
            Material baseRoca = AssetDatabase.LoadAssetAtPath<Material>(RutaRocaBase);
            MeshRenderer mr = acantilado.GetComponent<MeshRenderer>();
            if (baseRoca == null || mr == null)
            {
                informe.AppendLine("Roca: falta " + RutaRocaBase + " o el renderer; no se cambia el material.");
                return;
            }
            Material roca = KitS2c.Mat("T1386_Roca", () =>
            {
                Material m = new Material(baseRoca);
                m.SetColor("_BaseColor", new Color(0.46f, 0.48f, 0.56f));
                m.SetFloat("_Smoothness", 0.15f);
                return m;
            });
            mr.sharedMaterial = roca;
            informe.AppendLine("Roca: el acantilado usa T1386_Roca (copia clara de T1356_Roca; mismo mapa de textura y UV de mundo, no es polígono plano liso).");
        }

        // ---------------------------------------------------------------- Validar

        [MenuItem("Mecha Cyberpunk/T-1386/Autopista (validar)")]
        public static void Validar()
        {
            StringBuilder s = new StringBuilder();
            int fallos = 0;
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mia = raiz.Find(NombreRaiz);
                if (mia == null)
                {
                    s.AppendLine("FALLO: no existe " + NombreRaiz + " (corre Aplicar).");
                    fallos++;
                }
                else
                {
                    int objetos = mia.GetComponentsInChildren<Transform>(true).Length;
                    s.AppendLine("Objetos bajo " + NombreRaiz + ": " + objetos + (objetos > MaxObjetosNuevos ? "  FALLO (tope " + MaxObjetosNuevos + ")" : ""));
                    fallos += objetos > MaxObjetosNuevos ? 1 : 0;
                    s.AppendLine("Luces del prefab: " + contenido.GetComponentsInChildren<Light>(true).Length);
                    s.AppendLine("Barreras AU02: " + (mia.Find("Barreras_AU02")?.childCount ?? 0) + ", montones AU03: " + (mia.Find("Chatarra_AU03")?.childCount ?? 0)
                        + ", casas nuevas: " + (mia.Find("Casas")?.childCount ?? 0));
                    // Nada de lo nuevo (casas, chatarra, carros, torre) choca con lo que ya había ni queda flotando o enterrado.
                    List<Ocupado> ocupados = Ocupados(raiz, mia);
                    foreach (string grupo in new[] { "Casas", "Chatarra_AU03", "CalleBaja" })
                    {
                        Transform g = mia.Find(grupo);
                        if (g == null)
                        {
                            continue;
                        }
                        foreach (Transform hijo in g)
                        {
                            Bounds? caja = CajaDe(hijo.gameObject);
                            if (!caja.HasValue)
                            {
                                continue;
                            }
                            Bounds interior = caja.Value;
                            interior.Expand(-0.3f);
                            Ocupado choca = ocupados.FirstOrDefault(c => c.Caja.Intersects(interior) && !(hijo.name.StartsWith("Casa_")));
                            if (choca != null && grupo != "Casas")
                            {
                                s.AppendLine("CHOQUE " + grupo + "/" + hijo.name + " con una caja en " + choca.Caja.center);
                                fallos++;
                            }
                            if (hijo.name.StartsWith("AU03_") || hijo.name.StartsWith("CarroEstacionado_"))
                            {
                                float baja = caja.Value.min.y;
                                if (baja < -0.15f || baja > 0.3f)
                                {
                                    s.AppendLine("ALTURA " + grupo + "/" + hijo.name + ": base en y=" + baja.ToString("0.00"));
                                    fallos++;
                                }
                            }
                            if (grupo == "Chatarra_AU03" || hijo.name.StartsWith("CarroEstacionado_"))
                            {
                                if (caja.Value.max.z > 236f && caja.Value.min.z < 236f && grupo == "Chatarra_AU03")
                                {
                                    s.AppendLine("CALLE " + hijo.name + " llega a z<236");
                                    fallos++;
                                }
                            }
                        }
                    }
                    Transform barr = mia.Find("Barreras_AU02");
                    if (barr != null)
                    {
                        int sinPiso = 0;
                        foreach (Transform b in barr)
                        {
                            Bounds? caja = CajaDe(b.gameObject);
                            if (caja.HasValue && b.GetComponentsInChildren<Collider>(true).Any(c => c.enabled))
                            {
                                sinPiso++;
                            }
                        }
                        s.AppendLine("Barreras con collider encendido (debe ser 0, el parapeto ya cierra el borde): " + sinPiso);
                        fallos += sinPiso;
                    }
                }
                if (raiz.Find("Tablero/Principal") == null)
                {
                    s.AppendLine("FALLO: falta Tablero/Principal");
                    fallos++;
                }
                if (raiz.Find("Guardarrail") != null && mia != null && mia.Find("Barreras_AU02")?.childCount > 0 && raiz.Find("Guardarrail").gameObject.activeSelf)
                {
                    s.AppendLine("AVISO: AU02 y AU05 están los dos encendidos en los bordes (se encimarían).");
                }
                int huecos = raiz.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Hueco_"));
                s.AppendLine("Huecos (piezas que faltaban): " + huecos);

                // Atorones abiertos: qué colliders hay alrededor de esos puntos (para revisar a mano si es geometría).
                foreach (Vector3 p in new[] { new Vector3(-204f, 5.6f, 233f), new Vector3(-157f, 10f, 252f) })
                {
                    Bounds zona = new Bounds(p, Vector3.one * 5f);
                    List<string> cerca = raiz.GetComponentsInChildren<Collider>(false).Where(c => c.enabled && c.bounds.Intersects(zona))
                        .Select(c => c.transform.name + " " + c.bounds.min.ToString("0.0") + "->" + c.bounds.max.ToString("0.0")).Take(12).ToList();
                    s.AppendLine("ATORON " + p + ": " + (cerca.Count == 0 ? "no hay ningún collider del prefab en 2.5 m (no es geometría de esta zona: tráfico o físico del carro)" : string.Join(" | ", cerca)));
                }
                // Túnel: cota de las bocas AU06 contra el piso del túnel (0.4 m de escalón reportado).
                foreach (string boca in new[] { "Tunel/AU06_Entrada", "Tunel/AU06_Salida" })
                {
                    Transform b = raiz.Find(boca);
                    Bounds? caja = b == null ? (Bounds?)null : CajaDe(b.gameObject);
                    s.AppendLine("TUNEL " + boca + ": " + (caja.HasValue ? "y mín " + caja.Value.min.y.ToString("0.00") + " (piso del túnel y=10.00); colliders propios: "
                        + b.GetComponentsInChildren<Collider>(true).Count(c => c.enabled) : "no está"));
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            s.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1386-validar.txt", s.ToString());
            Debug.Log("[Arte] T-1386 validar:\n" + s);
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>
        /// K18: desde la calle baja (3.2 m), un poco al este de una columna de la autopista y mirando al oeste-noroeste: el tablero cruza arriba a la
        /// derecha, la columna gruesa al frente, las casas de 3 pisos a la izquierda, la boca del túnel iluminada en el cerro al fondo y la torre.
        /// La rampa norte sube hacia el este (x > -107), así que no cabe en este encuadre: es el único elemento de K18 que el trazado no permite aquí.
        /// A18: aérea desde el sur-este de la boca. No guarda nada. Salida en docs/capturas/t-1386/.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1386/Autopista (fotos)")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1386");
            Directory.CreateDirectory("Logs");
            List<Vector3> columnas = T.Columnas(T.Principal());
            Vector3 col = columnas.Where(c => c.x < -200f && c.x > -300f).OrderBy(c => Mathf.Abs(c.x + 240f)).FirstOrDefault();
            if (col == Vector3.zero)
            {
                col = new Vector3(-240f, 10f, 250f);
            }
            Vector3 camaraK18 = new Vector3(col.x + 22f, PisoFoto, ZCalleBaja);
            Vector3 mira = new Vector3(-330f, 8f, 250f);
            ColocarS2cLook.Foto(camaraK18, mira, "Logs/t1386-autopista-k18-juego.png", 65f);
            ColocarS2.LadoALado("imageassets/referencias/look/K18_AutopistaElevada.png", "Logs/t1386-autopista-k18-juego.png", "docs/capturas/t-1386/autopista-k18.png");
            Vector3 camaraA18 = new Vector3(-230f, 48f, 195f);
            ColocarS2cLook.Foto(camaraA18, new Vector3(-325f, 14f, 248f), "Logs/t1386-autopista-a18-juego.png", 60f);
            ColocarS2.LadoALado("imageassets/referencias/look/A18_Autopista_Aerea.png", "Logs/t1386-autopista-a18-juego.png", "docs/capturas/t-1386/autopista-a18.png");
        }
    }
}
