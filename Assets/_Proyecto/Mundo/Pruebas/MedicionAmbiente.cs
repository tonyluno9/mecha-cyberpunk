using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Reloj;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Herramienta de prueba de la ambientación de noche: solo existe en la escena temporal del build de medición
    /// (<c>ConstruirMedicionAmbiente</c>), nunca en las escenas del juego. Pone el reloj a la hora pedida, lleva al jugador
    /// a cada punto (para que la lluvia y los interiores lo sigan), mira con su propia cámara a la altura de los ojos,
    /// mide los fotogramas, guarda una captura y escribe el informe. Al terminar cierra el build.
    /// </summary>
    public class MedicionAmbiente : MonoBehaviour
    {
        private const string ArgumentoSalida = "-medicionSalida";
        private const string ArgumentoCalidad = "-medicionCalidad";
        private const string ArgumentoHora = "-medicionHora";
        private const int BitsProfundidad = 24;

        [System.Serializable]
        private struct Punto
        {
            public string nombre;
            [Tooltip("Pies del jugador (m).")] public Vector3 pies;
            [Tooltip("Hacia dónde mira la cámara.")] public Vector3 mira;
        }

        [Tooltip("Hora del reloj del mundo durante la medición (h).")]
        [SerializeField, Range(0f, 24f)] private float hora = 23f;
        [Tooltip("Altura de los ojos sobre los pies (m).")]
        [SerializeField, Min(0f)] private float alturaOjos = 1.7f;
        [Tooltip("Segundos de espera antes de empezar (carga, prólogo).")]
        [SerializeField, Min(0f)] private float segundosInicio = 6f;
        [Tooltip("Segundos de calentamiento en cada punto antes de medir.")]
        [SerializeField, Min(0f)] private float segundosCalentar = 4f;
        [Tooltip("Segundos de medición en cada punto.")]
        [SerializeField, Min(1f)] private float segundosMedir = 8f;
        [Tooltip("Ancho de la captura (px).")]
        [SerializeField, Min(16)] private int ancho = 1920;
        [Tooltip("Alto de la captura (px).")]
        [SerializeField, Min(16)] private int alto = 1080;
        [Tooltip("Campo de visión vertical de la cámara de medición (°).")]
        [SerializeField, Range(20f, 110f)] private float campoVision = 70f;
        [SerializeField] private Punto[] puntos;

        private readonly StringBuilder informe = new StringBuilder();

        private IEnumerator Start()
        {
            string salida = Argumento(ArgumentoSalida, Path.Combine(Application.persistentDataPath, "medicion"));
            Directory.CreateDirectory(salida);
            string calidad = Argumento(ArgumentoCalidad, null);
            if (float.TryParse(Argumento(ArgumentoHora, null), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float horaPedida))
            {
                hora = horaPedida;
            }
            if (calidad != null)
            {
                string[] nombres = QualitySettings.names;
                for (int i = 0; i < nombres.Length; i++)
                {
                    if (nombres[i] == calidad)
                    {
                        QualitySettings.SetQualityLevel(i, true);
                    }
                }
            }
            yield return new WaitForSecondsRealtime(segundosInicio);

            Camera camara = CrearCamara();
            informe.AppendLine("gpu=" + SystemInfo.graphicsDeviceName + " api=" + SystemInfo.graphicsDeviceType
                + " pantalla=" + Screen.width + "x" + Screen.height + " calidad=" + QualitySettings.names[QualitySettings.GetQualityLevel()]
                + " vsync=" + QualitySettings.vSyncCount + " hora=" + hora);

            foreach (Punto punto in puntos)
            {
                ApagarOtrasCamaras(camara);
                PonerHora();
                MoverJugador(punto.pies);
                camara.transform.SetPositionAndRotation(punto.pies + Vector3.up * alturaOjos, Quaternion.LookRotation(punto.mira, Vector3.up));
                yield return new WaitForSecondsRealtime(segundosCalentar);
                camara.transform.SetPositionAndRotation(punto.pies + Vector3.up * alturaOjos, Quaternion.LookRotation(punto.mira, Vector3.up));

                List<float> tiempos = new List<float>();
                float inicio = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - inicio < segundosMedir)
                {
                    yield return null;
                    tiempos.Add(Time.unscaledDeltaTime);
                }
                tiempos.Sort();
                float suma = 0f;
                foreach (float t in tiempos)
                {
                    suma += t;
                }
                float promedio = suma / tiempos.Count;
                float peor1 = tiempos[Mathf.Clamp(Mathf.FloorToInt(tiempos.Count * 0.99f), 0, tiempos.Count - 1)];
                yield return new WaitForEndOfFrame();
                Capturar(camara, Path.Combine(salida, punto.nombre + ".png"));
                informe.AppendLine(punto.nombre + ": fps_promedio=" + (1f / promedio).ToString("F1") + " ms_promedio=" + (promedio * 1000f).ToString("F2")
                    + " fps_1pct_bajo=" + (1f / peor1).ToString("F1") + " fotogramas=" + tiempos.Count
                    + " jugador=" + PosicionJugador() + " hora=" + HoraActual());
            }
            File.WriteAllText(Path.Combine(salida, "medicion.txt"), informe.ToString());
            Debug.Log("[Mundo] MedicionAmbiente:\n" + informe);
            Application.Quit(0);
        }

        private static string Argumento(string nombre, string porDefecto)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == nombre)
                {
                    return args[i + 1];
                }
            }
            return porDefecto;
        }

        private Camera CrearCamara()
        {
            GameObject objeto = new GameObject("CamaraMedicion");
            Camera camara = objeto.AddComponent<Camera>();
            camara.nearClipPlane = 0.05f;
            camara.farClipPlane = 1000f;
            camara.fieldOfView = campoVision;
            camara.clearFlags = CameraClearFlags.Skybox;
            camara.depth = 100f;
            camara.allowHDR = true;
            UniversalAdditionalCameraData datos = camara.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            return camara;
        }

        private static void ApagarOtrasCamaras(Camera propia)
        {
            foreach (Camera otra in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (otra != propia)
                {
                    otra.enabled = false;
                }
            }
        }

        private void PonerHora()
        {
            RelojMundo reloj = FindFirstObjectByType<RelojMundo>();
            if (reloj != null)
            {
                reloj.Pausar(true);
                if (Mathf.Abs(reloj.Hora - hora) > 0.01f)
                {
                    reloj.AvanzarHasta(hora);
                }
            }
        }

        private float HoraActual()
        {
            RelojMundo reloj = FindFirstObjectByType<RelojMundo>();
            return reloj != null ? reloj.Hora : -1f;
        }

        private static void MoverJugador(Vector3 pies)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            CharacterController control = jugador.Raiz.GetComponent<CharacterController>();
            if (control != null)
            {
                control.enabled = false;
            }
            jugador.Raiz.position = pies + Vector3.up * 0.05f;
            if (control != null)
            {
                control.enabled = true;
            }
            // La cámara de medición va dentro de la cabeza del jugador: su modelo no se dibuja (como en primera persona).
            foreach (Renderer modelo in jugador.Raiz.GetComponentsInChildren<Renderer>())
            {
                modelo.enabled = false;
            }
        }

        private static string PosicionJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position.ToString("F1") : "sin jugador";
        }

        private void Capturar(Camera camara, string ruta)
        {
            RenderTexture rt = new RenderTexture(ancho, alto, BitsProfundidad);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, ancho, alto), 0, 0);
            imagen.Apply();
            RenderTexture.active = previa;
            camara.targetTexture = null;
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Destroy(imagen);
            rt.Release();
            Destroy(rt);
        }
    }
}
