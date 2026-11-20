using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1492 con la entrada simulada en Ciudad_Prologo: E y C usan las dos ranuras de consumibles (E solo si no hay nada con qué interactuar), Alt + C cambia
    /// el objeto de la ranura 2, y en la clínica un clic con el ratón elige el implante y el doble clic lo instala sin Enter.
    /// </summary>
    public class ConsumiblesClinicaT1492Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float SegundosCiudad = 30f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            EquipoRueda.Reiniciar();
            RanurasConsumibles.Reiniciar();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            Pausa.Fijar(false);
            EquipoRueda.Reiniciar();
            RanurasConsumibles.Reiniciar();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            CatalogoDestinosMapa.Limpiar();
            yield return null;
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            yield return null;
            Release(tecla);
            yield return null;
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

        private IEnumerator CargarCiudad()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null
                    && Registro<ServicioInventario>.Actual != null && Registro<ServicioImplantes>.Actual != null;
            }, "la escena arrancó", SegundosCiudad);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        private static DatosConsumible Consumible(string id)
        {
            DatosObjeto objeto = Registro<ServicioInventario>.Actual.Catalogo.Buscar(id);
            Assert.IsNotNull(objeto as DatosConsumible, "El catálogo trae " + id);
            return (DatosConsumible)objeto;
        }

        // Una bebida (siempre sirve) y las papitas (curan: piden vida incompleta). Se hiere al jugador para que las dos sirvan.
        private static void PrepararConsumibles(out DatosConsumible bebida, out DatosConsumible papitas)
        {
            bebida = Consumible("bebida_energetica");
            papitas = Consumible("papitas");
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            inventario.Agregar(bebida, 3);
            inventario.Agregar(papitas, 3);
            VidaAPie vida = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<VidaAPie>();
            vida.RecibirDanioSoloVida(new InfoDanio(40f, TipoDanio.Disparo, vida.transform.position, Vector3.forward, null));
        }

        [UnityTest]
        public IEnumerator C_UsaLaRanura2_YElHudMuestraLasDosConSuTecla()
        {
            yield return CargarCiudad();
            PrepararConsumibles(out DatosConsumible bebida, out DatosConsumible papitas);
            yield return new WaitForSecondsRealtime(0.5f);
            HudConsumibles hud = Object.FindFirstObjectByType<HudConsumibles>();
            Assert.IsNotNull(hud);
            Assert.AreEqual(bebida, hud.Equipado(0), "Ranura 1: el primero del catálogo");
            Assert.AreEqual(papitas, hud.Equipado(1), "Ranura 2: el segundo");
            Assert.AreEqual(string.Empty, hud.TextoConsumo.text, "T-1529: sin línea de texto repetida encima de las casillas");
            HudH10 h10 = Object.FindFirstObjectByType<HudH10>();
            Assert.AreEqual("E", h10.TeclaRanura(0));
            Assert.AreEqual("C", h10.TeclaRanura(1));
            Assert.AreEqual(string.Empty, h10.CantidadRanura(1), "T-1529: la casilla no escribe la cantidad");

            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            int antes = inventario.Cantidad(papitas);
            yield return Pulsar(teclado.cKey);
            Assert.AreEqual(antes - 1, inventario.Cantidad(papitas), "C usa el objeto de la ranura 2");
        }

        [UnityTest]
        public IEnumerator E_UsaLaRanura1_SoloSiNoHayNadaConQueInteractuar()
        {
            yield return CargarCiudad();
            PrepararConsumibles(out DatosConsumible bebida, out _);
            yield return new WaitForSecondsRealtime(0.5f);
            InteraccionAPie interaccion = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<InteraccionAPie>();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            bool habiaAlgo = interaccion.TipoCandidato != TipoCandidatoAPie.Ninguno;
            int antes = inventario.Cantidad(bebida);
            yield return Pulsar(teclado.eKey);
            if (habiaAlgo)
            {
                Assert.AreEqual(antes, inventario.Cantidad(bebida), "Con algo enfrente, E interactúa y no gasta el objeto");
            }
            else
            {
                Assert.AreEqual(antes - 1, inventario.Cantidad(bebida), "Sin nada enfrente, E usa el objeto de la ranura 1");
            }
        }

        [UnityTest]
        public IEnumerator AltC_CambiaElObjetoDeLaRanura2_SinGastarlo()
        {
            yield return CargarCiudad();
            PrepararConsumibles(out DatosConsumible bebida, out DatosConsumible papitas);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosConsumible tercero = null;
            foreach (DatosObjeto objeto in inventario.Catalogo.Objetos)
            {
                if (objeto is DatosConsumible c && c != bebida && c != papitas)
                {
                    tercero = c;
                    break;
                }
            }
            Assert.IsNotNull(tercero, "Hay un tercer consumible en el catálogo");
            inventario.Agregar(tercero, 2);
            yield return new WaitForSecondsRealtime(0.5f);
            HudConsumibles hud = Object.FindFirstObjectByType<HudConsumibles>();
            Assert.AreEqual(papitas, hud.Equipado(1));
            int antes = inventario.Cantidad(papitas);

            Press(teclado.leftAltKey);
            yield return null;
            yield return Pulsar(teclado.cKey);
            Release(teclado.leftAltKey);
            yield return null;
            yield return null;

            Assert.AreEqual(tercero, hud.Equipado(1), "Alt + C pasa al siguiente objeto que lleva");
            Assert.AreEqual(bebida, hud.Equipado(0), "La ranura 1 no cambia");
            Assert.AreEqual(antes, inventario.Cantidad(papitas), "Cambiar de objeto no gasta nada");
        }

        // T-1529: bebida y luego papitas. Al gastarse la bebida (ranura 1), las papitas siguen en la ranura 2 y la tecla C las usa.
        [UnityTest]
        public IEnumerator BebidaYLuegoPapitas_ElSegundoConsumibleSiSeUsa()
        {
            yield return CargarCiudad();
            DatosConsumible bebida = Consumible("bebida_energetica");
            DatosConsumible papitas = Consumible("papitas");
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            inventario.Agregar(bebida, 1);
            inventario.Agregar(papitas, 3);
            VidaAPie vida = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<VidaAPie>();
            vida.RecibirDanioSoloVida(new InfoDanio(60f, TipoDanio.Disparo, vida.transform.position, Vector3.forward, null));
            yield return new WaitForSecondsRealtime(0.5f);
            HudConsumibles hud = Object.FindFirstObjectByType<HudConsumibles>();
            Assert.AreEqual(bebida, hud.Equipado(0));
            Assert.AreEqual(papitas, hud.Equipado(1));

            // La bebida (ranura 1, tecla E) se gasta: se usa con el componente para no depender de que no haya nada enfrente.
            ConsumiblesJugador consumibles = Registro<IJugador>.Actual.Raiz.GetComponent<ConsumiblesJugador>();
            Assert.IsTrue(consumibles.Usar(bebida), "La bebida se usa");
            Assert.AreEqual(0, inventario.Cantidad(bebida), "Era la única");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsNull(hud.Equipado(0), "La ranura 1 queda vacía");
            Assert.AreEqual(papitas, hud.Equipado(1), "Las papitas siguen en la ranura 2 (tecla C)");

            // 60 de daño menos 25 de la bebida: la vida sigue incompleta y las papitas sirven.
            int antes = inventario.Cantidad(papitas);
            Assert.Less(vida.Actual, vida.Maxima, "La vida sigue incompleta");
            yield return Pulsar(teclado.cKey);
            Assert.AreEqual(antes - 1, inventario.Cantidad(papitas), "C usa las papitas después de la bebida");
        }

        // T-1529: cada implante instalado suma vida máxima (+10 nivel I, +25 nivel II), se ve en la vida del jugador y se conserva al guardar y cargar.
        [UnityTest]
        public IEnumerator Implantes_SumanVidaMaxima_YSeConservanAlGuardarYCargar()
        {
            yield return CargarCiudad();
            ServicioImplantes implantes = Registro<ServicioImplantes>.Actual;
            VidaAPie vida = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<VidaAPie>();
            implantes.QuitarTodos();
            yield return null;
            float baseMax = vida.Maxima;
            Assert.AreEqual(0f, vida.VidaMaximaPorImplantes, 1e-4f);
            var piernas = UnityEditor.AssetDatabase.LoadAssetAtPath<DatosImplante>("Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_piernas_1.asset");
            var ojos = UnityEditor.AssetDatabase.LoadAssetAtPath<DatosImplante>("Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_ojos_2.asset");
            Assert.IsNotNull(piernas);
            Assert.IsNotNull(ojos);
            float actualAntes = vida.Actual;

            implantes.Regalar(piernas);
            yield return null;
            Assert.AreEqual(baseMax + 10f, vida.Maxima, 1e-3f, "Nivel I suma 10");
            Assert.AreEqual(actualAntes + 10f, vida.Actual, 1e-3f, "La vida que se gana se llena");
            implantes.Regalar(ojos);
            yield return null;
            Assert.AreEqual(baseMax + 35f, vida.Maxima, 1e-3f, "10 + 25");

            string guardado = implantes.Guardar();
            implantes.QuitarTodos();
            yield return null;
            Assert.AreEqual(baseMax, vida.Maxima, 1e-3f, "Sin implantes vuelve la base");
            implantes.Cargar(guardado);
            yield return null;
            Assert.AreEqual(baseMax + 35f, vida.Maxima, 1e-3f, "Cargar la partida devuelve la vida máxima");
            Assert.LessOrEqual(vida.Actual, vida.Maxima);
        }

        [UnityTest]
        public IEnumerator Clinica_ClicElige_YDobleClicInstalaSinEnter()
        {
            yield return CargarCiudad();
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(9000 - cartera.Dinero, MotivoDinero.Otro);
            ServicioImplantes implantes = Registro<ServicioImplantes>.Actual;
            MostradorClinica mostrador = Object.FindFirstObjectByType<MostradorClinica>();
            Assert.IsNotNull(mostrador, "Está la clínica del Doc");
            PantallaClinica clinica = Object.FindFirstObjectByType<PantallaClinica>(FindObjectsInactive.Include);
            mostrador.AbrirDesdeDoc();
            yield return EsperarHasta(() => clinica.Visible, "abrir la clínica", 5f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(0, clinica.Indice, "Abre en el primer espacio");

            // Un clic en la casilla de Brazos la elige y no instala nada.
            yield return Clic("Espacio_" + EspacioImplante.Brazos, 1);
            Assert.AreEqual((int)EspacioImplante.Brazos, clinica.Indice, "Un clic elige la casilla");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(0, implantes.Nivel(EspacioImplante.Brazos), "Un clic solo no instala");
            int dineroAntes = cartera.Dinero;

            // Doble clic en Piernas: la elige y la instala, sin tocar Enter.
            yield return Clic("Espacio_" + EspacioImplante.Piernas, 2);
            yield return EsperarHasta(() => implantes.Nivel(EspacioImplante.Piernas) >= 1, "el doble clic instala el implante", 15f);
            Assert.LessOrEqual(cartera.Dinero, dineroAntes, "No regala dinero (la primera instalación puede ir sin costo)");
        }

        // Mueve el ratón (simulado) al centro de la casilla y hace <paramref name="veces"/> clics seguidos; avisa si el ratón no le pega a la casilla.
        private IEnumerator Clic(string nombreCasilla, int veces)
        {
            RectTransform casilla = null;
            foreach (RectTransform r in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.name == nombreCasilla)
                {
                    casilla = r;
                    break;
                }
            }
            Assert.IsNotNull(casilla, "Existe la casilla " + nombreCasilla);
            Canvas canvas = casilla.GetComponentInParent<Canvas>().rootCanvas;
            Camera camara = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 pantalla = RectTransformUtility.WorldToScreenPoint(camara, casilla.TransformPoint(casilla.rect.center));
            Set(raton.position, pantalla);
            yield return null;
            yield return null;
            var impactos = new List<RaycastResult>();
            EventSystem sistema = EventSystem.current;
            Assert.IsNotNull(sistema, "Hay un EventSystem");
            sistema.RaycastAll(new PointerEventData(sistema) { position = pantalla }, impactos);
            bool pega = false;
            foreach (RaycastResult impacto in impactos)
            {
                if (impacto.gameObject.transform.IsChildOf(casilla) || impacto.gameObject.transform == casilla)
                {
                    pega = true;
                    break;
                }
            }
            Assert.IsTrue(pega, "El ratón en " + pantalla + " le pega a " + nombreCasilla + " (falla si el lienzo no tiene GraphicRaycaster)");
            for (int i = 0; i < veces; i++)
            {
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return null;
            }
            yield return null;
        }
    }
}
