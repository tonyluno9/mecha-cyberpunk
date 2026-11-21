using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1544: las casas de Ciudad_Prologo cuyo cuerpo es una caja generada por código (malla "*_Cuerpo_N" guardada como .asset, material de color
    /// liso S2c_Cuerpo_* / S2p_NovaCuerpo) pasan a tener modelos reales. Orden en batchmode (cada paso es idempotente):
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.CasasT1544.Diagnostico -> Logs/t1544-casas.txt (no cambia nada)
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.CasasT1544.Aplicar     -> deshace lo de T-1544, rehace, guarda la escena y vuelve a diagnosticar
    ///   (opcional) -executeMethod MechaCyberpunk.Mundo.Editor.CasasT1544.Revertir -> deja las casas como estaban (sin guardar la escena).
    /// Casa normal: el render de la caja se apaga (su collider se queda: el techo es transitable y las escaleras a los techos siguen donde estaban)
    /// y el modelo del barrio llena la misma huella y altura, con la fachada hacia la calle, asentado en el piso. Si las piezas del kit ya tapan el
    /// frente (puertas, ventanas, toldos: es el look de la calle) el modelo se mete 0.35 m detrás de ellas y las piezas se quedan; si no, el modelo es
    /// la fachada y se apagan las piezas del kit que queden encimadas.
    /// Lugar del juego (negocio, puerta funcional, casa del jugador...): la caja NO se toca; se cubren sus lados y su parte trasera con paneles de
    /// fachada de modelos reales (aplastados a 0.3 m, a ras de la caja) y el frente queda como está.
    /// Las torres del skyline (Torre_N) y otras cajas lisas solo se listan.
    /// </summary>
    public static class CasasT1544
    {
        public const string RutaLog = "Logs/t1544-casas.txt";
        public const string PrefijoModelo = "T1544_CasaM_";
        public const string PrefijoPanel = "T1544_CasaP_";
        public const string PrefijoMarca = "T1544_Marca_";
        public const string NombreCuerpoApagado = "CuerpoApagado";
        public const string PrefijoApagada = "Apagada_";
        public const string NombreAzotea = "AzoteaT1318";
        public const int MaxVerticesCaja = 200;
        private const float AzarX = 7f;
        private const float AzarZ = 13f;
        private const float MargenCosto = 0.2f;
        private const float MargenPieza = 0.25f;
        private const float FraccionEncimada = 0.5f;
        private const float AdentroPuntoPiso = 0.4f;
        private const float SaliePanel = 0.02f;
        private const float PasoCaraTapada = 0.35f;
        private const float AlturaSalidaRayo = 0.25f;
        private const float DistanciaCamaraLado = 6f;
        private const float DistanciaCamaraAtras = 9f;
        private const float AlturaCamara = 1.7f;

        public enum Categoria
        {
            /// <summary>Cuerpo de una casa o edificio de calle: se cambia (o se cubre si es un lugar).</summary>
            Casa,
            /// <summary>Torre del skyline: solo se lista.</summary>
            Torre,
            /// <summary>Otra caja de color liso y tamaño de edificio: solo se lista.</summary>
            Otra
        }

        public class Caja
        {
            public MeshRenderer Renderer;
            public Transform Cuerpo;
            public Categoria Categoria;
            public string Ruta;
            public string Grupo;
            public string Barrio;
            public string Material;
            public string Malla;
            /// <summary>Ancho (x, a lo largo del frente), alto (y) y fondo (z) en metros.</summary>
            public Vector3 Tam;
            public Vector3 Centro;
            /// <summary>Hacia la calle, en el plano del suelo.</summary>
            public Vector3 Frente;
            public float BaseY;
            public float CoberturaKit;
            public List<string> Kit = new List<string>();
            public int PiezasKit;
            /// <summary>Por qué es un lugar del juego (null si no lo es).</summary>
            public string Lugar;
            public bool FrenteCorregido;
        }

        private class PuntoLugar
        {
            public Vector3 Pos;
            public string Que;
        }

        private class Ajuste
        {
            public ReemplazarEdificiosT1502.Medida Modelo;
            public int Tramos;
            public int Pilas;
            public Vector3 Escala;
            public float Costo;
            public string Nivel;
        }

        // ================= Menús y puntos de entrada =================

        [MenuItem("Mecha Cyberpunk/T-1544 Diagnostico de casas caja")]
        public static void Diagnostico()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            List<Caja> cajas = Escanear(escena);
            string texto = Informar(escena, cajas);
            ReemplazarEdificiosT1502.Escribir(RutaLog, texto);
            Debug.Log("[T-1544] Diagnostico: " + cajas.Count(c => c.Categoria == Categoria.Casa && c.Lugar == null) + " casas caja visibles por cambiar, "
                + cajas.Count(c => c.Categoria == Categoria.Casa && c.Lugar != null) + " lugares, " + cajas.Count(c => c.Categoria == Categoria.Torre)
                + " torres y " + cajas.Count(c => c.Categoria == Categoria.Otra) + " otras cajas (solo listadas); ver " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1544 Aplicar casas con modelo")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            int deshechas = Deshacer(escena);
            Physics.SyncTransforms();

            List<Caja> cajas = Escanear(escena);
            List<Caja> casas = cajas.Where(c => c.Categoria == Categoria.Casa).ToList();
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas = CargarPaletas();

            int reemplazadas = 0;
            int cubiertas = 0;
            int saltadas = 0;
            int piezasApagadas = 0;
            StringBuilder saltos = new StringBuilder();
            foreach (Caja c in casas)
            {
                string motivo = null;
                try
                {
                    int apagadas;
                    motivo = c.Lugar == null ? Reemplazar(c, paletas, out apagadas) : Cubrir(c, paletas, casas, out apagadas);
                    piezasApagadas += apagadas;
                }
                catch (System.Exception e)
                {
                    motivo = "error: " + e.Message;
                    Debug.LogError("[T-1544] " + c.Ruta + ": " + e);
                }
                if (motivo == null)
                {
                    if (c.Lugar == null)
                    {
                        reemplazadas++;
                    }
                    else
                    {
                        cubiertas++;
                    }
                }
                else
                {
                    saltadas++;
                    saltos.AppendLine(c.Ruta + "\t" + motivo);
                    Debug.LogWarning("[T-1544] Se salta " + c.Ruta + ": " + motivo);
                }
            }
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[T-1544] Aplicar: " + reemplazadas + " casas reemplazadas por modelo, " + cubiertas + " lugares con los lados y la parte trasera cubiertos, "
                + saltadas + " saltadas, " + piezasApagadas + " piezas de kit apagadas por encimarse (" + deshechas + " casas deshechas antes de rehacer)");
            if (saltos.Length > 0)
            {
                Debug.Log("[T-1544] Saltadas:\n" + saltos);
            }
            Diagnostico();
        }

        [MenuItem("Mecha Cyberpunk/T-1544 Revertir casas")]
        public static void Revertir()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            int n = Deshacer(escena);
            Physics.SyncTransforms();
            Debug.Log("[T-1544] Revertir: " + n + " casas vueltas a la caja original (la escena no se guardó)");
        }

        // ================= Escaneo =================

        /// <summary>
        /// Cuerpos generados (malla .asset de pocos vértices) visibles: casas, torres y otras cajas lisas de tamaño de edificio.
        /// Las casas ya traen su frente, kit pegado y si son lugar del juego.
        /// </summary>
        public static List<Caja> Escanear(Scene escena)
        {
            List<Caja> lista = new List<Caja>();
            List<PuntoLugar> puntos = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!mr.enabled)
                    {
                        continue;
                    }
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    Mesh malla = mf != null ? mf.sharedMesh : null;
                    if (malla == null || malla.vertexCount == 0 || malla.vertexCount > MaxVerticesCaja)
                    {
                        continue;
                    }
                    string rutaMalla = AssetDatabase.GetAssetPath(malla);
                    if (string.IsNullOrEmpty(rutaMalla) || !rutaMalla.EndsWith(".asset"))
                    {
                        continue;
                    }
                    Transform t = mr.transform;
                    if (EsNuestro(t))
                    {
                        continue;
                    }
                    string ruta = RutaDe(t);
                    Vector3 esc = t.lossyScale;
                    Vector3 tam = Vector3.Scale(malla.bounds.size, new Vector3(Mathf.Abs(esc.x), Mathf.Abs(esc.y), Mathf.Abs(esc.z)));
                    Material mat = mr.sharedMaterial;
                    string nombreMat = mat != null ? mat.name : "";
                    Categoria cat;
                    if (CalculoCasasT1544.EsTorre(ruta))
                    {
                        cat = Categoria.Torre;
                    }
                    else if (CalculoCasasT1544.EsCuerpoDeCasa(ruta, t.name, malla.name, nombreMat, tam))
                    {
                        cat = Categoria.Casa;
                    }
                    else
                    {
                        bool lisa = mat != null && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null;
                        if (!lisa || tam.y < CalculoCasasT1544.AltoMinimoCasa || Mathf.Min(tam.x, tam.z) < 2f || ruta.Contains("Tragamonedas"))
                        {
                            continue;
                        }
                        cat = Categoria.Otra;
                    }

                    Vector3 frente = -t.forward;
                    frente.y = 0f;
                    frente = frente.sqrMagnitude < 0.0001f ? Vector3.forward : frente.normalized;
                    Caja c = new Caja
                    {
                        Renderer = mr,
                        Cuerpo = t,
                        Categoria = cat,
                        Ruta = ruta,
                        Grupo = t.parent != null && t.parent.parent != null ? RutaDe(t.parent.parent) : "",
                        Barrio = CalculoCasasT1544.BarrioDe(ruta),
                        Material = nombreMat,
                        Malla = malla.name,
                        Tam = tam,
                        Centro = t.TransformPoint(malla.bounds.center),
                        Frente = frente
                    };
                    c.BaseY = c.Centro.y - tam.y * 0.5f;
                    if (cat == Categoria.Casa)
                    {
                        if (puntos == null)
                        {
                            puntos = BuscarPuntosLugar(escena);
                        }
                        AnalizarKit(c);
                        c.Lugar = BuscarLugar(c, puntos);
                    }
                    lista.Add(c);
                }
            }
            return lista;
        }

        private static bool EsNuestro(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name.StartsWith("T1544_"))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool EsPiezaPropia(Transform pieza, Transform cuerpo)
        {
            return pieza == cuerpo || pieza.name == NombreAzotea || pieza.name.StartsWith("T1544_");
        }

        /// <summary>Piezas del kit pegadas a la casa (hijos del padre del cuerpo), cuántas cubren el frente y si el frente apunta bien.</summary>
        private static void AnalizarKit(Caja c)
        {
            Transform padre = c.Cuerpo.parent;
            if (padre == null)
            {
                return;
            }
            Dictionary<string, int> nombres = new Dictionary<string, int>();
            foreach (Transform h in padre.Cast<Transform>())
            {
                if (EsPiezaPropia(h, c.Cuerpo) || h.GetComponentInChildren<Renderer>(false) == null)
                {
                    continue;
                }
                string n = QuitarSufijo(h.name);
                int k;
                nombres.TryGetValue(n, out k);
                nombres[n] = k + 1;
                c.PiezasKit++;
            }
            c.Kit = nombres.OrderBy(p => p.Key).Select(p => p.Key + (p.Value > 1 ? " x" + p.Value : "")).ToList();

            // Renderers de las piezas, sin el cuerpo, la azotea ni lo nuestro.
            List<Renderer> piezas = new List<Renderer>();
            foreach (Renderer r in padre.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || EsNuestro(r.transform) || r.transform == c.Cuerpo || r.transform.name == NombreAzotea)
                {
                    continue;
                }
                piezas.Add(r);
            }
            Vector3 lat = Vector3.Cross(Vector3.up, c.Frente);
            float techo = c.BaseY + c.Tam.y;

            // Si las piezas están detrás del cuerpo y no delante, el frente estaba al revés.
            float suma = 0f;
            int cuenta = 0;
            foreach (Renderer r in piezas)
            {
                Bounds b = r.bounds;
                if (b.center.y > c.BaseY + c.Tam.y * 0.8f || b.size.y < 0.5f)
                {
                    continue;
                }
                suma += Vector3.Dot(b.center - c.Centro, c.Frente);
                cuenta++;
            }
            if (cuenta >= 3 && suma / cuenta < -1f)
            {
                c.Frente = -c.Frente;
                lat = Vector3.Cross(Vector3.up, c.Frente);
                c.FrenteCorregido = true;
            }

            List<Vector2> intervalos = new List<Vector2>();
            foreach (Renderer r in piezas)
            {
                Bounds b = r.bounds;
                if (b.size.y < 1f || b.center.y > techo * 0.98f || b.center.y < c.BaseY - 1f)
                {
                    continue;
                }
                float profundidad = Vector3.Dot(b.center - c.Centro, c.Frente) - c.Tam.z * 0.5f;
                if (Mathf.Abs(profundidad) > 1.2f)
                {
                    continue;
                }
                float t0 = Vector3.Dot(b.center - c.Centro, lat);
                float medio = Mathf.Abs(lat.x) * b.extents.x + Mathf.Abs(lat.z) * b.extents.z;
                intervalos.Add(new Vector2(t0 - medio, t0 + medio));
            }
            c.CoberturaKit = CalculoCasasT1544.Cobertura(intervalos, c.Tam.x);
        }

        private static string QuitarSufijo(string nombre)
        {
            int i = nombre.IndexOf(" (", System.StringComparison.Ordinal);
            return i > 0 ? nombre.Substring(0, i) : nombre;
        }

        private static List<PuntoLugar> BuscarPuntosLugar(Scene escena)
        {
            List<PuntoLugar> puntos = new List<PuntoLugar>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && CalculoCasasT1544.EsTipoLugar(mb.GetType().Name))
                    {
                        puntos.Add(new PuntoLugar { Pos = mb.transform.position, Que = mb.GetType().Name + " en " + RutaDe(mb.transform) });
                    }
                }
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (CalculoCasasT1544.EsNombreLugar(t.name))
                    {
                        puntos.Add(new PuntoLugar { Pos = t.position, Que = "objeto " + RutaDe(t) });
                    }
                }
            }
            return puntos;
        }

        /// <summary>Motivo por el que la casa es un lugar del juego (componente de lugar en ella, marca de lugar cerca, ancestro de lugar), o null.</summary>
        private static string BuscarLugar(Caja c, List<PuntoLugar> puntos)
        {
            Transform padre = c.Cuerpo.parent;
            if (padre != null)
            {
                foreach (MonoBehaviour mb in padre.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && CalculoCasasT1544.EsTipoLugar(mb.GetType().Name))
                    {
                        return mb.GetType().Name + " dentro de la casa";
                    }
                }
                foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
                {
                    if (CalculoCasasT1544.EsNombreLugar(t.name))
                    {
                        return "objeto " + t.name + " dentro de la casa";
                    }
                }
            }
            for (Transform p = c.Cuerpo; p != null; p = p.parent)
            {
                if (CalculoCasasT1544.EsNombreLugar(p.name))
                {
                    return "dentro de " + p.name;
                }
            }
            foreach (PuntoLugar punto in puntos)
            {
                Vector3 d = punto.Pos - c.Centro;
                Vector3 local = new Vector3(Vector3.Dot(d, Vector3.Cross(Vector3.up, c.Frente)), d.y, Vector3.Dot(d, c.Frente));
                Vector3 tam = new Vector3(c.Tam.x, c.Tam.y + 2f, c.Tam.z);
                if (CalculoCasasT1544.DentroDeCaja(local, tam, CalculoCasasT1544.MargenLugar))
                {
                    return punto.Que;
                }
            }
            return null;
        }

        // ================= Paletas =================

        private static Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> CargarPaletas()
        {
            Dictionary<string, ReemplazarEdificiosT1502.Medida> cache = new Dictionary<string, ReemplazarEdificiosT1502.Medida>();
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> r = new Dictionary<string, List<ReemplazarEdificiosT1502.Medida>>();
            foreach (string barrio in CalculoEdificiosT1512.BarriosConPaleta())
            {
                IEnumerable<string> rutas = CalculoEdificiosT1512.RutasPara(barrio).Concat(AnterioresDe(barrio));
                r[barrio] = ReemplazarEdificiosT1502.Medidas(rutas.Distinct().ToArray(), cache);
            }
            IEnumerable<string> todas = CalculoEdificiosT1512.RutasPara("").Concat(ReemplazarEdificiosT1502.EstiloNeon)
                .Concat(ReemplazarEdificiosT1502.EstiloSuburbios).Concat(ReemplazarEdificiosT1502.EstiloMercado);
            r[""] = ReemplazarEdificiosT1502.Medidas(todas.Distinct().ToArray(), cache);
            return r;
        }

        private static IEnumerable<string> AnterioresDe(string barrio)
        {
            switch (barrio)
            {
                case "NeonAlto": return ReemplazarEdificiosT1502.EstiloNeon;
                case "Suburbios": return ReemplazarEdificiosT1502.EstiloSuburbios;
                case "Tianguis":
                case "Mercado": return ReemplazarEdificiosT1502.EstiloMercado;
                default: return new string[0];
            }
        }

        private static List<ReemplazarEdificiosT1502.Medida> PaletaDe(Caja c, Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas)
        {
            List<ReemplazarEdificiosT1502.Medida> p;
            if (!paletas.TryGetValue(c.Barrio, out p) || p.Count == 0)
            {
                paletas.TryGetValue("", out p);
            }
            return p;
        }

        /// <summary>Mejor modelo de la paleta para llenar el hueco (primero con los límites estrictos y, si ninguno cabe, con los relajados).</summary>
        private static Ajuste Elegir(List<ReemplazarEdificiosT1502.Medida> paleta, Vector3 centro, float ancho, float fondo, float alto, bool panel)
        {
            if (paleta == null)
            {
                return null;
            }
            foreach (string nivel in new[] { CalculoCasasT1544.Estricto, CalculoCasasT1544.Relajado })
            {
                List<Ajuste> opciones = new List<Ajuste>();
                foreach (ReemplazarEdificiosT1502.Medida m in paleta)
                {
                    int tramos, pilas;
                    Vector3 esc;
                    float costo;
                    bool ok = panel
                        ? CalculoCasasT1544.AjustarPanel(m.Tam, ancho, alto, CalculoCasasT1544.GrosorPanel, nivel, out tramos, out pilas, out esc, out costo)
                        : CalculoCasasT1544.Ajustar(m.Tam, ancho, fondo, alto, nivel, out tramos, out pilas, out esc, out costo);
                    if (ok)
                    {
                        opciones.Add(new Ajuste { Modelo = m, Tramos = tramos, Pilas = pilas, Escala = esc, Costo = costo, Nivel = nivel });
                    }
                }
                if (opciones.Count > 0)
                {
                    float mejor = opciones.Min(o => o.Costo);
                    List<Ajuste> buenas = opciones.Where(o => o.Costo <= mejor + MargenCosto).ToList();
                    return buenas[Mathf.Abs(Mathf.RoundToInt(centro.x * AzarX + centro.z * AzarZ)) % buenas.Count];
                }
            }
            return null;
        }

        // ================= Reemplazo de casas normales =================

        /// <summary>Devuelve null si la casa quedó con modelo, o el motivo por el que se salta.</summary>
        private static string Reemplazar(Caja c, Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas, out int apagadas)
        {
            apagadas = 0;
            Transform padre = c.Cuerpo.parent;
            if (padre == null || c.Cuerpo.GetComponentInParent<LODGroup>() != null)
            {
                return "sin padre o dentro de un LODGroup";
            }
            float hueco = HuecoAlPiso(c);
            float receso = CalculoCasasT1544.Receso(c.CoberturaKit);
            float ancho = c.Tam.x;
            float fondo = c.Tam.z - receso;
            float alto = c.Tam.y + hueco;
            Ajuste a = Elegir(PaletaDe(c, paletas), c.Centro, ancho, fondo, alto, false);
            if (a == null)
            {
                return "ningún modelo de la paleta llena " + ReemplazarEdificiosT1502.Tam(new Vector3(ancho, alto, fondo));
            }

            Vector3 fr = c.Frente;
            Quaternion rot = Quaternion.LookRotation(fr, Vector3.up);
            Vector3 lat = rot * Vector3.right;
            Vector3 suelo = new Vector3(c.Centro.x, c.BaseY - hueco, c.Centro.z) - fr * (receso * 0.5f);
            int idx = c.Cuerpo.GetSiblingIndex();
            GameObject contenedor = NuevoContenedor(padre, PrefijoModelo + idx);
            Vector3 ps = contenedor.transform.lossyScale;
            for (int i = 0; i < a.Tramos; i++)
            {
                for (int j = 0; j < a.Pilas; j++)
                {
                    Vector3 baseCentro = suelo + lat * (((i + 0.5f) / a.Tramos - 0.5f) * ancho) + Vector3.up * (j * alto / a.Pilas);
                    GameObject pieza = (GameObject)PrefabUtility.InstantiatePrefab(a.Modelo.Prefab, contenedor.transform);
                    pieza.name = "T1544_Modelo_" + a.Modelo.Prefab.name + "_" + i + "_" + j;
                    pieza.transform.SetPositionAndRotation(baseCentro - rot * Vector3.Scale(a.Modelo.Desfase, a.Escala), rot);
                    pieza.transform.localScale = new Vector3(a.Escala.x / NoCero(ps.x), a.Escala.y / NoCero(ps.y), a.Escala.z / NoCero(ps.z));
                    pieza.isStatic = true;
                }
            }

            GameObject marca = NuevoContenedor(padre, PrefijoMarca + idx);
            NuevoContenedor(marca.transform, NombreCuerpoApagado);
            c.Renderer.enabled = false; // el collider de la caja se queda: techo transitable y escaleras a los techos

            // Piezas del kit que quedaron metidas dentro del modelo: se apagan.
            Vector3 medidas = new Vector3(Mathf.Abs(lat.x) * ancho + Mathf.Abs(fr.x) * fondo, alto, Mathf.Abs(lat.z) * ancho + Mathf.Abs(fr.z) * fondo);
            Bounds volumen = new Bounds(suelo + Vector3.up * (alto * 0.5f), medidas);
            volumen.Expand(-2f * MargenPieza);
            foreach (Transform h in padre.Cast<Transform>().ToList())
            {
                if (EsPiezaPropia(h, c.Cuerpo) || !h.gameObject.activeSelf)
                {
                    continue;
                }
                Renderer[] rs = h.GetComponentsInChildren<Renderer>(false);
                if (rs.Length == 0)
                {
                    continue;
                }
                Bounds pb = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    pb.Encapsulate(r.bounds);
                }
                if (CalculoCasasT1544.FraccionDentro(pb, volumen) > FraccionEncimada)
                {
                    h.gameObject.SetActive(false);
                    NuevoContenedor(marca.transform, PrefijoApagada + h.GetSiblingIndex());
                    apagadas++;
                }
            }
            Debug.Log("[T-1544] " + c.Ruta + ": " + a.Tramos + "x" + a.Pilas + " " + a.Modelo.Prefab.name + " escala " + ReemplazarEdificiosT1502.Tam(a.Escala)
                + " (" + a.Nivel + "), receso " + receso.ToString("F2") + " m, hueco al piso " + hueco.ToString("F2") + " m, " + apagadas + " piezas de kit apagadas");
            return null;
        }

        // ================= Lugares: solo se cubren los lados y la parte trasera =================

        private static string Cubrir(Caja c, Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas, List<Caja> todas, out int apagadas)
        {
            apagadas = 0;
            Transform padre = c.Cuerpo.parent;
            if (padre == null)
            {
                return "sin padre";
            }
            float hueco = HuecoAlPiso(c);
            float alto = c.Tam.y + hueco;
            Vector3 fr = c.Frente;
            Vector3 lat = Vector3.Cross(Vector3.up, fr);
            List<ReemplazarEdificiosT1502.Medida> paleta = PaletaDe(c, paletas);
            Vector3 suelo = new Vector3(c.Centro.x, c.BaseY - hueco, c.Centro.z);

            // Trasera y los dos lados: normal hacia afuera, ancho de la cara y distancia del centro a la cara.
            (Vector3 normal, float ancho, float distancia, string nombre)[] caras =
            {
                (-fr, c.Tam.x, c.Tam.z * 0.5f, "Atras"),
                (lat, c.Tam.z, c.Tam.x * 0.5f, "LadoA"),
                (-lat, c.Tam.z, c.Tam.x * 0.5f, "LadoB")
            };
            List<(Ajuste a, Vector3 normal, float ancho, float distancia, string nombre)> trabajo = new List<(Ajuste, Vector3, float, float, string)>();
            int tapadas = 0;
            foreach ((Vector3 normal, float ancho, float distancia, string nombre) cara in caras)
            {
                if (CaraTapada(c, cara.normal, cara.ancho, cara.distancia, todas))
                {
                    tapadas++;
                    continue;
                }
                Ajuste a = Elegir(paleta, c.Centro + cara.normal, cara.ancho, 0f, alto, true);
                if (a == null)
                {
                    return "ningún modelo de la paleta sirve de panel para " + cara.nombre + " (" + ReemplazarEdificiosT1502.Tam(new Vector3(cara.ancho, alto, 0f)) + ")";
                }
                trabajo.Add((a, cara.normal, cara.ancho, cara.distancia, cara.nombre));
            }

            int idx = c.Cuerpo.GetSiblingIndex();
            GameObject contenedor = NuevoContenedor(padre, PrefijoPanel + idx);
            Vector3 ps = contenedor.transform.lossyScale;
            foreach ((Ajuste a, Vector3 normal, float ancho, float distancia, string nombre) t in trabajo)
            {
                Quaternion rot = Quaternion.LookRotation(t.normal, Vector3.up);
                Vector3 lateral = rot * Vector3.right;
                // El panel queda a ras de la cara de la caja y asoma 2 cm para ganarle al z-fighting; el resto del grosor queda dentro de la caja.
                Vector3 centroCara = suelo + t.normal * (t.distancia + SaliePanel - CalculoCasasT1544.GrosorPanel * 0.5f);
                for (int i = 0; i < t.a.Tramos; i++)
                {
                    for (int j = 0; j < t.a.Pilas; j++)
                    {
                        Vector3 baseCentro = centroCara + lateral * (((i + 0.5f) / t.a.Tramos - 0.5f) * t.ancho) + Vector3.up * (j * alto / t.a.Pilas);
                        GameObject pieza = (GameObject)PrefabUtility.InstantiatePrefab(t.a.Modelo.Prefab, contenedor.transform);
                        pieza.name = "T1544_Panel_" + t.nombre + "_" + t.a.Modelo.Prefab.name + "_" + i + "_" + j;
                        pieza.transform.SetPositionAndRotation(baseCentro - rot * Vector3.Scale(t.a.Modelo.Desfase, t.a.Escala), rot);
                        pieza.transform.localScale = new Vector3(t.a.Escala.x / NoCero(ps.x), t.a.Escala.y / NoCero(ps.y), t.a.Escala.z / NoCero(ps.z));
                        pieza.isStatic = true;
                        // Un panel es solo piel: el collider de la caja (que no se toca) ya es la pared.
                        foreach (Collider col in pieza.GetComponentsInChildren<Collider>(true))
                        {
                            col.enabled = false;
                        }
                    }
                }
            }
            NuevoContenedor(padre, PrefijoMarca + idx); // sin CuerpoApagado: la caja se queda tal cual
            Debug.Log("[T-1544] Lugar " + c.Ruta + " (" + c.Lugar + "): " + trabajo.Count + " caras cubiertas, " + tapadas + " tapadas por casas vecinas");
            return null;
        }

        /// <summary>Verdadero si una casa vecina tapa la cara (al menos 2 de 3 puntos pegados a la cara caen dentro de otra caja).</summary>
        private static bool CaraTapada(Caja c, Vector3 normal, float ancho, float distancia, List<Caja> todas)
        {
            Vector3 lateral = Vector3.Cross(Vector3.up, normal);
            Vector3 medio = c.Centro + normal * (distancia + PasoCaraTapada);
            Vector3[] muestras = { medio, medio + lateral * (ancho * 0.3f), medio - lateral * (ancho * 0.3f) };
            int dentro = 0;
            foreach (Vector3 p in muestras)
            {
                foreach (Caja o in todas)
                {
                    if (o == c)
                    {
                        continue;
                    }
                    Vector3 d = p - o.Centro;
                    Vector3 local = new Vector3(Vector3.Dot(d, Vector3.Cross(Vector3.up, o.Frente)), d.y, Vector3.Dot(d, o.Frente));
                    if (CalculoCasasT1544.DentroDeCaja(local, o.Tam, 0f))
                    {
                        dentro++;
                        break;
                    }
                }
            }
            return dentro >= 2;
        }

        // ================= Piso =================

        /// <summary>Cuánto baja el terreno bajo la base de la caja (m, 0 si no baja): se estira el modelo hasta el piso para que no flote.</summary>
        private static float HuecoAlPiso(Caja c)
        {
            Vector3 lat = Vector3.Cross(Vector3.up, c.Frente);
            float a = Mathf.Max(c.Tam.x * 0.5f - AdentroPuntoPiso, 0f);
            float f = Mathf.Max(c.Tam.z * 0.5f - AdentroPuntoPiso, 0f);
            Vector3[] puntos =
            {
                c.Centro,
                c.Centro + lat * a + c.Frente * f, c.Centro + lat * a - c.Frente * f,
                c.Centro - lat * a + c.Frente * f, c.Centro - lat * a - c.Frente * f
            };
            float hueco = 0f;
            foreach (Vector3 p in puntos)
            {
                Vector3 origen = new Vector3(p.x, c.BaseY + AlturaSalidaRayo, p.z);
                RaycastHit[] golpes = Physics.RaycastAll(origen, Vector3.down, AlturaSalidaRayo + CalculoCasasT1544.HuecoMaximoPiso, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(golpes, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit h in golpes)
                {
                    if (EsNuestro(h.collider.transform) || (c.Cuerpo.parent != null && h.collider.transform.IsChildOf(c.Cuerpo.parent)))
                    {
                        continue;
                    }
                    float h0 = c.BaseY - h.point.y;
                    if (h0 > CalculoCasasT1544.HuecoMinimoPiso)
                    {
                        hueco = Mathf.Max(hueco, Mathf.Min(h0, CalculoCasasT1544.HuecoMaximoPiso));
                    }
                    break;
                }
            }
            return hueco;
        }

        // ================= Deshacer =================

        /// <summary>Quita lo que puso T-1544: modelos y paneles, vuelve a prender la caja y las piezas de kit apagadas. Devuelve cuántas casas deshizo.</summary>
        private static int Deshacer(Scene escena)
        {
            List<Transform> marcas = new List<Transform>();
            List<Transform> contenedores = new List<Transform>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith(PrefijoMarca))
                    {
                        marcas.Add(t);
                    }
                    else if (t.name.StartsWith(PrefijoModelo) || t.name.StartsWith(PrefijoPanel))
                    {
                        contenedores.Add(t);
                    }
                }
            }
            int n = 0;
            foreach (Transform marca in marcas)
            {
                Transform padre = marca.parent;
                int idx;
                if (padre != null && int.TryParse(marca.name.Substring(PrefijoMarca.Length), out idx))
                {
                    foreach (Transform h in marca.Cast<Transform>())
                    {
                        int k;
                        if (h.name == NombreCuerpoApagado && idx < padre.childCount)
                        {
                            MeshRenderer mr = padre.GetChild(idx).GetComponent<MeshRenderer>();
                            if (mr != null)
                            {
                                mr.enabled = true;
                            }
                        }
                        else if (h.name.StartsWith(PrefijoApagada) && int.TryParse(h.name.Substring(PrefijoApagada.Length), out k) && k < padre.childCount)
                        {
                            padre.GetChild(k).gameObject.SetActive(true);
                        }
                    }
                }
                n++;
            }
            foreach (Transform t in marcas.Concat(contenedores))
            {
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            return n;
        }

        // ================= Utilidades =================

        private static GameObject NuevoContenedor(Transform padre, string nombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.isStatic = true;
            return go;
        }

        private static float NoCero(float v)
        {
            return Mathf.Abs(v) < 0.0001f ? 1f : v;
        }

        private static string RutaDe(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        // ================= Informe =================

        private static string Informar(Scene escena, List<Caja> cajas)
        {
            List<Caja> casas = cajas.Where(c => c.Categoria == Categoria.Casa).OrderBy(c => c.Ruta).ToList();
            List<Caja> normales = casas.Where(c => c.Lugar == null).ToList();
            List<Caja> lugares = casas.Where(c => c.Lugar != null).ToList();
            int conModelo = 0;
            int conPaneles = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith(PrefijoModelo))
                    {
                        conModelo++;
                    }
                    else if (t.name.StartsWith(PrefijoPanel))
                    {
                        conPaneles++;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1544: casas con cuerpo de caja generada en " + CreadorPrologoCiudad.RutaEscena);
            sb.AppendLine("Cajas de casa visibles: " + casas.Count + " (" + normales.Count + " normales por cambiar, " + lugares.Count + " lugares del juego)");
            sb.AppendLine("Ya con modelo T-1544: " + conModelo + " casas reemplazadas, " + conPaneles + " lugares con paneles en lados y trasera");
            sb.AppendLine("Torres del skyline visibles (solo listadas): " + cajas.Count(c => c.Categoria == Categoria.Torre));
            sb.AppendLine("Otras cajas lisas de tamaño de edificio (solo listadas): " + cajas.Count(c => c.Categoria == Categoria.Otra));
            sb.AppendLine();
            sb.AppendLine("Por grupo (casas visibles):");
            foreach (IGrouping<string, Caja> g in casas.GroupBy(x => x.Grupo).OrderBy(q => q.Key))
            {
                sb.AppendLine("  " + g.Key + ": " + g.Count() + " (" + g.Count(c => c.Lugar != null) + " lugares)");
            }
            sb.AppendLine();
            sb.AppendLine("ruta\tbarrio\tancho x alto x fondo (m)\tmaterial\tkit pegado (piezas)\tcobertura del frente\tfrente\ttipo");
            foreach (Caja c in casas)
            {
                sb.AppendLine(c.Ruta + "\t" + c.Barrio + "\t" + ReemplazarEdificiosT1502.Tam(c.Tam) + "\t" + c.Material + "\t" + c.PiezasKit + ": " + string.Join(", ", c.Kit)
                    + "\t" + c.CoberturaKit.ToString("P0") + "\t" + c.Frente.x.ToString("F0") + "," + c.Frente.z.ToString("F0") + (c.FrenteCorregido ? " (corregido por el kit)" : "")
                    + "\t" + (c.Lugar == null ? "casa normal" : "LUGAR: " + c.Lugar));
            }
            sb.AppendLine();
            sb.AppendLine("Posiciones para capturas (una casa por grupo; cámara a " + AlturaCamara + " m del piso mirando al centro de la casa):");
            foreach (IGrouping<string, Caja> g in casas.GroupBy(x => x.Grupo).OrderBy(q => q.Key))
            {
                Caja c = g.First();
                Vector3 lat = Vector3.Cross(Vector3.up, c.Frente);
                Vector3 centro = new Vector3(c.Centro.x, c.BaseY + AlturaCamara, c.Centro.z);
                Vector3 lado = centro + lat * (c.Tam.x * 0.5f + DistanciaCamaraLado);
                Vector3 atras = centro - c.Frente * (c.Tam.z * 0.5f + DistanciaCamaraAtras);
                sb.AppendLine("  " + c.Ruta + ": centro " + V(c.Centro) + "; lado " + V(lado) + "; atras " + V(atras));
            }
            sb.AppendLine();
            sb.AppendLine("Torres del skyline (no se cambian en T-1544):");
            foreach (Caja c in cajas.Where(x => x.Categoria == Categoria.Torre).OrderBy(x => x.Ruta))
            {
                sb.AppendLine("  " + c.Ruta + "\t" + ReemplazarEdificiosT1502.Tam(c.Tam) + "\t" + c.Material);
            }
            sb.AppendLine();
            sb.AppendLine("Otras cajas lisas (no se cambian en T-1544):");
            foreach (Caja c in cajas.Where(x => x.Categoria == Categoria.Otra).OrderBy(x => x.Ruta))
            {
                sb.AppendLine("  " + c.Ruta + "\t" + ReemplazarEdificiosT1502.Tam(c.Tam) + "\t" + c.Material + "\t" + c.Malla);
            }
            return sb.ToString();
        }

        private static string V(Vector3 v)
        {
            return "(" + v.x.ToString("F1") + ", " + v.y.ToString("F1") + ", " + v.z.ToString("F1") + ")";
        }
    }
}
