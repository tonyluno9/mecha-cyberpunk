using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Economia
{
    /// <summary>
    /// La apuesta del jugador sobre su próxima pelea (C# puro, T-144, reglas de D-tony-80): una por pelea, se cobra al apostar,
    /// se paga según el momio si gana y se pierde si pierde. La pieza en juego va al inventario si apostó lo suficiente.
    /// </summary>
    public class ModeloApuestas
    {
        // Momio de respaldo mientras no se ha anunciado ninguna pelea (el contrato pide que sea mayor que 0).
        private const float MomioNeutro = 1f;

        private DatosApuestaPelea datos;
        private string rival = string.Empty;
        private bool anunciada;
        private bool cerrada;
        private int apostado;

        public bool Abierta => anunciada && !cerrada;
        public string Rival => Abierta ? rival : string.Empty;
        public float Momio => datos != null ? datos.Momio : MomioNeutro;
        public int ApuestaMaxima => datos != null ? datos.ApuestaMaxima : 0;
        public int Apostado => apostado;

        /// <summary>Hay una pelea anunciada que todavía no se resuelve ni se devuelve (abierta o cerrada).</summary>
        public bool Pendiente => anunciada;

        public event Action Cambio;
        public event Action<int> Resuelta;

        /// <summary>
        /// Anuncia la próxima pelea. False (sin cambiar nada) si no hay datos o si todavía hay una apuesta sin resolver.
        /// </summary>
        public bool Anunciar(string alias, DatosApuestaPelea datosPelea)
        {
            if (datosPelea == null || apostado > 0)
            {
                return false;
            }
            datos = datosPelea;
            rival = alias ?? string.Empty;
            anunciada = true;
            cerrada = false;
            Cambio?.Invoke();
            return true;
        }

        public bool PuedeApostar(int monto, ICartera cartera) =>
            Abierta && apostado == 0 && monto > 0 && monto <= ApuestaMaxima && cartera != null && cartera.PuedePagar(monto);

        public bool Apostar(int monto, ICartera cartera)
        {
            if (!PuedeApostar(monto, cartera) || !cartera.IntentarPagar(monto, MotivoDinero.Apuesta))
            {
                return false;
            }
            apostado = monto;
            Cambio?.Invoke();
            return true;
        }

        public bool Cancelar(ICartera cartera)
        {
            if (!Abierta || apostado == 0 || cartera == null)
            {
                return false;
            }
            int monto = apostado;
            apostado = 0;
            cartera.Abonar(monto, MotivoDinero.Apuesta);
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Empezó la pelea: ya no se apuesta ni se cancela.</summary>
        public void Cerrar()
        {
            if (!anunciada || cerrada)
            {
                return;
            }
            cerrada = true;
            Cambio?.Invoke();
        }

        /// <summary>
        /// Termina la pelea. Ganando abona lo apostado más lo apostado por el momio y, si apostó lo suficiente, entrega la pieza
        /// (o paga su valor de venta si no cabe); perdiendo no devuelve nada. Avisa <see cref="Resuelta"/> solo si había apuesta.
        /// </summary>
        public void Resolver(bool gano, ICartera cartera, IAlmacenObjetos inventario)
        {
            if (!anunciada)
            {
                return;
            }
            int monto = apostado;
            DatosApuestaPelea reglas = datos;
            Limpiar();
            if (monto == 0)
            {
                Cambio?.Invoke();
                return;
            }
            int neto = -monto;
            if (gano)
            {
                int ganancia = (int)Math.Round(monto * (double)reglas.Momio, MidpointRounding.AwayFromZero);
                neto = ganancia;
                cartera?.Abonar(monto + ganancia, MotivoDinero.Apuesta);
                EntregarPieza(reglas, monto, cartera, inventario);
            }
            Cambio?.Invoke();
            Resuelta?.Invoke(neto);
        }

        /// <summary>La pelea no terminó: devuelve lo apostado y deja todo cerrado. No avisa <see cref="Resuelta"/>.</summary>
        public void Devolver(ICartera cartera)
        {
            if (!anunciada)
            {
                return;
            }
            int monto = apostado;
            Limpiar();
            if (monto > 0)
            {
                cartera?.Abonar(monto, MotivoDinero.Apuesta);
            }
            Cambio?.Invoke();
        }

        private static void EntregarPieza(DatosApuestaPelea reglas, int monto, ICartera cartera, IAlmacenObjetos inventario)
        {
            DatosObjeto pieza = reglas.PiezaEnJuego;
            if (pieza == null || monto < reglas.ApuestaMinimaPorPieza)
            {
                return;
            }
            if (inventario != null && inventario.PuedeAgregar(pieza, 1))
            {
                inventario.Agregar(pieza, 1);
            }
            else if (pieza.ValorVenta > 0)
            {
                cartera?.Abonar(pieza.ValorVenta, MotivoDinero.Venta);
            }
        }

        private void Limpiar()
        {
            anunciada = false;
            cerrada = false;
            apostado = 0;
            rival = string.Empty;
        }
    }
}
