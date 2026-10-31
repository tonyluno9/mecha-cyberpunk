using System.Collections;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-446 con el prefab real, sin jugador: un pandillero (a puños y armado) que espera en su zona se aleja de su hogar patrullando,
    /// no se pasa del radio de patrulla y sigue en estado Esperando.
    /// </summary>
    public class PandilleroPatrullaPruebas
    {
        private const string RutaPunos = "Assets/_Proyecto/IA/Prefabs/PandilleroAPie.prefab";
        private const string RutaArmado = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const float SegundosPatrullando = 12f;
        private const float MargenRadio = 2.5f;
        private const float DistanciaMinimaEsperada = 1.5f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const int CuadrosPorSegundo = 60;

        private GameObject piso;
        private GameObject pandillero;

        [SetUp]
        public void Preparar()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
        }

        [TearDown]
        public void Limpiar()
        {
            if (pandillero != null)
            {
                Object.DestroyImmediate(pandillero);
            }
            Time.captureFramerate = 0;
            Object.DestroyImmediate(piso);
        }

        private static IEnumerator Calentar()
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

        private IEnumerator Patrullar(string ruta)
        {
            // En modo batch los cuadros duran décimas de milisegundo y el CharacterController ignora pasos menores a su minMoveDistance: se fija 60 cuadros por segundo.
            Time.captureFramerate = CuadrosPorSegundo;
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta " + ruta);
            pandillero = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            PandilleroAPie p = pandillero.GetComponentInChildren<PandilleroAPie>();
            Assert.IsNotNull(p);
            p.Configurar(new Vector3(0f, 0.05f, 0f), null);
            float radio = p.Datos.RadioPatrulla;
            Assert.Greater(radio, 0f, "Los pandilleros traen patrulla por defecto");
            float maxima = 0f;
            float fin = Time.time + SegundosPatrullando;
            while (Time.time < fin)
            {
                yield return null;
                Vector3 desde = p.transform.position - p.Hogar;
                desde.y = 0f;
                maxima = Mathf.Max(maxima, desde.magnitude);
                Assert.AreEqual(EstadoPandillero.Esperando, p.Estado, "Patrullar no es pelear");
            }
            Debug.Log("[Prueba] T-446 " + ruta + ": se alejó " + maxima + " m del hogar (radio " + radio + ")");
            Assert.Greater(maxima, DistanciaMinimaEsperada, "Se mueve: patrulla");
            Assert.LessOrEqual(maxima, radio + MargenRadio, "No se sale de su zona");
        }

        [UnityTest]
        public IEnumerator PandilleroAPunos_EsperandoEnSuZona_Patrulla()
        {
            yield return Patrullar(RutaPunos);
        }

        [UnityTest]
        public IEnumerator PandilleroArmado_EsperandoEnSuZona_Patrulla()
        {
            yield return Patrullar(RutaArmado);
        }
    }
}
