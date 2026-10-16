using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    public class CalculoRelojPruebas
    {
        private const float Tolerancia = 0.01f;
        private const float SegundosPorHoraNoche = 60f;
        private const float SegundosPorHoraDia = 40f;

        private List<EventoReloj> eventos;

        [SetUp]
        public void Preparar()
        {
            eventos = new List<EventoReloj>();
        }

        private static CalculoReloj Crear(float horaInicial = 21f, int diaInicial = 1)
        {
            return new CalculoReloj(7f, 19f, 8f, 12f, horaInicial, diaInicial);
        }

        private static void AvanzarEnPasos(CalculoReloj reloj, float segundos, float paso, List<EventoReloj> eventos)
        {
            // La cuenta del tiempo restante va en double para que los pasos no acumulen error de float.
            double restante = segundos;
            while (restante > 1e-6)
            {
                float dt = (float)System.Math.Min(paso, restante);
                reloj.Avanzar(dt, eventos);
                restante -= dt;
            }
        }

        private IEnumerable<int> Horas() => eventos.Where(e => e.Tipo == TipoEventoReloj.HoraEntera).Select(e => e.Valor);
        private int Contar(TipoEventoReloj tipo) => eventos.Count(e => e.Tipo == tipo);

        [Test]
        public void Avanzar_UnDiaCompleto_Dura1200SegundosReales()
        {
            CalculoReloj reloj = Crear(0f);
            AvanzarEnPasos(reloj, 1199.99f, 0.05f, eventos);
            Assert.AreEqual(1, reloj.Dia, "Falta un instante para dar la vuelta");
            Assert.Greater(reloj.Hora, 23.99f);
            reloj.Avanzar(0.02f, eventos);
            Assert.AreEqual(2, reloj.Dia);
            Assert.AreEqual(0f, reloj.Hora, 0.01f);
        }

        [Test]
        public void Avanzar_De7a19_Tarda480Segundos()
        {
            CalculoReloj reloj = Crear(7f);
            AvanzarEnPasos(reloj, 480f - Tolerancia, 0.1f, eventos);
            Assert.IsFalse(reloj.EsDeNoche, "A los 479.99 s todavía es de día");
            reloj.Avanzar(2f * Tolerancia, eventos);
            Assert.IsTrue(reloj.EsDeNoche, "A los 480.01 s ya es de noche");
            Assert.AreEqual(19f, reloj.Hora, 0.001f);
        }

        [Test]
        public void Avanzar_De19a7_Tarda720Segundos()
        {
            CalculoReloj reloj = Crear(19f);
            AvanzarEnPasos(reloj, 720f - Tolerancia, 0.1f, eventos);
            Assert.IsTrue(reloj.EsDeNoche, "A los 719.99 s todavía es de noche");
            reloj.Avanzar(2f * Tolerancia, eventos);
            Assert.IsFalse(reloj.EsDeNoche, "A los 720.01 s ya es de día");
            Assert.AreEqual(2, reloj.Dia);
        }

        [Test]
        public void Avanzar_60SegundosDesdeLas21_LlegaALas22()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.Avanzar(60f, eventos);
            Assert.AreEqual(22f, reloj.Hora, 0.001f);
            CollectionAssert.AreEqual(new[] { 22 }, Horas());
        }

        [Test]
        public void Avanzar_DtQueCruzaElAnochecer_CadaParteAvanzaASuVelocidad()
        {
            CalculoReloj reloj = Crear(18f);
            // 18:00 a 19:00 son 40 s de día; los 30 s restantes son media hora de noche (60 s por hora).
            reloj.Avanzar(70f, eventos);
            Assert.AreEqual(19.5f, reloj.Hora, 0.001f);
            Assert.IsTrue(reloj.EsDeNoche);
            Assert.AreEqual(1, Contar(TipoEventoReloj.Franja));
            Assert.AreEqual(1, eventos.First(e => e.Tipo == TipoEventoReloj.Franja).Valor, "La franja nueva es la noche");
            CollectionAssert.AreEqual(new[] { 19 }, Horas());
        }

        [Test]
        public void Avanzar_DtQueCruzaElAmanecer_CadaParteAvanzaASuVelocidad()
        {
            CalculoReloj reloj = Crear(6f);
            // 6:00 a 7:00 son 60 s de noche; los 20 s restantes son media hora de día (40 s por hora).
            reloj.Avanzar(80f, eventos);
            Assert.AreEqual(7.5f, reloj.Hora, 0.001f);
            Assert.IsFalse(reloj.EsDeNoche);
            Assert.AreEqual(0, eventos.First(e => e.Tipo == TipoEventoReloj.Franja).Valor, "La franja nueva es el día");
        }

        [Test]
        public void Avanzar_CruzaLas00_SumaUnDiaUnaSolaVez()
        {
            CalculoReloj reloj = Crear(23.5f);
            reloj.Avanzar(60f, eventos);
            Assert.AreEqual(2, reloj.Dia);
            Assert.AreEqual(0.5f, reloj.Hora, 0.001f);
            Assert.AreEqual(1, Contar(TipoEventoReloj.Dia));
            Assert.AreEqual(2, eventos.First(e => e.Tipo == TipoEventoReloj.Dia).Valor);
            reloj.Avanzar(10f, eventos);
            Assert.AreEqual(1, Contar(TipoEventoReloj.Dia), "No vuelve a avisar el día");
        }

        [Test]
        public void Avanzar_DtDeTresHoras_ReportaLasTresHorasEnOrden()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.Avanzar(3f * SegundosPorHoraNoche, eventos);
            CollectionAssert.AreEqual(new[] { 22, 23, 0 }, Horas());
            Assert.AreEqual(2, reloj.Dia);
        }

        [Test]
        public void Avanzar_DtDeTresHorasDeDia_ReportaLasTresHorasEnOrden()
        {
            CalculoReloj reloj = Crear(10f);
            reloj.Avanzar(3f * SegundosPorHoraDia, eventos);
            CollectionAssert.AreEqual(new[] { 11, 12, 13 }, Horas());
        }

        [Test]
        public void Avanzar_DtGigante_NoSeSaltaHorasNiDias()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.Avanzar(3600f, eventos);
            Assert.AreEqual(4, reloj.Dia, "Tres días enteros más lo que falta para las 00:00");
            Assert.AreEqual(3, Contar(TipoEventoReloj.Dia));
            Assert.AreEqual(72, Horas().Count());
            Assert.AreEqual(21f, reloj.Hora, 0.001f);
        }

        [Test]
        public void Hora_NuncaVale24()
        {
            CalculoReloj reloj = Crear(23.99999f);
            Assert.Less(reloj.Hora, 24f);
            reloj.Avanzar(1f, eventos);
            Assert.GreaterOrEqual(reloj.Hora, 0f);
            Assert.Less(reloj.Hora, 24f);
        }

        [Test]
        public void AvanzarHasta_8Desde21_DejaElDiaSiguienteALas8ConTresAvisos()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.AvanzarHasta(8f, eventos);
            Assert.AreEqual(8f, reloj.Hora, 0.001f);
            Assert.AreEqual(2, reloj.Dia);
            Assert.AreEqual(1, Contar(TipoEventoReloj.Dia));
            Assert.AreEqual(1, Contar(TipoEventoReloj.Franja));
            Assert.AreEqual(0, eventos.First(e => e.Tipo == TipoEventoReloj.Franja).Valor);
            CollectionAssert.AreEqual(new[] { 8 }, Horas());
        }

        [Test]
        public void AvanzarHasta_HoraDeHoyMasTarde_NoCambiaElDia()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.AvanzarHasta(23.5f, eventos);
            Assert.AreEqual(1, reloj.Dia);
            Assert.AreEqual(23.5f, reloj.Hora, 0.001f);
            Assert.AreEqual(0, Contar(TipoEventoReloj.Dia));
            Assert.AreEqual(0, Contar(TipoEventoReloj.Franja), "La franja no cambió");
            CollectionAssert.AreEqual(new[] { 23 }, Horas());
        }

        [Test]
        public void AvanzarHasta_LaHoraActual_NoCambiaNada()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.AvanzarHasta(21f, eventos);
            Assert.AreEqual(21f, reloj.Hora, 0.0001f);
            Assert.AreEqual(1, reloj.Dia);
            Assert.IsEmpty(eventos);
        }

        [Test]
        public void AvanzarHasta_24_EsLaMedianocheDelDiaSiguiente()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.AvanzarHasta(24f, eventos);
            Assert.AreEqual(0f, reloj.Hora, 0.001f);
            Assert.AreEqual(2, reloj.Dia);
        }

        [Test]
        public void AvanzarHasta_EnPausa_SigueFuncionando()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.Pausar(true);
            reloj.AvanzarHasta(12f, eventos);
            Assert.AreEqual(12f, reloj.Hora, 0.001f);
            Assert.AreEqual(2, reloj.Dia);
        }

        [Test]
        public void Avanzar_EnPausa_NoAvanza()
        {
            CalculoReloj reloj = Crear(21f);
            reloj.Pausar(true);
            reloj.Avanzar(600f, eventos);
            Assert.AreEqual(21f, reloj.Hora, 0.0001f);
            Assert.IsEmpty(eventos);
            reloj.Pausar(true);
            reloj.Pausar(false);
            reloj.Avanzar(60f, eventos);
            Assert.AreEqual(22f, reloj.Hora, 0.001f);
        }

        [Test]
        public void Constructor_HorasInvalidas_Falla()
        {
            Assert.Throws<System.ArgumentException>(() => new CalculoReloj(19f, 7f, 8f, 12f, 21f, 1));
            Assert.Throws<System.ArgumentException>(() => new CalculoReloj(7f, 19f, 0f, 12f, 21f, 1));
        }
    }
}
