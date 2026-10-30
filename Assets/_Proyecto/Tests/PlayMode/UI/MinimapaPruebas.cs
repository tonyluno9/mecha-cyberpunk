using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-409 en Ciudad_Prologo, en la calle del Tianguis con un pandillero a 30 m y el CHATO reclutado: el ratón gira la cámara 90° y el
    /// minimapa gira lo mismo con la flecha arriba; el pandillero sale en rojo en su lugar y a 80 m no sale; el aliado sale naranja; M abre el
    /// mapa con su leyenda, la rueda hace zoom de 1x a 3x en el cursor, un clic marca un punto y otro lo quita; M cierra; el minimapa muestra el
    /// punto con su distancia y W lo acerca. Atajos: el jugador y el pandillero se colocan por código y los grupos de pandilleros se apagan
    /// para que nadie dispare. Con CAPTURAR_T409=1 guarda docs/capturas/T-409-1 (minimapa) y T-409-2 (mapa) en 1080p y 1440p.
    /// </summary>
    public class MinimapaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T409";
        private const string NombreAliado = "EL CHATO";
        private static readonly Vector3 Inicio = new Vector3(-20f, 0.5f, 0f);
        private const float DeltaGiro = 30f;
        private const float ToleranciaGrados = 2f;
        private const float ToleranciaPx = 3f;
        private const float DistanciaCerca = 30f;
        private const float DistanciaLejos = 80f;
        private const float DistanciaPunto = 40f;
        private const float RuedaArriba = 120f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            PuntoMarcadoMapa.Quitar();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
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
            yield return null;
        }

        private IEnumerator Clic(Vector2 pantalla)
        {
            Set(raton.position, pantalla);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        private IEnumerator Rueda(float cuanto)
        {
            Set(raton.scroll, new Vector2(0f, cuanto));
            yield return null;
            Set(raton.scroll, Vector2.zero);
            yield return null;
        }

        private static void Capturar(string nombre, Camera camara)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/" + nombre + ".png", camara);
            CapturaUI.Capturar2K("docs/capturas/" + nombre + "-1440.png", camara);
        }

        private static void AssertColor(Color esperado, Color real, string que)
        {
            Assert.AreEqual(esperado.r, real.r, 0.02f, que + " (r)");
            Assert.AreEqual(esperado.g, real.g, 0.02f, que + " (g)");
            Assert.AreEqual(esperado.b, real.b, 0.02f, que + " (b)");
        }

        private static void Poner(PandilleroAPie pandillero, Vector3 donde)
        {
            pandillero.gameObject.SetActive(true);
            pandillero.Configurar(donde, null);
            pandillero.Reaparecer();
        }

        // Dónde debe salir en el minimapa un punto del mundo.
        private static Vector2 Esperado(Minimapa minimapa, IJugador jugador, Vector3 punto)
        {
            CalculoMinimapa.Ubicar(jugador.Raiz.position, jugador.Camara.transform.eulerAngles.y, punto, minimapa.Escala, minimapa.RadioPx, false, out Vector2 px);
            return px;
        }

        [UnityTest]
        public IEnumerator RecorridoMapaYPunto()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<IPandillaJugador>.Actual != null;
            }, "el director arrancó", 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador.Camara;
            Minimapa minimapa = UnityEngine.Object.FindFirstObjectByType<Minimapa>();
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(minimapa, "La ciudad trae el minimapa");
            Assert.IsNotNull(mapa);
            Assert.IsTrue(minimapa.Visible, "A pie se ve el minimapa");
            Assert.AreEqual(Minimapa.OrdenCanvas, minimapa.GetComponentInChildren<Canvas>().sortingOrder);
            Assert.AreEqual(4, CuantosLugares(), "Están las marcas de El Mercado, I+D+I, el CIC y la casa");

            // Un pandillero suelto (fuera de su grupo) y los grupos apagados, para que nadie dispare durante la prueba.
            PandilleroAPie pandillero = null;
            foreach (PandilleroAPie p in UnityEngine.Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!p.EsJefe && p.Datos != null && !p.Datos.UsaPistola)
                {
                    pandillero = p;
                }
            }
            Assert.IsNotNull(pandillero, "Hay un pandillero en la ciudad");
            pandillero.transform.SetParent(null, true);
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            pandillero.gameObject.SetActive(false);

            // En la calle del Tianguis mirando al este.
            jugador.Colocar(new Pose(Inicio, Quaternion.LookRotation(Vector3.right)));
            yield return null;
            yield return null;

            // a. El ratón gira la cámara 90°: el dibujo gira lo mismo y la flecha sigue arriba.
            minimapa.Actualizar();
            float rumboAntes = camara.transform.eulerAngles.y;
            float giroAntes = minimapa.Mundo.localEulerAngles.z;
            float finGiro = Time.realtimeSinceStartup + 5f;
            while (Mathf.Abs(Mathf.DeltaAngle(rumboAntes, camara.transform.eulerAngles.y)) < 90f && Time.realtimeSinceStartup < finGiro)
            {
                Set(raton.delta, new Vector2(DeltaGiro, 0f));
                yield return null;
            }
            Set(raton.delta, Vector2.zero);
            yield return null;
            yield return null;
            minimapa.Actualizar();
            float giroCamara = Mathf.DeltaAngle(rumboAntes, camara.transform.eulerAngles.y);
            float giroMinimapa = Mathf.DeltaAngle(giroAntes, minimapa.Mundo.localEulerAngles.z);
            Assert.GreaterOrEqual(Mathf.Abs(giroCamara), 88f, "La cámara giró unos 90°");
            Assert.AreEqual(giroCamara, giroMinimapa, ToleranciaGrados, "El minimapa gira lo mismo que la cámara");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, minimapa.Flecha.localEulerAngles.z), 0.01f, "La flecha sigue apuntando arriba");

            // b. Mirando otra vez al este: el pandillero a 30 m sale en rojo en su lugar; a 80 m no sale.
            jugador.Colocar(new Pose(Inicio, Quaternion.LookRotation(Vector3.right)));
            yield return null;
            Vector3 cerca = Inicio + new Vector3(18f, 0f, -24f);
            Poner(pandillero, cerca);
            yield return null;
            minimapa.Actualizar();
            Image rojo = minimapa.MarcaDe(pandillero);
            Assert.IsNotNull(rojo, "El pandillero a 30 m sale en el minimapa");
            AssertColor(KitUI.Estilo.Enemigo, rojo.color, "el enemigo va en #FF3333");
            Assert.Less(Vector2.Distance(Esperado(minimapa, jugador, pandillero.transform.position), rojo.rectTransform.anchoredPosition), ToleranciaPx,
                "Sale en su lugar");
            Assert.Greater(rojo.rectTransform.anchoredPosition.x, 0f, "Al sur, mirando al este, queda a la derecha");
            Assert.Greater(rojo.rectTransform.anchoredPosition.y, 0f, "Y al este, arriba");
            Poner(pandillero, Inicio + Vector3.forward * DistanciaLejos);
            yield return null;
            minimapa.Actualizar();
            Assert.IsNull(minimapa.MarcaDe(pandillero), "A 80 m no sale");
            pandillero.gameObject.SetActive(false);

            // c. El CHATO se une: sale del color de tu pandilla (naranja por defecto).
            Assert.IsTrue(((PandillaJugador)Registro<IPandillaJugador>.Actual).Reclutar(NombreAliado));
            yield return EsperarHasta(() => AliadoAPie.Activos.Count > 0, "aparece el aliado", 10f);
            AliadoAPie chato = AliadoAPie.Activos[0];
            yield return EsperarHasta(() => minimapa.AliadosVisibles > 0
                || CalculoMinimapa.DistanciaPlana(chato.transform.position, jugador.Raiz.position) > minimapa.RadioMetros, "el aliado en el minimapa", 5f);
            if (CalculoMinimapa.DistanciaPlana(chato.transform.position, jugador.Raiz.position) > minimapa.RadioMetros)
            {
                // Apareció lejos: se lo acerca para la prueba.
                chato.transform.position = Inicio + Vector3.back * 4f;
                yield return null;
                minimapa.Actualizar();
            }
            Assert.Greater(minimapa.AliadosVisibles, 0, "El aliado sale en el minimapa");
            for (int i = 0; i < minimapa.AliadosCreados; i++)
            {
                if (minimapa.Aliado(i).gameObject.activeSelf)
                {
                    AssertColor(ColoresPandilla.Color(ColoresPandilla.Predeterminado), minimapa.Aliado(i).color, "el aliado va naranja");
                }
            }

            // Captura 1: el minimapa en el Tianguis con un enemigo y el aliado.
            Poner(pandillero, cerca);
            yield return null;
            minimapa.Actualizar();
            Capturar("T-409-1", camara);
            pandillero.gameObject.SetActive(false);

            // d. M abre el mapa: leyenda y cursor visible.
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto, "M abre el mapa");
            Assert.IsTrue(PantallaMapa.HayAbierto);
            Assert.AreEqual(PantallaMapa.NombreCiudad, mapa.Titulo.text);
            Assert.IsTrue(Cursor.visible, "Con el mapa abierto se ve el cursor");
            string leyenda = string.Join("|", ListaLeyenda(mapa));
            foreach (string etiqueta in PantallaMapa.EtiquetasFijasLeyenda)
            {
                StringAssert.Contains(etiqueta, leyenda);
            }
            StringAssert.Contains(ColoresPandilla.NombrePredeterminado, leyenda, "La leyenda trae el nombre de tu pandilla");
            foreach (IMarcaMapa lugar in Registro<IMarcaMapa>.Todos)
            {
                Assert.IsNotNull(mapa.MarcaDe(lugar), "El mapa siempre muestra " + lugar.Tipo);
            }

            // e. La rueda: zoom en el cursor, entre 1x y 3x.
            Assert.AreEqual(1f, mapa.Zoom, 0.001f);
            Vector3 bajoCursor = new Vector3(40f, 0f, 20f);
            Vector2 antes = mapa.PantallaDe(bajoCursor);
            Set(raton.position, antes);
            yield return null;
            yield return Rueda(RuedaArriba);
            Assert.Greater(mapa.Zoom, 1f, "La rueda hacia arriba acerca");
            Assert.Less(Vector2.Distance(antes, mapa.PantallaDe(bajoCursor)), 2f, "El punto bajo el cursor se queda bajo el cursor");
            for (int i = 0; i < 12; i++)
            {
                yield return Rueda(RuedaArriba);
            }
            Assert.AreEqual(PantallaMapa.ZoomMaximo, mapa.Zoom, 0.001f, "Hasta 3x");
            for (int i = 0; i < 12; i++)
            {
                yield return Rueda(-RuedaArriba);
            }
            Assert.AreEqual(PantallaMapa.ZoomMinimo, mapa.Zoom, 0.001f, "Y de regreso a 1x");

            // f. Un clic marca un punto 40 m al este; otro clic sobre él lo quita; otro lo vuelve a poner.
            Vector3 punto = Inicio + Vector3.right * DistanciaPunto;
            yield return Clic(mapa.PantallaDe(punto));
            Assert.IsTrue(PuntoMarcadoMapa.Hay, "El clic marca un punto");
            Assert.Less(CalculoMinimapa.DistanciaPlana(punto, PuntoMarcadoMapa.Posicion), 1f, "Donde se hizo el clic");
            yield return Clic(mapa.PantallaDe(punto));
            Assert.IsFalse(PuntoMarcadoMapa.Hay, "Otro clic sobre él lo quita");
            yield return Clic(mapa.PantallaDe(punto));
            Assert.IsTrue(PuntoMarcadoMapa.Hay);
            Assert.IsTrue(mapa.PuntoMarcado.gameObject.activeSelf, "El mapa dibuja el punto");

            // Captura 2: el mapa con su leyenda, el punto marcado, el aliado y los iconos.
            yield return null;
            Capturar("T-409-2", camara);

            // g. M cierra; el minimapa muestra el punto con su distancia y W lo acerca.
            yield return Pulsar(teclado.mKey);
            Assert.IsFalse(mapa.Abierto, "M cierra el mapa");
            minimapa.Actualizar();
            Assert.IsTrue(minimapa.PuntoVisible, "El punto sale en el minimapa");
            float distancia = CalculoMinimapa.DistanciaPlana(jugador.Raiz.position, PuntoMarcadoMapa.Posicion);
            Assert.AreEqual(CalculoFlechaObjetivo.TextoDistancia(distancia), minimapa.TextoPunto.text);
            StringAssert.EndsWith(" m", minimapa.TextoPunto.text);
            Assert.Less(Vector2.Distance(Esperado(minimapa, jugador, PuntoMarcadoMapa.Posicion), minimapa.PuntoMarcado.anchoredPosition), ToleranciaPx,
                "El punto está en su lugar del minimapa");
            Press(teclado.wKey);
            yield return new WaitForSeconds(1.5f);
            Release(teclado.wKey);
            yield return null;
            minimapa.Actualizar();
            float despues = CalculoMinimapa.DistanciaPlana(jugador.Raiz.position, PuntoMarcadoMapa.Posicion);
            Assert.Less(despues, distancia - 2f, "W acerca el punto");
            Assert.AreEqual(CalculoFlechaObjetivo.TextoDistancia(despues), minimapa.TextoPunto.text, "La distancia baja en el minimapa");
        }

        private static int CuantosLugares()
        {
            int cuantos = 0;
            foreach (IMarcaMapa marca in Registro<IMarcaMapa>.Todos)
            {
                if (marca is MarcaLugar)
                {
                    cuantos++;
                }
            }
            return cuantos;
        }

        private static string[] ListaLeyenda(PantallaMapa mapa)
        {
            string[] textos = new string[mapa.TextosLeyenda.Count];
            for (int i = 0; i < textos.Length; i++)
            {
                textos[i] = mapa.TextosLeyenda[i].text;
            }
            return textos;
        }
    }
}
