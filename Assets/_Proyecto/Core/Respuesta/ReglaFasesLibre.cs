using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-historia-libre-23nov (escalada progresiva, 24-nov): la regla pura de las fases del modo libre. Cada fase dura su tiempo máximo o hasta que
    /// matas su número de enemigos: lo que pase primero sube a la siguiente. Nunca sube más de una fase por paso y las bajas de sobra no pasan a la
    /// fase nueva, así que un evento grande (explotar un camión con muchos dentro) nunca salta fases. La última fase no termina. Sin Unity: el tiempo entra como dt.
    /// </summary>
    public sealed class ReglaFasesLibre
    {
        private readonly FaseModoLibre[] fases;

        public ReglaFasesLibre(FaseModoLibre[] fases)
        {
            this.fases = fases != null && fases.Length > 0 ? fases : new[] { new FaseModoLibre() };
            Iniciar(1);
        }

        /// <summary>Cuántas fases hay.</summary>
        public int Total => fases.Length;

        /// <summary>Fase actual, de 1 a <see cref="Total"/>.</summary>
        public int Nivel { get; private set; }

        public FaseModoLibre Fase => fases[Nivel - 1];

        /// <summary>Segundos que lleva la fase actual.</summary>
        public float SegundosEnFase { get; private set; }

        /// <summary>Bajas de la fase actual que cuentan para <see cref="FaseModoLibre.Muertes"/>.</summary>
        public int MuertesDeFase { get; private set; }

        /// <summary>Bajas de la fase actual que cuentan para <see cref="FaseModoLibre.MuertesAlterna"/>.</summary>
        public int MuertesAlternaDeFase { get; private set; }

        public bool EsUltima => Nivel >= fases.Length;

        /// <summary>Empieza (o reinicia) en la fase dada, con el reloj y las bajas en cero.</summary>
        public void Iniciar(int nivel)
        {
            Nivel = Math.Max(1, Math.Min(fases.Length, nivel));
            SegundosEnFase = 0f;
            MuertesDeFase = 0;
            MuertesAlternaDeFase = 0;
        }

        /// <summary>Murió una unidad de este tipo: cuenta para la fase actual (solo si es de los tipos que la fase cuenta).</summary>
        public void RegistrarBaja(TipoRespuesta tipo)
        {
            FaseModoLibre f = Fase;
            if (FaseModoLibre.Contiene(f.TiposMuertes, tipo))
            {
                MuertesDeFase++;
            }
            if (FaseModoLibre.Contiene(f.TiposMuertesAlterna, tipo))
            {
                MuertesAlternaDeFase++;
            }
        }

        /// <summary>True si la fase actual ya se cumplió (tiempo o bajas) y no es la última.</summary>
        public bool Cumplida
        {
            get
            {
                if (EsUltima)
                {
                    return false;
                }
                FaseModoLibre f = Fase;
                return (f.Segundos > 0f && SegundosEnFase >= f.Segundos)
                    || (f.Muertes > 0 && MuertesDeFase >= f.Muertes)
                    || (f.MuertesAlterna > 0 && MuertesAlternaDeFase >= f.MuertesAlterna);
            }
        }

        /// <summary>Avanza el reloj de la fase. Devuelve true si subió de fase (a lo más una por llamada).</summary>
        public bool Avanzar(float dt)
        {
            SegundosEnFase += Math.Max(0f, dt);
            if (!Cumplida)
            {
                return false;
            }
            Iniciar(Nivel + 1);
            return true;
        }

        /// <summary>Segundos desde el arranque del modo libre en que empieza la fase <paramref name="nivel"/> si solo pasa el tiempo (para el reporte).</summary>
        public float SegundosHastaFase(int nivel)
        {
            float suma = 0f;
            for (int i = 0; i < Math.Min(nivel - 1, fases.Length); i++)
            {
                suma += fases[i].Segundos;
            }
            return suma;
        }
    }
}
