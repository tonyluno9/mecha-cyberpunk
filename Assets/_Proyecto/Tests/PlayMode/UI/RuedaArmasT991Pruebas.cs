using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-991: la rueda de armas y objetos con entrada simulada: mantener Tab abre (tiempo a 0.25), el ratón mueve el cursor, soltar equipa el arma o usa el objeto
    /// y devuelve el tiempo; lo que no se tiene no se elige; las teclas 1 y 6 ya no hacen nada; la pausa no pierde su escala.
    /// Capturas en docs/capturas/t991/ con CAPTURAR_T991=1 (en Ciudad_Prologo de día).
    /// </summary>
    public class RuedaArmasT991Pruebas : BaseHudObjetivo
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private RuedaArmasObjetos rueda;
        private ArmasAPie armas;
        private ConsumiblesJugador consumibles;
        private VidaAPie vida;
        private ServicioInventario inventario;
        private MechaCyberpunk.Personaje.Movimiento.ControladorAPie controlador;

        protected override bool QuitarEscudo => true;

        public override void TearDown()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Pausa.Fijar(false);
            base.TearDown();
        }

        private static InfoDanio Bala(float cantidad) => new InfoDanio(cantidad, TipoDanio.Disparo, Vector3.zero, Vector3.forward, null);

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.3f);
            rueda = hud.GetComponent<RuedaArmasObjetos>();
            armas = jugador.GetComponent<ArmasAPie>();
            consumibles = jugador.GetComponent<ConsumiblesJugador>();
            vida = jugador.GetComponent<VidaAPie>();
            controlador = jugador.GetComponent<MechaCyberpunk.Personaje.Movimiento.ControladorAPie>();
            inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(rueda, "El HUD a pie trae la rueda");
            Assert.IsTrue(rueda.Armada);
            Assert.IsNotNull(inventario);
            yield return null;
        }

        private DatosConsumible Dato(string id) => (DatosConsumible)inventario.Catalogo.Buscar(id);

        private IEnumerator AbrirRueda()
        {
            Press(teclado.tabKey);
            yield return null;
            yield return null;
        }

        private IEnumerator ApuntarA(int casilla)
        {
            float a = ReglaRuedaArmas.AnguloDeCasilla(casilla) * Mathf.Deg2Rad;
            Vector2 meta = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 120f;
            Set(raton.delta, meta - rueda.Cursor);
            yield return null;
            yield return null;
        }

        private IEnumerator Soltar()
        {
            Release(teclado.tabKey);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tab_AbreLaRuedaConElTiempoA025_YSoltarSinElegirDevuelveElTiempo()
        {
            yield return Preparar();
            Assert.IsFalse(RuedaArmasObjetos.Abierta);
            yield return AbrirRueda();
            Assert.IsTrue(RuedaArmasObjetos.Abierta, "Mantener Tab abre la rueda");
            Assert.AreEqual(0.25f, Time.timeScale, 0.001f);
            Assert.AreEqual(0.02f * 0.25f, Time.fixedDeltaTime, 0.0005f, "El paso de la física se escala");
            Assert.AreEqual(-1, rueda.Resaltada, "Sin mover el ratón no hay nada resaltado");
            Assert.IsTrue(controlador.EntradaBloqueada);
            yield return Soltar();
            Assert.IsFalse(RuedaArmasObjetos.Abierta);
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "Al cerrar se devuelve la escala");
            Assert.AreEqual(0.02f, Time.fixedDeltaTime, 0.0005f);
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "Sin nada resaltado solo cierra");
            Assert.IsFalse(controlador.EntradaBloqueada);
        }

        [UnityTest]
        public IEnumerator MoverElRatonHaciaLaPistola_YSoltar_LaEquipa()
        {
            yield return Preparar();
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva);
            yield return AbrirRueda();
            yield return ApuntarA(1);
            Assert.AreEqual(1, rueda.Resaltada, "La pistola queda resaltada");
            Assert.AreEqual("PISTOLA", rueda.TextoNombre.text);
            yield return Soltar();
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "Soltar Tab equipa la pistola");
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator MoverElRatonHaciaLaVenda_YSoltar_LaUsa()
        {
            yield return Preparar();
            vida.RecibirDanio(Bala(60f));
            inventario.Agregar(Dato("venda"), 1);
            yield return null;
            yield return AbrirRueda();
            Assert.AreEqual("x1", rueda.TextoCantidad(7), "La venda muestra cuántas lleva");
            yield return ApuntarA(7);
            Assert.AreEqual(7, rueda.Resaltada);
            yield return Soltar();
            Assert.AreEqual(0, inventario.Cantidad(Dato("venda")), "Soltar Tab usa la venda");
            Assert.IsTrue(consumibles.Curando);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator UnArmaQueNoTienes_YUnObjetoSinPiezas_NoSeEligen()
        {
            yield return Preparar();
            Assert.IsFalse(armas.TieneArma(ArmaAPie.Escopeta));
            yield return AbrirRueda();
            Assert.IsTrue(rueda.CasillaApagada(2), "Escopeta apagada");
            Assert.IsTrue(rueda.CasillaApagada(5), "Bebida sin piezas apagada");
            Assert.IsFalse(rueda.CasillaApagada(0), "Puños siempre está");
            yield return ApuntarA(2);
            Assert.AreEqual(-1, rueda.Resaltada, "La escopeta que no tienes no se resalta");
            yield return Soltar();
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "No se equipó nada");
            vida.RecibirDanio(Bala(60f));
            yield return AbrirRueda();
            yield return ApuntarA(5);
            Assert.AreEqual(-1, rueda.Resaltada, "La bebida sin piezas no se resalta");
            yield return Soltar();
        }

        [UnityTest]
        public IEnumerator LasTeclas1Y6YF_YaNoCambianArmaNiUsanObjeto()
        {
            yield return Preparar();
            armas.Equipar(ArmaAPie.Pistola);
            vida.RecibirDanio(Bala(60f));
            inventario.Agregar(Dato("bebida_energetica"), 1);
            yield return null;
            Press(teclado.digit1Key);
            yield return null;
            Release(teclado.digit1Key);
            Press(teclado.digit6Key);
            yield return null;
            Release(teclado.digit6Key);
            Press(teclado.fKey);
            yield return null;
            Release(teclado.fKey);
            yield return null;
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "1 ya no pone los puños");
            Assert.AreEqual(1, inventario.Cantidad(Dato("bebida_energetica")), "6 y F ya no usan la bebida");
            Assert.IsFalse(RuedaArmasObjetos.Abierta);
        }

        [UnityTest]
        public IEnumerator ConLaRuedaAbierta_ElRatonNoGiraLaCamara()
        {
            yield return Preparar();
            Quaternion antes = jugadorAPieCamara().rotation;
            yield return AbrirRueda();
            Set(raton.delta, new Vector2(400f, 200f));
            yield return null;
            yield return null;
            Assert.Less(Quaternion.Angle(antes, jugadorAPieCamara().rotation), 0.5f, "La cámara no se movió con la rueda abierta");
            yield return Soltar();
            Set(raton.delta, new Vector2(400f, 0f));
            yield return null;
            yield return null;
            Assert.Greater(Quaternion.Angle(antes, jugadorAPieCamara().rotation), 1f, "Cerrada la rueda, el ratón vuelve a girar la cámara");
        }

        private Transform jugadorAPieCamara() => Registro<IJugador>.Actual.Camara.transform;

        [UnityTest]
        public IEnumerator LaPausaTomaElControl_LaRuedaSeCierraSinPisarLaEscala()
        {
            yield return Preparar();
            yield return AbrirRueda();
            Assert.IsTrue(RuedaArmasObjetos.Abierta);
            Assert.AreEqual(1f, RuedaArmasObjetos.EscalaAnterior, 0.001f);
            Time.timeScale = 0f; // lo que hace la pantalla de pausa
            Pausa.Fijar(true);
            yield return null;
            Assert.IsFalse(RuedaArmasObjetos.Abierta, "La rueda se cierra sin elegir");
            Assert.AreEqual(0f, Time.timeScale, "No pisa la escala de la pausa");
            Pausa.Fijar(false);
            Time.timeScale = 1f;
            yield return Soltar();
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva);
        }

        [UnityTest]
        public IEnumerator SiMueres_ConLaRuedaAbierta_SeCierraYDevuelveElTiempo()
        {
            yield return Preparar();
            yield return AbrirRueda();
            vida.RecibirDanio(Bala(9999f));
            yield return null;
            yield return null;
            Assert.IsTrue(vida.EstaKO);
            Assert.IsFalse(RuedaArmasObjetos.Abierta);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        // ---- Capturas ----

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            if (SceneManager.GetActiveScene().path == RutaEscena)
            {
                foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    Object.Destroy(raiz);
                }
                Registros.LimpiarTodos();
            }
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

        private static void Capturar(string nombre)
        {
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t991"));
            CapturaUI.Capturar(Path.Combine("docs", "capturas", "t991", nombre), Camera.main);
        }

        [UnityTest]
        public IEnumerator Capturas_RuedaConLaPistolaYConUnObjetoResaltado()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") != "1")
            {
                Assert.Ignore("Sin CAPTURAR_T991=1 no se sacan capturas.");
            }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<IRelojMundo>.Actual != null && Registro<ServicioInventario>.Actual != null;
            }, "el director arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                Press(teclado.f6Key);
                yield return null;
                Release(teclado.f6Key);
                yield return null;
            }
            Registro<IRelojMundo>.Actual.AvanzarHasta(12f);
            inventario = Registro<ServicioInventario>.Actual;
            IJugador j = Registro<IJugador>.Actual;
            rueda = Object.FindFirstObjectByType<RuedaArmasObjetos>();
            Assert.IsNotNull(rueda);
            vida = j.Raiz.GetComponentInChildren<VidaAPie>();
            j.Raiz.GetComponentInChildren<ArsenalJugador>().DarMejora(ArmaAPie.Pistola); // en la historia la pistola llega después: aquí se regala para ver la casilla encendida
            vida.RecibirDanio(Bala(50f));
            inventario.Agregar(Dato("venda"), 2);
            inventario.Agregar(Dato("bebida_energetica"), 1);
            inventario.Agregar(Dato("papitas"), 3);
            // Se saca al jugador de su celda: en la calle, frente a EL DOC (así la foto se ve en la ciudad).
            foreach (MonoBehaviour p in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (p.GetType().Name == "PersonajeApoyo" && p.gameObject.name.ToLowerInvariant().Contains("doc"))
                {
                    Vector3 frente = p.transform.position + p.transform.forward * 3f;
                    Vector3 hacia = p.transform.position - frente;
                    hacia.y = 0f;
                    j.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
                }
            }
            yield return new WaitForSecondsRealtime(0.8f);

            Press(teclado.tabKey);
            yield return null;
            yield return null;
            Assert.IsTrue(RuedaArmasObjetos.Abierta, "La rueda abrió en la ciudad");
            yield return ApuntarA(1);
            Assert.AreEqual(1, rueda.Resaltada);
            yield return new WaitForSecondsRealtime(0.3f);
            Capturar("rueda-armas.png");
            yield return ApuntarA(7);
            Assert.AreEqual(7, rueda.Resaltada);
            yield return new WaitForSecondsRealtime(0.3f);
            Capturar("rueda-objetos.png");
            Release(teclado.tabKey);
            yield return null;
            yield return null;
        }
    }
}
