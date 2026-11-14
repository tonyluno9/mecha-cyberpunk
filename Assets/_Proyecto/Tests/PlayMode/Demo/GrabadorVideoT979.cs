using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace MechaCyberpunk.Tests.Demo
{
    /// <summary>
    /// T-979: graba el juego a un video en tiempo real (solo con el editor con ventana: en batchmode ScreenCapture no sirve). Cada cuadro de la vista de juego
    /// (con la interfaz) se lee de la GPU sin detener el juego (AsyncGPUReadback) y va a ffmpeg por su entrada estándar, a 30 cuadros/s constantes (se repite o
    /// se salta un cuadro según el reloj real). El sonido sale del AudioListener activo (<see cref="OidoGrabadorT979"/>) a un .wav. Solo cuenta mientras
    /// <see cref="Grabando"/>: entre tramos se pausa. Al <see cref="Terminar"/> deja video.mp4 y audio.wav en la carpeta.
    /// </summary>
    public class GrabadorVideoT979 : MonoBehaviour
    {
        public const int Ancho = 1920;
        public const int Alto = 1080;
        public const int CuadrosPorSegundo = 30;

        public static GrabadorVideoT979 Instancia { get; private set; }
        // Se lee desde el hilo de audio: un bool simple (no se compara un objeto de Unity fuera del hilo principal).
        public static bool Grabando => grabandoAhora;
        private static volatile bool grabandoAhora;

        public static readonly ConcurrentQueue<float[]> Sonido = new ConcurrentQueue<float[]>();

        private bool grabando;
        private double segundosGrabados;
        private long cuadrosEscritos;
        private long cuadrosPedidos;
        private Process ffmpeg;
        private Stream entrada;
        private RenderTexture captura;
        private byte[] bufer;
        private FileStream wav;
        private long muestrasWav;
        private int canales = 2;
        private string carpeta;

        public double SegundosGrabados => segundosGrabados;

        public static GrabadorVideoT979 Empezar(string carpetaSalida, string rutaFfmpeg)
        {
            var go = new GameObject("GrabadorVideoT979");
            DontDestroyOnLoad(go);
            GrabadorVideoT979 g = go.AddComponent<GrabadorVideoT979>();
            Instancia = g;
            g.carpeta = carpetaSalida;
            Directory.CreateDirectory(carpetaSalida);
            g.captura = new RenderTexture(Ancho, Alto, 0, RenderTextureFormat.ARGB32);
            g.bufer = new byte[Ancho * Alto * 4];
            var info = new ProcessStartInfo
            {
                FileName = rutaFfmpeg,
                Arguments = "-y -loglevel error -f rawvideo -pix_fmt rgba -s " + Ancho + "x" + Alto + " -r " + CuadrosPorSegundo
                    + " -i - -c:v libx264 -preset veryfast -crf 16 -pix_fmt yuv420p \"" + Path.Combine(carpetaSalida, "video.mp4") + "\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
            };
            g.ffmpeg = Process.Start(info);
            g.entrada = g.ffmpeg.StandardInput.BaseStream;
            g.wav = new FileStream(Path.Combine(carpetaSalida, "audio.wav"), FileMode.Create);
            g.wav.Write(new byte[44], 0, 44);
            while (Sonido.TryDequeue(out _)) { }
            g.StartCoroutine(g.Ciclo());
            return g;
        }

        public void Fijar(bool encendido)
        {
            if (encendido && !grabando)
            {
                while (Sonido.TryDequeue(out _)) { }
            }
            grabando = encendido;
            grabandoAhora = encendido;
        }

        private IEnumerator Ciclo()
        {
            var fin = new WaitForEndOfFrame();
            while (true)
            {
                yield return fin;
                AsegurarOido();
                if (!grabando)
                {
                    continue;
                }
                segundosGrabados += Time.unscaledDeltaTime;
                long debidos = (long)(segundosGrabados * CuadrosPorSegundo) - cuadrosPedidos;
                if (debidos <= 0)
                {
                    continue;
                }
                cuadrosPedidos += debidos;
                ScreenCapture.CaptureScreenshotIntoRenderTexture(captura);
                long veces = debidos;
                AsyncGPUReadback.Request(captura, 0, TextureFormat.RGBA32, r => Escribir(r, veces));
            }
        }

        private void Escribir(AsyncGPUReadbackRequest r, long veces)
        {
            if (r.hasError || entrada == null)
            {
                return;
            }
            r.GetData<byte>().CopyTo(bufer);
            for (long i = 0; i < veces; i++)
            {
                entrada.Write(bufer, 0, bufer.Length);
                cuadrosEscritos++;
            }
        }

        private void Update()
        {
            while (Sonido.TryDequeue(out float[] bloque))
            {
                if (wav == null)
                {
                    continue;
                }
                var bytes = new byte[bloque.Length * 2];
                for (int i = 0; i < bloque.Length; i++)
                {
                    short s = (short)(Mathf.Clamp(bloque[i], -1f, 1f) * short.MaxValue);
                    bytes[i * 2] = (byte)(s & 0xff);
                    bytes[i * 2 + 1] = (byte)((s >> 8) & 0xff);
                }
                wav.Write(bytes, 0, bytes.Length);
                muestrasWav += bloque.Length;
            }
        }

        // El sonido final de la mezcla se oye en el AudioListener activo (cambia entre a pie, carro y escenas): ahí se pone el oído.
        private void AsegurarOido()
        {
            foreach (AudioListener oyente in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (oyente.isActiveAndEnabled && oyente.GetComponent<OidoGrabadorT979>() == null)
                {
                    oyente.gameObject.AddComponent<OidoGrabadorT979>();
                }
            }
            canales = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
        }

        /// <summary>Cierra el video (espera a ffmpeg) y el .wav. Devuelve los cuadros escritos.</summary>
        public IEnumerator Terminar(Action<long> alTerminar)
        {
            grabando = false;
            grabandoAhora = false;
            // Las lecturas de GPU pendientes terminan en unos cuadros.
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
            AsyncGPUReadback.WaitAllRequests();
            Update();
            entrada.Flush();
            entrada.Close();
            entrada = null;
            ffmpeg.WaitForExit(120000);
            CerrarWav();
            Debug.Log("[T-979] audio llamadas " + OidoGrabadorT979.Llamadas + ", oyentes " + FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length + ", volumen " + AudioListener.volume + ", pausa " + AudioListener.pause + "; video: " + cuadrosEscritos + " cuadros (" + (cuadrosEscritos / (float)CuadrosPorSegundo).ToString("F1") + " s), sonido " + (muestrasWav / (float)canales / AudioSettings.outputSampleRate).ToString("F1") + " s en " + carpeta);
            alTerminar?.Invoke(cuadrosEscritos);
            Destroy(captura);
            Destroy(gameObject);
            Instancia = null;
        }

        private void CerrarWav()
        {
            int frecuencia = AudioSettings.outputSampleRate;
            long datos = muestrasWav * 2;
            wav.Seek(0, SeekOrigin.Begin);
            var w = new BinaryWriter(wav);
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write((int)(36 + datos));
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)canales);
            w.Write(frecuencia);
            w.Write(frecuencia * canales * 2);
            w.Write((short)(canales * 2));
            w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write((int)datos);
            w.Flush();
            wav.Close();
            wav = null;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                grabandoAhora = false;
                Instancia = null;
            }
        }
    }
}
