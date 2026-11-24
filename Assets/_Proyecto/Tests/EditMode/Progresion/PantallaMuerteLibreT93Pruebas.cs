using System.Collections.Generic;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T93: las filas de estadísticas de HAS MUERTO (modo libre), que la pantalla nueva dibuja como etiqueta y valor.</summary>
    public class PantallaMuerteLibreT93Pruebas
    {
        [Test]
        public void FilasEstadisticas_SinRecordNuevo_SieteFilasConElMejorTiempo()
        {
            KeyValuePair<string, string>[] filas = PantallaMuerteLibre.FilasEstadisticas(4, 2, 1, 125f, 3, 1500, false, 300f);
            Assert.AreEqual(7, filas.Length);
            Assert.AreEqual("SKULLS ELIMINADOS", filas[0].Key);
            Assert.AreEqual("4", filas[0].Value);
            Assert.AreEqual("2", filas[1].Value);
            Assert.AreEqual("1", filas[2].Value);
            Assert.AreEqual("02:05", filas[3].Value);
            Assert.AreEqual("3", filas[4].Value);
            Assert.AreEqual(FormatoDinero.Texto(1500), filas[5].Value);
            Assert.AreEqual(PantallaMuerteLibre.EtiquetaRecord, filas[6].Key);
            Assert.AreEqual("05:00", filas[6].Value);
        }

        [Test]
        public void FilasEstadisticas_RecordNuevo_DiceNuevoRecord()
        {
            KeyValuePair<string, string>[] filas = PantallaMuerteLibre.FilasEstadisticas(0, 0, 0, 10f, 0, 0, true, 10f);
            Assert.AreEqual(PantallaMuerteLibre.TextoNuevoRecord, filas[6].Value);
        }

        [Test]
        public void TextoEstadisticas_SinDirector_Vacio()
        {
            Assert.AreEqual(string.Empty, PantallaMuerteLibre.TextoEstadisticas(null));
        }
    }
}
