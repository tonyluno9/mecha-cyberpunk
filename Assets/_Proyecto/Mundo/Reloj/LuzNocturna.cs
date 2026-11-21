using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// Prende de noche y apaga de día una luz y/o el brillo emisivo de unos renderers (farolas, letreros de neón).
    /// Sigue la franja del reloj del mundo de la escena.
    /// </summary>
    public class LuzNocturna : MonoBehaviour
    {
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Luz que se prende de noche (opcional).")]
        [SerializeField] private Light luz;

        [Tooltip("Renderers cuyo brillo emisivo se prende de noche (opcional).")]
        [SerializeField] private Renderer[] renderers;

        private const float CambioMinimo = 0.02f;

        private IRelojMundo reloj;
        private MaterialPropertyBlock bloque;
        private Color[] emisionPrendida;
        private float intensidadPrendida;
        private float factor = -1f;

        /// <summary>T-1545: la luz que esta pieza prende y apaga (null si solo maneja emisión).</summary>
        public Light Luz => luz;

        /// <summary>
        /// T-1545: multiplica la intensidad de noche de la luz (y la de ahora). Hay que usarlo en vez de tocar <c>Light.intensity</c>,
        /// porque este componente la reescribe con la intensidad que guardó al despertar.
        /// </summary>
        public void EscalarIntensidad(float multiplo)
        {
            if (luz == null || multiplo <= 0f)
            {
                return;
            }
            intensidadPrendida *= multiplo;
            luz.intensity *= multiplo;
        }

        /// <summary>T-535: brillo actual de 0 (apagada) a 1 (prendida); entre los dos al amanecer y al anochecer.</summary>
        public float Factor => Mathf.Max(0f, factor);

        /// <summary>True si la luz y el brillo están prendidos ahora.</summary>
        public bool EstaPrendida { get; private set; }

        private void Awake()
        {
            bloque = new MaterialPropertyBlock();
            intensidadPrendida = luz != null ? luz.intensity : 0f;
            if (renderers == null)
            {
                renderers = new Renderer[0];
            }
            emisionPrendida = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;
                emisionPrendida[i] = material != null && material.HasProperty(IdEmision) ? material.GetColor(IdEmision) : Color.black;
            }
        }

        private void OnEnable()
        {
            reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.FranjaCambio += AlCambiarFranja;
                Poner(reloj.EsDeNoche);
            }
            else
            {
                Registro<IRelojMundo>.Agregado += AlLlegarReloj;
            }
        }

        private void OnDisable()
        {
            Registro<IRelojMundo>.Agregado -= AlLlegarReloj;
            if (reloj != null)
            {
                reloj.FranjaCambio -= AlCambiarFranja;
                reloj = null;
            }
        }

        private void AlLlegarReloj(IRelojMundo nuevo)
        {
            Registro<IRelojMundo>.Agregado -= AlLlegarReloj;
            reloj = nuevo;
            reloj.FranjaCambio += AlCambiarFranja;
            Poner(reloj.EsDeNoche);
        }

        private void AlCambiarFranja(FranjaDia franja) => Poner(franja == FranjaDia.Noche);

        /// <summary>Prende (true) o apaga (false) la luz y el brillo.</summary>
        public void Poner(bool prendida)
        {
            if (reloj != null)
            {
                // T-535: con reloj, la hora manda (se prende y se apaga poco a poco).
                Aplicar(CalculoLuzNocturna.Factor(reloj.Hora, reloj.HoraAmanecer, reloj.HoraAnochecer));
                return;
            }
            Aplicar(prendida ? 1f : 0f);
        }

        private void Update()
        {
            if (reloj == null)
            {
                return;
            }
            float nuevo = CalculoLuzNocturna.Factor(reloj.Hora, reloj.HoraAmanecer, reloj.HoraAnochecer);
            if (Mathf.Abs(nuevo - factor) >= CambioMinimo || (nuevo == 0f) != (factor == 0f) || (nuevo == 1f) != (factor == 1f))
            {
                Aplicar(nuevo);
            }
        }

        private void Aplicar(float nuevo)
        {
            factor = nuevo;
            EstaPrendida = nuevo > 0.5f;
            if (luz != null)
            {
                luz.enabled = nuevo > 0.01f;
                luz.intensity = intensidadPrendida * nuevo;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }
                renderers[i].GetPropertyBlock(bloque);
                bloque.SetColor(IdEmision, emisionPrendida[i] * nuevo);
                renderers[i].SetPropertyBlock(bloque);
            }
        }
    }
}
