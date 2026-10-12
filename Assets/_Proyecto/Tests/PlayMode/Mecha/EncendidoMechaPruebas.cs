using System;
using System.Collections;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Movimiento;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>El encendido de 3 pasos al subir al mecha estacionado de Pruebas_Calle, con el jugador real y entrada simulada.</summary>
    public class EncendidoMechaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaTony = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 8f;
        private const float MaxSegundosFrenar = 6f;
        private const float MaxSegundosEspera = 8f;
        private const float RetrasoTecla = 0.1f;
        private const float SegundosMovimiento = 1f;
        private const float MinimoMovido = 0.5f;
        private const float MaximoQuieto = 0.01f;
        private const float PausaSistemasEnLinea = 1.3f;
        private const float DanioPrueba = 10f;
        private const float MinimoEncendido = 4.5f;
        private const float MaximoEncendido = 5.5f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private InteraccionAPie interaccion;
        private MontajeMecha montaje;
        private ControladorMecha controlador;
        private EncendidoMecha encendido;
        private PeleadorMecha peleador;
        private GolpeMecha golpe;
        private PantallaEncendido pantalla;
        private SacudidaCabina cabina;
        private int bajarNegado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            bajarNegado = 0;
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private IEnumerator CargarEscena(string ruta)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private void BuscarMecha()
        {
            montaje = UnityEngine.Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(montaje, "La escena trae el mecha");
            controlador = montaje.GetComponent<ControladorMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            Assert.IsNotNull(encendido, "El mecha trae EncendidoMecha");
            peleador = montaje.GetComponent<PeleadorMecha>();
            golpe = montaje.GetComponent<GolpeMecha>();
            pantalla = montaje.GetComponentInChildren<PantallaEncendido>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla de encendido");
            cabina = montaje.GetComponentInChildren<SacudidaCabina>(true);
        }

        private IEnumerator CargarCalle()
        {
            yield return CargarEscena(RutaCalle);
            jugador = UnityEngine.Object.FindFirstObjectByType<JugadorAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            interaccion = jugador.GetComponent<InteraccionAPie>();
            BuscarMecha();
            interaccion.BajarNegado += () => bajarNegado++;
            yield return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string descripcion)
        {
            float fin = Time.time + MaxSegundosEspera;
            while (Time.time < fin && !condicion())
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + descripcion);
        }

        private IEnumerator Subir()
        {
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E frente al mecha lo sube");
        }

        // Pulsa cada tecla RetrasoTecla después de que se pide y sigue hasta que el mecha queda encendido.
        private IEnumerator Encender()
        {
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso1 && encendido.Progreso <= 0f, "paso 1 esperando");
            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.fKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, "paso 2");
            yield return Esperar(RetrasoTecla);
            Press(teclado.spaceKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso3, "paso 3");
            Release(teclado.spaceKey);
            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.qKey);
            yield return EsperarHasta(() => encendido.EstaEncendido, "encendido completo");
        }

        private IEnumerator Frenar()
        {
            float fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && controlador.VelocidadLocal.magnitude > 0.1f)
            {
                yield return null;
            }
            Assert.Less(controlador.VelocidadLocal.magnitude, 0.5f, "El mecha frenó");
        }

        [UnityTest]
        public IEnumerator Subir_CabinaRecienSubida_EstaApagadaConElPaso1EnPantalla()
        {
            yield return CargarCalle();
            Assert.IsFalse(pantalla.Visible, "Vacío, la pantalla está apagada");

            yield return Subir();
            yield return null;

            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso);
            Assert.IsFalse(encendido.EstaEncendido);
            Assert.IsFalse(peleador.ControlActivo, "Sin encender no hay control");
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(encendido.Config.TextoPaso1, pantalla.Texto.text);
            Assert.AreEqual(encendido.Config.DesplazamientoAgachado, cabina.DesplazamientoBase.y, 0.001f, "La cámara empieza agachada");
        }

        [UnityTest]
        public IEnumerator Encendiendo_ConWClicYE_NoMueveNoGolpeaNiDejaBajar()
        {
            yield return CargarCalle();
            yield return Subir();
            Vector3 antes = montaje.transform.position;

            Press(teclado.wKey);
            Press(raton.leftButton);
            yield return Esperar(SegundosMovimiento);
            Release(teclado.wKey);
            Release(raton.leftButton);
            yield return null;
            Assert.AreEqual(0f, Vector3.Distance(antes, montaje.transform.position), MaximoQuieto, "W no mueve al mecha");
            Assert.AreEqual(FaseGolpe.Listo, golpe.Fase, "El clic no golpea");

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(1, bajarNegado, "La E avisa que no puede bajar");
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "No deja bajar");
            Assert.IsTrue(montaje.Ocupado);
            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso, "Esas teclas no avanzan el encendido");
        }

        [UnityTest]
        public IEnumerator Encendiendo_PasoAPaso_LaPantallaMuestraCadaPasoYLaCabinaSeEnciende()
        {
            yield return CargarCalle();
            yield return Subir();
            yield return null;
            Assert.AreEqual(encendido.Config.TextoPaso1, pantalla.Texto.text);

            yield return Tecla(teclado.fKey);
            Assert.IsTrue(pantalla.Visible);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, "paso 2");
            yield return null;
            Assert.AreEqual(encendido.Config.TextoPaso2, pantalla.Texto.text);

            Press(teclado.spaceKey);
            yield return Esperar(encendido.Config.SegundosMantenerPaso2 / 2f);
            float mitad = cabina.DesplazamientoBase.y;
            Assert.Greater(mitad, encendido.Config.DesplazamientoAgachado + 0.1f, "Subiendo");
            Assert.Less(mitad, -0.1f, "Todavía a medio levantar");
            Assert.AreEqual(0.5f, pantalla.Relleno, 0.2f, "La barra sigue el progreso");
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso3, "paso 3");
            Release(teclado.spaceKey);
            yield return null;
            Assert.AreEqual(encendido.Config.TextoPaso3, pantalla.Texto.text);
            Assert.AreEqual(0f, cabina.DesplazamientoBase.y, 0.05f, "Ya está a su altura");

            yield return Tecla(teclado.qKey);
            yield return EsperarHasta(() => encendido.EstaEncendido, "encendido");
            yield return null;
            Assert.AreEqual(encendido.Config.TextoEncendido, pantalla.Texto.text);
            Assert.IsTrue(pantalla.Visible);
            yield return Esperar(PausaSistemasEnLinea);
            Assert.IsFalse(pantalla.Visible, "La pantalla se apaga");
        }

        [UnityTest]
        public IEnumerator Encendiendo_ConUnaTeclaEquivocada_ParpadeaEnRojoYNoAvanza()
        {
            yield return CargarCalle();
            yield return Subir();
            yield return null;

            Color normal = pantalla.Texto.color;
            yield return Tecla(teclado.qKey);

            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso, "Q no avanza el paso 1");
            Assert.AreNotEqual(normal, pantalla.Texto.color, "La línea parpadea en rojo");
            Assert.Greater(pantalla.Texto.color.r, pantalla.Texto.color.g);
            yield return Esperar(1f);
            Assert.AreEqual(normal, pantalla.Texto.color, "Después vuelve a su color");
        }

        [UnityTest]
        public IEnumerator Encender_ConTeclasCasiInmediatas_TardaCercaDeCincoSegundosYDaControl()
        {
            yield return CargarCalle();
            yield return Subir();
            float inicio = Time.time - Time.deltaTime;

            yield return Encender();
            yield return EsperarHasta(() => peleador.ControlActivo, "control del piloto");
            float duracion = Time.time - inicio;

            Assert.GreaterOrEqual(duracion, MinimoEncendido);
            Assert.LessOrEqual(duracion, MaximoEncendido);

            Vector3 antes = montaje.transform.position;
            Press(teclado.wKey);
            yield return Esperar(SegundosMovimiento);
            Release(teclado.wKey);
            Assert.Greater(Vector3.Distance(antes, montaje.transform.position), MinimoMovido, "Después, W mueve al mecha");

            yield return Frenar();
            Press(raton.leftButton);
            FaseGolpe maxima = FaseGolpe.Listo;
            float fin = Time.time + 0.5f;
            while (Time.time < fin)
            {
                yield return null;
                if (golpe.Fase != FaseGolpe.Listo)
                {
                    maxima = golpe.Fase;
                }
            }
            Release(raton.leftButton);
            Assert.AreNotEqual(FaseGolpe.Listo, maxima, "Después, el clic golpea");
        }

        [UnityTest]
        public IEnumerator Bajar_YVolverASubir_PideElEncendidoOtraVez()
        {
            yield return CargarCalle();
            yield return Subir();
            yield return Encender();
            yield return EsperarHasta(() => peleador.ControlActivo, "control del piloto");
            yield return Esperar(PausaSistemasEnLinea);
            yield return Frenar();

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Encendido y detenido, la E baja");
            Assert.IsFalse(pantalla.Visible);

            yield return Subir();
            yield return null;

            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso, "Al volver a subir hay que encender de nuevo");
            Assert.IsFalse(peleador.ControlActivo);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(encendido.Config.TextoPaso1, pantalla.Texto.text);
            Assert.AreEqual(encendido.Config.DesplazamientoAgachado, cabina.DesplazamientoBase.y, 0.001f);
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "Sigue sin dejar bajar");
        }

        [UnityTest]
        public IEnumerator Encendiendo_ConUnGolpeDeDaniadorDePrueba_BajaLaVida()
        {
            yield return CargarCalle();
            yield return Subir();
            VidaMecha vida = montaje.GetComponent<VidaMecha>();
            float antes = vida.Actual;

            GameObject objeto = new GameObject("DaniadorDePrueba");
            objeto.SetActive(false);
            DaniadorDePrueba daniador = objeto.AddComponent<DaniadorDePrueba>();
            daniador.Configurar(montaje.gameObject, DanioPrueba,
                new InputAction("danar", InputActionType.Button, "<Keyboard>/h"));
            objeto.SetActive(true);
            yield return null;
            yield return Tecla(teclado.hKey);

            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso, "Sigue encendiendo");
            Assert.AreEqual(antes - DanioPrueba, vida.Actual, 0.01f, "Se puede pegar al mecha mientras enciende");
        }

        [UnityTest]
        public IEnumerator PelearPreparada_MientrasEnciende_LoDejaEncendidoAlInstante()
        {
            yield return CargarCalle();
            yield return Subir();
            Assert.IsFalse(encendido.EstaEncendido);

            peleador.PrepararPelea(new Pose(montaje.transform.position, montaje.transform.rotation));
            yield return null;

            Assert.IsTrue(encendido.EstaEncendido, "Una pelea de arena no pide encendido");
            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(0f, cabina.DesplazamientoBase.y, 0.001f);
        }

        [UnityTest]
        public IEnumerator Directo_EnPruebasTony_NoPideEncendidoYSeMueveDesdeElPrimerFrame()
        {
            yield return CargarEscena(RutaTony);
            BuscarMecha();

            Assert.IsTrue(encendido.EstaEncendido, "Un mecha Directo empieza encendido");
            Assert.IsTrue(peleador.ControlActivo);
            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(0f, cabina.DesplazamientoBase.y, 0.001f);

            Vector3 antes = montaje.transform.position;
            Press(teclado.wKey);
            yield return Esperar(SegundosMovimiento);
            Release(teclado.wKey);

            Assert.Greater(Vector3.Distance(antes, montaje.transform.position), MinimoMovido, "W mueve al mecha de inmediato");
        }
    }
}
