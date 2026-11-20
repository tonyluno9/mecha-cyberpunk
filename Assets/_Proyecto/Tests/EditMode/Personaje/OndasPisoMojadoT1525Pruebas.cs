using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1525: ondas de pisada en todo piso mojado (decals planos y asfalto), sin salpicadura de gotas.</summary>
    public class OndasPisoMojadoT1525Pruebas
    {
        [Test]
        public void PisaAreaPlana_CentroSi_FueraNo_AltoNo()
        {
            Assert.IsTrue(ReglaEfectosPie.PisaAreaPlana(0f, 0f, 2f, 1f, 0f), "en el centro");
            Assert.IsTrue(ReglaEfectosPie.PisaAreaPlana(1.5f, 0f, 2f, 1f, 0.1f), "dentro de la elipse");
            Assert.IsFalse(ReglaEfectosPie.PisaAreaPlana(2f, 0f, 2f, 1f, 0f), "en el borde del eje largo ya no");
            Assert.IsFalse(ReglaEfectosPie.PisaAreaPlana(0f, 0.9f, 2f, 1f, 0f), "en el borde del eje corto ya no");
            Assert.IsFalse(ReglaEfectosPie.PisaAreaPlana(0f, 0f, 2f, 1f, 2f), "muy arriba del decal no cuenta");
            Assert.IsFalse(ReglaEfectosPie.PisaAreaPlana(0f, 0f, 0f, 1f, 0f), "un decal sin ancho no se pisa");
        }

        [Test]
        public void Anillos_Agregar_Activa_YVenceConLaVidaDeLaOnda()
        {
            AnillosSuelo anillos = new AnillosSuelo();
            try
            {
                anillos.Agregar(new Vector3(1f, 0f, 2f), 10f);
                Assert.AreEqual(1, anillos.Activos, "una pisada, una onda");
                Assert.AreEqual(new Vector3(1f, 0f, 2f), anillos.UltimaPosicion);
                for (int i = 0; i < AnillosSuelo.Cantidad + 2; i++)
                {
                    anillos.Agregar(new Vector3(i, 0f, 0f), 10f);
                }
                Assert.AreEqual(AnillosSuelo.Cantidad, anillos.Activos, "se reutilizan los cuadros: nunca pasan del maximo");
                Assert.AreEqual(AnillosSuelo.Cantidad, anillos.Actualizar(10f + ReglaOndasCharco.Vida - 0.1f), "aun vivas");
                Assert.AreEqual(0, anillos.Actualizar(10f + ReglaOndasCharco.Vida + 0.1f), "vencidas");
            }
            finally
            {
                anillos.Destruir();
            }
        }
    }
}
