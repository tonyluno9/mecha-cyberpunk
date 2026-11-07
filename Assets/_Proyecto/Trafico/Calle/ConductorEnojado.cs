using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-703: el conductor de un carro de tráfico al que le dieron un golpe fuerte: se baja, se queda parado junto a la puerta mirando a quien
    /// chocó, le reclama (gesto de burla/enojo cada pocos segundos) y, pasado el tiempo, vuelve al carro (desaparece). No se mueve del lugar.
    /// </summary>
    public class ConductorEnojado : MonoBehaviour
    {
        private const float GradosPorSegundo = 360f;
        private const float SegundosEntreGestos = 2.2f;
        private const float AlturaBusquedaPiso = 3f;

        private Vector3 puntoAmenaza;
        private float restante;
        private float proximoGesto;
        private int gestos;

        /// <summary>Cuántos gestos de enojo hizo.</summary>
        public int Gestos => gestos;

        /// <summary>Segundos que le faltan para volver al carro.</summary>
        public float Restante => restante;

        /// <summary>Lo baja al piso, lo hace mirar a <paramref name="haciaQuien"/> y empieza el reclamo durante <paramref name="segundos"/>.</summary>
        public void Iniciar(Vector3 haciaQuien, float segundos)
        {
            puntoAmenaza = haciaQuien;
            restante = Mathf.Max(0.1f, segundos);
            proximoGesto = Time.time + 0.3f;
            PegarAlPiso();
            Girar(1000f);
        }

        private void Update()
        {
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            Girar(GradosPorSegundo * Time.deltaTime);
            if (Time.time >= proximoGesto)
            {
                proximoGesto = Time.time + SegundosEntreGestos;
                gestos++;
                BroadcastMessage("Burlar", SendMessageOptions.DontRequireReceiver); // lo atiende VisualPersonaje del modelo activo (Trafico no lo referencia)
            }
        }

        private void PegarAlPiso()
        {
            if (Physics.Raycast(transform.position + Vector3.up * AlturaBusquedaPiso, Vector3.down, out RaycastHit piso, AlturaBusquedaPiso * 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = transform.position;
                p.y = piso.point.y;
                transform.position = p;
            }
        }

        private void Girar(float grados)
        {
            Vector3 hacia = puntoAmenaza - transform.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(hacia, Vector3.up), grados);
            }
        }
    }
}
