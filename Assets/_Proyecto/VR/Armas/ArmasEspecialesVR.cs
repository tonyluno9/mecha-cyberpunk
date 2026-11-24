using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR5: las armas especiales en VR, con la misma lógica, daño, sonidos y efectos que con el ratón (<see cref="ArmasEspecialesAPie"/>):
    /// - Lanzagranadas y rifle de plasma: van en la mano derecha (<see cref="PistolaVR"/>) y el gatillo derecho dispara desde su cañón, hacia donde
    ///   apunta el control. El plasma carga con el gatillo sostenido y suelta el rayo, que sigue al cañón; la línea de mira sale del cañón.
    /// - Granada: el grip derecho la toma (aparece en la mano) y al soltarlo sale desde la mano con su velocidad × <see cref="FactorLanzamiento"/>.
    ///   Con la granada activa el grip derecho no agarra enemigos (<see cref="AgarreImplanteVR"/>). Si se acabaron, el grip empieza la recarga.
    /// Corre antes que PistolaVR (le dice si ocultar la granada) y antes que ArmasEspecialesAPie (que dispara en su LateUpdate). Lo agrega
    /// <see cref="ArranqueCombateVR"/>.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class ArmasEspecialesVR : MonoBehaviour
    {
        /// <summary>La granada sale con la velocidad de la mano por esto (un lanzamiento sin peso en la mano se siente corto).</summary>
        public const float FactorLanzamiento = 1.5f;
        /// <summary>Tope de la velocidad de la granada (m/s), por si el rastreo del control salta.</summary>
        public const float VelocidadMaximaLanzamiento = 30f;

        private const float AmplitudTomar = 0.3f;
        private const float SegundosTomar = 0.05f;
        private const float AmplitudLanzar = 0.6f;
        private const float SegundosLanzar = 0.08f;
        private const float SegundosReintentoLanzar = 1f;
        private float reintentarHasta = -1f;
        private Vector3 velocidadAlSoltar;
        private const float AmplitudTiro = 0.7f;
        private const float SegundosTiro = 0.1f;
        private const float AmplitudRayo = 0.25f;
        private const float SegundosRayo = 0.05f;

        private AdaptadorVR adaptador;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private ArmasEspecialesAPie especiales;
        private PistolaVR pistolaVR;
        private bool bolaVisible;

        /// <summary>True mientras el grip derecho sostiene una granada.</summary>
        public bool GranadaTomada { get; private set; }

        /// <summary>Granadas lanzadas con la mano.</summary>
        public int Lanzadas { get; private set; }

        /// <summary>Velocidad de la mano (m/s) al soltar la última granada.</summary>
        public Vector3 UltimaVelocidadMano { get; private set; }

        /// <summary>Velocidad con que salió la última granada (m/s).</summary>
        public Vector3 UltimaVelocidadGranada { get; private set; }

        /// <summary>Desde dónde salió la última granada.</summary>
        public Vector3 UltimoOrigenGranada { get; private set; }

        /// <summary>True si con esa arma el grip derecho es de la granada y no agarra enemigos.</summary>
        public static bool TomaElGrip(ArmaAPie arma) => arma == ArmaAPie.Granada;

        private static bool UsaGatillo(ArmaAPie arma) => arma == ArmaAPie.Lanzagranadas || arma == ArmaAPie.RiflePlasma;

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            especiales = GetComponent<ArmasEspecialesAPie>();
            pistolaVR = GetComponent<PistolaVR>();
            if (adaptador == null || pistola == null || armas == null || especiales == null || pistolaVR == null)
            {
                Debug.LogWarning("[VR] ArmasEspecialesVR sin AdaptadorVR, PistolaAPie, ArmasAPie, ArmasEspecialesAPie o PistolaVR en " + name + ".", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo += AlDisparar;
            }
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo -= AlDisparar;
            }
            GranadaTomada = false;
            if (pistolaVR != null)
            {
                pistolaVR.OcultarArma = false;
            }
            if (especiales != null)
            {
                especiales.BocaExterna = null;
                especiales.GatilloExterno = false;
            }
        }

        private void LateUpdate()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            if (!adaptador.Armado || !pistolaVR.isActiveAndEnabled)
            {
                return;
            }
            MostrarBola();
            ArmaAPie activa = armas.ArmaActiva;
            bool bloqueado = MenusVR.BloqueaDisparo || Pausa.Activa;
            if (TomaElGrip(activa))
            {
                LeerGrip(bloqueado);
            }
            else
            {
                GranadaTomada = false; // un cambio de arma con la granada tomada la guarda sin lanzarla
            }
            pistolaVR.OcultarArma = TomaElGrip(activa) && !GranadaTomada;
            pistolaVR.Actualizar();
            especiales.BocaExterna = pistolaVR.Boca;
            especiales.GatilloExterno = !bloqueado && UsaGatillo(activa) && EntradaVR.Sostenido(EntradaVR.GatilloDerecho);
            if (activa == ArmaAPie.RiflePlasma && especiales.EstadoPlasma == EstadoPlasma.Rayo)
            {
                VibrarDerecha(AmplitudRayo, SegundosRayo);
            }
        }

        private void LeerGrip(bool bloqueado)
        {
            if (!GranadaTomada && !bloqueado && EntradaVR.Pulso(EntradaVR.GripDerecho))
            {
                Tomar();
            }
            else if (GranadaTomada && EntradaVR.Solto(EntradaVR.GripDerecho))
            {
                Lanzar();
            }
            else if (GranadaTomada && Time.time < reintentarHasta && !EntradaVR.Sostenido(EntradaVR.GripDerecho))
            {
                // Se soltó antes de que pasara el tiempo entre granadas: sale en cuanto se pueda, con la velocidad del momento de soltar.
                Lanzar(velocidadAlSoltar);
            }
        }

        /// <summary>Toma una granada con la mano derecha si hay en el cargador. Sin granadas, empieza la recarga. Devuelve si quedó en la mano.</summary>
        public bool Tomar()
        {
            CargadorPistola cargador = pistola.Cargador;
            if (!TomaElGrip(armas.ArmaActiva) || cargador == null || cargador.Recargando)
            {
                return false;
            }
            if (cargador.Balas <= 0)
            {
                // Vacío: el intento solo avisa ClicVacio, y PistolaAPie empieza la recarga con su sonido (como el clic con el ratón).
                cargador.IntentarDisparar();
                return false;
            }
            GranadaTomada = true;
            VibrarDerecha(AmplitudTomar, SegundosTomar);
            return true;
        }

        /// <summary>Suelta la granada tomada desde la mano derecha, con la velocidad de la mano. Devuelve si salió.</summary>
        public bool Lanzar()
        {
            ManoVR m = adaptador.ManoDerecha;
            reintentarHasta = Time.time + SegundosReintentoLanzar;
            return Lanzar(m != null ? m.Velocidad : Vector3.zero);
        }

        private bool Lanzar(Vector3 velocidadMano)
        {
            if (!GranadaTomada)
            {
                return false;
            }
            GranadaTomada = false;
            ManoVR mano = adaptador.ManoDerecha;
            if (mano == null)
            {
                return false;
            }
            velocidadAlSoltar = velocidadMano;
            Vector3 velocidad = Vector3.ClampMagnitude(velocidadMano * FactorLanzamiento, VelocidadMaximaLanzamiento);
            Vector3 origen = mano.Palma;
            if (!especiales.LanzarGranadaExterna(origen, velocidad))
            {
                // Aún no pasa el tiempo entre granadas: sigue en la mano y se reintenta (hasta SegundosReintentoLanzar).
                GranadaTomada = Time.time < reintentarHasta;
                return false;
            }
            reintentarHasta = -1f;
            Lanzadas++;
            UltimaVelocidadMano = velocidadMano;
            UltimaVelocidadGranada = velocidad;
            UltimoOrigenGranada = origen;
            mano.Vibrar(AmplitudLanzar, SegundosLanzar);
            return true;
        }

        private void AlDisparar(ArmaAPie arma, Vector3 boca)
        {
            if (UsaGatillo(arma))
            {
                VibrarDerecha(AmplitudTiro, SegundosTiro);
            }
        }

        private void VibrarDerecha(float amplitud, float segundos)
        {
            ManoVR mano = adaptador != null ? adaptador.ManoDerecha : null;
            if (mano != null)
            {
                mano.Vibrar(amplitud, segundos);
            }
        }

        // La bola de la carga del plasma cuelga del jugador: AdaptadorVR la dejaría en solo sombra como al cuerpo.
        private void MostrarBola()
        {
            if (bolaVisible)
            {
                return;
            }
            Transform bola = especiales.BolaCarga;
            if (bola == null)
            {
                return;
            }
            bolaVisible = true;
            if (bola.GetComponent<IgnorarOcultarVR>() == null)
            {
                bola.gameObject.AddComponent<IgnorarOcultarVR>();
            }
            Renderer r = bola.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
