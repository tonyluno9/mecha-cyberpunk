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

            Quaternion giro = Quaternion.Euler(controlador.Inclinacion - (zoom != null ? zoom.Sacudida : 0f), controlador.GiroCamara, 0f);
            Quaternion giroHorizontal = Quaternion.Euler(0f, controlador.GiroCamara, 0f);
            Vector3 pivote = controlador.transform.position + Vector3.up * altura + giroHorizontal * (Vector3.right * lateral);
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
    }
}
