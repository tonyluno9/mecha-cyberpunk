using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1498: con la búsqueda al máximo baja una nave de Nova a un punto libre del piso, flota a pocos metros, baja soldados por cuerdas hasta el piso y se va.
    /// Bajo un techo no hay punto libre y no baja ninguna.
    /// </summary>
    public class NaveNovaT1498Pruebas
    {
        private const string RutaNave = "Assets/Arte/Vehiculos/Drones/Prefabs/Dron_NV01.prefab";
        private const int Soldados = 4;

        private sealed class JugadorQuieto : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) => Raiz.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private sealed class CalorMaximo : ICalor
        {
            public float Valor => 100f;
            public float Maximo => 100f;
            public int Nivel => 3;
            public event Action<float> ValorCambio { add { } remove { } }
            public event Action<int> NivelCambio { add { } remove { } }
            public void Sumar(float cantidad, MotivoCalor motivo) { }
            public void Enfriar(float cantidad) { }
            public void Borrar() { }
        }

        private sealed class ZonaDoble : IZonaBusqueda
        {
            public bool Activa => true;
            public Vector3 Centro => Vector3.zero;
            public float Radio => 50f;
            public bool Visto => true;
        }

        private sealed class CuerpoJugador : MonoBehaviour, IRecibeDanio
        {
            public ResultadoDanio RecibirDanio(InfoDanio info) => new ResultadoDanio(0f, TipoBloqueo.Ninguno, false);
        }

        private GameObject piso;
        private GameObject techo;
        private GameObject jugador;
        private RespuestaNova respuesta;
        private ConfigRespuestaNova config;
        private bool habiaActual;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(300f, 1f, 300f);
            jugador = new GameObject("JugadorNave");
            jugador.AddComponent<CapsuleCollider>().center = Vector3.up;
            jugador.AddComponent<CuerpoJugador>();
            Registro<IJugador>.Agregar(new JugadorQuieto { Raiz = jugador.transform });
            Registro<ICalor>.Agregar(new CalorMaximo());
            Registro<IZonaBusqueda>.Agregar(new ZonaDoble());
            habiaActual = RespuestaNova.Actual != null;
            if (habiaActual)
            {
                RespuestaNova.Actual.enabled = false;
            }
            ConfigRespuestaNova original = Resources.Load<ConfigRespuestaNova>(ConfigRespuestaNova.Recurso);
            Assert.IsNotNull(original, "IA/Resources/ConfigRespuestaNova");
            config = Object.Instantiate(original);
            GameObject nave = AssetDatabase.LoadAssetAtPath<GameObject>(RutaNave);
            Assert.IsNotNull(nave, "Dron_NV01");
            config.ConfigurarNaves(new[] { nave }, 2, Soldados);
            respuesta = new GameObject("RespuestaNovaNave").AddComponent<RespuestaNova>();
            respuesta.Configurar(config);
            DirectorRespuesta.DepurarForzarPuertaNova(true); // T-1660: estas pruebas son de Nova, no de su puerta
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(respuesta.gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(piso);
            if (techo != null)
            {
                Object.DestroyImmediate(techo);
            }
            Object.DestroyImmediate(jugador);
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            foreach (NaveNova n in Object.FindObjectsByType<NaveNova>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(n.gameObject);
            }
            if (habiaActual && RespuestaNova.Actual != null)
            {
                RespuestaNova.Actual.enabled = true;
            }
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator CincoEstrellas_LlegaLaNave_FlotaBajaSoldadosPorCuerdaYSeVa()
        {
            NaveNova nave = respuesta.LlamarNave(jugador.transform.position, 5);
            Assert.IsNotNull(nave, "hay un punto libre y llega una nave");
            Vector3 flote = nave.PuntoFlote;
            Assert.AreEqual(config.AlturaFlote, flote.y, 0.01f, "flota a la altura configurada sobre el piso");
            Assert.Less(Vector3.Distance(new Vector3(flote.x, 0f, flote.z), jugador.transform.position), 30f, "baja cerca del jugador");

            float fin = Time.time + 30f;
            while (Time.time < fin && nave != null && nave.Actual == NaveNova.Fase.Llegando)
            {
                yield return null;
            }
            Assert.IsTrue(nave != null && nave.Actual == NaveNova.Fase.Descargando, "llegó y se queda flotando");
            Assert.That(nave.transform.position.y, Is.InRange(config.AlturaFlote - 1f, config.AlturaFlote + 1f), "flota a pocos metros del piso");

            bool vioCuerda = false;
            while (Time.time < fin && nave != null)
            {
                vioCuerda |= Object.FindObjectsByType<DescensoNova>(FindObjectsSortMode.None).Any(d => !d.Llego && d.Cuerda != null && d.Cuerda.positionCount == 2);
                yield return null;
            }
            Assert.IsTrue(vioCuerda, "los soldados bajan colgados de una cuerda");
            Assert.IsTrue(respuesta.Nave == null, "la nave se fue");
            Assert.AreEqual(Soldados, respuesta.DeNaveVivos, "dejó a los soldados de Nova");
            foreach (PandilleroAPie p in respuesta.DeNave)
            {
                DescensoNova d = p.GetComponent<DescensoNova>();
                Assert.IsTrue(d.Llego, "ya tocó el piso");
                Assert.IsTrue(p.enabled, "su IA volvió a funcionar");
                Assert.Less(p.transform.position.y, 1f, "está en el piso");
                Assert.IsNull(d.Cuerda, "la cuerda se quitó");
            }
        }

        [Test]
        public void BajoUnTecho_NoHayPuntoLibre_NoBajaNave()
        {
            techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            techo.transform.position = new Vector3(0f, 8f, 0f);
            techo.transform.localScale = new Vector3(300f, 1f, 300f);
            Physics.SyncTransforms();
            Assert.IsFalse(respuesta.BuscarPuntoNave(jugador.transform.position, out _, out _));
            Assert.IsNull(respuesta.LlamarNave(jugador.transform.position, 5));
        }

        [Test]
        public void TresEstrellas_NoBajaNave()
        {
            Assert.IsNull(respuesta.LlamarNave(jugador.transform.position, 3));
        }
    }
}
