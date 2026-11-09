using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-814: reglas del banco de armas (pieza que sigue, bloqueadas, precio y tabla antes/después).</summary>
    public class ReglaMejoraArmaPruebas
    {
        [TestCase(PiezaArma.Cargador, 1)]
        [TestCase(PiezaArma.Canon, 2)]
        [TestCase(PiezaArma.Mira, 3)]
        public void NivelDe_CadaPieza_EsSuNivelEnOrden(PiezaArma pieza, int nivel)
        {
            Assert.AreEqual(nivel, ReglaMejoraArma.NivelDe(pieza));
            Assert.AreEqual(pieza, ReglaMejoraArma.PiezaDeNivel(nivel));
        }

        [Test]
        public void Estado_ArmaBase_CargadorDisponibleYLoDemasBloqueado()
        {
            Assert.AreEqual(EstadoPieza.Disponible, ReglaMejoraArma.Estado(PiezaArma.Cargador, 0));
            Assert.AreEqual(EstadoPieza.Bloqueada, ReglaMejoraArma.Estado(PiezaArma.Canon, 0));
            Assert.AreEqual(EstadoPieza.Bloqueada, ReglaMejoraArma.Estado(PiezaArma.Mira, 0));
            Assert.AreEqual(PiezaArma.Cargador, ReglaMejoraArma.Siguiente(0));
        }

        [Test]
        public void Estado_NivelDos_CargadorYCanonInstaladosMiraDisponible()
        {
            Assert.AreEqual(EstadoPieza.Instalada, ReglaMejoraArma.Estado(PiezaArma.Cargador, 2));
            Assert.AreEqual(EstadoPieza.Instalada, ReglaMejoraArma.Estado(PiezaArma.Canon, 2));
            Assert.AreEqual(EstadoPieza.Disponible, ReglaMejoraArma.Estado(PiezaArma.Mira, 2));
            Assert.AreEqual(PiezaArma.Mira, ReglaMejoraArma.Siguiente(2));
            Assert.IsTrue(ReglaMejoraArma.HaySiguiente(2));
        }

        [TestCase(FiltroArmas.Todas, 0, true)]
        [TestCase(FiltroArmas.Todas, 3, true)]
        [TestCase(FiltroArmas.Mejorables, 2, true)]
        [TestCase(FiltroArmas.Mejorables, 3, false)]
        [TestCase(FiltroArmas.AlMaximo, 2, false)]
        [TestCase(FiltroArmas.AlMaximo, 3, true)]
        public void PasaFiltro_SegunPestanaYNivel(FiltroArmas filtro, int nivel, bool esperado)
        {
            Assert.AreEqual(esperado, ReglaMejoraArma.PasaFiltro(filtro, nivel));
        }

        [Test]
        public void Estado_AlMaximo_TodoInstaladoYNoHaySiguiente()
        {
            foreach (PiezaArma p in new[] { PiezaArma.Cargador, PiezaArma.Canon, PiezaArma.Mira })
            {
                Assert.AreEqual(EstadoPieza.Instalada, ReglaMejoraArma.Estado(p, 3), p.ToString());
            }
            Assert.IsFalse(ReglaMejoraArma.HaySiguiente(3));
        }

        [Test]
        public void Estado_SinElArma_TodoBloqueado()
        {
            Assert.AreEqual(EstadoPieza.Bloqueada, ReglaMejoraArma.Estado(PiezaArma.Cargador, -1));
            Assert.IsFalse(ReglaMejoraArma.HaySiguiente(-1));
        }

        [Test]
        public void PiezaQueFalta_MiraConArmaBase_PideElCargador()
        {
            Assert.AreEqual(PiezaArma.Cargador, ReglaMejoraArma.PiezaQueFalta(PiezaArma.Mira, 0));
            Assert.AreEqual(PiezaArma.Canon, ReglaMejoraArma.PiezaQueFalta(PiezaArma.Mira, 1));
            Assert.AreEqual(PiezaArma.Canon, ReglaMejoraArma.PiezaQueFalta(PiezaArma.Canon, 1), "La disponible no pide otra");
        }

        [Test]
        public void Precio_ConOferta_CobraLaOfertaYSinOfertaElDelArma()
        {
            Assert.AreEqual(180, ReglaMejoraArma.Precio(180, true, 150));
            Assert.AreEqual(150, ReglaMejoraArma.Precio(0, false, 150));
            DatosArma nivelI = DatosArma.Crear("p1", "Pistola I", ArmaAPie.Pistola, 1, 150, 30f, 1, 10, 1.2f, 0.25f, false, 0f, 0f, 60f);
            DatosArma sinOferta = DatosArma.Crear("p2", "Pistola II", ArmaAPie.Pistola, 2, 300, 36f, 1, 12, 1f, 0.25f, false, 0f, 0f, 60f);
            Assert.AreEqual(150, ReglaMejoraArma.Precio(nivelI));
            Assert.AreEqual(300, ReglaMejoraArma.Precio(sinOferta));
            Assert.AreEqual(0, ReglaMejoraArma.Precio(null));
            Object.DestroyImmediate(nivelI);
            Object.DestroyImmediate(sinOferta);
        }

        [Test]
        public void Comparar_PistolaBaseContraI_MejoraDanioCargadorYRecarga()
        {
            DatosArma baseArma = DatosArma.Crear("p0", "Pistola", ArmaAPie.Pistola, 0, 150, 25f, 1, 8, 1.4f, 0.25f, false, 0f, 0f, 60f);
            DatosArma nivelI = DatosArma.Crear("p1", "Pistola I", ArmaAPie.Pistola, 1, 150, 30f, 1, 10, 1.2f, 0.25f, false, 0f, 0f, 60f);
            var filas = new List<FilaEstadistica>();
            ReglaMejoraArma.Comparar(EstadisticasArma.De(baseArma), EstadisticasArma.De(nivelI), EstadisticaArma.Cargador, filas);

            Assert.AreEqual(5, filas.Count);
            FilaEstadistica danio = filas[0];
            FilaEstadistica cargador = filas[1];
            FilaEstadistica recarga = filas[2];
            FilaEstadistica cadencia = filas[3];
            Assert.AreEqual(EstadisticaArma.Danio, danio.Estadistica);
            Assert.AreEqual(25f, danio.Antes);
            Assert.AreEqual(30f, danio.Despues);
            Assert.AreEqual(1, danio.Cambio);
            Assert.IsTrue(cargador.Resaltada, "La estadística de la pieza va resaltada");
            Assert.IsFalse(danio.Resaltada);
            Assert.AreEqual(8f, cargador.Antes);
            Assert.AreEqual(10f, cargador.Despues);
            Assert.AreEqual(1, recarga.Cambio, "Menos recarga es mejor");
            Assert.AreEqual(0, cadencia.Cambio, "La cadencia no cambia");
            Assert.AreEqual(4f, cadencia.Antes, 0.001f, "0.25 s entre disparos = 4 por segundo");
            Object.DestroyImmediate(baseArma);
            Object.DestroyImmediate(nivelI);
        }

        [Test]
        public void Resaltada_MiraDePistolaSinCambioDeDispersion_ResaltaLaQueMasMejora()
        {
            // Pistola II → III (DatosArma_pistola_2/3): la dispersión queda en 0, el cargador sube 25 %, el daño 22 % y la recarga baja 20 %.
            var antes = new EstadisticasArma(36f, 12, 1f, 4f, 0f);
            var despues = new EstadisticasArma(44f, 15, 0.8f, 4f, 0f);
            Assert.AreEqual(EstadisticaArma.Cargador, ReglaMejoraArma.Resaltada(antes, despues, EstadisticaArma.Dispersion));
            var filas = new List<FilaEstadistica>();
            ReglaMejoraArma.Comparar(antes, despues, EstadisticaArma.Dispersion, filas);
            Assert.IsFalse(filas[4].Resaltada, "No se resalta la dispersión 0 → 0");
            Assert.IsTrue(filas[1].Resaltada, "Se resalta lo que de verdad mejora");
        }

        [Test]
        public void Resaltada_LaDeLaPiezaCambia_SeQuedaLaDeLaPieza()
        {
            var antes = new EstadisticasArma(9f, 4, 2.4f, 1.1f, 6f);
            var despues = new EstadisticasArma(9f, 4, 2.4f, 1.1f, 4f);
            Assert.AreEqual(EstadisticaArma.Dispersion, ReglaMejoraArma.Resaltada(antes, despues, EstadisticaArma.Dispersion));
            Assert.AreEqual(EstadisticaArma.Recarga, ReglaMejoraArma.Resaltada(antes, antes, EstadisticaArma.Recarga), "Si nada cambia, la de la pieza");
        }

        [Test]
        public void EstadisticasDe_Escopeta_SumaLosPerdigonesAlDanio()
        {
            DatosArma escopeta = DatosArma.Crear("e0", "Escopeta", ArmaAPie.Escopeta, 0, 350, 9f, 8, 4, 2.4f, 0.9f, false, 6f, 4f, 20f);
            EstadisticasArma e = EstadisticasArma.De(escopeta);
            Assert.AreEqual(72f, e.Danio);
            Assert.AreEqual(6f, e.Dispersion);
            Object.DestroyImmediate(escopeta);
        }

        [Test]
        public void Cambio_DispersionMenor_EsMejora()
        {
            Assert.AreEqual(1, ReglaMejoraArma.Cambio(EstadisticaArma.Dispersion, 6f, 4f));
            Assert.AreEqual(-1, ReglaMejoraArma.Cambio(EstadisticaArma.Danio, 30f, 25f));
            Assert.AreEqual(0, ReglaMejoraArma.Cambio(EstadisticaArma.Cargador, 8f, 8f));
        }

        [Test]
        public void Fraccion_ContraElNivelTres_LlenaSegunLoMejor()
        {
            Assert.AreEqual(0.5f, ReglaMejoraArma.Fraccion(EstadisticaArma.Cargador, 8f, 16f), 0.001f);
            Assert.AreEqual(1f, ReglaMejoraArma.Fraccion(EstadisticaArma.Danio, 50f, 44f), "No pasa de lleno");
            Assert.AreEqual(0.5f, ReglaMejoraArma.Fraccion(EstadisticaArma.Recarga, 1.6f, 0.8f), 0.001f, "En recarga, menos llena más");
            Assert.AreEqual(1f, ReglaMejoraArma.Fraccion(EstadisticaArma.Dispersion, 0f, 0f), "Sin dispersión: lleno");
        }

        [Test]
        public void Texto_FormateaCadaEstadistica()
        {
            Assert.AreEqual("30", ReglaMejoraArma.Texto(EstadisticaArma.Danio, 30f));
            Assert.AreEqual("1.2 s", ReglaMejoraArma.Texto(EstadisticaArma.Recarga, 1.2f));
            Assert.AreEqual("4.0/s", ReglaMejoraArma.Texto(EstadisticaArma.Cadencia, 4f));
            Assert.AreEqual("6.0°", ReglaMejoraArma.Texto(EstadisticaArma.Dispersion, 6f));
            Assert.AreEqual("+2", ReglaMejoraArma.TextoDiferencia(EstadisticaArma.Cargador, 8f, 10f));
            Assert.AreEqual("-0.2 s", ReglaMejoraArma.TextoDiferencia(EstadisticaArma.Recarga, 1.4f, 1.2f));
            Assert.AreEqual(string.Empty, ReglaMejoraArma.TextoDiferencia(EstadisticaArma.Cadencia, 4f, 4f));
        }
    }
}
