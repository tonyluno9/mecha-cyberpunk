using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-728: la historia del dinero en la ciudad, como un jugador: al salir de casa ves cómo asaltan a una persona de afuera (cinemática, el jugador queda quieto y NO te quitan
    /// dinero), Enter la salta, el carro del asaltante sale del pool del tráfico y huye a velocidad de persecución, el objetivo es "RÓBATE UN CARRO" y al subirte a uno "PERSÍGUELOS",
    /// el carro se detiene (por el final de su ruta o por un disparo a las llantas) y al vencer al asaltante la víctima te da una parte. Guardado temporal.
    /// Atajos anotados: al jugador se le coloca en la puerta con Colocar, el asaltante cae por daño directo y subir al carro es IntentarSubir del montable.
    /// </summary>
    public class AsaltoDineroT728Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int Dinero = 500;
        private string carpeta;
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            carpeta = Path.Combine(Application.temporaryCachePath, "asalto_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            if (carpeta != null && Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            restaurarRondas?.Invoke(); // el asset de config queda como estaba
            restaurarRondas = null;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(raiz);
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator Esperar(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private static IPuntoHistoria Punto(string id)
        {
            foreach (IPuntoHistoria p in Registro<IPuntoHistoria>.Todos)
            {
                if (p != null && p.Id == id) return p;
            }
            return null;
        }

        private IEnumerator Cargar(Action<DirectorPandillas> listo)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return Esperar(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null
                    && Registro<IObjetivos>.Actual != null && Punto(AsaltoMundoId.Principal) != null;
            }, "el director y el carro del asaltante (AsaltoMundo) están listos", 40f);
            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera.Dinero < Dinero) cartera.Abonar(Dinero - cartera.Dinero, MotivoDinero.Otro);
            SinRondasDePolicia();
            listo(director);
        }

        // T-933: hay patrullas de ronda al azar y una que viera robar el carro subiría el calor y arrestaría al jugador; esta prueba mide el asalto, no la ronda.
        private static Action restaurarRondas;

        private static void SinRondasDePolicia()
        {
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (m.GetType().Name != "GestorPersecucion")
                {
                    continue;
                }
                System.Reflection.BindingFlags privado = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                ScriptableObject config = (ScriptableObject)m.GetType().GetField("config", privado).GetValue(m);
                System.Reflection.FieldInfo campo = config.GetType().GetField("patrullasDeRonda", privado);
                object antes = campo.GetValue(config);
                campo.SetValue(config, 0);
                restaurarRondas = () => campo.SetValue(config, antes);
                foreach (MonoBehaviour ronda in (System.Collections.IEnumerable)m.GetType().GetField("rondas", privado).GetValue(m))
                {
                    if (ronda != null)
                    {
                        Object.Destroy(ronda.gameObject);
                    }
                }
            }
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        // Capturas (solo con CAPTURAR_S1=1): la cámara dada con el HUD y las barras.
        private static void Foto(string nombre, Camera camara)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1" || camara == null) return;
            MechaCyberpunk.Tests.UI.CapturaUI.Capturar("docs/capturas/s1d-T-728-" + nombre + ".png", camara);
        }

        private static Camera CamaraAsalto()
        {
            GameObject objeto = GameObject.Find("CamaraAsalto");
            return objeto != null ? objeto.GetComponent<Camera>() : null;
        }

        private static string TextoObjetivo => Registro<IObjetivos>.Actual.HayObjetivo ? Registro<IObjetivos>.Actual.Actual.Texto : "";

        // Sale de la casa de verdad: aparece en la puerta (Colocar) y el director cuenta que salió.
        private static IEnumerator PonerEnLaPuerta(DirectorPandillas director)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 entrada = default;
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar != null && lugar.Id == IdLugar.Casa) entrada = lugar.PosicionEntrada;
            }
            Assert.Greater(entrada.sqrMagnitude, 1f, "La casa tiene su lugar con puerta");
            // La puerta de la casa del director es el punto de salida (radio 2.5 m): se aparece sobre ella.
            Transform puerta = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name == "PuertaCasa" || t.name == "Casa_Puerta") puerta = t;
            }
            Vector3 destino = puerta != null ? puerta.position : entrada;
            jugador.Colocar(new Pose(destino + Vector3.up * 0.1f, Quaternion.identity));
            yield return null;
            yield return null;
        }

        // Un montable de carro que se pueda subir y que no sea el de los asaltantes (el de los asaltantes va a más de 4 m del punto que se le da).
        private static IMontable CarroPorRobar(Vector3 lejosDe)
        {
            IMontable mejor = null;
            float menor = float.MaxValue;
            IJugador jugador = Registro<IJugador>.Actual;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m == null || m.Modo != ModoJugador.Carro || !m.PuedeSubir || m.Ocupado) continue;
                Vector3 d = m.PuntoSubida - lejosDe; d.y = 0f;
                if (d.magnitude < 6f) continue;
                float dj = Vector3.Distance(m.PuntoSubida, jugador.Raiz.position);
                if (dj < menor) { menor = dj; mejor = m; }
            }
            return mejor;
        }

        [UnityTest]
        public IEnumerator ControlDesactivado_DejaLaVelocidadEnCero_ElMunecoNoSeQuedaCaminando()
        {
            yield return Cargar(d => { });
            IJugador jugador = Registro<IJugador>.Actual;
            ControladorAPie control = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            CharacterController cc = jugador.Raiz.GetComponent<CharacterController>();
            control.ActivarControl(true);
            yield return null;
            // Caminaba: el CharacterController guarda la velocidad de su último Move y el visual (que la lee) seguía caminando en su lugar.
            cc.Move(jugador.Raiz.forward * 0.3f);
            Assert.Greater(cc.velocity.magnitude, 0.5f, "Preparación: el jugador traía velocidad");
            control.ActivarControl(false);
            Assert.Less(cc.velocity.magnitude, 0.05f, "Sin control, la velocidad queda en cero");
            yield return null;
            yield return null;
            Assert.Less(cc.velocity.magnitude, 0.05f, "y sigue quieto en los cuadros siguientes");
            control.ActivarControl(true);
        }

        [UnityTest]
        public IEnumerator Asalto_ALaVictimaNoAlJugador_RobasUnCarroLoPersigues_YLaVictimaTeDaUnaParte()
        {
            DirectorPandillas director = null;
            yield return Cargar(d => director = d);
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            director.Cargar("{\"paso\":\"Despierta\"}");
            yield return null;
            yield return PonerEnLaPuerta(director);
            CharacterController cc = jugador.Raiz.GetComponent<CharacterController>();
            // Si la puerta del director queda a más de 2.5 m del punto elegido, se cuenta la salida como lo hace el juego.
            if (director.Paso == PasoHistoria.Despierta)
            {
                director.Cargar("{\"paso\":\"Asalto\"}");
            }
            yield return Esperar(() => director.Paso == PasoHistoria.Asalto, "al salir de casa empieza el asalto", 5f);
            AsaltoDinero asalto = director.Asalto;
            ControladorAPie control = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            yield return null;

            // a. La cinemática: el jugador quieto (sin velocidad que mueva su animación), barras, una víctima de afuera y el asaltante; el HUD oculto.
            Assert.AreEqual(FaseAsalto.Cinematica, asalto.Fase);
            Assert.IsTrue(asalto.CinematicaVisible, "Hay barras de cinemática");
            Assert.IsFalse(control.ControlActivo, "El jugador no se mueve durante la cinemática");
            Assert.Less(cc.velocity.magnitude, 0.05f, "Quieto: sin velocidad no se queda caminando en su sitio");
            Assert.IsNotNull(asalto.Asaltante, "Hay un asaltante");
            Assert.IsNotNull(asalto.Victima, "Hay una persona de afuera a la que asaltan");
            Assert.AreNotSame(jugador.Raiz.gameObject, asalto.Victima);
            Assert.Greater(Vector3.Distance(asalto.Victima.transform.position, jugador.Raiz.position), 3f, "La víctima no eres tú");
            Assert.AreEqual(AsaltoDinero.TextoRobarCarro, TextoObjetivo);
            Assert.AreEqual(Dinero, cartera.Dinero, "No te quitan tu dinero");
            yield return new WaitForSecondsRealtime(1.2f);
            MechaCyberpunk.UI.Hud.HudH10 hudNuevo = Object.FindFirstObjectByType<MechaCyberpunk.UI.Hud.HudH10>();
            Assert.IsNotNull(hudNuevo, "Hay HUD nuevo en la ciudad");
            Assert.IsTrue(MechaCyberpunk.UI.Hud.HudCinematica.Activa, "La cinemática avisa al HUD");
            Assert.IsFalse(hudNuevo.Canvas.enabled, "El HUD nuevo está oculto durante la cinemática");
            Assert.AreEqual(Dinero, cartera.Dinero, "Sigue sin tocarse tu dinero");
            Assert.Less(cc.velocity.magnitude, 0.05f, "Sigue quieto");
            UnityEngine.UI.Text letrero = null;
            foreach (UnityEngine.UI.Text t in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
            {
                if (t.name == "TeclaSaltar") letrero = t;
            }
            Assert.IsNotNull(letrero, "Queda el letrero de saltar");
            Assert.AreEqual("ENTER: SALTAR", letrero.text);
            Camera cinema = CamaraAsalto();
            Foto("cinematica-asalto", cinema);

            // b. Enter (tecla real) salta la cinemática: el robo es a la víctima y el jugador queda libre al subir el asaltante al carro.
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(Dinero, cartera.Dinero, "Tu dinero sigue igual");
            Assert.AreEqual(ReglaAsalto.DineroDeLaVictima, asalto.DineroRobado, "Le robaron a la víctima");
            yield return Esperar(() => asalto.Fase == FaseAsalto.Persecucion, "el asaltante sube al carro y empieza la persecución", 40f);
            Assert.IsTrue(control.ControlActivo, "El jugador queda libre");
            Assert.IsFalse(asalto.CinematicaVisible, "Se quitan las barras");
            Assert.IsFalse(MechaCyberpunk.UI.Hud.HudCinematica.Activa, "Acabó la cinemática: el HUD vuelve");
            yield return null;
            Assert.IsTrue(hudNuevo.Canvas.enabled, "El HUD nuevo se ve otra vez");
            IPuntoHistoria carro = Punto(AsaltoMundoId.Principal);
            yield return Esperar(() => carro.Pose.position != default, "el carro tiene posición", 5f);
            Assert.IsNull(asalto.Victima, "La víctima ya no hace falta en la escena");

            // c. A pie el objetivo es RÓBATE UN CARRO; el carro huye a velocidad de persecución real (no a 10 m/s).
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(AsaltoDinero.TextoRobarCarro, TextoObjetivo);
            float maxima = 0f;
            float finVel = Time.realtimeSinceStartup + 12f;
            Vector3 ultimaPos = carro.Pose.position;
            float ultimoT = Time.time;
            while (Time.realtimeSinceStartup < finVel && asalto.Fase == FaseAsalto.Persecucion)
            {
                // Rapidez medida en ventanas de medio segundo con la posición del carro.
                if (Time.time - ultimoT >= 0.5f)
                {
                    Vector3 d = carro.Pose.position - ultimaPos; d.y = 0f;
                    maxima = Mathf.Max(maxima, d.magnitude / (Time.time - ultimoT));
                    ultimaPos = carro.Pose.position;
                    ultimoT = Time.time;
                }
                if (maxima >= 12.5f) break;
                yield return null;
            }
            Assert.GreaterOrEqual(maxima, 12.5f, "El carro de los asaltantes llega a velocidad de persecución (máxima vista " + maxima + " m/s)");
            Foto("persecucion-a-pie", jugador.Camara);

            // d. Te robas un carro: el objetivo cambia a PERSÍGUELOS y el marcador sigue al carro de los asaltantes.
            IMontable robado = CarroPorRobar(carro.Pose.position);
            Assert.IsNotNull(robado, "Hay un carro por robar en la calle");
            Vector3 objetivoAPie = objetivos.Posicion;
            Assert.Less(Vector3.Distance(objetivoAPie, robado.PuntoSubida), 40f, "A pie el marcador apunta a un carro por robar, no al de los asaltantes");
            jugador.Colocar(new Pose(robado.PuntoSubida + Vector3.up * 0.1f, jugador.Raiz.rotation));
            yield return null;
            Assert.IsTrue(((MechaCyberpunk.Personaje.Jugador.JugadorAPie)jugador).Subir(robado), "El jugador sube al carro");
            yield return Esperar(() => jugador.Modo == ModoJugador.Carro, "el jugador va en el carro", 5f);
            yield return Esperar(() => TextoObjetivo == AsaltoDinero.TextoPerseguir, "el objetivo cambia a PERSÍGUELOS", 3f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.Less(Vector3.Distance(objetivos.Posicion, carro.Pose.position), 25f, "El marcador está junto al carro de los asaltantes");
            Foto("persecucion-en-carro", jugador.Camara);

            // e. El carro llega al final de su ruta (o se detiene): baja el asaltante y toca atraparlo.
            yield return Esperar(() => asalto.Fase == FaseAsalto.Enfrentando, "el carro se detiene y baja el asaltante", 100f);
            Assert.AreEqual(AsaltoDinero.TextoRecuperar, TextoObjetivo);
            Assert.IsTrue(asalto.Asaltante.activeInHierarchy);
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return null;
                Vector3 p = asalto.Asaltante.transform.position;
                GameObject foto = new GameObject("CamaraFoto");
                Camera camaraFoto = foto.AddComponent<Camera>();
                camaraFoto.CopyFrom(jugador.Camara);
                camaraFoto.depth += 3f;
                camaraFoto.transform.position = AsaltoDinero.PosicionLibre(p + Vector3.up * 1f,
                    new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, new Vector3(1f, 0f, 1f).normalized, new Vector3(-1f, 0f, -1f).normalized }, 9f, 2.5f);
                camaraFoto.transform.rotation = Quaternion.LookRotation((p + Vector3.up * 0.8f - camaraFoto.transform.position).normalized);
                Foto("asaltante-bajo-del-carro", camaraFoto);
                Object.Destroy(foto);
            }
            PandilleroAPie pandillero = asalto.Asaltante.GetComponent<PandilleroAPie>();
            Assert.IsTrue(pandillero.enabled, "El asaltante pelea");

            // f. Al vencerlo, la víctima te da la mitad de lo robado y la historia sigue.
            pandillero.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, pandillero.transform.position, Vector3.forward, null));
            yield return Esperar(() => director.Paso == PasoHistoria.PrimeraPelea, "la historia sigue a la primera pelea", 5f);
            Assert.AreEqual(Dinero + ReglaAsalto.Recompensa(ReglaAsalto.DineroDeLaVictima), cartera.Dinero, "La víctima te dio una parte");
        }

        [UnityTest]
        public IEnumerator Asalto_UnDisparoALasLlantas_DetieneElCarroYElAsaltanteBaja()
        {
            DirectorPandillas director = null;
            yield return Cargar(d => director = d);
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            Transform puerta = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name == "PuertaCasa" || t.name == "Casa_Puerta") puerta = t;
            }
            if (puerta != null) jugador.Colocar(new Pose(puerta.position + Vector3.up * 0.1f, Quaternion.identity));
            director.Cargar("{\"paso\":\"Asalto\"}");
            yield return null;
            AsaltoDinero asalto = director.Asalto;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Pulsar(teclado.enterKey);
            yield return Esperar(() => asalto.Fase == FaseAsalto.Persecucion, "empieza la persecución", 40f);

            // El carro huye: se le dispara a las ruedas. El rayo pega primero en la franja de las llantas y esa franja recibe el daño del disparo.
            GameObject llantas = null;
            yield return Esperar(() => (llantas = GameObject.Find("LlantasAsalto")) != null, "el carro tiene su franja de llantas", 5f);
            Collider franja = llantas.GetComponent<Collider>();
            Physics.SyncTransforms();
            Vector3 centro = franja.bounds.center;
            Vector3 lado = llantas.transform.right * 6f;
            Assert.IsTrue(Physics.Raycast(centro + lado, -lado.normalized, out RaycastHit golpe, 12f, ~0, QueryTriggerInteraction.Ignore), "El disparo pega en el carro");
            IRecibeDanio objetivo = golpe.collider.GetComponentInParent<IRecibeDanio>();
            Assert.IsNotNull(objetivo, "Lo que pega en las ruedas recibe el daño del disparo");
            Assert.IsTrue(objetivo is IObjetoInerte, "Es un objeto: no sangra");
            Assert.Greater(objetivo.RecibirDanio(new InfoDanio(20f, TipoDanio.Disparo, golpe.point, -lado.normalized, null)).DanioAplicado, 0f);

            // El carro frena y se detiene mucho antes del final de su ruta; baja el asaltante.
            float inicio = Time.realtimeSinceStartup;
            yield return Esperar(() => asalto.Fase == FaseAsalto.Enfrentando, "la llanta ponchada detiene el carro", 20f);
            Assert.Less(Time.realtimeSinceStartup - inicio, 12f, "Se detuvo por la llanta, no por llegar al final");
            Assert.AreEqual(AsaltoDinero.TextoRecuperar, TextoObjetivo);
            Assert.AreEqual(Dinero, cartera.Dinero, "Tu dinero nunca se tocó");
            PandilleroAPie pandillero = asalto.Asaltante.GetComponent<PandilleroAPie>();
            pandillero.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, pandillero.transform.position, Vector3.forward, null));
            yield return Esperar(() => director.Paso == PasoHistoria.PrimeraPelea, "la historia sigue", 5f);
            Assert.AreEqual(Dinero + ReglaAsalto.Recompensa(ReglaAsalto.DineroDeLaVictima), cartera.Dinero, "La víctima te da una parte");
        }

        [UnityTest]
        public IEnumerator Asalto_AlCambiarDePaso_ElJugadorQuedaLibreYLaEscenaSeQuita()
        {
            DirectorPandillas director = null;
            yield return Cargar(d => director = d);
            ICartera cartera = Registro<ICartera>.Actual;
            ControladorAPie control = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<ControladorAPie>();
            director.Cargar("{\"paso\":\"Asalto\"}");
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(Dinero, cartera.Dinero, "No te quitan el dinero");
            // Un guardado que cae en otro paso (o F6) quita el asalto: el dinero vuelve y el control también.
            director.Cargar("{\"paso\":\"PrimeraPelea\"}");
            yield return null;
            Assert.AreEqual(Dinero, cartera.Dinero, "Cancelar el asalto no toca tu dinero");
            Assert.IsNull(director.Asalto.Victima, "La víctima se quita al cancelar");
            Assert.IsTrue(control.ControlActivo);
            Assert.AreEqual(FaseAsalto.Inactivo, director.Asalto.Fase);
        }
    }
}
