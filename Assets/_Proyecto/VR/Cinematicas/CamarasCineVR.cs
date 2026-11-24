using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.VR.Cinematicas
{
    /// <summary>
    /// V12-B: en VR la cabeza del jugador nunca la mueve una animación. Cada cuadro busca las cámaras de cinemática que se prenden
    /// (la silla del Doc, el asalto del prólogo, el flashback, o cualquiera que en PC tape la vista del jugador: ver
    /// <see cref="ReglaCineVR.EsCamaraDeCine"/>) y las manda a una RenderTexture que se ve en <see cref="PantallaCineVR"/> (VR15: la pantalla grande curva, ~105° a 3.5 m), quieta frente
    /// a la cabeza, con un fundido corto al empezar y al terminar (tapa el salto del jugador al asiento y de vuelta).
    /// Al apagarse o destruirse la cámara se quita de la pantalla y recupera su targetTexture (null).
    /// No toca: la cabeza del visor, la del rig de menús, la del láser (ya tiene textura), cámaras con textura (minimapa, retratos,
    /// hologramas, espejos), cámaras Overlay de URP, <see cref="CamaraTitulo"/>, la del recorrido de Menu_Calles ni las de <see cref="Excluir"/>.
    /// Sin jugador VR (rig de menús) solo actúa sobre las forzadas (la del <see cref="DirectorFlashback"/>).
    /// Solo actúa con <see cref="ModoVR.Activo"/>; se crea solo al arrancar y sobrevive a los cambios de escena.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class CamarasCineVR : MonoBehaviour
    {
        /// <summary>Tamaño de la textura de cada cámara de cinemática (px, 16:9; VR15: la pantalla grande mide ~105° de la vista).</summary>
        public const int AnchoTextura = 1920;
        public const int AltoTextura = 1080;
        /// <summary>Prioridad en <see cref="PantallaCineVR"/> (más la profundidad de la cámara: gana la que en PC queda encima). Los videos van más alto.</summary>
        public const int PrioridadBase = 10;
        /// <summary>Fundido a negro al empezar y al terminar una cámara de cinemática (s).</summary>
        public const float SegundosFundido = 0.4f;
        private const int BitsProfundidad = 24;
        private const float SegundosBarrido = 1f;

        private sealed class Redirigida
        {
            public Camera Camara;
            public RenderTexture Textura;
        }

        private static readonly HashSet<Camera> excluidas = new HashSet<Camera>();

        /// <summary>El de la sesión (null hasta que arranca).</summary>
        public static CamarasCineVR Actual { get; private set; }

        private readonly List<Redirigida> redirigidas = new List<Redirigida>();
        private readonly HashSet<Camera> forzadas = new HashSet<Camera>();
        // Lo que no cambia de una cámara (componentes): si por su tipo podría ser de cinemática. Se calcula una vez por cámara.
        private readonly Dictionary<Camera, bool> porTipo = new Dictionary<Camera, bool>();
        private readonly List<Camera> muertas = new List<Camera>();
        private Camera[] camaras = new Camera[16];
        private float barrido;

        /// <summary>Cuántas cámaras van ahora a la pantalla de cine.</summary>
        public int Redirigidas => redirigidas.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            excluidas.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            Asegurar();
        }

        /// <summary>Crea el componente si no hay (no hace nada sin VR: solo espera).</summary>
        public static CamarasCineVR Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            var objeto = new GameObject("CamarasCineVR");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<CamarasCineVR>();
        }

        /// <summary>Que esta cámara nunca vaya a la pantalla de cine (la maneja otro: Inicio, una vista previa...).</summary>
        public static void Excluir(Camera camara)
        {
            if (camara != null)
            {
                excluidas.Add(camara);
            }
        }

        public static void QuitarExclusion(Camera camara)
        {
            excluidas.Remove(camara);
        }

        /// <summary>True si la cámara se está dibujando en la pantalla de cine.</summary>
        public static bool EstaRedirigida(Camera camara)
        {
            return Actual != null && Actual.Buscar(camara) != null;
        }

        /// <summary>La textura en la que se dibuja la cámara (null si no está redirigida).</summary>
        public static RenderTexture TexturaDe(Camera camara)
        {
            Redirigida r = Actual != null ? Actual.Buscar(camara) : null;
            return r != null ? r.Textura : null;
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
                SoltarTodas();
                Actual = null;
            }
        }

        private void LateUpdate()
        {
            if (!ModoVR.Activo)
            {
                if (redirigidas.Count > 0)
                {
                    SoltarTodas();
                }
                return;
            }
            barrido -= Time.unscaledDeltaTime;
            if (barrido <= 0f)
            {
                barrido = SegundosBarrido;
                BuscarForzadas();
            }
            Revisar();
            BuscarNuevas();
        }

        // Las que ya van al cine: se sueltan si se apagaron, se destruyeron, ya no son de cinemática o su dueño les puso otra textura.
        private void Revisar()
        {
            for (int i = redirigidas.Count - 1; i >= 0; i--)
            {
                Redirigida r = redirigidas[i];
                if (r.Camara == null)
                {
                    redirigidas.RemoveAt(i);
                    Soltar(r, true);
                    continue;
                }
                if (r.Camara.targetTexture != r.Textura)
                {
                    // El dueño la mandó a otra textura: ya no es nuestra; no se le devuelve nada.
                    redirigidas.RemoveAt(i);
                    Soltar(r, false);
                    continue;
                }
                if (!r.Camara.isActiveAndEnabled || !EsDeCine(r.Camara, true))
                {
                    redirigidas.RemoveAt(i);
                    Soltar(r, true);
                    continue;
                }
                if (!PantallaCineVR.Tiene(r.Camara))
                {
                    PantallaCineVR.Mostrar(r.Camara, r.Textura, (float)AnchoTextura / AltoTextura, Prioridad(r.Camara));
                }
            }
        }

        private void BuscarNuevas()
        {
            int cantidad = Camera.allCamerasCount;
            if (camaras.Length < cantidad)
            {
                camaras = new Camera[cantidad * 2];
            }
            cantidad = Camera.GetAllCameras(camaras);
            for (int i = 0; i < cantidad; i++)
            {
                Camera c = camaras[i];
                camaras[i] = null;
                if (c != null && c.targetTexture == null && EsDeCine(c, false))
                {
                    Redirigir(c);
                }
            }
        }

        private bool EsDeCine(Camera c, bool yaRedirigida)
        {
            if (c == null || c.cameraType != CameraType.Game)
            {
                return false;
            }
            AdaptadorVR a = AdaptadorVR.Actual;
            bool hayJugador = a != null && a.Armado && a.Camara != null;
            Camera cabeza = hayJugador ? a.Camara : SistemaVR.CamaraVR;
            MenusVR menus = MenusVR.Actual;
            bool esCabeza = c == cabeza || c == SistemaVR.CamaraVR || (menus != null && c == menus.CamaraRigMenus);
            RecorridoCallesMenu recorrido = RecorridoCallesMenu.Actual;
            bool excluida = excluidas.Contains(c) || (recorrido != null && c == recorrido.Camara) || !PuedeSerPorTipo(c);
            bool tieneTextura = !yaRedirigida && c.targetTexture != null;
            return ReglaCineVR.EsCamaraDeCine(esCabeza, excluida, tieneTextura, true, hayJugador, forzadas.Contains(c),
                c.depth, cabeza != null ? cabeza.depth : 0f);
        }

        // Por sus componentes (una vez por cámara): ni la del título, ni una Overlay de URP, ni una que cuelga de los menús VR.
        private bool PuedeSerPorTipo(Camera c)
        {
            if (porTipo.TryGetValue(c, out bool puede))
            {
                return puede;
            }
            puede = c.GetComponent<CamaraTitulo>() == null;
            if (puede && c.TryGetComponent(out UniversalAdditionalCameraData datos) && datos.renderType == CameraRenderType.Overlay)
            {
                puede = false;
            }
            MenusVR menus = MenusVR.Actual;
            if (puede && menus != null && c.transform.IsChildOf(menus.transform))
            {
                puede = false;
            }
            porTipo[c] = puede;
            return puede;
        }

        private void BuscarForzadas()
        {
            forzadas.RemoveWhere(c => c == null);
            muertas.Clear();
            foreach (Camera c in porTipo.Keys)
            {
                if (c == null)
                {
                    muertas.Add(c);
                }
            }
            foreach (Camera c in muertas)
            {
                porTipo.Remove(c);
            }
            muertas.Clear();
            foreach (DirectorFlashback d in FindObjectsByType<DirectorFlashback>(FindObjectsSortMode.None))
            {
                if (d != null && d.Camara != null)
                {
                    forzadas.Add(d.Camara);
                }
            }
        }

        /// <summary>Para las pruebas y para quien arme una cinemática a mano: que esta cámara se trate como de cinemática aunque no haya jugador.</summary>
        public void Forzar(Camera camara)
        {
            if (camara != null)
            {
                forzadas.Add(camara);
            }
        }

        private void Redirigir(Camera c)
        {
            var textura = new RenderTexture(AnchoTextura, AltoTextura, BitsProfundidad) { name = "CineVR_" + c.name };
            textura.Create();
            c.targetTexture = textura;
            c.ResetAspect();
            SistemaVR.PermitirXR(c, false);
            redirigidas.Add(new Redirigida { Camara = c, Textura = textura });
            PantallaCineVR.Mostrar(c, textura, (float)AnchoTextura / AltoTextura, Prioridad(c));
            FundirJugador();
            Debug.Log("[VR] Cámara de cinemática " + c.name + ": a la pantalla de cine (no al visor).");
        }

        private static int Prioridad(Camera c)
        {
            return PrioridadBase + Mathf.RoundToInt(c.depth);
        }

        private void Soltar(Redirigida r, bool devolver)
        {
            PantallaCineVR.Ocultar(r.Camara);
            if (devolver && r.Camara != null && r.Camara.targetTexture == r.Textura)
            {
                r.Camara.targetTexture = null;
                r.Camara.ResetAspect();
            }
            if (r.Textura != null)
            {
                r.Textura.Release();
                Destroy(r.Textura);
                r.Textura = null;
            }
            if (ModoVR.Activo)
            {
                FundirJugador();
            }
        }

        private void SoltarTodas()
        {
            for (int i = redirigidas.Count - 1; i >= 0; i--)
            {
                Redirigida r = redirigidas[i];
                redirigidas.RemoveAt(i);
                Soltar(r, true);
            }
        }

        private Redirigida Buscar(Camera camara)
        {
            if (camara == null)
            {
                return null;
            }
            foreach (Redirigida r in redirigidas)
            {
                if (r.Camara == camara)
                {
                    return r;
                }
            }
            return null;
        }

        private static void FundirJugador()
        {
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null && a.Armado)
            {
                a.Fundir(SegundosFundido);
            }
        }
    }
}
