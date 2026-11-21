using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1553: la fase de disparo del cañón del E16 (decisión de Tony 20-nov): se detiene, avisa, juega E16_Disparar_Canon y dispara con el brazo extendido
    /// (cuadro 10 de 37); nunca camina mientras dispara; solo en rango, con enfriamiento y solo si lo pide la conducta (persiguiendo).
    /// </summary>
    public class ReglaCanonE16Pruebas
    {
        private const float Dt = 0.01f;

        private static ReglaCanonE16 Crear() => new ReglaCanonE16(ReglaCanonE16.Parametros.Diseno);

        private static ReglaCanonE16.Entrada En(float distancia, bool enPelea = true, bool puede = true) =>
            new ReglaCanonE16.Entrada { Distancia = distancia, EnPelea = enPelea, PuedeIniciar = puede };

        // Avanza hasta que empiece el aviso; devuelve los segundos que tardó (o -1).
        private static float HastaAviso(ReglaCanonE16 r, float distancia, float max = 20f)
        {
            for (float t = 0f; t < max; t += Dt)
            {
                if (r.Avanzar(Dt, En(distancia)).EmpezoAviso)
                {
                    return t;
                }
            }
            return -1f;
        }

        [Test]
        public void Diseno_ElTiroSaleEnElCuadroDiezDeTreintaYSieteACuadroPorTreintaSegundos()
        {
            ReglaCanonE16.Parametros p = ReglaCanonE16.Parametros.Diseno;
            Assert.AreEqual(10, ReglaCanonE16.Parametros.CuadroDisparo);
            Assert.AreEqual(37, ReglaCanonE16.Parametros.CuadrosClip);
            Assert.AreEqual(10f / 30f, p.SegundosAlImpacto, 1e-5f);
            Assert.AreEqual(37f / 30f, p.SegundosClip, 1e-5f);
            Assert.Less(p.SegundosAlImpacto, p.SegundosClip, "el brazo se extiende antes de que termine el clip");
        }

        [Test]
        public void Inactivo_NoEmpiezaAntesDelPrimerTiro_YLuegoSi()
        {
            ReglaCanonE16 r = Crear();
            float t = HastaAviso(r, 10f);
            Assert.AreEqual(r.Config.SegundosPrimerTiro, t, 0.05f, "espera SegundosPrimerTiro peleando");
            Assert.AreEqual(ReglaCanonE16.Fase.Aviso, r.Actual);
            Assert.IsTrue(r.Bloqueando);
        }

        [Test]
        public void SinPelea_ElEnfriamientoNoCorre_NiEmpieza()
        {
            ReglaCanonE16 r = Crear();
            for (float t = 0f; t < 10f; t += Dt)
            {
                Assert.IsFalse(r.Avanzar(Dt, En(10f, enPelea: false)).EmpezoAviso);
            }
            Assert.AreEqual(r.Config.SegundosPrimerTiro, r.SegundosDeEnfriamiento, 1e-4f, "sin pelea el enfriamiento no baja");
        }

        [TestCase(2f)]
        [TestCase(3.9f)]
        [TestCase(25.5f)]
        [TestCase(80f)]
        public void FueraDeRango_NoDispara(float distancia)
        {
            ReglaCanonE16 r = Crear();
            Assert.AreEqual(-1f, HastaAviso(r, distancia, 12f), "a " + distancia + " m no avisa");
            Assert.IsFalse(r.EnRango(distancia));
        }

        [Test]
        public void SiLaConductaNoPermite_NoEmpieza_AunqueEsteEnRango()
        {
            ReglaCanonE16 r = Crear();
            for (float t = 0f; t < 12f; t += Dt)
            {
                Assert.IsFalse(r.Avanzar(Dt, En(10f, puede: false)).EmpezoAviso, "aturdido, golpeando o recuperándose: no dispara");
            }
        }

        [Test]
        public void Ciclo_AvisoLuegoClipLuegoDisparoEnElCuadroDiez_YTerminaALos37Cuadros()
        {
            ReglaCanonE16 r = Crear();
            HastaAviso(r, 10f);
            float enAviso = 0f;
            bool empezoClip = false;
            while (r.Actual == ReglaCanonE16.Fase.Aviso && enAviso < 5f)
            {
                ReglaCanonE16.Salida s = r.Avanzar(Dt, En(10f));
                enAviso += Dt;
                empezoClip |= s.EmpezoClip;
                Assert.IsFalse(s.Disparo, "no dispara durante el aviso");
            }
            Assert.IsTrue(empezoClip, "al terminar el aviso empieza E16_Disparar_Canon");
            Assert.AreEqual(r.Config.SegundosAviso, enAviso, 0.03f);
            Assert.AreEqual(ReglaCanonE16.Fase.Disparo, r.Actual);

            float enClip = 0f;
            float tiroEn = -1f;
            int tiros = 0;
            while (r.Actual == ReglaCanonE16.Fase.Disparo && enClip < 5f)
            {
                ReglaCanonE16.Salida s = r.Avanzar(Dt, En(10f));
                enClip += Dt;
                if (s.Disparo)
                {
                    tiros++;
                    tiroEn = enClip;
                }
            }
            Assert.AreEqual(1, tiros, "un solo tiro por clip");
            Assert.AreEqual(10f / 30f, tiroEn, 0.03f, "sale con el brazo extendido, hacia el cuadro 10");
            Assert.AreEqual(37f / 30f, enClip, 0.03f, "el clip dura sus 37 cuadros");
            Assert.AreEqual(ReglaCanonE16.Fase.Inactivo, r.Actual);
            Assert.AreEqual(1, r.Disparos);
        }

        [Test]
        public void Mientras_AvisaODispara_Bloquea_ParaQueNoCamine()
        {
            ReglaCanonE16 r = Crear();
            HastaAviso(r, 10f);
            int cuadrosBloqueado = 0;
            while (r.Bloqueando && cuadrosBloqueado < 1000)
            {
                Assert.IsTrue(r.Actual == ReglaCanonE16.Fase.Aviso || r.Actual == ReglaCanonE16.Fase.Disparo);
                r.Avanzar(Dt, En(10f));
                cuadrosBloqueado++;
            }
            float total = cuadrosBloqueado * Dt;
            Assert.AreEqual(r.Config.SegundosAviso + r.Config.SegundosClip, total, 0.05f, "bloquea todo el aviso y todo el clip");
            Assert.IsFalse(r.Bloqueando, "después queda libre para caminar");
        }

        [Test]
        public void DespuesDeDisparar_EsperaElEnfriamiento_AntesDeOtroTiro()
        {
            ReglaCanonE16 r = Crear();
            HastaAviso(r, 10f);
            while (r.Bloqueando) { r.Avanzar(Dt, En(10f)); }
            float espera = HastaAviso(r, 10f, 30f);
            Assert.AreEqual(r.Config.SegundosEnfriamiento, espera, 0.1f, "6 s entre tiros");
        }

        [Test]
        public void SiPierdeLaPelea_EnElAviso_Cancela_YNoDispara()
        {
            ReglaCanonE16 r = Crear();
            HastaAviso(r, 10f);
            r.Avanzar(Dt, En(10f, enPelea: false));
            Assert.AreEqual(ReglaCanonE16.Fase.Inactivo, r.Actual);
            Assert.AreEqual(0, r.Disparos);
        }

        [Test]
        public void Cancelar_ADeMedioClip_NoDisparaDespues_YDejaUnEnfriamientoCorto()
        {
            ReglaCanonE16 r = Crear();
            HastaAviso(r, 10f);
            while (r.Actual != ReglaCanonE16.Fase.Disparo) { r.Avanzar(Dt, En(10f)); }
            r.Avanzar(Dt, En(10f)); // apenas empezó el clip
            r.Cancelar();
            Assert.IsFalse(r.Bloqueando);
            for (int i = 0; i < 200; i++)
            {
                Assert.IsFalse(r.Avanzar(Dt, En(10f, puede: false)).Disparo, "cancelado: no sale el tiro");
            }
            Assert.GreaterOrEqual(r.SegundosDeEnfriamiento, 0f);
        }

        [Test]
        public void Reiniciar_VuelveAlPrimerTiro()
        {
            ReglaCanonE16 r = Crear();
            HastaAviso(r, 10f);
            while (r.Bloqueando) { r.Avanzar(Dt, En(10f)); }
            r.Reiniciar();
            Assert.AreEqual(0, r.Disparos);
            Assert.AreEqual(r.Config.SegundosPrimerTiro, r.SegundosDeEnfriamiento, 1e-4f);
            Assert.AreEqual(ReglaCanonE16.Fase.Inactivo, r.Actual);
        }

        [Test]
        public void ProgresoAviso_SubeDeCeroAUnoSoloEnElAviso()
        {
            ReglaCanonE16 r = Crear();
            Assert.AreEqual(0f, r.ProgresoAviso);
            HastaAviso(r, 10f);
            float anterior = -1f;
            while (r.Actual == ReglaCanonE16.Fase.Aviso)
            {
                Assert.GreaterOrEqual(r.ProgresoAviso, anterior);
                anterior = r.ProgresoAviso;
                r.Avanzar(Dt, En(10f));
            }
            Assert.Greater(anterior, 0.9f);
            Assert.AreEqual(0f, r.ProgresoAviso, "fuera del aviso vale 0");
        }
    }
}
