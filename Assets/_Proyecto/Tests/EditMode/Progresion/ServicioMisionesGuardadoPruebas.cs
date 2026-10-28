using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>El guardado de ServicioMisiones (T-184): qué misiones se cumplieron, cuántas veces y el último día.</summary>
    public class ServicioMisionesGuardadoPruebas
    {
        private GameObject objeto;
        private ServicioMisiones servicio;

        [SetUp]
        public void SetUp()
        {
            objeto = new GameObject("ServicioMisionesDePrueba");
            servicio = objeto.AddComponent<ServicioMisiones>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(objeto);
        }

        private const string Dos = "{\"misiones\":[{\"id\":\"barrio_tiendita\",\"veces\":3,\"ultimoDia\":5},{\"id\":\"corp_entrega\",\"veces\":1,\"ultimoDia\":2}]}";

        [Test]
        public void Cargar_ConDatos_ConservaVecesYUltimoDia()
        {
            servicio.Cargar(Dos);

            Assert.AreEqual(3, servicio.VecesCumplida("barrio_tiendita"));
            Assert.AreEqual(5, servicio.UltimoDiaCumplida("barrio_tiendita"));
            Assert.AreEqual(1, servicio.VecesCumplida("corp_entrega"));
            Assert.AreEqual(2, servicio.UltimoDiaCumplida("corp_entrega"));
            Assert.AreEqual(0, servicio.VecesCumplida("robo_soplo"));
            Assert.AreEqual(ContextoMision.Nunca, servicio.UltimoDiaCumplida("robo_soplo"));
        }

        [Test]
        public void GuardarYCargar_EnOtroServicio_DaLoMismo()
        {
            servicio.Cargar(Dos);
            string texto = servicio.Guardar();
            var otroObjeto = new GameObject("Otro");
            try
            {
                ServicioMisiones otro = otroObjeto.AddComponent<ServicioMisiones>();
                otro.Cargar(texto);

                Assert.AreEqual(3, otro.VecesCumplida("barrio_tiendita"));
                Assert.AreEqual(5, otro.UltimoDiaCumplida("barrio_tiendita"));
                Assert.AreEqual(1, otro.VecesCumplida("corp_entrega"));
                Assert.AreEqual(2, otro.UltimoDiaCumplida("corp_entrega"));
            }
            finally
            {
                Object.DestroyImmediate(otroObjeto);
            }
        }

        [Test]
        public void Cargar_ReemplazaLoQueHabia()
        {
            servicio.Cargar(Dos);
            servicio.Cargar("{\"misiones\":[{\"id\":\"robo_soplo\",\"veces\":1,\"ultimoDia\":9}]}");

            Assert.AreEqual(0, servicio.VecesCumplida("barrio_tiendita"), "lo anterior se olvida");
            Assert.AreEqual(1, servicio.VecesCumplida("robo_soplo"));
            Assert.AreEqual(9, servicio.UltimoDiaCumplida("robo_soplo"));
        }

        [TestCase("esto no es json")]
        [TestCase("")]
        public void Cargar_ConTextoRoto_NoCambiaNadaYAvisa(string texto)
        {
            servicio.Cargar(Dos);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("misiones no entendieron"));

            servicio.Cargar(texto);

            Assert.AreEqual(3, servicio.VecesCumplida("barrio_tiendita"), "queda lo que había");
            Assert.AreEqual(5, servicio.UltimoDiaCumplida("barrio_tiendita"));
        }

        [Test]
        public void Cargar_ConEntradaSinId_LaIgnoraYAvisa()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("sin id"));

            servicio.Cargar("{\"misiones\":[{\"id\":\"\",\"veces\":2,\"ultimoDia\":1},{\"id\":\"corp_cobro\",\"veces\":1,\"ultimoDia\":4}]}");

            Assert.AreEqual(1, servicio.VecesCumplida("corp_cobro"));
            Assert.AreEqual(4, servicio.UltimoDiaCumplida("corp_cobro"));
        }

        [Test]
        public void Guardar_SinMisionesCumplidas_NoTraeEntradas_NiLaMisionEnCurso()
        {
            string texto = servicio.Guardar();

            StringAssert.Contains("misiones", texto);
            Assert.IsFalse(texto.Contains("\"id\""), "sin misiones cumplidas no hay entradas: " + texto);
            Assert.IsFalse(servicio.HayMision, "una misión en curso nunca va en el guardado");
            Assert.AreEqual("misiones", servicio.ClaveGuardado);
        }

        [Test]
        public void ModeloHistorial_AnotarSumaVecesYGuardaElDia()
        {
            var modelo = new ModeloHistorialMisiones();
            modelo.Anotar("a", 2);
            modelo.Anotar("a", 4);
            modelo.Anotar("", 7);

            Assert.AreEqual(2, modelo.Veces("a"));
            Assert.AreEqual(4, modelo.UltimoDia("a"));
            Assert.AreEqual(1, modelo.Ids.Count(), "el id vacío no se anota");
        }
    }
}
