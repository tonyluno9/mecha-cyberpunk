using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Recorrido;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR.Cinematicas
{
    /// <summary>
    /// VR36: mientras dura una cinemática, en el visor todo alrededor de la pantalla grande es negro (un cine a oscuras): la cabeza deja de
    /// dibujar el juego (calles, NPC, manos, láser, HUD de casco) y solo dibuja la capa UI de Unity, donde van la pantalla grande
    /// (<see cref="PantallaCineVR"/>) y, mientras tanto, los paneles de MenusVR (la pantalla de texto de la cinemática, fundidos, avisos).
    /// El fondo de la cabeza es negro y su plano lejano se acerca a poco más que la pantalla (nada lejano se cuela).
    /// <list type="bullet">
    /// <item>Cinemática = <see cref="CinematicaPendiente.EnCurso"/>, una cámara de cinemática en la pantalla (<see cref="CamarasCineVR"/>) o un
    /// video de cinemática en ella (<see cref="VideosCineVR"/>).</item>
    /// <item>Entre una cinemática y la siguiente el negro se mantiene <see cref="SegundosRetencion"/> s: van seguidas, sin ver el juego en medio.</item>
    /// <item>Con la pausa abierta el negro se quita (los menús y el láser se ven como siempre).</item>
    /// </list>
    /// Al terminar, la cámara y las capas de los Canvas vuelven a como estaban. Solo con <see cref="ModoVR.Activo"/>; en PC no hace nada
    /// (la cinemática ya es pantalla completa).
    /// </summary>
    [DefaultExecutionOrder(10001)]
    public class NegroCineVR : MonoBehaviour
    {
        /// <summary>Cuánto sigue el negro después de la última cinemática (s): cubre el salto a la siguiente de la fila.</summary>
        public const float SegundosRetencion = 0.75f;
        /// <summary>Plano lejano de la cabeza en negro (m): un poco más allá de la pantalla grande.</summary>
        public const float LejosNegro = PantallaCineVR.Radio + 1.5f;
        private const float SegundosBarrido = 0.25f;

        /// <summary>El de la sesión (null hasta que arranca).</summary>
        public static NegroCineVR Actual { get; private set; }

        /// <summary>True mientras la cabeza está en negro.</summary>
        public static bool Activo => Actual != null && Actual.camara != null;

        /// <summary>Para las pruebas: fuerza el negro como si hubiera una cinemática.</summary>
        public static bool Forzado { get; set; }

        private Camera camara;
        private CameraClearFlags fondoPrevio;
        private Color colorPrevio;
        private int mascaraPrevia;
        private float lejosPrevio;
        private float retencion;
        private float barrido;
        private readonly Dictionary<GameObject, int> capasPrevias = new Dictionary<GameObject, int>();
        private readonly List<Canvas> temporal = new List<Canvas>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            Forzado = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            Asegurar();
        }

        /// <summary>Crea el componente si no hay (sin VR solo espera).</summary>
        public static NegroCineVR Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            var objeto = new GameObject("NegroCineVR");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<NegroCineVR>();
        }

        /// <summary>True si ahora hay una cinemática (sin contar la retención).</summary>
        public static bool HayCinematica()
        {
            if (Forzado || CinematicaPendiente.EnCurso)
            {
                return true;
            }
            CamarasCineVR camaras = CamarasCineVR.Actual;
            if (camaras != null && camaras.Redirigidas > 0)
            {
                return true;
            }
            VideosCineVR videos = VideosCineVR.Actual;
            return videos != null && videos.Mostrando > 0;
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
                Restaurar();
                Actual = null;
            }
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            bool cine = ModoVR.Activo && HayCinematica();
            retencion = cine ? SegundosRetencion : retencion - dt;
            Camera cabeza = SistemaVR.CamaraVR;
            bool aplicar = ModoVR.Activo && retencion > 0f && !Pausa.Activa && cabeza != null;
            if (!aplicar)
            {
                Restaurar();
                return;
            }
            if (camara != cabeza)
            {
                Restaurar();
                Guardar(cabeza);
            }
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            camara.cullingMask = 1 << PantallaCineVR.CapaUI;
            camara.farClipPlane = Mathf.Max(camara.nearClipPlane + 0.1f, LejosNegro);
            barrido -= dt;
            if (barrido <= 0f)
            {
                barrido = SegundosBarrido;
                PasarPanelesACapaUI();
            }
        }

        private void Guardar(Camera c)
        {
            camara = c;
            fondoPrevio = c.clearFlags;
            colorPrevio = c.backgroundColor;
            mascaraPrevia = c.cullingMask;
            lejosPrevio = c.farClipPlane;
            barrido = 0f;
            Debug.Log("[VR] Cinemática: alrededor de la pantalla grande todo en negro.");
        }

        // Los paneles de MenusVR (y sus Canvas anidados) se siguen viendo en negro: se pasan a la capa UI mientras dura.
        private void PasarPanelesACapaUI()
        {
            MenusVR menus = MenusVR.Actual;
            if (menus == null)
            {
                return;
            }
            temporal.Clear();
            menus.LienzosDePanel(temporal);
            int capa = PantallaCineVR.CapaUI;
            foreach (Canvas c in temporal)
            {
                GameObject objeto = c.gameObject;
                if (objeto.layer == capa || capasPrevias.ContainsKey(objeto))
                {
                    continue;
                }
                capasPrevias[objeto] = objeto.layer;
                objeto.layer = capa;
            }
            temporal.Clear();
        }

        private void Restaurar()
        {
            if (camara != null)
            {
                camara.clearFlags = fondoPrevio;
                camara.backgroundColor = colorPrevio;
                camara.cullingMask = mascaraPrevia;
                camara.farClipPlane = lejosPrevio;
                Debug.Log("[VR] Cinemática terminada: el juego se vuelve a ver.");
            }
            camara = null;
            foreach (KeyValuePair<GameObject, int> par in capasPrevias)
            {
                if (par.Key != null)
                {
                    par.Key.layer = par.Value;
                }
            }
            capasPrevias.Clear();
        }
    }
}
