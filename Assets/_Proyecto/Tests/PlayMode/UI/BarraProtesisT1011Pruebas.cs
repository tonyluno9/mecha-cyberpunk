using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1011: la barra de prótesis como el mockup M31 en la ciudad real: "PIERNAS" con el dibujo de la prótesis a la izquierda y el medidor debajo, en columna en el lateral izquierdo (T-1302);
    /// la activa en amarillo. Con CAPTURAR_T991=1 deja docs/capturas/t1011/barra-m31.png.
    /// </summary>
    public class BarraProtesisT1011Pruebas : BaseHudObjetivo
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaImplantes = "Assets/_Proyecto/Progresion/Datos/Implantes/";

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            if (SceneManager.GetActiveScene().path == RutaEscena)
            {
                foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(raiz);
                Registros.LimpiarTodos();
            }
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Ciudad_BarraConPiernasActivasYBrazos_ComoM31()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ServicioImplantes>.Actual != null;
            }, "el director arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                Press(teclado.f6Key);
                yield return null;
                Release(teclado.f6Key);
                yield return null;
            }
            Registro<IRelojMundo>.Actual?.AvanzarHasta(12f);
            ServicioImplantes implantes = Registro<ServicioImplantes>.Actual;
            implantes.QuitarTodos();
            implantes.Regalar(AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaImplantes + "DatosImplante_implante_piernas_2.asset"));
            implantes.Regalar(AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaImplantes + "DatosImplante_implante_brazos_1.asset"));
            yield return new WaitForSecondsRealtime(0.5f);

            BarraHabilidades barra = Object.FindFirstObjectByType<BarraHabilidades>();
            Assert.IsNotNull(barra);
            Assert.IsTrue(barra.Visible, "A pie la barra se ve");
            Assert.AreEqual("PIERNAS", barra.EtiquetaMostrada(HabilidadProtesis.Piernas));
            Assert.AreEqual("BRAZOS", barra.EtiquetaMostrada(HabilidadProtesis.Brazos));
            Assert.AreEqual("CABEZA", barra.EtiquetaMostrada(HabilidadProtesis.Cabeza), "Sin visor la casilla sigue con su nombre, en gris");
            Assert.AreEqual("—", barra.NombreMostrado(HabilidadProtesis.Cabeza));
            Assert.IsNotNull(barra.IconoMostrado(HabilidadProtesis.Piernas), "Lleva el dibujo de la prótesis");
            Assert.AreEqual("Protesis_Piernas_2", barra.IconoMostrado(HabilidadProtesis.Piernas).name, "Con el nivel II, el dibujo del II");
            Assert.AreEqual("Protesis_Brazos_1", barra.IconoMostrado(HabilidadProtesis.Brazos).name);
            RectTransform piernas = barra.CasillaDe(HabilidadProtesis.Piernas);
            RectTransform cabeza = barra.CasillaDe(HabilidadProtesis.Cabeza);
            Assert.Greater(piernas.anchoredPosition.y, cabeza.anchoredPosition.y, "En columna, de arriba abajo en el orden de sus teclas");
            Assert.AreEqual(piernas.anchoredPosition.x, cabeza.anchoredPosition.x, 0.5f, "Todas en la misma columna");
            Assert.AreEqual(BarraHabilidades.MargenIzquierdo, piernas.anchoredPosition.x, 0.5f, "Pegada al lado izquierdo");
            Assert.AreEqual(0f, piernas.anchorMin.x, "Anclada a la izquierda");

            // La tecla [Q] ya la prueba HabilidadesProtesisT992Pruebas; aquí se activa directo (en la ciudad la historia puede tener la entrada ocupada).
            Assert.AreEqual(1, HabilidadesProtesis.Actual.Nivel(HabilidadProtesis.Piernas) > 0 ? 1 : 0, "Tiene las piernas");
            Assert.IsTrue(HabilidadesProtesis.Actual.Pulsar(HabilidadProtesis.Piernas), "Se activan las piernas");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(EstadoHabilidad.Activa, HabilidadesProtesis.Actual.Estado(HabilidadProtesis.Piernas));
            Color activa = barra.ColorMostrado(HabilidadProtesis.Piernas);
            Assert.Greater(activa.r, 0.9f, "La activa va en amarillo");
            Assert.Greater(activa.g, 0.8f);
            Assert.Less(activa.b, 0.4f);
            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") == "1")
            {
                Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1011"));
                CapturaUI.Capturar(Path.Combine("docs", "capturas", "t1011", "barra-m31.png"), Camera.main);
            }
        }
    }
}
