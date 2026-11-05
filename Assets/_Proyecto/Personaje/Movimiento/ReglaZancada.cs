namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// Fase de la zancada (T-596): acumula la distancia recorrida en el piso y avisa cada vez que cae un pie. Así los pasos suenan al ritmo
    /// real de la pisada (a más velocidad, más seguidos; parado no suenan) en vez de un temporizador fijo. Alterna pie izquierdo y derecho.
    /// </summary>
    public class ReglaZancada
    {
        private float acumulado;
        private bool tocaDerecho;

        public ReglaZancada(float metrosPorPasoCaminar, float metrosPorPasoCorrer)
        {
            MetrosPorPasoCaminar = metrosPorPasoCaminar;
            MetrosPorPasoCorrer = metrosPorPasoCorrer;
        }

        public float MetrosPorPasoCaminar { get; }
        public float MetrosPorPasoCorrer { get; }

        /// <summary>El pie del último paso (true = derecho).</summary>
        public bool UltimoFueDerecho { get; private set; }

        /// <summary>Pasos dados en total.</summary>
        public int Pasos { get; private set; }

        /// <summary>Suma la distancia recorrida y devuelve cuántos pasos cayeron. En el aire no se acumula.</summary>
        public int Avanzar(float metros, bool corriendo, bool enPiso)
        {
            if (!enPiso || metros <= 0f)
            {
                return 0;
            }
            float zancada = corriendo ? MetrosPorPasoCorrer : MetrosPorPasoCaminar;
            acumulado += metros;
            int dados = 0;
            while (acumulado >= zancada)
            {
                acumulado -= zancada;
                UltimoFueDerecho = tocaDerecho;
                tocaDerecho = !tocaDerecho;
                Pasos++;
                dados++;
            }
            return dados;
        }

        /// <summary>Pone la distancia acumulada (al arrancar a moverse el primer paso cae a media zancada).</summary>
        public void Reiniciar(float metros)
        {
            acumulado = metros;
        }
    }
}
