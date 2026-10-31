using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-471: abrir los créditos, que suban solos, que la flecha y la rueda los muevan y que Esc cierre y llame al callback.</summary>
    public class PantallaCreditosPruebas : InputTestFixture
    {
        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.unscaledTime + segundos;
            while (Time.unscaledTime < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Abrir_SeDesplazaYEscCierraConCallback()
        {
            int llamadas = 0;
            PantallaCreditos pantalla = PantallaCreditos.Abrir(() => llamadas++);
            yield return null;
            Assert.IsTrue(pantalla.Abierta);
            Assert.Greater(pantalla.Lineas.Count, 20);
            Assert.IsTrue(PilaPantallas.Contiene(pantalla));
            float inicio = pantalla.Desplazamiento;
            yield return Esperar(0.5f);
            Assert.Greater(pantalla.Desplazamiento, inicio + 5f, "El desplazamiento automático no avanzó.");

            float antesRueda = pantalla.Desplazamiento;
            Set(raton.scroll, new Vector2(0f, -240f));
            yield return null;
            Set(raton.scroll, Vector2.zero);
            yield return null;
            Assert.Greater(pantalla.Desplazamiento, antesRueda + 50f, "La rueda hacia abajo no avanzó.");

            float antesFlecha = pantalla.Desplazamiento;
            Press(teclado.upArrowKey);
            yield return null;
            yield return null;
            Release(teclado.upArrowKey);
            yield return null;
            Assert.Less(pantalla.Desplazamiento, antesFlecha, "La flecha arriba no subió.");

            PressAndRelease(teclado.escapeKey);
            yield return null;
            yield return null;
            Assert.AreEqual(1, llamadas);
            Assert.IsTrue(pantalla == null, "La pantalla no se destruyó.");
            Assert.IsTrue(PilaPantallas.Vacia);
            if (Environment.GetEnvironmentVariable("CAPTURAR_T471") == "1")
            {
                yield return Capturar();
            }
        }

        // Abre otra vez, avanza hasta la parte de equipo y guarda la captura 1920x1080 (solo con CAPTURAR_T471=1).
        private IEnumerator Capturar()
        {
            PantallaCreditos p = PantallaCreditos.Abrir();
            yield return null;
            Canvas canvas = p.GetComponentInChildren<Canvas>();
            GameObject objetoCamara = new GameObject("CamCaptura");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camara;
            canvas.planeDistance = 1f;
            yield return Esperar(6f);
            RenderTexture rt = new RenderTexture(1920, 1080, 24);
            camara.targetTexture = rt;
            camara.aspect = 1920f / 1080f;
            Canvas.ForceUpdateCanvases();
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/T-471-1.png", foto.EncodeToPNG());
            p.Cerrar();
            UnityEngine.Object.Destroy(objetoCamara);
        }
    }
}
