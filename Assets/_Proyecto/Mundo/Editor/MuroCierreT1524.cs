using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1524: cierra el muro de la ciudad alrededor de TODO el mapa y vuelve sólidas las puertas MU03.
    /// No cambia el aspecto del muro de T-1513 (MU01, MU02, MU03 intactos): solo agrega colisión (todo bajo la raíz T1524_MuroCierre,
    /// que se borra y se rehace) y, donde un rayo encuentra un hueco que no es una puerta, tramos MU01 y torres MU02 iguales a los de T-1513.
    /// - Colliders altos (10 m) sobre cada tramo del muro viejo (el viejo mide 6 m y se podía trepar o saltar).
    /// - A cada puerta MU03, un BoxCollider sólido (no trigger) que cubre todo el vano. La matriz de capas de DynamicsManager es toda
    ///   verdadera (todo choca con todo), así que bloquea a pie y en carro sin tocar capas.
    /// Orden en batchmode (Unity cerrado):
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.MuroCierreT1524.Diagnostico  -> solo informe Logs/t1524-muro.txt (no guarda)
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.MuroCierreT1524.Aplicar      -> cierra, guarda y escribe el informe antes/después
    ///   3. Diagnostico otra vez: debe decir 0 huecos.
    /// </summary>
    public static class MuroCierreT1524
    {
        public const string NombreRaiz = "T1524_MuroCierre";
        public const string RutaLog = "Logs/t1524-muro.txt";
        public const string EtiquetaLog = "[T-1524] ";

        /// <summary>Alto de los colliders nuevos del muro (m); mínimo pedido 8.</summary>
        public const float AltoColision = 10f;
        public const float GrosorColision = 2f;

        private static readonly float[] AlturasRayo = { 1f, 3f };
        private const float AlturaRayoAlto = 7.5f;
        private const float RetiroRayo = 4f;
        private const float AlcanceRayo = 8f;
        private const float Tolerancia = 2.5f;
        private const float MitadTorre = 3.4f;

        public class Escaneo
        {
            public CalculoMuroT1524.Lado[] Lados;
            public List<CalculoMuroT1524.Hueco> Huecos = new List<CalculoMuroT1524.Hueco>();
            public List<CalculoMuroT1524.Hueco> Bajos = new List<CalculoMuroT1524.Hueco>();
            public int Muestras;
        }

        // ------------------------------------------------------------------ entradas

        [MenuItem("Mecha Cyberpunk/T-1524 Muro: diagnostico")]
        public static void Diagnostico()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1524 diagnostico del muro (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            Informar(sb, escena, null);
            Escribir(sb);
            Debug.Log(EtiquetaLog + "diagnostico escrito en " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1524 Muro: cerrar")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            GameObject viejo = GameObject.Find(NombreRaiz);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
            Physics.SyncTransforms();

            List<Transform> muros = MuroCiudadT1513.MurosViejos();
            if (muros.Count == 0)
            {
                throw new System.InvalidOperationException("[Mundo] T-1524: no hay muros en " + MuroCiudadT1513.RutaOrillas);
            }
            GameObject prefabA = MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabA);
            GameObject prefabB = MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabB);
            GameObject prefabTorre = MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabTorre);
            MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabPuerta);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1524 cierre del muro (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            sb.AppendLine("=== ANTES (sin " + NombreRaiz + ") ===");
            Escaneo antes = EscanearEscena();
            Informar(sb, escena, antes);

            float alto = muros.Max(m => m.lossyScale.y);
            List<CalculoMuroT1513.Segmento> segmentos = muros.Select(MuroCiudadT1513.Segmento).ToList();
            Vector3 tamTorre = MuroCiudadT1513.Medir(prefabTorre);
            CalculoMuroT1513.Plan plan = CalculoMuroT1513.Planear(segmentos, new Vector2(tamTorre.x, tamTorre.z));

            GameObject raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            float baseY = muros.Min(m => BaseY(m));

            // 1. Colliders altos sobre cada tramo del muro viejo.
            int altos = 0;
            foreach (Transform m in muros)
            {
                BoxCollider bc = m.GetComponent<BoxCollider>();
                if (bc == null)
                {
                    continue;
                }
                Vector3 s = m.lossyScale;
                GameObject g = new GameObject("MuroAlto_" + altos);
                g.transform.SetParent(raiz.transform, false);
                g.transform.SetPositionAndRotation(new Vector3(bc.bounds.center.x, baseY + AltoColision * 0.5f, bc.bounds.center.z), m.rotation);
                g.AddComponent<BoxCollider>().size = new Vector3(Mathf.Abs(s.x * bc.size.x), AltoColision, Mathf.Abs(s.z * bc.size.z));
                g.isStatic = true;
                altos++;
            }

            // 2. Puerta MU03: collider sólido que cubre todo el vano.
            GameObject objT1513 = GameObject.Find(MuroCiudadT1513.NombreRaiz);
            Transform raizT1513 = objT1513 != null ? objT1513.transform : null;
            int puertas = 0;
            foreach (CalculoMuroT1513.Puerta p in plan.Puertas)
            {
                float grosor = 2.5f;
                float baseP = baseY;
                Transform pieza = raizT1513 != null ? raizT1513.Find("Puerta_" + puertas) : null;
                if (pieza != null)
                {
                    Renderer[] rs = pieza.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        Bounds b = rs[0].bounds;
                        foreach (Renderer r in rs)
                        {
                            b.Encapsulate(r.bounds);
                        }
                        grosor = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z), 2.5f, 8f);
                        baseP = Mathf.Min(baseY, b.min.y);
                    }
                }
                GameObject g = new GameObject("PuertaSolida_" + puertas);
                g.transform.SetParent(raiz.transform, false);
                g.transform.SetPositionAndRotation(new Vector3(p.Centro.x, baseP + AltoColision * 0.5f, p.Centro.z), Quaternion.LookRotation(p.Dir));
                g.AddComponent<BoxCollider>().size = new Vector3(grosor, AltoColision, p.Ancho + 1f);
                g.isStatic = true;
                puertas++;
            }
            Physics.SyncTransforms();

            // 3. Huecos que quedan (no puertas): tramos MU01 y torres MU02 en esquinas.
            Escaneo medio = EscanearEscena();
            Vector3 tamA = MuroCiudadT1513.Medir(prefabA);
            Vector3 tamB = MuroCiudadT1513.Medir(prefabB);
            float escalaA = alto / tamA.y;
            float escalaB = alto / tamB.y;
            float largoA = Mathf.Max(tamA.x, tamA.z) * escalaA;
            float largoB = Mathf.Max(tamB.x, tamB.z) * escalaB;
            int tramos = 0;
            int torres = 0;
            foreach (CalculoMuroT1524.Hueco h in medio.Huecos)
            {
                CalculoMuroT1524.Lado lado = medio.Lados[h.Lado];
                float ini = Mathf.Max(-lado.Mitad, h.TIni - 1f);
                float fin = Mathf.Min(lado.Mitad, h.TFin + 1f);
                if (ini <= -lado.Mitad + CalculoMuroT1524.MargenEsquina + 3f)
                {
                    ini = -lado.Mitad;
                    torres += PonerTorreEsquina(prefabTorre, raiz.transform, raizT1513, lado.Punto(-lado.Mitad)) ? 1 : 0;
                    ini += MitadTorre;
                }
                if (fin >= lado.Mitad - CalculoMuroT1524.MargenEsquina - 3f)
                {
                    fin = lado.Mitad;
                    torres += PonerTorreEsquina(prefabTorre, raiz.transform, raizT1513, lado.Punto(lado.Mitad)) ? 1 : 0;
                    fin -= MitadTorre;
                }
                float largo = fin - ini;
                if (largo < 0.5f)
                {
                    continue;
                }
                CalculoMuroT1513.Reparto r = CalculoMuroT1513.Repartir(largo, largoA, largoB);
                float recorrido = 0f;
                for (int k = 0; k < r.EsB.Count; k++)
                {
                    bool esB = r.EsB[k];
                    float lp = (esB ? largoB : largoA) * r.Factor;
                    Vector3 centro = lado.Punto(ini + recorrido + lp * 0.5f);
                    recorrido += lp;
                    MuroCiudadT1513.Colocar(esB ? prefabB : prefabA, raiz.transform, "Cierre_" + tramos + "_" + k + (esB ? "b" : "a"),
                        centro, lado.Eje, esB ? escalaB : escalaA, r.Factor, true, 0f);
                }
                GameObject caja = new GameObject("CierreColision_" + tramos);
                caja.transform.SetParent(raiz.transform, false);
                Vector3 c = lado.Punto((ini + fin) * 0.5f);
                caja.transform.SetPositionAndRotation(new Vector3(c.x, baseY + AltoColision * 0.5f, c.z), Quaternion.LookRotation(lado.Eje));
                caja.AddComponent<BoxCollider>().size = new Vector3(GrosorColision, AltoColision, largo + 1f);
                caja.isStatic = true;
                tramos++;
            }
            Physics.SyncTransforms();

            sb.AppendLine();
            sb.AppendLine("=== APLICADO ===");
            sb.AppendLine("Colliders altos (" + AltoColision + " m) sobre el muro viejo: " + altos);
            sb.AppendLine("Puertas MU03 con collider solido: " + puertas);
            sb.AppendLine("Huecos fuera de puertas encontrados tras las puertas: " + medio.Huecos.Count + ", cerrados con MU01: " + tramos + ", torres MU02 nuevas en esquinas: " + torres);
            sb.AppendLine("Puertas y las calles que salian por ellas (quedan cerradas; el trafico lo arregla T-1526):");
            DescribirPuertas(sb, plan, medio.Lados);

            sb.AppendLine();
            sb.AppendLine("=== DESPUES ===");
            Escaneo despues = EscanearEscena();
            ResumenEscaneo(sb, despues);
            sb.AppendLine(despues.Huecos.Count == 0 && despues.Bajos.Count == 0 ? "RESULTADO: 0 huecos, 0 tramos bajos" : "RESULTADO: QUEDAN huecos o tramos bajos");
            Escribir(sb);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log(EtiquetaLog + "antes " + antes.Huecos.Count + " huecos y " + antes.Bajos.Count + " bajos; colliders altos " + altos + ", puertas solidas " + puertas
                + ", tramos MU01 " + tramos + ", torres " + torres + "; despues " + despues.Huecos.Count + " huecos y " + despues.Bajos.Count + " bajos; ver " + RutaLog);
        }

        // ------------------------------------------------------------------ escaneo

        /// <summary>Lados del rectángulo que forman los extremos del muro viejo (líneas centrales).</summary>
        public static CalculoMuroT1524.Lado[] LadosDelMuro()
        {
            List<Transform> muros = MuroCiudadT1513.MurosViejos();
            if (muros.Count == 0)
            {
                throw new System.InvalidOperationException("[Mundo] T-1524: no hay muros en " + MuroCiudadT1513.RutaOrillas);
            }
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            foreach (Transform m in muros)
            {
                CalculoMuroT1513.Segmento s = MuroCiudadT1513.Segmento(m);
                foreach (Vector3 p in new[] { s.A, s.B })
                {
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minZ = Mathf.Min(minZ, p.z);
                    maxZ = Mathf.Max(maxZ, p.z);
                }
            }
            return CalculoMuroT1524.Lados(minX, maxX, minZ, maxZ);
        }

        /// <summary>Recorre el perímetro cada 1 m con rayos horizontales de adentro hacia afuera a 1 m, 3 m y 7.5 m de altura.</summary>
        public static Escaneo EscanearEscena()
        {
            Physics.SyncTransforms();
            Escaneo e = new Escaneo { Lados = LadosDelMuro() };
            for (int i = 0; i < e.Lados.Length; i++)
            {
                CalculoMuroT1524.Lado lado = e.Lados[i];
                List<float> ts = CalculoMuroT1524.Muestras(lado);
                List<bool> cerrado = new List<bool>();
                List<bool> cerradoAlto = new List<bool>();
                foreach (float t in ts)
                {
                    Vector3 punto = lado.Punto(t);
                    bool ok = true;
                    foreach (float h in AlturasRayo)
                    {
                        ok &= Cerrado(punto, lado.Fuera, h);
                    }
                    cerrado.Add(ok);
                    // Un punto con hueco bajo no se cuenta otra vez como "bajo".
                    cerradoAlto.Add(!ok || Cerrado(punto, lado.Fuera, AlturaRayoAlto));
                    e.Muestras++;
                }
                e.Huecos.AddRange(CalculoMuroT1524.Huecos(i, ts, cerrado));
                e.Bajos.AddRange(CalculoMuroT1524.Huecos(i, ts, cerradoAlto));
            }
            return e;
        }

        private static bool Cerrado(Vector3 puntoLinea, Vector3 fuera, float altura)
        {
            Vector3 origen = puntoLinea - fuera * RetiroRayo + Vector3.up * altura;
            RaycastHit[] golpes = Physics.RaycastAll(origen, fuera, AlcanceRayo, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in golpes)
            {
                float d = Vector3.Dot(h.point - puntoLinea, fuera);
                if (Mathf.Abs(d) <= Tolerancia)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ informe

        private static void Informar(StringBuilder sb, Scene escena, Escaneo ya)
        {
            List<Transform> muros = MuroCiudadT1513.MurosViejos();
            Escaneo e = ya ?? EscanearEscena();
            GameObject objT1513 = GameObject.Find(MuroCiudadT1513.NombreRaiz);
            sb.AppendLine("Muro viejo (T530_Afueras/Orillas/Muro): " + muros.Count + " tramos, alto de sus colliders (m): " + string.Join(", ",
                muros.Select(m => m.GetComponent<BoxCollider>() != null ? m.GetComponent<BoxCollider>().bounds.size.y.ToString("F1") : "sin").Distinct()));
            if (objT1513 != null)
            {
                int tr = 0;
                int to = 0;
                int pu = 0;
                foreach (Transform t in objT1513.transform)
                {
                    tr += t.name.StartsWith("Muro_") ? 1 : 0;
                    to += t.name.StartsWith("Torre") ? 1 : 0;
                    pu += t.name.StartsWith("Puerta_") ? 1 : 0;
                }
                sb.AppendLine("T-1513: tramos MU01 " + tr + ", torres MU02 " + to + ", puertas MU03 " + pu);
            }
            else
            {
                sb.AppendLine("T-1513: no hay raiz " + MuroCiudadT1513.NombreRaiz);
            }
            sb.AppendLine("Raiz " + NombreRaiz + ": " + (GameObject.Find(NombreRaiz) != null ? "existe" : "no existe"));
            foreach (CalculoMuroT1524.Lado l in e.Lados)
            {
                sb.AppendLine("Lado " + l.Nombre + ": linea en " + l.Centro + ", largo " + (l.Mitad * 2f).ToString("F0") + " m");
            }
            ResumenEscaneo(sb, e);
            JugableFueraDelMuro(sb, e.Lados);
            PisoTerminaAntes(sb, e.Lados);
            EscapesAlternos(sb, e.Lados);
        }

        private static void ResumenEscaneo(StringBuilder sb, Escaneo e)
        {
            sb.AppendLine("Muestras de perimetro: " + e.Muestras + " (cada " + CalculoMuroT1524.Paso + " m, rayos a 1 y 3 m de altura)");
            sb.AppendLine("HUECOS (sin collider a 1 o 3 m): " + e.Huecos.Count);
            foreach (CalculoMuroT1524.Hueco h in e.Huecos)
            {
                CalculoMuroT1524.Lado l = e.Lados[h.Lado];
                sb.AppendLine("  " + l.Nombre + " t " + h.TIni.ToString("F1") + ".." + h.TFin.ToString("F1") + " (" + h.Largo.ToString("F1") + " m) entre " + l.Punto(h.TIni) + " y " + l.Punto(h.TFin));
            }
            sb.AppendLine("TRAMOS BAJOS (collider no llega a 7.5 m): " + e.Bajos.Count);
            foreach (CalculoMuroT1524.Hueco h in e.Bajos)
            {
                CalculoMuroT1524.Lado l = e.Lados[h.Lado];
                sb.AppendLine("  " + l.Nombre + " t " + h.TIni.ToString("F1") + ".." + h.TFin.ToString("F1") + " (" + h.Largo.ToString("F1") + " m)");
            }
        }

        /// <summary>Lo jugable (NavMesh, calles, nodos de trafico) contra el rectangulo del muro: lo que queda fuera se reporta.</summary>
        private static void JugableFueraDelMuro(StringBuilder sb, CalculoMuroT1524.Lado[] lados)
        {
            float minX = lados.Min(l => l.Centro.x - (l.Eje.x != 0f ? l.Mitad : 0f));
            float maxX = lados.Max(l => l.Centro.x + (l.Eje.x != 0f ? l.Mitad : 0f));
            float minZ = lados.Min(l => l.Centro.z - (l.Eje.z != 0f ? l.Mitad : 0f));
            float maxZ = lados.Max(l => l.Centro.z + (l.Eje.z != 0f ? l.Mitad : 0f));
            Rect dentro = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            sb.AppendLine("Rectangulo del muro (lineas): x " + minX + ".." + maxX + ", z " + minZ + ".." + maxZ);

            NavMeshTriangulation nav = NavMesh.CalculateTriangulation();
            if (nav.vertices.Length > 0)
            {
                Bounds b = new Bounds(nav.vertices[0], Vector3.zero);
                int fuera = 0;
                foreach (Vector3 v in nav.vertices)
                {
                    b.Encapsulate(v);
                    fuera += dentro.Contains(new Vector2(v.x, v.z)) ? 0 : 1;
                }
                sb.AppendLine("NavMesh: bounds " + b.min + ".." + b.max + "; vertices fuera del muro " + fuera + " de " + nav.vertices.Length
                    + (fuera > 0 ? " (esperado: las calles de afuera T530_Afueras/Salida_*, que las puertas dejan cerradas)" : ""));
            }
            else
            {
                sb.AppendLine("NavMesh: sin datos en la escena");
            }

            foreach (string nombre in new[] { "S2_Calles", "T530_Afueras" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g == null)
                {
                    continue;
                }
                Renderer[] rs = g.GetComponentsInChildren<Renderer>(false);
                if (rs.Length == 0)
                {
                    continue;
                }
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                sb.AppendLine("Bounds de " + nombre + ": " + b.min + ".." + b.max);
            }

            int nodos = 0;
            int nodosFuera = 0;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string n = mb != null ? mb.GetType().Name : "";
                if (n.Contains("Nodo") || n.Contains("Waypoint") || n.Contains("PuntoRuta"))
                {
                    nodos++;
                    Vector3 p = mb.transform.position;
                    nodosFuera += dentro.Contains(new Vector2(p.x, p.z)) ? 0 : 1;
                }
            }
            sb.AppendLine("Nodos de trafico (tipos Nodo/Waypoint/PuntoRuta): " + nodos + ", fuera del muro: " + nodosFuera + " (T-1526 los revisa)");
        }

        /// <summary>Rayos hacia abajo 3 m dentro del muro: si no hay piso ahi, el piso termina antes del muro.</summary>
        private static void PisoTerminaAntes(StringBuilder sb, CalculoMuroT1524.Lado[] lados)
        {
            GameObject raizNueva = GameObject.Find(NombreRaiz);
            int total = 0;
            foreach (CalculoMuroT1524.Lado l in lados)
            {
                List<float> ts = CalculoMuroT1524.Muestras(l);
                List<bool> hayPiso = new List<bool>();
                foreach (float t in ts)
                {
                    Vector3 o = l.Punto(t) - l.Fuera * 3f + Vector3.up * 4f;
                    bool piso = false;
                    foreach (RaycastHit h in Physics.RaycastAll(o, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        bool propio = raizNueva != null && h.collider.transform.IsChildOf(raizNueva.transform);
                        piso |= h.normal.y > 0.5f && !propio;
                    }
                    hayPiso.Add(piso);
                }
                List<CalculoMuroT1524.Hueco> sin = CalculoMuroT1524.Huecos(0, ts, hayPiso);
                total += sin.Count;
                foreach (CalculoMuroT1524.Hueco h in sin)
                {
                    sb.AppendLine("  PISO termina antes del muro: " + l.Nombre + " t " + h.TIni.ToString("F1") + ".." + h.TFin.ToString("F1") + " (" + h.Largo.ToString("F1") + " m)");
                }
            }
            sb.AppendLine("Tramos del perimetro sin piso 3 m adentro del muro: " + total);
        }

        /// <summary>Agua, puerto, autopista elevada, puentes y tuneles: nombres en la escena y plataformas pegadas al muro.</summary>
        private static void EscapesAlternos(StringBuilder sb, CalculoMuroT1524.Lado[] lados)
        {
            string[] claves = { "agua", "puerto", "malecon", "autopista", "puente", "tunel", "rampa" };
            List<string> hallados = new List<string>();
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raiz.activeInHierarchy)
                {
                    continue;
                }
                string n = raiz.name.ToLowerInvariant();
                if (claves.Any(c => n.Contains(c)))
                {
                    hallados.Add(raiz.name + " en " + raiz.transform.position);
                }
                foreach (Transform t in raiz.transform)
                {
                    string nh = t.name.ToLowerInvariant();
                    if (claves.Any(c => nh.Contains(c)) && hallados.Count < 40)
                    {
                        hallados.Add(raiz.name + "/" + t.name + " en " + t.position);
                    }
                }
            }
            sb.AppendLine("Objetos de agua/puerto/autopista/puente/tunel (raices e hijos directos, max 40): " + hallados.Count);
            foreach (string h in hallados)
            {
                sb.AppendLine("  " + h);
            }
            int agua = 0;
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                agua += c.gameObject.layer == 4 && c.gameObject.activeInHierarchy ? 1 : 0;
            }
            sb.AppendLine("Colliders en la capa Water: " + agua);

            // Plataformas (collider cuya cima esta entre 2 y 12 m) pegadas por dentro al muro: desde ahi se podria saltar.
            int plataformas = 0;
            foreach (CalculoMuroT1524.Lado l in lados)
            {
                Vector3 centro = l.Centro - l.Fuera * 6f;
                Vector3 mitad = new Vector3(Mathf.Abs(l.Eje.x) * l.Mitad + Mathf.Abs(l.Fuera.x) * 6f, 15f, Mathf.Abs(l.Eje.z) * l.Mitad + Mathf.Abs(l.Fuera.z) * 6f);
                foreach (Collider c in Physics.OverlapBox(new Vector3(centro.x, 8f, centro.z), mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    string n = c.name;
                    if (n.StartsWith("Muro") || n.StartsWith("Puerta") || n.StartsWith("Cierre") || n.StartsWith("Torre") || c.transform.root.name.StartsWith("T15"))
                    {
                        continue;
                    }
                    if (c.bounds.max.y >= 2f && c.bounds.max.y <= 12f && c.bounds.size.x > 3f && c.bounds.size.z > 3f && plataformas < 40)
                    {
                        plataformas++;
                        sb.AppendLine("  plataforma junto al muro " + l.Nombre + ": " + n + " cima " + c.bounds.max.y.ToString("F1") + " m en " + c.bounds.center);
                    }
                }
            }
            sb.AppendLine("Plataformas junto al muro (cima 2-12 m, max 40): " + plataformas);
        }

        private static void DescribirPuertas(StringBuilder sb, CalculoMuroT1513.Plan plan, CalculoMuroT1524.Lado[] lados)
        {
            int i = 0;
            Renderer[] todos = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (CalculoMuroT1513.Puerta p in plan.Puertas)
            {
                CalculoMuroT1524.Lado lado = lados.OrderBy(l => Mathf.Abs(Vector3.Dot(p.Centro - l.Centro, l.Fuera))).First();
                sb.AppendLine("  Puerta_" + i + " lado " + lado.Nombre + " centro " + p.Centro + " ancho " + p.Ancho.ToString("F0") + " m, hacia afuera " + lado.Fuera);
                Vector3 tam = new Vector3(Mathf.Abs(lado.Eje.x) * (p.Ancho + 6f) + 30f * Mathf.Abs(lado.Fuera.x), 40f,
                    Mathf.Abs(lado.Eje.z) * (p.Ancho + 6f) + 30f * Mathf.Abs(lado.Fuera.z));
                Bounds zona = new Bounds(new Vector3(p.Centro.x, 0f, p.Centro.z), tam);
                HashSet<string> calles = new HashSet<string>();
                foreach (Renderer r in todos)
                {
                    string n = r.name.ToLowerInvariant();
                    if ((n.Contains("asfalto") || n.Contains("calle") || n.Contains("carril") || n.Contains("avenida")) && r.bounds.Intersects(zona) && calles.Count < 6)
                    {
                        calles.Add(r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name);
                    }
                }
                sb.AppendLine("    calles que salian por aqui: " + (calles.Count == 0 ? "(ninguna con nombre de calle; ver T530_Afueras/Salida_*)" : string.Join("; ", calles)));
                i++;
            }
        }

        // ------------------------------------------------------------------ piezas

        private static float BaseY(Transform muro)
        {
            BoxCollider bc = muro.GetComponent<BoxCollider>();
            return bc != null ? bc.bounds.min.y : muro.position.y - muro.lossyScale.y * 0.5f;
        }

        private static bool PonerTorreEsquina(GameObject torre, Transform raiz, Transform raizT1513, Vector3 esquina)
        {
            foreach (Transform raizBusca in new[] { raizT1513, raiz })
            {
                if (raizBusca == null)
                {
                    continue;
                }
                foreach (Transform t in raizBusca)
                {
                    if (t.name.StartsWith("Torre") && Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(esquina.x, esquina.z)) < CalculoMuroT1513.RadioEsquina)
                    {
                        return false;
                    }
                }
            }
            MuroCiudadT1513.Colocar(torre, raiz, "TorreEsquina_T1524_" + raiz.childCount, esquina, Vector3.forward, 1f, 1f, false, 0f);
            return true;
        }

        private static void Escribir(StringBuilder sb)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, sb.ToString());
        }
    }
}
