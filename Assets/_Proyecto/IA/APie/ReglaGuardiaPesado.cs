using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: patrón de pelea del guardia pesado de Nova (E12). Lógica pura, sin escena. Espera de guardia hasta que lo provocan; ya en pelea persigue
    /// lento, y a <see cref="Parametros.AlcanceInicio"/> o menos ataca: empujón con escudo (aviso 0.7 s, 18) y, cada 3 ataques, un mazazo (aviso 1.1 s, 28).
    /// Se recupera 1.2 s después de cada ataque. Si se le rompe el escudo (<see cref="Aturdir"/>) queda aturdido 1.2 s y castigable.
    /// La conducta traduce las fases a movimiento y comprueba la geometría del impacto.
    /// </summary>
    public sealed class ReglaGuardiaPesado
    {
        public enum Fase { Guardia, Persigue, AvisoEmpujon, AvisoMazazo, Recupera, Aturdido }

        public enum Ataque { Empujon, Mazazo }

        public readonly struct Parametros
        {
            public readonly float AlcanceInicio;
            public readonly float AvisoEmpujon;
            public readonly float DanioEmpujon;
            public readonly float RadioEmpujon;
            public readonly float ArcoEmpujon;
            public readonly float AvisoMazazo;
            public readonly float DanioMazazo;
            public readonly float RadioMazazo;
            public readonly float ArcoMazazo;
            public readonly int AtaquesPorMazazo;
            public readonly float Recupera;
            public readonly float Aturdido;
            public readonly float GradosGiro;

            public Parametros(float alcanceInicio, float avisoEmpujon, float danioEmpujon, float radioEmpujon, float arcoEmpujon, float avisoMazazo,
                float danioMazazo, float radioMazazo, float arcoMazazo, int ataquesPorMazazo, float recupera, float aturdido, float gradosGiro)
            {
                AlcanceInicio = alcanceInicio;
                AvisoEmpujon = avisoEmpujon;
                DanioEmpujon = danioEmpujon;
                RadioEmpujon = radioEmpujon;
                ArcoEmpujon = arcoEmpujon;
                AvisoMazazo = avisoMazazo;
                DanioMazazo = danioMazazo;
                RadioMazazo = radioMazazo;
                ArcoMazazo = arcoMazazo;
                AtaquesPorMazazo = ataquesPorMazazo;
                Recupera = recupera;
                Aturdido = aturdido;
                GradosGiro = gradosGiro;
            }

            /// <summary>Los valores de docs/diseno/enemigos-nuevos-16nov.md, sección 3.</summary>
            public static Parametros Diseno => new Parametros(2f, 0.7f, 18f, 2.2f, 100f, 1.1f, 28f, 2.5f, 120f, 3, 1.2f, 1.2f, 90f);
        }

        public struct Entrada
        {
            /// <summary>Distancia en el plano al jugador (m).</summary>
            public float Distancia;
            /// <summary>True si está provocado y el jugador es atacable y está cerca; si no, el guardia vuelve a su puesto.</summary>
            public bool EnPelea;
        }

        public struct Salida
        {
            /// <summary>Empieza un aviso (de empujón o de mazazo).</summary>
            public bool EmpezoAviso;
            /// <summary>Termina el aviso: el golpe cae en este cuadro (la conducta comprueba si el jugador está en el arco).</summary>
            public bool Impacto;
            public Ataque Tipo;
        }

        private readonly Parametros p;
        private float tFase;
        private Ataque actual;

        public ReglaGuardiaPesado(Parametros parametros)
        {
            p = parametros;
        }

        public Parametros Config => p;
        public Fase Actual { get; private set; }
        /// <summary>Ataques completados (los que ya cayeron) desde el último reinicio.</summary>
        public int AtaquesHechos { get; private set; }
        /// <summary>Solo se le pega x1.5 mientras está aturdido.</summary>
        public bool Castigable => Actual == Fase.Aturdido;
        public Ataque AtaqueActual => actual;
        public float TiempoEnFase => tFase;

        /// <summary>El próximo ataque que lanzará: cada <see cref="Parametros.AtaquesPorMazazo"/>-ésimo es un mazazo.</summary>
        public Ataque Proximo => (AtaquesHechos + 1) % p.AtaquesPorMazazo == 0 ? Ataque.Mazazo : Ataque.Empujon;

        /// <summary>Avance del aviso actual de 0 a 1 (0 fuera de un aviso).</summary>
        public float ProgresoAviso
        {
            get
            {
                switch (Actual)
                {
                    case Fase.AvisoEmpujon: return Math.Min(1f, tFase / p.AvisoEmpujon);
                    case Fase.AvisoMazazo: return Math.Min(1f, tFase / p.AvisoMazazo);
                    default: return 0f;
                }
            }
        }

        public void Reiniciar()
        {
            Actual = Fase.Guardia;
            tFase = 0f;
            AtaquesHechos = 0;
        }

        /// <summary>Se le rompió el escudo: aturdido (interrumpe lo que hacía).</summary>
        public void Aturdir()
        {
            Actual = Fase.Aturdido;
            tFase = 0f;
        }

        /// <summary>Un cuadro de <paramref name="dt"/> segundos.</summary>
        public Salida Avanzar(float dt, Entrada e)
        {
            Salida s = default;
            if (!e.EnPelea)
            {
                if (Actual != Fase.Guardia)
                {
                    Actual = Fase.Guardia;
                    tFase = 0f;
                }
                return s;
            }
            tFase += dt;
            switch (Actual)
            {
                case Fase.Guardia:
                    Actual = Fase.Persigue;
                    tFase = 0f;
                    break;
                case Fase.Persigue:
                    if (e.Distancia <= p.AlcanceInicio)
                    {
                        actual = Proximo;
                        Actual = actual == Ataque.Mazazo ? Fase.AvisoMazazo : Fase.AvisoEmpujon;
                        tFase = 0f;
                        s.EmpezoAviso = true;
                        s.Tipo = actual;
                    }
                    break;
                case Fase.AvisoEmpujon:
                    if (tFase >= p.AvisoEmpujon)
                    {
                        Impactar(ref s);
                    }
                    break;
                case Fase.AvisoMazazo:
                    if (tFase >= p.AvisoMazazo)
                    {
                        Impactar(ref s);
                    }
                    break;
                case Fase.Recupera:
                    if (tFase >= p.Recupera)
                    {
                        Actual = Fase.Persigue;
                        tFase = 0f;
                    }
                    break;
                case Fase.Aturdido:
                    if (tFase >= p.Aturdido)
                    {
                        Actual = Fase.Persigue;
                        tFase = 0f;
                    }
                    break;
            }
            return s;
        }

        private void Impactar(ref Salida s)
        {
            s.Impacto = true;
            s.Tipo = actual;
            AtaquesHechos++;
            Actual = Fase.Recupera;
            tFase = 0f;
        }
    }
}
