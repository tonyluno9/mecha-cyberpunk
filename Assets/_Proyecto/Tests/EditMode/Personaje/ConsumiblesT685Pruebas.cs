using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-685: reglas puras de los consumibles y datos de las expendedoras.</summary>
    public class ConsumiblesT685Pruebas
    {
        [Test]
        public void CuracionGradual_EntregaElTotalEnElTiempoYNoMas()
        {
            var c = new CuracionGradual();
            c.Iniciar(70f, 4f);
            float dado = 0f;
            dado += c.Avanzar(1f);
            Assert.AreEqual(17.5f, dado, 0.001f, "Un cuarto del tiempo, un cuarto de la cura");
            for (int i = 0; i < 10; i++) dado += c.Avanzar(1f);
            Assert.AreEqual(70f, dado, 0.001f);
            Assert.IsFalse(c.Activa);
        }

        [Test]
        public void CuracionGradual_ConTotalCero_NoHaceNada()
        {
            var c = new CuracionGradual();
            c.Iniciar(0f, 4f);
            Assert.IsFalse(c.Activa);
            Assert.AreEqual(0f, c.Avanzar(1f), 0.0001f);
        }

        [Test]
        public void Seleccion_SiguienteDaLaVueltaYAcomodaLosIndices()
        {
            Assert.AreEqual(1, ReglaConsumibles.Siguiente(0, 3));
            Assert.AreEqual(0, ReglaConsumibles.Siguiente(2, 3));
            Assert.AreEqual(-1, ReglaConsumibles.Siguiente(0, 0));
            Assert.AreEqual(2, ReglaConsumibles.Acomodar(5, 3));
            Assert.AreEqual(-1, ReglaConsumibles.Acomodar(0, 0));
        }

        [Test]
        public void DatosConsumible_Crear_GuardaLosEfectos()
        {
            DatosConsumible d = DatosConsumible.Crear("x", "X", 3, 10f, 0f, 5f, 0f, 1f, 0f);
            Assert.AreEqual(TipoObjeto.Consumible, d.Tipo);
            Assert.AreEqual(3, d.Maximo);
            Assert.AreEqual(10f, d.Curacion);
            Assert.AreEqual(5f, d.SegundosSinDanio);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void Assets_CincoConsumiblesEnElCatalogoConOfertaYLugares()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>("Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset");
            string[] ids = { "bebida_energetica", "papitas", "venda", "cafe", "bateria_escudo" };
            foreach (string id in ids)
            {
                var c = catalogo.Buscar(id) as DatosConsumible;
                Assert.IsNotNull(c, id + " está en el catálogo como consumible");
                Assert.IsNotNull(c.Oferta, id + " tiene oferta");
                Assert.Greater(c.Oferta.Precio, 0, id + " cuesta");
            }
            var bebida = (DatosConsumible)catalogo.Buscar("bebida_energetica");
            Assert.AreEqual(5f, bebida.SegundosSinDanio, "La bebida da 5 s sin recibir daño");
            Assert.Greater(bebida.Curacion, 0f);
            var venda = (DatosConsumible)catalogo.Buscar("venda");
            var papitas = (DatosConsumible)catalogo.Buscar("papitas");
            Assert.Greater(venda.Curacion, papitas.Curacion, "La venda cura más que las papitas");
            Assert.Greater(venda.SegundosCuracion, 0f, "y tarda");

            var datos = AssetDatabase.LoadAssetAtPath<DatosExpendedoras>("Assets/_Proyecto/UI/Datos/Resources/DatosExpendedoras_Ciudad.asset");
            Assert.IsNotNull(datos);
            Assert.AreEqual(3, datos.Posiciones.Count, "Casa, tianguis y Nova");
            Assert.IsNotNull(datos.Modelo);
            Assert.AreEqual(5, datos.Tienda.Ofertas.Count);
        }
    }
}
