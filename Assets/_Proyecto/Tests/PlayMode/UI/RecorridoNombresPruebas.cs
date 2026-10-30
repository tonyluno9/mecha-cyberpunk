using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.Tiendas;
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
    /// T-414 en Ciudad_Prologo real, como un jugador: lee el letrero de El Mercado, entra con W, abre la tienda con E (EL MERCADO, precios en Ƀ),
    /// camina por la calle del Tianguis (el HUD dice SKULLS), abre el mapa con M (MEGALOPOLIS) y mira El Fox. Con CAPTURAR_T414=1 guarda las capturas.
    /// </summary>
    public class RecorridoNombresPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T414";
        private const int DineroInicial = 400;
        private const float DistanciaPuerta = 5f;
        private const float DistanciaFox = 8f;
        private const float AlturaOjos = 0.1f;
        private const float XSuburbios = -80f;
        private const float XTianguis = 0f;
        private const float AlcanceLetrero = 14f;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
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

        private static Pose Mirando(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = hacia - desde;
            dir.y = 0f;
            return new Pose(desde, Quaternion.LookRotation(dir.normalized, Vector3.up));
        }

        private static Transform Buscar(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        // Un letrero del mundo (Text en canvas World Space o TextMesh) cuya primera línea es ese texto, sin importar mayúsculas;
        // si se da un punto, a menos de AlcanceLetrero de él.
        private static bool HayLetrero(string texto, Vector3? cerca)
        {
            foreach (Text t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.canvas != null && t.canvas.renderMode == RenderMode.WorldSpace && Coincide(t.text, texto, t.transform.position, cerca))
                {
                    return true;
                }
            }
            foreach (TextMesh t in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (Coincide(t.text, texto, t.transform.position, cerca))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Coincide(string contenido, string texto, Vector3 posicion, Vector3? cerca)
        {
            return string.Equals(contenido.Split('\n')[0].Trim(), texto, StringComparison.OrdinalIgnoreCase)
                && (cerca == null || (posicion - cerca.Value).magnitude < AlcanceLetrero);
        }

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/" + nombre, Registro<IJugador>.Actual.Camara);
        }

        [UnityTest]
        public IEnumerator RecorridoNombres()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null && Registro<ICartera>.Actual != null,
                "jugador, territorio y cartera", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            yield return new WaitForSecondsRealtime(1f);
            Registro<ICartera>.Actual.Abonar(DineroInicial, MotivoDinero.Otro);

            // a. Frente a la puerta de El Mercado: el letrero lo dice.
            Transform lugar = Buscar("Lugar_MercadoNegro");
            Assert.IsNotNull(lugar);
            Transform puerta = null;
            foreach (Transform t in lugar.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Puerta")
                {
                    puerta = t;
                }
            }
            Assert.IsNotNull(puerta, "El mercado tiene puerta.");
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in UnityEngine.Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                if (m.name.Contains("MercadoNegro"))
                {
                    mostrador = m;
                }
            }
            Assert.IsNotNull(mostrador);
            Vector3 haciaAdentro = lugar.position - puerta.position;
            haciaAdentro.y = 0f;
            haciaAdentro.Normalize();
            Vector3 inicio = puerta.position - haciaAdentro * DistanciaPuerta;
            inicio.y = puerta.position.y + AlturaOjos;
            jugador.Colocar(Mirando(inicio, puerta.position));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(HayLetrero("El Mercado", puerta.position), "El letrero de la puerta dice El Mercado.");
            Assert.IsFalse(HayLetrero("Mercado Negro", null), "Ya no dice Mercado Negro.");

            // Entra con W hasta el mostrador y abre la tienda con E.
            Press(teclado.wKey);
            float fin = Time.realtimeSinceStartup + 40f;
            while ((mostrador.transform.position - jugador.Raiz.position).sqrMagnitude > 2.2f * 2.2f && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return new WaitForSecondsRealtime(0.3f);
            PantallaTienda pantalla = UnityEngine.Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => mostrador.PantallaAbierta && pantalla.Visible, "abrir la tienda con E", 5f);
            Assert.AreEqual("EL MERCADO", pantalla.Titulo);
            int fila = -1;
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoFila(i).ToUpperInvariant().Contains("ESCOPETA"))
                {
                    fila = i;
                }
            }
            Assert.GreaterOrEqual(fila, 0, "La tienda ofrece la escopeta.");
            StringAssert.Contains(Moneda.Texto(350), pantalla.TextoFila(fila), "El precio lleva Ƀ.");
            yield return new WaitForSecondsRealtime(0.3f);
            Capturar("T-414-2.png");
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !mostrador.PantallaAbierta, "cerrar la tienda con Esc", 5f);

            // b. Las otras puertas: I+D+I, El Fox y VivaCoin; los modelos viejos están apagados.
            Assert.IsTrue(HayLetrero("I+D+I", null), "Hay un letrero I+D+I.");
            Assert.IsTrue(HayLetrero("El Fox", null), "Hay un letrero El Fox.");
            Assert.IsTrue(HayLetrero("VivaCoin", null), "Hay un letrero VivaCoin.");
            Assert.IsFalse(Buscar("Letrero_Cantina").gameObject.activeInHierarchy, "El modelo viejo de la cantina está apagado.");
            Assert.IsFalse(Buscar("Letrero_Casino").gameObject.activeInHierarchy, "El modelo viejo del casino está apagado.");

            // c. Tu zona dice SUBURBIOS; la calle del Tianguis, SKULLS.
            HudTerritorio hud = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            Assert.IsNotNull(hud);
            jugador.Colocar(new Pose(new Vector3(XSuburbios, 0.5f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return EsperarHasta(() => hud.TextoZona.text.Contains("SUBURBIOS"), "el HUD dice SUBURBIOS", 20f);
            StringAssert.Contains("SUBURBIOS · LOS DE SUBURBIOS · RESPETO", hud.TextoZona.text);
            jugador.Colocar(new Pose(new Vector3(XTianguis, 0.5f, 0f), Quaternion.LookRotation(Vector3.right)));
            Press(teclado.wKey);
            yield return EsperarHasta(() => hud.TextoZona.text.Contains("EL TIANGUIS"), "el HUD dice EL TIANGUIS", 20f);
            Release(teclado.wKey);
            StringAssert.Contains("EL TIANGUIS · SKULLS · RESPETO", hud.TextoZona.text);

            // d. El mapa: M abre (MEGALOPOLIS, SUBURBIOS) y M cierra.
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(mapa);
            yield return Pulsar(teclado.mKey);
            yield return EsperarHasta(() => mapa.Abierto, "abrir el mapa con M", 5f);
            Assert.AreEqual("MEGALOPOLIS", mapa.Titulo.text);
            bool haySuburbios = false;
            foreach (Text etiqueta in mapa.Etiquetas)
            {
                haySuburbios |= etiqueta.text.Contains("SUBURBIOS");
            }
            Assert.IsTrue(haySuburbios, "El mapa muestra SUBURBIOS.");
            yield return Pulsar(teclado.mKey);
            yield return EsperarHasta(() => !mapa.Abierto, "cerrar el mapa con M", 5f);

            // e. Frente a El Fox: captura 1 (letrero y HUD con Ƀ).
            Transform neon = Buscar("LetreroElFox");
            Assert.IsNotNull(neon, "Está el letrero de neón de El Fox.");
            Vector3 afuera = -neon.forward;
            afuera.y = 0f;
            Vector3 frente = neon.position + afuera.normalized * DistanciaFox;
            frente.y = Buscar("Lugar_Bar").position.y + AlturaOjos;
            jugador.Colocar(Mirando(frente, neon.position));
            yield return new WaitForSecondsRealtime(0.8f);
            Capturar("T-414-1.png");
        }
    }
}
