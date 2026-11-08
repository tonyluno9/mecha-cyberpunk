using System.Collections;
using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-893: el menú de inicio con el fondo de calles reales, el botón CARGAR y el recuadro de aviso.</summary>
    public class MenuInicioS2mPruebas
    {
        private MenuInicio menu;

        [UnityTest]
        public IEnumerator CamaraReal_RecorreLasCallesConCorteCada8Segundos()
        {
            // T-898: se vuelve a cargar el menú, ahora con la ciudad; sin scripts de juego en esa escena.
            FondoCallesInicio.CargarEnBatch = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Proyecto/Escenas/Inicio.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            menu = Object.FindFirstObjectByType<MenuInicio>();
            float limite = Time.realtimeSinceStartup + 120f;
            while (!menu.FondoCalles.CamaraReal && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            FondoCallesInicio.CargarEnBatch = false;
            Assert.IsTrue(menu.FondoCalles.CamaraReal, "La escena de calles cargó");
            RecorridoCallesMenu recorrido = RecorridoCallesMenu.Actual;
            Assert.IsNotNull(recorrido);
            Assert.AreEqual(FondoCallesInicio.Calles, recorrido.Calles);
            Assert.AreEqual(0, recorrido.Calle);
            Vector3 antes = recorrido.Camara.transform.position;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(Vector3.Distance(antes, recorrido.Camara.transform.position), 0.5f, "La cámara se mueve por la calle");
            CapturarCalle(recorrido.Camara, 0);
            for (int i = 1; i < FondoCallesInicio.Calles; i++)
            {
                float hasta = Time.realtimeSinceStartup + recorrido.SegundosPorCalle + 3f;
                while (recorrido.Calle != i && Time.realtimeSinceStartup < hasta)
                {
                    yield return null;
                }
                Assert.AreEqual(i, recorrido.Calle, "Corta a la calle " + i);
                yield return new WaitForSecondsRealtime(0.5f);
                CapturarCalle(recorrido.Camara, i);
            }
        }

        private static void CapturarCalle(Camera camara, int calle)
        {
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_S2M_INICIO") == "1")
            {
                CapturaUI.Capturar("docs/capturas/s2m-c1-calle-" + calle + ".png", camara);
            }
        }

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Proyecto/Escenas/Inicio.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            menu = Object.FindFirstObjectByType<MenuInicio>();
            Assert.IsNotNull(menu);
        }

        [Test]
        public void Fondo_TieneLasCuatroCallesDelJuego()
        {
            Assert.AreEqual(FondoCallesInicio.Calles, menu.FondoCalles.CantidadFotos);
        }

        [Test]
        public void Cargar_EstaEnElMenuConSuEtiqueta()
        {
            Assert.IsNotNull(menu.Cargar);
            Assert.AreEqual(MenuInicio.TextoCargar, menu.Cargar.Etiqueta);
        }

        [UnityTest]
        public IEnumerator Aviso_SeMuestraYSeCierra()
        {
            Assert.IsFalse(menu.AvisoVisible);
            menu.MostrarAviso("LA PARTIDA GUARDADA ESTÁ DAÑADA");
            Assert.IsTrue(menu.AvisoVisible);
            Assert.AreEqual("LA PARTIDA GUARDADA ESTÁ DAÑADA", menu.TextoDelAviso);
            menu.CerrarAviso();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(menu.AvisoVisible);
        }
    }
}
