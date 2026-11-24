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
        /// <summary>T-1631: primer nivel de la jerarquía que es de NOVA (los soldados humanoides).</summary>
        public const int PrimerNivelNova = 4;
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
        private bool puertaNova;
        private int camionetasLlegadas;
        private FaseModoLibre libre;

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

        /// <summary>
        /// T-historia-libre-23nov: la fase del modo libre que manda ahora (null fuera del modo libre). Con ella la escalada no sale de las estrellas:
        /// solo llegan los tipos de la fase, la misión protegida no aplica, NOVA entra si la fase lo dice y valen los topes del modo libre.
        /// </summary>
        public FaseModoLibre Libre => libre;

        /// <summary>T-historia-libre-23nov: pone (o quita con null) la fase del modo libre.</summary>
        public void PonerModoLibre(FaseModoLibre fase)
        {
            libre = fase;
            if (fase == null)
            {
                Reiniciar();
            }
        }

        /// <summary>T-historia-libre-23nov: enemigos vivos que cuentan para el tope del modo libre (todo menos los vehículos).</summary>
        public int VivosLibre
        {
            get
            {
                int suma = 0;
                for (int i = 0; i < vivos.Length; i++)
                {
                    suma += i == (int)TipoRespuesta.Patrulla || i == (int)TipoRespuesta.Camioneta ? 0 : vivos[i];
                }
                return suma;
            }
        }

        /// <summary>T-1660: cuántas camionetas ya llegaron junto al jugador en esta búsqueda (lo dice quien las maneja).</summary>
        public int CamionetasLlegadas { get => camionetasLlegadas; set => camionetasLlegadas = Math.Max(0, value); }

        /// <summary>T-1660: policías a pie muertos en esta búsqueda (se reinicia al perder las estrellas).</summary>
        public int PoliciasMuertos => perdidas[(int)TipoRespuesta.PoliciaAPie];

        /// <summary>T-1660: camionetas que hay que esperar antes de Nova (0 si no hay modelo de camioneta).</summary>
        public int CamionetasRequeridas => ReglaPuertaNova.CamionetasRequeridas(disponible[(int)TipoRespuesta.Camioneta], p.CamionetasParaNova);

        /// <summary>T-1660: True si Nova ya puede llegar (10 policías muertos y las camionetas llegadas; una misión protegida que permite a Nova la abre). Se queda abierta hasta perder las estrellas.</summary>
        public bool PuertaNovaAbierta => puertaNova || PuertaForzada || (mision && novaPermitida);

        /// <summary>T-1660: para pruebas y depuración: abre la puerta de Nova sin cumplir las condiciones.</summary>
        public bool PuertaForzada { get; set; }

        /// <summary>
        /// T-1631 (T17): "NOVA INTERVIENE". Con las estrellas del máximo (sin misión protegida y con algo de NOVA para generar) no se pide policía nueva, la que hay se
        /// retira, los drones solo salen como drones de NOVA (desde <see cref="ParametrosRespuesta.NivelDronNova"/>) y NOVA llega por la jerarquía con su tope de vivos.
        /// </summary>
        public bool ModoNova { get; private set; }

        /// <summary>T-1631: unidades de NOVA vivas que cuentan para <see cref="ParametrosRespuesta.TopeNovaVivos"/> (sin el jefe; con los drones solo en modo Nova).</summary>
        public int NovaVivos
        {
            get
            {
                int suma = 0;
                for (int i = 0; i < vivos.Length; i++)
                {
                    suma += CuentaParaTopeNova((TipoRespuesta)i) ? vivos[i] : 0;
                }
                return suma;
            }
        }

        private bool CuentaParaTopeNova(TipoRespuesta tipo) => (EsNova(tipo) && tipo != TipoRespuesta.Jefe) || (ModoNova && tipo == TipoRespuesta.Dron);

        /// <summary>T-1631: True si el tipo es de la policía (niveles 1 a 3: a pie, patrullas, camionetas y drones).</summary>
        public static bool EsPolicia(TipoRespuesta tipo) => NivelDe(tipo) < PrimerNivelNova;

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
        public bool PermiteMantener(TipoRespuesta tipo) => libre != null ? libre.Permite(tipo) : Estrellas > 0 && PermitidoPorMision(tipo) && !BloqueadoPorModoNova(tipo) && NivelDe(tipo) <= TopeDeNivel(Estrellas);

        // T-1631: en modo Nova no entra policía; el dron solo entra cuando ya se desbloqueó su nivel de NOVA.
        private bool BloqueadoPorModoNova(TipoRespuesta tipo)
        {
            if (libre != null)
            {
                return !libre.Permite(tipo); // T-historia-libre-23nov: en el modo libre manda la fase
            }
            if (!ModoNova)
            {
                return false;
            }
            return tipo == TipoRespuesta.Dron ? NivelDesbloqueado < p.NivelDronNova : EsPolicia(tipo);
        }

        private bool HayNovaQueGenerar()
        {
            for (int t = 0; t < NumeroTipos; t++)
            {
                if (EsNova((TipoRespuesta)t) && disponible[t])
                {
                    return true;
                }
            }
            return false;
        }

        public void Avanzar(float ahora, int estrellas, bool misionActiva, bool novaEnMision, int[] vivosActuales)
        {
            if (libre != null)
            {
                AvanzarLibre(estrellas, vivosActuales);
                return;
            }
            mision = misionActiva;
            novaPermitida = novaEnMision;
            for (int i = 0; i < NumeroTipos; i++)
            {
                vivos[i] = vivosActuales != null && i < vivosActuales.Length ? vivosActuales[i] : 0;
            }
            Estrellas = estrellas;
            if (estrellas > 0 && !puertaNova)
            {
                puertaNova = ReglaPuertaNova.Abierta(PoliciasMuertos, p.PoliciasMuertosParaNova, camionetasLlegadas, CamionetasRequeridas);
            }
            ModoNova = estrellas > 0 && !misionActiva && p.EstrellasModoNova > 0 && estrellas >= p.EstrellasModoNova && PuertaNovaAbierta && HayNovaQueGenerar();
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
            int tope = TopeDeNivel(estrellas);
            if (NivelDesbloqueado > tope)
            {
                NivelDesbloqueado = tope; // perdiste estrellas: los niveles altos se cierran primero
            }
            if (ModoNova && NivelDesbloqueado < PrimerNivelNova)
            {
                // T-1631: NOVA INTERVIENE: se salta lo que falta de la policía y empieza por el primer nivel de NOVA (con su retraso de nivel nuevo).
                NivelDesbloqueado = PrimerNivelNova;
                desdeNivel = ahora;
                perdidasDelNivel = 0;
                RetrasarNivel(NivelDesbloqueado, ahora);
            }
            while (NivelDesbloqueado < tope && (!NivelDisponible(NivelDesbloqueado) || NivelNoTeDetuvo(NivelDesbloqueado, ahora)))
            {
                NivelDesbloqueado++;
                desdeNivel = ahora;
                perdidasDelNivel = 0;
                RetrasarNivel(NivelDesbloqueado, ahora);
            }
        }

        // T-historia-libre-23nov: en el modo libre no cuentan la misión ni la puerta de Nova; el nivel es el más alto de los tipos de la fase.
        private void AvanzarLibre(int estrellas, int[] vivosActuales)
        {
            mision = false;
            novaPermitida = false;
            for (int i = 0; i < NumeroTipos; i++)
            {
                vivos[i] = vivosActuales != null && i < vivosActuales.Length ? vivosActuales[i] : 0;
            }
            Estrellas = Math.Max(estrellas, libre.Estrellas);
            ModoNova = libre.ModoNova && HayNovaQueGenerar();
            int nivel = 1;
            foreach (TipoRespuesta t in libre.Tipos ?? new TipoRespuesta[0])
            {
                if (disponible[(int)t])
                {
                    nivel = Math.Max(nivel, NivelDe(t));
                }
            }
            NivelDesbloqueado = nivel;
        }

        /// <summary>T-historia-libre-23nov: cuántos de este tipo caben a la vez en el modo libre.</summary>
        private int CupoLibre(TipoRespuesta tipo)
        {
            switch (tipo)
            {
                case TipoRespuesta.Patrulla:
                case TipoRespuesta.Camioneta: return p.TopePatrullasLibre;
                case TipoRespuesta.Dron: return p.TopeDronesLibre;
                case TipoRespuesta.RobotGrande: return p.TopeRobotsGrandesLibre;
                case TipoRespuesta.Jefe:
                    AjusteTipoRespuesta a = p.AjusteDe(tipo);
                    return a != null ? a.Tope : 0;
                default: return p.TopeEnemigosLibre;
            }
        }

        // T-1660: con la puerta cerrada la escalada se queda en la policía (niveles 1 a 3); Nova solo entra cuando se abre.
        private int TopeDeNivel(int estrellas)
        {
            int tope = NivelMaximoPara(estrellas);
            return PuertaNovaAbierta ? tope : Math.Min(tope, PrimerNivelNova - 1);
        }

        private void Reiniciar()
        {
            puertaNova = false;
            camionetasLlegadas = 0;
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
            // T-1631: en modo Nova hay que vencer más de uno por nivel (1 y luego "1 o 2 más") antes de que llegue el siguiente nivel.
            int necesarias = ModoNova && nivel >= PrimerNivelNova ? Math.Max(1, p.PerdidasNovaParaSubir) : 1;
            return (perdidasDelNivel >= necesarias && vivosDelNivel == 0) || ahora - desdeNivel >= p.SegundosParaSubir;
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
            if (libre != null)
            {
                return libre.Permite(tipo) ? CupoLibre(tipo) : 0;
            }
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
            if (libre != null)
            {
                // T-historia-libre-23nov: goteo hasta los topes del modo libre; las patrullas tampoco salen si ya no caben sus policías.
                if (!libre.Permite(tipo) || !disponible[(int)tipo] || ahora < proximo[(int)tipo] || vivos[(int)tipo] >= CupoLibre(tipo))
                {
                    return false;
                }
                if (tipo == TipoRespuesta.Patrulla || tipo == TipoRespuesta.Camioneta)
                {
                    return Vivos(TipoRespuesta.Patrulla) + Vivos(TipoRespuesta.Camioneta) < p.TopePatrullasLibre && VivosLibre < p.TopeEnemigosLibre;
                }
                return VivosLibre < p.TopeEnemigosLibre;
            }
            int nivel = NivelDe(tipo);
            if (NivelDesbloqueado <= 0 || nivel > NivelDesbloqueado || nivel > TopeDeNivel(Estrellas)
                || !disponible[(int)tipo] || !PermitidoPorMision(tipo) || BloqueadoPorModoNova(tipo))
            {
                return false;
            }
            if (ModoNova && CuentaParaTopeNova(tipo) && NovaVivos >= p.TopeNovaVivos)
            {
                return false; // T-1631: nunca más de TopeNovaVivos de NOVA a la vez
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
            if (libre != null)
            {
                proximo[(int)tipo] = Math.Max(proximo[(int)tipo], ahora + p.SegundosReposicionLibre); // T-historia-libre-23nov: reemplazo pronto
                return;
            }
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
