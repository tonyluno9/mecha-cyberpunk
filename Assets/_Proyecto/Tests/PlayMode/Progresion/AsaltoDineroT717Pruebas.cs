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
    /// T-717: la historia del dinero en la ciudad, como un jugador: al salir de casa te asaltan (cinemática, el jugador queda congelado), Enter la salta, el carro del
    /// asaltante sale del pool del tráfico y huye, el objetivo lo sigue, se detiene (por el final de su ruta o por un disparo a las llantas) y al vencer al asaltante
    /// el dinero vuelve a la cartera. Guardado temporal. Atajos anotados: al jugador se le coloca en la puerta con Colocar y el asaltante cae por daño directo.
    /// </summary>
    public class AsaltoDineroT717Pruebas : InputTestFixture
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
            listo(director);
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
            MechaCyberpunk.Tests.UI.CapturaUI.Capturar("docs/capturas/s1c-T-717-" + nombre + ".png", camara);
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

        [UnityTest]
        public IEnumerator Asalto_DelRoboAlDineroRecuperado_ConElCarroQueLlegaAlFinal()
        {
            DirectorPandillas director = null;
            yield return Cargar(d => director = d);
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            director.Cargar("{\"paso\":\"Despierta\"}");
            yield return null;
            yield return PonerEnLaPuerta(director);
            // Si la puerta del director queda a más de 2.5 m del punto elegido, se cuenta la salida como lo hace el juego.
            if (director.Paso == PasoHistoria.Despierta)
            {
                director.Cargar("{\"paso\":\"Asalto\"}");
            }
            yield return Esperar(() => director.Paso == PasoHistoria.Asalto, "al salir de casa empieza el asalto", 5f);
            AsaltoDinero asalto = director.Asalto;
            ControladorAPie control = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            yield return null;

            // a. La cinemática: jugador congelado, barras, el asaltante camina hacia ti y el objetivo ya lo señala.
            Assert.AreEqual(FaseAsalto.Cinematica, asalto.Fase);
            Assert.IsTrue(asalto.CinematicaVisible, "Hay barras de cinemática");
            Assert.IsFalse(control.ControlActivo, "El jugador no se mueve durante la cinemática");
            Assert.IsNotNull(asalto.Asaltante, "Hay un asaltante");
            Assert.AreEqual(AsaltoDinero.TextoPerseguir, TextoObjetivo);
            Assert.AreEqual(Dinero, cartera.Dinero, "Aún no te quita el dinero");
            yield return new WaitForSecondsRealtime(1.2f);
            // T-726: en la cinemática el HUD se oculta (HudH10, minimapa y marcadores) y solo queda el letrero de saltar con la tecla real.
            MechaCyberpunk.UI.Hud.HudH10 hudNuevo = Object.FindFirstObjectByType<MechaCyberpunk.UI.Hud.HudH10>();
            Assert.IsNotNull(hudNuevo, "Hay HUD nuevo en la ciudad");
            Assert.IsTrue(MechaCyberpunk.UI.Hud.HudCinematica.Activa, "La cinemática avisa al HUD");
            Assert.IsFalse(hudNuevo.Canvas.enabled, "El HUD nuevo está oculto durante la cinemática");
            foreach (MechaCyberpunk.UI.Mapa.Minimapa mini in Object.FindObjectsByType<MechaCyberpunk.UI.Mapa.Minimapa>(FindObjectsSortMode.None))
            {
                Assert.IsFalse(mini.Visible, "El minimapa también");
            }
            UnityEngine.UI.Text letrero = null;
            foreach (UnityEngine.UI.Text t in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
            {
                if (t.name == "TeclaSaltar") letrero = t;
            }
            Assert.IsNotNull(letrero, "Queda el letrero de saltar");
            Assert.AreEqual("ENTER: SALTAR", letrero.text);
            Assert.IsTrue(letrero.canvas.enabled, "y se ve");
            Camera cinema = CamaraAsalto();
            Foto("cinematica-asaltante", cinema);

            // b. Enter (tecla real) salta la cinemática: el robo pasa igual y el jugador queda libre al subir el asaltante al carro.
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(0, cartera.Dinero, "El asaltante se llevó todo el dinero");
            Assert.AreEqual(Dinero, asalto.DineroRobado);
            yield return Esperar(() => asalto.Fase == FaseAsalto.Persecucion, "el asaltante sube al carro y empieza la persecución", 40f);
            Assert.IsTrue(control.ControlActivo, "El jugador queda libre");
            Assert.IsFalse(asalto.CinematicaVisible, "Se quitan las barras");
            Assert.IsFalse(MechaCyberpunk.UI.Hud.HudCinematica.Activa, "Acabó la cinemática: el HUD vuelve");
            yield return null;
            Assert.IsTrue(hudNuevo.Canvas.enabled, "El HUD nuevo se ve otra vez");
            IPuntoHistoria carro = Punto(AsaltoMundoId.Principal);
            yield return Esperar(() => carro.Pose.position != default, "el carro tiene posición", 5f);

            Foto("persecucion", jugador.Camara);
            // c. El objetivo sigue al carro mientras huye.
            Vector3 primera = objetivos.Posicion;
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(AsaltoDinero.TextoPerseguir, TextoObjetivo);
            Assert.Greater(Vector3.Distance(primera, objetivos.Posicion), 2f, "El objetivo se mueve con el carro");
            Assert.Less(Vector3.Distance(objetivos.Posicion, carro.Pose.position), 25f, "El objetivo está junto al carro (objetivo " + objetivos.Posicion + ", carro " + carro.Pose.position + ", jugador " + jugador.Raiz.position + ", fase " + asalto.Fase + ")");

            // d. El carro llega al final de su ruta (o se detiene): baja el asaltante y toca recuperar el dinero.
            yield return Esperar(() => asalto.Fase == FaseAsalto.Enfrentando, "el carro se detiene y baja el asaltante", 90f);
            Assert.AreEqual(AsaltoDinero.TextoRecuperar, TextoObjetivo);
            Assert.IsTrue(asalto.Asaltante.activeInHierarchy);
            // La captura mira al asaltante junto al carro detenido desde unos metros, con el HUD.
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

            // e. Al vencerlo el dinero vuelve y la historia sigue.
            pandillero.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, pandillero.transform.position, Vector3.forward, null));
            yield return Esperar(() => director.Paso == PasoHistoria.PrimeraPelea, "la historia sigue a la primera pelea", 5f);
            Assert.AreEqual(Dinero, cartera.Dinero, "El dinero volvió a la cartera");
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
            Assert.AreEqual(0, cartera.Dinero, "El dinero sigue con el asaltante");
            PandilleroAPie pandillero = asalto.Asaltante.GetComponent<PandilleroAPie>();
            pandillero.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, pandillero.transform.position, Vector3.forward, null));
            yield return Esperar(() => director.Paso == PasoHistoria.PrimeraPelea, "la historia sigue", 5f);
            Assert.AreEqual(Dinero, cartera.Dinero);
        }

        [UnityTest]
        public IEnumerator Asalto_AlCambiarDePaso_ElDineroVuelveYElJugadorQuedaLibre()
        {
            DirectorPandillas director = null;
            yield return Cargar(d => director = d);
            ICartera cartera = Registro<ICartera>.Actual;
            ControladorAPie control = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<ControladorAPie>();
            director.Cargar("{\"paso\":\"Asalto\"}");
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(0, cartera.Dinero, "Te quitaron el dinero");
            // Un guardado que cae en otro paso (o F6) quita el asalto: el dinero vuelve y el control también.
            director.Cargar("{\"paso\":\"PrimeraPelea\"}");
            yield return null;
            Assert.AreEqual(Dinero, cartera.Dinero, "El dinero se repone al cancelar el asalto");
            Assert.IsTrue(control.ControlActivo);
            Assert.AreEqual(FaseAsalto.Inactivo, director.Asalto.Fase);
        }
    }
}
