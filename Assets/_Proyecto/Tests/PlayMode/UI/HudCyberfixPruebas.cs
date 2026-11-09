using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-521 en Ciudad_Prologo: el HUD rehecho con las piezas de UI (marcos, símbolo de Biits, panel del arma con 5 casillas, aliados compactos,
    /// zona con su respeto) y las fuentes Teko y Rajdhani. Con CAPTURAR_T521=1 guarda docs/capturas/T-521-1 (1080p) y T-521-1-1440.
    /// </summary>
    public class HudCyberfixPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T521";
        private const string LetrasEspanol = "ÁÉÍÓÚÑñ¿¡·";
        private static readonly Vector3 Inicio = new Vector3(-20f, 0.5f, 0f);

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        // Solo si la prueba cargó la ciudad: en las pruebas sin escena, destruir las raíces mataría al ejecutor de pruebas.
        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            Scene escena = SceneManager.GetActiveScene();
            if (escena.name == "Ciudad_Prologo")
            {
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    UnityEngine.Object.Destroy(raiz);
                }
            }
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        [Test]
        public void Fuentes_TitulosEnTekoYTextoEnRajdhani()
        {
            StringAssert.Contains("Teko", KitUI.Estilo.FuenteDe(RolTexto.Titulo).name);
            StringAssert.Contains("Teko", KitUI.Estilo.FuenteDe(RolTexto.Hud).name);
            StringAssert.Contains("Rajdhani", KitUI.Estilo.FuenteDe(RolTexto.Texto).name);
            StringAssert.Contains("Rajdhani", KitUI.Estilo.FuenteDe(RolTexto.Boton).name);
            foreach (RolTexto rol in new[] { RolTexto.Titulo, RolTexto.Texto })
            {
                Font fuente = KitUI.Estilo.FuenteDe(rol);
                foreach (char c in LetrasEspanol)
                {
                    Assert.IsTrue(fuente.HasCharacter(c), fuente.name + " trae " + c);
                }
            }
        }

        [Test]
        public void Piezas_TodasLasPiezasDelHudEstanCargadas()
        {
            PiezasUI piezas = PiezasUI.Actual;
            Assert.IsNotNull(piezas, "UI/Resources/PiezasUI.asset");
            Assert.IsNotNull(piezas.MarcoPanel);
            Assert.IsNotNull(piezas.BarraVida);
            Assert.IsNotNull(piezas.BarraCalor3);
            Assert.IsNotNull(piezas.SimboloBiits);
            Assert.IsNotNull(piezas.MarcoMinimapa);
            Assert.IsNotNull(piezas.PanelArma);
            Assert.AreEqual(5, piezas.Armas.Length);
            foreach (Sprite arma in piezas.Armas)
            {
                Assert.IsNotNull(arma);
            }
            Assert.IsNotNull(piezas.Insignia(IdPandilla.Calaveras));
            Assert.IsNotNull(piezas.Insignia(IdPandilla.Viboras));
            Assert.IsNull(piezas.Insignia(IdPandilla.Ninguna));
            Assert.Greater(piezas.MarcoPanel.border.x, 0f, "marco_panel es 9-slice");
            Assert.Greater(piezas.BotonNormal.border.x, 0f, "los botones son 9-slice");
        }

        [UnityTest]
        public IEnumerator HudEnLaCiudad_VidaCalorBiitsArmaYZona()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null;
            }, "el director arrancó", 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            IJugador jugador = Registro<IJugador>.Actual;
            HudAPie hud = UnityEngine.Object.FindFirstObjectByType<HudAPie>();
            HudTerritorio zona = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(zona);
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            jugador.Colocar(new Pose(Inicio, Quaternion.LookRotation(Vector3.right)));
            Registro<ICartera>.Actual.Abonar(1250, MotivoDinero.Premio);
            Registro<ICalor>.Actual.Sumar(40f, MotivoCalor.Otro);
            yield return new WaitForSecondsRealtime(1f);
            yield return null;

            // Vida llena en turquesa.
            VidaAPie vida = jugador.Raiz.GetComponent<VidaAPie>();
            Assert.AreEqual(KitUI.Estilo.Turquesa, hud.Relleno.color, "la vida llena va turquesa");

            // Todo en español y sin etiqueta "HUD".
            Assert.AreEqual("CALOR", hud.TextoCalor.text);
            foreach (Text t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Assert.AreNotEqual("HUD", t.text, "no hay etiqueta HUD: " + t.name);
            }

            // Biits: el símbolo propio y la cifra sin la Ƀ de texto.
            Assert.AreEqual(Moneda.Texto(Registro<ICartera>.Actual.Dinero).Replace(Moneda.Simbolo, string.Empty), hud.CifraDinero.text.Trim());
            StringAssert.DoesNotContain(Moneda.Simbolo, hud.CifraDinero.text);
            Assert.IsFalse(hud.TextoDinero.enabled, "el texto de datos queda apagado");
            Assert.IsTrue(hud.CifraDinero.gameObject.activeInHierarchy);
            Assert.IsNotNull(hud.GrupoDinero.transform.Find("Simbolo").GetComponent<Image>().sprite);

            // Calor: 1 de 3 niveles lleno.
            Assert.AreEqual(3, hud.MarcasCalor.Length);
            Assert.AreEqual(1, hud.MarcasCalorLlenas);

            // Arma: 5 casillas y el icono del arma activa.
            Assert.AreEqual(5, hud.CasillasArma.Length);
            Assert.IsNotNull(hud.IconoArma.sprite, "icono del arma activa");
            Assert.IsTrue(hud.IconoArma.enabled);

            // Zona: el panel con el texto del dueño y tu respeto.
            yield return EsperarHasta(() => zona.TextoZona.text.Length > 0, "la zona", 20f);
            Assert.IsTrue(zona.TextoZona.transform.parent.gameObject.activeSelf);
            StringAssert.Contains("RESPETO", zona.TextoZona.text);

            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Camera camara = jugador.Camara;
                CapturaUI.Capturar("docs/capturas/T-521-1.png", camara);
                CapturaUI.Capturar2K("docs/capturas/T-521-1-1440.png", camara);
            }

            // La vida baja (debajo del 30 %) pasa a rojo. T-684: sin escudo, para medir solo la vida.
            jugador.Raiz.GetComponent<MechaCyberpunk.Personaje.Vida.EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            vida.RecibirDanio(new InfoDanio(vida.Maxima * 0.75f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return null;
            Assert.AreEqual(KitUI.Estilo.Enemigo, hud.Relleno.color, "la vida baja va roja");
            Assert.Greater(hud.Relleno.color.r, 0.9f);
            Assert.Less(hud.Relleno.color.b, 0.4f);
        }
    }
}
