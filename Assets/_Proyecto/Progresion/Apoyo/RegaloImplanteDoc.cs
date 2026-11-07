using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// T-864 (playtest 6-nov tarde): al empezar el juego EL DOC te regala un implante. La primera vez que hablas con él sin ningún implante, te
    /// instala gratis <see cref="regalo"/> (<see cref="ServicioImplantes.Regalar"/>) y te lo dice. Como el implante se guarda con la partida, no se
    /// repite. Va junto al <see cref="PersonajeApoyo"/> del Doc; solo escucha su evento Usado.
    /// </summary>
    public class RegaloImplanteDoc : MonoBehaviour
    {
        [Tooltip("El Doc (su PersonajeApoyo).")]
        [SerializeField] private PersonajeApoyo doc;
        [Tooltip("El implante que regala.")]
        [SerializeField] private DatosImplante regalo;
        [Tooltip("Lo que dice al regalarlo.")]
        [SerializeField] private string frase = "Toma, mijo, va por la casa: te puse un exo de piernas. Para lo demás, siéntate en la silla.";

        /// <summary>True si ya lo regaló en esta partida (para las pruebas).</summary>
        public bool Regalado { get; private set; }

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
            if (Regalado || servicio == null || servicio.Cantidad > 0)
            {
                return;
            }
            if (servicio.Regalar(regalo) == ResultadoImplante.Instalado)
            {
                Regalado = true;
                doc.Decir(frase);
            }
        }
    }
}
