using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// El conductor al que le robaron el carro (T-511): sale corriendo en línea recta, alejándose del jugador, esquivando lo que le cierre el paso
    /// (da vuelta al lado libre), pegado al piso, y desaparece a los pocos segundos. T-937: ahora lleva la cápsula del peatón; sus rayos la ignoran
    /// (antes el rayo al piso daba en su propia cápsula y lo subía 1.8 m cada cuadro: salía volando).
    /// </summary>
    public class ConductorHuye : MonoBehaviour
    {
        private const float RapidezHuida = 6f;
        private const float SegundosDeHuida = 8f;
        private const float AlturaDelPecho = 1f;
        private const float DistanciaVista = 1.2f;
        private const float AlturaBusquedaPiso = 3f;
        private const float GiroAlEsquivar = 70f;

        private Vector3 direccion = Vector3.forward;
        private float restante = SegundosDeHuida;
        private bool listo;

        /// <summary>Dirección en la que corre ahora.</summary>
        public Vector3 Direccion => direccion;

        /// <summary>Empieza a correr alejándose de <paramref name="amenaza"/> (el jugador).</summary>
        public void Iniciar(Vector3 amenaza)
        {
            Vector3 lejos = transform.position - amenaza;
            lejos.y = 0f;
            direccion = lejos.sqrMagnitude > 0.01f ? lejos.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(direccion, Vector3.up);
            listo = true;
        }

        private void Update()
        {
            if (!listo)
            {
                return;
            }
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 pecho = transform.position + Vector3.up * AlturaDelPecho;
            if (Rayo(pecho, direccion, DistanciaVista, out _))
            {
                direccion = Quaternion.Euler(0f, GiroAlEsquivar, 0f) * direccion;
            }
            Vector3 siguiente = transform.position + direccion * (RapidezHuida * Time.deltaTime);
            if (Rayo(siguiente + Vector3.up * AlturaBusquedaPiso, Vector3.down, AlturaBusquedaPiso * 2f, out RaycastHit piso))
            {
                siguiente.y = piso.point.y;
            }
            transform.SetPositionAndRotation(siguiente, Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direccion, Vector3.up), 720f * Time.deltaTime));
        }

        // El primer golpe del rayo que no sea del propio conductor (su cápsula, sus huesos).
        private bool Rayo(Vector3 origen, Vector3 sentido, float largo, out RaycastHit golpe)
        {
            golpe = default;
            float menor = float.MaxValue;
            bool hay = false;
            foreach (RaycastHit g in Physics.RaycastAll(origen, sentido, largo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (g.distance < menor && !g.collider.transform.IsChildOf(transform))
                {
                    menor = g.distance;
                    golpe = g;
                    hay = true;
                }
            }
            return hay;
        }
    }
}
