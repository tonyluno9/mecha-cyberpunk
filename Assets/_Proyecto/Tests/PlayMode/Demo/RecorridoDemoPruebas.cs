using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.Progresion.Sonido;
using MechaCyberpunk.Tests.UI;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Demo
{
    /// <summary>
    /// T-474: la demo como un jugador nuevo, de principio a fin y sin atajos de jugabilidad: título, OPCIONES, CRÉDITOS, NUEVA PARTIDA, creación,
    /// despertar en Suburbios, pelea a puños, pistola, CHATO, pausa y guardado, salir al título y CONTINUAR, DOC, INVENTOR, Tianguis y su jefe,
    /// Neón Alto con EL HACKER y CONTINUARÁ. Solo se usan teclas y ratón simulados (Input System); el jugador camina hacia lo que dice el
    /// marcador del objetivo (la posición que resuelve <see cref="UbicacionObjetivo"/>), apunta con el ratón y dispara. Si un tramo se traba, se
    /// anota el tope en Logs/t474-bugs.txt (con captura docs/capturas/T-474-bug-N.png) y SOLO ENTONCES se coloca al jugador junto al objetivo para
    /// poder probar el tramo siguiente. Salidas: Logs/t474-tiempos.txt (tiempo, FPS y ambiente por tramo), Logs/t474-bugs.txt y Logs/t474-consola.txt.
    /// Correr (Unity cerrado): Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter MechaCyberpunk.Tests.Demo.RecorridoDemoPruebas (T474_HASTA=3, 4, 9, 10 u 11 lo corta tras ese tramo; T474_SIN_CONTINUAR=1 no sale al título; T474_DESDE=12 solo depura el final: salta a Neón Alto).
    /// T-476: el bot hornea un NavMesh en ejecución (NavMeshBuilder con los colliders sólidos y quietos), sigue su camino de esquina en esquina con WASD, prueba las puertas con E (si no abren, las excluye y vuelve a hornear), compra PUÑOS I y PISTOLA II en el CIC y pelea con visión, ráfagas, R, Q y retirada para que reaparezcan los grupos.
    /// </summary>
    public partial class RecorridoDemoPruebas : InputTestFixture
    {
        // T-475: la ruta por rejilla se usa desde cerca (EL DOC está tras una pared a 6 m en línea recta).
        private const float DistanciaRutaMinima = 4f;
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreJugador = "ALAN";
        private const float VelocidadMaxima = 1f;
        private const float DanoMatar = 100000f;
        private const float DistanciaInteraccion = 1.6f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private readonly List<string> tiempos = new List<string>();
        private readonly List<string> bugs = new List<string>();
        private readonly Dictionary<string, int> consola = new Dictionary<string, int>();
        private int numeroBug;
        private string tramoActual = "";
        private double inicioTramo;
        private int cuadrosTramo;
        private float peorCuadro;
        private float gradosPorUnidad = 0.1f;
        private float ultimoEnviado;
        private float ultimoGiro;
        private bool hayUltimo;
        private int muertes;
        // T-514: ventaja solo de la prueba (el juego no cambia) en las peleas con pistola del Tianguis y del jefe: si la vida baja de
        // UmbralVentaja, se cura. El bot sigue cubriéndose al 50 % como un jugador; esto solo evita el KO que le quita respeto.
        private const float UmbralVentaja = 0.45f;
        private bool ventajaDePrueba;
        private int curasDePrueba;
        private bool rescateUsado;
        // T-514: un blanco que se esconde tras una cubierta no se persigue de frente para siempre: se le flanquea y, si sigue sin verse, se deja un rato.
        private const float SegundosSinVistaParaDejar = 8f;
        private const float SegundosBlancoDejado = 10f;
        private const float DistanciaFlanqueo = 8f;
        private const float GradosFlanqueo = 70f;
        private readonly Dictionary<PandilleroAPie, float> dejadosHasta = new Dictionary<PandilleroAPie, float>();
        private PandilleroAPie sinVistaDe;
        private float sinVistaDesde;
        private float flancoSigno = 1f;
        private float proximoCambioFlanco;
        private int trazasMirada;
        private float finRumbo;
        private float finE;
        private PandilleroAPie blanco;
        private float desvioHasta;
        private float proximaMedida;
        private Vector3 posMedida;
        private float proximaTraza;
        private float proximaTraza2;
        private bool sinE;
        private Vector3 zonaCentro;
        private Vector3 retirada;
        private float desvio;
        private float ladoRumbo = 1f;

        // ---------- infraestructura ----------

        public override void Setup()
        {
            base.Setup();
            ventajaDePrueba = false;
            curasDePrueba = 0;
            rescateUsado = false;
            dejadosHasta.Clear();
            sinVistaDe = null;
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t474_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
            FlujoPartida.TituloEnEditor = true;
            Application.logMessageReceived += AlLog;
            Directory.CreateDirectory("Logs");
            Directory.CreateDirectory("docs/capturas");
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlLog;
            FlujoPartida.TituloEnEditor = false;
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            try
            {
                Directory.Delete(carpeta, true);
            }
            catch (Exception)
            {
                // La carpeta temporal se queda; no importa.
            }
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    asset.Disable();
                    asset.devices = dispositivos;
                    asset.devices = null;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private void AlLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Log)
            {
                return;
            }
            string clave = "[" + tipo + "] [" + tramoActual + "] " + (mensaje.Length > 220 ? mensaje.Substring(0, 220) : mensaje);
            consola.TryGetValue(clave, out int n);
            consola[clave] = n + 1;
        }

        private void Escribir(string archivo, IEnumerable<string> lineas)
        {
            File.WriteAllLines(Path.Combine("Logs", archivo), lineas, new UTF8Encoding(false));
        }

        private void GuardarSalidas()
        {
            Escribir("t474-tiempos.txt", tiempos);
            Escribir("t474-bugs.txt", bugs);
            List<string> lineas = new List<string>();
            foreach (KeyValuePair<string, int> par in consola)
            {
                lineas.Add(par.Value + "x " + par.Key);
            }
            Escribir("t474-consola.txt", lineas);
        }

        private void IniciarTramo(string nombre)
        {
            tramoActual = nombre;
            inicioTramo = Time.realtimeSinceStartupAsDouble;
            cuadrosTramo = 0;
            peorCuadro = 0f;
        }

        private void TerminarTramo(string nota = "")
        {
            double segundos = Time.realtimeSinceStartupAsDouble - inicioTramo;
            float fps = segundos > 0 ? (float)(cuadrosTramo / segundos) : 0f;
            GestorSonidos gestor = Object.FindFirstObjectByType<GestorSonidos>();
            string ambiente = gestor != null ? gestor.AmbienteDeseado : "(sin gestor)";
            string linea = tramoActual + " | " + segundos.ToString("F1") + " s | " + fps.ToString("F0") + " FPS medios | peor cuadro "
                + (peorCuadro * 1000f).ToString("F0") + " ms | ambiente " + ambiente + (nota.Length > 0 ? " | " + nota : "");
            tiempos.Add(linea);
            Debug.Log("[T-474] " + linea);
            GuardarSalidas();
        }

        private bool comprando;
        private bool escTienda;

        private void Cuadro()
        {
            cuadrosTramo++;
            peorCuadro = Mathf.Max(peorCuadro, Time.unscaledDeltaTime);
            // Un E de más junto a un mostrador abre su tienda (modal): fuera de las compras, el jugador la cierra con Esc.
            if (!comprando && PilaPantallas.Arriba is MechaCyberpunk.UI.Tiendas.PantallaTienda)
            {
                if (!escTienda)
                {
                    Presionar(teclado.escapeKey);
                    escTienda = true;
                }
            }
            else if (escTienda)
            {
                Soltar(teclado.escapeKey);
                escTienda = false;
            }
        }

        private IEnumerator Siguiente()
        {
            yield return null;
            Cuadro();
        }

        private void Bug(string titulo, string detalle, string carpetaDueña, bool captura = true)
        {
            numeroBug++;
            string foto = "";
            if (captura)
            {
                try
                {
                    IJugador j = Registro<IJugador>.Actual;
                    Camera cam = j != null && j.Camara != null ? j.Camara : Camera.main;
                    if (cam != null)
                    {
                        foto = "docs/capturas/T-474-bug-" + numeroBug + ".png";
                        CapturaUI.Capturar(foto, cam);
                    }
                }
                catch (Exception e)
                {
                    foto = "(captura falló: " + e.Message + ")";
                }
            }
            string linea = "BUG " + numeroBug + " | " + titulo + " | " + detalle + " | tramo " + tramoActual + " | carpeta " + carpetaDueña + " | " + foto;
            bugs.Add(linea);
            Debug.Log("[T-474][BUG] " + linea);
            GuardarSalidas();
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla, int cuadrosDespues = 2)
        {
            Presionar(tecla);
            yield return null;
            Soltar(tecla);
            for (int i = 0; i < cuadrosDespues; i++)
            {
                yield return null;
            }
        }

        private IEnumerator Escribir(string texto)
        {
            foreach (char letra in texto)
            {
                InputSystem.QueueTextEvent(teclado, letra);
            }
            yield return null;
            yield return null;
        }

        private IEnumerator Segundos(float s)
        {
            float fin = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return Siguiente();
            }
        }

        // ---------- el jugador: caminar, mirar, golpear, disparar ----------

        private static IJugador Jugador => Registro<IJugador>.Actual;
        private static ControladorAPie Controlador => Jugador != null ? Jugador.Raiz.GetComponentInChildren<ControladorAPie>() : null;
        private static DirectorPandillas Director => Object.FindFirstObjectByType<DirectorPandillas>();

        private static string TextoObjetivo
        {
            get
            {
                IObjetivos o = Registro<IObjetivos>.Actual;
                return o != null && o.HayObjetivo ? o.Actual.Texto : "(sin objetivo)";
            }
        }

        // Lo que enseña el marcador: la posición del objetivo actual.
        private static bool PosicionMarcador(out Vector3 posicion)
        {
            posicion = Vector3.zero;
            IObjetivos o = Registro<IObjetivos>.Actual;
            return o != null && o.HayObjetivo && UbicacionObjetivo.Resolver(o.Actual, Registro<ILugar>.Todos, out posicion);
        }

        private static float Rumbo(Vector3 desde, Vector3 hasta)
        {
            Vector3 d = hasta - desde;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // Los eventos de estado de InputTestFixture.Press/Release se pisan entre sí dentro de un mismo cuadro (solo gana el último) y los
        // eventos delta no sirven con teclas: se guarda lo que está pulsado y cada cambio manda el estado completo del teclado y del ratón.
        private readonly HashSet<Key> teclasPulsadas = new HashSet<Key>();
        private int botonesRaton;
        private Vector2 deltaRaton;

        private void Presionar(ButtonControl c) => Cambiar(c, true);

        private void Soltar(ButtonControl c) => Cambiar(c, false);

        private void Cambiar(ButtonControl c, bool pulsada)
        {
            if (c is KeyControl tecla)
            {
                if (pulsada ? teclasPulsadas.Add(tecla.keyCode) : teclasPulsadas.Remove(tecla.keyCode))
                {
                    InputSystem.QueueStateEvent(teclado, new KeyboardState(new List<Key>(teclasPulsadas).ToArray()));
                }
                return;
            }
            int bit = c == raton.leftButton ? 1 << (int)MouseButton.Left : c == raton.rightButton ? 1 << (int)MouseButton.Right : 1 << (int)MouseButton.Middle;
            botonesRaton = pulsada ? botonesRaton | bit : botonesRaton & ~bit;
            EnviarRaton();
        }

        private void MoverRaton(Vector2 delta)
        {
            if (delta == Vector2.zero && deltaRaton == Vector2.zero)
            {
                return;
            }
            deltaRaton = delta;
            EnviarRaton();
        }

        private void EnviarRaton()
        {
            MouseState estado = new MouseState { position = raton.position.ReadValue(), delta = deltaRaton, buttons = (ushort)botonesRaton };
            InputSystem.QueueStateEvent(raton, estado);
        }

        private void SoltarTeclas()
        {
            Soltar(teclado.wKey);
            Soltar(teclado.aKey);
            Soltar(teclado.sKey);
            Soltar(teclado.dKey);
            Soltar(teclado.leftShiftKey);
            Soltar(teclado.spaceKey);
        }

        // Gira la cámara con el ratón hacia un rumbo (aprende cuántos grados da cada unidad de delta). Devuelve el error que queda.
        private float MirarHacia(float rumbo, float fraccion, float errorInclinacion = 0f)
        {
            ControladorAPie c = Controlador;
            if (c == null)
            {
                return 0f;
            }
            if (hayUltimo && Mathf.Abs(ultimoEnviado) > 0.01f)
            {
                float real = Mathf.DeltaAngle(ultimoGiro, c.GiroCamara);
                float estimada = real / ultimoEnviado;
                if (estimada > 0.0005f && estimada < 5f)
                {
                    gradosPorUnidad = Mathf.Lerp(gradosPorUnidad, estimada, 0.5f);
                }
            }
            float error = Mathf.DeltaAngle(c.GiroCamara, rumbo);
            float enviar = Mathf.Abs(error) < 0.5f ? 0f : error * fraccion / gradosPorUnidad;
            ultimoEnviado = enviar;
            ultimoGiro = c.GiroCamara;
            hayUltimo = true;
            MoverRaton(new Vector2(enviar, Mathf.Abs(errorInclinacion) < 0.5f ? 0f : errorInclinacion * fraccion / gradosPorUnidad));
            if (++trazasMirada <= 6)
            {
                Debug.Log("[T-474] mirar: error " + error.ToString("F1") + " envio " + enviar.ToString("F1") + " gpu " + gradosPorUnidad.ToString("F4") + " cursor " + c.CursorBloqueado);
            }
            return error;
        }

        // Cuánto se puede avanzar (m, hasta `largo`) en esa dirección antes de chocar con algo sólido (ignora al jugador y los gatillos).
        private static float Libre(Vector3 pos, float rumbo, float largo)
        {
            Vector3 dir = Quaternion.Euler(0f, rumbo, 0f) * Vector3.forward;
            float minima = largo;
            foreach (RaycastHit g in Physics.SphereCastAll(pos + Vector3.up * 0.7f, 0.2f, dir, largo, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!g.collider.transform.IsChildOf(Jugador.Raiz) && g.distance > 0f)
                {
                    minima = Mathf.Min(minima, g.distance);
                }
            }
            return minima;
        }

        // ---------- ruta por las calles (NavMesh horneado en ejecución desde los colliders, como un jugador que lee el mapa) ----------

        private NavMeshDataInstance instanciaNavMesh;
        private readonly HashSet<Collider> hojasCerradas = new HashSet<Collider>();
        private readonly HashSet<Collider> hojasProbadas = new HashSet<Collider>();
        private readonly Dictionary<Collider, int> intentosPuerta = new Dictionary<Collider, int>();
        private bool navMeshListo;
        private NavMeshPath camino;
        private Vector3[] esquinas = new Vector3[0];
        private Vector3 rutaDestino;
        private float rutaHasta;
        private int rutaGeneracion = -1;

        // Hornea un NavMesh con los colliders sólidos y quietos de la ciudad. Las hojas de puerta no cuentan (se abren con E), ni nada que se mueva.
        private void HornearNavMesh()
        {
            navMeshListo = false;
            if (instanciaNavMesh.valid)
            {
                NavMesh.RemoveNavMeshData(instanciaNavMesh);
            }
            List<NavMeshBuildSource> fuentes = new List<NavMeshBuildSource>();
            Bounds limites = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(400f, 40f, 400f));
            NavMeshBuilder.CollectSources(limites, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), fuentes);
            int antes = fuentes.Count;
            fuentes.RemoveAll(s =>
            {
                Collider c = s.component as Collider;
                if (c == null)
                {
                    return false;
                }
                return c.isTrigger || (c.name == "Hoja" && !hojasCerradas.Contains(c)) || c.attachedRigidbody != null || c is CharacterController
                    || c.GetComponentInParent<CharacterController>() != null || c.GetComponentInParent<PandilleroAPie>() != null
                    || (Jugador != null && c.transform.IsChildOf(Jugador.Raiz)) || c.GetComponentInParent<PersonajeApoyo>() != null;
            });
            // T-978: una malla de choque sin lectura (la pasarela PS01 de T-910) no se puede hornear en ejecución (error en el log). Se deja fuera: va por
            // encima de la calle y el bot camina por abajo.
            fuentes.RemoveAll(s => s.component is MeshCollider mc && mc.sharedMesh != null && !mc.sharedMesh.isReadable);
            NavMeshBuildSettings ajustes = NavMesh.CreateSettings();
            ajustes.agentRadius = 0.35f;
            ajustes.agentHeight = 1.7f;
            ajustes.agentClimb = 0.4f;
            ajustes.agentSlope = 40f;
            ajustes.minRegionArea = 0.5f;
            NavMeshData datos = NavMeshBuilder.BuildNavMeshData(ajustes, fuentes, limites, Vector3.zero, Quaternion.identity);
            instanciaNavMesh = NavMesh.AddNavMeshData(datos);
            navMeshListo = datos != null;
            camino = new NavMeshPath();
            Debug.Log("[T-476] NavMesh horneado: " + fuentes.Count + " fuentes (de " + antes + ") triángulos " + NavMesh.CalculateTriangulation().indices.Length / 3);
        }

        // Como un jugador ante una puerta que le cierra el paso: se acerca, la mira de frente y pulsa E; si no se mueve, esa puerta no abre (cerrada en la demo)
        // y el NavMesh se hornea de nuevo sin ese paso. Devuelve true si había una puerta por probar.
        private IEnumerator UsarPuerta(Func<bool> terminar)
        {
            Collider hoja = null;
            float mejor = 3.5f;
            Vector3 pos = Jugador.Raiz.position;
            foreach (Collider c in Physics.OverlapSphere(pos + Vector3.up * 0.9f, 3.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                float d = Plano(c.bounds.center, pos);
                if (c.name == "Hoja" && c.enabled && !hojasCerradas.Contains(c) && !hojasProbadas.Contains(c) && d < mejor)
                {
                    mejor = d;
                    hoja = c;
                }
            }
            if (hoja == null)
            {
                yield break;
            }
            hojasProbadas.Add(hoja);
            Vector3 antes = hoja.bounds.center;
            float fin = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < fin && Plano(Jugador.Raiz.position, hoja.bounds.center) > 1.3f && !terminar())
            {
                PasoCaminar(hoja.bounds.center, false);
                yield return Siguiente();
            }
            SoltarTeclas();
            for (int i = 0; i < 10; i++)
            {
                MirarHacia(Rumbo(Jugador.Raiz.position, hoja.bounds.center), 0.7f);
                yield return Siguiente();
            }
            MoverRaton(Vector2.zero);
            yield return Pulsar(teclado.eKey, 30);
            bool abrio = !hoja.enabled || Vector3.Distance(hoja.bounds.center, antes) > 0.2f || !hoja.gameObject.activeInHierarchy;
            Debug.Log("[T-476] puerta " + hoja.transform.parent?.name + " en " + antes + (abrio ? ": abrió" : ": no abre"));
            if (!abrio)
            {
                // ¿Es la puerta (horario o cerrada en la demo) o fue la E del bot (mirada o alcance)? IntentarAbrir responde lo mismo que E.
                IRelojMundo reloj = Registro<IRelojMundo>.Actual;
                string motivo = "";
                bool laPuertaAbre = false;
                foreach (IPuerta puerta in Registro<IPuerta>.Todos)
                {
                    if (Plano(puerta.Posicion, antes) < 4f)
                    {
                        ResultadoPuerta respuesta = puerta.IntentarAbrir();
                        laPuertaAbre |= respuesta == ResultadoPuerta.Abierta || respuesta == ResultadoPuerta.YaAbierta;
                        motivo += puerta.Lugar + " " + respuesta + "; ";
                    }
                }
                string hora = reloj != null ? "hora del mundo " + reloj.Hora.ToString("F1") + " día " + reloj.Dia : "sin reloj";
                Debug.Log("[T-476] puerta: IntentarAbrir dice " + motivo + hora);
                intentosPuerta.TryGetValue(hoja, out int intentos);
                intentosPuerta[hoja] = ++intentos;
                hojasProbadas.Remove(hoja);
                if (!laPuertaAbre && intentos >= 3)
                {
                    Bug("La puerta en el camino del marcador no abre", motivo + hora + "; es la única ruta del NavMesh hacia el objetivo \"" + TextoObjetivo + "\"", "A", false);
                    hojasCerradas.Add(hoja);
                    HornearNavMesh();
                    esquinas = new Vector3[0];
                }
                else if (!laPuertaAbre)
                {
                    yield return Segundos(10f);
                }
            }
        }

        // Si el jugador se traba junto a una hoja de puerta, esa puerta no se abre (cerrada en la demo): se marca y se vuelve a hornear sin ese paso.
        private bool MarcarHojasTrabadas(Vector3 pos)
        {
            bool nueva = false;
            foreach (Collider c in Physics.OverlapSphere(pos + Vector3.up * 0.9f, 1.6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.name == "Hoja" && c.enabled && hojasCerradas.Add(c))
                {
                    nueva = true;
                    Debug.Log("[T-476] puerta que no abre: " + c.transform.parent?.name + " en " + c.transform.position);
                }
            }
            if (nueva)
            {
                HornearNavMesh();
                esquinas = new Vector3[0];
            }
            return nueva;
        }

        // Camino de esquina en esquina hasta el destino; false si no hay NavMesh o no hay camino.
        private bool CalcularRuta(Vector3 desde, Vector3 hasta)
        {
            if (!navMeshListo || instanciaNavMesh.valid == false)
            {
                HornearNavMesh();
            }
            rutaDestino = hasta;
            rutaHasta = Time.realtimeSinceStartup + 1.5f;
            esquinas = new Vector3[0];
            if (!NavMesh.SamplePosition(desde, out NavMeshHit a, 4f, NavMesh.AllAreas) || !NavMesh.SamplePosition(hasta, out NavMeshHit b, 8f, NavMesh.AllAreas))
            {
                return false;
            }
            if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, camino) || camino.corners.Length < 2)
            {
                return false;
            }
            esquinas = camino.corners;
            return true;
        }

        // Un cuadro de caminata hacia el destino siguiendo el camino del NavMesh (lejos) o directo (cerca).
        private void PasoRuta(Vector3 destino, bool correr, float distanciaRuta = 6f)
        {
            Vector3 pos = Jugador.Raiz.position;
            if (Plano(pos, destino) > distanciaRuta)
            {
                if (esquinas.Length < 2 || Plano(rutaDestino, destino) > 3f || Time.realtimeSinceStartup > rutaHasta)
                {
                    CalcularRuta(pos, destino);
                }
                if (esquinas.Length >= 2)
                {
                    // La primera esquina a más de 0.8 m; si la siguiente se ve sin obstáculo y está cerca, se corta la esquina.
                    int i = 1;
                    while (i < esquinas.Length - 1 && Plano(pos, esquinas[i]) < 0.8f)
                    {
                        i++;
                    }
                    Vector3 punto = esquinas[i];
                    if (i + 1 < esquinas.Length && Plano(pos, esquinas[i + 1]) < 5f
                        && Libre(pos, Rumbo(pos, esquinas[i + 1]), Plano(pos, esquinas[i + 1])) >= Plano(pos, esquinas[i + 1]) - 0.05f)
                    {
                        punto = esquinas[i + 1];
                    }
                    PasoCaminar(punto, correr);
                    return;
                }
            }
            esquinas = new Vector3[0];
            PasoCaminar(destino, correr);
        }

        // Un cuadro de caminata hacia el destino: como un jugador, rodea lo que tiene enfrente (prueba rumbos cada 30° y mantiene el
        // elegido 0.5 s) y pulsa WASD (8 direcciones respecto a la cámara) con Shift para correr.
        private void PasoCaminar(Vector3 destino, bool correr)
        {
            Vector3 pos = Jugador.Raiz.position;
            if (finE > 0f && Time.realtimeSinceStartup >= finE)
            {
                Soltar(teclado.eKey);
                finE = 0f;
            }
            float haciaDestino = Rumbo(pos, destino);
            float rumbo = haciaDestino;
            if (Time.realtimeSinceStartup >= proximaMedida)
            {
                if (Plano(posMedida, destino) - Plano(pos, destino) < 0.4f && Plano(pos, destino) > 3f)
                {
                    desvioHasta = Time.realtimeSinceStartup + 3f;
                    // Como un jugador frente a una puerta cerrada: también prueba con E.
                    if (!sinE)
                    {
                        Presionar(teclado.eKey);
                    }
                    finE = Time.realtimeSinceStartup + 0.15f;
                }
                posMedida = pos;
                proximaMedida = Time.realtimeSinceStartup + 1f;
            }
            if (Time.realtimeSinceStartup < desvioHasta && Plano(pos, destino) > 3f)
            {
                if (Time.realtimeSinceStartup >= finRumbo)
                {
                    float mejor = -1f;
                    float elegido = 0f;
                    for (int k = 0; k <= 6; k++)
                    {
                        float grados = k * 30f;
                        float[] lados = k == 0 ? new[] { 0f } : (ladoRumbo > 0 ? new[] { grados, -grados } : new[] { -grados, grados });
                        foreach (float o in lados)
                        {
                            float libre = Libre(pos, haciaDestino + o, 2f);
                            // Libre 1.2 m o más basta; entre los libres gana el más recto; si ninguno, el que deja más espacio.
                            float puntos = libre >= 0.8f ? 10f - Mathf.Abs(o) * 0.01f : libre;
                            if (puntos > mejor)
                            {
                                mejor = puntos;
                                elegido = o;
                            }
                        }
                    }
                    desvio = elegido;
                    ladoRumbo = desvio != 0f ? Mathf.Sign(desvio) : ladoRumbo;
                    finRumbo = Time.realtimeSinceStartup + 0.5f;
                }
                rumbo = haciaDestino + desvio;
            }
            // Cerca del destino o atorado, el jugador mira hacia él con el ratón (las puertas y los profes se usan "de frente").
            if (Plano(pos, destino) < 6f || Time.realtimeSinceStartup < desvioHasta)
            {
                MirarHacia(haciaDestino, 0.5f);
            }
            else
            {
                MoverRaton(Vector2.zero);
            }
            float rel = Mathf.DeltaAngle(Controlador.GiroCamara, rumbo);
            float a = Mathf.Abs(rel);
            Poner(teclado.wKey, a < 67.5f);
            Poner(teclado.sKey, a > 112.5f);
            Poner(teclado.dKey, rel > 22.5f && rel < 157.5f);
            Poner(teclado.aKey, rel < -22.5f && rel > -157.5f);
            Poner(teclado.leftShiftKey, correr);
            if (Time.realtimeSinceStartup >= proximaTraza && trazasMirada++ < 14)
            {
                proximaTraza = Time.realtimeSinceStartup + 0.25f;
                Debug.Log("[T-474] paso: estado w" + teclado.wKey.isPressed + " s" + teclado.sKey.isPressed + " a" + teclado.aKey.isPressed + " d" + teclado.dKey.isPressed + " rumbo " + rumbo.ToString("F0") + " cam " + Controlador.GiroCamara.ToString("F0") + " rel " + rel.ToString("F0") + " desvio " + desvio + " teclas w" + (a < 67.5f) + " s" + (a > 112.5f) + " d" + (rel > 22.5f && rel < 157.5f)
                    + " | pos " + pos + " vel " + Controlador.Velocidad);
            }
        }

        private void Poner(ButtonControl tecla, bool presionada)
        {
            if (presionada)
            {
                Presionar(tecla);
            }
            else
            {
                Soltar(tecla);
            }
        }

        // Camina hacia lo que da la función hasta quedar a `parada` m (plano). Si se traba lo anota; con `rescate` coloca al jugador cerca.
        private IEnumerator Caminar(Func<Vector3?> destino, float parada, float maximo, string que, Func<bool> terminar = null, bool rescate = true)
        {
            float fin = Time.realtimeSinceStartup + maximo;
            Vector3 anterior = Jugador.Raiz.position;
            float proximaMedida = Time.realtimeSinceStartup + 1.5f;
            int trabas = 0;
            float finEscape = 0f;
            float lado = 1f;
            while (true)
            {
                if (terminar != null && terminar())
                {
                    SoltarTeclas();
                    yield break;
                }
                Vector3? d = destino();
                if (d == null)
                {
                    SoltarTeclas();
                    yield return Siguiente();
                    if (Time.realtimeSinceStartup > fin)
                    {
                        yield break;
                    }
                    continue;
                }
                Vector3 pos = Jugador.Raiz.position;
                if (Plano(pos, d.Value) <= parada)
                {
                    SoltarTeclas();
                    yield break;
                }
                if (Time.realtimeSinceStartup > fin || trabas >= 5)
                {
                    SoltarTeclas();
                    Bug("Tope al caminar: " + que, "el jugador no llegó a " + d.Value + " (a " + Plano(pos, d.Value).ToString("F1") + " m) tras "
                        + maximo.ToString("F0") + " s o " + trabas + " trabas; posición " + pos, "A", true);
                    if (rescate)
                    {
                        yield return Colocar(d.Value, parada);
                    }
                    yield break;
                }
                if (Time.realtimeSinceStartup >= finEscape)
                {
                    PasoRuta(d.Value, true);
                }
                else
                {
                    // Escape: de lado y saltando un momento.
                    Soltar(teclado.wKey);
                    Presionar(lado > 0 ? teclado.dKey : teclado.aKey);
                    Presionar(teclado.spaceKey);
                    MoverRaton(Vector2.zero);
                }
                if (Time.realtimeSinceStartup >= proximaMedida)
                {
                    if (Plano(pos, anterior) < 0.5f && Time.realtimeSinceStartup >= finEscape)
                    {
                        trabas++;
                        yield return UsarPuerta(terminar ?? (() => false));
                        ControladorAPie cc = Controlador;
                        Debug.Log("[T-474] estado: control " + cc.ControlActivo + " enPiso " + cc.EnPiso + " vel " + cc.Velocidad + " timeScale " + Time.timeScale + " w " + teclado.wKey.isPressed + " s " + teclado.sKey.isPressed + " a " + teclado.aKey.isPressed
                            + " libres " + Libre(pos, 0f, 2f) + "," + Libre(pos, 90f, 2f) + "," + Libre(pos, 180f, 2f) + "," + Libre(pos, 270f, 2f) + " pausa " + Pausa.Activa);
                        Debug.Log("[T-474] traba " + trabas + " en " + pos + " camino a " + d.Value + " yaw cam " + Controlador.GiroCamara.ToString("F0") + " desvio " + desvio);
                        lado = -lado;
                        finEscape = Time.realtimeSinceStartup + 1.2f;
                    }
                    else if (Time.realtimeSinceStartup >= finEscape)
                    {
                        Soltar(teclado.aKey);
                        Soltar(teclado.dKey);
                        Soltar(teclado.spaceKey);
                        if (Plano(pos, anterior) >= 2f)
                        {
                            trabas = 0;
                        }
                    }
                    anterior = pos;
                    proximaMedida = Time.realtimeSinceStartup + 1.5f;
                }
                if (pos.y < -5f)
                {
                    Bug("El jugador cae al vacío", "y=" + pos.y.ToString("F1") + " en " + pos, "A", true);
                    SoltarTeclas();
                    yield return Colocar(d.Value, parada);
                    yield break;
                }
                yield return Siguiente();
            }
        }

        // Solo tras un tope ya anotado: se coloca al jugador junto al destino para seguir probando.
        private IEnumerator Colocar(Vector3 destino, float distancia)
        {
            Vector3 hacia = destino - Jugador.Raiz.position;
            hacia.y = 0f;
            Vector3 dir = hacia.sqrMagnitude > 0.01f ? hacia.normalized : Vector3.forward;
            Vector3 punto = destino - dir * Mathf.Max(distancia, 1.2f);
            // T-748: el rayo sale 1.5 m sobre el destino (no 4 m sobre el punto): en interiores (banco de Tino) desde 4 m pegaba en el techo y el jugador quedaba en la azotea, 4 m arriba.
            if (Physics.Raycast(new Vector3(punto.x, Mathf.Max(destino.y, Jugador.Raiz.position.y) + 1.5f, punto.z), Vector3.down, out RaycastHit piso, 10f))
            {
                punto = piso.point;
            }
            punto.y += 0.1f;
            Jugador.Colocar(new Pose(punto, Quaternion.LookRotation(dir, Vector3.up)));
            Controlador?.ReiniciarMovimiento();
            yield return Siguiente();
            yield return Siguiente();
        }

        private IEnumerator EsperarConVida()
        {
            VidaAPie vida = Jugador.Raiz.GetComponentInChildren<VidaAPie>();
            if (vida != null && vida.EstaKO)
            {
                muertes++;
                Bug("El jugador cayó KO (" + muertes + ")", "vida 0 en " + Jugador.Raiz.position + " durante " + TextoObjetivo, "B", true);
                float fin = Time.realtimeSinceStartup + 25f;
                while (vida.EstaKO && Time.realtimeSinceStartup < fin)
                {
                    yield return Siguiente();
                }
            }
        }

        // Camina hacia el marcador y pulsa E; devuelve cuando `listo` es verdadero.
        private IEnumerator IrYUsar(string que, Func<bool> listo, float maximoCamino = 150f, float maximoUso = 8f, string idProfe = null)
        {
            if (listo())
            {
                yield break;
            }
            if (!PosicionMarcador(out Vector3 _))
            {
                Bug("El marcador no tiene posición: " + que, "el objetivo \"" + TextoObjetivo + "\" no resuelve lugar en UbicacionObjetivo", "A", true);
                yield break;
            }
            yield return Caminar(() => PosicionMarcador(out Vector3 p) ? p : (Vector3?)null, DistanciaInteraccion, maximoCamino, que, listo);
            if (idProfe != null && !listo())
            {
                // El marcador lleva al lugar; el jugador ve al profe y camina hasta él.
                PersonajeApoyo profe = null;
                foreach (PersonajeApoyo pa in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
                {
                    profe = pa.Datos.Id == idProfe ? pa : profe;
                }
                if (profe != null)
                {
                    Vector3 mk = PosicionMarcador(out Vector3 pm) ? pm : Vector3.zero;
                    Debug.Log("[T-474] " + que + ": marcador " + mk + " profe " + profe.transform.position + " (" + Plano(mk, profe.transform.position).ToString("F1") + " m del marcador) jugador " + Jugador.Raiz.position);
                    if (Plano(mk, profe.transform.position) > 2.5f)
                    {
                        Bug("El marcador de " + que + " queda lejos del personaje", "el marcador está a " + Plano(mk, profe.transform.position).ToString("F1") + " m de " + idProfe, "A", false);
                    }
                    PersonajeApoyo destinoProfe = profe;
                    yield return Caminar(() => destinoProfe.transform.position, 1.3f, 40f, que + " (el personaje)", listo);
                }
            }
            float fin = Time.realtimeSinceStartup + maximoUso;
            int intentos = 0;
            while (!listo() && Time.realtimeSinceStartup < fin)
            {
                if (PosicionMarcador(out Vector3 p) && Plano(Jugador.Raiz.position, p) > DistanciaInteraccion + 0.4f)
                {
                    PasoRuta(p, false, DistanciaRutaMinima);
                    yield return Siguiente();
                    continue;
                }
                SoltarTeclas();
                for (int i = 0; i < 6; i++)
                {
                    MirarHacia(Rumbo(Jugador.Raiz.position, p), 0.7f);
                    yield return Siguiente();
                }
                MoverRaton(Vector2.zero);
                yield return Pulsar(teclado.eKey, 8);
                intentos++;
            }
            SoltarTeclas();
            if (!listo())
            {
                Bug("E no hace nada junto al objetivo: " + que, "tras " + intentos + " pulsaciones de E a " + (PosicionMarcador(out Vector3 q)
                    ? Plano(Jugador.Raiz.position, q).ToString("F1") : "?") + " m del marcador, el objetivo sigue \"" + TextoObjetivo + "\"", "A", true);
            }
        }

        private static IEnumerable<PandilleroAPie> Enemigos()
        {
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && p.gameObject.activeInHierarchy && !p.EstaKO && (p.Grupo == null || p.Grupo.Pandilla != IdPandilla.Jugador))
                {
                    yield return p;
                }
            }
        }

        // El enemigo al que se persigue: se mantiene el mismo mientras siga en pie (si no, el jugador oscila entre dos equidistantes).
        private PandilleroAPie Blanco(Func<PandilleroAPie, bool> filtro)
        {
            if (blanco != null && blanco.gameObject.activeInHierarchy && !blanco.EstaKO && (filtro == null || filtro(blanco)))
            {
                return blanco;
            }
            blanco = MasCercano(filtro);
            return blanco;
        }

        private static PandilleroAPie MasCercano(Func<PandilleroAPie, bool> filtro = null)
        {
            PandilleroAPie mejor = null;
            float mejorD = float.MaxValue;
            Vector3 pos = Jugador.Raiz.position;
            foreach (PandilleroAPie p in Enemigos())
            {
                if (filtro != null && !filtro(p))
                {
                    continue;
                }
                float d = (p.transform.position - pos).sqrMagnitude;
                if (d < mejorD)
                {
                    mejorD = d;
                    mejor = p;
                }
            }
            return mejor;
        }

        // Pelea a puños: camina al enemigo más cercano (o al marcador si no hay) y da golpes con el clic izquierdo.
        private IEnumerator PelearAPunos(Func<bool> terminar, Func<PandilleroAPie, bool> filtro, float maximo, string que)
        {
            float fin = Time.realtimeSinceStartup + maximo;
            int cuadro = 0;
            while (!terminar())
            {
                if (Time.realtimeSinceStartup > fin)
                {
                    SoltarTeclas();
                    Bug("La pelea no termina: " + que, "tras " + maximo.ToString("F0") + " s sigue \"" + TextoObjetivo + "\"", "A", true);
                    yield break;
                }
                yield return EsperarConVida();
                PandilleroAPie e = Blanco(filtro);
                Vector3 pos = Jugador.Raiz.position;
                if (Time.realtimeSinceStartup >= proximaTraza2)
                {
                    proximaTraza2 = Time.realtimeSinceStartup + 30f;
                    PosicionMarcador(out Vector3 m);
                    foreach (float h in new[] { 0.3f, 0.9f, 1.5f })
                    {
                        foreach (float dz in new[] { 0f, 1f, -1f })
                        {
                            if (Physics.Raycast(pos + new Vector3(0f, h, dz), Vector3.right, out RaycastHit rh, 6f, ~0, QueryTriggerInteraction.Ignore))
                            {
                                Debug.Log("[T-474] rayo h" + h + " dz" + dz + " -> " + rh.collider.name + " (" + rh.collider.transform.parent?.name + ") a " + rh.distance.ToString("F2") + " m");
                            }
                        }
                    }
                    Debug.Log("[T-474] pelea: jugador " + pos + " enemigo " + (e != null ? e.name + " " + e.transform.position + " estado " + e.Estado : "ninguno") + " marcador " + m + " vida enemigo " + (e != null ? e.Actual : 0f)
                        + " vidaJ " + Jugador.Raiz.GetComponentInChildren<VidaAPie>().Actual + " ambiente");
                }
                if (e != null && Plano(e.transform.position, pos) < 30f)
                {
                    float d = Plano(e.transform.position, pos);
                    PasoRuta(e.transform.position, d > 2.5f);
                    if (d <= 2.2f)
                    {
                        float fase = Time.realtimeSinceStartup % 0.5f;
                        if (fase < 0.12f)
                        {
                            Presionar(raton.leftButton);
                        }
                        else
                        {
                            Soltar(raton.leftButton);
                        }
                    }
                    else
                    {
                        Soltar(raton.leftButton);
                    }
                }
                else if (PosicionMarcador(out Vector3 destino))
                {
                    PasoRuta(destino, true);
                }
                else
                {
                    SoltarTeclas();
                }
                yield return Siguiente();
            }
            SoltarTeclas();
            Soltar(raton.leftButton);
        }

        private Vector3 atascoPos;
        private float atascoProxima;
        private float atascoHasta;
        private float atascoLado = 1f;

        // PasoRuta con salida de atascos: si en 1.5 s casi no avanzó, un momento de lado y saltando (alternando) y se vuelve a planificar.
        private void AvanzarConAtasco(Vector3 destino, bool correr)
        {
            Vector3 pos = Jugador.Raiz.position;
            if (Time.realtimeSinceStartup >= atascoProxima)
            {
                if (Plano(pos, atascoPos) < 0.5f && Time.realtimeSinceStartup >= atascoHasta && Plano(pos, destino) > 3f)
                {
                    atascoLado = -atascoLado;
                    atascoHasta = Time.realtimeSinceStartup + 1.0f;
                    esquinas = new Vector3[0];
                    MarcarHojasTrabadas(pos);
                    Debug.Log("[T-476] atasco en " + pos + " hacia " + destino + " (NavMesh " + NavMesh.SamplePosition(pos, out NavMeshHit _, 2f, NavMesh.AllAreas) + ") control " + Controlador.ControlActivo + " pausa " + Pausa.Activa + " pila " + PilaPantallas.Cantidad + (PilaPantallas.Arriba != null ? " " + PilaPantallas.Arriba.name : "") + " vel " + Controlador.Velocidad + " cursor " + Controlador.CursorBloqueado + " w " + teclado.wKey.isPressed + " timeScale " + Time.timeScale);
                }
                atascoPos = pos;
                atascoProxima = Time.realtimeSinceStartup + 1.5f;
            }
            if (Time.realtimeSinceStartup < atascoHasta)
            {
                Soltar(teclado.wKey);
                Presionar(atascoLado > 0 ? teclado.dKey : teclado.aKey);
                Soltar(atascoLado > 0 ? teclado.aKey : teclado.dKey);
                Presionar(teclado.spaceKey);
                return;
            }
            Soltar(teclado.spaceKey);
            PasoRuta(destino, correr);
        }

        // El enemigo al que se le tira: el más cercano que se ve (a menos de 25 m); si no se ve ninguno, el más cercano. Cambia solo si el nuevo es claramente mejor.
        private PandilleroAPie ElegirBlancoTiro(Func<PandilleroAPie, bool> filtro)
        {
            PandilleroAPie mejor = null;
            float mejorPuntos = float.MaxValue;
            Vector3 pos = Jugador.Raiz.position;
            foreach (PandilleroAPie p in Enemigos())
            {
                if (filtro != null && !filtro(p))
                {
                    continue;
                }
                if (dejadosHasta.TryGetValue(p, out float hasta) && Time.realtimeSinceStartup < hasta)
                {
                    continue;
                }
                float d = Plano(p.transform.position, pos);
                float puntos = d + (d < 25f && LineaDeVista(p, out Vector3 _) ? 0f : 1000f) + (p == blanco ? -4f : 0f);
                if (puntos < mejorPuntos)
                {
                    mejorPuntos = puntos;
                    mejor = p;
                }
            }
            blanco = mejor;
            return mejor;
        }

        // Los enemigos de la zona del objetivo (a menos de 60 m del marcador): no se persigue a las otras pandillas de la ciudad.
        private bool CercaDelMarcador(PandilleroAPie p)
        {
            return Plano(p.transform.position, zonaCentro) < 60f;
        }

        // T-514: en la pelea del Tianguis el jefe todavía no es el objetivo (VENCE A LOS SKULLS cuenta a los pandilleros): el bot no le gasta balas.
        private bool PandilleroCercaSinJefe(PandilleroAPie p)
        {
            if (!CercaDelMarcador(p))
            {
                return false;
            }
            foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Jefe == p)
                {
                    return false;
                }
            }
            return true;
        }

        // Sin enemigos de la zona: el grupo vencido reaparece a los 40 s si el jugador está a más de 30 m, así que se espera a 50 m (también si el objetivo dice "VUELVE A CASA" tras un KO).
        private bool DestinoSinEnemigos(out Vector3 destino)
        {
            if (zonaCentro != Vector3.zero)
            {
                destino = retirada;
                return true;
            }
            return PosicionMarcador(out destino);
        }

        // ¿Se ve el pecho del enemigo desde la cámara? (lo que bloquea es lo sólido que no sea el jugador ni otro pandillero).
        private static bool LineaDeVista(PandilleroAPie e, out Vector3 pecho)
        {
            pecho = e.transform.position + Vector3.up * 1.2f;
            Vector3 origen = Controlador.Camara.position;
            Vector3 hacia = pecho - origen;
            foreach (RaycastHit g in Physics.RaycastAll(origen, hacia.normalized, hacia.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (g.collider.transform.IsChildOf(Jugador.Raiz) || g.collider.GetComponentInParent<PandilleroAPie>() != null)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        // Apunta la cámara al punto con el ratón (giro e inclinación); devuelve el error mayor en grados.
        private static bool AtacarHabilitada(PistolaAPie p)
        {
            var campo = typeof(PistolaAPie).GetField("atacar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var r = campo != null ? campo.GetValue(p) as UnityEngine.InputSystem.InputActionReference : null;
            return r != null && r.action.enabled;
        }

        private float ApuntarA(Vector3 punto, float fraccion)
        {
            Vector3 origen = Controlador.Camara.position;
            Vector3 d = punto - origen;
            float horizontal = new Vector2(d.x, d.z).magnitude;
            float deseada = -Mathf.Atan2(d.y, horizontal) * Mathf.Rad2Deg;
            float errorInclinacion = Controlador.Inclinacion - deseada;
            float errorGiro = MirarHacia(Rumbo(origen, punto), fraccion, errorInclinacion);
            return Mathf.Max(Mathf.Abs(errorGiro), Mathf.Abs(errorInclinacion));
        }

        // Combate a distancia con la pistola como un jugador: elige al enemigo vivo más cercano, se acerca por el NavMesh hasta verlo a
        // ~14 m, apunta a su pecho con clic derecho (con el apuntado asistido), dispara ráfagas cortas, recarga con R, se cubre con Q si
        // la vida baja de 50 % (regenera tras unos segundos sin daño) y vuelve a pelear.
        private IEnumerator PelearConPistola(Func<bool> terminar, Func<PandilleroAPie, bool> filtro, float maximo, string que)
        {
            float fin = Time.realtimeSinceStartup + maximo;
            PistolaAPie pistola = Jugador.Raiz.GetComponentInChildren<PistolaAPie>();
            ArmasAPie armas = Jugador.Raiz.GetComponentInChildren<ArmasAPie>();
            VidaAPie vida = Jugador.Raiz.GetComponentInChildren<VidaAPie>();
            CubiertaYRodada cubiertaJ = Jugador.Raiz.GetComponentInChildren<CubiertaYRodada>();
            if (armas != null && armas.ArmaActiva != ArmaAPie.Pistola)
            {
                yield return Pulsar(teclado.digit2Key);
            }
            sinE = true;
            float errorT741 = -1f;
            int disparosT741 = 0, impactosT741 = 0;
            if (pistola != null) { pistola.Disparo += () => disparosT741++; pistola.BalaImpacto += (a, i) => impactosT741++; }
            float proximoEquipo = 0f;
            float proximaRecarga = 0f;
            bool volcado = false;
            bool cubriendo = false;
            float finCubierta = 0f;
            float proximoCambioLado = 0f;

            float lado = 1f;
            while (!terminar())
            {
                if (Time.realtimeSinceStartup > fin)
                {
                    SoltarTeclas();
                    Soltar(raton.leftButton);
                    Soltar(raton.rightButton);
                    sinE = false;
                    zonaCentro = Vector3.zero;
                    Bug("La pelea con pistola no termina: " + que, "tras " + maximo.ToString("F0") + " s sigue \"" + TextoObjetivo + "\"; enemigos vivos "
                        + CuentaEnemigos(), "B", true);
                    yield break;
                }
                // T-741: si la pistola no quedo en la mano (la tecla 2 se pierde mientras un golpe esta en curso, o tras cargar la partida), se vuelve a pedir; con punos el clic solo golpea y el bot no dispara jamas.
                if (armas != null && armas.ArmaActiva != ArmaAPie.Pistola && Time.realtimeSinceStartup >= proximoEquipo)
                {
                    proximoEquipo = Time.realtimeSinceStartup + 1f;
                    Debug.Log("[T-741] arma activa " + armas.ArmaActiva + ", tiene pistola " + armas.TieneArma(ArmaAPie.Pistola) + "; pulsa 2");
                    yield return Pulsar(teclado.digit2Key);
                }
                yield return EsperarConVida();
                if (ventajaDePrueba && vida != null && !vida.EstaKO && vida.Maxima > 0f && vida.Actual < vida.Maxima * UmbralVentaja)
                {
                    vida.CurarTodo();
                    curasDePrueba++;
                }
                PandilleroAPie e = ElegirBlancoTiro(filtro);
                Vector3 pos = Jugador.Raiz.position;
                float fraccionVida = vida != null && vida.Maxima > 0f ? vida.Actual / vida.Maxima : 1f;
                if (Time.realtimeSinceStartup >= proximaTraza2)
                {
                    proximaTraza2 = Time.realtimeSinceStartup + 2f;
                    if (vida != null && vida.Actual < 90f && !volcado)
                    {
                        volcado = true;
                        foreach (PandilleroAPie q in PandilleroAPie.Activos)
                        {
                            Debug.Log("[T-476] activo " + q.name + " act " + q.gameObject.activeInHierarchy + " KO " + q.EstaKO + " pos " + q.transform.position + " estado " + q.Estado + " pandilla " + (q.Grupo != null ? q.Grupo.Pandilla.ToString() : "-"));
                        }
                    }
                    Debug.Log("[T-476] pistola: pos " + pos + " vida " + (vida != null ? vida.Actual.ToString("F0") : "?") + " balas " + (pistola != null ? pistola.Balas : -1) + " enemigo "
                        + (e != null ? e.name + " d " + Plano(e.transform.position, pos).ToString("F1") + " estado " + e.Estado + " vista " + LineaDeVista(e, out Vector3 _) : "ninguno")
                        + " vivos " + CuentaEnemigos() + " cubriendo " + cubriendo + " apuntando " + Controlador.Apuntando + " | T-741 error " + errorT741.ToString("F1") + " disparos " + disparosT741 + " impactos " + impactosT741 + " armas.Balas " + (armas != null ? armas.Balas : -1) + " agachado " + (cubiertaJ != null && cubiertaJ.AgachadoLibre) + " arma activa " + (armas != null ? armas.ArmaActiva.ToString() : "?") + " clicIzq " + raton.leftButton.isPressed + " atacarHabilitada " + AtacarHabilitada(pistola) + " control " + (Controlador != null && Controlador.ControlActivo));
                }
                if (cubriendo)
                {
                    // Agachado tras la cubierta hasta recuperarse (la vida regenera sin recibir daño).
                    SoltarTeclas();
                    Soltar(raton.leftButton);
                    Soltar(raton.rightButton);
                    if (pistola != null && pistola.Balas < pistola.TamanoCargador && !pistola.Recargando && Time.realtimeSinceStartup > proximaRecarga)
                    {
                        proximaRecarga = Time.realtimeSinceStartup + 2f;
                        yield return Pulsar(teclado.rKey, 0);
                    }
                    if (fraccionVida > 0.85f || Time.realtimeSinceStartup > finCubierta)
                    {
                        cubriendo = false;
                        if (cubiertaJ != null && cubiertaJ.Cubierto)
                        {
                            yield return Pulsar(teclado.qKey, 0);
                        }
                    }
                    yield return Siguiente();
                    continue;
                }
                if (fraccionVida < 0.5f && e != null)
                {
                    SoltarTeclas();
                    Soltar(raton.leftButton);
                    Soltar(raton.rightButton);
                    yield return Pulsar(teclado.qKey, 1);
                    cubriendo = cubiertaJ != null && cubiertaJ.Cubierto;
                    finCubierta = Time.realtimeSinceStartup + 18f;
                    if (!cubriendo)
                    {
                        // Sin cubierta cerca: huye unos segundos (corriendo lejos del enemigo) para dejar que regenere.
                        Vector3 lejos = pos + (pos - e.transform.position).normalized * 15f;
                        float finHuida = Time.realtimeSinceStartup + 6f;
                        while (Time.realtimeSinceStartup < finHuida && !terminar())
                        {
                            AvanzarConAtasco(lejos, true);
                            yield return Siguiente();
                        }
                    }
                    continue;
                }
                if (e != null)
                {
                    float d = Plano(e.transform.position, pos);
                    bool ve = LineaDeVista(e, out Vector3 pecho) || d < 2.5f;
                    float ahora = Time.realtimeSinceStartup;
                    if (ve || sinVistaDe != e)
                    {
                        sinVistaDe = ve ? null : e;
                        sinVistaDesde = ahora;
                    }
                    else if (ahora - sinVistaDesde > SegundosSinVistaParaDejar)
                    {
                        // Lleva rato escondido: se deja y se busca otro blanco.
                        dejadosHasta[e] = ahora + SegundosBlancoDejado;
                        sinVistaDe = null;
                    }
                    if (!ve && d < 12f)
                    {
                        // Escondido cerca: rodearlo hacia un costado para verlo, como haría un jugador.
                        if (ahora >= proximoCambioFlanco)
                        {
                            flancoSigno = -flancoSigno;
                            proximoCambioFlanco = ahora + 3f;
                        }
                        Vector3 desdeEnemigo = pos - e.transform.position;
                        desdeEnemigo.y = 0f;
                        Vector3 flanco = Quaternion.Euler(0f, GradosFlanqueo * flancoSigno, 0f) * desdeEnemigo.normalized * DistanciaFlanqueo;
                        AvanzarConAtasco(e.transform.position + flanco, true);
                        Soltar(raton.leftButton);
                    }
                    else if (!ve || d > 14f)
                    {
                        AvanzarConAtasco(e.transform.position, true);
                        Soltar(raton.rightButton);
                        Soltar(raton.leftButton);
                        if (pistola != null && pistola.Balas < pistola.TamanoCargador / 2 && !pistola.Recargando && Time.realtimeSinceStartup > proximaRecarga)
                        {
                            proximaRecarga = Time.realtimeSinceStartup + 2f;
                            yield return Pulsar(teclado.rKey, 0);
                        }
                    }
                    else
                    {
                        Soltar(teclado.leftShiftKey);
                        Presionar(raton.rightButton);
                        float error = ApuntarA(pecho, 0.8f);
                        errorT741 = error;
                        if (Time.realtimeSinceStartup >= proximoCambioLado)
                        {
                            lado = -lado;
                            proximoCambioLado = Time.realtimeSinceStartup + UnityEngine.Random.Range(0.8f, 1.8f);
                        }
                        // De costado respecto a la cámara para ser un blanco difícil; más cerca de 6 m retrocede.
                        Poner(teclado.dKey, lado > 0 && d > 5f);
                        Poner(teclado.aKey, lado < 0 && d > 5f);
                        Poner(teclado.sKey, d < 6f);
                        Soltar(teclado.wKey);
                        if (pistola != null && pistola.Balas == 0 && !pistola.Recargando && Time.realtimeSinceStartup > proximaRecarga)
                        {
                            proximaRecarga = Time.realtimeSinceStartup + 1.5f;
                            Soltar(raton.leftButton);
                            Presionar(teclado.rKey);
                        }
                        else
                        {
                            Soltar(teclado.rKey);
                            if (error < 4f && Time.realtimeSinceStartup % 0.45f < 0.2f)
                            {
                                Presionar(raton.leftButton);
                            }
                            else
                            {
                                Soltar(raton.leftButton);
                            }
                        }
                    }
                }
                else if (DestinoSinEnemigos(out Vector3 destino))
                {
                    Soltar(raton.rightButton);
                    if (Plano(pos, destino) > 3f)
                    {
                        AvanzarConAtasco(destino, true);
                    }
                    else
                    {
                        SoltarTeclas();
                    }
                }
                else
                {
                    SoltarTeclas();
                }
                yield return Siguiente();
            }
            SoltarTeclas();
            Soltar(raton.leftButton);
            Soltar(raton.rightButton);
            Soltar(teclado.rKey);
            Soltar(teclado.qKey);
            sinE = false;
                    zonaCentro = Vector3.zero;
        }

        private static int CuentaEnemigos()
        {
            int n = 0;
            foreach (PandilleroAPie _ in Enemigos())
            {
                n++;
            }
            return n;
        }

        private static IEnumerator EsperarCiudad(DirectorPandillas anterior, Action<DirectorPandillas> alListo, string que = "la ciudad arrancó")
        {
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director != anterior && director.Iniciado && Registro<IJugador>.Actual != null
                    && Registro<ICartera>.Actual != null && ServicioGuardado.Actual != null;
            }, que, 60f);
            alListo(director);
        }

        private IEnumerator EsperarTitulo(Action<PantallaTitulo> alListo)
        {
            PantallaTitulo titulo = null;
            yield return EsperarHasta(() =>
            {
                titulo = Object.FindFirstObjectByType<PantallaTitulo>();
                return titulo != null && titulo.Activo && titulo.Abierta;
            }, "el título", 60f);
            alListo(titulo);
        }

        // Baja o sube con las flechas hasta que el botón esté elegido.
        private IEnumerator Elegir(MenuCyberfix menu, BotonCyberfix boton, string que)
        {
            for (int i = 0; i < 8 && !ReferenceEquals(menu.Elegido, boton); i++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            for (int i = 0; i < 8 && !ReferenceEquals(menu.Elegido, boton); i++)
            {
                yield return Pulsar(teclado.upArrowKey);
            }
            if (!ReferenceEquals(menu.Elegido, boton))
            {
                Bug("No se llega a " + que + " con ↓", "tras 8 pulsaciones el elegido es " + (menu.Elegido != null ? menu.Elegido.Etiqueta : "nada"), "A", true);
            }
        }

        // ---------- el recorrido ----------

        // T-476: el NavMesh horneado en ejecución conecta la casa con los lugares de la historia (si no, el bot no puede llegar).
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator NavMesh_LugaresDeLaHistoria_TienenCamino()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Segundos(2f);
            HornearNavMesh();
            Vector3[] lugares =
            {
                new Vector3(-59f, 0f, -77f), new Vector3(-44.3f, 0f, -77f), new Vector3(-32.8f, 1.24f, 28.9f), new Vector3(-36f, 0.2f, 85f), new Vector3(74.5f, 0.2f, 47f)
            };
            string[] nombres = { "casa", "INVENTOR", "DOC", "Tianguis", "HACKER" };
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < lugares.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    bool a = NavMesh.SamplePosition(lugares[i], out NavMeshHit pa, 8f, NavMesh.AllAreas);
                    bool b = NavMesh.SamplePosition(lugares[j], out NavMeshHit pb, 8f, NavMesh.AllAreas);
                    NavMeshPath camino = new NavMeshPath();
                    bool hay = a && b && NavMesh.CalculatePath(pa.position, pb.position, NavMesh.AllAreas, camino);
                    Debug.Log("[T-476] ruta " + nombres[i] + " -> " + nombres[j] + ": " + (hay ? camino.status.ToString() : "sin camino") + " muestreo " + (a ? Vector3.Distance(lugares[i], pa.position).ToString("F1") : "-") + "/" + (b ? Vector3.Distance(lugares[j], pb.position).ToString("F1") : "-")
                        + " fin del camino a " + (hay && camino.corners.Length > 0 ? Vector3.Distance(camino.corners[camino.corners.Length - 1], lugares[j]).ToString("F1") : "-") + " m");
                    Assert.IsTrue(hay && camino.corners.Length > 0 && Plano(camino.corners[camino.corners.Length - 1], lugares[j]) < 3f, nombres[i] + " -> " + nombres[j] + " llega a menos de 3 m del lugar");
                }
            }
        }

        [UnityTest]
        [Explicit("T-474/T-476: recorrido largo (más de 10 min); se corre a propósito con -testFilter, no en la suite")]
        [Timeout(3000000)]
        public IEnumerator DemoDePrincipioAFin()
        {
            tiempos.Clear();
            bugs.Clear();
            double inicioTotal = Time.realtimeSinceStartupAsDouble;

            // 1. Título.
            IniciarTramo("01 Título (opciones y créditos)");
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaTitulo titulo = null;
            yield return EsperarTitulo(t => titulo = t);
            yield return Segundos(1f);
            if (titulo.Continuar.interactable)
            {
                Bug("CONTINUAR activo sin partidas", "con la ruta de guardado vacía CONTINUAR debería estar desactivado", "A", true);
            }
            yield return Elegir(titulo.Menu, titulo.Opciones, "OPCIONES");
            yield return Pulsar(teclado.enterKey);
            yield return Segundos(0.5f);
            yield return Pulsar(teclado.escapeKey);
            if (!titulo.Abierta)
            {
                Bug("Esc en OPCIONES no vuelve al título", "el título no está abierto tras Esc", "A", true);
            }
            yield return Elegir(titulo.Menu, titulo.Creditos, "CRÉDITOS");
            yield return Pulsar(teclado.enterKey);
            PantallaCreditos creditos = Object.FindFirstObjectByType<PantallaCreditos>();
            yield return Segundos(0.5f);
            if (creditos == null || !creditos.Abierta)
            {
                Bug("El botón CRÉDITOS no abre los créditos", "PantallaCreditos " + (creditos == null ? "no existe" : "no está abierta"), "B", true);
            }
            else
            {
                float antes = creditos.Desplazamiento;
                yield return Segundos(2f);
                if (creditos.Desplazamiento <= antes)
                {
                    Bug("Los créditos no suben solos", "el desplazamiento no cambió en 2 s", "B", true);
                }
                yield return Pulsar(teclado.escapeKey);
                yield return Segundos(0.3f);
                if (creditos.Abierta || !titulo.Abierta)
                {
                    Bug("Esc en CRÉDITOS no vuelve al título", "créditos abiertos=" + creditos.Abierta + ", título abierto=" + titulo.Abierta, "B", true);
                }
            }
            TerminarTramo();

            // 2. Creación del personaje.
            IniciarTramo("02 Creación de personaje");
            yield return Elegir(titulo.Menu, titulo.Nueva, "NUEVA PARTIDA");
            yield return Pulsar(teclado.enterKey);
            PantallaCreacionPandilla creacion = Object.FindFirstObjectByType<PantallaCreacionPandilla>();
            Assert.IsTrue(creacion != null && creacion.Abierta, "Enter en NUEVA PARTIDA abre la creación");
            yield return Pulsar(teclado.downArrowKey);
            yield return Escribir(NombreJugador);
            if (creacion.Nombre.Valor != NombreJugador)
            {
                Bug("El nombre escrito no entra", "escribí " + NombreJugador + " y el campo dice " + creacion.Nombre.Valor, "A", true);
            }
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            if (!ReferenceEquals(creacion.Menu.Elegido, creacion.Empezar))
            {
                Bug("EMPEZAR no queda elegido con ↓", "elegido: " + (creacion.Menu.Elegido != null ? creacion.Menu.Elegido.Etiqueta : "nada"), "A", true);
            }
            DirectorPandillas directorViejo = Director;
            Presionar(teclado.enterKey);
            yield return null;
            Soltar(teclado.enterKey);
            DirectorPandillas director = null;
            yield return EsperarCiudad(directorViejo, d => director = d);
            yield return Segundos(1f);
            TerminarTramo();
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso);

            // 3. Despertar y salir de casa.
            IniciarTramo("03 Despertar y salir de casa");
            string ayudaInicio = director.Pantalla.Ayuda;
            if (ayudaInicio != DirectorPandillas.AyudaInicio)
            {
                Bug("Al despertar no sale MEGALOPOLIS · SUBURBIOS", "la ayuda dice \"" + ayudaInicio + "\"", "A", true);
            }
            if (TextoObjetivo != DirectorPandillas.TextoSalir)
            {
                Bug("Objetivo inicial incorrecto", "dice \"" + TextoObjetivo + "\"", "A", true);
            }
            yield return IrYUsar("la puerta de casa", () => director.Paso != PasoHistoria.Despierta, 60f, 0.1f);
            TerminarTramo();
            if (Environment.GetEnvironmentVariable("T474_HASTA") == "3")
            {
                yield break;
            }

            // 3b. T-728: el asalto. Asaltan a una persona (Enter salta la cinemática), el carro huye, se detiene (llanta, fin de la ruta o atorón) y vences al asaltante.
            IniciarTramo("03b Asalto: asaltan a una persona y huyen en carro");
            if (director.Paso == PasoHistoria.Asalto)
            {
                if (TextoObjetivo != AsaltoDinero.TextoRobarCarro)
                {
                    Bug("Objetivo del asalto incorrecto", "dice \"" + TextoObjetivo + "\"", "A", true);
                }
                yield return Segundos(1.5f);
                Presionar(teclado.enterKey);
                yield return null;
                Soltar(teclado.enterKey);
                float finAsalto = Time.realtimeSinceStartup + 100f;
                while (director.Asalto.Fase != FaseAsalto.Maletin && director.Paso == PasoHistoria.Asalto && Time.realtimeSinceStartup < finAsalto)
                {
                    yield return null;
                }
                if (director.Paso == PasoHistoria.Asalto)
                {
                    // T-987: ya no hay que vencer al asaltante: se recoge el maletín que soltaron con E.
                    if (director.Asalto.Maletin != null)
                    {
                        yield return Colocar(director.Asalto.Maletin.transform.position, 1.4f);
                        yield return Pulsar(teclado.eKey, 15);
                    }
                }
                if (director.Paso == PasoHistoria.Asalto)
                {
                    Bug("El asalto no terminó", "fase " + director.Asalto.Fase, "A", true);
                    director.Cargar("{\"paso\":\"PrimeraPelea\"}"); // se rescata para probar lo que sigue
                    yield return Segundos(1f);
                }
            }
            else
            {
                Bug("Salir de casa no lleva al asalto", "el paso es " + director.Paso, "A", true);
            }
            TerminarTramo();

            // 3c. T-718: el tutorial con las teclas reales. Lo que el bot ya hizo caminando (moverse) cuenta como visto; lo demás se ofrece, muestra la tecla real y se hace.
            IniciarTramo("03c Tutorial: correr, agacharse y golpear con las teclas reales");
            TutorialLigero tutorialLigero = Object.FindFirstObjectByType<TutorialLigero>();
            if (tutorialLigero == null)
            {
                Bug("No hay tutorial en la ciudad", "TutorialLigero no está en la escena", "A", true);
            }
            else
            {
                // El bot ya presionó esas teclas en los tramos anteriores (caminar, correr, agacharse, golpear, mapa): el tutorial las dio por vistas (Regla.Serializar).
                Debug.Log("[T718] tutorial activo " + tutorialLigero.Regla.Activo + ", vistas " + tutorialLigero.Regla.Serializar());
                foreach (AccionTutorial accion in new[] { AccionTutorial.Moverse, AccionTutorial.Correr, AccionTutorial.Agacharse, AccionTutorial.Golpear })
                {
                    float finAviso = Time.realtimeSinceStartup + 30f;
                    while (!tutorialLigero.Regla.Vista(accion) && !(tutorialLigero.Visible && tutorialLigero.Regla.Aviso == accion) && Time.realtimeSinceStartup < finAviso)
                    {
                        if (accion == AccionTutorial.Moverse)
                        {
                            Presionar(teclado.wKey);
                            yield return null;
                            Soltar(teclado.wKey);
                        }
                        yield return null;
                    }
                    if (tutorialLigero.Regla.Vista(accion) && !(tutorialLigero.Visible && tutorialLigero.Regla.Aviso == accion))
                    {
                        continue; // ya lo había hecho caminando o peleando
                    }
                    if (!(tutorialLigero.Visible && tutorialLigero.Regla.Aviso == accion))
                    {
                        Bug("El tutorial no ofrece " + accion, "no salió el aviso en 30 s", "A", true);
                        continue;
                    }
                    string esperada = ReglaTutorial.Texto(accion, tutorialLigero.TeclaReal);
                    if (tutorialLigero.Texto.text != esperada)
                    {
                        Bug("El aviso de " + accion + " no muestra las teclas reales", "dice \"" + tutorialLigero.Texto.text + "\", esperaba \"" + esperada + "\"", "A", true);
                    }
                    switch (accion)
                    {
                        case AccionTutorial.Moverse:
                            Presionar(teclado.wKey);
                            yield return null;
                            Soltar(teclado.wKey);
                            break;
                        case AccionTutorial.Correr:
                            Presionar(teclado.wKey);
                            Presionar(teclado.leftShiftKey);
                            yield return Segundos(0.4f);
                            Soltar(teclado.leftShiftKey);
                            Soltar(teclado.wKey);
                            break;
                        case AccionTutorial.Agacharse:
                            yield return Pulsar(teclado.leftCtrlKey);
                            yield return Segundos(0.3f);
                            yield return Pulsar(teclado.leftCtrlKey); // se levanta
                            break;
                        case AccionTutorial.Golpear:
                            Presionar(raton.leftButton);
                            yield return Segundos(0.2f);
                            Soltar(raton.leftButton);
                            break;
                    }
                    yield return null;
                    if (!tutorialLigero.Regla.Vista(accion))
                    {
                        Bug("El tutorial no dio por hecho " + accion, "la acción se hizo con la tecla real y el aviso sigue", "A", true);
                    }
                }
            }
            TerminarTramo();

            // 4. Primera pelea a puños.
            IniciarTramo("04 Primera pelea (2 SKULLS a golpes)");
            if (director.Paso == PasoHistoria.PrimeraPelea)
            {
                if (TextoObjetivo != DirectorPandillas.TextoDefender)
                {
                    Bug("Objetivo de la primera pelea incorrecto", "dice \"" + TextoObjetivo + "\"", "A", true);
                }
                yield return PelearAPunos(() => director.Paso != PasoHistoria.PrimeraPelea, p => p.name.StartsWith("CalaveraPunos"), 120f, "los 2 SKULLS");
            }
            else
            {
                Bug("Salir de casa no lleva a la primera pelea", "el paso es " + director.Paso, "A", true);
            }
            TerminarTramo();

            if (Environment.GetEnvironmentVariable("T474_HASTA") == "4")
            {
                yield break;
            }

            // 5. La pistola.
            IniciarTramo("05 Recoger la pistola");
            if (director.Paso == PasoHistoria.PrimeraPelea)
            {
                // La pelea no terminó: se rescata para probar lo que sigue (ya anotado como bug).
                foreach (PandilleroAPie p in new List<PandilleroAPie>(Enemigos()))
                {
                    p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
                }
                yield return Segundos(1f);
            }
            if (director.Paso == PasoHistoria.Pistola)
            {
                yield return IrYUsar("la pistola", () => director.Paso != PasoHistoria.Pistola, 60f);
            }
            else
            {
                Bug("Tras la pelea no toca recoger la pistola", "el paso es " + director.Paso, "A", true);
            }
            TerminarTramo();

            // 6. El CHATO.
            IniciarTramo("06 Paso del CHATO (T-930: avanza solo)");
            if (director.Paso == PasoHistoria.Chato)
            {
                yield return EsperarHasta(() => director.Paso != PasoHistoria.Chato, "el paso del CHATO avanza solo", 10f);
            }
            TerminarTramo();

            // 7. Pausa, guardar, salir al título, CONTINUAR.
            IniciarTramo("07 Pausa, guardado y CONTINUAR");
            Vector3 posAntes = Jugador.Raiz.position;
            PasoHistoria pasoAntes = director.Paso;
            PantallaPausa pausa = Object.FindFirstObjectByType<PantallaPausa>();
            yield return Pulsar(teclado.escapeKey);
            yield return Segundos(0.3f);
            if (pausa == null || !pausa.Abierta || !Pausa.Activa)
            {
                Bug("Esc no abre la pausa", "pausa " + (pausa == null ? "no existe" : "abierta=" + pausa.Abierta) + ", Pausa.Activa=" + Pausa.Activa, "A", true);
            }
            else
            {
                if (Time.timeScale != 0f)
                {
                    Bug("La pausa no detiene el tiempo", "timeScale=" + Time.timeScale, "A", true);
                }
                yield return Elegir(pausa.Menu, pausa.Guardar, "GUARDAR");
                yield return Pulsar(teclado.enterKey);
                yield return Segundos(0.3f);
                PantallaRanuras ranuras = pausa.Ranuras;
                if (!ranuras.Abierta)
                {
                    Bug("GUARDAR no abre las ranuras", "ranuras cerradas", "A", true);
                }
                else
                {
                    yield return Pulsar(teclado.enterKey);
                    yield return Segundos(0.5f);
                    if (ranuras.ConfirmacionAbierta)
                    {
                        yield return Pulsar(teclado.enterKey);
                    }
                    yield return Segundos(0.5f);
                    ResumenRanura r = ServicioGuardado.Actual.ResumenDe(1);
                    string rutaGuardado = ServicioGuardado.Actual.RutaRanura(1);
                    Debug.Log("[T-474] guardado " + rutaGuardado + ": " + (File.Exists(rutaGuardado) ? File.ReadAllText(rutaGuardado) : "(no existe)"));
                    if (r.Vacia)
                    {
                        Bug("Guardar en la ranura 1 no escribe nada", "ResumenDe(1).Vacia es verdadero tras Enter en la fila", "A", true);
                    }
                    if (ranuras.Abierta)
                    {
                        Bug("Guardar no cierra las ranuras", "tras guardar siguen abiertas", "A", true);
                        yield return Pulsar(teclado.escapeKey);
                    }
                }
                if (Environment.GetEnvironmentVariable("T474_SIN_CONTINUAR") == "1")
                {
                    yield return Pulsar(teclado.escapeKey);
                    TerminarTramo("sin salir al título (T474_SIN_CONTINUAR)");
                    goto SinContinuar;
                }
                yield return Elegir(pausa.Menu, pausa.Salir, "SALIR AL TÍTULO");
                yield return Pulsar(teclado.enterKey);
                yield return Segundos(0.3f);
                if (!pausa.ConfirmacionAbierta)
                {
                    Bug("SALIR AL TÍTULO no pregunta", "no apareció ¿SALIR?", "A", true);
                }
                else
                {
                    yield return Elegir(pausa.MenuConfirmar, pausa.Si, "SÍ");
                    DirectorPandillas anterior = director;
                    yield return Pulsar(teclado.enterKey);
                    yield return EsperarTitulo(t => titulo = t);
                    yield return Segundos(1f);
                    if (!titulo.Continuar.interactable)
                    {
                        Bug("CONTINUAR sigue desactivado tras guardar", "el título no ve la partida guardada", "A", true);
                    }
                    else
                    {
                        for (int rn = 0; rn <= 3; rn++)
                        {
                            string rr = ServicioGuardado.Actual.RutaRanura(rn);
                            string txt = File.Exists(rr) ? File.ReadAllText(rr) : "";
                            int ih = txt.IndexOf("historia_pandillas", StringComparison.Ordinal);
                            Debug.Log("[T-474] ranura " + rn + ": existe " + (txt.Length > 0) + (txt.Length > 0 ? " fecha " + ServicioGuardado.Actual.ResumenDe(rn).FechaUtc.ToString("O") + " " + txt.Substring(ih, 60) : ""));
                        }
                        Debug.Log("[T-474] CONTINUAR apunta a la ranura " + titulo.RanuraContinuar);
                        yield return Elegir(titulo.Menu, titulo.Continuar, "CONTINUAR");
                        // T-475: en el título la escena es otra (con su propio director); la ciudad nueva es la que sustituye a ESE director, no al del tramo 06.
                        anterior = Object.FindFirstObjectByType<DirectorPandillas>();
                        Presionar(teclado.enterKey);
                        yield return null;
                        Soltar(teclado.enterKey);
                        yield return EsperarCiudad(anterior, d => director = d, "CONTINUAR cargó la ciudad");
                        yield return Segundos(1.5f);
                        Debug.Log("[T-474] tras CONTINUAR: paso " + director.Paso + " (iniciado " + director.Iniciado + ") pos " + Jugador.Raiz.position);
                        yield return Segundos(3f);
                        Debug.Log("[T-474] 3 s después: paso " + director.Paso);
                        float lejos = Plano(Jugador.Raiz.position, posAntes);
                        if (lejos > 3f || director.Paso != pasoAntes)
                        {
                            Bug("CONTINUAR no te deja donde estabas", "posición a " + lejos.ToString("F1") + " m de donde guardé; paso " + director.Paso + " (esperado " + pasoAntes + ")", "A", true);
                            if (director.Paso != pasoAntes)
                            {
                                // Rescate (ya anotado): se restaura el paso para poder probar los tramos que siguen.
                                director.Cargar("{\"paso\":\"" + pasoAntes + "\"}");
                                yield return Segundos(0.5f);
                            }
                        }
                    }
                }
            }
            TerminarTramo();
            SinContinuar:

            // Solo para depurar el final: T474_DESDE=12 salta de aquí a Neón Alto (carga el paso y coloca al jugador en El Tianguis). No es parte del recorrido.
            if (Environment.GetEnvironmentVariable("T474_DESDE") == "12")
            {
                director.Cargar("{\"paso\":\"" + PasoHistoria.NeonAlto + "\"}");
                yield return Segundos(0.5f);
                yield return Colocar(new Vector3(-37f, 0.3f, 84f), 1.2f);
                if (float.TryParse(Environment.GetEnvironmentVariable("T474_ESPERA"), out float esperaDebug))
                {
                    yield return Segundos(esperaDebug);
                }
                goto TramoHacker;
            }

            // 8. EL DOC e EL INVENTOR.
            IniciarTramo("08 EL DOC (curar)");
            if (director.Paso == PasoHistoria.Doc)
            {
                yield return IrYUsar("EL DOC", () => director.Paso != PasoHistoria.Doc, 75f, 8f, DirectorPandillas.IdDoc);
            }
            else
            {
                Bug("Tras reclutar al CHATO no toca EL DOC", "el paso es " + director.Paso, "A", true);
            }
            TerminarTramo();
            IniciarTramo("09 EL INVENTOR (mejora de la pistola)");
            if (director.Paso == PasoHistoria.Inventor)
            {
                yield return IrYUsar("EL INVENTOR", () => director.Paso != PasoHistoria.Inventor, 75f, 8f, DirectorPandillas.IdInventor);
                // T-747: desde T-814 las mejoras de armas se compran en el banco de trabajo de Tino (BancoArmas), no en un mostrador (el MostradorCIC
                // está apagado a propósito y Tino solo manda al banco). El jugador se para frente al banco, E lo abre, Enter compra y Esc sale.
                comprando = true;
                ICartera cartera = Registro<ICartera>.Actual;
                int antesDeComprar = cartera.Dinero;
                MechaCyberpunk.UI.Armas.BancoArmas banco = Object.FindFirstObjectByType<MechaCyberpunk.UI.Armas.BancoArmas>();
                if (banco == null)
                {
                    Bug("El CIC no tiene banco de trabajo", "no hay BancoArmas en la escena", "A", true);
                }
                else
                {
                    // Junto a Tino el banco queda a un par de metros: primero E ahí; si no abre, el jugador se acerca al banco.
                    float inicioBanco = Time.realtimeSinceStartup;
                    bool acercado = false;
                    float finBanco = inicioBanco + 12f;
                    while (!banco.PantallaAbierta && Time.realtimeSinceStartup < finBanco)
                    {
                        if (!acercado && Time.realtimeSinceStartup - inicioBanco > 4f)
                        {
                            acercado = true;
                            yield return Colocar(banco.Posicion + Vector3.back * 1.0f, 1.2f);
                        }
                        yield return Pulsar(teclado.eKey, 20);
                        yield return Siguiente();
                    }
                    if (!banco.PantallaAbierta)
                    {
                        Bug("El banco del CIC no se abre", "12 s y varios E frente al banco de Tino y no está abierto (pausa " + Pausa.Activa + ", control " + Controlador.ControlActivo + ")", "A", true);
                    }
                    yield return Segundos(1f);
                    yield return Pulsar(teclado.enterKey);
                    yield return Pulsar(teclado.escapeKey, 10);
                    yield return Segundos(0.3f);
                    Debug.Log("[T-476] banco del CIC: dinero " + antesDeComprar + " -> " + cartera.Dinero + ", pausa " + Pausa.Activa + ", control " + Controlador.ControlActivo);
                    if (banco.PantallaAbierta)
                    {
                        Bug("Esc no cierra el banco del CIC", "el banco sigue abierto tras Esc", "A", true);
                    }
                }
                comprando = false;
                if (!Controlador.ControlActivo)
                {
                    Bug("Esc no devuelve el control tras el banco del CIC", "el control del jugador sigue apagado tras Esc", "A", true);
                }
            }
            TerminarTramo();

            if (Environment.GetEnvironmentVariable("T474_HASTA") == "9")
            {
                yield break;
            }

            // 10. Conquistar El Tianguis.
            IniciarTramo("10 El Tianguis (respeto 70)");
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (director.Paso == PasoHistoria.Tianguis)
            {
                PosicionMarcador(out zonaCentro);
                retirada = zonaCentro + (Jugador.Raiz.position - zonaCentro).normalized * 50f;
                ventajaDePrueba = true;
                yield return PelearConPistola(() => director.Paso != PasoHistoria.Tianguis, PandilleroCercaSinJefe, 200f, "los SKULLS del Tianguis"); // T-802: menos de 200 s
                if (director.Paso == PasoHistoria.Tianguis)
                {
                    rescateUsado = true;
                    // Rescate: los SKULLS restantes caen por daño para poder probar al jefe.
                    foreach (PandilleroAPie p in new List<PandilleroAPie>(Enemigos()))
                    {
                        p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
                    }
                    yield return Segundos(2f);
                }
            }
            ventajaDePrueba = false;
            TerminarTramo("respeto tianguis " + territorio.Respeto("tianguis") + ", curas de prueba " + curasDePrueba);

            if (Environment.GetEnvironmentVariable("T474_HASTA") == "10")
            {
                yield break;
            }

            // 11. El jefe.
            IniciarTramo("11 El jefe de los SKULLS");
            if (director.Paso == PasoHistoria.Jefe)
            {
                PosicionMarcador(out zonaCentro);
                retirada = zonaCentro + (Jugador.Raiz.position - zonaCentro).normalized * 50f;
                ventajaDePrueba = true;
                yield return PelearConPistola(() => director.Paso != PasoHistoria.Jefe, CercaDelMarcador, 180f, "el jefe de los SKULLS");
                ventajaDePrueba = false;
                if (director.Paso == PasoHistoria.Jefe)
                {
                    rescateUsado = true;
                    foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (j.Zona == "tianguis" && j.Jefe != null)
                        {
                            j.Jefe.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, j.Jefe.transform.position, Vector3.forward, null));
                        }
                    }
                    yield return Segundos(2f);
                }
            }
            else if (director.Paso == PasoHistoria.Tianguis)
            {
                Bug("El Tianguis no avanza al jefe", "sigue en Tianguis, respeto " + territorio.Respeto("tianguis"), "A", true);
            }
            TerminarTramo("tianguis es de " + territorio.Dueno("tianguis"));
            if (Environment.GetEnvironmentVariable("T474_HASTA") == "11")
            {
                yield break;
            }


            TramoHacker:
            // 12. Neón Alto: EL HACKER y CONTINUARÁ.
            IniciarTramo("12 Neón Alto con EL HACKER");
            if (director.Paso == PasoHistoria.NeonAlto)
            {
                yield return IrYUsar("EL HACKER", () => director.Paso != PasoHistoria.NeonAlto, 300f, 8f, DirectorPandillas.IdHacker);
                yield return Segundos(13f);
            }
            else
            {
                Bug("Vencer al jefe no lleva a Neón Alto", "el paso es " + director.Paso, "A", true);
            }
            TerminarTramo();

            IniciarTramo("13 CONTINUARÁ");
            if (director.Paso == PasoHistoria.Continuara)
            {
                yield return EsperarHasta(() => director.Pantalla.ContinuaraVisible, "pantalla CONTINUARÁ", 30f);
                yield return Segundos(1.2f);
                yield return Pulsar(teclado.enterKey);
                yield return Segundos(0.5f);
                if (director.Paso != PasoHistoria.Libre)
                {
                    Bug("Enter en CONTINUARÁ no sigue jugando", "el paso es " + director.Paso, "A", true);
                }
            }
            else
            {
                Bug("EL HACKER no lleva a CONTINUARÁ", "el paso es " + director.Paso, "A", true);
            }
            TerminarTramo();

            tiempos.Add("TOTAL | " + (Time.realtimeSinceStartupAsDouble - inicioTotal).ToString("F0") + " s | muertes del jugador " + muertes + " | bugs " + numeroBug + " | curas de prueba " + curasDePrueba + " | rescate " + rescateUsado);
            GuardarSalidas();
            Assert.AreEqual(PasoHistoria.Libre, director.Paso, "El recorrido llegó al juego libre (bugs anotados: " + numeroBug + ")");
            Assert.IsFalse(rescateUsado, "El bot venció a los SKULLS y al jefe sin que la prueba los matara (curas de prueba " + curasDePrueba + ")");
        }
    }
}
