using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-977: regresan los sonidos de docs/sonidos-sugerencias.md en su orden, con volumen y tiempo entre repeticiones; los molestos no; mezcla final.</summary>
    public class SonidosRegresanT977Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        private static readonly string[] OrdenDeLaLista =
        {
            "AMB_CALLE_NOCHE", "VEH_PUERTA_ABRIR", "VEH_PUERTA_CERRAR", "ARM_BALA_IMPACTO_CONCRETO", "PAN_RIVAL_ALERTA", "PAN_JEFE_ENTRADA",
            "UI_OBJETIVO_NUEVO", "UI_OBJETIVO_CUMPLIDO", "VEH_FRENO_DERRAPE", "JUG_PASO_ASFALTO", "CAL_SUBE_NIVEL", "ECO_DINERO_RECOGER",
            "MUN_PUERTA_CASA_ABRIR", "MUN_PUERTA_CASA_CERRAR", "AMB_CASINO", "AMB_CLINICA",
        };

        // RMS medido de los archivos (dB, la parte activa): con su nivel, los tres ambientes quedan cerca uno de otro.
        private static readonly Dictionary<string, float> RmsAmbiente = new Dictionary<string, float>
        {
            { "AMB_CALLE_NOCHE", -46.5f }, { "AMB_CASINO", -37.8f }, { "AMB_CLINICA", -27.1f },
        };

        private static CatalogoSonidos Catalogo()
        {
            CatalogoSonidos c = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(c);
            return c;
        }

        [Test]
        public void LosQueRegresan_SonLosDeLaLista_EnSuOrden_ConArchivo_YEncendidos()
        {
            CollectionAssert.AreEqual(OrdenDeLaLista, ReglaMezclaSonidos.IdsRegresados);
            CatalogoSonidos c = Catalogo();
            foreach (string id in ReglaMezclaSonidos.IdsRegresados)
            {
                Assert.IsTrue(c.Tiene(id), id + " tiene archivo");
                Assert.IsTrue(c.Encendido(id), id + " encendido en el catálogo");
            }
        }

        [Test]
        public void LosMolestos_SiguenApagados()
        {
            CatalogoSonidos c = Catalogo();
            foreach (string id in ReglaMezclaSonidos.IdsMolestos)
            {
                Assert.IsFalse(c.Encendido(id), id + " es de los molestos");
                Assert.IsFalse(ReglaMezclaSonidos.EncendidoPorDefecto(id), id);
            }
            foreach (string id in new[] { "AMB_TIANGUIS", "AMB_INTERIOR_CASA", "AMB_MERCADO", "AMB_BAR", "AMB_CLANDESTINO", "AMB_CALLE_DIA", "JUG_PASO_INTERIOR", "ARM_BALA_ROZA" })
            {
                Assert.IsFalse(c.Encendido(id), id + " no estaba en la lista");
            }
        }

        [Test]
        public void CadaUnoTieneVolumenRazonable_YNoSeRepiteEnExceso()
        {
            foreach (string id in ReglaMezclaSonidos.IdsRegresados)
            {
                if (id.StartsWith("AMB_", StringComparison.Ordinal))
                {
                    Assert.That(ReglaMezclaSonidos.VolumenAmbiente(id), Is.InRange(0.05f, 1f), id);
                    continue;
                }
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.That(p.Volumen, Is.InRange(0.4f, 0.9f), id + ": volumen");
                Assert.That(p.MaxVoces, Is.InRange(1, 2), id + ": límite de voces");
                Assert.GreaterOrEqual(p.SegundosMinimos, ReglaMezclaSonidos.SegundosMinimosPorId, id + ": tiempo entre repeticiones");
            }
            Assert.AreEqual(1, ReglaMezclaSonidos.Para("PAN_RIVAL_ALERTA").MaxVoces, "un solo grito aunque te vean varios");
            Assert.GreaterOrEqual(ReglaMezclaSonidos.Para("PAN_RIVAL_ALERTA").SegundosMinimos, 2f);
            Assert.Less(ReglaMezclaSonidos.Para("JUG_PASO_ASFALTO").Volumen, ReglaMezclaSonidos.Para("ARM_PIS_DISPARO").Volumen, "los pasos van bajo los disparos");
            Assert.Less(ReglaMezclaSonidos.Para("ARM_BALA_IMPACTO_CONCRETO").Volumen, ReglaMezclaSonidos.Para("ARM_PIS_DISPARO").Volumen, "el impacto bajo el disparo");
        }

        [Test]
        public void LosAmbientes_QuedanParejosEntreSi_YBajitos()
        {
            CatalogoSonidos c = Catalogo();
            float baseAmbiente = c.Ganancia(SeccionSonido.Ambiente) * c.PisoOpcionAmbiente;
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (KeyValuePair<string, float> par in RmsAmbiente)
            {
                float db = par.Value + 20f * Mathf.Log10(baseAmbiente * ReglaMezclaSonidos.VolumenAmbiente(par.Key));
                min = Mathf.Min(min, db);
                max = Mathf.Max(max, db);
                Assert.Less(db, -40f, par.Key + " queda bajito (" + db + " dB)");
            }
            Assert.LessOrEqual(max - min, 4f, "entre ambientes no hay saltos de volumen al cambiar de zona");
        }

        [Test]
        public void LaMezclaFinal_EstaEnElAsset()
        {
            CatalogoSonidos c = Catalogo();
            Assert.AreEqual(CatalogoSonidos.MezclaEfectos, c.Ganancia(SeccionSonido.Efectos), 1e-4f);
            Assert.AreEqual(CatalogoSonidos.MezclaVoces, c.Ganancia(SeccionSonido.Voces), 1e-4f);
            Assert.AreEqual(CatalogoSonidos.MezclaAmbiente, c.Ganancia(SeccionSonido.Ambiente), 1e-4f);
            Assert.AreEqual(CatalogoSonidos.MezclaMusica, c.Ganancia(SeccionSonido.Musica), 1e-4f);
            Assert.AreEqual(CatalogoSonidos.MezclaInterfaz, c.Ganancia(SeccionSonido.Interfaz), 1e-4f);
            Assert.Less(CatalogoSonidos.MezclaInterfaz, CatalogoSonidos.MezclaEfectos, "la interfaz debajo de los efectos");
        }

        [Test]
        public void LosPasos_SoloLosPideElJugador()
        {
            List<string> donde = new List<string>();
            foreach (string f in Directory.GetFiles("Assets/_Proyecto", "*.cs", SearchOption.AllDirectories))
            {
                string n = f.Replace('\\', '/');
                // La tabla de mezcla nombra los IDs pero no los pide.
                if (n.Contains("/Tests/") || n.Contains("/Editor/") || n.Contains("/Progresion/Sonido/")) continue;
                string texto = File.ReadAllText(f);
                if (texto.Contains("\"JUG_PASO_") || texto.Contains("ReglaSuperficiePaso.IdPaso(")) donde.Add(n);
            }
            CollectionAssert.AreEquivalent(new[] { "Assets/_Proyecto/Personaje/Movimiento/ReglaSuperficiePaso.cs", "Assets/_Proyecto/Personaje/Movimiento/ControladorAPie.cs" }, donde);
        }
    }
}
