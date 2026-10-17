using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>
    /// La apuesta del jugador en la escena (T-144): envuelve <see cref="ModeloApuestas"/> y se registra como
    /// <see cref="IApuestasPelea"/> (y como <c>Registro&lt;ServicioApuestas&gt;</c> para quien anuncia y resuelve: Tony).
    /// Busca la cartera y el inventario por registro. No se guarda (D-tony-80). Va en ServiciosEconomia.prefab.
    /// </summary>
    public class ServicioApuestas : MonoBehaviour, IApuestasPelea
    {
        private sealed class AlmacenInventario : IAlmacenObjetos
        {
            private readonly ServicioInventario inventario;

            public AlmacenInventario(ServicioInventario inventario) => this.inventario = inventario;

            public bool PuedeAgregar(DatosObjeto objeto, int n) => inventario.PuedeAgregar(objeto, n);

            public int Agregar(DatosObjeto objeto, int n) => inventario.Agregar(objeto, n);
        }

        private readonly ModeloApuestas modelo = new ModeloApuestas();
        private bool avisoSinCartera;

        public bool Abierta => modelo.Abierta;
        public string Rival => modelo.Rival;
        public float Momio => modelo.Momio;
        public int ApuestaMaxima => modelo.ApuestaMaxima;
        public int Apostado => modelo.Apostado;

        /// <summary>Hay una pelea anunciada sin resolver ni devolver (abierta o ya empezada).</summary>
        public bool Pendiente => modelo.Pendiente;

        public event Action Cambio
        {
            add => modelo.Cambio += value;
            remove => modelo.Cambio -= value;
        }

        public event Action<int> Resuelta
        {
            add => modelo.Resuelta += value;
            remove => modelo.Resuelta -= value;
        }

        private void OnEnable()
        {
            Registro<IApuestasPelea>.Agregar(this);
            Registro<ServicioApuestas>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IApuestasPelea>.Quitar(this);
            Registro<ServicioApuestas>.Quitar(this);
        }

        public bool PuedeApostar(int monto) => modelo.PuedeApostar(monto, Registro<ICartera>.Actual);

        public bool Apostar(int monto) => modelo.Apostar(monto, Registro<ICartera>.Actual);

        public bool Cancelar() => modelo.Cancelar(Registro<ICartera>.Actual);

        /// <summary>Anuncia la próxima pelea. False si no hay datos o queda una apuesta sin resolver.</summary>
        public bool Anunciar(string alias, DatosApuestaPelea datos) => modelo.Anunciar(alias, datos);

        /// <summary>Empezó la pelea: ya no se apuesta ni se cancela.</summary>
        public void Cerrar() => modelo.Cerrar();

        /// <summary>Termina la pelea. Devuelve true y el cambio neto si había apuesta.</summary>
        public bool Resolver(bool gano, out int neto)
        {
            int resultado = 0;
            bool hubo = false;
            Action<int> alAvisar = n =>
            {
                resultado = n;
                hubo = true;
            };
            modelo.Resuelta += alAvisar;
            ICartera cartera = Registro<ICartera>.Actual;
            AvisarSiFaltaCartera(cartera);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            modelo.Resolver(gano, cartera, inventario != null ? new AlmacenInventario(inventario) : null);
            modelo.Resuelta -= alAvisar;
            neto = resultado;
            return hubo;
        }

        /// <summary>La pelea no terminó: devuelve lo apostado.</summary>
        public void Devolver()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            AvisarSiFaltaCartera(cartera);
            modelo.Devolver(cartera);
        }

        private void AvisarSiFaltaCartera(ICartera cartera)
        {
            if (cartera == null && modelo.Apostado > 0 && !avisoSinCartera)
            {
                avisoSinCartera = true;
                Debug.LogWarning("[Economia] ServicioApuestas no encontró la cartera; la apuesta no se paga ni se devuelve.", this);
            }
        }
    }
}
