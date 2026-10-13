using System;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// Hoja de una puerta: sube hacia el dintel al abrir y baja al cerrar. Abierta, su collider deja pasar.
    /// Se cierra sola tras unos segundos si nadie está en el umbral (la caja del umbral, medida sin trigger para no estorbar a los rayos).
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

        private readonly Collider[] ocupantes = new Collider[MaximoOcupantes];
        private Vector3 posicionCerrada;
        private float progreso;
        private float segundosSinNadie;
        private bool abierta;

        /// <summary>True si la hoja está abierta o abriéndose (el collider ya no estorba).</summary>
        public bool EstaAbierta => abierta;

        /// <summary>Avisa al abrir (true) o cerrar (false), también cuando se cierra sola.</summary>
        public event Action<bool> Cambio;

        /// <summary>Cuántos cuerpos (con CharacterController o Rigidbody) están ahora en el umbral, sin usar un trigger para no estorbar a los rayos de otros.</summary>
        public int OcupantesEnUmbral => ContarOcupantes();

        private void Awake()
        {
            if (config == null || colliderHoja == null || umbral == null)
            {
                Debug.LogError("[Mundo] HojaPuerta en '" + name + "' necesita config, collider de la hoja y umbral.", this);
                enabled = false;
                return;
            }
            posicionCerrada = transform.localPosition;
        }

        public void Abrir()
        {
            if (abierta)
            {
                return;
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
            float paso = Time.deltaTime / config.DuracionApertura;
            progreso = Mathf.Clamp01(progreso + (abierta ? paso : -paso));
            transform.localPosition = posicionCerrada + Vector3.up * (config.Desplazamiento * progreso);

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
