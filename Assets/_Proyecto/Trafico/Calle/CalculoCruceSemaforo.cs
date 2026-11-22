using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1630: la lógica de los semáforos de un cruce, sin MonoBehaviour. Un cruce tiene dos ejes (0 y 1) que se alternan: cuando uno está en verde, el otro está en rojo.
    /// Ciclo de un eje: verde, ámbar y rojo; el otro eje hace lo mismo medio ciclo después. Cada eje queda en rojo <c>despeje</c> s antes de que el otro se ponga en verde
    /// (rojo para todos, para que se vacíe el cruce). También decide si un carro debe detenerse y qué poste le toca obedecer.
    /// </summary>
    public sealed class CalculoCruceSemaforo
    {
        /// <summary>Cuántos ejes se alternan en un cruce.</summary>
        public const int Ejes = 2;
        /// <summary>Coseno mínimo entre hacia dónde va el carro y el sentido que controla un poste.</summary>
        public const float MinimoAlineacion = 0.8f;
        /// <summary>Un poste a más de esto (m) a un lado de la marcha del carro no es suyo.</summary>
        public const float LateralMaximo = 14f;
        /// <summary>Un carro que ya rebasó la línea de parada por menos de esto (m) todavía cuenta como antes de ella (se pasó un poco al frenar).</summary>
        public const float ToleranciaLinea = 1.5f;

        private readonly float verde;
        private readonly float ambar;
        private readonly float despeje;

        public CalculoCruceSemaforo(float segundosVerde, float segundosAmbar, float segundosDespeje)
        {
            verde = Mathf.Max(0.01f, segundosVerde);
            ambar = Mathf.Max(0.01f, segundosAmbar);
            despeje = Mathf.Max(0f, segundosDespeje);
        }

        /// <summary>Duración total del ciclo de un cruce (s): los dos ejes pasan por verde, ámbar y rojo.</summary>
        public float Ciclo => 2f * (verde + ambar + despeje);

        /// <summary>La luz del eje (0 o 1) a los <paramref name="tiempo"/> s del ciclo.</summary>
        public FaseCruce FaseEn(int eje, float tiempo)
        {
            float local = Mod(tiempo + (eje == 0 ? 0f : Ciclo * 0.5f), Ciclo);
            if (local < verde)
            {
                return FaseCruce.Verde;
            }
            return local < verde + ambar ? FaseCruce.Ambar : FaseCruce.Rojo;
        }

        /// <summary>
        /// true si un carro debe frenar para quedar en la línea de parada, a <paramref name="metrosALinea"/> m de él. Rojo: siempre (si no la rebasó). Ámbar: solo si alcanza a frenar
        /// con <paramref name="desaceleracion"/> (m/s²) yendo a <paramref name="velocidad"/> (m/s); si no, pasa. Verde: nunca.
        /// </summary>
        public static bool DebeDetenerse(FaseCruce fase, float metrosALinea, float velocidad, float desaceleracion)
        {
            if (fase == FaseCruce.Verde || metrosALinea <= -ToleranciaLinea)
            {
                return false;
            }
            if (fase == FaseCruce.Rojo)
            {
                return true;
            }
            float frenado = velocidad * velocidad / (2f * Mathf.Max(0.1f, desaceleracion));
            return frenado <= Mathf.Max(0f, metrosALinea);
        }

        /// <summary>
        /// true si el poste en <paramref name="poste"/>, que le habla a los carros que van hacia <paramref name="sentido"/>, es el que le toca a un carro en <paramref name="posicion"/> que va hacia
        /// <paramref name="adelante"/>: va casi en su sentido, el poste está delante (o apenas rebasado), a un lado razonable y a menos de <paramref name="alcance"/> m de su línea de parada.
        /// <paramref name="metros"/> es lo que le falta hasta la línea de parada (el poste menos <paramref name="margenParada"/>, que cuenta el medio carro y el espacio antes de la línea).
        /// </summary>
        public static bool Controla(Vector3 posicion, Vector3 adelante, Vector3 poste, Vector3 sentido, float margenParada, float alcance, out float metros)
        {
            metros = float.MaxValue;
            adelante.y = 0f;
            sentido.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f || sentido.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            adelante.Normalize();
            sentido.Normalize();
            if (Vector3.Dot(adelante, sentido) < MinimoAlineacion)
            {
                return false;
            }
            Vector3 hacia = poste - posicion;
            hacia.y = 0f;
            float alLargo = Vector3.Dot(hacia, sentido);
            float lateral = Mathf.Abs(Vector3.Dot(hacia, Vector3.Cross(Vector3.up, sentido)));
            if (lateral > LateralMaximo)
            {
                return false;
            }
            metros = alLargo - margenParada;
            return metros <= alcance && metros > -ToleranciaLinea;
        }

        private static float Mod(float x, float m)
        {
            float r = x % m;
            return r < 0f ? r + m : r;
        }
    }
}
