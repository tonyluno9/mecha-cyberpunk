using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
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
    /// T-429: negocios en tus zonas como un jugador. Sin zona tuya no se toman; con ella se toman y mejoran con E pagando, al cambiar el día pagan Biits
    /// con aviso en el HUD, si pierdes la zona dejan de pagar, se listan en el mapa (M) y se guardan. Atajos: F6 salta la historia, Colocar mueve al
    /// jugador, Ceder/RegistrarVencido cambian el dueño, el reloj salta con AvanzarHasta. Con CAPTURAR_T429=1 y el editor abierto deja
    /// docs/capturas/T-429-1.png (mapa con negocios) y T-429-2.png (HUD sin encimar).
    /// </summary>
    public class NegociosPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T429";
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

        private static NegocioPunto Punto(string id)
        {
            foreach (NegocioPunto p in Object.FindObjectsByType<NegocioPunto>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.IdNegocio == id)
                {
                    return p;
                }
            }
            return null;
        }

        private static IEnumerator Poner(IJugador jugador, Vector3 punto)
        {
            jugador.Colocar(new Pose(punto + Vector3.up * 0.1f, Quaternion.identity));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // Salta el reloj a la próxima vez que marque esa hora (cruza una medianoche si ya pasó) y espera el aviso del día.
        private static IEnumerator PasarUnDia()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            int dia = reloj.Dia;
            reloj.AvanzarHasta(Mathf.Repeat(reloj.Hora - 1f, 24f));
            yield return null;
            Assert.Greater(reloj.Dia, dia, "El reloj cruzó la medianoche");
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
                .Invoke(new[] { Enum.Parse(tipoClase, "FixedResolution"), ancho, alto, "T-429" });
            grupo.GetType().GetMethod("AddCustomSize", Todo).Invoke(grupo, new[] { tamano });
            int indice = (int)grupo.GetType().GetMethod("GetTotalCount", Todo).Invoke(grupo, null) - 1;
            Type tipoVista = editor.GetType("UnityEditor.GameView");
            UnityEditor.EditorWindow vista = UnityEditor.EditorWindow.GetWindow(tipoVista);
            tipoVista.GetMethod("SizeSelectionCallback", Todo).Invoke(vista, new object[] { indice, null });
        }

        private static IEnumerator Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1" || Application.isBatchMode)
            {
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), foto.EncodeToPNG());
            Debug.Log("[T-429] Captura " + nombre + " de " + foto.width + "x" + foto.height);
            Object.Destroy(foto);
        }

        [UnityTest]
        public IEnumerator TomarMejorarCobrarYPerderUnNegocio()
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
                    && Registro<ICartera>.Actual != null && Registro<ServicioNegocios>.Actual != null && Registro<IRelojMundo>.Actual != null;
            }, "el director arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Libre, historia.Paso);
            ServicioNegocios negocios = Registro<ServicioNegocios>.Actual;
            ServicioTerritorio territorio = Registro<ServicioTerritorio>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.AreEqual(6, negocios.Cantidad);
            NegocioPunto puesto = Punto("puesto");
            NegocioPunto club = Punto("club");
            Assert.IsNotNull(puesto);
            Assert.IsNotNull(club);
            Assert.AreEqual("tianguis", territorio.ZonaEn(puesto.Posicion), "El puesto está dentro de su zona");
            Assert.AreEqual("neon", territorio.ZonaEn(club.Posicion), "El club está dentro de su zona");
            Assert.AreEqual("refineria", territorio.ZonaEn(Punto("taller").Posicion));
            Assert.AreEqual("fosa", territorio.ZonaEn(Punto("tiendita").Posicion));
            Assert.Greater(Vector3.Distance(puesto.Posicion, Punto("fonda").Posicion), 5f, "Los dos negocios del Tianguis no están encimados");

            // Con la zona ajena no se puede tomar.
            territorio.Ceder("tianguis", IdPandilla.Calaveras, 0);
            cartera.Abonar(5000, MotivoDinero.Premio);
            Assert.IsFalse(puesto.PuedeUsar);
            StringAssert.Contains("CONQUISTA LA ZONA", puesto.Texto);
            Assert.IsFalse(negocios.Comprar("puesto"));
            Assert.AreEqual(0, negocios.Nivel("puesto"));

            // Con la zona tuya: tomar, mejorar dos veces y llegar al máximo, pagando cada vez.
            territorio.Ceder("tianguis", IdPandilla.Jugador, 100);
            Assert.IsTrue(puesto.PuedeUsar);
            StringAssert.StartsWith("TOMAR", puesto.Texto);
            int dinero = cartera.Dinero;
            int precio1 = negocios.PrecioSiguiente("puesto");
            puesto.Usar();
            Assert.AreEqual(1, negocios.Nivel("puesto"));
            Assert.AreEqual(dinero - precio1, cartera.Dinero);
            int ingreso1 = negocios.IngresoDia("puesto");
            Assert.Greater(ingreso1, 0);
            StringAssert.StartsWith("MEJORAR", puesto.Texto);
            puesto.Usar();
            puesto.Usar();
            Assert.AreEqual(3, negocios.Nivel("puesto"));
            Assert.Greater(negocios.IngresoDia("puesto"), ingreso1, "Mejorar rinde más");
            Assert.IsFalse(puesto.PuedeUsar, "Al máximo ya no hay más");
            StringAssert.Contains("MÁX", puesto.Texto);
            Assert.IsFalse(negocios.Comprar("puesto"));

            // Sin dinero no se toma.
            cartera.CargarDeuda(cartera.Dinero + 1, MotivoDinero.Otro);
            territorio.Ceder("neon", IdPandilla.Jugador, 100);
            Assert.IsFalse(negocios.Comprar("club"));
            Assert.AreEqual(0, negocios.Nivel("club"));
            cartera.Abonar(5000, MotivoDinero.Premio);
            Assert.IsTrue(negocios.Comprar("club"));

            // Al cambiar el día pagan, con aviso en el HUD.
            HudTerritorio hud = Object.FindFirstObjectByType<HudTerritorio>();
            int esperado = negocios.TotalPorDia;
            Assert.AreEqual(negocios.IngresoDia("puesto") + negocios.IngresoDia("club"), esperado);
            dinero = cartera.Dinero;
            yield return PasarUnDia();
            Assert.AreEqual(dinero + esperado, cartera.Dinero, "El día pagó el ingreso de los dos negocios");
            yield return null;
            StringAssert.Contains("DE TUS NEGOCIOS", hud.TextoAviso.text);
            StringAssert.Contains(Moneda.Texto(esperado), hud.TextoAviso.text);

            // El mapa los lista con nivel e ingreso.
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto);
            yield return null;
            StringAssert.Contains(Moneda.Texto(esperado), mapa.EncabezadoNegocios.text);
            Assert.AreEqual("PUESTO DEL TIANGUIS · NIV 3 · " + Moneda.Texto(negocios.IngresoDia("puesto")) + "/DÍA", mapa.TextosNegocios[1].text);
            StringAssert.Contains("CLUB NEÓN · NIV 1", mapa.TextosNegocios[3].text);
            StringAssert.Contains("SIN TOMAR", mapa.TextosNegocios[0].text);
            yield return Capturar("T-429-1.png");
            yield return Pulsar(teclado.mKey);

            // Se guarda.
            string guardado = negocios.Guardar();
            negocios.Cargar("{\"ids\":[],\"niveles\":[]}");
            Assert.AreEqual(0, negocios.Nivel("puesto"));
            negocios.Cargar(guardado);
            Assert.AreEqual(3, negocios.Nivel("puesto"));
            Assert.AreEqual(1, negocios.Nivel("club"));

            // Si pierdes la zona, ese negocio deja de pagar (el otro sigue) y vuelve al recuperarla.
            territorio.Ceder("tianguis", IdPandilla.Viboras, 30);
            Assert.AreEqual(0, negocios.IngresoDia("puesto"));
            StringAssert.Contains("CONQUISTA LA ZONA", puesto.Texto);
            dinero = cartera.Dinero;
            int solo = negocios.IngresoDia("club");
            yield return PasarUnDia();
            Assert.AreEqual(dinero + solo, cartera.Dinero, "Solo paga el club");
            yield return null;
            yield return Pulsar(teclado.mKey);
            yield return null;
            StringAssert.Contains("ZONA PERDIDA", mapa.TextosNegocios[1].text);
            yield return Pulsar(teclado.mKey);
            territorio.Ceder("tianguis", IdPandilla.Jugador, 100);
            Assert.Greater(negocios.IngresoDia("puesto"), 0);

            // El HUD: el aviso "ES DE" queda debajo de la fila de la zona, sin encimarse (T-429).
            Vector3 enNeon = club.Posicion;
            territorio.Ceder("neon", IdPandilla.Viboras, 100);
            yield return Poner(jugador, enNeon);
            territorio.RegistrarVencido(enNeon, IdPandilla.Viboras, true);
            yield return null;
            yield return null;
            StringAssert.Contains("ES DE", hud.TextoAviso.text);
            Assert.IsNotEmpty(hud.TextoZona.text);
            Vector3[] zona = new Vector3[4];
            Vector3[] aviso = new Vector3[4];
            hud.TextoZona.rectTransform.GetWorldCorners(zona);
            hud.TextoAviso.rectTransform.GetWorldCorners(aviso);
            Assert.GreaterOrEqual(zona[1].y, aviso[1].y - 0.01f, "El aviso (arriba) no baja encima de la zona: la parte de arriba del aviso queda bajo la zona");
            Assert.LessOrEqual(aviso[1].y, zona[0].y + 0.01f, "El aviso empieza debajo de la fila de la zona");
            yield return Capturar("T-429-2.png");
        }
    }
}
