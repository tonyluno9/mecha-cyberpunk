using System.IO;
using UnityEditor;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1560: reaplica la tabla de decisiones (<see cref="TablaClipsT1560"/>) en cada importación de un clip PJ01_*, NPC_*, E15_*, E16_*, E18_* o E20_*. Corre después de ImportadorClipsPJ01 (orden 200) y de
    /// ImportadorClipsRobotsNovaT1553 (300): esos dos vuelven a poner el rango, la raíz y el bucle por defecto en cada reimportación, y sin este paso el rango, la raíz y la compresión que fija
    /// <see cref="AnimacionesT1560.Aplicar"/> se perderían la próxima vez que Unity reimporte el FBX (por ejemplo al cambiar de rama o reemplazar el archivo).
    /// </summary>
    public class ImportadorClipsT1560 : AssetPostprocessor
    {
        private const int OrdenDespuesDeTodos = 400;

        public override int GetPostprocessOrder() => OrdenDespuesDeTodos;

        private void OnPreprocessModel()
        {
            if (!TablaClipsT1560.EsClipDeLaTabla(assetPath))
            {
                return;
            }
            AnimacionesT1560.AjustarImportador((ModelImporter)assetImporter, Path.GetFileNameWithoutExtension(assetPath));
        }
    }
}
