using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.VR.Ruta;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// T89: la línea de ruta sigue manejando carro o moto (ModoJugador.Carro) y sale de la posición del vehículo ocupado; a pie sale de los pies;
    /// en el mecha no hay ruta.
    /// </summary>
    public class T89RutaManejandoPruebas
    {
        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo { get; set; }
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private sealed class VehiculoDoble : MonoBehaviour, IMontable
        {
            public bool OcupadoDoble;
            public ModoJugador Modo => ModoJugador.Carro;
            public string Nombre => "Moto de prueba";
            public Vector3 PuntoSubida => transform.position;
            public bool Ocupado => OcupadoDoble;
            public bool PuedeSubir => !OcupadoDoble;
            public bool PuedeBajar => true;
            public Transform Asiento => transform;
            public Camera Camara => null;
            public event Action<bool> OcupadoCambio { add { } remove { } }
            public bool IntentarSubir(IJugador jugador) => false;
            public bool IntentarBajar(out Pose salida) { salida = Pose.identity; return false; }
            private void OnEnable() => Registro<IMontable>.Agregar(this);
            private void OnDisable() => Registro<IMontable>.Quitar(this);
        }

        private GameObject raiz;
        private GameObject carroLibre;
        private GameObject carroOcupado;

        [TearDown]
        public void Limpiar()
        {
            if (raiz != null) UnityEngine.Object.DestroyImmediate(raiz);
            if (carroLibre != null) UnityEngine.Object.DestroyImmediate(carroLibre);
            if (carroOcupado != null) UnityEngine.Object.DestroyImmediate(carroOcupado);
        }

        [Test]
        public void APie_SaleDeLosPies()
        {
            raiz = new GameObject("T89Jugador");
            raiz.transform.position = new Vector3(1f, 0f, 2f);
            var j = new JugadorDoble { Raiz = raiz.transform, Modo = ModoJugador.APie };
            Assert.IsTrue(RutaObjetivoVR.CalcularOrigen(j, out Vector3 origen, out Transform vehiculo));
            Assert.AreEqual(new Vector3(1f, 0f, 2f), origen);
            Assert.IsNull(vehiculo);
        }

        [Test]
        public void Manejando_SaleDelVehiculoOcupado()
        {
            raiz = new GameObject("T89Jugador");
            raiz.transform.position = new Vector3(-50f, 0f, -50f); // la raíz desfasada: el origen debe ser el vehículo
            carroLibre = new GameObject("T89CarroLibre");
            carroLibre.transform.position = new Vector3(5f, 0f, 5f);
            carroLibre.AddComponent<VehiculoDoble>();
            carroOcupado = new GameObject("T89Moto");
            carroOcupado.transform.position = new Vector3(30f, 0.5f, 40f);
            carroOcupado.AddComponent<VehiculoDoble>().OcupadoDoble = true;
            var j = new JugadorDoble { Raiz = raiz.transform, Modo = ModoJugador.Carro };

            Assert.IsTrue(RutaObjetivoVR.CalcularOrigen(j, out Vector3 origen, out Transform vehiculo), "Manejando la ruta sigue.");
            Assert.AreEqual(carroOcupado.transform.position, origen, "Sale de la posición del vehículo que maneja.");
            Assert.AreSame(carroOcupado.transform, vehiculo);
        }

        [Test]
        public void Manejando_SinMontableRegistrado_SaleDeLaRaiz()
        {
            raiz = new GameObject("T89Jugador");
            raiz.transform.position = new Vector3(7f, 0f, 8f);
            var j = new JugadorDoble { Raiz = raiz.transform, Modo = ModoJugador.Carro };
            Assert.IsTrue(RutaObjetivoVR.CalcularOrigen(j, out Vector3 origen, out Transform vehiculo));
            Assert.AreEqual(new Vector3(7f, 0f, 8f), origen, "La raíz sigue al asiento.");
            Assert.IsNull(vehiculo);
        }

        [Test]
        public void EnElMecha_NoHayRuta()
        {
            raiz = new GameObject("T89Jugador");
            var j = new JugadorDoble { Raiz = raiz.transform, Modo = ModoJugador.Mecha };
            Assert.IsFalse(RutaObjetivoVR.CalcularOrigen(j, out _, out _));
            Assert.IsFalse(RutaObjetivoVR.CalcularOrigen(null, out _, out _));
        }
    }
}
