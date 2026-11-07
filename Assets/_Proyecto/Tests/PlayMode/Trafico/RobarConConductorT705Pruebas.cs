using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Trafico.Testigos;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-705: al robar un carro del tráfico con conductor, lo sacan, cae al suelo y se levanta asustado; es un testigo: si el jugador se queda cerca llama a la
    /// policía con el teléfono (Inter_Telefono) y hay calor; si el jugador se va, lo matan o no hay quien llame, no hay persecución. El civil de T-663 usa
    /// la misma animación del teléfono al llamar.
    /// </summary>
    public class RobarConConductorT705Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaConfigTrafico = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const string RutaPrefabCarro = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab";
        private const string RutaPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        private const float SegundosLlamada = 5f;

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

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private ConfigPersecucion config;
        private ConfigTraficoCalle configTrafico;
        private CalorReal real;
        private GameObject jugador;
        private JugadorDoble datosJugador;
        private CarroTrafico carro;
        private GameObject piso;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            config.GetType().GetField("segundosLlamadaCivil", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(config, SegundosLlamada);
            configTrafico = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfigTrafico);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            jugador = new GameObject("JugadorDePrueba");
            datosJugador = new JugadorDoble { Raiz = jugador.transform };
            Registro<IJugador>.Agregar(datosJugador);
            real = new CalorReal();
            Registro<ICalor>.Agregar(real);
            GameObject objeto = new GameObject("Testigos");
            objeto.SetActive(false);
            objeto.AddComponent<TestigosDelito>().Configurar(config);
            objeto.SetActive(true);
            extras.Add(objeto);
            Physics.SyncTransforms();
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
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            foreach (GameObject c in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (c != null && c.name == CarroTrafico.NombreConductorRobado)
                {
                    Object.DestroyImmediate(c);
                }
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Un carro de tráfico detenido (acaba de recibir un golpe) con el jugador junto a su puerta, listo para robarlo.
        private IEnumerator CarroDetenido()
        {
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCarro).GetComponent<CarroTrafico>();
            carro = Object.Instantiate(molde);
            extras.Add(carro.gameObject);
            carro.Preparar(configTrafico, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(3), () => jugador.transform.position);
            yield return Esperar(0.3f);
            carro.AlChocar(5f, carro.transform.position);
            yield return Esperar(0.5f);
            IMontable montable = carro.GetComponent<IMontable>();
            jugador.transform.position = montable.PuntoSubida - carro.transform.right * 0.3f;
            Physics.SyncTransforms();
            Assert.IsTrue(montable.PuedeSubir, "El carro detenido se puede subir");
            CrearCamaraDeFoto();
        }

        // Una foto del conductor y el carro, desde enfrente y un poco arriba, para revisarla a ojo.
        private void Capturar(Transform conductor, string nombre)
        {
            Vector3 lado = carro.transform.right;
            camaraFoto.transform.position = conductor.position - lado * 3.2f + carro.transform.forward * 1.5f + Vector3.up * 1.6f;
            camaraFoto.transform.LookAt(conductor.position + Vector3.up * 0.7f);
            CapturaPrueba.Guardar(camaraFoto, nombre);
        }

        // La cámara existe desde antes del robo: sin ninguna cámara que lo vea, Unity no avanza la animación del conductor.
        private Camera camaraFoto;

        private void CrearCamaraDeFoto()
        {
            GameObject luz = new GameObject("LuzDePrueba");
            extras.Add(luz);
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            GameObject c = new GameObject("CamaraDePrueba");
            extras.Add(c);
            camaraFoto = c.AddComponent<Camera>();
            camaraFoto.clearFlags = CameraClearFlags.SolidColor;
            camaraFoto.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            c.transform.position = carro.transform.position + carro.transform.right * -3.2f + Vector3.up * 1.6f;
            c.transform.LookAt(carro.transform.position + Vector3.up);
        }

        private bool Robar()
        {
            return carro.GetComponent<IMontable>().IntentarSubir(datosJugador);
        }

        [UnityTest]
        public IEnumerator ElConductor_SaleCaeAlSueloYSeLevantaAsustado()
        {
            yield return CarroDetenido();
            Vector3 puerta = carro.GetComponent<IMontable>().PuntoSubida;
            Assert.IsNull(GameObject.Find(CarroTrafico.NombreConductorRobado), "Antes del robo no hay conductor afuera");
            Assert.IsTrue(Robar(), "El jugador sube");
            GameObject conductor = GameObject.Find(CarroTrafico.NombreConductorRobado);
            Assert.IsNotNull(conductor, "Lo sacan del carro");
            ConductorRobado fases = conductor.GetComponent<ConductorRobado>();
            Assert.IsNotNull(fases);
            Assert.IsNotNull(conductor.GetComponent<PeatonMundo>(), "Es un testigo (civil)");
            Assert.IsTrue(fases.Tirado, "Cae al suelo");
            float maximaAltura = 0f;
            float fin = Time.time + 0.6f;
            while (Time.time < fin)
            {
                maximaAltura = Mathf.Max(maximaAltura, conductor.transform.position.y);
                yield return null;
            }
            Vector3 d = conductor.transform.position - puerta;
            d.y = 0f;
            Assert.Less(d.magnitude, 1.2f, "Quedó junto a la puerta (" + d.magnitude.ToString("F2") + " m)");
            Assert.Less(maximaAltura, 0.35f, "Va por el suelo, no vuela (" + maximaAltura.ToString("F2") + " m)");
            Assert.IsTrue(fases.Tirado, "Sigue tirado un momento");
            Capturar(conductor.transform, "s1c-T-705-conductor-en-el-suelo.png");
            yield return Esperar(ConductorRobado.SegundosCaido + ConductorRobado.SegundosLevantarse);
            Assert.IsFalse(fases.Tirado, "Se levantó");
            Assert.AreEqual(ConductorRobado.FaseConductor.Asustado, fases.Fase, "Y está asustado");
            Capturar(conductor.transform, "s1c-T-705-conductor-asustado-llamando.png");
        }

        [UnityTest]
        public IEnumerator ElJugadorSeQuedaCerca_ElConductorLlamaConElTelefonoYHayCalor()
        {
            yield return CarroDetenido();
            Assert.AreEqual(0f, real.Valor);
            Assert.IsTrue(Robar());
            PeatonMundo peaton = GameObject.Find(CarroTrafico.NombreConductorRobado).GetComponent<PeatonMundo>();
            Assert.IsTrue(peaton.Llamando, "Es testigo y llama");
            Animator animador = peaton.GetComponentInChildren<Animator>(false);
            Assert.IsNotNull(animador, "Tiene modelo animado");
            string recorrido = "";
            float t0 = Time.time;
            string ultimo = "";
            bool vioElTelefono = false;
            float fin = Time.time + SegundosLlamada + 0.3f;
            while (Time.time < fin)
            {
                yield return null;
                AnimatorStateInfo estadoAhora = animador.GetCurrentAnimatorStateInfo(0);
                string nombreAhora = estadoAhora.IsName("Telefono") ? "Telefono" : estadoAhora.IsName("Idle") ? "Idle" : estadoAhora.IsName("LevantarseBocaArriba") ? "LevantarseArriba" : estadoAhora.IsName("LevantarseBocaAbajo") ? "LevantarseAbajo" : "otro" + estadoAhora.shortNameHash;
                if (nombreAhora != ultimo)
                {
                    ultimo = nombreAhora;
                    recorrido += nombreAhora + "@" + (Time.time - t0).ToString("F1") + " ";
                }
                vioElTelefono |= animador.GetCurrentAnimatorStateInfo(0).IsName("Telefono");
            }
            Assert.IsTrue(vioElTelefono, "Usa la animación Inter_Telefono al llamar (estados: " + recorrido + ", gestos " + peaton.GestosTelefono + ", llamando " + peaton.Llamando + ")");
            Assert.GreaterOrEqual(peaton.GestosTelefono, 1);
            Assert.GreaterOrEqual(real.Valor, 25f, "Terminó de llamar: hay calor (persecución)");
        }

        [UnityTest]
        public IEnumerator ElJugadorSeVa_ElConductorNoLlamaYNoHayPersecucion()
        {
            yield return CarroDetenido();
            Assert.IsTrue(Robar());
            GameObject conductor = GameObject.Find(CarroTrafico.NombreConductorRobado);
            PeatonMundo peaton = conductor.GetComponent<PeatonMundo>();
            Assert.IsTrue(peaton.Llamando);
            // El jugador se aleja con el carro antes de que termine la llamada.
            jugador.transform.position += new Vector3(0f, 0f, 60f);
            yield return Esperar(SegundosLlamada + 0.5f);
            Assert.IsFalse(peaton.Llamando, "Perdió de vista al jugador: la llamada se canceló");
            Assert.AreEqual(0f, real.Valor, 0.001f, "Nadie llamó: no hay persecución");
            yield return Esperar(2.5f);
            Assert.IsNotNull(conductor.GetComponent<ConductorHuye>(), "Asustado, sale corriendo");
        }

        [UnityTest]
        public IEnumerator SiMatanAlConductor_NoLlamaYNoHayCalor()
        {
            yield return CarroDetenido();
            Assert.IsTrue(Robar());
            PeatonMundo peaton = GameObject.Find(CarroTrafico.NombreConductorRobado).GetComponent<PeatonMundo>();
            yield return Esperar(0.5f);
            peaton.RecibirDanio(new InfoDanio(500f, TipoDanio.Disparo, peaton.transform.position, Vector3.forward, null));
            Assert.IsTrue(peaton.Muerto);
            yield return Esperar(SegundosLlamada + 0.5f);
            Assert.IsFalse(peaton.Llamando);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Muerto no llama");
        }

        [UnityTest]
        public IEnumerator ElCivilQueLlama_UsaLaAnimacionDelTelefono()
        {
            GameObject objeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPeaton), new Vector3(10f, 0f, 10f), Quaternion.identity);
            extras.Add(objeto);
            PeatonMundo civil = objeto.GetComponent<PeatonMundo>();
            // Sin ninguna cámara que lo vea, Unity no avanza la animación (culling): se pone una mirándolo.
            GameObject c = new GameObject("CamaraDePrueba");
            extras.Add(c);
            c.AddComponent<Camera>();
            c.transform.position = new Vector3(10f, 1.6f, 6f);
            c.transform.LookAt(new Vector3(10f, 1f, 10f));
            yield return null;
            civil.IniciarLlamada();
            Animator animador = civil.GetComponentInChildren<Animator>(false);
            Assert.IsNotNull(animador);
            bool vioElTelefono = false;
            float fin = Time.time + 2.5f;
            while (Time.time < fin)
            {
                yield return null;
                vioElTelefono |= animador.GetCurrentAnimatorStateInfo(0).IsName("Telefono");
            }
            Assert.IsTrue(vioElTelefono, "El civil que llama juega Inter_Telefono");
            Assert.GreaterOrEqual(civil.GestosTelefono, 1);
            civil.TerminarLlamada();
        }
    }
}
