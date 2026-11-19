using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1395 (ronda 2 de la Autopista elevada): corre DESPUÉS de ZonaAutopistaT1386.Aplicar. Edita el prefab Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab
    /// (no la escena) y pone todo lo nuevo bajo el hijo raíz "T1395_Autopista", que Aplicar borra y rehace (se puede correr dos veces). NO corras
    /// ColocarT1356Autopista.Aplicar: rehace la zona y borra los arreglos de T-1360 a T-1377.
    /// Qué hace: viste las casas reales de la calle baja (fila sur z=205 y fila oeste x=-252, ya con la fila oeste prolongada hasta la curva) con letreros
    /// de neón, aires, toldos, postes de madera con cables, carros estacionados y basura; prende los faroles AU01 (lámpara emisiva y luz de sodio); hace
    /// brillar por dentro la boca del túnel; separa las barreras AU02 de los faroles y asienta la chatarra AU03. Las fotos K18 y A18 salen desde donde se ve
    /// lo que muestra la referencia. Métodos: Aplicar, Validar (Logs/t1395-validar.txt) y Fotos (en una corrida de Unity aparte, no guarda nada).
    /// </summary>
    public static class ZonaAutopistaT1395
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1395_Autopista";
        public const string Raiz1386 = "T1386_Autopista";

        private const float YBanqueta = 0.2f;
        private const float YAsfalto = 0.02f;
        private const int MaxObjetosNuevos = 3000;
        private const int MaxNeones = 12;
        private const int TopeLuces = 40;
        private static readonly Color Sodio = new Color(1f, 0.55f, 0.2f);
        private static readonly string[] Basura = { "BS01a_Basura", "BS01b_Basura", "BS01c_Basura", "BS01d_Basura", "BS01e_Basura" };
        private static readonly string[] Cosas = { "C16_Tambo", "C11_Basurero", "BT01_BoteBasura", "C17_Huacales" };
        private static readonly string[] Carros =
        {
            "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab", "Assets/Arte/Vehiculos/Prefabs/V01b_CarroPro.prefab",
            "Assets/Arte/Vehiculos/Prefabs/V02.prefab", "Assets/Arte/Vehiculos/Prefabs/V03.prefab",
        };

        private static readonly StringBuilder informe = new StringBuilder();

        private sealed class Ocupado
        {
            public Bounds Caja;
            /// <summary>Si el renderer es de un montón AU03 de la ronda 1: el montón (para no chocarlo consigo mismo).</summary>
            public Transform Grupo;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1395/Autopista ronda 2 (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                if (raiz.Find(Raiz1386) == null)
                {
                    informe.AppendLine("FALTA " + Raiz1386 + " en el prefab: corre primero ZonaAutopistaT1386.Aplicar. No se cambió nada.");
                }
                else
                {
                    int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                    KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1395, raiz);
                    Transform casasNuevas = Hijo(o.Raiz, "CasasNuevas");
                    Transform letreros = Hijo(o.Raiz, "Detalles");
                    Transform postes = Hijo(o.Raiz, "PostesMadera");
                    Transform estacionados = Hijo(o.Raiz, "Estacionados");
                    Transform basura = Hijo(o.Raiz, "Basura");
                    Transform faroles = Hijo(o.Raiz, "Faroles");
                    Transform tunel = Hijo(o.Raiz, "BocaTunel");

                    AjustarRonda1(raiz);
                    List<Ocupado> ocupados = Ocupados(raiz, o.Raiz);
                    FilaOesteHastaLaCurva(o, casasNuevas, ocupados);
                    List<Transform> casas = CasasDeLaCalle(raiz, o.Raiz);
                    Vestir(o, letreros, casas, ocupados);
                    PostesYCables(o, raiz, postes, casas, ocupados);
                    Estacionar(o, estacionados, ocupados);
                    Tirar(o, basura, ocupados);
                    Faroles(o, raiz, faroles);
                    BocaDelTunel(o, tunel);

                    KitS2c.Terminar(o);
                    int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                    int objetos = o.Raiz.GetComponentsInChildren<Transform>(true).Length;
                    informe.AppendLine("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (suben " + (lucesDespues - lucesAntes) + ", tope " + TopeLuces + ")");
                    informe.AppendLine("Objetos nuevos bajo " + NombreRaiz + ": " + objetos + " (tope " + MaxObjetosNuevos + ")");
                    PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1395-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1395 aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Utilidades

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static GameObject Cargar(string codigo)
        {
            GameObject p = KitS2c.Prefab(codigo);
            if (p == null)
            {
                informe.AppendLine("FALTA " + codigo + ": se omite lo que lo usa.");
            }
            return p;
        }

        /// <summary>Caja de los renderers del prefab (sin girar, escala 1) y centro respecto al pivote.</summary>
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

        /// <summary>Caja de renderers y colliders de un objeto.</summary>
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

        /// <summary>Caja de los renderers encendidos de un objeto (lo que se ve).</summary>
        private static Bounds? CajaVisible(GameObject go)
        {
            Bounds? caja = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || r.name.StartsWith("Halo"))
                {
                    continue;
                }
                if (caja.HasValue)
                {
                    Bounds w = caja.Value;
                    w.Encapsulate(r.bounds);
                    caja = w;
                }
                else
                {
                    caja = r.bounds;
                }
            }
            return caja;
        }

        /// <summary>Cajas de todo lo visible que ya hay en el prefab (menos lo de T1395, los pisos grandes, el fondo, los halos y los cables), para no encimar nada.</summary>
        private static List<Ocupado> Ocupados(Transform raiz, Transform mia)
        {
            Transform acantilado = raiz.Find("Acantilado");
            Transform fondo = raiz.Find("Fondo");
            Transform chatarra = raiz.Find(Raiz1386 + "/Chatarra_AU03");
            List<Ocupado> lista = new List<Ocupado>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || (mia != null && r.transform.IsChildOf(mia)))
                {
                    continue;
                }
                if (r.name == "Cables" || r.name.StartsWith("Halo") || r.name.StartsWith("Haz_") || r.name.StartsWith("Charco") || r.name.StartsWith("Linea"))
                {
                    continue;
                }
                Bounds b = r.bounds;
                bool piso = b.size.x > 60f || b.size.z > 60f || b.size.y < 0.4f;
                if (piso || (acantilado != null && r.transform.IsChildOf(acantilado)) || (fondo != null && r.transform.IsChildOf(fondo)))
                {
                    continue;
                }
                Transform grupo = null;
                if (chatarra != null && r.transform.IsChildOf(chatarra) && r.transform != chatarra)
                {
                    Transform t = r.transform;
                    while (t.parent != chatarra)
                    {
                        t = t.parent;
                    }
                    grupo = t;
                }
                lista.Add(new Ocupado { Caja = b, Grupo = grupo });
            }
            return lista;
        }

        private static bool Libre(Bounds caja, List<Ocupado> ocupados)
        {
            return !ocupados.Any(c => c.Caja.Intersects(caja));
        }

        // ---------------------------------------------------------------- Ajustes a lo de la ronda 1

        /// <summary>
        /// Barreras AU02: se reactivan todas y se apagan las que quedan encima del pie de un farol AU01 (el pie está a 6.7 m del centro y la barrera ocupa de 6.5 a 7.2).
        /// Chatarra AU03: se asienta con su base en y = 0 y se quita (por nombre) la que choque con otra cosa. Se apunta todo en el log.
        /// </summary>
        private static void AjustarRonda1(Transform raiz)
        {
            Transform m1 = raiz.Find(Raiz1386);
            Transform lum = raiz.Find("Luminarias");
            List<Vector3> pies = new List<Vector3>();
            if (lum != null)
            {
                foreach (Transform l in lum)
                {
                    if (l.name.StartsWith("AU01_Luminaria"))
                    {
                        pies.Add(l.position);
                    }
                }
            }
            Transform barr = m1.Find("Barreras_AU02");
            int apagadas = 0;
            if (barr != null)
            {
                foreach (Transform b in barr)
                {
                    if (!b.gameObject.activeSelf)
                    {
                        b.gameObject.SetActive(true);
                    }
                    Bounds? caja = CajaVisible(b.gameObject);
                    if (!caja.HasValue)
                    {
                        continue;
                    }
                    Bounds w = caja.Value;
                    w.Expand(new Vector3(0.6f, 0f, 0.6f));
                    if (pies.Any(p => w.Contains(new Vector3(p.x, w.center.y, p.z)) && Mathf.Abs(p.y - w.min.y) < 1f))
                    {
                        b.gameObject.SetActive(false);
                        apagadas++;
                        informe.AppendLine("Barrera " + b.name + " apagada: estaba encima del pie de un farol.");
                    }
                }
            }
            informe.AppendLine("AU02: " + apagadas + " barreras apagadas por encimarse con faroles (su parapeto con collider sigue cerrando el borde).");

            Transform ch = m1.Find("Chatarra_AU03");
            if (ch == null)
            {
                return;
            }
            List<Ocupado> otros = Ocupados(raiz, null);
            int asentadas = 0;
            List<Transform> montones = ch.Cast<Transform>().Where(t => t.name.StartsWith("AU03_")).ToList();
            foreach (Transform m in montones)
            {
                Bounds? caja = CajaDe(m.gameObject);
                if (!caja.HasValue)
                {
                    continue;
                }
                float dy = -caja.Value.min.y;
                if (Mathf.Abs(dy) > 0.05f)
                {
                    m.position += Vector3.up * dy;
                    asentadas++;
                    informe.AppendLine("AU03 " + m.name + ": asentada (base estaba en y=" + caja.Value.min.y.ToString("0.00") + ").");
                }
            }
            int quitadas = 0;
            foreach (Transform m in montones)
            {
                Bounds? caja = CajaVisible(m.gameObject);
                if (!caja.HasValue)
                {
                    continue;
                }
                Bounds interior = caja.Value;
                interior.Expand(-0.4f);
                if (otros.Any(c => c.Grupo != m && c.Caja.Intersects(interior)))
                {
                    informe.AppendLine("AU03 " + m.name + " quitada: se encimaba con otra cosa en " + caja.Value.center.ToString("0.0"));
                    Object.DestroyImmediate(m.gameObject);
                    quitadas++;
                }
            }
            informe.AppendLine("AU03: " + montones.Count + " montones revisados, " + asentadas + " asentadas, " + quitadas + " quitadas.");
        }

        // ---------------------------------------------------------------- Casas de la calle baja

        /// <summary>La fila oeste de T-1386 llega a z=170; se prolonga a z=205 (tangente por fuera de la banqueta de la vuelta) para que la calle no se quede en un muro vacío.</summary>
        private static void FilaOesteHastaLaCurva(KitS2c.Obra o, Transform padre, List<Ocupado> ocupados)
        {
            (float x0, float x1, float z0, float z1) oeste = T.HuellasCasas[3];
            const float z0 = 171.5f;
            const float z1 = 205f;
            Bounds huella = new Bounds(new Vector3((oeste.x0 + oeste.x1) * 0.5f, 4.5f, (z0 + z1) * 0.5f), new Vector3(oeste.x1 - oeste.x0 + 0.4f, 9f, z1 - z0));
            if (!Libre(huella, ocupados))
            {
                informe.AppendLine("Fila oeste prolongada (x " + oeste.x0 + " a " + oeste.x1 + ", z " + z0 + " a " + z1 + "): la huella choca con algo; no se pone.");
                return;
            }
            KitS2c.Frente(o, padre, new Vector3(oeste.x1, 0f, z0), new Vector3(oeste.x1, 0f, z1), Vector3.right, oeste.x1 - oeste.x0, i =>
            {
                KitS2c.Estilo e = KitS2c.EstiloAlAzar(o);
                e.Pisos = o.Chance(0.5f) ? 3 : 2;
                e.Neon = null;
                e.Foco = false;
                e.Tinaco = true;
                return e;
            });
            foreach (Transform c in padre)
            {
                Bounds? caja = CajaDe(c.gameObject);
                if (caja.HasValue)
                {
                    ocupados.Add(new Ocupado { Caja = caja.Value });
                }
            }
            informe.AppendLine("Fila oeste prolongada: " + padre.childCount + " casas nuevas de 2 y 3 pisos en x " + oeste.x1 + ", z " + z0 + " a " + z1 + " (sin focos, sin neón propio: los pone Vestir).");
        }

        /// <summary>Las casas reales (Casa_N) cuyo frente cae en la fila sur de la calle baja (z=205) o en la fila oeste (x=-252). Se anotan con su posición en el log.</summary>
        private static List<Transform> CasasDeLaCalle(Transform raiz, Transform mia)
        {
            (float x0, float x1, float z0, float z1) sur = T.HuellasCasas[0];
            (float x0, float x1, float z0, float z1) oeste = T.HuellasCasas[3];
            List<Transform> lista = new List<Transform>();
            foreach (string grupo in new[] { Raiz1386 + "/Casas", "Bajo/Casitas", NombreRaiz + "/CasasNuevas" })
            {
                Transform g = raiz.Find(grupo);
                if (g == null)
                {
                    informe.AppendLine("Casas: no existe " + grupo);
                    continue;
                }
                foreach (Transform c in g)
                {
                    if (!c.name.StartsWith("Casa_") || !c.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    bool enSur = Mathf.Abs(c.position.z - sur.z1) < 0.6f && c.position.x > -215f && c.position.x < -120f;
                    bool enOeste = Mathf.Abs(c.position.x - oeste.x1) < 0.6f && c.position.z > 60f && c.position.z < 206f;
                    if (enSur || enOeste)
                    {
                        lista.Add(c);
                    }
                }
            }
            informe.AppendLine("Casas reales de la calle baja y la calle oeste (" + lista.Count + "):");
            foreach (Transform c in lista.OrderBy(c => c.position.x).ThenBy(c => c.position.z))
            {
                informe.AppendLine("  " + c.parent.name + "/" + c.name + " en (" + c.position.x.ToString("0.0") + ", " + c.position.z.ToString("0.0") + ")");
            }
            return lista;
        }

        private static Vector3 NormalDe(Transform casa)
        {
            return Mathf.Abs(casa.position.z - T.HuellasCasas[0].z1) < 0.6f ? Vector3.forward : Vector3.right;
        }

        /// <summary>Pone una pieza del kit pegada a la pared (su cara a -Z hacia afuera), centrada en el punto de la pared.</summary>
        private static GameObject PonerEnPared(Transform padre, string codigo, Vector3 puntoPared, Vector3 normal, float escala)
        {
            GameObject prefab = Cargar(codigo);
            if (prefab == null)
            {
                return null;
            }
            Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
            Quaternion giro = Quaternion.LookRotation(-normal);
            Vector3 centroDeseado = puntoPared + normal * (tam.z * escala * 0.5f + 0.02f);
            Vector3 pos = centroDeseado - giro * (centro * escala);
            GameObject go = Poner(padre, prefab, pos, giro, Vector3.one * escala, false);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        /// <summary>
        /// Letreros de neón (bandera magenta y turquesa, pared naranja, con su Point light chica de LuzNocturna), aires y toldos en las fachadas de las casas.
        /// Primero las que quedan más cerca de la cámara de K18; tope de neones = tope de luces nuevas.
        /// </summary>
        private static void Vestir(KitS2c.Obra o, Transform padre, List<Transform> casas, List<Ocupado> ocupados)
        {
            Vector3 camara = new Vector3(-148f, 2.8f, 217f);
            int neones = 0;
            int aires = 0;
            int toldos = 0;
            int i = 0;
            Color[] colores = { KitS2c.Magenta, KitS2c.Turquesa, KitS2c.Sodio, KitS2c.Turquesa, KitS2c.Magenta };
            foreach (Transform casa in casas.OrderBy(c => (c.position - camara).sqrMagnitude))
            {
                Vector3 normal = NormalDe(casa);
                Vector3 lado = Vector3.Cross(Vector3.up, normal).normalized;
                Bounds? caja = CajaDe(casa.gameObject);
                if (!caja.HasValue)
                {
                    continue;
                }
                float ancho = Mathf.Abs(lado.x) > 0.5f ? caja.Value.size.x : caja.Value.size.z;
                float alto = caja.Value.max.y;
                Transform neonViejo = casa.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Neon");
                float signo = i % 2 == 0 ? 1f : -1f;
                if (neonViejo != null)
                {
                    signo = Vector3.Dot(neonViejo.position - casa.position, lado) > 0f ? -1f : 1f;
                }
                // Neón: uno por casa (de pared naranja si toca) hasta el tope.
                if (neones < MaxNeones)
                {
                    Color color = colores[i % colores.Length];
                    bool bandera = color != KitS2c.Sodio;
                    Vector3 p = casa.position + lado * (signo * ancho * 0.38f) + Vector3.up * (bandera ? 3.2f : 2.4f);
                    KitS2c.Neon(o, padre, p, normal, lado, color, bandera);
                    neones++;
                }
                // Aire de pared en el segundo piso, del lado contrario al neón.
                if (alto > 5.5f && o.Chance(0.6f))
                {
                    Vector3 p = casa.position + lado * (-signo * ancho * 0.3f) + Vector3.up * 4.7f;
                    GameObject aire = PonerEnPared(padre, "C10_AireAcondicionado", p, normal, 1f);
                    if (aire != null)
                    {
                        aire.name = "Aire_" + aires++;
                    }
                }
                // Toldo sobre la puerta si la casa no tiene uno.
                if (casa.Find("Toldo") == null && o.Chance(0.6f) && Toldo(padre, casa, normal))
                {
                    toldos++;
                }
                i++;
            }
            informe.AppendLine("Casas vestidas: " + i + "; letreros de neón " + neones + " (cada uno con su luz chica), aires " + aires + ", toldos " + toldos + ".");
        }

        /// <summary>Toldo TL01 sobre el vano de la planta baja, igual que KitS2c.Toldo (que es privado). false si la casa no tiene vano o el kit no tiene la pieza.</summary>
        private static bool Toldo(Transform padre, Transform casa, Vector3 normal)
        {
            Transform baja = casa.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("C01_Fachada") || t.name.StartsWith("C02_Fachada"));
            if (baja == null)
            {
                return false;
            }
            string codigo = baja.name.StartsWith("C02_Fachada") ? KitS2c.PlantaVivienda : KitS2c.PlantaLocal;
            KitS2c.Medida m = KitS2c.Medir(codigo);
            if (!m.Vano.HasValue || KitS2c.Prefab(KitS2c.CodigoToldo) == null)
            {
                return false;
            }
            KitS2c.Medida mt = KitS2c.Medir(KitS2c.CodigoToldo);
            Rect v = m.Vano.Value;
            float e = (v.width * baja.localScale.x + 0.5f) / mt.Caja.size.x;
            Vector3 dintel = baja.TransformPoint(new Vector3(v.center.x, v.yMax, m.Muro));
            GameObject toldo = KitS2c.Pieza(padre, KitS2c.CodigoToldo, dintel - Vector3.up * 0.1f + normal * (mt.Caja.size.z * e * 0.5f), Quaternion.LookRotation(-normal), Vector3.one * e, false);
            toldo.name = "Toldo_" + casa.name;
            return true;
        }

        // ---------------------------------------------------------------- Postes de madera y cables


        /// <summary>
        /// Postes del Cerro (C07_PosteLuz con transformador, KitS2c.Poste) en la banqueta de la calle baja (frente a las casas) y de la calle oeste,
        /// con tres cables entre postes, cables hacia los postes de luz C07 vecinos y acometidas a la pared de las casas.
        /// </summary>
        private static void PostesYCables(KitS2c.Obra o, Transform raiz, Transform padre, List<Transform> casas, List<Ocupado> ocupados)
        {
            List<Vector3> topesC07 = new List<Vector3>();
            Transform c07 = raiz.Find("Bajo/Postes");
            if (c07 != null)
            {
                foreach (Transform p in c07)
                {
                    if (p.name.StartsWith("Poste_"))
                    {
                        topesC07.Add(p.position + Vector3.up * 6.75f);
                    }
                }
            }
            // Fila sur: línea a lo largo de x, en la banqueta (z = 207.4); fila oeste: línea a lo largo de z, en x = -250.4.
            Vector3[] direcciones = { Vector3.right, Vector3.forward };
            Vector3[] normalesCasa = { Vector3.forward, Vector3.right };
            for (int fila = 0; fila < 2; fila++)
            {
                Vector3 d = direcciones[fila];
                Vector3 perp = Vector3.Cross(Vector3.up, d).normalized;
                List<float> posiciones = fila == 0
                    ? new List<float> { -208f, -181f, -154f, -130f }
                    : new List<float> { 99f, 121f, 143f, 165f, 187f };
                List<Vector3> topes = new List<Vector3>();
                foreach (float s in posiciones)
                {
                    Vector3 pie = fila == 0 ? new Vector3(s, YBanqueta, 207.4f) : new Vector3(-250.4f, YBanqueta, s);
                    Bounds caja = new Bounds(pie + Vector3.up * 3.5f, new Vector3(0.9f, 7f, 0.9f));
                    if (!Libre(caja, ocupados))
                    {
                        informe.AppendLine("Poste de madera en " + pie.ToString("0.0") + ": hay algo; no se pone.");
                        continue;
                    }
                    ocupados.Add(new Ocupado { Caja = caja });
                    // Modelo C07_PosteLuz con transformador, el mismo que usa el Cerro (KitS2c.Poste): trae su lámpara de sodio, su halo y devuelve la punta.
                    topes.Add(KitS2c.Poste(o, padre, pie, fila == 0 ? Vector3.forward : Vector3.right, YBanqueta, topes.Count % 2 == 0));
                }
                // Tres cables entre postes seguidos (en los extremos del travesaño y al centro).
                for (int k = 0; k + 1 < topes.Count; k++)
                {
                    foreach (float desv in new[] { -0.8f, 0f, 0.8f })
                    {
                        KitS2c.Cable(o, topes[k] + perp * desv, topes[k + 1] + perp * desv, o.Rango(0.7f, 1.1f));
                    }
                }
                // Hacia el poste de luz más cercano (a menos de 16 m) y acometidas a la pared de las casas.
                Vector3 normalCasa = normalesCasa[fila];
                foreach (Vector3 tope in topes)
                {
                    Vector3? cercano = topesC07.Where(q => T.Planta(q - tope).magnitude < 16f).OrderBy(q => T.Planta(q - tope).magnitude).Select(q => (Vector3?)q).FirstOrDefault();
                    if (cercano.HasValue)
                    {
                        KitS2c.Cable(o, tope + Vector3.up * -0.25f, cercano.Value, 0.8f);
                    }
                    List<Transform> vecinas = casas.Where(c => NormalDe(c) == normalCasa).OrderBy(c => T.Planta(c.position - tope).magnitude).Take(2).ToList();
                    foreach (Transform c in vecinas)
                    {
                        Vector3 pared = c.position + Vector3.up * o.Rango(5.2f, 5.8f) + normalCasa * 0.08f;
                        KitS2c.Cable(o, tope + Vector3.up * -0.1f, pared, 1.3f);
                    }
                }
                informe.AppendLine((fila == 0 ? "Calle baja" : "Calle oeste") + ": " + topes.Count + " postes de madera con cables (3 por tramo, a los C07 y a las casas).");
            }
        }

        // ---------------------------------------------------------------- Carros estacionados y basura

        private static void Estacionar(KitS2c.Obra o, Transform padre, List<Ocupado> ocupados)
        {
            int puestos = 0;
            // Calle baja: carril de estacionamiento pegado a la banqueta sur (z 208 a 210.5), a lo largo de x.
            for (float x = -212f; x < -128f && puestos < 4; x += o.Rango(11f, 17f))
            {
                if (PonerCarro(o, padre, ocupados, new Vector3(x, 0f, 0f), true, puestos))
                {
                    puestos++;
                }
            }
            // Calle oeste: pegados a la banqueta oeste (x -249 a -246.5), a lo largo de z.
            int oeste = 0;
            for (float z = 104f; z < 176f && oeste < 3; z += o.Rango(18f, 26f))
            {
                if (PonerCarro(o, padre, ocupados, new Vector3(0f, 0f, z), false, puestos + oeste))
                {
                    oeste++;
                }
            }
            informe.AppendLine("Carros estacionados nuevos: " + puestos + " en la calle baja y " + oeste + " en la calle oeste.");
        }

        private static bool PonerCarro(KitS2c.Obra o, Transform padre, List<Ocupado> ocupados, Vector3 punto, bool alLargoDeX, int numero)
        {
            string ruta = Carros[numero % Carros.Length];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                informe.AppendLine("FALTA " + ruta);
                return false;
            }
            Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
            float largo = Mathf.Max(tam.x, tam.z);
            float ancho = Mathf.Min(tam.x, tam.z);
            bool largoEnX = tam.x >= tam.z;
            // El eje largo del carro debe quedar a lo largo de la calle: sin girar si ya está así, 90 grados si no.
            float yaw = (largoEnX == alLargoDeX ? 0f : 90f) + (o.Chance(0.5f) ? 0f : 180f);
            Quaternion giro = Quaternion.Euler(0f, yaw, 0f);
            Vector3 c = alLargoDeX ? new Vector3(punto.x, 0f, T.ZCalleBaja - 7f + 0.4f + ancho * 0.5f) : new Vector3(-249f + 0.4f + ancho * 0.5f, 0f, punto.z);
            Vector3 huella = alLargoDeX ? new Vector3(largo + 0.6f, tam.y, ancho + 0.4f) : new Vector3(ancho + 0.4f, tam.y, largo + 0.6f);
            Bounds caja = new Bounds(new Vector3(c.x, tam.y * 0.5f + YAsfalto, c.z), huella);
            if (!Libre(caja, ocupados))
            {
                return false;
            }
            Vector3 pos = new Vector3(c.x, YAsfalto, c.z) - giro * new Vector3(centro.x, centro.y - tam.y * 0.5f, centro.z);
            GameObject car = Poner(padre, prefab, pos, giro, Vector3.one, true);
            car.name = "CarroEstacionado_" + padre.childCount;
            ocupados.Add(new Ocupado { Caja = caja });
            return true;
        }

        /// <summary>Basura, tambos, huacales y botes en la banqueta, pegados a las fachadas (con la altura de la banqueta, y = 0.2).</summary>
        private static void Tirar(KitS2c.Obra o, Transform padre, List<Ocupado> ocupados)
        {
            int puestas = 0;
            for (int intento = 0; intento < 600 && puestas < 34; intento++)
            {
                bool sur = puestas % 3 != 2;
                string codigo = puestas % 3 == 0 ? o.Uno(Cosas) : o.Uno(Basura);
                GameObject prefab = Cargar(codigo);
                if (prefab == null)
                {
                    continue;
                }
                Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
                Vector3 c = sur ? new Vector3(o.Rango(-214f, -122f), 0f, o.Rango(205.9f, 207.2f)) : new Vector3(o.Rango(-251.6f, -249.8f), 0f, o.Rango(100f, 203f));
                Quaternion giro = Quaternion.Euler(0f, o.Rango(0f, 360f), 0f);
                float r = Mathf.Max(tam.x, tam.z) * 0.5f;
                Bounds caja = new Bounds(new Vector3(c.x, YBanqueta + tam.y * 0.5f, c.z), new Vector3(r * 2f + 0.3f, tam.y, r * 2f + 0.3f));
                if (!Libre(caja, ocupados))
                {
                    continue;
                }
                Vector3 pos = new Vector3(c.x, YBanqueta, c.z) - giro * new Vector3(centro.x, centro.y - tam.y * 0.5f, centro.z);
                GameObject go = Poner(padre, prefab, pos, giro, Vector3.one, false);
                go.name = "Basura_" + puestas;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                ocupados.Add(new Ocupado { Caja = caja });
                puestas++;
            }
            informe.AppendLine("Basura y objetos de calle en las banquetas: " + puestas);
        }

        // ---------------------------------------------------------------- Faroles AU01

        /// <summary>
        /// Cada farol AU01 (uno cada 36 m, alternando lados) recibe una placa emisiva de sodio bajo la cabeza de la lámpara (con el material EmisionNoche del proyecto, que
        /// brilla de noche) y su luz pasa a sodio (1, 0.55, 0.2), con la luz puesta en la cabeza. Si a un farol le quitaron la luz, se le pone otra (se apunta).
        /// </summary>
        private static void Faroles(KitS2c.Obra o, Transform raiz, Transform padre)
        {
            Transform lum = raiz.Find("Luminarias");
            if (lum == null)
            {
                informe.AppendLine("Faroles: no existe Luminarias en el prefab.");
                return;
            }
            Material mat = KitS2c.Mat("T1395_LamparaSodio", () => KitS2c.Emision(new Color(3.6f, 1.9f, 0.6f), Texture2D.whiteTexture));
            int n = 0;
            int restauradas = 0;
            foreach (Transform l in lum)
            {
                if (!l.name.StartsWith("AU01_Luminaria"))
                {
                    continue;
                }
                MeshFilter mf = l.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh != null).OrderByDescending(m => m.sharedMesh.vertexCount).FirstOrDefault();
                Vector3 cabeza = Vector3.zero;
                bool encontrada = mf != null && ColocarS2.CabezaLampara(mf, out cabeza);
                Light luz = l.GetComponentInChildren<Light>(true);
                if (!encontrada)
                {
                    cabeza = luz != null ? luz.transform.position : l.position + Vector3.up * 8.7f;
                }
                if (luz == null)
                {
                    luz = KitS2c.Luz(l, cabeza, LightType.Spot, Sodio, KitS2c.IntensidadPoste * 1.3f, 26f, 100f, Vector3.down);
                    restauradas++;
                    informe.AppendLine("Farol " + l.name + ": no tenía luz; se le puso un spot de sodio.");
                }
                luz.color = Sodio;
                luz.range = Mathf.Max(luz.range, 26f);
                luz.shadows = LightShadows.None;
                if (encontrada)
                {
                    luz.transform.position = cabeza + Vector3.down * 0.05f;
                }
                Vector3 dir = T.Planta(cabeza - l.position);
                if (dir.sqrMagnitude < 0.04f)
                {
                    dir = l.forward;
                }
                GameObject placa = KitS2c.Losa(o, padre, "LamparaSodio_" + n, cabeza + Vector3.up * 0.06f, Quaternion.LookRotation(dir.normalized), new Vector3(0.42f, 0.06f, 0.95f), mat, 1f, false);
                GameObjectUtility.SetStaticEditorFlags(placa, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                n++;
            }
            informe.AppendLine("Faroles AU01: " + n + " con placa emisiva de sodio y luz de sodio (" + restauradas + " con luz repuesta).");
        }

        // ---------------------------------------------------------------- Boca del túnel

        /// <summary>
        /// El interior de la boca brilla como en K18: tiras emisivas en las dos paredes (a 1.1 y 4.6 m) y dos hileras de placas en el techo a lo largo de los primeros 46 m del
        /// túnel (toda la vuelta visible), una luz puntual dentro y un spot desde la boca hacia afuera que baña la calle y la rampa.
        /// </summary>
        private static void BocaDelTunel(KitS2c.Obra o, Transform padre)
        {
            Material pared = KitS2c.Mat("T1395_TunelPared", () => KitS2c.Emision(new Color(2.8f, 2.3f, 1.5f), Texture2D.whiteTexture));
            Material techo = KitS2c.Mat("T1395_TunelTecho", () => KitS2c.Emision(new Color(3.4f, 3.0f, 2.2f), Texture2D.whiteTexture));
            List<Vector3> eje = T.Tunel();
            int piezas = 0;
            for (int k = 0; k < 12; k++)
            {
                T.PuntoEn(eje, 2f + 4f * k, out Vector3 pt, out Vector3 tg);
                Vector3 der = new Vector3(tg.z, 0f, -tg.x);
                Quaternion giro = Quaternion.LookRotation(tg);
                foreach (float lado in new[] { -1f, 1f })
                {
                    foreach (float alto in new[] { 1.1f, 4.6f })
                    {
                        KitS2c.Losa(o, padre, "TiraPared_" + k + "_" + piezas++, pt + der * (lado * 6.9f) + Vector3.up * alto, giro, new Vector3(0.12f, 0.22f, 3.6f), pared, 1f, false);
                    }
                    KitS2c.Losa(o, padre, "PlacaTecho_" + k + "_" + piezas++, pt + der * (lado * 2.7f) + Vector3.up * (6.5f - 0.06f), giro, new Vector3(1f, 0.1f, 3.4f), techo, 1f, false);
                }
            }
            T.PuntoEn(eje, 6f, out Vector3 dentro, out _);
            KitS2c.Luz(padre, dentro + Vector3.up * 5.5f, LightType.Point, new Color(1f, 0.85f, 0.55f), 260f, 30f, 0f);
            T.PuntoEn(eje, 3f, out Vector3 boca, out _);
            KitS2c.Luz(padre, boca + Vector3.up * 5f, LightType.Spot, new Color(1f, 0.85f, 0.55f), 300f, 42f, 110f, new Vector3(1f, -0.35f, 0f));
            informe.AppendLine("Boca del túnel: " + piezas + " tiras y placas emisivas en los primeros 46 m, 1 luz puntual dentro y 1 spot hacia afuera.");
        }

        // ---------------------------------------------------------------- Validar

        [MenuItem("Mecha Cyberpunk/T-1395/Autopista ronda 2 (validar)")]
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
                    int lucesMias = mia.GetComponentsInChildren<Light>(true).Length;
                    s.AppendLine("Luces del prefab: " + contenido.GetComponentsInChildren<Light>(true).Length + "; luces nuevas bajo " + NombreRaiz + ": " + lucesMias + (lucesMias > TopeLuces ? "  FALLO (tope " + TopeLuces + ")" : ""));
                    fallos += lucesMias > TopeLuces ? 1 : 0;
                    s.AppendLine("Casas nuevas: " + (mia.Find("CasasNuevas")?.childCount ?? 0) + ", letreros de neón: " + mia.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Neon")
                        + ", aires: " + mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Aire_")) + ", toldos: " + mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Toldo_"))
                        + ", postes de madera: " + (mia.Find("PostesMadera")?.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Poste_")) ?? 0)
                        + ", carros: " + (mia.Find("Estacionados")?.childCount ?? 0) + ", basura: " + (mia.Find("Basura")?.childCount ?? 0));
                    if (!raiz.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.name == "Cables" && r.transform.IsChildOf(mia)))
                    {
                        s.AppendLine("FALLO: no hay malla de cables bajo " + NombreRaiz);
                        fallos++;
                    }

                    // Nada de lo nuevo choca con lo que ya había ni con lo demás de T1395, ni flota ni está enterrado.
                    List<Ocupado> ocupados = Ocupados(raiz, mia);
                    List<(string grupo, Transform hijo, Bounds caja)> propios = new List<(string, Transform, Bounds)>();
                    foreach (string grupo in new[] { "Estacionados", "Basura", "PostesMadera" })
                    {
                        Transform g = mia.Find(grupo);
                        if (g == null)
                        {
                            continue;
                        }
                        foreach (Transform hijo in g)
                        {
                            Bounds? caja = CajaVisible(hijo.gameObject) ?? CajaDe(hijo.gameObject);
                            if (!caja.HasValue)
                            {
                                continue;
                            }
                            Bounds interior = caja.Value;
                            interior.Expand(-0.3f);
                            Ocupado choca = ocupados.FirstOrDefault(c => c.Caja.Intersects(interior));
                            if (choca != null)
                            {
                                s.AppendLine("CHOQUE " + grupo + "/" + hijo.name + " con una caja en " + choca.Caja.center.ToString("0.0"));
                                fallos++;
                            }
                            float baja = caja.Value.min.y;
                            float esperada = grupo == "Estacionados" ? YAsfalto : YBanqueta;
                            if (Mathf.Abs(baja - esperada) > 0.15f)
                            {
                                s.AppendLine("ALTURA " + grupo + "/" + hijo.name + ": base en y=" + baja.ToString("0.00") + " (debía estar en " + esperada.ToString("0.00") + ")");
                                fallos++;
                            }
                            // Carril: solo los carros estacionados pueden estar sobre el asfalto (z 208 a 222 en la calle baja, x -249 a -235 en la oeste).
                            bool enCalleBaja = caja.Value.max.z > 208.2f && caja.Value.min.z < 221.8f && caja.Value.min.x > -214f && caja.Value.max.x < -118f;
                            bool enCalleOeste = caja.Value.max.x > -248.8f && caja.Value.min.x < -235.2f && caja.Value.min.z > 60f && caja.Value.max.z < 185f;
                            if (grupo != "Estacionados" && (enCalleBaja || enCalleOeste))
                            {
                                s.AppendLine("CARRIL " + grupo + "/" + hijo.name + " invade el asfalto de la calle.");
                                fallos++;
                            }
                            if (grupo == "Estacionados" && caja.Value.max.z > 211.2f && caja.Value.min.x > -214f && caja.Value.max.x < -118f)
                            {
                                s.AppendLine("CARRIL " + hijo.name + " ocupa más de 3.2 m desde la banqueta (estorba el paso).");
                                fallos++;
                            }
                            propios.Add((grupo, hijo, caja.Value));
                        }
                    }
                    for (int a = 0; a < propios.Count; a++)
                    {
                        for (int b = a + 1; b < propios.Count; b++)
                        {
                            Bounds ia = propios[a].caja;
                            ia.Expand(-0.2f);
                            if (ia.Intersects(propios[b].caja))
                            {
                                s.AppendLine("CHOQUE entre " + propios[a].hijo.name + " y " + propios[b].hijo.name + " (T1395).");
                                fallos++;
                            }
                        }
                    }

                    // Faroles: cada AU01 con luz de sodio y su placa emisiva.
                    Transform lum = raiz.Find("Luminarias");
                    int faroles = lum == null ? 0 : lum.Cast<Transform>().Count(t => t.name.StartsWith("AU01_Luminaria"));
                    int placas = mia.Find("Faroles")?.childCount ?? 0;
                    int sinSodio = lum == null ? 0 : lum.Cast<Transform>().Where(t => t.name.StartsWith("AU01_Luminaria"))
                        .Count(t => { Light l = t.GetComponentInChildren<Light>(true); return l == null || Mathf.Abs(l.color.g - Sodio.g) > 0.02f; });
                    s.AppendLine("Faroles AU01: " + faroles + ", placas emisivas: " + placas + ", sin luz de sodio: " + sinSodio);
                    if (faroles != placas || sinSodio > 0)
                    {
                        s.AppendLine("FALLO: hay faroles sin placa o sin luz de sodio.");
                        fallos++;
                    }

                    // Barreras AU02 encendidas que se encimen con un pie de farol.
                    int barrerasSobreFarol = 0;
                    Transform barr = raiz.Find(Raiz1386 + "/Barreras_AU02");
                    if (barr != null && lum != null)
                    {
                        List<Vector3> pies = lum.Cast<Transform>().Where(t => t.name.StartsWith("AU01_Luminaria")).Select(t => t.position).ToList();
                        foreach (Transform b in barr)
                        {
                            Bounds? caja = CajaVisible(b.gameObject);
                            if (b.gameObject.activeInHierarchy && caja.HasValue && pies.Any(p => caja.Value.Contains(new Vector3(p.x, caja.Value.center.y, p.z)) && Mathf.Abs(p.y - caja.Value.min.y) < 1f))
                            {
                                barrerasSobreFarol++;
                            }
                        }
                    }
                    s.AppendLine("Barreras AU02 encendidas encima de un farol (debe ser 0): " + barrerasSobreFarol);
                    fallos += barrerasSobreFarol;

                    // Chatarra AU03: base entre -0.15 y 0.3 y sin encimarse con nada ni entre sí.
                    Transform ch = raiz.Find(Raiz1386 + "/Chatarra_AU03");
                    if (ch != null)
                    {
                        List<Ocupado> todos = Ocupados(raiz, null);
                        foreach (Transform m in ch)
                        {
                            Bounds? caja = CajaDe(m.gameObject);
                            if (!caja.HasValue)
                            {
                                continue;
                            }
                            if (caja.Value.min.y < -0.15f || caja.Value.min.y > 0.3f)
                            {
                                s.AppendLine("ALTURA " + m.name + ": base en y=" + caja.Value.min.y.ToString("0.00"));
                                fallos++;
                            }
                            Bounds visible = CajaVisible(m.gameObject) ?? caja.Value;
                            visible.Expand(-0.4f);
                            if (todos.Any(c => c.Grupo != m && c.Caja.Intersects(visible)))
                            {
                                s.AppendLine("CHOQUE " + m.name + " (AU03) con otra cosa.");
                                fallos++;
                            }
                        }
                        s.AppendLine("Chatarra AU03 revisada: " + ch.childCount + " montones.");
                    }

                    // Túnel: piezas y luces de la boca.
                    int tiras = mia.Find("BocaTunel")?.GetComponentsInChildren<MeshRenderer>(true).Count() ?? 0;
                    s.AppendLine("Boca del túnel: " + tiras + " piezas emisivas.");
                    if (tiras < 20)
                    {
                        s.AppendLine("FALLO: la boca del túnel casi no tiene piezas emisivas.");
                        fallos++;
                    }
                    // Todo lo emisivo de T1395 usa un material que brilla (EmisionNoche).
                    int sinBrillo = mia.GetComponentsInChildren<MeshRenderer>(true).Count(r => (r.name.StartsWith("LamparaSodio_") || r.name.StartsWith("TiraPared_") || r.name.StartsWith("PlacaTecho_"))
                        && (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name != "MechaCyberpunk/Mundo/EmisionNoche"));
                    s.AppendLine("Piezas emisivas sin shader EmisionNoche (debe ser 0): " + sinBrillo);
                    fallos += sinBrillo;
                    // Colliders: los de lo nuevo coinciden con una malla visible (postes de madera y carros).
                    int colliderSinMalla = mia.GetComponentsInChildren<Collider>(true).Count(c => c.enabled && c.GetComponent<MeshRenderer>() == null && c.GetComponentInChildren<MeshRenderer>() == null);
                    s.AppendLine("Colliders encendidos sin malla visible (debe ser 0): " + colliderSinMalla);
                    fallos += colliderSinMalla;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            s.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1395-validar.txt", s.ToString());
            Debug.Log("[Arte] T-1395 validar:\n" + s);
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>
        /// K18: desde la calle baja (z ~ 217, a 2.8 m), mirando al oeste: a la izquierda las casas de la fila sur con letreros y cables, al centro la rampa norte que sube,
        /// a la derecha el tablero con su columna y la boca del túnel iluminada en el cerro, la torre de reflectores al fondo. Salen dos variantes más (k18b más cerca
        /// de las columnas de la rampa, k18c más atrás) por si la primera no queda. A18: aérea desde el este-sureste mirando la boca del túnel (y una variante a18b).
        /// No guarda nada. Salida en docs/capturas/t-1395/. Corre en una corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1395/Autopista ronda 2 (fotos)")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1395");
            Directory.CreateDirectory("Logs");
            Tomar("K18_AutopistaElevada", "autopista-k18", new Vector3(-148f, 2.8f, 217f), new Vector3(-300f, 20f, 238f), 75f);
            Tomar("K18_AutopistaElevada", "autopista-k18b", new Vector3(-172f, 2.8f, 218.5f), new Vector3(-330f, 16f, 236f), 72f);
            Tomar("K18_AutopistaElevada", "autopista-k18c", new Vector3(-130f, 2.8f, 216f), new Vector3(-330f, 14f, 230f), 78f);
            Tomar("A18_Autopista_Aerea", "autopista-a18", new Vector3(-205f, 46f, 150f), new Vector3(-335f, 12f, 238f), 62f);
            Tomar("A18_Autopista_Aerea", "autopista-a18b", new Vector3(-160f, 60f, 140f), new Vector3(-320f, 10f, 240f), 60f);
        }

        private static void Tomar(string referencia, string nombre, Vector3 desde, Vector3 hacia, float campo)
        {
            string juego = "Logs/t1395-" + nombre + "-juego.png";
            ColocarS2cLook.Foto(desde, hacia, juego, campo);
            ColocarS2.LadoALado("imageassets/referencias/look/" + referencia + ".png", juego, "docs/capturas/t-1395/" + nombre + ".png");
        }
    }
}
