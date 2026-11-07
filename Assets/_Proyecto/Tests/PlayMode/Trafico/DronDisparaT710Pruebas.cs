using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// T-710: el dron de la policía dispara de verdad: suena el disparo, deja un trazo finísimo y cortísimo, si falla y la bala pasa cerca del jugador suena
    /// el silbido de la bala rozando, y si acierta hace daño real. Con el jugador de pie y calor suficiente.
    /// </summary>
    public class DronDisparaT710Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";

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
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Impactos++;
                DanioTotal += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private sealed class SonidosDoble : ISonidos
        {
            public readonly List<(string id, Vector3 donde)> Sonaron = new List<(string, Vector3)>();
            public void Reproducir(string id) => Sonaron.Add((id, Vector3.zero));
            public void ReproducirEn(string id, Vector3 posicion) => Sonaron.Add((id, posicion));
            public int ReproducirBucle(string id, Transform sigue) => Sonidos.SinBucle;
            public void DetenerBucle(int bucle) { }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) => 1f;
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private GameObject piso;
        private GameObject jugador;
        private GameObject dron;
        private BlancoDoble blanco;
        private SonidosDoble sonidos;
        private Func<float> azarAnterior;

        private void Preparar(Func<float> azar)
        {
            Registros.LimpiarTodos();
            azarAnterior = DronMundo.Azar;
            DronMundo.Azar = azar;
            sonidos = new SonidosDoble();
            Registro<ISonidos>.Agregar(sonidos);
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
        public IEnumerator Dron_QueAcierta_HaceDanioReal_ConTrazoFinisimo_YSonidoDeDisparo()
        {
            Preparar(() => 0f); // siempre acierta
            DronMundo d = dron.GetComponent<DronMundo>();
            bool vioTrazo = false;
            float grosor = 1f;
            float largo = 0f;
            float fin = Time.time + 5f;
            while (Time.time < fin && !(vioTrazo && blanco.Impactos > 0))
            {
                yield return null;
                if (d.TrazoVisible)
                {
                    vioTrazo = true;
                    grosor = d.Trazo.startWidth;
                    largo = Vector3.Distance(d.Trazo.GetPosition(0), d.Trazo.GetPosition(1));
                }
            }
            Assert.Greater(blanco.Impactos, 0, "El dron le hace daño al jugador");
            Assert.Greater(blanco.DanioTotal, 0f, "El daño es real");
            Assert.IsTrue(vioTrazo, "Cada disparo deja un trazo");
            Assert.LessOrEqual(grosor, 0.01f, "El trazo es un hilo finísimo");
            Assert.LessOrEqual(largo, 6.01f, "Y cortísimo");
            Assert.IsTrue(sonidos.Sonaron.Exists(s => s.id == DronMundo.IdDisparo), "Suena el disparo");
            Assert.AreEqual(0, d.Rozaduras, "La bala que pega no silba");
            yield return Esperar(0.2f);
            Assert.IsFalse(d.TrazoVisible, "El trazo dura un instante");
        }

        [UnityTest]
        public IEnumerator Dron_QueFalla_PeroPasaCerca_HaceSonarLaBalaRozando()
        {
            Preparar(() => 1f); // nunca acierta: la bala pasa junto al jugador
            DronMundo d = dron.GetComponent<DronMundo>();
            float fin = Time.time + 6f;
            while (Time.time < fin && d.Rozaduras == 0)
            {
                yield return null;
            }
            Assert.AreEqual(0, blanco.Impactos, "Sin acertar no hay daño");
            Assert.Greater(d.Disparos, 0, "Dispara");
            Assert.Greater(d.Rozaduras, 0, "La bala pasa cerca del jugador");
            (string id, Vector3 donde) roce = sonidos.Sonaron.Find(s => s.id == DronMundo.IdRoce);
            Assert.AreEqual(DronMundo.IdRoce, roce.id, "Suena la bala rozando");
            Assert.LessOrEqual(Vector3.Distance(roce.donde, jugador.transform.position + Vector3.up * 1.2f), CalculoTiroDron.RadioRoce + 0.1f,
                "El silbido suena junto al jugador");
        }
    }
}
