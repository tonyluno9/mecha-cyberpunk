using System.Collections;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;

namespace MechaCyberpunk.UI.Pruebas
{
    /// <summary>
    /// Prueba del inicio de partida en el build: solo actúa con <c>-pruebaInicio &lt;carpeta&gt; [-partidaVieja &lt;archivo&gt;]</c>.
    /// Las partidas van a la carpeta (RutaForzada), nunca a las del jugador. Con los botones reales del menú:
    /// A) con una partida vieja copiada, CONTINUAR (no debe caer ni quedar fuera del muro); B) NUEVA PARTIDA (directo a la carga, sin
    /// creación; se levanta en la calle) y guarda en la ranura 1; C) sale al menú y CONTINUAR (aparece donde guardó).
    /// Deja capturas y resultados.txt y cierra el juego.
    /// </summary>
    public class PruebaInicioBuild : MonoBehaviour
    {
        public const string Bandera = "-pruebaInicio";
        public const string BanderaVieja = "-partidaVieja";
        private const float MaximoEspera = 120f;
        private const float SegundosCaida = 6f;
        private const float AlturaMinimaBien = -1f;
        private const float LimiteMuroX = 135f;
        private const float LimiteMuroZ = 88f;

        private string carpeta;
        private string vieja;
        private readonly StringBuilder log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Revisar()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            string carpeta = null, vieja = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == Bandera) carpeta = args[i + 1];
                if (args[i] == BanderaVieja) vieja = args[i + 1];
            }
            if (carpeta == null)
            {
                return;
            }
            Directory.CreateDirectory(carpeta);
            foreach (string f in Directory.GetFiles(carpeta, "partida*.json")) File.Delete(f);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            if (vieja != null && File.Exists(vieja))
            {
                File.Copy(vieja, ServicioGuardado.RutaForzada, true);
            }
            // La prueba sigue aunque la ventana pierda el foco (si no, el build se pausa).
            Application.runInBackground = true;
            GameObject go = new GameObject("PruebaInicioBuild");
            DontDestroyOnLoad(go);
            PruebaInicioBuild p = go.AddComponent<PruebaInicioBuild>();
            p.carpeta = carpeta;
            p.vieja = vieja;
        }

        private IEnumerator Start()
        {
            Linea("Resolución " + Screen.width + "x" + Screen.height + "; partidas en " + ServicioGuardado.RutaForzada);
            MenuInicio menu = null;
            yield return EsperarMenu(m => menu = m);

            if (vieja != null)
            {
                Linea("A) Partida vieja: " + vieja + " (HayPartida " + menu.HayPartida + ")");
                yield return Foto("A0_menu_con_partida_vieja");
                menu.Continuar.Pulsar();
                yield return EsperarJugador("A");
                yield return Foto("A1_continuar_vieja");
                yield return new WaitForSecondsRealtime(SegundosCaida);
                Comprobar("A", Jugador());
                yield return Foto("A2_continuar_vieja_6s");
                FlujoPartida.IrAlTitulo();
                yield return EsperarMenu(m => menu = m);
            }

            Linea("B) Nueva partida");
            menu.Nueva.Pulsar();
            yield return null;
            Linea("  estado tras pulsar: " + menu.Estado + " (debe ser Cargando, sin creación); carga visible " + PantallaCargando.Visible);
            yield return EsperarJugador("B");
            yield return new WaitForSecondsRealtime(1f);
            yield return Foto("B1_nueva_levantandose");
            yield return new WaitForSecondsRealtime(5f);
            yield return Foto("B2_nueva_en_la_calle");
            Vector3 guardada = Jugador();
            Comprobar("B", guardada);
            bool ok = ServicioGuardado.Actual != null && ServicioGuardado.Actual.Guardar(ServicioGuardado.PrimeraRanura);
            Linea("  guardó en la ranura 1: " + ok + " en " + guardada.ToString("F2"));

            Linea("C) Salir al menú y CONTINUAR");
            FlujoPartida.IrAlTitulo();
            yield return EsperarMenu(m => menu = m);
            yield return Foto("C0_menu");
            Linea("  CONTINUAR ofrece la ranura " + menu.RanuraContinuar);
            menu.Continuar.Pulsar();
            yield return EsperarJugador("C");
            yield return new WaitForSecondsRealtime(SegundosCaida);
            Vector3 p = Jugador();
            Comprobar("C", p);
            Linea("  distancia a donde guardó: " + Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(guardada.x, 0f, guardada.z)).ToString("F2") + " m");
            yield return Foto("C1_continuar");

            File.WriteAllText(Path.Combine(carpeta, "resultados.txt"), log.ToString());
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        private static Vector3 Jugador() => Registro<IJugador>.Actual != null ? Registro<IJugador>.Actual.Raiz.position : new Vector3(float.NaN, float.NaN, float.NaN);

        private void Comprobar(string paso, Vector3 p)
        {
            bool cae = p.y < AlturaMinimaBien;
            bool fuera = Mathf.Abs(p.x) > LimiteMuroX || Mathf.Abs(p.z) > LimiteMuroZ;
            Linea("  " + paso + ": jugador en " + p.ToString("F2") + (cae ? " CAYÓ" : " sobre el piso") + (fuera ? " FUERA DEL MURO" : " dentro del muro"));
        }

        private IEnumerator EsperarMenu(System.Action<MenuInicio> listo)
        {
            float fin = Time.realtimeSinceStartup + MaximoEspera;
            MenuInicio m = null;
            while (Time.realtimeSinceStartup < fin)
            {
                m = FindFirstObjectByType<MenuInicio>();
                if (m != null && m.Estado == EstadoInicio.Principal && !PantallaCargando.Visible)
                {
                    break;
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.5f);
            listo(m);
        }

        private IEnumerator EsperarJugador(string paso)
        {
            float inicio = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - inicio < MaximoEspera && (Registro<IJugador>.Actual == null || PantallaCargando.Visible))
            {
                yield return null;
            }
            Linea("  " + paso + ": la ciudad cargó en " + (Time.realtimeSinceStartup - inicio).ToString("F1") + " s; jugador en " + Jugador().ToString("F2"));
        }

        private IEnumerator Foto(string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, nombre + ".png"));
            yield return null;
        }

        private void Linea(string texto)
        {
            log.AppendLine(texto);
            Debug.Log("[UI] PruebaInicio: " + texto);
        }
    }
}
