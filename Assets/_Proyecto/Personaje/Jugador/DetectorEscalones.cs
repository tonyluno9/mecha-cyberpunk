using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-1515: mira el piso bajo el jugador y un poco adelante, en la dirección en que va, y dice si sube o baja escalones de concreto (ReglaEscalones).
    /// Lo agrega AnimacionJugador. No cuenta en la escalera de mano (TreparAPie) ni en el aire.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class DetectorEscalones : MonoBehaviour
    {
        private const float DistanciaAdelante = 0.45f;
        private const float AlturaOrigen = 0.6f;
        private const float AlcanceRayo = 1.2f;
        private const float VelocidadMinima = 0.3f;

        private static readonly RaycastHit[] golpes = new RaycastHit[8];

        private CharacterController cuerpo;
        private ControladorAPie controlador;
        private TreparAPie trepar;
        private readonly SeguidorEscalones seguidor = new SeguidorEscalones();

        /// <summary>0 nada, 1 subiendo, -1 bajando (ya filtrado).</summary>
        public int Tipo => seguidor.Actual;

        private void Awake()
        {
            cuerpo = GetComponent<CharacterController>();
            controlador = GetComponent<ControladorAPie>();
            trepar = GetComponent<TreparAPie>();
        }

        private void OnDisable() => seguidor.Reiniciar();

        private void Update()
        {
            seguidor.Avanzar(Detectar(), Time.deltaTime);
        }

        private int Detectar()
        {
            if (cuerpo == null || !cuerpo.enabled || controlador == null || !controlador.EnPiso || (trepar != null && trepar.enabled && trepar.Trepando))
            {
                return 0;
            }
            Vector3 velocidad = cuerpo.velocity;
            velocidad.y = 0f;
            if (velocidad.magnitude < VelocidadMinima)
            {
                return 0;
            }
            Vector3 dir = velocidad.normalized;
            if (!Piso(transform.position, out float alturaAqui, out float normalAqui)
                || !Piso(transform.position + dir * DistanciaAdelante, out float alturaAdelante, out float normalAdelante))
            {
                return 0;
            }
            return ReglaEscalones.Clasificar(alturaAqui, alturaAdelante, normalAqui, normalAdelante);
        }

        // Piso bajo un punto: rayo hacia abajo desde un poco más arriba que la rodilla, sin contar lo propio ni los triggers.
        private bool Piso(Vector3 punto, out float altura, out float normalY)
        {
            altura = 0f;
            normalY = 0f;
            Vector3 origen = new Vector3(punto.x, transform.position.y + AlturaOrigen, punto.z);
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, golpes, AlcanceRayo, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < n; i++)
            {
                if (golpes[i].collider.transform.root == transform.root || golpes[i].distance >= mejor)
                {
                    continue;
                }
                mejor = golpes[i].distance;
                altura = golpes[i].point.y;
                normalY = golpes[i].normal.y;
                hay = true;
            }
            return hay;
        }
    }
}
