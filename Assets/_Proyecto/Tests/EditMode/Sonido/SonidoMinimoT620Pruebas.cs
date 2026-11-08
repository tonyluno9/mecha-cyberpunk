using System.Collections.Generic;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-620: solo suenan los IDs encendidos; limite de 12 voces, una por ID, 0.1 s entre repeticiones y volumen parejo.</summary>
    public class SonidoMinimoT620Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        [Test]
        public void SinInterruptores_SuenaTodo()
        {
            CatalogoSonidos c = ScriptableObject.CreateInstance<CatalogoSonidos>();
            Assert.IsTrue(c.Encendido("JUG_PASO_ASFALTO"));
        }

        [Test]
        public void ConInterruptores_SoloSuenaLoEncendido()
        {
            CatalogoSonidos c = ScriptableObject.CreateInstance<CatalogoSonidos>();
            c.FijarEncendido("ARM_PIS_DISPARO", true);
            c.FijarEncendido("UI_TEXTO", false);
            Assert.IsTrue(c.Encendido("ARM_PIS_DISPARO"));
            Assert.IsFalse(c.Encendido("UI_TEXTO"), "apagado");
            Assert.IsFalse(c.Encendido("JUG_PASO_ASFALTO"), "fuera de la lista = apagado");
            c.FijarEncendido("UI_TEXTO", true);
            Assert.IsTrue(c.Encendido("UI_TEXTO"), "se puede prender");
        }

        [Test]
        public void Sincronizar_PoneLosEncendidosPorDefectoYConservaLoPuesto()
        {
            CatalogoSonidos c = ScriptableObject.CreateInstance<CatalogoSonidos>();
            c.Fijar(new List<CatalogoSonidos.Entrada> { new CatalogoSonidos.Entrada { id = "UI_TEXTO" }, new CatalogoSonidos.Entrada { id = "UI_MOVER" } });
            c.FijarEncendido("UI_TEXTO", true);
            c.SincronizarInterruptores();
            Assert.IsTrue(c.Encendido("UI_TEXTO"), "lo que ya estaba puesto se conserva");
            Assert.IsTrue(c.Encendido("UI_MOVER"));
            Assert.IsTrue(c.Encendido(ReglaMezclaSonidos.IdExplosion));
            Assert.IsFalse(c.Encendido("AMB_CALLE_NOCHE"));
        }

        [Test]
        public void ElAssetDelJuego_TieneEncendidosSoloLosPedidos()
        {
            CatalogoSonidos c = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(c);
            Assert.AreEqual(24, ReglaMezclaSonidos.IdsEncendidos.Length); // T-729: sin el silbido de la bala rozando (el silbato de la policía)
            int encendidos = 0;
            foreach (CatalogoSonidos.Interruptor i in c.Interruptores)
            {
                if (i.encendido)
                {
                    encendidos++;
                    Assert.IsTrue(ReglaMezclaSonidos.EncendidoPorDefecto(i.id), i.id + " no deberia estar encendido");
                }
            }
            foreach (string id in ReglaMezclaSonidos.IdsEncendidos)
            {
                Assert.IsTrue(c.Encendido(id), id);
            }
            Assert.AreEqual(ReglaMezclaSonidos.IdsEncendidos.Length, encendidos);
            foreach (string apagado in new[] { "UI_TEXTO", "VEH_CLAXON_TRAFICO", "JUG_PASO_ASFALTO", "AMB_CALLE_NOCHE", "PAN_RIVAL_ALERTA", "VEH_PUERTA_ABRIR" })
            {
                Assert.IsFalse(c.Encendido(apagado), apagado);
            }
        }

        [Test]
        public void ElAssetDelJuego_TieneClipDeCadaEncendido()
        {
            CatalogoSonidos c = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            List<string> sinClip = new List<string>();
            foreach (string id in ReglaMezclaSonidos.IdsEncendidos)
            {
                if (!c.Tiene(id)) sinClip.Add(id);
            }
            CollectionAssert.IsEmpty(sinClip, "todos los encendidos tienen archivo (el codigo solo pide RALENTI para el motor; ACELERA queda listo)");
        }

        [Test]
        public void LosEncendidosSuenanParejos()
        {
            foreach (string id in ReglaMezclaSonidos.IdsEncendidos)
            {
                Assert.AreEqual(ReglaMezclaSonidos.VolumenParejo, ReglaMezclaSonidos.Para(id).Volumen, 1e-5f, id);
            }
        }

        [Test]
        public void MismoIdAntesDeUnaDecima_Repite()
        {
            Assert.IsTrue(ReglaMezclaSonidos.Repite(10.05f, 10f, 0f));
            Assert.IsFalse(ReglaMezclaSonidos.Repite(10.11f, 10f, 0f));
            Assert.IsTrue(ReglaMezclaSonidos.Repite(10.5f, 10f, 1f), "la politica propia puede pedir mas");
            Assert.AreEqual(12, ReglaMezclaSonidos.MaxVocesTotal);
        }

        [Test]
        public void T729_ElSilbidoDeLaBalaQueRoza_EstaApagadoEnElCatalogo()
        {
            CatalogoSonidos c = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(c);
            Assert.IsFalse(c.Encendido("ARM_BALA_ROZA"), "T-729: el silbato de la policía (el silbido de la bala que roza) ya no suena");
            Assert.IsTrue(c.Encendido("CAL_PATRULLA_SIRENA"), "La sirena sigue");
        }
    }
}
