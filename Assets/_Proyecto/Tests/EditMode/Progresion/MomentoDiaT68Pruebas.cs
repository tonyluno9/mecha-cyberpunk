using MechaCyberpunk.Progresion.Opciones;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T68: siempre de noche; el día solo se elige en OPCIONES. Reglas puras y valor guardado.</summary>
    public class MomentoDiaT68Pruebas
    {
        private const float Amanecer = 7f;
        private const float Anochecer = 19f;

        [Test]
        public void PorDefecto_EsNoche()
        {
            Assert.AreEqual(MomentoDelDia.Noche, ReglaMomentoDia.PorDefecto);
            Assert.AreEqual(MomentoDelDia.Noche, ReglaOpciones.PorDefecto().momentoDia);
        }

        [TestCase(23f)]
        [TestCase(0.5f)]
        [TestCase(3.9f)]
        [TestCase(20.5f)]
        public void Noche_HorasDeNoche_NoVuelve(float hora)
        {
            Assert.IsFalse(ReglaMomentoDia.HayQueVolver(MomentoDelDia.Noche, hora, Amanecer, Anochecer), hora.ToString());
        }

        [TestCase(4.5f)]
        [TestCase(7f)]
        [TestCase(12f)]
        [TestCase(19.5f)]
        [TestCase(float.NaN)]
        public void Noche_HorasDeAmanecerODia_Vuelve(float hora)
        {
            Assert.IsTrue(ReglaMomentoDia.HayQueVolver(MomentoDelDia.Noche, hora, Amanecer, Anochecer), hora.ToString());
        }

        [TestCase(9f)]
        [TestCase(12f)]
        [TestCase(16.9f)]
        public void Dia_HorasDeDia_NoVuelve(float hora)
        {
            Assert.IsFalse(ReglaMomentoDia.HayQueVolver(MomentoDelDia.Dia, hora, Amanecer, Anochecer), hora.ToString());
        }

        [TestCase(23f)]
        [TestCase(2f)]
        [TestCase(7.5f)]
        [TestCase(18f)]
        public void Dia_HorasDeNocheOAtardecer_Vuelve(float hora)
        {
            Assert.IsTrue(ReglaMomentoDia.HayQueVolver(MomentoDelDia.Dia, hora, Amanecer, Anochecer), hora.ToString());
        }

        [Test]
        public void HoraDe_CadaMomentoCaeDentroDeSuPropioRango()
        {
            Assert.IsFalse(ReglaMomentoDia.HayQueVolver(MomentoDelDia.Noche, ReglaMomentoDia.HoraDe(MomentoDelDia.Noche), Amanecer, Anochecer));
            Assert.IsFalse(ReglaMomentoDia.HayQueVolver(MomentoDelDia.Dia, ReglaMomentoDia.HoraDe(MomentoDelDia.Dia), Amanecer, Anochecer));
        }

        [Test]
        public void Fijar_AvisaSoloAlCambiarYDiceQuienLoCambio()
        {
            MomentoDelDia antes = ReglaMomentoDia.Actual;
            int avisos = 0;
            bool ultimoPorJugador = false;
            System.Action<MomentoDelDia, bool> oyente = (m, j) => { avisos++; ultimoPorJugador = j; };
            ReglaMomentoDia.Cambio += oyente;
            try
            {
                ReglaMomentoDia.Fijar(MomentoDelDia.Noche, false);
                ReglaMomentoDia.Fijar(MomentoDelDia.Dia, true);
                Assert.AreEqual(1, avisos);
                Assert.IsTrue(ultimoPorJugador);
                ReglaMomentoDia.Fijar(MomentoDelDia.Dia, true);
                Assert.AreEqual(1, avisos, "mismo valor no avisa");
            }
            finally
            {
                ReglaMomentoDia.Cambio -= oyente;
                ReglaMomentoDia.Fijar(antes, false);
            }
        }

        [Test]
        public void Json_GuardaYLeeElMomentoYUnArchivoViejoEsNoche()
        {
            ValoresOpciones v = ReglaOpciones.PorDefecto();
            v.momentoDia = MomentoDelDia.Dia;
            Assert.IsTrue(ReglaOpciones.DeJson(ReglaOpciones.AJson(v), out ValoresOpciones leidos));
            Assert.AreEqual(MomentoDelDia.Dia, leidos.momentoDia);
            Assert.IsTrue(ReglaOpciones.DeJson("{\"sensibilidad\":1.5}", out ValoresOpciones viejo));
            Assert.AreEqual(MomentoDelDia.Noche, viejo.momentoDia);
            Assert.IsTrue(ReglaOpciones.DeJson("{\"momentoDia\":9}", out ValoresOpciones raro));
            Assert.AreEqual(MomentoDelDia.Noche, raro.momentoDia);
        }
    }
}
