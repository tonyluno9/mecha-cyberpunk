using System;
using System.Collections;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1344: las puertas y el cofre se abren y cierran con su animación, se enganchan al subir y bajar, y sobreviven a que las suelten.</summary>
    public class PuertasCarroT1344Pruebas
    {
        // Montable mínimo: solo lanza OcupadoCambio y da el punto de subida del lado izquierdo.
        private sealed class MontableFalso : MonoBehaviour, IMontable
        {
            public ModoJugador Modo => ModoJugador.Carro;
            public string Nombre => "Falso";
            public Vector3 PuntoSubida => transform.TransformPoint(new Vector3(-1.5f, 0f, 0f));
            public bool Ocupado { get; private set; }
            public bool PuedeSubir => true;
            public bool PuedeBajar => true;
            public Transform Asiento => transform;
            public Camera Camara => null;
            public event Action<bool> OcupadoCambio;
            public bool IntentarSubir(IJugador jugador) { Ocupado = true; OcupadoCambio?.Invoke(true); return true; }
            public bool IntentarBajar(out Pose salida) { salida = Pose.identity; Ocupado = false; OcupadoCambio?.Invoke(false); return true; }
        }

        private GameObject carro;
        private Transform bisagraI;
        private Transform bisagraD;
        private Transform bisagraCofre;
        private PuertasCarro puertas;

        private static Transform Hijo(Transform padre, string nombre, Vector3 pos)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            return g.transform;
        }

        private static void ConMalla(Transform pieza)
        {
            pieza.gameObject.AddComponent<MeshFilter>();
            pieza.gameObject.AddComponent<MeshRenderer>();
        }

        private void Asignar(string campo, Transform valor)
        {
            typeof(PuertasCarro).GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(puertas, valor);
        }

        [SetUp]
        public void Preparar()
        {
            carro = new GameObject("CarroPuertasPrueba");
            carro.SetActive(false);
            Transform visual = Hijo(carro.transform, "Visual_T511", Vector3.zero);
            bisagraI = Hijo(visual, "Bisagra_Puerta_I", new Vector3(-0.9f, 0.5f, 0.5f));
            bisagraD = Hijo(visual, "Bisagra_Puerta_D", new Vector3(0.9f, 0.5f, 0.5f));
            bisagraCofre = Hijo(visual, "Bisagra_Cofre", new Vector3(0f, 0.8f, 0.8f));
            ConMalla(Hijo(bisagraI, "Puerta_I", new Vector3(0f, 0f, -0.5f)));
            ConMalla(Hijo(bisagraD, "Puerta_D", new Vector3(0f, 0f, -0.5f)));
            ConMalla(Hijo(bisagraCofre, "Cofre", new Vector3(0f, 0f, 0.5f)));
            puertas = carro.AddComponent<PuertasCarro>();
            Asignar("bisagraPuertaIzquierda", bisagraI);
            Asignar("bisagraPuertaDerecha", bisagraD);
            Asignar("bisagraCofre", bisagraCofre);
            carro.AddComponent<MontableFalso>();
            carro.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.Destroy(carro);
        }

        [UnityTest]
        public IEnumerator AbrirPuerta_TrasMedioSegundo_EstaA70Grados_YCerrar_Regresa()
        {
            puertas.AbrirPuerta(true);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(puertas.PuertaAbierta(true));
            Assert.AreEqual(70f, Quaternion.Angle(Quaternion.identity, bisagraI.localRotation), 2f);
            Assert.AreEqual(1f, puertas.AperturaPuerta(true), 0.03f);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, bisagraD.localRotation), 0.1f, "La otra puerta no se mueve");
            puertas.CerrarPuerta(true);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, bisagraI.localRotation), 1f);
        }

        [UnityTest]
        public IEnumerator AbrirCofre_Levanta55Grados()
        {
            puertas.AbrirCofre();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(55f, Quaternion.Angle(Quaternion.identity, bisagraCofre.localRotation), 2f);
        }

        [UnityTest]
        public IEnumerator SubirAlCarro_AbreLaPuertaDelLado_YLaCierraDespues()
        {
            IMontable montable = carro.GetComponent<IMontable>();
            montable.IntentarSubir(null);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(puertas.PuertaAbierta(true), "El punto de subida está a la izquierda");
            Assert.IsFalse(puertas.PuertaAbierta(false));
            Assert.Greater(puertas.AperturaPuerta(true), 0.9f);
            yield return new WaitForSeconds(2.4f);
            Assert.IsFalse(puertas.PuertaAbierta(true), "Se cerró sola");
            Assert.Less(puertas.AperturaPuerta(true), 0.01f);
            montable.IntentarBajar(out _);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(puertas.PuertaAbierta(true), "También abre al bajar");
        }

        [UnityTest]
        public IEnumerator SoltarLasPiezas_NoLanzaExcepciones()
        {
            puertas.AbrirPuerta(true);
            puertas.AbrirCofre();
            yield return new WaitForSeconds(0.1f);
            // Igual que DanioCarro.LanzarPiezas: las piezas salen y las bisagras quedan sin hijo.
            bisagraI.GetChild(0).SetParent(null, true);
            bisagraCofre.GetChild(0).SetParent(null, true);
            UnityEngine.Object.Destroy(bisagraD.gameObject);
            puertas.AbrirPuerta(false);
            puertas.CerrarPuerta(true);
            yield return new WaitForSeconds(0.5f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
