using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Base de las pruebas jugables a pie: prefab real, piso creado en la prueba, entrada simulada y datos propios.</summary>
    public abstract class BaseJugadorAPie : InputTestFixture
    {
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        protected Keyboard teclado;
        protected Mouse raton;
        protected GameObject piso;
        protected GameObject jugador;
        protected ControladorAPie controlador;
        protected JugadorAPie jugadorAPie;
        protected DatosPersonaje datos;
        private readonly System.Collections.Generic.List<GameObject> extras = new System.Collections.Generic.List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            jugador = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            controlador = jugador.GetComponent<ControladorAPie>();
            jugadorAPie = jugador.GetComponent<JugadorAPie>();

            // Datos propios de la prueba para no tocar el asset del proyecto.
            datos = ScriptableObject.CreateInstance<DatosPersonaje>();
            SerializedObject so = new SerializedObject(controlador);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public override void TearDown()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.Destroy(extra);
                }
            }
            extras.Clear();
            Object.Destroy(jugador);
            Object.Destroy(piso);
            Object.Destroy(datos);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        protected GameObject Caja(Vector3 centro, Vector3 tamano)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            extras.Add(caja);
            return caja;
        }

        // Rampa que sube hacia +Z desde (0, 0, zInicio); el borde bajo de la cara de arriba queda a ras del piso.
        protected GameObject Rampa(float zInicio, float grados, float largo)
        {
            const float grosor = 0.5f;
            float radianes = grados * Mathf.Deg2Rad;
            Vector3 direccion = new Vector3(0f, Mathf.Sin(radianes), Mathf.Cos(radianes));
            Vector3 normal = new Vector3(0f, Mathf.Cos(radianes), -Mathf.Sin(radianes));
            Vector3 centro = new Vector3(0f, 0f, zInicio) + direccion * (largo / 2f) - normal * (grosor / 2f);
            GameObject rampa = Caja(centro, new Vector3(10f, grosor, largo));
            rampa.transform.rotation = Quaternion.Euler(-grados, 0f, 0f);
            return rampa;
        }

        protected static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Los primeros frames de PlayMode son lentos (deltaTime enorme): se espera a que los frames se estabilicen.
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
    }
}
