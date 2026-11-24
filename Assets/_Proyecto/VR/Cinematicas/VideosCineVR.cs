using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MechaCyberpunk.VR.Cinematicas
{
    /// <summary>
    /// V12-B: los videos de cinemática en VR se ven en <see cref="PantallaCineVR"/> (VR15: la pantalla grande curva, ~105° a 3.5 m, quieta frente a la cabeza), con su audio de siempre.
    /// <list type="bullet">
    /// <item>Un VideoPlayer que dibuja sobre una cámara (plano cercano o lejano) se pasaría pegado a la cara: se manda a una RenderTexture propia
    /// y se le devuelve su modo al apagarse.</item>
    /// <item>Un VideoPlayer que dibuja en una RenderTexture que muestra un RawImage de un Canvas de pantalla (la cinemática con video de
    /// CinematicaPendiente) sale en la pantalla de cine mientras se reproduce, y ese Canvas se apaga mientras tanto: si no, su negro y su
    /// video (pegados a la cabeza) taparían la pantalla. Al parar el video el Canvas se vuelve a prender (el fundido de salida sigue igual).</item>
    /// </list>
    /// Solo con <see cref="ModoVR.Activo"/> y el jugador VR armado: el video del logo de Inicio (rig de menús) lo maneja Inicio.
    /// Los videos de una textura que no sale en pantalla (pantallas del mundo) no se tocan, ni los de <see cref="Excluir"/>.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class VideosCineVR : MonoBehaviour
    {
        /// <summary>Prioridad en <see cref="PantallaCineVR"/>: un video de cinemática va sobre las cámaras de cinemática.</summary>
        public const int Prioridad = 100;
        private const int AnchoRespaldo = 1280;
        private const int AltoRespaldo = 720;
        private const float SegundosBarrido = 0.25f;

        private sealed class Seguido
        {
            public VideoPlayer Video;
            public bool Redirigido;
            public VideoRenderMode ModoPrevio;
            public RenderTexture TexturaPrevia;
            public RenderTexture Propia;
            public Texture Evaluada;
            public bool EnPantalla;
            public bool Mostrando;
            public readonly List<Canvas> Fuentes = new List<Canvas>();
            public readonly List<Canvas> Apagados = new List<Canvas>();
        }

        private static readonly HashSet<VideoPlayer> excluidos = new HashSet<VideoPlayer>();

        /// <summary>El de la sesión (null hasta que arranca).</summary>
        public static VideosCineVR Actual { get; private set; }

        private readonly List<Seguido> seguidos = new List<Seguido>();
        private float barrido;

        /// <summary>Cuántos videos se ven ahora en la pantalla de cine.</summary>
        public int Mostrando
        {
            get
            {
                int n = 0;
                foreach (Seguido s in seguidos)
                {
                    if (s.Mostrando)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            excluidos.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            Asegurar();
        }

        /// <summary>Crea el componente si no hay (no hace nada sin VR: solo espera).</summary>
        public static VideosCineVR Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            var objeto = new GameObject("VideosCineVR");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<VideosCineVR>();
        }

        /// <summary>Que este video nunca vaya a la pantalla de cine.</summary>
        public static void Excluir(VideoPlayer video)
        {
            if (video != null)
            {
                excluidos.Add(video);
            }
        }

        public static void QuitarExclusion(VideoPlayer video)
        {
            excluidos.Remove(video);
        }

        /// <summary>True si el video se ve ahora en la pantalla de cine.</summary>
        public static bool EnCine(VideoPlayer video)
        {
            Seguido s = Actual != null ? Actual.Buscar(video) : null;
            return s != null && s.Mostrando;
        }

        /// <summary>True si el video se pasó de una cámara a una textura propia.</summary>
        public static bool Redirigido(VideoPlayer video)
        {
            Seguido s = Actual != null ? Actual.Buscar(video) : null;
            return s != null && s.Redirigido;
        }

        /// <summary>Busca los VideoPlayer nuevos ya (las pruebas y quien acabe de crear uno no esperan al barrido).</summary>
        public void BarrerAhora()
        {
            barrido = 0f;
            Barrer();
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(gameObject);
                return;
            }
            Actual = this;
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                SoltarTodos();
                Actual = null;
            }
        }

        private void LateUpdate()
        {
            if (!ModoVR.Activo)
            {
                if (seguidos.Count > 0)
                {
                    SoltarTodos();
                    seguidos.Clear();
                }
                return;
            }
            barrido -= Time.unscaledDeltaTime;
            if (barrido <= 0f)
            {
                barrido = SegundosBarrido;
                Barrer();
            }
            AdaptadorVR a = AdaptadorVR.Actual;
            bool hayJugador = a != null && a.Armado;
            for (int i = seguidos.Count - 1; i >= 0; i--)
            {
                Seguido s = seguidos[i];
                if (s.Video == null)
                {
                    Soltar(s);
                    seguidos.RemoveAt(i);
                    continue;
                }
                Actualizar(s, hayJugador);
            }
        }

        private void Actualizar(Seguido s, bool hayJugador)
        {
            VideoPlayer v = s.Video;
            bool excluido = excluidos.Contains(v);
            if (!hayJugador || excluido || !v.isActiveAndEnabled)
            {
                Soltar(s);
                return;
            }
            if (!s.Redirigido && ReglaCineVR.DibujaEnCamara(v.renderMode))
            {
                Redirigir(s);
            }
            if (s.Redirigido && (v.renderMode != VideoRenderMode.RenderTexture || v.targetTexture != s.Propia))
            {
                // El dueño cambió el modo o la textura: ya no es nuestro.
                s.Redirigido = false;
                LiberarPropia(s);
            }
            Texture textura = v.renderMode == VideoRenderMode.RenderTexture ? v.targetTexture : null;
            // Con la lista de clips de CinematicaPendiente hay dos VideoPlayer alternados: el RawImage pasa a mostrar la textura del segundo
            // cuando le toca, así que mientras suena y no estaba en pantalla se vuelve a buscar.
            if (!s.Redirigido && (textura != s.Evaluada || (!s.EnPantalla && v.isPlaying)))
            {
                s.Evaluada = textura;
                s.EnPantalla = textura != null && BuscarFuentes(textura, s.Fuentes);
            }
            bool alCine = ReglaCineVR.VideoAlCine(hayJugador, excluido, v.isPlaying, s.Redirigido, s.EnPantalla);
            if (alCine && textura != null)
            {
                if (!s.Mostrando || !PantallaCineVR.Tiene(v))
                {
                    s.Mostrando = true;
                    PantallaCineVR.Mostrar(v, textura, Aspecto(v, textura), Prioridad);
                    Debug.Log("[VR] Video " + v.name + ": a la pantalla de cine.");
                }
                ApagarFuentes(s);
            }
            else if (s.Mostrando)
            {
                QuitarDeLaPantalla(s);
            }
        }

        private void Barrer()
        {
            foreach (VideoPlayer v in FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None))
            {
                if (v == null)
                {
                    continue;
                }
                Seguido s = Buscar(v);
                if (s == null)
                {
                    seguidos.Add(new Seguido { Video = v });
                }
                else if (!s.EnPantalla && !s.Redirigido)
                {
                    s.Evaluada = null; // el RawImage pudo crearse después del video: se vuelve a buscar
                }
            }
        }

        // Los Canvas raíz de pantalla (o ya pasados a World Space por MenusVR) cuyos RawImage muestran la textura.
        private static bool BuscarFuentes(Texture textura, List<Canvas> fuentes)
        {
            fuentes.Clear();
            MenusVR menus = MenusVR.Actual;
            foreach (RawImage imagen in FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (imagen == null || imagen.texture != textura)
                {
                    continue;
                }
                Canvas lienzo = imagen.GetComponentInParent<Canvas>(true);
                Canvas raiz = lienzo != null ? lienzo.rootCanvas : null;
                if (raiz == null || fuentes.Contains(raiz))
                {
                    continue;
                }
                bool dePantalla = raiz.renderMode != RenderMode.WorldSpace || (menus != null && menus.TipoDe(raiz) != null);
                if (dePantalla)
                {
                    fuentes.Add(raiz);
                }
            }
            return fuentes.Count > 0;
        }

        private static float Aspecto(VideoPlayer v, Texture textura)
        {
            if (v.clip != null && v.clip.height > 0)
            {
                return (float)v.clip.width / v.clip.height;
            }
            return textura.height > 0 ? (float)textura.width / textura.height : (float)AnchoRespaldo / AltoRespaldo;
        }

        private void Redirigir(Seguido s)
        {
            VideoPlayer v = s.Video;
            int ancho = v.clip != null && v.clip.width > 0 ? (int)v.clip.width : AnchoRespaldo;
            int alto = v.clip != null && v.clip.height > 0 ? (int)v.clip.height : AltoRespaldo;
            s.ModoPrevio = v.renderMode;
            s.TexturaPrevia = v.targetTexture;
            s.Propia = new RenderTexture(ancho, alto, 0) { name = "CineVR_" + v.name };
            s.Propia.Create();
            v.targetTexture = s.Propia;
            v.renderMode = VideoRenderMode.RenderTexture;
            s.Redirigido = true;
            Debug.Log("[VR] Video " + v.name + " dibujaba en la cámara: va a una textura (no pegado a la cara).");
        }

        private static void ApagarFuentes(Seguido s)
        {
            foreach (Canvas c in s.Fuentes)
            {
                if (c != null && c.enabled)
                {
                    c.enabled = false;
                    if (!s.Apagados.Contains(c))
                    {
                        s.Apagados.Add(c);
                    }
                }
            }
        }

        private static void PrenderFuentes(Seguido s)
        {
            foreach (Canvas c in s.Apagados)
            {
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            s.Apagados.Clear();
        }

        private static void QuitarDeLaPantalla(Seguido s)
        {
            s.Mostrando = false;
            PantallaCineVR.Ocultar(s.Video);
            PrenderFuentes(s);
        }

        // Lo deja como estaba: fuera de la pantalla, sus Canvas prendidos y, si se redirigió, su modo de antes.
        private void Soltar(Seguido s)
        {
            if (s.Mostrando)
            {
                QuitarDeLaPantalla(s);
            }
            if (s.Redirigido)
            {
                s.Redirigido = false;
                if (s.Video != null && s.Video.targetTexture == s.Propia)
                {
                    s.Video.renderMode = s.ModoPrevio;
                    s.Video.targetTexture = s.TexturaPrevia;
                }
                LiberarPropia(s);
            }
        }

        private void LiberarPropia(Seguido s)
        {
            if (s.Propia != null)
            {
                if (s.Video != null && s.Video.targetTexture == s.Propia)
                {
                    s.Video.targetTexture = s.TexturaPrevia;
                }
                s.Propia.Release();
                Destroy(s.Propia);
                s.Propia = null;
            }
            s.Evaluada = null;
        }

        private void SoltarTodos()
        {
            foreach (Seguido s in seguidos)
            {
                Soltar(s);
            }
        }

        private Seguido Buscar(VideoPlayer video)
        {
            if (video == null)
            {
                return null;
            }
            foreach (Seguido s in seguidos)
            {
                if (s.Video == video)
                {
                    return s;
                }
            }
            return null;
        }
    }
}
