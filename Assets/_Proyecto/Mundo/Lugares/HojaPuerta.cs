using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// Hoja de una puerta: sube hacia el dintel al abrir y baja al cerrar. Abierta, su collider deja pasar.
    /// T-804: con <see cref="ConBisagra"/> gira sobre su orilla (la bisagra) los grados de <see cref="ConfigPuertas.GradosBisagra"/>, hacia el lado
    /// contrario de quien abre (el jugador registrado o el punto de <see cref="AbrirDesde"/>), como una puerta de casa.
    /// Se cierra sola tras unos segundos si nadie está en el umbral (la caja del umbral, medida sin trigger para no estorbar a los rayos).
    /// T-884: con <see cref="Corrediza"/> se desliza de lado (<see cref="DesplazamientoCorredizo"/>), como la puerta de cristal de una tienda.
    /// </summary>
    public class HojaPuerta : MonoBehaviour
    {
        private const int MaximoOcupantes = 16;
        private const float Mitad = 0.5f;

        [SerializeField] private ConfigPuertas config;
        [Tooltip("Collider de la hoja: se apaga mientras la puerta está abierta.")]
        [SerializeField] private Collider colliderHoja;
        [Tooltip("Centro del umbral: mientras haya alguien dentro de esta caja, la puerta no se cierra sola.")]
        [SerializeField] private Transform umbral;
        [Tooltip("Tamaño de la caja del umbral (m).")]
        [SerializeField] private Vector3 tamanoUmbral = new Vector3(3f, 3f, 2.4f);
        [Tooltip("T-804: gira sobre su bisagra en lugar de subir al dintel.")]
        [SerializeField] private bool conBisagra;
        [Tooltip("T-804: punto de la bisagra en el espacio local de la hoja, antes de su escala (para un cubo, (0.5, 0, 0) es su orilla +X).")]
        [SerializeField] private Vector3 bisagraLocal = new Vector3(0.5f, 0f, 0f);
        [Tooltip("T-884: se desliza de lado (puerta de cristal de tienda) en lugar de subir al dintel.")]
        [SerializeField] private bool corrediza;
        [Tooltip("T-884: hacia dónde y cuánto se desliza al abrir, en el espacio del padre (m).")]
        [SerializeField] private Vector3 desplazamientoCorredizo = new Vector3(0f, 0f, 2.5f);

        private readonly Collider[] ocupantes = new Collider[MaximoOcupantes];
        private Vector3 posicionCerrada;
        private Quaternion rotacionCerrada;
        private Vector3 bisagraPadre;
        private float sentido = 1f;
        private float progreso;
        private float segundosSinNadie;
        private bool abierta;

        /// <summary>True si la hoja está abierta o abriéndose (el collider ya no estorba).</summary>
        public bool EstaAbierta => abierta;

        /// <summary>Avisa al abrir (true) o cerrar (false), también cuando se cierra sola.</summary>
        public event Action<bool> Cambio;

        /// <summary>Cuántos cuerpos (con CharacterController o Rigidbody) están ahora en el umbral, sin usar un trigger para no estorbar a los rayos de otros.</summary>
        public int OcupantesEnUmbral => ContarOcupantes();

        /// <summary>T-884: true si la hoja se desliza de lado.</summary>
        public bool Corrediza => corrediza;
        /// <summary>T-884: lo que se desliza al abrir del todo, en el espacio del padre (m).</summary>
        public Vector3 DesplazamientoCorredizo => desplazamientoCorredizo;
        /// <summary>T-910: cuánto está abierta, de 0 (cerrada) a 1 (abierta del todo), ya suavizado.</summary>
        public float ProgresoApertura => Suavizar(progreso);

        /// <summary>T-804: true si la hoja gira sobre una bisagra.</summary>
        public bool ConBisagra => conBisagra;

        /// <summary>T-804: ángulo actual de la hoja con bisagra (°, con signo); 0 cerrada.</summary>
        public float AnguloActual => conBisagra ? sentido * config.GradosBisagra * Suavizar(progreso) : 0f;

        /// <summary>T-804: la bisagra en el mundo (m).</summary>
        public Vector3 BisagraMundo => transform.parent != null ? transform.parent.TransformPoint(bisagraPadre) : bisagraPadre;

        private void Awake()
        {
            if (config == null || colliderHoja == null || umbral == null)
            {
                Debug.LogError("[Mundo] HojaPuerta en '" + name + "' necesita config, collider de la hoja y umbral.", this);
                enabled = false;
                return;
            }
            posicionCerrada = transform.localPosition;
            rotacionCerrada = transform.localRotation;
            bisagraPadre = posicionCerrada + rotacionCerrada * Vector3.Scale(bisagraLocal, transform.localScale);
        }

        /// <summary>Abre; con bisagra, hacia el lado contrario del jugador registrado (si no hay, hacia su lado positivo).</summary>
        public void Abrir()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                AbrirDesde(jugador.Raiz.position);
                return;
            }
            AbrirConSentido(1f);
        }

        /// <summary>T-804: abre alejándose de 'desde' (quien abre), para que la hoja no le pegue.</summary>
        public void AbrirDesde(Vector3 desde)
        {
            AbrirConSentido(SentidoLejosDe(desde));
        }

        /// <summary>T-804: sentido de giro (+1 o -1) que aleja la orilla libre de la hoja del punto 'desde'.</summary>
        public float SentidoLejosDe(Vector3 desde)
        {
            Vector3 bisagra = BisagraMundo;
            Vector3 libre = transform.position - bisagra;
            libre.y = 0f;
            // Con giro positivo en Y la orilla libre avanza hacia up x libre.
            Vector3 avance = Vector3.Cross(Vector3.up, libre);
            Vector3 haciaQuien = desde - transform.position;
            haciaQuien.y = 0f;
            return Vector3.Dot(avance, haciaQuien) > 0f ? -1f : 1f;
        }

        private void AbrirConSentido(float nuevoSentido)
        {
            if (abierta)
            {
                return;
            }
            if (progreso <= 0f)
            {
                sentido = nuevoSentido; // a medio cerrar sigue hacia el mismo lado
            }
            abierta = true;
            segundosSinNadie = 0f;
            colliderHoja.enabled = false;
            Cambio?.Invoke(true);
        }

        public void Cerrar()
        {
            if (!abierta)
            {
                return;
            }
            abierta = false;
            colliderHoja.enabled = true;
            Cambio?.Invoke(false);
        }

        private void Update()
        {
            float paso = Time.deltaTime / (conBisagra ? config.DuracionBisagra : config.DuracionApertura);
            progreso = Mathf.Clamp01(progreso + (abierta ? paso : -paso));
            if (conBisagra)
            {
                Quaternion giro = Quaternion.AngleAxis(AnguloActual, Vector3.up);
                transform.localPosition = bisagraPadre + giro * (posicionCerrada - bisagraPadre);
                transform.localRotation = giro * rotacionCerrada;
            }
            else if (corrediza)
            {
                transform.localPosition = posicionCerrada + desplazamientoCorredizo * Suavizar(progreso);
            }
            else
            {
                transform.localPosition = posicionCerrada + Vector3.up * (config.Desplazamiento * progreso);
            }

            if (!abierta)
            {
                return;
            }
            if (ContarOcupantes() > 0)
            {
                segundosSinNadie = 0f;
                return;
            }
            segundosSinNadie += Time.deltaTime;
            if (segundosSinNadie >= config.SegundosParaCerrar)
            {
                Cerrar();
            }
        }

        // Arranca y frena suave (la hoja de bisagra no gira a velocidad constante).
        private static float Suavizar(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private int ContarOcupantes()
        {
            if (umbral == null)
            {
                return 0;
            }
            Transform t = umbral;
            Vector3 centro = t.position;
            Vector3 mitad = Vector3.Scale(tamanoUmbral, t.lossyScale) * Mitad;
            int total = Physics.OverlapBoxNonAlloc(centro, mitad, ocupantes, t.rotation, ~0, QueryTriggerInteraction.Ignore);
            int cuenta = 0;
            for (int i = 0; i < total; i++)
            {
                Collider c = ocupantes[i];
                if (c.GetComponent<CharacterController>() != null || c.attachedRigidbody != null)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }
    }
}
