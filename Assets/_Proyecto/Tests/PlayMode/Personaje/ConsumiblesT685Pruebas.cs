using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-685: usar bebida, papitas, venda, café y batería con teclas (Tab elige, F usa, 6 a 0 directo), se guardan en el inventario,
    /// y comprarlos en la expendedora con E. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-685-*.png.
    /// </summary>
    public class ConsumiblesT685Pruebas : BaseJugadorAPie
    {
        private VidaAPie vida;
        private ConsumiblesJugador consumibles;
        private ServicioInventario inventario;
        private CatalogoObjetos catalogo;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            yield return Esperar(0.2f);
            vida = jugador.GetComponent<VidaAPie>();
            consumibles = jugador.GetComponent<ConsumiblesJugador>();
            inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(consumibles, "El jugador trae ConsumiblesJugador");
            Assert.IsNotNull(inventario, "Hay inventario");
            catalogo = inventario.Catalogo;
            yield return null;
        }

        private DatosConsumible Dato(string id) => (DatosConsumible)catalogo.Buscar(id);

        private static InfoDanio Bala(float cantidad) => new InfoDanio(cantidad, TipoDanio.Disparo, Vector3.zero, Vector3.forward, null);

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bebida_Tecla6_CuraYDa5SegundosSinDanio()
        {
            yield return Preparar();
            vida.RecibirDanio(Bala(60f));
            float vidaBaja = vida.Actual;
            inventario.Agregar(Dato("bebida_energetica"), 2);
            yield return null;
            yield return Pulsar(teclado.digit6Key);
            Assert.AreEqual(1, inventario.Cantidad(Dato("bebida_energetica")), "Gasta una");
            Assert.AreEqual(vidaBaja + 25f, vida.Actual, 0.01f, "Cura");
            Assert.AreEqual(5f, vida.InvulnerableRestante, 0.3f, "5 s sin recibir daño");
            float antes = vida.Actual;
            vida.RecibirDanio(Bala(50f));
            Assert.AreEqual(antes, vida.Actual, 0.001f, "El daño se ignora");
            yield return Esperar(5.3f);
            float antesDelGolpe = vida.Actual;
            vida.RecibirDanio(Bala(10f));
            Assert.Less(vida.Actual, antesDelGolpe, "Pasados los 5 s vuelve a recibir daño");
        }

        [UnityTest]
        public IEnumerator Papitas_TabYF_CuranPoco_YNoSeGastanConLaVidaLlena()
        {
            yield return Preparar();
            inventario.Agregar(Dato("papitas"), 2);
            yield return null;
            Assert.AreEqual(Dato("papitas"), consumibles.Seleccionado);
            yield return Pulsar(teclado.fKey);
            Assert.AreEqual(2, inventario.Cantidad(Dato("papitas")), "Con la vida llena no se gastan");
            vida.RecibirDanio(Bala(50f));
            float baja = vida.Actual;
            yield return Pulsar(teclado.fKey);
            Assert.AreEqual(1, inventario.Cantidad(Dato("papitas")));
            Assert.AreEqual(baja + 15f, vida.Actual, 0.01f, "Curan poco");
        }

        [UnityTest]
        public IEnumerator Tab_RecorreLosQueLleva()
        {
            yield return Preparar();
            inventario.Agregar(Dato("papitas"), 1);
            inventario.Agregar(Dato("cafe"), 1);
            yield return null;
            DatosConsumible primero = consumibles.Seleccionado;
            yield return Pulsar(teclado.tabKey);
            Assert.AreNotEqual(primero, consumibles.Seleccionado);
            yield return Pulsar(teclado.tabKey);
            Assert.AreEqual(primero, consumibles.Seleccionado, "Da la vuelta");
        }

        [UnityTest]
        public IEnumerator Venda_CuraMasPeroDespacio()
        {
            yield return Preparar();
            vida.RecibirDanio(Bala(100f));
            float baja = vida.Actual;
            inventario.Agregar(Dato("venda"), 1);
            yield return null;
            yield return Pulsar(teclado.digit8Key);
            Assert.IsTrue(consumibles.Curando);
            Assert.Less(vida.Actual - baja, 30f, "No cura todo de golpe");
            yield return Esperar(4.5f);
            Assert.That(vida.Actual - baja, Is.InRange(68f, 80f), "Termina con 70 de vida (más la regeneración natural)");
            Assert.IsFalse(consumibles.Curando);
        }

        [UnityTest]
        public IEnumerator Cafe_AceleraYSePasa()
        {
            yield return Preparar();
            float z0 = jugador.transform.position.z;
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            Release(teclado.wKey);
            float normal = jugador.transform.position.z - z0;
            yield return Esperar(0.4f);
            jugador.transform.position = new Vector3(0f, 0.05f, 0f);
            Physics.SyncTransforms();
            inventario.Agregar(Dato("cafe"), 1);
            yield return null;
            yield return Pulsar(teclado.digit9Key);
            Assert.AreEqual(1.35f, controlador.FactorExtraVelocidad, 0.001f);
            Assert.Greater(consumibles.VelocidadRestante, 15f);
            z0 = jugador.transform.position.z;
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            Release(teclado.wKey);
            float rapido = jugador.transform.position.z - z0;
            Assert.Greater(rapido, normal * 1.15f, "Con café avanza más rápido: " + rapido + " contra " + normal);
        }

        [UnityTest]
        public IEnumerator Bateria_RecuperaElEscudo()
        {
            yield return Preparar();
            EscudoJugador escudo = jugador.GetComponent<EscudoJugador>();
            escudo.Iniciar(30f, 0f, 99f);
            vida.RecibirDanio(Bala(30f));
            Assert.AreEqual(0f, escudo.Actual, 0.001f);
            inventario.Agregar(Dato("bateria_escudo"), 1);
            yield return null;
            yield return Pulsar(teclado.digit0Key);
            Assert.AreEqual(30f, escudo.Actual, 0.01f);
            Assert.AreEqual(0, inventario.Cantidad(Dato("bateria_escudo")));
        }

        [UnityTest]
        public IEnumerator Inventario_GuardaYCargaLosConsumibles()
        {
            yield return Preparar();
            inventario.Agregar(Dato("papitas"), 3);
            inventario.Agregar(Dato("bebida_energetica"), 2);
            string texto = inventario.Guardar();
            inventario.Quitar(Dato("papitas"), 3);
            inventario.Quitar(Dato("bebida_energetica"), 2);
            Assert.AreEqual(0, inventario.Cantidad(Dato("papitas")));
            inventario.Cargar(texto);
            Assert.AreEqual(3, inventario.Cantidad(Dato("papitas")));
            Assert.AreEqual(2, inventario.Cantidad(Dato("bebida_energetica")));
        }
    }
}
