using System.Collections.Generic;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Sonido;
using MechaCyberpunk.Vehiculos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.SonidosVehiculosPasos
{
    /// <summary>
    /// T-1534: pisadas del jugador (tabla de la BITACORA de Blender, detector por tiempo normalizado, superficie del piso) y sonidos de vehículos
    /// (eléctrico sin ralentí, grava, puente, raspón, lluvia dentro del carro).
    /// </summary>
    public class SonidosVehiculosPasos20novPruebas
    {
        private sealed class SonidosDoble : ISonidos
        {
            public readonly List<string> Sonaron = new List<string>();
            public readonly List<string> Bucles = new List<string>();
            public readonly List<int> Detenidos = new List<int>();
            public readonly List<float> Tonos = new List<float>();
            public void Reproducir(string id) => Sonaron.Add(id);
            public void ReproducirEn(string id, Vector3 posicion) => Sonaron.Add(id);
            public int ReproducirBucle(string id, Transform sigue) { Bucles.Add(id); return Bucles.Count; }
            public void DetenerBucle(int bucle) => Detenidos.Add(bucle);
            public void FijarTono(int bucle, float tono) => Tonos.Add(tono);
            public float Volumen(SeccionSonido seccion) => 1f;
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private SonidosDoble sonidos;
        private GameObject carro;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            sonidos = new SonidosDoble();
            Registro<ISonidos>.Agregar(sonidos);
            carro = new GameObject("CarroPrueba");
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(carro);
            ReglaLluviaDentroCarro.FuenteLluvia = null;
            Registros.LimpiarTodos();
        }

        // ---------------------------------------------------------------- Pisadas: tabla de la BITACORA

        // Clip, cuadros, pie izquierdo, pie derecho (BITACORA de Blender, pedido 19-nov noche: "Pisadas = fotograma en que el talon/planta toca el piso").
        [TestCase("PJ01_Caminar", 31, 1, 16)]
        [TestCase("PJ01_Trote", 21, 1, 11)]
        [TestCase("PJ01_Sprint", 19, 1, 10)]
        [TestCase("PJ01_Trote_Izquierda", 21, 1, 11)]
        [TestCase("PJ01_Trote_Derecha", 21, 1, 11)]
        [TestCase("PJ01_Trote_Atras", 21, 1, 11)]
        [TestCase("PJ01_CaminarApuntando_Adelante", 27, 1, 14)]
        [TestCase("PJ01_CaminarApuntando_Atras", 27, 1, 14)]
        [TestCase("PJ01_CaminarApuntando_Izquierda", 23, 1, 12)]
        [TestCase("PJ01_CaminarApuntando_Derecha", 23, 1, 12)]
        [TestCase("PJ01_Escaleras_Subir", 37, 1, 19)]
        [TestCase("PJ01_Escaleras_Bajar", 35, 1, 18)]
        public void Tabla_CadaClip_UnaPisadaPorPieEnElFotogramaDeLaBitacora(string clip, int cuadros, int izquierdo, int derecho)
        {
            Assert.IsTrue(TablaPisadasPJ01.Buscar(clip, out TablaPisadasPJ01.Pisadas p), clip);
            Assert.AreEqual(cuadros, p.Cuadros);
            Assert.AreEqual(izquierdo, p.Izquierdo);
            Assert.AreEqual(derecho, p.Derecho);
            Assert.AreEqual(0f, p.FaseIzquierdo, 1e-5f, "El pie izquierdo cae en el cuadro 1 (y en el último: bucle)");
            Assert.AreEqual((derecho - 1f) / (cuadros - 1f), p.FaseDerecho, 1e-5f);
            Assert.Greater(p.FaseDerecho, 0.4f);
            Assert.Less(p.FaseDerecho, 0.6f, "El pie derecho cae a mitad del ciclo");
        }

        [Test]
        public void Tabla_ClipsSinPisadas_NoEstan()
        {
            Assert.IsFalse(TablaPisadasPJ01.Buscar("PJ01_Salto_Aire", out _));
            Assert.IsFalse(TablaPisadasPJ01.Buscar("PJ01_Idle", out _));
            Assert.IsFalse(TablaPisadasPJ01.Buscar(null, out _));
            Assert.IsTrue(TablaPisadasPJ01.Buscar("Caminar", out _), "También sin el prefijo PJ01_");
        }

        [Test]
        public void Tabla_ClipsConEventosDelImportador_NoSeDuplicanPorTiempo()
        {
            foreach (TablaPisadasPJ01.Pisadas p in TablaPisadasPJ01.Todas)
            {
                AnimationEvent[] eventos = ImportadorClipsPJ01.EventosPisadas(p.Clip);
                if (p.ConEventos)
                {
                    Assert.AreEqual(2, eventos.Length, p.Clip + ": el importador ya le pone un evento por pie");
                    Assert.AreEqual(Mathf.Max(ImportadorClipsPJ01.FraccionMinimaEvento, p.FaseIzquierdo), eventos[0].time, 1e-4f, p.Clip);
                    Assert.AreEqual(p.FaseDerecho, eventos[1].time, 1e-4f, p.Clip);
                }
                else
                {
                    Assert.IsEmpty(eventos, p.Clip + ": sin eventos en el clip, suena por tiempo normalizado");
                }
            }
        }

        [Test]
        public void Tabla_LosSeisDeLocomocion_SonLosQueTrae_ClipsConPisadasDelImportador()
        {
            int conEventos = 0;
            foreach (TablaPisadasPJ01.Pisadas p in TablaPisadasPJ01.Todas)
            {
                if (p.ConEventos) conEventos++;
            }
            Assert.AreEqual(ImportadorClipsPJ01.ClipsConPisadas.Length, conEventos);
            foreach ((string clip, int cuadros, int izq, int der) in ImportadorClipsPJ01.ClipsConPisadas)
            {
                Assert.IsTrue(TablaPisadasPJ01.Buscar(clip, out TablaPisadasPJ01.Pisadas p), clip);
                Assert.AreEqual(cuadros, p.Cuadros, clip);
                Assert.AreEqual(izq, p.Izquierdo, clip);
                Assert.AreEqual(der, p.Derecho, clip);
            }
        }

        // ---------------------------------------------------------------- Pisadas: detector por tiempo

        [TestCase("PJ01_Caminar")]
        [TestCase("PJ01_CaminarApuntando_Adelante")]
        [TestCase("PJ01_Escaleras_Subir")]
        [TestCase("PJ01_Escaleras_Bajar")]
        public void Detector_TresVueltas_UnEventoPorPieYPorVuelta_EnSuFotograma(string clip)
        {
            TablaPisadasPJ01.Buscar(clip, out TablaPisadasPJ01.Pisadas p);
            var detector = new DetectorPisadasPorTiempo();
            const int pasosPorVuelta = 3000;
            int izquierdos = 0, derechos = 0, ambos = 0;
            float primerDerecho = -1f;
            for (int i = 0; i <= pasosPorVuelta * 3; i++)
            {
                float t = i / (float)pasosPorVuelta;
                int pies = detector.Avanzar(t, p.FaseIzquierdo, p.FaseDerecho);
                if (pies == 3) ambos++;
                if ((pies & 1) != 0) izquierdos++;
                if ((pies & 2) != 0)
                {
                    derechos++;
                    if (primerDerecho < 0f) primerDerecho = t;
                }
            }
            Assert.AreEqual(0, ambos, "Nunca los dos pies en el mismo paso");
            Assert.AreEqual(3, izquierdos, "Un izquierdo por vuelta (en los cuadros 1 = último)");
            Assert.AreEqual(3, derechos, "Un derecho por vuelta");
            Assert.AreEqual(p.FaseDerecho, primerDerecho, 1.5f / pasosPorVuelta, "El derecho suena en su fotograma de la bitácora");
        }

        [Test]
        public void Detector_PrimeraLectura_SoloFijaElPuntoDePartida()
        {
            var detector = new DetectorPisadasPorTiempo();
            Assert.AreEqual(0, detector.Avanzar(0.7f, 0f, 0.5f));
            Assert.AreEqual(1, detector.Avanzar(1.01f, 0f, 0.5f), "Cruza el cuadro 1 de la siguiente vuelta: pie izquierdo");
        }

        [Test]
        public void Detector_SaltoDeUnaVueltaOHaciaAtras_NoDisparaNada_YReiniciarOlvida()
        {
            var detector = new DetectorPisadasPorTiempo();
            detector.Avanzar(0.1f, 0f, 0.5f);
            Assert.AreEqual(0, detector.Avanzar(1.5f, 0f, 0.5f), "Un cuadro muy largo (más de una vuelta) no ametralla pasos");
            Assert.AreEqual(0, detector.Avanzar(0.2f, 0f, 0.5f), "Hacia atrás: el estado se reinició");
            detector.Reiniciar();
            Assert.AreEqual(0, detector.Avanzar(0.9f, 0f, 0.5f));
            Assert.AreEqual(1, detector.Avanzar(1.01f, 0f, 0.5f), "Sigue después de reiniciar");
        }

        // ---------------------------------------------------------------- Pisadas: superficie

        [TestCase("Arenal_Playa", "PAS_TIERRA")]
        [TestCase("Duna", "PAS_TIERRA")]
        [TestCase("Pasto_Parque", "PAS_TIERRA")]
        [TestCase("Cesped", "PAS_TIERRA")]
        [TestCase("Loseta_Plaza", "PAS_CONCRETO")]
        [TestCase("Azulejo_Banio", "PAS_CONCRETO")]
        [TestCase("Madera_Vieja", "PAS_MADERA")]
        [TestCase("Grava_Obra", "PAS_GRAVA")]
        [TestCase("Charco_01", "PAS_MOJADO")]
        [TestCase("Rejilla", "PAS_METAL")]
        [TestCase("Banqueta", "PAS_CONCRETO")]
        public void Superficie_PorNombreDelPiso_DaElIdDePasoMasCercano(string piso, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSuperficiePaso.IdPaso(IdLugar.Calle, piso, false));
        }

        [Test]
        public void Superficie_ArenaPastoYLoseta_SuenanConElMasCercano_HastaQueHayaLosPropios()
        {
            Assert.AreEqual(ReglaSuperficiePaso.Tierra, ReglaSuperficiePaso.Arena);
            Assert.AreEqual(ReglaSuperficiePaso.Tierra, ReglaSuperficiePaso.Pasto);
            Assert.AreEqual(ReglaSuperficiePaso.Concreto, ReglaSuperficiePaso.Loseta);
        }

        [Test]
        public void Superficie_LaArenaDeLaJaula_NoEsArena_YLaLosetaDentroDeUnLocalGanaALaMadera()
        {
            Assert.AreEqual("PAS_CONCRETO", ReglaSuperficiePaso.IdPaso(IdLugar.Calle, "|M|Piso|Arena_Jaula", false), "Arena a secas es la jaula de pelea, no arena");
            Assert.AreEqual("PAS_MADERA", ReglaSuperficiePaso.IdPaso(IdLugar.Clandestino, "|M|Piso|Arena_Jaula", false));
            Assert.AreEqual(ReglaSuperficiePaso.Loseta, ReglaSuperficiePaso.IdPaso(IdLugar.Casino, "|Loseta|Piso|Casino", false));
            Assert.AreEqual("PAS_MADERA", ReglaSuperficiePaso.IdPaso(IdLugar.Casino, "|M|Piso|Casino", false));
        }

        [Test]
        public void Superficie_TodosLosIdsDePaso_TienenPoliticaDeVariacionDeTono()
        {
            foreach (string id in new[] { ReglaSuperficiePaso.Concreto, ReglaSuperficiePaso.Metal, ReglaSuperficiePaso.Madera, ReglaSuperficiePaso.Tierra, ReglaSuperficiePaso.Grava, ReglaSuperficiePaso.Mojado })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.AreEqual("paso", p.Grupo, id);
                Assert.Greater(p.VariacionTono, 0f, id + ": un paso nunca suena idéntico");
            }
        }

        // ---------------------------------------------------------------- Vehículos: eléctrico

        [Test]
        public void Grupo_Electrico_SinRalentiNiApagar_ConMoverAcelerarYPasar()
        {
            GrupoSonidoVehiculo g = GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Electrico);
            Assert.IsTrue(g.EsElectrico);
            Assert.AreEqual("VEH_ELECTRICO_ARRANQUE", g.Arranque);
            Assert.IsNull(g.Ralenti, "Sin ralentí: parado va en silencio");
            Assert.AreEqual("VEH_ELECTRICO_MOVER", g.Mover);
            Assert.AreEqual("VEH_ELECTRICO_ACELERAR", g.Acelerar);
            Assert.AreEqual("VEH_ELECTRICO_PASAR", g.Pasar);
            Assert.IsFalse(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Normal).EsElectrico);
            Assert.IsFalse(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Superdeportivo).EsElectrico);
            Assert.AreEqual("VEH_NORMAL_RALENTI", GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Superdeportivo).Ralenti, "Los deportivos usan el loop del carro normal");
            Assert.AreEqual(TipoVehiculoSonido.Electrico, GrupoSonidoVehiculo.Detectar(false, 1200f, "Superdeportivo eléctrico"));
        }

        [Test]
        public void Electrico_Encendido_ArranqueUnaVez_YParadoEnSilencio()
        {
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Electrico), 40f);
            motor.Encender(carro.transform, true);
            for (int i = 0; i < 300; i++)
            {
                motor.Actualizar(0f, 0f, 0.05f);
            }
            CollectionAssert.AreEqual(new[] { "VEH_ELECTRICO_ARRANQUE" }, sonidos.Sonaron);
            Assert.AreEqual(0, sonidos.Bucles.Count, "Sin ralentí: 15 s parado y no suena ningún bucle");
            Assert.AreEqual(Sonidos.SinBucle, motor.Bucle);
        }

        [Test]
        public void Electrico_PisarDesdeParado_AceleraUnaVez_Y_AlMoverseSuenaMoverSubiendoElTono()
        {
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Electrico), 40f);
            motor.Encender(carro.transform, false);
            motor.Actualizar(0f, 0f, 1f);
            motor.Actualizar(0f, 1f, 0.02f); // pisa a fondo
            motor.Actualizar(0.1f, 1f, 0.02f);
            CollectionAssert.AreEqual(new[] { "VEH_ELECTRICO_ACELERAR" }, sonidos.Sonaron);
            Assert.AreEqual(0, sonidos.Bucles.Count, "Casi parado todavía: silencio");
            motor.Actualizar(3f, 1f, 0.02f);
            CollectionAssert.AreEqual(new[] { "VEH_ELECTRICO_MOVER" }, sonidos.Bucles);
            float tonoBajo = 0f;
            for (int i = 0; i < 50; i++)
            {
                motor.Actualizar(4f, 1f, 0.02f);
            }
            tonoBajo = motor.TonoActual;
            for (int i = 0; i < 150; i++)
            {
                motor.Actualizar(25f, 1f, 0.02f);
            }
            Assert.Greater(motor.TonoActual, tonoBajo, "El tono sube con la rapidez");
            Assert.AreEqual(1, sonidos.Bucles.Count, "Un solo bucle MOVER");
            Assert.AreEqual(1, sonidos.Sonaron.FindAll(s => s == "VEH_ELECTRICO_ACELERAR").Count, "Pisar una vez, un solo acelerador");
        }

        [Test]
        public void Electrico_AlFrenarHastaParar_ElBucleSeDetiene_YAlApagarNoHayApagar()
        {
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Electrico), 40f);
            motor.Encender(carro.transform, false);
            motor.Actualizar(0f, 0f, 1f);
            motor.Actualizar(5f, 0.2f, 0.02f);
            Assert.AreEqual(1, sonidos.Bucles.Count);
            motor.Actualizar(0.2f, 0f, 0.02f);
            Assert.AreEqual(1, sonidos.Detenidos.Count, "Parado: el bucle se corta, silencio");
            int sonaron = sonidos.Sonaron.Count;
            motor.Apagar(true);
            Assert.AreEqual(sonaron, sonidos.Sonaron.Count, "El eléctrico no tiene sonido de apagado");
            Assert.IsFalse(motor.Encendido);
        }

        [Test]
        public void ReglaElectrico_PasarSuenaUnaVezAlLlegarARapido_YSeRearmaAlBajar()
        {
            var regla = new ReglaMotorElectrico();
            int pasar = 0;
            for (int i = 0; i < 200; i++)
            {
                if (regla.Actualizar(30f, 40f, 1f, 0.1f).Pasar) pasar++;
            }
            Assert.AreEqual(1, pasar, "Una vez, no cada medio segundo");
            for (int i = 0; i < 20; i++)
            {
                regla.Actualizar(2f, 40f, 0f, 0.1f);
            }
            Assert.IsTrue(regla.Actualizar(30f, 40f, 0f, 0.1f).Pasar, "Después de frenar y volver a acelerar vuelve a pasar");
        }

        [Test]
        public void ReglaElectrico_HisteresisDelBucle_NoPrendeYApagaEnElUmbral()
        {
            var regla = new ReglaMotorElectrico();
            Assert.IsTrue(regla.Actualizar(1f, 40f, 0f, 0.02f).IniciarMover);
            Assert.IsFalse(regla.Actualizar(0.6f, 40f, 0f, 0.02f).DetenerMover, "Entre 0.4 y 0.8 m/s sigue sonando");
            Assert.IsTrue(regla.Moviendo);
            Assert.IsTrue(regla.Actualizar(0.3f, 40f, 0f, 0.02f).DetenerMover);
            Assert.IsFalse(regla.Moviendo);
        }

        [Test]
        public void IdPasaCerca_ElElectricoPasaConSuTurbina()
        {
            Assert.AreEqual("VEH_ELECTRICO_PASAR", GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Electrico).Pasar);
            Assert.IsNull(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Normal).Pasar, "Los demás siguen con VEH_PASA_CERCA");
        }

        [Test]
        public void Mezcla_LosIdsDeVehiculoNuevos_TienenPoliticaPropia_NoLaGenerica()
        {
            foreach (string id in new[] { "VEH_ELECTRICO_MOVER", "VEH_ELECTRICO_ACELERAR", "VEH_ELECTRICO_PASAR", "VEH_LLANTAS_GRAVA", "VEH_PUENTE", "VEH_ELECTRICO_ARRANQUE" })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.IsFalse(string.IsNullOrEmpty(p.Grupo), id);
                Assert.AreNotEqual("nuevo", p.Grupo, id + ": política genérica");
                Assert.Greater(p.MaxVoces, 0, id);
                Assert.Greater(p.Volumen, 0f, id);
            }
        }

        // ---------------------------------------------------------------- Vehículos: suelo, raspón y lluvia

        [TestCase("|Grava_Obra|Piso|Calle", true)]
        [TestCase("Tierra_Lote", true)]
        [TestCase("Asfalto", false)]
        [TestCase(null, false)]
        public void Suelo_Grava_PorNombreDelPiso(string piso, bool esperado)
        {
            Assert.AreEqual(esperado, ReglaSueloCarro.EsGrava(piso));
        }

        [Test]
        public void Suelo_BucleDeGrava_SoloRodandoSobreGrava_ConHisteresis()
        {
            Assert.IsTrue(ReglaSueloCarro.SuenaGrava(true, 5f, false));
            Assert.IsFalse(ReglaSueloCarro.SuenaGrava(true, 1.5f, false), "Muy lento para empezar");
            Assert.IsTrue(ReglaSueloCarro.SuenaGrava(true, 1.5f, true), "Ya sonaba: sigue hasta bajar de 1 m/s");
            Assert.IsFalse(ReglaSueloCarro.SuenaGrava(false, 20f, true), "Sobre asfalto no suena");
            Assert.IsFalse(ReglaSueloCarro.SuenaGrava(true, 0.5f, true));
        }

        [Test]
        public void Suelo_Puente_SuenaAlEntrarARapido_ConTiempoMinimo()
        {
            Assert.IsTrue(ReglaSueloCarro.EsPuente("Junta_Puente_Norte"));
            Assert.IsFalse(ReglaSueloCarro.EsPuente("Asfalto"));
            Assert.IsTrue(ReglaSueloCarro.SuenaPuente(false, true, 10f, 5f));
            Assert.IsFalse(ReglaSueloCarro.SuenaPuente(true, true, 10f, 5f), "Ya estaba en el puente");
            Assert.IsFalse(ReglaSueloCarro.SuenaPuente(false, true, 2f, 5f), "Muy lento");
            Assert.IsFalse(ReglaSueloCarro.SuenaPuente(false, true, 10f, 0.3f), "Muy pronto");
        }

        [Test]
        public void Suelo_Raspon_RoceDeCostado_ConVelocidadAlta_PeroNoElGolpeDeFrente()
        {
            Assert.IsTrue(ReglaSueloCarro.EsRaspon(12f, 0.95f, 1f, 5f), "Pared al costado, casi sin meterse");
            Assert.IsFalse(ReglaSueloCarro.EsRaspon(12f, 0.1f, 9f, 5f), "Choque de frente: lo cubre VEH_CHOQUE normal");
            Assert.IsFalse(ReglaSueloCarro.EsRaspon(12f, 0.95f, 6f, 5f), "De costado pero metiéndose fuerte: es un choque");
            Assert.IsFalse(ReglaSueloCarro.EsRaspon(2f, 0.95f, 1f, 5f), "Muy lento");
            Assert.IsFalse(ReglaSueloCarro.EsRaspon(12f, 0.95f, 1f, 0.2f), "Muy pronto");
            Assert.AreEqual("VEH_CHOQUE", ReglaSueloCarro.IdRaspon, "El contacto usa los VEH_CHOQUE_* que ya están");
        }

        [Test]
        public void Lluvia_DentroDelCarro_SoloLloviendoYConElJugadorEnUnCarro()
        {
            Assert.IsTrue(ReglaLluviaDentroCarro.Suena(true, false, true));
            Assert.IsFalse(ReglaLluviaDentroCarro.Suena(true, true, true), "En moto se moja: suena la lluvia de afuera");
            Assert.IsFalse(ReglaLluviaDentroCarro.Suena(false, false, true));
            Assert.IsFalse(ReglaLluviaDentroCarro.Suena(true, false, false));
            Assert.AreEqual("CLI_LLUVIA_DENTRO_CARRO", ReglaLluviaDentroCarro.Id);
        }

        [Test]
        public void Lluvia_SinFuenteDelClima_NoLlueve_ConFuente_SigueAlClima()
        {
            Assert.IsFalse(ReglaLluviaDentroCarro.Llueve);
            bool llueve = true;
            ReglaLluviaDentroCarro.FuenteLluvia = () => llueve;
            Assert.IsTrue(ReglaLluviaDentroCarro.Llueve);
            llueve = false;
            Assert.IsFalse(ReglaLluviaDentroCarro.Llueve);
        }

        [Test]
        public void Mezcla_LluviaDentroDelCarro_TienePolitica()
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para(ReglaLluviaDentroCarro.Id);
            Assert.IsFalse(string.IsNullOrEmpty(p.Grupo));
            Assert.AreNotEqual("nuevo", p.Grupo);
            Assert.AreEqual(1, ReglaMezclaSonidos.MaxBuclesDelId(ReglaLluviaDentroCarro.Id));
        }
    }
}
