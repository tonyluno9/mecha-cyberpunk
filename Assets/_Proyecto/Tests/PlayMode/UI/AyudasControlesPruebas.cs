using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
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
    /// T-134: la ayuda de controles del HUD a pie en Pruebas_APie (jugador, HUD y carro de prueba reales) y el llamado al mecha en
    /// Pruebas_Calle, con entrada simulada. Mide los rectángulos del HUD a 1920 x 1080 y 1280 x 720.
    /// </summary>
    public class AyudasControlesPruebas : InputTestFixture
    {
        private const string RutaAPie = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";
        private const string VariableCaptura = "CAPTURAR_T134";
        private const string RutaCaptura = "docs/capturas/T-134-1.png";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MargenSegundos = 1.5f;
        private const float DistanciaAlCarro = 1.5f;
        private const float AlturaJugador = 0.05f;
        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const float PesoMatch = 0.5f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const float DistanciaPlanoCanvas = 1f;
        private static readonly Vector2Int[] Resoluciones = { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) };

        private Keyboard teclado;
        private Mouse raton;
        private ConfigAyudas config;
        private JugadorAPie jugador;
        private AyudaControles ayuda;
        private HudAPie hud;

        public override void Setup()
        {
            // Otras pruebas dejan acciones prendidas; se apagan antes de que el fixture reinicie el Input System.
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            AyudasVistas.Olvidar();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            config = AssetDatabase.LoadAssetAtPath<ConfigAyudas>(RutaConfig);
        }

        public override void TearDown()
        {
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
            jugador = UnityEngine.Object.FindFirstObjectByType<JugadorAPie>();
            ayuda = UnityEngine.Object.FindFirstObjectByType<AyudaControles>(FindObjectsInactive.Include);
            hud = UnityEngine.Object.FindFirstObjectByType<HudAPie>(FindObjectsInactive.Include);
            Assert.IsNotNull(jugador, "La escena trae el jugador");
            Assert.IsNotNull(ayuda, "El HUD trae la ayuda de controles");
            Assert.IsNotNull(hud, "La escena trae el HUD a pie");
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

        private static IEnumerator EsperarHasta(Func<bool> condicion, string descripcion, float maximo)
        {
            float fin = Time.time + maximo;
            while (Time.time < fin && !condicion())
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + descripcion);
        }

        // Pone al jugador frente al punto de subida del carro, a DistanciaAlCarro, mirando a +Z.
        private CarroDePrueba PonerFrenteAlCarro()
        {
            CarroDePrueba carro = UnityEngine.Object.FindFirstObjectByType<CarroDePrueba>();
            Assert.IsNotNull(carro, "Pruebas_APie trae el carro de prueba");
            Vector3 punto = carro.PuntoSubida;
            jugador.Colocar(new Pose(new Vector3(punto.x, punto.y + AlturaJugador, punto.z - DistanciaAlCarro), Quaternion.identity));
            return carro;
        }

        [UnityTest]
        public IEnumerator APie_AlArrancar_LaAyudaDeCaminarSeVe()
        {
            yield return CargarEscena(RutaAPie);

            Assert.IsTrue(ayuda.Visible, "La ayuda a pie aparece al arrancar");
            Assert.AreEqual(ContextoAyuda.APie, ayuda.Contexto);
            Assert.AreEqual("WASD caminar · SHIFT correr · ESPACIO saltar · E usar · Q cubrirse · CTRL rodar", ayuda.Texto.text);
            Assert.IsTrue(ayuda.Grupo.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator APie_TresSegundosCaminandoConW_LaAyudaSeOculta()
        {
            yield return CargarEscena(RutaAPie);
            Assert.IsTrue(ayuda.Visible);
            Vector3 inicio = jugador.Raiz.position;

            Press(teclado.wKey);
            yield return Esperar(config.SegundosCaminando - MargenSegundos);
            Assert.IsTrue(ayuda.Visible, "A mitad de camino todavía se ve");
            yield return EsperarHasta(() => !ayuda.Visible, "se oculta tras caminar", config.SegundosCaminando + MargenSegundos);
            Release(teclado.wKey);

            Assert.Greater(Vector3.Distance(inicio, jugador.Raiz.position), 1f, "Sí caminó");
            Assert.IsTrue(AyudasVistas.YaVista(ContextoAyuda.APie));
        }

        [UnityTest]
        public IEnumerator APie_SinMoverse_LaAyudaSeOcultaA12Segundos()
        {
            yield return CargarEscena(RutaAPie);
            Assert.IsTrue(ayuda.Visible);

            yield return Esperar(config.SegundosMaximos - MargenSegundos);
            Assert.IsTrue(ayuda.Visible, "Antes de los 12 s sigue");
            yield return EsperarHasta(() => !ayuda.Visible, "se oculta por tiempo", 2f * MargenSegundos);
        }

        [UnityTest]
        public IEnumerator APie_OtroJugadorEnLaMismaSesion_NoReaparece()
        {
            yield return CargarEscena(RutaAPie);
            Assert.IsTrue(ayuda.Visible);
            Press(teclado.wKey);
            yield return EsperarHasta(() => !ayuda.Visible, "se oculta tras caminar", config.SegundosCaminando + MargenSegundos);
            Release(teclado.wKey);

            // Otro jugador en la misma sesión (por ejemplo, al volver de otra escena): la ayuda ya se vio.
            yield return CargarEscena(RutaAPie);
            yield return Esperar(0.5f);

            Assert.IsFalse(ayuda.Visible, "La ayuda a pie no reaparece");
            Assert.IsFalse(ayuda.Grupo.activeSelf);
        }

        [UnityTest]
        public IEnumerator Carro_AlSubir_MuestraLaAyudaDelCarroYManejarlaLaOculta()
        {
            yield return CargarEscena(RutaAPie);
            CarroDePrueba carro = PonerFrenteAlCarro();
            yield return Esperar(0.3f);

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "La E sube al carro");
            Assert.IsTrue(ayuda.Visible, "La ayuda del carro se ve al subir");
            Assert.AreEqual(ContextoAyuda.Carro, ayuda.Contexto);
            Assert.AreEqual("WASD o FLECHAS manejar · ESPACIO freno · E bajar (detenido)", ayuda.Texto.text); // D-tony-189

            Press(teclado.upArrowKey);
            yield return EsperarHasta(() => !ayuda.Visible, "se oculta tras manejar", config.SegundosManejando + MargenSegundos);
            Release(teclado.upArrowKey);

            Assert.Greater(Mathf.Abs(carro.Velocidad), 0.1f, "El carro se movió");
            Assert.IsTrue(AyudasVistas.YaVista(ContextoAyuda.Carro));
        }

        [UnityTest]
        public IEnumerator Carro_BajarYVolverASubir_LaAyudaDelCarroNoReaparece()
        {
            yield return CargarEscena(RutaAPie);
            CarroDePrueba carro = PonerFrenteAlCarro();
            yield return Esperar(0.3f);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(ayuda.Visible);
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "La E baja del carro quieto");

            PonerFrenteAlCarro();
            yield return Esperar(0.3f);
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugador.Modo);
            Assert.IsFalse(ayuda.Visible && ayuda.Contexto == ContextoAyuda.Carro, "La segunda subida no la muestra");
        }

        [UnityTest]
        public IEnumerator Llamar_EnLaZonaDeLlamado_MuestraLaLineaYLaMLaOculta()
        {
            AyudasVistas.Marcar(ContextoAyuda.APie);
            yield return CargarEscena(RutaCalle);
            yield return Esperar(0.3f);

            Assert.IsTrue(ayuda.Visible, "En el punto de llamado se ofrece la ayuda");
            Assert.AreEqual(ContextoAyuda.Llamar, ayuda.Contexto);
            Assert.AreEqual("M · llamar a tu mecha", ayuda.Texto.text);

            yield return Tecla(teclado.mKey);
            yield return null;

            Assert.IsFalse(ayuda.Visible, "Pulsó M: ya sabe la tecla");
            Assert.IsTrue(AyudasVistas.YaVista(ContextoAyuda.Llamar));
        }

        [UnityTest]
        public IEnumerator Llamar_SinMechaQueLlamar_NoSeOfrece()
        {
            AyudasVistas.Marcar(ContextoAyuda.APie);
            yield return CargarEscena(RutaAPie);
            yield return Esperar(0.5f);

            Assert.IsFalse(ayuda.Visible, "Pruebas_APie no tiene mecha que llamar: no se ofrece la ayuda");
        }

        [UnityTest]
        public IEnumerator Layout_LaAyudaAPie_NoTapaVidaObjetivoDineroNiAvisosA1920Y1280()
        {
            yield return CargarEscena(RutaAPie);
            Assert.IsTrue(ayuda.Visible);
            Canvas.ForceUpdateCanvases();
            RectTransform rectAyuda = ayuda.GetComponent<RectTransform>();

            var vecinos = new Dictionary<string, RectTransform>
            {
                { "Vida", hud.GrupoVida.GetComponent<RectTransform>() },
                { "Objetivo", hud.GrupoObjetivo.GetComponent<RectTransform>() },
                { "Dinero", hud.GrupoDinero.GetComponent<RectTransform>() },
                { "AvisoDinero", hud.GrupoAvisoDinero.GetComponent<RectTransform>() },
                { "Renta", hud.GrupoRenta.GetComponent<RectTransform>() },
                { "Interaccion", hud.GrupoInteraccion.GetComponent<RectTransform>() },
                { "Llamado", hud.GrupoLlamado.GetComponent<RectTransform>() },
                { "Calor", hud.GrupoCalor.GetComponent<RectTransform>() },
                { "Cancion", hud.GrupoCancion.GetComponent<RectTransform>() },
                { "Arma", hud.GrupoArma.GetComponent<RectTransform>() },
            };

            foreach (Vector2Int resolucion in Resoluciones)
            {
                Rect pantalla = new Rect(0f, 0f, resolucion.x, resolucion.y);
                Rect rect = RectEnPantalla(rectAyuda, resolucion);
                Assert.IsTrue(pantalla.Contains(rect.min) && pantalla.Contains(rect.max),
                    resolucion + ": la ayuda queda dentro de la pantalla: " + rect);
                foreach (KeyValuePair<string, RectTransform> vecino in vecinos)
                {
                    Rect otro = RectEnPantalla(vecino.Value, resolucion);
                    Assert.IsFalse(rect.Overlaps(otro), resolucion + ": la ayuda no se superpone con " + vecino.Key + " (" + rect + " contra " + otro + ")");
                }
            }
        }

        [UnityTest]
        public IEnumerator Layout_LasTresLineasDelHud_CabenEnElRectanguloDeLaAyuda()
        {
            yield return CargarEscena(RutaAPie);
            RectTransform rectAyuda = ayuda.GetComponent<RectTransform>();
            Text texto = ayuda.Texto;
            foreach (ContextoAyuda contexto in new[] { ContextoAyuda.APie, ContextoAyuda.Carro, ContextoAyuda.Llamar })
            {
                texto.text = config.Texto(contexto);
                Canvas.ForceUpdateCanvases();
                Assert.LessOrEqual(texto.preferredHeight, rectAyuda.rect.height + 0.01f, contexto + ": el texto cabe en el rectángulo");
            }
        }

        [UnityTest]
        public IEnumerator Captura_HudAPieConLaAyuda_GuardaT134Uno()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableCaptura)))
            {
                Assert.Ignore("Sin " + VariableCaptura + " no se regraba la captura.");
            }
            yield return CargarEscena(RutaAPie);
            Assert.IsTrue(ayuda.Visible);
            yield return Esperar(0.5f);
            // En batchmode no hay pantalla que capturar: el HUD pasa a un canvas de cámara y se renderiza a una textura.
            Canvas lienzo = hud.GetComponent<Canvas>();
            Camera camara = jugador.Camara;
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = camara;
            lienzo.planeDistance = DistanciaPlanoCanvas;
            yield return null;
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

        // Rectángulo en píxeles de un grupo del HUD (anclas iguales) con el CanvasScaler a esa resolución:
        // escala = (ancho / 1920) ^ 0.5 * (alto / 1080) ^ 0.5.
        private static Rect RectEnPantalla(RectTransform rect, Vector2Int resolucion)
        {
            Assert.AreEqual(rect.anchorMin, rect.anchorMax, rect.name + ": las anclas deben ser iguales");
            float escala = Mathf.Pow(resolucion.x / AnchoReferencia, PesoMatch) * Mathf.Pow(resolucion.y / AltoReferencia, PesoMatch);
            Vector2 lienzo = new Vector2(resolucion.x, resolucion.y) / escala;
            Vector2 tamano = rect.sizeDelta;
            Vector2 esquina = Vector2.Scale(rect.anchorMin, lienzo) + rect.anchoredPosition - Vector2.Scale(rect.pivot, tamano);
            return new Rect(esquina * escala, tamano * escala);
        }
    }
}
