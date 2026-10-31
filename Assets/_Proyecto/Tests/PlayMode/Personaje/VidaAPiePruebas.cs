using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables de la vida y el KO del jugador a pie.</summary>
    public class VidaAPiePruebas : BaseJugadorAPie
    {
        private VidaAPie vida;
        private InfoDanio ejemplo;
        private int impactos;
        private IPeleador ultimoObjetivo;
        private bool objetivoRecibido;
        private ResultadoDanio ultimoResultado;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            vida = jugador.GetComponent<VidaAPie>();
            ejemplo = new InfoDanio(30f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null);
            impactos = 0;
            objetivoRecibido = false;
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) =>
            {
                impactos++;
                ultimoObjetivo = objetivo;
                objetivoRecibido = true;
                ultimoResultado = resultado;
            };
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Vida_RecibirDanio_BajaAvisaYPublicaImpactoConObjetivoNull()
        {
            yield return Calentar();
            int cambios = 0;
            vida.Cambio += () => cambios++;

            ResultadoDanio resultado = vida.RecibirDanio(ejemplo);

            Assert.AreEqual(vida.Maxima - 30f, vida.Actual, 0.0001f);
            Assert.AreEqual(30f, resultado.DanioAplicado, 0.0001f);
            Assert.AreEqual(1, cambios);
            Assert.AreEqual(1, impactos);
            Assert.IsTrue(objetivoRecibido);
            Assert.IsNull(ultimoObjetivo);
            Assert.AreEqual(30f, ultimoResultado.DanioAplicado, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Vida_CantidadCeroONegativa_SeIgnora()
        {
            yield return Calentar();
            ResultadoDanio cero = vida.RecibirDanio(new InfoDanio(0f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            ResultadoDanio negativa = vida.RecibirDanio(new InfoDanio(-5f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));

            Assert.AreEqual(0f, cero.DanioAplicado);
            Assert.AreEqual(0f, negativa.DanioAplicado);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.0001f);
            Assert.AreEqual(0, impactos);
        }

        [UnityTest]
        public IEnumerator Vida_EnKO_IgnoraYNoPublicaNada()
        {
            yield return Calentar();
            vida.RecibirDanio(new InfoDanio(vida.Maxima, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO);
            Assert.AreEqual(1, impactos, "El golpe que causa el KO sí se publica");

            ResultadoDanio resultado = vida.RecibirDanio(ejemplo);

            Assert.AreEqual(0f, resultado.DanioAplicado);
            Assert.AreEqual(1, impactos);
            Assert.AreEqual(0f, vida.Actual);
        }

        [UnityTest]
        public IEnumerator Vida_KO_ApagaElControlYLaCamaraCae()
        {
            yield return Calentar();
            vida.RecibirDanio(new InfoDanio(vida.Maxima, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.IsFalse(controlador.ControlActivo);

            yield return Esperar(1f);
            Assert.AreEqual(vida.Datos.AlturaCamaraKO, controlador.Camara.localPosition.y, 0.02f);
            float roll = Mathf.Abs(Mathf.DeltaAngle(0f, controlador.Camara.localEulerAngles.z));
            Assert.AreEqual(vida.Datos.LadeoCamaraKO, roll, 1f);
        }

        [UnityTest]
        public IEnumerator Vida_EnKO_WNoMueveYElClicNoPega()
        {
            yield return Calentar();
            GameObject muneco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            muneco.transform.position = new Vector3(0f, 0.9f, 1.2f);
            muneco.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            MunecoAPie vidaMuneco = muneco.AddComponent<MunecoAPie>();
            yield return null;

            vida.RecibirDanio(new InfoDanio(vida.Maxima, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Vector3 inicio = jugador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.Less((jugador.transform.position - inicio).magnitude, 0.01f, "En KO no se mueve");

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.5f);
            Assert.AreEqual(vidaMuneco.Maxima, vidaMuneco.Actual, 0.0001f, "En KO el clic no pega");
            Object.Destroy(muneco);
        }

        [UnityTest]
        public IEnumerator Vida_RestaurarDespuesDeKO_VidaCompletaYWMueveOtraVez()
        {
            yield return Calentar();
            float alturaOjos = controlador.Camara.localPosition.y;
            vida.RecibirDanio(new InfoDanio(vida.Maxima, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return Esperar(0.5f);

            vida.Restaurar();
            Assert.IsFalse(vida.EstaKO);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.0001f);
            Assert.IsTrue(controlador.ControlActivo);

            yield return Esperar(0.2f);
            Vector3 inicio = jugador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(jugador.transform.position.z - inicio.z, 2f);
            Assert.AreEqual(alturaOjos, controlador.Camara.localPosition.y, 0.1f, "La cámara vuelve a la altura de los ojos");
        }

        [UnityTest]
        public IEnumerator Vida_DaniadorYRevividorDePrueba_LlevanAKOYRevivenConLaTecla()
        {
            yield return Calentar();
            GameObject herramientas = new GameObject("Herramientas");
            herramientas.SetActive(false);
            var daniador = herramientas.AddComponent<MechaCyberpunk.Combate.Pruebas.DaniadorDePrueba>();
            daniador.Configurar(jugador, 50f, new InputAction("Danio", InputActionType.Button, "<Keyboard>/h"));
            var revividor = herramientas.AddComponent<RevividorDePrueba>();
            revividor.Configurar(vida, new InputAction("Revivir", InputActionType.Button, "<Keyboard>/k"));
            herramientas.SetActive(true);
            yield return null;

            for (int i = 0; i < 2; i++)
            {
                Press(teclado.hKey);
                yield return null;
                Release(teclado.hKey);
                yield return null;
            }
            Assert.IsTrue(vida.EstaKO);

            Press(teclado.kKey);
            yield return null;
            Release(teclado.kKey);
            yield return null;
            Assert.IsFalse(vida.EstaKO);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.0001f);
            Object.Destroy(herramientas);
        }
    }
}
