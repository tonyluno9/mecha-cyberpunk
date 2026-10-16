using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-078: la cámara libre camina de la casa al carro viejo, lo sube, un piloto de prueba lo maneja hasta el clandestino
    /// dando una vuelta sin atravesar nada, lo baja y camina hasta la puerta. Todo con entrada simulada, de noche.
    /// </summary>
    public class RecorridoCarroPruebas : InputTestFixture
    {
        private const string RutaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        private const string RutaConfigCamara = "Assets/_Proyecto/Mundo/Datos/ConfigCamaraLibre_Pruebas.asset";
        private const string PuntoCochera = "Casa_Cochera";
        private const string PuntoPuertaCasa = "Casa_Puerta";
        private const string PuntoPuertaClandestino = "Clandestino_Puerta";
        private const float HoraNoche = 22f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        // Caminata de la cámara libre.
        private const float AnguloParaAvanzar = 25f;
        private const float LimiteCaminata = 20f;
        private const float DistanciaAlSubir = 1.2f;
        private const float DistanciaAlLlegar = 1.5f;
        private const float DistanciaEntradaPrueba = 6f;

        // Piloto.
        private const float RadioDeLlegada = 4f;
        private const float MaximaEnRuta = 14f;
        private const float MirarMasAlla = 15f;
        private const float DistanciaParada = 2f;
        private const float DesaceleracionPiloto = 3.5f;
        private const float AnguloCurva = 15f;
        private const float RapidezEnCurva = 5f;
        private const float AlturaRayo = 0.8f;
        private const float DistanciaRayo = 8f;
        private const float RapidezMinimaParaFrenar = 0.5f;
        private const float MargenRapidez = 0.5f;
        private const float RapidezDetenido = 0.3f;
        private const float LimiteManejo = 60f;
        private const float GiroMinimo = 80f;
        private const float AlturaMaximaEnCalle = 0.12f;
        private const float PenetracionMaxima = 0.1f;
        private const float FrameMaximo = 0.1f;
        private const float RapidezBajar = 0.5f;
        private const float ColchonPenetracion = 0.5f;
        private const int MaxVecinos = 64;

        // Medición de la calle principal (m).
        private const float InicioCalle = -110f;
        private const float FinCalle = 110f;
        private const float LimiteMedicion = 40f;
        private const float AlturaInicial = 0.1f;

        // Zona despejada alrededor del carro estacionado (m).
        private const float DistanciaMaximaCochera = 6f;
        private const float DespejeAlrededor = 8f;

        private struct Paso
        {
            public readonly Vector2 Punto;
            public readonly float Rapidez;

            public Paso(float x, float z, float rapidez)
            {
                Punto = new Vector2(x, z);
                Rapidez = rapidez;
            }
        }

        // De la cochera al centro de la calle norte-sur, al norte, vuelta a la derecha y al este hasta la puerta del clandestino.
        private static readonly Paso[] Ruta =
        {
            new Paso(-47f, -45f, 8f),
            new Paso(-47f, -25f, 14f),
            new Paso(-47f, -9f, 6f),
            new Paso(-43f, -2f, 5f),
            new Paso(-34f, 1.5f, 8f),
            new Paso(-24f, 3.2f, 10f),
            new Paso(-10f, 3.2f, 6f),
            new Paso(0f, 3.2f, 0f)
        };

        // Mide, en cada FixedUpdate, la mayor penetración de los colliders del carro contra todo lo demás.
        private sealed class MedidorPenetracion : MonoBehaviour
        {
            public float Maxima;
            private Collider[] propios;
            private readonly Collider[] vecinos = new Collider[MaxVecinos];

            private void Awake()
            {
                propios = GetComponentsInChildren<Collider>();
            }

            private void FixedUpdate()
            {
                foreach (Collider propio in propios)
                {
                    Bounds caja = propio.bounds;
                    int cuenta = Physics.OverlapBoxNonAlloc(caja.center, caja.extents + Vector3.one * ColchonPenetracion, vecinos,
                        Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < cuenta; i++)
                    {
                        Collider otro = vecinos[i];
                        if (otro.isTrigger || otro.transform.IsChildOf(transform) || EsPiso(otro) || otro.GetComponentInParent<CamaraLibre>() != null)
                        {
                            continue;
                        }
                        if (Physics.ComputePenetration(propio, propio.transform.position, propio.transform.rotation,
                                otro, otro.transform.position, otro.transform.rotation, out Vector3 _, out float distancia))
                        {
                            Maxima = Mathf.Max(Maxima, distancia);
                        }
                    }
                }
            }

            private static bool EsPiso(Collider otro)
            {
                return otro.name == "Asfalto" || otro.name == "Banqueta";
            }
        }

        // Cuenta, cada frame, el frame más largo y el mínimo y máximo de AudioListener prendidos.
        private sealed class MedidorFrames : MonoBehaviour
        {
            public float FrameMasLargo;
            public int ListenersMinimo = int.MaxValue;
            public int ListenersMaximo;

            private void Update()
            {
                FrameMasLargo = Mathf.Max(FrameMasLargo, Time.unscaledDeltaTime);
                int prendidos = 0;
                foreach (AudioListener oyente in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                {
                    if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                    {
                        prendidos++;
                    }
                }
                ListenersMinimo = Mathf.Min(ListenersMinimo, prendidos);
                ListenersMaximo = Mathf.Max(ListenersMaximo, prendidos);
            }
        }

        private Keyboard teclado;
        private Mouse raton;
        private float sensibilidad;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            sensibilidad = AssetDatabase.LoadAssetAtPath<ConfigCamaraLibre>(RutaConfigCamara).Sensibilidad;
            errores.Clear();
            Application.logMessageReceived += RecibirLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RecibirLog;
            base.TearDown();
        }

        private void RecibirLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(tipo + ": " + mensaje);
            }
        }

        private static IEnumerator CargarEscena(string ruta)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            Physics.SyncTransforms();
        }

        private static void PonerNoche()
        {
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            Assert.IsNotNull(reloj, "La escena debe traer un RelojMundo");
            reloj.Pausar(true);
            reloj.AvanzarHasta(HoraNoche);
        }

        private static Vector3 Posicion(string nombre)
        {
            GameObject objeto = GameObject.Find(nombre);
            Assert.IsNotNull(objeto, "Falta " + nombre + " en la escena");
            return objeto.transform.position;
        }

        private void Soltar(params Key[] teclas)
        {
            foreach (Key tecla in teclas)
            {
                Release(teclado[tecla]);
            }
        }

        // Camina con W y gira con el ratón hacia el objetivo (plano horizontal) hasta quedar a 'distancia' de él.
        private IEnumerator Caminar(CamaraLibre camara, Vector3 objetivo, float distancia)
        {
            float limite = Time.time + LimiteCaminata;
            bool avanzando = false;
            while (Time.time < limite)
            {
                Vector3 falta = objetivo - camara.Raiz.position;
                falta.y = 0f;
                if (falta.magnitude <= distancia)
                {
                    break;
                }
                float deseado = Mathf.Atan2(falta.x, falta.z) * Mathf.Rad2Deg;
                float error = Mathf.DeltaAngle(camara.Yaw, deseado);
                Set(raton.delta, new Vector2(error / sensibilidad, 0f));
                bool debeAvanzar = Mathf.Abs(error) < AnguloParaAvanzar;
                if (debeAvanzar && !avanzando)
                {
                    Press(teclado.wKey);
                }
                else if (!debeAvanzar && avanzando)
                {
                    Release(teclado.wKey);
                }
                avanzando = debeAvanzar;
                yield return null;
            }
            if (avanzando)
            {
                Release(teclado.wKey);
            }
            // Un último giro para quedar mirando al objetivo.
            Vector3 resto = objetivo - camara.Raiz.position;
            resto.y = 0f;
            Set(raton.delta, new Vector2(Mathf.DeltaAngle(camara.Yaw, Mathf.Atan2(resto.x, resto.z) * Mathf.Rad2Deg) / sensibilidad, 0f));
            yield return null;
            yield return null;
        }

        private IEnumerator PulsarE()
        {
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
        }

        // Piloto de prueba: A/D según el ángulo al siguiente punto, W/S según la rapidez deseada y un rayo al frente.
        private sealed class Piloto
        {
            private readonly RaycastHit[] impactos = new RaycastHit[16];
            private bool w;
            private bool s;
            private bool a;
            private bool d;

            public int Paso;
            public float GiroAcumulado;
            private float yawAnterior = float.NaN;

            public void Manejar(RecorridoCarroPruebas prueba, CarroViejo carro)
            {
                Transform t = carro.transform;
                if (!float.IsNaN(yawAnterior))
                {
                    GiroAcumulado += Mathf.Abs(Mathf.DeltaAngle(yawAnterior, t.eulerAngles.y));
                }
                yawAnterior = t.eulerAngles.y;

                bool ultimo = Paso == Ruta.Length - 1;
                Vector3 objetivo = new Vector3(Ruta[Paso].Punto.x, t.position.y, Ruta[Paso].Punto.y);
                Vector3 falta = objetivo - t.position;
                falta.y = 0f;
                if (!ultimo && falta.magnitude < RadioDeLlegada)
                {
                    Paso++;
                    return;
                }
                Vector3 rumbo = ultimo ? falta + Vector3.right * MirarMasAlla : falta;
                float angulo = Vector3.SignedAngle(t.forward, rumbo, Vector3.up);
                float deseada = RapidezSegura(falta.magnitude);
                if (Mathf.Abs(angulo) > AnguloCurva)
                {
                    deseada = Mathf.Min(deseada, RapidezEnCurva);
                }
                bool peligro = HayObstaculo(carro);
                bool acelerar = carro.Velocidad < deseada && !peligro;
                bool frenar = (carro.Velocidad > deseada + MargenRapidez || peligro) && carro.Velocidad > RapidezMinimaParaFrenar;
                Aplicar(prueba, acelerar, frenar, angulo < -AnguloCurva * 0.2f, angulo > AnguloCurva * 0.2f);
            }

            // La rapidez más alta desde la que aún se alcanza la rapidez de cada punto restante frenando con la desaceleración del piloto.
            private float RapidezSegura(float distanciaAlPaso)
            {
                float segura = float.MaxValue;
                float distancia = distanciaAlPaso;
                for (int i = Paso; i < Ruta.Length; i++)
                {
                    if (i > Paso)
                    {
                        distancia += Vector2.Distance(Ruta[i - 1].Punto, Ruta[i].Punto);
                    }
                    float frenando = i == Ruta.Length - 1 ? Mathf.Max(0f, distancia - DistanciaParada) : distancia;
                    float limite = Mathf.Sqrt(Ruta[i].Rapidez * Ruta[i].Rapidez + 2f * DesaceleracionPiloto * frenando);
                    segura = Mathf.Min(segura, limite);
                }
                return Mathf.Min(segura, MaximaEnRuta);
            }

            public void Soltar(RecorridoCarroPruebas prueba)
            {
                Aplicar(prueba, false, false, false, false);
            }

            private bool HayObstaculo(CarroViejo carro)
            {
                Transform t = carro.transform;
                int cuenta = Physics.RaycastNonAlloc(t.position + Vector3.up * AlturaRayo, t.forward, impactos, DistanciaRayo, ~0,
                    QueryTriggerInteraction.Ignore);
                for (int i = 0; i < cuenta; i++)
                {
                    if (!impactos[i].collider.transform.IsChildOf(t))
                    {
                        return true;
                    }
                }
                return false;
            }

            private void Aplicar(RecorridoCarroPruebas prueba, bool nuevoW, bool nuevoS, bool nuevoA, bool nuevoD)
            {
                Cambiar(prueba, Key.W, ref w, nuevoW);
                Cambiar(prueba, Key.S, ref s, nuevoS);
                Cambiar(prueba, Key.A, ref a, nuevoA);
                Cambiar(prueba, Key.D, ref d, nuevoD);
            }

            private static void Cambiar(RecorridoCarroPruebas prueba, Key tecla, ref bool actual, bool nuevo)
            {
                if (actual == nuevo)
                {
                    return;
                }
                actual = nuevo;
                if (nuevo)
                {
                    prueba.Press(prueba.teclado[tecla]);
                }
                else
                {
                    prueba.Release(prueba.teclado[tecla]);
                }
            }
        }

        [UnityTest]
        public IEnumerator Recorrido_DeLaCasaAlClandestino_SubeManejaDaLaVueltaYBaja()
        {
            yield return CargarEscena(RutaPruebas);
            PonerNoche();
            CamaraLibre camara = Object.FindFirstObjectByType<CamaraLibre>();
            CarroViejo carro = Object.FindFirstObjectByType<CarroViejo>();
            Assert.IsNotNull(camara);
            Assert.IsNotNull(carro);
            Vector3 puertaClandestino = Posicion(PuntoPuertaClandestino);
            Assert.AreEqual(ModoJugador.APie, camara.Modo);
            Assert.Less(Vector3.Distance(camara.Raiz.position, Posicion(PuntoPuertaCasa)), 1f, "La cámara debe empezar en la puerta de la casa");

            // a) Caminar hasta el punto de subida y subir con E.
            yield return Caminar(camara, carro.PuntoSubida, DistanciaAlSubir);
            yield return PulsarE();
            Assert.AreEqual(ModoJugador.Carro, camara.Modo, "No subió al carro con E");
            Assert.IsTrue(carro.Ocupado);

            // b) Manejar con el piloto hasta el clandestino.
            MedidorPenetracion penetracion = carro.gameObject.AddComponent<MedidorPenetracion>();
            MedidorFrames frames = camara.gameObject.AddComponent<MedidorFrames>();
            Piloto piloto = new Piloto();
            float inicio = Time.time;
            float limite = inicio + LimiteManejo;
            bool llego = false;
            int ultimoRegistro = -1;
            while (Time.time < limite)
            {
                piloto.Manejar(this, carro);
                yield return null;
                if (Mathf.FloorToInt((Time.time - inicio) * 0.4f) != ultimoRegistro)
                {
                    ultimoRegistro = Mathf.FloorToInt((Time.time - inicio) * 0.4f);
                    Debug.Log("[Piloto] t=" + (Time.time - inicio).ToString("F1") + " paso " + piloto.Paso + " pos " + carro.transform.position + " vel " + carro.Velocidad.ToString("F1") + " yaw " + carro.transform.eulerAngles.y.ToString("F0"));
                }
                bool final = piloto.Paso == Ruta.Length - 1;
                if (final && carro.Rapidez < RapidezDetenido
                    && Vector3.Distance(carro.transform.position, puertaClandestino) < DistanciaEntradaPrueba)
                {
                    llego = true;
                    break;
                }
            }
            float segundos = Time.time - inicio;
            piloto.Soltar(this);
            Debug.Log("[Medicion] T-078 casa al clandestino con el piloto: " + segundos.ToString("F1") + " s, giro " + piloto.GiroAcumulado.ToString("F0") + " grados");
            Assert.IsTrue(llego, "El carro no llegó detenido a menos de 6 m del clandestino en " + LimiteManejo + " s (paso " + piloto.Paso + ", a "
                + Vector3.Distance(carro.transform.position, puertaClandestino).ToString("F1") + " m)");
            Assert.GreaterOrEqual(piloto.GiroAcumulado, GiroMinimo, "No dio la vuelta");
            Assert.Less(carro.transform.position.y, AlturaMaximaEnCalle, "El carro se subió a la banqueta");

            // c) Bajar detenido y caminar hasta la puerta.
            float espera = Time.time + 3f;
            while (carro.Rapidez > RapidezBajar && Time.time < espera)
            {
                yield return null;
            }
            yield return PulsarE();
            Assert.AreEqual(ModoJugador.APie, camara.Modo, "No bajó del carro con E");
            Assert.IsFalse(carro.Ocupado);
            yield return Caminar(camara, puertaClandestino, DistanciaAlLlegar);
            Assert.Less(Vector3.Distance(camara.Raiz.position, puertaClandestino), DistanciaAlLlegar + 0.5f, "No llegó caminando a la entrada");

            Assert.Less(penetracion.Maxima, PenetracionMaxima, "El carro atravesó algo: " + penetracion.Maxima.ToString("F3") + " m");
            Assert.Less(frames.FrameMasLargo, FrameMaximo, "Hubo un frame de " + frames.FrameMasLargo + " s");
            Assert.AreEqual(1, frames.ListenersMinimo, "Hubo un momento sin AudioListener");
            Assert.AreEqual(1, frames.ListenersMaximo, "Hubo más de un AudioListener a la vez");
            Assert.IsEmpty(errores, "Errores en la consola: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Medicion_CallePrincipalConWAFondo_ReportaElTiempo()
        {
            yield return CargarEscena(RutaPruebas);
            PonerNoche();
            CamaraLibre camara = Object.FindFirstObjectByType<CamaraLibre>();
            camara.gameObject.SetActive(false);
            CarroViejo carro = Object.FindFirstObjectByType<CarroViejo>();
            carro.transform.SetPositionAndRotation(new Vector3(InicioCalle, AlturaInicial, 0f), Quaternion.LookRotation(Vector3.right, Vector3.up));
            carro.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            yield return null;
            yield return null;

            carro.TomarControl(true);
            Press(teclado.wKey);
            float inicio = Time.time;
            float limite = inicio + LimiteMedicion;
            while (carro.transform.position.x < FinCalle && Time.time < limite)
            {
                yield return null;
            }
            float segundos = Time.time - inicio;
            Release(teclado.wKey);
            Debug.Log("[Medicion] T-078 calle principal con W a fondo, " + (FinCalle - InicioCalle) + " m: " + segundos.ToString("F1") + " s");
            Assert.GreaterOrEqual(carro.transform.position.x, FinCalle, "No cruzó la calle principal con W");
            Assert.IsEmpty(errores, "Errores en la consola: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Cochera_EnCiudadYPruebas_TieneUnSoloCarroYSinObjetosEstorbando(
            [ValueSource(nameof(Escenas))] string ruta)
        {
            yield return CargarEscena(ruta);
            Vector3 cochera = Posicion(PuntoCochera);
            int montables = 0;
            CarroViejo carro = null;
            foreach (MonoBehaviour comportamiento in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (comportamiento is IMontable)
                {
                    montables++;
                    carro = comportamiento as CarroViejo;
                }
            }
            Assert.AreEqual(1, montables, "Debe haber exactamente un IMontable en " + ruta);
            Assert.IsNotNull(carro, "El IMontable debe ser el carro viejo");
            Assert.Less(Vector3.Distance(carro.transform.position, cochera), DistanciaMaximaCochera, "El carro no está en la cochera");
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool estorba = t.name.StartsWith("Carro_") || t.name.StartsWith("Bote_");
                if (estorba && !t.IsChildOf(carro.transform))
                {
                    Assert.GreaterOrEqual(Vector3.Distance(t.position, carro.transform.position), DespejeAlrededor,
                        t.name + " está a menos de " + DespejeAlrededor + " m del carro en " + ruta);
                }
            }
        }

        private static IEnumerable<string> Escenas()
        {
            yield return RutaPruebas;
            yield return RutaCiudad;
        }
    }
}
