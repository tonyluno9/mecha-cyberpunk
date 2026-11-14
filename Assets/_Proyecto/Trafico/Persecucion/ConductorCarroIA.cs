using System;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-986: los números del conductor IA (vienen de <c>ConfigPersecucion</c>); las pruebas los arman a mano.</summary>
    public struct ParametrosConductorIA
    {
        /// <summary>Distancia de mira con el carro parado (m).</summary>
        public float DistanciaMiraMinima;
        /// <summary>Cuántos metros más se mira por cada m/s de rapidez (s).</summary>
        public float SegundosMira;
        /// <summary>Velocidad (m/s) con la que se toma una curva de 90 grados.</summary>
        public float VelocidadCurva90;
        /// <summary>Velocidad (m/s) más baja que se pide en una curva, aunque sea de vuelta en U.</summary>
        public float VelocidadMinimaCurva;
        /// <summary>Qué parte de la aceleración lateral del carro se usa al tomar una curva (0 a 1).</summary>
        public float FraccionAgarre;
        /// <summary>Desaceleración (m/s2) con la que se planea frenar antes de una curva o de la meta; menor que la que el carro puede.</summary>
        public float DesaceleracionPlaneada;
        /// <summary>Error de velocidad (m/s) con el que se pisa el acelerador a fondo.</summary>
        public float RangoAceleracion;
        /// <summary>Error de velocidad (m/s) con el que se pisa el freno a fondo.</summary>
        public float RangoFreno;
        /// <summary>Si sobra tanta velocidad (m/s) se frena a fondo de golpe.</summary>
        public float ErrorFrenoFuerte;
        /// <summary>Error de velocidad (m/s) que se deja pasar sin tocar los pedales.</summary>
        public float ZonaMuerta;
        /// <summary>Rapidez (m/s) bajo la cual el carro cuenta como quieto.</summary>
        public float RapidezQuieta;
        /// <summary>Segundos quieto queriendo moverse para considerarse atorado.</summary>
        public float SegundosParaAtoro;
        /// <summary>Segundos que da reversa al atorarse.</summary>
        public float SegundosReversa;
        /// <summary>Volante (0 a 1) con el que da reversa si no hay hacia dónde girar.</summary>
        public float VolanteReversaMinimo;
        /// <summary>Ángulo (grados) al objetivo desde el cual un giro cuenta como muy cerrado.</summary>
        public float AnguloFrenoMano;
        /// <summary>Rapidez (m/s) desde la cual se usa el freno de mano en un giro muy cerrado.</summary>
        public float VelocidadFrenoMano;
    }

    /// <summary>
    /// T-986: el conductor IA de una patrulla. Lógica pura (sin MonoBehaviour): recibe la pose y la velocidad del carro, el punto al que mirar y la velocidad que
    /// quiere llevar, y devuelve la <see cref="EntradaCarro"/> que el carro recibe en vez del teclado.
    /// Volante por persecución pura (normalizado al ángulo máximo de las ruedas a esa rapidez), acelerador y freno por error de velocidad, y reversa si se atora.
    /// </summary>
    public sealed class ConductorCarroIA
    {
        private const float UmbralDistancia = 0.01f;
        private const float DosVeces = 2f;
        private const float CurvaturaMinima = 1e-4f;
        private readonly ParametrosManejo carro;
        private readonly ParametrosConductorIA p;
        private float segundosAtorada;
        private float segundosDeReversa;
        private int reversas;

        public ConductorCarroIA(ParametrosManejo parametrosCarro, ParametrosConductorIA parametros)
        {
            carro = parametrosCarro;
            p = parametros;
        }

        /// <summary>True mientras da reversa por haberse atorado.</summary>
        public bool EnReversa => segundosDeReversa > 0f;

        /// <summary>Cuántas veces se ha atorado y dado reversa.</summary>
        public int Reversas => reversas;

        public void Reiniciar()
        {
            segundosAtorada = 0f;
            segundosDeReversa = 0f;
        }

        /// <summary>A qué distancia (m) delante del carro conviene poner el punto de mira con esta rapidez.</summary>
        public static float DistanciaMira(in ParametrosConductorIA parametros, float rapidez)
        {
            return parametros.DistanciaMiraMinima + parametros.SegundosMira * Math.Max(0f, rapidez);
        }

        /// <summary>Velocidad (m/s) con la que se toma una curva de <paramref name="anguloGrados"/> entre dos tramos (0 recto, 90 esquina, 180 vuelta en U); nunca más que <paramref name="velocidadMaxima"/>.</summary>
        public static float VelocidadEnCurva(in ParametrosConductorIA parametros, float velocidadMaxima, float anguloGrados)
        {
            float angulo = Math.Clamp(Math.Abs(anguloGrados), 0f, 180f);
            float en90 = Math.Min(velocidadMaxima, parametros.VelocidadCurva90);
            float minima = Math.Min(en90, parametros.VelocidadMinimaCurva);
            return angulo <= 90f
                ? velocidadMaxima + (en90 - velocidadMaxima) * (angulo / 90f)
                : en90 + (minima - en90) * ((angulo - 90f) / 90f);
        }

        /// <summary>La mayor velocidad (m/s) que permite llegar a <paramref name="velocidadFinal"/> en <paramref name="distancia"/> m frenando con <paramref name="desaceleracion"/>.</summary>
        public static float VelocidadParaFrenar(float distancia, float velocidadFinal, float desaceleracion)
        {
            return (float)Math.Sqrt(velocidadFinal * velocidadFinal + DosVeces * Math.Max(0f, desaceleracion) * Math.Max(0f, distancia));
        }

        /// <summary>
        /// La entrada de este paso. <paramref name="adelante"/> es hacia dónde mira el carro, <paramref name="velocidad"/> su velocidad en el mundo,
        /// <paramref name="objetivo"/> el punto de mira y <paramref name="velocidadDeseada"/> los m/s que quiere llevar (0 para frenar y quedarse).
        /// </summary>
        public EntradaCarro Calcular(Vector3 posicion, Vector3 adelante, Vector3 velocidad, Vector3 objetivo, float velocidadDeseada, float dt)
        {
            adelante.y = 0f;
            adelante = adelante.sqrMagnitude > UmbralDistancia ? adelante.normalized : Vector3.forward;
            velocidad.y = 0f;
            float rapidez = velocidad.magnitude;
            float vAdelante = Vector3.Dot(velocidad, adelante);
            Vector3 hacia = objetivo - posicion;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            float angulo = distancia > UmbralDistancia ? Vector3.SignedAngle(adelante, hacia, Vector3.up) : 0f;
            float anguloMaximo = CalculoManejo.AnguloDireccion(carro, rapidez);

            ActualizarAtoro(rapidez, velocidadDeseada, dt);
            if (EnReversa)
            {
                float contrario = -Mathf.Clamp(angulo / anguloMaximo, -1f, 1f);
                if (Math.Abs(contrario) < p.VolanteReversaMinimo)
                {
                    contrario = (reversas % 2 == 0 ? 1f : -1f) * p.VolanteReversaMinimo;
                }
                return new EntradaCarro(0f, 1f, contrario, 0f);
            }

            // Persecución pura: el arco que pasa por el carro y por el punto de mira.
            float curvatura = distancia > UmbralDistancia ? DosVeces * Mathf.Sin(angulo * Mathf.Deg2Rad) / distancia : 0f;
            float volante = Mathf.Clamp(Mathf.Atan(curvatura * carro.DistanciaEntreEjes) * Mathf.Rad2Deg / anguloMaximo, -1f, 1f);

            // La curva que describe el volante no puede pedir más agarre del que hay.
            float limite = velocidadDeseada;
            float curvaAbs = Math.Abs(curvatura);
            if (curvaAbs > CurvaturaMinima)
            {
                limite = Math.Min(limite, Math.Max(p.VelocidadMinimaCurva, (float)Math.Sqrt(p.FraccionAgarre * carro.AceleracionLateralMaxima / curvaAbs)));
            }
            float meta = Math.Max(0f, limite);
            if (meta < p.RapidezQuieta * 0.5f)
            {
                volante = 0f; // para frenar y quedarse, derecho: así no sigue girando al detenerse
            }
            float error = meta - vAdelante;
            float acelerar = 0f;
            float frenar = 0f;
            float frenoMano = 0f;
            if (meta < p.RapidezQuieta * 0.5f)
            {
                if (vAdelante > carro.UmbralReversa + p.ZonaMuerta)
                {
                    frenar = 1f;
                }
                else
                {
                    frenoMano = 1f;
                }
            }
            else if (error > p.ZonaMuerta)
            {
                acelerar = Mathf.Clamp01(error / p.RangoAceleracion);
            }
            else if (error < -p.ZonaMuerta && vAdelante > carro.UmbralReversa + p.ZonaMuerta)
            {
                frenar = error < -p.ErrorFrenoFuerte ? 1f : Mathf.Clamp01(-error / p.RangoFreno);
            }
            if (meta >= p.RapidezQuieta * 0.5f && Math.Abs(angulo) >= p.AnguloFrenoMano && rapidez >= p.VelocidadFrenoMano)
            {
                frenoMano = 1f;
            }
            return new EntradaCarro(acelerar, frenar, volante, frenoMano);
        }

        // Quiere moverse pero lleva segundos casi quieto: da reversa un rato y vuelve a intentar.
        private void ActualizarAtoro(float rapidez, float velocidadDeseada, float dt)
        {
            if (segundosDeReversa > 0f)
            {
                segundosDeReversa -= dt;
                return;
            }
            bool atorada = velocidadDeseada > p.RapidezQuieta && rapidez < p.RapidezQuieta;
            segundosAtorada = atorada ? segundosAtorada + dt : 0f;
            if (segundosAtorada >= p.SegundosParaAtoro)
            {
                segundosAtorada = 0f;
                segundosDeReversa = p.SegundosReversa;
                reversas++;
            }
        }
    }
}
