using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Montaje;
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

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-170: la pantalla de piezas de la cabina. Escena Pruebas_Calle con el jugador real, entrada simulada (W, E, F, Espacio, Q):
    /// lista las 5 piezas desde que se prenden las luces de la cabina, sigue durante el encendido, se apaga 5 s después y no tapa
    /// el 40 % x 40 % central de la vista.
    /// </summary>
    public class PantallaPiezasCabinaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const float DistanciaInicialAlTaller = 5f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 8f;
        private const float MaxSegundosEspera = 8f;
        private const float RetrasoTecla = 0.1f;
        private const float MargenApagado = 1f;
        private const float SegundosDespuesEsperados = 5f;
        private const int NumeroPiezas = 5;
        private const float MitadCentro = 0.2f;
        private const string VariableCaptura = "CAPTURAR_T170";
        private const string RutaCaptura1 = "docs/capturas/T-170-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-170-2.png";
        private const float DistanciaCapturaFuera = 9f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const float SegundosAsentar = 0.5f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;

        private Keyboard teclado;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private EncendidoMecha encendido;
        private PantallaPiezasCabina pantalla;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
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

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = UnityEngine.Object.FindFirstObjectByType<JugadorAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            montaje = UnityEngine.Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(montaje, "La escena trae el mecha");
            encendido = montaje.GetComponent<EncendidoMecha>();
            pantalla = montaje.GetComponentInChildren<PantallaPiezasCabina>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla de piezas de la cabina");
            Transform mecha = montaje.transform;
            jugador.Colocar(new Pose(mecha.position + mecha.forward * DistanciaInicialAlTaller, Quaternion.LookRotation(-mecha.forward)));
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

        private static int Lineas(string texto)
        {
            return texto.Split('\n').Length;
        }

        [UnityTest]
        public IEnumerator Encender_ConEFEspacioYQ_ListaLas5PiezasDuranteElEncendidoYSeApagaDespues()
        {
            yield return CargarCalle();
            Assert.IsFalse(pantalla.Visible, "Sin subir, apagada");

            yield return Subir();
            yield return null;
            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso);
            Assert.IsFalse(pantalla.Visible, "En el paso 1 la cabina sigue a oscuras");

            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.fKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, "paso 2");
            yield return null;
            Assert.IsTrue(pantalla.Visible, "Con las luces de la cabina, la pantalla se prende");
            Assert.AreEqual(NumeroPiezas, Lineas(pantalla.Texto.text), "Lista las 5 piezas:\n" + pantalla.Texto.text);

            Press(teclado.spaceKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso3, "paso 3");
            Release(teclado.spaceKey);
            yield return Esperar(RetrasoTecla);
            Assert.IsTrue(pantalla.Visible, "Sigue en el paso 3");

            yield return Tecla(teclado.qKey);
            yield return EsperarHasta(() => encendido.EstaEncendido, "encendido completo");
            yield return Esperar(SegundosDespuesEsperados - MargenApagado);
            Assert.IsTrue(pantalla.Visible, "Sigue encendida casi 5 s después del encendido");
            yield return Esperar(2f * MargenApagado);
            Assert.IsFalse(pantalla.Visible, "Se apaga 5 s después del encendido");
        }

        [UnityTest]
        public IEnumerator Encender_ConPiezaDeFuego_ElNombreSaleEnElColorDeSuRama()
        {
            yield return CargarCalle();
            EquipoMecha equipo = montaje.GetComponent<EquipoMecha>();
            Assert.IsNotNull(equipo, "El mecha trae EquipoMecha");
            DatosPieza fuego = equipo.Catalogo.Buscar("pieza_brazoizq_fuego_2");
            Assert.IsNotNull(fuego);
            DatosPieza anterior = equipo.Puesta(RanuraPieza.BrazoIzquierdo);
            equipo.Equipar(fuego);
            yield return null;

            Assert.IsTrue(pantalla.Texto.text.Contains(fuego.Nombre), "Lista el nombre de la pieza");
            Assert.IsTrue(pantalla.Texto.text.Contains("<color=#"), "Cada nombre lleva el color de su rama");
            Assert.AreEqual(NumeroPiezas, Lineas(pantalla.Texto.text));
            Assert.IsFalse(pantalla.Texto.text.Contains("$"), "No muestra dinero");

            equipo.Equipar(anterior);
        }

        [UnityTest]
        public IEnumerator Captura_CabinaYMechaEstacionado_GuardaT170UnoYDos()
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                Assert.Ignore("Sin " + VariableCaptura + "=1 no se regraban las capturas.");
            }
            yield return CargarCalle();
            EquipoMecha equipo = montaje.GetComponent<EquipoMecha>();
            DatosPieza izquierdo = equipo.Puesta(RanuraPieza.BrazoIzquierdo);
            DatosPieza derecho = equipo.Puesta(RanuraPieza.BrazoDerecho);
            DatosPieza torso = equipo.Puesta(RanuraPieza.Torso);
            DatosPieza cabina = equipo.Puesta(RanuraPieza.Cabina);
            equipo.Equipar(equipo.Catalogo.Buscar("pieza_brazoizq_fuego_2"));
            equipo.Equipar(equipo.Catalogo.Buscar("pieza_brazoder_militar_4"));
            equipo.Equipar(equipo.Catalogo.Buscar("pieza_torso_tech_1"));
            equipo.Equipar(equipo.Catalogo.Buscar("pieza_cabina_cuerpoacuerpo_1"));
            yield return null;

            // Captura 2: el mecha estacionado desde fuera, con la cámara del jugador a pie mirándolo.
            Camera camaraPie = jugador.Camara;
            Transform mecha = montaje.transform;
            jugador.Colocar(new Pose(mecha.position + mecha.forward * DistanciaCapturaFuera, Quaternion.LookRotation(-mecha.forward)));
            yield return null;
            yield return null;
            Capturar(camaraPie, RutaCaptura2);

            // Captura 1: dentro, durante el encendido, con la pantalla de piezas prendida.
            yield return Subir();
            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.fKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, "paso 2");
            Press(teclado.spaceKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso3, "paso 3");
            Release(teclado.spaceKey);
            yield return Esperar(SegundosAsentar);
            Assert.IsTrue(pantalla.Visible);
            Capturar(montaje.GetComponentInChildren<Camera>(true), RutaCaptura1);

            equipo.Equipar(izquierdo);
            equipo.Equipar(derecho);
            equipo.Equipar(torso);
            equipo.Equipar(cabina);
        }

        private static void Capturar(Camera camara, string ruta)
        {
            var movidos = new System.Collections.Generic.List<Canvas>();
            foreach (Canvas lienzo in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = DistanciaPlanoCanvas;
                    movidos.Add(lienzo);
                }
            }
            var destino = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            var imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            string completa = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", ruta));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(completa));
            System.IO.File.WriteAllBytes(completa, imagen.EncodeToPNG());
            UnityEngine.Object.Destroy(imagen);
            destino.Release();
            UnityEngine.Object.Destroy(destino);
        }

        [UnityTest]
        public IEnumerator Encendiendo_PantallaEncendida_NoTapaElCentroDeLaVista()
        {
            yield return CargarCalle();
            yield return Subir();
            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.fKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, "paso 2");
            yield return null;
            Assert.IsTrue(pantalla.Visible);

            Camera camara = montaje.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(camara, "El mecha trae su cámara");
            var esquinas = new Vector3[4];
            ((RectTransform)pantalla.Lienzo.transform).GetWorldCorners(esquinas);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector3 esquina in esquinas)
            {
                Vector3 vista = camara.WorldToViewportPoint(esquina);
                Assert.Greater(vista.z, 0f, "La pantalla está delante de la cámara");
                minX = Mathf.Min(minX, vista.x);
                maxX = Mathf.Max(maxX, vista.x);
                minY = Mathf.Min(minY, vista.y);
                maxY = Mathf.Max(maxY, vista.y);
            }
            float centro = 0.5f;
            bool tapaCentro = minX < centro + MitadCentro && maxX > centro - MitadCentro
                && minY < centro + MitadCentro && maxY > centro - MitadCentro;
            Assert.IsFalse(tapaCentro, "La pantalla no toca el 40 % x 40 % central: x " + minX + ".." + maxX + ", y " + minY + ".." + maxY);
        }
    }
}
