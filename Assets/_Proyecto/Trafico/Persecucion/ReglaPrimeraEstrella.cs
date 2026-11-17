using MechaCyberpunk.Core;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1303: pegarle a un policía o a una patrulla (golpe, bala o choque) te pone en búsqueda en ese momento: si el calor no llega a la primera estrella,
    /// sube lo justo para llegar (en pasos del 5 % del máximo; los umbrales viven en los datos de Progresion). La víctima es la policía: no hacen falta
    /// más testigos, por eso suma con <see cref="MotivoCalor.Mision"/>, el único motivo que no pregunta a los testigos. IA tiene la misma regla en PandilleroAPie.
    /// </summary>
    public static class ReglaPrimeraEstrella
    {
        public const float FraccionPaso = 0.05f;
        private const int PasosMaximos = 40;

        public static void Subir(ICalor calor)
        {
            if (calor == null || calor.Maximo <= 0f)
            {
                return;
            }
            float paso = calor.Maximo * FraccionPaso;
            for (int i = 0; i < PasosMaximos && calor.Nivel <= 0 && calor.Valor < calor.Maximo; i++)
            {
                calor.Sumar(paso, MotivoCalor.Mision);
            }
        }
    }
}
