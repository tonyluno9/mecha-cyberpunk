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
    /// T-581: una patrulla que ve al jugador de cerca ya no corta por una pared. Sin pared (control) sí deja la calle y va directo;
    /// con una pared en medio se queda sobre la calle todo el tiempo.
    /// </summary>
    public class PatrullaParedT581Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Lado = 80f;
        private const float Altura = 0.2f;
        private const float Paso = 94f;
        private const float DistanciaDelJugador = 20f;
        private const float SegundosDeJuego = 12f;
        private const float ToleranciaDeCalle = 2f; // T-666: va por su carril, a 1.6 m del eje de la calle (la calle mide 14 m)

        private readonly List<GameObject> extras = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(600f, 1f, 600f);
            extras.Add(piso);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject extra in extras)
            {
                Object.DestroyImmediate(extra);
            }
            extras.Clear();
            Registros.LimpiarTodos();
        }

        // Devuelve la máxima distancia a la calle que alcanzó la patrulla.
        private IEnumerator Correr(bool conPared, System.Action<float> resultado)
        {
            List<Vector2> centros = new List<Vector2>
            {
                new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
                new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
            };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, Lado, Altura);
            int nodo = 0;
            Vector3 inicio = calles.Nodo(nodo);
            Vector3 calle = (calles.Nodo(calles.Vecinos(nodo)[0]) - inicio).normalized;
            Vector3 lateral = new Vector3(-calle.z, 0f, calle.x);
            Vector3 jugador = inicio + lateral * DistanciaDelJugador;
            if (conPared)
            {
                GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pared.transform.position = inicio + lateral * (DistanciaDelJugador / 2f) + Vector3.up * 3f;
                pared.transform.rotation = Quaternion.LookRotation(calle, Vector3.up);
                pared.transform.localScale = new Vector3(0.5f, 6f, 60f);
                extras.Add(pared);
            }
            GameObject cuerpoJugador = new GameObject("JugadorDePrueba");
            cuerpoJugador.transform.position = jugador;
            extras.Add(cuerpoJugador);
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            patrulla.Preparar(config.Crear(), config, calles, nodo, () => cuerpoJugador.transform.position);
            Physics.SyncTransforms();
            float maxima = 0f;
            float limite = Time.time + SegundosDeJuego;
            while (Time.time < limite)
            {
                yield return null;
                maxima = Mathf.Max(maxima, calles.DistanciaALaCalle(patrulla.transform.position));
            }
            Debug.Log("[T-581] conPared " + conPared + " directo " + patrulla.VaDirecto + " patrulla " + patrulla.transform.position + " jugador " + jugador + " inicio " + inicio + " calle " + calle + " enabled " + patrulla.enabled);
            resultado(maxima);
        }

        [UnityTest]
        public IEnumerator SinPared_LaPatrullaDeCercaVaDirectoYDejaLaCalle()
        {
            float maxima = 0f;
            yield return Correr(false, m => maxima = m);
            Assert.Greater(maxima, 5f, "Control: sin pared la patrulla va directo al jugador (se alejó de la calle " + maxima.ToString("F2") + " m)");
        }

        [UnityTest]
        public IEnumerator ConPared_LaPatrullaNoCortaPorLaPared()
        {
            float maxima = 0f;
            yield return Correr(true, m => maxima = m);
            Assert.LessOrEqual(maxima, ToleranciaDeCalle, "Con una pared en medio la patrulla se quedó en la calle (se alejó " + maxima.ToString("F2") + " m)");
        }
    }
}
