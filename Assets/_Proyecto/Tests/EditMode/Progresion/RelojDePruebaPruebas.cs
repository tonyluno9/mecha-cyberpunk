using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class RelojDePruebaPruebas
    {
        private ModeloRelojDePrueba reloj;
        private List<int> horas;
        private List<int> dias;
        private List<FranjaDia> franjas;

        private void Preparar(float hora)
        {
            reloj = new ModeloRelojDePrueba(hora);
            horas = new List<int>();
            dias = new List<int>();
            franjas = new List<FranjaDia>();
            reloj.HoraEnteraCambio += h => horas.Add(h);
            reloj.DiaCambio += d => dias.Add(d);
            reloj.FranjaCambio += f => franjas.Add(f);
        }

        [Test]
        public void Constructor_EmpiezaElDia1ALas21EnNoche()
        {
            Preparar(21f);
            Assert.AreEqual(1, reloj.Dia);
            Assert.AreEqual(21f, reloj.Hora, 0.0001f);
            Assert.AreEqual(FranjaDia.Noche, reloj.Franja);
            Assert.IsTrue(reloj.EsDeNoche);
        }

        [Test]
        public void Avanzar_VariasHoras_AvisaCadaHoraEnteraEnOrden()
        {
            Preparar(21.5f);
            reloj.Avanzar(3f);

            CollectionAssert.AreEqual(new[] { 22, 23, 0 }, horas);
            Assert.AreEqual(0.5f, reloj.Hora, 0.001f);
        }

        [Test]
        public void Avanzar_AlPasarLas00_AvisaDiaCambioConElDiaNuevo()
        {
            Preparar(23.5f);
            reloj.Avanzar(1f);

            CollectionAssert.AreEqual(new[] { 2 }, dias);
            Assert.AreEqual(2, reloj.Dia);
        }

        [Test]
        public void Avanzar_SinCruzarHora_NoAvisaNada()
        {
            Preparar(10.1f);
            reloj.Avanzar(0.5f);

            Assert.IsEmpty(horas);
            Assert.IsEmpty(dias);
            Assert.IsEmpty(franjas);
        }

        [Test]
        public void Avanzar_PorLaHoraDeAmanecerYAnochecer_AvisaLaFranja()
        {
            Preparar(6.5f);
            reloj.Avanzar(1f);
            CollectionAssert.AreEqual(new[] { FranjaDia.Dia }, franjas);

            reloj.Avanzar(12f);
            CollectionAssert.AreEqual(new[] { FranjaDia.Dia, FranjaDia.Noche }, franjas);
        }

        [Test]
        public void Franja_En0659EsNocheYEn0700EsDia()
        {
            Preparar(6.99f);
            Assert.AreEqual(FranjaDia.Noche, reloj.Franja);
            Preparar(7f);
            Assert.AreEqual(FranjaDia.Dia, reloj.Franja);
            Preparar(19f);
            Assert.AreEqual(FranjaDia.Noche, reloj.Franja);
        }

        [Test]
        public void AvanzarHasta_MismaHora_NoHaceNadaNiAvisa()
        {
            Preparar(8f);
            reloj.AvanzarHasta(8f);

            Assert.AreEqual(1, reloj.Dia);
            Assert.IsEmpty(horas);
            Assert.IsEmpty(dias);
            Assert.IsEmpty(franjas);
        }

        [Test]
        public void AvanzarHasta_HoraPasada_SaltaAlDiaSiguienteConUnSoloAvisoDeHora()
        {
            Preparar(21f);
            reloj.AvanzarHasta(8f);

            Assert.AreEqual(2, reloj.Dia);
            Assert.AreEqual(8f, reloj.Hora, 0.0001f);
            CollectionAssert.AreEqual(new[] { 2 }, dias);
            CollectionAssert.AreEqual(new[] { 8 }, horas);
            CollectionAssert.AreEqual(new[] { FranjaDia.Dia }, franjas);
        }

        [Test]
        public void AvanzarHasta_HoraFuturaDeHoy_NoCambiaElDia()
        {
            Preparar(8f);
            reloj.AvanzarHasta(12f);

            Assert.AreEqual(1, reloj.Dia);
            Assert.IsEmpty(dias);
            CollectionAssert.AreEqual(new[] { 12 }, horas);
            Assert.IsEmpty(franjas, "Dia a Dia: la franja no cambió");
        }

        [Test]
        public void AvanzarHastaSiete_DesdeLas21_EnSieteSaltosLlegaAlDia8()
        {
            Preparar(21f);
            reloj.AvanzarHasta(8f);
            for (int i = 0; i < 6; i++) reloj.Avanzar(24f);

            Assert.AreEqual(8, reloj.Dia);
        }

        [Test]
        public void Avanzar_DosMedianoches_AvisaDosDiaCambioEnOrden()
        {
            Preparar(23f);
            reloj.Avanzar(26f);

            CollectionAssert.AreEqual(new[] { 2, 3 }, dias);
            Assert.AreEqual(3, reloj.Dia);
        }

        [Test]
        public void Restablecer_FijaDiaYHoraSinAvisarDiaNiHora_PeroSiLaFranja()
        {
            Preparar(21f);
            reloj.Restablecer(5, 12f);

            Assert.AreEqual(5, reloj.Dia);
            Assert.AreEqual(12f, reloj.Hora, 0.0001f);
            Assert.IsEmpty(dias);
            Assert.IsEmpty(horas);
            CollectionAssert.AreEqual(new[] { FranjaDia.Dia }, franjas);
        }
    }
}
