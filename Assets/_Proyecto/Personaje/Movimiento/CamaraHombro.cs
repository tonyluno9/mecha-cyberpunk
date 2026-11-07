using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// Cámara de hombro (T-400): órbita alrededor del jugador con el giro y la inclinación de <see cref="ControladorAPie"/>.
    /// Va en el objeto de la cámara; cada frame fija su posición en el mundo (queda hija del jugador, pero no la mueve la jerarquía).
    /// Una esfera la acerca si hay un muro entre el jugador y la cámara. Si la cámara está apagada (carro) o el jugador en KO, no hace nada.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CamaraHombro : MonoBehaviour
    {
        private const int MaxChoques = 16;
        private const float AlturaCabeza = 1.7f;

        [SerializeField] private ConfigCamaraHombro config;
        [SerializeField] private ControladorAPie controlador;
        [SerializeField] private Camera camara;

        private readonly RaycastHit[] choques = new RaycastHit[MaxChoques];
        private float lateral;
        private float altura;
        private float distancia;
        private float fov;
        private bool iniciada;
        private ZoomSniper zoom;
        private SensacionDisparo sensacion;

        public float DistanciaActual => distancia;

        private void Awake()
        {
            if (camara == null)
            {
                camara = GetComponent<Camera>();
            }
            if (controlador == null)
            {
                controlador = GetComponentInParent<ControladorAPie>();
            }
            zoom = controlador != null ? controlador.GetComponent<ZoomSniper>() : null;
            if (config == null || controlador == null || camara == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en CamaraHombro de " + name, this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (!camara.enabled || !controlador.ControlActivo)
            {
                iniciada = false;
                return;
            }
            bool apuntando = controlador.Apuntando;
            float objetivoLateral = apuntando ? config.LateralApuntando : config.LateralNormal;
            float objetivoAltura = apuntando ? config.AlturaApuntando : config.AlturaNormal;
            float objetivoDistancia = apuntando ? config.DistanciaApuntando : config.DistanciaNormal;
            float objetivoFov = apuntando ? config.FovApuntando : config.FovNormal;
            if (!iniciada)
            {
                iniciada = true;
                lateral = objetivoLateral;
                altura = objetivoAltura;
                distancia = objetivoDistancia;
                fov = objetivoFov;
            }
            else
            {
                float paso = 1f - Mathf.Exp(-config.Suavizado * Time.deltaTime);
                lateral = Mathf.Lerp(lateral, objetivoLateral, paso);
                altura = Mathf.Lerp(altura, objetivoAltura, paso);
                distancia = Mathf.Lerp(distancia, objetivoDistancia, paso);
                fov = Mathf.Lerp(fov, objetivoFov, paso);
            }

            if (sensacion == null)
            {
                sensacion = controlador.GetComponent<SensacionDisparo>();
            }
            float patada = sensacion != null ? sensacion.Patada : 0f;
            float desvio = sensacion != null ? sensacion.Desvio : 0f;
            Quaternion giro = Quaternion.Euler(controlador.Inclinacion - (zoom != null ? zoom.Sacudida : 0f) - patada, controlador.GiroCamara + desvio, 0f);
            Quaternion giroHorizontal = Quaternion.Euler(0f, controlador.GiroCamara, 0f);
            Vector3 cabeza = controlador.transform.position + Vector3.up * LibreArriba(altura - controlador.AjusteAlturaCamara);
            // T-530: el hombro (pivote) puede quedar dentro de un muro si el jugador está pegado a él; el rayo de la cámara saldría de adentro
            // y no lo vería. Se acerca el pivote a la cabeza hasta antes del muro.
            Vector3 pivote = cabeza + giroHorizontal * (Vector3.right * Recortar(cabeza, giroHorizontal * Vector3.right, lateral));
            Vector3 deseada = pivote - giro * Vector3.forward * distancia;
            Vector3 posicion = deseada;

            Vector3 haciaCamara = deseada - pivote;
            float largo = haciaCamara.magnitude;
            if (largo > 0.001f)
            {
                int cantidad = Physics.SphereCastNonAlloc(pivote, config.RadioColision, haciaCamara / largo, choques, largo, ~0, QueryTriggerInteraction.Ignore);
                float minima = largo;
                for (int i = 0; i < cantidad; i++)
                {
                    RaycastHit choque = choques[i];
                    if (choque.collider.transform.root == controlador.transform.root || choque.distance <= 0f)
                    {
                        continue;
                    }
                    minima = Mathf.Min(minima, choque.distance);
                }
                posicion = pivote + haciaCamara / largo * minima;
            }

            transform.SetPositionAndRotation(posicion, giro);
            camara.fieldOfView = zoom != null ? Mathf.Lerp(fov, config.FovSniper, zoom.Mezcla) : fov;
        }

        /// <summary>
        /// T-699: la cámara va alta (más que la cabeza); bajo un techo (casas, túneles) el pivote no puede pasar el techo: se recorta la altura
        /// hasta antes de lo que haya encima del jugador.
        /// </summary>
        private float LibreArriba(float alturaDeseada)
        {
            if (alturaDeseada <= AlturaCabeza)
            {
                return alturaDeseada;
            }
            Vector3 desde = controlador.transform.position + Vector3.up * AlturaCabeza;
            float largo = alturaDeseada - AlturaCabeza;
            int cantidad = Physics.SphereCastNonAlloc(desde, config.RadioColision, Vector3.up, choques, largo, ~0, QueryTriggerInteraction.Ignore);
            float minima = largo;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit choque = choques[i];
                if (choque.collider.transform.root == controlador.transform.root || choque.distance <= 0f)
                {
                    continue;
                }
                minima = Mathf.Min(minima, choque.distance);
            }
            return AlturaCabeza + minima;
        }

        /// <summary>Distancia libre (hasta 'largo') desde 'origen' en 'direccion' antes de chocar con algo que no sea el jugador, menos el radio de la cámara.</summary>
        private float Recortar(Vector3 origen, Vector3 direccion, float largo)
        {
            float signo = Mathf.Sign(largo);
            float abs = Mathf.Abs(largo);
            if (abs < 0.001f)
            {
                return largo;
            }
            int cantidad = Physics.SphereCastNonAlloc(origen, config.RadioColision, direccion * signo, choques, abs, ~0, QueryTriggerInteraction.Ignore);
            float minima = abs;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit choque = choques[i];
                if (choque.collider.transform.root == controlador.transform.root)
                {
                    continue;
                }
                minima = Mathf.Min(minima, Mathf.Max(0f, choque.distance));
            }
            return minima * signo;
        }
    }
}
