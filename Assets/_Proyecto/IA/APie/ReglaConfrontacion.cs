namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1659 (T41, sobre T-1651): la máquina de la confrontación de un grupo de maleantes de calle, sin Unity. No atacan de primeras.
    /// Reposo: nadie te vio. Mirando: te vieron de lejos y te miran (nadie se mueve). Si te quedas cerca el tiempo de la config, pasa a Acercamiento:
    /// UNO solo se acerca caminando lento. Al llegar, Empujon: te empuja. Pelea: tú atacaste a cualquiera del grupo (los demás vienen).
    /// Retirada: te alejaste o se aburrieron; vuelven a su lugar. El tiempo entra por parámetro.
    /// </summary>
    public sealed class ReglaConfrontacion
    {
        public enum Fase
        {
            Reposo,
            Mirando,
            Acercamiento,
            Empujon,
            Pelea,
            Retirada
        }

        public readonly struct Parametros
        {
            /// <summary>Te ven y te miran a esta distancia o menos (m).</summary>
            public readonly float DistanciaDeteccion;
            /// <summary>Segundos seguidos cerca antes de que uno se acerque.</summary>
            public readonly float SegundosCerca;
            /// <summary>Distancia (m) a la que cuenta como estar cerca.</summary>
            public readonly float DistanciaCerca;
            /// <summary>Una vez que uno se acerca, si te alejas más que esto (m) se acaba todo.</summary>
            public readonly float DistanciaDejarDeSeguir;
            public readonly float SegundosRetirada;
            public readonly int EmpujonesMaximos;
            /// <summary>El que se acerca empieza a empujar a esta distancia o menos (m).</summary>
            public readonly float DistanciaContacto;

            public Parametros(float distanciaDeteccion, float segundosCerca, float distanciaCerca, float distanciaDejarDeSeguir, float segundosRetirada, int empujonesMaximos, float distanciaContacto)
            {
                DistanciaDeteccion = distanciaDeteccion;
                SegundosCerca = segundosCerca;
                DistanciaCerca = distanciaCerca;
                DistanciaDejarDeSeguir = distanciaDejarDeSeguir;
                SegundosRetirada = segundosRetirada;
                EmpujonesMaximos = empujonesMaximos;
                DistanciaContacto = distanciaContacto;
            }
        }

        public struct Entrada
        {
            /// <summary>Distancia en el plano del miembro más cercano al jugador (m).</summary>
            public float Distancia;
            /// <summary>Hay jugador atacable y algún miembro del grupo está en alerta.</summary>
            public bool Alerta;
            /// <summary>El jugador atacó: algún miembro es hostil.</summary>
            public bool Hostil;
        }

        private readonly Parametros p;

        public Fase Actual { get; private set; } = Fase.Reposo;
        public float SegundosEnFase { get; private set; }
        /// <summary>Segundos seguidos que lleva el jugador cerca mientras lo miran (se reinicia si se aleja de "cerca").</summary>
        public float SegundosCerca { get; private set; }
        public int Empujones { get; private set; }

        public ReglaConfrontacion(Parametros parametros) => p = parametros;

        public float SegundosCercaRestantes => Actual == Fase.Mirando ? System.Math.Max(0f, p.SegundosCerca - SegundosCerca) : 0f;

        public void RegistrarEmpujon() => Empujones++;

        /// <summary>Avanza <paramref name="dt"/> segundos. Devuelve true si cambió de fase.</summary>
        public bool Avanzar(float dt, Entrada e)
        {
            SegundosEnFase += dt;
            Fase antes = Actual;
            if (e.Hostil)
            {
                Ir(Fase.Pelea);
                return antes != Actual;
            }
            bool fueraDeVista = !e.Alerta || e.Distancia > p.DistanciaDeteccion;
            bool lejos = !e.Alerta || e.Distancia > p.DistanciaDejarDeSeguir;
            switch (Actual)
            {
                case Fase.Reposo:
                    if (!fueraDeVista)
                    {
                        Ir(Fase.Mirando);
                    }
                    break;
                case Fase.Mirando:
                    if (fueraDeVista)
                    {
                        Ir(Fase.Reposo); // nunca se movieron: solo dejan de mirar
                    }
                    else if (e.Distancia <= p.DistanciaCerca)
                    {
                        SegundosCerca += dt;
                        if (SegundosCerca >= p.SegundosCerca)
                        {
                            Ir(Fase.Acercamiento);
                        }
                    }
                    else
                    {
                        SegundosCerca = 0f; // se alejó de "cerca": el reloj vuelve a empezar
                    }
                    break;
                case Fase.Acercamiento:
                    if (lejos)
                    {
                        Ir(Fase.Retirada);
                    }
                    else if (e.Distancia <= p.DistanciaContacto)
                    {
                        Ir(Fase.Empujon);
                    }
                    break;
                case Fase.Empujon:
                    if (lejos || (p.EmpujonesMaximos > 0 && Empujones >= p.EmpujonesMaximos))
                    {
                        Ir(Fase.Retirada);
                    }
                    break;
                case Fase.Pelea:
                    Ir(Fase.Reposo); // ya no hay hostiles: se calmó
                    break;
                case Fase.Retirada:
                    if (fueraDeVista && SegundosEnFase >= p.SegundosRetirada)
                    {
                        Ir(Fase.Reposo);
                    }
                    break;
            }
            return antes != Actual;
        }

        private void Ir(Fase nueva)
        {
            if (nueva == Actual)
            {
                return;
            }
            Actual = nueva;
            SegundosEnFase = 0f;
            SegundosCerca = 0f;
            Empujones = 0;
        }
    }
}
