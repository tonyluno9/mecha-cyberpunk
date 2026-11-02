using System;
using System.Collections.Generic;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Reglas puras de la cola de avisos del centro (T-523): uno a la vez, el de más prioridad primero (a igual prioridad, el que llegó antes),
    /// duración corta, sin repetir el mismo texto, con tope de espera (si se llena, se descarta el menos importante y más viejo).
    /// Un aviso con <c>clave</c> reemplaza el texto del que ya esté en la cola o en pantalla con esa clave (para sumas que cambian, como la reputación).
    /// </summary>
    public class ReglaColaAvisos
    {
        public readonly struct Aviso
        {
            public string Texto { get; }
            public PrioridadAviso Prioridad { get; }
            public TipoAviso Tipo { get; }
            public float Segundos { get; }
            public string Clave { get; }
            public long Orden { get; }

            public Aviso(string texto, PrioridadAviso prioridad, TipoAviso tipo, float segundos, string clave, long orden)
            {
                Texto = texto;
                Prioridad = prioridad;
                Tipo = tipo;
                Segundos = segundos;
                Clave = clave;
                Orden = orden;
            }

            public Aviso ConTexto(string nuevo) => new Aviso(nuevo, Prioridad, Tipo, Segundos, Clave, Orden);
        }

        private readonly List<Aviso> espera = new List<Aviso>();
        private readonly float segundosMinimos;
        private readonly float segundosMaximos;
        private readonly int tope;
        private long contador;
        private float restante;

        public ReglaColaAvisos(float segundosMinimos, float segundosMaximos, int tope)
        {
            this.segundosMinimos = Math.Max(0.1f, segundosMinimos);
            this.segundosMaximos = Math.Max(this.segundosMinimos, segundosMaximos);
            this.tope = Math.Max(1, tope);
        }

        /// <summary>True si hay un aviso en pantalla.</summary>
        public bool HayActual { get; private set; }

        public Aviso Actual { get; private set; }

        /// <summary>Cuántos avisos esperan su turno.</summary>
        public int EnEspera => espera.Count;

        /// <summary>Los textos que esperan, en el orden en que saldrán.</summary>
        public IReadOnlyList<string> TextosEnEspera
        {
            get
            {
                List<Aviso> orden = new List<Aviso>(espera);
                orden.Sort(Comparar);
                List<string> textos = new List<string>(orden.Count);
                foreach (Aviso a in orden)
                {
                    textos.Add(a.Texto);
                }
                return textos;
            }
        }

        public static float Limitar(float segundos, float minimo, float maximo) => Math.Min(Math.Max(segundos, minimo), maximo);

        public void Publicar(string texto, PrioridadAviso prioridad, TipoAviso tipo, float segundos, string clave = null)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return;
            }
            bool conClave = !string.IsNullOrEmpty(clave);
            if (conClave)
            {
                if (HayActual && Actual.Clave == clave)
                {
                    Actual = Actual.ConTexto(texto);
                    return;
                }
                for (int i = 0; i < espera.Count; i++)
                {
                    if (espera[i].Clave == clave)
                    {
                        espera[i] = espera[i].ConTexto(texto);
                        return;
                    }
                }
            }
            if ((HayActual && Actual.Texto == texto) || espera.Exists(a => a.Texto == texto))
            {
                return;
            }
            float duracion = Limitar(segundos <= 0f ? segundosMinimos : segundos, segundosMinimos, segundosMaximos);
            espera.Add(new Aviso(texto, prioridad, tipo, duracion, clave, contador++));
            if (espera.Count > tope)
            {
                Descartar();
            }
            if (!HayActual)
            {
                Siguiente();
            }
        }

        /// <summary>Pasa el tiempo; con <paramref name="dt"/> en 0 todo queda quieto (pausa).</summary>
        public void Avanzar(float dt)
        {
            if (!HayActual)
            {
                if (espera.Count > 0)
                {
                    Siguiente();
                }
                return;
            }
            restante -= dt;
            if (restante <= 0f)
            {
                HayActual = false;
                Siguiente();
            }
        }

        public void Vaciar()
        {
            espera.Clear();
            HayActual = false;
            restante = 0f;
        }

        private static int Comparar(Aviso a, Aviso b)
        {
            int porPrioridad = ((int)b.Prioridad).CompareTo((int)a.Prioridad);
            return porPrioridad != 0 ? porPrioridad : a.Orden.CompareTo(b.Orden);
        }

        private void Siguiente()
        {
            if (espera.Count == 0)
            {
                return;
            }
            int mejor = 0;
            for (int i = 1; i < espera.Count; i++)
            {
                if (Comparar(espera[i], espera[mejor]) < 0)
                {
                    mejor = i;
                }
            }
            Actual = espera[mejor];
            espera.RemoveAt(mejor);
            HayActual = true;
            restante = Actual.Segundos;
        }

        // Sale el de menos prioridad y, entre iguales, el más viejo.
        private void Descartar()
        {
            int peor = 0;
            for (int i = 1; i < espera.Count; i++)
            {
                bool menosImportante = espera[i].Prioridad < espera[peor].Prioridad;
                bool igualYMasViejo = espera[i].Prioridad == espera[peor].Prioridad && espera[i].Orden < espera[peor].Orden;
                if (menosImportante || igualYMasViejo)
                {
                    peor = i;
                }
            }
            espera.RemoveAt(peor);
        }
    }
}
