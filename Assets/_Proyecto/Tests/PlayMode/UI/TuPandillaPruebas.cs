using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-415 en Ciudad_Prologo: el nombre y el color de tu pandilla salen en el HUD de zona, en el mapa, en el CHATO y en el aviso de
    /// reclutar, se repintan al cambiarlos y sobreviven a guardar y cargar. Con CAPTURAR_T415=1 guarda docs/capturas/T-415-1.png.
    /// </summary>
    public class TuPandillaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T415";
        private const string NombreAliado = "EL CHATO";
        private const float XSuburbios = -80f;
        private const float SegundosCaminar = 0.8f;
        private const float Tolerancia = 0.02f;
        private static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        private Keyboard teclado;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            carpeta = Path.Combine(Application.temporaryCachePath, "tu_pandilla_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            base.TearDown();
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

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static void AssertColor(Color esperado, Color real, string que)
        {
            Assert.AreEqual(esperado.r, real.r, Tolerancia, que + " (r)");
            Assert.AreEqual(esperado.g, real.g, Tolerancia, que + " (g)");
            Assert.AreEqual(esperado.b, real.b, Tolerancia, que + " (b)");
        }

        private static Color RopaDe(AliadoAPie aliado)
        {
            Renderer r = null;
            foreach (Renderer candidato in aliado.GetComponentsInChildren<Renderer>(true))
            {
                if (!(candidato is LineRenderer))
                {
                    r = candidato;
                    break;
                }
            }
            Assert.IsNotNull(r, "El aliado tiene renderer.");
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            r.GetPropertyBlock(bloque);
            return bloque.GetColor(IdColor);
        }

        private static Color ManzanaDe(PantallaMapa mapa, string zona)
        {
            for (int i = 0; i < mapa.Etiquetas.Length; i++)
            {
                if (mapa.Etiquetas[i].text.Contains(zona))
                {
                    return mapa.Manzanas[i].color;
                }
            }
            Assert.Fail("El mapa no muestra " + zona);
            return Color.clear;
        }

        [UnityTest]
        public IEnumerator NombreYColorEnTodo()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null
                && Registro<IPandillaJugador>.Actual != null && Registro<IPerfilJugador>.Actual is ServicioPerfil
                && ServicioGuardado.Actual != null, "jugador, territorio, pandilla, perfil y guardado", 30f);
            yield return new WaitForSecondsRealtime(1f);
            IJugador jugador = Registro<IJugador>.Actual;
            ServicioPerfil perfil = (ServicioPerfil)Registro<IPerfilJugador>.Actual;
            HudTerritorio hud = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            HudAliados hudAliados = UnityEngine.Object.FindFirstObjectByType<HudAliados>();
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(hudAliados);
            Assert.IsNotNull(mapa);

            // Por defecto: LOS DE SUBURBIOS y naranja.
            Assert.AreEqual(ColoresPandilla.NombrePredeterminado, perfil.NombrePandilla);
            Assert.AreEqual(ColoresPandilla.Predeterminado, perfil.ColorPandilla);

            // El CHATO se une: el aviso lleva el nombre real.
            Assert.IsTrue(((PandillaJugador)Registro<IPandillaJugador>.Actual).Reclutar(NombreAliado));
            yield return EsperarHasta(() => AliadoAPie.Activos.Count > 0, "aparece el aliado", 10f);
            AliadoAPie chato = AliadoAPie.Activos[0];
            yield return EsperarHasta(() => hudAliados.TextoAviso.text.Length > 0, "el aviso de reclutar", 5f);
            Assert.AreEqual("EL CHATO SE UNE A LOS DE SUBURBIOS", hudAliados.TextoAviso.text);
            AssertColor(ColoresPandilla.Color(0), RopaDe(chato), "el CHATO empieza naranja");

            // Cambias la pandilla: todo se repinta sin recargar la escena.
            perfil.FijarPandilla("los neón", 3);
            Assert.AreEqual("LOS NEÓN", perfil.NombrePandilla);
            Assert.AreEqual(3, perfil.ColorPandilla);
            AssertColor(ColoresPandilla.Color(3), RopaDe(chato), "el CHATO pasa a azul");

            jugador.Colocar(new Pose(new Vector3(XSuburbios, 0.5f, 0f), Quaternion.LookRotation(Vector3.right)));
            Press(teclado.wKey);
            yield return new WaitForSecondsRealtime(SegundosCaminar);
            Release(teclado.wKey);
            yield return EsperarHasta(() => hud.TextoZona.text.Contains("SUBURBIOS"), "el HUD de zona", 20f);
            StringAssert.Contains("SUBURBIOS · LOS NEÓN · RESPETO", hud.TextoZona.text);
            AssertColor(ColoresPandilla.Color(3), hud.TextoZona.color, "el HUD de zona va azul");

            // El mapa: tus manzanas azules.
            yield return Pulsar(teclado.mKey);
            yield return EsperarHasta(() => mapa.Abierto, "abrir el mapa con M", 5f);
            yield return new WaitForSecondsRealtime(0.3f);
            AssertColor(ColoresPandilla.Color(3), ManzanaDe(mapa, "SUBURBIOS"), "SUBURBIOS en el mapa va azul");
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar("docs/capturas/T-415-1.png", jugador.Camara);
            }
            // Al cambiar a AMARILLO el mapa abierto se repinta.
            perfil.FijarPandilla("los neón", 2);
            yield return null;
            yield return null;
            AssertColor(ColoresPandilla.Color(2), ManzanaDe(mapa, "SUBURBIOS"), "SUBURBIOS en el mapa pasa a amarillo");
            AssertColor(ColoresPandilla.Color(2), RopaDe(chato), "el CHATO pasa a amarillo");
            perfil.FijarPandilla("los neón", 3);
            yield return Pulsar(teclado.mKey);
            yield return EsperarHasta(() => !mapa.Abierto, "cerrar el mapa con M", 5f);

            // Guardar, cambiar y cargar: vuelve LOS NEÓN y azul.
            Assert.IsTrue(ServicioGuardado.Actual.Guardar());
            perfil.FijarPandilla("otros", 0);
            Assert.AreEqual("OTROS", perfil.NombrePandilla);
            Assert.IsTrue(ServicioGuardado.Actual.Cargar());
            Assert.AreEqual("LOS NEÓN", perfil.NombrePandilla);
            Assert.AreEqual(3, perfil.ColorPandilla);
            AssertColor(ColoresPandilla.Color(3), RopaDe(chato), "el CHATO vuelve a azul");
        }
    }
}
