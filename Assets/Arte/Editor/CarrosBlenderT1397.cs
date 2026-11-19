using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1397: vuelve a armar los 11 carros que T-1380 arregló en Blender (V01A, V01B, V02, V03, V04, V05, V06, V09, V10, V22, V23).
    /// Los FBX nuevos ya se copiaron sobre Assets/Arte/Vehiculos/Piezas/&lt;código&gt;/&lt;código&gt;.fbx (mismo .meta). Aquí se arma cada prefab Carro_* con su creador:
    /// V01a, V01b, V02, V03, V04, V05, V06, V09 y V10 con <see cref="CreadorCarrosT1343"/> (conservan tamaño) y V22 y V23 con <see cref="CreadorCarrosT1342"/>.
    /// El armado deja puertas y cofre con bisagra (PuertasCarro) y los cristales con el material transparente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CarrosBlenderT1397.Aplicar
    /// </summary>
    public static class CarrosBlenderT1397
    {
        public static readonly string[] CarrosT1343 = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V09", "V10" };
        public static readonly string[] CarrosT1342 = { "V22", "V23" };
        public const string RutaInforme = "Logs/t1397-carros.txt";

        public static string[] Todos() => CarrosT1343.Concat(CarrosT1342).ToArray();

        [MenuItem("Mecha Cyberpunk/T-1397/Armar carros arreglados")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            foreach (string codigo in Todos())
            {
                CreadorCarrosBlenderT1312.ConfigurarImportador(codigo);
            }
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in CarrosT1343)
            {
                CreadorCarrosT1343.ArmarCarroPrefab(codigo, informe);
            }
            foreach (var c in CreadorCarrosT1342.Carros.Where(c => CarrosT1342.Contains(c.codigo)))
            {
                informe.AppendLine(CreadorCarrosT1342.ArmarUno(c.codigo, c.largo, informe));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Arte] T-1397 carros:\n" + informe);
        }
    }
}
