using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Pruebas jugables del HUD a pie: prefabs reales del jugador y del HUD, entrada simulada.</summary>
    public class HudAPiePruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaHud = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private class JugadorFalso : IJugador
        {
            public ModoJugador ModoActual = ModoJugador.APie;
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoActual;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio;
            public void Colocar(Pose pose) { }

            public void CambiarModo(ModoJugador modo)
            {
                ModoActual = modo;
                ModoCambio?.Invoke(modo);
            }
        }

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private GameObject objetoHud;
        private GameObject raizFalsa;
        private HudAPie hud;
        private ConfigHudAPie config;
        private VidaAPie vida;
        private ArmasAPie armas;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            config = AssetDatabase.LoadAssetAtPath<ConfigHudAPie>(RutaConfig);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            UnityEngine.Object.Destroy(jugador);
            UnityEngine.Object.Destroy(objetoHud);
            UnityEngine.Object.Destroy(raizFalsa);
            UnityEngine.Object.Destroy(piso);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private void CrearJugador()
        {
            jugador = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            vida = jugador.GetComponent<VidaAPie>();
            armas = jugador.GetComponent<ArmasAPie>();
        }

        private void CrearJugadorSinHud()
        {
            CrearJugador();
            foreach (HudAPie propio in jugador.GetComponentsInChildren<HudAPie>(true))
            {
                UnityEngine.Object.DestroyImmediate(propio.gameObject);
            }
        }

        /// <summary>Toma el HUD anidado del jugador si hay uno; si no, instancia el prefab. Siempre queda un solo HudAPie.</summary>
        private void CrearHud()
        {
            hud = jugador != null ? jugador.GetComponentInChildren<HudAPie>(true) : null;
            if (hud == null)
            {
                objetoHud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaHud));
                hud = objetoHud.GetComponent<HudAPie>();
            }
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<HudAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                "Debe haber exactamente un HudAPie en la escena");
        }

        private JugadorFalso RegistrarFalso(ModoJugador modo)
        {
            raizFalsa = new GameObject("JugadorFalso");
            JugadorFalso falso = new JugadorFalso { Raiz = raizFalsa.transform, ModoActual = modo };
            Registro<IJugador>.Agregar(falso);
            return falso;
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vida_Completa_DiceLaVidaYLaBarraEstaLlena()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();

            Assert.AreEqual("VIDA " + Mathf.CeilToInt(vida.Maxima), hud.TextoVida.text);
            Assert.AreEqual(1f, hud.Relleno.fillAmount, 0.01f);
            Assert.AreEqual(config.ColorVida, hud.Relleno.color);
        }

        [UnityTest]
        public IEnumerator Vida_TrasDanioDe25_Dice75YBarraAl75()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();

            vida.RecibirDanio(new InfoDanio(25f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return null;

            Assert.AreEqual("VIDA 75", hud.TextoVida.text);
            Assert.AreEqual(0.75f, hud.Relleno.fillAmount, 0.01f);
            Assert.AreEqual(config.ColorVida, hud.Relleno.color);
        }

        [UnityTest]
        public IEnumerator Vida_PorDebajoDel30Porciento_LaBarraCambiaAColorBajo()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();

            vida.RecibirDanio(new InfoDanio(vida.Maxima * 0.75f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return null;

            Assert.AreEqual(0.25f, hud.Relleno.fillAmount, 0.01f);
            Assert.AreEqual(config.ColorVidaBaja, hud.Relleno.color);
        }

        [UnityTest]
        public IEnumerator Arma_ConEntradaSimulada_SigueLosPunosLaPistolaElDisparoYLaRecarga()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            Assert.AreEqual("PUÑOS", hud.TextoArma.text);

            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual("PISTOLA 8/8", hud.TextoArma.text);

            yield return Clic();
            Assert.AreEqual("PISTOLA 7/8", hud.TextoArma.text);

            yield return Tecla(teclado.rKey);
            StringAssert.Contains("RECARGANDO", hud.TextoArma.text);
            Assert.AreEqual(config.ColorAviso, hud.TextoArma.color);

            yield return Esperar(armas.GetComponent<PistolaAPie>().Datos.TiempoRecarga + 0.2f);
            Assert.AreEqual("PISTOLA 8/8", hud.TextoArma.text);
            Assert.AreEqual(config.ColorTexto, hud.TextoArma.color);

            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual("PUÑOS", hud.TextoArma.text);
        }

        [UnityTest]
        public IEnumerator Cancion_SinLlamarMostrarCancion_MuestraElTextoDeEspera()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();

            Assert.AreEqual(config.TextoCancionEspera, hud.TextoCancion.text);
        }

        [UnityTest]
        public IEnumerator Cancion_MostrarCancion_CambiaElTexto()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();

            hud.MostrarCancion("Cumbia del Chip");

            Assert.AreEqual("Cumbia del Chip", hud.TextoCancion.text);

            hud.MostrarCancion(string.Empty);
            Assert.AreEqual(config.TextoCancionEspera, hud.TextoCancion.text);
        }

        [UnityTest]
        public IEnumerator Modo_APieCarroYMecha_MuestraTodoSoloLaCancionONada()
        {
            CrearHud();
            JugadorFalso falso = RegistrarFalso(ModoJugador.APie);
            yield return Calentar();
            // T-400: con los puños en la mano la retícula no se ve.
            AssertGrupos(true, false, true, true);

            falso.CambiarModo(ModoJugador.Mecha);
            AssertGrupos(false, false, false, false);

            falso.CambiarModo(ModoJugador.Carro);
            AssertGrupos(false, false, false, true);

            falso.CambiarModo(ModoJugador.APie);
            AssertGrupos(true, false, true, true);
        }

        [UnityTest]
        public IEnumerator Modo_SinJugadorRegistrado_NoMuestraNada()
        {
            CrearHud();
            yield return Calentar();

            AssertGrupos(false, false, false, false);
        }

        [UnityTest]
        public IEnumerator Jugador_SeRegistraDespuesDelHud_ElHudLoEncuentraPorAgregado()
        {
            CrearHud();
            yield return Calentar();
            AssertGrupos(false, false, false, false);

            CrearJugadorSinHud();
            yield return null;

            AssertGrupos(true, false, true, true);   // T-400: con los puños la retícula no se ve
            Assert.AreEqual("VIDA " + Mathf.CeilToInt(vida.Maxima), hud.TextoVida.text);
            vida.RecibirDanio(new InfoDanio(25f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.AreEqual("VIDA 75", hud.TextoVida.text);
        }

        private void AssertGrupos(bool vidaActiva, bool miraActiva, bool armaActiva, bool cancionActiva)
        {
            Assert.AreEqual(vidaActiva, hud.GrupoVida.activeInHierarchy, "vida");
            Assert.AreEqual(miraActiva, hud.GrupoMira.activeInHierarchy, "mira");
            Assert.AreEqual(armaActiva, hud.GrupoArma.activeInHierarchy, "arma");
            Assert.AreEqual(cancionActiva, hud.GrupoCancion.activeInHierarchy, "canción");
        }
    }
}
