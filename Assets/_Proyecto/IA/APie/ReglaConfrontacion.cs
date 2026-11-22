namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1651 (T26): la máquina de la confrontación de un grupo de maleantes de calle, sin Unity. Reposo: nadie te detectó. Advertencia: te detectaron y
    /// advierten un rato (se acercan un poco, provocan). Empujon: te quedaste tras la advertencia: solo empujan y tiran. Pelea: tú atacaste (la IA normal pelea).
    /// Retirada: te alejaste o se aburrieron; vuelven a su lugar hasta que salgas de su detección. El tiempo entra por parámetro.
    /// </summary>
    public sealed class ReglaConfrontacion
    {
        public enum Fase
        {
            Reposo,
            Advertencia,
            Empujon,
            Pelea,
            Retirada
        }

        public readonly struct Parametros
        {
            public readonly float DistanciaDeteccion;
            public readonly float SegundosAdvertencia;
            public readonly float DistanciaQuedarse;
            public readonly float DistanciaDejarDeSeguir;
            public readonly float SegundosRetirada;
            public readonly int EmpujonesMaximos;

            public Parametros(float distanciaDeteccion, float segundosAdvertencia, float distanciaQuedarse, float distanciaDejarDeSeguir, float segundosRetirada, int empujonesMaximos)
            {
                DistanciaDeteccion = distanciaDeteccion;
                SegundosAdvertencia = segundosAdvertencia;
                DistanciaQuedarse = distanciaQuedarse;
                DistanciaDejarDeSeguir = distanciaDejarDeSeguir;
                SegundosRetirada = segundosRetirada;
                EmpujonesMaximos = empujonesMaximos;
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
        public int Empujones { get; private set; }

        public ReglaConfrontacion(Parametros parametros) => p = parametros;

        public float SegundosAdvertenciaRestantes => Actual == Fase.Advertencia ? System.Math.Max(0f, p.SegundosAdvertencia - SegundosEnFase) : 0f;

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
            bool lejos = !e.Alerta || e.Distancia > p.DistanciaDejarDeSeguir;
            switch (Actual)
            {
                case Fase.Reposo:
                    if (e.Alerta && e.Distancia <= p.DistanciaDeteccion)
                    {
                        Ir(Fase.Advertencia);
                    }
                    break;
                case Fase.Advertencia:
                    if (lejos)
                    {
                        Ir(Fase.Retirada);
                    }
                    else if (SegundosEnFase >= p.SegundosAdvertencia)
                    {
                        Ir(e.Distancia <= p.DistanciaQuedarse ? Fase.Empujon : Fase.Retirada);
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
                    if ((!e.Alerta || e.Distancia > p.DistanciaDeteccion) && SegundosEnFase >= p.SegundosRetirada)
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
            if (nueva == Fase.Empujon || nueva == Fase.Advertencia)
            {
                Empujones = 0;
            }
        }
    }
}
