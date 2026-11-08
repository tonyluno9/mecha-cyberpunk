using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-726: en una cinemática el HUD (HudH10, avisos, minimapa, marcador) se oculta y solo queda el letrero de saltar con la tecla real del Input System;
    /// y Opciones > Controles muestra las teclas reales de las acciones (no texto fijo). Jugador y HUD reales, entrada simulada.
    /// </summary>
    public class HudCinematicaT726Pruebas : BaseHudObjetivo
    {
        private HudH10 h10;

        [TearDown]
        public void SalirDeLaCinematica()
        {
            while (HudCinematica.Activa)
            {
                HudCinematica.Salir();
            }
        }

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.3f);
            h10 = hud.GetComponent<HudH10>();
            Assert.IsTrue(h10.Armado);
        }

        private List<Canvas> LienzosEncendidosDelHud()
        {
            List<Canvas> lista = new List<Canvas>();
            foreach (Canvas c in hud.GetComponentsInChildren<Canvas>())
            {
                if (c.enabled)
                {
                    lista.Add(c);
                }
            }
            return lista;
        }

        [UnityTest]
        public IEnumerator Cinematica_OcultaTodoElHudYAlSalirVuelve()
        {
            yield return Preparar();
            List<Canvas> antes = LienzosEncendidosDelHud();
            Assert.IsTrue(antes.Contains(h10.Canvas), "El HUD nuevo se ve antes de la cinemática");
            Assert.Greater(antes.Count, 0);

            HudCinematica.Entrar();
            yield return null;
            yield return null;
            Assert.IsTrue(HudCinematica.Activa);
            foreach (Canvas c in antes)
            {
                Assert.IsFalse(c.enabled, "Oculto en la cinemática: " + c.name);
            }
            Assert.AreEqual(0, LienzosEncendidosDelHud().Count, "No queda ningún lienzo del HUD dibujándose");
            Assert.GreaterOrEqual(HudCinematica.Ocultador.Ocultos, antes.Count);

            // Aunque el HUD quiera dibujarse otra vez (HudH10 y los avisos se actualizan cada cuadro), sigue oculto.
            h10.Canvas.enabled = true;
            yield return null;
            Assert.IsFalse(h10.Canvas.enabled, "No vuelve mientras dure la cinemática");

            HudCinematica.Salir();
            yield return null;
            yield return null;
            Assert.IsFalse(HudCinematica.Activa);
            foreach (Canvas c in antes)
            {
                Assert.IsTrue(c.enabled, "Vuelve al terminar: " + c.name);
            }
        }

        [UnityTest]
        public IEnumerator Cinematica_ElLetreroDeSaltarNoSeOcultaYUsaLaTeclaRealDelInputSystem()
        {
            yield return Preparar();
            GameObject barras = new GameObject("BarrasDeCinematica");
            Canvas lienzo = KitUI.CrearCanvas(barras.transform, "CanvasCinematica", 30);
            HudCinematica.Entrar();
            yield return null;
            yield return null;
            Assert.IsTrue(lienzo.enabled, "El lienzo de la cinemática se queda (solo se oculta el del HUD)");
            Assert.AreEqual("ENTER: SALTAR", HudCinematica.TextoSaltar());
            Assert.IsFalse(HudCinematica.SaltarPresionado, "Sin pulsar nada no salta");
            yield return Tecla(teclado.enterKey);
            yield return null;
            Press(teclado.enterKey);
            yield return null;
            Assert.IsTrue(HudCinematica.SaltarPresionado, "Enter (tecla simulada del Input System) salta");
            Release(teclado.enterKey);
            yield return null;
            Assert.IsFalse(HudCinematica.SaltarPresionado);
            HudCinematica.Salir();
            Object.Destroy(barras);
        }

        [UnityTest]
        public IEnumerator Cinematica_FueraDeUnaCinematicaEnterNoSalta()
        {
            yield return Preparar();
            Press(teclado.enterKey);
            yield return null;
            Assert.IsFalse(HudCinematica.Activa);
            Assert.IsFalse(HudCinematica.SaltarPresionado, "Solo cuenta durante una cinemática");
            Release(teclado.enterKey);
        }

        [UnityTest]
        public IEnumerator Cinematica_DosEntradasSolapadasVuelvenCuandoSalenLasDos()
        {
            yield return Preparar();
            List<Canvas> antes = LienzosEncendidosDelHud();
            HudCinematica.Entrar();
            HudCinematica.Entrar();
            yield return null;
            HudCinematica.Salir();
            yield return null;
            Assert.IsTrue(HudCinematica.Activa);
            Assert.IsFalse(h10.Canvas.enabled, "Sigue oculto con una cinemática aún en curso");
            HudCinematica.Salir();
            yield return null;
            yield return null;
            Assert.IsFalse(HudCinematica.Activa);
            Assert.IsTrue(h10.Canvas.enabled);
            Assert.AreEqual(antes.Count, LienzosEncendidosDelHud().Count);
        }

        // ---- Opciones > Controles ----

        private static string TextoDelPanel(out GameObject raiz)
        {
            raiz = new GameObject("PruebaOpciones");
            Canvas canvas = KitUI.CrearCanvas(raiz.transform, "Canvas", 5);
            PanelOpcionesInicio panel = PanelOpcionesInicio.Crear(canvas.transform);
            return panel.Controles.text;
        }

        [UnityTest]
        public IEnumerator Opciones_ControlesMuestraLasTeclasRealesDeLasAcciones()
        {
            yield return Preparar();
            GameObject raiz;
            string texto = TextoDelPanel(out raiz);
            StringAssert.Contains("MOVER  " + TeclasHud.TextoMover(TeclasHud.Buscar("Mover")).ToUpperInvariant(), texto);
            StringAssert.Contains("CORRER  " + TeclasHud.Texto(TeclasHud.Buscar("Correr")).ToUpperInvariant(), texto);
            StringAssert.Contains("SALTAR  " + TeclasHud.Texto(TeclasHud.Buscar("Saltar")).ToUpperInvariant(), texto);
            StringAssert.Contains("INTERACTUAR  " + TeclasHud.Texto(TeclasHud.Buscar("Interactuar")).ToUpperInvariant(), texto);
            StringAssert.Contains("RECARGAR  " + TeclasHud.Texto(TeclasHud.Buscar("Recargar")).ToUpperInvariant(), texto);
            StringAssert.Contains("TELÉFONO  " + TeclasHud.Texto(TeclasHud.Buscar("Telefono")).ToUpperInvariant(), texto);
            Object.Destroy(raiz);
        }

        [UnityTest]
        public IEnumerator Opciones_ControlesSigueUnaTeclaCambiadaEnElInputSystem()
        {
            yield return Preparar();
            InputAction recargar = TeclasHud.Buscar("Recargar");
            Assert.IsNotNull(recargar);
            GameObject raiz = null;
            try
            {
                recargar.ApplyBindingOverride(0, "<Keyboard>/g");
                string texto = TextoDelPanel(out raiz);
                StringAssert.Contains("RECARGAR  G", texto, "El texto sale de la acción, no está escrito fijo");
                StringAssert.DoesNotContain("RECARGAR  R", texto);
            }
            finally
            {
                recargar.RemoveAllBindingOverrides();
                if (raiz != null)
                {
                    Object.Destroy(raiz);
                }
            }
        }

        [UnityTest]
        public IEnumerator Opciones_EnElMenuDeInicioSinJugadorTambienSalenLasTeclasReales()
        {
            // Sin jugador no hay acciones cargadas en la escena: salen del asset real de ControlesHud.
            GameObject raiz;
            string texto = TextoDelPanel(out raiz);
            StringAssert.Contains("RECARGAR  " + TeclasHud.Texto(ControlesHud.Actual.AccionAPie("Recargar")).ToUpperInvariant(), texto);
            StringAssert.Contains("CORRER  " + TeclasHud.Texto(ControlesHud.Actual.AccionAPie("Correr")).ToUpperInvariant(), texto);
            Object.Destroy(raiz);
            yield return null;
        }
    }
}
