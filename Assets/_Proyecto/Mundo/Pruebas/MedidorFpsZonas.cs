using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// T-1366: mide los FPS por zona en el build. Solo se activa con el argumento <c>-medirFpsZonas</c> (o con <see cref="IniciarEnEditor"/>).
    /// Carga Ciudad_Prologo con partida nueva, pone la hora de noche y lleva una cámara por <see cref="VistasFpsT1366.Todas"/>:
    /// espera <see cref="SegundosEspera"/> s y mide <see cref="SegundosMedicion"/> s en cada vista. Escribe fps-zonas.csv y fps-zonas.txt
    /// junto al exe (o en persistentDataPath) y sale. Sin el argumento no hace nada.
    /// </summary>
    public class MedidorFpsZonas : MonoBehaviour
    {
        public const string Argumento = "-medirFpsZonas";
        public const string EscenaJuego = "Ciudad_Prologo";
        public const float HoraMedicion = 23f;
        public const float SegundosEspera = 3f;
        public const float SegundosMedicion = 5f;
        private const float SegundosEscena = 90f;
        private const float SegundosAsentar = 6f;
        private const float RadioLucesMetros = 60f;
        private const string TipoFlujo = "MechaCyberpunk.Progresion.Partida.FlujoPartida";
        private const string MetodoNuevaPartida = "IrANuevaPartida";

        private static bool activo;
        private readonly Dictionary<Mesh, int> trianguloPorMalla = new Dictionary<Mesh, int>();
        private readonly List<CalculoFpsT1366.Resultado> resultados = new List<CalculoFpsT1366.Resultado>();
        private bool escenaCargada;
        private Camera camara;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => activo = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ArrancarPorArgumento()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, Argumento, StringComparison.OrdinalIgnoreCase))
                {
                    Crear();
                    return;
                }
            }
        }

        /// <summary>En el editor, en Play (desde cualquier escena): arranca la medición como en el build.</summary>
        public static void IniciarEnEditor()
        {
            if (Application.isPlaying) Crear();
        }

        private static void Crear()
        {
            if (activo) return;
            activo = true;
            GameObject go = new GameObject("MedidorFpsZonas");
            DontDestroyOnLoad(go);
            go.AddComponent<MedidorFpsZonas>();
        }

        private void OnEnable() => SceneManager.sceneLoaded += AlCargarEscena;

        private void OnDisable() => SceneManager.sceneLoaded -= AlCargarEscena;

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (escena.name == EscenaJuego) escenaCargada = true;
        }

        private IEnumerator Start()
        {
            int vsync = QualitySettings.vSyncCount;
            int objetivo = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            string error = null;
            try
            {
                yield return CargarEscena();
                if (!escenaCargada)
                {
                    error = "No cargo " + EscenaJuego + " en " + SegundosEscena + " s.";
                }
                else
                {
                    yield return Preparar();
                    IReadOnlyList<VistaFpsT1366> vistas = VistasFpsT1366.Todas;
                    for (int i = 0; i < vistas.Count; i++)
                    {
                        yield return MedirVista(vistas[i]);
                    }
                }
            }
            finally
            {
                QualitySettings.vSyncCount = vsync;
                Application.targetFrameRate = objetivo;
            }
            Terminar(error);
        }

        private IEnumerator CargarEscena()
        {
            escenaCargada = false;
            Type flujo = BuscarTipo(TipoFlujo);
            MethodInfo metodo = flujo?.GetMethod(MetodoNuevaPartida, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (metodo != null)
            {
                metodo.Invoke(null, null);   // partida nueva: salta el título
            }
            else
            {
                Debug.LogWarning("[Mundo] MedidorFpsZonas: no encontre FlujoPartida; cargo la escena directo (puede abrir en el titulo).");
                SceneManager.LoadScene(EscenaJuego);
            }
            float limite = Time.realtimeSinceStartup + SegundosEscena;
            while (!escenaCargada && Time.realtimeSinceStartup < limite) yield return null;
        }

        private static Type BuscarTipo(string nombre)
        {
            foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type tipo = ensamblado.GetType(nombre, false);
                if (tipo != null) return tipo;
            }
            return null;
        }

        private IEnumerator Preparar()
        {
            float limite = Time.realtimeSinceStartup + SegundosAsentar;
            while (Time.realtimeSinceStartup < limite) yield return null;
            // Sin entrada del jugador: todas las acciones prendidas se apagan.
            foreach (InputAction accion in new List<InputAction>(InputSystem.ListEnabledActions())) accion.Disable();
            PonerNoche();
            camara = CrearCamara();
            yield return null;
        }

        private static void PonerNoche()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.Pausar(true);
                reloj.AvanzarHasta(HoraMedicion);
            }
            Reloj.CicloDiaNoche ciclo = FindFirstObjectByType<Reloj.CicloDiaNoche>();
            if (ciclo != null) ciclo.Aplicar(HoraMedicion);
        }

        /// <summary>Cámara nueva con los ajustes de la principal (campo de visión, planos, URP); las demás cámaras de pantalla se apagan.</summary>
        private static Camera CrearCamara()
        {
            Camera principal = Camera.main;
            GameObject go = new GameObject("CamaraMedidorFps");
            Camera nueva = go.AddComponent<Camera>();
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            if (principal != null)
            {
                nueva.CopyFrom(principal);
                nueva.targetTexture = null;
                UniversalAdditionalCameraData origen = principal.GetUniversalAdditionalCameraData();
                datos.renderPostProcessing = origen.renderPostProcessing;
                datos.antialiasing = origen.antialiasing;
                datos.antialiasingQuality = origen.antialiasingQuality;
                datos.renderShadows = origen.renderShadows;
                datos.volumeLayerMask = origen.volumeLayerMask;
                datos.SetRenderer(0);
                datos.requiresDepthOption = origen.requiresDepthOption;
                datos.requiresColorOption = origen.requiresColorOption;
            }
            else
            {
                datos.renderPostProcessing = true;
                nueva.nearClipPlane = 0.05f;
                nueva.farClipPlane = 600f;
                nueva.fieldOfView = 70f;
            }
            nueva.depth = 100f;
            foreach (Camera otra in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (otra != nueva && otra.targetTexture == null) otra.enabled = false;
            }
            return nueva;
        }

        private IEnumerator MedirVista(VistaFpsT1366 vista)
        {
            VistasFpsT1366.Resolver(vista, VistasFpsT1366.AlturaConFisica, out Vector3 posicion, out Vector3 mirar);
            camara.transform.position = posicion;
            camara.transform.LookAt(mirar);

            float fin = Time.realtimeSinceStartup + SegundosEspera;
            while (Time.realtimeSinceStartup < fin) yield return null;

            List<float> tiempos = new List<float>(2048);
            double cpuSuma = 0.0;
            double gpuSuma = 0.0;
            int cpuN = 0;
            int gpuN = 0;
            FrameTiming[] tomas = new FrameTiming[1];
            using (ProfilerRecorder dibujos = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            using (ProfilerRecorder pases = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
            {
                long dibujosMax = -1;
                long pasesMax = -1;
                fin = Time.realtimeSinceStartup + SegundosMedicion;
                while (Time.realtimeSinceStartup < fin)
                {
                    yield return null;
                    tiempos.Add(Time.unscaledDeltaTime * 1000f);
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, tomas) > 0)
                    {
                        if (tomas[0].cpuFrameTime > 0.0) { cpuSuma += tomas[0].cpuFrameTime; cpuN++; }
                        if (tomas[0].gpuFrameTime > 0.0) { gpuSuma += tomas[0].gpuFrameTime; gpuN++; }
                    }
                    if (dibujos.Valid) dibujosMax = Math.Max(dibujosMax, dibujos.LastValue);
                    if (pases.Valid) pasesMax = Math.Max(pasesMax, pases.LastValue);
                }

                CalculoFpsT1366.Resultado r = new CalculoFpsT1366.Resultado
                {
                    Zona = vista.Zona,
                    Vista = vista.Nombre,
                    Frames = tiempos.Count,
                    MsPromedio = CalculoFpsT1366.Promedio(tiempos),
                    MsPeor1Pct = CalculoFpsT1366.PeorPorcentaje(tiempos),
                    CpuMs = cpuN > 0 ? cpuSuma / cpuN : -1.0,
                    GpuMs = gpuN > 0 ? gpuSuma / gpuN : -1.0,
                    DrawCalls = dibujosMax,
                    SetPassCalls = pasesMax,
                };
                ContarEscena(ref r);
                resultados.Add(r);
            }
        }

        private void ContarEscena(ref CalculoFpsT1366.Resultado r)
        {
            int visibles = 0;
            long triangulos = 0;
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.isVisible) continue;
                visibles++;
                triangulos += Triangulos(renderer);
            }
            Plane[] planos = GeometryUtility.CalculateFrustumPlanes(camara);
            Vector3 centro = camara.transform.position;
            int cerca = 0;
            int enVista = 0;
            foreach (Light luz in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!luz.enabled || luz.type == LightType.Directional) continue;
                float distancia = Vector3.Distance(centro, luz.transform.position);
                if (distancia < RadioLucesMetros) cerca++;
                Bounds caja = new Bounds(luz.transform.position, Vector3.one * (luz.range * 2f));
                if (GeometryUtility.TestPlanesAABB(planos, caja)) enVista++;
            }
            r.RenderersVisibles = visibles;
            r.Triangulos = triangulos;
            r.Luces60m = cerca;
            r.LucesEnVista = enVista;
        }

        private long Triangulos(Renderer renderer)
        {
            Mesh malla = null;
            if (renderer is SkinnedMeshRenderer piel)
            {
                malla = piel.sharedMesh;
            }
            else if (renderer.TryGetComponent(out MeshFilter filtro))
            {
                malla = filtro.sharedMesh;
            }
            if (malla == null) return 0;
            if (!trianguloPorMalla.TryGetValue(malla, out int cuenta))
            {
                for (int i = 0; i < malla.subMeshCount; i++) cuenta += (int)(malla.GetIndexCount(i) / 3);
                trianguloPorMalla[malla] = cuenta;
            }
            return cuenta;
        }

        private void Terminar(string error)
        {
            string csv = CalculoFpsT1366.DocumentoCsv(resultados);
            string texto = (error != null ? "ERROR: " + error + "\n" : "") + CalculoFpsT1366.Resumen(resultados);
            string carpeta = CarpetaSalida();
            try
            {
                File.WriteAllText(Path.Combine(carpeta, "fps-zonas.csv"), csv);
                File.WriteAllText(Path.Combine(carpeta, "fps-zonas.txt"), texto);
                Debug.Log("[Mundo] MedidorFpsZonas: resultados en " + carpeta);
            }
            catch (Exception e)
            {
                Debug.LogError("[Mundo] MedidorFpsZonas no pudo escribir en " + carpeta + ": " + e.Message);
            }
            activo = false;
            Application.Quit(error == null ? 0 : 1);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private static string CarpetaSalida()
        {
            try
            {
                string junto = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrEmpty(junto))
                {
                    string prueba = Path.Combine(junto, ".escritura-fps");
                    File.WriteAllText(prueba, "x");
                    File.Delete(prueba);
                    return junto;
                }
            }
            catch (Exception)
            {
                // sin permiso junto al exe: se usa persistentDataPath
            }
            return Application.persistentDataPath;
        }
    }
}
