using System;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-441: el tutorial ligero avisa cada acción una vez, en orden, se apaga y recuerda lo visto.</summary>
    public class ReglaTutorialPruebas
    {
        private const float Aviso = 5f;
        private const float Pausa = 1f;

        private static bool Todo(AccionTutorial accion) => true;

        [Test]
        public void Avanzar_SinNadaVisto_OfreceMoverseYLuegoSigueLaCadena()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, Pausa);
            Assert.IsTrue(regla.Avanzar(0.1f, Todo));
            Assert.IsTrue(regla.HayAviso);
            Assert.AreEqual(AccionTutorial.Moverse, regla.Aviso);

            regla.Hecha(AccionTutorial.Moverse);
            Assert.IsFalse(regla.HayAviso, "Hacer la acción quita el aviso");
            regla.Avanzar(0.5f, Todo);
            Assert.IsFalse(regla.HayAviso, "Hay una pausa entre avisos");
            regla.Avanzar(0.6f, Todo);
            regla.Avanzar(0.1f, Todo);
            Assert.AreEqual(AccionTutorial.Apuntar, regla.Aviso);
        }

        [Test]
        public void Avanzar_ElAvisoSinHacerLaAccion_SeVaAlPasarElTiempoYNoVuelve()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, 0f);
            regla.Avanzar(0.1f, Todo);
            Assert.AreEqual(AccionTutorial.Moverse, regla.Aviso);
            regla.Avanzar(Aviso + 0.1f, Todo);
            Assert.IsTrue(regla.Vista(AccionTutorial.Moverse), "Pasado el tiempo se da por visto");
            regla.Avanzar(0.1f, Todo);
            Assert.AreNotEqual(AccionTutorial.Moverse, regla.Aviso, "No repite el mismo aviso");
        }

        [Test]
        public void Avanzar_AccionNoDisponible_NoSale()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, 0f);
            regla.Hecha(AccionTutorial.Moverse);
            Func<AccionTutorial, bool> sinCarro = a => a != AccionTutorial.SubirAlCarro && a != AccionTutorial.Robar;
            for (int i = 0; i < 200; i++)
            {
                regla.Avanzar(Aviso + 1f, sinCarro);
            }
            Assert.IsFalse(regla.Vista(AccionTutorial.SubirAlCarro));
            Assert.IsFalse(regla.Vista(AccionTutorial.Robar), "Sin carro cerca no se ofrece ni se da por visto");
            Assert.IsTrue(regla.Vista(AccionTutorial.Misiones), "Las demás sí se recorrieron");
        }

        [Test]
        public void Avanzar_RobarNecesitaHaberSubidoAUnCarro()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, 0f);
            regla.Hecha(AccionTutorial.Moverse);
            Func<AccionTutorial, bool> soloRobar = a => a == AccionTutorial.Robar;
            regla.Avanzar(0.1f, soloRobar);
            Assert.IsFalse(regla.HayAviso, "Robar espera a SubirAlCarro");
            regla.Hecha(AccionTutorial.SubirAlCarro);
            regla.Avanzar(0.1f, soloRobar);
            Assert.IsTrue(regla.HayAviso);
            Assert.AreEqual(AccionTutorial.Robar, regla.Aviso);
        }

        [Test]
        public void Avanzar_SiDejaDeEstarDisponible_EscondeElAvisoSinDarloPorVisto()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, 0f);
            regla.Avanzar(0.1f, Todo);
            Assert.IsTrue(regla.HayAviso);
            Assert.IsTrue(regla.Avanzar(0.1f, a => false));
            Assert.IsFalse(regla.HayAviso);
            Assert.IsFalse(regla.Vista(AccionTutorial.Moverse));
        }

        [Test]
        public void Avanzar_Apagado_NoAvisaYQuitaElQueHabia()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, 0f);
            regla.Avanzar(0.1f, Todo);
            Assert.IsTrue(regla.HayAviso);
            regla.Activo = false;
            Assert.IsTrue(regla.Avanzar(0.1f, Todo), "Al apagarlo cambia el aviso");
            Assert.IsFalse(regla.HayAviso);
            regla.Avanzar(10f, Todo);
            Assert.IsFalse(regla.HayAviso);
            Assert.AreEqual(0, regla.CantidadVistas, "Apagado no da nada por visto");
        }

        [Test]
        public void SerializarYCargar_VaYVuelveYIgnoraNombresRotos()
        {
            ReglaTutorial regla = new ReglaTutorial(Aviso, 0f);
            regla.Hecha(AccionTutorial.Moverse);
            regla.Hecha(AccionTutorial.Mapa);
            string texto = regla.Serializar();
            Assert.AreEqual("Moverse,Mapa", texto);

            ReglaTutorial otra = new ReglaTutorial(Aviso, 0f);
            otra.Cargar(texto + ",Inventada, 99");
            Assert.IsTrue(otra.Vista(AccionTutorial.Moverse));
            Assert.IsTrue(otra.Vista(AccionTutorial.Mapa));
            Assert.AreEqual(2, otra.CantidadVistas);
            otra.Cargar(null);
            Assert.AreEqual(0, otra.CantidadVistas);
        }

        [Test]
        public void Texto_TodasLasAccionesTienenAviso()
        {
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                Assert.IsNotEmpty(ReglaTutorial.Texto(accion), accion.ToString());
            }
        }

        [Test]
        public void Opciones_TutorialPorDefectoEncendidoYSeGuarda()
        {
            ValoresOpciones defecto = ReglaOpciones.PorDefecto();
            Assert.IsTrue(defecto.tutorial);
            Assert.AreEqual(string.Empty, defecto.tutorialVisto);
            defecto.tutorial = false;
            defecto.tutorialVisto = "Moverse,Mapa";
            Assert.IsTrue(ReglaOpciones.DeJson(ReglaOpciones.AJson(defecto), out ValoresOpciones leidos));
            Assert.IsFalse(leidos.tutorial);
            Assert.AreEqual("Moverse,Mapa", leidos.tutorialVisto);
            Assert.IsTrue(ReglaOpciones.DeJson("{\"sensibilidad\":1.5}", out ValoresOpciones viejo));
            Assert.IsTrue(viejo.tutorial, "Un opciones.json viejo sin el campo enciende el tutorial");
        }
    }
}
