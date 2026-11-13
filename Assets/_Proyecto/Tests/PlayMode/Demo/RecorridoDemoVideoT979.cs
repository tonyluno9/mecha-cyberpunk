using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Demo
{
    /// <summary>
    /// T-979: el video de la demo (2 a 3 min, 1080p). Lo juega el mismo bot del recorrido y se graba solo lo que se ve bien: título, creación, la ciudad de noche
    /// (cámara que recorre la calle sin HUD), el asalto y la persecución, EL DOC y su clínica, el banco de Tino, el casino VivaCoin y EL HACKER con NOVA y CONTINUARÁ.
    /// Necesita el editor CON VENTANA (en batchmode ScreenCapture no sirve) y ffmpeg. Deja video.mp4 y audio.wav en T979_SALIDA (Logs/video-t979 por defecto).
    /// Correr: Unity.exe -projectPath . -runTests -testPlatform PlayMode -testFilter "RecorridoDemoPruebas.VideoDemoT979" -calidad Alto, con T979_FFMPEG=ruta de ffmpeg.exe.
    /// </summary>
    public partial class RecorridoDemoPruebas
    {
        private GrabadorVideoT979 grabador;

        [UnityTest]
        [Explicit("T-979: graba el video de la demo; solo a propósito, con el editor con ventana")]
        [Timeout(3600000)]
        public IEnumerator VideoDemoT979()
        {
            LogAssert.ignoreFailingMessages = true;
            string ffmpeg = Environment.GetEnvironmentVariable("T979_FFMPEG") ?? "ffmpeg";
            string salida = Path.GetFullPath(Environment.GetEnvironmentVariable("T979_SALIDA") ?? "Logs/video-t979");
            PlayModeWindow.SetCustomRenderingResolution(GrabadorVideoT979.Ancho, GrabadorVideoT979.Alto, "Video T-979");

            // 1. Título.
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaTitulo titulo = null;
            yield return EsperarTitulo(t => titulo = t);
            yield return Segundos(1.5f);
            Debug.Log("[T-979] pantalla " + Screen.width + "x" + Screen.height);
            grabador = GrabadorVideoT979.Empezar(salida, ffmpeg);
            grabador.Fijar(true);
            yield return Segundos(3f);
            yield return Elegir(titulo.Menu, titulo.Opciones, "OPCIONES");
            yield return Segundos(1.2f);
            yield return Elegir(titulo.Menu, titulo.Nueva, "NUEVA PARTIDA");
            yield return Segundos(1.5f);

            // 2. Creación del personaje.
            yield return Pulsar(teclado.enterKey);
            yield return Segundos(1f);
            yield return Pulsar(teclado.downArrowKey);
            yield return Escribir(NombreJugador);
            yield return Segundos(1f);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            grabador.Fijar(false);
            DirectorPandillas directorViejo = Director;
            Presionar(teclado.enterKey);
            yield return null;
            Soltar(teclado.enterKey);
            DirectorPandillas director = null;
            yield return EsperarCiudad(directorViejo, d => director = d);

            // 3. Despierta en su cuarto (MEGALOPOLIS · SUBURBIOS).
            yield return Grabar(6f);

            // 4. La ciudad de noche: una cámara recorre la calle principal, sin HUD.
            yield return Toma(new Vector3(-62f, 6f, -2f), new Vector3(1f, -0.12f, 0.05f), new Vector3(4f, 2.4f, -2f), new Vector3(1f, -0.04f, 0f), 14f, 62f);

            // 5. Sale de casa.
            grabador.Fijar(true);
            float finPuerta = Time.realtimeSinceStartup + 9f;
            IEnumerator puerta = IrYUsar("la puerta de casa", () => director.Paso != PasoHistoria.Despierta, 60f, 0.1f);
            while (Time.realtimeSinceStartup < finPuerta && puerta.MoveNext())
            {
                yield return puerta.Current;
            }
            grabador.Fijar(false);
            while (puerta.MoveNext())
            {
                yield return puerta.Current;
            }

            // 6. El asalto (cinemática sin saltar), la persecución y la pelea.
            if (director.Paso == PasoHistoria.Asalto)
            {
                grabador.Fijar(true);
                float finCine = Time.realtimeSinceStartup + 12f;
                while (director.Asalto.Fase == FaseAsalto.Cinematica && Time.realtimeSinceStartup < finCine)
                {
                    yield return Siguiente();
                }
                if (director.Asalto.Fase == FaseAsalto.Cinematica)
                {
                    yield return Pulsar(teclado.enterKey);
                }
                // La persecución: una cámara sigue al asaltante (el marcador) desde atrás y arriba, sin HUD, mientras el bot corre detrás.
                grabador.Fijar(false);
                HudCinematica.Entrar();
                var goSigue = new GameObject("CamaraPersecucionT979");
                Camera camSigue = goSigue.AddComponent<Camera>();
                camSigue.depth = 100f;
                camSigue.fieldOfView = 60f;
                camSigue.nearClipPlane = 0.1f;
                camSigue.farClipPlane = 600f;
                UniversalAdditionalCameraData datosSigue = goSigue.GetComponent<UniversalAdditionalCameraData>() ?? goSigue.AddComponent<UniversalAdditionalCameraData>();
                datosSigue.renderPostProcessing = true;
                Vector3 anterior = Vector3.zero;
                bool hayAnterior = false;
                bool colocada = false;
                Vector3 frente = Vector3.forward;
                float inicioPersecucion = Time.realtimeSinceStartup;
                float finAsalto = Time.realtimeSinceStartup + 100f;
                while (director.Asalto.Fase != FaseAsalto.Maletin && director.Paso == PasoHistoria.Asalto && Time.realtimeSinceStartup < finAsalto)
                {
                    bool enToma = Time.realtimeSinceStartup - inicioPersecucion < 13f;
                    if (PosicionMarcador(out Vector3 marca))
                    {
                        PasoRuta(marca, true);
                        if (hayAnterior && (marca - anterior).sqrMagnitude > 0.0004f)
                        {
                            frente = Vector3.Slerp(frente, Vector3.ProjectOnPlane(marca - anterior, Vector3.up).normalized, 0.05f);
                        }
                        anterior = marca;
                        hayAnterior = true;
                        Vector3 deseada = marca - frente * 9f + Vector3.up * 4.5f;
                        goSigue.transform.position = colocada ? Vector3.Lerp(goSigue.transform.position, deseada, 0.08f) : deseada;
                        colocada = true;
                        goSigue.transform.rotation = Quaternion.LookRotation(marca + Vector3.up - goSigue.transform.position, Vector3.up);
                    }
                    if (!enToma && camSigue.enabled)
                    {
                        camSigue.enabled = false;
                        HudCinematica.Salir();
                    }
                    grabador.Fijar(enToma && colocada);
                    yield return Siguiente();
                }
                if (camSigue.enabled)
                {
                    HudCinematica.Salir();
                }
                Object.Destroy(goSigue);
                // La pelea no se graba: el bot la hace pegado a una puerta y no se ve bien.
                grabador.Fijar(false);
                // T-987: se recoge el maletín que soltaron con E (ya no hay que vencer al asaltante).
                if (director.Asalto.Maletin != null)
                {
                    yield return Colocar(director.Asalto.Maletin.transform.position, 1.4f);
                    yield return Pulsar(teclado.eKey, 15);
                }
            }

            // 7. EL DOC y su clínica.
            director.Cargar("{\"paso\":\"" + PasoHistoria.Doc + "\"}");
            yield return Segundos(1f);
            PersonajeApoyo doc = Apoyo(DirectorPandillas.IdDoc);
            if (doc != null)
            {
                yield return Colocar(doc.transform.position, 2.5f);
                yield return Segundos(1f);
                grabador.Fijar(true);
                IEnumerator alDoc = IrYUsar("EL DOC", () => director.Paso != PasoHistoria.Doc, 40f, 8f, DirectorPandillas.IdDoc);
                while (alDoc.MoveNext())
                {
                    yield return alDoc.Current;
                }
                yield return Segundos(3f);
                grabador.Fijar(false);
            }
            MostradorClinica clinica = Object.FindFirstObjectByType<MostradorClinica>();
            if (clinica != null)
            {
                yield return Colocar(clinica.Posicion, 1.2f);
                yield return Segundos(0.8f);
                float finAbrir = Time.realtimeSinceStartup + 4f;
                while (!clinica.PantallaAbierta && Time.realtimeSinceStartup < finAbrir)
                {
                    yield return Pulsar(teclado.eKey, 15);
                }
                Debug.Log("[T-979] clínica abierta: " + clinica.PantallaAbierta);
                if (clinica.PantallaAbierta)
                {
                    grabador.Fijar(true);
                    yield return Navegar(9f);
                    yield return Pulsar(teclado.escapeKey, 10);
                    yield return Segundos(0.6f);
                    grabador.Fijar(false);
                }
            }

            // 8. EL INVENTOR y el banco de trabajo de Tino (CIC): como en el recorrido, primero se habla con él.
            director.Cargar("{\"paso\":\"" + PasoHistoria.Inventor + "\"}");
            yield return Segundos(1f);
            PersonajeApoyo inventor = Apoyo(DirectorPandillas.IdInventor);
            if (inventor != null)
            {
                yield return Colocar(inventor.transform.position, 4f);
                IEnumerator alInventor = IrYUsar("EL INVENTOR", () => director.Paso != PasoHistoria.Inventor, 40f, 8f, DirectorPandillas.IdInventor);
                while (alInventor.MoveNext())
                {
                    yield return alInventor.Current;
                }
            }
            comprando = true;
            BancoArmas banco = Object.FindFirstObjectByType<BancoArmas>();
            if (banco != null)
            {
                yield return Colocar(banco.Posicion + Vector3.back * 1.0f, 1.2f);
                yield return Segundos(0.8f);
                float finAbrir = Time.realtimeSinceStartup + 6f;
                while (!banco.PantallaAbierta && Time.realtimeSinceStartup < finAbrir)
                {
                    yield return Pulsar(teclado.eKey, 15);
                }
                Debug.Log("[T-979] banco abierto: " + banco.PantallaAbierta);
                if (banco.PantallaAbierta)
                {
                    grabador.Fijar(true);
                    yield return Navegar(10f);
                    yield return Pulsar(teclado.escapeKey, 10);
                    yield return Segundos(0.6f);
                    grabador.Fijar(false);
                }
            }
            comprando = false;

            // 9. El casino VivaCoin por dentro.
            yield return Colocar(new Vector3(66f, 0.3f, 48f), 1.2f);
            yield return Segundos(2f);
            yield return Toma(new Vector3(38f, 4f, 39f), new Vector3(1f, -0.05f, 0.5f), new Vector3(49f, 2f, 45.5f), new Vector3(1f, 0.08f, 0.1f), 7f, 60f);
            yield return Toma(new Vector3(61f, 1.8f, 47f), new Vector3(1f, -0.02f, 0.1f), new Vector3(71f, 1.9f, 48.5f), new Vector3(1f, -0.05f, 0.15f), 8f, 70f);
            yield return Toma(new Vector3(82f, 2.4f, 42.5f), new Vector3(-0.8f, -0.12f, 0.6f), new Vector3(80f, 2.2f, 45f), new Vector3(-1f, -0.1f, 0.35f), 6f, 72f);
            yield return Toma(new Vector3(70f, 1.7f, 52.5f), new Vector3(-0.6f, -0.05f, 0.8f), new Vector3(72f, 1.7f, 53f), new Vector3(0f, -0.05f, 1f), 5f, 62f);

            // 10. Neón Alto: EL HACKER, NOVA y CONTINUARÁ.
            director.Cargar("{\"paso\":\"" + PasoHistoria.NeonAlto + "\"}");
            yield return Segundos(1f);
            PersonajeApoyo hacker = Apoyo(DirectorPandillas.IdHacker);
            if (hacker != null)
            {
                yield return Colocar(hacker.transform.position, 6f);
                yield return Segundos(1.5f);
                grabador.Fijar(true);
                IEnumerator alHacker = IrYUsar("EL HACKER", () => director.Paso != PasoHistoria.NeonAlto, 40f, 8f, DirectorPandillas.IdHacker);
                while (alHacker.MoveNext())
                {
                    yield return alHacker.Current;
                }
                float finFinal = Time.realtimeSinceStartup + 30f;
                while (!(director.Pantalla != null && director.Pantalla.ContinuaraVisible) && Time.realtimeSinceStartup < finFinal)
                {
                    yield return Siguiente();
                }
                yield return Segundos(4f);
            }
            grabador.Fijar(false);

            long cuadros = 0;
            yield return grabador.Terminar(c => cuadros = c);
            Assert.Greater(cuadros, GrabadorVideoT979.CuadrosPorSegundo * 60, "Se grabó al menos un minuto");
        }

        private IEnumerator Grabar(float segundos)
        {
            grabador.Fijar(true);
            yield return Segundos(segundos);
            grabador.Fijar(false);
        }

        // En un menú (clínica o banco) baja y cambia de pestaña poco a poco, como quien lo mira.
        private IEnumerator Navegar(float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            int i = 0;
            yield return Segundos(1.5f);
            while (Time.realtimeSinceStartup < fin)
            {
                yield return Pulsar(i % 3 == 2 ? teclado.rightArrowKey : teclado.downArrowKey);
                i++;
                yield return Segundos(1.4f);
            }
        }

        // Una toma de cámara sin HUD: va de un punto a otro (mirando de una dirección a otra) en `segundos`, con el movimiento suavizado.
        private IEnumerator Toma(Vector3 desde, Vector3 miraDesde, Vector3 hasta, Vector3 miraHasta, float segundos, float campo)
        {
            HudCinematica.Entrar();
            var go = new GameObject("CamaraVideoT979");
            Camera cam = go.AddComponent<Camera>();
            cam.depth = 100f;
            cam.fieldOfView = campo;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            UniversalAdditionalCameraData datos = go.GetComponent<UniversalAdditionalCameraData>() ?? go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            float inicio = Time.realtimeSinceStartup;
            go.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(miraDesde, Vector3.up));
            yield return null;
            grabador.Fijar(true);
            while (Time.realtimeSinceStartup - inicio < segundos)
            {
                float f = Mathf.SmoothStep(0f, 1f, (Time.realtimeSinceStartup - inicio) / segundos);
                go.transform.SetPositionAndRotation(Vector3.Lerp(desde, hasta, f), Quaternion.Slerp(Quaternion.LookRotation(miraDesde, Vector3.up), Quaternion.LookRotation(miraHasta, Vector3.up), f));
                yield return null;
            }
            grabador.Fijar(false);
            Object.Destroy(go);
            HudCinematica.Salir();
            yield return null;
        }

        private static PersonajeApoyo Apoyo(string id)
        {
            foreach (PersonajeApoyo pa in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                if (pa.Datos != null && pa.Datos.Id == id)
                {
                    return pa;
                }
            }
            return null;
        }
    }
}
