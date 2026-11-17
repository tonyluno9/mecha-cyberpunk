using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Inventario;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1315: qué va en la rueda (equipar y quitar, tope, lista de la rueda), la entrada del inventario y el texto del tutorial con la tecla de consumir.</summary>
    public class EquipoRuedaT1315Pruebas
    {
        [SetUp]
        public void Preparar() => EquipoRueda.Reiniciar();

        [TearDown]
        public void Limpiar() => EquipoRueda.Reiniciar();

        [Test]
        public void PorOmision_TodoVaEquipado()
        {
            Assert.IsTrue(EquipoRueda.Equipado(EquipoRueda.IdArma(ArmaAPie.Fusil)));
            Assert.IsTrue(EquipoRueda.Equipado("bebida"));
        }

        [Test]
        public void Quitar_YVolverAEquipar_CambiaElEstado_YAvisa()
        {
            int avisos = 0;
            void Contar() => avisos++;
            EquipoRueda.Cambio += Contar;
            string fusil = EquipoRueda.IdArma(ArmaAPie.Fusil);
            EquipoRueda.Poner(fusil, false);
            Assert.IsFalse(EquipoRueda.Equipado(fusil));
            EquipoRueda.Poner(fusil, false);
            Assert.AreEqual(1, avisos, "Repetir lo mismo no avisa otra vez");
            EquipoRueda.Poner(fusil, true);
            Assert.IsTrue(EquipoRueda.Equipado(fusil));
            Assert.AreEqual(2, avisos);
            EquipoRueda.Cambio -= Contar;
        }

        [Test]
        public void HayLugar_ElTopeSonLasCasillasDeLaRueda()
        {
            Assert.AreEqual(ReglaRuedaArmas.Casillas, EquipoRueda.Maximo);
            Assert.IsTrue(EquipoRueda.HayLugar(EquipoRueda.Maximo - 1));
            Assert.IsFalse(EquipoRueda.HayLugar(EquipoRueda.Maximo));
        }

        [Test]
        public void Disposicion_NoMuestraLoQueSeQuito_PeroLosPunosSiempre()
        {
            string escopeta = EquipoRueda.IdArma(ArmaAPie.Escopeta);
            EquipoRueda.Poner(escopeta, false);
            EquipoRueda.Poner(EquipoRueda.IdArma(ArmaAPie.Punos), false);
            List<int> sin = ReglaRuedaArmas.Disposicion(c => !ReglaRuedaArmas.EsArma(c) || c == (int)ArmaAPie.Punos
                || EquipoRueda.Equipado(EquipoRueda.IdArma((ArmaAPie)c)));
            CollectionAssert.DoesNotContain(sin, (int)ArmaAPie.Escopeta, "La escopeta quitada no sale en la rueda");
            CollectionAssert.Contains(sin, (int)ArmaAPie.Pistola, "Lo demás sigue");
        }

        [Test]
        public void ElegirConsumible_TomaElPrimeroEquipadoQueSirve_YSinNingunoQueSirveElPrimeroEquipado()
        {
            List<string> ids = new List<string> { "bebida", "papitas", "venda" };
            Assert.AreEqual(0, EquipoRueda.ElegirConsumible(ids, i => true));
            EquipoRueda.Poner("bebida", false);
            Assert.AreEqual(1, EquipoRueda.ElegirConsumible(ids, i => true), "La bebida quitada no se consume");
            Assert.AreEqual(2, EquipoRueda.ElegirConsumible(ids, i => i == 2), "Salta a la que sirve");
            Assert.AreEqual(1, EquipoRueda.ElegirConsumible(ids, i => false), "Si ninguna sirve, el primero equipado");
            EquipoRueda.Poner("papitas", false);
            EquipoRueda.Poner("venda", false);
            Assert.AreEqual(-1, EquipoRueda.ElegirConsumible(ids, i => true), "Todo quitado: nada que consumir");
            Assert.AreEqual(-1, EquipoRueda.ElegirConsumible(null, null));
        }

        [Test]
        public void EntradaInventario_ConEquipo_SoloEquipadaSiEsEquipable()
        {
            EntradaInventario fusil = new EntradaInventario("arma_Fusil", "FUSIL", CategoriaInventario.Armas, 1, 0, 0, null, string.Empty).ConEquipo(true, true);
            Assert.IsTrue(fusil.Equipable);
            Assert.IsTrue(fusil.Equipada);
            EntradaInventario implante = new EntradaInventario("implante_Ojos", "OJOS", CategoriaInventario.Implantes, 1, 0, 1, null, string.Empty).ConEquipo(false, true);
            Assert.IsFalse(implante.Equipable);
            Assert.IsFalse(implante.Equipada, "Los implantes no van en la rueda");
        }

        [Test]
        public void TutorialConsumibles_DiceLaTeclaDeConsumir_QueSaleDeLaFuncionDeTeclas()
        {
            string normal = ReglaTutorial.Texto(AccionTutorial.Consumibles);
            StringAssert.Contains("B USA EL OBJETO EQUIPADO", normal);
            string otra = ReglaTutorial.Texto(AccionTutorial.Consumibles, n => n == ReglaTutorial.TeclaConsumir ? "Z" : "Tab");
            StringAssert.Contains("Z USA EL OBJETO EQUIPADO", otra, "La tecla no está escrita en el aviso: viene de quien contesta");
        }
    }
}
