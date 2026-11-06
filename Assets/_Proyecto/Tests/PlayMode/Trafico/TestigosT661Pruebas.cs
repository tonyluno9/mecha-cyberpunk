using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Trafico.Testigos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-661, T-662 y T-663: la policía solo viene si te lo ganaste y alguien te vio. Sin testigo no hay calor; una cámara o una patrulla con línea de
    /// vista lo suman al instante; un civil lo suma si termina de llamar (si lo matan o huye, no); con la policía ya encima suma sin preguntar.
    /// </summary>
    public class TestigosT661Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float SegundosLlamada = 0.8f;
        private static readonly Vector3[] RutaCivil = { new Vector3(0f, 0f, 0f), new Vector3(400f, 0f, 0f) };

        public sealed class CalorReal : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;

            private void Poner(float valor)
            {
                int antes = Nivel;
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                if (Nivel != antes)
                {
                    NivelCambio?.Invoke(Nivel);
                }
            }

            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        public sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private ConfigPersecucion config;
        private ConfigPeatones configPeatones;
        private CalorReal real;
        private GameObject jugador;
        private TestigosDelito testigos;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            Poner(config, "segundosLlamadaCivil", SegundosLlamada);
            configPeatones = ScriptableObject.CreateInstance<ConfigPeatones>();
            Poner(configPeatones, "velocidad", 0.05f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            extras.Add(piso);
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = new Vector3(100f, 0f, 100f);
            extras.Add(jugador);
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugador.transform });
            real = new CalorReal();
            Registro<ICalor>.Agregar(real);
            GameObject objeto = new GameObject("Testigos");
            objeto.SetActive(false);
            testigos = objeto.AddComponent<TestigosDelito>();
            testigos.Configurar(config);
            objeto.SetActive(true);
            extras.Add(objeto);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(configPeatones);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
        }

        private static void Poner(ScriptableObject so, string campo, float valor)
        {
            so.GetType().GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(so, valor);
        }

        private void Delito(float cantidad = 20f) => Registro<ICalor>.Actual.Sumar(cantidad, MotivoCalor.Pelea);

        private CamaraSeguridad PonerCamara(Vector3 posicion, Vector3 haciaDonde)
        {
            GameObject objeto = new GameObject("CamaraPrueba");
            objeto.transform.position = posicion;
            Vector3 mira = haciaDonde - posicion;
            objeto.transform.rotation = Quaternion.LookRotation(mira, Vector3.up);
            CamaraSeguridad camara = objeto.AddComponent<CamaraSeguridad>();
            camara.Configurar(config.AlcanceCamara, config.SemiAnguloCamara);
            extras.Add(objeto);
            return camara;
        }

        private PeatonMundo PonerCivil(Vector3 posicion)
        {
            GameObject objeto = new GameObject("Civil");
            objeto.SetActive(false);
            objeto.transform.position = posicion;
            PeatonMundo civil = objeto.AddComponent<PeatonMundo>();
            Vector3[] ruta = { posicion, posicion + new Vector3(400f, 0f, 0f) };
            civil.Preparar(ruta, configPeatones, new System.Random(1), () => jugador.transform.position, 0f);
            objeto.SetActive(true);
            extras.Add(objeto);
            return civil;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DelitoSinTestigos_NoHayCalor()
        {
            yield return null;
            Delito();
            yield return Esperar(0.3f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Nadie lo vio: no hay calor");
            Assert.AreEqual(1, testigos.DelitosSinTestigos);
            Assert.AreEqual(0, real.Nivel);
        }

        [UnityTest]
        public IEnumerator Camara_QueVeAlJugador_SumaCalorAlInstante()
        {
            yield return null;
            PonerCamara(jugador.transform.position + new Vector3(12f, 5f, 0f), jugador.transform.position);
            Delito();
            Assert.AreEqual(20f, real.Valor, 0.001f, "Una cámara que lo ve: el calor sube de una vez");
        }

        [UnityTest]
        public IEnumerator Camara_DeEspaldasOLejosOTapada_NoCuentaComoTestigo()
        {
            yield return null;
            Vector3 p = jugador.transform.position;
            PonerCamara(p + new Vector3(12f, 5f, 0f), p + new Vector3(60f, 5f, 0f)); // mira hacia otro lado
            PonerCamara(p + new Vector3(200f, 5f, 0f), p);                               // demasiado lejos
            CamaraSeguridad tapada = PonerCamara(p + new Vector3(0f, 5f, 15f), p);
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = p + new Vector3(0f, 3f, 8f);
            pared.transform.localScale = new Vector3(10f, 8f, 0.5f);
            extras.Add(pared);
            Physics.SyncTransforms();
            Assert.IsFalse(tapada.Ve(p), "Con una pared en medio no ve");
            Delito();
            yield return Esperar(0.3f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Ninguna cámara lo veía");
        }

        [UnityTest]
        public IEnumerator PatrullaConLineaDeVista_SumaCalorAlInstante_Y_ConPared_No()
        {
            yield return null;
            Vector3 p = jugador.transform.position;
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            GrafoCalles grafo = GrafoCalles.DeManzanas(new[] { new Vector2(p.x - 47f, p.z - 47f), new Vector2(p.x + 47f, p.z - 47f), new Vector2(p.x - 47f, p.z + 47f), new Vector2(p.x + 47f, p.z + 47f) }, 80f, 0.2f);
            int nodo = 0;
            jugador.transform.position = grafo.Nodo(nodo) + new Vector3(40f, 0f, 0f);
            p = jugador.transform.position;
            Assert.Greater(Vector3.Distance(grafo.Nodo(nodo), p), 25f, "Control: hay un nodo a distancia media");
            patrulla.Preparar(config.Crear(), config, grafo, nodo, () => p);
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(patrulla.VeElPunto(p), "Control: la patrulla lo ve");
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = (p + patrulla.transform.position) / 2f + Vector3.up * 5f;
            Vector3 eje = patrulla.transform.position - p;
            eje.y = 0f;
            pared.transform.rotation = Quaternion.LookRotation(eje.normalized, Vector3.up);
            pared.transform.localScale = new Vector3(40f, 12f, 1f);
            extras.Add(pared);
            Physics.SyncTransforms();
            Assert.IsFalse(patrulla.VeElPunto(p), "Con una pared en medio no lo ve");
            Delito();
            Assert.AreEqual(0f, real.Valor, 0.001f, "Tapada por la pared: sin testigo");
            Object.DestroyImmediate(pared);
            Physics.SyncTransforms();
            Delito();
            Assert.AreEqual(20f, real.Valor, 0.001f, "Sin pared la patrulla es testigo");
        }

        [UnityTest]
        public IEnumerator Civil_QueVeElDelito_LlamaConRetraso_YEntoncesHayCalor()
        {
            yield return null;
            PeatonMundo civil = PonerCivil(jugador.transform.position + new Vector3(8f, 0f, 0f));
            yield return null;
            Delito();
            Assert.AreEqual(0f, real.Valor, 0.001f, "Al instante todavía no hay calor");
            Assert.IsTrue(civil.Llamando, "El civil se queda llamando");
            Assert.AreEqual(1, testigos.LlamadasEnCurso);
            Assert.IsNotNull(civil.transform.Find("IconoLlamada"), "Se ve el icono del teléfono sobre el civil");
            Assert.IsTrue(civil.transform.Find("IconoLlamada").gameObject.activeSelf);
            yield return Esperar(SegundosLlamada * 0.5f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "A la mitad de la llamada aún no hay calor");
            yield return Esperar(SegundosLlamada);
            Assert.AreEqual(20f, real.Valor, 0.001f, "Tras llamar, el calor sube");
            Assert.IsFalse(civil.Llamando);
            Assert.IsFalse(civil.transform.Find("IconoLlamada").gameObject.activeSelf, "El icono se quita al terminar");
        }

        [UnityTest]
        public IEnumerator Civil_MuertoAntesDeLlamar_NoLlegaNadie()
        {
            yield return null;
            PeatonMundo civil = PonerCivil(jugador.transform.position + new Vector3(8f, 0f, 0f));
            yield return null;
            Delito();
            Assert.IsTrue(civil.Llamando);
            ResultadoDanio resultado = civil.RecibirDanio(new InfoDanio(999f, TipoDanio.Disparo, civil.transform.position, Vector3.forward, null));
            Assert.IsTrue(resultado.CausoKO);
            Assert.IsTrue(civil.Muerto);
            yield return Esperar(SegundosLlamada * 3f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Muerto antes de llamar: sin calor");
            Assert.AreEqual(0, testigos.LlamadasEnCurso);
            Assert.AreEqual(0f, testigos.Pendiente, 0.001f);
        }

        [UnityTest]
        public IEnumerator Civil_QueSeAleja_AntesDeLlamar_NoLlama()
        {
            yield return null;
            PeatonMundo civil = PonerCivil(jugador.transform.position + new Vector3(8f, 0f, 0f));
            yield return null;
            Delito();
            Assert.IsTrue(civil.Llamando);
            // El jugador sale corriendo y el civil queda lejos: huyó de la escena antes de llamar.
            jugador.transform.position += new Vector3(-config.RadioAbandonoCivil - 20f, 0f, 0f);
            yield return Esperar(SegundosLlamada * 3f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Se perdió de vista antes de llamar: sin calor");
            Assert.IsFalse(civil.Llamando);
        }

        [UnityTest]
        public IEnumerator CivilLejosOTapado_NoVeElDelito()
        {
            yield return null;
            PeatonMundo lejos = PonerCivil(jugador.transform.position + new Vector3(config.RadioVistaCivil + 15f, 0f, 0f));
            yield return null;
            Delito();
            Assert.IsFalse(lejos.Llamando);
            yield return Esperar(SegundosLlamada * 2f);
            Assert.AreEqual(0f, real.Valor, 0.001f);
        }

        [UnityTest]
        public IEnumerator ConLaPoliciaYaEncima_YEnMisiones_ElCalorSumaSinTestigos()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(15f, MotivoCalor.Mision);
            Assert.AreEqual(15f, real.Valor, 0.001f, "Las misiones suben calor sin testigos");
            Delito(5f);
            Assert.AreEqual(20f, real.Valor, 0.001f, "Con calor ya hay policía encima: suma sin preguntar");
        }

        [UnityTest]
        public IEnumerator MientrasTeBuscan_ElCalorNoBajaSolo_PeroUnEsconditeSiLoEnfria()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(30f, MotivoCalor.Mision);
            Assert.AreEqual(1, real.Nivel);
            real.Enfriar(5f); // el enfriamiento natural del servicio de calor
            yield return null;
            yield return null;
            Assert.AreEqual(30f, real.Valor, 0.001f, "El calor no baja solo mientras te buscan");
            Registro<ICalor>.Actual.Enfriar(10f); // un escondite sí
            yield return null;
            yield return null;
            Assert.AreEqual(20f, real.Valor, 0.001f);
            Registro<ICalor>.Actual.Borrar();
            Assert.AreEqual(0f, real.Valor, 0.001f);
        }

        // Genera docs/capturas/T-663-civil-llamando.png: un civil asustado con el icono del teléfono encima.
        [UnityTest]
        public IEnumerator Captura_CivilLlamando()
        {
            yield return null;
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab");
            Assert.IsNotNull(molde);
            GameObject objeto = Object.Instantiate(molde);
            objeto.SetActive(false);
            objeto.transform.position = jugador.transform.position + new Vector3(8f, 0f, 0f);
            PeatonMundo civil = objeto.GetComponent<PeatonMundo>();
            civil.Preparar(new[] { objeto.transform.position, objeto.transform.position + new Vector3(400f, 0f, 0f) }, configPeatones, new System.Random(1), () => jugador.transform.position, 0f);
            objeto.SetActive(true);
            extras.Add(objeto);
            yield return null;
            Poner(config, "segundosLlamadaCivil", 30f);
            Delito();
            yield return null;
            GameObject camObjeto = new GameObject("CamaraCaptura");
            extras.Add(camObjeto);
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.18f);
            camObjeto.transform.position = civil.transform.position + new Vector3(-4f, 1.9f, -1.5f);
            camObjeto.transform.LookAt(civil.transform.position + Vector3.up * 1.5f);
            yield return null;
            CapturaPrueba.Guardar(cam, "T-663-civil-llamando.png");
            Assert.IsTrue(civil.Llamando);
        }
    }
}
