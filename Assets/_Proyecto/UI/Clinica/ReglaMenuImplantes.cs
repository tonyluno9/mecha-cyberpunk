using System;
using System.Collections.Generic;
using System.Globalization;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// Las reglas del menú de implantes con holograma (T-815), sin Unity: la tabla antes/después de los efectos (solo los que el implante
    /// instalado o el ofrecido tocan), cómo se lee cada número y a qué espacio del cuerpo pertenece un punto del holograma.
    /// </summary>
    public static class ReglaMenuImplantes
    {
        public const int CantidadEfectos = 11;
        private const float Tolerancia = 0.0001f;
        private const float Porciento = 100f;

        /// <summary>Por encima de esta fracción de la altura es la cabeza (OJOS).</summary>
        public const float AlturaCabeza = 0.85f;
        /// <summary>Por debajo de esta fracción de la altura son las PIERNAS.</summary>
        public const float AlturaCadera = 0.47f;
        /// <summary>Más lejos del centro que esta fracción de la altura (y arriba de la cadera) son los BRAZOS.</summary>
        public const float AnchoTorso = 0.13f;

        /// <summary>El valor del efecto (los sí/no, 0 o 1).</summary>
        public static float Valor(EfectosImplantes e, EfectoImplante efecto)
        {
            switch (efecto)
            {
                case EfectoImplante.GolpeAPie: return e.multiplicadorGolpeAPie;
                case EfectoImplante.Correr: return e.multiplicadorCorrer;
                case EfectoImplante.Salto: return e.multiplicadorSalto;
                case EfectoImplante.ReaccionMecha: return e.multiplicadorReaccionMecha;
                case EfectoImplante.VerVidaRival: return e.verVidaRival ? 1f : 0f;
                case EfectoImplante.VidaEnNumeros: return e.vidaRivalEnNumeros ? 1f : 0f;
                case EfectoImplante.AnticipacionAtaque: return e.anticipacionAtaque;
                case EfectoImplante.AlcanceApuntado: return e.bonusAlcanceApuntado;
                case EfectoImplante.Retroceso: return e.reduccionRetroceso;
                case EfectoImplante.EnfriamientoRodada: return e.reduccionEnfriamientoRodada;
                default: return e.reduccionRecarga;
            }
        }

        /// <summary>El valor que no cambia nada (sin implante).</summary>
        public static float Neutro(EfectoImplante efecto) => Valor(EfectosImplantes.Ninguno, efecto);

        /// <summary>True si un número menor es mejor (la reacción del mecha es un multiplicador de la espera).</summary>
        public static bool MenorEsMejor(EfectoImplante efecto) => efecto == EfectoImplante.ReaccionMecha;

        /// <summary>True si el efecto es sí/no.</summary>
        public static bool EsSiNo(EfectoImplante efecto) => efecto == EfectoImplante.VerVidaRival || efecto == EfectoImplante.VidaEnNumeros;

        private static bool EsReduccion(EfectoImplante efecto) =>
            efecto == EfectoImplante.Retroceso || efecto == EfectoImplante.EnfriamientoRodada || efecto == EfectoImplante.Recarga;

        /// <summary>1 si mejora, -1 si empeora, 0 si queda igual.</summary>
        public static int Cambio(EfectoImplante efecto, float antes, float despues)
        {
            float diferencia = despues - antes;
            if (Math.Abs(diferencia) < Tolerancia)
            {
                return 0;
            }
            return (diferencia > 0f) != MenorEsMejor(efecto) ? 1 : -1;
        }

        /// <summary>
        /// La tabla de lo instalado (<paramref name="antes"/>; <see cref="EfectosImplantes.Ninguno"/> si no hay) contra lo ofrecido: solo los
        /// efectos que alguno de los dos cambia, en el orden de <see cref="EfectoImplante"/>.
        /// </summary>
        public static void Comparar(EfectosImplantes antes, EfectosImplantes despues, List<FilaEfectoImplante> filas)
        {
            filas.Clear();
            for (int i = 0; i < CantidadEfectos; i++)
            {
                EfectoImplante efecto = (EfectoImplante)i;
                float a = Valor(antes, efecto);
                float d = Valor(despues, efecto);
                float neutro = Neutro(efecto);
                if (Math.Abs(a - neutro) < Tolerancia && Math.Abs(d - neutro) < Tolerancia)
                {
                    continue;
                }
                filas.Add(new FilaEfectoImplante(efecto, a, d, Cambio(efecto, a, d)));
            }
        }

        /// <summary>El número como se lee: multiplicadores en % (150%), reducciones con menos (-30%), alcance con más (+25%), aviso en s.</summary>
        public static string Texto(EfectoImplante efecto, float valor, string si, string no)
        {
            CultureInfo cultura = CultureInfo.InvariantCulture;
            if (EsSiNo(efecto))
            {
                return valor > 0.5f ? si : no;
            }
            if (efecto == EfectoImplante.AnticipacionAtaque)
            {
                return valor.ToString("0.0", cultura) + " s";
            }
            string numero = Math.Round(valor * Porciento).ToString("0", cultura) + "%";
            if (efecto == EfectoImplante.AlcanceApuntado)
            {
                return "+" + numero;
            }
            return EsReduccion(efecto) ? "-" + numero : numero;
        }

        /// <summary>La diferencia con signo como se lee ("+50%", "-15%", "+0.4 s"); vacía en los sí/no o si queda igual.</summary>
        public static string TextoDiferencia(EfectoImplante efecto, float antes, float despues)
        {
            float diferencia = despues - antes;
            if (EsSiNo(efecto) || Math.Abs(diferencia) < Tolerancia)
            {
                return string.Empty;
            }
            CultureInfo cultura = CultureInfo.InvariantCulture;
            if (efecto == EfectoImplante.AnticipacionAtaque)
            {
                return (diferencia > 0f ? "+" : "-") + Math.Abs(diferencia).ToString("0.0", cultura) + " s";
            }
            float mostrado = EsReduccion(efecto) ? -diferencia : diferencia;
            return (mostrado > 0f ? "+" : "-") + Math.Round(Math.Abs(mostrado) * Porciento).ToString("0", cultura) + "%";
        }

        /// <summary>
        /// El espacio del cuerpo de un punto del holograma de pie (en T o en A): <paramref name="altura"/> es la fracción de la altura (0 pies,
        /// 1 coronilla) y <paramref name="lado"/> la distancia al centro como fracción de la altura. Cabeza = OJOS, abajo de la cadera = PIERNAS,
        /// a los lados del torso = BRAZOS y el torso (la columna) = SINAPSIS.
        /// </summary>
        public static EspacioImplante Region(float altura, float lado)
        {
            if (altura >= AlturaCabeza)
            {
                return EspacioImplante.Ojos;
            }
            if (altura < AlturaCadera)
            {
                return EspacioImplante.Piernas;
            }
            return Math.Abs(lado) >= AnchoTorso ? EspacioImplante.Brazos : EspacioImplante.SistemaNervioso;
        }
    }
}
