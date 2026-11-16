using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1009: el guardia y el Capitán de Nova no atacan si no los provocas; si los golpeas, pelean con daño fuerte.</summary>
    public class NovaSeDefiendeT1009Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaGuardia = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Nova.asset";
        private const string RutaCapitan = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";

        private GameObject piso;
        private GameObject jugador;
        private GameObject objetoEnemigo;
        private PandilleroAPie enemigo;
        private VidaAPie vida;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            vida = jugador.GetComponent<VidaAPie>();
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(objetoEnemigo);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void Crear(string rutaDatos, Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            prefab.SetActive(false);
            objetoEnemigo = Object.Instantiate(prefab, posicion, Quaternion.LookRotation(-Vector3.forward));
            prefab.SetActive(true);
            SerializedObject so = new SerializedObject(objetoEnemigo.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(rutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            objetoEnemigo.SetActive(true);
            Physics.SyncTransforms();
            enemigo = objetoEnemigo.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        private void Golpear()
        {
            enemigo.RecibirDanio(new InfoDanio(1f, TipoDanio.Golpe, enemigo.transform.position + Vector3.up, Vector3.forward, null));
        }

        [Test]
        public void Datos_NovaSeDefiendeYEsFuerte()
        {
            DatosPandilleroAPie guardia = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaGuardia);
            DatosPandilleroAPie capitan = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaCapitan);
            Assert.IsTrue(guardia.SeDefiende);
            Assert.IsTrue(capitan.SeDefiende);
            Assert.GreaterOrEqual(guardia.VidaMaxima, 300f);
            Assert.GreaterOrEqual(capitan.VidaMaxima, 1200f);
        }

        [UnityTest]
        public IEnumerator Guardia_SinProvocarlo_NoAtaca()
        {
            yield return Esperar(0.5f);
            Crear(RutaGuardia, new Vector3(0f, 0.05f, 7f));
            float antes = vida.Actual;
            yield return Esperar(6f);
            Assert.IsFalse(enemigo.Provocado);
            Assert.AreEqual(antes, vida.Actual, 0.001f, "sin provocarlo no dispara");
        }

        [UnityTest]
        public IEnumerator Guardia_Golpeado_DisparaYHaceDanio()
        {
            yield return Esperar(0.5f);
            Crear(RutaGuardia, new Vector3(0f, 0.05f, 7f));
            yield return Esperar(0.3f);
            float antes = vida.Actual;
            Golpear();
            Assert.IsTrue(enemigo.Provocado);
            float fin = Time.time + 12f;
            while (vida.Actual >= antes && Time.time < fin) yield return null;
            Assert.Less(vida.Actual, antes, "golpeado, el guardia pelea y el jugador recibe daño");
        }

        [UnityTest]
        public IEnumerator Capitan_SinProvocarlo_NoAtaca()
        {
            yield return Esperar(0.5f);
            Crear(RutaCapitan, new Vector3(0f, 0.05f, 7f));
            enemigo.MarcarComoJefe();
            float antes = vida.Actual;
            yield return Esperar(6f);
            Assert.AreEqual(antes, vida.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator Capitan_Golpeado_SeAcercaYGolpeaFuerte()
        {
            yield return Esperar(0.5f);
            Crear(RutaCapitan, new Vector3(0f, 0.05f, 7f));
            enemigo.MarcarComoJefe();
            yield return Esperar(0.3f);
            float antes = vida.Actual;
            Golpear();
            float fin = Time.time + 15f;
            while (vida.Actual >= antes && Time.time < fin) yield return null;
            Assert.Less(vida.Actual, antes, "el Capitán golpeado pelea");
            Assert.GreaterOrEqual(antes - vida.Actual, 20f, "y pega fuerte");
        }
    }
}
