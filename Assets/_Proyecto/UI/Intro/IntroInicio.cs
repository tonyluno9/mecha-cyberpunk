using System;
using System.Collections;
using MechaCyberpunk.UI.Pruebas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MechaCyberpunk.UI.Intro
{
    /// <summary>
    /// La intro del juego: el video del logo a pantalla completa y con su proporción (RenderTexture en un RawImage con AspectRatioFitter,
    /// fondo negro). Se salta con Enter, clic o Escape. Va en el mismo objeto que <see cref="Inicio.MenuInicio"/>, que espera a
    /// <see cref="Terminada"/> para sonar el sonido del menú, entrar la música y mostrar el menú. Sin video asignado, en el editor (salvo
    /// <see cref="ReproducirEnEditor"/>) o en una prueba de build (<c>-pruebaInicio</c>) no hace nada: <see cref="DebeReproducir"/> es false.
    /// </summary>
    public class IntroInicio : MonoBehaviour
    {
        public const int OrdenCanvas = 1000;
        private const float SegundosPrepararPorDefecto = 8f;

        /// <summary>El editor se salta la intro (para no estorbar a las pruebas); poner true para verla jugando en el editor.</summary>
        public static bool ReproducirEnEditor;

        /// <summary>Pruebas: la intro corre aunque no haya video (pantalla negra hasta saltarla), para probar el flujo.</summary>
        public static bool SimularSinVideo;

        [SerializeField] private ConfigIntro config;

        private ReglaIntro regla;
        private VideoPlayer reproductor;
        private RenderTexture textura;
        private GameObject raiz;
        private bool avisado;
        private bool cerrando;

        public event Action Terminada;

        public FaseIntro Fase => regla != null ? regla.Fase : FaseIntro.Inactiva;
        public bool Saltada => regla != null && regla.Saltada;
        public ConfigIntro Config => config;
        /// <summary>V12: la textura donde se dibuja el video mientras suena (null sin video o ya cerrada). En VR se ve en la pantalla grande.</summary>
        public RenderTexture Textura => textura;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ReproducirEnEditor = false;
            SimularSinVideo = false;
        }

        /// <summary>Pruebas y herramientas de editor: pone la configuración por código.</summary>
        public void Configurar(ConfigIntro nueva) => config = nueva;

        public bool DebeReproducir
        {
            get
            {
                if (Omitida())
                {
                    return false;
                }
                return SimularSinVideo || (config != null && config.VideoLogo != null);
            }
        }

        private static bool Omitida()
        {
            if (Application.isEditor && !ReproducirEnEditor)
            {
                return true;
            }
            foreach (string a in Environment.GetCommandLineArgs())
            {
                if (a == PruebaInicioBuild.Bandera)
                {
                    return true;
                }
            }
            return false;
        }

        public void Iniciar()
        {
            float espera = config != null ? config.SegundosMaximoPreparar : SegundosPrepararPorDefecto;
            regla = new ReglaIntro(espera);
            bool hayVideo = config != null && config.VideoLogo != null;
            regla.Iniciar(hayVideo || SimularSinVideo);
            cerrando = false;
            avisado = false;
            if (regla.Fase == FaseIntro.Terminada)
            {
                cerrando = true;
                StartCoroutine(Avisar());
                return;
            }
            ArmarPantalla(hayVideo);
        }

        /// <summary>Salta la intro (lo mismo que Enter, clic o Escape).</summary>
        public void Saltar()
        {
            if (regla != null)
            {
                regla.Saltar();
            }
        }

        private void ArmarPantalla(bool hayVideo)
        {
            raiz = new GameObject("IntroInicio_Pantalla", typeof(RectTransform));
            raiz.transform.SetParent(transform, false);
            Canvas canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OrdenCanvas;
            raiz.AddComponent<CanvasScaler>();
            raiz.AddComponent<GraphicRaycaster>();

            Image fondo = NuevoHijo<Image>(raiz.transform, "Fondo");
            fondo.color = config != null ? config.ColorFondo : Color.black;
            Estirar(fondo.rectTransform);

            if (!hayVideo)
            {
                return;
            }
            VideoClip clip = config.VideoLogo;
            textura = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "IntroInicio_Video" };
            RawImage imagen = NuevoHijo<RawImage>(raiz.transform, "Video");
            imagen.texture = textura;
            imagen.raycastTarget = false;
            Estirar(imagen.rectTransform);
            AspectRatioFitter ajuste = imagen.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;

            reproductor = gameObject.AddComponent<VideoPlayer>();
            reproductor.playOnAwake = false;
            reproductor.source = VideoSource.VideoClip;
            reproductor.clip = clip;
            reproductor.renderMode = VideoRenderMode.RenderTexture;
            reproductor.targetTexture = textura;
            reproductor.isLooping = false;
            reproductor.audioOutputMode = VideoAudioOutputMode.Direct;
            reproductor.skipOnDrop = true;
            reproductor.loopPointReached += AlLlegarAlFinal;
            reproductor.errorReceived += AlFallar;
            reproductor.Play();
        }

        private static T NuevoHijo<T>(Transform padre, string nombre) where T : Graphic
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(padre, false);
            return go.AddComponent<T>();
        }

        private static void Estirar(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private void AlLlegarAlFinal(VideoPlayer v) => regla.AlAcabarVideo();

        private void AlFallar(VideoPlayer v, string mensaje)
        {
            Debug.LogWarning("[UI] Intro: el video falló (" + mensaje + "); se salta.");
            regla.AlAcabarVideo();
        }

        private void Update()
        {
            if (regla == null || cerrando)
            {
                return;
            }
            if (regla.Fase == FaseIntro.Video)
            {
                Keyboard teclado = Keyboard.current;
                Mouse raton = Mouse.current;
                bool pidio = (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame))
                    || (raton != null && raton.leftButton.wasPressedThisFrame);
                if (pidio)
                {
                    regla.Saltar();
                }
                regla.Actualizar(Time.unscaledDeltaTime, reproductor == null || reproductor.isPlaying);
            }
            if (regla.Fase == FaseIntro.Terminada)
            {
                cerrando = true;
                Cerrar();
                StartCoroutine(Avisar());
            }
        }

        private void Cerrar()
        {
            if (reproductor != null)
            {
                reproductor.loopPointReached -= AlLlegarAlFinal;
                reproductor.errorReceived -= AlFallar;
                reproductor.Stop();
                Destroy(reproductor);
                reproductor = null;
            }
            if (raiz != null)
            {
                Destroy(raiz);
            }
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
                textura = null;
            }
        }

        // Un cuadro después, para que el Enter o el clic que saltó la intro no toque también el menú.
        private IEnumerator Avisar()
        {
            yield return null;
            if (avisado)
            {
                yield break;
            }
            avisado = true;
            Terminada?.Invoke();
        }

        private void OnDestroy()
        {
            Cerrar();
        }
    }
}
