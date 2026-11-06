using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-665: el minimapa dibuja el círculo de búsqueda de la policía donde te vieron por última vez, y parpadea cuando no te ven.</summary>
    public class CirculoBusquedaMapaPruebas
    {
        private class JugadorFalso : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara { get; set; }
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { Raiz.position = pose.position; }
        }

        private class ZonaFalsa : IZonaBusqueda
        {
            public bool Activa { get; set; }
            public Vector3 Centro { get; set; }
            public float Radio { get; set; }
            public bool Visto { get; set; }
        }

        private GameObject camaraObjeto;
        private GameObject minimapaObjeto;

        [SetUp]
        public void Preparar() => Registros.LimpiarTodos();

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.Destroy(camaraObjeto);
            UnityEngine.Object.Destroy(minimapaObjeto);
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator Circulo_SeDibujaEnElCentroDondeTeVieron_ConSuRadio_YSeEscondeSinBusqueda()
        {
            camaraObjeto = new GameObject("Camara", typeof(Camera));
            Camera camara = camaraObjeto.GetComponent<Camera>();
            camara.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camaraObjeto.transform.position = new Vector3(-47f, 0f, -30f);
            Registro<IJugador>.Agregar(new JugadorFalso { Raiz = camaraObjeto.transform, Camara = camara });
            ZonaFalsa zona = new ZonaFalsa { Activa = false, Centro = new Vector3(-47f, 0f, -75f), Radio = 90f, Visto = true };
            Registro<IZonaBusqueda>.Agregar(zona);
            minimapaObjeto = new GameObject("Minimapa");
            Minimapa minimapa = minimapaObjeto.AddComponent<Minimapa>();
            yield return null;
            minimapa.Actualizar();
            Assert.IsFalse(minimapa.CirculoBusqueda.Visible, "Sin búsqueda no hay círculo");

            zona.Activa = true;
            minimapa.Actualizar();
            Assert.IsTrue(minimapa.CirculoBusqueda.Visible, "Con búsqueda se dibuja");
            Assert.AreEqual(2f * 90f * minimapa.Escala, minimapa.CirculoBusqueda.Rect.sizeDelta.x, 0.01f, "Su tamaño es el radio de búsqueda a la escala del minimapa");
            Vector2 esperado = new Vector2(zona.Centro.x, zona.Centro.z) * minimapa.Escala;
            Assert.AreEqual(esperado.x, minimapa.CirculoBusqueda.Rect.anchoredPosition.x, 0.01f);
            Assert.AreEqual(esperado.y, minimapa.CirculoBusqueda.Rect.anchoredPosition.y, 0.01f);

            // Cuando no te ven parpadea entre rojo y azul.
            zona.Visto = false;
            bool vioRojo = false;
            bool vioAzul = false;
            float fin = Time.realtimeSinceStartup + 1.6f;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
                minimapa.Actualizar();
                Color c = minimapa.CirculoBusqueda.ColorBorde;
                vioRojo |= c.r > c.b;
                vioAzul |= c.b > c.r;
            }
            Assert.IsTrue(vioRojo && vioAzul, "Sin que te vean, el círculo parpadea en rojo y azul");

            zona.Visto = true;
            yield return null;
            minimapa.Actualizar();
            Color fijo = minimapa.CirculoBusqueda.ColorBorde;
            Assert.Greater(fijo.r, fijo.b, "Mientras te ven, rojo fijo");

            CapturaUI.Capturar("docs/capturas/T-665-minimapa-circulo.png", camara);
            zona.Activa = false;
            minimapa.Actualizar();
            Assert.IsFalse(minimapa.CirculoBusqueda.Visible, "Al escapar se esconde");
        }
    }
}
