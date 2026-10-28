using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Clinica
{
    /// <summary>
    /// Las decisiones del doctor de la clínica (T-196, D-tony-170), sin Unity vivo: cuándo actúa, cuánta vida le deja al mecha, cuándo despierta
    /// el jugador y qué dice según la deuda.
    /// </summary>
    public static class ReglaDoctor
    {
        private const float HorasPorDia = 24f;

        /// <summary>
        /// El doctor actúa solo con la historia en Libre (o sin historia). Si cayó el mecha, tampoco actúa dentro de una pelea del flujo:
        /// perder ahí es asunto del flujo. El KO a pie siempre cuenta.
        /// </summary>
        public static bool Actua(bool hayHistoria, EtapaPrologo etapa, bool cayoElMecha, bool dentroDePeleaDelFlujo)
        {
            if (hayHistoria && etapa != EtapaPrologo.Libre)
            {
                return false;
            }
            return !(cayoElMecha && dentroDePeleaDelFlujo);
        }

        /// <summary>
        /// La fracción de vida (0 a 1) que le queda al mecha. Fuera del taller: la menor entre la que tenía y la del doctor (en KO es 0; el mecha
        /// la sube al piso de emergencia al fijarla). En el taller no se toca: devuelve la que tenía.
        /// </summary>
        public static float FraccionMecha(float fraccionActual, float fraccionDoctor, bool fueraDelTaller)
        {
            float actual = Mathf.Clamp01(fraccionActual);
            return fueraDelTaller ? Mathf.Min(actual, Mathf.Clamp01(fraccionDoctor)) : actual;
        }

        /// <summary>La hora del reloj en la que despierta: la actual más las horas que durmió, dentro del día.</summary>
        public static float HoraDeDespertar(float horaActual, float horasInconsciente)
        {
            return Mathf.Repeat(horaActual + horasInconsciente, HorasPorDia);
        }

        /// <summary>True si, después de cobrar, el dinero quedó en negativo (la deuda con el doctor).</summary>
        public static bool QuedoDebiendo(int dineroDespuesDeCobrar) => dineroDespuesDeCobrar < 0;

        /// <summary>Lo que dice el doctor: el texto normal, o el texto normal y el de la deuda si el dinero quedó en negativo.</summary>
        public static string TextoDoctor(int dineroDespuesDeCobrar, string texto, string textoDeuda)
        {
            return QuedoDebiendo(dineroDespuesDeCobrar) ? texto + " " + textoDeuda : texto;
        }
    }
}
