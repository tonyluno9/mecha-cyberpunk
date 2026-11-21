using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1553: la fase de disparo del cañón del robot pesado E16 (decisión de Tony 20-nov: "no camina al disparar: se detiene, apunta con el brazo del cañón y dispara").
    /// Lógica pura, sin escena. Ciclo: Inactivo (enfriamiento) -> Aviso (se detiene, gira hacia el jugador y avisa) -> Disparo (juega E16_Disparar_Canon, 37 cuadros a 30 por
    /// segundo; el tiro sale con el brazo extendido, hacia el cuadro 10) -> Inactivo. Mientras <see cref="Bloqueando"/> la conducta no debe mover al robot: nunca camina disparando.
    /// La conducta decide cuándo pedirlo (solo si persigue y el jugador está en el rango) y traduce <see cref="Salida"/> a aviso, clip, daño y ruido.
    /// </summary>
    public sealed class ReglaCanonE16
    {
        public enum Fase { Inactivo, Aviso, Disparo }

        public readonly struct Parametros
        {
            public readonly float DistanciaMinima;
            public readonly float DistanciaMaxima;
            public readonly float SegundosAviso;
            public readonly float SegundosAlImpacto;
            public readonly float SegundosClip;
            public readonly float SegundosEnfriamiento;
            public readonly float SegundosPrimerTiro;
            public readonly float ToleranciaGrados;
            public readonly float Danio;

            public Parametros(float distanciaMinima, float distanciaMaxima, float segundosAviso, float segundosAlImpacto, float segundosClip, float segundosEnfriamiento,
                float segundosPrimerTiro, float toleranciaGrados, float danio)
            {
                DistanciaMinima = distanciaMinima;
                DistanciaMaxima = distanciaMaxima;
                SegundosAviso = segundosAviso;
                SegundosAlImpacto = segundosAlImpacto;
                SegundosClip = segundosClip;
                SegundosEnfriamiento = segundosEnfriamiento;
                SegundosPrimerTiro = segundosPrimerTiro;
                ToleranciaGrados = toleranciaGrados;
                Danio = danio;
            }

            /// <summary>Cuadros por segundo de E16_Disparar_Canon (BITACORA de Blender).</summary>
            public const float CuadrosPorSegundo = 30f;
            /// <summary>Cuadros del clip E16_Disparar_Canon.</summary>
            public const int CuadrosClip = 37;
            /// <summary>Cuadro del clip en que el brazo del cañón ya está extendido y sale el tiro (~10 de 37).</summary>
            public const int CuadroDisparo = 10;

            /// <summary>
            /// Valores de diseño: solo dispara si el jugador está entre 4 y 25 m (más cerca golpea con el escudo, ver ReglaGuardiaPesado), avisa 0.8 s, el tiro sale en el
            /// cuadro 10, el clip dura sus 37 cuadros, espera 6 s entre tiros (2.5 s antes del primero) y solo conecta si el jugador sigue a 8 grados o menos de la mira.
            /// </summary>
            public static Parametros Diseno => new Parametros(4f, 25f, 0.8f, CuadroDisparo / CuadrosPorSegundo, CuadrosClip / CuadrosPorSegundo, 6f, 2.5f, 8f, 22f);
        }

        public struct Entrada
        {
            /// <summary>Distancia en el plano al jugador (m).</summary>
            public float Distancia;
            /// <summary>True si está peleando contra el jugador (provocado y atacable).</summary>
            public bool EnPelea;
            /// <summary>True si la conducta permite empezar un tiro ahora (persigue; no está aturdido, golpeando ni recuperándose).</summary>
            public bool PuedeIniciar;
        }

        public struct Salida
        {
            /// <summary>Empieza el aviso: se detiene y gira hacia el jugador.</summary>
            public bool EmpezoAviso;
            /// <summary>Empieza el clip E16_Disparar_Canon.</summary>
            public bool EmpezoClip;
            /// <summary>El tiro sale en este cuadro (la conducta comprueba la mira y hace el daño).</summary>
            public bool Disparo;
        }

        private readonly Parametros p;
        private float tFase;
        private float enfriamiento;
        private bool disparado;

        public ReglaCanonE16(Parametros parametros)
        {
            p = parametros;
            enfriamiento = p.SegundosPrimerTiro;
        }

        public Parametros Config => p;
        public Fase Actual { get; private set; }
        public int Disparos { get; private set; }
        public float SegundosEnFase => tFase;
        public float SegundosDeEnfriamiento => enfriamiento;

        /// <summary>True durante el aviso y el clip: el robot tiene que estar quieto.</summary>
        public bool Bloqueando => Actual != Fase.Inactivo;

        /// <summary>Avance del aviso de 0 a 1 (0 fuera del aviso).</summary>
        public float ProgresoAviso => Actual == Fase.Aviso ? Math.Min(1f, tFase / p.SegundosAviso) : 0f;

        /// <summary>True si <paramref name="distancia"/> cae en el rango de tiro.</summary>
        public bool EnRango(float distancia) => distancia >= p.DistanciaMinima && distancia <= p.DistanciaMaxima;

        /// <summary>Lo deja como al inicio (primer tiro tras <see cref="Parametros.SegundosPrimerTiro"/>).</summary>
        public void Reiniciar()
        {
            Actual = Fase.Inactivo;
            tFase = 0f;
            enfriamiento = p.SegundosPrimerTiro;
            disparado = false;
            Disparos = 0;
        }

        /// <summary>Corta el tiro (se rompió el escudo, el jugador dejó de ser atacable...). Queda un enfriamiento corto.</summary>
        public void Cancelar()
        {
            Actual = Fase.Inactivo;
            tFase = 0f;
            disparado = false;
            enfriamiento = Math.Max(enfriamiento, p.SegundosAviso);
        }

        public Salida Avanzar(float dt, Entrada e)
        {
            Salida s = default;
            switch (Actual)
            {
                case Fase.Inactivo:
                    if (e.EnPelea)
                    {
                        enfriamiento = Math.Max(0f, enfriamiento - dt); // el enfriamiento corre solo peleando
                    }
                    if (e.EnPelea && e.PuedeIniciar && enfriamiento <= 0f && EnRango(e.Distancia))
                    {
                        Actual = Fase.Aviso;
                        tFase = 0f;
                        s.EmpezoAviso = true;
                    }
                    break;
                case Fase.Aviso:
                    if (!e.EnPelea)
                    {
                        Cancelar();
                        break;
                    }
                    tFase += dt;
                    if (tFase >= p.SegundosAviso)
                    {
                        Actual = Fase.Disparo;
                        tFase = 0f;
                        disparado = false;
                        s.EmpezoClip = true;
                    }
                    break;
                case Fase.Disparo:
                    tFase += dt;
                    if (!disparado && tFase >= p.SegundosAlImpacto)
                    {
                        disparado = true;
                        Disparos++;
                        s.Disparo = true;
                    }
                    if (tFase >= p.SegundosClip)
                    {
                        Actual = Fase.Inactivo;
                        tFase = 0f;
                        disparado = false;
                        enfriamiento = p.SegundosEnfriamiento;
                    }
                    break;
            }
            return s;
        }
    }
}
