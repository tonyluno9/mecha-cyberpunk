using NUnit.Framework;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1700: tras el barrido no quedan tinacos, antenas, aires ni cajas flotando en Ciudad_Prologo.</summary>
    public class BarridoT1700Pruebas
    {
        [Test]
        public void Ciudad_DespuesDelBarrido_NoHayTinacosAntenasAiresNiCajasFlotando()
        {
            Scene escena = EditorSceneManager.OpenScene(BarridoT1700.RutaEscena, OpenSceneMode.Single);
            var flotantes = BarridoT1700.Flotantes(escena);
            var clases = flotantes.Where(f => f.Clase != "otro").Select(f => f.Clase + " " + BarridoT1700.Ruta(f.Unidad)).ToList();
            Assert.IsEmpty(clases, "flotando: " + string.Join(" | ", clases));
        }
    }
}
