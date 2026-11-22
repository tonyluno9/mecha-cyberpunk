using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1645: ningún vehículo fuera del muro. Los puntos de aparición no son datos (son carros colocados en la escena), así que se leen de la escena guardada.</summary>
    public class VehiculosDentroDelMuroT1645Pruebas
    {
        private static readonly Rect Area = new Rect(-135f, -88f, 270f, 176f);

        [Test]
        public void Decidir_DentroNoSeToca()
        {
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Nada, VigilanteVehiculosMuro.Decidir(new Vector3(0f, 0f, 0f), Area, false, true));
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Nada, VigilanteVehiculosMuro.Decidir(new Vector3(138f, 0f, 0f), Area, false, false), "3 m pasado el muro: dando la vuelta");
        }

        [Test]
        public void Decidir_FueraApagaPropsYReubicaElResto()
        {
            Vector3 fuera = new Vector3(-20f, 0f, -127.8f);
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Apagar, VigilanteVehiculosMuro.Decidir(fuera, Area, false, true));
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Reubicar, VigilanteVehiculosMuro.Decidir(fuera, Area, false, false));
        }

        [Test]
        public void Decidir_CaidoYOcupado()
        {
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Reubicar, VigilanteVehiculosMuro.Decidir(new Vector3(0f, -6f, 0f), Area, false, false));
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Reubicar, VigilanteVehiculosMuro.Decidir(new Vector3(0f, -6f, 0f), null, false, false), "sin muro solo vigila la caída");
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Nada, VigilanteVehiculosMuro.Decidir(new Vector3(0f, -60f, -200f), Area, true, false), "el carro del jugador no se toca");
            Assert.AreEqual(VigilanteVehiculosMuro.Accion.Nada, VigilanteVehiculosMuro.Decidir(new Vector3(0f, 0f, -200f), null, false, true), "sin muro no hay afuera");
        }

        [Test]
        public void PuntoAdentro_QuedaDentroDelArea()
        {
            Vector3 p = VigilanteVehiculosMuro.PuntoAdentro(new Vector3(-20f, 0f, -127.8f), Area);
            Assert.IsTrue(Area.Contains(new Vector2(p.x, p.z)));
            Assert.AreEqual(-20f, p.x, 0.01f);
            Assert.AreEqual(Area.yMin + VigilanteVehiculosMuro.MargenReubicado, p.z, 0.01f);
        }

        [Test]
        public void Escena_NingunVehiculoActivoFueraDelMuro()
        {
            Scene escena = EditorSceneManager.OpenScene(VehiculosFueraDelMuroT1645.RutaEscena, OpenSceneMode.Single);
            var fuera = VehiculosFueraDelMuroT1645.Encontrar(escena);
            Assert.AreEqual(0, fuera.Count, "vehículos activos fuera del muro (corre VehiculosFueraDelMuroT1645.Aplicar): "
                + string.Join("; ", fuera.ConvertAll(g => g.name + " " + g.transform.position)));
        }
    }
}
