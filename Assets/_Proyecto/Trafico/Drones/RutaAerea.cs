using UnityEngine;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1398: mueve este objeto en bucle por una ruta de puntos (x, z) a una altura, con velocidad en m/s, inclinándose al girar y oscilando suave.
    /// Los puntos y el movimiento van en el espacio local del padre. La lógica está en <see cref="CalculoRutaAerea"/>.
    /// T-1494: no atraviesa muros. Antes de cada paso lanza una esfera hacia donde va; si hay un obstáculo fijo no avanza y sube (<see cref="ReglaDesvioAereo"/>)
    /// hasta librarlo, y luego vuelve a bajar. La altura de ruta se limita a <c>alturaMaxima</c> para que se vea desde la calle.
    /// </summary>
    public class RutaAerea : MonoBehaviour
    {
        private const float SuavizadoAlabeo = 4f;
        private const float FraccionRadioLlegada = 0.98f;

        [SerializeField, Tooltip("Esquinas de la ruta (x, z) en m, en el espacio local del padre. El bucle se cierra solo. Al menos 3.")] private Vector2[] puntos;
        [SerializeField, Min(0f), Tooltip("Altura media sobre el origen del padre (m).")] private float altura = 20f;
        [SerializeField, Min(0.1f), Tooltip("Velocidad a lo largo de la ruta (m/s).")] private float velocidad = 8f;
        [SerializeField, Min(0f), Tooltip("Cuánto sube y baja (m).")] private float amplitudOscilacion = 0.6f;
        [SerializeField, Min(0.1f), Tooltip("Segundos de una oscilación completa.")] private float periodoOscilacion = 4f;
        [SerializeField, Range(0f, 45f), Tooltip("Tope de la inclinación al girar (°).")] private float alabeoMaximo = 12f;
        [SerializeField, Min(0f), Tooltip("Grados de inclinación por cada °/s que gira el rumbo.")] private float alabeoPorGiro = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Fracción de vuelta en que arranca, para que dos drones de la misma ruta no vayan juntos.")] private float fase;

        [Header("Obstáculos (T-1494)")]
        [SerializeField, Tooltip("Si es true, el dron no atraviesa geometría: sube para rodear lo que le bloquea el paso.")] private bool evitaObstaculos = true;
        [SerializeField, Min(0.05f), Tooltip("Radio de la esfera con que revisa su paso (m).")] private float radioDron = 0.8f;
        [SerializeField, Min(0f), Tooltip("Margen de despeje extra sobre el radio: el dron pasa al menos a este tramo de cualquier muro, sin rozarlo (m).")] private float margenDespeje = 1.5f;
        [SerializeField, Min(0.1f), Tooltip("Cuánto sube al toparse con algo (m/s).")] private float velocidadSubida = 8f;
        [SerializeField, Min(0.1f), Tooltip("Cuánto baja de regreso a su altura cuando está libre (m/s).")] private float velocidadBajada = 3f;
        [SerializeField, Min(0f), Tooltip("Máximo que sube sobre su altura de ruta (m).")] private float desvioMaximo = 40f;
        [SerializeField, Min(2f), Tooltip("Tope de la altura de ruta para que el dron se vea desde la calle (m).")] private float alturaMaxima = 14f;

        private static readonly RaycastHit[] impactos = new RaycastHit[16];
        private static readonly Collider[] tocados = new Collider[16];
        private ReglaDesvioAereo desvio;
        private CalculoRutaAerea calculo;
        private float tiempo;
        private float alabeoActual;

        public float Altura => altura;
        /// <summary>Altura de ruta que se usa: la de la ruta, con el tope <c>alturaMaxima</c> (m).</summary>
        public float AlturaEfectiva => Mathf.Min(altura, alturaMaxima);
        /// <summary>Metros que ha subido para rodear un obstáculo.</summary>
        public float Desvio => desvio != null ? desvio.Desvio : 0f;
        public float Velocidad => velocidad;
        public int CantidadPuntos => puntos != null ? puntos.Length : 0;

        private void Awake()
        {
            if (puntos == null || puntos.Length < 3)
            {
                Debug.LogError("[Trafico] RutaAerea en " + name + ": necesita al menos 3 puntos.", this);
                enabled = false;
                return;
            }
            calculo = new CalculoRutaAerea(puntos, AlturaEfectiva, velocidad, amplitudOscilacion, periodoOscilacion, alabeoMaximo, alabeoPorGiro);
            desvio = new ReglaDesvioAereo(velocidadSubida, velocidadBajada, desvioMaximo);
            Aplicar(0f, true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (!evitaObstaculos)
            {
                tiempo += dt;
                Aplicar(dt, false);
                return;
            }
            float siguiente = tiempo + dt;
            MuestraRutaAerea m = calculo.Muestra(siguiente, fase);
            Vector3 actual = transform.position;
            float bajo = desvio.DesvioBajando(dt);
            Vector3 destino = EnMundo(m.Posicion + Vector3.up * bajo);
            if (!Bloqueado(actual, destino))
            {
                desvio.Fijar(bajo);
                Avanzar(siguiente, m, destino, dt);
                return;
            }
            destino = EnMundo(m.Posicion + Vector3.up * desvio.Desvio);
            if (!Bloqueado(actual, destino))
            {
                Avanzar(siguiente, m, destino, dt);
                return;
            }
            // Bloqueado: no avanza en la ruta y sube hasta librar el obstáculo.
            float sube = desvio.DesvioSubiendo(dt) - desvio.Desvio;
            if (sube > 0f && !Bloqueado(actual, actual + Vector3.up * sube, false))
            {
                desvio.Fijar(desvio.Desvio + sube);
                transform.position = actual + Vector3.up * sube;
            }
        }

        private Vector3 EnMundo(Vector3 local) => transform.parent != null ? transform.parent.TransformPoint(local) : local;

        private void Avanzar(float nuevoTiempo, MuestraRutaAerea m, Vector3 destino, float dt)
        {
            tiempo = nuevoTiempo;
            alabeoActual = Mathf.Lerp(alabeoActual, m.Alabeo, 1f - Mathf.Exp(-SuavizadoAlabeo * dt));
            transform.position = destino;
            transform.localRotation = Quaternion.LookRotation(m.Rumbo, Vector3.up) * Quaternion.Euler(0f, 0f, alabeoActual);
        }

        // True si entre los dos puntos hay geometría fija (no se cuentan sus propias piezas ni cuerpos con Rigidbody: carros, objetos).
        private bool Bloqueado(Vector3 desde, Vector3 hasta, bool revisarDestino = true)
        {
            Vector3 dir = hasta - desde;
            float distancia = dir.magnitude;
            if (distancia < 0.0001f)
            {
                return false;
            }
            int n = Physics.SphereCastNonAlloc(desde, radioDron + margenDespeje, dir / distancia, impactos, distancia, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = impactos[i].collider;
                if (impactos[i].distance <= 0f || c.transform.IsChildOf(transform) || c.attachedRigidbody != null)
                {
                    continue;
                }
                return true;
            }
            // Un SphereCast no reporta lo que ya toca la esfera al salir (distance 0): sin esto, un paso que termina rozando el muro lo deja
            // "dentro" y los siguientes lo atraviesan. Se revisa el punto de llegada (al subir junto al muro no, o no podría rozarlo para subir).
            if (revisarDestino)
            {
                int m = Physics.OverlapSphereNonAlloc(hasta, (radioDron + margenDespeje) * FraccionRadioLlegada, tocados, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < m; i++)
                {
                    if (!tocados[i].transform.IsChildOf(transform) && tocados[i].attachedRigidbody == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void Aplicar(float dt, bool saltar)
        {
            MuestraRutaAerea m = calculo.Muestra(tiempo, fase);
            alabeoActual = saltar ? m.Alabeo : Mathf.Lerp(alabeoActual, m.Alabeo, 1f - Mathf.Exp(-SuavizadoAlabeo * dt));
            transform.localPosition = m.Posicion;
            transform.localRotation = Quaternion.LookRotation(m.Rumbo, Vector3.up) * Quaternion.Euler(0f, 0f, alabeoActual);
        }
    }
}
