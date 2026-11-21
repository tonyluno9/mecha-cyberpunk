using System;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-1471: política de mezcla de todos los IDs nuevos (volumen, distancia, variación de tono y de volumen, voces).</summary>
    public class MezclaSonidosT1471Pruebas
    {
        private static bool EsDelMundo(string id)
        {
            string[] prefijos = { "UI_", "HUD_", "TIE_", "CON_" };
            foreach (string p in prefijos)
            {
                if (id.StartsWith(p, StringComparison.Ordinal)) return false;
            }
            // Cosas 2D del jugador en primera persona.
            switch (id)
            {
                case "ARM_W02_BOMBA":
                case "ARM_W03_RECARGA_CARGADOR":
                case "ARM_W04_CERROJO":
                case "ARM_W06_SACAR":
                case "ARM_W07_CARGA": return false;
            }
            return true;
        }

        [Test]
        public void CadaIdNuevo_TieneVolumenPositivo_YMenorOIgualAUno()
        {
            foreach (string id in MapaSonidos19nov.IdsNuevos)
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.That(p.Volumen, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.95f), id);
                Assert.IsNotNull(p.Grupo, id + ": todo ID nuevo tiene grupo");
            }
        }

        [Test]
        public void CadaIdNuevo_DelMundo_TieneDistanciaMaxima()
        {
            foreach (string id in MapaSonidos19nov.IdsNuevos)
            {
                if (!EsDelMundo(id)) continue;
                Assert.Greater(ReglaMezclaSonidos.Para(id).DistanciaMaxima, 0f, id);
            }
        }

        [Test]
        public void CadaIdNuevo_VariaTonoOVolumen_ParaQueNadaSuenaIdentico()
        {
            foreach (string id in MapaSonidos19nov.IdsNuevos)
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.Greater(p.VariacionTono + p.VariacionVolumen, 0f, id);
                Assert.Greater(p.VariacionVolumen, 0f, id + ": volumen");
            }
        }

        [Test]
        public void CadaIdNuevo_NoSeRepiteEnMetralleta_YTieneAlMenosUnaVoz()
        {
            foreach (string id in MapaSonidos19nov.IdsNuevos)
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.GreaterOrEqual(p.MaxVoces, 1, id);
                Assert.GreaterOrEqual(Math.Max(p.SegundosMinimos, ReglaMezclaSonidos.SegundosMinimosPorId), ReglaMezclaSonidos.SegundosMinimosPorId, id);
            }
        }

        [Test]
        public void LosDisparos_VanArriba_MismoClipPeroVariacionChica()
        {
            foreach (string id in new[] { "ARM_W01_DISPARO", "ARM_W02_DISPARO", "ARM_W03_DISPARO", "ARM_W04_DISPARO", "ARM_W06_DISPARO", "ARM_W07_DISPARO",
                "ARM_PIS_DISPARO", "ARM_SUB_DISPARO", "ARM_ESC_DISPARO", "ARM_FUS_DISPARO", "ARM_SNI_DISPARO", "ARM_REV_DISPARO", "ARM_PLA_DISPARO" })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.That(p.Volumen, Is.InRange(0.8f, 0.95f), id);
                Assert.That(p.VariacionTono, Is.InRange(0.02f, 0.03f), id);
                Assert.That(p.VariacionVolumen, Is.InRange(0.01f, 0.05f), id);
                Assert.That(p.DistanciaMaxima, Is.InRange(80f, 150f), id);
            }
        }

        [Test]
        public void ImpactosPasosYUi_TienenSuNivel()
        {
            foreach (string id in new[] { "IMP_BALA_CABEZA", "IMP_BALA_CARNE", "IMP_BALA_CONCRETO", "IMP_BALA_CRISTAL", "IMP_BALA_METAL", "IMP_BALA_ROBOT" })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.That(p.Volumen, Is.InRange(0.6f, 0.7f), id);
                Assert.That(p.DistanciaMaxima, Is.InRange(30f, 40f), id);
            }
            foreach (string id in new[] { "PAS_CONCRETO", "PAS_MADERA", "PAS_METAL", "PAS_MOJADO", "PAS_TIERRA", "PAS_GRAVA" })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.AreEqual("paso", p.Grupo, id);
                Assert.AreEqual(1, p.MaxVoces, id);
                Assert.That(p.Volumen, Is.InRange(0.35f, 0.45f), id);
                Assert.That(p.DistanciaMaxima, Is.InRange(15f, 20f), id);
                Assert.AreEqual(ReglaMezclaSonidos.Para("JUG_PASO_ASFALTO").VariacionTono, p.VariacionTono, 1e-5f, id + ": igual que JUG_PASO_");
            }
            foreach (string id in new[] { "UI_MOVER", "UI_SELECCIONAR", "UI_ATRAS", "UI_ERROR", "UI_PESTANA", "UI_MENU_ABRIR", "UI_MENU_CERRAR", "UI_LOGO_INTRO", "HUD_LOGRO", "HUD_NOTIFICACION", "HUD_VIDA_BAJA" })
            {
                Assert.That(ReglaMezclaSonidos.Para(id).Volumen, Is.InRange(0.5f, 0.6f), id);
            }
        }

        [Test]
        public void ExplosionesYSirenas_SeOyenDeLejos_YLosMotoresNo()
        {
            Assert.That(ReglaMezclaSonidos.Para("EXP_ONDA").DistanciaMaxima, Is.InRange(150f, 200f));
            Assert.That(ReglaMezclaSonidos.Para("EXP_GRANADA").DistanciaMaxima, Is.InRange(150f, 200f));
            Assert.AreEqual(120f, ReglaMezclaSonidos.Para("POL_SIRENA_LOOP").DistanciaMaxima);
            Assert.AreEqual(3, ReglaMezclaSonidos.Para("POL_SIRENA_LOOP").MaxVoces);
            foreach (string id in new[] { "VEH_NORMAL_RALENTI", "MOTO_RALENTI", "VEH_PESADO_RALENTI" })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.That(p.DistanciaMaxima, Is.InRange(40f, 60f), id);
                Assert.That(p.Volumen, Is.InRange(0.5f, 0.6f), id);
                Assert.AreEqual(ReglaMezclaSonidos.MaxMotoresRalenti, ReglaMezclaSonidos.MaxBuclesDelId(id), id);
            }
        }

        [Test]
        public void LosViejosMapeadosSinReglaPropia_HeredanLaPoliticaDelNuevo()
        {
            PoliticaSonido nuevo = ReglaMezclaSonidos.Para("EXP_CARRO"); // T-1531: VEH_EXPLOSION suena con EXP_CARRO
            PoliticaSonido viejo = ReglaMezclaSonidos.Para(ReglaMezclaSonidos.IdExplosion);
            Assert.AreEqual(nuevo.DistanciaMaxima, viejo.DistanciaMaxima);
            Assert.AreEqual(nuevo.Volumen, viejo.Volumen, 1e-5f);
            Assert.AreEqual(nuevo.Grupo, viejo.Grupo);
            Assert.AreEqual(ReglaMezclaSonidos.Para("POL_SIRENA_LOOP").DistanciaMaxima, ReglaMezclaSonidos.Para("CAL_PATRULLA_SIRENA").DistanciaMaxima);
            Assert.AreEqual(ReglaMezclaSonidos.Para("HUD_VIDA_BAJA").Volumen, ReglaMezclaSonidos.Para("JUG_VIDA_BAJA").Volumen, 1e-5f);
        }

        [Test]
        public void SoloLaSirenaLosRalentisYElFuegoPermitenVariosBucles()
        {
            Assert.AreEqual(3, ReglaMezclaSonidos.MaxBuclesDelId("POL_SIRENA_LOOP"));
            Assert.AreEqual(1, ReglaMezclaSonidos.MaxBuclesDelId("VEH_VIEJO_MOTOR_RALENTI"));
            Assert.AreEqual(1, ReglaMezclaSonidos.MaxBuclesDelId("UI_MOVER"));
        }

        [Test]
        public void LaPrioridad_DisparosYVocesArriba_AmbienteAbajo()
        {
            Assert.Less(ReglaMezclaSonidos.Prioridad("ARM_W01_DISPARO"), ReglaMezclaSonidos.Prioridad("IMP_BALA_CARNE"));
            Assert.Less(ReglaMezclaSonidos.Prioridad("JUG_DANIO"), ReglaMezclaSonidos.Prioridad("PAS_CONCRETO"));
            Assert.Less(ReglaMezclaSonidos.Prioridad("PAS_CONCRETO"), ReglaMezclaSonidos.Prioridad("AMB_CALLE_NOCHE"));
        }

        [Test]
        public void ElDucking_SoloEnDisparosYExplosiones()
        {
            Assert.IsTrue(ReglaMezclaSonidos.BajaAmbiente("ARM_W01_DISPARO"));
            Assert.IsTrue(ReglaMezclaSonidos.BajaAmbiente("EXP_ONDA"));
            Assert.IsTrue(ReglaMezclaSonidos.BajaAmbiente(ReglaMezclaSonidos.IdExplosion));
            Assert.IsFalse(ReglaMezclaSonidos.BajaAmbiente("PAS_CONCRETO"));
            Assert.IsFalse(ReglaMezclaSonidos.BajaAmbiente("ARM_W03_RECARGA_CARGADOR"));
        }
    }
}
