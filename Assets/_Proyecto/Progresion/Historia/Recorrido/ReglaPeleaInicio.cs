using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// VR24: lógica pura de la pelea del inicio (<see cref="PasoRecorrido.PeleaSkulls"/>): 3 SKULLS a puños, uno pega a la vez, y la pelea no
    /// deja KO al jugador (los golpes paran cuando le queda poca vida). La usa <see cref="PeleaInicioSkulls"/>; aquí para probarla sin escena.
    /// </summary>
    public static class ReglaPeleaInicio
    {
        public const int CantidadSkulls = 3;
        /// <summary>Distancia (m, en el plano) a la que el SKULL de turno ya puede pegar.</summary>
        public const float DistanciaGolpe = 1.1f;
        /// <summary>
        /// A esta distancia se para el SKULL de turno. VR35: a 0.8 m (cápsulas de 0.3 m) su cuerpo queda al alcance del puño en VR; a 1.05 m quedaba
        /// justo fuera (la mano estirada llega a ~0.65 m) y casi ningún golpe conectaba.
        /// </summary>
        public const float DistanciaPararse = 0.8f;
        /// <summary>Radio al que esperan los que no tienen turno, rodeando al jugador (VR35: más cerca, a 1.8 m).</summary>
        public const float RadioRodeo = 1.8f;
        /// <summary>VR35: un SKULL sin turno que ya está a menos de RadioRodeo + esto del jugador no se mueve (no retrocede si el jugador se le acerca).</summary>
        public const float MargenSinRetroceder = 0.6f;
        /// <summary>Segundos entre golpes del grupo (pega uno a la vez).</summary>
        public const float SegundosEntreGolpes = 1.9f;
        /// <summary>Del inicio del golpe al contacto (la mano llega a ~0.35 s del clip de golpe).</summary>
        public const float SegundosHastaContacto = 0.35f;
        public const float DanioGolpe = 7f;
        /// <summary>Con esta fracción de vida o menos los SKULLS ya no le quitan más: la pelea del inicio no se pierde.</summary>
        public const float FraccionVidaMinima = 0.3f;
        /// <summary>Metros delante del punto de inicio a los que salen los SKULLS.</summary>
        public const float MetrosAparicion = 8f;
        /// <summary>Separación lateral (m) entre los SKULLS al salir.</summary>
        public const float SeparacionAparicion = 1.6f;

        /// <summary>True cuando ya cayeron todos.</summary>
        public static bool Terminada(int vencidos, int total) => total > 0 && vencidos >= total;

        /// <summary>VR35: true si el SKULL sin turno debe quedarse donde está (ya cerca del jugador: no retrocede para que se le pueda pegar).</summary>
        public static bool QuedarseSinTurno(float distanciaJugador) => distanciaJugador <= RadioRodeo + MargenSinRetroceder;

        /// <summary>True si el SKULL de turno puede empezar un golpe ahora.</summary>
        public static bool PuedeGolpear(float distancia, float ahora, float proximoGolpe) => distancia <= DistanciaGolpe && ahora >= proximoGolpe;

        /// <summary>Daño que se aplica con la vida del jugador dada: nunca lo baja de <see cref="FraccionVidaMinima"/> (0 si ya está ahí o menos).</summary>
        public static float Danio(float vidaActual, float vidaMaxima)
        {
            if (vidaMaxima <= 0f)
            {
                return DanioGolpe;
            }
            float margen = vidaActual - vidaMaxima * FraccionVidaMinima;
            return Mathf.Clamp(margen, 0f, DanioGolpe);
        }

        /// <summary>El siguiente con turno entre <paramref name="total"/> (salta a los caídos; -1 si no queda nadie).</summary>
        public static int SiguienteTurno(int actual, bool[] vivos)
        {
            if (vivos == null || vivos.Length == 0)
            {
                return -1;
            }
            for (int i = 1; i <= vivos.Length; i++)
            {
                int j = ((actual < 0 ? -1 : actual) + i) % vivos.Length;
                if (vivos[j])
                {
                    return j;
                }
            }
            return -1;
        }

        /// <summary>
        /// Puesto de espera del SKULL <paramref name="indice"/> (de <paramref name="total"/>) alrededor de <paramref name="jugador"/>: abiertos en abanico
        /// de ±50° hacia <paramref name="haciaElGrupo"/> (de donde vinieron), a <see cref="RadioRodeo"/>.
        /// </summary>
        public static Vector3 Puesto(Vector3 jugador, Vector3 haciaElGrupo, int indice, int total)
        {
            haciaElGrupo.y = 0f;
            haciaElGrupo = haciaElGrupo.sqrMagnitude > 0.0001f ? haciaElGrupo.normalized : Vector3.forward;
            float angulo = total <= 1 ? 0f : Mathf.Lerp(-50f, 50f, indice / (float)(total - 1));
            return jugador + Quaternion.Euler(0f, angulo, 0f) * haciaElGrupo * RadioRodeo;
        }

        /// <summary>Punto de aparición del SKULL <paramref name="indice"/>: delante del inicio y separados a lo ancho.</summary>
        public static Vector3 Aparicion(Vector3 inicio, Vector3 adelante, int indice, int total)
        {
            adelante.y = 0f;
            adelante = adelante.sqrMagnitude > 0.0001f ? adelante.normalized : Vector3.forward;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            float desplazamiento = (indice - (total - 1) * 0.5f) * SeparacionAparicion;
            return inicio + adelante * MetrosAparicion + lado * desplazamiento;
        }
    }
}
