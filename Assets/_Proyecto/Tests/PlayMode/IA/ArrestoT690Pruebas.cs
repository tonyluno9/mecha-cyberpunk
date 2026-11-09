using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Tests.Personaje;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Progresion.Prologo.Policia;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-690: el arresto tiene consecuencia. Una patrulla real te arresta quieto; sale la pantalla ARRESTADO, pierdes el 25 % del dinero,
    /// apareces en casa, el calor y la búsqueda se limpian y recuperas el control. Capturas con CAPTURAR_S1=1.
    /// </summary>
    public class ArrestoT690Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
        private static readonly Vector3 PosicionCasa = new Vector3(500f, 0f, -300f);
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private sealed class JugadorMovible : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) => Raiz.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; private set; }
            public event Action<int, MotivoDinero> Cambio;
            public CarteraDoble(int dinero) => Dinero = dinero;
            public bool PuedePagar(int cantidad) => cantidad > 0 && Dinero >= cantidad;
            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad)) return false;
                Dinero -= cantidad;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }
            public void Abonar(int cantidad, MotivoDinero motivo) { Dinero += cantidad; Cambio?.Invoke(cantidad, motivo); }
            public void CargarDeuda(int cantidad, MotivoDinero motivo) { Dinero -= cantidad; Cambio?.Invoke(-cantidad, motivo); }
        }

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            private void Poner(float valor)
            {
                int antes = Nivel;
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                if (Nivel != antes) NivelCambio?.Invoke(Nivel);
            }
            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        private sealed class PuntoDoble : IPuntoAparicion
        {
            public string Id { get; set; }
            public Pose Pose { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject barrio;
        private GameObject jugador;
        private ConfigPersecucion config;
        private GestorPersecucion gestor;
        private CalorDoble calor;
        private float escalaAnterior;
        private ConsecuenciaArresto consecuencia;

        private static void Poner(ScriptableObject so, string campo, float valor)
        {
            System.Reflection.FieldInfo f = so.GetType().GetField(campo, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f.SetValue(so, Convert.ChangeType(valor, f.FieldType));
        }

        private void Preparar(int dinero)
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            Poner(config, "segundosArresto", 1.5f);
            Poner(config, "distanciaArresto", 12f);
            Poner(config, "radioVistaDron", 1f);
                        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(2000f, 1f, 2000f);
            extras.Add(piso);
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            jugador = new GameObject("JugadorDePrueba");
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            jugador.transform.position = grafo.Nodo(0) + new Vector3(0f, -0.2f, 12f);
            consecuencia = jugador.AddComponent<ConsecuenciaArresto>();
            consecuencia.ConfigurarTiempos(0.5f, 1f, 0.2f, 0.5f);
            Registro<IJugador>.Agregar(new JugadorMovible { Raiz = jugador.transform });
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            Registro<ICartera>.Agregar(new CarteraDoble(dinero));
            Registro<IPuntoAparicion>.Agregar(new PuntoDoble { Id = "Casa_Puerta", Pose = new Pose(PosicionCasa, Quaternion.identity) });
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camObjeto.AddComponent<Camera>();
            camObjeto.transform.position = new Vector3(0f, 1.7f, 0f);
            camObjeto.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
            extras.Add(camObjeto);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            EventosPolicia.LimpiarSuscriptores();
            foreach (GameObject extra in extras)
            {
                if (extra != null) Object.DestroyImmediate(extra);
            }
            extras.Clear();
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        private IEnumerator EsperarArresto()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(60f, MotivoCalor.Mision);
            Time.timeScale = 4f;
            float fin = Time.realtimeSinceStartup + 60f;
            while (Time.realtimeSinceStartup < fin && consecuencia.Pantalla == null)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsNotNull(consecuencia.Pantalla, "tras el arresto sale la pantalla ARRESTADO");
        }

        [UnityTest]
        public IEnumerator Arresto_ConDinero_MuestraPantallaCobra25PorCientoYTeLlevaACasa()
        {
            Preparar(1000);
            Vector3 donde = jugador.transform.position;
            yield return EsperarArresto();
            PantallaAviso aviso = consecuencia.Pantalla;
            Assert.AreEqual("ARRESTADO", aviso.Titulo);
            StringAssert.Contains("250", aviso.Detalle, "la pantalla dice cuánto pierdes");
            Assert.AreEqual(0, calor.Nivel, "la búsqueda se limpia");
            while (aviso != null && aviso.Visibilidad < 1f) yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
            CapturaS1.FotoCanvas("s1t-T-690-arrestado", aviso.GetComponentInChildren<Canvas>(), 1280, 720);
            Assert.IsTrue(consecuencia.Ocupado);
            float fin = Time.realtimeSinceStartup + 30f;
            while (consecuencia.Ocupado && Time.realtimeSinceStartup < fin) yield return null;
            Assert.IsFalse(consecuencia.Ocupado, "terminó la secuencia");
            Assert.AreEqual(750, Registro<ICartera>.Actual.Dinero, "perdió el 25 % de 1000");
            Assert.Less(Vector3.Distance(jugador.transform.position, PosicionCasa), 0.1f, "apareció en casa");
            Assert.Greater(Vector3.Distance(donde, PosicionCasa), 100f);
            Assert.AreEqual(0, calor.Nivel);
            fin = Time.realtimeSinceStartup + 10f;
            while (consecuencia.Pantalla != null && Time.realtimeSinceStartup < fin) yield return null;
            Assert.IsNull(consecuencia.Pantalla, "la pantalla se quita");
        }

        [UnityTest]
        public IEnumerator Arresto_SinDinero_NoCobraNadaPeroTeLlevaACasa()
        {
            Preparar(0);
            yield return EsperarArresto();
            float fin = Time.realtimeSinceStartup + 30f;
            while (consecuencia.Ocupado && Time.realtimeSinceStartup < fin) yield return null;
            Assert.AreEqual(0, Registro<ICartera>.Actual.Dinero, "sin deuda");
            Assert.AreEqual(0, consecuencia.UltimaMulta);
            Assert.Less(Vector3.Distance(jugador.transform.position, PosicionCasa), 0.1f, "apareció en casa");
        }
    }
}
