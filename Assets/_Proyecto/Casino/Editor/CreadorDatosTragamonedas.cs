using MechaCyberpunk.Casino.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Casino.Editor
{
    /// <summary>
    /// Crea DatosTragamonedas_Base.asset (T-300) si no existe; no pisa un asset ya ajustado a mano.
    /// Batchmode: -executeMethod MechaCyberpunk.Casino.Editor.CreadorDatosTragamonedas.Crear
    /// </summary>
    public static class CreadorDatosTragamonedas
    {
        public const string RutaDatos = "Assets/_Proyecto/Casino/Datos/DatosTragamonedas_Base.asset";
        private const int IndiceCereza = 0;
        private const int MultiplicadorDosCerezas = 1;
        private const int ApuestaBase = 50;

        [MenuItem("Mecha Cyberpunk/Crear T-300")]
        public static void Crear()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosTragamonedas>(RutaDatos) != null)
            {
                Debug.Log("[Casino] DatosTragamonedas_Base ya existe; no se toca.");
                return;
            }
            DatosTragamonedas datos = ScriptableObject.CreateInstance<DatosTragamonedas>();
            datos.Fijar(new[]
            {
                new SimboloTragamonedas { nombre = "CEREZA", peso = 40, multiplicadorTres = 6 },
                new SimboloTragamonedas { nombre = "LIMON", peso = 25, multiplicadorTres = 8 },
                new SimboloTragamonedas { nombre = "CAMPANA", peso = 18, multiplicadorTres = 15 },
                new SimboloTragamonedas { nombre = "SIETE", peso = 10, multiplicadorTres = 40 },
                new SimboloTragamonedas { nombre = "NEON", peso = 7, multiplicadorTres = 100 },
            }, IndiceCereza, MultiplicadorDosCerezas, ApuestaBase);
            AssetDatabase.CreateAsset(datos, RutaDatos);
            AssetDatabase.SaveAssets();
            Debug.Log("[Casino] T-300 creada: DatosTragamonedas_Base.asset.");
        }
    }
}
