using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Un arma tirada en el piso (T-406): con E la recoges y pasa a tu inventario en su nivel base (0), así <see cref="ArsenalJugador"/> la reconoce.
    /// Se registra como <see cref="IInteractuable"/> mientras está en la escena.
    /// </summary>
    public class ArmaEnPiso : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ArmaAPie arma = ArmaAPie.Pistola;
        [SerializeField] private string nombre = "la pistola";

        private bool tomada;

        public ArmaAPie Arma => arma;
        public bool FueTomada => tomada;
        public Vector3 Posicion => transform.position;
        public string Texto => "Recoger " + nombre;
        public bool PuedeUsar => !tomada;

        /// <summary>Se dispara una vez al recogerla.</summary>
        public event Action<ArmaEnPiso> Tomada;

        private void OnEnable() => Registro<IInteractuable>.Agregar(this);

        private void OnDisable() => Registro<IInteractuable>.Quitar(this);

        public void Usar()
        {
            if (tomada)
            {
                return;
            }
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosArma datos = arsenal != null && arsenal.Catalogo != null ? arsenal.Catalogo.Buscar(arma, 0) : null;
            if (datos == null || inventario == null)
            {
                Debug.LogWarning("[Personaje] ArmaEnPiso: falta el arsenal, el catálogo o el inventario; no se puede recoger.", this);
                return;
            }
            inventario.Agregar(datos, 1);
            tomada = true;
            Tomada?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
