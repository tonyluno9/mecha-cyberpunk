using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
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
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
    /// Correr (Unity cerrado): Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter MechaCyberpunk.Tests.Demo.RecorridoDemoPruebas (T474_HASTA=3 o 4 lo corta tras ese tramo; T474_SIN_CONTINUAR=1 no sale al título).
    /// </summary>
    public class RecorridoDemoPruebas : InputTestFixture
    {
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
        private int trazasMirada;
        private float finRumbo;
        private float finE;
        private PandilleroAPie blanco;
        private float desvioHasta;
        private float proximaMedida;
        private Vector3 posMedida;
        private float proximaTraza;
        private float proximaTraza2;
        private float desvio;
        private float ladoRumbo = 1f;

        // ---------- infraestructura ----------

        public override void Setup()
        {
            base.Setup();
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

        private void Cuadro()
        {
            cuadrosTramo++;
            peorCuadro = Mathf.Max(peorCuadro, Time.unscaledDeltaTime);
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
        private float MirarHacia(float rumbo, float fraccion)
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
            MoverRaton(new Vector2(enviar, 0f));
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

        // ---------- ruta por las calles (lo que un jugador hace leyendo el mapa) ----------

        private const float CeldaM = 1.5f;
        private const float MinX = -140f;
        private const float MinZ = -140f;
        private const int CeldasX = 190;
        private const int CeldasZ = 200;
        private bool[] libres;
        private List<Vector3> ruta;
        private int rutaIndice;
        private Vector3 rutaDestino;
        private float rutaHasta;

        private static Vector2Int CeldaDe(Vector3 p) => new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x - MinX) / CeldaM), 0, CeldasX - 1), Mathf.Clamp(Mathf.FloorToInt((p.z - MinZ) / CeldaM), 0, CeldasZ - 1));

        private static Vector3 CentroDe(Vector2Int c, float y) => new Vector3(MinX + (c.x + 0.5f) * CeldaM, y, MinZ + (c.y + 0.5f) * CeldaM);

        private readonly Collider[] choques = new Collider[16];

        // Rejilla de celdas por donde cabe el jugador (piso a menos de 1.8 m y sin nada sólido a su altura). Las hojas de puerta cuentan como libres.
        private void ConstruirRejilla()
        {
            libres = new bool[CeldasX * CeldasZ];
            for (int x = 0; x < CeldasX; x++)
            {
                for (int z = 0; z < CeldasZ; z++)
                {
                    Vector3 c = CentroDe(new Vector2Int(x, z), 0f);
                    if (!Physics.Raycast(c + Vector3.up * 2.4f, Vector3.down, out RaycastHit piso, 5f, ~0, QueryTriggerInteraction.Ignore) || piso.point.y > 1.8f)
                    {
                        continue;
                    }
                    Vector3 abajo = piso.point + Vector3.up * 0.5f;
                    Vector3 arriba = piso.point + Vector3.up * 1.5f;
                    int n = Physics.OverlapCapsuleNonAlloc(abajo, arriba, 0.3f, choques, ~0, QueryTriggerInteraction.Ignore);
                    bool libre = true;
                    for (int i = 0; i < n && libre; i++)
                    {
                        libre = choques[i].name == "Hoja" || choques[i].transform.IsChildOf(Jugador.Raiz);
                    }
                    libres[x * CeldasZ + z] = libre;
                }
            }
        }

        // A* en 8 direcciones desde el jugador hasta el destino; devuelve false si no hay camino.
        private bool CalcularRuta(Vector3 desde, Vector3 hasta)
        {
            if (libres == null)
            {
                ConstruirRejilla();
            }
            Vector2Int a = CeldaDe(desde);
            Vector2Int b = CeldaDe(hasta);
            int total = CeldasX * CeldasZ;
            float[] coste = new float[total];
            int[] padre = new int[total];
            for (int i = 0; i < total; i++)
            {
                coste[i] = float.MaxValue;
                padre[i] = -1;
            }
            // Las celdas de salida y de llegada se aceptan aunque caigan sobre algo (puerta, personaje).
            SortedSet<(float, int)> abiertos = new SortedSet<(float, int)>();
            int inicio = a.x * CeldasZ + a.y;
            int meta = b.x * CeldasZ + b.y;
            coste[inicio] = 0f;
            abiertos.Add((Vector2Int.Distance(a, b), inicio));
            while (abiertos.Count > 0)
            {
                (float _, int actual) = abiertos.Min;
                abiertos.Remove(abiertos.Min);
                if (actual == meta)
                {
                    break;
                }
                int cx = actual / CeldasZ;
                int cz = actual % CeldasZ;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = cx + dx;
                        int nz = cz + dz;
                        if ((dx == 0 && dz == 0) || nx < 0 || nz < 0 || nx >= CeldasX || nz >= CeldasZ)
                        {
                            continue;
                        }
                        int vecino = nx * CeldasZ + nz;
                        if (!libres[vecino] && vecino != meta)
                        {
                            continue;
                        }
                        if (dx != 0 && dz != 0 && (!libres[(cx + dx) * CeldasZ + cz] || !libres[cx * CeldasZ + cz + dz]))
                        {
                            continue;
                        }
                        float nuevo = coste[actual] + (dx != 0 && dz != 0 ? 1.414f : 1f);
                        if (nuevo < coste[vecino])
                        {
                            coste[vecino] = nuevo;
                            padre[vecino] = actual;
                            abiertos.Add((nuevo + Vector2Int.Distance(new Vector2Int(nx, nz), b), vecino));
                        }
                    }
                }
            }
            if (padre[meta] < 0 && inicio != meta)
            {
                ruta = null;
                return false;
            }
            List<Vector3> puntos = new List<Vector3>();
            for (int c = meta; c >= 0 && c != inicio; c = padre[c])
            {
                puntos.Add(CentroDe(new Vector2Int(c / CeldasZ, c % CeldasZ), desde.y));
            }
            puntos.Reverse();
            puntos.Add(hasta);
            ruta = puntos;
            rutaIndice = 0;
            rutaDestino = hasta;
            rutaHasta = Time.realtimeSinceStartup + 10f;
            return true;
        }

        // Un cuadro de caminata hacia el destino siguiendo la ruta por las calles (lejos) o directo (cerca).
        private void PasoRuta(Vector3 destino, bool correr)
        {
            Vector3 pos = Jugador.Raiz.position;
            if (Plano(pos, destino) > 25f)
            {
                if (ruta == null || Plano(rutaDestino, destino) > 4f || Time.realtimeSinceStartup > rutaHasta)
                {
                    CalcularRuta(pos, destino);
                }
                if (ruta != null)
                {
                    while (rutaIndice < ruta.Count - 1 && Plano(pos, ruta[rutaIndice]) < 1.5f)
                    {
                        rutaIndice++;
                    }
                    // Apunta a un punto unos metros adelante en la ruta (el más lejano que se ve sin obstáculo).
                    int objetivo = rutaIndice;
                    for (int i = rutaIndice; i < Mathf.Min(ruta.Count, rutaIndice + 5); i++)
                    {
                        if (Libre(pos, Rumbo(pos, ruta[i]), Mathf.Min(Plano(pos, ruta[i]), 6f)) >= Mathf.Min(Plano(pos, ruta[i]), 6f) - 0.01f)
                        {
                            objetivo = i;
                        }
                    }
                    PasoCaminar(ruta[objetivo], correr);
                    return;
                }
            }
            ruta = null;
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
                    Presionar(teclado.eKey);
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
            if (Physics.Raycast(punto + Vector3.up * 4f, Vector3.down, out RaycastHit piso, 10f))
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
                    PasoRuta(p, false);
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

        // Combate a distancia con la pistola: se acerca a ~12 m del enemigo más cercano, apunta con el ratón y dispara; recarga con R.
        private IEnumerator PelearConPistola(Func<bool> terminar, Func<PandilleroAPie, bool> filtro, float maximo, string que)
        {
            float fin = Time.realtimeSinceStartup + maximo;
            PistolaAPie pistola = Jugador.Raiz.GetComponentInChildren<PistolaAPie>();
            ArmasAPie armas = Jugador.Raiz.GetComponentInChildren<ArmasAPie>();
            if (armas != null && armas.ArmaActiva != ArmaAPie.Pistola)
            {
                yield return Pulsar(teclado.digit2Key);
            }
            int cuadro = 0;
            float proximaRecarga = 0f;
            while (!terminar())
            {
                if (Time.realtimeSinceStartup > fin)
                {
                    SoltarTeclas();
                    Soltar(raton.leftButton);
                    Soltar(raton.rightButton);
                    Bug("La pelea con pistola no termina: " + que, "tras " + maximo.ToString("F0") + " s sigue \"" + TextoObjetivo + "\"; enemigos vivos "
                        + CuentaEnemigos(), "B", true);
                    yield break;
                }
                yield return EsperarConVida();
                PandilleroAPie e = Blanco(filtro);
                Vector3 pos = Jugador.Raiz.position;
                if (e != null && Plano(e.transform.position, pos) < 60f)
                {
                    float d = Plano(e.transform.position, pos);
                    if (d > 13f)
                    {
                        PasoRuta(e.transform.position, true);
                        Soltar(raton.rightButton);
                        Soltar(raton.leftButton);
                    }
                    else
                    {
                        Soltar(teclado.leftShiftKey);
                        Soltar(teclado.wKey);
                        Presionar(raton.rightButton);
                        MirarHacia(Rumbo(pos, e.transform.position), 0.8f);
                        cuadro++;
                        if (pistola != null && pistola.Balas == 0 && !pistola.Recargando && Time.realtimeSinceStartup > proximaRecarga)
                        {
                            proximaRecarga = Time.realtimeSinceStartup + 1.5f;
                            Presionar(teclado.rKey);
                        }
                        else
                        {
                            Soltar(teclado.rKey);
                            if (Time.realtimeSinceStartup % 0.3f < 0.1f)
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
                else if (PosicionMarcador(out Vector3 destino))
                {
                    Soltar(raton.rightButton);
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
            Soltar(raton.rightButton);
            Soltar(teclado.rKey);
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

        [UnityTest]
        [Explicit("T-474: recorrido largo (unos 10 min); se corre a propósito con -testFilter, no en la suite")]
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
            IniciarTramo("06 Reclutar al CHATO");
            if (director.Paso == PasoHistoria.Chato)
            {
                yield return IrYUsar("EL CHATO", () => director.Paso != PasoHistoria.Chato, 120f);
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
            }
            TerminarTramo();

            // 10. Conquistar El Tianguis.
            IniciarTramo("10 El Tianguis (respeto 70)");
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (director.Paso == PasoHistoria.Tianguis)
            {
                yield return PelearConPistola(() => director.Paso != PasoHistoria.Tianguis, p => true, 100f, "los SKULLS del Tianguis");
                if (director.Paso == PasoHistoria.Tianguis)
                {
                    // Rescate: los SKULLS restantes caen por daño para poder probar al jefe.
                    foreach (PandilleroAPie p in new List<PandilleroAPie>(Enemigos()))
                    {
                        p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
                    }
                    yield return Segundos(2f);
                }
            }
            TerminarTramo("respeto tianguis " + territorio.Respeto("tianguis"));

            // 11. El jefe.
            IniciarTramo("11 El jefe de los SKULLS");
            if (director.Paso == PasoHistoria.Jefe)
            {
                yield return PelearConPistola(() => director.Paso != PasoHistoria.Jefe, p => true, 100f, "el jefe de los SKULLS");
                if (director.Paso == PasoHistoria.Jefe)
                {
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

            tiempos.Add("TOTAL | " + (Time.realtimeSinceStartupAsDouble - inicioTotal).ToString("F0") + " s | muertes del jugador " + muertes + " | bugs " + numeroBug);
            GuardarSalidas();
            Assert.AreEqual(PasoHistoria.Libre, director.Paso, "El recorrido llegó al juego libre (bugs anotados: " + numeroBug + ")");
        }
    }
}
