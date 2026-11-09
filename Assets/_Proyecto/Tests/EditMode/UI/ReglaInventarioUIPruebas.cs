using System.Collections.Generic;
using MechaCyberpunk.UI.Inventario;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-904: las reglas puras del inventario: filtrar por categoría, contar, moverse por la cuadrícula y desplazarla.</summary>
    public class ReglaInventarioUIPruebas
    {
        private static List<EntradaInventario> Muestra()
        {
            return new List<EntradaInventario>
            {
                new EntradaInventario("a1", "PISTOLA", CategoriaInventario.Armas, 1, 0, 1, "Cat_Pistola", string.Empty),
                new EntradaInventario("a2", "ESCOPETA", CategoriaInventario.Armas, 1, 0, 0, "Cat_Escopeta", string.Empty),
                new EntradaInventario("m1", "BALAS", CategoriaInventario.Municion, 12, 60, 0, "Cat_Municion", string.Empty),
                new EntradaInventario("c1", "KIT", CategoriaInventario.Consumibles, 2, 3, 0, "Cat_Consumibles", "Repara"),
            };
        }

        [Test]
        public void Filtrar_Todo_DevuelveTodasEnElMismoOrden()
        {
            List<EntradaInventario> r = ReglaInventarioUI.Filtrar(Muestra(), CategoriaInventario.Todo);
            CollectionAssert.AreEqual(new[] { "a1", "a2", "m1", "c1" }, r.ConvertAll(e => e.Id));
        }

        [Test]
        public void Filtrar_Armas_SoloDevuelveLasArmas()
        {
            List<EntradaInventario> r = ReglaInventarioUI.Filtrar(Muestra(), CategoriaInventario.Armas);
            CollectionAssert.AreEqual(new[] { "a1", "a2" }, r.ConvertAll(e => e.Id));
        }

        [Test]
        public void Filtrar_CategoriaSinNada_DevuelveListaVacia()
        {
            Assert.AreEqual(0, ReglaInventarioUI.Filtrar(Muestra(), CategoriaInventario.Implantes).Count);
        }

        [Test]
        public void Filtrar_ListaNula_DevuelveListaVacia()
        {
            Assert.AreEqual(0, ReglaInventarioUI.Filtrar(null, CategoriaInventario.Todo).Count);
        }

        [Test]
        public void Contar_PorCategoria_CuentaLasEntradas()
        {
            Assert.AreEqual(4, ReglaInventarioUI.Contar(Muestra(), CategoriaInventario.Todo));
            Assert.AreEqual(2, ReglaInventarioUI.Contar(Muestra(), CategoriaInventario.Armas));
            Assert.AreEqual(1, ReglaInventarioUI.Contar(Muestra(), CategoriaInventario.Municion));
            Assert.AreEqual(0, ReglaInventarioUI.Contar(Muestra(), CategoriaInventario.Otros));
        }

        [Test]
        public void Mover_DentroDeLaCuadricula_AvanzaPorColumnasYFilas()
        {
            Assert.AreEqual(1, ReglaInventarioUI.Mover(0, 1, 0, 10));
            Assert.AreEqual(4, ReglaInventarioUI.Mover(0, 0, 1, 10));
            Assert.AreEqual(5, ReglaInventarioUI.Mover(1, 0, 1, 10));
            Assert.AreEqual(0, ReglaInventarioUI.Mover(4, 0, -1, 10));
        }

        [Test]
        public void Mover_ContraLosBordes_SeQuedaDondeEstaba()
        {
            Assert.AreEqual(0, ReglaInventarioUI.Mover(0, -1, 0, 10), "izquierda del borde");
            Assert.AreEqual(3, ReglaInventarioUI.Mover(3, 1, 0, 10), "derecha del borde (4 columnas)");
            Assert.AreEqual(2, ReglaInventarioUI.Mover(2, 0, -1, 10), "arriba de la primera fila");
        }

        [Test]
        public void Mover_AUnaFilaCorta_CaeEnLaUltimaCasilla()
        {
            Assert.AreEqual(5, ReglaInventarioUI.Mover(3, 0, 1, 6), "bajar desde la columna 3 a una fila de 2 casillas");
            Assert.AreEqual(5, ReglaInventarioUI.Mover(5, 1, 0, 6), "un paso a la derecha fuera de la lista no mueve");
        }

        [Test]
        public void Mover_SinCasillas_Devuelve0()
        {
            Assert.AreEqual(0, ReglaInventarioUI.Mover(0, 1, 1, 0));
        }

        [Test]
        public void FilaInicial_ElegidaFueraDeVista_Desplaza()
        {
            Assert.AreEqual(0, ReglaInventarioUI.FilaInicial(3, 0), "dentro de la vista no mueve");
            Assert.AreEqual(1, ReglaInventarioUI.FilaInicial(16, 0), "la fila 4 (índice 16) pide empezar en la fila 1");
            Assert.AreEqual(0, ReglaInventarioUI.FilaInicial(2, 3), "subir regresa a la fila de la elegida");
        }

        [Test]
        public void Siguiente_DaLaVuelta()
        {
            Assert.AreEqual(CategoriaInventario.Armas, ReglaInventarioUI.Siguiente(CategoriaInventario.Todo, 1));
            Assert.AreEqual(CategoriaInventario.Otros, ReglaInventarioUI.Siguiente(CategoriaInventario.Todo, -1));
            Assert.AreEqual(CategoriaInventario.Todo, ReglaInventarioUI.Siguiente(CategoriaInventario.Otros, 1));
        }

        [Test]
        public void IconoDeCategoria_UsaLasTexturasDeResources()
        {
            Assert.AreEqual("Cat_Pistola", ReglaInventarioUI.IconoDeCategoria(CategoriaInventario.Armas));
            Assert.AreEqual("Cat_Municion", ReglaInventarioUI.IconoDeCategoria(CategoriaInventario.Municion));
            Assert.AreEqual("Cat_Consumibles", ReglaInventarioUI.IconoDeCategoria(CategoriaInventario.Consumibles));
            Assert.AreEqual("Cat_Implantes", ReglaInventarioUI.IconoDeCategoria(CategoriaInventario.Implantes));
            Assert.IsNull(ReglaInventarioUI.IconoDeCategoria(CategoriaInventario.Todo));
        }

        [Test]
        public void EntradaInventario_ConDato_GuardaLosParesEnOrden()
        {
            EntradaInventario e = new EntradaInventario("x", "X", CategoriaInventario.Otros, 1, 0, 0, null, null);
            e.ConDato("A", "1").ConDato("B", "2");
            Assert.AreEqual(2, e.Datos.Count);
            Assert.AreEqual("B", e.Datos[1].Key);
            Assert.AreEqual(string.Empty, e.Descripcion, "descripción nula queda vacía");
        }
    }
}
