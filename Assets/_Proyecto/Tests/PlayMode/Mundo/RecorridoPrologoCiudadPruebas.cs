using System.Collections;
using System.Diagnostics;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-093: el recorrido del prólogo en Ciudad_Prologo.unity con el jugador de Tony y entrada simulada (W, A, D, E, Espacio):
    /// de la puerta de la casa por la calle al callejón, al clandestino y al carro; en el carro por las esquinas de regreso; y de la puerta de la casa a la cama.
    /// Solo usa los registros de Core y los nombres de los prefabs de Tony, porque Mundo no referencia sus ensamblados.
    /// El jugador gira porque la prueba orienta su raíz; el ratón no se simula.
    /// </summary>
    public class RecorridoPrologoCiudadPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string IdAparicionPuerta = "Casa_Puerta";
        private const string IdAparicionCama = "Casa_Cama";
        private const string IdAparicionSalida = "Clandestino_Salida";
        private const string IdCarro = "Prologo_Carro";
        private const string IdEsquina1 = "Prologo_Esquina_1";
        private const string IdEsquina2 = "Prologo_Esquina_2";
        private const string NombreAnclaje = "Emboscada_Anclaje";
        private const float RadioLlegadaAPie = 1.2f;
        private const float RadioLlegadaCarro = 7f;
        private const float RadioParada = 5f;
        private const float RadioFrenteALaPuerta = 0.5f;
        private const float MetrosAntesDeLaPuerta = 3f;
        private const float MetrosDelPasilloALaCama = -0.25f;
        private const float MetrosDelPasilloAlSur = -1.5f;   // la grúa de motor del taller (T-419) ocupa el pasillo en el centro
        private const float SegundosPuertaAbriendo = 1.5f;
        private const float MaxSegundosPorTramoAPie = 60f;
        private const float MaxSegundosPorTramoCarro = 60f;
        private const float MetrosPorSegundoMediosMaximoAPie = 9f;
        private const float GradosSinGiro = 8f;
        private const float GradosParaFrenar = 35f;
        private const float MetrosPorSegundoRecto = 11f;
        private const float MetrosPorSegundoEnVuelta = 4.5f;
        private const float SegundosEntreTeclas = 0.3f;
        private const float MetrosDelCarroAlPunto = 3f;
        private const float MaxSegundosParaBajar = 10f;
        private const float SegundosRetroceso = 2f;
        private const float CentroCalleX = -16f;
        private const float SegundosDeLaCama = 0.5f;
        private const string NombreDirectorHistoria = "DirectorPandillas";
        private const string PrefijoCalaveras = "Calavera";
        private const float GradosParaReorientar = 15f;

        private Keyboard teclado;
        private int erroresEnConsola;
        private string primerError;

        private void AlRecibirLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                erroresEnConsola++;
                primerError ??= mensaje;
            }
        }

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            erroresEnConsola = 0;
            primerError = null;
            Application.logMessageReceived += AlRecibirLog;
        }

        public override void TearDown()
        {
            Release(teclado.wKey);
            Release(teclado.aKey);
            Release(teclado.dKey);
            Release(teclado.sKey);
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Application.logMessageReceived -= AlRecibirLog;
            base.TearDown();
        }

        private static IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            yield return null;
            // T-406/T-418: DirectorPandillas espera unos cuadros y entonces acuesta al jugador en la cama (Casa_Cama).
            yield return new WaitForSeconds(SegundosDeLaCama);
        }

        // T-418: la historia de la demo pone a los Calaveras frente a la casa al salir. Esta prueba es de geometría del recorrido:
        // se apaga el director de la historia y se quitan los Calaveras (por nombre; Mundo no referencia a Progresion).
        private static void QuitarLaHistoria()
        {
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (m.GetType().Name == NombreDirectorHistoria)
                {
                    m.enabled = false;
                }
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && t.name.StartsWith(PrefijoCalaveras))
                {
                    Object.Destroy(t.gameObject);
                }
            }
        }

        // La historia de la demo empieza en la cama: las pruebas del recorrido parten de la puerta de la casa.
        private static void IrALaPuertaDeLaCasa()
        {
            Jugador().Colocar(new Pose(Aparicion(IdAparicionPuerta).Pose.position + Vector3.up * 0.05f, Aparicion(IdAparicionPuerta).Pose.rotation));
        }

        private static T Buscar<T>(System.Collections.Generic.IReadOnlyList<T> lista, System.Func<T, bool> condicion, string que)
        {
            foreach (T item in lista)
            {
                if (condicion(item))
                {
                    return item;
                }
            }
            Assert.Fail("Falta " + que);
            return default;
        }

        private static IPuntoHistoria Historia(string id) =>
            Buscar(Registro<IPuntoHistoria>.Todos, p => p.Id == id, "el punto de historia " + id);

        private static IPuntoAparicion Aparicion(string id) =>
            Buscar(Registro<IPuntoAparicion>.Todos, p => p.Id == id, "la aparición " + id);

        // Hay varios carros en la ciudad (estacionados y de tráfico): el del prólogo es el que espera junto a Prologo_Carro.
        private static IMontable Carro()
        {
            IMontable mejor = null;
            float distanciaMejor = float.MaxValue;
            Vector3 punto = Historia(IdCarro).Pose.position;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                float d = m.Modo == ModoJugador.Carro ? Vector3.Distance(Plano(m.PuntoSubida), Plano(punto)) : float.MaxValue;
                if (d < distanciaMejor)
                {
                    distanciaMejor = d;
                    mejor = m;
                }
            }
            Assert.IsNotNull(mejor, "Falta el carro (IMontable en modo Carro)");
            return mejor;
        }

        // T-400 (tercera persona): W camina hacia donde mira la cámara, no el cuerpo. Colocar con la rotación deseada
        // reinicia el giro de la cámara del jugador (ReiniciarMovimiento) y así W va hacia el destino.
        private static void Orientar(Vector3 hacia)
        {
            Transform raiz = Jugador().Raiz;
            Jugador().Colocar(new Pose(raiz.position, Quaternion.LookRotation(Plano(hacia).normalized, Vector3.up)));
        }

        private static IJugador Jugador()
        {
            Assert.AreEqual(1, Registro<IJugador>.Todos.Count, "Un solo IJugador");
            return Registro<IJugador>.Todos[0];
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static GameObject EnEscena(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t.gameObject;
                    }
                }
            }
            return null;
        }

        // Camina con W hacia el destino girando la raíz del jugador hacia él; falla si no llega, si se queda trabado o si da un salto de posición.
        private IEnumerator Caminar(string tramo, Vector3 destino, float radio = RadioLlegadaAPie)
        {
            Transform raiz = Jugador().Raiz;
            Stopwatch reloj = Stopwatch.StartNew();
            float inicio = Time.time;
            Vector3 anterior = raiz.position;
            float recorrido = 0f;
            Orientar(Plano(destino) - Plano(raiz.position));
            yield return null;
            Press(teclado.wKey);
            while (Vector3.Distance(Plano(raiz.position), Plano(destino)) > radio)
            {
                Assert.Less(Time.time - inicio, MaxSegundosPorTramoAPie, tramo + ": no llegó a " + destino + "; quedó en " + raiz.position + " (¿muro o estorbo en el camino?)");
                Vector3 hacia = Plano(destino) - Plano(raiz.position);
                if (Vector3.Angle(Plano(raiz.forward), hacia) > GradosParaReorientar)
                {
                    Orientar(hacia);
                }
                yield return null;
                float paso = Vector3.Distance(raiz.position, anterior);
                recorrido += paso;
                anterior = raiz.position;
            }
            Release(teclado.wKey);
            yield return null;
            float media = recorrido / Mathf.Max(Time.time - inicio, 0.01f);
            Assert.Less(media, MetrosPorSegundoMediosMaximoAPie, tramo + ": la velocidad media delata un salto (¿se atravesó algo?)");
            Debug.Log("[Mundo] T-093 " + tramo + ": " + (Time.time - inicio).ToString("F1") + " s de juego, " + reloj.Elapsed.TotalSeconds.ToString("F1") + " s reales, velocidad media " + media.ToString("F1") + " m/s");
        }

        [UnityTest]
        public IEnumerator Escena_PublicaTodoLoQueElPrologoDeTonyBusca()
        {
            yield return Cargar();
            Assert.AreEqual(1, Registro<IJugador>.Todos.Count, "Un solo IJugador");
            int oyentes = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (oyente.isActiveAndEnabled)
                {
                    oyentes++;
                }
            }
            Assert.AreEqual(1, oyentes, "Un solo AudioListener activo");
            foreach (string id in new[] { IdCarro, IdEsquina1, IdEsquina2 })
            {
                Assert.IsNotNull(Historia(id));
            }
            foreach (string id in new[] { IdAparicionPuerta, IdAparicionCama, IdAparicionSalida })
            {
                Assert.IsNotNull(Aparicion(id));
            }
            Assert.IsNotNull(Carro());
            IGuardable reloj = Buscar(Registro<IGuardable>.Todos, g => g.ClaveGuardado == "reloj", "el reloj guardable");
            Assert.IsNotNull(reloj);
            foreach (string nombre in new[] { "DirectorPrologo", "MostradorPeleas", "MechaEstacionado", "Emboscada", "JugadorAPie", "CamaJugador", "EspejoJugador" })
            {
                Assert.IsNotNull(EnEscena(nombre), "Falta el prefab de Tony " + nombre + " en la escena");
            }
            // El carro espera junto a Prologo_Carro para que el objetivo del prólogo apunte a donde está.
            Assert.Less(Vector3.Distance(Plano(Carro().PuntoSubida), Plano(Historia(IdCarro).Pose.position)), MetrosDelCarroAlPunto + 3f);
            // T-418: la demo empieza con el jugador despierto en la cama.
            Assert.Less(Vector3.Distance(Plano(Jugador().Raiz.position), Plano(Aparicion(IdAparicionCama).Pose.position)), RadioLlegadaAPie);
            Assert.AreEqual(0, erroresEnConsola, "Errores en consola; el primero: " + primerError);
        }

        [UnityTest]
        public IEnumerator Recorrido_APie_CasaCallejonClandestinoYCarro_SinAtravesarNada()
        {
            yield return Cargar();
            QuitarLaHistoria();
            IrALaPuertaDeLaCasa();
            yield return null;
            Vector3 esquina1 = Historia(IdEsquina1).Pose.position;
            Vector3 esquina2 = Historia(IdEsquina2).Pose.position;
            Vector3 anclaje = EnEscena(NombreAnclaje).transform.position;
            Vector3 salida = Aparicion(IdAparicionSalida).Pose.position;
            Vector3 carro = Carro().PuntoSubida;

            yield return Caminar("1 casa a la calle (Esquina_2)", esquina2);
            yield return Caminar("2 calle al fondo del callejón (emboscada)", new Vector3(esquina2.x, 0f, anclaje.z));
            yield return Caminar("3 callejón", anclaje);
            Assert.Less(Vector3.Distance(Plano(Jugador().Raiz.position), Plano(anclaje)), RadioLlegadaAPie + 0.1f);
            yield return Caminar("4 de vuelta a la calle", new Vector3(esquina2.x, 0f, anclaje.z));
            yield return Caminar("5 calle al cruce (Esquina_1)", esquina1);
            yield return Caminar("6 cruce al clandestino (Clandestino_Salida)", salida);
            yield return Caminar("7 clandestino al carro", carro);
            Assert.AreEqual(ModoJugador.APie, Jugador().Modo);
            Assert.AreEqual(0, erroresEnConsola, "Errores en consola; el primero: " + primerError);
        }

        // El conductor de prueba: gira A o D según el ángulo al destino y acelera con W. Devuelve cuando llega, con el carro ya frenando.
        private void Teclas(bool acelera, bool frena, bool izquierda, bool derecha)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState(
                acelera ? Key.W : Key.None, frena ? Key.S : Key.None, izquierda ? Key.A : Key.None, derecha ? Key.D : Key.None));
        }

        private IEnumerator Manejar(string tramo, IMontable carro, Vector3 destino, float radio)
        {
            Transform cuerpo = carro.Asiento;
            float inicio = Time.time;
            Vector3 anterior = cuerpo.position;
            while (Vector3.Distance(Plano(cuerpo.position), Plano(destino)) > radio)
            {
                Assert.Less(Time.time - inicio, MaxSegundosPorTramoCarro, tramo + ": el carro no llegó a " + destino + "; quedó en " + cuerpo.position);
                Vector3 hacia = Plano(destino) - Plano(cuerpo.position);
                float angulo = Vector3.SignedAngle(Plano(cuerpo.forward), hacia, Vector3.up);
                float metrosPorSegundo = Vector3.Distance(cuerpo.position, anterior) / Mathf.Max(Time.deltaTime, 0.0001f);
                anterior = cuerpo.position;
                float objetivo = Mathf.Abs(angulo) > GradosParaFrenar ? MetrosPorSegundoEnVuelta : MetrosPorSegundoRecto;
                // Un solo evento de estado por cuadro: varios Press/Release en el mismo cuadro parten del estado viejo y se pisan.
                Teclas(metrosPorSegundo < objetivo, metrosPorSegundo >= objetivo, angulo < -GradosSinGiro, angulo > GradosSinGiro);
                yield return null;
            }
            Teclas(false, false, false, false);
            yield return null;   // el evento se procesa antes de pulsar otras teclas
            Debug.Log("[Mundo] T-093 " + tramo + ": " + (Time.time - inicio).ToString("F1") + " s de juego");
        }

        [UnityTest]
        public IEnumerator Recorrido_Regreso_EnCarroPorLasEsquinasYCamaDeLaCasa()
        {
            yield return Cargar();
            QuitarLaHistoria();
            IMontable carro = Carro();
            IJugador jugador = Jugador();
            jugador.Colocar(new Pose(carro.PuntoSubida + Vector3.up * 0.05f, jugador.Raiz.rotation));
            yield return new WaitForSeconds(SegundosEntreTeclas);
            PressAndRelease(teclado.eKey);
            yield return new WaitForSeconds(SegundosEntreTeclas);
            Assert.IsTrue(carro.Ocupado, "E junto a la puerta sube al carro");
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo);

            // Hay puestos del Tianguis (T-407) a lo largo de la calle, 3 m delante del carro: se retrocede para tener espacio
            // y se sale al centro de la calle antes de tirar hacia la esquina.
            Teclas(false, true, false, false);
            yield return new WaitForSeconds(SegundosRetroceso);
            Teclas(false, false, false, false);
            yield return null;
            yield return Manejar("8a carro al centro de la calle", carro, new Vector3(CentroCalleX, 0f, 0f), RadioParada);
            yield return Manejar("8 carro a la esquina 1", carro, Historia(IdEsquina1).Pose.position, RadioLlegadaCarro);
            yield return Manejar("9 esquina 1 a la esquina 2", carro, Historia(IdEsquina2).Pose.position, RadioLlegadaCarro);
            yield return Manejar("10 esquina 2 frente a la casa", carro, Historia(IdEsquina2).Pose.position, RadioParada);

            // Frena (Espacio) hasta que PuedeBajar y baja con E.
            Release(teclado.wKey);
            Press(teclado.spaceKey);
            float inicio = Time.time;
            while (!carro.PuedeBajar)
            {
                Assert.Less(Time.time - inicio, MaxSegundosParaBajar, "El carro no frenó para bajar");
                yield return null;
            }
            Release(teclado.spaceKey);
            PressAndRelease(teclado.eKey);
            yield return new WaitForSeconds(SegundosEntreTeclas);
            Assert.IsFalse(carro.Ocupado, "E baja del carro");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);

            // De la calle a la cama por la puerta de la casa.
            IPuerta puertaCasa = Buscar(Registro<IPuerta>.Todos, p => p.Lugar == IdLugar.Casa, "la puerta de la casa");
            Vector3 entradaCasa = Aparicion(IdAparicionPuerta).Pose.position;
            Vector3 ejeDeLaPuerta = Plano(puertaCasa.Posicion - entradaCasa).normalized;
            yield return Caminar("11a calle al eje de la puerta", entradaCasa - ejeDeLaPuerta * MetrosAntesDeLaPuerta, RadioFrenteALaPuerta);
            yield return Caminar("11b eje de la puerta a su frente", entradaCasa, RadioFrenteALaPuerta);
            Orientar(puertaCasa.Posicion - Jugador().Raiz.position);
            yield return null;
            yield return null;
            PressAndRelease(teclado.eKey);   // la puerta de la casa abre con E
            yield return null;
            yield return new WaitForSeconds(SegundosPuertaAbriendo);
            Assert.IsTrue(puertaCasa.EstaAbierta, "E frente a la puerta de la casa la abre");
            Vector3 centroDeLaCasa = EnEscena("Lugar_Casa").transform.position;
            yield return Caminar("12 puerta al centro de la casa", centroDeLaCasa);
            // La mesa con sus bancos queda a un lado: se rodea por el pasillo entre los bancos y los pies de la cama.
            Vector3 cama = Aparicion(IdAparicionCama).Pose.position;
            Vector3 pasillo = new Vector3(cama.x + MetrosDelPasilloALaCama, 0f, centroDeLaCasa.z + MetrosDelPasilloAlSur);
            yield return Caminar("13 centro de la casa al pasillo de la cama", pasillo, RadioFrenteALaPuerta);
            yield return Caminar("14 pasillo a los pies de la cama", cama, RadioFrenteALaPuerta);
            Assert.AreEqual(0, erroresEnConsola, "Errores en consola; el primero: " + primerError);
        }
    }
}
