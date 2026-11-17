using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1323: las reglas de la llegada de Nova (caída, destello, aviso de 5 estrellas, tamaño del pesado, quién persigue).</summary>
    public class NovaT1323ReglasPruebas
    {
        [Test]
        public void SegundosDeCaida_DoceMetros_EsLaFormulaDeLaGravedad()
        {
            Assert.AreEqual(Mathf.Sqrt(2f * 12f / 20f), RespuestaNova.SegundosDeCaida(12f), 0.001f);
            Assert.AreEqual(0f, RespuestaNova.SegundosDeCaida(0f), 0.001f);
            Assert.Greater(RespuestaNova.SegundosDeCaida(24f), RespuestaNova.SegundosDeCaida(12f));
        }

        [Test]
        public void ConfigPorOmision_CaeDeArriba_ConAvisoQueSuena_YPesadoMasGrande()
        {
            ConfigRespuestaNova config = ScriptableObject.CreateInstance<ConfigRespuestaNova>();
            try
            {
                Assert.Greater(config.AlturaCaida, 3f, "caen desde unos metros arriba");
                Assert.Greater(config.SegundosDestello, 0f);
                Assert.Greater(config.IntensidadDestello, 0f);
                Assert.IsFalse(string.IsNullOrEmpty(config.SonidoLlegada), "hay un ID para el aviso de 5 estrellas");
                Assert.IsTrue(ReglaMezclaSonidos.EncendidoPorDefecto(config.SonidoLlegada), "el aviso usa un sonido que sí está encendido");
                Assert.Greater(config.EscalaPesado, 1f, "el pesado se agranda");
                Assert.GreaterOrEqual(config.DistanciaVista, config.DistanciaMaxima, "los que aparecen ya te vieron");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ZonaCabeza_Constantes_CabenEnLaRegladeCabeza()
        {
            // El centro de la esfera está donde ReglaZonaDanio mide la cabeza (10 cm sobre el hueso) y su radio no se sale del 20 cm de tolerancia.
            Assert.AreEqual(0.1f, ZonaCabezaNova.SubidaCentro, 0.0001f);
            Assert.LessOrEqual(ZonaCabezaNova.RadioEsfera, 0.2f);
            Assert.Greater(ZonaCabezaNova.RadioEsfera, 0.05f);
        }
    }
}
