using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// Base de las pruebas de T-193: carga Pruebas_Implantes con entrada simulada y vigila consola y tirones de cuadro.
    /// Atajos permitidos y anotados: colocar al jugador frente a un sitio y fijar dinero, liga e implantes por código.
    /// </summary>
    public abstract class BaseImplantesPruebas : InputTestFixture
    {
        protected const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Implantes.unity";
        protected const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset";
        protected const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/Implantes/CatalogoImplantes_Base.asset";
        protected const string RutaDatosPersonaje = "Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset";
        protected const string RutaJugadorPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        protected const string VariableCaptura = "CAPTURAR_T193";
        protected const int FramesEstables = 10;
        protected const int MaxFramesCalentamiento = 1000;
        protected const int FramesParaElHud = 5;
        protected const int FramesSinMedirTrasEscena = 12;
        protected const int AnchoCaptura = 1280;
        protected const int AltoCaptura = 720;
        protected const int ProfundidadCaptura = 24;
        protected const float DeltaTimeEstable = 0.05f;
        protected const float AlturaInicio = 0.05f;
        protected const float DistanciaMostrador = 2f;
        protected const float MaxSegundosAviso = 3f;
        protected const float MaxSegundosOperacion = 20f;
        protected const float DistanciaPlanoCanvas = 0.35f;
        protected const float SegundosAndando = 0.5f;
        protected const float MinimoAvance = 0.3f;
        protected const int HoraNoche = 21;
        protected const int DineroAlto = 10000;

        protected Keyboard teclado;
        protected Mouse raton;
        protected string carpeta;
        protected HashSet<GameObject> raicesIniciales;
        protected readonly List<string> errores = new List<string>();
        protected readonly List<string> atajos = new List<string>();
        protected readonly MedidorFrames medidor = new MedidorFrames();
        protected int framesSinMedir;

        protected JugadorAPie jugador;
        protected ControladorAPie controlador;
        protected HudAPie hud;
        protected ServicioImplantes implantes;
        protected ServicioLigas ligas;
        protected ICartera cartera;
        protected IRelojMundo reloj;
        protected MostradorClinica mostrador;
        protected PantallaClinica pantalla;
        protected ConfigPantallaClinica config;
        protected CatalogoImplantes catalogo;

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "implantes_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            atajos.Clear();
            medidor.Reiniciar();
            framesSinMedir = 0;
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(mensaje);
        }

        protected void Vigilar()
        {
            if (framesSinMedir > 0) framesSinMedir--;
            else medidor.Registrar(Time.unscaledDeltaTime);
        }

        protected IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                yield return null;
                Vigilar();
            }
        }

        protected IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Vigilar();
            Release(tecla);
            yield return null;
            Vigilar();
        }

        protected IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            framesSinMedir = FramesSinMedirTrasEscena;
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                Vigilar();
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            mostrador = Object.FindFirstObjectByType<MostradorClinica>();
            Assert.IsNotNull(jugador, "Pruebas_Implantes trae al jugador");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(mostrador, "La escena trae el mostrador de la clínica");
            pantalla = mostrador.Pantalla;
            controlador = jugador.GetComponent<ControladorAPie>();
            implantes = Registro<ServicioImplantes>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            cartera = Registro<ICartera>.Actual;
            reloj = Registro<IRelojMundo>.Actual;
            config = AssetDatabase.LoadAssetAtPath<ConfigPantallaClinica>(RutaConfig);
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoImplantes>(RutaCatalogo);
            Assert.IsNotNull(implantes, "Hay servicio de implantes");
            Assert.IsNotNull(ligas, "Hay servicio de ligas");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(reloj, "Hay reloj de prueba");
            Assert.IsNotNull(config, "Existe la config de la clínica");
            Assert.IsNotNull(catalogo, "Existe el catálogo de implantes");
            yield return Cuadros(FramesParaElHud);
        }

        protected void PonerDinero(int objetivo)
        {
            int diferencia = objetivo - cartera.Dinero;
            if (diferencia > 0) cartera.Abonar(diferencia, MotivoDinero.Otro);
            else if (diferencia < 0) cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            Assert.AreEqual(objetivo, cartera.Dinero, "dinero de salida");
            atajos.Add("Dinero fijado en " + objetivo);
        }

        protected void PonerLiga(int liga)
        {
            Assert.IsTrue(ligas.Poner(liga, 1, liga - 1), "se pudo fijar la liga " + liga);
            atajos.Add("Liga fijada en " + liga);
        }

        protected void CargarImplantes(params string[] ids)
        {
            implantes.Cargar("{\"ids\":[\"" + string.Join("\",\"", ids) + "\"]}");
            atajos.Add("Implantes por código: " + string.Join(",", ids));
        }

        protected DatosImplante Datos(EspacioImplante espacio, int nivel)
        {
            DatosImplante datos = catalogo.Buscar(espacio, nivel);
            Assert.IsNotNull(datos, "el catálogo trae " + espacio + " " + nivel);
            return datos;
        }

        protected static float Normalizar(float hora)
        {
            float h = hora % 24f;
            return h < 0f ? h + 24f : h;
        }

        // Pone al jugador frente al mostrador mirando al norte y espera que el HUD ofrezca la E.
        protected IEnumerator IrAlMostrador(string textoEsperado)
        {
            jugador.Colocar(new Pose(mostrador.Posicion + new Vector3(0f, AlturaInicio, -DistanciaMostrador), Quaternion.identity));
            atajos.Add("Colocar: frente al mostrador de la clínica");
            yield return Cuadros(FramesParaElHud);
            float fin = Time.time + MaxSegundosAviso;
            while (hud.TextoAvisoInteraccion.text != textoEsperado && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.AreEqual(textoEsperado, hud.TextoAvisoInteraccion.text, "el HUD dice lo que ofrece la E");
        }

        protected IEnumerator AbrirClinica()
        {
            yield return IrAlMostrador("E · " + config.TextoMostrador);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla de la clínica");
            Assert.IsFalse(controlador.ControlActivo, "con la pantalla abierta el control a pie queda congelado");
        }

        protected IEnumerator EsperarOperacion()
        {
            float fin = Time.time + MaxSegundosOperacion;
            while (mostrador.Operando && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.IsFalse(mostrador.Operando, "la operación terminó");
        }

        protected void CapturarConHud(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1") return;
            Camera camara = jugador.Camara;
            var movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                lienzo.worldCamera = camara;
                lienzo.planeDistance = DistanciaPlanoCanvas;
                movidos.Add(lienzo);
            }
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            rt.Release();
            Object.Destroy(rt);
            foreach (Canvas lienzo in movidos) lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[T-193] captura en " + ruta);
        }

        protected void VerificarFinal(string nombre)
        {
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
            Assert.IsTrue(medidor.Cumple, "sin tirones: " + medidor.Resumen());
            Debug.Log("[T-193] " + nombre + " atajos: " + string.Join("; ", atajos) + " | " + medidor.Resumen());
        }
    }
}
