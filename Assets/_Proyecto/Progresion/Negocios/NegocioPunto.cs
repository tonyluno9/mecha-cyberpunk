using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>El punto de un negocio en la calle (T-429): con E lo tomas o lo mejoras, si la zona es tuya y alcanza el dinero.</summary>
    public class NegocioPunto : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ServicioNegocios servicio;
        [SerializeField] private string idNegocio = string.Empty;
        private bool registrado;

        public string IdNegocio => idNegocio;
        public Vector3 Posicion => transform.position;

        public string Texto
        {
            get
            {
                int i = servicio != null && servicio.Datos != null ? servicio.Datos.IndiceDe(idNegocio) : -1;
                if (i < 0)
                {
                    return string.Empty;
                }
                string nombre = servicio.Definicion(i).nombre;
                int nivel = servicio.Nivel(idNegocio);
                if (!servicio.ZonaTuya(idNegocio))
                {
                    return nombre + " · CONQUISTA LA ZONA";
                }
                int precio = servicio.PrecioSiguiente(idNegocio);
                if (precio <= 0)
                {
                    return nombre + " · NIVEL " + nivel + " (MÁX) · " + Moneda.Texto(servicio.IngresoDia(idNegocio)) + " POR DÍA";
                }
                return (nivel == 0 ? "TOMAR " : "MEJORAR ") + nombre + " · " + Moneda.Texto(precio);
            }
        }

        public bool PuedeUsar => servicio != null && servicio.ZonaTuya(idNegocio) && servicio.PrecioSiguiente(idNegocio) > 0;

        public void Usar() => servicio?.Comprar(idNegocio);

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
