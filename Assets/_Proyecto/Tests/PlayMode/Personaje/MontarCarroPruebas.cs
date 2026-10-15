using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables de subir y bajar del carro de prueba con el jugador real y entrada simulada.</summary>
    public class MontarCarroPruebas : BaseJugadorAPie
    {
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private const string RutaHud = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        // El PuntoSubida del carro queda 1.5 m al frente del jugador, que mira a +Z.
        private static readonly Vector3 PosicionCarro = new Vector3(2f, 0f, 1.1f);
        private const float MetrosManejando = 15f;
        private const float SegundosManejando = 3f;
        private const float DistanciaAsientoMaxima = 0.3f;
        private const float VelocidadParaBajar = 0.5f;
        private const float MaxSegundosFrenar = 5f;
        private const int Ciclos = 10;

        /// <summary>Doble de una zona del mundo: guarda los colliders que entran y si alguno trae un IJugador en sus padres.</summary>
        public class ZonaEspia : MonoBehaviour
        {
            public int Entradas;
            public int ConJugador;

            private void OnTriggerEnter(Collider otro)
            {
                Entradas++;
                if (otro.GetComponentInParent<IJugador>() != null)
                {
                    ConJugador++;
                }
            }
        }

        private GameObject carroObj;
        private CarroDePrueba carro;
        private GameObject hudObj;
        private GameObject zonaObj;
        private int avisosModo;
        private ModoJugador ultimoModo;

        public override void Setup()
        {
            base.Setup();
            carroObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), PosicionCarro, Quaternion.identity);
            carro = carroObj.GetComponent<CarroDePrueba>();
            avisosModo = 0;
        }

        public override void TearDown()
        {
            Object.Destroy(carroObj);
            Object.Destroy(hudObj);
            Object.Destroy(zonaObj);
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            yield return Calentar();
            jugadorAPie.ModoCambio += modo =>
            {
                avisosModo++;
                ultimoModo = modo;
            };
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Subir()
        {
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "La E frente al carro debe subir");
        }

        private static int OyentesPrendidos()
        {
            int cuenta = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private static int CamarasPrendidas()
        {
            int cuenta = 0;
            foreach (Camera camara in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camara.enabled)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Subir_ConEFrenteAlCarro_CambiaAModoCarroYDeCamara()
        {
            yield return Preparar();
            Assert.IsTrue(carro.PuedeSubir);
            Assert.AreEqual(1, OyentesPrendidos(), "A pie solo oye el jugador");

            yield return Subir();

            Assert.IsTrue(carro.Ocupado);
            Assert.AreEqual(1, avisosModo, "ModoCambio avisa una sola vez");
            Assert.AreEqual(ModoJugador.Carro, ultimoModo);
            Assert.AreSame(carro.Camara, jugadorAPie.Camara);
            Assert.IsTrue(carro.Camara.enabled, "La cámara del carro está prendida");
            Assert.IsFalse(jugador.GetComponentInChildren<Camera>().enabled, "La del jugador está apagada");
            Assert.IsFalse(jugador.GetComponent<CharacterController>().enabled, "El CharacterController queda apagado");
            Assert.AreEqual(1, OyentesPrendidos(), "En toda la escena hay un solo AudioListener habilitado");
            Assert.AreEqual(1, CamarasPrendidas(), "Y una sola cámara prendida");
        }

        [UnityTest]
        public IEnumerator Subir_LejosDelCarro_NoSube()
        {
            yield return Preparar();
            jugadorAPie.Colocar(new Pose(new Vector3(-20f, 0.05f, 0f), Quaternion.identity));
            yield return null;

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.IsFalse(carro.Ocupado);
            Assert.AreEqual(0, avisosModo);
        }

        [UnityTest]
        public IEnumerator Manejar_FlechaArriba3Segundos_MueveElCarroYLaPersonaSigueElAsiento()
        {
            yield return Preparar();
            yield return Subir();
            Vector3 inicio = carro.transform.position;

            Press(teclado.upArrowKey);
            yield return Esperar(SegundosManejando);
            Release(teclado.upArrowKey);

            Assert.Greater(Vector3.Distance(inicio, carro.transform.position), MetrosManejando, "En 3 s avanza más de 15 m");
            yield return null;
            Assert.Less(Vector3.Distance(jugador.transform.position, carro.Asiento.position), DistanciaAsientoMaxima,
                "La raíz del jugador va pegada al asiento");
        }

        [UnityTest]
        public IEnumerator Manejar_WYClicIzquierdo_NoMuevenNiAtacanALaPersona()
        {
            yield return Preparar();
            yield return Subir();
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            Vector3 inicio = jugador.transform.position;

            Press(teclado.wKey);
            Press(raton.leftButton);
            yield return Esperar(1f);
            Release(raton.leftButton);
            Release(teclado.wKey);

            Assert.Less(Vector3.Distance(inicio, jugador.transform.position), 0.01f, "W no mueve a la persona en el carro");
            Assert.Less(Vector3.Distance(PosicionCarro, carro.transform.position), 0.01f, "Y el carro no se movió");
            Assert.AreEqual(FaseGolpe.Listo, golpe.Fase, "El clic no ataca");
        }

        [UnityTest]
        public IEnumerator Manejar_PasandoPorUnTrigger_ElTriggerNoVeAlJugador()
        {
            yield return Preparar();
            zonaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zonaObj.transform.position = new Vector3(PosicionCarro.x - 0.5f, 1f, 12f);
            zonaObj.transform.localScale = new Vector3(8f, 3f, 2f);
            zonaObj.GetComponent<Collider>().isTrigger = true;
            ZonaEspia zona = zonaObj.AddComponent<ZonaEspia>();
            yield return Subir();

            Press(teclado.upArrowKey);
            yield return Esperar(SegundosManejando);
            Release(teclado.upArrowKey);

            Assert.Greater(zona.Entradas, 0, "El carro sí cruzó el trigger");
            Assert.AreEqual(0, zona.ConJugador, "Ningún collider que entró tiene IJugador en sus padres");
        }

        [UnityTest]
        public IEnumerator Bajar_ConElCarroRapido_NoBajaYElHudDiceFrenaParaBajar()
        {
            hudObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaHud));
            HudAPie hud = hudObj.GetComponent<HudAPie>();
            yield return Preparar();
            yield return Subir();

            Press(teclado.upArrowKey);
            yield return Esperar(1.5f);
            Assert.Greater(carro.Velocidad, VelocidadParaBajar);
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "A más de 0.5 m/s no baja");
            Assert.IsTrue(carro.Ocupado);
            Assert.AreEqual(1, avisosModo, "No hubo otro aviso de modo");
            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual("Frena para bajar", hud.TextoAvisoInteraccion.text);
            Release(teclado.upArrowKey);
        }

        [UnityTest]
        public IEnumerator Bajar_YaDetenido_BajaJuntoALaPuertaYCaminaOtraVez()
        {
            yield return Preparar();
            yield return Subir();
            Press(teclado.upArrowKey);
            yield return Esperar(1.5f);
            Release(teclado.upArrowKey);
            float limite = Time.time + MaxSegundosFrenar;
            while (!carro.PuedeBajar && Time.time < limite)
            {
                yield return null;
            }
            Assert.IsTrue(carro.PuedeBajar, "El carro frena hasta poder bajar");
            Quaternion giroCarro = carro.transform.rotation;

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.AreEqual(2, avisosModo, "Subir y bajar: dos avisos");
            Assert.AreEqual(ModoJugador.APie, ultimoModo);
            Assert.IsFalse(carro.Ocupado);
            Assert.IsTrue(jugador.GetComponent<CharacterController>().enabled);
            Assert.IsTrue(jugador.GetComponentInChildren<Camera>().enabled, "Su cámara vuelve");
            Assert.AreSame(jugador.GetComponentInChildren<Camera>(), jugadorAPie.Camara);
            Assert.IsFalse(carro.Camara.enabled);
            Assert.AreEqual(1, OyentesPrendidos());
            Assert.AreEqual(1, CamarasPrendidas());
            Assert.Less(Vector3.Distance(jugador.transform.position, carro.PuntoSubida), 0.3f, "Queda junto a la puerta");
            Assert.IsFalse(carroObj.GetComponent<Collider>().bounds.Contains(jugador.transform.position), "Fuera del collider del carro");
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(0f, giroCarro.eulerAngles.y, 0f), Quaternion.Euler(0f, jugador.transform.eulerAngles.y, 0f)), 1f,
                "Mira hacia donde iba el carro");

            yield return Esperar(0.2f);
            Vector3 antes = jugador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(Vector3.Distance(antes, jugador.transform.position), 1f, "Con W camina otra vez");
        }

        [UnityTest]
        public IEnumerator SubirYBajar_DiezVeces_NoDejaCamarasNiOyentesDeMas()
        {
            yield return Preparar();
            for (int i = 0; i < Ciclos; i++)
            {
                yield return Tecla(teclado.eKey);
                Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Sube en el ciclo " + i);
                Assert.AreEqual(1, OyentesPrendidos(), "Un oyente dentro (ciclo " + i + ")");
                Assert.AreEqual(1, CamarasPrendidas(), "Una cámara dentro (ciclo " + i + ")");

                yield return Tecla(teclado.eKey);
                Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "Baja en el ciclo " + i);
                Assert.AreEqual(1, OyentesPrendidos(), "Un oyente afuera (ciclo " + i + ")");
                Assert.AreEqual(1, CamarasPrendidas(), "Una cámara afuera (ciclo " + i + ")");
            }
            Assert.AreEqual(2 * Ciclos, avisosModo);
            Assert.AreEqual(1, Registro<IMontable>.Cantidad, "El carro sigue siendo el único montable");
        }
    }
}
