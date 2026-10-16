using System;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Tiempos del parpadeo de un neón viejo: prendido un rato y luego una ráfaga de apagones cortos e irregulares.
    /// Todos los tiempos en segundos y los apagones son conteos enteros.
    /// </summary>
    public readonly struct ParametrosParpadeo
    {
        public const float EncendidoMinimoPorDefecto = 1.5f;
        public const float EncendidoMaximoPorDefecto = 4.5f;
        public const int ApagonesMinimosPorDefecto = 1;
        public const int ApagonesMaximosPorDefecto = 4;
        public const float ApagadoMinimoPorDefecto = 0.08f;
        public const float ApagadoMaximoPorDefecto = 0.3f;
        public const float EntreMinimoPorDefecto = 0.06f;
        public const float EntreMaximoPorDefecto = 0.16f;

        public readonly float EncendidoMinimo;
        public readonly float EncendidoMaximo;
        public readonly int ApagonesMinimos;
        public readonly int ApagonesMaximos;
        public readonly float ApagadoMinimo;
        public readonly float ApagadoMaximo;
        public readonly float EntreMinimo;
        public readonly float EntreMaximo;

        public ParametrosParpadeo(float encendidoMinimo, float encendidoMaximo, int apagonesMinimos, int apagonesMaximos,
            float apagadoMinimo, float apagadoMaximo, float entreMinimo, float entreMaximo)
        {
            EncendidoMinimo = encendidoMinimo;
            EncendidoMaximo = encendidoMaximo;
            ApagonesMinimos = apagonesMinimos;
            ApagonesMaximos = apagonesMaximos;
            ApagadoMinimo = apagadoMinimo;
            ApagadoMaximo = apagadoMaximo;
            EntreMinimo = entreMinimo;
            EntreMaximo = entreMaximo;
        }

        /// <summary>Los valores de neón viejo con que se afinó el barrio.</summary>
        public static ParametrosParpadeo PorDefecto => new ParametrosParpadeo(EncendidoMinimoPorDefecto, EncendidoMaximoPorDefecto,
            ApagonesMinimosPorDefecto, ApagonesMaximosPorDefecto, ApagadoMinimoPorDefecto, ApagadoMaximoPorDefecto,
            EntreMinimoPorDefecto, EntreMaximoPorDefecto);
    }

    /// <summary>
    /// Lógica pura del parpadeo de un neón viejo (sin Unity): prendido de 1.5 a 4.5 s, luego una ráfaga de 1 a 4 apagones de
    /// 0.08 a 0.3 s separados por 0.06 a 0.16 s prendidos. El tiempo entra como <c>dt</c> y el azar se inyecta
    /// (<c>Func&lt;float&gt;</c> devuelve valores en [0, 1)): con el mismo azar da la misma secuencia.
    /// </summary>
    public class CalculoParpadeo
    {
        private enum Fase
        {
            Encendido,
            Apagon,
            Entre
        }

        private readonly Func<float> azar;
        private readonly ParametrosParpadeo parametros;
        private Fase fase;
        private float restante;
        private int apagonesPendientes;

        public CalculoParpadeo(Func<float> azar, ParametrosParpadeo parametros)
        {
            this.azar = azar ?? throw new ArgumentNullException(nameof(azar));
            this.parametros = parametros;
            EmpezarEncendido();
        }

        /// <summary>True si el neón está prendido ahora.</summary>
        public bool EstaEncendido => fase != Fase.Apagon;

        /// <summary>Avanza <paramref name="dt"/> segundos (puede cruzar varias fases) y devuelve si queda prendido.</summary>
        public bool Avanzar(float dt)
        {
            if (dt < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(dt), "dt no puede ser negativo");
            }
            while (dt >= restante)
            {
                dt -= restante;
                CambiarFase();
            }
            restante -= dt;
            return EstaEncendido;
        }

        private float Entre(float minimo, float maximo)
        {
            return minimo + (maximo - minimo) * azar();
        }

        private void EmpezarEncendido()
        {
            fase = Fase.Encendido;
            restante = Entre(parametros.EncendidoMinimo, parametros.EncendidoMaximo);
        }

        private void EmpezarApagon()
        {
            fase = Fase.Apagon;
            restante = Entre(parametros.ApagadoMinimo, parametros.ApagadoMaximo);
        }

        private void CambiarFase()
        {
            switch (fase)
            {
                case Fase.Encendido:
                    apagonesPendientes = parametros.ApagonesMinimos
                        + (int)(azar() * (parametros.ApagonesMaximos - parametros.ApagonesMinimos + 1));
                    apagonesPendientes = Math.Min(Math.Max(apagonesPendientes, parametros.ApagonesMinimos), parametros.ApagonesMaximos);
                    EmpezarApagon();
                    break;
                case Fase.Apagon:
                    apagonesPendientes--;
                    if (apagonesPendientes > 0)
                    {
                        fase = Fase.Entre;
                        restante = Entre(parametros.EntreMinimo, parametros.EntreMaximo);
                    }
                    else
                    {
                        EmpezarEncendido();
                    }
                    break;
                default:
                    EmpezarApagon();
                    break;
            }
        }
    }
}
