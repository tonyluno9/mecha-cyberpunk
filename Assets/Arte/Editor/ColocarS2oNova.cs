using UnityEditor;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-888 (playtest 7-nov, sesión 2): los callejones rotos detrás de la calle de Nova eran las rendijas de 1.5 m entre las torres del kit
    /// (del lado norte cruzaban a la calle de atrás por un pasillo de un metro; del lado sur terminaban en bolsas sin salida, ver
    /// docs/capturas/s2t-mapa-paso.png). Las torres van juntas (ColocarS2cKit.SeparacionTorres), se rehace Nova y se vuelve a poner la luz
    /// de T-880 (los postes de Nova se crean de nuevo). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oNova.Colocar
    /// </summary>
    public static class ColocarS2oNova
    {
        [MenuItem("Mecha Cyberpunk/S2o/9 Nova sin rendijas entre torres")]
        public static void Colocar()
        {
            ColocarS2cNova.Colocar();
            ColocarS2.Abrir();
            ColocarS2oLuz.Numeros();
            ColocarS2.Guardar("s2o-nova");
        }

        /// <summary>La calle de Nova de noche y desde arriba (docs/capturas/s2o-nova-*.png). Batchmode: ...ColocarS2oNova.Fotos</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            ColocarS2oLuz.Numeros();
            ColocarS2cLook.Foto(new UnityEngine.Vector3(58f, 2.6f, ColocarS2cNova.ZCentro - 0.5f), new UnityEngine.Vector3(130f, 5f, ColocarS2cNova.ZCentro), "docs/capturas/s2o-nova-calle.png", 66f);
            ColocarS2.LadoALado("imageassets/referencias/look/K06_CalleNova.png", "docs/capturas/s2o-nova-calle.png", "docs/capturas/s2o-nova-k06.png");
            ColocarS2cLook.Foto(new UnityEngine.Vector3(94f, 14f, -66f), new UnityEngine.Vector3(94f, 4f, -50f), "docs/capturas/s2o-nova-atras.png", 70f);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ColocarS2.RutaEscena, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }
    }
}
