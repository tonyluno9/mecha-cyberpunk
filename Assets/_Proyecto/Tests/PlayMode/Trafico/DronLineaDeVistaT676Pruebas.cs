using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-676: el dron de la policía solo dispara si ve al jugador; un techo o un muro en medio lo deja sin tiro.</summary>
    public class DronLineaDeVistaT676Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";

        private sealed class CalorDoble : ICalor
        {
            public float Valor => 100f;
            public float Maximo => 100f;
            public int Nivel => 5;
            public event Action<float> ValorCambio { add { } remove { } }
            public event Action<int> NivelCambio { add { } remove { } }
            public void Sumar(float cantidad, MotivoCalor motivo) { }
            public void Enfriar(float cantidad) { }
            public void Borrar() { }
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private sealed class BlancoDoble : MonoBehaviour, IRecibeDanio
        {
            public int Impactos;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Impactos++;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private GameObject piso;
        private GameObject jugador;
        private GameObject dron;
        private GameObject techo;
        private BlancoDoble blanco;
        private Func<float> azarAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            azarAnterior = DronMundo.Azar;
            DronMundo.Azar = () => 0f;
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(100f, 1f, 100f);
            jugador = new GameObject("JugadorDePrueba");
            CapsuleCollider capsula = jugador.AddComponent<CapsuleCollider>();
            capsula.height = 1.8f;
            capsula.center = new Vector3(0f, 0.9f, 0f);
            blanco = jugador.AddComponent<BlancoDoble>();
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugador.transform });
            Registro<ICalor>.Agregar(new CalorDoble());
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            dron = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron), new Vector3(0f, config.AlturaDron, 0f), Quaternion.identity);
            dron.GetComponent<DronMundo>().Preparar(config.Crear(), config, () => jugador.transform.position);
        }

        [TearDown]
        public void Limpiar()
        {
            DronMundo.Azar = azarAnterior;
            Object.Destroy(dron);
            Object.Destroy(jugador);
            Object.Destroy(piso);
            if (techo != null)
            {
                Object.Destroy(techo);
            }
            Registros.LimpiarTodos();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ConLineaLibre_ElDronDispara()
        {
            yield return Esperar(4f);
            Assert.Greater(blanco.Impactos, 0, "El dron ve al jugador y dispara");
        }

        [UnityTest]
        public IEnumerator ConUnTechoEnMedio_ElDronNoDisparaNiAvisa()
        {
            techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            techo.transform.position = new Vector3(0f, 3f, 0f);
            techo.transform.localScale = new Vector3(12f, 0.3f, 12f);
            Physics.SyncTransforms();
            yield return Esperar(5f);
            Assert.AreEqual(0, blanco.Impactos, "Sin línea de vista no hay disparo");
        }
    }
}
