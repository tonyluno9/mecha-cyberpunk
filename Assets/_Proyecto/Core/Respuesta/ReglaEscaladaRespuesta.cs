using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1620 (T8): la lógica pura del director de respuesta, sin Unity. Jerarquía fija de 8 niveles que se desbloquean en orden (las estrellas solo dicen hasta
    /// dónde pueden llegar); cooldown de reposición por unidad perdida; topes por tipo y totales; empieza con una unidad y sube de a poco con las bajas.
    /// El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaEscaladaRespuesta
    {
        public const int Niveles = 8;
        private static readonly int NumeroTipos = Enum.GetValues(typeof(TipoRespuesta)).Length;

        private readonly ParametrosRespuesta p;
        private readonly Random azar;
        private readonly int[] vivos;
        private readonly int[] perdidas;
        private readonly float[] proximo;
        private readonly bool[] disponible;
        private float desdeNivel;
        private int perdidasDelNivel;
        private bool mision;
        private bool novaPermitida;

        public ReglaEscaladaRespuesta(ParametrosRespuesta parametros, Random azar = null)
        {
            p = parametros ?? ParametrosRespuesta.PorOmision();
            this.azar = azar ?? new Random(1620);
            vivos = new int[NumeroTipos];
            perdidas = new int[NumeroTipos];
            proximo = new float[NumeroTipos];
            disponible = new bool[NumeroTipos];
            for (int i = 0; i < NumeroTipos; i++)
            {
                disponible[i] = true;
            }
        }

        public int Estrellas { get; private set; }

        /// <summary>Nivel más alto ya desbloqueado (0 sin búsqueda).</summary>
        public int NivelDesbloqueado { get; private set; }

        public int VivosTotal
        {
            get
            {
                // Los policías a pie bajan de las patrullas y no cuentan aparte en el total.
                int suma = 0;
                for (int i = 0; i < vivos.Length; i++)
                {
                    suma += i == (int)TipoRespuesta.PoliciaAPie ? 0 : vivos[i];
                }
                return suma;
            }
        }

        public int Vivos(TipoRespuesta tipo) => vivos[(int)tipo];

        public int Perdidas(TipoRespuesta tipo) => perdidas[(int)tipo];

        /// <summary>Nivel de la jerarquía (1 a 8) de un tipo.</summary>
        public static int NivelDe(TipoRespuesta tipo)
        {
            switch (tipo)
            {
                case TipoRespuesta.PoliciaAPie:
                case TipoRespuesta.Patrulla: return 1;
                case TipoRespuesta.Camioneta: return 2;
                case TipoRespuesta.Dron: return 3;
                case TipoRespuesta.NovaHumanoide: return 4;
                case TipoRespuesta.RobotMediano: return 5;
                case TipoRespuesta.Perro: return 6;
                case TipoRespuesta.RobotGrande: return 7;
                default: return 8;
            }
        }

        /// <summary>True si el tipo es de NOVA (nivel 4 en adelante).</summary>
        public static bool EsNova(TipoRespuesta tipo) => NivelDe(tipo) >= 4;

        /// <summary>Nivel más alto al que pueden llegar las estrellas dadas (0 sin estrellas).</summary>
        public int NivelMaximoPara(int estrellas)
        {
            if (estrellas <= 0)
            {
                return 0;
            }
            int[] tabla = p.NivelMaximoPorEstrellas;
            if (tabla == null || tabla.Length == 0)
            {
                return Niveles;
            }
            int nivel = tabla[Math.Min(estrellas, tabla.Length) - 1];
            return Math.Max(1, Math.Min(Niveles, nivel));
        }

        /// <summary>Dice si hay con qué generar este tipo (prefab, calles...). Sin eso su nivel se salta.</summary>
        public void Declarar(TipoRespuesta tipo, bool existe) => disponible[(int)tipo] = existe;

        /// <summary>Durante una misión protegida la policía no entra; NOVA sí solo si la misión lo permite.</summary>
        public bool PermitidoPorMision(TipoRespuesta tipo) => !mision || (novaPermitida && EsNova(tipo));

        /// <summary>True si el tipo todavía puede estar en pie con las estrellas y la misión de ahora (para retirar primero los niveles altos).</summary>
        public bool PermiteMantener(TipoRespuesta tipo) => Estrellas > 0 && PermitidoPorMision(tipo) && NivelDe(tipo) <= NivelMaximoPara(Estrellas);

        public void Avanzar(float ahora, int estrellas, bool misionActiva, bool novaEnMision, int[] vivosActuales)
        {
            mision = misionActiva;
            novaPermitida = novaEnMision;
            for (int i = 0; i < NumeroTipos; i++)
            {
                vivos[i] = vivosActuales != null && i < vivosActuales.Length ? vivosActuales[i] : 0;
            }
            Estrellas = estrellas;
            if (estrellas <= 0)
            {
                Reiniciar();
                return;
            }
            if (NivelDesbloqueado == 0)
            {
                NivelDesbloqueado = 1;
                desdeNivel = ahora;
                perdidasDelNivel = 0;
            }
            int tope = NivelMaximoPara(estrellas);
            if (NivelDesbloqueado > tope)
            {
                NivelDesbloqueado = tope; // perdiste estrellas: los niveles altos se cierran primero
            }
            while (NivelDesbloqueado < tope && (!NivelDisponible(NivelDesbloqueado) || NivelNoTeDetuvo(NivelDesbloqueado, ahora)))
            {
                NivelDesbloqueado++;
                desdeNivel = ahora;
                perdidasDelNivel = 0;
                RetrasarNivel(NivelDesbloqueado, ahora);
            }
        }

        private void Reiniciar()
        {
            NivelDesbloqueado = 0;
            perdidasDelNivel = 0;
            Array.Clear(perdidas, 0, perdidas.Length);
            Array.Clear(proximo, 0, proximo.Length);
        }

        private bool NivelDisponible(int nivel)
        {
            for (int t = 0; t < NumeroTipos; t++)
            {
                TipoRespuesta tipo = (TipoRespuesta)t;
                if (tipo != TipoRespuesta.PoliciaAPie && NivelDe(tipo) == nivel && disponible[t] && PermitidoPorMision(tipo))
                {
                    return true;
                }
            }
            return false;
        }

        // "No te detuvo": sus unidades murieron (hubo bajas y ya no queda ninguna) o pasó su tiempo y sigues ahí.
        private bool NivelNoTeDetuvo(int nivel, float ahora)
        {
            int vivosDelNivel = 0;
            for (int t = 0; t < NumeroTipos; t++)
            {
                if (NivelDe((TipoRespuesta)t) == nivel)
                {
                    vivosDelNivel += vivos[t];
                }
            }
            return (perdidasDelNivel > 0 && vivosDelNivel == 0) || ahora - desdeNivel >= p.SegundosParaSubir;
        }

        private void RetrasarNivel(int nivel, float ahora)
        {
            for (int t = 0; t < NumeroTipos; t++)
            {
                if (NivelDe((TipoRespuesta)t) == nivel)
                {
                    proximo[t] = Math.Max(proximo[t], ahora + p.RetrasoNivelNuevo);
                }
            }
        }

        /// <summary>Cuántos de este tipo puede haber a la vez ahora: crece con las bajas y con los niveles de arriba ya desbloqueados, sin pasar del tope.</summary>
        public int CupoActual(TipoRespuesta tipo)
        {
            AjusteTipoRespuesta a = p.AjusteDe(tipo);
            if (a == null)
            {
                return 0;
            }
            int porBajas = p.PerdidasParaUnoMas > 0 ? perdidas[(int)tipo] / p.PerdidasParaUnoMas : 0;
            int porNiveles = Math.Max(0, NivelDesbloqueado - NivelDe(tipo));
            return Math.Min(a.Tope, Math.Max(1, a.CupoInicial) + porBajas + porNiveles);
        }

        public bool PuedeGenerar(TipoRespuesta tipo, float ahora)
        {
            int nivel = NivelDe(tipo);
            if (NivelDesbloqueado <= 0 || nivel > NivelDesbloqueado || nivel > NivelMaximoPara(Estrellas)
                || !disponible[(int)tipo] || !PermitidoPorMision(tipo))
            {
                return false;
            }
            return ahora >= proximo[(int)tipo] && VivosTotal < p.TopeTotal && vivos[(int)tipo] < CupoActual(tipo);
        }

        /// <summary>Salió una unidad: cuenta como viva y bloquea otra del mismo tipo unos segundos.</summary>
        public void RegistrarGenerado(TipoRespuesta tipo, float ahora)
        {
            vivos[(int)tipo]++;
            proximo[(int)tipo] = Math.Max(proximo[(int)tipo], ahora + p.SegundosEntreGenerados);
        }

        /// <summary>Murió una unidad (no una retirada): empieza el cooldown de su reemplazo.</summary>
        public void RegistrarPerdida(TipoRespuesta tipo, float ahora)
        {
            perdidas[(int)tipo]++;
            if (NivelDe(tipo) == NivelDesbloqueado)
            {
                perdidasDelNivel++;
            }
            AjusteTipoRespuesta a = p.AjusteDe(tipo);
            float min = a != null ? a.CooldownMin : 0f;
            float max = a != null ? Math.Max(min, a.CooldownMax) : 0f;
            float espera = min + (float)azar.NextDouble() * (max - min);
            proximo[(int)tipo] = Math.Max(proximo[(int)tipo], ahora + espera);
        }
    }
}
