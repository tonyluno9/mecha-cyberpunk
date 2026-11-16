using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1107: la riña SNAKES contra SKULLS en un piso de prueba con el jugador real. Se pegan entre ellos sin que el jugador esté cerca; con el
    /// jugador a 5 m o si le pega a alguno, los seis lo atacan; si un bando cae, los demás vuelven a su punto; y la riña se rearma sola.
    /// </summary>
    public class RinaPandillasT1107Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaSnakes = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Snakes.prefab";
        private const string RutaSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaViboras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        private const string RutaCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private GameObject piso;
        private GameObject jugador;
        private RinaPandillas rina;

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
            piso.transform.localScale = new Vector3(600f, 1f, 600f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (rina != null)
            {
                Object.DestroyImmediate(rina.gameObject);
            }
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void CrearRina(Vector3 centro)
        {
            rina = ConstructorRina.Crear(null, "Rina_Prueba", centro, Vector3.right,
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaSnakes), AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaViboras), IdPandilla.Viboras,
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkulls), AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaCalaveras), IdPandilla.Calaveras);
            Physics.SyncTransforms();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        private float VidaDelBando(PandillaAPie bando) => bando.Pandilleros.Sum(p => p.EstaKO ? 0f : p.Actual);

        // De espaldas a la riña: su cámara no la ve.
        private void ColocarJugador(Vector3 punto) => Registro<IJugador>.Actual.Colocar(new Pose(punto, Quaternion.Euler(0f, 180f, 0f)));

        [UnityTest]
        public IEnumerator Rina_SinJugadorCerca_LosDosBandosSeGolpeanEntreEllos()
        {
            CrearRina(new Vector3(0f, 0f, 50f));
            yield return null;
            Assert.AreEqual(6, rina.Miembros.Count(p => p.isActiveAndEnabled), "los seis están en pie al empezar");
            Assert.AreEqual(IdPandilla.Viboras, rina.BandoA.Pandilla);
            float a0 = VidaDelBando(rina.BandoA);
            float b0 = VidaDelBando(rina.BandoB);
            yield return Esperar(10f);
            Assert.Less(VidaDelBando(rina.BandoA), a0, "la vida de los SNAKES baja");
            Assert.Less(VidaDelBando(rina.BandoB), b0, "la vida de los SKULLS baja");
            Assert.AreEqual(RinaPandillas.Estado.Rina, rina.EstadoActual, "el jugador a 50 m no la interrumpe");
            Transform raizJugador = Registro<IJugador>.Actual.Raiz;
            Assert.IsTrue(rina.Miembros.All(p => p.ObjetivoActual != raizJugador), "nadie apunta al jugador");
            Assert.IsTrue(rina.Miembros.All(p => !p.EstaKO), "la riña no mata: nadie cae por golpes de 35%");
        }

        [UnityTest]
        public IEnumerator Rina_ConElJugadorA5m_LosSeisLoAtacan()
        {
            CrearRina(new Vector3(0f, 0f, 5.5f)); // el más cercano queda a ~4.6 m
            yield return Esperar(2.5f);
            Assert.AreEqual(RinaPandillas.Estado.Liberada, rina.EstadoActual, "el jugador a menos de 6 m suelta la riña");
            Assert.IsTrue(rina.Miembros.All(p => !p.GetComponent<ConductaRina>().Controla), "ninguna conducta lleva el turno");
            Transform raizJugador = Registro<IJugador>.Actual.Raiz;
            Assert.AreEqual(6, rina.Miembros.Count(p => p.ObjetivoActual == raizJugador), "los seis tienen como objetivo al jugador");
        }

        [UnityTest]
        public IEnumerator Rina_SiElJugadorLePegaAUno_LosDosBandosVanContraEl()
        {
            CrearRina(new Vector3(0f, 0f, 40f));
            yield return Esperar(1f);
            Assert.AreEqual(RinaPandillas.Estado.Rina, rina.EstadoActual);
            PandilleroAPie victima = rina.BandoA.Pandilleros[0];
            victima.RecibirDanio(new InfoDanio(5f, TipoDanio.Golpe, victima.transform.position + Vector3.up, Vector3.forward, null));
            yield return Esperar(1f);
            Assert.AreEqual(RinaPandillas.Estado.Liberada, rina.EstadoActual, "el daño que no es de la riña la suelta");
            Assert.IsTrue(rina.Miembros.All(p => !p.GetComponent<ConductaRina>().Controla), "los dos bandos dejan la riña");
            Assert.IsTrue(rina.Miembros.Where(p => !p.EstaKO).All(p => p.Alerta), "los vivos quedan en alerta contra el jugador");
        }

        [UnityTest]
        public IEnumerator Rina_UnBandoCae_LosQueQuedanVuelvenASuPunto()
        {
            ColocarJugador(new Vector3(0f, 0.05f, -20f)); // lejos de la riña (a ~70 m) pero sin pasar de 80 m
            CrearRina(new Vector3(0f, 0f, 50f));
            Time.timeScale = 4f;
            float fin = Time.time + 400f; // tiempo escalado
            while (rina.EstadoActual == RinaPandillas.Estado.Rina && Time.time < fin) yield return null;
            Time.timeScale = 1f;
            Assert.AreEqual(RinaPandillas.Estado.Terminada, rina.EstadoActual, "un bando cayó solo por la riña");
            PandillaAPie caido = rina.BandoA.Pandilleros.All(p => p.EstaKO || p.VidaFraccion <= ReglaRina.FraccionMinimaVida) ? rina.BandoA : rina.BandoB;
            Assert.IsTrue(caido.Pandilleros.All(p => p.EstaKO || p.VidaFraccion <= ReglaRina.FraccionMinimaVida), "el bando caído quedó sin vida útil");
            Assert.IsTrue(rina.Miembros.All(p => !p.GetComponent<ConductaRina>().Controla), "nadie sigue en la riña");
            yield return Esperar(25f);
            foreach (PandilleroAPie p in rina.Miembros.Where(m => !m.EstaKO))
            {
                Vector3 d = p.transform.position - p.Hogar;
                d.y = 0f;
                Assert.Less(d.magnitude, Mathf.Max(1.5f, p.Datos.RadioPatrulla + 1f), p.name + " está en su punto (o patrullando a su alrededor)");
                Assert.IsNull(p.ObjetivoActual, p.name + " no persigue a nadie");
            }
        }

        [UnityTest]
        public IEnumerator Rina_SeRearmaCuandoElJugadorEstaLejosYNadieLaVe()
        {
            CrearRina(new Vector3(0f, 0f, 40f));
            yield return Esperar(0.5f);
            PandilleroAPie victima = rina.BandoB.Pandilleros[0];
            victima.RecibirDanio(new InfoDanio(500f, TipoDanio.Golpe, victima.transform.position + Vector3.up, Vector3.forward, null));
            yield return Esperar(0.5f);
            Assert.AreEqual(RinaPandillas.Estado.Liberada, rina.EstadoActual);
            Assert.IsTrue(victima.EstaKO, "el golpe del jugador lo venció");
            ColocarJugador(new Vector3(0f, 0.05f, -60f)); // a ~100 m, sin cámara principal que la vea
            yield return Esperar(1f);
            Assert.AreEqual(1, rina.Rearmes, "se rearmó una vez");
            Assert.AreEqual(RinaPandillas.Estado.Rina, rina.EstadoActual);
            Assert.AreEqual(6, rina.Miembros.Count(p => p.isActiveAndEnabled && !p.EstaKO), "los seis viven de nuevo");
            Assert.IsTrue(rina.Miembros.All(p => p.VidaFraccion > 0.8f), "con la vida casi llena (ya empezaron a pegarse)");
            Assert.IsTrue(rina.Miembros.All(p => p.GetComponent<ConductaRina>().Controla), "otra vez en la riña");
            float antes = VidaDelBando(rina.BandoA);
            yield return Esperar(8f);
            Assert.Less(VidaDelBando(rina.BandoA), antes, "la riña vuelve a empezar");
        }
    }
}
