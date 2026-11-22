using System.Linq;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1561 (decisión de Tony, 21-nov: "transiciones con lógica"): las reglas puras de modo pelea, armas, granada, prioridades y capa de brazos
    /// (ReglasTransicionesT1561), y lo que dejó TransicionesT1561.Aplicar en el ControladorBase (la segunda mitad necesita haberlo corrido).
    /// </summary>
    public class TransicionesT1561Pruebas
    {
        // ---------- Pelear -> correr y correr -> pelear ----------

        [TestCase(0f, false, true)]
        [TestCase(1.62f, false, true)]
        [TestCase(2f, false, true)]
        [TestCase(2.5f, false, false)]
        [TestCase(4.5f, false, false)]
        [TestCase(1f, true, false)]
        [TestCase(0f, true, false)]
        public void GuardiaPermitida_SoloParadoOCaminandoDespacioYSinShift(float velocidad, bool shift, bool esperada)
        {
            Assert.AreEqual(esperada, ReglasTransicionesT1561.GuardiaPermitida(velocidad, shift));
        }

        [Test]
        public void DebeSalirDePelea_ConShift_Si_SinShift_No()
        {
            Assert.IsTrue(ReglasTransicionesT1561.DebeSalirDePelea(true));
            Assert.IsFalse(ReglasTransicionesT1561.DebeSalirDePelea(false));
        }

        [Test]
        public void NecesitaSubirGuardia_SoloCorriendoYSinLaGuardiaArriba()
        {
            Assert.IsTrue(ReglasTransicionesT1561.NecesitaSubirGuardia(4.5f, false), "corriendo y con los brazos sueltos");
            Assert.IsFalse(ReglasTransicionesT1561.NecesitaSubirGuardia(4.5f, true), "ya está en guardia");
            Assert.IsFalse(ReglasTransicionesT1561.NecesitaSubirGuardia(0f, false), "parado golpea de una vez");
            Assert.IsFalse(ReglasTransicionesT1561.NecesitaSubirGuardia(1.6f, false), "caminando despacio también");
        }

        [Test]
        public void GuardiaLista_CorriendoEsperaLaSubidaYLuegoGolpea()
        {
            Assert.IsFalse(ReglasTransicionesT1561.GuardiaLista(4.5f, false, 0f));
            Assert.IsFalse(ReglasTransicionesT1561.GuardiaLista(4.5f, false, ReglasTransicionesT1561.SegundosSubirGuardia - 0.01f));
            Assert.IsTrue(ReglasTransicionesT1561.GuardiaLista(4.5f, false, ReglasTransicionesT1561.SegundosSubirGuardia));
            Assert.IsTrue(ReglasTransicionesT1561.GuardiaLista(4.5f, true, 0f), "con la guardia arriba no espera");
            Assert.IsTrue(ReglasTransicionesT1561.GuardiaLista(0f, false, 0f), "parado no espera");
        }

        [Test]
        public void TiemposDeGuardia_SonCortos()
        {
            Assert.That(ReglasTransicionesT1561.SegundosSalidaGuardia, Is.InRange(0.1f, 0.25f));
            Assert.That(ReglasTransicionesT1561.SegundosSubirGuardia, Is.InRange(0.1f, 0.25f));
            Assert.That(ReglasTransicionesT1561.VelocidadMaximaGuardia, Is.GreaterThan(ReglasAnimacion.VelocidadPJ01Caminar), "caminar normal cabe en la guardia");
            Assert.That(ReglasTransicionesT1561.VelocidadMaximaGuardia, Is.LessThan(ReglasAnimacion.VelocidadPJ01Trote), "el trote no");
        }

        // ---------- Armas ----------

        [Test]
        public void AccionAlCorrer_SinArmaMantiene_ConRifleGuarda_ConPistolaCorreConEllaSegunLaDecision()
        {
            Assert.AreEqual(AccionArmaAlCorrer.Mantener, ReglasTransicionesT1561.AccionAlCorrer(TipoArmaVisual.Ninguna));
            AccionArmaAlCorrer rifle = ReglasTransicionesT1561.RifleCorreConArma ? AccionArmaAlCorrer.Mantener : AccionArmaAlCorrer.GuardarYCorrer;
            Assert.AreEqual(rifle, ReglasTransicionesT1561.AccionAlCorrer(TipoArmaVisual.ArmaLarga));
            AccionArmaAlCorrer pistola = ReglasTransicionesT1561.PistolaCorreConArma ? AccionArmaAlCorrer.Mantener : AccionArmaAlCorrer.GuardarYCorrer;
            Assert.AreEqual(pistola, ReglasTransicionesT1561.AccionAlCorrer(TipoArmaVisual.Pistola));
            Assert.IsFalse(ReglasTransicionesT1561.RifleCorreConArma, "decisión por omisión: el rifle se guarda al correr (Tony puede cambiarla)");
        }

        [Test]
        public void PuedeDisparar_ConElArmaEnLaPoseYSinRecargar_Si()
        {
            Assert.IsTrue(ReglasTransicionesT1561.PuedeDisparar(true, false, AccionPersonaje.Locomocion, 0f));
            Assert.IsTrue(ReglasTransicionesT1561.PuedeDisparar(true, false, AccionPersonaje.Apuntar, 0f));
        }

        [Test]
        public void PuedeDisparar_ArmaGuardada_EsperaALaPose_YSaleSolaPasadoElTope()
        {
            Assert.IsFalse(ReglasTransicionesT1561.PuedeDisparar(false, false, AccionPersonaje.Locomocion, 0.2f), "sacando el arma todavía no dispara");
            Assert.IsTrue(ReglasTransicionesT1561.PuedeDisparar(false, false, AccionPersonaje.Locomocion, ReglasTransicionesT1561.SegundosMaximosSacar),
                "red de seguridad: un evento perdido no deja sin disparar");
        }

        [Test]
        public void PuedeDisparar_RecargandoBloquea_AunqueElArmaEsteEnLaPose()
        {
            Assert.IsFalse(ReglasTransicionesT1561.PuedeDisparar(true, true, AccionPersonaje.Locomocion, 0f));
            Assert.IsFalse(ReglasTransicionesT1561.PuedeDisparar(true, false, AccionPersonaje.Recarga, 0f));
            Assert.IsFalse(ReglasTransicionesT1561.PuedeDisparar(false, true, AccionPersonaje.Locomocion, 99f), "ni con el tope: la recarga manda");
        }

        [TestCase(AccionPersonaje.Granada)]
        [TestCase(AccionPersonaje.Interaccion)]
        [TestCase(AccionPersonaje.Carro)]
        [TestCase(AccionPersonaje.Derribo)]
        [TestCase(AccionPersonaje.Muerte)]
        public void PuedeDisparar_AccionesLargasBloquean(AccionPersonaje accion)
        {
            Assert.IsFalse(ReglasTransicionesT1561.PuedeDisparar(true, false, accion, 0f));
            Assert.IsTrue(ReglasTransicionesT1561.BloqueaGolpe(accion));
        }

        [Test]
        public void RecargaSigueAlCorrer_SoloLaPistola()
        {
            Assert.IsTrue(ReglasTransicionesT1561.RecargaSigueAlCorrer(TipoArmaVisual.Pistola), "sigue con PJ01_Pistola_RecargarCaminar");
            Assert.IsFalse(ReglasTransicionesT1561.RecargaSigueAlCorrer(TipoArmaVisual.ArmaLarga), "el arma larga cancela la recarga");
        }

        // ---------- Granada ----------

        [Test]
        public void Granada_SeSueltaEnElCuadroDelLanzamiento_NoAlApretar()
        {
            float dePie = ReglasTransicionesT1561.SegundosHastaSoltarGranada(false);
            float agachado = ReglasTransicionesT1561.SegundosHastaSoltarGranada(true);
            Assert.That(dePie, Is.GreaterThan(0.5f), "no sale al apretar");
            Assert.That(agachado, Is.GreaterThan(0.5f));
            // Cuadro 54 de PJ01_Granada_DePie y 56 de PJ01_Granada_Agachado, a 24 cuadros por segundo y con la rapidez del estado.
            Assert.AreEqual((ReglasTransicionesT1561.CuadroSueltaDePie - 1) / 24f / ReglasTransicionesT1561.RapidezGranada, dePie, 0.0001f);
            Assert.AreEqual((ReglasTransicionesT1561.CuadroSueltaAgachado - 1) / 24f / ReglasTransicionesT1561.RapidezGranada, agachado, 0.0001f);
        }

        [Test]
        public void Granada_SueltaAntesDeQueTermineElGesto_YElClipSegunLaPostura()
        {
            foreach (bool agachado in new[] { false, true })
            {
                Assert.Less(ReglasTransicionesT1561.SegundosHastaSoltarGranada(agachado), ReglasTransicionesT1561.SegundosGestoGranada(agachado));
            }
            Assert.AreEqual("PJ01_Granada_DePie", ReglasTransicionesT1561.ClipGranada(false));
            Assert.AreEqual("PJ01_Granada_Agachado", ReglasTransicionesT1561.ClipGranada(true));
        }

        [Test]
        public void Granada_ElMismoMetodoDeCuadroDaElCuadro28DeLanzar()
        {
            // Control con la bitácora: PJ01_Lanzar suelta ~cuadro 28 y la mano derecha medida en Blender va más rápido en el 28 (3.7 m/s).
            Assert.AreEqual(27 / 24f, (28 - 1) / ReglasTransicionesT1561.FotogramasPorSegundo, 0.0001f);
        }

        // ---------- Prioridades ----------

        [TestCase(AccionPersonaje.Locomocion)]
        [TestCase(AccionPersonaje.Guardia)]
        [TestCase(AccionPersonaje.Apuntar)]
        [TestCase(AccionPersonaje.Golpe)]
        [TestCase(AccionPersonaje.Disparo)]
        public void Interrumpe_LasAccionesLigerasCedenACualquiera(AccionPersonaje actual)
        {
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Recarga, actual));
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Dano, actual));
            Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(actual, actual), "no se interrumpe a sí misma");
        }

        [TestCase(AccionPersonaje.Recarga)]
        [TestCase(AccionPersonaje.Granada)]
        public void Interrumpe_RecargaYGranada_LasCortaElDanoElDerriboYLaMuerte_NoUnGolpeNiUnDisparo(AccionPersonaje actual)
        {
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Dano, actual));
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Derribo, actual));
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Muerte, actual));
            Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Golpe, actual));
            Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Disparo, actual));
        }

        [Test]
        public void Interrumpe_LaMuerteGanaATodo_YNadieCortaLaMuerte()
        {
            foreach (AccionPersonaje a in System.Enum.GetValues(typeof(AccionPersonaje)))
            {
                if (a == AccionPersonaje.Muerte)
                {
                    Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Locomocion, a), "nadie corta la muerte");
                    Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Muerte, a));
                    continue;
                }
                Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Muerte, a), "la muerte corta " + a);
            }
        }

        [Test]
        public void Interrumpe_DerriboGanaATodoMenosLaMuerte_InteraccionYCarroSoloLosCortaElDanoOMas()
        {
            Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Dano, AccionPersonaje.Derribo));
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Muerte, AccionPersonaje.Derribo));
            Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Recarga, AccionPersonaje.Interaccion));
            Assert.IsTrue(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Derribo, AccionPersonaje.Carro));
            Assert.IsFalse(ReglasTransicionesT1561.Interrumpe(AccionPersonaje.Dano, AccionPersonaje.Dano));
        }

        [Test]
        public void DanoInterrumpe_SoloDesdeUnaFraccionDeLaVida()
        {
            Assert.IsTrue(ReglasTransicionesT1561.DanoInterrumpe(10f, 100f));
            Assert.IsFalse(ReglasTransicionesT1561.DanoInterrumpe(2f, 100f), "un rasguño no corta la recarga");
            Assert.IsFalse(ReglasTransicionesT1561.DanoInterrumpe(10f, 0f), "sin vida máxima no hay fracción");
        }

        // ---------- Capa de brazos (armas y puños) ----------

        [Test]
        public void PesoCapaArriba_EnElAireSeApaga_YAlAterrizarVuelve()
        {
            float aire = ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, false, false, false, false, false, false, false, false);
            float suelo = ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, false, false, false, false, false, true, false, false);
            Assert.AreEqual(0f, aire);
            Assert.AreEqual(1f, suelo);
        }

        [Test]
        public void PesoCapaArriba_AlMorirOCaerVaACero_AunqueApunte()
        {
            Assert.AreEqual(0f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(true, false, false, true, false, false, false, true, false, false));
        }

        [Test]
        public void PesoCapaArriba_AgachadoSinApuntarSeApaga_ConGranadaOApuntandoNo()
        {
            Assert.AreEqual(0f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, true, false, false, false, false, true, false, false));
            Assert.AreEqual(1f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, true, true, false, false, false, true, false, false));
            Assert.AreEqual(1f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, true, false, true, false, false, true, false, false), "la granada agachado necesita los brazos");
        }

        [Test]
        public void PesoCapaArriba_ManejandoSoloAlPitar()
        {
            Assert.AreEqual(0f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, false, false, false, false, false, true, true, false));
            Assert.AreEqual(1f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, false, false, false, false, false, true, true, true));
        }

        [Test]
        public void PesoCapaArriba_CubiertoCelularOGestoSeApaga()
        {
            Assert.AreEqual(0f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, true, false, false, false, false, false, true, false, false));
            Assert.AreEqual(0f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, false, false, false, true, false, true, false, false));
            Assert.AreEqual(0f, ReglasTransicionesT1561.PesoObjetivoCapaArriba(false, false, false, false, false, false, true, true, false, false));
        }

        // ---------- Carro ----------

        [Test]
        public void Carro_ElConductorArranca18CuadrosDespues_YHuyeCuandoElClipLlegaAlCuadro145()
        {
            Assert.AreEqual(0.75f, ReglasTransicionesT1561.SegundosRetrasoConductor, 0.0001f);
            Assert.AreEqual(98 / 24f, ReglasTransicionesT1561.SegundosSacarConductor, 0.0001f);
            Assert.AreEqual(0.75f + 144f / 24f, ReglasTransicionesT1561.SegundosHastaQueConductorHuye(), 0.0001f);
        }

        // ---------- Interacciones ----------

        [TestCase(GestoVisual.AbrirPuerta, true)]
        [TestCase(GestoVisual.Boton, true)]
        [TestCase(GestoVisual.Recoger, true)]
        [TestCase(GestoVisual.Beber, true)]
        [TestCase(GestoVisual.Sentarse, true)]
        [TestCase(GestoVisual.CamillaAcostarse, true)]
        [TestCase(GestoVisual.Colgarse, true)]
        [TestCase(GestoVisual.CarroEntrar, true)]
        [TestCase(GestoVisual.MotoSubir, true)]
        [TestCase(GestoVisual.Pitar, false)]
        [TestCase(GestoVisual.Telefono, false)]
        [TestCase(GestoVisual.Empujon, false)]
        [TestCase(GestoVisual.BloquearTambaleo, false)]
        public void InteraccionGuardaArma_SoloLasInteracciones(GestoVisual gesto, bool esperado)
        {
            Assert.AreEqual(esperado, ReglasTransicionesT1561.InteraccionGuardaArma(gesto));
        }

        [TestCase("Beber")]
        [TestCase("CamillaAcostarse")]
        [TestCase("ColgadoSubir")]
        public void Bloqueo_BeberCamillaYSubirDeColgadoBloqueanElMovimientoSoloMientrasDuran(string estado)
        {
            Assert.IsTrue(ReglaBloqueoAnimacion.EstadoBloquea(estado));
            Assert.IsFalse(ReglaBloqueoAnimacion.DebeBloquear(true, ReglaBloqueoAnimacion.SegundosMaximos), "con el tope de seguridad deja de bloquear");
            Assert.IsFalse(ReglaBloqueoAnimacion.DebeBloquear(false, 0f), "al terminar el clip se mueve");
        }

        [Test]
        public void Bloqueo_SentarseYColgarseNoBloqueanPorQueSonPosesEnBucle()
        {
            Assert.IsFalse(ReglaBloqueoAnimacion.EstadoBloquea("Sentarse"));
            Assert.IsFalse(ReglaBloqueoAnimacion.EstadoBloquea("Colgarse"));
        }

        // ---------- NPC y robots ----------

        [Test]
        public void HeridoPideAyuda_ElIdentificadorDecide_YSonLasDosCosas()
        {
            int pide = 0, quieto = 0;
            for (int i = 0; i < 20; i++)
            {
                if (ReglasTransicionesT1561.HeridoPideAyuda(i)) { pide++; } else { quieto++; }
            }
            Assert.AreEqual(10, pide);
            Assert.AreEqual(10, quieto);
            Assert.AreEqual(ReglasTransicionesT1561.HeridoPideAyuda(7), ReglasTransicionesT1561.HeridoPideAyuda(7), "determinista");
        }

        [Test]
        public void MezclaNpc_EsCorta()
        {
            Assert.That(ReglasTransicionesT1561.SegundosMezclaNpc, Is.InRange(0.1f, 0.3f));
        }

        [Test]
        public void Robots_ElE16BloqueaConGolpeFuerte_ElE15NoUsaBloquear()
        {
            Assert.IsTrue(ReglasTransicionesT1561.GolpeFuerteParaRobot(20f, 100f));
            Assert.IsFalse(ReglasTransicionesT1561.GolpeFuerteParaRobot(3f, 100f));
            Assert.IsTrue(ReglasTransicionesT1561.RobotUsaBloquear("E16_RobotPesadoNova_a"));
            Assert.IsFalse(ReglasTransicionesT1561.RobotUsaBloquear("E15_EliteNova"));
            Assert.IsFalse(ReglasTransicionesT1561.RobotUsaBloquear(null));
        }

        [Test]
        public void GestoBloquearTambaleo_EsElNumero19_ElContratoConT1560()
        {
            Assert.AreEqual(19, (int)GestoVisual.BloquearTambaleo);
            Assert.AreEqual(15, (int)GestoVisual.Beber);
            Assert.AreEqual(18, (int)GestoVisual.ColgadoSubir);
            Assert.AreEqual(20, (int)GestoVisual.Colgarse);
            var repetidos = System.Enum.GetValues(typeof(GestoVisual)).Cast<int>().GroupBy(x => x).Where(g => g.Count() > 1);
            Assert.IsEmpty(repetidos, "ningún gesto repite número");
        }

        // ---------- Controlador (necesita TransicionesT1561.Aplicar) ----------

        private static AnimatorController Controlador()
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            Assert.IsNotNull(c, "Falta " + ConstructorControladorBase.RutaControlador);
            return c;
        }

        private static AnimatorStateMachine Capa(AnimatorController c, string nombre)
        {
            AnimatorControllerLayer capa = c.layers.FirstOrDefault(l => l.name == nombre);
            Assert.IsNotNull(capa, "Falta la capa " + nombre);
            return capa.stateMachine;
        }

        private static AnimatorState Estado(AnimatorStateMachine m, string nombre)
        {
            AnimatorState s = m.states.Select(x => x.state).FirstOrDefault(x => x.name == nombre);
            Assert.IsNotNull(s, "Falta el estado " + nombre + " (¿se corrió TransicionesT1561.Aplicar?)");
            return s;
        }

        [Test]
        public void Controlador_TieneLosParametrosNuevos()
        {
            AnimatorController c = Controlador();
            foreach (string p in new[] { ReglasAnimacion.Granada, ReglasAnimacion.Interrumpir, ReglasTransicionesT1561.ParametroSentado, ReglasTransicionesT1561.ParametroColgado })
            {
                Assert.IsTrue(c.parameters.Any(x => x.name == p), "Falta el parámetro " + p);
            }
        }

        [Test]
        public void Controlador_RifleSacarYGuardarTienenSusClips_YElArmaLargaYaNoApareceDeGolpe()
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            Assert.AreEqual("PJ01_Rifle_Sacar", Estado(arriba, "RifleSacar").motion.name);
            Assert.AreEqual("PJ01_Rifle_Guardar", Estado(arriba, "RifleGuardar").motion.name);
            AnimatorState vacio = Estado(arriba, "Vacio");
            Assert.IsFalse(vacio.transitions.Any(t => t.destinationState != null && t.destinationState.name == "LargaIdle"), "de Vacio al arma larga se pasa por Rifle_Sacar");
            Assert.IsTrue(vacio.transitions.Any(t => t.destinationState != null && t.destinationState.name == "RifleSacar"));
            Assert.IsTrue(Estado(arriba, "LargaIdle").transitions.Any(t => t.destinationState.name == "RifleGuardar"), "y se guarda con Rifle_Guardar");
            Assert.IsTrue(Estado(arriba, "LargaApuntar").transitions.Any(t => t.destinationState.name == "RifleGuardar"));
        }

        [Test]
        public void Controlador_GranadaDePieYAgachadoTienenSusClips_YSeEligenPorLaPostura()
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            AnimatorState dePie = Estado(arriba, "GranadaDePie"), agachado = Estado(arriba, "GranadaAgachado");
            Assert.AreEqual("PJ01_Granada_DePie", dePie.motion.name);
            Assert.AreEqual("PJ01_Granada_Agachado", agachado.motion.name);
            Assert.AreEqual(ReglasTransicionesT1561.RapidezGranada, dePie.speed, 0.0001f);
            AnimatorStateTransition aDePie = arriba.anyStateTransitions.First(t => t.destinationState == dePie);
            AnimatorStateTransition aAgachado = arriba.anyStateTransitions.First(t => t.destinationState == agachado);
            Assert.IsTrue(aDePie.conditions.Any(k => k.parameter == ReglasAnimacion.Agachado && k.mode == AnimatorConditionMode.IfNot));
            Assert.IsTrue(aAgachado.conditions.Any(k => k.parameter == ReglasAnimacion.Agachado && k.mode == AnimatorConditionMode.If));
            Assert.LessOrEqual(aDePie.duration, 0.25f);
        }

        [Test]
        public void Controlador_LaGuardiaSaleEnCasi02Segundos_YSinExitTimeSalvoAcciones()
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            AnimatorState guardia = Estado(arriba, "PunosGuardia"), vacio = Estado(arriba, "Vacio");
            AnimatorStateTransition salida = guardia.transitions.First(t => t.destinationState == vacio && t.conditions.Any(k => k.parameter == ReglasAnimacion.EnGuardia));
            Assert.AreEqual(ReglasTransicionesT1561.SegundosSalidaGuardia, salida.duration, 0.0001f);
            Assert.IsFalse(salida.hasExitTime, "sale en cuanto la guardia deja de estar permitida");
            foreach (string golpe in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte" })
            {
                AnimatorState g = Estado(arriba, golpe);
                AnimatorStateTransition aVacio = g.transitions.First(t => t.destinationState == vacio && t.hasExitTime);
                Assert.AreEqual(ReglasTransicionesT1561.SegundosSalidaGuardia, aVacio.duration, 0.0001f, golpe + " baja los brazos en 0.2 s");
            }
        }

        [Test]
        public void Controlador_DanoInterrumpeGolpeRecargaYGranada_ConElDisparadorInterrumpir()
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            foreach (string nombre in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte", "PistolaRecargar", "PistolaRecargarCaminar", "LargaRecargar", "GranadaDePie", "GranadaAgachado" })
            {
                AnimatorState s = Estado(arriba, nombre);
                AnimatorStateTransition t = s.transitions.FirstOrDefault(x => x.conditions.Any(k => k.parameter == ReglasAnimacion.Interrumpir));
                Assert.IsNotNull(t, nombre + " se interrumpe");
                Assert.IsFalse(t.hasExitTime, nombre + ": interrumpir no espera el final del clip");
                Assert.LessOrEqual(t.duration, 0.25f);
            }
        }

        [Test]
        public void Controlador_RecargaDePistolaSigueComoRecargarCaminarSiEchaACorrer()
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            AnimatorState recargar = Estado(arriba, "PistolaRecargar");
            AnimatorStateTransition t = recargar.transitions.FirstOrDefault(x => x.destinationState != null && x.destinationState.name == "PistolaRecargarCaminar");
            Assert.IsNotNull(t);
            Assert.IsTrue(t.conditions.Any(k => k.parameter == ReglasVisual.Velocidad && k.mode == AnimatorConditionMode.Greater));
        }

        [TestCase("Beber", "PJ01_Beber", (int)GestoVisual.Beber)]
        [TestCase("CamillaAcostarse", "PJ01_Camilla_Acostarse", (int)GestoVisual.CamillaAcostarse)]
        [TestCase("ColgadoSubir", "PJ01_Colgado_Subir", (int)GestoVisual.ColgadoSubir)]
        [TestCase("Sentarse", "PJ01_Sentado", (int)GestoVisual.Sentarse)]
        [TestCase("Colgarse", "PJ01_Colgado_Idle", (int)GestoVisual.Colgarse)]
        public void Controlador_LosGestosNuevosDeLaBaseTienenClipYSeLanzanConSuNumero(string estado, string clip, int gesto)
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine m = Capa(c, ReglasAnimacion.CapaBase);
            AnimatorState s = Estado(m, estado);
            Assert.AreEqual(clip, s.motion.name);
            Assert.AreEqual(ReglasAnimacion.EtiquetaGesto, s.tag, "con la etiqueta Gesto la capa de brazos se apaga");
            AnimatorStateTransition entrada = m.anyStateTransitions.First(t => t.destinationState == s);
            Assert.IsTrue(entrada.conditions.Any(k => k.parameter == ReglasAnimacion.GestoTipo && Mathf.Approximately(k.threshold, gesto)));
        }

        [Test]
        public void Controlador_ElDerriboDeExplosionSigueEncadenandoConLevantarseBocaArriba()
        {
            // T-1540 (decisión del 21-nov): el derribo termina en el piso y se levanta con PJ01_Dano_Levantarse_BocaArriba, que termina en Idle.
            AnimatorController c = Controlador();
            AnimatorStateMachine m = Capa(c, ReglasAnimacion.CapaBase);
            AnimatorState derribo = Estado(m, "DerriboExplosion");
            Assert.IsTrue(derribo.transitions.Any(t => t.destinationState != null && t.destinationState.name == "LevantarseBocaArriba" && t.hasExitTime));
            AnimatorState levantar = Estado(m, "LevantarseBocaArriba");
            Assert.IsTrue(levantar.transitions.Any(t => t.destinationState != null && t.destinationState.name == "Idle" && t.hasExitTime));
        }

        [Test]
        public void Controlador_Repetir_AgregarEstados_NoDuplicaTransiciones()
        {
            AnimatorController c = Controlador();
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            int anyAntes = arriba.anyStateTransitions.Length;
            int golpeAntes = Estado(arriba, "GolpeIzq").transitions.Length;
            int vacioAntes = Estado(arriba, "Vacio").transitions.Length;
            TransicionesT1561.AgregarEstados(c);
            Assert.AreEqual(anyAntes, arriba.anyStateTransitions.Length, "Any State");
            Assert.AreEqual(golpeAntes, Estado(arriba, "GolpeIzq").transitions.Length, "salidas del golpe");
            Assert.AreEqual(vacioAntes, Estado(arriba, "Vacio").transitions.Length, "salidas de Vacio");
        }
    }
}
