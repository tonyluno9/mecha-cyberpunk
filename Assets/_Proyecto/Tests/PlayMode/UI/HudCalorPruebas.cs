using System.Collections;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>El grupo de calor del HUD a pie: oculto en 0, marcas por nivel y nada en el mecha.</summary>
    public class HudCalorPruebas : BaseHudObjetivo
    {
        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.1f);
        }

        [UnityTest]
        public IEnumerator Calor_EnCero_GrupoOculto()
        {
            yield return Preparar();
            Assert.IsNotNull(Registro<ICalor>.Actual, "El jugador trae el servicio de calor");
            Assert.IsFalse(hud.GrupoCalor.activeSelf);
        }

        [UnityTest]
        public IEnumerator Calor_Nivel1_AparecaConUnaMarcaNaranja()
        {
            yield return Preparar();
            Registro<ICalor>.Actual.Sumar(30f, MotivoCalor.Otro);
            yield return null;

            Assert.IsTrue(hud.GrupoCalor.activeSelf);
            Assert.AreEqual("CALOR", hud.TextoCalor.text);
            Assert.AreEqual(3, hud.MarcasCalor.Length);
            Assert.AreEqual(1, hud.MarcasCalorLlenas);
        }

        [UnityTest]
        public IEnumerator Calor_Nivel3_MuestraTresMarcas()
        {
            yield return Preparar();
            Registro<ICalor>.Actual.Sumar(80f, MotivoCalor.Otro);
            yield return null;

            Assert.IsTrue(hud.GrupoCalor.activeSelf);
            Assert.AreEqual(3, hud.MarcasCalorLlenas);
        }

        [UnityTest]
        public IEnumerator Calor_AlBorrar_VuelveAOcultarse()
        {
            yield return Preparar();
            Registro<ICalor>.Actual.Sumar(80f, MotivoCalor.Otro);
            yield return null;
            Registro<ICalor>.Actual.Borrar();
            yield return null;

            Assert.IsFalse(hud.GrupoCalor.activeSelf);
        }

        [UnityTest]
        public IEnumerator Calor_EnModoMecha_ElHudNoMuestraNada()
        {
            JugadorFalso falso = RegistrarFalso(ModoJugador.APie);
            CrearHud();
            CrearServicio();
            yield return Calentar();
            Registro<ICalor>.Actual.Sumar(80f, MotivoCalor.Otro);
            yield return null;
            Assert.IsTrue(hud.GrupoCalor.activeSelf, "A pie con calor se ve");

            falso.CambiarModo(ModoJugador.Mecha);
            yield return null;
            Assert.IsFalse(hud.GrupoCalor.activeSelf, "En el mecha lo muestra la cabina, no el HUD");
        }
    }
}
