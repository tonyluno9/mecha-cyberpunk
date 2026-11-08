using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-883 (playtest 7-nov, sesión 2): el taller El Canepa a escala real (cabe un carro con espacio: 16 x 8 m y 5.4 m de alto, en
    /// ColocarS2nPuertas), con el letrero L15 de Tony y Canepa de 1.80 m (ColocarS2tTaller). Rehace los dos en orden. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oTaller.Colocar
    /// </summary>
    public static class ColocarS2oTaller
    {
        [MenuItem("Mecha Cyberpunk/S2o/4 Taller El Canepa a escala")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            ColocarS2nPuertas.TallerMecanico();
            string informe = ColocarS2.Informe.ToString();
            ColocarS2tTaller.Colocar();
            ColocarS2.Informe.Insert(0, informe);
            ColocarS2.Guardar("s2o-taller");
        }

        public static void Fotos(string cuando)
        {
            ColocarS2cLook.Foto(new Vector3(-50f, 2f, -71.5f), new Vector3(-34f, 3f, -71.5f), "docs/capturas/s2o-taller-fachada-" + cuando + ".png", 60f);
            ColocarS2cLook.Foto(new Vector3(-36.3f, 1.7f, -74.8f), new Vector3(-26f, 1.4f, -69f), "docs/capturas/s2o-taller-dentro-" + cuando + ".png", 75f);
            ColocarS2cLook.Foto(new Vector3(-33.5f, 1.6f, -69.2f), new Vector3(-35.4f, 1.1f, -70.6f), "docs/capturas/s2o-taller-canepa-" + cuando + ".png", 55f);
        }
    }
}
