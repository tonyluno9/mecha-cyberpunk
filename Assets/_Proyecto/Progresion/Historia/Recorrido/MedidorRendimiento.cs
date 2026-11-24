using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-rendimiento-tope-24nov: medición automática de fps en el build. Solo existe si el juego se lanza con <c>-medirRendimiento</c>
    /// (<see cref="ReglaMedicionRendimiento.Argumento"/>); sin él no hace NADA. Arranca una partida nueva (en un guardado aparte, sin tocar las
    /// partidas reales), salta la historia al juego libre, empieza el modo libre con el jugador invulnerable y quieto, y recorre las fases 1 a 6 y
    /// luego topes de enemigos crecientes en la fase 6 (sin editar assets: cambia los números en memoria). Por paso: calentamiento y medición;
    /// escribe una línea en persistentDataPath/rendimiento.csv y un <c>[Rendimiento] ...</c> en el log. Al final <c>[Rendimiento] FIN</c> y sale.
    /// </summary>
    public class MedidorRendimiento : MonoBehaviour
    {
        public const string Prefijo = "[Rendimiento] ";
        public const string NombreCsv = "rendimiento.csv";
        private const string CarpetaGuardado = "medicion_rendimiento";
        private const float SegundosCalentamiento = 8f;
        private const float SegundosMedicion = 17f;
        private const float SegundosEsperaEscena = 120f;
        private const float SegundosEsperaHistoria = 60f;
        private const float SegundosLevantarse = 6f;
        private const float SegundosEntreMuestras = 1f;
        private const float SegundosInvulnerable = 100000f;
        private const float GradosPorSegundoCamara = 120f;
        private const float MetrosMaximosApuntar = 90f;
        private const float SegundosReposicionMedicion = 2f;
        private const float SegundosEntreGeneradosMedicion = 1.5f;
        private const int CuadrosReservados = 8192;

        public static bool Activo { get; private set; }

        private readonly List<float> cuadros = new List<float>(CuadrosReservados);
        private string rutaCsv;
        private VidaAPie vida;
        private ControladorAPie control;
        private float proximaMuestra;
        private float proximoBlindaje;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Activo = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Arrancar()
        {
            if (Activo || !ReglaMedicionRendimiento.TieneArgumento(Environment.GetCommandLineArgs()))
            {
                return;
            }
            Activo = true;
            // La partida nueva se guarda aparte: las partidas reales (AUTO y ranuras) no se tocan.
            string carpeta = Path.Combine(Application.persistentDataPath, CarpetaGuardado);
            try
            {
                if (Directory.Exists(carpeta))
                {
                    Directory.Delete(carpeta, true);
                }
                Directory.CreateDirectory(carpeta);
            }
            catch (Exception e)
            {
                Debug.LogWarning(Prefijo + "no se pudo limpiar " + carpeta + ": " + e.Message);
            }
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida_auto.json");
            GameObject go = new GameObject("MedidorRendimiento");
            DontDestroyOnLoad(go);
            go.AddComponent<MedidorRendimiento>();
        }

        private void Start()
        {
            StartCoroutine(Medir());
        }

        private IEnumerator Medir()
        {
            Application.runInBackground = true;
            rutaCsv = Path.Combine(Application.persistentDataPath, NombreCsv);
            Debug.Log(Prefijo + "INICIO gpu=\"" + SystemInfo.graphicsDeviceName + "\" cpu=\"" + SystemInfo.processorType + "\" resolucion=" + Screen.width + "x"
                + Screen.height + " calidad=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + " csv=" + rutaCsv);

            // 1. Partida nueva hasta la ciudad jugable.
            yield return new WaitForSecondsRealtime(2f);
            if (!EnCiudadJugable())
            {
                Debug.Log(Prefijo + "partida nueva");
                FlujoPartida.IrANuevaPartida();
            }
            float limite = Time.realtimeSinceStartup + SegundosEsperaEscena;
            while (!EnCiudadJugable() && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            if (!EnCiudadJugable())
            {
                Terminar("ERROR no cargó la ciudad jugable en " + SegundosEsperaEscena + " s (escena " + SceneManager.GetActiveScene().path + ")");
                yield break;
            }

            // 2. Sin cinemáticas: salta C01, espera a que se levante y salta la historia al juego libre.
            limite = Time.realtimeSinceStartup + SegundosEsperaHistoria;
            while (Time.realtimeSinceStartup < limite)
            {
                DirectorRecorrido d = DirectorRecorrido.Actual;
                if (CinematicaPendiente.EnCurso)
                {
                    CinematicaPendiente.SaltarActual();
                }
                else if (d == null || d.Paso != PasoRecorrido.Recuerdo)
                {
                    break;
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(SegundosLevantarse);
            DirectorRecorrido.SaltarAPaso((int)PasoRecorrido.Libre);
            yield return new WaitForSecondsRealtime(1f);
            limite = Time.realtimeSinceStartup + SegundosEsperaHistoria;
            while ((CinematicaPendiente.EnCurso || CinematicaEnCurso.Activa) && Time.realtimeSinceStartup < limite)
            {
                CinematicaPendiente.SaltarActual();
                yield return null;
            }
            BuscarJugador();
            control?.ActivarControl(true);
            Blindar();
            DirectorModoLibre.Iniciar(false);
            StartCoroutine(MantenerLibre());
            Debug.Log(Prefijo + "modo libre iniciado; jugador en " + (control != null ? control.transform.position.ToString("F0") : "(sin jugador)"));

            // 3. Sin vsync ni tope de fps: se mide lo que da la máquina.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            ParametrosRespuesta p = DirectorRespuesta.Parametros;
            int topeAsset = p.TopeEnemigosLibre;
            p.SegundosReposicionLibre = Mathf.Min(p.SegundosReposicionLibre, SegundosReposicionMedicion);
            p.SegundosEntreGenerados = Mathf.Min(p.SegundosEntreGenerados, SegundosEntreGeneradosMedicion);
            EscribirCsv(EncabezadoCsv(), false);

            List<PasoMedicionRendimiento> plan = ReglaMedicionRendimiento.Plan(ReglaMedicionRendimiento.FasesLibre, ReglaMedicionRendimiento.TopesPorOmision);
            int faseActual = -1;
            for (int i = 0; i < plan.Count; i++)
            {
                PasoMedicionRendimiento paso = plan[i];
                int tope = paso.TopeEnemigos > 0 ? paso.TopeEnemigos : topeAsset;
                if (paso.TopeEnemigos > 0)
                {
                    p.TopeEnemigosLibre = tope;
                    p.TopePatrullasLibre = ReglaMedicionRendimiento.TopePatrullas(tope);
                    p.TopeDronesLibre = ReglaMedicionRendimiento.TopeDrones(tope);
                    p.TopeCapsulasLibre = ReglaMedicionRendimiento.TopeCapsulas(tope);
                    p.TopeRobotsGrandesLibre = ReglaMedicionRendimiento.TopeRobotsGrandes(tope);
                }
                if (paso.Fase != faseActual)
                {
                    faseActual = paso.Fase;
                    DirectorModoLibre.DepurarSaltarAFase(paso.Fase);
                }
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Debug.Log(Prefijo + "paso " + (i + 1) + "/" + plan.Count + " fase " + paso.Fase + " tope " + tope + ": calentando " + SegundosCalentamiento + " s");

                float fin = Time.realtimeSinceStartup + SegundosCalentamiento;
                while (Time.realtimeSinceStartup < fin)
                {
                    Cuidar();
                    yield return null;
                }

                cuadros.Clear();
                int muestras = 0;
                int sumaEnemigos = 0;
                int maxEnemigos = 0;
                int cuadrosEnPausa = 0;
                proximaMuestra = 0f;
                yield return null; // el primer delta medido ya es de un cuadro completo
                fin = Time.realtimeSinceStartup + SegundosMedicion;
                while (Time.realtimeSinceStartup < fin)
                {
                    cuadros.Add(Time.unscaledDeltaTime);
                    if (Pausa.Activa)
                    {
                        cuadrosEnPausa++;
                    }
                    if (Time.unscaledTime >= proximaMuestra)
                    {
                        proximaMuestra = Time.unscaledTime + SegundosEntreMuestras;
                        int vivos = DirectorRespuesta.VivosLibre;
                        muestras++;
                        sumaEnemigos += vivos;
                        maxEnemigos = Mathf.Max(maxEnemigos, vivos);
                    }
                    Cuidar();
                    yield return null;
                }

                float fps = ReglaMedicionRendimiento.FpsPromedio(cuadros);
                float bajo = ReglaMedicionRendimiento.FpsUnoPorCientoBajo(cuadros);
                Conteo c = Contar(); // después de medir: los Find no entran en los cuadros medidos
                float enemigosProm = muestras > 0 ? (float)sumaEnemigos / muestras : 0f;
                string nombreFase = DirectorRespuesta.ModoLibre != null ? DirectorRespuesta.ModoLibre.Nombre : "-";
                string linea = Prefijo + "paso=" + (i + 1) + "/" + plan.Count + " fase=" + paso.Fase + " tope=" + tope
                    + " fps=" + F(fps) + " low1=" + F(bajo) + " ms=" + F(fps > 0f ? 1000f / fps : 0f) + " cuadros=" + cuadros.Count
                    + " enemigosProm=" + F(enemigosProm) + " enemigosMax=" + maxEnemigos + " vivosFin=" + DirectorRespuesta.VivosLibre
                    + " pandilleros=" + c.Pandilleros + " vehiculos=" + c.VehiculosRespuesta + " trafico=" + c.CarrosTrafico + " patrullasMundo=" + c.PatrullasMundo
                    + " drones=" + c.Drones + " capsulas=" + CapsulaCaida.Activas + " luces=" + c.Luces + " lucesSombra=" + c.LucesSombra
                    + " particulasSistemas=" + c.SistemasParticulas + " particulas=" + c.Particulas + " cuerpos=" + PandilleroAPie.CuerposTirados.Count
                    + " rigidbodies=" + c.RigidbodiesDinamicos + " pausa=" + cuadrosEnPausa + " faseNombre=\"" + nombreFase + "\"";
                Debug.Log(linea);
                EscribirCsv(string.Join(",", new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture), paso.Fase.ToString(CultureInfo.InvariantCulture), "\"" + nombreFase + "\"",
                    tope.ToString(CultureInfo.InvariantCulture), p.TopePatrullasLibre.ToString(CultureInfo.InvariantCulture),
                    p.TopeDronesLibre.ToString(CultureInfo.InvariantCulture), p.TopeCapsulasLibre.ToString(CultureInfo.InvariantCulture),
                    F(fps), F(bajo), F(fps > 0f ? 1000f / fps : 0f), cuadros.Count.ToString(CultureInfo.InvariantCulture),
                    F(enemigosProm), maxEnemigos.ToString(CultureInfo.InvariantCulture), DirectorRespuesta.VivosLibre.ToString(CultureInfo.InvariantCulture),
                    c.Pandilleros.ToString(CultureInfo.InvariantCulture), c.VehiculosRespuesta.ToString(CultureInfo.InvariantCulture),
                    c.CarrosTrafico.ToString(CultureInfo.InvariantCulture), c.PatrullasMundo.ToString(CultureInfo.InvariantCulture),
                    c.Drones.ToString(CultureInfo.InvariantCulture), CapsulaCaida.Activas.ToString(CultureInfo.InvariantCulture),
                    c.Luces.ToString(CultureInfo.InvariantCulture), c.LucesSombra.ToString(CultureInfo.InvariantCulture),
                    c.SistemasParticulas.ToString(CultureInfo.InvariantCulture), c.Particulas.ToString(CultureInfo.InvariantCulture),
                    PandilleroAPie.CuerposTirados.Count.ToString(CultureInfo.InvariantCulture), c.RigidbodiesDinamicos.ToString(CultureInfo.InvariantCulture),
                    cuadrosEnPausa.ToString(CultureInfo.InvariantCulture), Screen.width + "x" + Screen.height,
                }), true);
            }
            Terminar("FIN csv=" + rutaCsv);
        }

        private static string EncabezadoCsv() => "paso,fase,nombreFase,topeEnemigos,topePatrullas,topeDrones,topeCapsulas,fpsPromedio,fps1Bajo,msPromedio,cuadros,"
            + "enemigosPromedio,enemigosMax,vivosLibreFin,pandillerosVivos,vehiculosRespuesta,carrosTrafico,patrullasMundo,drones,capsulas,lucesActivas,"
            + "lucesConSombra,sistemasParticulasVivos,particulasVivas,cuerpos,rigidbodiesDinamicos,cuadrosEnPausa,resolucion";

        private void Terminar(string texto)
        {
            Debug.Log(Prefijo + texto);
            Debug.Log(Prefijo + "FIN");
            Application.Quit();
        }

        private static bool EnCiudadJugable()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return !FlujoPartida.EnTitulo && DirectorRecorrido.Actual != null && jugador != null && jugador.Raiz != null
                && SceneManager.GetActiveScene().path == FlujoPartida.RutaEscena;
        }

        private void BuscarJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raiz = jugador != null ? jugador.Raiz : null;
            vida = raiz != null ? raiz.GetComponentInChildren<VidaAPie>(true) : null;
            control = raiz != null ? raiz.GetComponentInChildren<ControladorAPie>(true) : null;
        }

        // Cada cuadro: la cámara hacia el enemigo vivo más cercano; cada segundo, el jugador sigue sin recibir daño (y se levanta si cayó).
        private void Cuidar()
        {
            if (Time.unscaledTime >= proximoBlindaje)
            {
                proximoBlindaje = Time.unscaledTime + SegundosEntreMuestras;
                if (vida == null || control == null)
                {
                    BuscarJugador();
                }
                Blindar();
            }
            Apuntar();
        }

        private void Blindar()
        {
            if (vida == null)
            {
                return;
            }
            if (vida.EstaKO)
            {
                vida.Restaurar();
                Debug.LogWarning(Prefijo + "el jugador cayó pese a la invulnerabilidad; restaurado");
            }
            vida.DarInvulnerabilidad(SegundosInvulnerable);
        }

        private void Apuntar()
        {
            if (control == null)
            {
                return;
            }
            Vector3 desde = control.transform.position;
            float mejor = MetrosMaximosApuntar * MetrosMaximosApuntar;
            PandilleroAPie objetivo = null;
            foreach (PandilleroAPie e in PandilleroAPie.Activos)
            {
                if (e == null || e.EstaKO)
                {
                    continue;
                }
                float d = (e.transform.position - desde).sqrMagnitude;
                if (d < mejor)
                {
                    mejor = d;
                    objetivo = e;
                }
            }
            if (objetivo == null)
            {
                return;
            }
            Vector3 hacia = objetivo.transform.position - desde;
            if (hacia.x * hacia.x + hacia.z * hacia.z < 0.01f)
            {
                return;
            }
            float giro = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(control.GiroCamara, giro);
            float paso = GradosPorSegundoCamara * Time.unscaledDeltaTime;
            control.Empujar(Mathf.Clamp(delta, -paso, paso), 0f);
        }

        private struct Conteo
        {
            public int Pandilleros, VehiculosRespuesta, CarrosTrafico, PatrullasMundo, Drones, Luces, LucesSombra, SistemasParticulas, Particulas, RigidbodiesDinamicos;
        }

        private static Conteo Contar()
        {
            Conteo c = new Conteo();
            foreach (PandilleroAPie e in PandilleroAPie.Activos)
            {
                if (e != null && !e.EstaKO)
                {
                    c.Pandilleros++;
                }
            }
            ReglaEscaladaRespuesta regla = DirectorRespuesta.Regla;
            c.VehiculosRespuesta = regla.Vivos(TipoRespuesta.Patrulla) + regla.Vivos(TipoRespuesta.Camioneta);
            c.Drones = regla.Vivos(TipoRespuesta.Dron);
            // Tráfico y patrullas son de otra área (Trafico/, de Alan): se cuentan por reflexión para no referenciar su ensamblado.
            c.CarrosTrafico = ContarEstatico("MechaCyberpunk.Trafico.Calle.CarroTrafico", "Activos");
            c.PatrullasMundo = ContarEstatico("MechaCyberpunk.Trafico.Persecucion.PatrullaMundo", "Activas");
            foreach (Light luz in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (luz.enabled && luz.intensity > 0f)
                {
                    c.Luces++;
                    if (luz.shadows != LightShadows.None)
                    {
                        c.LucesSombra++;
                    }
                }
            }
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (ps.particleCount > 0)
                {
                    c.SistemasParticulas++;
                    c.Particulas += ps.particleCount;
                }
            }
            foreach (Rigidbody rb in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!rb.isKinematic && !rb.IsSleeping())
                {
                    c.RigidbodiesDinamicos++;
                }
            }
            return c;
        }

        private static readonly Dictionary<string, MemberInfo> miembros = new Dictionary<string, MemberInfo>();

        private static int ContarEstatico(string tipo, string miembro)
        {
            string clave = tipo + "." + miembro;
            if (!miembros.TryGetValue(clave, out MemberInfo info))
            {
                info = null;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = a.GetType(tipo, false);
                    if (t != null)
                    {
                        const BindingFlags banderas = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                        info = (MemberInfo)t.GetField(miembro, banderas) ?? t.GetProperty(miembro, banderas);
                        break;
                    }
                }
                miembros[clave] = info;
            }
            object valor = info is FieldInfo f ? f.GetValue(null) : info is PropertyInfo p ? p.GetValue(null) : null;
            if (valor is ICollection coleccion)
            {
                return coleccion.Count;
            }
            if (valor is IEnumerable enumerable)
            {
                int n = 0;
                foreach (object _ in enumerable)
                {
                    n++;
                }
                return n;
            }
            return -1;
        }

        private void EscribirCsv(string linea, bool agregar)
        {
            try
            {
                if (agregar)
                {
                    File.AppendAllText(rutaCsv, linea + "\n", Encoding.UTF8);
                }
                else
                {
                    File.WriteAllText(rutaCsv, linea + "\n", Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(Prefijo + "no se pudo escribir " + rutaCsv + ": " + e.Message);
            }
        }

        private static string F(float v) => v.ToString("F1", CultureInfo.InvariantCulture);
    
        // Mientras se mide, la historia no puede seguir: los avisos de cinemáticas en fila (C02 -> C03...) la regresaban a pasos guiados
        // y durante una cinemática la persecución retira a todos. Cada cuadro se salta la cinemática y se vuelve al juego libre.
        private IEnumerator MantenerLibre()
        {
            while (true)
            {
                if (CinematicaPendiente.EnCurso)
                {
                    CinematicaPendiente.SaltarActual();
                }
                DirectorRecorrido d = DirectorRecorrido.Actual;
                if (d != null && d.Paso != PasoRecorrido.Libre && !CinematicaPendiente.EnCurso)
                {
                    Debug.Log(Prefijo + "la historia volvió a " + d.Paso + ": se regresa al juego libre");
                    DirectorRecorrido.SaltarAPaso((int)PasoRecorrido.Libre);
                }
                yield return null;
            }
        }
    }
}
