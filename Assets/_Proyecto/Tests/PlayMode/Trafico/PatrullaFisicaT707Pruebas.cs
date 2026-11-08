using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-707: la patrulla es un carro de verdad (el de Vehiculos): sólida, maneja por las calles hacia el jugador, se deja robar y, mientras el jugador va en
    /// ella, la policía siempre lo encuentra. Sobre Barrio.prefab con un doble de ICalor y un jugador de prueba; las posiciones salen de las manzanas del barrio.
    /// </summary>
    public class PatrullaFisicaT707Pruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float LadoManzana = 80f;
        private const float AlturaCalles = 0.2f;
        private const float EscalaDeTiempo = 5f;
        private const float SegundosMaximos = 90f;
        private const float DistanciaParaSubir = 9f;
        private const int FramesDeEspera = 3;

        internal sealed class CalorDoble : ICalor
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

        internal sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject barrio;
        private GameObject jugadorObjeto;
        private JugadorDoble jugador;
        private CalorDoble calor;
        private GestorPersecucion gestor;
        private ConfigPersecucion config;
        private GrafoCalles calles;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            foreach (GestorPersecucion propio in barrio.GetComponentsInChildren<GestorPersecucion>())
            {
                propio.enabled = false;
            }
            List<Vector2> centros = barrio.transform.Cast<Transform>().Where(t => t.name.StartsWith("Manzana_"))
                .Select(t => new Vector2(t.position.x, t.position.z)).ToList();
            calles = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
            Vector3 cruce = calles.Nodo(calles.MasCercano(barrio.transform.position));
            jugadorObjeto = new GameObject("JugadorDePrueba");
            jugadorObjeto.transform.position = cruce;
            jugador = new JugadorDoble { Raiz = jugadorObjeto.transform };
            Registro<IJugador>.Agregar(jugador);
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);

            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            config = ConfigPersecucionPrueba.SinArresto();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
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

        private static IEnumerator Frames(int n = FramesDeEspera)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        private PatrullaMundo UnaPatrulla() => gestor.GetComponentsInChildren<PatrullaMundo>().FirstOrDefault(p => !p.Robada);

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        // Espera (con el tiempo acelerado) a que la patrulla llegue junto al jugador y se pueda subir a ella.
        private IEnumerator EsperarQueLlegue(PatrullaMundo patrulla)
        {
            CarroViejo carro = patrulla.GetComponent<CarroViejo>();
            Time.timeScale = EscalaDeTiempo;
            float limite = Time.time + SegundosMaximos;
            while (Time.time < limite && !(Plano(patrulla.transform.position, jugadorObjeto.transform.position) <= DistanciaParaSubir && carro.PuedeSubir))
            {
                yield return null;
            }
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Patrulla_EsUnCarroSolido_ConCuerpoCinematicoYColliders()
        {
            calor.Poner(1);
            yield return Frames();
            PatrullaMundo patrulla = UnaPatrulla();
            Assert.IsNotNull(patrulla);
            Collider[] colliders = patrulla.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();
            Assert.GreaterOrEqual(colliders.Length, 1, "La patrulla lleva colliders sólidos");
            Rigidbody cuerpo = patrulla.GetComponent<Rigidbody>();
            Assert.IsNotNull(cuerpo);
            Assert.IsTrue(cuerpo.isKinematic, "Como los carros del tráfico: cuerpo cinemático mientras maneja la IA");
            Assert.IsNotNull(patrulla.GetComponent<IMontable>(), "Se puede robar como cualquier carro");
            Assert.IsTrue(patrulla.GetComponent<CarroViejo>().EstaEnTrafico);
            Assert.IsNotNull(patrulla.GetComponent<CarroEstacionado>(), "Al robarla suma calor como cualquier carro");

            // Un rayo desde arriba choca con ella (es un objeto sólido, no un fantasma).
            Physics.SyncTransforms(); // la patrulla se mueve por transform: se pone al día la física antes del rayo
            Vector3 centro = patrulla.transform.position;
            Assert.IsTrue(Physics.Raycast(centro + Vector3.up * 6f, Vector3.down, out RaycastHit golpe, 12f, ~0, QueryTriggerInteraction.Ignore));
            Assert.IsTrue(golpe.collider.transform.IsChildOf(patrulla.transform), "El rayo pegó en " + golpe.collider.name);
            // La raíz queda sobre la calle, ni hundida ni flotando.
            RaycastHit[] abajo = Physics.RaycastAll(centro + Vector3.up * 6f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !h.collider.transform.IsChildOf(patrulla.transform)).OrderBy(h => h.distance).ToArray();
            Assert.IsNotEmpty(abajo, "Hay suelo bajo la patrulla");
            Assert.That(centro.y - abajo[0].point.y, Is.InRange(-0.05f, 0.6f), "Altura de la raíz sobre el suelo");
        }

        [UnityTest]
        public IEnumerator Patrulla_ManejaPorLasCallesHastaElJugador_YSeDejaRobar()
        {
            calor.Poner(1);
            yield return Frames();
            PatrullaMundo patrulla = UnaPatrulla();
            Assert.IsNotNull(patrulla);
            yield return EsperarQueLlegue(patrulla);
            CarroViejo carro = patrulla.GetComponent<CarroViejo>();
            Assert.LessOrEqual(Plano(patrulla.transform.position, jugadorObjeto.transform.position), DistanciaParaSubir, "La patrulla llegó junto al jugador");
            Assert.IsTrue(carro.PuedeSubir, "Detenida junto al jugador se puede subir");

            Camera cam = new GameObject("CamaraCaptura").AddComponent<Camera>();
            cam.farClipPlane = 300f;
            Vector3 p = patrulla.transform.position;
            cam.transform.position = p - patrulla.transform.forward * 8f + patrulla.transform.right * 3f + Vector3.up * 2.5f;
            cam.transform.LookAt(p + Vector3.up * 0.8f);
            yield return null;
            CapturaPrueba.Guardar(cam, "s1c-T-707-patrulla-solida.png");

            float alturaAntes = patrulla.transform.position.y;
            Assert.IsTrue(carro.IntentarSubir(jugador), "El jugador sube a la patrulla");
            yield return Frames();
            float asentada = Time.time + 2f;
            while (Time.time < asentada)
            {
                yield return null;
            }
            Assert.AreEqual(4, carro.RuedasEnPiso, "Robada, se apoya en sus cuatro ruedas");
            Assert.IsNull(patrulla.transform.parent, "Robada, queda suelta en la escena");
            Assert.That(Mathf.Abs(patrulla.transform.position.y - alturaAntes), Is.LessThan(0.25f), "Al robarla no da un salto ni se hunde");
            Assert.IsTrue(patrulla.Robada);
            Assert.IsTrue(PatrullaMundo.JugadorEnPatrulla);
            Assert.IsFalse(patrulla.GetComponent<Rigidbody>().isKinematic, "Robada, es un carro con física completa");
            Assert.IsFalse(patrulla.enabled, "Robada, la IA deja de manejarla");
            cam.transform.LookAt(patrulla.transform.position + Vector3.up * 0.8f);
            yield return null;
            CapturaPrueba.Guardar(cam, "s1c-T-707-patrulla-robada.png");
            Object.DestroyImmediate(cam.gameObject);

            // La policía manda otra en su lugar: la robada ya no cuenta como perseguidora.
            float limite = Time.time + 10f;
            while (Time.time < limite && gestor.Patrullas < 1)
            {
                yield return null;
            }
            Assert.AreEqual(1, gestor.Patrullas, "Llega otra patrulla en lugar de la robada");
            Assert.AreNotSame(patrulla, UnaPatrulla());

            Assert.IsTrue(carro.IntentarBajar(out _), "El jugador se baja");
            yield return Frames();
            Assert.IsFalse(PatrullaMundo.JugadorEnPatrulla);
        }

        [UnityTest]
        public IEnumerator RobarUnaPatrulla_LaPoliciaSiempreTeEncuentra_PeroAlBajarteYaNo()
        {
            calor.Poner(1);
            yield return Frames();
            PatrullaMundo robada = UnaPatrulla();
            yield return EsperarQueLlegue(robada);
            CarroViejo carro = robada.GetComponent<CarroViejo>();
            Assert.IsTrue(carro.IntentarSubir(jugador));
            calor.Poner(3);
            yield return Frames();
            Assert.AreEqual(3, gestor.Patrullas, "Con el calor 3 hay 3 patrullas de la policía");

            // El jugador (en la patrulla robada) se va al punto de las calles más lejos de todas las patrullas.
            PatrullaMundo[] perseguidoras = gestor.GetComponentsInChildren<PatrullaMundo>().Where(x => !x.Robada).ToArray();
            Vector3 lejos = Enumerable.Range(0, calles.Cantidad).Select(i => calles.Nodo(i))
                .OrderByDescending(n => perseguidoras.Min(x => Plano(x.transform.position, n))).First();
            jugadorObjeto.transform.position = lejos;
            Assert.Greater(perseguidoras.Min(x => Plano(x.transform.position, lejos)), config.RadioVista,
                "El jugador queda fuera del radio de vista de todas las patrullas");
            yield return Frames(5);
            foreach (PatrullaMundo x in perseguidoras)
            {
                Assert.IsTrue(x.VeAlJugador, "GPS: la patrulla lo ubica aunque esté lejos y sin línea de vista");
            }
            Assert.AreEqual(0f, gestor.SegundosSinVer, 0.001f, "Mientras vaya en la patrulla, nunca lo pierden de vista");

            // Al bajarse, el GPS se apaga: las patrullas lejanas dejan de verlo.
            Assert.IsTrue(carro.IntentarBajar(out _));
            yield return Frames(5);
            Assert.IsFalse(PatrullaMundo.JugadorEnPatrulla);
            Assert.IsTrue(perseguidoras.Any(x => !x.VeAlJugador), "Sin GPS, las patrullas lejanas ya no lo ven");
        }
    }
}
