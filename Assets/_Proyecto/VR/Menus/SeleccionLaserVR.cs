using System.Collections.Generic;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR44: la selección del láser en todos los paneles de VR, como en la máquina expendedora y la pausa.
    /// <list type="bullet">
    /// <item>Elige el botón bajo el láser y, con un menú abierto, el más cercano aunque el láser caiga un poco fuera (magnetismo,
    /// <see cref="ReglaSeleccionVR"/>). El gatillo hace clic solo en el elegido: mientras está abajo, el elegido no cambia.</item>
    /// <item>El elegido se marca con un marco cian encima (salvo los botones del kit Cyberfix, como la pausa, y la máquina expendedora, que ya se
    /// resaltan solos) y el control vibra leve al pasar a otro botón.</item>
    /// <item>Un punto cian marca dónde toca el láser el panel (el láser se corta ahí, ver <see cref="TocarPanel"/>).</item>
    /// </list>
    /// Va en dos Canvas World Space propios, hijos de MenusVR (el barrido no los toca), con el orden de dibujo más alto.
    /// </summary>
    public sealed class SeleccionLaserVR
    {
        public const string NombreMarco = "MarcoSeleccionVR";
        public const string NombrePunto = "PuntoLaserVR";
        /// <summary>Encima de todo lo demás (los fundidos usan 30000+).</summary>
        private const int OrdenVisual = 32000;
        private const float GrosorMarcoPx = 5f;
        private const float HolguraMarcoPx = 6f;
        private const float AlfaRelleno = 0.12f;
        /// <summary>Separación del marco y del punto hacia la cabeza (m): delante del botón, sin pelear con él.</summary>
        private const float SeparacionVisual = 0.004f;
        private const float TamanoPuntoPx = 16f;
        private const int LadoTexturaPunto = 32;
        private const float BordeSuavePunto = 3f;
        private const float AmplitudVibracion = 0.15f;
        private const float SegundosVibracion = 0.03f;
        // Cuánto se mete el punto de prueba hacia el centro del gráfico (unidades locales) para no caer justo en su borde.
        private const float AdentroLocal = 2f;
        private static readonly Color ColorMarco = new Color(0f, 0.95f, 1f, 1f);
        private static readonly Color ColorPunto = new Color(0.6f, 1f, 1f, 1f);

        private struct Info
        {
            public GameObject Manejador;
            public RectTransform Rect;
            public Selectable Seleccionable;
            public bool SinMarco;
        }

        private readonly Dictionary<Graphic, Info> cache = new Dictionary<Graphic, Info>();
        private readonly List<ReglaSeleccionVR.Candidato> candidatos = new List<ReglaSeleccionVR.Candidato>();
        private readonly List<GameObject> manejadores = new List<GameObject>();
        private readonly List<Graphic> graficos = new List<Graphic>();
        private readonly List<Info> infos = new List<Info>();
        private readonly List<Vector3> puntos = new List<Vector3>();
        private readonly List<float> distancias = new List<float>();
        private readonly List<float> escalas = new List<float>();
        private readonly List<Canvas> lienzosCandidato = new List<Canvas>();

        private GameObject elegido;
        private RectTransform rectElegido;
        private bool marcoElegido;
        private float metrosPorPxElegido = ReglaLienzosVR.EscalaFrente;

        private Transform padre;
        private Canvas lienzoMarco;
        private RectTransform[] bordes;
        private Image relleno;
        private Canvas lienzoPunto;
        private Texture2D texturaPunto;
        private Sprite spritePunto;

        /// <summary>El botón elegido (null si ninguno).</summary>
        public GameObject Elegido => elegido;
        /// <summary>Veces que cambió el elegido a otro botón (las pruebas lo miran: cada cambio vibra).</summary>
        public int Cambios { get; private set; }
        public bool MarcoVisible => lienzoMarco != null && lienzoMarco.gameObject.activeSelf;
        public bool PuntoVisible => lienzoPunto != null && lienzoPunto.gameObject.activeSelf;
        public Vector3 PosicionPunto => lienzoPunto != null ? lienzoPunto.transform.position : Vector3.zero;
        /// <summary>El Canvas del marco (null hasta que se usa).</summary>
        public Canvas LienzoMarco => lienzoMarco;

        public SeleccionLaserVR(Transform padreVisuales)
        {
            padre = padreVisuales;
        }

        /// <summary>Olvida lo que sabía de cada gráfico (se llama en cada barrido: las pantallas rehacen sus filas).</summary>
        public void Limpiar()
        {
            cache.Clear();
        }

        /// <summary>Ningún botón elegido (el láser se apagó o apunta a otra cosa).</summary>
        public void Soltar()
        {
            elegido = null;
            rectElegido = null;
        }

        /// <summary>
        /// Elige el botón de este cuadro. <paramref name="directo"/> es lo que toca el rayo de verdad (si <paramref name="hayDirecto"/>);
        /// con <paramref name="iman"/> (un menú abierto) también atrae al botón más cercano. Con <paramref name="bloqueado"/> (el gatillo
        /// sigue abajo) ese botón se queda elegido. Devuelve lo que se manda a la UI.
        /// </summary>
        public bool Elegir(Ray rayo, float alcance, List<LienzoConvertidoVR> lienzos, Camera camara, BaseRaycaster modulo,
            bool hayDirecto, RaycastResult directo, bool iman, GameObject bloqueado, out RaycastResult resultado)
        {
            resultado = directo;
            Juntar(rayo, alcance, lienzos, camara, hayDirecto, directo, iman, bloqueado, out int indiceDirecto);
            int actual = elegido != null ? manejadores.IndexOf(elegido) : -1;
            int indice = bloqueado != null ? manejadores.IndexOf(bloqueado) : -1;
            if (indice < 0)
            {
                indice = ReglaSeleccionVR.Elegir(candidatos, indiceDirecto, actual);
            }
            if (indice < 0)
            {
                Cambiar(null, default, 0f);
                return hayDirecto;
            }
            Cambiar(manejadores[indice], infos[indice], escalas[indice]);
            if (indice == indiceDirecto && hayDirecto)
            {
                return true;
            }
            Graphic g = graficos[indice];
            Canvas c = lienzosCandidato[indice];
            resultado = new RaycastResult
            {
                gameObject = g.gameObject,
                module = modulo,
                distance = distancias[indice],
                worldPosition = puntos[indice],
                worldNormal = -g.rectTransform.forward,
                screenPosition = camara.WorldToScreenPoint(puntos[indice]),
                index = 0,
                depth = g.depth,
                sortingLayer = c.sortingLayerID,
                sortingOrder = RaycasterLaserVR.OrdenDe(c),
            };
            return true;
        }

        // Los botones de los paneles cerca del rayo: uno por manejador de clic, con su distancia (px) al punto del rayo en su plano.
        private void Juntar(Ray rayo, float alcance, List<LienzoConvertidoVR> lienzos, Camera camara, bool hayDirecto, RaycastResult directo,
            bool iman, GameObject bloqueado, out int indiceDirecto)
        {
            candidatos.Clear();
            manejadores.Clear();
            graficos.Clear();
            infos.Clear();
            puntos.Clear();
            distancias.Clear();
            escalas.Clear();
            lienzosCandidato.Clear();
            indiceDirecto = -1;
            GameObject manejadorDirecto = null;
            if (hayDirecto && directo.gameObject != null)
            {
                Graphic g = directo.gameObject.GetComponent<Graphic>();
                manejadorDirecto = g != null ? InfoDe(g).Manejador : null;
            }
            // Lo que el rayo toca de verdad tapa lo de abajo (una ventana sobre la tienda): solo cuenta lo de ese orden o más arriba.
            int ordenMinimo = hayDirecto ? directo.sortingOrder : int.MinValue;
            if (camara == null)
            {
                return;
            }
            foreach (LienzoConvertidoVR l in lienzos)
            {
                if (l.Canvas == null || l.Tipo != TipoLienzoVR.Frente || !l.Canvas.isActiveAndEnabled)
                {
                    continue;
                }
                float metrosPorPx = Mathf.Abs(l.Canvas.transform.lossyScale.x);
                if (metrosPorPx < 1e-8f)
                {
                    continue;
                }
                foreach (Canvas c in l.Lienzos)
                {
                    if (c == null || !c.isActiveAndEnabled || RaycasterLaserVR.OrdenDe(c) < ordenMinimo)
                    {
                        continue;
                    }
                    IList<Graphic> lista = GraphicRegistry.GetRaycastableGraphicsForCanvas(c);
                    for (int i = 0; i < lista.Count; i++)
                    {
                        Agregar(lista[i], c, rayo, alcance, camara, metrosPorPx, iman, manejadorDirecto, bloqueado, ref indiceDirecto);
                    }
                }
            }
        }

        private void Agregar(Graphic g, Canvas c, Ray rayo, float alcance, Camera camara, float metrosPorPx, bool iman, GameObject manejadorDirecto,
            GameObject bloqueado, ref int indiceDirecto)
        {
            if (g == null || !g.raycastTarget || !g.isActiveAndEnabled || g.depth == -1 || g.canvasRenderer.cull)
            {
                return;
            }
            Info info = InfoDe(g);
            if (info.Manejador == null)
            {
                return;
            }
            bool esBloqueado = info.Manejador == bloqueado;
            bool esDirecto = info.Manejador == manejadorDirecto;
            if (!iman && !esDirecto && !esBloqueado)
            {
                return;
            }
            if (info.Seleccionable != null && !info.Seleccionable.IsInteractable() && !esBloqueado)
            {
                return;
            }
            RectTransform rt = g.rectTransform;
            if (!Proyectar(rt, rayo, alcance, out float distancia, out Vector3 punto, out Vector2 local))
            {
                return;
            }
            Rect r = rt.rect;
            var cerca = new Vector2(Mathf.Clamp(local.x, r.xMin, r.xMax), Mathf.Clamp(local.y, r.yMin, r.yMax));
            float px = Vector3.Distance(punto, rt.TransformPoint(cerca)) / metrosPorPx;
            if (px > ReglaSeleccionVR.RadioImanPx && !esBloqueado)
            {
                return;
            }
            if (!esBloqueado)
            {
                // Los filtros del juego: CanvasGroup (blocksRaycasts), máscaras de las listas (una fila fuera de la vista no cuenta).
                Vector2 dentro = Vector2.MoveTowards(cerca, r.center, AdentroLocal);
                if (!g.Raycast(camara.WorldToScreenPoint(rt.TransformPoint(dentro)), camara))
                {
                    return;
                }
            }
            float area = AreaPx(info.Rect != null ? info.Rect : rt, metrosPorPx);
            var candidato = new ReglaSeleccionVR.Candidato(px, RaycasterLaserVR.OrdenDe(c), g.depth, area);
            int k = manejadores.IndexOf(info.Manejador);
            if (k < 0)
            {
                manejadores.Add(info.Manejador);
                candidatos.Add(candidato);
                graficos.Add(g);
                infos.Add(info);
                puntos.Add(punto);
                distancias.Add(distancia);
                escalas.Add(metrosPorPx);
                lienzosCandidato.Add(c);
                k = manejadores.Count - 1;
            }
            else if (px < candidatos[k].DistanciaPx)
            {
                candidatos[k] = candidato;
                graficos[k] = g;
                puntos[k] = punto;
                distancias[k] = distancia;
                lienzosCandidato[k] = c;
            }
            if (esDirecto && !ReglaSeleccionVR.EsFondo(area))
            {
                indiceDirecto = k;
            }
        }

        private static bool Proyectar(RectTransform rt, Ray rayo, float alcance, out float distancia, out Vector3 punto, out Vector2 local)
        {
            distancia = 0f;
            punto = Vector3.zero;
            local = Vector2.zero;
            Vector3 normal = rt.forward;
            float denominador = Vector3.Dot(normal, rayo.direction);
            if (denominador <= 1e-5f)
            {
                return false;
            }
            distancia = Vector3.Dot(rt.position - rayo.origin, normal) / denominador;
            if (distancia < 0f || distancia > alcance)
            {
                return false;
            }
            punto = rayo.origin + rayo.direction * distancia;
            Vector3 l = rt.InverseTransformPoint(punto);
            local = new Vector2(l.x, l.y);
            return true;
        }

        private static float AreaPx(RectTransform rt, float metrosPorPx)
        {
            Vector3 escala = rt.lossyScale;
            Rect r = rt.rect;
            float ancho = Mathf.Abs(r.width * escala.x) / metrosPorPx;
            float alto = Mathf.Abs(r.height * escala.y) / metrosPorPx;
            return ancho * alto;
        }

        private Info InfoDe(Graphic g)
        {
            if (cache.TryGetValue(g, out Info info))
            {
                return info;
            }
            GameObject objeto = g.gameObject;
            GameObject clic = ExecuteEvents.GetEventHandler<IPointerClickHandler>(objeto);
            Selectable seleccionable = objeto.GetComponentInParent<Selectable>();
            GameObject manejador = clic != null ? clic : (seleccionable != null ? seleccionable.gameObject : null);
            info.Manejador = manejador;
            info.Seleccionable = seleccionable != null && seleccionable.gameObject == manejador ? seleccionable : null;
            info.Rect = manejador != null ? manejador.transform as RectTransform : null;
            info.SinMarco = manejador != null && ResaltaSolo(manejador);
            cache[g] = info;
            return info;
        }

        // Los que ya se resaltan bien solos: los botones del kit (la pausa, las opciones) y la máquina expendedora (pasar el láser elige la fila).
        private static bool ResaltaSolo(GameObject manejador)
        {
            if (manejador.GetComponentInParent<BotonCyberfix>() != null)
            {
                return true;
            }
            PantallaTienda tienda = manejador.GetComponentInParent<PantallaTienda>();
            return tienda != null && !tienda.ModoMercado;
        }

        private void Cambiar(GameObject nuevo, Info info, float metrosPorPx)
        {
            if (nuevo == elegido)
            {
                return;
            }
            elegido = nuevo;
            rectElegido = nuevo != null ? (info.Rect != null ? info.Rect : nuevo.transform as RectTransform) : null;
            marcoElegido = nuevo != null && !info.SinMarco;
            if (metrosPorPx > 0f)
            {
                metrosPorPxElegido = metrosPorPx;
            }
            if (nuevo != null)
            {
                Cambios++;
                Vibrar();
            }
        }

        // Un toque leve en el control derecho (sin visor no hace nada).
        private static void Vibrar()
        {
            UnityEngine.XR.InputDevice control = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (control.isValid)
            {
                control.SendHapticImpulse(0u, AmplitudVibracion, SegundosVibracion);
            }
        }

        /// <summary>
        /// VR44: dónde corta el láser el panel de menús más cercano (aunque ahí no haya nada que se pueda tocar): distancia (m), punto y
        /// giro del panel. False si el rayo no cae en ningún panel abierto.
        /// </summary>
        public static bool TocarPanel(Ray rayo, float alcance, List<LienzoConvertidoVR> lienzos, out float distancia, out Vector3 punto,
            out Quaternion giro)
        {
            distancia = float.MaxValue;
            punto = Vector3.zero;
            giro = Quaternion.identity;
            bool hay = false;
            foreach (LienzoConvertidoVR l in lienzos)
            {
                if (l.Canvas == null || l.Tipo != TipoLienzoVR.Frente || !l.Canvas.isActiveAndEnabled)
                {
                    continue;
                }
                var rt = (RectTransform)l.Canvas.transform;
                if (!Proyectar(rt, rayo, alcance, out float d, out Vector3 p, out Vector2 local) || !rt.rect.Contains(local) || d >= distancia)
                {
                    continue;
                }
                if (!TieneAlgoVisible(l))
                {
                    continue;
                }
                hay = true;
                distancia = d;
                punto = p;
                giro = rt.rotation;
            }
            return hay;
        }

        // Un panel sin ningún gráfico encendido (una pantalla cerrada que sigue convertida) no corta el láser.
        private static bool TieneAlgoVisible(LienzoConvertidoVR l)
        {
            foreach (Canvas c in l.Lienzos)
            {
                if (c != null && c.isActiveAndEnabled && GraphicRegistry.GetGraphicsForCanvas(c).Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // ---- Lo que se ve ----

        /// <summary>
        /// Coloca el marco sobre el elegido y el punto donde toca el láser (cada cuadro, después de colocar los paneles).
        /// <paramref name="escalaPunto"/>: m por px del punto (para que se vea igual de grande a cualquier distancia).
        /// </summary>
        public void Dibujar(bool visible, bool hayPunto, Vector3 punto, Quaternion giro, float escalaPunto)
        {
            bool marco = visible && elegido != null && rectElegido != null && marcoElegido && rectElegido.gameObject.activeInHierarchy;
            if (marco || (visible && hayPunto))
            {
                Asegurar();
            }
            if (lienzoMarco != null)
            {
                if (marco)
                {
                    ColocarMarco();
                }
                if (lienzoMarco.gameObject.activeSelf != marco)
                {
                    lienzoMarco.gameObject.SetActive(marco);
                }
            }
            if (lienzoPunto != null)
            {
                bool verPunto = visible && hayPunto;
                if (verPunto)
                {
                    Transform t = lienzoPunto.transform;
                    t.SetPositionAndRotation(punto - giro * Vector3.forward * SeparacionVisual, giro);
                    t.localScale = Vector3.one * Mathf.Max(1e-6f, escalaPunto);
                }
                if (lienzoPunto.gameObject.activeSelf != verPunto)
                {
                    lienzoPunto.gameObject.SetActive(verPunto);
                }
            }
        }

        private void ColocarMarco()
        {
            var raiz = (RectTransform)lienzoMarco.transform;
            Rect r = rectElegido.rect;
            Vector3 escala = rectElegido.lossyScale;
            Vector3 centro = rectElegido.TransformPoint(r.center);
            raiz.SetPositionAndRotation(centro - rectElegido.forward * SeparacionVisual, rectElegido.rotation);
            raiz.localScale = escala;
            float escalaX = Mathf.Max(1e-8f, Mathf.Abs(escala.x));
            float escalaY = Mathf.Max(1e-8f, Mathf.Abs(escala.y));
            float holguraX = HolguraMarcoPx * metrosPorPxElegido / escalaX;
            float holguraY = HolguraMarcoPx * metrosPorPxElegido / escalaY;
            float grosorX = GrosorMarcoPx * metrosPorPxElegido / escalaX;
            float grosorY = GrosorMarcoPx * metrosPorPxElegido / escalaY;
            raiz.sizeDelta = new Vector2(Mathf.Abs(r.width) + holguraX * 2f, Mathf.Abs(r.height) + holguraY * 2f);
            // Arriba, abajo, izquierda, derecha.
            bordes[0].sizeDelta = new Vector2(0f, grosorY);
            bordes[1].sizeDelta = new Vector2(0f, grosorY);
            bordes[2].sizeDelta = new Vector2(grosorX, 0f);
            bordes[3].sizeDelta = new Vector2(grosorX, 0f);
        }

        private void Asegurar()
        {
            if (lienzoMarco == null)
            {
                lienzoMarco = CrearLienzo(NombreMarco);
                var raiz = (RectTransform)lienzoMarco.transform;
                relleno = CrearImagen(raiz, "Relleno", new Color(ColorMarco.r, ColorMarco.g, ColorMarco.b, AlfaRelleno));
                Estirar(relleno.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                bordes = new RectTransform[4];
                bordes[0] = Borde(raiz, "Arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                bordes[1] = Borde(raiz, "Abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
                bordes[2] = Borde(raiz, "Izquierda", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
                bordes[3] = Borde(raiz, "Derecha", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
                lienzoMarco.gameObject.SetActive(false);
            }
            if (lienzoPunto == null)
            {
                lienzoPunto = CrearLienzo(NombrePunto);
                var raiz = (RectTransform)lienzoPunto.transform;
                raiz.sizeDelta = new Vector2(TamanoPuntoPx, TamanoPuntoPx);
                Image imagen = CrearImagen(raiz, "Punto", ColorPunto);
                Estirar(imagen.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                imagen.sprite = SpritePunto();
                lienzoPunto.gameObject.SetActive(false);
            }
        }

        private Canvas CrearLienzo(string nombre)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Canvas canvas = objeto.AddComponent<Canvas>();
            // World Space en el acto: el barrido de MenusVR (y CinematicaPendiente) no tocan los Canvas del mundo.
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = OrdenVisual;
            var rect = (RectTransform)objeto.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return canvas;
        }

        private RectTransform Borde(RectTransform padreBorde, string nombre, Vector2 min, Vector2 max, Vector2 pivote)
        {
            Image imagen = CrearImagen(padreBorde, nombre, ColorMarco);
            Estirar(imagen.rectTransform, min, max, pivote);
            return imagen.rectTransform;
        }

        private static Image CrearImagen(RectTransform padreImagen, string nombre, Color color)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padreImagen, false);
            Image imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        private static void Estirar(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivote)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivote;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        // Un círculo suave hecho en código (sin arte): el punto del láser.
        private Sprite SpritePunto()
        {
            if (spritePunto != null)
            {
                return spritePunto;
            }
            texturaPunto = new Texture2D(LadoTexturaPunto, LadoTexturaPunto, TextureFormat.RGBA32, false)
            {
                name = NombrePunto,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            float radio = LadoTexturaPunto * 0.5f;
            for (int y = 0; y < LadoTexturaPunto; y++)
            {
                for (int x = 0; x < LadoTexturaPunto; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radio, radio)) / radio;
                    float alfa = Mathf.Clamp01((1f - d) * BordeSuavePunto);
                    texturaPunto.SetPixel(x, y, new Color(1f, 1f, 1f, alfa));
                }
            }
            texturaPunto.Apply();
            spritePunto = Sprite.Create(texturaPunto, new Rect(0f, 0f, LadoTexturaPunto, LadoTexturaPunto), new Vector2(0.5f, 0.5f), 100f);
            spritePunto.name = NombrePunto;
            return spritePunto;
        }

        /// <summary>Quita los Canvas, el sprite y la textura.</summary>
        public void Destruir()
        {
            if (lienzoMarco != null)
            {
                Object.Destroy(lienzoMarco.gameObject);
            }
            if (lienzoPunto != null)
            {
                Object.Destroy(lienzoPunto.gameObject);
            }
            if (spritePunto != null)
            {
                Object.Destroy(spritePunto);
            }
            if (texturaPunto != null)
            {
                Object.Destroy(texturaPunto);
            }
            lienzoMarco = null;
            lienzoPunto = null;
            spritePunto = null;
            texturaPunto = null;
            bordes = null;
            relleno = null;
            Soltar();
            cache.Clear();
        }
    }
}
