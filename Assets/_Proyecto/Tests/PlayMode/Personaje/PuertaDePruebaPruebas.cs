using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Reglas del doble de puerta y de lugar: siguen el contrato de IPuerta.IntentarAbrir.</summary>
    public class PuertaDePruebaPruebas
    {
        private GameObject local;
        private LugarDePrueba lugar;
        private PuertaDePrueba puerta;
        private int avisosCambio;
        private bool ultimoAviso;

        [SetUp]
        public void SetUp()
        {
            LocalesDePrueba.LimpiarGlobales();
            avisosCambio = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (local != null) Object.DestroyImmediate(local);
            LocalesDePrueba.LimpiarGlobales();
        }

        private void Crear(bool cerradoEnDemo, bool abiertoPorHorario)
        {
            local = LocalesDePrueba.Crear(IdLugar.Bar, Vector3.zero, new HorarioLugar(20f, 6f), cerradoEnDemo, abiertoPorHorario);
            lugar = local.GetComponent<LugarDePrueba>();
            puerta = local.GetComponentInChildren<PuertaDePrueba>();
            puerta.Cambio += abierta => { avisosCambio++; ultimoAviso = abierta; };
        }

        [Test]
        public void IntentarAbrir_LugarAbierto_DaAbiertaYLaPuertaQuedaAbierta()
        {
            Crear(false, true);
            Assert.AreEqual(ResultadoPuerta.Abierta, puerta.IntentarAbrir());
            Assert.IsTrue(puerta.EstaAbierta);
        }

        [Test]
        public void IntentarAbrir_YaAbierta_DaYaAbiertaSinAvisar()
        {
            Crear(false, true);
            puerta.IntentarAbrir();
            avisosCambio = 0;
            Assert.AreEqual(ResultadoPuerta.YaAbierta, puerta.IntentarAbrir());
            Assert.AreEqual(0, avisosCambio);
        }

        [Test]
        public void IntentarAbrir_YaAbiertaConLugarCerrado_DaYaAbiertaAntesQueNada()
        {
            Crear(true, false);
            lugar.PonerJugadorDentro(true);
            puerta.IntentarAbrir();
            Assert.AreEqual(ResultadoPuerta.YaAbierta, puerta.IntentarAbrir());
        }

        [Test]
        public void IntentarAbrir_JugadorDentroConLugarCerrado_AbreSiempre()
        {
            Crear(false, false);
            lugar.PonerJugadorDentro(true);
            Assert.AreEqual(ResultadoPuerta.Abierta, puerta.IntentarAbrir());
            Assert.IsTrue(puerta.EstaAbierta);
        }

        [Test]
        public void IntentarAbrir_JugadorDentroYCerradoEnDemo_AbreSiempre()
        {
            Crear(true, true);
            lugar.PonerJugadorDentro(true);
            Assert.AreEqual(ResultadoPuerta.Abierta, puerta.IntentarAbrir());
        }

        [Test]
        public void IntentarAbrir_CerradoEnDemo_DaBloqueadaYNoAbre()
        {
            Crear(true, true);
            Assert.AreEqual(ResultadoPuerta.Bloqueada, puerta.IntentarAbrir());
            Assert.IsFalse(puerta.EstaAbierta);
            Assert.AreEqual(0, avisosCambio);
        }

        [Test]
        public void IntentarAbrir_CerradoEnDemoYFueraDeHorario_DaBloqueada()
        {
            Crear(true, false);
            Assert.AreEqual(ResultadoPuerta.Bloqueada, puerta.IntentarAbrir());
        }

        [Test]
        public void IntentarAbrir_FueraDeHorario_DaCerradaPorHorarioYNoAbre()
        {
            Crear(false, false);
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, puerta.IntentarAbrir());
            Assert.IsFalse(puerta.EstaAbierta);
            Assert.AreEqual(0, avisosCambio);
        }

        [Test]
        public void Cambio_AvisaSoloAlAbrirYAlCerrarDeVerdad()
        {
            Crear(false, true);
            puerta.Cerrar();
            Assert.AreEqual(0, avisosCambio, "Cerrar una puerta cerrada no avisa");

            puerta.IntentarAbrir();
            Assert.AreEqual(1, avisosCambio);
            Assert.IsTrue(ultimoAviso);

            puerta.IntentarAbrir();
            Assert.AreEqual(1, avisosCambio, "YaAbierta no avisa");

            puerta.Cerrar();
            Assert.AreEqual(2, avisosCambio);
            Assert.IsFalse(ultimoAviso);

            puerta.Cerrar();
            Assert.AreEqual(2, avisosCambio);
        }

        [Test]
        public void IntentarAbrir_CuentaCadaLlamada()
        {
            Crear(true, true);
            puerta.IntentarAbrir();
            puerta.IntentarAbrir();
            Assert.AreEqual(2, puerta.IntentosAbrir);
        }

        [Test]
        public void Lugar_SeRegistraAlCrearseYLaPuertaTambien()
        {
            Crear(false, true);
            Assert.AreSame(lugar, Registro<ILugar>.Actual);
            Assert.AreSame(puerta, Registro<IPuerta>.Actual);
            Assert.AreEqual(IdLugar.Bar, puerta.Lugar);
        }

        [Test]
        public void Lugar_AlCerrarPorHorarioConLaPuertaAbierta_LaCierra()
        {
            Crear(false, true);
            puerta.IntentarAbrir();
            lugar.PonerAbiertoPorHorario(false);
            Assert.IsFalse(puerta.EstaAbierta);
        }

        [Test]
        public void Lugar_AlCerrarPorHorarioConElJugadorDentro_NoCierraLaPuerta()
        {
            Crear(false, true);
            lugar.PonerJugadorDentro(true);
            puerta.IntentarAbrir();
            lugar.PonerAbiertoPorHorario(false);
            Assert.IsTrue(puerta.EstaAbierta);
        }

        [Test]
        public void EstadoCambio_AvisaSoloCambiosRealesDeEstaAbierto()
        {
            Crear(false, true);
            int avisosLugar = 0;
            int avisosGlobales = 0;
            lugar.EstadoCambio += _ => avisosLugar++;
            EventosLugares.EstadoLugarCambio += (id, abierto) => { if (id == IdLugar.Bar) avisosGlobales++; };

            lugar.PonerAbiertoPorHorario(true);
            Assert.AreEqual(0, avisosLugar, "Poner el mismo valor no avisa");

            lugar.PonerAbiertoPorHorario(false);
            Assert.AreEqual(1, avisosLugar);
            Assert.AreEqual(1, avisosGlobales);
            Assert.IsFalse(lugar.EstaAbierto);

            lugar.PonerAbiertoPorHorario(false);
            Assert.AreEqual(1, avisosLugar);

            lugar.PonerAbiertoPorHorario(true);
            Assert.AreEqual(2, avisosLugar);
            Assert.IsTrue(lugar.EstaAbierto);
        }

        [Test]
        public void EstaAbierto_CerradoEnDemo_EsFalsoAunqueElHorarioAbra()
        {
            Crear(true, true);
            Assert.IsFalse(lugar.EstaAbierto);
        }
    }
}
