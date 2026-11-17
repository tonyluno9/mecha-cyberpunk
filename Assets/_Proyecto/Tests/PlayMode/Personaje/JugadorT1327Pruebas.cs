using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1327: atropello, golpe que se acerca, daño de caída solo a la vida y bajar escalones sin flotar. Sin clases de policía ni IA.</summary>
    public class JugadorT1327Pruebas : BaseJugadorAPie
    {
        private GameObject muneco;
        private GameObject carro;

        /// <summary>Carro de mentira: un cubo con Rigidbody y el contrato IMontable.</summary>
        private class CarroFalso : MonoBehaviour, IMontable
        {
            public ModoJugador Modo => ModoJugador.Carro;
            public string Nombre => "Carro falso";
            public Vector3 PuntoSubida => transform.position;
            public bool Ocupado => false;
            public bool PuedeSubir => false;
            public bool PuedeBajar => false;
            public Transform Asiento => transform;
            public Camera Camara => null;
            public bool IntentarSubir(IJugador jugador) => false;
            public bool IntentarBajar(out Pose salida) { salida = Pose.identity; return false; }
#pragma warning disable CS0067
            public event Action<bool> OcupadoCambio;
#pragma warning restore CS0067
        }

        public override void TearDown()
        {
            if (muneco != null) UnityEngine.Object.Destroy(muneco);
            if (carro != null) UnityEngine.Object.Destroy(carro);
            base.TearDown();
        }

        private void PonerCarro(Vector3 posicion, Vector3 velocidad)
        {
            carro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carro.transform.position = posicion;
            carro.transform.localScale = new Vector3(2f, 1.2f, 4f);
            Rigidbody cuerpo = carro.AddComponent<Rigidbody>();
            cuerpo.useGravity = false;
            cuerpo.mass = 1200f;
            carro.AddComponent<CarroFalso>();
            Physics.SyncTransforms();
            cuerpo.linearVelocity = velocidad;
        }

        [UnityTest]
        public IEnumerator Atropello_CarroRapido_TiraAlJugadorYLuegoSeLevanta()
        {
            yield return Calentar();
            foreach (Animator a in jugador.GetComponentsInChildren<Animator>(true)) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            AtropelloJugador atropello = jugador.GetComponent<AtropelloJugador>();
            Assert.IsNotNull(atropello, "TreparAPie debe agregar AtropelloJugador");
            yield return Esperar(0.3f);
            // El carro viene por -Z a 12 m/s hacia el jugador (que está en el origen).
            PonerCarro(new Vector3(0f, 0.8f, -6f), new Vector3(0f, 0f, 12f));
            bool cayo = false;
            float zInicial = jugador.transform.position.z;
            float fin = Time.time + 1.5f;
            while (Time.time < fin)
            {
                if (atropello.Derribado)
                {
                    cayo = true;
                    break;
                }
                yield return null;
            }
            Assert.IsTrue(cayo, "el carro rápido derriba al jugador");
            Assert.IsTrue(controlador.Derribado, "mientras está tirado el control de a pie queda bloqueado");
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            if (visual != null && visual.gameObject.activeInHierarchy)
            {
                Assert.IsTrue(visual.Caido, "se ve la animación de caída");
            }
            UnityEngine.Object.Destroy(carro);
            yield return Esperar(0.5f);
            Assert.Greater(jugador.transform.position.z, zInicial + 0.3f, "el golpe lo empuja en la dirección del carro");
            yield return Esperar(AtropelloJugador.SegundosEnSuelo + AtropelloJugador.SegundosLevantandose + 0.3f);
            Assert.IsFalse(atropello.Derribado, "se levanta");
            Assert.IsFalse(controlador.Derribado);
            if (visual != null)
            {
                Assert.IsFalse(visual.Caido, "ya no está tirado");
            }
        }

        [UnityTest]
        public IEnumerator Atropello_CarroLento_NoTira()
        {
            yield return Calentar();
            AtropelloJugador atropello = jugador.GetComponent<AtropelloJugador>();
            yield return Esperar(0.3f);
            PonerCarro(new Vector3(0f, 0.8f, -4f), new Vector3(0f, 0f, 1.5f));
            yield return Esperar(2.5f);
            Assert.AreEqual(0, atropello.Derribos, "un carro a paso de tortuga no tira a nadie");
        }

        [UnityTest]
        public IEnumerator Golpe_Muneco_SeAcercaUnPoco()
        {
            yield return Calentar();
            muneco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            muneco.transform.position = jugador.transform.position + Vector3.forward * 2.2f + Vector3.up * 0.9f;
            muneco.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            muneco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return null;
            float antes = Vector3.Distance(new Vector3(jugador.transform.position.x, 0f, jugador.transform.position.z), new Vector3(0f, 0f, 2.2f));
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.5f);
            float despues = Vector3.Distance(new Vector3(jugador.transform.position.x, 0f, jugador.transform.position.z), new Vector3(0f, 0f, 2.2f));
            Assert.Less(despues, antes - 0.3f, "el golpe dirigido acerca al jugador al muñeco");
        }

        [UnityTest]
        public IEnumerator DanioDeCaida_ConEscudo_SoloBajaLaVida()
        {
            yield return Calentar();
            EscudoJugador escudo = jugador.GetComponent<EscudoJugador>();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            escudo.Iniciar(50f, 0f, 100f);
            escudo.Restaurar();
            yield return null;
            float vidaAntes = vida.Actual;
            vida.RecibirDanioSoloVida(new InfoDanio(10f, TipoDanio.Golpe, jugador.transform.position, Vector3.down, null));
            Assert.AreEqual(50f, escudo.Actual, 1e-3f, "el escudo no absorbe el daño de caída");
            Assert.AreEqual(vidaAntes - 10f, vida.Actual, 1e-3f, "la vida recibe todo");
            vida.RecibirDanio(new InfoDanio(10f, TipoDanio.Golpe, jugador.transform.position, Vector3.forward, null));
            Assert.Less(escudo.Actual, 50f, "control: un golpe normal sí lo absorbe el escudo");
        }

        [UnityTest]
        public IEnumerator Escalones_BajandoCaminando_NoFlota()
        {
            yield return Calentar();
            // Escalones que bajan hacia +Z: 4 escalones de 0.2 m y 0.8 m de huella; el último llega al piso.
            for (int i = 0; i < 4; i++)
            {
                float alto = 0.8f - 0.2f * i;
                Caja(new Vector3(0f, alto / 2f, 1f + 0.8f * i), new Vector3(6f, alto, 0.8f));
            }
            Caja(new Vector3(0f, 0.4f, -1f), new Vector3(6f, 0.8f, 4f));
            Physics.SyncTransforms();
            jugadorAPie.Colocar(new Pose(new Vector3(0f, 0.85f, 0f), Quaternion.identity));
            yield return Esperar(0.5f);
            Assert.IsTrue(controlador.EnPiso, "arranca parado arriba");
            Press(teclado.wKey);
            float segundosEnAire = 0f; // en segundos: en batchmode un cuadro dura ~1 ms
            float fin = Time.time + 2.5f;
            float yMinima = 10f;
            while (Time.time < fin)
            {
                yield return null;
                if (!controlador.EnPiso)
                {
                    segundosEnAire += Time.deltaTime;
                }
                yMinima = Mathf.Min(yMinima, jugador.transform.position.y);
            }
            Release(teclado.wKey);
            Assert.Less(jugador.transform.position.y, 0.15f, "bajó los escalones hasta el piso");
            Assert.LessOrEqual(segundosEnAire, 0.05f, "bajando escalones el jugador sigue pegado al piso");
        }
    }
}
