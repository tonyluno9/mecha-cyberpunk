using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Vehiculos.Carro;
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

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1361: recorrido automático en Play, en carro y a pie, por el mapa nuevo (Malecón, Autopista, Cerro y Puerto y sus uniones U1..U9) para encontrar bugs.
    /// Es un barrido: las pruebas NO fallan por los incidentes (atorado, golpe fuerte, escalón, caída al vacío, sin camino, errores de consola, tráfico atorado);
    /// fallan solo si no pueden ni empezar (sin ciudad, sin jugador, sin calles de la zona). Cada incidente deja una captura en docs/capturas/bugs-18nov/recorrido-&lt;parte&gt;-&lt;n&gt;.png
    /// y una línea en Logs/recorrido-zonas-&lt;parte&gt;.json; la traza cada 0.25 s va a Logs/recorrido-zonas-&lt;parte&gt;-traza.csv.
    /// En carro: un CarroViejo real con su física, manejado con W, A, S y D simuladas hacia la línea de la CalleZona a ~47 km/h.
    /// A pie: el jugador real con WASD, Mayús y ratón simulados; la ruta sale de un NavMesh horneado en ejecución (como RecorridoDemoPruebas).
    /// Una prueba por parte (cada una carga la ciudad y dura menos de 4 min por su presupuesto de tiempo).
    /// </summary>
    [Category("Zonas")]
    [Ignore("T-1483: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
    public class RecorridoZonasT1361Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string CarpetaCapturas = "docs/capturas/bugs-18nov";
        private const string CarpetaLogs = "Logs";
        private const float LimiteCargaJugador = 40f;
        private const float PresupuestoSegundos = 215f;
        private const float PresupuestoPaseoSegundos = 110f;
        private const int MaxCapturas = 30;
        private const int MaxErroresConsola = 15;
        private const float PasoRuta = 4f;
        private const float PasoMuestra = 0.25f;
        // Carro (m/s): 13 = 47 km/h en recta, 8 = 29 km/h en curva cerrada.
        private const float VelocidadRecta = 13f;
        private const float VelocidadCurva = 8f;
        private const float GradosCurva = 35f;
        private const float ZonaMuertaVolante = 5f;
        private const float SegundosAtascoCarro = 3f;
        private const float MetrosAtascoCarro = 1f;
        private const float CambioBruscoVelocidad = 8f;
        private const float SaltoVertical = 0.4f;
        private const float VentanaSalto = 0.2f;
        private const float CaidaAlVacioCarro = 2f;
        private const float SegundosTraficoAtorado = 5f;
        private const float DistanciaTrafico = 30f;
        private const float UnionCerca = 12f;
        // A pie.
        private const float SegundosAtascoPie = 3f;
        private const float MetrosAtascoPie = 0.5f;
        private const float CaidaInesperadaPie = 1.5f;
        private const float CaidaAlVacioPie = 4f;
        private const float SegundosSinSalida = 10f;
        private const float OffsetBanquetaMinimo = 4.5f;

        [Serializable]
        public class Incidente
        {
            public string tipo;
            public int gravedad;
            public Vector3 posicion;
            public string detalle;
            public string captura;
        }

        [Serializable]
        public class Informe
        {
            public string parte;
            public string modo;
            public float segundos;
            public float metros;
            public int omitidos;
            public string resumen;
            public List<Incidente> incidentes = new List<Incidente>();
        }

        private struct Muestra
        {
            public float t;
            public Vector3 p;
        }

        private class Cadena
        {
            public List<Vector3> puntos = new List<Vector3>();
            public float ancho = 14f;
        }

        private class Union
        {
            public string nombre;
            public List<Vector3> puntos = new List<Vector3>();
            public bool idaYVuelta;
        }

        private Keyboard teclado;
        private Mouse raton;
        private string parte = "";
        private string modo = "";
        private float inicioParte;
        private float finPresupuesto;
        private float metros;
        private int omitidos;
        private int capturas;
        private readonly List<Incidente> incidentes = new List<Incidente>();
        private readonly List<string> traza = new List<string>();
        private readonly Dictionary<string, (float t, Vector3 p)> ultimos = new Dictionary<string, (float t, Vector3 p)>();
        private readonly List<string> logsPendientes = new List<string>();
        private readonly HashSet<string> logsVistos = new HashSet<string>();
        private int erroresConsola;
        private HashSet<int> raicesAntes;
        private NavMeshDataInstance instanciaNavMesh;
        private Vector2 deltaRaton;
        private float gradosPorUnidad = 0.1f;
        private float ultimoEnviado;
        private float ultimoGiro;
        private bool hayUltimo;
        private bool llego;

        // ---------- infraestructura ----------

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += AlLog;
            incidentes.Clear();
            traza.Clear();
            ultimos.Clear();
            logsPendientes.Clear();
            logsVistos.Clear();
            erroresConsola = 0;
            omitidos = 0;
            capturas = 0;
            metros = 0f;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlLog;
            LogAssert.ignoreFailingMessages = false;
            if (instanciaNavMesh.valid)
            {
                NavMesh.RemoveNavMeshData(instanciaNavMesh);
            }
            Registros.LimpiarTodos();
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        // Los .inputactions son assets: guardan qué controles resolvieron en la prueba anterior (misma razón que MapasDePruebaTrafico).
        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                {
                    continue;
                }
                asset.Disable();
                asset.devices = dispositivos;
                asset.devices = null;
            }
        }

        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            Scene ciudad = SceneManager.GetActiveScene();
            if (ciudad.path == RutaEscena)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("VaciaZonasT1361"));
                yield return SceneManager.UnloadSceneAsync(ciudad);
                if (raicesAntes != null)
                {
                    foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.parent == null && !raicesAntes.Contains(x.gameObject.GetInstanceID())).ToArray())
                    {
                        Object.DestroyImmediate(t.gameObject);
                    }
                }
            }
            Registros.LimpiarTodos();
        }

        private void AlLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo != LogType.Error && tipo != LogType.Exception && tipo != LogType.Assert)
            {
                return;
            }
            string clave = mensaje.Length > 120 ? mensaje.Substring(0, 120) : mensaje;
            if (erroresConsola < MaxErroresConsola && logsVistos.Add(clave))
            {
                erroresConsola++;
                logsPendientes.Add("[" + tipo + "] " + (mensaje.Length > 300 ? mensaje.Substring(0, 300) : mensaje));
            }
        }

        private void DrenarLogs(Vector3 pos, Vector3 dir)
        {
            if (logsPendientes.Count == 0)
            {
                return;
            }
            string[] mensajes = logsPendientes.ToArray();
            logsPendientes.Clear();
            foreach (string m in mensajes)
            {
                Anotar("error-consola", 2, pos, dir, m, false);
            }
        }

        private IEnumerator CargarCiudad()
        {
            Registros.LimpiarTodos();
            raicesAntes = new HashSet<int>(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.parent == null).Select(t => t.gameObject.GetInstanceID()));
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float limite = Time.realtimeSinceStartup + LimiteCargaJugador;
            while (Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae al jugador");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        private void IniciarParte(string id, string modoParte)
        {
            parte = id;
            modo = modoParte;
            inicioParte = Time.realtimeSinceStartup;
            finPresupuesto = inicioParte + PresupuestoSegundos;
            Directory.CreateDirectory(CarpetaLogs);
            Directory.CreateDirectory(CarpetaCapturas);
        }

        private bool HayPresupuesto => Time.realtimeSinceStartup < finPresupuesto;

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Vector3 V(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        // ---------- incidentes, capturas e informe ----------

        private void Anotar(string tipo, int gravedad, Vector3 pos, Vector3 dir, string detalle, bool conCaptura = true)
        {
            if (ultimos.TryGetValue(tipo, out (float t, Vector3 p) u) && Time.time - u.t < 5f && Plano(u.p, pos) < 6f)
            {
                omitidos++;
                return;
            }
            ultimos[tipo] = (Time.time, pos);
            string archivo = "";
            if (conCaptura && capturas < MaxCapturas)
            {
                capturas++;
                archivo = Capturar(pos, dir, incidentes.Count + 1);
            }
            incidentes.Add(new Incidente { tipo = tipo, gravedad = gravedad, posicion = pos, detalle = detalle, captura = archivo });
            Debug.Log("[T-1361] " + parte + " #" + incidentes.Count + " " + tipo + " (gravedad " + gravedad + ") en " + pos.ToString("F1") + ": " + detalle);
        }

        // Cámara a 5 m detrás y 3 m arriba del punto, mirándolo, a una RenderTexture. Devuelve la ruta relativa del PNG ("" si falla).
        private string Capturar(Vector3 objetivo, Vector3 dir, int numero)
        {
            GameObject objeto = null;
            RenderTexture rt = null;
            Texture2D textura = null;
            try
            {
                Vector3 atras = new Vector3(dir.x, 0f, dir.z);
                atras = atras.sqrMagnitude < 0.01f ? Vector3.forward : atras.normalized;
                objeto = new GameObject("CamRecorridoT1361");
                Camera cam = objeto.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = 70f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 500f;
                cam.transform.position = objetivo - atras * 5f + Vector3.up * 3f;
                cam.transform.LookAt(objetivo + Vector3.up * 0.8f);
                rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture anterior = RenderTexture.active;
                RenderTexture.active = rt;
                textura = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                textura.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                textura.Apply();
                RenderTexture.active = anterior;
                string relativa = CarpetaCapturas + "/recorrido-" + parte + "-" + numero + ".png";
                File.WriteAllBytes(relativa, textura.EncodeToPNG());
                return relativa;
            }
            catch (Exception e)
            {
                Debug.Log("[T-1361] la captura " + numero + " falló: " + e.Message);
                return "";
            }
            finally
            {
                if (rt != null)
                {
                    rt.Release();
                    Object.Destroy(rt);
                }
                if (textura != null)
                {
                    Object.Destroy(textura);
                }
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
        }

        private void GuardarInforme()
        {
            SoltarTeclas();
            Dictionary<string, int> porTipo = new Dictionary<string, int>();
            foreach (Incidente i in incidentes)
            {
                porTipo.TryGetValue(i.tipo, out int n);
                porTipo[i.tipo] = n + 1;
            }
            StringBuilder resumen = new StringBuilder();
            foreach (KeyValuePair<string, int> par in porTipo)
            {
                resumen.Append(par.Key).Append('=').Append(par.Value).Append(' ');
            }
            Informe informe = new Informe
            {
                parte = parte,
                modo = modo,
                segundos = Time.realtimeSinceStartup - inicioParte,
                metros = metros,
                omitidos = omitidos,
                resumen = resumen.ToString().Trim(),
                incidentes = incidentes
            };
            Directory.CreateDirectory(CarpetaLogs);
            File.WriteAllText(CarpetaLogs + "/recorrido-zonas-" + parte + ".json", JsonUtility.ToJson(informe, true), new UTF8Encoding(false));
            File.WriteAllLines(CarpetaLogs + "/recorrido-zonas-" + parte + "-traza.csv", new[] { "t,x,y,z,velocidad,nota" }.Concat(traza), new UTF8Encoding(false));
            Debug.Log("[T-1361] RESUMEN " + parte + " (" + modo + "): " + incidentes.Count + " incidentes (" + informe.resumen + "), " + omitidos + " repetidos omitidos, "
                + metros.ToString("F0") + " m en " + informe.segundos.ToString("F0") + " s, " + capturas + " capturas, " + erroresConsola + " errores de consola");
        }

        // ---------- teclas y ratón ----------

        private void Poner(ButtonControl tecla, bool presionada)
        {
            if (tecla.isPressed == presionada)
            {
                return;
            }
            if (presionada)
            {
                Press(tecla);
            }
            else
            {
                Release(tecla);
            }
        }

        private void SoltarTeclas()
        {
            if (teclado == null)
            {
                return;
            }
            Poner(teclado.wKey, false);
            Poner(teclado.aKey, false);
            Poner(teclado.sKey, false);
            Poner(teclado.dKey, false);
            Poner(teclado.leftShiftKey, false);
            Poner(teclado.spaceKey, false);
            MoverRaton(Vector2.zero);
        }

        private void MoverRaton(Vector2 delta)
        {
            if (raton == null || (delta == Vector2.zero && deltaRaton == Vector2.zero))
            {
                return;
            }
            deltaRaton = delta;
            InputSystem.QueueStateEvent(raton, new MouseState { position = raton.position.ReadValue(), delta = deltaRaton });
        }

        // Gira la cámara con el ratón hacia un rumbo; aprende cuántos grados da cada unidad de delta (como la demo, T-474).
        private void MirarHacia(ControladorAPie ctrl, float rumbo, float fraccion)
        {
            if (hayUltimo && Mathf.Abs(ultimoEnviado) > 0.01f)
            {
                float estimada = Mathf.DeltaAngle(ultimoGiro, ctrl.GiroCamara) / ultimoEnviado;
                if (estimada > 0.0005f && estimada < 5f)
                {
                    gradosPorUnidad = Mathf.Lerp(gradosPorUnidad, estimada, 0.5f);
                }
            }
            float error = Mathf.DeltaAngle(ctrl.GiroCamara, rumbo);
            float enviar = Mathf.Abs(error) < 1f ? 0f : error * fraccion / gradosPorUnidad;
            ultimoEnviado = enviar;
            ultimoGiro = ctrl.GiroCamara;
            hayUltimo = true;
            MoverRaton(new Vector2(enviar, 0f));
        }

        // ---------- rutas ----------

        private static List<Vector3> Densificar(IList<Vector3> puntos, float paso)
        {
            List<Vector3> ruta = new List<Vector3>();
            if (puntos.Count == 0)
            {
                return ruta;
            }
            ruta.Add(puntos[0]);
            for (int i = 1; i < puntos.Count; i++)
            {
                Vector3 a = puntos[i - 1];
                Vector3 b = puntos[i];
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / paso));
                for (int k = 1; k <= n; k++)
                {
                    ruta.Add(Vector3.Lerp(a, b, k / (float)n));
                }
            }
            return ruta;
        }

        private static bool Cerca(Vector3 a, Vector3 b)
        {
            return Plano(a, b) < UnionCerca / 3f && Mathf.Abs(a.y - b.y) < 2f;
        }

        // Une las CalleZona de la zona que se tocan por los extremos en cadenas continuas.
        private static List<Cadena> CadenasDeZona(string zona)
        {
            List<(List<Vector3> puntos, float ancho)> restantes = CalleZona.Activas.Where(c => c.Zona == zona && c.Cantidad >= 2).OrderBy(c => c.name)
                .Select(c => (c.Puntos(), c.Ancho)).ToList();
            List<Cadena> cadenas = new List<Cadena>();
            while (restantes.Count > 0)
            {
                Cadena actual = new Cadena { puntos = new List<Vector3>(restantes[0].puntos), ancho = restantes[0].ancho };
                restantes.RemoveAt(0);
                bool seguir = true;
                while (seguir)
                {
                    seguir = false;
                    for (int i = 0; i < restantes.Count; i++)
                    {
                        List<Vector3> otra = restantes[i].puntos;
                        Vector3 fin = actual.puntos[actual.puntos.Count - 1];
                        if (Cerca(fin, otra[0]))
                        {
                            actual.puntos.AddRange(otra.Skip(1));
                        }
                        else if (Cerca(fin, otra[otra.Count - 1]))
                        {
                            List<Vector3> al = new List<Vector3>(otra);
                            al.Reverse();
                            actual.puntos.AddRange(al.Skip(1));
                        }
                        else
                        {
                            continue;
                        }
                        restantes.RemoveAt(i);
                        seguir = true;
                        break;
                    }
                }
                cadenas.Add(actual);
            }
            return cadenas;
        }

        // Los puntos de la calle de la zona más cercana a <paramref name="punto"/>, hacia el lado largo, hasta <paramref name="largo"/> m.
        private static List<Vector3> ContinuarDesde(Vector3 punto, string zona, float largo)
        {
            float mejor = float.MaxValue;
            List<Vector3> mejorLinea = null;
            int mejorIndice = 0;
            foreach (CalleZona calle in CalleZona.Activas.Where(c => c.Zona == zona && c.Cantidad >= 2))
            {
                List<Vector3> linea = Densificar(calle.Puntos(), 5f);
                for (int i = 0; i < linea.Count; i++)
                {
                    float d = Plano(linea[i], punto) + Mathf.Abs(linea[i].y - punto.y) * 0.5f;
                    if (d < mejor)
                    {
                        mejor = d;
                        mejorLinea = linea;
                        mejorIndice = i;
                    }
                }
            }
            List<Vector3> resultado = new List<Vector3>();
            if (mejorLinea == null)
            {
                return resultado;
            }
            int paso = mejorLinea.Count - 1 - mejorIndice >= mejorIndice ? 1 : -1;
            float acumulado = 0f;
            for (int i = mejorIndice; i >= 0 && i < mejorLinea.Count && acumulado < largo; i += paso)
            {
                if (resultado.Count > 0)
                {
                    acumulado += Vector3.Distance(resultado[resultado.Count - 1], mejorLinea[i]);
                }
                resultado.Add(mejorLinea[i]);
            }
            return resultado;
        }

        // y real del piso bajo (x, z) cerca de la altura esperada (ignora carros y al jugador).
        private static float AlturaDePiso(Vector3 p)
        {
            RaycastHit[] golpes = Physics.RaycastAll(new Vector3(p.x, p.y + 3f, p.z), Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(golpes, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.GetComponentInParent<CarroViejo>() == null && g.collider.GetComponentInParent<IJugador>() == null && g.collider.GetComponentInParent<CharacterController>() == null)
                {
                    return g.point.y;
                }
            }
            return p.y;
        }

        private static Vector3 ConPiso(Vector3 p)
        {
            return new Vector3(p.x, AlturaDePiso(p), p.z);
        }

        private static List<Union> Uniones()
        {
            List<Union> lista = new List<Union>();
            Union u1 = new Union { nombre = "U1" };
            u1.puntos.AddRange(new[] { V(-47f, 0f, -170f), V(-47f, 0f, -201f), V(-47f, 0f, -226f) }.Select(ConPiso));
            u1.puntos.AddRange(ContinuarDesde(V(-47f, 0f, -226f), "Malecon", 60f).Skip(1));
            lista.Add(u1);
            Union u2 = new Union { nombre = "U2" };
            u2.puntos.AddRange(new[] { V(47f, 0f, -170f), V(47f, 0f, -201f), V(47f, 0f, -226f) }.Select(ConPiso));
            u2.puntos.AddRange(ContinuarDesde(V(47f, 0f, -226f), "Malecon", 60f).Skip(1));
            lista.Add(u2);
            Union u4 = new Union { nombre = "U4_Cerro", idaYVuelta = true };
            u4.puntos.AddRange(new[] { V(-200f, 0f, 0f), V(-249f, 0f, 0f), V(-305f, 0f, 0f) }.Select(ConPiso));
            u4.puntos.AddRange(ContinuarDesde(V(-305f, 0f, 0f), "Cerro", 200f).Skip(1));
            lista.Add(u4);
            Union u9 = new Union { nombre = "U9" };
            u9.puntos.AddRange(new[] { V(300f, 0f, 40f), V(300f, 0f, 0f), V(249f, 0f, 0f), V(200f, 0f, 0f) }.Select(ConPiso));
            lista.Add(u9);
            Union u7 = new Union { nombre = "U7_RampaNorte", idaYVuelta = true };
            u7.puntos.AddRange(new[] { V(-107f, 0f, 170f), V(-107f, 0f, 201f), V(-120f, 0f, 215f) }.Select(ConPiso));
            u7.puntos.AddRange(ContinuarDesde(V(-120f, 0f, 215f), "Autopista", 160f).Skip(1));
            lista.Add(u7);
            Union u5 = new Union { nombre = "U5_PuertoOeste", idaYVuelta = true };
            u5.puntos.AddRange(new[] { V(110f, 0f, -215f), V(160f, 0f, -215f) }.Select(ConPiso));
            u5.puntos.AddRange(ContinuarDesde(V(160f, 0f, -215f), "Puerto", 150f).Skip(1));
            lista.Add(u5);
            Union u8 = new Union { nombre = "U8_PuertoNorte", idaYVuelta = true };
            u8.puntos.AddRange(new[] { V(300f, 0f, 40f), V(300f, 0f, 0f), V(300f, 0f, -45f) }.Select(ConPiso));
            u8.puntos.AddRange(ContinuarDesde(V(300f, 0f, -45f), "Puerto", 150f).Skip(1));
            lista.Add(u8);
            return lista;
        }

        // Puntos desplazados al lado derecho de la marcha (la banqueta).
        private static List<Vector3> ConDesplazamiento(IList<Vector3> puntos, float lateral)
        {
            List<Vector3> r = new List<Vector3>();
            for (int i = 0; i < puntos.Count; i++)
            {
                Vector3 a = puntos[Mathf.Max(0, i - 1)];
                Vector3 b = puntos[Mathf.Min(puntos.Count - 1, i + 1)];
                Vector3 t = new Vector3(b.x - a.x, 0f, b.z - a.z);
                t = t.sqrMagnitude < 0.0001f ? Vector3.forward : t.normalized;
                r.Add(puntos[i] + Vector3.Cross(Vector3.up, t) * lateral);
            }
            return r;
        }

        private static float OffsetBanqueta(float ancho)
        {
            return Mathf.Max(OffsetBanquetaMinimo, ancho * 0.5f - 1.5f);
        }

        private static Bounds LimitesDe(IEnumerable<Vector3> puntos, float margen)
        {
            Bounds b = new Bounds(puntos.First(), Vector3.zero);
            foreach (Vector3 p in puntos)
            {
                b.Encapsulate(p);
            }
            b.Expand(new Vector3(margen * 2f, 0f, margen * 2f));
            b.Expand(new Vector3(0f, 24f, 0f));
            return b;
        }

        // ---------- EN CARRO ----------

        private static CarroViejo CrearCarro(Vector3 inicio)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            Assert.IsNotNull(prefab, "existe " + RutaCarro);
            GameObject objeto = Object.Instantiate(prefab, inicio + Vector3.up * 0.6f, Quaternion.identity);
            CarroViejo creado = objeto.GetComponent<CarroViejo>();
            Assert.IsNotNull(creado, "el prefab trae CarroViejo");
            return creado;
        }

        private IEnumerator EsperarYMontar(CarroViejo carro)
        {
            Physics.SyncTransforms();
            yield return Esperar(1f);
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            bool montado = jugador != null && jugador.Subir(carro);
            if (!montado)
            {
                Debug.Log("[T-1361] el jugador no pudo subir al carro; se toma el control directo (el tráfico no sigue al carro)");
                carro.TomarControl(true);
            }
            yield return Esperar(0.5f);
        }

        private static void ColocarCarro(CarroViejo carro, Rigidbody cuerpo, Vector3 p, Vector3 haciaPunto)
        {
            Vector3 d = haciaPunto - p;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f)
            {
                d = Vector3.forward;
            }
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            Vector3 destino = p + Vector3.up * 0.6f;
            carro.transform.SetPositionAndRotation(destino, Quaternion.LookRotation(d, Vector3.up));
            cuerpo.position = destino;
            cuerpo.rotation = Quaternion.LookRotation(d, Vector3.up);
            Physics.SyncTransforms();
            cuerpo.WakeUp();
        }

        private static string QuienChoca(CarroViejo carro, Vector3 pos)
        {
            foreach (Collider c in Physics.OverlapSphere(pos, 3.5f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => Vector3.Distance(x.ClosestPoint(pos), pos)))
            {
                if (c.transform.IsChildOf(carro.transform) || c.GetComponentInParent<IJugador>() != null)
                {
                    continue;
                }
                Transform raiz = c.transform;
                string ruta = raiz.name;
                for (int i = 0; i < 3 && raiz.parent != null; i++)
                {
                    raiz = raiz.parent;
                    ruta = raiz.name + "/" + ruta;
                }
                return ruta;
            }
            return "nada a 3.5 m (choque con algo que no se ve)";
        }

        private IEnumerator Conducir(CarroViejo carro, List<Vector3> linea, string nombre, bool contarTrafico, Dictionary<Vector3, bool> cubiertos)
        {
            List<Vector3> ruta = Densificar(linea, PasoRuta);
            if (ruta.Count < 3)
            {
                yield break;
            }
            Rigidbody cuerpo = carro.GetComponent<Rigidbody>();
            ColocarCarro(carro, cuerpo, ruta[0], ruta[Mathf.Min(2, ruta.Count - 1)]);
            yield return Esperar(0.7f);
            List<Muestra> historial = new List<Muestra>();
            Dictionary<CarroTrafico, (Vector3 ancla, float desde)> anclas = new Dictionary<CarroTrafico, (Vector3 ancla, float desde)>();
            HashSet<CarroTrafico> reportados = new HashSet<CarroTrafico>();
            int indice = 0;
            int atascos = 0;
            float reversaHasta = 0f;
            float sinAtascoHasta = Time.time + SegundosAtascoCarro + 0.5f;
            float volcadoDesde = -1f;
            float proximaMuestra = Time.time;
            float inicio = Time.time;
            float limite = inicio + ruta.Count * PasoRuta / 3.5f + 20f;
            Vector3 velocidadPrevia = cuerpo.linearVelocity;
            Vector3 posicionPrevia = carro.transform.position;
            int incidentesAntes = incidentes.Count;
            Debug.Log("[T-1361] " + parte + " tramo " + nombre + ": " + ruta.Count + " puntos, " + (ruta.Count * PasoRuta).ToString("F0") + " m, de " + ruta[0].ToString("F0") + " a " + ruta[ruta.Count - 1].ToString("F0"));

            while (Time.time < limite && indice < ruta.Count - 2 && HayPresupuesto)
            {
                yield return null;
                float dt = Time.deltaTime;
                Vector3 pos = carro.transform.position;
                Vector3 vel = cuerpo.linearVelocity;
                float rapidez = vel.magnitude;
                Vector3 adelante = Vector3.ProjectOnPlane(carro.transform.forward, Vector3.up);
                metros += Plano(pos, posicionPrevia);
                posicionPrevia = pos;

                // Avanza el índice al punto más cercano de la ruta (ventana hacia adelante).
                float mejorD = Plano(pos, ruta[indice]);
                for (int j = indice + 1; j < Mathf.Min(indice + 25, ruta.Count); j++)
                {
                    float d = Plano(pos, ruta[j]);
                    if (d < mejorD)
                    {
                        mejorD = d;
                        indice = j;
                    }
                }
                if (cubiertos != null)
                {
                    foreach (Vector3 clave in cubiertos.Keys.ToArray())
                    {
                        if (Plano(pos, clave) < UnionCerca && Mathf.Abs(pos.y - clave.y) < 6f)
                        {
                            cubiertos[clave] = true;
                        }
                    }
                }

                // Control: girar hacia el punto a ~10-25 m y llevar la velocidad.
                bool enReversa = Time.time < reversaHasta;
                int adelanteIdx = Mathf.Clamp(Mathf.RoundToInt(2f + rapidez * 0.7f / PasoRuta), 2, 6);
                Vector3 meta = ruta[Mathf.Min(indice + adelanteIdx, ruta.Count - 1)];
                Vector3 lejos = ruta[Mathf.Min(indice + adelanteIdx + 5, ruta.Count - 1)];
                float angulo = Vector3.SignedAngle(adelante, new Vector3(meta.x - pos.x, 0f, meta.z - pos.z), Vector3.up);
                float anguloLejos = Vector3.SignedAngle(adelante, new Vector3(lejos.x - pos.x, 0f, lejos.z - pos.z), Vector3.up);
                float objetivo = Mathf.Max(Mathf.Abs(angulo), Mathf.Abs(anguloLejos)) > GradosCurva ? VelocidadCurva : VelocidadRecta;
                float giro = enReversa ? -angulo : angulo;
                Poner(teclado.dKey, giro > ZonaMuertaVolante);
                Poner(teclado.aKey, giro < -ZonaMuertaVolante);
                Poner(teclado.wKey, !enReversa && rapidez < objetivo);
                Poner(teclado.sKey, enReversa || rapidez > objetivo + 4f);

                // Incidentes.
                if (pos.y < ruta[indice].y - CaidaAlVacioCarro)
                {
                    Anotar("caida-al-vacio", 4, pos, adelante, nombre + ": el carro cayó " + (ruta[indice].y - pos.y).ToString("F1") + " m bajo la calle (calle y=" + ruta[indice].y.ToString("F1") + ")");
                    break;
                }
                if (dt > 0f && dt < 0.1f && (vel - velocidadPrevia).magnitude > CambioBruscoVelocidad)
                {
                    Anotar("golpe-fuerte", 3, pos, adelante, nombre + ": cambio de " + (vel - velocidadPrevia).magnitude.ToString("F1") + " m/s en un cuadro (de " + velocidadPrevia.magnitude.ToString("F1") + " a " + rapidez.ToString("F1") + "); cerca: " + QuienChoca(carro, pos));
                }
                velocidadPrevia = vel;
                historial.Add(new Muestra { t = Time.time, p = pos });
                while (historial.Count > 0 && Time.time - historial[0].t > SegundosAtascoCarro + 0.3f)
                {
                    historial.RemoveAt(0);
                }
                float minY = pos.y;
                float maxY = pos.y;
                for (int k = historial.Count - 1; k >= 0 && Time.time - historial[k].t <= VentanaSalto; k--)
                {
                    minY = Mathf.Min(minY, historial[k].p.y);
                    maxY = Mathf.Max(maxY, historial[k].p.y);
                }
                if (maxY - minY > SaltoVertical)
                {
                    Anotar("escalon", 2, pos, adelante, nombre + ": salto vertical de " + (maxY - minY).ToString("F2") + " m en " + VentanaSalto.ToString("F1") + " s (a " + rapidez.ToString("F1") + " m/s); cerca: " + QuienChoca(carro, pos));
                }
                if (carro.transform.up.y < 0.3f)
                {
                    volcadoDesde = volcadoDesde < 0f ? Time.time : volcadoDesde;
                    if (Time.time - volcadoDesde > 2f)
                    {
                        Anotar("carro-volcado", 3, pos, adelante, nombre + ": el carro quedó volcado; se endereza en la ruta");
                        ColocarCarro(carro, cuerpo, ruta[indice], ruta[Mathf.Min(indice + 2, ruta.Count - 1)]);
                        volcadoDesde = -1f;
                        historial.Clear();
                        velocidadPrevia = Vector3.zero;
                    }
                }
                else
                {
                    volcadoDesde = -1f;
                }
                if (!enReversa && Time.time >= sinAtascoHasta && historial.Count > 0 && Time.time - historial[0].t >= SegundosAtascoCarro && Plano(pos, historial[0].p) < MetrosAtascoCarro)
                {
                    atascos++;
                    Anotar("atorado", 3, pos, adelante, nombre + ": avanzó menos de " + MetrosAtascoCarro.ToString("F0") + " m en " + SegundosAtascoCarro.ToString("F0") + " s (intento " + atascos + "); cerca: " + QuienChoca(carro, pos));
                    sinAtascoHasta = Time.time + SegundosAtascoCarro + 1.5f;
                    if (atascos >= 4)
                    {
                        Debug.Log("[T-1361] " + parte + " tramo " + nombre + ": se abandona tras " + atascos + " atascos");
                        break;
                    }
                    if (atascos % 2 == 0)
                    {
                        // Segundo atasco seguido: salta 12 m adelante en la ruta para seguir barriendo.
                        int salto = Mathf.Min(indice + 3, ruta.Count - 2);
                        ColocarCarro(carro, cuerpo, ruta[salto], ruta[Mathf.Min(salto + 2, ruta.Count - 1)]);
                        indice = salto;
                        historial.Clear();
                        velocidadPrevia = Vector3.zero;
                    }
                    else
                    {
                        reversaHasta = Time.time + 1.5f;
                    }
                }

                if (Time.time >= proximaMuestra)
                {
                    proximaMuestra = Time.time + PasoMuestra;
                    traza.Add(Time.time.ToString("F2") + "," + pos.x.ToString("F1") + "," + pos.y.ToString("F2") + "," + pos.z.ToString("F1") + "," + rapidez.ToString("F1") + "," + nombre + (enReversa ? " reversa" : ""));
                    DrenarLogs(pos, adelante);
                    if (contarTrafico)
                    {
                        RevisarTrafico(pos, adelante, anclas, reportados);
                    }
                }
            }
            SoltarTeclas();
            Debug.Log("[T-1361] " + parte + " tramo " + nombre + " terminó: índice " + indice + "/" + (ruta.Count - 1) + ", " + (Time.time - inicio).ToString("F0") + " s, " + (incidentes.Count - incidentesAntes) + " incidentes");
        }

        private void RevisarTrafico(Vector3 pos, Vector3 dir, Dictionary<CarroTrafico, (Vector3 ancla, float desde)> anclas, HashSet<CarroTrafico> reportados)
        {
            foreach (CarroTrafico c in CarroTrafico.Activos.ToArray())
            {
                if (c == null || !c.gameObject.activeInHierarchy || Plano(c.transform.position, pos) > DistanciaTrafico)
                {
                    continue;
                }
                Vector3 p = c.transform.position;
                if (!anclas.TryGetValue(c, out (Vector3 ancla, float desde) a) || Vector3.Distance(p, a.ancla) > 1f)
                {
                    anclas[c] = (p, Time.time);
                }
                else if (Time.time - a.desde >= SegundosTraficoAtorado && reportados.Add(c))
                {
                    Anotar("trafico-atorado", 1, p, dir, c.name + " no avanza hace " + (Time.time - a.desde).ToString("F0") + " s (causa " + c.Causa + ", bloqueado " + c.Bloqueado + ", chocado " + c.Chocado + ", pitidos " + c.Pitidos + ")");
                }
            }
        }

        private IEnumerator BarridoEnCarro(string id, string zona, bool contarTrafico, Dictionary<Vector3, bool> cubiertos)
        {
            yield return CargarCiudad();
            List<Cadena> cadenas = CadenasDeZona(zona);
            Assert.IsNotEmpty(cadenas, "hay CalleZona de " + zona);
            IniciarParte(id, "carro");
            CarroViejo carro = CrearCarro(cadenas[0].puntos[0]);
            yield return EsperarYMontar(carro);
            try
            {
                int n = 0;
                foreach (Cadena cadena in cadenas)
                {
                    if (!HayPresupuesto)
                    {
                        Anotar("sin-presupuesto", 1, cadena.puntos[0], Vector3.forward, "no alcanzó el tiempo para el tramo " + n + " en adelante", false);
                        break;
                    }
                    yield return Conducir(carro, cadena.puntos, zona + "_cadena" + n++, contarTrafico, cubiertos);
                }
                if (cubiertos != null)
                {
                    foreach (KeyValuePair<Vector3, bool> par in cubiertos.Where(p => !p.Value))
                    {
                        Anotar("no-cubierto", 1, par.Key, Vector3.forward, "el carro no pasó a menos de " + UnionCerca + " m de " + par.Key.ToString("F0") + " (las cadenas de CalleZona no llegan ahí)", false);
                    }
                }
            }
            finally
            {
                GuardarInforme();
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Malecon_EnCarro()
        {
            yield return BarridoEnCarro("malecon-carro", "Malecon", true, null);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Autopista_EnCarro()
        {
            // Puntos que deben recorrerse: rampa norte, autopista, curva, rampa este y túnel (plan-zonas-18nov.md, U6, U7 y U8).
            Dictionary<Vector3, bool> cubiertos = new Dictionary<Vector3, bool>
            {
                { V(-120f, 0f, 215f), false }, { V(30f, 10f, 250f), false }, { V(240f, 10f, 250f), false },
                { V(300f, 10f, 190f), false }, { V(300f, 0f, 60f), false }, { V(-330f, 10f, 250f), false }, { V(-360f, 10f, 185f), false }
            };
            yield return BarridoEnCarro("autopista-carro", "Autopista", true, cubiertos);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Uniones_EnCarro()
        {
            yield return CargarCiudad();
            List<Union> uniones = Uniones();
            Assert.IsNotEmpty(uniones.Where(u => u.puntos.Count >= 3), "hay uniones armadas");
            IniciarParte("uniones-carro", "carro");
            CarroViejo carro = CrearCarro(uniones[0].puntos[0]);
            yield return EsperarYMontar(carro);
            try
            {
                foreach (Union u in uniones)
                {
                    if (!HayPresupuesto)
                    {
                        Anotar("sin-presupuesto", 1, u.puntos[0], Vector3.forward, "no alcanzó el tiempo para " + u.nombre + " en adelante", false);
                        break;
                    }
                    yield return Conducir(carro, u.puntos, u.nombre, false, null);
                    if (u.idaYVuelta && HayPresupuesto)
                    {
                        List<Vector3> vuelta = new List<Vector3>(u.puntos);
                        vuelta.Reverse();
                        yield return Conducir(carro, vuelta, u.nombre + "_salida", false, null);
                    }
                }
            }
            finally
            {
                GuardarInforme();
            }
        }

        // ---------- A PIE ----------

        private void HornearNavMesh(Bounds limites)
        {
            if (instanciaNavMesh.valid)
            {
                NavMesh.RemoveNavMeshData(instanciaNavMesh);
            }
            List<NavMeshBuildSource> fuentes = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(limites, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), fuentes);
            int antes = fuentes.Count;
            IJugador jugador = Registro<IJugador>.Actual;
            fuentes.RemoveAll(s =>
            {
                Collider c = s.component as Collider;
                if (c == null)
                {
                    return false;
                }
                return c.isTrigger || c.attachedRigidbody != null || c is CharacterController || c.GetComponentInParent<CharacterController>() != null
                    || (jugador != null && c.transform.IsChildOf(jugador.Raiz)) || (c is MeshCollider mc && mc.sharedMesh != null && !mc.sharedMesh.isReadable);
            });
            NavMeshBuildSettings ajustes = NavMesh.CreateSettings();
            ajustes.agentRadius = 0.35f;
            ajustes.agentHeight = 1.7f;
            ajustes.agentClimb = 0.4f;
            ajustes.agentSlope = 40f;
            ajustes.minRegionArea = 0.5f;
            NavMeshData datos = NavMeshBuilder.BuildNavMeshData(ajustes, fuentes, limites, Vector3.zero, Quaternion.identity);
            instanciaNavMesh = NavMesh.AddNavMeshData(datos);
            Debug.Log("[T-1361] " + parte + " NavMesh horneado en " + limites.center.ToString("F0") + " tamaño " + limites.size.ToString("F0") + ": " + fuentes.Count + " fuentes (de " + antes + ")");
        }

        private void Paso(ControladorAPie ctrl, Vector3 pos, Vector3 meta, bool correr)
        {
            float rumbo = Mathf.Atan2(meta.x - pos.x, meta.z - pos.z) * Mathf.Rad2Deg;
            MirarHacia(ctrl, rumbo, 0.5f);
            float rel = Mathf.DeltaAngle(ctrl.GiroCamara, rumbo);
            float a = Mathf.Abs(rel);
            Poner(teclado.wKey, a < 67.5f);
            Poner(teclado.sKey, a > 112.5f);
            Poner(teclado.dKey, rel > 22.5f && rel < 157.5f);
            Poner(teclado.aKey, rel < -22.5f && rel > -157.5f);
            Poner(teclado.leftShiftKey, correr);
        }

        private static void Teletransportar(IJugador jugador, Vector3 destino)
        {
            jugador.Colocar(new Pose(destino + Vector3.up * 0.2f, jugador.Raiz.rotation));
        }

        // Camina hasta <paramref name="meta"/> (plano, 1.2 m). Pone llego=true si lo logra; si no, anota el incidente y deja al jugador donde quedó.
        private IEnumerator Ir(ControladorAPie ctrl, IJugador jugador, Vector3 meta, bool correr, string nombre)
        {
            llego = false;
            hayUltimo = false;
            float inicio = Time.time;
            float limite = inicio + SegundosSinSalida + Plano(jugador.Raiz.position, meta) / 2.5f;
            int atascos = 0;
            Vector3 referencia = jugador.Raiz.position;
            float proximaReferencia = Time.time + SegundosAtascoPie;
            float proximaMuestra = Time.time;
            List<Muestra> historial = new List<Muestra>();
            Vector3 anterior = jugador.Raiz.position;
            while (HayPresupuesto)
            {
                Vector3 pos = jugador.Raiz.position;
                Vector3 hacia = new Vector3(meta.x - pos.x, 0f, meta.z - pos.z);
                if (Plano(pos, meta) < 1.2f && Mathf.Abs(pos.y - meta.y) < 2.5f)
                {
                    llego = true;
                    break;
                }
                if (Time.time > limite)
                {
                    Anotar("sin-salida", 3, pos, hacia, nombre + ": en " + (Time.time - inicio).ToString("F0") + " s no alcanzó " + meta.ToString("F1") + " (a " + Plano(pos, meta).ToString("F1") + " m) y no hay camino por donde seguir");
                    break;
                }
                Paso(ctrl, pos, meta, correr);
                yield return null;
                pos = jugador.Raiz.position;
                metros += Plano(pos, anterior);
                anterior = pos;
                if (Time.time >= proximaMuestra)
                {
                    proximaMuestra = Time.time + PasoMuestra;
                    traza.Add(Time.time.ToString("F2") + "," + pos.x.ToString("F1") + "," + pos.y.ToString("F2") + "," + pos.z.ToString("F1") + "," + (Plano(pos, referencia) / Mathf.Max(0.01f, Time.time - (proximaReferencia - SegundosAtascoPie))).ToString("F1") + "," + nombre);
                    DrenarLogs(pos, hacia);
                }
                historial.Add(new Muestra { t = Time.time, p = pos });
                while (historial.Count > 0 && Time.time - historial[0].t > 1f)
                {
                    historial.RemoveAt(0);
                }
                if (historial.Count > 0 && historial[0].p.y - pos.y > CaidaInesperadaPie)
                {
                    Anotar("caida", 3, pos, hacia, nombre + ": bajó " + (historial[0].p.y - pos.y).ToString("F1") + " m en 1 s (de y=" + historial[0].p.y.ToString("F1") + " a " + pos.y.ToString("F1") + "); se devuelve al último punto");
                    Teletransportar(jugador, historial[0].p);
                    historial.Clear();
                    referencia = historial.Count > 0 ? historial[0].p : jugador.Raiz.position;
                    proximaReferencia = Time.time + SegundosAtascoPie;
                    continue;
                }
                if (pos.y < meta.y - CaidaAlVacioPie)
                {
                    Anotar("caida-al-vacio", 4, pos, hacia, nombre + ": quedó " + (meta.y - pos.y).ToString("F1") + " m bajo el camino (y=" + pos.y.ToString("F1") + " contra " + meta.y.ToString("F1") + ")");
                    break;
                }
                if (Time.time >= proximaReferencia)
                {
                    if (Plano(pos, referencia) < MetrosAtascoPie && Plano(pos, meta) > 1.5f)
                    {
                        atascos++;
                        Anotar("atorado", 2, pos, hacia, nombre + ": avanzó menos de " + MetrosAtascoPie.ToString("F1") + " m en " + SegundosAtascoPie.ToString("F0") + " s hacia " + meta.ToString("F1") + " (intento " + atascos + ")");
                        if (atascos >= 2)
                        {
                            break;
                        }
                        Poner(teclado.spaceKey, true);
                        yield return Esperar(0.2f);
                        Poner(teclado.spaceKey, false);
                    }
                    referencia = jugador.Raiz.position;
                    proximaReferencia = Time.time + SegundosAtascoPie;
                }
            }
            SoltarTeclas();
        }

        // Camina por el NavMesh de donde está hasta <paramref name="destino"/>; si no hay camino lo anota y teletransporta para seguir.
        private IEnumerator CaminarA(ControladorAPie ctrl, IJugador jugador, Vector3 destino, bool correr, string nombre)
        {
            Vector3 pos = jugador.Raiz.position;
            NavMeshHit desde;
            NavMeshHit hasta;
            if (!NavMesh.SamplePosition(pos, out desde, 6f, NavMesh.AllAreas) || !NavMesh.SamplePosition(destino, out hasta, 8f, NavMesh.AllAreas))
            {
                Anotar("sin-camino", 3, destino, destino - pos, nombre + ": no hay NavMesh a menos de 8 m de " + destino.ToString("F1") + " (o del jugador en " + pos.ToString("F1") + "): zona sin suelo caminable");
                Teletransportar(jugador, destino);
                yield return Esperar(0.4f);
                yield break;
            }
            NavMeshPath camino = new NavMeshPath();
            if (!NavMesh.CalculatePath(desde.position, hasta.position, NavMesh.AllAreas, camino) || camino.status != NavMeshPathStatus.PathComplete)
            {
                Anotar("sin-camino", 3, hasta.position, hasta.position - pos, nombre + ": el NavMesh no une " + desde.position.ToString("F1") + " con " + hasta.position.ToString("F1") + " (" + camino.status + ")");
                Teletransportar(jugador, hasta.position);
                yield return Esperar(0.4f);
                yield break;
            }
            Vector3[] esquinas = camino.corners;
            for (int i = 1; i < esquinas.Length && HayPresupuesto; i++)
            {
                yield return Ir(ctrl, jugador, esquinas[i], correr, nombre);
                if (!llego)
                {
                    Teletransportar(jugador, esquinas[i]);
                    yield return Esperar(0.3f);
                }
            }
        }

        private IEnumerator PrepararPie(Vector3 inicio, Vector3 haciaDonde)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "hay jugador");
            Vector3 d = haciaDonde - inicio;
            d.y = 0f;
            jugador.Colocar(new Pose(inicio + Vector3.up * 0.3f, Quaternion.LookRotation(d.sqrMagnitude < 0.01f ? Vector3.forward : d.normalized, Vector3.up)));
            yield return Esperar(1f);
            ControladorAPie ctrl = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            Assert.IsNotNull(ctrl, "el jugador trae ControladorAPie");
            float limite = Time.realtimeSinceStartup + 10f;
            while (!ctrl.ControlActivo && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsTrue(ctrl.ControlActivo, "el jugador a pie tiene el control");
        }

        private static ControladorAPie Controlador => Registro<IJugador>.Actual?.Raiz.GetComponentInChildren<ControladorAPie>();

        [UnityTest, Timeout(300000)]
        public IEnumerator Malecon_APie()
        {
            yield return CargarCiudad();
            List<Cadena> cadenas = CadenasDeZona("Malecon");
            Assert.IsNotEmpty(cadenas, "hay CalleZona del Malecon");
            IniciarParte("malecon-apie", "a pie");
            List<Vector3> todos = cadenas.SelectMany(c => c.puntos).ToList();
            HornearNavMesh(LimitesDe(todos, 60f));
            List<Vector3> paseo = new List<Vector3>();
            foreach (Cadena c in cadenas)
            {
                paseo.AddRange(ConDesplazamiento(Densificar(c.puntos, 20f), OffsetBanqueta(c.ancho)));
            }
            yield return PrepararPie(paseo[0], paseo[Mathf.Min(1, paseo.Count - 1)]);
            IJugador jugador = Registro<IJugador>.Actual;
            ControladorAPie ctrl = Controlador;
            try
            {
                // Paseo: por la banqueta a lo largo de la avenida (primero camina, luego corre).
                for (int i = 1; i < paseo.Count && Time.realtimeSinceStartup < inicioParte + PresupuestoPaseoSegundos; i++)
                {
                    yield return CaminarA(ctrl, jugador, paseo[i], i % 3 == 0, "paseo" + i);
                }
                // Escaleras, playa, muelle y faro: se buscan por nombre en la escena.
                Transform[] todosLosObjetos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                Transform[] escaleras = todosLosObjetos.Where(t => t.name.Contains("EscaleraPlaya") && t.position.z < -190f).OrderBy(t => t.position.x).ToArray();
                if (escaleras.Length == 0)
                {
                    Anotar("no-encontrado", 2, paseo[0], Vector3.forward, "no hay ningún objeto ML02_EscaleraPlaya en la escena", false);
                }
                foreach (Transform e in escaleras.Where((t, i) => i == 0 || i == escaleras.Length - 1).Distinct())
                {
                    if (!HayPresupuesto)
                    {
                        break;
                    }
                    Vector3 mar = new Vector3(e.forward.x, 0f, e.forward.z).normalized;
                    mar = mar.z > 0f ? -mar : mar;
                    Vector3 cima = V(e.position.x, 0.2f, e.position.z);
                    Vector3 playa = V(e.position.x + mar.x * 10f, -2.2f, e.position.z + mar.z * 10f);
                    yield return CaminarA(ctrl, jugador, cima, false, "escalera-cima " + e.name);
                    yield return CaminarA(ctrl, jugador, playa, true, "escalera-playa " + e.name);
                    yield return CaminarA(ctrl, jugador, playa + Vector3.right * 15f, true, "playa-este");
                    yield return CaminarA(ctrl, jugador, playa + Vector3.left * 15f, true, "playa-oeste");
                }
                Transform muelle = todosLosObjetos.FirstOrDefault(t => t.name == "Muelle");
                if (muelle == null)
                {
                    Anotar("no-encontrado", 2, paseo[0], Vector3.forward, "no hay ningún objeto 'Muelle' en la escena", false);
                }
                else if (HayPresupuesto)
                {
                    Bounds b = Medida(muelle);
                    yield return CaminarA(ctrl, jugador, V(b.center.x, b.max.y, b.max.z - 1f), false, "muelle-entrada");
                    yield return CaminarA(ctrl, jugador, V(b.center.x, b.max.y, b.min.z + 2f), true, "muelle-punta");
                }
                Transform faro = todosLosObjetos.FirstOrDefault(t => t.name.Contains("ML04_Faro"));
                if (faro == null)
                {
                    Anotar("no-encontrado", 2, paseo[0], Vector3.forward, "no hay ningún objeto ML04_Faro en la escena", false);
                }
                else if (HayPresupuesto)
                {
                    Bounds b = Medida(faro);
                    yield return CaminarA(ctrl, jugador, V(b.center.x, b.min.y, b.center.z), false, "faro");
                }
            }
            finally
            {
                GuardarInforme();
            }
        }

        private static Bounds Medida(Transform raiz)
        {
            Renderer[] r = raiz.GetComponentsInChildren<Renderer>();
            if (r.Length == 0)
            {
                return new Bounds(raiz.position, Vector3.one);
            }
            Bounds b = r[0].bounds;
            foreach (Renderer x in r)
            {
                b.Encapsulate(x.bounds);
            }
            return b;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Autopista_APie()
        {
            yield return CargarCiudad();
            Assert.IsNotEmpty(CadenasDeZona("Autopista"), "hay CalleZona de la Autopista");
            IniciarParte("autopista-apie", "a pie");
            HornearNavMesh(new Bounds(V(-30f, 5f, 235f), V(740f, 40f, 140f)));
            // Rampa norte desde la calle cruzada, tablero hasta la curva este y túnel al cerro (U6, U7 y U8 de plan-zonas-18nov.md).
            Vector3[] ruta =
            {
                V(-107f, 0f, 201f), V(-120f, 0f, 215f), V(-60f, 5f, 232f), V(30f, 10f, 250f), V(100f, 10f, 250f), V(170f, 10f, 250f), V(240f, 10f, 250f),
                V(300f, 10f, 190f), V(100f, 10f, 250f), V(-60f, 10f, 250f), V(-200f, 10f, 250f), V(-330f, 10f, 250f), V(-360f, 10f, 185f)
            };
            yield return PrepararPie(ruta[0], ruta[1]);
            IJugador jugador = Registro<IJugador>.Actual;
            ControladorAPie ctrl = Controlador;
            try
            {
                for (int i = 1; i < ruta.Length && HayPresupuesto; i++)
                {
                    yield return CaminarA(ctrl, jugador, ruta[i], true, "autopista" + i);
                }
            }
            finally
            {
                GuardarInforme();
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Uniones_APie()
        {
            yield return CargarCiudad();
            List<Union> uniones = Uniones();
            Assert.IsNotEmpty(uniones.Where(u => u.puntos.Count >= 3), "hay uniones armadas");
            IniciarParte("uniones-apie", "a pie");
            try
            {
                foreach (Union u in uniones)
                {
                    if (!HayPresupuesto)
                    {
                        Anotar("sin-presupuesto", 1, u.puntos[0], Vector3.forward, "no alcanzó el tiempo para " + u.nombre + " en adelante", false);
                        break;
                    }
                    List<Vector3> banqueta = ConDesplazamiento(Densificar(u.puntos, 15f), OffsetBanquetaMinimo + 1f);
                    HornearNavMesh(LimitesDe(banqueta, 50f));
                    yield return PrepararPie(banqueta[0], banqueta[Mathf.Min(1, banqueta.Count - 1)]);
                    IJugador jugador = Registro<IJugador>.Actual;
                    ControladorAPie ctrl = Controlador;
                    for (int i = 1; i < banqueta.Count && HayPresupuesto; i++)
                    {
                        yield return CaminarA(ctrl, jugador, banqueta[i], true, u.nombre + "_" + i);
                    }
                }
            }
            finally
            {
                GuardarInforme();
            }
        }
    }
}
