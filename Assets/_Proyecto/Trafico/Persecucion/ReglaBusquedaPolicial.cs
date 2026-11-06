using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// La búsqueda policial (T-665), sin escena: un círculo centrado donde te vieron por última vez. Mientras te ven, el círculo te sigue; si no te ven
    /// y sigues dentro, no pasa nada; solo si pasas <c>segundosParaEscapar</c> seguidos sin que te vean Y fuera del círculo, escapaste.
    /// </summary>
    public sealed class ReglaBusquedaPolicial
    {
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
        public float SegundosFuera { get; private set; }
        public bool Visto { get; private set; }

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
            Vector3 d = jugador - Centro;
            d.y = 0f;
            if (d.magnitude <= radio)
            {
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
