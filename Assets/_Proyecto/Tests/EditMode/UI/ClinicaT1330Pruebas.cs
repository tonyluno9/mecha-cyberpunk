using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1330: la clínica lleva su tienda de prótesis de ataque y el efecto de reacción tiene un nombre claro.</summary>
    public class ClinicaT1330Pruebas
    {
        [Test]
        public void MostradorClinica_LlevaSuPantallaTienda_ParaLaPestanaDeProtesis()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/UI/Prefabs/MostradorClinica.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponentInChildren<PantallaTienda>(true), "sin PantallaTienda la pestaña [TAB] no sale (Ciudad no trae otra)");
        }

        [Test]
        public void CatalogoDeProtesis_TieneLaserYMisilesConTienda()
        {
            MechaCyberpunk.Personaje.CatalogoProtesisAtaque catalogo = Resources.Load<MechaCyberpunk.Personaje.CatalogoProtesisAtaque>("CatalogoProtesisAtaque");
            Assert.IsNotNull(catalogo);
            Assert.IsNotNull(catalogo.Tienda);
            Assert.AreEqual(2, catalogo.Protesis.Count, "brazo láser y hombrera de misiles");
        }

        [Test]
        public void NombreDelEfectoDeReaccion_NoEsElViejoEsperaEnElMecha()
        {
            ConfigPantallaClinica config = AssetDatabase.LoadAssetAtPath<ConfigPantallaClinica>("Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset");
            Assert.IsNotNull(config);
            string nombre = config.NombreEfecto((int)EfectoImplante.ReaccionMecha);
            StringAssert.DoesNotContain("ESPERA", nombre);
            StringAssert.Contains("GOLPE", nombre);
            StringAssert.Contains("MECHA", nombre);
        }
    }
}
