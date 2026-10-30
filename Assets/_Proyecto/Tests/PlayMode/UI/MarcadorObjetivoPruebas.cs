using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
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
    /// T-408 en Ciudad_Prologo, como un jugador nuevo: al despertar el objetivo es la puerta; con W el rombo se ve sobre ella y la distancia baja;
    /// al girar 180° con el ratón aparece la flecha en el borde (a 64 px) del lado correcto; M abre el mapa con el rombo del objetivo y M lo cierra;
    /// al salir de la casa el objetivo cambia a "DEFIENDE A DOÑA LUPE". Con CAPTURAR_T408=1 guarda docs/capturas/T-408-*.png en 1080p y 1440p.
    /// </summary>
    public class MarcadorObjetivoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T408";
        private const float AlturaRombo = 2.2f;
        private const float ToleranciaRombo = 10f;
        private const float MargenFlecha = 64f;
        private const float ToleranciaFlecha = 2f;
        private const float DeltaGiro = 40f;
        private const float AnchoObjetivo = 1100f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
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

        // Ángulo horizontal (°) entre hacia dónde mira la cámara y el punto.
        private static float AnguloA(Camera camara, Vector3 punto)
        {
            Vector3 hacia = punto - camara.transform.position;
            hacia.y = 0f;
            Vector3 adelante = camara.transform.forward;
            adelante.y = 0f;
            return Vector3.Angle(adelante, hacia);
        }

        private static float AnchoDeUnaLinea(Text texto, string contenido)
        {
            TextGenerationSettings ajustes = texto.GetGenerationSettings(Vector2.zero);
            ajustes.scaleFactor = 1f;
            return texto.cachedTextGeneratorForLayout.GetPreferredWidth(contenido, ajustes);
        }

        [UnityTest]
        public IEnumerator RecorridoJugadorNuevo()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<IObjetivos>.Actual != null;
            }, "el director arrancó", 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador.Camara;
            MarcadorObjetivo marcador = UnityEngine.Object.FindFirstObjectByType<MarcadorObjetivo>();
            HudAPie hud = UnityEngine.Object.FindFirstObjectByType<HudAPie>();
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(camara, "El jugador tiene cámara");
            Assert.IsNotNull(marcador, "El HUD trae el marcador del objetivo");
            Assert.IsNotNull(hud);
            Assert.IsNotNull(mapa);
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso);
            Assert.AreEqual(DirectorPandillas.TextoSalir, hud.TextoObjetivo.text, "Al despertar: SAL A LA CALLE");
            Assert.IsTrue(Registro<IObjetivos>.Actual.Actual.TienePosicion);
            Vector3 puerta = Registro<IObjetivos>.Actual.Actual.Posicion;

            // El texto de arriba cabe en una línea en 1080p: el de ahora y todos los de la historia.
            foreach (string texto in DirectorPandillas.TextosObjetivo)
            {
                Assert.Less(AnchoDeUnaLinea(hud.TextoObjetivo, texto), AnchoObjetivo, "Cabe en una línea: " + texto);
            }

            // a. Mira hacia la puerta (como quien busca la salida) y camina con W: el rombo está sobre ella y la distancia baja.
            Vector3 hacia = puerta - jugador.Raiz.position;
            hacia.y = 0f;
            jugador.Colocar(new Pose(jugador.Raiz.position, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return null;
            yield return null;
            Assert.IsTrue(marcador.RomboVisible, "Con la puerta enfrente se ve el rombo (distancia " + marcador.Distancia + " m)");
            Assert.IsFalse(marcador.FlechaVisible);
            Vector3 proyectado = camara.WorldToScreenPoint(puerta + Vector3.up * AlturaRombo);
            float error = Vector2.Distance(new Vector2(proyectado.x, proyectado.y), marcador.Rombo.position);
            Assert.Less(error, ToleranciaRombo, "El rombo queda donde se proyecta el objetivo (px)");
            float distanciaInicial = marcador.Distancia;
            StringAssert.EndsWith(" m", marcador.TextoDistancia);

            Press(teclado.wKey);
            float finCamino = Time.realtimeSinceStartup + 3f;
            while (director.Paso == PasoHistoria.Despierta && marcador.Distancia > distanciaInicial - 1f && Time.realtimeSinceStartup < finCamino)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso, "Todavía no sale (se detuvo antes de la puerta)");
            Assert.Less(marcador.Distancia, distanciaInicial - 0.5f, "La distancia baja al caminar hacia la puerta");
            Assert.IsTrue(marcador.RomboVisible, "Sigue el rombo");
            Capturar("T-408-1", camara);

            // b. El ratón gira la cámara 180°: el rombo se apaga y la flecha queda en el borde, del lado del objetivo.
            float finGiro = Time.realtimeSinceStartup + 5f;
            while (AnguloA(camara, puerta) < 170f && Time.realtimeSinceStartup < finGiro)
            {
                Set(raton.delta, new Vector2(DeltaGiro, 0f));
                yield return null;
            }
            Set(raton.delta, Vector2.zero);
            yield return null;
            yield return null;
            Assert.Greater(AnguloA(camara, puerta), 160f, "Quedó de espaldas a la puerta");
            Assert.IsFalse(marcador.RomboVisible, "De espaldas no hay rombo");
            Assert.IsTrue(marcador.FlechaVisible, "De espaldas sale la flecha del borde");
            Vector2 lienzo = marcador.Lienzo.rect.size;
            Vector2 p = marcador.Flecha.anchoredPosition;
            float alBorde = Mathf.Min(Mathf.Min(p.x, lienzo.x - p.x), Mathf.Min(p.y, lienzo.y - p.y));
            Assert.AreEqual(MargenFlecha, alBorde, ToleranciaFlecha, "La flecha queda a 64 px del borde");
            Vector3 local = camara.transform.InverseTransformPoint(puerta + Vector3.up * AlturaRombo);
            if (Mathf.Abs(local.x) > 0.3f)
            {
                Assert.AreEqual(Mathf.Sign(local.x), Mathf.Sign(p.x - lienzo.x * 0.5f), "La flecha queda del lado del objetivo");
                Assert.AreEqual(Mathf.Sign(local.x), Mathf.Sign(Mathf.Cos(marcador.AnguloFlecha * Mathf.Deg2Rad)), "Y apunta hacia ese lado");
            }
            Assert.Greater(Vector2.Distance(p, lienzo * 0.5f), lienzo.y * 0.3f, "Nunca en el centro");
            Capturar("T-408-2", camara);

            // c. M abre el mapa con el rombo del objetivo en su lugar; M lo cierra.
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto, "M abre el mapa");
            yield return null;
            Assert.IsTrue(mapa.RomboObjetivoVisible, "El mapa muestra el rombo del objetivo");
            Vector2 enMapa = new Vector2(puerta.x, puerta.z) * mapa.PixelesPorMetro;
            Assert.Less(Vector2.Distance(enMapa, mapa.RomboObjetivo.anchoredPosition), 1f, "El rombo está donde está la puerta");
            Capturar("T-408-3", camara);
            yield return Pulsar(teclado.mKey);
            Assert.IsFalse(mapa.Abierto, "M lo cierra");

            // d. Vuelve a mirar la puerta y sale caminando: el texto cambia a DEFIENDE A DOÑA LUPE.
            hacia = puerta - jugador.Raiz.position;
            hacia.y = 0f;
            jugador.Colocar(new Pose(jugador.Raiz.position, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return null;
            Press(teclado.wKey);
            finCamino = Time.realtimeSinceStartup + 20f;
            while (director.Paso == PasoHistoria.Despierta && Time.realtimeSinceStartup < finCamino)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            yield return null;
            Assert.AreEqual(PasoHistoria.PrimeraPelea, director.Paso, "Salió de la casa");
            Assert.AreEqual(DirectorPandillas.TextoDefender, hud.TextoObjetivo.text, "El texto cambia a DEFIENDE A DOÑA LUPE");
            Assert.IsTrue(marcador.RomboVisible || marcador.FlechaVisible, "El marcador ahora señala a DOÑA LUPE");
        }
    }
}
