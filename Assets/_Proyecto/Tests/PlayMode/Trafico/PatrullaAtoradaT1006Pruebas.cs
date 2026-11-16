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
    /// T-1006: una patrulla que persigue al jugador con un muro de lado a lado de la calle delante se atora; debe dar reversa, recolocarse y volver a avanzar.
    /// Una patrulla volcada a la vista de la cámara se endereza.
    /// </summary>
    public class PatrullaAtoradaT1006Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Lado = 80f;
        private const float Altura = 0.2f;
        private const float Paso = 94f;
        private const float DistanciaAlMuro = 7f;
        private const float DistanciaAlJugador = 80f; // cerca del cruce siguiente: la ruta pasa por el muro
        private const float SegundosMaximos = 30f;
        private const float RapidezDeMarcha = 3f;

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
            Time.timeScale = 1f;
            foreach (GameObject extra in extras)
            {
                Object.DestroyImmediate(extra);
            }
            extras.Clear();
            Registros.LimpiarTodos();
        }

        private static GrafoCalles Grafo()
        {
            List<Vector2> centros = new List<Vector2>
            {
                new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
                new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
            };
            return GrafoCalles.DeManzanas(centros, Lado, Altura);
        }

        [UnityTest]
        public IEnumerator PatrullaContraUnMuro_DaReversa_SeRecolocaYVuelveAAvanzar()
        {
            GrafoCalles calles = Grafo();
            int nodo = 0;
            Vector3 inicio = calles.Nodo(nodo);
            Vector3 calle = (calles.Nodo(calles.Vecinos(nodo)[0]) - inicio).normalized;
            // Un muro de lado a lado de la calle, delante de la patrulla, y el jugador del otro lado.
            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.transform.position = inicio + calle * DistanciaAlMuro + Vector3.up * 3f;
            muro.transform.rotation = Quaternion.LookRotation(calle, Vector3.up);
            muro.transform.localScale = new Vector3(40f, 6f, 0.5f);
            extras.Add(muro);
            GameObject jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = inicio + calle * DistanciaAlJugador;
            extras.Add(jugador);
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            patrulla.Preparar(config.Crear(), config, calles, nodo, () => jugador.transform.position);
            Physics.SyncTransforms();
            Time.timeScale = 3f;
            float fin = Time.time + SegundosMaximos;
            bool recolocada = false;
            float avanzoDespues = 0f;
            Vector3 donde = Vector3.zero;
            while (Time.time < fin && avanzoDespues < 10f)
            {
                yield return null;
                if (!recolocada && patrulla.Recolocaciones > 0)
                {
                    recolocada = true;
                    donde = patrulla.transform.position;
                }
                if (recolocada && patrulla.Rapidez > RapidezDeMarcha)
                {
                    avanzoDespues = Vector3.Distance(patrulla.transform.position, donde);
                }
            }
            Time.timeScale = 1f;
            Debug.Log("[T-1006] reversas " + patrulla.Reversas + " recolocaciones " + patrulla.Recolocaciones + " avanzo despues " + avanzoDespues);
            Assert.GreaterOrEqual(patrulla.Reversas, 1, "Primero intenta liberarse dando reversa");
            Assert.GreaterOrEqual(patrulla.Recolocaciones, 1, "Si sigue atorada se recoloca en la calle");
            Assert.Greater(avanzoDespues, 10f, "Tras liberarse vuelve a avanzar de verdad (m)");
        }

        [UnityTest]
        public IEnumerator PatrullaVolcadaALaVista_SeEndereza()
        {
            GrafoCalles calles = Grafo();
            Vector3 inicio = calles.Nodo(0);
            GameObject jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = inicio + Vector3.right * 40f;
            extras.Add(jugador);
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            patrulla.Preparar(config.Crear(), config, calles, 0, () => jugador.transform.position);
            // Una cámara principal que la ve.
            GameObject camObjeto = new GameObject("CamaraPrincipal");
            camObjeto.tag = "MainCamera";
            camObjeto.AddComponent<Camera>();
            camObjeto.transform.position = inicio + new Vector3(0f, 6f, -12f);
            camObjeto.transform.LookAt(inicio);
            extras.Add(camObjeto);
            yield return null;
            Rigidbody cuerpo = patrulla.GetComponent<Rigidbody>();
            cuerpo.rotation = Quaternion.Euler(180f, 0f, 0f) * cuerpo.rotation;
            cuerpo.position += Vector3.up * 1f;
            float fin = Time.time + config.SegundosVolcada + 6f;
            while (Time.time < fin && patrulla != null && patrulla.Enderezadas == 0)
            {
                yield return null;
            }
            Assert.IsTrue(patrulla != null, "A la vista no desaparece");
            Assert.GreaterOrEqual(patrulla.Enderezadas, 1, "Se enderezó");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Vector3.Dot(patrulla.transform.up, Vector3.up), 0.7f, "Quedó sobre sus ruedas");
        }
    }
}
