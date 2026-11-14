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
    /// T-986: las patrullas manejan con la física del carro del jugador (CarroViejo con un conductor IA). Persecución completa sobre Barrio.prefab:
    /// el jugador huye en un carro por varias calles con vueltas, sube el calor, salen patrullas, lo persiguen y lo alcanzan. En cada paso de física de cada
    /// patrulla se mide: cuerpo no cinemático, sin saltos de posición, sin girar sobre su eje, derrape en curvas y frenados reales. Deja capturas en docs/capturas/t986.
    /// </summary>
    public class PersecucionFisicaT986Pruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaCarroJugador = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const float LadoManzana = 80f;
        private const float PasoCuadricula = 94f;
        private const float AlturaCalles = 0.2f;
        private const float EscalaDeTiempo = 3f;
        private const float SegundosMaximos = 150f;
        private const float VelocidadJugador = 9f;
        private const float VelocidadJugadorCiudad = 13f;
        private const float SegundosCiudad = 80f;
        private const int CapturasCiudad = 6;
        private const float MiraJugador = 8f;
        private const float RadioNodoJugador = 4f;
        private const int NivelDeCalor = 3;
        private const float SegundosEntreCapturas = 2f;
        private const int MaximoDeCapturas = 8;
        private const float MargenSalto = 0.05f;
        private const float MargenGiro = 0.6f;
        private const float RapidezQuieta = 0.5f;
        private const float GiroMaximoQuieta = 10f;
        private const float DerrapeMinimo = 0.5f;
        private const float FrenadoMinimo = 3f;
        private const float VentanaFrenado = 0.3f;
        private const int PasosOmitidos = 5;
        private const float LargoBrazo = 55f;
        private const float SegundosParaDetenida = 0.4f;
        private const float SegundosTrasAlcanzar = 2f;
        private const float SegundosExtraSinCapturas = 6f;
        private const float SegundosLibreTrasChoque = 0.4f;
        private const float EmpujonMinimo = 1f;

        /// <summary>Anota cuándo fue el último choque de un cuerpo (con otro carro, un policía o la ciudad).</summary>
        private sealed class Contactos : MonoBehaviour
        {
            public float Ultimo = float.MinValue;
            public int Cantidad;

            private void OnCollisionEnter(Collision c)
            {
                Cantidad++;
                Ultimo = Time.time;
            }

            private void OnCollisionStay(Collision c)
            {
                Ultimo = Time.time;
            }
        }

        /// <summary>Mide cada paso de física de cada patrulla activa.</summary>
        private sealed class Telemetria : MonoBehaviour
        {
            private sealed class Historia
            {
                public Vector3 posicion;
                public float rapidez;
                public float yaw;
                public Vector3 velocidad;
                public float empujon = float.MinValue;
                public int pasos;
                public float segundosQuieta;
                public Contactos contactos;
                public readonly Queue<float> rapideces = new Queue<float>();
            }

            private readonly Dictionary<PatrullaMundo, Historia> historias = new Dictionary<PatrullaMundo, Historia>();
            public ParametrosManejo Carro;
            public int Muestras;
            public int MuestrasConChoque;
            public int Choques;
            public int Cinematicas;
            public int SaltosMalos;
            public float MaxExcesoSalto = float.MinValue;
            public int GirosMalos;
            public float MaxExcesoGiro = float.MinValue;
            public float MaxGiroQuieta;
            public float MaxLateral;
            public float MaxFrenado;
            public float MaxRapidez;
            public int PatrullasVistas;
            public int Volcadas;
            public readonly HashSet<PatrullaMundo> Distintas = new HashSet<PatrullaMundo>();

            private void FixedUpdate()
            {
                float dt = Time.fixedDeltaTime;
                float radioMinimo = Carro.DistanciaEntreEjes / Mathf.Tan(Carro.AnguloDireccionParado * Mathf.Deg2Rad);
                foreach (PatrullaMundo patrulla in PatrullaMundo.Activas.ToArray())
                {
                    if (patrulla == null)
                    {
                        continue;
                    }
                    Rigidbody cuerpo = patrulla.GetComponent<Rigidbody>();
                    Distintas.Add(patrulla);
                    if (cuerpo.isKinematic)
                    {
                        Cinematicas++;
                    }
                    if (!historias.TryGetValue(patrulla, out Historia h))
                    {
                        h = new Historia { contactos = patrulla.gameObject.AddComponent<Contactos>() };
                        historias[patrulla] = h;
                    }
                    Vector3 posicion = cuerpo.position;
                    float rapidez = cuerpo.linearVelocity.magnitude;
                    float yaw = cuerpo.rotation.eulerAngles.y;
                    if (!patrulla.Orientada)
                    {
                        h.pasos = 0; // todavía se está colocando al aparecer (fuera de la vista): eso no se mide
                    }
                    // Un empujón que no es choque (una explosión, un golpe de un cuerpo que no avisa) cambia la velocidad más de lo que un motor da en un paso.
                    if (h.pasos > 0 && (cuerpo.linearVelocity - h.velocidad).magnitude > EmpujonMinimo)
                    {
                        h.empujon = Time.time;
                    }
                    h.velocidad = cuerpo.linearVelocity;
                    bool libre = Time.time - Mathf.Max(h.contactos.Ultimo, h.empujon) > SegundosLibreTrasChoque;
                    if (h.pasos >= PasosOmitidos && !libre)
                    {
                        MuestrasConChoque++;
                    }
                    if (h.pasos >= PasosOmitidos && libre)
                    {
                        Muestras++;
                        float salto = (posicion - h.posicion).magnitude;
                        float permitido = Mathf.Max(rapidez, h.rapidez) * dt + MargenSalto;
                        MaxExcesoSalto = Mathf.Max(MaxExcesoSalto, salto - permitido);
                        if (salto > permitido)
                        {
                            SaltosMalos++;
                        }
                        float giro = Mathf.Abs(Mathf.DeltaAngle(h.yaw, yaw)) * Mathf.Deg2Rad / dt;
                        float tope = Mathf.Max(rapidez, h.rapidez) / radioMinimo + MargenGiro;
                        MaxExcesoGiro = Mathf.Max(MaxExcesoGiro, giro - tope);
                        if (giro > tope)
                        {
                            GirosMalos++;
                            if (GirosMalos <= 6)
                            {
                                Debug.Log("[T-986] giro malo " + patrulla.name + " t=" + Time.time.ToString("F2") + " giro " + giro.ToString("F1") + " rad/s v=" + rapidez.ToString("F2") + " pos " + posicion + " edad " + h.pasos + " yaw " + h.yaw + "->" + yaw + " cerca de " + string.Join(",", Physics.OverlapSphere(posicion, 3.5f).Select(c => c.name).Distinct().Take(8)));
                            }
                        }
                        // "Detenida": menos de 0.5 m/s de forma continua desde hace un rato (frenando, el giro que traía se apaga en unas décimas).
                        h.segundosQuieta = rapidez < RapidezQuieta ? h.segundosQuieta + dt : 0f;
                        if (h.segundosQuieta >= SegundosParaDetenida)
                        {
                            MaxGiroQuieta = Mathf.Max(MaxGiroQuieta, giro * Mathf.Rad2Deg);
                        }
                        Vector3 derecha = cuerpo.rotation * Vector3.right;
                        float lateral = Mathf.Abs(Vector3.Dot(cuerpo.linearVelocity, derecha));
                        if (giro > 0.3f)
                        {
                            MaxLateral = Mathf.Max(MaxLateral, lateral);
                        }
                        MaxRapidez = Mathf.Max(MaxRapidez, rapidez);
                        if (h.rapideces.Count >= Mathf.RoundToInt(VentanaFrenado / dt))
                        {
                            MaxFrenado = Mathf.Max(MaxFrenado, (h.rapideces.Peek() - rapidez) / VentanaFrenado);
                        }
                        if (Vector3.Dot(cuerpo.rotation * Vector3.up, Vector3.up) < 0.3f)
                        {
                            Volcadas++;
                        }
                    }
                    h.rapideces.Enqueue(rapidez);
                    while (h.rapideces.Count > Mathf.RoundToInt(VentanaFrenado / dt))
                    {
                        h.rapideces.Dequeue();
                    }
                    h.posicion = posicion;
                    h.rapidez = rapidez;
                    h.yaw = yaw;
                    h.pasos++;
                }
            }
        }

        /// <summary>El jugador: un carro que sigue una ruta de cruces con el mismo conductor IA (por la entrada externa del carro).</summary>
        private sealed class JugadorEnCarro
        {
            private readonly CarroViejo carro;
            private readonly Rigidbody cuerpo;
            private readonly ConductorCarroIA conductor;
            private readonly ParametrosConductorIA parametros;
            private readonly List<Vector3> ruta;
            private int indice = 1;
            private readonly float velocidadMaxima;

            public JugadorEnCarro(CarroViejo carro, ConfigPersecucion config, List<Vector3> ruta, float velocidadMaxima)
            {
                this.velocidadMaxima = velocidadMaxima;
                this.carro = carro;
                cuerpo = carro.GetComponent<Rigidbody>();
                this.ruta = ruta;
                parametros = config.ParametrosConductor;
                conductor = new ConductorCarroIA(carro.ParametrosManejo, parametros);
                carro.ConducirConIA(Entrada);
            }

            public bool Termino => indice >= ruta.Count;

            private EntradaCarro Entrada()
            {
                Vector3 posicion = cuerpo.position;
                while (indice < ruta.Count && Plano(ruta[indice] - posicion) < RadioNodoJugador)
                {
                    indice++;
                }
                if (indice >= ruta.Count)
                {
                    return conductor.Calcular(posicion, cuerpo.rotation * Vector3.forward, cuerpo.linearVelocity, ruta[ruta.Count - 1], 0f, Time.fixedDeltaTime);
                }
                Vector3 actual = posicion;
                float restante = MiraJugador;
                Vector3 rumbo = (ruta[indice] - actual).normalized;
                float velocidad = velocidadMaxima;
                float acumulado = 0f;
                for (int i = indice; i < ruta.Count; i++)
                {
                    Vector3 tramo = ruta[i] - actual;
                    tramo.y = 0f;
                    float largo = tramo.magnitude;
                    if (largo > 0.01f)
                    {
                        rumbo = tramo / largo;
                    }
                    if (largo >= restante)
                    {
                        break;
                    }
                    restante -= largo;
                    actual = ruta[i];
                }
                Vector3 objetivo = actual + rumbo * restante;
                for (int k = indice; k < ruta.Count - 1 && acumulado < 40f; k++)
                {
                    Vector3 entra = ruta[k] - ruta[k - 1];
                    Vector3 sale = ruta[k + 1] - ruta[k];
                    entra.y = 0f;
                    sale.y = 0f;
                    float vc = ConductorCarroIA.VelocidadEnCurva(parametros, velocidadMaxima, Vector3.Angle(entra, sale));
                    acumulado = Plano(ruta[k] - posicion);
                    velocidad = Mathf.Min(velocidad, ConductorCarroIA.VelocidadParaFrenar(Mathf.Max(0f, acumulado - RadioNodoJugador), vc, parametros.DesaceleracionPlaneada));
                }
                return conductor.Calcular(posicion, cuerpo.rotation * Vector3.forward, cuerpo.linearVelocity, objetivo, velocidad, Time.fixedDeltaTime);
            }

            private static float Plano(Vector3 v)
            {
                v.y = 0f;
                return v.magnitude;
            }

            public Vector3 Posicion => cuerpo.position;
            public Vector3 Adelante => cuerpo.rotation * Vector3.forward;
            public Transform Raiz => carro.transform;
        }

        private GameObject barrio;
        private bool ciudad;
        private HashSet<int> raicesAntes;
        private Transform raizCiudad;
        private GameObject telemetriaObjeto;
        private UnityEngine.SceneManagement.Scene escenaCiudad;
        private GameObject jugadorObjeto;
        private PatrullaFisicaT707Pruebas.CalorDoble calor;
        private GestorPersecucion gestor;
        private GrafoCalles calles;
        private ConfigPersecucion config;
        private float escalaAnterior;
        private float pasoAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            pasoAnterior = Time.fixedDeltaTime;
            calor = new PatrullaFisicaT707Pruebas.CalorDoble();
            Registro<ICalor>.Agregar(calor);
            config = ConfigPersecucionPrueba.SinArresto();
        }

        // El barrio de verdad (con sus edificios, cierres y cosas en la calle) o una cuadrícula abierta de seis manzanas sin nada más que el piso.
        private void Montar(bool barrioReal)
        {
            List<Vector2> centros;
            if (barrioReal)
            {
                barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
                foreach (GestorPersecucion propio in barrio.GetComponentsInChildren<GestorPersecucion>())
                {
                    propio.enabled = false;
                }
                centros = barrio.transform.Cast<Transform>().Where(t => t.name.StartsWith("Manzana_")).Select(t => new Vector2(t.position.x, t.position.z)).ToList();
            }
            else
            {
                barrio = new GameObject("BarrioPrueba");
                centros = new List<Vector2>();
                for (int columna = -1; columna <= 1; columna++)
                {
                    for (int fila = -1; fila <= 0; fila++)
                    {
                        Vector2 centro = new Vector2(columna * PasoCuadricula, (fila * 2 + 1) * PasoCuadricula / 2f);
                        centros.Add(centro);
                        GameObject m = new GameObject("Manzana_" + centros.Count);
                        m.transform.SetParent(barrio.transform, false);
                        m.transform.position = new Vector3(centro.x, 0f, centro.y);
                    }
                }
            }
            calles = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.GetComponent<MeshRenderer>().enabled = false;
            piso.transform.SetParent(barrio.transform, true);
            piso.transform.position = new Vector3(barrio.transform.position.x, AlturaCalles - 0.5f, barrio.transform.position.z);
            piso.transform.localScale = new Vector3(600f, 1f, 600f);
            Physics.SyncTransforms();
            int cerrados = BloqueosDeCalles.Marcar(calles); // los cierres del barrio detienen a una patrulla de verdad
            Debug.Log("[T-986] tramos cerrados: " + cerrados);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            Time.fixedDeltaTime = pasoAnterior;
            Object.DestroyImmediate(jugadorObjeto);
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(telemetriaObjeto);
            if (ciudad && gestor != null)
            {
                Object.DestroyImmediate(gestor.gameObject); // sus patrullas cuelgan de él
            }
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        // La ciudad de verdad (Ciudad_Prologo, la de la demo): se carga igual que RecorridoDemoPruebas y la persecución cuelga de su barrio.
        private IEnumerator MontarCiudad()
        {
            ciudad = true;
            raicesAntes = CierresCiudadT986Pruebas.InstantaneaDeRaices();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(CierresCiudadT986Pruebas.RutaEscena,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            escenaCiudad = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }
            foreach (GestorPersecucion propio in Object.FindObjectsByType<GestorPersecucion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                propio.enabled = false;
            }
            calles = CierresCiudadT986Pruebas.GrafoDeLaCiudad(out raizCiudad);
            Assert.IsNotNull(calles, "La ciudad tiene calles para la persecución");
            Physics.SyncTransforms();
            int cerrados = BloqueosDeCalles.Marcar(calles);
            Debug.Log("[T-986] ciudad: " + calles.Cantidad + " nodos, tramos cerrados: " + cerrados);
            // Los dobles del jugador y del calor mandan: lo que la escena haya registrado se quita.
            Registros.LimpiarTodos();
            calor = new PatrullaFisicaT707Pruebas.CalorDoble();
            Registro<ICalor>.Agregar(calor);
        }

        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            if (ciudad)
            {
                ciudad = false;
                UnityEngine.SceneManagement.Scene vacia = UnityEngine.SceneManagement.SceneManager.CreateScene("VaciaT986");
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(vacia);
                if (escenaCiudad.IsValid() && escenaCiudad.isLoaded)
                {
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(escenaCiudad);
                }
                CierresCiudadT986Pruebas.QuitarRaicesNuevas(raicesAntes);
            }
        }

        // El camino simple más largo del grafo (sin repetir cruces) y de regreso por el mismo: con vueltas en las esquinas y en U en las puntas.
        private List<Vector3> RutaLargaDeIdaYVuelta()
        {
            List<int> mejor = new List<int>();
            for (int inicio = 0; inicio < calles.Cantidad; inicio++)
            {
                Recorrer(new List<int> { inicio }, ref mejor);
            }
            List<Vector3> ida = mejor.Select(i => calles.Nodo(i)).ToList();
            List<Vector3> ruta = new List<Vector3>(ida);
            for (int i = ida.Count - 2; i >= 0; i--)
            {
                ruta.Add(ida[i]);
            }
            return ruta;
        }

        private void Recorrer(List<int> actual, ref List<int> mejor)
        {
            if (actual.Count > mejor.Count)
            {
                mejor = new List<int>(actual);
            }
            foreach (int v in calles.Vecinos(actual[actual.Count - 1]))
            {
                if (!actual.Contains(v) && calles.TramoLibre(actual[actual.Count - 1], v))
                {
                    actual.Add(v);
                    Recorrer(actual, ref mejor);
                    actual.RemoveAt(actual.Count - 1);
                }
            }
        }

        // Dentro de los cierres del barrio solo queda un tramo libre entre dos cruces y los brazos de las calles que salen de ellos hasta los cierres:
        // la ruta del jugador entra por un brazo, dobla en el primer cruce, cruza el tramo y dobla al revés en el segundo (dos esquinas).
        private List<Vector3> RutaConVueltas()
        {
            for (int a = 0; a < calles.Cantidad; a++)
            {
                foreach (int b in calles.Vecinos(a))
                {
                    if (b <= a || !calles.TramoLibre(a, b))
                    {
                        continue;
                    }
                    Vector3 brazoA = Brazo(a, b, true);
                    Vector3 brazoB = Brazo(b, a, false);
                    if (brazoA != Vector3.zero && brazoB != Vector3.zero)
                    {
                        return new List<Vector3> { calles.Nodo(a) + brazoA * LargoBrazo, calles.Nodo(a), calles.Nodo(b), calles.Nodo(b) + brazoB * LargoBrazo };
                    }
                }
            }
            return new List<Vector3>();
        }

        // La dirección de un brazo que sale de "nodo" en perpendicular al tramo hacia "otro": hacia -z (sur) o hacia +z (norte).
        private Vector3 Brazo(int nodo, int otro, bool sur)
        {
            Vector3 tramo = calles.Nodo(otro) - calles.Nodo(nodo);
            tramo.y = 0f;
            foreach (int v in calles.Vecinos(nodo))
            {
                Vector3 dir = calles.Nodo(v) - calles.Nodo(nodo);
                dir.y = 0f;
                dir.Normalize();
                if (v != otro && Mathf.Abs(Vector3.Dot(dir, tramo.normalized)) < 0.1f && (sur ? dir.z < 0f : dir.z > 0f))
                {
                    return dir;
                }
            }
            return Vector3.zero;
        }

        private static void Foto(JugadorEnCarro jugador, IEnumerable<PatrullaMundo> patrullas, string nombre)
        {
            Vector3 centro = jugador.Posicion;
            PatrullaMundo cercana = patrullas.Where(p => p != null).OrderBy(p => (p.transform.position - centro).sqrMagnitude).FirstOrDefault();
            float distancia = 0f;
            if (cercana != null)
            {
                distancia = Vector3.ProjectOnPlane(cercana.transform.position - centro, Vector3.up).magnitude;
                if (distancia < 70f)
                {
                    centro = (centro + cercana.transform.position) / 2f;
                }
                else
                {
                    distancia = 0f;
                }
            }
            Camera cam = new GameObject("CamaraCaptura").AddComponent<Camera>();
            cam.farClipPlane = 400f;
            cam.fieldOfView = 60f;
            Vector3 atras = -Vector3.ProjectOnPlane(jugador.Adelante, Vector3.up).normalized;
            // Cámara alta: la primera posición desde la que ninguna línea a los puntos de interés la tapa algo sólido (techos, toldos, letreros).
            Vector3 elegida = centro + Vector3.up * (12f + distancia * 0.6f) + atras * (6f + distancia * 0.4f);
            bool libre = false;
            foreach (float altura in new[] { 12f, 16f, 22f, 30f, 40f })
            {
                foreach (float retroceso in new[] { 6f, 2f, 10f, 0f })
                {
                    Vector3 candidata = centro + Vector3.up * (altura + distancia * 0.6f) + atras * (retroceso + distancia * 0.4f);
                    if (!LineaTapada(candidata, centro) && !LineaTapada(candidata, jugador.Posicion + Vector3.up * 1.5f))
                    {
                        elegida = candidata;
                        libre = true;
                        break;
                    }
                }
                if (libre)
                {
                    break;
                }
            }
            cam.transform.position = elegida;
            cam.transform.LookAt(centro);
            CapturaPrueba.Guardar(cam, nombre);
            Object.DestroyImmediate(cam.gameObject);
        }

        // Algo sólido fijo entre la cámara y el punto (los cuerpos sueltos, como los carros, no cuentan).
        private static bool LineaTapada(Vector3 desde, Vector3 hasta)
        {
            Vector3 d = hasta - desde;
            foreach (RaycastHit h in Physics.RaycastAll(desde, d.normalized, d.magnitude - 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.attachedRigidbody == null && h.collider.GetComponentInParent<MechaCyberpunk.Trafico.Peatones.PeatonMundo>() == null)
                {
                    return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator PersecucionEnElBarrio_LasPatrullasManejanConLaFisicaDelCarro_SinTeletransportesNiGirosSobreSuEje()
        {
            Montar(true);
            yield return Correr(true, false);
        }

        [UnityTest]
        public IEnumerator PersecucionEnCuadricula_LasPatrullasDerrapanEnLasCurvasYFrenanComoUnCarro()
        {
            Montar(false);
            yield return Correr(false, true);
        }

        [UnityTest]
        public IEnumerator PersecucionEnLaCiudadDePrologo_LasPatrullasManejanConFisicaYAlcanzanAlJugador()
        {
            yield return MontarCiudad();
            yield return Correr(true, false, true);
        }

        private IEnumerator Correr(bool conCapturas, bool exigeDerrape, bool enCiudad = false)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, "..", "docs", "capturas", "t986"));
            GameObject prefabCarro = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarroJugador);
            List<Vector3> ruta = enCiudad ? RutaLargaDeIdaYVuelta() : RutaConVueltas();
            Assert.GreaterOrEqual(ruta.Count, 4, "La ruta del jugador tiene varias calles con vueltas");
            Vector3 primerRumbo = (ruta[1] - ruta[0]).normalized;
            Vector3 salida = ruta[0] + Vector3.Cross(Vector3.up, primerRumbo) * 1.6f;
            GameObject carroObjeto = Object.Instantiate(prefabCarro, salida + Vector3.up * 0.6f, Quaternion.LookRotation(primerRumbo));
            jugadorObjeto = carroObjeto;
            CarroViejo carroJugador = carroObjeto.GetComponent<CarroViejo>();
            Assert.IsNotNull(carroJugador);
            yield return null;
            JugadorEnCarro jugador = new JugadorEnCarro(carroJugador, config, ruta, enCiudad ? VelocidadJugadorCiudad : VelocidadJugador);
            Registro<IJugador>.Agregar(new PatrullaFisicaT707Pruebas.JugadorDoble { Raiz = jugador.Raiz });

            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(enCiudad ? raizCiudad : barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
            telemetriaObjeto = enCiudad ? new GameObject("Telemetria986") : barrio;
            Telemetria telemetria = telemetriaObjeto.AddComponent<Telemetria>();
            telemetria.Carro = carroJugador.ParametrosManejo;

            // Primero maneja un rato sin calor; luego sube el calor y salen las patrullas.
            yield return new WaitForSeconds(2f);
            float inicioPersecucion = Time.time;
            calor.Poner(NivelDeCalor);
            Time.timeScale = EscalaDeTiempo;
            int capturas = 0;
            float proximaCaptura = Time.time + 1f;
            float alcance = -1f;
            float fin = Time.time + (enCiudad ? SegundosCiudad : SegundosMaximos);
            int tope = enCiudad ? CapturasCiudad : MaximoDeCapturas;
            string prefijo = enCiudad ? "t986/ciudad-" : "t986/persecucion-";
            float separacion = enCiudad ? SegundosCiudad / (CapturasCiudad + 1) : SegundosEntreCapturas;
            while (Time.time < fin)
            {
                yield return null;
                if (conCapturas && capturas < tope && Time.time >= proximaCaptura)
                {
                    capturas++;
                    proximaCaptura = Time.time + separacion;
                    Foto(jugador, PatrullaMundo.Activas, prefijo + capturas + ".png");
                }
                if (alcance < 0f && PatrullaMundo.Activas.Any(p => p != null && p.PoliciasBajaron))
                {
                    alcance = Time.time - inicioPersecucion;
                }
                if (!enCiudad && alcance >= 0f && (capturas >= MaximoDeCapturas || !conCapturas) && Time.time > inicioPersecucion + alcance + SegundosTrasAlcanzar + (conCapturas ? 0f : SegundosExtraSinCapturas))
                {
                    break;
                }
            }
            Time.timeScale = 1f;
            if (conCapturas && capturas < tope)
            {
                capturas++;
                Foto(jugador, PatrullaMundo.Activas, prefijo + capturas + ".png");
            }
            telemetria.Choques = telemetria.Distintas.Sum(p => p != null ? p.GetComponent<Contactos>().Cantidad : 0);
            Debug.Log("[T-986] persecución: " + telemetria.Muestras + " muestras libres y " + telemetria.MuestrasConChoque + " con choque (" + telemetria.Choques + " choques) de " + telemetria.Distintas.Count + " patrullas; alcanzó al jugador a los " + alcance.ToString("F1")
                + " s; cinemáticas " + telemetria.Cinematicas + "; saltos malos " + telemetria.SaltosMalos + " (exceso máx " + telemetria.MaxExcesoSalto.ToString("F3") + " m); giros malos "
                + telemetria.GirosMalos + " (exceso máx " + telemetria.MaxExcesoGiro.ToString("F3") + " rad/s); giro quieta máx " + telemetria.MaxGiroQuieta.ToString("F2")
                + " °/s; lateral máx " + telemetria.MaxLateral.ToString("F2") + " m/s; frenado máx " + telemetria.MaxFrenado.ToString("F2") + " m/s2; rapidez máx "
                + telemetria.MaxRapidez.ToString("F2") + " m/s; volcadas " + telemetria.Volcadas + "; capturas " + capturas);

            Assert.GreaterOrEqual(telemetria.Distintas.Count, 1, "Salieron patrullas");
            Assert.Greater(telemetria.Muestras, 200, "Se midieron muchos pasos de física");
            Assert.AreEqual(0, telemetria.Cinematicas, "Ninguna patrulla es cinemática: todas manejan con física");
            Assert.AreEqual(0, telemetria.SaltosMalos, "Sin teletransportes: el salto de posición por paso nunca pasa de v*dt + 0.05 m (peor exceso " + telemetria.MaxExcesoSalto + ")");
            Assert.AreEqual(0, telemetria.GirosMalos, "El giro nunca pasa de rapidez / radio mínimo + margen (peor exceso " + telemetria.MaxExcesoGiro + " rad/s)");
            Assert.Less(telemetria.MaxGiroQuieta, GiroMaximoQuieta, "Una patrulla casi detenida no gira sobre su eje (°/s)");
            if (exigeDerrape)
            {
                Assert.Greater(telemetria.MaxLateral, DerrapeMinimo, "En alguna curva cerrada derrapa (velocidad lateral m/s)");
            }
            Assert.Greater(telemetria.MaxFrenado, FrenadoMinimo, "Hay al menos un frenado real de más de 3 m/s2");
            Assert.AreEqual(0, telemetria.Volcadas, "Ninguna patrulla se volteó");
            Assert.GreaterOrEqual(alcance, 0f, "Una patrulla alcanzó al jugador (llegó a la distancia para bajar policías)");
        }
    }
}
