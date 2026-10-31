using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion.Sonido;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-407: el recorrido de un jugador nuevo en Ciudad_Prologo: llega al mercado negro, ve el mostrador y el letrero, compra la escopeta con E, ↓ y Enter,
    /// sale al Tianguis (zona marcada, 8 o más puestos y arco) y dispara a un muñeco a 8 m. Entrada simulada; al jugador se le coloca al inicio de cada tramo.
    /// Con CAPTURAR_T407=1 deja docs/capturas/T-407-1.png (dentro del mercado) y T-407-2.png (el Tianguis).
    /// </summary>
    public class RecorridoRonda2Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T407";
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const int PuestosMinimos = 8;
        private const float DistanciaMuneco = 8f;
        private const int DineroInicial = 400;
        private const float AlcanceLuces = 1f;
        private const float VistaAtrasDelArco = 9f;
        private const float VistaDentroDelTianguis = 8f;
        private const string RutaMuneco = "Assets/_Proyecto/Personaje/Pruebas/MunecoAPie.prefab";

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

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
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

        private void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = Registro<IJugador>.Actual.Camara;
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay)
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
            string ruta = Path.Combine("docs", "capturas", nombre);
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(imagen);
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

        [UnityTest]
        public IEnumerator MercadoTianguisEscopeta()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null
                && Registro<ISonidos>.Actual != null && Registro<ArsenalJugador>.Actual != null, "jugador, territorio, sonidos y arsenal", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            GestorSonidos gestor = (GestorSonidos)Registro<ISonidos>.Actual;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ArmasAPie armas = UnityEngine.Object.FindFirstObjectByType<ArmasAPie>();
            Assert.IsNotNull(armas);
            yield return new WaitForSecondsRealtime(1f);

            // Una sola escucha de audio prendida; la música queda apagada (volumen 0) y el ambiente al 80 % y no hay lluvia sonora.
            int escuchas = 0;
            foreach (AudioListener a in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                escuchas += a.isActiveAndEnabled ? 1 : 0;
            }
            Assert.AreEqual(1, escuchas, "Un solo AudioListener prendido.");
            Assert.AreEqual(0.8f, gestor.Volumen(SeccionSonido.Ambiente), 0.001f, "El ambiente arranca al 80 % (T-442).");
            Assert.AreEqual(0f, gestor.Volumen(SeccionSonido.Musica), "La música arranca apagada.");
            foreach (AudioSource fuente in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                Assert.IsFalse(fuente.isPlaying && fuente.loop && fuente.volume > 0f && fuente.transform.parent != gestor.transform,
                    "Suena un bucle que no es del gestor: " + fuente.name);
            }

            // Dinero: Ƀ400 en la cartera.
            Registro<ICartera>.Actual.Abonar(DineroInicial, MotivoDinero.Otro);

            // a. El mercado negro: el jugador aparece en la calle frente a la puerta y camina hacia adentro con W.
            Transform lugar = Buscar("Lugar_MercadoNegro");
            Assert.IsNotNull(lugar);
            Transform puerta = null;
            foreach (Transform t in lugar.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Puerta")
                {
                    puerta = t;
                }
            }
            Assert.IsNotNull(puerta, "El mercado negro tiene puerta.");
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in UnityEngine.Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                if (m.name.Contains("MercadoNegro"))
                {
                    mostrador = m;
                }
            }
            Assert.IsNotNull(mostrador, "Hay un mostrador del mercado negro.");
            Vector3 haciaAdentro = (lugar.position - puerta.position);
            haciaAdentro.y = 0f;
            haciaAdentro.Normalize();
            Vector3 inicio = puerta.position - haciaAdentro * 5f;
            inicio.y = puerta.position.y + 0.1f;
            jugador.Colocar(Mirando(inicio, puerta.position));
            yield return null;
            yield return null;
            IdLugar adentro = IdLugar.Ninguno;
            Action<IdLugar> alEntrar = l => adentro = l;
            EventosLugares.JugadorEntro += alEntrar;
            Press(teclado.wKey);
            float fin = Time.realtimeSinceStartup + 25f;
            while (adentro != IdLugar.MercadoNegro && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.AreEqual(IdLugar.MercadoNegro, adentro, "Caminando con W desde la calle se entra al mercado negro (jugador en " + jugador.Raiz.position + ").");
            float fin2 = Time.realtimeSinceStartup + 15f;
            while ((mostrador.transform.position - jugador.Raiz.position).sqrMagnitude > 2.2f * 2.2f && Time.realtimeSinceStartup < fin2)
            {
                yield return null;
            }
            Release(teclado.wKey);
            EventosLugares.JugadorEntro -= alEntrar;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Less((mostrador.transform.position - jugador.Raiz.position).magnitude, 2.5f, "Se llega al mostrador caminando (jugador en " + jugador.Raiz.position + ").");

            // El cuarto: mostrador, estantes, luz y un letrero con texto, todo dentro de la zona.
            Bounds cuarto = default;
            bool hayCuarto = false;
            foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == "ZonaInterior" && (mb.transform.position - lugar.position).sqrMagnitude < 25f)
                {
                    Collider c = mb.GetComponent<Collider>();
                    cuarto = c.bounds;
                    hayCuarto = true;
                }
            }
            Assert.IsTrue(hayCuarto, "El mercado negro tiene su zona interior.");
            cuarto.Expand(AlcanceLuces);
            int renderers = 0;
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                renderers += r.enabled && cuarto.Contains(r.bounds.center) ? 1 : 0;
            }
            int luces = 0;
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                luces += l.enabled && l.type != LightType.Directional && cuarto.Contains(l.transform.position) ? 1 : 0;
            }
            int textos = 0;
            foreach (Text t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                textos += t.canvas != null && t.canvas.renderMode == RenderMode.WorldSpace && cuarto.Contains(t.transform.position) && !string.IsNullOrEmpty(t.text) ? 1 : 0;
            }
            Debug.Log("[Prueba] cuarto " + cuarto + " renderers " + renderers + " luces " + luces + " letreros " + textos);
            Assert.GreaterOrEqual(renderers, 20, "El cuarto del mercado negro tiene mostrador, estantes y cajas.");
            Assert.GreaterOrEqual(luces, 1, "El cuarto tiene luz.");
            Assert.GreaterOrEqual(textos, 1, "El cuarto tiene un letrero con texto.");
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador se ofrece (aviso: " + mostrador.Texto + ").");
            Assert.AreEqual(IdLugar.MercadoNegro, adentro);
            Vector3 lateral = Vector3.Cross(Vector3.up, haciaAdentro).normalized * 2.4f;
            Vector3 aLaVista = jugador.Raiz.position + lateral;
            jugador.Colocar(Mirando(aLaVista, mostrador.transform.position));
            yield return new WaitForSecondsRealtime(0.5f);
            Capturar("T-407-1.png");
            jugador.Colocar(Mirando(jugador.Raiz.position - lateral, mostrador.transform.position));
            yield return new WaitForSecondsRealtime(0.5f);

            // b. E abre la tienda; ↓ hasta la ESCOPETA y Enter compra; Esc cierra.
            PantallaTienda pantalla = UnityEngine.Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => mostrador.PantallaAbierta && pantalla.Visible, "abrir la tienda con E", 5f);
            int filaEscopeta = -1;
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoFila(i).ToUpperInvariant().Contains("ESCOPETA"))
                {
                    filaEscopeta = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(filaEscopeta, 0, "La tienda ofrece la escopeta.");
            StringAssert.Contains("350", pantalla.TextoFila(filaEscopeta), "El precio de la escopeta se ve en la fila.");
            int bajadas = 0;
            while (pantalla.IndiceElegido != filaEscopeta && bajadas++ < 30)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            Assert.AreEqual(filaEscopeta, pantalla.IndiceElegido);
            int antesComprar = gestor.Pedidos("ECO_COMPRA");
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(arsenal.Tiene(ArmaAPie.Escopeta), "Enter compra la escopeta.");
            Assert.AreEqual(antesComprar + 1, gestor.Pedidos("ECO_COMPRA"), "La compra suena.");
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !mostrador.PantallaAbierta, "cerrar la tienda con Esc", 5f);
            yield return new WaitForSecondsRealtime(0.2f);

            // c. El Tianguis: la zona está marcada, tiene 8 o más puestos y su arco.
            Transform arco = Buscar("ArcoTianguis");
            Assert.IsNotNull(arco, "Hay un arco EL TIANGUIS.");
            int puestos = 0;
            foreach (Transform t in Buscar("GTA_T407").GetComponentsInChildren<Transform>(true))
            {
                puestos += t.name.StartsWith("Puesto_") && t.parent != null && t.parent.name == "Puestos" ? 1 : 0;
            }
            Assert.GreaterOrEqual(puestos, PuestosMinimos, "El Tianguis tiene 8 o más puestos.");
            Vector3 vista = arco.position + new Vector3(-VistaAtrasDelArco, 0f, 0f);
            vista.y = arco.position.y + 0.1f;
            jugador.Colocar(Mirando(vista, vista + Vector3.right));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("tianguis", territorio.ZonaEn(arco.position + Vector3.right * 3f), "El centro del arco es zona del Tianguis.");
            Assert.AreEqual("EL TIANGUIS", territorio.Nombre("tianguis"));
            Capturar("T-407-3.png");
            Vector3 enCalle = arco.position + new Vector3(VistaDentroDelTianguis, 0f, 0f);
            enCalle.y = arco.position.y + 0.1f;
            jugador.Colocar(Mirando(enCalle, enCalle + Vector3.right));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("tianguis", territorio.ZonaEn(enCalle), "En la calle del Tianguis la zona es la del Tianguis.");
            Capturar("T-407-2.png");

            // d. La escopeta: 3 la saca, sale con el cargador lleno, clic derecho apunta, clic izquierdo dispara a un muñeco a 8 m, R recarga.
            Vector3 puntoTiro = arco.position + Vector3.right * 20f;
            puntoTiro.y = arco.position.y + 0.1f;
            Vector3 posMuneco = puntoTiro + Vector3.right * DistanciaMuneco;
            GameObject prefabMuneco = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RutaMuneco);
            Assert.IsNotNull(prefabMuneco);
            MunecoAPie muneco = UnityEngine.Object.Instantiate(prefabMuneco, posMuneco, Quaternion.identity).GetComponent<MunecoAPie>();
            Assert.IsNotNull(muneco);
            float vidaAntes = muneco.Actual;
            jugador.Colocar(Mirando(puntoTiro, posMuneco));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Pulsar(teclado.digit3Key);
            Assert.AreEqual(ArmaAPie.Escopeta, armas.ArmaActiva, "3 saca la escopeta.");
            Assert.AreEqual(armas.TamanoCargador, armas.Balas, "Sale con el cargador lleno.");
            int balas = armas.Balas;
            int disparosAntes = gestor.Pedidos("ARM_ESC_DISPARO");
            Press(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Pulsar(raton.leftButton);
            Release(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(balas - 1, armas.Balas, "Disparar baja el cargador.");
            Assert.Less(muneco.Actual, vidaAntes, "Los perdigones dañan al muñeco a 8 m.");
            Assert.AreEqual(disparosAntes + 1, gestor.Pedidos("ARM_ESC_DISPARO"), "El disparo suena.");
            Assert.AreEqual(8, armas.GetComponent<PistolaAPie>().DatosActuales.Perdigones, "La escopeta lanza 8 perdigones.");
            yield return Pulsar(teclado.rKey);
            Assert.IsTrue(armas.Recargando, "R recarga.");
            yield return EsperarHasta(() => !armas.Recargando && armas.Balas == armas.TamanoCargador, "terminar la recarga", 10f);
        }
    }
}
