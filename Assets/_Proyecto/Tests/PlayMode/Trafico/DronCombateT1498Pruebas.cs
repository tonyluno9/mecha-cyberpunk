using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Drones;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1498: un dron de combate (variante DR02 pesado) usa su modelo con rotores, su vida y su daño, le baja vida al jugador a pie con la búsqueda alta y
    /// aparece en el indicador de daño (MarcadorDronHud) mientras dispara.
    /// </summary>
    public class DronCombateT1498Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaModelo = "Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR02_a.prefab";
        private const float DanioVariante = 9f;
        private const float VidaVariante = 90f;

        private sealed class CalorDoble : ICalor
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
            public float DanioTotal;
            public float PrimerDanio;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                if (Impactos == 0)
                {
                    PrimerDanio = info.Cantidad;
                }
                Impactos++;
                DanioTotal += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private GameObject piso;
        private GameObject jugador;
        private GameObject camara;
        private GameObject dron;
        private BlancoDoble blanco;
        private Func<float> azarAnterior;
        private ConfigPersecucion config;

        private DronMundo Preparar(bool conVariante)
        {
            Registros.LimpiarTodos();
            azarAnterior = DronMundo.Azar;
            DronMundo.Azar = () => 0f; // siempre acierta
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(100f, 1f, 100f);
            jugador = new GameObject("JugadorDePrueba");
            CapsuleCollider capsula = jugador.AddComponent<CapsuleCollider>();
            capsula.height = 1.8f;
            capsula.center = new Vector3(0f, 0.9f, 0f);
            blanco = jugador.AddComponent<BlancoDoble>();
            camara = new GameObject("CamaraPrueba") { tag = "MainCamera" };
            camara.AddComponent<Camera>();
            camara.transform.position = new Vector3(0f, 1.6f, -8f);
            Physics.SyncTransforms();
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugador.transform });
            Registro<ICalor>.Agregar(new CalorDoble());
            config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            dron = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron), new Vector3(0f, config.AlturaDron, 0f), Quaternion.identity);
            DronMundo d = dron.GetComponent<DronMundo>();
            d.Preparar(config.Crear(), config, () => jugador.transform.position);
            if (conVariante)
            {
                GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
                Assert.IsNotNull(modelo, "Dron_DR02_a");
                d.AplicarVariante(new VarianteDronCombate("DR02_Pesado", modelo, 4, VidaVariante, DanioVariante, 2f, 0.7f, 6.5f, 1f, 1.1f));
            }
            return d;
        }

        [TearDown]
        public void Limpiar()
        {
            DronMundo.Azar = azarAnterior;
            Object.Destroy(dron);
            Object.Destroy(jugador);
            Object.Destroy(piso);
            Object.Destroy(camara);
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator Variante_PonePesadoConSuModeloRotoresVidaYDanio()
        {
            DronMundo d = Preparar(true);
            yield return null;
            Transform visual = dron.transform.Find(DronMundo.NombreVisualVariante);
            Assert.IsNotNull(visual, "el modelo de la variante");
            Assert.IsNotNull(visual.GetComponent<GiroRotores>(), "sus rotores giran");
            Assert.Greater(visual.GetComponent<GiroRotores>().CantidadRotores, 0);
            Assert.AreEqual(VidaVariante, d.Vida, 0.0001f);
            Assert.IsNotNull(d.Variante);
            Assert.IsNotNull(dron.GetComponent<MarcadorDronHud>(), "el marcador del HUD lo pone el propio dron");
        }

        [UnityTest]
        public IEnumerator Variante_QueDispara_LeBajaVidaAlJugador_YApareceEnElIndicador()
        {
            DronMundo d = Preparar(true);
            MarcadorDronHud marcador = dron.GetComponent<MarcadorDronHud>();
            float fin = Time.time + 6f;
            while (Time.time < fin && blanco.Impactos == 0)
            {
                yield return null;
            }
            Assert.Greater(blanco.Impactos, 0, "el dron pesado le pega al jugador");
            Assert.AreEqual(DanioVariante, blanco.PrimerDanio, 0.0001f, "el daño es el de la variante, no el del DR01");
            yield return null;
            yield return null;
            Assert.IsTrue(d.Amenazando, "recién disparó: está amenazando");
            Assert.IsTrue(marcador.Visible, "aparece en el indicador del HUD");
        }

        [UnityTest]
        public IEnumerator SinVariante_SigueSiendoElDR01ConElDanioDeLaConfig()
        {
            DronMundo d = Preparar(false);
            float fin = Time.time + 6f;
            while (Time.time < fin && blanco.Impactos == 0)
            {
                yield return null;
            }
            Assert.IsNull(d.Variante);
            Assert.IsNull(dron.transform.Find(DronMundo.NombreVisualVariante));
            Assert.AreEqual(config.DanioDron, blanco.PrimerDanio, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Variante_SeDerribaConSuVida()
        {
            DronMundo d = Preparar(true);
            yield return null;
            d.RecibirDanio(new InfoDanio(VidaVariante - 1f, TipoDanio.Disparo, d.transform.position, Vector3.down, null));
            Assert.IsFalse(d.EstaCayendo, "con 1 de vida todavía vuela");
            d.RecibirDanio(new InfoDanio(5f, TipoDanio.Disparo, d.transform.position, Vector3.down, null));
            Assert.IsTrue(d.EstaCayendo, "sin vida cae");
        }
    }
}
