using MechaCyberpunk.IA.Adaptacion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-200: la memoria de los rivales se guarda y se carga sin perder nada, y un texto malo no cambia nada.</summary>
    public class MemoriaRivalesPruebas
    {
        private GameObject objeto;
        private MemoriaRivales memoria;

        [SetUp]
        public void SetUp()
        {
            objeto = new GameObject("MemoriaRivales");
            memoria = objeto.AddComponent<MemoriaRivales>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(objeto);
        }

        private static void Llenar(MemoriaRival m, int a, int b, int c, int peleas, int derrotas)
        {
            m.evidencias = new[] { a, b, c };
            m.peleas = peleas;
            m.derrotasSeguidas = derrotas;
        }

        [Test]
        public void Obtener_RivalNuevo_CreaUnaMemoriaVaciaYLaMismaSiSePideOtraVez()
        {
            MemoriaRival uno = memoria.Obtener("DatosRival_A");

            Assert.AreEqual("DatosRival_A", uno.idRival);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, uno.evidencias);
            Assert.AreSame(uno, memoria.Obtener("DatosRival_A"));
            Assert.AreEqual(1, memoria.Cantidad);
        }

        [Test]
        public void GuardarCargar_ConDosRivales_DejaTodoIgual()
        {
            Llenar(memoria.Obtener("DatosRival_A"), 3, 0, 1, 5, 0);
            Llenar(memoria.Obtener("DatosRival_B"), 0, 2, 0, 2, 1);
            string texto = memoria.Guardar();

            memoria.OlvidarTodo();
            memoria.Cargar(texto);

            Assert.AreEqual(2, memoria.Cantidad);
            MemoriaRival a = memoria.Obtener("DatosRival_A");
            CollectionAssert.AreEqual(new[] { 3, 0, 1 }, a.evidencias);
            Assert.AreEqual(5, a.peleas);
            Assert.AreEqual(0, a.derrotasSeguidas);
            MemoriaRival b = memoria.Obtener("DatosRival_B");
            CollectionAssert.AreEqual(new[] { 0, 2, 0 }, b.evidencias);
            Assert.AreEqual(2, b.peleas);
            Assert.AreEqual(1, b.derrotasSeguidas);
            Assert.AreEqual(texto, memoria.Guardar(), "Guardar de nuevo da el mismo texto");
        }

        [Test]
        public void Cargar_TextoInvalido_NoCambiaNadaNiLanzaYAvisa()
        {
            Llenar(memoria.Obtener("DatosRival_A"), 2, 0, 0, 3, 0);
            string antes = memoria.Guardar();
            int avisos = 0;
            memoria.Cambio += () => avisos++;

            foreach (string malo in new[] { "esto no es json", "[1,2", string.Empty, null })
            {
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no se entiende"));
                Assert.DoesNotThrow(() => memoria.Cargar(malo));
            }

            Assert.AreEqual(antes, memoria.Guardar());
            Assert.AreEqual(0, avisos, "Un texto que no se entiende no avisa Cambio");
        }

        [Test]
        public void Cargar_EvidenciasDeOtroLargo_LasAjustaATres()
        {
            memoria.Cargar("{\"memorias\":[{\"idRival\":\"X\",\"evidencias\":[4],\"peleas\":1,\"derrotasSeguidas\":0}]}");

            CollectionAssert.AreEqual(new[] { 4, 0, 0 }, memoria.Obtener("X").evidencias);
        }

        [Test]
        public void Olvidar_UnRival_BorraSoloEseYAvisa()
        {
            Llenar(memoria.Obtener("DatosRival_A"), 1, 0, 0, 1, 0);
            Llenar(memoria.Obtener("DatosRival_B"), 0, 1, 0, 1, 0);
            int avisos = 0;
            memoria.Cambio += () => avisos++;

            memoria.Olvidar("DatosRival_A");

            Assert.AreEqual(1, avisos);
            Assert.IsNull(memoria.Buscar("DatosRival_A"));
            Assert.IsNotNull(memoria.Buscar("DatosRival_B"));
            memoria.Olvidar("DatosRival_A");
            Assert.AreEqual(1, avisos, "Olvidar a quien no se recuerda no avisa");
        }

        [Test]
        public void OlvidarTodo_BorraTodoYAvisaUnaVez()
        {
            Llenar(memoria.Obtener("DatosRival_A"), 1, 0, 0, 1, 0);
            Llenar(memoria.Obtener("DatosRival_B"), 0, 1, 0, 1, 0);
            int avisos = 0;
            memoria.Cambio += () => avisos++;

            memoria.OlvidarTodo();

            Assert.AreEqual(0, memoria.Cantidad);
            Assert.AreEqual(1, avisos);
            memoria.OlvidarTodo();
            Assert.AreEqual(1, avisos, "Sin nada que olvidar no avisa");
        }

        [Test]
        public void Cargar_Valido_AvisaCambio()
        {
            Llenar(memoria.Obtener("DatosRival_A"), 1, 0, 0, 1, 0);
            string texto = memoria.Guardar();
            int avisos = 0;
            memoria.Cambio += () => avisos++;

            memoria.Cargar(texto);

            Assert.AreEqual(1, avisos);
        }
    }
}
