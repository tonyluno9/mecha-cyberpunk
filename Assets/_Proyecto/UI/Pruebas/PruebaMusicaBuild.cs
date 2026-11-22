using System.Collections;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Sonido;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;

namespace MechaCyberpunk.UI.Pruebas
{
    /// <summary>
    /// Prueba de la música del menú y el volumen en el build: solo actúa con <c>-pruebaMusica &lt;carpeta&gt;</c> (fase 1) y luego
    /// <c>-pruebaMusica &lt;carpeta&gt; -fase2</c> (otra vez el juego: ¿se guardaron los volúmenes?). Las opciones van a la carpeta, nunca a
    /// las del jugador. Mide lo que se puede medir del audio: fuente sonando, nivel del fundido, grupo del mezclador y sus decibeles.
    /// </summary>
    public class PruebaMusicaBuild : MonoBehaviour
    {
        public const string Bandera = "-pruebaMusica";
        public const string BanderaFase2 = "-fase2";
        private const float MaximoEspera = 120f;
        private const float MaximoBucle = 50f;
        private const float MusicaGuardada = 0.55f;
        private const float GeneralGuardado = 0.9f;
        private const float EfectosGuardado = 0.4f;

        private string carpeta;
        private bool fase2;
        private readonly StringBuilder log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Revisar()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            string carpeta = null;
            bool fase2 = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == Bandera && i + 1 < args.Length) carpeta = args[i + 1];
                if (args[i] == BanderaFase2) fase2 = true;
            }
            if (carpeta == null)
            {
                return;
            }
            Directory.CreateDirectory(carpeta);
            string opciones = Path.Combine(carpeta, "opciones.json");
            if (!fase2 && File.Exists(opciones)) File.Delete(opciones);
            ServicioOpciones.FijarRuta(opciones);
            Application.runInBackground = true;
            GameObject go = new GameObject("PruebaMusicaBuild");
            DontDestroyOnLoad(go);
            PruebaMusicaBuild p = go.AddComponent<PruebaMusicaBuild>();
            p.carpeta = carpeta;
            p.fase2 = fase2;
        }

        private IEnumerator Start()
        {
            // El fundido de entrada se mide desde que aparece la música (antes de que el menú termine de armarse).
            float limite = Time.realtimeSinceStartup + MaximoEspera;
            while (!fase2 && MusicaMenu.Actual == null && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            if (!fase2)
            {
                float t0 = Time.realtimeSinceStartup;
                foreach (float t in new[] { 0f, 0.5f, 1f, 2f })
                {
                    while (Time.realtimeSinceStartup - t0 < t) yield return null;
                    Linea("Entrada del tema a los " + t.ToString("0.0") + " s: " + Estado());
                }
            }
            MenuInicio menu = null;
            yield return EsperarMenu(m => menu = m, false);
            if (fase2)
            {
                yield return Fase2(menu);
            }
            else
            {
                yield return Fase1(menu);
            }
            File.WriteAllText(Path.Combine(carpeta, fase2 ? "resultados_fase2.txt" : "resultados_fase1.txt"), log.ToString());
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        private IEnumerator Fase1(MenuInicio menu)
        {
            Linea("FASE 1 (" + Screen.width + "x" + Screen.height + ")");
            Linea("Al abrir el menú: " + Estado());
            yield return new WaitForSecondsRealtime(0.4f);
            Linea("  a los 0.4 s: " + Estado() + " (debe ir entrando)");
            yield return new WaitForSecondsRealtime(1.6f);
            Linea("  a los 2.0 s: " + Estado() + " (debe estar en 1)");
            ServicioOpciones s = FindFirstObjectByType<ServicioOpciones>();
            Linea("Deslizadores en vivo (mismo camino que el deslizador de opciones):");
            foreach (float v in new[] { 0.3f, 0f, 1f, MusicaGuardada })
            {
                s.FijarVolumen(SeccionSonido.Musica, v);
                yield return null;
                Linea("  MÚSICA " + v.ToString("0.00") + " -> VolMusica " + Db(MezclaAudio.ParamMusica) + " dB (esperado " + MezclaAudio.Decibeles(v).ToString("0.0") + ")");
            }
            s.FijarVolumen(SeccionSonido.General, GeneralGuardado);
            s.FijarVolumen(SeccionSonido.Efectos, EfectosGuardado);
            Linea("  GENERAL " + GeneralGuardado + " -> VolGeneral " + Db(MezclaAudio.ParamGeneral) + " dB; EFECTOS " + EfectosGuardado + " -> VolEfectos " + Db(MezclaAudio.ParamEfectos) + " dB");
            Linea("  pantallas del menú: la música no se reinicia al abrir opciones");
            int vueltasAntes = MusicaMenu.Actual != null ? MusicaMenu.Actual.Vueltas : -1;
            float tiempoAntes = MusicaMenu.Actual != null ? MusicaMenu.Actual.TiempoPista : -1f;
            menu.BotonOpciones.Pulsar();
            yield return new WaitForSecondsRealtime(0.5f);
            menu.Opciones.Mostrar(PestanaOpciones.Audio);
            yield return new WaitForSecondsRealtime(1f);
            yield return Foto("1_opciones_audio");
            Linea("  opciones abiertas: " + Estado() + "; tiempo de la pista " + tiempoAntes.ToString("0.0") + " -> " + MusicaMenu.Actual.TiempoPista.ToString("0.0") + " s (sigue, no vuelve a 0)");

            Linea("Bucle (el tema dura 40.4 s; loop normal, como lo hizo Tony):");
            float inicio = Time.realtimeSinceStartup;
            while (MusicaMenu.Actual != null && MusicaMenu.Actual.Vueltas == vueltasAntes && Time.realtimeSinceStartup - inicio < MaximoBucle)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Linea("  dio la vuelta: " + (MusicaMenu.Actual.Vueltas > vueltasAntes) + "; " + Estado());
            File.WriteAllText(Path.Combine(carpeta, "resultados_fase1.txt"), log.ToString());

            // Volver al menú principal antes de la nueva partida (la prueba abrió opciones): se recarga el inicio.
            FlujoPartida.IrAlTitulo();
            yield return EsperarMenu(m => menu = m, true);
            Linea("Volver al inicio desde el menú: " + Estado());

            Linea("NUEVA PARTIDA: la música baja durante la carga y se apaga antes del juego");
            menu.Nueva.Pulsar();
            float t0 = Time.realtimeSinceStartup;
            float apagada = -1f;
            while (Time.realtimeSinceStartup - t0 < MaximoEspera && (Registro<IJugador>.Actual == null || PantallaCargando.Visible))
            {
                if (apagada < 0f && (MusicaMenu.Actual == null || (MusicaMenu.Actual.Nivel <= 0f && !MusicaMenu.Actual.FuenteActiva.isPlaying)))
                {
                    apagada = Time.realtimeSinceStartup - t0;
                }
                yield return null;
            }
            float juego = Time.realtimeSinceStartup - t0;
            Linea("  apagada a los " + apagada.ToString("0.00") + " s; el juego empezó a los " + juego.ToString("0.00") + " s -> " + (apagada >= 0f && apagada <= juego ? "se apaga ANTES" : "NO se apagó antes"));
            yield return new WaitForSecondsRealtime(2f);
            Linea("  ya en el juego: " + Estado());

            Linea("SALIR AL MENÚ desde el juego: vuelve a sonar");
            FlujoPartida.IrAlTitulo();
            yield return EsperarMenu(m => menu = m, true);
            yield return new WaitForSecondsRealtime(2f);
            Linea("  en el menú otra vez: " + Estado());
        }

        private IEnumerator Fase2(MenuInicio menu)
        {
            ServicioOpciones s = FindFirstObjectByType<ServicioOpciones>();
            Linea("FASE 2 (otra vez el juego, mismas opciones):");
            Linea("  guardados: GENERAL " + s.Volumen(SeccionSonido.General).ToString("0.00") + " (esperado " + GeneralGuardado + "), MÚSICA " + s.Volumen(SeccionSonido.Musica).ToString("0.00")
                + " (" + MusicaGuardada + "), EFECTOS " + s.Volumen(SeccionSonido.Efectos).ToString("0.00") + " (" + EfectosGuardado + ")");
            Linea("  mezclador: VolGeneral " + Db(MezclaAudio.ParamGeneral) + " dB, VolMusica " + Db(MezclaAudio.ParamMusica) + " dB, VolEfectos " + Db(MezclaAudio.ParamEfectos) + " dB");
            yield return new WaitForSecondsRealtime(2f);
            Linea("  música: " + Estado());
            menu.BotonOpciones.Pulsar();
            yield return new WaitForSecondsRealtime(0.5f);
            menu.Opciones.Mostrar(PestanaOpciones.Audio);
            yield return new WaitForSecondsRealtime(1f);
            yield return Foto("2_opciones_audio_guardadas");
        }

        private static string Estado()
        {
            MusicaMenu m = MusicaMenu.Actual;
            if (m == null)
            {
                return "sin música";
            }
            AudioSource f = m.FuenteActiva;
            return "nivel " + m.Nivel.ToString("0.00") + ", sonando " + f.isPlaying + ", pista " + (f.clip != null ? f.clip.name : "-") + " " + m.TiempoPista.ToString("0.0") + " s, grupo "
                + (f.outputAudioMixerGroup != null ? f.outputAudioMixerGroup.name : "(ninguno)") + ", estéreo " + (f.clip != null && f.clip.channels == 2) + ", vueltas " + m.Vueltas;
        }

        private static string Db(string parametro)
        {
            return MezclaAudio.Mezclador != null && MezclaAudio.Mezclador.GetFloat(parametro, out float v) ? v.ToString("0.0") : "?";
        }

        private IEnumerator EsperarMenu(System.Action<MenuInicio> listo, bool esperarNuevo)
        {
            float fin = Time.realtimeSinceStartup + MaximoEspera;
            MenuInicio anterior = esperarNuevo ? FindFirstObjectByType<MenuInicio>() : null;
            MenuInicio m = null;
            while (Time.realtimeSinceStartup < fin)
            {
                m = FindFirstObjectByType<MenuInicio>();
                if (m != null && m != anterior && m.Estado == EstadoInicio.Principal && !PantallaCargando.Visible)
                {
                    break;
                }
                yield return null;
            }
            yield return null;
            listo(m);
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
            Debug.Log("[UI] PruebaMusica: " + texto);
        }
    }
}
