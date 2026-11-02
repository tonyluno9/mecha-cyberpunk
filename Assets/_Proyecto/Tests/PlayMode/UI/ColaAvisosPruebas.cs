using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-523: la cola de avisos del centro en el HUD vivo, y el HUD sin lista de controles ni línea de radio.</summary>
    public class ColaAvisosPruebas : BaseHudObjetivo
    {
        private const string CanvasDeLaCola = "CanvasAvisos";

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.1f);
        }

        // Los textos que se dibujan ahora: activos, encendidos, con letras y con el grupo visible (alfa mayor que 0).
        private static List<Text> TextosVisibles()
        {
            List<Text> lista = new List<Text>();
            foreach (Text t in Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!t.enabled || string.IsNullOrWhiteSpace(t.text) || !t.gameObject.activeInHierarchy)
                {
                    continue;
                }
                bool oculto = false;
                foreach (CanvasGroup g in t.GetComponentsInParent<CanvasGroup>())
                {
                    oculto |= g.alpha <= 0f;
                }
                if (!oculto)
                {
                    lista.Add(t);
                }
            }
            return lista;
        }

        private static bool EsDeLaCola(Text t) => t.canvas != null && t.canvas.name == CanvasDeLaCola;

        private static int CuantosDelCentro()
        {
            int cuenta = 0;
            foreach (Text t in TextosVisibles())
            {
                if (EsDeLaCola(t))
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Cola_TresAvisosAlMismoTiempo_SeVenDeUnoEnUnoYEnOrden()
        {
            yield return Preparar();
            ColaAvisos cola = ColaAvisos.Actual;
            Assert.IsNotNull(cola, "el HUD trae la cola");
            ColaAvisos.Publicar("PRIMERO", PrioridadAviso.Normal);
            ColaAvisos.Publicar("SEGUNDO", PrioridadAviso.Normal);
            ColaAvisos.Publicar("TERCERO", PrioridadAviso.Normal);
            yield return null;
            yield return null;
            Assert.IsTrue(cola.Visible);
            Assert.AreEqual("PRIMERO", cola.Texto);
            Assert.AreEqual(1, CuantosDelCentro(), "un solo texto en el centro");
            Assert.AreEqual(2, cola.EnEspera);

            foreach (string esperado in new[] { "PRIMERO", "SEGUNDO", "TERCERO" })
            {
                float limite = Time.realtimeSinceStartup + 6f;
                Assert.AreEqual(esperado, cola.Texto);
                while (cola.Visible && cola.Texto == esperado && Time.realtimeSinceStartup < limite)
                {
                    Assert.LessOrEqual(CuantosDelCentro(), 1);
                    yield return null;
                }
            }
            Assert.IsFalse(cola.Visible, "al acabar, la cola se esconde");
        }

        [UnityTest]
        public IEnumerator Cola_UnPremio_PasaPorLaColaYNoSeDibujaEnSuSitioViejo()
        {
            yield return Preparar();
            Registro<ICartera>.Actual.Abonar(1250, MotivoDinero.Premio);
            yield return null;
            yield return null;
            Assert.IsTrue(ColaAvisos.Actual.Visible);
            StringAssert.Contains("+Ƀ1,250", ColaAvisos.Actual.Texto);
            foreach (Text t in TextosVisibles())
            {
                if (!EsDeLaCola(t))
                {
                    StringAssert.DoesNotContain("PREMIO", t.text, "el aviso viejo junto a los Biits ya no se dibuja: " + t.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Hud_NoTieneListaDeControlesNiLineaDeRadio()
        {
            yield return Preparar();
            yield return Esperar(0.5f);
            Assert.IsNull(Object.FindFirstObjectByType<AyudaControles>(FindObjectsInactive.Include), "sin línea fija de controles");
            hud.MostrarCancion("Cumbia del Chip");
            yield return null;
            foreach (Text t in TextosVisibles())
            {
                string texto = t.text.ToUpperInvariant();
                StringAssert.DoesNotContain("WASD", texto, t.name);
                StringAssert.DoesNotContain("CAMINAR", texto, t.name);
                StringAssert.DoesNotContain("SIN SEÑAL", texto, t.name);
                if (!EsDeLaCola(t))
                {
                    StringAssert.DoesNotContain("RADIO", texto, t.name);
                }
            }
            Assert.IsFalse(hud.GrupoCancion.activeSelf, "la línea fija de radio no se ve");
            StringAssert.Contains("Cumbia del Chip", ColaAvisos.Actual.Texto, "al cambiar de estación sale un aviso breve en la cola");
        }
    }
}
