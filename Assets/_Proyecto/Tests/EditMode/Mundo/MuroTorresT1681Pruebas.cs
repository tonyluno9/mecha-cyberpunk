using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T35 (T-1681): cada torre del muro unida a sus tramos, sin rendijas pasables ni visibles. Pide haber corrido MuroTorresT1681.Aplicar.</summary>
    public class MuroTorresT1681Pruebas
    {
        private static MuroTorresT1681.Medida medida;

        private static MuroTorresT1681.Medida Medida()
        {
            if (medida == null)
            {
                EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
                medida = MuroTorresT1681.Medir();
            }
            return medida;
        }

        [Test]
        public void Escena_TorresDelMuro_SonLas16()
        {
            Assert.AreEqual(16, Medida().Torres, "torres MU02 en el muro (4 esquinas y 2 por cada una de las 6 puertas)");
        }

        [Test]
        public void Escena_JuntoALasTorres_NingunaRendijaPasable()
        {
            MuroTorresT1681.Medida m = Medida();
            Assert.Greater(m.Capsulas, 1000, "no se revisaron las capsulas");
            Assert.Greater(m.Rayos, 1000, "no se lanzaron los rayos");
            Assert.AreEqual(0, m.Pasables.Count, "capsulas del jugador libres en la linea del muro: " + string.Join("; ", m.Pasables.Take(10)));
            Assert.AreEqual(0, m.RayosLibres.Count, "rayos horizontales que salen: " + string.Join("; ", m.RayosLibres.Take(10)));
        }

        [Test]
        public void Escena_JuntoALasTorres_NingunHuecoVisibleDeMasDe5cm()
        {
            MuroTorresT1681.Medida m = Medida();
            Assert.AreEqual(0, m.Visibles.Count, "rendijas visibles: " + string.Join("; ", m.Visibles.Take(10).Select(v => v.Torre + " t " + v.TIni.ToString("F2") + ".." + v.TFin.ToString("F2")
                + " ang " + v.Angulo + " h " + v.Altura)));
        }
    }
}
