using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// La búsqueda policial (T-665, simplificada en T-729), sin escena: un círculo centrado donde te vieron por última vez (solo para dibujarlo en el mapa). Mientras te ven, el
    /// círculo te sigue y no escapas; si pasas <c>segundosParaEscapar</c> seguidos sin que te vea nadie, ya no te buscan, estés donde estés. Los últimos <see cref="SegundosAviso"/> s
    /// de ese tiempo son el aviso de que te están por perder (las estrellas parpadean solo entonces).
    /// </summary>
    public sealed class ReglaBusquedaPolicial
    {
        /// <summary>T-729: cuántos segundos antes de perderte empieza el aviso (las estrellas del HUD parpadean). El HUD usa su propia constante: HUD (UI) no ve Trafico.</summary>
        public const float SegundosAviso = 8f;

        private readonly float radio;
        private readonly float segundosParaEscapar;

        public ReglaBusquedaPolicial(float metrosRadio, float segundos)
        {
            radio = Mathf.Max(0f, metrosRadio);
            segundosParaEscapar = Mathf.Max(0f, segundos);
        }

        public bool Activa { get; private set; }
        public Vector3 Centro { get; private set; }
        public float Radio => radio;
        /// <summary>Segundos seguidos sin que te vea nadie (se reinicia cuando te ven o hay un delito nuevo).</summary>
        public float SegundosFuera { get; private set; }
        public bool Visto { get; private set; }
        public float SegundosParaEscapar => segundosParaEscapar;

        /// <summary>True en los últimos <see cref="SegundosAviso"/> s antes de perderte: la policía está por darte por escapado.</summary>
        public bool PorPerderte => Activa && !Visto && SegundosFuera >= segundosParaEscapar - SegundosAviso;

        /// <summary>Empieza la búsqueda con el círculo en <paramref name="donde"/>.</summary>
        public void Iniciar(Vector3 donde)
        {
            Activa = true;
            Centro = donde;
            SegundosFuera = 0f;
            Visto = true;
        }

        public void Detener()
        {
            Activa = false;
            SegundosFuera = 0f;
            Visto = false;
        }

        /// <summary>Un delito nuevo con testigos: el círculo vuelve a donde estás.</summary>
        public void Recentrar(Vector3 donde)
        {
            if (Activa)
            {
                Centro = donde;
                SegundosFuera = 0f;
            }
        }

        /// <summary>Avanza <paramref name="dt"/> s. Devuelve true en el cuadro en que escapas (y la búsqueda se detiene).</summary>
        public bool Avanzar(Vector3 jugador, bool teVen, float dt)
        {
            if (!Activa)
            {
                return false;
            }
            Visto = teVen;
            if (teVen)
            {
                Centro = jugador;
                SegundosFuera = 0f;
                return false;
            }
            SegundosFuera += dt;
            if (SegundosFuera < segundosParaEscapar)
            {
                return false;
            }
            Detener();
            return true;
        }
    }
}
