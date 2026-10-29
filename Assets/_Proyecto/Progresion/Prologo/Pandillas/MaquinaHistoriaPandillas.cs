using System;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>Los pasos de la historia corta del juego de pandillas (T-406), en orden.</summary>
    public enum PasoHistoria
    {
        Despierta,
        PrimeraPelea,
        Pistola,
        Chato,
        Tianguis,
        Jefe,
        Continuara,
        Libre
    }

    /// <summary>Lo que le pasa al jugador y hace avanzar la historia. El valor de cada aviso es el del paso que termina.</summary>
    public enum AvisoHistoria
    {
        SalioDeCasa,
        PeleaGanada,
        PistolaTomada,
        ChatoUnido,
        JefeDisponible,
        TianguisConquistado,
        ContinuaraCerrado
    }

    /// <summary>
    /// Reglas puras de la historia de pandillas (T-406): cada aviso solo avanza si es el del paso actual, así que el orden no se puede saltar.
    /// </summary>
    public sealed class MaquinaHistoriaPandillas
    {
        public PasoHistoria Paso { get; private set; } = PasoHistoria.Despierta;

        public bool Terminada => Paso == PasoHistoria.Libre;

        /// <summary>Pone el paso (al cargar una partida).</summary>
        public void Iniciar(PasoHistoria paso) => Paso = paso;

        /// <summary>True si el aviso era el del paso actual y la historia avanzó.</summary>
        public bool Avisar(AvisoHistoria aviso)
        {
            if (Paso == PasoHistoria.Libre || (int)aviso != (int)Paso)
            {
                return false;
            }
            Paso = (PasoHistoria)((int)Paso + 1);
            return true;
        }

        /// <summary>Salta un paso (atajo de prueba). False si ya terminó.</summary>
        public bool Saltar()
        {
            if (Paso == PasoHistoria.Libre)
            {
                return false;
            }
            Paso = (PasoHistoria)((int)Paso + 1);
            return true;
        }

        public static bool TryLeer(string texto, out PasoHistoria paso) => Enum.TryParse(texto, out paso) && Enum.IsDefined(typeof(PasoHistoria), paso);
    }
}
