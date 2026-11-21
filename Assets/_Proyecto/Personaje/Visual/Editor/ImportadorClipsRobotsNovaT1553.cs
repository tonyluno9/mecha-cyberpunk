using System.IO;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1553: configura la importación de los clips que viven en las carpetas Clips de E16 y E18. Corre después de PostprocesadorAssetsT250 y de PostprocesadorArte (que tratan
    /// Assets/Arte/Personajes/E16_* y E18_* como el modelo del personaje: con material, avatar propio y escala de su userData) y deja cada clip como lo importaba
    /// ImportadorClipsPJ01 en PJ01/Clips (armadura PJ01) o con el rig del personaje (<see cref="ClipsRobotsNovaT1553.ConfigurarRigPropio"/>).
    /// </summary>
    public class ImportadorClipsRobotsNovaT1553 : AssetPostprocessor
    {
        private const int OrdenDespuesDeTodos = 300;

        public override int GetPostprocessOrder() => OrdenDespuesDeTodos;

        private void OnPreprocessModel()
        {
            if (!ClipsRobotsNovaT1553.EsClipDeRobot(assetPath))
            {
                return;
            }
            string nombre = Path.GetFileNameWithoutExtension(assetPath);
            var importador = (ModelImporter)assetImporter;
            if (ClipsRobotsNovaT1553.TieneRigPropio(nombre))
            {
                ClipsRobotsNovaT1553.ConfigurarRigPropio(importador, nombre);
            }
            else
            {
                ImportadorClipsPJ01.ConfigurarModelo(importador);
            }
        }
    }
}
