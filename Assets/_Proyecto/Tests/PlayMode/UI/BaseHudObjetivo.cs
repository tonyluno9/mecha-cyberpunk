using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// Base de las pruebas jugables del objetivo y la interacción del HUD: prefabs reales del jugador, el HUD y los
    /// servicios de progresión, locales de prueba y entrada simulada.
    /// </summary>
    public abstract class BaseHudObjetivo : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaHud = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaLocal = "Assets/_Proyecto/Personaje/Pruebas/LocalDePrueba.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private static float SensibilidadRaton => UnityEditor.AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>("Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset").SensibilidadRaton;

        protected class JugadorFalso : IJugador
        {
            public ModoJugador ModoActual = ModoJugador.APie;
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoActual;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio;
            public void Colocar(Pose pose) { }

            public void CambiarModo(ModoJugador modo)
            {
                ModoActual = modo;
                ModoCambio?.Invoke(modo);
            }
        }

        protected Keyboard teclado;
        protected Mouse raton;
        protected ConfigHudAPie config;
        protected GameObject jugador;
        protected HudAPie hud;
        protected ServicioObjetivos servicio;
        private GameObject piso;
        private GameObject objetoHud;
        private GameObject objetoServicio;
        private GameObject raizFalsa;
        private readonly List<GameObject> locales = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            config = AssetDatabase.LoadAssetAtPath<ConfigHudAPie>(RutaConfig);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            foreach (GameObject local in locales)
            {
                UnityEngine.Object.Destroy(local);
            }
            locales.Clear();
            UnityEngine.Object.Destroy(jugador);
            UnityEngine.Object.Destroy(objetoHud);
            UnityEngine.Object.Destroy(objetoServicio);
            UnityEngine.Object.Destroy(raizFalsa);
            UnityEngine.Object.Destroy(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        /// <summary>
        /// Instancia el jugador con el HUD y los servicios que ya trae anidados (desde T-059). Con <paramref name="conHud"/> en falso
        /// quita el HUD anidado, para pruebas que lo crean después con <see cref="CrearHud"/>.
        /// </summary>
        protected void CrearJugador(bool conHud = true)
        {
            jugador = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            if (!conHud)
            {
                foreach (HudAPie propio in jugador.GetComponentsInChildren<HudAPie>(true))
                {
                    UnityEngine.Object.DestroyImmediate(propio.gameObject);
                }
            }
        }

        /// <summary>Toma el HUD anidado del jugador si hay uno; si no, instancia el prefab. Siempre queda un solo HudAPie.</summary>
        protected void CrearHud()
        {
            hud = jugador != null ? jugador.GetComponentInChildren<HudAPie>(true) : null;
            if (hud == null)
            {
                objetoHud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaHud));
                hud = objetoHud.GetComponent<HudAPie>();
            }
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<HudAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                "Debe haber exactamente un HudAPie en la escena");
        }

        /// <summary>Toma el servicio de objetivos anidado en el jugador si hay uno; si no, instancia ServiciosProgresion.</summary>
        protected void CrearServicio()
        {
            servicio = jugador != null ? jugador.GetComponentInChildren<ServicioObjetivos>(true) : null;
            if (servicio == null)
            {
                objetoServicio = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios));
                servicio = objetoServicio.GetComponent<ServicioObjetivos>();
            }
        }

        protected JugadorFalso RegistrarFalso(ModoJugador modo)
        {
            raizFalsa = new GameObject("JugadorFalso");
            JugadorFalso falso = new JugadorFalso { Raiz = raizFalsa.transform, ModoActual = modo };
            Registro<IJugador>.Agregar(falso);
            return falso;
        }

        /// <summary>Instancia el local de prueba con su puerta en esa posición; la calle queda hacia -Z.</summary>
        protected GameObject CrearLocal(IdLugar id, Vector3 posicionPuerta, HorarioLugar horario, bool cerradoEnDemo, bool abiertoPorHorario)
        {
            GameObject local = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaLocal), posicionPuerta, Quaternion.identity);
            local.name = id.ToString();
            LugarDePrueba lugar = local.GetComponent<LugarDePrueba>();
            lugar.Configurar(id, id.ToString(), horario, cerradoEnDemo);
            lugar.PonerAbiertoPorHorario(abiertoPorHorario);
            locales.Add(local);
            return local;
        }

        protected static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        protected static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        protected IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        /// <summary>Gira con el ratón simulado; positivo es a la derecha.</summary>
        protected IEnumerator Girar(float grados)
        {
            Set(raton.delta, new Vector2(grados / SensibilidadRaton, 0f));
            yield return null;
            yield return null;
        }
    }
}
