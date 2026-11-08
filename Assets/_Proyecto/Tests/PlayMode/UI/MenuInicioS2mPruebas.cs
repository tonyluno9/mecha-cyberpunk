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
