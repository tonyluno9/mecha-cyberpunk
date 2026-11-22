using System.Text;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1654 (T28): para comprobar los pasos en Play por eval. <see cref="Ultimo"/> dice qué clip, tono y volumen tuvo el último sonido puntual;
    /// <see cref="Listar"/> cuenta los clips de cada piso; <see cref="Sonar"/> toca un paso de ese piso en la posición del oyente.
    /// </summary>
    public static class DepuracionPasos
    {
        private static readonly string[] Pisos = { "PAS_CONCRETO", "PAS_METAL", "PAS_MADERA", "PAS_MOJADO", "PAS_TIERRA", "PAS_GRAVA" };

        public static string Ultimo()
        {
            GestorSonidos g = Registro<ISonidos>.Actual as GestorSonidos;
            if (g == null)
            {
                return "[Pasos] no hay GestorSonidos";
            }
            string texto = "[Pasos] ultimo id " + g.UltimoIdClip + " clip " + (g.UltimoClip != null ? g.UltimoClip.name : "-") + " tono " + g.UltimoTono.ToString("F3")
                + " volumen relativo " + g.UltimoVolumenRelativo.ToString("F3");
            Debug.Log(texto);
            return texto;
        }

        public static string Listar()
        {
            GestorSonidos g = Registro<ISonidos>.Actual as GestorSonidos;
            if (g == null || g.Catalogo == null)
            {
                return "[Pasos] no hay catalogo";
            }
            StringBuilder sb = new StringBuilder("[Pasos]");
            foreach (string id in Pisos)
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                int n = 0;
                string nombres = string.Empty;
                foreach (CatalogoSonidos.Entrada e in g.Catalogo.Entradas)
                {
                    if (e != null && e.id == id)
                    {
                        foreach (AudioClip c in e.finales)
                        {
                            n++;
                            nombres += (nombres.Length > 0 ? "," : string.Empty) + (c != null ? c.name : "null");
                        }
                    }
                }
                sb.Append(" | ").Append(id).Append(' ').Append(n).Append(" clips [").Append(nombres).Append("] vol ").Append(p.Volumen.ToString("F2"))
                    .Append(" tono +-").Append(p.VariacionTono.ToString("F2")).Append(" varVol ").Append(p.VariacionVolumen.ToString("F2"));
            }
            string texto = sb.ToString();
            Debug.Log(texto);
            return texto;
        }

        /// <summary>Toca un paso de este piso (por ejemplo "PAS_METAL") donde está la cámara y escribe qué sonó.</summary>
        public static string Sonar(string id)
        {
            Camera c = Camera.main;
            Sonidos.ReproducirEn(id, c != null ? c.transform.position : Vector3.zero);
            return Ultimo();
        }
    }
}
