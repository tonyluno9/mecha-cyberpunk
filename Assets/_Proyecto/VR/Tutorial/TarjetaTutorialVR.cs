using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.VR.Guia;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Tutorial
{
    /// <summary>
    /// VR62/VR45: una tarjeta del tutorial de VR. Un lienzo del mundo (World Space, como la guía <see cref="GuiaControlesVR"/>, estilo H10)
    /// con la imagen de la página de la guía, su frase corta en cian, una explicación de 1–2 líneas y el botón APRENDIDO. Flota abajo y a un
    /// lado de la vista (<see cref="Adelante"/>, <see cref="Lado"/>, <see cref="Bajada"/>), semitransparente, y sigue a la cabeza con suavidad
    /// para que se pueda hacer el gesto mientras se lee. APRENDIDO se elige con el láser del control derecho (resaltado como en la máquina
    /// expendedora: el botón bajo el rayo se elige, con zona de clic ampliada y vibración leve al entrar) y el gatillo derecho. Cuando se hace
    /// el gesto, <see cref="Lograr"/> pinta una palomita, vibra y la cierra sola. No va en <see cref="PilaPantallas"/> ni pausa el juego: eso lo
    /// decide <see cref="TutorialVR"/>.
    /// </summary>
    public class TarjetaTutorialVR : MonoBehaviour
    {
        public const string TextoAprendido = "APRENDIDO";
        public const string PrefijoTitulo = "TUTORIAL · ";
        public const float AnchoPx = 1000f;
        public const float AltoPx = 760f;
        /// <summary>Tamaño de un px de la tarjeta en el mundo (m): 1000 px = 0.62 m.</summary>
        public const float MetrosPorPx = 0.00062f;
        /// <summary>Distancia al frente de la cabeza (m).</summary>
        public const float Adelante = 0.9f;
        /// <summary>Corrimiento a la derecha de la vista (m).</summary>
        public const float Lado = 0.32f;
        /// <summary>Cuánto queda debajo de los ojos (m).</summary>
        public const float Bajada = 0.24f;
        /// <summary>Segundos que se ve la palomita antes de cerrarse.</summary>
        public const float SegundosPalomita = 0.7f;
        /// <summary>Zona extra alrededor de APRENDIDO que también cuenta como apuntado (px).</summary>
        public const float MargenIman = 50f;

        private const float AnchoImagenPx = 880f;
        private const float AltoImagenPx = 495f;
        private const float CentroImagenYPx = 60f;
        private const float YTituloPx = 345f;
        private const float YFrasePx = -222f;
        private const float YExplicacionPx = -276f;
        private const float YBotonPx = -340f;
        private const float AnchoBotonPx = 320f;
        private const float AltoBotonPx = 56f;
        private const float AlfaFondo = 0.72f;
        private const float RapidezSeguir = 4f;
        private const float AlcanceRayo = 4f;
        private const float GrosorRayo = 0.004f;
        private const float GrosorPalomita = 14f;
        private const float AmplitudPasar = 0.15f;
        private const float SegundosPasar = 0.03f;
        private const float AmplitudLograr = 0.6f;
        private const float SegundosLograr = 0.2f;
        private const int TamanoExplicacion = 22;
        private static readonly Color ColorRayo = new Color(0.361f, 0.890f, 0.839f, 0.8f);
        private static readonly Vector2 GrosorContorno = new Vector2(1.5f, -1.5f);

        private static Material materialRayo;

        private Canvas canvas;
        private Image imagen;
        private Text frase;
        private Text explicacion;
        private GameObject palomita;
        private BotonCyberfix aprendido;
        private LineRenderer rayo;
        private Sprite creado;
        private bool apuntadoAntes;
        private float cerrarEn = -1f;
        private bool cerrada;
        private bool colocada;

        /// <summary>La tarjeta que se ve ahora (null si no hay).</summary>
        public static TarjetaTutorialVR Actual { get; private set; }

        /// <summary>True si el rayo derecho apunta a APRENDIDO de la tarjeta visible: el gatillo es clic, no disparo (para MenusVR.BloqueaDisparo).</summary>
        public static bool BotonApuntado { get; private set; }

        public PaginaGuiaVR Pagina { get; private set; }
        public Canvas Canvas => canvas;
        public Image Imagen => imagen;
        public BotonCyberfix BotonAprendido => aprendido;
        public string Frase => frase != null ? frase.text : string.Empty;
        public string Explicacion => explicacion != null ? explicacion.text : string.Empty;
        /// <summary>True si se cerró porque se hizo el gesto (se ve la palomita).</summary>
        public bool Lograda { get; private set; }
        public bool Visible => canvas != null && canvas.gameObject.activeSelf;

        /// <summary>Al cerrarse: la tarjeta y si fue por el gesto (true) o por APRENDIDO (false).</summary>
        public event Action<TarjetaTutorialVR, bool> Cerrada;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            BotonApuntado = false;
        }

        /// <summary>Crea la tarjeta de la página frente a la cabeza (cierra la anterior si había).</summary>
        public static TarjetaTutorialVR Crear(PaginaGuiaVR pagina)
        {
            if (Actual != null)
            {
                Actual.Cerrar(false);
            }
            var objeto = new GameObject("TarjetaTutorialVR_" + pagina);
            TarjetaTutorialVR tarjeta = objeto.AddComponent<TarjetaTutorialVR>();
            tarjeta.Construir(pagina);
            return tarjeta;
        }

        /// <summary>APRENDIDO: se cierra sin gesto.</summary>
        public void Aprender() => Cerrar(false);

        /// <summary>El gesto se hizo bien: palomita, vibración y se cierra sola tras <see cref="SegundosPalomita"/> s.</summary>
        public void Lograr()
        {
            if (cerrada || Lograda)
            {
                return;
            }
            Lograda = true;
            if (palomita != null)
            {
                palomita.SetActive(true);
            }
            if (aprendido != null)
            {
                aprendido.interactable = false;
            }
            Sonidos.Reproducir(BotonCyberfix.SonidoAceptar);
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null)
            {
                if (a.ManoIzquierda != null) a.ManoIzquierda.Vibrar(AmplitudLograr, SegundosLograr);
                if (a.ManoDerecha != null) a.ManoDerecha.Vibrar(AmplitudLograr, SegundosLograr);
            }
            cerrarEn = Time.unscaledTime + SegundosPalomita;
        }

        /// <summary>Esconde o vuelve a mostrar la tarjeta (mientras hay una pantalla encima).</summary>
        public void Mostrar(bool visible)
        {
            if (canvas != null && canvas.gameObject.activeSelf != visible)
            {
                canvas.gameObject.SetActive(visible);
                colocada = false;
            }
            if (!visible)
            {
                SoltarApuntado();
            }
        }

        /// <summary>Cierra la tarjeta (avisa <see cref="Cerrada"/> una vez).</summary>
        public void Cerrar(bool porGesto)
        {
            if (cerrada)
            {
                return;
            }
            cerrada = true;
            SoltarApuntado();
            if (Actual == this)
            {
                Actual = null;
            }
            Cerrada?.Invoke(this, porGesto);
            Destroy(gameObject);
        }

        private void Construir(PaginaGuiaVR pagina)
        {
            Actual = this;
            Pagina = pagina;
            var datos = TextosGuiaVR.De(pagina);

            var objeto = new GameObject("CanvasTarjetaVR", typeof(RectTransform));
            objeto.transform.SetParent(transform, false);
            canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 65;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.dynamicPixelsPerUnit = 2f;
            var rect = (RectTransform)objeto.transform;
            rect.sizeDelta = new Vector2(AnchoPx, AltoPx);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * MetrosPorPx;

            Image fondo = TrazoH10.Panel(rect, "Fondo", TrazoH10.TurquesaTenue, TrazoH10.ConAlfa(TrazoH10.FondoPantalla, AlfaFondo));
            TrazoH10.Estirar(fondo.rectTransform);
            fondo.raycastTarget = false;

            Texto(rect, "Titulo", PrefijoTitulo + datos.titulo, TrazoH10.TamanoSeccion, TrazoH10.Texto, YTituloPx, 50f);

            imagen = TrazoH10.Imagen(rect, "ImagenTarjeta", Color.white);
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
            TrazoH10.Colocar(imagen.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, CentroImagenYPx), new Vector2(AnchoImagenPx, AltoImagenPx));
            imagen.sprite = CargarImagen(datos.imagen);
            imagen.color = imagen.sprite != null ? Color.white : TrazoH10.TurquesaFondo;
            Image marco = TrazoH10.Panel(rect, "MarcoImagen", TrazoH10.TurquesaTenue, Color.clear);
            marco.raycastTarget = false;
            TrazoH10.Colocar(marco.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, CentroImagenYPx), new Vector2(AnchoImagenPx + 4f, AltoImagenPx + 4f));

            frase = Texto(rect, "Frase", datos.frase, TrazoH10.TamanoFila, TrazoH10.Turquesa, YFrasePx, 44f);
            explicacion = Texto(rect, "Explicacion", datos.explicacion, TamanoExplicacion, TrazoH10.Texto, YExplicacionPx, 60f);
            explicacion.horizontalOverflow = HorizontalWrapMode.Wrap;

            aprendido = KitUI.CrearBoton(rect, TextoAprendido, Aprender);
            aprendido.Texto.alignment = TextAnchor.MiddleCenter;
            VestidoH10.Boton(aprendido);
            TrazoH10.Punto((RectTransform)aprendido.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, YBotonPx), new Vector2(AnchoBotonPx, AltoBotonPx));

            CrearPalomita(rect);
            VestidoH10.Fuente(rect);
            CrearRayo();
            Colocar(true);
            Sonidos.Reproducir("UI_MENU_ABRIR");
            Debug.Log("[VR] Tutorial: tarjeta " + pagina + (imagen.sprite != null ? "." : " (falta la imagen " + datos.imagen + ")."));
        }

        private Text Texto(RectTransform padre, string nombre, string contenido, int tamano, Color color, float y, float alto)
        {
            Text t = TrazoH10.Etiqueta(padre, nombre, contenido, tamano, color, TextAnchor.MiddleCenter);
            TrazoH10.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, y), new Vector2(AnchoPx - 2f * TrazoH10.MargenPantalla, alto));
            t.raycastTarget = false;
            Outline contorno = t.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
            contorno.effectDistance = GrosorContorno;
            return t;
        }

        // La palomita: dos trazos turquesa sobre la imagen (sin depender de que la fuente tenga el signo ✓).
        private void CrearPalomita(RectTransform padre)
        {
            var grupo = new GameObject("Palomita", typeof(RectTransform));
            grupo.transform.SetParent(padre, false);
            var r = (RectTransform)grupo.transform;
            TrazoH10.Estirar(r);
            Vector2 centro = new Vector2(AnchoPx * 0.5f, AltoPx * 0.5f + CentroImagenYPx);
            Trazo(r, "Corto", centro + new Vector2(-90f, 0f), centro + new Vector2(-25f, -70f));
            Trazo(r, "Largo", centro + new Vector2(-25f, -70f), centro + new Vector2(110f, 95f));
            palomita = grupo;
            palomita.SetActive(false);
        }

        private static void Trazo(RectTransform padre, string nombre, Vector2 a, Vector2 b)
        {
            Image linea = TrazoH10.Imagen(padre, nombre, TrazoH10.Turquesa);
            linea.raycastTarget = false;
            TrazoH10.Unir(linea.rectTransform, a, b);
            linea.rectTransform.sizeDelta = new Vector2(linea.rectTransform.sizeDelta.x, GrosorPalomita);
        }

        private void CrearRayo()
        {
            var objeto = new GameObject("RayoTarjetaVR");
            objeto.transform.SetParent(transform, false);
            rayo = objeto.AddComponent<LineRenderer>();
            rayo.useWorldSpace = true;
            rayo.positionCount = 2;
            rayo.widthMultiplier = GrosorRayo;
            rayo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rayo.receiveShadows = false;
            if (materialRayo == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                materialRayo = shader != null ? new Material(shader) : new Material(Canvas.GetDefaultCanvasMaterial());
            }
            rayo.sharedMaterial = materialRayo;
            rayo.startColor = ColorRayo;
            rayo.endColor = ColorRayo;
            rayo.enabled = false;
        }

        private static Sprite CargarSprite(string nombre, out bool creadoAqui)
        {
            creadoAqui = false;
            string ruta = TextosGuiaVR.CarpetaImagenes + "/" + nombre;
            Sprite sprite = Resources.Load<Sprite>(ruta);
            if (sprite != null)
            {
                return sprite;
            }
            Texture2D textura = Resources.Load<Texture2D>(ruta);
            if (textura == null)
            {
                Debug.LogWarning("[VR] Tutorial: falta la imagen " + ruta + " en Resources (Assets/Arte/UI/ControlesVR/Resources).");
                return null;
            }
            creadoAqui = true;
            Sprite hecho = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f));
            hecho.name = textura.name;
            return hecho;
        }

        private Sprite CargarImagen(string nombre)
        {
            Sprite s = CargarSprite(nombre, out bool creadoAqui);
            if (creadoAqui)
            {
                creado = s;
            }
            return s;
        }

        private static Transform Cabeza()
        {
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null && a.Armado && a.Camara != null)
            {
                return a.Camara.transform;
            }
            Camera principal = Camera.main;
            return principal != null ? principal.transform : null;
        }

        private static Transform ManoDerecha()
        {
            if (MenusVR.Actual != null && MenusVR.Actual.ManoLaser != null)
            {
                return MenusVR.Actual.ManoLaser;
            }
            AdaptadorVR a = AdaptadorVR.Actual;
            return a != null && a.ManoDerecha != null ? a.ManoDerecha.transform : null;
        }

        // Abajo y a la derecha de la vista, en el plano (sin inclinar con la cabeza), de cara a los ojos. Al principio salta; luego sigue suave.
        private void Colocar(bool deGolpe)
        {
            Transform cabeza = Cabeza();
            if (cabeza == null)
            {
                return;
            }
            Vector3 frente = cabeza.forward;
            frente.y = 0f;
            frente = frente.sqrMagnitude > 1e-4f ? frente.normalized : (transform.forward.sqrMagnitude > 1e-4f ? transform.forward : Vector3.forward);
            Vector3 derecha = Vector3.Cross(Vector3.up, frente);
            Vector3 destino = cabeza.position + frente * Adelante + derecha * Lado + Vector3.down * Bajada;
            Vector3 posicion = deGolpe ? destino : Vector3.Lerp(transform.position, destino, 1f - Mathf.Exp(-RapidezSeguir * Time.unscaledDeltaTime));
            Vector3 mirar = posicion - cabeza.position;
            mirar.y = 0f;
            Quaternion giro = mirar.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(mirar.normalized, Vector3.up) : Quaternion.LookRotation(frente, Vector3.up);
            transform.SetPositionAndRotation(posicion, giro);
        }

        // El rayo del control derecho contra el plano de la tarjeta: dónde toca (px) y a qué distancia.
        private bool RayoATarjeta(out Vector2 px, out float distancia, out Vector3 punto)
        {
            px = Vector2.zero;
            distancia = 0f;
            punto = Vector3.zero;
            Transform mano = ManoDerecha();
            if (mano == null || canvas == null)
            {
                return false;
            }
            Transform lienzo = canvas.transform;
            var plano = new Plane(-lienzo.forward, lienzo.position);
            var r = new Ray(mano.position, mano.forward);
            if (!plano.Raycast(r, out distancia) || distancia > AlcanceRayo)
            {
                return false;
            }
            punto = r.GetPoint(distancia);
            Vector3 local = lienzo.InverseTransformPoint(punto);
            px = new Vector2(local.x, local.y);
            return Mathf.Abs(px.x) <= AnchoPx * 0.5f && Mathf.Abs(px.y) <= AltoPx * 0.5f;
        }

        /// <summary>True si el rayo del control derecho toca APRENDIDO (con su zona ampliada).</summary>
        public bool ApuntaAAprendido()
        {
            if (aprendido == null || !RayoATarjeta(out Vector2 px, out _, out _))
            {
                return false;
            }
            var rect = (RectTransform)aprendido.transform;
            Vector3 local = rect.InverseTransformPoint(canvas.transform.TransformPoint(new Vector3(px.x, px.y, 0f)));
            Rect zona = rect.rect;
            float dx = Mathf.Max(zona.xMin - local.x, 0f, local.x - zona.xMax);
            float dy = Mathf.Max(zona.yMin - local.y, 0f, local.y - zona.yMax);
            return dx * dx + dy * dy <= MargenIman * MargenIman;
        }

        private void Update()
        {
            if (cerrada)
            {
                return;
            }
            if (!ModoVR.Activo)
            {
                Cerrar(false);
                return;
            }
            if (cerrarEn > 0f && Time.unscaledTime >= cerrarEn)
            {
                Cerrar(true);
                return;
            }
            if (!Visible || Lograda)
            {
                SoltarApuntado();
                return;
            }
            bool apunta = ApuntaAAprendido();
            BotonApuntado = apunta;
            if (apunta)
            {
                if (!aprendido.EstaElegido)
                {
                    aprendido.Elegir();
                }
                if (!apuntadoAntes)
                {
                    VibrarDerecha(AmplitudPasar, SegundosPasar);
                }
                if (EntradaVR.Pulso(EntradaVR.GatilloDerecho))
                {
                    apuntadoAntes = true;
                    aprendido.Pulsar(); // llama a Aprender
                    return;
                }
            }
            else if (apuntadoAntes)
            {
                Deseleccionar();
            }
            apuntadoAntes = apunta;
        }

        private void LateUpdate()
        {
            if (cerrada || !Visible)
            {
                if (rayo != null)
                {
                    rayo.enabled = false;
                }
                return;
            }
            Colocar(!colocada);
            colocada = true;
            // El rayo solo se dibuja mientras apunta a la tarjeta (fuera de los menús el láser de MenusVR no se ve).
            bool toca = RayoATarjeta(out _, out _, out Vector3 punto);
            Transform mano = ManoDerecha();
            if (rayo != null)
            {
                rayo.enabled = toca && mano != null && !Lograda;
                if (rayo.enabled)
                {
                    rayo.SetPosition(0, mano.position);
                    rayo.SetPosition(1, punto);
                }
            }
        }

        private void Deseleccionar()
        {
            if (aprendido != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == aprendido.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static void VibrarDerecha(float amplitud, float segundos)
        {
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null && a.ManoDerecha != null)
            {
                a.ManoDerecha.Vibrar(amplitud, segundos);
            }
        }

        private void SoltarApuntado()
        {
            if (Actual == this || Actual == null)
            {
                BotonApuntado = false;
            }
            if (apuntadoAntes)
            {
                Deseleccionar();
            }
            apuntadoAntes = false;
            if (rayo != null)
            {
                rayo.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
                BotonApuntado = false;
            }
            if (creado != null)
            {
                Destroy(creado);
            }
        }
    }
}
