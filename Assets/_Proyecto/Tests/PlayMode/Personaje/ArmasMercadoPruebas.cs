using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-416: el recorrido de un jugador con Ƀ3000 en Ciudad_Prologo. Camina a El Mercado, E abre la tienda, compra FUSIL, SNIPER y SNIPER I con
    /// flechas y Enter, 5 saca el sniper, clic derecho hace zoom (FOV 15, mira prendida), clic izquierdo tumba a un blanco de 60 de vida a 80 m,
    /// sin zoom casi todo falla, R recarga a 5 balas y 4 saca el fusil, que dispara solo mientras se mantiene el clic.
    /// Con CAPTURAR_T416=1 deja docs/capturas/T-416-1.png (mira del sniper, de noche) y T-416-2.png (El Mercado).
    /// </summary>
    public class ArmasMercadoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T416";
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const int Dinero = 3000;
        private const float DistanciaBlanco = 80f;
        private const float VidaBlanco = 60f;
        private const float LateralApuntando = 0.7f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject plataforma;
        private GameObject blanco;

        /// <summary>Blanco simple de 60 de vida: cuenta los tiros que le pegan.</summary>
        private class BlancoDeTiro : MonoBehaviour, IRecibeDanio
        {
            public float Vida = VidaBlanco;
            public int Impactos;
            public bool KO => Vida <= 0f;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Impactos++;
                Vida -= info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, Vida <= 0f);
            }
        }

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            UnityEngine.Random.InitState(Environment.TickCount); // no deja la semilla fija a las pruebas que siguen
            if (plataforma != null)
            {
                Object.Destroy(plataforma);
            }
            if (blanco != null)
            {
                Object.Destroy(blanco);
            }
            base.TearDown();
        }

        // La escena de la ciudad no debe quedar viva para las pruebas que siguen.
        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
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
        }

        private static Pose Mirando(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = hacia - desde;
            dir.y = 0f;
            return new Pose(desde, Quaternion.LookRotation(dir.normalized, Vector3.up));
        }

        // La fila del arma en su nivel base (no la de "<nombre> I").
        private static int FilaBase(PantallaTienda pantalla, string nombre)
        {
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                string texto = pantalla.TextoFila(i).ToUpperInvariant();
                if (texto.Contains(nombre) && !texto.Contains(nombre + " I"))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FilaCon(PantallaTienda pantalla, string nombre)
        {
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoFila(i).ToUpperInvariant().Contains(nombre))
                {
                    return i;
                }
            }
            return -1;
        }

        // Elige la fila con las flechas y compra con Enter.
        private IEnumerator Comprar(PantallaTienda pantalla, int fila, string que)
        {
            Assert.GreaterOrEqual(fila, 0, "La tienda ofrece " + que);
            int pasos = 0;
            while (pantalla.IndiceElegido != fila && pasos++ < 60)
            {
                yield return Pulsar(pantalla.IndiceElegido < fila ? teclado.downArrowKey : teclado.upArrowKey);
            }
            Assert.AreEqual(fila, pantalla.IndiceElegido, "Las flechas llegan a " + que);
            yield return Pulsar(teclado.enterKey);
        }

        private void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = Registro<IJugador>.Actual.Camara;
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay && lienzo.enabled)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = 1f;
                    movidos.Add(lienzo);
                }
            }
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            Texture2D imagen = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
        }

        private static Transform Buscar(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        private BlancoDeTiro PonerBlanco(Vector3 posicion)
        {
            if (blanco != null)
            {
                Object.Destroy(blanco);
            }
            blanco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            blanco.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            blanco.transform.position = posicion + Vector3.up * 0.9f;
            Physics.SyncTransforms();
            return blanco.AddComponent<BlancoDeTiro>();
        }

        [UnityTest]
        public IEnumerator RecorridoFusilYSniper()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null
                && Registro<ICartera>.Actual != null, "jugador, arsenal y cartera", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ArmasAPie armas = Object.FindFirstObjectByType<ArmasAPie>();
            ControladorAPie controlador = armas.GetComponent<ControladorAPie>();
            ZoomSniper zoom = armas.GetComponent<ZoomSniper>();
            MiraSniper mira = Object.FindFirstObjectByType<MiraSniper>();
            Assert.IsNotNull(zoom, "El jugador trae ZoomSniper");
            Assert.IsNotNull(mira, "La escena trae la mira del sniper");
            Camera camara = jugador.Camara;
            yield return new WaitForSecondsRealtime(1f);

            cartera.Abonar(Dinero - cartera.Dinero, MotivoDinero.Otro);
            Assert.AreEqual(Dinero, cartera.Dinero);
            Assert.IsFalse(arsenal.Tiene(ArmaAPie.Sniper), "Empieza sin sniper");
            yield return Pulsar(teclado.digit5Key);
            Assert.AreNotEqual(ArmaAPie.Sniper, armas.ArmaActiva, "Sin sniper, 5 no hace nada");

            // a. Se camina hasta El Mercado y E abre la tienda.
            Transform lugar = Buscar("Lugar_MercadoNegro");
            Transform puerta = null;
            foreach (Transform t in lugar.GetComponentsInChildren<Transform>(true))
            {
                puerta = t.name == "Puerta" ? t : puerta;
            }
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                mostrador = m.name.Contains("MercadoNegro") ? m : mostrador;
            }
            Assert.IsNotNull(puerta);
            Assert.IsNotNull(mostrador);
            Vector3 haciaAdentro = lugar.position - puerta.position;
            haciaAdentro.y = 0f;
            haciaAdentro.Normalize();
            Vector3 inicio = puerta.position - haciaAdentro * 5f;
            inicio.y = puerta.position.y + 0.1f;
            jugador.Colocar(Mirando(inicio, puerta.position));
            yield return null;
            yield return null;
            Press(teclado.wKey);
            float fin = Time.realtimeSinceStartup + 25f;
            while ((mostrador.transform.position - jugador.Raiz.position).sqrMagnitude > 2.2f * 2.2f && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return new WaitForSecondsRealtime(0.3f);
            PantallaTienda pantalla = Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => mostrador.PantallaAbierta && pantalla.Visible, "abrir El Mercado con E", 5f);

            // b. La lista: PISTOLA, ESCOPETA, FUSIL y SNIPER con su precio, y nada de "subfusil".
            foreach (string nombre in new[] { "PISTOLA", "ESCOPETA", "FUSIL", "SNIPER" })
            {
                Assert.GreaterOrEqual(FilaBase(pantalla, nombre), 0, "El Mercado lista " + nombre);
            }
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                StringAssert.DoesNotContain("SUBFUSIL", pantalla.TextoFila(i).ToUpperInvariant());
            }
            StringAssert.Contains("700", pantalla.TextoFila(FilaBase(pantalla, "FUSIL")));
            StringAssert.Contains("900", pantalla.TextoFila(FilaBase(pantalla, "SNIPER")));
            yield return new WaitForSecondsRealtime(0.4f);
            Capturar("T-416-2.png");

            // c. Compra FUSIL, SNIPER y SNIPER I.
            yield return Comprar(pantalla, FilaBase(pantalla, "FUSIL"), "FUSIL");
            Assert.IsTrue(arsenal.Tiene(ArmaAPie.Fusil), "Enter compra el fusil");
            Assert.AreEqual(Dinero - 700, cartera.Dinero);
            yield return Comprar(pantalla, FilaBase(pantalla, "SNIPER"), "SNIPER");
            Assert.IsTrue(arsenal.Tiene(ArmaAPie.Sniper), "Enter compra el sniper");
            Assert.AreEqual(0, arsenal.Nivel(ArmaAPie.Sniper));
            yield return Comprar(pantalla, FilaCon(pantalla, "SNIPER I"), "SNIPER I");
            Assert.AreEqual(1, arsenal.Nivel(ArmaAPie.Sniper), "SNIPER I es nivel 1");
            Assert.AreEqual(Dinero - 700 - 900 - 400, cartera.Dinero, "Cobra Ƀ700, Ƀ900 y Ƀ400");
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !mostrador.PantallaAbierta, "cerrar la tienda con Esc", 5f);

            // d. Un plano en el cielo con un blanco a 80 m (un poco a la derecha: la cámara apuntando va sobre el hombro derecho).
            plataforma = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plataforma.transform.position = new Vector3(4000f, 400f, 4000f);
            plataforma.transform.localScale = new Vector3(300f, 1f, 300f);
            Vector3 suelo = plataforma.transform.position + Vector3.up * 0.55f;
            Vector3 puntoTiro = suelo - Vector3.right * 40f;
            Vector3 derecha = Vector3.Cross(Vector3.up, Vector3.right);
            BlancoDeTiro objetivo = PonerBlanco(puntoTiro + Vector3.right * DistanciaBlanco + derecha * LateralApuntando);
            jugador.Colocar(Mirando(puntoTiro, puntoTiro + Vector3.right));
            yield return new WaitForSecondsRealtime(0.5f);

            yield return Pulsar(teclado.digit5Key);
            Assert.AreEqual(ArmaAPie.Sniper, armas.ArmaActiva, "5 saca el sniper");
            Assert.AreEqual(5, armas.TamanoCargador, "SNIPER I: cargador de 5");
            Assert.AreEqual(5, armas.Balas);
            Assert.IsFalse(mira.Visible, "Sin clic derecho no hay mira");
            Assert.AreEqual(60f, camara.fieldOfView, 1f, "FOV normal 60");

            // Sin zoom, a 80 m casi todo falla (dispersión 8°, semilla fija).
            UnityEngine.Random.InitState(1234);
            Time.timeScale = 8f;
            for (int tiro = 0; tiro < 10; tiro++)
            {
                if (armas.Balas <= 0)
                {
                    yield return Pulsar(teclado.rKey);
                    yield return EsperarHasta(() => !armas.Recargando && armas.Balas > 0, "recargar", 20f);
                }
                int antes = armas.Balas;
                yield return Pulsar(raton.leftButton);
                Assert.AreEqual(antes - 1, armas.Balas, "El clic dispara el sniper (tiro " + tiro + ")");
                yield return new WaitForSeconds(1.4f);
            }
            Time.timeScale = 1f;
            Assert.LessOrEqual(objetivo.Impactos, 3, "Sin zoom, de 10 tiros a 80 m pegan 3 o menos (pegaron " + objetivo.Impactos + ")");
            objetivo.Impactos = 0;
            objetivo.Vida = VidaBlanco;
            if (armas.Balas < 1)
            {
                yield return Pulsar(teclado.rKey);
                yield return EsperarHasta(() => !armas.Recargando && armas.Balas > 0, "recargar", 20f);
            }

            // e. Clic derecho: zoom a FOV 15 en menos de 0.3 s, sensibilidad baja y mira prendida.
            Press(raton.rightButton);
            float inicioZoom = Time.realtimeSinceStartup;
            yield return EsperarHasta(() => Mathf.Abs(camara.fieldOfView - 15f) <= 0.5f, "FOV 15", 2f);
            Assert.Less(Time.realtimeSinceStartup - inicioZoom, 0.3f, "El zoom tarda menos de 0.3 s");
            Assert.IsTrue(mira.Visible, "La mira está prendida");
            Assert.AreEqual(0.35f, controlador.FactorSensibilidad, 0.05f, "La sensibilidad baja a x0.35");
            yield return new WaitForSecondsRealtime(0.3f);
            StringAssert.EndsWith(" m", mira.TextoDistancia);
            int metros = int.Parse(mira.TextoDistancia.Replace(" m", string.Empty));
            Assert.That(metros, Is.InRange(75, 90), "La mira marca unos 80 m (marcó " + mira.TextoDistancia + ")");

            int balasAntes = armas.Balas;
            yield return Pulsar(raton.leftButton);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(balasAntes - 1, armas.Balas);
            Assert.AreEqual(1, objetivo.Impactos, "Un tiro con zoom le pega al blanco a 80 m");
            Assert.IsTrue(objetivo.KO, "Un tiro con zoom tumba a un blanco de 60 de vida");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(0f, zoom.Sacudida, 0.01f, "La sacudida vuelve en 0.3 s");

            // Soltar el clic derecho devuelve todo.
            Release(raton.rightButton);
            yield return EsperarHasta(() => Mathf.Abs(camara.fieldOfView - 60f) <= 0.5f, "FOV 60", 2f);
            Assert.IsFalse(mira.Visible, "Al soltar se apaga la mira");
            Assert.AreEqual(1f, controlador.FactorSensibilidad, 0.01f);

            // f. R recarga a 5 balas.
            Time.timeScale = 8f;
            while (armas.Balas > 0)
            {
                yield return Pulsar(raton.leftButton);
                yield return new WaitForSeconds(1.4f);
            }
            Time.timeScale = 1f;
            yield return Pulsar(teclado.rKey);
            Assert.IsTrue(armas.Recargando, "R recarga");
            yield return EsperarHasta(() => !armas.Recargando && armas.Balas == 5, "recargar a 5 balas", 10f);

            // g. 4 saca el fusil; mantener el clic dispara solo, 8 o más balas en 1 s.
            yield return Pulsar(teclado.digit4Key);
            Assert.AreEqual(ArmaAPie.Fusil, armas.ArmaActiva, "4 saca el fusil");
            Assert.IsFalse(zoom.Activo, "Con el fusil no hay zoom del sniper");
            Press(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(48f, camara.fieldOfView, 1f, "Con el fusil, clic derecho sigue apuntando a FOV 48");
            Assert.IsFalse(mira.Visible, "El fusil no usa la mira del sniper");
            Release(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(30, armas.TamanoCargador, "Fusil base: cargador de 30");
            int antesFusil = armas.Balas;
            Press(raton.leftButton);
            yield return new WaitForSecondsRealtime(1f);
            Release(raton.leftButton);
            yield return null;
            int gastadas = antesFusil - armas.Balas;
            Assert.GreaterOrEqual(gastadas, 8, "El fusil dispara solo: 8 o más balas en 1 s (gastó " + gastadas + ")");

            // h. Solo para la captura: de noche, en la calle del Tianguis, la mira del sniper apunta a un pandillero.
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Registro<IRelojMundo>.Actual?.AvanzarHasta(22f);
                Transform arco = Buscar("ArcoTianguis");
                Vector3 desde = arco.position + Vector3.right * 20f;
                desde.y = arco.position.y + 0.1f;
                Vector3 donde = desde + Vector3.right * 32f + derecha * LateralApuntando;
                GameObject pandillero = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab"),
                    donde + Vector3.up * 0.05f, Quaternion.identity);
                UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(pandillero.GetComponent<MechaCyberpunk.IA.APie.PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = UnityEditor.AssetDatabase
                    .LoadAssetAtPath<ScriptableObject>("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                jugador.Colocar(Mirando(desde, donde - derecha * LateralApuntando));
                yield return Pulsar(teclado.digit5Key);
                Press(raton.rightButton);
                yield return new WaitForSecondsRealtime(1f);
                Capturar("T-416-1.png");
                Release(raton.rightButton);
                Object.Destroy(pandillero);
            }
        }
    }
}
