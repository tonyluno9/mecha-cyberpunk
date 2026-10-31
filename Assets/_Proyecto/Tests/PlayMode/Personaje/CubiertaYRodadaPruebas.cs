using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-421: Q se pega a una pared y protege de balas del otro lado; apuntar se asoma; CTRL rueda con invulnerabilidad y enfriamiento.</summary>
    public class CubiertaYRodadaPruebas : BaseJugadorAPie
    {
        private IEnumerator Pulsar(Key tecla)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
            yield return null;
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator Cubrirse_ConQ_ProtegeDeBalasDelOtroLadoYAlApuntarSeAsoma()
        {
            yield return Calentar();
            Caja(new Vector3(0f, 1f, 1.2f), new Vector3(6f, 2f, 0.4f));
            Physics.SyncTransforms();
            yield return null;
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            Assert.IsNotNull(cubierta, "ControladorAPie crea la cubierta");

            yield return Pulsar(Key.Q);
            yield return Esperar(0.5f);
            Assert.IsTrue(cubierta.Cubierto, "Q se pega a la pared cercana");
            Assert.IsTrue(cubierta.Agachado);

            Vector3 tirador = new Vector3(0f, 1.2f, 12f);
            Vector3 direccion = (jugador.transform.position + Vector3.up * 0.6f - tirador).normalized;
            float antes = vida.Actual;
            vida.RecibirDanio(new InfoDanio(20f, TipoDanio.Disparo, jugador.transform.position + Vector3.up * 0.6f, direccion, null));
            Assert.AreEqual(antes, vida.Actual, 0.001f, "La bala del otro lado no hace daño");
            Assert.IsTrue(((IProtegeDeBalas)vida).ProtegeDe(tirador, out Vector3 punto));
            Assert.Greater(punto.z, 0.9f, "Pega en la pared");

            Vector3 delFrente = new Vector3(0f, 1.2f, -12f);
            Assert.IsFalse(((IProtegeDeBalas)vida).ProtegeDe(delFrente, out _), "Una bala del lado abierto no se cubre");

            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine("docs", "capturas", "T-421-1.png"), foto.EncodeToPNG());
                Object.Destroy(foto);
            }

            Press(raton.rightButton);
            yield return null;
            yield return null;
            Assert.IsFalse(cubierta.Agachado, "Al apuntar se asoma");
            Assert.IsFalse(((IProtegeDeBalas)vida).ProtegeDe(tirador, out _), "Asomado no protege");
            Release(raton.rightButton);
            yield return null;

            yield return Pulsar(Key.Q);
            Assert.IsFalse(cubierta.Cubierto, "Q otra vez sale");
        }

        [UnityTest]
        public IEnumerator Rodar_ConCtrl_EsInvulnerableAvanzaYTieneEnfriamiento()
        {
            yield return Calentar();
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            Vector3 inicio = jugador.transform.position;

            InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.LeftCtrl));
            yield return null;
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            yield return null;
            Assert.IsTrue(cubierta.Rodando);
            float antes = vida.Actual;
            vida.RecibirDanio(new InfoDanio(20f, TipoDanio.Disparo, inicio, Vector3.forward, null));
            Assert.AreEqual(antes, vida.Actual, 0.001f, "Invulnerable al empezar a rodar");

            yield return Esperar(cubierta.Datos.DuracionRodada + 0.1f);
            Assert.IsFalse(cubierta.Rodando);
            Assert.Greater((jugador.transform.position - inicio).magnitude, cubierta.Datos.DistanciaRodada * 0.5f, "Se desplazó");
            vida.RecibirDanio(new InfoDanio(20f, TipoDanio.Disparo, inicio, Vector3.forward, null));
            Assert.Less(vida.Actual, antes, "Pasada la rodada vuelve a recibir daño");
            Assert.IsFalse(cubierta.RodadaLista, "Sigue en enfriamiento");
            Assert.IsFalse(cubierta.Rodar());
            yield return Esperar(cubierta.Datos.EnfriamientoRodada);
            Assert.IsTrue(cubierta.RodadaLista);
        }
    }
}
