using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-545: anima la pistola W01 con las curvas de W01_Pistola_Disparo.blend y W01_Pistola_Recarga.blend (Tony, 2-nov): al disparar la corredera
    /// va 2 cm atrás y regresa (10 cuadros a 24 fps); al recargar sale el cargador (cae girando), entra uno nuevo y la corredera se corre
    /// (40 cuadros), estirado a lo que dura la recarga del juego. La del jugador escucha su <see cref="PistolaAPie"/>; la de un pandillero, los
    /// disparos que suenan en su boca (<see cref="EventosRuido"/>).
    /// </summary>
    public class AnimacionPistola : MonoBehaviour
    {
        private const float CuadrosPorSegundo = 24f;
        private const float CuadrosDisparo = 10f;
        private const float CuadrosRecarga = 40f;
        private const float RecorridoCorredera = 0.02f;
        private const float RadioOido = 1.2f;

        [Tooltip("La corredera (se mueve en su eje X local, hacia atrás).")]
        [SerializeField] private Transform corredera;
        [Tooltip("El cargador (sale por abajo al recargar).")]
        [SerializeField] private Transform bateria;
        [Tooltip("Escala del modelo respecto al FBX (m por m): las distancias de las curvas se multiplican por ella.")]
        [Min(0.01f)] [SerializeField] private float escalaModelo = 1f;

        private PistolaAPie pistola;
        private Vector3 correderaReposo;
        private Vector3 bateriaReposo;
        private Quaternion bateriaGiroReposo;
        private Transform nueva;
        private float tDisparo = -1f;
        private float tRecarga = -1f;
        private float duracionRecarga = 1.6f;
        private bool recargaba;

        public bool Disparando => tDisparo >= 0f;
        public bool Recargando => tRecarga >= 0f;
        public Transform Corredera => corredera;
        public float DuracionRecarga => duracionRecarga;

        public void Configurar(Transform nuevaCorredera, Transform nuevaBateria, float escala)
        {
            corredera = nuevaCorredera;
            bateria = nuevaBateria;
            escalaModelo = Mathf.Max(0.01f, escala);
        }

        private void Awake()
        {
            if (corredera != null)
            {
                correderaReposo = corredera.localPosition;
            }
            if (bateria != null)
            {
                bateriaReposo = bateria.localPosition;
                bateriaGiroReposo = bateria.localRotation;
                nueva = Instantiate(bateria.gameObject, bateria.parent).transform;
                nueva.name = "Bateria_Nueva";
                nueva.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            pistola = GetComponentInParent<PistolaAPie>();
            if (pistola != null)
            {
                pistola.Disparo += Disparar;
            }
            else
            {
                EventosRuido.Disparo += AlOirDisparo;
            }
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Disparo -= Disparar;
            }
            EventosRuido.Disparo -= AlOirDisparo;
            Reposo();
        }

        private void AlOirDisparo(Vector3 donde)
        {
            if (isActiveAndEnabled && (donde - transform.position).sqrMagnitude <= RadioOido * RadioOido)
            {
                Disparar();
            }
        }

        public void Disparar()
        {
            if (tRecarga < 0f)
            {
                tDisparo = 0f;
            }
        }

        public void Recargar(float segundos)
        {
            duracionRecarga = Mathf.Max(0.2f, segundos);
            tRecarga = 0f;
            tDisparo = -1f;
        }

        private void Update()
        {
            if (pistola != null)
            {
                bool recarga = pistola.Recargando;
                if (recarga && !recargaba)
                {
                    Recargar(pistola.Cargador != null ? pistola.Cargador.TiempoRecarga : duracionRecarga);
                }
                recargaba = recarga;
            }
            float dt = Time.deltaTime;
            if (tDisparo >= 0f)
            {
                tDisparo += dt;
                float cuadro = 1f + tDisparo * CuadrosPorSegundo;
                PonerCorredera(CurvaCorredera(cuadro, 1f));
                if (cuadro >= CuadrosDisparo)
                {
                    tDisparo = -1f;
                    Reposo();
                }
            }
            else if (tRecarga >= 0f)
            {
                tRecarga += dt;
                float cuadro = 1f + tRecarga / duracionRecarga * (CuadrosRecarga - 1f);
                Recarga(cuadro);
                if (cuadro >= CuadrosRecarga)
                {
                    tRecarga = -1f;
                    Reposo();
                }
            }
        }

        /// <summary>Cuánto va atrás la corredera (0 a 1): cuadro 'inicio' en reposo, 1 cuadro después atrás, se queda 1 y regresa en 3 (como el .blend).</summary>
        public static float CurvaCorredera(float cuadro, float inicio)
        {
            float c = cuadro - inicio;
            if (c <= 0f || c >= 5f)
            {
                return 0f;
            }
            if (c <= 1f)
            {
                return c;
            }
            if (c <= 2f)
            {
                return 1f;
            }
            return 1f - (c - 2f) / 3f;
        }

        private void PonerCorredera(float atras)
        {
            if (corredera != null)
            {
                corredera.localPosition = correderaReposo + Vector3.left * (RecorridoCorredera * escalaModelo * atras);
            }
        }

        private void Recarga(float cuadro)
        {
            if (bateria != null)
            {
                // Sale (cuadros 7-19): baja y gira mientras cae; luego ya no está.
                float sale = Mathf.Clamp01((cuadro - 7f) / 12f);
                bateria.gameObject.SetActive(cuadro < 19f);
                bateria.localPosition = bateriaReposo + new Vector3(0.025f * sale, -0.6f * sale * sale, -0.26f * sale) * escalaModelo;
                bateria.localRotation = bateriaGiroReposo * Quaternion.Euler(sale * 90f, 0f, sale * 45f);
            }
            if (nueva != null)
            {
                // Entra la nueva (cuadros 12-27) desde abajo y adelante.
                bool visible = cuadro >= 12f && cuadro < CuadrosRecarga;
                nueva.gameObject.SetActive(visible);
                float entra = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((cuadro - 12f) / 15f));
                nueva.localPosition = bateriaReposo + new Vector3(0.01f, -0.05f, -0.13f) * ((1f - entra) * escalaModelo);
                nueva.localRotation = bateriaGiroReposo;
            }
            PonerCorredera(CurvaCorredera(cuadro, 30f));
        }

        private void Reposo()
        {
            PonerCorredera(0f);
            if (bateria != null)
            {
                bateria.gameObject.SetActive(true);
                bateria.localPosition = bateriaReposo;
                bateria.localRotation = bateriaGiroReposo;
            }
            if (nueva != null)
            {
                nueva.gameObject.SetActive(false);
            }
        }
    }
}
