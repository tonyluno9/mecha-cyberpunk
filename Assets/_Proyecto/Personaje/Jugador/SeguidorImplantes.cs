using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Sigue al <see cref="IImplantes"/> registrado (T-192), venga antes o después de quien lo usa. Sin servicio los efectos son
    /// <see cref="EfectosImplantes.Ninguno"/> y todos los niveles 0. Quien lo usa llama <see cref="Activar"/> en OnEnable y
    /// <see cref="Desactivar"/> en OnDisable.
    /// </summary>
    public sealed class SeguidorImplantes
    {
        private IImplantes actual;
        private bool activo;

        /// <summary>Se avisa cuando cambian los implantes o llega o se va el servicio.</summary>
        public event Action Cambio;

        public EfectosImplantes Efectos => actual != null ? actual.Efectos : EfectosImplantes.Ninguno;

        public int Nivel(EspacioImplante espacio) => actual != null ? actual.Nivel(espacio) : 0;

        public void Activar()
        {
            if (activo) return;
            activo = true;
            Registro<IImplantes>.Agregado += AlLlegar;
            Registro<IImplantes>.Quitado += AlIrse;
            Conectar(Registro<IImplantes>.Actual);
        }

        public void Desactivar()
        {
            if (!activo) return;
            activo = false;
            Registro<IImplantes>.Agregado -= AlLlegar;
            Registro<IImplantes>.Quitado -= AlIrse;
            Desconectar();
        }

        private void AlLlegar(IImplantes servicio) => Conectar(servicio);

        private void AlIrse(IImplantes servicio)
        {
            if (ReferenceEquals(servicio, actual)) Conectar(Registro<IImplantes>.Actual);
        }

        private void Conectar(IImplantes servicio)
        {
            if (ReferenceEquals(servicio, actual)) return;
            Desconectar();
            actual = servicio;
            if (actual != null) actual.Cambio += AlCambiar;
            Cambio?.Invoke();
        }

        private void Desconectar()
        {
            if (actual != null) actual.Cambio -= AlCambiar;
            actual = null;
        }

        private void AlCambiar() => Cambio?.Invoke();
    }
}
