using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// Drones y patrullas sobre Barrio.prefab (T-341), con un doble de ICalor y un jugador de prueba en los registros de Core: el nivel crea y retira
    /// perseguidores, entrar a la casa borra el calor, y la patrulla se queda sobre las calles durante 60 s de juego. Instancia el prefab, no lo escribe.
    /// </summary>
    public class PersecucionBarrioPruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float LadoManzana = 80f;
        private const float AlturaCalles = 0.2f;
        private const float SegundosDeJuego = 60f;
        private const float EscalaDeTiempo = 5f;
        private const float ToleranciaDeCalle = 0.05f;
        private const float MetrosMinimosRecorridos = 20f;
        private const int FramesDeEspera = 3;
        private const float DistanciaDeCaptura = 25f;

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel { get; private set; }
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;

            public void Poner(int nivel)
            {
                if (nivel == Nivel)
                {
                    return;
                }
                Nivel = nivel;
                Valor = nivel * 30f;
                ValorCambio?.Invoke(Valor);
                NivelCambio?.Invoke(Nivel);
            }

            public void Sumar(float cantidad, MotivoCalor motivo) { }
            public void Enfriar(float cantidad) { }
            public void Borrar() => Poner(0);
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject barrio;
        private GameObject jugadorObjeto;
        private CalorDoble calor;
        private GestorPersecucion gestor;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            jugadorObjeto = new GameObject("JugadorDePrueba");
            jugadorObjeto.transform.position = new Vector3(-47f, AlturaCalles, 0f);
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugadorObjeto.transform });
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);

            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            Object.DestroyImmediate(jugadorObjeto);
            Object.DestroyImmediate(barrio);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        private static IEnumerator EsperarFrames()
        {
            for (int i = 0; i < FramesDeEspera; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Nivel0_SinPerseguidores_Subir_LosCrea_YBajarA0_LosRetira()
        {
            yield return EsperarFrames();
            Assert.AreEqual(0, gestor.Drones);
            Assert.AreEqual(0, gestor.Patrullas);

            calor.Poner(1);
            yield return EsperarFrames();
            Assert.AreEqual(1, gestor.Drones);
            Assert.AreEqual(0, gestor.Patrullas);

            calor.Poner(3);
            yield return EsperarFrames();
            Assert.AreEqual(2, gestor.Drones);
            Assert.AreEqual(1, gestor.Patrullas);

            calor.Poner(0);
            yield return EsperarFrames();
            Assert.AreEqual(0, gestor.Drones);
            Assert.AreEqual(0, gestor.Patrullas);
            Assert.AreEqual(0, gestor.GetComponentsInChildren<DronMundo>().Length);
            Assert.AreEqual(0, gestor.GetComponentsInChildren<PatrullaMundo>().Length);
        }

        [UnityTest]
        public IEnumerator EntrarALaCasa_BorraElCalor_YRetiraAlosPerseguidores()
        {
            calor.Poner(3);
            yield return EsperarFrames();
            Assert.AreEqual(3, gestor.Drones + gestor.Patrullas);

            EventosLugares.PublicarEntro(IdLugar.Bar);
            yield return EsperarFrames();
            Assert.AreEqual(3, calor.Nivel, "Entrar a otro lugar no borra el calor");

            EventosLugares.PublicarEntro(IdLugar.Casa);
            yield return EsperarFrames();
            Assert.AreEqual(0, calor.Nivel);
            Assert.AreEqual(0, gestor.Drones + gestor.Patrullas);
        }

        [UnityTest]
        public IEnumerator Dron_SeAcercaAlJugador_YVuelaSobreEl()
        {
            calor.Poner(1);
            yield return null;
            DronMundo dron = gestor.GetComponentInChildren<DronMundo>();
            Assert.IsNotNull(dron);
            float antes = Vector3.Distance(dron.transform.position, jugadorObjeto.transform.position);
            Time.timeScale = EscalaDeTiempo;
            float limite = Time.time + SegundosDeJuego;
            while (Time.time < limite)
            {
                yield return null;
            }
            float despues = Vector3.Distance(dron.transform.position, jugadorObjeto.transform.position);
            Assert.Less(despues, antes);
            Assert.Greater(dron.transform.position.y - jugadorObjeto.transform.position.y, 0f, "El dron vuela sobre el jugador");
            Assert.AreEqual(0, dron.GetComponentsInChildren<Collider>().Length, "El dron no lleva colliders");
        }

        [UnityTest]
        public IEnumerator Patrulla_EnSesentaSegundos_NoSaleDeLaCalle_YSeMueve()
        {
            calor.Poner(3);
            yield return null;
            PatrullaMundo patrulla = gestor.GetComponentInChildren<PatrullaMundo>();
            Assert.IsNotNull(patrulla);
            Assert.AreEqual(0, patrulla.GetComponentsInChildren<Collider>().Length, "La patrulla no lleva colliders");

            List<Vector2> centros = barrio.transform.Cast<Transform>()
                .Where(t => t.name.StartsWith("Manzana_"))
                .Select(t => new Vector2(t.position.x, t.position.z)).ToList();
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);

            Vector3 inicio = patrulla.transform.position;
            float recorrido = 0f;
            Vector3 anterior = inicio;
            Time.timeScale = EscalaDeTiempo;
            float limite = Time.time + SegundosDeJuego;
            while (Time.time < limite)
            {
                yield return null;
                Vector3 ahora = patrulla.transform.position;
                recorrido += Vector3.Distance(anterior, ahora);
                anterior = ahora;
                Assert.LessOrEqual(calles.DistanciaALaCalle(ahora), ToleranciaDeCalle, "La patrulla salió de la calle en " + ahora);
            }
            Assert.Greater(recorrido, MetrosMinimosRecorridos, "La patrulla debe avanzar hacia el jugador");
        }

        // Genera docs/capturas/T-341-1.png (dron con su luz) y T-341-2.png (patrulla con sirena) desde una cámara de prueba.
        [UnityTest]
        public IEnumerator Capturas_DronYPatrulla()
        {
            calor.Poner(3);
            yield return null;
            yield return null;
            DronMundo dron = gestor.GetComponentInChildren<DronMundo>();
            PatrullaMundo patrulla = gestor.GetComponentInChildren<PatrullaMundo>();
            Assert.IsNotNull(dron);
            Assert.IsNotNull(patrulla);
            // Deja que se acerquen al jugador antes de fotografiarlos.
            Time.timeScale = EscalaDeTiempo;
            float limite = Time.time + SegundosDeJuego;
            Vector3 suelo = jugadorObjeto.transform.position;
            while (Time.time < limite && Vector3.Distance(patrulla.transform.position, suelo) > DistanciaDeCaptura)
            {
                yield return null;
            }
            Time.timeScale = 0f;
            GameObject camObjeto = new GameObject("CamaraCaptura");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;
            Vector3 dronPos = dron.transform.position;
            camObjeto.transform.position = suelo + Vector3.up * 1.7f;
            camObjeto.transform.LookAt(dronPos);
            yield return null;
            Capturar(cam, "T-341-1.png");
            Vector3 patPos = patrulla.transform.position;
            camObjeto.transform.position = patPos - patrulla.transform.forward * 9f + Vector3.up * 4f;
            camObjeto.transform.LookAt(patPos + Vector3.up);
            yield return null;
            Capturar(cam, "T-341-2.png");
            Object.DestroyImmediate(camObjeto);
        }

        private static void Capturar(Camera cam, string archivo)
        {
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
        }
    }
}
