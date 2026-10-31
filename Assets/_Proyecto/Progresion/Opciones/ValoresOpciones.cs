using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Opciones
{
    /// <summary>
    /// Lo que se guarda en opciones.json (T-411), aparte de las partidas. Los volúmenes van de 0 a 1 en el orden de
    /// <see cref="Core.SeccionSonido"/>; resolución 0 x 0 = la nativa de la pantalla. <see cref="ReglaOpciones"/> lo recorta y lo convierte.
    /// </summary>
    [Serializable]
    public class ValoresOpciones
    {
        public float sensibilidad = 1f;
        public bool invertirY;
        public NivelApuntadoAsistido apuntadoAsistido = NivelApuntadoAsistido.Suave;
        public float[] volumenes = new float[0];
        public CalidadGrafica calidad = CalidadGrafica.Alta;
        public int resolucionAncho;
        public int resolucionAlto;
        public bool pantallaCompleta = true;

        public ValoresOpciones Copiar()
        {
            ValoresOpciones copia = (ValoresOpciones)MemberwiseClone();
            copia.volumenes = (float[])volumenes.Clone();
            return copia;
        }
    }
}
