using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-78: la tabla paso a candados, el orden con el paso nuevo, el guardado por nombre y las reglas puras del inicio guiado.</summary>
    public class GestorCandadosT78Pruebas
    {
        [TearDown]
        public void Despues() => CandadosMundo.Liberar();

        [TestCase(PasoRecorrido.Recuerdo)]
        [TestCase(PasoRecorrido.Despierta)]
        [TestCase(PasoRecorrido.Molestan)]
        [TestCase(PasoRecorrido.Maletin)]
        [TestCase(PasoRecorrido.Ranura)]
        [TestCase(PasoRecorrido.Decision)]
        [TestCase(PasoRecorrido.PeleaSkulls)]
        public void EmpujonYCinematicas_TodoBloqueado(PasoRecorrido paso)
        {
            GestorCandados.Candados c = GestorCandados.CandadosPara(paso);
            Assert.IsTrue(c.Policia && c.Pandillas && c.Nova && c.RoboCarros && c.Tiendas, paso.ToString());
        }

        // T-83: desde que decide quedarse ya puede usar carros; policía, pandillas, NOVA y tiendas siguen bloqueadas.
        [TestCase(PasoRecorrido.Dinero)]
        [TestCase(PasoRecorrido.Suero)]
        [TestCase(PasoRecorrido.UsarProtesis)]
        [TestCase(PasoRecorrido.Armas)]
        [TestCase(PasoRecorrido.Chica)]
        public void CaminoAlDoc_CarrosLibresLoDemasBloqueado(PasoRecorrido paso)
        {
            GestorCandados.Candados c = GestorCandados.CandadosPara(paso);
            Assert.IsTrue(c.Policia && c.Pandillas && c.Nova && c.Tiendas, paso.ToString());
            Assert.IsFalse(c.RoboCarros, paso.ToString());
        }

        [TestCase(PasoRecorrido.GuerraSkulls)]
        [TestCase(PasoRecorrido.GuerraSnakes)]
        [TestCase(PasoRecorrido.Hacker)]
        [TestCase(PasoRecorrido.Robots)]
        [TestCase(PasoRecorrido.Hermano)]
        [TestCase(PasoRecorrido.JefeFinal)]
        [TestCase(PasoRecorrido.Puertas)]
        [TestCase(PasoRecorrido.Fin)]
        [TestCase(PasoRecorrido.Libre)]
        [TestCase(PasoRecorrido.Ninguno)]
        public void DesdeLaGuerraDeLosSkulls_MundoLibre(PasoRecorrido paso)
        {
            Assert.IsFalse(GestorCandados.CandadosPara(paso).Alguno, paso.ToString());
        }

        [Test]
        public void LaTiendaAbiertaEsLaDelPaso()
        {
            Assert.AreEqual(CandadosMundo.TiendaDoc, GestorCandados.CandadosPara(PasoRecorrido.Dinero).TiendaAbierta);
            Assert.AreEqual(CandadosMundo.TiendaDoc, GestorCandados.CandadosPara(PasoRecorrido.Suero).TiendaAbierta);
            Assert.AreEqual(CandadosMundo.TiendaTino, GestorCandados.CandadosPara(PasoRecorrido.Armas).TiendaAbierta);
            Assert.AreEqual(string.Empty, GestorCandados.CandadosPara(PasoRecorrido.UsarProtesis).TiendaAbierta);
        }

        [Test]
        public void Aplicar_PoneYQuitaLosCandadosDelMundo()
        {
            GestorCandados.Aplicar(PasoRecorrido.Molestan);
            Assert.IsTrue(CandadosMundo.Policia && CandadosMundo.RoboCarros && CandadosMundo.Pandillas && CandadosMundo.Nova);
            Assert.IsTrue(MisionProtegida.Activa);
            GestorCandados.Aplicar(PasoRecorrido.GuerraSkulls);
            Assert.IsFalse(CandadosMundo.Alguno);
            Assert.IsFalse(MisionProtegida.Activa);
        }

        [Test]
        public void Siguiente_MetaUsarProtesisEntreSueroYArmas()
        {
            Assert.AreEqual(PasoRecorrido.UsarProtesis, GestorCandados.Siguiente(PasoRecorrido.Suero));
            Assert.AreEqual(PasoRecorrido.Armas, GestorCandados.Siguiente(PasoRecorrido.UsarProtesis));
            Assert.AreEqual(PasoRecorrido.PeleaSkulls, GestorCandados.Siguiente(PasoRecorrido.Decision)); // VR24
            Assert.AreEqual(PasoRecorrido.Dinero, GestorCandados.Siguiente(PasoRecorrido.PeleaSkulls));
            Assert.AreEqual(PasoRecorrido.GuerraSnakes, GestorCandados.Siguiente(PasoRecorrido.GuerraSkulls));
        }

        [Test]
        public void PasoNuevo_NoMuevePasosViejosYSeGuardaPorNombre()
        {
            Assert.AreEqual(9, (int)PasoRecorrido.Armas, "los números de los pasos viejos no cambian");
            Assert.AreEqual(12, (int)PasoRecorrido.GuerraSkulls);
            Assert.IsTrue(Enum.TryParse("UsarProtesis", false, out PasoRecorrido leido) && leido == PasoRecorrido.UsarProtesis);
            Assert.IsFalse(Enum.TryParse("PasoQueNoExiste", false, out PasoRecorrido _), "una partida de otra versión no entra");
        }

        [Test]
        public void GuardarYCargar_ElPasoNuevoVuelve()
        {
            GameObject objeto = new GameObject("Director_T78");
            try
            {
                DirectorRecorrido director = objeto.AddComponent<DirectorRecorrido>();
                director.Cargar("{\"paso\":\"UsarProtesis\",\"hambre\":false,\"avisos\":\"protesis_paso_0\"}");
                Assert.AreEqual(PasoRecorrido.UsarProtesis, director.Paso);
                StringAssert.Contains("\"paso\":\"UsarProtesis\"", director.Guardar());
                director.Cargar("{\"paso\":\"Armas\",\"hambre\":false,\"avisos\":\"\"}");
                Assert.AreEqual(PasoRecorrido.Armas, director.Paso, "una partida vieja en Armas sigue cargando");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void AvisoLejosDelDoc_SoloLejosYAlejandose()
        {
            Assert.IsFalse(GestorCandados.DebeAvisarLejosDelDoc(30f, 20f), "cerca no avisa");
            Assert.IsFalse(GestorCandados.DebeAvisarLejosDelDoc(90f, 100f), "lejos pero volviendo no avisa");
            Assert.IsTrue(GestorCandados.DebeAvisarLejosDelDoc(90f, 80f), "lejos y alejándose avisa");
        }

        [Test]
        public void UsoDeLasPiernas_PideActivasYCorriendo()
        {
            Assert.IsFalse(GestorCandados.UsoPiernasCompleto(false, true, 5f));
            Assert.IsFalse(GestorCandados.UsoPiernasCompleto(true, false, 5f));
            Assert.IsFalse(GestorCandados.UsoPiernasCompleto(true, true, 0.1f));
            Assert.IsTrue(GestorCandados.UsoPiernasCompleto(true, true, 5f));
        }

        [Test]
        public void BancoDeTino_NoDejaSalirSinArmaYMejora()
        {
            Assert.AreEqual(GestorCandados.AvisoSinArma, GestorCandados.FaltaEnElBanco(false, false));
            Assert.AreEqual(GestorCandados.AvisoSinMejora, GestorCandados.FaltaEnElBanco(true, false));
            Assert.AreEqual(string.Empty, GestorCandados.FaltaEnElBanco(true, true));
        }

        [Test]
        public void ObjetivosNuevos_CabenEnElHud()
        {
            Assert.LessOrEqual(TablaRecorrido.ObjetivoTino.Length, 32);
            Assert.LessOrEqual(TablaRecorrido.ObjetivoUsarProtesis.Length, 32);
            Assert.AreEqual("VE CON TINO", TablaRecorrido.ObjetivoTino);
        }
    }
}
