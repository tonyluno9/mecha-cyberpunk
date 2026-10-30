using UnityEngine;

namespace MechaCyberpunk.Progresion.Partida
{
    /// <summary>
    /// La cámara lenta del título (T-413, D-tony-245): al despertar decide si la escena abre en el título
    /// (<see cref="FlujoPartida.DecidirTitulo"/>, por eso corre antes que todos). En el título apaga las demás cámaras que dibujan a la
    /// pantalla y recorre despacio una vuelta cerrada por las calles del barrio, a <see cref="altura"/> m, mirando hacia adelante y abajo.
    /// Fuera del título (o al empezar a jugar) se apaga sola.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(Camera))]
    public class CamaraTitulo : MonoBehaviour
    {
        private const float SegundosRevisar = 0.5f;
        private const float DistanciaMinima = 0.01f;

        [Tooltip("Puntos de la vuelta sobre el piso (la altura se suma). Se recorren en orden y del último se vuelve al primero.")]
        [SerializeField] private Vector3[] puntos = new Vector3[0];
        [Tooltip("Velocidad del recorrido (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidad = 2f;
        [Tooltip("Altura sobre los puntos (m).")]
        [SerializeField] private float altura = 12f;
        [Tooltip("Cuántos metros por delante mira, sobre el recorrido.")]
        [Min(1f)] [SerializeField] private float mirarAdelante = 25f;
        [Tooltip("Cuántos metros más abajo que la cámara mira.")]
        [SerializeField] private float mirarAbajo = 9f;
        [Tooltip("Qué tan rápido gira hacia donde mira (más es más brusco).")]
        [Min(0.1f)] [SerializeField] private float suavidadGiro = 1.5f;

        private Camera camara;
        private float recorrido;
        private float largo;
        private float relojRevisar;

        public Camera Camara => camara;

        /// <summary>Metros recorridos desde que empezó (pruebas: la cámara se mueve).</summary>
        public float Recorrido => recorrido;

        public int CantidadPuntos => puntos.Length;

        /// <summary>Para el creador de la escena.</summary>
        public void Configurar(Vector3[] nuevosPuntos, float nuevaVelocidad, float nuevaAltura)
        {
            puntos = nuevosPuntos ?? new Vector3[0];
            velocidad = Mathf.Max(0.1f, nuevaVelocidad);
            altura = nuevaAltura;
        }

        private void Awake()
        {
            camara = GetComponent<Camera>();
            if (!FlujoPartida.DecidirTitulo())
            {
                camara.enabled = false;
                gameObject.SetActive(false);
                return;
            }
            if (puntos.Length < 2)
            {
                Debug.LogWarning("[Progresion] CamaraTitulo sin recorrido: el título queda con la cámara quieta.", this);
            }
            largo = 0f;
            for (int i = 0; i < puntos.Length; i++)
            {
                largo += Vector3.Distance(puntos[i], puntos[(i + 1) % puntos.Length]);
            }
            Colocar(true);
        }

        private void Start()
        {
            if (FlujoPartida.EnTitulo)
            {
                camara.enabled = true;
                ApagarOtras();
            }
        }

        private void LateUpdate()
        {
            if (!FlujoPartida.EnTitulo)
            {
                gameObject.SetActive(false);
                return;
            }
            relojRevisar -= Time.unscaledDeltaTime;
            if (relojRevisar <= 0f)
            {
                relojRevisar = SegundosRevisar;
                ApagarOtras();
            }
            if (puntos.Length < 2 || largo <= DistanciaMinima)
            {
                return;
            }
            recorrido += velocidad * Time.unscaledDeltaTime;
            Colocar(false);
        }

        // Solo una cámara dibuja a la pantalla en el título: la del jugador y las demás se apagan (la escena se recarga al salir).
        private void ApagarOtras()
        {
            foreach (Camera otra in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (otra != camara && otra.enabled && otra.targetTexture == null)
                {
                    otra.enabled = false;
                }
            }
        }

        private void Colocar(bool alInstante)
        {
            if (puntos.Length < 2 || largo <= DistanciaMinima)
            {
                return;
            }
            Vector3 posicion = PuntoEn(recorrido) + Vector3.up * altura;
            Vector3 objetivo = PuntoEn(recorrido + mirarAdelante) + Vector3.up * (altura - mirarAbajo);
            Vector3 direccion = objetivo - posicion;
            transform.position = posicion;
            if (direccion.sqrMagnitude < DistanciaMinima)
            {
                return;
            }
            Quaternion deseada = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = alInstante ? deseada : Quaternion.Slerp(transform.rotation, deseada, 1f - Mathf.Exp(-suavidadGiro * Time.unscaledDeltaTime));
        }

        // El punto del recorrido a tantos metros del inicio (da vueltas).
        private Vector3 PuntoEn(float metros)
        {
            float d = Mathf.Repeat(metros, largo);
            for (int i = 0; i < puntos.Length; i++)
            {
                Vector3 a = puntos[i];
                Vector3 b = puntos[(i + 1) % puntos.Length];
                float tramo = Vector3.Distance(a, b);
                if (d <= tramo || i == puntos.Length - 1)
                {
                    return tramo > DistanciaMinima ? Vector3.Lerp(a, b, d / tramo) : a;
                }
                d -= tramo;
            }
            return puntos[0];
        }
    }
}
