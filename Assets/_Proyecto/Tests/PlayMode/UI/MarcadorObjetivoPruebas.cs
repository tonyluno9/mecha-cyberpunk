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
        private const float FrenteCuarto = 0.55f;
        private const float SegundosApertura = 1.1f;
        private const float SegundosTramo = 12f;
        private const float LlegadaTramo = 0.3f;
        private const float VelocidadSalida = 2.5f;
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

            // d. Sale caminando por las puertas reales (T-804: puerta del cuarto, pasillo y puerta de la calle, cada una con E):
            // el texto cambia a DEFIENDE A DOÑA LUPE.
            yield return SalirDeLaCasa(jugador, () => director.Paso != PasoHistoria.Despierta);
            yield return null;
            yield return null;
            Assert.AreEqual(PasoHistoria.Asalto, director.Paso, "Salió de la casa");
            Assert.AreEqual(AsaltoDinero.TextoPerseguir, hud.TextoObjetivo.text, "T-717: el texto cambia a PERSIGUE AL ASALTANTE");
            Assert.IsTrue(marcador.RomboVisible || marcador.FlechaVisible, "El marcador ahora señala al asaltante");
        }

        // T-804: del cuarto a la banqueta por las dos puertas de la casa, como SalidaCasaPruebas (Progresion): camina con el
        // CharacterController del jugador y abre cada puerta con IntentarAbrir, que es lo que hace la E.
        private static IEnumerator SalirDeLaCasa(IJugador jugador, System.Func<bool> listo)
        {
            Vector3 cuarto = GameObject.Find("Cuarto_Habitacion").transform.position;
            IPuerta[] puertas = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(
                System.Linq.Enumerable.Where(Registro<IPuerta>.Todos, p => p.Lugar == IdLugar.Casa), p => Vector3.Distance(p.Posicion, cuarto)));
            Assert.AreEqual(2, puertas.Length, "La casa tiene la puerta del cuarto y la de la calle");
            IPuerta delCuarto = puertas[0];
            IPuerta calle = puertas[1];
            Vector3[] tramo1 = { delCuarto.Posicion + Vector3.forward * FrenteCuarto + Vector3.left * 0.9f, delCuarto.Posicion + Vector3.forward * FrenteCuarto };
            Vector3[] tramo2 = { delCuarto.Posicion + Vector3.back, calle.Posicion + Vector3.left * 1.1f };
            Vector3[] tramo3 = { calle.Posicion + Vector3.right * 2.5f };
            foreach ((Vector3[] puntos, IPuerta puerta) paso in new[] { (tramo1, delCuarto), (tramo2, calle), (tramo3, (IPuerta)null) })
            {
                foreach (Vector3 destino in paso.puntos)
                {
                    yield return CaminarHasta(jugador, destino, listo);
                }
                if (listo() || paso.puerta == null)
                {
                    yield break;
                }
                Assert.AreEqual(ResultadoPuerta.Abierta, paso.puerta.IntentarAbrir(), "Abre la puerta de la casa");
                yield return new WaitForSeconds(SegundosApertura);
            }
        }

        private static IEnumerator CaminarHasta(IJugador jugador, Vector3 destino, System.Func<bool> listo)
        {
            CharacterController cc = ((Component)jugador).GetComponent<CharacterController>();
            float fin = Time.time + SegundosTramo;
            Vector3 falta = destino - jugador.Raiz.position;
            falta.y = 0f;
            while (falta.magnitude > LlegadaTramo && !listo())
            {
                Assert.Less(Time.time, fin, "No llegó caminando a " + destino + " (está en " + jugador.Raiz.position + ")");
                cc.Move(Vector3.ClampMagnitude(falta.normalized * VelocidadSalida * Time.deltaTime, falta.magnitude) + Vector3.down * Time.deltaTime);
                yield return null;
                falta = destino - jugador.Raiz.position;
                falta.y = 0f;
            }
        }
    }
}
