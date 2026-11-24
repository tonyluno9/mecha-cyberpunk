using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T90: al empezar una cinemática se cierran los menús abiertos (aquí la ventana de aviso del tutorial) y al terminar no vuelven.</summary>
    public class CierreMenusCinematicaT90Pruebas
    {
        [TearDown]
        public void Limpiar()
        {
            CinematicaEnCurso.Fijar(false);
            while (VentanaAviso.Abierta)
            {
                VentanaAviso.Entender();
            }
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator EmpezarCinematica_ConAvisoAbierto_LoCierraYNoVuelveAlTerminar()
        {
            VentanaAviso.Mostrar("AVISO UNO");
            VentanaAviso.Mostrar("AVISO DOS");
            yield return null;
            Assert.IsTrue(VentanaAviso.Abierta);
            Assert.IsTrue(Pausa.Activa);
            int veces = CierreMenusCinematica.Veces;

            CinematicaEnCurso.Fijar(true);

            Assert.AreEqual(veces + 1, CierreMenusCinematica.Veces, "el vigilante escucha el inicio de la cinemática");
            Assert.IsFalse(VentanaAviso.Abierta, "la ventana se cierra en el mismo cuadro, antes de que la cinemática guarde el estado");
            Assert.IsFalse(Pausa.Activa, "la pausa de la ventana se suelta");
            Assert.AreEqual(1f, Time.timeScale, "el tiempo vuelve");
            Assert.GreaterOrEqual(CierreMenusCinematica.UltimosCerrados, 1);

            yield return null;
            CinematicaEnCurso.Fijar(false);
            yield return null;
            yield return null;
            Assert.IsFalse(VentanaAviso.Abierta, "al terminar no se reabre (ni el aviso de la cola)");
            Assert.AreEqual(veces + 1, CierreMenusCinematica.Veces, "terminar no vuelve a cerrar ni abrir nada");
        }

        [UnityTest]
        public IEnumerator CerrarTodo_DosVeces_LaSegundaNoCierraNada()
        {
            VentanaAviso.Mostrar("AVISO");
            yield return null;
            Assert.GreaterOrEqual(CierreMenusCinematica.CerrarTodo(), 1, "la primera cierra la ventana");
            Assert.AreEqual(0, CierreMenusCinematica.CerrarTodo(), "con todo cerrado no hay nada que cerrar");
            Assert.IsFalse(PestanasBancoTino.CerrarPorCinematica(), "sin banco abierto no hay nada que cerrar");
            Assert.IsFalse(VentanaAviso.Abierta);
        }
    }
}
