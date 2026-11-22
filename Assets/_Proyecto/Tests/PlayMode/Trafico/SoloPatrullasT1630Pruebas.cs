using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
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
    /// T-1630 en una red chica de 6 manzanas armada aquí: la patrulla de ronda se detiene en un semáforo en rojo y arranca en verde, con sirena (alerta) se pasa el rojo,
    /// el tráfico civil apagado no saca ningún carro en movimiento (y encendido sí) y un carro estacionado se queda quieto y sigue siendo robable.
    /// </summary>
    public class SoloPatrullasT1630Pruebas
    {
        private const string RutaConfigPersecucion = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaConfigTrafico = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaCarroTrafico = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico.prefab";
        private const string RutaCarroEstacionado = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V09.prefab";
        private const float Paso = 94f;
        private const float EscalaDeTiempo = 3f;
        private const float DistanciaPoste = 8.5f;
        private const float SegundosParaRonda = 15f;
        private const float SegundosParaDetenerse = 90f;
        private const float SegundosParado = 2f;
        private const float VelocidadQueCorre = 3f;
        private const float VelocidadParada = 0.3f;
        private const float VelocidadQueArranca = 2f;
        private const float SegundosParaArrancar = 10f;
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private readonly List<GameObject> extras = new List<GameObject>();
        private readonly List<SemaforoCruce> semaforos = new List<SemaforoCruce>();
        private GameObject barrio;
        private GameObject jugador;
        private ConfigPersecucion config;
        private GestorPersecucion gestor;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            EstadisticasTrafico.Reiniciar();
            escalaAnterior = Time.timeScale;
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(2000f, 1f, 2000f);
            extras.Add(piso);
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = Vector3.zero;
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            Registro<ICalor>.Agregar(new TestigosT661Pruebas.CalorReal());
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camObjeto.AddComponent<Camera>();
            camObjeto.transform.position = new Vector3(0f, 600f, 0f);
            camObjeto.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // mira al cielo: nada de lo que nace se ve
            extras.Add(camObjeto);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            semaforos.Clear();
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(jugador);
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            EstadisticasTrafico.Reiniciar();
        }

        private void Poner(string campo, int valor)
        {
            FieldInfo info = typeof(ConfigPersecucion).GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance);
            info.SetValue(config, System.Convert.ChangeType(valor, info.FieldType));
        }

        // El gestor del barrio con una sola patrulla de ronda (y sin arresto).
        private void IniciarGestor()
        {
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfigPersecucion));
            Poner("patrullasDeRonda", 1);
            Poner("segundosArresto", 9999);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        // Un semáforo en cada cruce de la red, con un poste por cada calle que llega (en la esquina derecha, como los pone el editor).
        private void PonerSemaforos()
        {
            GrafoCalles calles = GestorPersecucion.ArmarCallesDe(barrio.transform);
            Assert.IsNotNull(calles, "La red de prueba tiene calles");
            for (int i = 0; i < calles.Cantidad; i++)
            {
                if (calles.Vecinos(i).Count < 3)
                {
                    continue;
                }
                Vector3 c = calles.Nodo(i);
                GameObject objeto = new GameObject("SemaforoPrueba_" + i);
                extras.Add(objeto);
                objeto.transform.position = c;
                SemaforoCruce cruce = objeto.AddComponent<SemaforoCruce>();
                foreach (int vecino in calles.Vecinos(i))
                {
                    Vector3 v = calles.Nodo(vecino) - c;
                    v.y = 0f;
                    Vector3 sentido = Mathf.Abs(v.x) > Mathf.Abs(v.z) ? new Vector3(-Mathf.Sign(v.x), 0f, 0f) : new Vector3(0f, 0f, -Mathf.Sign(v.z));
                    GameObject poste = new GameObject("Poste");
                    poste.transform.SetParent(objeto.transform, false);
                    poste.transform.position = c - sentido * DistanciaPoste + Vector3.Cross(Vector3.up, sentido) * DistanciaPoste;
                    cruce.AgregarPoste(new SemaforoCruce.Poste(poste.transform, Mathf.Abs(sentido.x) > 0.5f ? 0 : 1, sentido, true, null, null));
                }
                semaforos.Add(cruce);
            }
            Assert.Greater(semaforos.Count, 0, "Hay al menos un cruce con semáforo");
        }

        private void Forzar(FaseCruce? fase)
        {
            foreach (SemaforoCruce s in semaforos)
            {
                s.Forzar(fase);
            }
        }

        // Saca la patrulla de ronda, pone todos los semáforos en rojo y espera a que corra y luego se quede parada unos segundos.
        private IEnumerator EsperarPatrullaParadaEnRojo(System.Action<PatrullaMundo> listo)
        {
            IniciarGestor();
            float limite = Time.time + SegundosParaRonda;
            while (Time.time < limite && gestor.Rondas.Count == 0)
            {
                yield return null;
            }
            Assert.AreEqual(1, gestor.Rondas.Count, "Sale la patrulla de ronda");
            PatrullaMundo ronda = gestor.Rondas[0];
            PonerSemaforos();
            Forzar(FaseCruce.Rojo);
            Time.timeScale = EscalaDeTiempo;
            bool corrio = false;
            float quietaDesde = -1f;
            float tope = Time.time + SegundosParaDetenerse;
            while (Time.time < tope)
            {
                yield return null;
                Assert.IsTrue(ronda != null, "La patrulla sigue en la calle");
                float v = ronda.Rapidez;
                corrio |= v > VelocidadQueCorre;
                if (corrio && v < VelocidadParada)
                {
                    quietaDesde = quietaDesde < 0f ? Time.time : quietaDesde;
                    if (Time.time - quietaDesde >= SegundosParado)
                    {
                        break;
                    }
                }
                else
                {
                    quietaDesde = -1f;
                }
            }
            Assert.IsTrue(corrio, "Primero circula (de ronda, tranquila)");
            Assert.GreaterOrEqual(quietaDesde, 0f, "Después se detiene por el semáforo");
            Assert.GreaterOrEqual(Time.time - quietaDesde, SegundosParado, "Y se queda parada mientras sigue en rojo");
            listo(ronda);
        }

        [UnityTest]
        public IEnumerator PatrullaDeRonda_SeDetieneEnRojo_YArrancaEnVerde()
        {
            PatrullaMundo ronda = null;
            yield return EsperarPatrullaParadaEnRojo(p => ronda = p);
            Assert.IsTrue(ronda.EnRonda, "Sigue de ronda");
            Assert.IsTrue(RegistroSemaforos.Consultar(ronda.transform.position, ronda.transform.forward, out FaseCruce fase, out float metros), "Tiene un semáforo al frente");
            Assert.AreEqual(FaseCruce.Rojo, fase);
            Assert.GreaterOrEqual(metros, -CalculoCruceSemaforo.ToleranciaLinea, "Quedó antes de la línea de parada");
            Assert.LessOrEqual(metros, 8f, "Y cerca de ella");

            Forzar(FaseCruce.Verde);
            float tope = Time.time + SegundosParaArrancar;
            while (Time.time < tope && ronda.Rapidez < VelocidadQueArranca)
            {
                yield return null;
            }
            Assert.Greater(ronda.Rapidez, VelocidadQueArranca, "En verde arranca");
        }

        [UnityTest]
        public IEnumerator PatrullaConSirena_NoSeDetieneEnRojo()
        {
            PatrullaMundo ronda = null;
            yield return EsperarPatrullaParadaEnRojo(p => ronda = p);
            ronda.Alertar(); // empezó la búsqueda: deja la ronda y persigue con sirena
            Assert.IsFalse(ronda.EnRonda);
            float tope = Time.time + SegundosParaArrancar;
            while (Time.time < tope && ronda.Rapidez < VelocidadQueArranca)
            {
                yield return null;
            }
            Assert.Greater(ronda.Rapidez, VelocidadQueArranca, "Con sirena se pasa el alto y sigue (los semáforos siguen en rojo)");
        }

        private TraficoCalle CrearTrafico(bool circulan)
        {
            GameObject raiz = new GameObject("TraficoDePrueba");
            extras.Add(raiz);
            TraficoCalle trafico = raiz.AddComponent<TraficoCalle>();
            trafico.Configurar(AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfigTrafico),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarroTrafico).GetComponent<CarroTrafico>(), barrio.transform);
            trafico.CirculanCiviles = circulan;
            return trafico;
        }

        [UnityTest]
        public IEnumerator TraficoCivilApagado_NoSaleNingunCarroEnMovimiento_YEncendidoSi()
        {
            ConfigTraficoCalle numeros = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfigTrafico);
            TraficoCalle trafico = CrearTrafico(false);
            float tope = Time.time + 3f;
            while (Time.time < tope)
            {
                yield return null;
                Assert.AreEqual(0, trafico.Vivos, "Con los civiles apagados no hay carros vivos");
                Assert.AreEqual(0, CarroTrafico.Activos.Count, "Ni uno en movimiento");
            }
            Assert.AreEqual(numeros.Maximo, trafico.Carros.Count, "El pool sigue armado para refuerzos y asaltos");
            trafico.CirculanCiviles = true;
            tope = Time.time + 5f;
            while (Time.time < tope && trafico.Vivos == 0)
            {
                yield return null;
            }
            Assert.Greater(trafico.Vivos, 0, "Encendidos salen carros");
        }

        [UnityTest]
        public IEnumerator CarroEstacionado_SeQuedaQuieto_YSigueSiendoRobable()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarroEstacionado);
            Assert.IsNotNull(prefab, "Falta " + RutaCarroEstacionado);
            CrearTrafico(false); // con la ciudad andando pero sin civiles que lo toquen
            GameObject carro = Object.Instantiate(prefab, new Vector3(30f, 0.45f, 30f), Quaternion.Euler(0f, 90f, 0f));
            extras.Add(carro);
            yield return new WaitForSeconds(1f); // se asienta
            Vector3 antes = carro.transform.position;
            Quaternion giroAntes = carro.transform.rotation;
            yield return new WaitForSeconds(4f);
            Assert.Less(Vector3.Distance(antes, carro.transform.position), 0.05f, "No se mueve");
            Assert.Less(Quaternion.Angle(giroAntes, carro.transform.rotation), 1f, "Ni gira");
            Rigidbody cuerpo = carro.GetComponent<Rigidbody>();
            if (cuerpo != null && !cuerpo.isKinematic)
            {
                Assert.Less(cuerpo.linearVelocity.magnitude, 0.1f, "Quieto");
            }
            CarroViejo robable = carro.GetComponent<CarroViejo>();
            Assert.IsNotNull(robable, "Es un carro robable");
            Assert.IsTrue(robable.IntentarSubir(null), "Se puede subir y robar");
        }
    }
}
