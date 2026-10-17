using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Pruebas_Prologo (T-127): lo que el barrio greybox registra al arrancar y las distancias que importan al prólogo.</summary>
    public class EscenaPrologoPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const float ToleranciaPose = 0.1f;
        private const float RadioEmboscada = 12f;
        private const float SegundosDeJuego = 3f;
        private const int MaxFramesEscena = 600;

        private string carpeta;
        private readonly List<string> errores = new List<string>();
        private HashSet<GameObject> raicesIniciales;


        [SetUp]
        public void Preparar()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            Registros.LimpiarTodos();
            carpeta = Path.Combine(Application.temporaryCachePath, "escena_prologo_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            Application.logMessageReceived += AlRegistrar;
        }

        [UnityTearDown]
        public IEnumerator Limpiar()
        {
            Application.logMessageReceived -= AlRegistrar;
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
        }

        private void AlRegistrar(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private static IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            // El director del prólogo (T-132) arrancaría la pelea del campeonato solo; esta prueba maneja la escena a mano.
            MechaCyberpunk.Progresion.Prologo.DirectorPrologo prologo = Object.FindFirstObjectByType<MechaCyberpunk.Progresion.Prologo.DirectorPrologo>();
            if (prologo != null)
            {
                Object.Destroy(prologo);
            }
            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
            }
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private static IPuntoHistoria PuntoHistoria(string id)
        {
            foreach (IPuntoHistoria punto in Registro<IPuntoHistoria>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static float DistanciaASegmento(Vector3 punto, Vector3 inicio, Vector3 fin)
        {
            Vector2 p = new Vector2(punto.x, punto.z);
            Vector2 a = new Vector2(inicio.x, inicio.z);
            Vector2 b = new Vector2(fin.x, fin.z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        [UnityTest]
        public IEnumerator Arranca_RegistraLoQueElPrologoNecesita()
        {
            yield return CargarEscena();

            IJugador jugador = Registro<IJugador>.Actual;
            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "un jugador");
            Assert.IsNotNull(jugador);
            IPuntoAparicion puerta = Aparicion("Casa_Puerta");
            Assert.IsNotNull(puerta, "aparición Casa_Puerta");
            Assert.LessOrEqual(DistanciaHorizontal(jugador.Raiz.position, puerta.Pose.position), ToleranciaPose, "el jugador empieza en Casa_Puerta");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);

            Assert.AreEqual(2, Registro<IMontable>.Cantidad, "mecha estacionado y carro");
            int mechas = 0;
            int carros = 0;
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                mechas += montable.Modo == ModoJugador.Mecha ? 1 : 0;
                carros += montable.Modo == ModoJugador.Carro ? 1 : 0;
            }
            Assert.AreEqual(1, carros, "un carro montable");
            Assert.AreEqual(1, mechas, "un mecha montable");

            Assert.AreEqual(1, Registro<IArenaPelea>.Cantidad, "una arena");
            Assert.AreEqual(1, Registro<ITallerMecha>.Cantidad, "un taller");
            Assert.AreEqual(1, Registro<IPuntoLlamadoMecha>.Cantidad, "un punto de llamado");
            Assert.AreEqual(1, Registro<IRelojMundo>.Cantidad, "un reloj");
            Assert.IsTrue(Registro<IRelojMundo>.Actual.EsDeNoche, "es de noche");
            Assert.AreEqual(1, Registro<IRelojMundo>.Actual.Dia, "día 1");

            Assert.IsNotNull(Aparicion("Casa_Cama"), "aparición Casa_Cama");
            Assert.IsNotNull(Aparicion("Clandestino_Salida"), "aparición Clandestino_Salida");
            Assert.IsNotNull(PuntoHistoria("Prologo_Carro"), "punto Prologo_Carro");
            Assert.IsNotNull(PuntoHistoria("Prologo_Esquina_1"), "punto Prologo_Esquina_1");

            bool casa = false;
            bool clandestino = false;
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                casa |= lugar.Id == IdLugar.Casa;
                clandestino |= lugar.Id == IdLugar.Clandestino;
            }
            Assert.IsTrue(casa, "lugar Casa");
            Assert.IsTrue(clandestino, "lugar Clandestino");
        }

        [UnityTest]
        public IEnumerator Arranca_ElMechaEstaEnElEstacionamientoDelTaller()
        {
            yield return CargarEscena();

            Vector3 estacionamiento = Registro<ITallerMecha>.Actual.Estacionamiento.position;
            float mejor = float.MaxValue;
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable.Modo != ModoJugador.Mecha)
                {
                    continue;
                }
                mejor = Mathf.Min(mejor, DistanciaHorizontal(((Component)montable).transform.position, estacionamiento));
            }
            Assert.Less(mejor, ToleranciaPose, "el mecha a menos de 0.1 m del estacionamiento (m)");
        }

        [UnityTest]
        public IEnumerator Arranca_LaEmboscadaQuedaEnElRadioDelLlamadoYLejosDeLaRutaCasaEsquina()
        {
            yield return CargarEscena();

            GameObject emboscada = GameObject.Find("Emboscada");
            Assert.IsNotNull(emboscada, "la emboscada está en la escena");
            Vector3 entrada = emboscada.transform.position;
            IPuntoLlamadoMecha llamado = Registro<IPuntoLlamadoMecha>.Actual;
            Assert.LessOrEqual(DistanciaHorizontal(entrada, llamado.Llegada.position), llamado.Radio, "la entrada del callejón está dentro del radio del llamado (m)");

            Vector3 inicio = Aparicion("Casa_Puerta").Pose.position;
            Vector3 esquina = PuntoHistoria("Prologo_Esquina_1").Pose.position;
            float distancia = DistanciaASegmento(entrada, inicio, esquina);
            Assert.Greater(distancia, RadioEmboscada, "la emboscada no se dispara caminando de la casa a la esquina (m)");
        }

        [UnityTest]
        public IEnumerator Arranca_UnSoloOyenteYUnaCamaraYSinErroresEn3Segundos()
        {
            yield return CargarEscena();

            float fin = Time.time + SegundosDeJuego;
            while (Time.time < fin)
            {
                yield return null;
            }

            int oyentes = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                {
                    oyentes++;
                }
            }
            int camaras = 0;
            foreach (Camera camara in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camara.enabled && camara.gameObject.activeInHierarchy)
                {
                    camaras++;
                }
            }
            Assert.AreEqual(1, oyentes, "un solo AudioListener activo");
            Assert.AreEqual(1, camaras, "una sola cámara activa");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
