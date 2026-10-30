using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.MenuPausa;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-410 en Ciudad_Prologo (ruta de guardado temporal): M abre el mapa y Esc lo cierra sin pausar; con un pandillero armado
    /// disparándote a 15 m, Esc pausa y durante 2 s reales la vida no baja, el pandillero no se mueve, <c>Time.timeScale</c> es 0, el clic
    /// no dispara (el cargador no cambia) y W no mueve; ↓ elige GUARDAR, Enter abre las ranuras en GUARDAR y Esc vuelve a la pausa; el ratón
    /// encima de CONTINUAR lo elige y el clic continúa (tiempo en 1, cursor bloqueado). Atajos: la pistola se regala con
    /// <see cref="ArsenalJugador.DarMejora"/> y la noche se pone con el reloj. Con CAPTURAR_T410=1 guarda docs/capturas/T-410-1 en 1080p y 1440p.
    /// </summary>
    public class PausaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T410";
        private const float DistanciaPandillero = 15f;
        private const float AlturaOjos = 1.5f;
        private const float SegundosPausa = 2f;
        private const float SegundosMaxDisparo = 20f;
        private const float HoraNoche = 22f;
        private const int Direcciones = 16;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t410_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            try
            {
                Directory.Delete(carpeta, true);
            }
            catch (Exception)
            {
                // La carpeta temporal se queda; no importa.
            }
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        // Cambiar `devices` en un mapa apagado descarta su estado de resolución: la escena los resuelve de nuevo al prenderlos.
        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    asset.Disable();
                    asset.devices = dispositivos;
                    asset.devices = null;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
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

        private static void Capturar(string nombre, Camera camara)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/" + nombre + ".png", camara);
            CapturaUI.Capturar2K("docs/capturas/" + nombre + "-1440.png", camara);
        }

        // Un pandillero armado y un punto a 15 m de él, con vista libre y piso, dentro de su territorio.
        private static bool BuscarTirador(out PandilleroAPie tirador, out Vector3 punto)
        {
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                foreach (PandilleroAPie p in grupo.Pandilleros)
                {
                    if (p == null || !p.isActiveAndEnabled || p.EstaKO || !p.Datos.UsaPistola)
                    {
                        continue;
                    }
                    Vector3 ojos = p.transform.position + Vector3.up * AlturaOjos;
                    for (int i = 0; i < Direcciones; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, i * 360f / Direcciones, 0f) * Vector3.forward;
                        Vector3 destino = p.transform.position + dir * DistanciaPandillero;
                        Vector3 plano = destino - grupo.Centro;
                        plano.y = 0f;
                        if (plano.magnitude > grupo.RadioTerritorio * 0.8f)
                        {
                            continue;
                        }
                        if (Physics.Raycast(ojos, dir, DistanciaPandillero + 1f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        if (!Physics.Raycast(destino + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 6f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        tirador = p;
                        punto = piso.point + Vector3.up * 0.05f;
                        return true;
                    }
                }
            }
            tirador = null;
            punto = Vector3.zero;
            return false;
        }

        [UnityTest]
        public IEnumerator RecorridoPausa()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas d = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return d != null && d.Iniciado && Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null;
            }, "la ciudad arrancó", 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            IJugador jugador = Registro<IJugador>.Actual;
            CursorLockMode cursorInicial = Cursor.lockState;
            PantallaPausa pausa = UnityEngine.Object.FindFirstObjectByType<PantallaPausa>();
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(pausa, "La ciudad trae la pausa");
            Assert.IsNotNull(mapa, "La ciudad trae el mapa");
            Assert.IsNotNull(pausa.Ranuras, "La pausa encuentra las ranuras");
            Registro<IRelojMundo>.Actual?.AvanzarHasta(HoraNoche);

            // Pistola en la mano (atajo: se regala).
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (!arsenal.Tiene(ArmaAPie.Pistola))
            {
                arsenal.DarMejora(ArmaAPie.Pistola);
            }
            ArmasAPie armas = jugador.Raiz.GetComponentInChildren<ArmasAPie>();
            PistolaAPie pistola = jugador.Raiz.GetComponentInChildren<PistolaAPie>();
            VidaAPie vida = jugador.Raiz.GetComponentInChildren<VidaAPie>();
            Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola), "La pistola queda en la mano");
            yield return null;
            yield return null;
            if (pistola.Balas <= 0)
            {
                yield return Pulsar(teclado.rKey);
                yield return EsperarHasta(() => !pistola.Recargando && pistola.Balas > 0, "recargar la pistola", 10f);
            }
            Assert.Greater(pistola.Balas, 0);

            // a. M abre el mapa y Esc lo cierra sin pausar; el siguiente Esc pausa (aquí se comprueba más abajo, ya con el tirador).
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto, "M abre el mapa");
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(mapa.Abierto, "Esc cierra el mapa");
            Assert.IsFalse(pausa.Abierta, "El Esc que cierra el mapa no pausa");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(PilaPantallas.Vacia);

            // b. Un pandillero armado a 15 m te dispara. Atajo: al empezar la historia los grupos armados están apagados; se prenden.
            int armados = 0;
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (grupo.Datos != null && grupo.Datos.UsaPistola)
                {
                    grupo.gameObject.SetActive(true);
                    armados++;
                }
            }
            Assert.Greater(armados, 0, "La ciudad trae grupos armados");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(BuscarTirador(out PandilleroAPie tirador, out Vector3 punto), "Hay un pandillero armado con vista libre a 15 m");
            Vector3 hacia = tirador.transform.position - punto;
            hacia.y = 0f;
            jugador.Colocar(new Pose(punto, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            float vidaInicial = vida.Actual;
            yield return EsperarHasta(() => vida.Actual < vidaInicial, "que el pandillero te dispare", SegundosMaxDisparo);
            Assert.IsFalse(vida.EstaKO);

            // c. Esc pausa: 2 s reales sin daño, sin movimiento, sin disparo y sin caminar.
            yield return Pulsar(teclado.escapeKey);
            Assert.IsTrue(pausa.Abierta, "Esc pausa con la pila vacía");
            Assert.IsTrue(Pausa.Activa);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.AreSame(pausa.Continuar, pausa.Menu.Elegido, "Empieza en CONTINUAR");
            Assert.IsTrue(pausa.Opciones.interactable, "OPCIONES se puede elegir (T-411)");
            float vidaPausa = vida.Actual;
            Vector3 posTirador = tirador.transform.position;
            Vector3 posJugador = jugador.Raiz.position;
            int balas = pistola.Balas;
            float fin = Time.realtimeSinceStartup + SegundosPausa;
            bool clic = false;
            Press(teclado.wKey);
            while (Time.realtimeSinceStartup < fin)
            {
                if (clic) Release(raton.leftButton); else Press(raton.leftButton);
                clic = !clic;
                yield return null;
            }
            Release(teclado.wKey);
            Release(raton.leftButton);
            yield return null;
            Assert.AreEqual(vidaPausa, vida.Actual, 0.0001f, "En pausa la vida no baja");
            Assert.Less(Vector3.Distance(posTirador, tirador.transform.position), 0.01f, "En pausa el pandillero no se mueve");
            Assert.AreEqual(0f, Time.timeScale, "Sigue en pausa");
            Assert.AreEqual(balas, pistola.Balas, "En pausa el clic no dispara");
            Assert.Less(Vector3.Distance(posJugador, jugador.Raiz.position), 0.01f, "En pausa W no mueve");
            Assert.IsTrue(pausa.Abierta);
            Assert.AreSame(pausa.Continuar, pausa.Menu.Elegido, "W (arriba) no salió de CONTINUAR");
            Assert.IsTrue(pausa.Continuar.EstaElegido, "Los clics en el fondo no le quitan lo elegido a CONTINUAR");
            Capturar("T-410-1", jugador.Camara);

            // d. ↓ GUARDAR, Enter abre las ranuras en GUARDAR y Esc vuelve a la pausa.
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(pausa.Guardar, pausa.Menu.Elegido, "↓ elige GUARDAR");
            yield return Pulsar(teclado.enterKey);
            PantallaRanuras ranuras = pausa.Ranuras;
            Assert.IsTrue(ranuras.Abierta, "GUARDAR abre las ranuras");
            Assert.AreEqual(ModoRanuras.Guardar, ranuras.Modo);
            Assert.IsTrue(PilaPantallas.EsArriba(ranuras), "Las ranuras quedan encima de la pausa");
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(ranuras.Abierta, "Esc cierra las ranuras");
            Assert.IsTrue(pausa.Abierta, "Y vuelve a la pausa");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreSame(pausa.Guardar, pausa.Menu.Elegido, "Vuelve con GUARDAR elegido");

            // e. El ratón encima de CONTINUAR lo elige y el clic continúa.
            RectTransform rect = (RectTransform)pausa.Continuar.transform;
            Vector2 centro = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Set(raton.position, centro);
            yield return null;
            yield return null;
            Assert.AreSame(pausa.Continuar, pausa.Menu.Elegido, "El ratón encima elige CONTINUAR");
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
            Assert.IsFalse(pausa.Abierta, "El clic en CONTINUAR continúa");
            Assert.IsFalse(Pausa.Activa);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(0, MenuCyberfix.Abiertos, "No queda ningún menú abierto");
            Debug.Log("[Prueba] cursor al continuar: " + Cursor.lockState + " (al empezar: " + cursorInicial + ")");
            // En batchmode no hay ventana y Cursor.lockState se queda en None: ahí basta con que no quede menú abierto (el kit lo bloquea).
            if (cursorInicial == CursorLockMode.Locked)
            {
                Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "El cursor vuelve a quedar bloqueado");
            }
            Assert.IsTrue(jugador.Raiz.GetComponentInChildren<MechaCyberpunk.Personaje.Movimiento.ControladorAPie>().ControlActivo, "El jugador vuelve a moverse");
            Assert.IsTrue(PilaPantallas.Vacia);
        }
    }
}
