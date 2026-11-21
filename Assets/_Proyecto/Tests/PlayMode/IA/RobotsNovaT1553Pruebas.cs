using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1553 (decisiones de Tony 20-nov): el E16 se detiene a disparar el cañón (aviso, E16_Disparar_Canon, tiro con el brazo extendido) y no se mueve mientras dispara; camina con el
    /// escudo arriba (E16_CaminarEscudo) al acercarse o al recibir disparos; el E18 no lleva arma: vuela con el jetpack hacia el jugador, aterriza y pelea a puñetazos, quieto
    /// mientras golpea. Con el jugador real (JugadorAPie) y un piso. Necesita haber corrido RobotsNovaT1553.Aplicar.
    /// </summary>
    public class RobotsNovaT1553Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaE16 = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E16_RobotPesadoNova_a.prefab";
        private const string RutaE18 = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_E18_EliteNova_a.prefab";
        // Mira al jugador (que está en el origen) desde +Z.
        private static readonly Quaternion MiraAlJugador = Quaternion.Euler(0f, 180f, 0f);
        private const float MovimientoMaximoQuieto = 0.05f;

        private GameObject piso;
        private GameObject jugador;
        private readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        public override void TearDown()
        {
            foreach (GameObject e in extras)
            {
                Object.DestroyImmediate(e);
            }
            extras.Clear();
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private GameObject Crear(string ruta, Vector3 posicion)
        {
            GameObject g = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), posicion, MiraAlJugador);
            extras.Add(g);
            Physics.SyncTransforms();
            return g;
        }

        private static float Plano(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private static bool Reproduce(Animator a, string clip)
        {
            if (a == null) { return false; }
            for (int capa = 0; capa < a.layerCount; capa++)
            {
                foreach (AnimatorClipInfo c in a.GetCurrentAnimatorClipInfo(capa)) { if (c.clip != null && c.clip.name == clip) { return true; } }
                foreach (AnimatorClipInfo c in a.GetNextAnimatorClipInfo(capa)) { if (c.clip != null && c.clip.name == clip) { return true; } }
            }
            return false;
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) { yield return null; }
        }

        // ---------------------------------------------------------------- E16

        [UnityTest]
        public IEnumerator E16_SeDetieneADisparar_ConAvisoYClip_YNoSeMueveMientrasDispara()
        {
            yield return Esperar(0.3f);
            GameObject robot = Crear(RutaE16, new Vector3(0f, 0.05f, 14f));
            ConductaGuardiaPesado c = robot.GetComponent<ConductaGuardiaPesado>();
            Assert.IsTrue(c.DisparaCanon, "el E16 trae la fase de disparo (corre RobotsNovaT1553.Aplicar)");
            PandilleroAPie p = robot.GetComponent<PandilleroAPie>();
            yield return null;
            c.Provocar();

            bool vioAviso = false, vioClip = false;
            Vector3 inicio = Vector3.zero;
            bool enBloqueo = false;
            float movimientoMaximo = 0f;
            float fin = Time.time + 16f;
            while (Time.time < fin && !(c.DisparosCanon >= 1 && !c.Canon.Bloqueando))
            {
                yield return null;
                if (c.Canon.Bloqueando)
                {
                    if (!enBloqueo) { enBloqueo = true; inicio = robot.transform.position; }
                    movimientoMaximo = Mathf.Max(movimientoMaximo, Plano(robot.transform.position, inicio));
                    vioAviso |= c.Canon.Actual == ReglaCanonE16.Fase.Aviso;
                    vioClip |= Reproduce(p.Visual != null ? p.Visual.Animador : null, "E16_Disparar_Canon");
                }
                else
                {
                    enBloqueo = false;
                }
            }
            Assert.GreaterOrEqual(c.DisparosCanon, 1, "disparó el cañón");
            Assert.IsTrue(vioAviso, "avisó antes de disparar");
            Assert.IsTrue(vioClip, "jugó E16_Disparar_Canon");
            Assert.LessOrEqual(movimientoMaximo, MovimientoMaximoQuieto, "nunca se mueve mientras avisa y dispara (se movió " + movimientoMaximo + " m)");
            Assert.GreaterOrEqual(c.DisparosCanonConectados, 1, "el jugador quieto en la mira recibe el tiro");
        }

        [UnityTest]
        public IEnumerator E16_AlAcercarse_CaminaConElEscudoArriba_YLejos_ConE16Caminar()
        {
            yield return Esperar(0.3f);
            GameObject cerca = Crear(RutaE16, new Vector3(0f, 0.05f, 9f));
            ConductaGuardiaPesado c = cerca.GetComponent<ConductaGuardiaPesado>();
            VisualPersonaje visual = cerca.GetComponent<PandilleroAPie>().Visual;
            yield return null;
            c.Provocar();
            yield return Esperar(1.2f);
            Assert.IsTrue(c.EscudoArribaRegla.EscudoArriba, "a 9 m (menos de 12) levanta el escudo");
            Assert.Greater(visual.PesoEscudo, 0.95f);
            Assert.IsTrue(Reproduce(visual.Animador, "E16_CaminarEscudo"), "camina con E16_CaminarEscudo");
            Object.DestroyImmediate(cerca);

            GameObject lejos = Crear(RutaE16, new Vector3(0f, 0.05f, 22f));
            ConductaGuardiaPesado cl = lejos.GetComponent<ConductaGuardiaPesado>();
            VisualPersonaje vl = lejos.GetComponent<PandilleroAPie>().Visual;
            yield return null;
            cl.Provocar();
            yield return Esperar(1.2f);
            Assert.IsFalse(cl.EscudoArribaRegla.EscudoArriba, "a 20 m camina sin escudo");
            Assert.IsTrue(Reproduce(vl.Animador, "E16_Caminar"), "camina con E16_Caminar");
            Assert.IsFalse(Reproduce(vl.Animador, "E16_CaminarEscudo"));
        }

        [UnityTest]
        public IEnumerator E16_AlRecibirDisparos_LevantaElEscudoAunqueEsteLejos()
        {
            yield return Esperar(0.3f);
            GameObject robot = Crear(RutaE16, new Vector3(0f, 0.05f, 22f));
            ConductaGuardiaPesado c = robot.GetComponent<ConductaGuardiaPesado>();
            VisualPersonaje visual = robot.GetComponent<PandilleroAPie>().Visual;
            yield return null;
            c.Provocar();
            yield return Esperar(0.6f);
            Assert.IsFalse(c.EscudoArribaRegla.EscudoArriba);
            c.ProvocarPorDisparo(); // un disparo del jugador contra el robot
            yield return Esperar(0.8f);
            Assert.IsTrue(c.EscudoArribaRegla.EscudoArriba, "recibió un disparo: se cubre");
            Assert.IsTrue(Reproduce(visual.Animador, "E16_CaminarEscudo"));
        }

        [UnityTest]
        public IEnumerator E12_NoDisparaElCanon_NiCaminaConEscudo()
        {
            yield return Esperar(0.3f);
            GameObject guardia = Crear("Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E12.prefab", new Vector3(0f, 0.05f, 12f));
            ConductaGuardiaPesado c = guardia.GetComponent<ConductaGuardiaPesado>();
            yield return null;
            c.Provocar();
            yield return Esperar(4f);
            Assert.IsNull(c.Canon, "el cañón es solo del E16");
            Assert.AreEqual(0, c.DisparosCanon);
            Assert.IsNull(c.EscudoArribaRegla);
            Assert.AreEqual(0f, guardia.GetComponent<PandilleroAPie>().Visual != null ? guardia.GetComponent<PandilleroAPie>().Visual.PesoEscudo : 0f);
        }

        // ---------------------------------------------------------------- E18

        [UnityTest]
        public IEnumerator E18_SinArma_VuelaAterrizaYPeleaAPunos_QuietoAlGolpear()
        {
            yield return Esperar(0.3f);
            GameObject elite = Crear(RutaE18, new Vector3(0f, 0.05f, 22f));
            Assert.IsFalse(elite.GetComponentsInChildren<Transform>(true).Any(t => t.name == "W10"), "el E18 no lleva la W10");
            ConductaEliteNova c = elite.GetComponent<ConductaEliteNova>();
            Assert.IsNotNull(c, "ConductaEliteNova (corre RobotsNovaT1553.Aplicar)");
            yield return null;

            var fases = new HashSet<ReglaEliteNova.Fase>();
            bool jugoVuelo = false, jugoAterrizaje = false;
            float alturaMaxima = 0f;
            Vector3 inicioQuieto = Vector3.zero;
            bool enQuieto = false;
            float movimientoQuieto = 0f;
            float fin = Time.time + 25f;
            while (Time.time < fin && c.GolpesConectados < 1)
            {
                yield return null;
                ReglaEliteNova.Fase f = c.Regla.Actual;
                fases.Add(f);
                alturaMaxima = Mathf.Max(alturaMaxima, elite.transform.position.y);
                if (c.Reproductor.Reproduciendo)
                {
                    jugoVuelo |= c.Reproductor.ClipActual.name == "E18_Jetpack_Volar";
                    jugoAterrizaje |= c.Reproductor.ClipActual.name == "E18_Caida_Aterrizaje";
                }
                if (f == ReglaEliteNova.Fase.AvisoGolpe || f == ReglaEliteNova.Fase.Recupera)
                {
                    if (!enQuieto) { enQuieto = true; inicioQuieto = elite.transform.position; }
                    movimientoQuieto = Mathf.Max(movimientoQuieto, Plano(elite.transform.position, inicioQuieto));
                }
                else
                {
                    enQuieto = false;
                }
            }
            CollectionAssert.IsSupersetOf(fases, new[] { ReglaEliteNova.Fase.Vuela, ReglaEliteNova.Fase.Aterrizaje, ReglaEliteNova.Fase.Persigue, ReglaEliteNova.Fase.AvisoGolpe });
            Assert.IsTrue(jugoVuelo, "jugó E18_Jetpack_Volar");
            Assert.IsTrue(jugoAterrizaje, "jugó E18_Caida_Aterrizaje");
            Assert.Greater(alturaMaxima, 1f, "voló sobre el piso");
            Assert.LessOrEqual(movimientoQuieto, MovimientoMaximoQuieto, "se queda quieto para golpear (se movió " + movimientoQuieto + " m)");
            Assert.GreaterOrEqual(c.GolpesConectados, 1, "pegó un puñetazo al jugador");
            Assert.Less(Plano(elite.transform.position, jugador.transform.position), 3f, "aterrizó cerca del jugador");
        }

        [UnityTest]
        public IEnumerator E18_Muerto_NoSigueConElClipDeVuelo()
        {
            yield return Esperar(0.3f);
            GameObject elite = Crear(RutaE18, new Vector3(0f, 0.05f, 22f));
            ConductaEliteNova c = elite.GetComponent<ConductaEliteNova>();
            yield return null;
            float fin = Time.time + 5f;
            while (Time.time < fin && !c.Reproductor.Reproduciendo) { yield return null; }
            Assert.IsTrue(c.Reproductor.Reproduciendo, "está volando");
            PandilleroAPie p = elite.GetComponent<PandilleroAPie>();
            p.RecibirDanio(new InfoDanio(p.Maxima * 10f, TipoDanio.Disparo, elite.transform.position + Vector3.up, Vector3.forward, null));
            yield return Esperar(0.3f);
            Assert.IsTrue(p.EstaKO);
            Assert.IsFalse(c.Reproductor.Reproduciendo, "muerto: el clip de vuelo no tapa la muerte");
        }
    }
}
