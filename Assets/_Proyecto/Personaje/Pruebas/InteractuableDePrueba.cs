using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Doble de prueba de un interactuable de Alan o del taller (solo para escenas y pruebas de Tony). Se registra en
    /// <c>Registro&lt;IInteractuable&gt;</c>, usa la posición de su transform y cuenta cuántas veces lo usaron.
    /// </summary>
    public class InteractuableDePrueba : MonoBehaviour, IInteractuable
    {
        [SerializeField] private string texto = "Probar interactuable";
        [SerializeField] private bool puedeUsar = true;

        /// <summary>Cuántas veces se llamó a Usar (para las pruebas).</summary>
        public int Usos { get; private set; }

        public Vector3 Posicion => transform.position;

        public string Texto
        {
            get => texto;
            set => texto = value;
        }

        public bool PuedeUsar
        {
            get => puedeUsar;
            set => puedeUsar = value;
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IInteractuable>.Quitar(this);
        }

        public void Usar()
        {
            Usos++;
        }
    }
}
