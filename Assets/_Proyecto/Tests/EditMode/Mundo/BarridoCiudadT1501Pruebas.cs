using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1501: la ciudad no tiene objetos flotando, hundidos (carros incluidos) ni calles que pierdan la textura (reglas en BarridoCiudadT1501).</summary>
    public class BarridoCiudadT1501Pruebas
    {
        private Scene escena;
        private List<BarridoCiudadT1501.Hallazgo> hallazgos;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(BarridoCiudadT1501.RutaEscena, OpenSceneMode.Additive);
            hallazgos = BarridoCiudadT1501.Analizar(escena);
        }

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        private string[] De(params BarridoCiudadT1501.Tipo[] tipos)
        {
            return hallazgos.Where(h => tipos.Contains(h.Tipo) && (h.Arreglable || h.Tipo > BarridoCiudadT1501.Tipo.CarroHundido)).Select(h => h.Tipo + " " + h.Medida.ToString("F2") + " " + h.Ruta).ToArray();
        }

        [Test]
        public void Ciudad_SinObjetosFlotando() => Assert.IsEmpty(De(BarridoCiudadT1501.Tipo.Flotante, BarridoCiudadT1501.Tipo.CarroFlotante));

        [Test]
        public void Ciudad_SinObjetosNiCarrosHundidos() => Assert.IsEmpty(De(BarridoCiudadT1501.Tipo.Hundido, BarridoCiudadT1501.Tipo.CarroHundido));

        [Test]
        public void Calles_SinMaterialNiShaderDeError() => Assert.IsEmpty(De(BarridoCiudadT1501.Tipo.CalleSinMaterial, BarridoCiudadT1501.Tipo.CalleShaderError));

        [Test]
        public void Calles_ConTexturaYEscalaValida() =>
            Assert.IsEmpty(De(BarridoCiudadT1501.Tipo.CalleSinTextura, BarridoCiudadT1501.Tipo.CalleEscalaCero, BarridoCiudadT1501.Tipo.CalleTilingExtremo, BarridoCiudadT1501.Tipo.CalleSinUV));
    }
}
