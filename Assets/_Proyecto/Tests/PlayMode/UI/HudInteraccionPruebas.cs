using System.Collections;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Pruebas jugables del aviso de interacción del HUD: la E frente a puertas con jugador real y entrada simulada.</summary>
    public class HudInteraccionPruebas : BaseHudObjetivo
    {
        private const float DistanciaPuerta = 1.5f;
        private const float MargenAviso = 0.5f;
        private const float HoraAbre = 20f;
        private const float HoraCierra = 6f;

        private static Vector3 PuertaAlFrente => new Vector3(0f, 0f, DistanciaPuerta);

        private IEnumerator Preparar(IdLugar id, bool cerradoEnDemo, bool abiertoPorHorario)
        {
            CrearJugador();
            CrearHud();
            CrearLocal(id, PuertaAlFrente, new HorarioLugar(HoraAbre, HoraCierra), cerradoEnDemo, abiertoPorHorario);
            yield return Calentar();
            yield return Esperar(0.1f);
        }

        [UnityTest]
        public IEnumerator Bar_FrenteALaPuerta_DiceEAbrirBar()
        {
            yield return Preparar(IdLugar.Bar, false, true);

            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual("E · Abrir Bar", hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator Bar_TrasAbrirLaPuerta_ElAvisoDesaparece()
        {
            yield return Preparar(IdLugar.Bar, false, true);

            yield return Tecla(teclado.eKey);
            yield return Esperar(0.3f);

            Assert.IsFalse(hud.GrupoInteraccion.activeSelf, "Una puerta abierta ya no tiene aviso");
            Assert.AreEqual(string.Empty, hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator Clandestino_ConLaEDiceCerradoAbre2000UnosSegundosYVuelveAEAbrir()
        {
            yield return Preparar(IdLugar.Clandestino, false, false);
            Assert.AreEqual("E · Abrir Clandestino", hud.TextoAvisoInteraccion.text);

            yield return Tecla(teclado.eKey);
            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual("CERRADO · abre 20:00", hud.TextoAvisoInteraccion.text);

            yield return Esperar(config.DuracionAvisoPuerta * 0.5f);
            Assert.AreEqual("CERRADO · abre 20:00", hud.TextoAvisoInteraccion.text, "A la mitad de la duración sigue el aviso");

            yield return Esperar(config.DuracionAvisoPuerta * 0.5f + MargenAviso);
            Assert.AreEqual("E · Abrir Clandestino", hud.TextoAvisoInteraccion.text, "Pasada la duración vuelve a lo que hace la E");
        }

        [UnityTest]
        public IEnumerator Casino_CerradoEnLaDemo_ConLaEDiceProximamente()
        {
            yield return Preparar(IdLugar.Casino, true, true);
            Assert.AreEqual("E · Abrir Casino", hud.TextoAvisoInteraccion.text);

            yield return Tecla(teclado.eKey);

            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual("CERRADO · PRÓXIMAMENTE", hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator Puerta_DeEspaldas_NoHayAviso()
        {
            yield return Preparar(IdLugar.Bar, false, true);
            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);

            yield return Girar(180f);
            yield return null;

            Assert.IsFalse(hud.GrupoInteraccion.activeSelf, "Sin puerta de frente no hay aviso");
        }

        [UnityTest]
        public IEnumerator Modo_Carro_NoHayAvisoDeInteraccion()
        {
            CrearHud();
            JugadorFalso falso = RegistrarFalso(ModoJugador.APie);
            CrearLocal(IdLugar.Bar, PuertaAlFrente, new HorarioLugar(HoraAbre, HoraCierra), false, true);
            yield return Calentar();

            falso.CambiarModo(ModoJugador.Carro);
            yield return null;

            Assert.IsFalse(hud.GrupoInteraccion.activeSelf);
            falso.CambiarModo(ModoJugador.Mecha);
            yield return null;
            Assert.IsFalse(hud.GrupoInteraccion.activeSelf);
        }
    }
}
