using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// El punto de una mejora de la guarida (T-448): con E se compra el siguiente nivel del armero, el garaje o la cama.
    /// El texto dice qué mejora es, su nivel y cuánto cuesta el siguiente.
    /// </summary>
    public class GuaridaPunto : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ServicioGuarida servicio;
        [SerializeField] private string idMejora = string.Empty;

        private bool registrado;

        public string IdMejora => idMejora;
        public Vector3 Posicion => transform.position;

        public string Texto
        {
            get
            {
                if (servicio == null || servicio.Datos == null || servicio.Datos.IndiceDe(idMejora) < 0)
                {
                    return string.Empty;
                }
                string nombre = servicio.Nombre(idMejora);
                int nivel = servicio.Nivel(idMejora);
                int precio = servicio.PrecioSiguiente(idMejora);
                if (precio <= 0)
                {
                    return nombre + " · NIVEL " + nivel + " (MÁX)";
                }
                return (nivel == 0 ? "ESTRENAR " : "MEJORAR ") + nombre + " (NIVEL " + (nivel + 1) + ") · " + Moneda.Texto(precio);
            }
        }

        public bool PuedeUsar => servicio != null && servicio.PrecioSiguiente(idMejora) > 0;

        public void Usar() => servicio?.Comprar(idMejora);

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
        }
    }
}
