using System.Collections;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-899: el menú de inicio con la base visual H10: panel compacto, datos reales, letra Rajdhani y opciones dentro del panel.</summary>
    public class InicioTrazoH10Pruebas
    {
        private MenuInicio menu;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Proyecto/Escenas/Inicio.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            menu = Object.FindFirstObjectByType<MenuInicio>();
            Assert.IsNotNull(menu);
        }

        [UnityTest]
        public IEnumerator Opciones_QuedanDentroDelPanelCompacto()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            RectTransform panel = Buscar("PanelMenu");
            Assert.Less(panel.rect.height, 600f, "El panel no ocupa toda la altura");
            Rect zona = EnPantalla(panel);
            foreach (BotonInicio boton in new[] { menu.Nueva, menu.BotonOpciones, menu.Creditos, menu.Salir })
            {
                Rect r = EnPantalla((RectTransform)boton.transform);
                Assert.IsTrue(zona.Contains(r.min) && zona.Contains(r.max), boton.Etiqueta + " queda dentro del panel");
            }
        }

        [Test]
        public void Botones_UsanRajdhani()
        {
            Assert.AreEqual(TrazoH10.Fuente, menu.Nueva.Texto.font);
        }

        [UnityTest]
        public IEnumerator PanelDeDatos_DiceSiNoHayPartida()
        {
            yield return null;
            Text dato = Buscar("DatoPartida").GetComponent<Text>();
            Assert.IsFalse(string.IsNullOrEmpty(dato.text), "Siempre dice algo real sobre la partida");
        }

        [UnityTest]
        public IEnumerator Entrada_TerminaConTodoVisible()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            CanvasGroup grupo = menu.Nueva.GetComponent<CanvasGroup>();
            Assert.IsNotNull(grupo, "La opción entró con su animación");
            Assert.AreEqual(1f, grupo.alpha, 0.01f);
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_UI") == "1")
            {
                CapturaUI.Capturar("docs/capturas/ui-inicio.png", Camera.main);
            }
        }

        [UnityTest]
        public IEnumerator Aviso_UsaCoralYSeCierraConClic()
        {
            menu.MostrarAviso("LA PARTIDA GUARDADA ESTÁ DAÑADA");
            yield return new WaitForSecondsRealtime(0.4f);
            RectTransform aviso = Buscar("Aviso");
            Assert.AreEqual(TrazoH10.Coral, aviso.Find("Linea").GetComponent<Image>().color);
            Assert.IsTrue(aviso.GetComponent<Image>().raycastTarget, "El aviso recibe el clic");
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_UI") == "1")
            {
                CapturaUI.Capturar("docs/capturas/ui-inicio-aviso.png", Camera.main);
            }
        }

        private RectTransform Buscar(string nombre)
        {
            foreach (RectTransform r in menu.Canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (r.name == nombre)
                {
                    return r;
                }
            }
            Assert.Fail("Falta " + nombre);
            return null;
        }

        private static Rect EnPantalla(RectTransform r)
        {
            Vector3[] esquinas = new Vector3[4];
            r.GetWorldCorners(esquinas);
            return Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
        }
    }
}
