using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1532: los IDs nuevos del 20-nov para armas (lógica pura): el flujo del plasma por estados, el lanzagranadas, el revólver, el clic vacío, los casquillos, los misiles y el golpe a metal.</summary>
    public class SonidosArmas20novPruebas
    {
        private const EstadoPlasma Reposo = EstadoPlasma.Reposo;
        private const EstadoPlasma Cargando = EstadoPlasma.Cargando;
        private const EstadoPlasma Rayo = EstadoPlasma.Rayo;

        [Test]
        public void Plasma_Secuencia_GatilloCargaDisparoRayoEnfriar()
        {
            Assert.AreEqual(EventoSonidoPlasma.Gatillo, ReglaSonidoPlasma.Evaluar(Reposo, Cargando, false, false));
            Assert.AreEqual("ARM_W07_GATILLO", SonidosArmasT1472.PlasmaGatillo);
            Assert.IsTrue(ReglaSonidoPlasma.SuenaCarga(Cargando));
            Assert.AreEqual("ARM_W07_CARGA", SonidosArmasT1472.PlasmaCarga);

            Assert.AreEqual(EventoSonidoPlasma.Ninguno, ReglaSonidoPlasma.Evaluar(Cargando, Cargando, false, false));

            Assert.AreEqual(EventoSonidoPlasma.Disparo, ReglaSonidoPlasma.Evaluar(Cargando, Rayo, true, false));
            Assert.AreEqual("ARM_W07_DISPARO", SonidosArmasT1472.PlasmaDisparo);
            Assert.IsFalse(ReglaSonidoPlasma.SuenaCarga(Rayo));
            Assert.IsTrue(ReglaSonidoPlasma.SuenaRayo(Rayo));
            CollectionAssert.AreEqual(new[] { "ARM_W07_RAYO_LOOP", "ARM_W07_RAYO_CAPA_EXTRA" }, ReglaSonidoPlasma.IdsBucleRayo());

            Assert.AreEqual(EventoSonidoPlasma.Ninguno, ReglaSonidoPlasma.Evaluar(Rayo, Rayo, false, false));

            Assert.AreEqual(EventoSonidoPlasma.Enfriar, ReglaSonidoPlasma.Evaluar(Rayo, Reposo, false, false));
            Assert.AreEqual("ARM_W07_ENFRIAR", SonidosArmasT1472.PlasmaEnfriar);
            Assert.IsFalse(ReglaSonidoPlasma.SuenaRayo(Reposo));
        }

        [Test]
        public void Plasma_SinCelda_SuenaSinEnergiaYNoHayDisparo()
        {
            Assert.AreEqual(EventoSonidoPlasma.SinEnergia, ReglaSonidoPlasma.Evaluar(Cargando, Reposo, false, true));
            Assert.AreEqual("ARM_W07_SIN_ENERGIA", SonidosArmasT1472.PlasmaSinEnergia);
        }

        [Test]
        public void Plasma_CargaInstantanea_GatilloYDisparoEnElMismoCuadro()
        {
            Assert.AreEqual(EventoSonidoPlasma.Gatillo | EventoSonidoPlasma.Disparo, ReglaSonidoPlasma.Evaluar(Reposo, Rayo, true, false));
        }

        [Test]
        public void Plasma_SoltarAntesDeTiempo_NoHayDisparoNiEnfriar()
        {
            Assert.AreEqual(EventoSonidoPlasma.Ninguno, ReglaSonidoPlasma.Evaluar(Cargando, Reposo, false, false));
        }

        [Test]
        public void Plasma_EnReposoSinPulsar_NoSuenaNada()
        {
            Assert.AreEqual(EventoSonidoPlasma.Ninguno, ReglaSonidoPlasma.Evaluar(Reposo, Reposo, false, false));
        }

        [Test]
        public void Plasma_ConLaReglaReal_LaSecuenciaCompletaDaLosEventosEnOrden()
        {
            ReglaRayoPlasma regla = new ReglaRayoPlasma(0.5f, 0.4f);
            List<EventoSonidoPlasma> vistos = new List<EventoSonidoPlasma>();
            for (int cuadro = 0; cuadro < 40; cuadro++)
            {
                EstadoPlasma antes = regla.Estado;
                bool empezo = regla.Avanzar(0.05f, cuadro < 15, () => true);
                EventoSonidoPlasma e = ReglaSonidoPlasma.Evaluar(antes, regla.Estado, empezo, false);
                if (e != EventoSonidoPlasma.Ninguno)
                {
                    vistos.Add(e);
                }
            }
            CollectionAssert.AreEqual(new[] { EventoSonidoPlasma.Gatillo, EventoSonidoPlasma.Disparo, EventoSonidoPlasma.Enfriar }, vistos);
        }

        [Test]
        public void Plasma_ConLaReglaReal_SinCeldaSuenaSinEnergia()
        {
            ReglaRayoPlasma regla = new ReglaRayoPlasma(0.2f, 0.4f);
            bool sono = false;
            for (int cuadro = 0; cuadro < 10; cuadro++)
            {
                EstadoPlasma antes = regla.Estado;
                bool falto = false;
                bool empezo = regla.Avanzar(0.05f, true, () => { falto = true; return false; });
                EventoSonidoPlasma e = ReglaSonidoPlasma.Evaluar(antes, regla.Estado, empezo, falto);
                Assert.IsFalse((e & EventoSonidoPlasma.Disparo) != 0, "Sin celda no hay disparo");
                sono |= (e & EventoSonidoPlasma.SinEnergia) != 0;
            }
            Assert.IsTrue(sono);
        }

        [Test]
        public void Lanzagranadas_PideSuDisparoPropio()
        {
            Assert.AreEqual("ARM_W08_DISPARO", SonidosArmasT1472.IdDisparo(ArmaAPie.Lanzagranadas));
            Assert.AreEqual("ARM_W08_DISPARO", PistolaAPie.IdDisparo(ArmaAPie.Lanzagranadas));
        }

        [Test]
        public void Revolver_RecargaYMartilloSonLaMecanica_SacarEsElSuyo()
        {
            Assert.AreEqual("ARM_W06_MECANICA", SonidosArmasT1472.IdRecarga(ArmaAPie.Revolver));
            Assert.AreEqual("ARM_W06_MECANICA", SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Revolver));
            Assert.AreEqual("ARM_W06_SACAR", SonidosArmasT1472.IdSacar(ArmaAPie.Revolver));
        }

        [Test]
        public void ClicVacio_EsArmSinBalas()
        {
            Assert.AreEqual("ARM_SIN_BALAS", SonidosArmasT1472.SinBalas);
        }

        [TestCase(ArmaAPie.Pistola, "CAS_CHICO")]
        [TestCase(ArmaAPie.Subfusil, "CAS_CHICO")]
        [TestCase(ArmaAPie.Fusil, "CAS_CHICO")]
        [TestCase(ArmaAPie.Escopeta, "CAS_GRANDE")]
        [TestCase(ArmaAPie.Sniper, "CAS_GRANDE")]
        [TestCase(ArmaAPie.Revolver, null)]
        [TestCase(ArmaAPie.RiflePlasma, null)]
        [TestCase(ArmaAPie.Punos, null)]
        public void Casquillo_PorArma(ArmaAPie arma, string esperado)
        {
            Assert.AreEqual(esperado, SonidosArmasT1472.IdCasquillo(arma));
        }

        [Test]
        public void Misiles_LanzarEsElIdNuevo()
        {
            Assert.AreEqual("IMP_MISILES_LANZAR", SonidosArmasT1472.MisilesLanzar);
        }

        [TestCase("MechaRival", "MEL_GOLPE_METAL")]
        [TestCase("DronPatrulla", "MEL_GOLPE_METAL")]
        [TestCase("GuardiaNova_01", "MEL_GOLPE_METAL")]
        [TestCase("Pandillero", "MEL_GOLPE_CARNE")]
        [TestCase("", "MEL_GOLPE_CARNE")]
        [TestCase(null, "MEL_GOLPE_CARNE")]
        public void GolpeConectado_MetalORobotVsCarne(string nombres, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSonidosJugador.IdGolpeConectado(nombres));
        }
    }
}
