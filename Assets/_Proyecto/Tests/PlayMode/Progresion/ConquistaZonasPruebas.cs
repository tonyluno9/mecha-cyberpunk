using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-428: en el juego libre NEÓN ALTO y LA REFINERÍA (cuarta zona, de NOVA) se conquistan de punta a punta: vencer a su gente sube el respeto,
    /// con 70 sale el jefe con aviso en el HUD, vencerlo hace la zona tuya, el mapa y el HUD cambian de color y se guarda. Atajos anotados: F6
    /// salta la historia, Colocar mueve al jugador, los pandilleros caen por IRecibeDanio y los últimos vencidos hasta 70 se registran por ITerritorio.
    /// Con CAPTURAR_T428=1 y el editor abierto deja docs/capturas/T-428-1.png (la zona) y T-428-2.png (el mapa con 4 zonas).
    /// </summary>
    public class ConquistaZonasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T428";
        private const float DanoMatar = 100000f;
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
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

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static JefePandilla Jefe(string zona)
        {
            foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == zona)
                {
                    return j;
                }
            }
            return null;
        }

        private static PandillaAPie Grupo(IdPandilla pandilla)
        {
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.Pandilla == pandilla)
                {
                    return g;
                }
            }
            return null;
        }

        private static void Matar(PandilleroAPie p)
        {
            p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
        }

        private static IEnumerator Poner(IJugador jugador, Vector3 punto)
        {
            jugador.Colocar(new Pose(punto + Vector3.up * 0.1f, Quaternion.identity));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // Vista Game del editor, sin el muñeco del jugador (OnGUI no corre en batchmode: las capturas se toman con el editor abierto).
        private static IEnumerator Capturar(IJugador jugador, string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1" || Application.isBatchMode)
            {
                yield break;
            }
            yield return new WaitForEndOfFrame();
            Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), foto.EncodeToPNG());
            Debug.Log("[T-428] Captura " + nombre + " de " + foto.width + "x" + foto.height);
            Object.Destroy(foto);
        }

        private IEnumerator Conquistar(string zona, IdPandilla pandilla, string nombreZona, string nombreJefe, float vidaMinimaJefe)
        {
            ServicioTerritorio territorio = Registro<ServicioTerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            HudTerritorio hud = Object.FindFirstObjectByType<HudTerritorio>();
            JefePandilla jefe = Jefe(zona);
            Assert.IsNotNull(jefe, "Hay un punto de jefe en " + zona);
            Assert.AreEqual(zona, territorio.ZonaEn(jefe.transform.position));
            Assert.AreEqual(pandilla, territorio.Dueno(zona));
            Assert.IsFalse(jefe.Vivo);

            // La gente de la zona (daño directo) sube el respeto 10 por cabeza y el HUD lo dice con el color del dueño.
            PandillaAPie grupo = Grupo(pandilla);
            Assert.IsNotNull(grupo, "Hay un grupo de " + pandilla);
            yield return Poner(jugador, grupo.Centro);
            yield return EsperarHasta(() => grupo.Presentes == grupo.Pandilleros.Length, "el grupo aparece", 10f);
            int antes = territorio.Respeto(zona);
            foreach (PandilleroAPie p in grupo.Pandilleros)
            {
                Assert.AreEqual(zona, territorio.ZonaEn(p.transform.position), "El grupo está dentro de su zona");
                Matar(p);
            }
            Assert.AreEqual(antes + grupo.Pandilleros.Length * ReglaTerritorio.PorVencido, territorio.Respeto(zona));
            yield return null;
            yield return null;
            StringAssert.Contains(nombreZona, hud.TextoZona.text);
            StringAssert.Contains("RESPETO 30", hud.TextoZona.text);
            Assert.AreEqual(FormatoTerritorio.ColorDe(pandilla), hud.TextoZona.color);

            // Los últimos vencidos hasta 70: sale el jefe con su aviso, con nombre y más vida que un pandillero.
            for (int i = 0; i < 4; i++)
            {
                territorio.RegistrarVencido(jefe.transform.position, pandilla, false);
            }
            Assert.AreEqual(70, territorio.Respeto(zona));
            yield return EsperarHasta(() => jefe.Vivo, "aparece el jefe de " + zona, 10f);
            Assert.AreEqual(nombreJefe, jefe.Jefe.Datos.Nombre);
            Assert.GreaterOrEqual(jefe.Jefe.Maxima, vidaMinimaJefe, "El jefe aguanta más que un pandillero");
            Assert.Greater(jefe.Jefe.Maxima, grupo.Datos.VidaMaxima * 3f);
            yield return null;
            StringAssert.Contains("EL JEFE DE " + nombreZona + " TE BUSCA", hud.TextoAviso.text);
            yield return Capturar(jugador, zona == "refineria" ? "T-428-1.png" : "T-428-0.png");

            // Vencer al jefe: la zona es tuya y el aviso y el HUD cambian.
            Matar(jefe.Jefe);
            yield return EsperarHasta(() => territorio.Dueno(zona) == IdPandilla.Jugador, "conquistar " + zona, 5f);
            Assert.AreEqual(100, territorio.Respeto(zona));
            yield return null;
            StringAssert.Contains(nombreZona + " ES DE", hud.TextoAviso.text);
            Assert.AreEqual(FormatoTerritorio.ColorDe(IdPandilla.Jugador), hud.TextoAviso.color);
            yield return Poner(jugador, jefe.transform.position);
            yield return null;
            Assert.AreEqual(FormatoTerritorio.ColorDe(IdPandilla.Jugador), hud.TextoZona.color, "El HUD pinta la zona del color de tu pandilla");
        }

        // Vista Game fija en 1920x1080 (API interna del editor, por reflexión; solo para las capturas).
        private static void FijarVistaGame(int ancho, int alto)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1" || Application.isBatchMode)
            {
                return;
            }
            const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            Type tipoTamanos = editor.GetType("UnityEditor.GameViewSizes");
            object tamanos = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(tipoTamanos).GetProperty("instance", Todo).GetValue(null);
            object grupoActual = tipoTamanos.GetProperty("currentGroupType", Todo).GetValue(tamanos);
            object grupo = tipoTamanos.GetMethod("GetGroup", Todo).Invoke(tamanos, new[] { grupoActual });
            Type tipoTamano = editor.GetType("UnityEditor.GameViewSize");
            Type tipoClase = editor.GetType("UnityEditor.GameViewSizeType");
            object tamano = tipoTamano.GetConstructor(new[] { tipoClase, typeof(int), typeof(int), typeof(string) })
                .Invoke(new[] { Enum.Parse(tipoClase, "FixedResolution"), ancho, alto, "T-428" });
            grupo.GetType().GetMethod("AddCustomSize", Todo).Invoke(grupo, new[] { tamano });
            int indice = (int)grupo.GetType().GetMethod("GetTotalCount", Todo).Invoke(grupo, null) - 1;
            Type tipoVista = editor.GetType("UnityEditor.GameView");
            UnityEditor.EditorWindow vista = UnityEditor.EditorWindow.GetWindow(tipoVista);
            tipoVista.GetMethod("SizeSelectionCallback", Todo).Invoke(vista, new object[] { indice, null });
        }

        [UnityTest]
        public IEnumerator NeonAltoYLaRefineriaSeConquistan()
        {
            FijarVistaGame(1920, 1080);
            yield return null;
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ServicioTerritorio>.Actual != null
                    && Registro<IObjetivos>.Actual != null;
            }, "el director arrancó", 30f);
            ServicioTerritorio territorio = Registro<ServicioTerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Libre, historia.Paso);

            // Cuatro zonas: SUBURBIOS tuya y tres rivales; la 4.ª recorta a NEÓN ALTO.
            Assert.AreEqual(4, territorio.Zonas.Count);
            Assert.AreEqual("LA REFINERÍA", territorio.Nombre("refineria"));
            Assert.AreEqual(IdPandilla.Nova, territorio.Dueno("refineria"));
            Assert.AreEqual("neon", territorio.ZonaEn(new Vector3(94f, 0f, 47f)));
            Assert.AreEqual("refineria", territorio.ZonaEn(new Vector3(94f, 0f, -47f)));
            Assert.AreEqual("NOVA", FormatoTerritorio.NombreDe(IdPandilla.Nova));
            Assert.AreNotEqual(FormatoTerritorio.ColorDe(IdPandilla.Nova), FormatoTerritorio.ColorDe(IdPandilla.Viboras));
            Assert.IsNotNull(Jefe("refineria"));

            // El mapa: la manzana de la zona nueva sale en el color de NOVA.
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto);
            yield return null;
            Assert.AreEqual(FormatoTerritorio.ColorDe(IdPandilla.Nova), mapa.Manzanas[5].color, "Manzana_6 es de NOVA en el mapa");
            Assert.AreEqual(FormatoTerritorio.ColorDe(IdPandilla.Viboras), mapa.Manzanas[2].color, "Manzana_3 es de los SNAKES");
            yield return Capturar(jugador, "T-428-2.png");
            yield return Pulsar(teclado.mKey);

            yield return Conquistar("neon", IdPandilla.Viboras, "NEÓN ALTO", "LA CULEBRA", 300f);
            yield return Conquistar("refineria", IdPandilla.Nova, "LA REFINERÍA", "CAPITÁN NOVA", 400f);

            // El mapa cambia de color y se guarda.
            yield return Pulsar(teclado.mKey);
            yield return null;
            yield return null;
            Color tuyo = FormatoTerritorio.ColorDe(IdPandilla.Jugador);
            Assert.AreEqual(tuyo, mapa.Manzanas[2].color, "NEÓN ALTO es tuya en el mapa");
            Assert.AreEqual(tuyo, mapa.Manzanas[5].color, "LA REFINERÍA es tuya en el mapa");
            Assert.AreEqual(FormatoTerritorio.ColorDe(IdPandilla.Calaveras), mapa.Manzanas[1].color, "EL TIANGUIS sigue siendo de los SKULLS");
            yield return Pulsar(teclado.mKey);
            string guardado = territorio.Guardar();
            territorio.Ceder("neon", IdPandilla.Viboras, 0);
            territorio.Ceder("refineria", IdPandilla.Nova, 0);
            Assert.AreEqual(IdPandilla.Nova, territorio.Dueno("refineria"));
            territorio.Cargar(guardado);
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("neon"));
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("refineria"));
            Assert.AreEqual(100, territorio.Respeto("refineria"));

            // T-427 la puede atacar: NOVA contra LA REFINERÍA.
            DirectorAtaques ataques = Object.FindFirstObjectByType<DirectorAtaques>();
            Assert.IsTrue(ataques.Atacar("refineria"));
            Assert.AreEqual("refineria", ataques.ZonaAtacada);
        }
    }
}
