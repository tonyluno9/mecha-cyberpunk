using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1110: pelea del jefe final de Nova (E14) en tres fases por vida: Asedio (hasta 66%), Escudo (66% a 33%) y Furia (bajo 33%). Lógica pura, sin escena.
    /// Cada fase repite un ciclo de acciones: Asedio puñetazo, puñetazo, cañonazo; Escudo puñetazo, onda de choque, doble cañonazo; Furia puñetazo y onda
    /// seguidos (combo) y una ráfaga de 3 cañonazos. El escudo de energía (140 puntos) absorbe todo el daño en la fase Escudo; al romperse deja al jefe
    /// aturdido 3 s y vuelve a los 10 s si la vida sigue sobre 33%. En Furia no hay escudo, el jefe va 30% más rápido y los avisos duran 20% menos.
    /// La conducta traduce la regla a movimiento, aros y daño.
    /// </summary>
    public sealed class ReglaJefeFinalNova
    {
        public enum FaseJefe { Asedio = 1, Escudo = 2, Furia = 3 }

        public enum Accion { Camina, AvisoPunetazo, AvisoCanon, AvisoOnda, Onda, Recupera, Aturdido }

        /// <summary>Lo que toca hacer en el ciclo de la fase.</summary>
        public enum Ataque { Punetazo, Onda, Canon, DobleCanon, Rafaga }

        /// <summary>Dónde cae un aro de cañonazo: en el jugador, o a 3 m delante o detrás de él sobre su dirección de movimiento.</summary>
        public enum LugarAro { Jugador, DelanteDelJugador, DetrasDelJugador }

        public readonly struct Parametros
        {
            public readonly float VidaEscudo;
            public readonly float VidaFuria;
            public readonly float AlcancePunetazo;
            public readonly float AvisoPunetazo;
            public readonly float DanioPunetazo;
            public readonly float RadioPunetazo;
            public readonly float ArcoPunetazo;
            public readonly float RecuperaPunetazo;
            public readonly float RecuperaCombo;
            public readonly float AvisoCanon;
            public readonly float RadioCanon;
            public readonly float DanioCanon;
            public readonly float EscalonRafaga;
            public readonly float RecuperaCanon;
            public readonly float AlcanceOnda;
            public readonly float AvisoOnda;
            public readonly float DuracionOnda;
            public readonly float RadioMaximoOnda;
            public readonly float BandaOnda;
            public readonly float DanioOnda;
            public readonly float RecuperaOnda;
            public readonly float PuntosEscudo;
            public readonly float RegresoEscudo;
            public readonly float Aturdido;
            public readonly float FactorAvisoFuria;
            public readonly float FactorVelocidadFuria;

            public Parametros(float vidaEscudo, float vidaFuria, float alcancePunetazo, float avisoPunetazo, float danioPunetazo, float radioPunetazo, float arcoPunetazo,
                float recuperaPunetazo, float recuperaCombo, float avisoCanon, float radioCanon, float danioCanon, float escalonRafaga, float recuperaCanon,
                float alcanceOnda, float avisoOnda, float duracionOnda, float radioMaximoOnda, float bandaOnda, float danioOnda, float recuperaOnda,
                float puntosEscudo, float regresoEscudo, float aturdido, float factorAvisoFuria, float factorVelocidadFuria)
            {
                VidaEscudo = vidaEscudo;
                VidaFuria = vidaFuria;
                AlcancePunetazo = alcancePunetazo;
                AvisoPunetazo = avisoPunetazo;
                DanioPunetazo = danioPunetazo;
                RadioPunetazo = radioPunetazo;
                ArcoPunetazo = arcoPunetazo;
                RecuperaPunetazo = recuperaPunetazo;
                RecuperaCombo = recuperaCombo;
                AvisoCanon = avisoCanon;
                RadioCanon = radioCanon;
                DanioCanon = danioCanon;
                EscalonRafaga = escalonRafaga;
                RecuperaCanon = recuperaCanon;
                AlcanceOnda = alcanceOnda;
                AvisoOnda = avisoOnda;
                DuracionOnda = duracionOnda;
                RadioMaximoOnda = radioMaximoOnda;
                BandaOnda = bandaOnda;
                DanioOnda = danioOnda;
                RecuperaOnda = recuperaOnda;
                PuntosEscudo = puntosEscudo;
                RegresoEscudo = regresoEscudo;
                Aturdido = aturdido;
                FactorAvisoFuria = factorAvisoFuria;
                FactorVelocidadFuria = factorVelocidadFuria;
            }

            /// <summary>Los valores de docs/diseno/enemigos-nuevos-16nov.md, sección 4. Lo que el diseño no fija (pausas entre ataques, escalón de la ráfaga) está en el valor más corto que se siente justo.</summary>
            public static Parametros Diseno => new Parametros(0.66f, 0.33f, 3f, 0.9f, 25f, 3.5f, 120f, 0.8f, 0.2f, 1.2f, 2.5f, 30f, 0.25f, 0.8f,
                8f, 1.2f, 0.8f, 9f, 0.6f, 20f, 1f, 140f, 10f, 3f, 0.8f, 1.3f);
        }

        public struct Entrada
        {
            /// <summary>Distancia en el plano al jugador (m).</summary>
            public float Distancia;
            /// <summary>Vida del jefe de 0 a 1.</summary>
            public float VidaFraccion;
            /// <summary>False si no hay pelea (el recinto no está cerrado o el jugador no es atacable): el jefe se queda esperando.</summary>
            public bool EnPelea;
        }

        public struct Salida
        {
            /// <summary>Cambió la fase de la pelea (la nueva está en <see cref="ReglaJefeFinalNova.Fase"/>).</summary>
            public bool CambioFase;
            /// <summary>Al pasar a la fase Escudo: grito "¡Escudo activado!" y esfera cian.</summary>
            public bool EscudoActivado;
            /// <summary>El escudo volvió tras 10 s.</summary>
            public bool EscudoRegreso;
            /// <summary>Empieza el aviso de un ataque.</summary>
            public bool EmpezoAviso;
            public Ataque Tipo;
            /// <summary>Cae el puñetazo en este cuadro (la conducta comprueba radio y arco).</summary>
            public bool ImpactoPunetazo;
            /// <summary>Empieza a crecer el aro de la onda de choque.</summary>
            public bool EmpezoOnda;
            /// <summary>Aros de cañonazo que aparecen en este cuadro (null si ninguno), en el orden en que explotarán.</summary>
            public LugarAro[] NuevosAros;
            /// <summary>Cuántos aros explotan en este cuadro (los más viejos primero).</summary>
            public int Explotan;
        }

        public readonly struct ResultadoEscudo
        {
            public readonly bool Absorbido;
            public readonly bool SeRompio;

            public ResultadoEscudo(bool absorbido, bool seRompio)
            {
                Absorbido = absorbido;
                SeRompio = seRompio;
            }
        }

        private readonly struct Tiro
        {
            public readonly float Aparece;
            public readonly float Explota;
            public readonly LugarAro Lugar;

            public Tiro(float aparece, float explota, LugarAro lugar)
            {
                Aparece = aparece;
                Explota = explota;
                Lugar = lugar;
            }
        }

        private static readonly Ataque[] CicloAsedio = { Ataque.Punetazo, Ataque.Punetazo, Ataque.Canon };
        private static readonly Ataque[] CicloEscudo = { Ataque.Punetazo, Ataque.Onda, Ataque.DobleCanon };
        private static readonly Ataque[] CicloFuria = { Ataque.Punetazo, Ataque.Onda, Ataque.Rafaga };

        private readonly Parametros p;
        private readonly List<Tiro> tiros = new List<Tiro>();
        private int apareceSiguiente;
        private int explotaSiguiente;
        private float tAccion;
        private float duracionActual;
        private int indiceCiclo;
        private float regresoRestante;

        public ReglaJefeFinalNova(Parametros parametros)
        {
            p = parametros;
            Reiniciar();
        }

        public Parametros Config => p;
        public FaseJefe Fase { get; private set; }
        public Accion Actual { get; private set; }
        public bool EscudoActivo { get; private set; }
        public float AguanteEscudo { get; private set; }
        /// <summary>Segundos que faltan para que el escudo vuelva (0 si no hay cuenta).</summary>
        public float RegresoEscudoRestante => regresoRestante;
        /// <summary>Solo se le pega x1.5 mientras está aturdido.</summary>
        public bool Castigable => Actual == Accion.Aturdido;
        public Ataque AtaqueActual { get; private set; }
        public float TiempoEnAccion => tAccion;
        /// <summary>Radio (m) del aro de la onda de choque mientras crece (0 fuera de la onda).</summary>
        public float RadioOnda => Actual == Accion.Onda ? Math.Min(p.RadioMaximoOnda, p.RadioMaximoOnda * tAccion / p.DuracionOnda) : 0f;
        /// <summary>Multiplicador de los avisos: 1, o 0.8 en la fase Furia.</summary>
        public float FactorAviso => Fase == FaseJefe.Furia ? p.FactorAvisoFuria : 1f;
        /// <summary>Multiplicador de la velocidad: 1, o 1.3 en la fase Furia.</summary>
        public float FactorVelocidad => Fase == FaseJefe.Furia ? p.FactorVelocidadFuria : 1f;
        public float DuracionAvisoPunetazo => p.AvisoPunetazo * FactorAviso;
        public float DuracionAvisoCanon => p.AvisoCanon * FactorAviso;
        public float DuracionAvisoOnda => p.AvisoOnda * FactorAviso;

        /// <summary>Para pruebas: salta a la posición <paramref name="indice"/> del ciclo de la fase actual (0 = primer ataque).</summary>
        public void ForzarIndice(int indice)
        {
            indiceCiclo = indice;
        }

        /// <summary>El ataque que sigue en el ciclo de la fase.</summary>
        public Ataque Siguiente
        {
            get
            {
                Ataque[] ciclo = CicloDe(Fase);
                return ciclo[indiceCiclo % ciclo.Length];
            }
        }

        /// <summary>Avance del aviso actual de 0 a 1 (0 fuera de un aviso).</summary>
        public float ProgresoAviso
        {
            get
            {
                switch (Actual)
                {
                    case Accion.AvisoPunetazo: return Math.Min(1f, tAccion / DuracionAvisoPunetazo);
                    case Accion.AvisoOnda: return Math.Min(1f, tAccion / DuracionAvisoOnda);
                    case Accion.AvisoCanon: return Math.Min(1f, tAccion / DuracionAvisoCanon);
                    default: return 0f;
                }
            }
        }

        public void Reiniciar()
        {
            Fase = FaseJefe.Asedio;
            Actual = Accion.Camina;
            EscudoActivo = false;
            AguanteEscudo = 0f;
            regresoRestante = 0f;
            tAccion = 0f;
            indiceCiclo = 0;
            tiros.Clear();
            apareceSiguiente = 0;
            explotaSiguiente = 0;
        }

        /// <summary>
        /// El escudo de energía recibe un golpe de <paramref name="cantidad"/> (ya con el castigo). Si está arriba lo absorbe todo; al llegar a 0 puntos se rompe
        /// y el jefe queda aturdido.
        /// </summary>
        public ResultadoEscudo AbsorberDanio(float cantidad)
        {
            if (!EscudoActivo)
            {
                return new ResultadoEscudo(false, false);
            }
            AguanteEscudo = Math.Max(0f, AguanteEscudo - cantidad);
            if (AguanteEscudo > 0f)
            {
                return new ResultadoEscudo(true, false);
            }
            EscudoActivo = false;
            regresoRestante = p.RegresoEscudo;
            Actual = Accion.Aturdido;
            tAccion = 0f;
            tiros.Clear();
            apareceSiguiente = 0;
            explotaSiguiente = 0;
            return new ResultadoEscudo(true, true);
        }

        /// <summary>Un cuadro de <paramref name="dt"/> segundos.</summary>
        public Salida Avanzar(float dt, Entrada e)
        {
            Salida s = default;
            ActualizarFase(e.VidaFraccion, ref s);
            if (regresoRestante > 0f)
            {
                regresoRestante -= dt;
                if (regresoRestante <= 0f)
                {
                    regresoRestante = 0f;
                    if (Fase == FaseJefe.Escudo && e.VidaFraccion > p.VidaFuria)
                    {
                        EscudoActivo = true;
                        AguanteEscudo = p.PuntosEscudo;
                        s.EscudoRegreso = true;
                    }
                }
            }
            if (!e.EnPelea)
            {
                if (Actual != Accion.Camina)
                {
                    Actual = Accion.Camina;
                    tAccion = 0f;
                    tiros.Clear();
                    apareceSiguiente = 0;
                    explotaSiguiente = 0;
                }
                return s;
            }
            tAccion += dt;
            switch (Actual)
            {
                case Accion.Camina:
                    Decidir(e.Distancia, ref s);
                    break;
                case Accion.AvisoPunetazo:
                    if (tAccion >= DuracionAvisoPunetazo)
                    {
                        s.ImpactoPunetazo = true;
                        indiceCiclo++;
                        Recuperar(Fase == FaseJefe.Furia && Siguiente == Ataque.Onda ? p.RecuperaCombo : p.RecuperaPunetazo);
                    }
                    break;
                case Accion.AvisoOnda:
                    if (tAccion >= DuracionAvisoOnda)
                    {
                        Actual = Accion.Onda;
                        tAccion = 0f;
                        s.EmpezoOnda = true;
                    }
                    break;
                case Accion.Onda:
                    if (tAccion >= p.DuracionOnda)
                    {
                        indiceCiclo++;
                        Recuperar(p.RecuperaOnda);
                    }
                    break;
                case Accion.AvisoCanon:
                    AvanzarCanon(ref s);
                    break;
                case Accion.Recupera:
                    if (tAccion >= duracionActual)
                    {
                        Actual = Accion.Camina;
                        tAccion = 0f;
                    }
                    break;
                case Accion.Aturdido:
                    if (tAccion >= p.Aturdido)
                    {
                        Actual = Accion.Camina;
                        tAccion = 0f;
                    }
                    break;
            }
            return s;
        }

        private static Ataque[] CicloDe(FaseJefe fase)
        {
            switch (fase)
            {
                case FaseJefe.Escudo: return CicloEscudo;
                case FaseJefe.Furia: return CicloFuria;
                default: return CicloAsedio;
            }
        }

        private void ActualizarFase(float vida, ref Salida s)
        {
            FaseJefe nueva = vida <= p.VidaFuria ? FaseJefe.Furia : vida <= p.VidaEscudo ? FaseJefe.Escudo : FaseJefe.Asedio;
            if (nueva <= Fase)
            {
                return;
            }
            Fase = nueva;
            indiceCiclo = 0;
            s.CambioFase = true;
            if (Fase == FaseJefe.Escudo)
            {
                EscudoActivo = true;
                AguanteEscudo = p.PuntosEscudo;
                regresoRestante = 0f;
                s.EscudoActivado = true;
            }
            else
            {
                EscudoActivo = false;
                regresoRestante = 0f;
            }
        }

        private void Recuperar(float segundos)
        {
            Actual = Accion.Recupera;
            tAccion = 0f;
            duracionActual = segundos;
        }

        private void Decidir(float distancia, ref Salida s)
        {
            Ataque sig = Siguiente;
            bool puede = sig == Ataque.Punetazo ? distancia <= p.AlcancePunetazo : sig == Ataque.Onda ? distancia <= p.AlcanceOnda : true;
            if (!puede)
            {
                return;
            }
            AtaqueActual = sig;
            tAccion = 0f;
            s.EmpezoAviso = true;
            s.Tipo = sig;
            switch (sig)
            {
                case Ataque.Punetazo:
                    Actual = Accion.AvisoPunetazo;
                    break;
                case Ataque.Onda:
                    Actual = Accion.AvisoOnda;
                    break;
                default:
                    Actual = Accion.AvisoCanon;
                    ArmarTiros(sig);
                    AvanzarCanon(ref s);
                    break;
            }
        }

        private void ArmarTiros(Ataque tipo)
        {
            tiros.Clear();
            apareceSiguiente = 0;
            explotaSiguiente = 0;
            float a = DuracionAvisoCanon;
            switch (tipo)
            {
                case Ataque.DobleCanon:
                    tiros.Add(new Tiro(0f, a, LugarAro.Jugador));
                    tiros.Add(new Tiro(a, 2f * a, LugarAro.Jugador));
                    break;
                case Ataque.Rafaga:
                    tiros.Add(new Tiro(0f, a, LugarAro.Jugador));
                    tiros.Add(new Tiro(0f, a + p.EscalonRafaga, LugarAro.DelanteDelJugador));
                    tiros.Add(new Tiro(0f, a + 2f * p.EscalonRafaga, LugarAro.DetrasDelJugador));
                    break;
                default:
                    tiros.Add(new Tiro(0f, a, LugarAro.Jugador));
                    break;
            }
        }

        private void AvanzarCanon(ref Salida s)
        {
            List<LugarAro> nuevos = null;
            while (apareceSiguiente < tiros.Count && tiros[apareceSiguiente].Aparece <= tAccion)
            {
                nuevos = nuevos ?? new List<LugarAro>();
                nuevos.Add(tiros[apareceSiguiente].Lugar);
                apareceSiguiente++;
            }
            if (nuevos != null)
            {
                s.NuevosAros = s.NuevosAros == null ? nuevos.ToArray() : s.NuevosAros;
            }
            while (explotaSiguiente < apareceSiguiente && tiros[explotaSiguiente].Explota <= tAccion)
            {
                s.Explotan++;
                explotaSiguiente++;
            }
            if (explotaSiguiente >= tiros.Count)
            {
                indiceCiclo++;
                Recuperar(p.RecuperaCanon);
            }
        }
    }
}
