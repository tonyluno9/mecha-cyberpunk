using System.Collections;
using System.IO;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;

namespace MechaCyberpunk.UI.Pruebas
{
    /// <summary>
    /// Herramienta de prueba de la pantalla de carga en el build: solo actúa si el juego se abre con <c>-capturaCarga &lt;carpeta&gt;</c>.
    /// Muestra la pantalla de carga real cargando Ciudad_Prologo, guarda fotos al empezar, a mitad, en el fundido entre fotos y al
    /// terminar (para ver que no queda en negro), escribe cuánto tardó y cierra el juego. Sin la bandera no hace nada.
    /// </summary>
    public class CapturaCargaBuild : MonoBehaviour
    {
        public const string Bandera = "-capturaCarga";
        private const string EscenaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float SegundosAntesDeEmpezar = 1f;
        private const float SegundosTrasTerminar = 1.5f;
        private const float SegundosMaximos = 120f;
        private static readonly float[] MomentosFoto = { 0.05f, 2f, 7.2f };

        private string carpeta;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Revisar()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == Bandera)
                {
                    GameObject go = new GameObject("CapturaCargaBuild");
                    DontDestroyOnLoad(go);
                    go.AddComponent<CapturaCargaBuild>().carpeta = args[i + 1];
                    return;
                }
            }
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(carpeta);
            string prefijo = Screen.width + "x" + Screen.height + "_";
            yield return new WaitForSecondsRealtime(SegundosAntesDeEmpezar);
            float inicio = Time.realtimeSinceStartup;
            PantallaCargando.Mostrar().Asignar(EscenaInicio.CargarAsincrono(EscenaCiudad));
            int siguiente = 0;
            while (PantallaCargando.Visible && Time.realtimeSinceStartup - inicio < SegundosMaximos)
            {
                float t = Time.realtimeSinceStartup - inicio;
                if (siguiente < MomentosFoto.Length && t >= MomentosFoto[siguiente])
                {
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, prefijo + "carga_" + siguiente + "_" + MomentosFoto[siguiente].ToString("0.0") + "s.png"));
                    siguiente++;
                }
                yield return null;
            }
            float segundos = Time.realtimeSinceStartup - inicio;
            // Durante el desvanecido final y después: la escena debe verse, no negro.
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, prefijo + "fin_desvanece.png"));
            yield return new WaitForSecondsRealtime(SegundosTrasTerminar);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, prefijo + "fin_escena.png"));
            File.AppendAllText(Path.Combine(carpeta, "tiempos.txt"), prefijo + "carga " + segundos.ToString("0.00") + " s\n");
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }
    }
}
