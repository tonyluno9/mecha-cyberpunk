using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-51: el minimapa ya no dibuja ruta ni línea punteada al objetivo; solo el rombo.</summary>
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
        public IEnumerator ConObjetivo_HayRomboPeroNoLineaDeRuta()
        {
            camaraObjeto = new GameObject("Camara", typeof(Camera));
            Camera camara = camaraObjeto.GetComponent<Camera>();
            JugadorFalso jugador = new JugadorFalso { Raiz = camaraObjeto.transform, Camara = camara };
            camaraObjeto.transform.position = new Vector3(-47f, 0f, -60f);
            Registro<IJugador>.Agregar(jugador);
            Registro<IObjetivos>.Agregar(new ObjetivosFalso { Objetivo = new Objetivo("IR AL TIANGUIS", new Vector3(-47f, 0f, -20f)) });
            minimapaObjeto = new GameObject("Minimapa");
            Minimapa minimapa = minimapaObjeto.AddComponent<Minimapa>();
            yield return null;
            minimapa.Actualizar();

            Assert.IsTrue(minimapa.Visible);
            Assert.IsTrue(minimapa.RomboVisible, "El rombo del objetivo se queda");
            Assert.IsNull(minimapa.Raiz.Find("Mascara/Mundo/Desplazado/RutaGps"), "No hay capa de ruta");
        }
    }
}
