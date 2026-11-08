using System.Linq;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-137: Build Settings tiene exactamente el inicio, sus calles y las escenas de la demo, en orden y habilitadas.</summary>
    public class BuildSettingsDemoPruebas
    {
        private const string RutaDatosEscenasDemo = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        private const string RutaInicio = "Assets/_Proyecto/Escenas/Inicio.unity";
        private const string RutaMenuCalles = "Assets/_Proyecto/Escenas/Menu_Calles.unity";
        private const string RutaTitulo = "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity";
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaFlashback = "Assets/_Proyecto/Escenas/Flashback.unity";

        [Test]
        public void Escenas_InicioPrimeroYLasDeLaDemo_EnOrdenYHabilitadas()
        {
            EditorBuildSettingsScene[] escenas = EditorBuildSettings.scenes;
            CollectionAssert.AreEqual(new[] { RutaInicio, RutaMenuCalles, RutaTitulo, RutaCiudad, RutaFlashback }, escenas.Select(e => e.path).ToArray(),
                "Inicio (índice 0), Menu_Calles, CreacionPersonaje, Ciudad_Prologo y Flashback, nada más");
            Assert.IsTrue(escenas.All(e => e.enabled), "Todas habilitadas");
        }

        [Test]
        public void Escenas_CoincidenConDatosEscenasDemo()
        {
            DatosEscenas datos = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasDemo);
            Assert.IsNotNull(datos, "Existe DatosEscenas_Demo");
            CollectionAssert.AreEqual(new[] { datos.RutaTitulo, datos.RutaCiudad, datos.RutaFlashback },
                EditorBuildSettings.scenes.Skip(2).Select(e => e.path).ToArray(), "El build carga por ruta lo que dice DatosEscenas_Demo");
        }

        [Test]
        public void Escenas_CadaRutaEsUnaEscenaQueExiste()
        {
            foreach (EditorBuildSettingsScene escena in EditorBuildSettings.scenes)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(escena.path), "Existe " + escena.path);
            }
        }
    }
}
