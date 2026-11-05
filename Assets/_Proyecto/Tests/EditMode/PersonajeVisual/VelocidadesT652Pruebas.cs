using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-652: la rapidez de los estados del controlador base sigue la tabla de ReglasAnimacion; disparo y recarga no se tocan.</summary>
    public class VelocidadesT652Pruebas
    {
        private static AnimatorState Estado(int capa, string nombre)
        {
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            return c.layers[capa].stateMachine.states.First(s => s.state.name == nombre).state;
        }

        [TestCase(0, "CarroEntrar")]
        [TestCase(0, "CarroSalir")]
        [TestCase(0, "AbrirPuerta")]
        [TestCase(0, "Boton")]
        [TestCase(0, "Recoger")]
        [TestCase(0, "SaltoInicio")]
        [TestCase(0, "SaltoAterrizaje")]
        [TestCase(0, "LevantarseBocaArriba")]
        [TestCase(1, "DanoFrente")]
        public void Estado_ConRapidezEnLaTabla_ElControladorLaTiene(int capa, string estado)
        {
            Assert.Greater(ReglasAnimacion.VelocidadEstado(estado), 1f, estado);
            Assert.AreEqual(ReglasAnimacion.VelocidadEstado(estado), Estado(capa, estado).speed, 0.001f, estado);
        }

        [TestCase("PistolaDisparo")]
        [TestCase("PistolaRecargar")]
        [TestCase("PistolaRecargarCaminar")]
        [TestCase("LargaDisparo")]
        [TestCase("LargaRecargar")]
        public void Disparo_Y_Recarga_NoSeTocan(string estado)
        {
            Assert.AreEqual(1f, ReglasAnimacion.VelocidadEstado(estado));
            Assert.AreEqual(1f, Estado(1, estado).speed, 0.001f);
        }

        [Test]
        public void Golpes_UsanElParametroDeRapidez_YPorDefectoEs1ParaLosEnemigos()
        {
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            foreach (string golpe in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte" })
            {
                AnimatorState s = Estado(1, golpe);
                Assert.IsTrue(s.speedParameterActive, golpe);
                Assert.AreEqual(ReglasAnimacion.RapidezGolpe, s.speedParameter, golpe);
            }
            Assert.AreEqual(1f, c.parameters.First(p => p.name == ReglasAnimacion.RapidezGolpe).defaultFloat);
        }
    }
}
