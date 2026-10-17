using System;
using System.Collections;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-134: la pantalla de ayuda de controles de la cabina. Pruebas_Tony (mecha directo, P arranca la pelea) y Pruebas_Calle
    /// (mecha estacionado, encendido real), con entrada simulada.
    /// </summary>
    public class AyudaCabinaPruebas : InputTestFixture
    {
        private const string RutaTony = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";
        private const string RutaCaptura = "docs/capturas/T-134-2.png";
        private const string VariableCaptura = "CAPTURAR_T134";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float EsperaMaxima = 30f;
        private const float DanioMortal = 100000f;
        private const float DistanciaInicialAlTaller = 5f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 8f;
        private const float MaxSegundosEspera = 8f;
        private const float RetrasoTecla = 0.1f;
        private const float SegundosEntreClics = 0.3f;
        private const float SegundosGuardia = 0.4f;
        private const float MargenSegundos = 1f;
        private const float MitadCentro = 0.2f;
        private const float CentroVista = 0.5f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private static readonly float[] Aspectos = { 16f / 9f, 16f / 10f, 4f / 3f, 21f / 9f };

        private Keyboard teclado;
        private Mouse raton;
        private DirectorPelea director;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private EncendidoMecha encendido;
        private PantallaAyudaCabina pantalla;
        private ConfigAyudas config;

        public override void Setup()
        {
            // Otras pruebas dejan acciones prendidas; se apagan antes de que el fixture reinicie el Input System.
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            AyudasVistas.Olvidar();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            config = AssetDatabase.LoadAssetAtPath<ConfigAyudas>(RutaConfig);
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            AyudasVistas.Olvidar();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
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

        private static IEnumerator CargarEscena(string ruta)
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

        private IEnumerator CargarTony()
        {
            yield return CargarEscena(RutaTony);
            director = UnityEngine.Object.FindFirstObjectByType<DirectorPelea>();
            Assert.IsNotNull(director, "Falta FlujoPelea en Pruebas_Tony");
            pantalla = UnityEngine.Object.FindFirstObjectByType<PantallaAyudaCabina>(FindObjectsInactive.Include);
            Assert.IsNotNull(pantalla, "MechaJugador trae la pantalla de ayuda");
        }

        private IEnumerator CargarCalle()
        {
            yield return CargarEscena(RutaCalle);
            jugador = UnityEngine.Object.FindFirstObjectByType<JugadorAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            montaje = UnityEngine.Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(montaje, "La escena trae el mecha");
            encendido = montaje.GetComponent<EncendidoMecha>();
            pantalla = montaje.GetComponentInChildren<PantallaAyudaCabina>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla de ayuda");
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

        private IEnumerator Clic(ButtonControl boton)
        {
            Press(boton);
            yield return null;
            Release(boton);
            yield return Esperar(SegundosEntreClics);
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string descripcion, float maximo = MaxSegundosEspera)
        {
            float fin = Time.time + maximo;
            while (Time.time < fin && !condicion())
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + descripcion);
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            yield return EsperarHasta(() => director.Estado == estado, "estado " + estado, EsperaMaxima);
        }

        private IEnumerator EmpezarPelea()
        {
            yield return Tecla(teclado.pKey);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return null;
        }

        private static void Danar(IPeleador peleador, float cantidad)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        private IEnumerator SubirYEncender()
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pelea_AlEmpezarLaPrimera_LaCabinaMuestraLaLineaDePelea()
        {
            yield return CargarTony();
            Assert.IsFalse(pantalla.Visible, "Antes de la pelea no se ve");

            yield return EmpezarPelea();

            Assert.IsTrue(pantalla.Visible, "La ayuda de pelea se ve en la cabina");
            Assert.AreEqual(ContextoAyuda.MechaPelea, pantalla.Contexto);
            Assert.AreEqual(config.Texto(ContextoAyuda.MechaPelea), pantalla.Texto.text);
        }

        [UnityTest]
        public IEnumerator Pelea_TresClicsIzquierdosYUnoDerechoMantenido_LaAyudaSeOculta()
        {
            yield return CargarTony();
            yield return EmpezarPelea();
            Assert.IsTrue(pantalla.Visible);

            yield return Clic(raton.leftButton);
            yield return Clic(raton.leftButton);
            yield return Clic(raton.leftButton);
            Assert.IsTrue(pantalla.Visible, "Faltó subir la guardia");

            Press(raton.rightButton);
            yield return Esperar(SegundosGuardia);
            Release(raton.rightButton);
            yield return null;

            Assert.IsFalse(pantalla.Visible, "Ya golpeó 3 veces y subió la guardia");
        }

        [UnityTest]
        public IEnumerator Pelea_SinHacerNada_LaAyudaSeOcultaA20Segundos()
        {
            yield return CargarTony();
            yield return EmpezarPelea();
            // La pelea sigue: la ayuda se va por tiempo, no por el fin de la pelea. El rival no puede matar al jugador en ese rato.
            float inicio = Time.time;
            yield return EsperarHasta(() => !pantalla.Visible || director.Estado != EstadoFlujoPelea.Peleando, "se oculta",
                config.SegundosMaximosPelea + MargenSegundos);
            if (director.Estado == EstadoFlujoPelea.Peleando)
            {
                Assert.GreaterOrEqual(Time.time - inicio, config.SegundosMaximosPelea - MargenSegundos, "No se fue antes de tiempo");
            }
            Assert.IsFalse(pantalla.Visible);
        }

        [UnityTest]
        public IEnumerator Pelea_LaSegundaDeLaSesion_NoMuestraLaAyuda()
        {
            yield return CargarTony();
            yield return EmpezarPelea();
            Assert.IsTrue(pantalla.Visible, "La primera sí");
            Danar(director.Rival, DanioMortal);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.IsFalse(pantalla.Visible, "Al terminar la pelea se va");

            yield return Tecla(teclado.rKey);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return null;

            Assert.IsFalse(pantalla.Visible, "La segunda pelea de la sesión no la muestra");
        }

        [UnityTest]
        public IEnumerator Pantalla_EnLaVista_QuedaFueraDelCuarentaPorCientoCentral()
        {
            yield return CargarTony();
            Camera camara = pantalla.GetComponentInParent<MontajeMecha>().GetComponentInChildren<Camera>();
            RectTransform rect = (RectTransform)pantalla.Lienzo.transform;
            Vector3[] esquinas = new Vector3[4];
            rect.GetWorldCorners(esquinas);
            try
            {
                foreach (float aspecto in Aspectos)
                {
                    camara.aspect = aspecto;
                    float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                    foreach (Vector3 esquina in esquinas)
                    {
                        Vector3 vista = camara.WorldToViewportPoint(esquina);
                        Assert.Greater(vista.z, 0f, "Está delante de la cámara");
                        minX = Mathf.Min(minX, vista.x);
                        maxX = Mathf.Max(maxX, vista.x);
                        minY = Mathf.Min(minY, vista.y);
                        maxY = Mathf.Max(maxY, vista.y);
                    }
                    bool tocaElCentro = maxX > CentroVista - MitadCentro && minX < CentroVista + MitadCentro
                        && maxY > CentroVista - MitadCentro && minY < CentroVista + MitadCentro;
                    Assert.IsFalse(tocaElCentro, "Aspecto " + aspecto + ": la pantalla toca el 40 % central (x " + minX + " a " + maxX
                        + ", y " + minY + " a " + maxY + ")");
                    Assert.Greater(minX, 0f, "Aspecto " + aspecto + ": entra por la izquierda de la vista");
                    Assert.Less(maxX, 1f, "Aspecto " + aspecto + ": entra por la derecha de la vista");
                    Assert.Greater(minY, 0f, "Aspecto " + aspecto + ": entra por abajo de la vista");
                }
            }
            finally
            {
                camara.ResetAspect();
            }
        }

        [UnityTest]
        public IEnumerator Calle_AlEncenderElMecha_MuestraLaLineaDeCalleYSeVaA12Segundos()
        {
            yield return CargarCalle();
            Assert.IsFalse(pantalla.Visible);

            yield return SubirYEncender();

            Assert.IsTrue(pantalla.Visible, "La ayuda de calle se ve al quedar encendido");
            Assert.AreEqual(ContextoAyuda.MechaCalle, pantalla.Contexto);
            Assert.AreEqual(config.Texto(ContextoAyuda.MechaCalle), pantalla.Texto.text);

            yield return EsperarHasta(() => !pantalla.Visible, "se oculta", config.SegundosMaximos + MargenSegundos);
            Assert.IsTrue(AyudasVistas.YaVista(ContextoAyuda.MechaCalle));
        }

        [UnityTest]
        public IEnumerator Captura_CabinaEnLaPelea_GuardaT134Dos()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableCaptura)))
            {
                Assert.Ignore("Sin " + VariableCaptura + " no se regraba la captura.");
            }
            yield return CargarTony();
            yield return EmpezarPelea();
            Assert.IsTrue(pantalla.Visible);
            yield return Esperar(0.5f);

            Camera camara = pantalla.GetComponentInParent<MontajeMecha>().GetComponentInChildren<Camera>();
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            RenderTexture anterior = camara.targetTexture;
            camara.targetTexture = destino;
            camara.Render();
            camara.targetTexture = anterior;
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            UnityEngine.Object.Destroy(imagen);
            destino.Release();
            UnityEngine.Object.Destroy(destino);
        }
    }
}
