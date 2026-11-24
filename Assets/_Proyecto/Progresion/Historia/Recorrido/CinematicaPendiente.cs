using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10, 10.1): reproduce una <see cref="DatosCinematica"/>. Sin video: fundido a negro de 0.5 s, "CINEMÁTICA · NOMBRE", una línea con lo que pasa,
    /// barra de progreso y "ENTER: SALTAR"; dura exactamente la duración y vuelve con otro fundido. Con VideoClip: el video a pantalla completa con su audio.
    /// Durante la cinemática: <see cref="CinematicaEnCurso"/> activo, el jugador sin control, la entrada apagada, el HUD oculto y la música baja.
    /// <c>enNegro</c> se llama cuando la pantalla ya está negra y va a empezar el fundido de salida (momento de acomodar el mundo sin que se vea);
    /// <c>alTerminar</c> al devolver el control. Si ya hay una en curso, las nuevas hacen fila.
    /// </summary>
    public class CinematicaPendiente : MonoBehaviour
    {
        public const string PrefijoTitulo = "CINEMÁTICA · ";
        public const string TextoSaltar = "ENTER: SALTAR";
        /// <summary>V12: en VR se salta con B (o el gatillo derecho).</summary>
        public const string TextoSaltarVR = "B: SALTAR";
        public const int OrdenCanvas = 30000;
        private const float VolumenMusicaBajada = 0.2f;
        private const float SegundosSinSaltar = 0.3f;
        private const float AltoPorLetra = 1.4f;
        private const float AnchoBarra = 900f;
        private const float AltoBarra = 6f;
        private const float AlfaFondoBarra = 0.15f;

        private sealed class Pedido
        {
            public DatosCinematica Datos;
            public Action EnNegro;
            public Action AlTerminar;
        }

        private static CinematicaPendiente instancia;
        private static readonly Queue<Pedido> fila = new Queue<Pedido>();

        /// <summary>True mientras una cinemática está en pantalla.</summary>
        public static bool EnCurso => instancia != null && instancia.pedido != null;

        /// <summary>Id de la cinemática en pantalla ("" si ninguna).</summary>
        public static string IdActual => EnCurso ? instancia.pedido.Datos.Id : string.Empty;

        /// <summary>Todas las que han salido en esta sesión, en orden (para pruebas y depuración).</summary>
        public static readonly List<string> Historial = new List<string>();

        /// <summary>Se avisa con el id al devolver el control tras cada cinemática.</summary>
        public static event Action<string> Terminada;

        /// <summary>La regla de tiempo de la que está en pantalla (null si ninguna), para pruebas: fase, progreso y duración.</summary>
        public static ReglaCinematica ReglaActual => EnCurso ? instancia.regla : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            instancia = null;
            fila.Clear();
            Historial.Clear();
            Terminada = null;
        }

        /// <summary>Reproduce la cinemática (o la pone en fila). Los dos avisos pueden ser null.</summary>
        public static void Reproducir(DatosCinematica datos, Action enNegro = null, Action alTerminar = null)
        {
            if (datos == null)
            {
                Debug.LogWarning("[Historia] Cinemática sin datos: se salta y se sigue con la historia.");
                enNegro?.Invoke();
                alTerminar?.Invoke();
                return;
            }
            var pedidoNuevo = new Pedido { Datos = datos, EnNegro = enNegro, AlTerminar = alTerminar };
            if (instancia == null)
            {
                instancia = new GameObject("CinematicaPendiente").AddComponent<CinematicaPendiente>();
            }
            if (instancia.pedido != null)
            {
                fila.Enqueue(pedidoNuevo);
                return;
            }
            instancia.Iniciar(pedidoNuevo);
        }

        /// <summary>Salta el contenido de la que está en pantalla (lo mismo que ENTER). Para pruebas y depuración.</summary>
        public static bool SaltarActual()
        {
            if (!EnCurso || instancia.regla == null || !instancia.regla.PuedeSaltar)
            {
                return false;
            }
            instancia.saltarPedido = true;
            return true;
        }

        private Pedido pedido;
        private ReglaCinematica regla;
        private bool saltarPedido;
        private bool enNegroLlamado;
        private float inicio;

        private Canvas canvas;
        private Image negro;
        private RawImage pantallaVideo;
        private VideoPlayer video;
        private RenderTexture texturaVideo;
        private bool videoIniciado;
        private Text textoTitulo;
        private Text textoLinea;
        private Image barraFondo;
        private Image barraRelleno;
        private Text textoSaltar;

        private ControladorAPie controlador;
        private bool controlAntes;
        private readonly List<Canvas> canvasOcultos = new List<Canvas>();
        private readonly List<InputAction> accionesApagadas = new List<InputAction>();
        private float musicaAntes = -1f;

        private void OnDestroy()
        {
            if (instancia == this)
            {
                if (pedido != null)
                {
                    Liberar();
                }
                instancia = null;
            }
        }

        private void Iniciar(Pedido nuevo)
        {
            pedido = nuevo;
            DatosCinematica d = pedido.Datos;
            Historial.Add(d.Id);
            regla = new ReglaCinematica(d.DuracionEfectiva);
            saltarPedido = false;
            enNegroLlamado = false;
            inicio = Time.unscaledTime;
            Debug.Log("[Historia] Cinemática " + d.Id + " · " + d.Nombre + " (" + d.DuracionEfectiva.ToString("0.0") + " s" + (d.Video != null ? ", video" : ", pendiente") + ")");
            Bloquear();
            ArmarPantalla(d);
            if (d.Video != null)
            {
                PrepararVideo(d.Video);
            }
            Aplicar();
        }

        private void Bloquear()
        {
            CinematicaEnCurso.Fijar(true);
            IJugador jugador = Registro<IJugador>.Actual;
            controlador = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>(true) : null;
            if (controlador != null)
            {
                controlAntes = controlador.ControlActivo;
                controlador.ActivarControl(false);
            }
            // El HUD (todos los canvas raíz que no son de la historia) y la entrada se apagan; se vuelven a prender igual al terminar.
            canvasOcultos.Clear();
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace && c.sortingOrder < OrdenCanvas)
                {
                    c.enabled = false;
                    canvasOcultos.Add(c);
                }
            }
            accionesApagadas.Clear();
            InputSystem.ListEnabledActions(accionesApagadas);
            // V12: en VR el seguimiento de la cabeza y las manos (posición, giro, estado) sigue vivo: si se apaga, la imagen se queda pegada a la cara.
            accionesApagadas.RemoveAll(EsSeguimientoVR);
            foreach (InputAction accion in accionesApagadas)
            {
                accion.Disable();
            }
            musicaAntes = Sonidos.Volumen(SeccionSonido.Musica);
            Sonidos.FijarVolumen(SeccionSonido.Musica, musicaAntes * VolumenMusicaBajada);
            PilaPantallas.Abrir(this);
        }

        /// <summary>V12: true si la acción es de seguimiento de VR (pose de la cabeza o de un control), que nunca se apaga en VR.</summary>
        public static bool EsSeguimientoVR(InputAction accion)
        {
            if (accion == null || !MechaCyberpunk.Core.ModoVR.Activo)
            {
                return false;
            }
            string tipo = accion.expectedControlType;
            if (tipo == "Vector3" || tipo == "Quaternion" || tipo == "Pose")
            {
                return true;
            }
            string nombre = accion.name ?? string.Empty;
            // Los botones de los controles (EntradaVR, "VR_..."): B salta la cinemática en VR.
            if (nombre.StartsWith("VR_", StringComparison.Ordinal))
            {
                return true;
            }
            return nombre.IndexOf("Tracking", System.StringComparison.OrdinalIgnoreCase) >= 0
                || nombre.IndexOf("Position", System.StringComparison.OrdinalIgnoreCase) >= 0
                || nombre.IndexOf("Rotation", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Liberar()
        {
            PilaPantallas.Cerrar(this);
            foreach (InputAction accion in accionesApagadas)
            {
                accion.Enable();
            }
            accionesApagadas.Clear();
            foreach (Canvas c in canvasOcultos)
            {
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            canvasOcultos.Clear();
            if (musicaAntes >= 0f)
            {
                Sonidos.FijarVolumen(SeccionSonido.Musica, musicaAntes);
                musicaAntes = -1f;
            }
            if (controlador != null)
            {
                controlador.ActivarControl(controlAntes);
                controlador = null;
            }
            LimpiarPantalla();
            pedido = null;
            if (fila.Count == 0)
            {
                CinematicaEnCurso.Fijar(false);
            }
        }

        private void ArmarPantalla(DatosCinematica d)
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasCinematica", OrdenCanvas);
            canvas.overrideSorting = true;
            negro = KitUI.CrearImagen(canvas.transform, "Negro", new Color(0f, 0f, 0f, 0f));
            KitUI.Estirar(negro.rectTransform);
            negro.raycastTarget = true;
            if (d.Video != null)
            {
                GameObject v = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
                v.transform.SetParent(canvas.transform, false);
                KitUI.Estirar((RectTransform)v.transform);
                pantallaVideo = v.GetComponent<RawImage>();
                pantallaVideo.raycastTarget = false;
                pantallaVideo.enabled = false;
            }
            EstiloCyberfix e = KitUI.Estilo;
            bool pendiente = d.Video == null;
            textoTitulo = TextoCentrado("Titulo", RolTexto.Titulo, e.Turquesa, 0.56f);
            textoTitulo.text = PrefijoTitulo + d.Nombre;
            textoTitulo.enabled = pendiente;
            textoLinea = TextoCentrado("Linea", RolTexto.Texto, e.Texto, 0.44f);
            textoLinea.text = d.Texto;
            textoLinea.horizontalOverflow = HorizontalWrapMode.Wrap;
            textoLinea.rectTransform.sizeDelta = new Vector2(-480f, textoLinea.rectTransform.sizeDelta.y * 2.4f);
            textoLinea.enabled = pendiente;
            barraFondo = KitUI.CrearImagen(canvas.transform, "BarraFondo", new Color(1f, 1f, 1f, AlfaFondoBarra));
            KitUI.Colocar(barraFondo.rectTransform, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(AnchoBarra, AltoBarra));
            barraFondo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            barraRelleno = KitUI.CrearImagen(barraFondo.transform, "Relleno", e.Turquesa);
            RectTransform rr = barraRelleno.rectTransform;
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = new Vector2(0f, 1f);
            rr.pivot = new Vector2(0f, 0.5f);
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;
            barraFondo.enabled = pendiente;
            barraRelleno.enabled = pendiente;
            textoSaltar = KitUI.CrearTexto(canvas.transform, "Saltar", RolTexto.Ayuda, TextAnchor.LowerRight);
            KitUI.Colocar(textoSaltar.rectTransform, new Vector2(1f, 0f), new Vector2(-48f, 36f), new Vector2(400f, 40f));
            textoSaltar.color = e.Desactivado;
            textoSaltar.text = ModoVR.Activo ? TextoSaltarVR : TextoSaltar;
        }

        private Text TextoCentrado(string nombre, RolTexto rol, Color color, float altura)
        {
            Text texto = KitUI.CrearTexto(canvas.transform, nombre, rol, TextAnchor.MiddleCenter);
            RectTransform rect = texto.rectTransform;
            rect.anchorMin = new Vector2(0f, altura);
            rect.anchorMax = new Vector2(1f, altura);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, KitUI.Estilo.Tamano(rol) * AltoPorLetra);
            texto.color = color;
            return texto;
        }

        private void PrepararVideo(VideoClip clip)
        {
            texturaVideo = new RenderTexture((int)clip.width, (int)clip.height, 0);
            pantallaVideo.texture = texturaVideo;
            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.clip = clip;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = texturaVideo;
            video.isLooping = false;
            AudioSource fuente = gameObject.GetComponent<AudioSource>();
            if (fuente == null)
            {
                fuente = gameObject.AddComponent<AudioSource>();
            }
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            IMezclaAudio mezcla = Registro<IMezclaAudio>.Actual;
            if (mezcla != null)
            {
                fuente.outputAudioMixerGroup = mezcla.Grupo(SeccionSonido.Voces);
            }
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.SetTargetAudioSource(0, fuente);
            video.Prepare();
        }

        private void LimpiarPantalla()
        {
            if (video != null)
            {
                video.Stop();
                Destroy(video);
                video = null;
            }
            AudioSource fuente = GetComponent<AudioSource>();
            if (fuente != null)
            {
                Destroy(fuente);
            }
            if (texturaVideo != null)
            {
                texturaVideo.Release();
                Destroy(texturaVideo);
                texturaVideo = null;
            }
            videoIniciado = false;
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
        }

        private void Update()
        {
            if (pedido == null)
            {
                return;
            }
            bool saltar = saltarPedido;
            Keyboard teclado = Keyboard.current;
            if (teclado != null && Time.unscaledTime - inicio >= SegundosSinSaltar
                && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame))
            {
                saltar = true;
            }
            saltarPedido = false;
            // El video espera a estar preparado (con tope de 5 s): el contenido no corre hasta que el primer cuadro puede verse.
            bool esperaVideo = video != null && !videoIniciado && !video.isPrepared
                && regla.Actual == ReglaCinematica.Fase.FundidoEntrada && regla.EnFase >= ReglaCinematica.SegundosFundido - 0.01f
                && Time.unscaledTime - inicio < 5f;
            ReglaCinematica.Fase? cambio = esperaVideo ? null : regla.Avanzar(Time.unscaledDeltaTime, saltar);
            if (cambio.HasValue)
            {
                AlCambiarFase(cambio.Value);
            }
            if (pedido == null)
            {
                return;
            }
            Aplicar();
        }

        private void AlCambiarFase(ReglaCinematica.Fase fase)
        {
            switch (fase)
            {
                case ReglaCinematica.Fase.Contenido:
                    if (video != null)
                    {
                        video.Play();
                        videoIniciado = true;
                        pantallaVideo.enabled = true;
                    }
                    break;
                case ReglaCinematica.Fase.FundidoSalida:
                    if (video != null)
                    {
                        video.Stop();
                        pantallaVideo.enabled = false;
                    }
                    textoTitulo.enabled = false;
                    textoLinea.enabled = false;
                    barraFondo.enabled = false;
                    barraRelleno.enabled = false;
                    textoSaltar.enabled = false;
                    LlamarEnNegro();
                    break;
                case ReglaCinematica.Fase.Terminada:
                    Terminar();
                    break;
            }
        }

        private void LlamarEnNegro()
        {
            if (enNegroLlamado)
            {
                return;
            }
            enNegroLlamado = true;
            Ejecutar(pedido.EnNegro);
        }

        private void Terminar()
        {
            Pedido hecho = pedido;
            Liberar();
            Ejecutar(hecho.AlTerminar);
            Terminada?.Invoke(hecho.Datos.Id);
            if (pedido == null && fila.Count > 0)
            {
                Iniciar(fila.Dequeue());
            }
        }

        private static void Ejecutar(Action accion)
        {
            try
            {
                accion?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Aplicar()
        {
            if (negro == null)
            {
                return;
            }
            float alfa = regla.Alfa;
            SetAlfa(negro, alfa);
            SetAlfa(textoTitulo, alfa);
            SetAlfa(textoLinea, alfa);
            SetAlfa(textoSaltar, alfa);
            if (barraRelleno != null)
            {
                barraRelleno.rectTransform.anchorMax = new Vector2(regla.Progreso, 1f);
                SetAlfa(barraRelleno, alfa);
                SetAlfa(barraFondo, AlfaFondoBarra * alfa);
            }
        }

        private static void SetAlfa(Graphic g, float alfa)
        {
            if (g == null)
            {
                return;
            }
            Color c = g.color;
            c.a = alfa;
            g.color = c;
        }
    }
}
