using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Trafico.Drones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1494: un guardia de Nova a 60 m al que le das un balazo se acerca y ataca; el perro robot te alcanza y te baja vida; un dron que va hacia un
    /// muro no lo cruza. Pone en memoria los datos que aplica AplicarIaNovaT1494 (mismos valores).
    /// </summary>
    public class IaNovaT1494Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaGuardia = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_Armado.prefab";
        private const string RutaPerro = "Assets/_Proyecto/IA/Prefabs/PerroNova_E17_PerroRobotNova_a.prefab";
        private const string DatosNova = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Nova.asset";
        private const string DatosPerro = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_PerroRobotE17.asset";

        private GameObject piso;
        private GameObject jugador;
        private VidaAPie vidaJugador;
        private readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            RespuestaNova.Apagada = true;
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            PonerDatos(DatosNova, "reaccionaADisparos", true, null);
            PonerDatos(DatosPerro, "perroRobot", true, 20f);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            vidaJugador = jugador.GetComponent<VidaAPie>();
        }

        public override void TearDown()
        {
            foreach (GameObject e in extras)
            {
                Object.DestroyImmediate(e);
            }
            extras.Clear();
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            RespuestaNova.Apagada = false;
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private static void PonerDatos(string ruta, string campo, bool valor, float? radio)
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty(campo).boolValue = valor;
            if (radio.HasValue)
            {
                so.FindProperty("radioDeteccion").floatValue = radio.Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private PandilleroAPie Crear(string ruta, Vector3 posicion)
        {
            GameObject g = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), posicion, Quaternion.LookRotation(-Vector3.forward));
            extras.Add(g);
            Physics.SyncTransforms();
            return g.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Guardia_BalazoDesde60m_SeAcercaYAtaca()
        {
            yield return Esperar(0.5f);
            PandilleroAPie guardia = Crear(RutaGuardia, new Vector3(0f, 0.05f, 60f));
            yield return Esperar(0.5f);
            Assert.IsNotNull(guardia.Percepcion, "el guardia de Nova reacciona a disparos");
            float distanciaInicial = Vector3.Distance(guardia.transform.position, jugador.transform.position);
            float vidaAntes = vidaJugador.Actual;
            guardia.RecibirDanio(new InfoDanio(1f, TipoDanio.Disparo, guardia.transform.position + Vector3.up, Vector3.forward, null));
            Assert.IsTrue(guardia.Percepcion.Activa, "el impacto lo alerta");
            yield return Esperar(4f);
            Assert.Less(Vector3.Distance(guardia.transform.position, jugador.transform.position), distanciaInicial - 5f, "va hacia donde vino el disparo");
            float fin = Time.time + 40f;
            while (vidaJugador.Actual >= vidaAntes && Time.time < fin)
            {
                yield return null;
            }
            Assert.Less(vidaJugador.Actual, vidaAntes, "te ataca con su arma");
        }

        [UnityTest]
        public IEnumerator Guardia_SinDisparo_NoReaccionaAlejos()
        {
            yield return Esperar(0.5f);
            PandilleroAPie guardia = Crear(RutaGuardia, new Vector3(0f, 0.05f, 60f));
            float vidaAntes = vidaJugador.Actual;
            yield return Esperar(3f);
            Assert.IsFalse(guardia.Percepcion.Activa);
            Assert.AreEqual(vidaAntes, vidaJugador.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator Perro_Detecta_Alcanza_YMuerde()
        {
            yield return Esperar(0.5f);
            PandilleroAPie perro = Crear(RutaPerro, new Vector3(0f, 0.05f, 15f));
            yield return Esperar(0.3f);
            float vidaAntes = vidaJugador.Actual;
            float minimaDistancia = float.MaxValue;
            float fin = Time.time + 15f;
            while (vidaJugador.Actual >= vidaAntes && Time.time < fin)
            {
                minimaDistancia = Mathf.Min(minimaDistancia, Vector3.Distance(perro.transform.position, jugador.transform.position));
                yield return null;
            }
            Assert.Less(minimaDistancia, 3f, "el perro corre hacia ti");
            Assert.Less(vidaJugador.Actual, vidaAntes, "y te muerde");
        }

        [UnityTest]
        public IEnumerator Dron_HaciaUnMuro_NoLoCruzaPorDentro()
        {
            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extras.Add(muro);
            muro.transform.position = new Vector3(10f, 15f, 0f);
            muro.transform.localScale = new Vector3(2f, 30f, 120f); // x de 9 a 11, y de 0 a 30
            GameObject raiz = new GameObject("RaizDron");
            extras.Add(raiz);
            GameObject dron = new GameObject("Dron");
            dron.SetActive(false);
            dron.transform.SetParent(raiz.transform, false);
            RutaAerea ruta = dron.AddComponent<RutaAerea>();
            SerializedObject so = new SerializedObject(ruta);
            Vector2[] puntos = { new Vector2(-20f, -20f), new Vector2(-20f, 20f), new Vector2(30f, 20f), new Vector2(30f, -20f) };
            SerializedProperty lista = so.FindProperty("puntos");
            lista.arraySize = puntos.Length;
            for (int i = 0; i < puntos.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).vector2Value = puntos[i];
            }
            so.FindProperty("altura").floatValue = 6f;
            so.FindProperty("velocidad").floatValue = 12f;
            so.FindProperty("amplitudOscilacion").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            dron.SetActive(true);
            Physics.SyncTransforms();

            bool paso = false;
            float maxDesvio = 0f;
            float fin = Time.time + 40f;
            while (Time.time < fin && !paso)
            {
                yield return null;
                Vector3 p = dron.transform.position;
                maxDesvio = Mathf.Max(maxDesvio, ruta.Desvio);
                // Dentro del grosor real del muro (x de 9 a 11); en el borde la esfera de 0.8 m solo lo roza.
                if (p.x > 9f && p.x < 11f)
                {
                    Assert.Greater(p.y, 30f, "dentro del grosor del muro solo puede ir por encima de él");
                }
                paso = p.x > 13f;
            }
            Assert.Greater(maxDesvio, 5f, "subió para rodear el muro");
            Assert.IsTrue(paso, "y terminó pasándolo por arriba");
        }
    }
}
