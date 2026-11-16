using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// T-864 / T-1005 (playtest 16-nov): EL DOC ya no instala el exo de piernas solo. Mientras no tengas ningún implante, al hablar con él te dice
    /// <see cref="Frase"/> y el EXO DE PIERNAS I aparece GRATIS en el menú de la clínica: lo compras tú (cuesta 0 y se instala como cualquier
    /// compra). Va junto al <see cref="PersonajeApoyo"/> del Doc; solo escucha su evento Usado.
    /// </summary>
    public class RegaloImplanteDoc : MonoBehaviour
    {
        /// <summary>Lo que dice el Doc (las pruebas lo leen de aquí en vez de copiar el texto).</summary>
        public const string TextoFrase = "Elige tu prótesis, te regalo la de piernas. Siéntate en la silla.";

        [Tooltip("El Doc (su PersonajeApoyo).")]
        [SerializeField] private PersonajeApoyo doc;
        [Tooltip("El implante que ofrece gratis (EXO DE PIERNAS I); su precio es 0 en el catálogo.")]
        [SerializeField] private DatosImplante regalo;

        /// <summary>Lo que dice el Doc.</summary>
        public string Frase => TextoFrase;

        /// <summary>El implante que ofrece gratis.</summary>
        public DatosImplante Regalo => regalo;

        /// <summary>True si ya dijo la frase en esta partida (para las pruebas).</summary>
        public bool Avisado { get; private set; }

        public void Configurar(PersonajeApoyo personaje, DatosImplante implante)
        {
            doc = personaje;
            regalo = implante;
        }

        private void Awake()
        {
            if (doc == null)
            {
                doc = GetComponent<PersonajeApoyo>();
            }
            if (doc == null || regalo == null)
            {
                Debug.LogError("[Progresion] RegaloImplanteDoc sin Doc o sin implante en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (doc != null)
            {
                doc.Usado += AlUsar;
            }
        }

        private void OnDisable()
        {
            if (doc != null)
            {
                doc.Usado -= AlUsar;
            }
        }

        private void AlUsar(string id)
        {
            ServicioImplantes servicio = Registro<ServicioImplantes>.Actual;
            if (servicio == null || servicio.Cantidad > 0)
            {
                return;
            }
            Avisado = true;
            doc.Decir(TextoFrase);
        }
    }
}
