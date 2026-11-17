using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1303: si te buscan y estás en un techo, el policía va a la escalera de mano, sube con los clips de escalera y sale al techo; y nunca corre más
    /// rápido que su clip de correr (no flota).
    /// </summary>
    public class PoliciaEscaleraT1303Pruebas
    {
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";

        private sealed class JugadorQuieto : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) => Raiz.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private sealed class CalorFijo : ICalor
        {
            public float Valor => 20f;
            public float Maximo => 100f;
            public int Nivel => 1;
            public event Action<float> ValorCambio { add { } remove { } }
            public event Action<int> NivelCambio { add { } remove { } }
            public void Sumar(float cantidad, MotivoCalor motivo) { }
            public void Enfriar(float cantidad) { }
            public void Borrar() { }
        }

        private sealed class CuerpoJugador : MonoBehaviour, IRecibeDanio
        {
            public ResultadoDanio RecibirDanio(InfoDanio info) => new ResultadoDanio(0f, TipoBloqueo.Ninguno, false);
        }

        private readonly List<GameObject> creados = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = 1f;
            foreach (GameObject g in creados)
            {
                if (g != null) Object.DestroyImmediate(g);
            }
            creados.Clear();
            Registros.LimpiarTodos();
        }

        private GameObject Caja(Vector3 centro, Vector3 tamano)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.position = centro;
            g.transform.localScale = tamano;
            creados.Add(g);
            return g;
        }

        [UnityTest]
        public IEnumerator JugadorEnElTecho_ElPoliciaSubePorLaEscalera_YNoCorreMasQueSuClip()
        {
            Caja(new Vector3(0f, -0.5f, 0f), new Vector3(120f, 1f, 120f)); // piso
            Caja(new Vector3(0f, 1.5f, 6f), new Vector3(12f, 3f, 12f));    // edificio de 3 m; su pared sur está en z = 0
            GameObject escalera = new GameObject("EscaleraPrueba");
            escalera.transform.position = new Vector3(0f, 0f, 0f);
            escalera.transform.rotation = Quaternion.identity; // forward +z: hacia la pared
            escalera.AddComponent<BoxCollider>();
            escalera.AddComponent<PuntoTrepar>().AjustarTrigger();
            creados.Add(escalera);

            GameObject jugador = new GameObject("JugadorEnElTecho");
            jugador.transform.position = new Vector3(0f, 3.05f, 7f);
            jugador.AddComponent<CapsuleCollider>().center = Vector3.up;
            jugador.AddComponent<CuerpoJugador>();
            creados.Add(jugador);
            Registro<IJugador>.Agregar(new JugadorQuieto { Raiz = jugador.transform });
            Registro<ICalor>.Agregar(new CalorFijo());

            GameObject policia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), new Vector3(3f, 0.05f, -14f), Quaternion.identity);
            creados.Add(policia);
            yield return null;
            ConductaEscaleraPolicia conducta = policia.GetComponent<ConductaEscaleraPolicia>();
            Assert.IsNotNull(conducta, "Cada policía trae la conducta de subir escaleras");

            Time.timeScale = 2f;
            bool subio = false;
            bool usoEscalera = false;
            float rapidezMaxima = 0f;
            Vector3 antes = policia.transform.position;
            float fin = Time.time + 45f;
            while (Time.time < fin && !subio)
            {
                yield return null;
                Vector3 ahora = policia.transform.position;
                if (Time.deltaTime > 0f && conducta.FaseActual != ConductaEscaleraPolicia.Fase.Saliendo && conducta.FaseActual != ConductaEscaleraPolicia.Fase.Subiendo)
                {
                    Vector3 paso = ahora - antes;
                    paso.y = 0f;
                    rapidezMaxima = Mathf.Max(rapidezMaxima, paso.magnitude / Time.deltaTime);
                }
                antes = ahora;
                usoEscalera |= conducta.FaseActual == ConductaEscaleraPolicia.Fase.Subiendo;
                subio = ahora.y > 2.6f && conducta.FaseActual == ConductaEscaleraPolicia.Fase.Libre;
            }
            Time.timeScale = 1f;
            Debug.Log("[T-1303] policía en " + policia.transform.position + ", rapidez máxima en el piso " + rapidezMaxima.ToString("F2") + " m/s");
            Assert.IsTrue(usoEscalera, "Sube por la escalera (fase Subiendo)");
            Assert.IsTrue(subio, "Llega al techo y vuelve a su IA normal");
            Assert.LessOrEqual(rapidezMaxima, PandilleroAPie.VelocidadCorrerPolicia + 0.6f, "Nunca corre más rápido que su clip (no flota)");
        }
    }
}
