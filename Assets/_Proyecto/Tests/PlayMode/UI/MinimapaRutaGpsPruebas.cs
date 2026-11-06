using System;
using System.Collections.Generic;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-632: el minimapa dibuja la ruta por las calles hasta el objetivo y la recalcula al moverte.</summary>
    public class MinimapaRutaGpsPruebas
    {
        private class JugadorFalso : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara { get; set; }
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { Raiz.position = pose.position; }
        }

        // T-669: UI ya no ve Trafico; esta ruta de dos esquinas hace de las calles (mitad del camino en cada eje).
        private class RutaDeCallesFalsa : IRutaCalles
        {
            public List<Vector3> Calcular(Vector3 origen, Vector3 destino)
            {
                float medio = (origen.z + destino.z) * 0.5f;
                return new List<Vector3> { origen, new Vector3(origen.x, 0f, medio), new Vector3(destino.x, 0f, medio), destino };
            }
        }

        private class ObjetivosFalso : IObjetivos
        {
            public Objetivo Objetivo;
            public bool HayObjetivo => Objetivo.TienePosicion;
            public Objetivo Actual => Objetivo;
            public event Action<Objetivo> Cambio { add { } remove { } }
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
        public IEnumerator Ruta_SeDibujaPorLasCallesYSeRecalculaAlMoverte()
        {
            camaraObjeto = new GameObject("Camara", typeof(Camera));
            camaraObjeto.transform.rotation = Quaternion.identity;
            Camera camara = camaraObjeto.GetComponent<Camera>();
            camara.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.gameObject.AddComponent<UniversalAdditionalCameraData>();
            JugadorFalso jugador = new JugadorFalso { Raiz = camaraObjeto.transform, Camara = camara };
            camaraObjeto.transform.position = new Vector3(-47f, 0f, -60f);
            ObjetivosFalso objetivos = new ObjetivosFalso { Objetivo = new Objetivo("IR AL TIANGUIS", new Vector3(47f, 0f, 60f)) };
            Registro<IJugador>.Agregar(jugador);
            Registro<IObjetivos>.Agregar(objetivos);
            Registro<IRutaCalles>.Agregar(new RutaDeCallesFalsa());
            minimapaObjeto = new GameObject("Minimapa");
            Minimapa minimapa = minimapaObjeto.AddComponent<Minimapa>();
            yield return null;
            minimapa.Actualizar();

            Assert.IsTrue(minimapa.Visible);
            Assert.GreaterOrEqual(minimapa.Ruta.Count, 4, "Hay ruta con esquinas");
            Assert.AreEqual(minimapa.Ruta.Count - 1, minimapa.SegmentosRutaVisibles, "Un tramo dibujado por cada par de puntos");
            Assert.AreEqual(new Vector3(-47f, 0f, -60f), minimapa.Ruta[0], "La ruta sale de donde estás");
            Assert.AreEqual(new Vector3(47f, 0f, 60f), minimapa.Ruta[minimapa.Ruta.Count - 1], "y llega al objetivo");
            int antes = minimapa.RecalculosRuta;
            Vector3 primero = minimapa.Ruta[0];

            // Caminas 20 m al norte por la misma calle: se recalcula y la ruta ahora empieza donde estás.
            camaraObjeto.transform.position = new Vector3(-47f, 0f, -40f);
            minimapa.Actualizar();
            Assert.Greater(minimapa.RecalculosRuta, antes, "Al moverte se recalcula");
            Assert.AreNotEqual(primero, minimapa.Ruta[0]);
            Assert.AreEqual(-40f, minimapa.Ruta[0].z, 0.01f, "Empieza donde estás");

            // Sin moverte no se recalcula en cada cuadro.
            int despues = minimapa.RecalculosRuta;
            minimapa.Actualizar();
            Assert.AreEqual(despues, minimapa.RecalculosRuta);

            if (Environment.GetEnvironmentVariable("CAPTURAR_T632") == "1")
            {
                yield return null;
                CapturaUI.Capturar("docs/capturas/T-632-minimapa-ruta.png", camara);
            }

            // Sin objetivo no hay ruta.
            objetivos.Objetivo = Objetivo.Ninguno;
            minimapa.Actualizar();
            Assert.AreEqual(0, minimapa.Ruta.Count);
            Assert.AreEqual(0, minimapa.SegmentosRutaVisibles);
        }
    }
}
