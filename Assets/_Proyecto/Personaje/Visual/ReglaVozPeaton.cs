using System;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-voces-personajes-23nov: la voz de un peatón (Assets/Arte/Audio/19_Voces/Peatones/NPC_VOZn_&lt;MOMENTO&gt;_NN). Hay tres voces de hombre (1 normal,
    /// 2 grave, 3 aguda): a cada peatón le toca UNA por una semilla estable (su número de instancia) y la usa siempre. Una instancia lleva el estado de un
    /// peatón: una sola frase a la vez (<see cref="SegundosFrase"/>) y un enfriamiento por momento, más largo en lo ambiental (saludo, pregón, risa).
    /// Las mujeres y los robots no tienen voz grabada todavía (<see cref="UsaVozGrabada"/>). Lógica pura.
    /// </summary>
    public sealed class ReglaVozPeaton
    {
        public const int Voces = 3;
        public const string Prefijo = "NPC_VOZ";

        /// <summary>Segundos que un peatón queda "hablando" tras una frase: mientras, no dice otra.</summary>
        public const float SegundosFrase = 2.2f;

        /// <summary>Segundos que lo ocupa su quejido de dolor (NPC_H_DOLOR), para no pisarlo con otra frase.</summary>
        public const float SegundosDolor = 0.7f;

        /// <summary>Segundos mínimos entre dos frases ambientales de cualquier peatón (para que la calle no sea un coro).</summary>
        public const float SegundosEntreAmbientalesDeTodos = 1.5f;

        /// <summary>Distancia (m) al jugador por encima de la cual no se pide la voz (no se oiría).</summary>
        public const float DistanciaMaxima = 25f;

        /// <summary>El jugador a menos de esto (m) lo saluda al pasar.</summary>
        public const float DistanciaSaludo = 3f;

        /// <summary>Un vendedor pregona o un corro se ríe solo con el jugador a menos de esto (m).</summary>
        public const float DistanciaAmbiente = 14f;

        /// <summary>Radio (m) alrededor de un atropello en el que los peatones gritan de miedo.</summary>
        public const float RadioMiedoAtropello = 15f;

        private static readonly string[] Sufijos = { "MIEDO", "ENOJO", "POLICIA", "SALUDO", "VENDEDOR", "RISA" };
        private static readonly float[] Enfriamientos = { 4f, 5f, 10f, 30f, 15f, 12f };

        private readonly float[] ultimo = new float[Sufijos.Length];
        private float ocupadoHasta;

        public ReglaVozPeaton()
        {
            Reiniciar();
        }

        /// <summary>La voz (1 a 3) que le toca a la semilla; siempre la misma para la misma semilla.</summary>
        public static int VozPara(int semilla)
        {
            uint h = unchecked((uint)semilla * 2654435761u);
            h ^= h >> 16;
            return (int)(h % Voces) + 1;
        }

        /// <summary>El ID del catálogo: NPC_VOZ{voz}_{MOMENTO} (la toma _NN la elige el catálogo sin repetir la anterior).</summary>
        public static string Id(int voz, MomentoVozPeaton momento)
        {
            int v = Math.Max(1, Math.Min(Voces, voz));
            return Prefijo + v + "_" + Sufijos[(int)momento];
        }

        /// <summary>Saludo, pregón y risa: esperan además el silencio de todos los peatones.</summary>
        public static bool EsAmbiental(MomentoVozPeaton momento) =>
            momento == MomentoVozPeaton.Saludo || momento == MomentoVozPeaton.Vendedor || momento == MomentoVozPeaton.Risa;

        /// <summary>Segundos mínimos entre dos frases del mismo momento del mismo peatón.</summary>
        public static float Enfriamiento(MomentoVozPeaton momento) => Enfriamientos[(int)momento];

        /// <summary>Solo los hombres tienen voz grabada (el resto conserva su sonido de antes).</summary>
        public static bool UsaVozGrabada(GeneroNpc genero) => genero == GeneroNpc.Hombre;

        private static readonly string[] NoCiviles = { "guardia", "policia", "nova", "skull", "pandillero", "maton", "jefe" };

        /// <summary>False si el nombre es de un guardia, policía o pandillero: ellos tienen sus propias voces, no las de los civiles.</summary>
        public static bool EsCivil(string nombre)
        {
            string n = nombre ?? string.Empty;
            foreach (string parte in NoCiviles)
            {
                if (n.IndexOf(parte, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Si toca una frase ambiental de cualquier peatón: pasó el silencio común desde la última.</summary>
        public static bool TocaAmbientalDeTodos(float ahora, float ultimaDeTodos) => ahora - ultimaDeTodos >= SegundosEntreAmbientalesDeTodos;

        /// <summary>True si el peatón no está hablando.</summary>
        public bool Libre(float ahora) => ahora >= ocupadoHasta;

        /// <summary>Si puede decir ese momento ahora (libre y fuera del enfriamiento). Si sí, lo apunta y queda ocupado <see cref="SegundosFrase"/>.</summary>
        public bool Intentar(float ahora, MomentoVozPeaton momento)
        {
            int i = (int)momento;
            if (ahora < ocupadoHasta || ahora - ultimo[i] < Enfriamientos[i])
            {
                return false;
            }
            ultimo[i] = ahora;
            ocupadoHasta = ahora + SegundosFrase;
            return true;
        }

        /// <summary>Lo deja ocupado al menos <paramref name="segundos"/> (su quejido de dolor u otro sonido de la boca).</summary>
        public void Ocupar(float ahora, float segundos)
        {
            ocupadoHasta = Math.Max(ocupadoHasta, ahora + segundos);
        }

        public void Reiniciar()
        {
            for (int i = 0; i < ultimo.Length; i++)
            {
                ultimo[i] = float.NegativeInfinity;
            }
            ocupadoHasta = float.NegativeInfinity;
        }
    }
}
