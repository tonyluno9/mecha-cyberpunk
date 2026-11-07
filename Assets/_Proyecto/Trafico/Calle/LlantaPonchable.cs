using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-717: la franja baja (las ruedas) del carro del asaltante. Un disparo que la toca le ponchaba una llanta al carro (<see cref="CarroTrafico.Ponchar"/>).
    /// Es un objeto inerte: no sangra ni cuenta como persona.
    /// </summary>
    public class LlantaPonchable : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        private CarroTrafico carro;

        public CarroTrafico Carro => carro;

        public void Iniciar(CarroTrafico dueno) => carro = dueno;

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (carro == null || info.Cantidad <= 0f || info.Tipo != TipoDanio.Disparo)
            {
                return ResultadoDanio.Ignorado;
            }
            carro.Ponchar();
            return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
        }
    }
}
