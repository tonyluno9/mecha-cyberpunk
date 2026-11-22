using System.Collections;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1561 (decisión de Tony, 21-nov: "transiciones con lógica"): lo jugable con entrada simulada. Golpe -> correr (los brazos salen de la guardia), correr -> golpe (la guardia sube
    /// primero), disparar sin el arma en la mano (primero se saca), recargar bloquea el disparo y la granada se suelta en el cuadro del lanzamiento.
    /// Necesitan haber corrido TransicionesT1561.Aplicar (estados y prefab del jugador con sacarAntesDeDisparar). BaseJugadorAPie apaga sacarAntesDeDisparar para las pruebas viejas;
    /// aquí se vuelve a encender cuando hace falta.
    /// </summary>
    public class TransicionesT1561Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator animador;
        private PistolaAPie pistola;
        private GolpeAPie golpe;
        private int capaArriba;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            animador = visual.Animador;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            pistola = jugador.GetComponent<PistolaAPie>();
            golpe = jugador.GetComponent<GolpeAPie>();
            capaArriba = animador.GetLayerIndex(ReglasAnimacion.CapaArriba);
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private bool ArribaEn(string estado)
        {
            AnimatorStateInfo actual = animador.GetCurrentAnimatorStateInfo(capaArriba);
            return animador.IsInTransition(capaArriba) ? animador.GetNextAnimatorStateInfo(capaArriba).IsName(estado) || actual.IsName(estado) : actual.IsName(estado);
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
        }

        private IEnumerator Correr(float segundos)
        {
            Press(teclado.wKey);
            yield return null; // dos Press en el mismo cuadro: el segundo estado del teclado tapaba la W
            Press(teclado.leftShiftKey);
            yield return Esperar(segundos);
        }

        private void SoltarCarrera()
        {
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
        }

        // ---------- Pelear -> correr ----------

        [UnityTest]
        public IEnumerator PelearACorrer_LosBrazosSalenDeLaGuardia_SinCorrerConLosPunosLevantados()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            yield return Clic();
            yield return Esperar(0.9f);
            Assert.IsTrue(visual.EnGuardia, "Parado, tras el golpe, queda en guardia");
            Assert.IsTrue(ArribaEn("PunosGuardia"), "Los brazos juegan PJ01_Punos_Guardia");

            yield return Correr(0.2f);
            // En ~0.2 s la guardia ya no está permitida (Shift saca del modo pelea) y la capa de brazos baja con la transición corta.
            yield return Esperar(0.3f);
            Assert.Greater(controlador.Velocidad.magnitude, ReglasTransicionesT1561.VelocidadMaximaGuardia, "Va corriendo");
            Assert.IsFalse(visual.EnGuardia, "Con Shift se sale del modo pelea");
            Assert.IsFalse(ArribaEn("PunosGuardia"), "Nadie corre con los puños levantados");
            Assert.IsTrue(ArribaEn("Vacio"), "Los brazos vuelven a normales");

            // Soltar Shift no devuelve la guardia sola.
            SoltarCarrera();
            yield return Esperar(0.5f);
            Assert.IsFalse(visual.EnGuardia, "La guardia no vuelve sola al dejar de correr");
        }

        [UnityTest]
        public IEnumerator PelearACorrer_ConGolpeEnCurso_AlTerminarSinGuardiaSaleEnCorto()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            yield return Clic();
            yield return Correr(0.1f);
            yield return Esperar(1.2f);
            Assert.IsFalse(visual.EnGuardia, "Corriendo, tras el golpe, no vuelve a la guardia");
            Assert.IsFalse(ArribaEn("PunosGuardia"));
            SoltarCarrera();
        }

        // ---------- Correr -> pelear ----------

        [UnityTest]
        public IEnumerator CorrerAPelear_AlGolpearCorriendo_PrimeroSubeLaGuardiaYLuegoGolpea()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            yield return Correr(1.2f);
            Assert.Greater(controlador.Velocidad.magnitude, ReglasTransicionesT1561.VelocidadMaximaGuardia, "Va corriendo");
            Assert.IsFalse(visual.GuardiaArriba, "Corriendo los brazos van sueltos");

            float clic = Time.time;
            Press(raton.leftButton);
            bool guardiaVista = false;
            float inicioGolpe = -1f;
            while (Time.time < clic + 1f && inicioGolpe < 0f)
            {
                yield return null;
                guardiaVista |= visual.EnGuardia;
                if (golpe.Fase == FaseGolpe.Preparando)
                {
                    inicioGolpe = Time.time;
                }
            }
            Release(raton.leftButton);
            SoltarCarrera();
            Assert.GreaterOrEqual(inicioGolpe, 0f, "El golpe sí sale");
            Assert.IsTrue(guardiaVista, "Antes del golpe sube la guardia");
            Assert.GreaterOrEqual(inicioGolpe - clic, ReglasTransicionesT1561.SegundosSubirGuardia - 0.06f, "El golpe espera a que suba la guardia (" + (inicioGolpe - clic).ToString("F2") + " s)");
            Assert.LessOrEqual(inicioGolpe - clic, ReglasTransicionesT1561.SegundosSubirGuardia + 0.35f, "Pero no tarda de más");
        }

        [UnityTest]
        public IEnumerator CorrerAPelear_ParadoElGolpeSaleDeUnaVez()
        {
            yield return Calentar();
            yield return Esperar(0.5f);
            float clic = Time.time;
            Press(raton.leftButton);
            float inicioGolpe = -1f;
            while (Time.time < clic + 0.6f && inicioGolpe < 0f)
            {
                yield return null;
                if (golpe.Fase == FaseGolpe.Preparando)
                {
                    inicioGolpe = Time.time;
                }
            }
            Release(raton.leftButton);
            Assert.GreaterOrEqual(inicioGolpe, 0f, "El golpe sale");
            Assert.Less(inicioGolpe - clic, ReglasTransicionesT1561.SegundosSubirGuardia, "Parado no espera la guardia (" + (inicioGolpe - clic).ToString("F2") + " s)");
        }

        // ---------- Armas: sacar -> apuntar -> disparar ----------

        [UnityTest]
        public IEnumerator DispararSinArmaEnMano_PrimeroSacaElArma_Y_ElTiroSaleConLaPose()
        {
            yield return Calentar();
            yield return ElegirEnRueda(1);
            yield return Esperar(0.4f);
            pistola.SacarAntesDeDisparar = true;
            Assert.IsFalse(visual.ArmaEnMano, "La pistola está guardada");
            int disparos = 0;
            bool poseAlDisparar = false;
            pistola.Disparo += () => { disparos++; poseAlDisparar = visual.ArmaEnPose; };
            bool vioSacar = false;

            float clic = Time.time;
            yield return Clic();
            float tiro = -1f;
            while (Time.time < clic + 2f && tiro < 0f)
            {
                vioSacar |= ArribaEn("PistolaSacar");
                yield return null;
                if (disparos > 0)
                {
                    tiro = Time.time;
                }
            }
            Assert.IsTrue(vioSacar, "Primero juega Pistola_Sacar");
            Assert.Greater(tiro, 0f, "El tiro termina saliendo");
            Assert.Greater(tiro - clic, 0.2f, "No sale en el frame del clic (" + (tiro - clic).ToString("F2") + " s)");
            Assert.IsTrue(poseAlDisparar || tiro - clic >= ReglasTransicionesT1561.SegundosMaximosSacar - 0.1f, "El disparo real sale con el arma ya en la pose");
            Assert.AreEqual(1, disparos, "Un clic, un tiro");
        }

        [UnityTest]
        public IEnumerator DispararConElArmaEnPose_SaleAlInstante()
        {
            yield return Calentar();
            yield return ElegirEnRueda(1);
            pistola.SacarAntesDeDisparar = true;
            int disparos = 0;
            pistola.Disparo += () => disparos++;
            Press(raton.rightButton);
            yield return Esperar(1.2f);
            Assert.IsTrue(visual.ArmaEnPose, "Apuntando, el arma está en la pose");
            float clic = Time.time;
            yield return Clic();
            yield return null;
            Release(raton.rightButton);
            Assert.AreEqual(1, disparos, "Con el arma lista el tiro sale sin esperar");
            Assert.Less(Time.time - clic, 0.3f);
        }

        // ---------- Recargar bloquea disparar ----------

        [UnityTest]
        public IEnumerator Recargar_BloqueaElDisparoHastaQueTermina()
        {
            yield return Calentar();
            yield return ElegirEnRueda(1);
            pistola.SacarAntesDeDisparar = true;
            int disparos = 0;
            pistola.Disparo += () => disparos++;
            Press(raton.rightButton);
            yield return Esperar(1.2f);
            yield return Clic();
            yield return Esperar(pistola.Datos.TiempoEntreDisparos + 0.1f);
            Assert.AreEqual(1, disparos);

            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return Esperar(0.1f);
            Assert.IsTrue(pistola.Recargando, "Empezó a recargar");
            yield return Clic();
            yield return Esperar(0.2f);
            Assert.AreEqual(1, disparos, "Recargando no dispara");

            yield return Esperar(pistola.Datos.TiempoRecarga + 0.3f);
            Assert.IsFalse(pistola.Recargando);
            yield return Clic();
            yield return Esperar(0.2f);
            Release(raton.rightButton);
            Assert.AreEqual(2, disparos, "Al terminar la recarga vuelve a disparar");
        }

        // ---------- Granada: se suelta en el cuadro del lanzamiento ----------

        [UnityTest]
        public IEnumerator Granada_SeSueltaEnElCuadroDelLanzamiento_NoAlApretar()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Granada, 0), 1);
            yield return null;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Granada), "La granada se equipa");
            yield return Esperar(0.3f);
            ArmasEspecialesAPie especiales = jugador.GetComponent<ArmasEspecialesAPie>();

            float clic = Time.time;
            yield return Clic();
            yield return null; // el disparo de las armas especiales se procesa en LateUpdate
            Assert.IsTrue(especiales.GranadaEnLaMano, "Con el controlador de T-1561 la granada espera al cuadro del lanzamiento (¿se corrió TransicionesT1561.Aplicar?)");
            yield return null;
            Assert.IsNull(especiales.UltimoProyectil, "No sale al apretar");
            bool vioClip = false;
            float suelta = -1f;
            while (Time.time < clic + 3f && suelta < 0f)
            {
                vioClip |= ArribaEn("GranadaDePie");
                yield return null;
                if (especiales.UltimoProyectil != null)
                {
                    suelta = Time.time;
                }
            }
            Assert.IsTrue(vioClip, "La capa de brazos juega PJ01_Granada_DePie");
            Assert.Greater(suelta, 0f, "La granada sale");
            float esperado = ReglasTransicionesT1561.SegundosHastaSoltarGranada(false);
            Assert.AreEqual(esperado, suelta - clic, 0.25f, "Se suelta en el cuadro " + ReglasTransicionesT1561.CuadroSueltaDePie + " del clip (" + (suelta - clic).ToString("F2") + " s, esperado " + esperado.ToString("F2") + ")");
            Assert.IsFalse(especiales.GranadaEnLaMano);
        }

        [UnityTest]
        public IEnumerator Granada_UnDanoFuerteCortaElLanzamiento_YLaGranadaNoSale()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Granada, 0), 1);
            yield return null;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Granada));
            yield return Esperar(0.3f);
            ArmasEspecialesAPie especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            yield return Clic();
            yield return Esperar(0.3f);
            Assert.IsTrue(especiales.GranadaEnLaMano);

            visual.Interrumpir(); // lo que hace AnimacionJugador al recibir un golpe de cierta fuerza
            yield return Esperar(ReglasTransicionesT1561.SegundosHastaSoltarGranada(false) + 0.3f);
            Assert.IsNull(especiales.UltimoProyectil, "Interrumpida, la granada no sale");
            Assert.IsFalse(especiales.GranadaEnLaMano);
            Assert.IsFalse(ArribaEn("GranadaDePie"), "La capa de brazos volvió a su reposo");
        }

        // ---------- Muerte y salto ----------

        [UnityTest]
        public IEnumerator Muerte_ApagaLosBrazosYElArmaDeGolpe()
        {
            yield return Calentar();
            yield return ElegirEnRueda(1);
            Press(raton.rightButton);
            yield return Esperar(1.2f);
            Assert.Greater(visual.PesoCapaArriba, 0.9f, "Apuntando la capa de brazos está prendida");
            visual.Caido = true;
            Assert.AreEqual(0f, visual.PesoCapaArriba, 0.0001f, "Al caer, de golpe a 0");
            Assert.AreEqual(0f, animador.GetLayerWeight(capaArriba), 0.0001f);
            Assert.IsFalse(visual.ArmaEnMano, "El arma se guarda");
            Release(raton.rightButton);
            visual.Caido = false;
        }

        [UnityTest]
        public IEnumerator Salto_EnElAireLaCapaDeBrazosSeApaga_YAlAterrizarVuelve()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            Assert.Greater(visual.PesoCapaArriba, 0.9f, "En el piso está prendida");
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            bool apagada = false;
            float tope = Time.time + 1.5f;
            while (Time.time < tope && !apagada)
            {
                yield return null;
                apagada = !controlador.EnPiso && visual.PesoCapaArriba < 0.5f;
            }
            Assert.IsTrue(apagada, "En el aire los brazos se apagan");
            yield return Esperar(1.5f);
            Assert.IsTrue(controlador.EnPiso);
            Assert.Greater(visual.PesoCapaArriba, 0.9f, "Al aterrizar vuelven");
        }
    }
}
