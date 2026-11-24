using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// Base de las pruebas VR: prefab real del jugador sobre un piso de prueba, modo VR simulado (sin visor) y dispositivos
    /// simulados del XR Interaction Toolkit (visor y dos controles) que se mueven y pulsan desde la prueba.
    /// Uso: en un [UnityTest], <c>yield return ArmarVR();</c> y luego <see cref="PonerCabeza"/>, <see cref="PonerMano"/>,
    /// <see cref="Mantener"/>/<see cref="Soltar"/> y <see cref="Cuadros"/>.
    /// </summary>
    public abstract class BaseJugadorVR : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        protected XRSimulatedHMD visor;
        protected XRSimulatedController controlIzq;
        protected XRSimulatedController controlDer;
        protected GameObject piso;
        protected GameObject jugador;
        protected JugadorAPie jugadorAPie;
        protected ControladorAPie controlador;
        protected AdaptadorVR adaptador;
        protected readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Pausa.Fijar(false);
            Time.timeScale = 1f;
            SistemaVR.NoIntentarXR = true;
            visor = InputSystem.AddDevice<XRSimulatedHMD>();
            controlIzq = InputSystem.AddDevice<XRSimulatedController>();
            controlDer = InputSystem.AddDevice<XRSimulatedController>();
            InputSystem.SetDeviceUsage(controlIzq, UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(controlDer, UnityEngine.InputSystem.CommonUsages.RightHand);
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            ModoVR.Fijar(true, true);
            EntradaVR.Recrear();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPruebaVR";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
        }

        public override void TearDown()
        {
            foreach (GameObject g in extras)
            {
                if (g != null)
                {
                    Object.Destroy(g);
                }
            }
            extras.Clear();
            if (jugador != null)
            {
                Object.Destroy(jugador);
            }
            Object.Destroy(piso);
            EntradaVR.Apagar();
            ModoVR.Fijar(false);
            SistemaVR.NoIntentarXR = false;
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        /// <summary>Crea al jugador (prefab real) en <paramref name="posicion"/>, le pone el rig VR y espera a que esté armado.</summary>
        protected IEnumerator ArmarVR(Vector3 posicion = default)
        {
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            PonerMano(false, new Vector3(-0.2f, 1.2f, 0.3f), Quaternion.identity);
            PonerMano(true, new Vector3(0.2f, 1.2f, 0.3f), Quaternion.identity);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            jugador = Object.Instantiate(prefab, posicion + new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugadorAPie = jugador.GetComponent<JugadorAPie>();
            controlador = jugador.GetComponent<ControladorAPie>();
            jugador.GetComponent<MechaCyberpunk.Personaje.Vida.EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            adaptador = jugador.GetComponent<AdaptadorVR>();
            if (adaptador == null)
            {
                adaptador = jugador.AddComponent<AdaptadorVR>();
            }
            for (int i = 0; i < 120 && !adaptador.Armado; i++)
            {
                yield return null;
            }
            Assert.IsTrue(adaptador.Armado, "El rig VR no se armó.");
            // Unos cuadros para que el jugador caiga al piso y la altura se calibre.
            yield return Segundos(2.2f);
        }

        /// <summary>Pose del visor en el espacio del cuarto (rig).</summary>
        protected void PonerCabeza(Vector3 posicion, Quaternion rotacion)
        {
            Set(visor.centerEyePosition, posicion);
            Set(visor.centerEyeRotation, rotacion);
            Set(visor.devicePosition, posicion);
            Set(visor.deviceRotation, rotacion);
        }

        /// <summary>Pose de un control en el espacio del cuarto (rig).</summary>
        protected void PonerMano(bool derecha, Vector3 posicion, Quaternion rotacion)
        {
            XRSimulatedController c = derecha ? controlDer : controlIzq;
            Set(c.devicePosition, posicion);
            Set(c.deviceRotation, rotacion);
        }

        /// <summary>Mueve un control en línea recta de <paramref name="desde"/> a <paramref name="hasta"/> (espacio del cuarto) en <paramref name="segundos"/>.</summary>
        protected IEnumerator MoverMano(bool derecha, Vector3 desde, Vector3 hasta, float segundos, Quaternion rotacion)
        {
            float t = 0f;
            while (t < segundos)
            {
                t += Time.deltaTime;
                PonerMano(derecha, Vector3.Lerp(desde, hasta, Mathf.Clamp01(t / segundos)), rotacion);
                yield return null;
            }
            PonerMano(derecha, hasta, rotacion);
            yield return null;
        }

        protected void Mantener(ButtonControl boton) => Press(boton);

        protected void Soltar(ButtonControl boton) => Release(boton);

        /// <summary>Pulsa y suelta un botón (dos cuadros).</summary>
        protected IEnumerator Pulsar(ButtonControl boton)
        {
            Press(boton);
            yield return null;
            yield return null;
            Release(boton);
            yield return null;
        }

        protected void Stick(bool derecha, Vector2 valor) => Set((derecha ? controlDer : controlIzq).primary2DAxis, valor);

        protected static IEnumerator Cuadros(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        protected static IEnumerator Segundos(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        protected GameObject Caja(Vector3 centro, Vector3 tamano)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            extras.Add(caja);
            return caja;
        }
    }
}
