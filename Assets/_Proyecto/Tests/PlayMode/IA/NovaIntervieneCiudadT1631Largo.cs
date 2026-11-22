using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1631 (T17), comprobación larga en Ciudad_Prologo con el jugador invulnerable y quieto: 2 estrellas unos segundos (policía), luego 5 estrellas y ~3 minutos de
    /// juego (timeScale 3). Escribe docs/capturas/_tareas/T17/registro.txt (todas las líneas [Respuesta] y un resumen cada 15 s) y capturas con el HUD:
    /// aviso NOVA INTERVIENE, cápsula cayendo y Nova junto al jugador. Es Explicit: solo corre si se pide por nombre (necesita gráficos para las capturas).
    /// </summary>
    public class NovaIntervieneCiudadT1631Largo
    {
        private const string Escena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "docs/capturas/_tareas/T17/";
        private const float SegundosPolicia = 25f;
        private const float SegundosNova = 180f;
        private const float Escala = 3f;
        private const float MetrosNovaCerca = 14f;

        private readonly List<string> lineas = new List<string>();
        private bool avisoPendiente;

        private void AlLog(string texto, string traza, LogType tipo)
        {
            if (texto.StartsWith("[Respuesta]"))
            {
                lineas.Add(texto);
            }
        }

        [UnityTest, Explicit("Larga: ~3 minutos de juego en Ciudad_Prologo (escribe registro y capturas en docs/capturas/_tareas/T17)")]
        public IEnumerator CincoEstrellas_PoliciaSeRetira_YNovaLlegaProgresivo()
        {
            LogAssert.ignoreFailingMessages = true;
            Registros.LimpiarTodos();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(Escena,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            float limite = Time.realtimeSinceStartup + 90f;
            while (Registro<IJugador>.Actual == null || Registro<ICalor>.Actual == null || Registro<IZonaBusqueda>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae al jugador, el calor y la persecución");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
            VidaAPie vida = Registro<IJugador>.Actual.Raiz.GetComponentInChildren<VidaAPie>();
            Assert.IsNotNull(vida, "el jugador a pie lleva VidaAPie");
            Application.logMessageReceived += AlLog;
            System.Action alAviso = () => avisoPendiente = true;
            EventosRespuesta.NovaInterviene += alAviso;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1631 NOVA INTERVIENE, Ciudad_Prologo, jugador invulnerable y quieto; " + SegundosPolicia + " s con 2 estrellas y " + SegundosNova + " s con 5 (timeScale " + Escala + ")");
            Time.timeScale = Escala;
            float inicio = Time.time;
            float proximoResumen = inicio;
            int capturasCapsula = 0;
            int capturasNova = 0;
            bool avisoCapturado = false;
            bool modoNovaVisto = false;
            float realInicio = Time.realtimeSinceStartup;
            try
            {
                DirectorRespuesta.DepurarSubirEstrellas(2);
                while (Time.time - inicio < SegundosPolicia + SegundosNova)
                {
                    Assert.Less(Time.realtimeSinceStartup - realInicio, 900f, "Se pasó del tiempo real");
                    vida.CurarTodo();
                    vida.DarInvulnerabilidad(5f);
                    if (Time.time - inicio >= SegundosPolicia)
                    {
                        DirectorRespuesta.DepurarSubirEstrellas(5);
                    }
                    else
                    {
                        DirectorRespuesta.DepurarSubirEstrellas(2);
                    }
                    modoNovaVisto |= DirectorRespuesta.ModoNova;
                    if (Time.time >= proximoResumen)
                    {
                        proximoResumen += 15f;
                        sb.AppendLine("t=" + (Time.time - inicio).ToString("0") + " s: " + DirectorRespuesta.DepurarEstado()
                            + " | policias a pie " + PoliciasAPie() + ", capsulas en escena " + Object.FindObjectsByType<CapsulaCaida>(FindObjectsSortMode.None).Length);
                    }
                    if (avisoPendiente && !avisoCapturado)
                    {
                        avisoCapturado = true;
                        avisoPendiente = false;
                        yield return Capturar("1-nova-interviene.png", sb, 0.6f);
                    }
                    else if (capturasCapsula < 1 && Object.FindObjectsByType<CapsulaCaida>(FindObjectsSortMode.None).Length > 0)
                    {
                        capturasCapsula++;
                        yield return Capturar("2-capsula-cayendo.png", sb, 0.8f);
                    }
                    else if (capturasNova < 1 && NovaCerca(MetrosNovaCerca))
                    {
                        capturasNova++;
                        yield return Capturar("3-nova-junto-al-jugador.png", sb, 0.2f);
                    }
                    yield return null;
                }
            }
            finally
            {
                Time.timeScale = 1f;
                Application.logMessageReceived -= AlLog;
                EventosRespuesta.NovaInterviene -= alAviso;
            }
            sb.AppendLine();
            sb.AppendLine("Capturas: aviso " + (avisoCapturado ? "si" : "no") + ", capsula " + capturasCapsula + ", Nova junto al jugador " + capturasNova
                + " (las capturas usan ScreenCapture: incluyen el HUD si el Game view lo dibuja)");
            sb.AppendLine("Segundos reales: " + (Time.realtimeSinceStartup - realInicio).ToString("0"));
            sb.AppendLine();
            sb.AppendLine("--- Registro [Respuesta] ---");
            foreach (string l in lineas)
            {
                sb.AppendLine(l);
            }
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Carpeta + "registro.txt", sb.ToString());
            Debug.Log("[Prueba] T-1631 largo: registro en " + Carpeta + "registro.txt");
            Assert.IsTrue(modoNovaVisto, "con 5 estrellas entra el modo NOVA INTERVIENE");
            int modo = lineas.FindIndex(l => l.Contains("NOVA INTERVIENE"));
            Assert.GreaterOrEqual(modo, 0, "el registro anota NOVA INTERVIENE");
            for (int i = modo + 1; i < lineas.Count; i++)
            {
                bool policiaNueva = (lineas[i].Contains(" Patrulla ") || lineas[i].Contains(" Camioneta ")) && lineas[i].Contains("(sale)");
                Assert.IsFalse(policiaNueva, "no sale policía nueva en modo Nova: " + lineas[i]);
            }
            int primera = lineas.FindIndex(modo, l => l.Contains("(sale)") && (l.Contains("Nova") || l.Contains("Robot") || l.Contains("Perro")));
            if (primera >= 0)
            {
                Assert.IsTrue(lineas[primera].Contains("NovaHumanoide"), "el primero de Nova es un humanoide: " + lineas[primera]);
            }
        }


        private static int PoliciasAPie()
        {
            int n = 0;
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && p.Datos != null && p.Datos.EsPolicia && !p.EstaKO)
                {
                    n++;
                }
            }
            return n;
        }

        private static bool NovaCerca(float metros)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return false;
            }
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && p.Datos != null && !p.Datos.EsPolicia && !p.EstaKO && p.name.StartsWith("Nova_")
                    && Vector3.Distance(p.transform.position, jugador.Raiz.position) <= metros)
                {
                    return true;
                }
            }
            return false;
        }

        // Espera unos segundos de juego y guarda lo que se ve en el Game view (con el HUD de pantalla).
        private static IEnumerator Capturar(string nombre, StringBuilder sb, float segundosAntes)
        {
            float hasta = Time.time + segundosAntes;
            while (Time.time < hasta)
            {
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            Texture2D img = ScreenCapture.CaptureScreenshotAsTexture();
            if (img == null)
            {
                sb.AppendLine("captura " + nombre + ": no se pudo (sin gráficos)");
                yield break;
            }
            Directory.CreateDirectory(Carpeta);
            File.WriteAllBytes(Carpeta + nombre, img.EncodeToPNG());
            Object.Destroy(img);
            sb.AppendLine("captura " + nombre + " a los " + Time.time.ToString("0") + " s de juego");
        }
    }
}
