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

        private IRelojMundo reloj;
        private MaterialPropertyBlock bloque;
        private Color[] emisionPrendida;

        /// <summary>True si la luz y el brillo están prendidos ahora.</summary>
        public bool EstaPrendida { get; private set; }

        private void Awake()
        {
            bloque = new MaterialPropertyBlock();
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
            EstaPrendida = prendida;
            if (luz != null)
            {
                luz.enabled = prendida;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(bloque);
                bloque.SetColor(IdEmision, prendida ? emisionPrendida[i] : Color.black);
                renderers[i].SetPropertyBlock(bloque);
            }
        }
    }
}
