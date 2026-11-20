using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>
    /// T-1475: la voz del motor de un vehículo (una sola por carro). Al encender suena el ARRANQUE una vez y, tras un instante, el bucle de
    /// RALENTÍ siguiendo al vehículo; el tono sube con la rapidez (suavizado, y solo se manda si cambió); el ACELERÓN suena al pisar a fondo
    /// desde lento; al apagar se detiene el bucle y suena el APAGAR una vez. No es MonoBehaviour: el dueño la actualiza.
    /// </summary>
    public sealed class SonidoMotorCarro
    {
        public const float SegundosArranque = 0.8f;
        public const float TasaSuavizado = 4f;
        public const float CambioMinimoTono = 0.01f;
        /// <summary>T-1488: con marchas simuladas el tono sigue a las rpm más rápido, para que se oiga el cambio.</summary>
        public const float TasaSuavizadoMarchas = 10f;

        private readonly GrupoSonidoVehiculo grupo;
        private readonly float rapidezMaxima;
        private Transform sigue;
        private float segundosEncendido;
        private float tonoActual;
        private float tonoEnviado;
        private float pedalAnterior;
        private float segundosDesdeAceleron = ReglasSonidoVehiculoT1475.AceleronSegundosMinimos;
        private readonly SimuladorMarchas marchas;

        public SonidoMotorCarro(GrupoSonidoVehiculo grupo, float rapidezMaxima, int marchasSimuladas = 0)
        {
            marchas = marchasSimuladas > 0 ? new SimuladorMarchas(marchasSimuladas) : null;
            this.grupo = grupo;
            this.rapidezMaxima = rapidezMaxima;
            Bucle = Sonidos.SinBucle;
        }

        public bool Encendido { get; private set; }

        /// <summary>Identificador del bucle de ralentí (<see cref="Sonidos.SinBucle"/> si no suena).</summary>
        public int Bucle { get; private set; }

        public float TonoActual => tonoActual;

        /// <summary>T-1488: marcha simulada actual (0 si no hay marchas simuladas).</summary>
        public int Marcha => marchas != null ? marchas.Marcha : 0;

        /// <summary>T-1488: rpm simuladas (0 a 1); 0 si no hay marchas simuladas.</summary>
        public float Rpm => marchas != null ? marchas.Rpm : 0f;

        /// <summary>Enciende el motor (si ya estaba encendido no hace nada). <paramref name="conArranque"/> false: solo el bucle.</summary>
        public void Encender(Transform sigueA, bool conArranque)
        {
            if (Encendido)
            {
                return;
            }
            Encendido = true;
            sigue = sigueA;
            segundosEncendido = conArranque ? 0f : SegundosArranque;
            tonoActual = ReglasSonidoVehiculoT1475.TonoObjetivo(0f, rapidezMaxima, grupo.FactorTono);
            tonoEnviado = -1f;
            pedalAnterior = 0f;
            if (conArranque && sigue != null)
            {
                Sonidos.ReproducirEn(grupo.Arranque, sigue.position);
            }
        }

        /// <summary>Un paso: arranca el bucle cuando toca, sigue el tono y dispara el acelerón.</summary>
        public void Actualizar(float rapidez, float pedal, float dt)
        {
            if (!Encendido)
            {
                return;
            }
            segundosEncendido += dt;
            segundosDesdeAceleron += dt;
            if (Bucle == Sonidos.SinBucle)
            {
                if (segundosEncendido < SegundosArranque)
                {
                    pedalAnterior = pedal;
                    return;
                }
                Bucle = Sonidos.ReproducirBucle(grupo.Ralenti, sigue);
            }
            float objetivo;
            float tasa = TasaSuavizado;
            if (marchas != null)
            {
                // T-1488: el tono sigue a las rpm de la marcha simulada: sube dentro de cada marcha y cae al cambiar.
                float rpm = marchas.Actualizar(rapidez, rapidezMaxima, dt);
                objetivo = (ReglasSonidoVehiculoT1475.TonoReposo + (ReglasSonidoVehiculoT1475.TonoMaximo - ReglasSonidoVehiculoT1475.TonoReposo) * rpm)
                    * grupo.FactorTono;
                tasa = TasaSuavizadoMarchas;
            }
            else
            {
                objetivo = ReglasSonidoVehiculoT1475.TonoObjetivo(rapidez, rapidezMaxima, grupo.FactorTono);
            }
            tonoActual = ReglasSonidoVehiculoT1475.Suavizar(tonoActual, objetivo, dt, tasa);
            if (Bucle != Sonidos.SinBucle && Mathf.Abs(tonoActual - tonoEnviado) >= CambioMinimoTono)
            {
                tonoEnviado = tonoActual;
                Sonidos.FijarTono(Bucle, tonoActual);
            }
            if (sigue != null && ReglasSonidoVehiculoT1475.Aceleron(pedalAnterior, pedal, rapidez, rapidezMaxima, segundosDesdeAceleron))
            {
                segundosDesdeAceleron = 0f;
                Sonidos.ReproducirEn(ReglasSonidoVehiculoT1475.IdAceleron, sigue.position);
            }
            pedalAnterior = pedal;
        }

        /// <summary>Apaga el motor: detiene el bucle y suena el APAGAR una vez (si <paramref name="conSonido"/>).</summary>
        public void Apagar(bool conSonido)
        {
            if (!Encendido)
            {
                return;
            }
            Encendido = false;
            if (Bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(Bucle);
                Bucle = Sonidos.SinBucle;
            }
            if (conSonido && sigue != null)
            {
                Sonidos.ReproducirEn(grupo.Apagar, sigue.position);
            }
        }
    }

    /// <summary>T-1475: la alarma de un carro robado: un bucle que suena unos segundos y se corta solo. Una sola voz.</summary>
    public sealed class AlarmaCarro
    {
        public const float SegundosAlarma = 8f;

        private int bucle = Sonidos.SinBucle;
        private float restante;

        public bool Suena => bucle != Sonidos.SinBucle;

        public void Iniciar(Transform sigue)
        {
            if (Suena)
            {
                return;
            }
            bucle = Sonidos.ReproducirBucle(ReglasSonidoVehiculoT1475.IdAlarma, sigue);
            restante = SegundosAlarma;
        }

        public void Actualizar(float dt)
        {
            if (!Suena)
            {
                return;
            }
            restante -= dt;
            if (restante <= 0f)
            {
                Detener();
            }
        }

        public void Detener()
        {
            if (bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucle);
                bucle = Sonidos.SinBucle;
            }
        }
    }
}
