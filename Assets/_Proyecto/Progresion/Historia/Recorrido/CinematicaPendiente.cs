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
    /// Durante la cinemática: <see cref="CinematicaEnCurso"/> activo, el jugador sin control, la entrada apagada, el HUD oculto y, mientras suena la narración, DuckingNarracion.Activo (la música de fases se baja sola con esa bandera).
    /// <c>enNegro</c> se llama cuando la pantalla ya está negra y va a empezar el fundido de salida (momento de acomodar el mundo sin que se vea);
    /// <c>alTerminar</c> al devolver el control. Si ya hay una en curso, las nuevas hacen fila.
    /// </summary>
    public class CinematicaPendiente : MonoBehaviour
    {
        public const string PrefijoTitulo = "CINEMÁTICA · ";
        public const string TextoSaltar = "ENTER: SALTAR";
        /// <summary>V12: en VR se salta con B (o el gatillo derecho).</summary>
        public const string TextoSaltarVR = "B: SALTAR";
        /// <summary>T-omitir-cinematicas-24nov: el botón visible abajo a la derecha (clic o ENTER en PC; láser y gatillo, o B, en VR).</summary>
        public const string TextoOmitir = "OMITIR";
        public const string NombreCanvasOmitir = "CanvasOmitirCinematica";
        private const float AnchoBotonOmitir = 260f;
        private const float AltoBotonOmitir = 56f;
        private const float MargenBotonX = 48f;
        private const float MargenBotonY = 36f;
        private const float SeparacionAyuda = 12f;
        public const int OrdenCanvas = 30000;
        private const float SegundosSinSaltar = 0.3f;
        private const float AltoPorLetra = 1.4f;
        private const float AnchoBarra = 900f;
        private const float AltoBarra = 6f;
        private const float AlfaFondoBarra = 0.15f;
        private const float MargenVideo = 10f;

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
        // T-cinematicas-videos-23nov: dos VideoPlayer alternados; mientras uno suena, el otro ya prepara el clip siguiente (sin negro entre clips).
        private readonly VideoPlayer[] reproductores = new VideoPlayer[2];
        private readonly RenderTexture[] texturas = new RenderTexture[2];
        private readonly bool[] acabo = new bool[2];
        private IReadOnlyList<VideoClip> clips;
        private int clipActual;
        private int activo;
        private bool cambiando;
        private bool enPendiente;
        private float pendienteInicio;
        // El reproductor de la toma en pantalla si es video (null si no hay tomas o la toma es imagen fija).
        private VideoPlayer video => hayVideos && clips != null && clipActual < clips.Count && clips[clipActual] != null ? reproductores[activo] : null;
        private bool videoIniciado;
        // T-cinematicas-narracion-24nov: la frase del clip en pantalla (un AudioSource 2D en el grupo Voces) y su regla de tiempo.
        private AudioSource narrador;
        private readonly AudioSource[] fuentesVideo = new AudioSource[2];
        private ReglaNarracion reglaClip;
        private int reproductorClip;
        private bool fundiendoVoz;
        // T97: las tomas que son imagen fija se componen (Ken Burns y fundido con la anterior) en el lienzo, que se ve en pantallaVideo.
        private bool hayVideos;
        private bool enFoto;
        private RenderTexture lienzo;
        private RenderTexture lienzoAnterior;
        private bool hayAnterior;
        private Texture2D fotoActual;
        private ReglaFotoCinematica reglaFoto;
        private Canvas canvasOmitir;
        private CanvasGroup grupoOmitir;
        private RectTransform rectOmitir;
        private bool cursorGuardado;
        private CursorLockMode cursorAntes;
        private bool cursorVisibleAntes;
        private Text textoTitulo;
        private Text textoLinea;
        private Image barraFondo;
        private Image barraRelleno;
        private Text textoSaltar;

        private ControladorAPie controlador;
        private bool controlAntes;
        private readonly List<Canvas> canvasOcultos = new List<Canvas>();
        private readonly List<InputAction> accionesApagadas = new List<InputAction>();

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
            clips = d.Clips;
            clipActual = 0;
            activo = 0;
            cambiando = false;
            enPendiente = false;
            enFoto = false;
            hayAnterior = false;
            hayVideos = false;
            foreach (VideoClip c in clips)
            {
                hayVideos |= c != null;
            }
            // Con videos, el final lo marca el último clip (o la parte pendiente): el tope de la regla lleva margen por si el video se atrasa.
            if (clips.Count > 0)
            {
                regla = new ReglaCinematica(d.DuracionEfectiva + MargenVideo);
            }
            Debug.Log("[Historia] Cinemática " + d.Id + " · " + d.Nombre + " (" + d.DuracionEfectiva.ToString("0.0") + " s" + (clips.Count > 0 ? ", " + clips.Count + " tomas, " + d.CantidadFotos + " fijas" : ", pendiente") + ")");
            Bloquear();
            ArmarPantalla(d);
            if (clips.Count > 0)
            {
                PrepararTomas();
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
            // T-omitir-cinematicas-24nov: en PC el ratón queda libre para pulsar OMITIR; al terminar vuelve como estaba.
            if (!ModoVR.Activo)
            {
                cursorGuardado = true;
                cursorAntes = Cursor.lockState;
                cursorVisibleAntes = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
            DuckingNarracion.Activo = false;
            if (cursorGuardado)
            {
                cursorGuardado = false;
                Cursor.lockState = cursorAntes;
                Cursor.visible = cursorVisibleAntes;
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
            if (d.Clips.Count > 0)
            {
                GameObject v = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
                v.transform.SetParent(canvas.transform, false);
                KitUI.Estirar((RectTransform)v.transform);
                pantallaVideo = v.GetComponent<RawImage>();
                pantallaVideo.raycastTarget = false;
                pantallaVideo.enabled = false;
            }
            EstiloCyberfix e = KitUI.Estilo;
            bool pendiente = d.Clips.Count == 0;
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
            KitUI.Colocar(textoSaltar.rectTransform, new Vector2(1f, 0f), new Vector2(-MargenBotonX, MargenBotonY + AltoBotonOmitir + SeparacionAyuda),
                new Vector2(400f, 40f));
            textoSaltar.color = e.Desactivado;
            textoSaltar.text = ModoVR.Activo ? TextoSaltarVR : TextoSaltar;
            ArmarOmitir();
        }

        /// <summary>
        /// T-omitir-cinematicas-24nov: "OMITIR" abajo a la derecha, en su propio Canvas encima de la cinemática (así en VR no se apaga con el
        /// negro cuando el video pasa a la pantalla de cine, y el láser lo puede pulsar). Clic, ENTER, B o gatillo saltan la cinemática completa.
        /// </summary>
        private void ArmarOmitir()
        {
            canvasOmitir = KitUI.CrearCanvas(transform, NombreCanvasOmitir, OrdenCanvas + 1);
            canvasOmitir.overrideSorting = true;
            grupoOmitir = canvasOmitir.gameObject.AddComponent<CanvasGroup>();
            grupoOmitir.alpha = 0f;
            BotonCyberfix boton = KitUI.CrearBoton(canvasOmitir.transform, TextoOmitir, () => SaltarActual());
            rectOmitir = (RectTransform)boton.transform;
            KitUI.Colocar(rectOmitir, new Vector2(1f, 0f), new Vector2(-MargenBotonX, MargenBotonY), new Vector2(AnchoBotonOmitir, AltoBotonOmitir));
        }

        /// <summary>True si este cuadro se hizo clic izquierdo sobre OMITIR (en PC la entrada de la UI está apagada en la cinemática: se mira aquí).</summary>
        private bool ClicEnOmitir()
        {
            Mouse raton = Mouse.current;
            if (raton == null || rectOmitir == null || canvasOmitir == null || !canvasOmitir.enabled || !raton.leftButton.wasPressedThisFrame)
            {
                return false;
            }
            Camera camara = canvasOmitir.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasOmitir.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(rectOmitir, raton.position.ReadValue(), camara);
        }

        /// <summary>T-omitir-cinematicas-24nov: el botón OMITIR de la cinemática en pantalla (null si ninguna). Para pruebas.</summary>
        public static RectTransform BotonOmitir => EnCurso ? instancia.rectOmitir : null;

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

        private void PrepararTomas()
        {
            IMezclaAudio mezcla = Registro<IMezclaAudio>.Actual;
            // T97: sin ninguna toma de video no se crean VideoPlayer (las imágenes fijas van en el lienzo).
            for (int i = 0; i < 2 && hayVideos; i++)
            {
                // Cada reproductor en su hijo, con su AudioSource (2D, grupo Efectos: los videos traen solo efectos).
                var hijo = new GameObject("Video" + i);
                hijo.transform.SetParent(transform, false);
                VideoPlayer vp = hijo.AddComponent<VideoPlayer>();
                vp.playOnAwake = false;
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.isLooping = false;
                vp.skipOnDrop = true;
                AudioSource fuente = hijo.AddComponent<AudioSource>();
                fuente.playOnAwake = false;
                fuente.spatialBlend = 0f;
                if (mezcla != null)
                {
                    fuente.outputAudioMixerGroup = mezcla.Grupo(SeccionSonido.Efectos);
                }
                vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
                vp.SetTargetAudioSource(0, fuente);
                fuentesVideo[i] = fuente;
                int indice = i;
                vp.loopPointReached += _ => acabo[indice] = true;
                reproductores[i] = vp;
            }
            // T-cinematicas-narracion-24nov: la voz en off va 2D por el grupo Voces (los deslizadores de voces la controlan).
            var objetoVoz = new GameObject("Narracion");
            objetoVoz.transform.SetParent(transform, false);
            narrador = objetoVoz.AddComponent<AudioSource>();
            narrador.playOnAwake = false;
            narrador.loop = false;
            narrador.spatialBlend = 0f;
            if (mezcla != null)
            {
                narrador.outputAudioMixerGroup = mezcla.Grupo(SeccionSonido.Voces);
            }
            if (clips[0] != null)
            {
                Cargar(0, 0);
            }
            if (clips.Count > 1)
            {
                Cargar(1, 1);
            }
        }

        /// <summary>T97: si la toma que sigue es video, la prepara en el reproductor libre.</summary>
        private void PrecargarSiguiente()
        {
            if (clipActual + 1 < clips.Count)
            {
                Cargar(1 - activo, clipActual + 1);
            }
        }

        /// <summary>
        /// Pone el clip <paramref name="indiceClip"/> en el reproductor <paramref name="r"/> y lo prepara (su textura del tamaño del clip).
        /// T97: si esa toma es imagen fija no hace nada.
        /// </summary>
        private void Cargar(int r, int indiceClip)
        {
            VideoClip clip = clips[indiceClip];
            VideoPlayer vp = reproductores[r];
            if (clip == null || vp == null)
            {
                return;
            }
            if (texturas[r] == null || texturas[r].width != (int)clip.width || texturas[r].height != (int)clip.height)
            {
                if (texturas[r] != null)
                {
                    texturas[r].Release();
                    Destroy(texturas[r]);
                }
                texturas[r] = new RenderTexture((int)clip.width, (int)clip.height, 0);
            }
            acabo[r] = false;
            vp.clip = clip;
            vp.targetTexture = texturas[r];
            vp.Prepare();
        }

        /// <summary>
        /// Lleva la cadena de clips: al acabar uno arranca el siguiente (ya preparado) y deja ver el último cuadro del anterior hasta que el nuevo pinta.
        /// T97: una toma puede ser imagen fija (se compone en el lienzo y dura su narración + pausa, o 3 s); la siguiente entra con un fundido corto.
        /// Devuelve true cuando ya no queda nada que ver (fin del contenido).
        /// </summary>
        private bool AvanzarTomas()
        {
            if (enPendiente || clips == null || clips.Count == 0 || !videoIniciado)
            {
                return false;
            }
            int otro = 1 - activo;
            float dt = Time.unscaledDeltaTime;
            bool tomaFoto = enFoto && !cambiando;
            if (tomaFoto)
            {
                // T97: la imagen fija corre su tiempo (narración + pausa, o 3 s) y se dibuja con su Ken Burns y el fundido de entrada.
                reglaFoto.Avanzar(dt);
                reglaClip?.Avanzar(dt, true);
                DibujarFoto();
            }
            else
            {
                reglaClip?.Avanzar(dt, acabo[reproductorClip]);
            }
            if (cambiando)
            {
                // El nuevo ya pintó su primer cuadro: se cambia la imagen y el viejo prepara el que sigue.
                if (reproductores[otro].frame > 0 || acabo[otro])
                {
                    pantallaVideo.texture = texturas[otro];
                    if (reproductores[activo] != null)
                    {
                        reproductores[activo].Stop();
                    }
                    activo = otro;
                    cambiando = false;
                    enFoto = false;
                    PrecargarSiguiente();
                }
                return false;
            }
            // Con narración: el último cuadro se queda congelado hasta que acaba la frase y luego va la pausa (ReglaNarracion).
            bool listo = tomaFoto ? reglaFoto.Listo : acabo[activo] && (reglaClip == null || reglaClip.Listo);
            if (!listo)
            {
                return false;
            }
            if (clipActual + 1 < clips.Count)
            {
                clipActual++;
                if (clips[clipActual] == null)
                {
                    EmpezarFoto(clipActual);
                    return false;
                }
                reproductores[otro].Play();
                EmpezarClip(clipActual, otro);
                cambiando = true;
                return false;
            }
            // Se acabó el último clip: la parte pendiente si la hay; si no, termina el contenido.
            if (pedido.Datos.SegundosPendientes > 0.05f)
            {
                enPendiente = true;
                enFoto = false;
                pendienteInicio = Time.unscaledTime;
                if (reproductores[activo] != null)
                {
                    reproductores[activo].Stop();
                }
                pantallaVideo.enabled = false;
                textoTitulo.enabled = true;
                textoLinea.text = pedido.Datos.TextoPendiente;
                textoLinea.enabled = true;
                barraFondo.enabled = true;
                barraRelleno.enabled = true;
                return false;
            }
            return true;
        }

        /// <summary>T-cinematicas-narracion-24nov: el clip <paramref name="indice"/> acaba de empezar en el reproductor <paramref name="r"/>: arranca su frase.</summary>
        private void EmpezarClip(int indice, int r)
        {
            reproductorClip = r;
            AudioClip voz = pedido.Datos.Narracion(indice);
            reglaClip = new ReglaNarracion(voz != null ? voz.length : 0f, pedido.Datos.Pausa(indice));
            if (narrador == null)
            {
                return;
            }
            narrador.Stop();
            narrador.clip = voz;
            narrador.volume = 1f;
            if (voz != null)
            {
                narrador.Play();
            }
        }

        /// <summary>
        /// T97: empieza la toma <paramref name="indice"/>, que es una imagen fija: guarda lo que se veía (para el fundido cruzado), pone el lienzo
        /// en pantalla, arranca su frase y su tiempo (<see cref="ReglaFotoCinematica"/>) y prepara el video que siga, si sigue uno.
        /// </summary>
        private void EmpezarFoto(int indice)
        {
            AsegurarLienzos();
            Texture antes = pantallaVideo != null && pantallaVideo.enabled ? pantallaVideo.texture : null;
            hayAnterior = antes != null;
            if (hayAnterior)
            {
                Graphics.Blit(antes, lienzoAnterior);
            }
            if (hayVideos && reproductores[activo] != null)
            {
                reproductores[activo].Stop();
            }
            fotoActual = pedido.Datos.Foto(indice);
            EmpezarClip(indice, activo);
            AudioClip voz = pedido.Datos.Narracion(indice);
            reglaFoto = new ReglaFotoCinematica(ReglaFotoCinematica.Duracion(voz != null ? voz.length : 0f, pedido.Datos.Pausa(indice)));
            enFoto = true;
            cambiando = false;
            DibujarFoto();
            pantallaVideo.texture = lienzo;
            pantallaVideo.enabled = true;
            if (hayVideos && indice > 0)
            {
                PrecargarSiguiente();
            }
        }

        private void AsegurarLienzos()
        {
            if (lienzo == null)
            {
                lienzo = new RenderTexture(LienzoFotoCinematica.Ancho, LienzoFotoCinematica.Alto, 0) { name = "LienzoCinematica" };
                lienzo.Create();
            }
            if (lienzoAnterior == null)
            {
                lienzoAnterior = new RenderTexture(LienzoFotoCinematica.Ancho, LienzoFotoCinematica.Alto, 0) { name = "LienzoCinematicaAnterior" };
                lienzoAnterior.Create();
            }
        }

        // Compone la imagen fija en el lienzo: la toma anterior abajo y esta encima con su fundido de entrada y su encuadre Ken Burns.
        private void DibujarFoto()
        {
            if (lienzo == null || reglaFoto == null)
            {
                return;
            }
            float aspecto = fotoActual != null && fotoActual.height > 0 ? (float)fotoActual.width / fotoActual.height : LienzoFotoCinematica.Aspecto;
            Rect uv = ReglaFotoCinematica.Encuadre(reglaFoto.Avance, clipActual, aspecto, LienzoFotoCinematica.Aspecto);
            LienzoFotoCinematica.Componer(lienzo, hayAnterior ? lienzoAnterior : null, fotoActual, uv, reglaFoto.AlfaEntrada);
        }

        /// <summary>
        /// T97/VR: el lienzo con la imagen fija que está en pantalla (null si no hay cinemática, la toma es video o ya no está en el contenido).
        /// En VR <c>VideosCineVR</c> lo pone en la pantalla grande.
        /// </summary>
        public static RenderTexture TexturaFija => EnCurso && instancia.enFoto && instancia.lienzo != null && instancia.regla != null
            && instancia.regla.Actual == ReglaCinematica.Fase.Contenido ? instancia.lienzo : null;

        /// <summary>T97: true si la toma en pantalla es una imagen fija (para pruebas).</summary>
        public static bool EnImagenFija => EnCurso && instancia.enFoto;

        /// <summary>
        /// Baja los efectos del video (~-6 dB) y avisa a la música (<see cref="DuckingNarracion"/>) mientras suena la frase; al saltar, la frase
        /// se apaga con un fundido de <see cref="ReglaNarracion.SegundosFundidoVoz"/> s.
        /// </summary>
        private void AplicarNarracion(float dt)
        {
            bool sonando = reglaClip != null && reglaClip.VozSonando && !fundiendoVoz && regla.Actual == ReglaCinematica.Fase.Contenido;
            DuckingNarracion.Activo = sonando;
            float volumenVideo = sonando ? ReglaNarracion.VolumenVideoConVoz : 1f;
            for (int i = 0; i < 2; i++)
            {
                if (fuentesVideo[i] != null)
                {
                    fuentesVideo[i].volume = volumenVideo;
                }
            }
            if (fundiendoVoz && narrador != null)
            {
                narrador.volume = Mathf.Max(0f, narrador.volume - dt / ReglaNarracion.SegundosFundidoVoz);
                if (narrador.volume <= 0f)
                {
                    narrador.Stop();
                    fundiendoVoz = false;
                }
            }
        }

        /// <summary>
        /// V12/VR: true si el VideoPlayer es el de la cinemática en pantalla y su cuadro sigue a la vista aunque ya no corra (congelado mientras
        /// acaba la frase, o en la pausa). La pantalla de cine de VR lo deja puesto mientras tanto.
        /// </summary>
        public static bool SosteniendoCuadro(VideoPlayer v)
        {
            if (v == null || !EnCurso || instancia.regla == null || instancia.regla.Actual != ReglaCinematica.Fase.Contenido)
            {
                return false;
            }
            CinematicaPendiente c = instancia;
            // T97: con una imagen fija en pantalla ningún video sostiene su cuadro (la pantalla de cine muestra el lienzo, ver TexturaFija).
            return c.videoIniciado && !c.enPendiente && !c.cambiando && !c.enFoto && c.clips != null && c.clips.Count > 0 && c.reproductores[c.activo] == v;
        }

        /// <summary>True si el cuadro del clip en pantalla está congelado esperando a que acabe su frase (para pruebas).</summary>
        public static bool CuadroCongelado => EnCurso && instancia.reglaClip != null && instancia.reglaClip.Congelado;

        /// <summary>True si la frase del clip en pantalla está sonando (para pruebas).</summary>
        public static bool NarracionSonando => EnCurso && instancia.reglaClip != null && instancia.reglaClip.VozSonando && !instancia.fundiendoVoz;

        /// <summary>El AudioSource de la narración de la cinemática en pantalla (null si ninguna o sin videos). Para pruebas.</summary>
        public static AudioSource FuenteNarracion => EnCurso ? instancia.narrador : null;

        /// <summary>Índice del clip en pantalla (para pruebas).</summary>
        public static int ClipActual => EnCurso && instancia.clips != null ? instancia.clipActual : -1;

        /// <summary>True si la cinemática en pantalla está en su parte pendiente (sin video, tras los clips).</summary>
        public static bool EnPartePendiente => EnCurso && instancia.enPendiente;

        /// <summary>0 a 1 de la barra: la de la parte pendiente si se está en ella; si no, la de la regla.</summary>
        private float ProgresoBarra()
        {
            if (!enPendiente)
            {
                return regla.Progreso;
            }
            float seg = pedido.Datos.SegundosPendientes;
            return seg <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - pendienteInicio) / seg);
        }

        private void LimpiarPantalla()
        {
            for (int i = 0; i < 2; i++)
            {
                if (reproductores[i] != null)
                {
                    reproductores[i].Stop();
                    Destroy(reproductores[i].gameObject);
                    reproductores[i] = null;
                }
                if (texturas[i] != null)
                {
                    texturas[i].Release();
                    Destroy(texturas[i]);
                    texturas[i] = null;
                }
            }
            if (narrador != null)
            {
                narrador.Stop();
                Destroy(narrador.gameObject);
                narrador = null;
            }
            fuentesVideo[0] = null;
            fuentesVideo[1] = null;
            reglaClip = null;
            fundiendoVoz = false;
            if (canvasOmitir != null)
            {
                Destroy(canvasOmitir.gameObject);
                canvasOmitir = null;
                grupoOmitir = null;
                rectOmitir = null;
            }
            clips = null;
            enPendiente = false;
            cambiando = false;
            videoIniciado = false;
            enFoto = false;
            hayAnterior = false;
            hayVideos = false;
            fotoActual = null;
            reglaFoto = null;
            LiberarLienzo(ref lienzo);
            LiberarLienzo(ref lienzoAnterior);
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
        }

        private static void LiberarLienzo(ref RenderTexture textura)
        {
            if (textura == null)
            {
                return;
            }
            textura.Release();
            Destroy(textura);
            textura = null;
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
            if (Time.unscaledTime - inicio >= SegundosSinSaltar && ClicEnOmitir())
            {
                saltar = true;
            }
            saltarPedido = false;
            if (AvanzarTomas())
            {
                saltar = true; // el último clip acabó: se sale como al saltar
            }
            if (enPendiente && Time.unscaledTime - pendienteInicio >= pedido.Datos.SegundosPendientes)
            {
                saltar = true;
            }
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
            AplicarNarracion(Time.unscaledDeltaTime);
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
                        EmpezarClip(0, activo);
                        videoIniciado = true;
                        pantallaVideo.texture = texturas[activo];
                        pantallaVideo.enabled = true;
                    }
                    else if (clips != null && clips.Count > 0 && clips[0] == null)
                    {
                        // T97: la primera toma es una imagen fija: entra con su fundido desde el negro.
                        videoIniciado = true;
                        EmpezarFoto(0);
                    }
                    break;
                case ReglaCinematica.Fase.FundidoSalida:
                    for (int i = 0; i < 2; i++)
                    {
                        if (reproductores[i] != null)
                        {
                            reproductores[i].Stop();
                        }
                    }
                    if (pantallaVideo != null)
                    {
                        pantallaVideo.enabled = false;
                    }
                    textoTitulo.enabled = false;
                    textoLinea.enabled = false;
                    barraFondo.enabled = false;
                    barraRelleno.enabled = false;
                    textoSaltar.enabled = false;
                    if (canvasOmitir != null)
                    {
                        canvasOmitir.enabled = false;
                    }
                    // Saltar corta la frase con un fundido corto (nunca de golpe); la música ya puede volver.
                    if (narrador != null && narrador.isPlaying)
                    {
                        fundiendoVoz = true;
                    }
                    DuckingNarracion.Activo = false;
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
            if (grupoOmitir != null)
            {
                grupoOmitir.alpha = alfa;
            }
            if (barraRelleno != null)
            {
                barraRelleno.rectTransform.anchorMax = new Vector2(ProgresoBarra(), 1f);
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
