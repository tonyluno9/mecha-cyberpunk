using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Barrio;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-1633 (T12): bullying real en Ciudad_Prologo con el jugador invulnerable. Fuerza un evento SKULLS (grupo de acoso de Suburbios) y uno SNAKES
    /// (Neón Alto), deja correr cada uno hasta 40 s sin intervenir y otra vez interviniendo (golpe a un agresor), y escribe
    /// docs/capturas/_tareas/T12/registro.txt con quién habló, qué voz/subtítulo, el empujón, la huida y la intervención, más capturas.
    /// Se corre a propósito con -testFilter, con el editor con ventana (necesita render).
    /// </summary>
    public class BullyingCiudadT1633Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "docs/capturas/_tareas/T12";
        private const float SegundosPorEvento = 40f;
        private const float DistanciaJugador = 28f;
        private const int Ancho = 1280;
        private const int Alto = 720;

        private readonly StringBuilder registro = new StringBuilder();
        private readonly List<string> logsBarrio = new List<string>();
        private float t0;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            Application.logMessageReceived += AlLog;
            Subtitulos.Publicada += AlSubtitulo;
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            Application.logMessageReceived -= AlLog;
            Subtitulos.Publicada -= AlSubtitulo;
            DirectorEscenasBarrio.Habilitado = true;
            Scene ciudad = SceneManager.GetSceneByPath(RutaEscena);
            if (ciudad.IsValid() && ciudad.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("VaciaT1633"));
                yield return SceneManager.UnloadSceneAsync(ciudad);
            }
        }

        private void AlLog(string mensaje, string pila, LogType tipo)
        {
            if (mensaje.StartsWith("[Barrio]"))
            {
                Anotar("LOG " + mensaje);
            }
        }

        private void AlSubtitulo(Subtitulos.Linea l) => Anotar("SUBTITULO [" + l.Hablante + "] " + l.Texto);

        private void Anotar(string texto) => registro.AppendLine("t+" + (Time.time - t0).ToString("0.0") + "s " + texto);

        private static IJugador Jugador => Registro<IJugador>.Actual;

        private static void Teletransportar(Vector3 punto)
        {
            Transform raiz = Jugador.Raiz;
            CharacterController cc = raiz.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
            raiz.position = punto;
            if (cc != null)
            {
                cc.enabled = true;
            }
        }

        private IEnumerator Captura(string nombre, Vector3 mirar)
        {
            Directory.CreateDirectory(Carpeta);
            Vector3 camara = mirar + new Vector3(-4f, 2.2f, -4f);
            GameObject go = new GameObject("CamT1633");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.depth = 100f;
            go.transform.position = camara;
            go.transform.LookAt(mirar + Vector3.up);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            cam.enabled = false;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Carpeta + "/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Anotar("CAPTURA " + nombre + ".png");
        }

        // Corre una escena forzada: sin intervenir deja pasar el tiempo; con intervención golpea a un agresor cuando ya empezó a hablar.
        private IEnumerator Correr(string id, bool intervenir, string etiqueta, Vector3 puntoJugador)
        {
            Teletransportar(puntoJugador);
            yield return null;
            t0 = Time.time;
            Anotar("=== " + etiqueta + " (" + id + ", intervenir=" + intervenir + ")");
            string r = DirectorEscenasBarrio.ForzarEscena(id);
            Anotar("FORZAR " + r);
            if (!r.StartsWith("ok"))
            {
                yield break;
            }
            bool capturadoEmpujon = false;
            bool intervino = false;
            int lineas = 0;
            Subtitulos.Publicada += l => lineas++;
            while (Time.time - t0 < SegundosPorEvento)
            {
                if (!DirectorEscenasBarrio.HayEscenaEnCurso && (!intervenir || intervino))
                {
                    break;
                }
                if (lineas >= 3 && !capturadoEmpujon)
                {
                    capturadoEmpujon = true;
                    yield return Captura(etiqueta + "_empujon", PosicionAgresor());
                }
                if (intervenir && !intervino && lineas >= 4)
                {
                    PandilleroAPie a = Agresor();
                    if (a != null)
                    {
                        intervino = true;
                        Anotar("INTERVENCION: el jugador golpea a " + a.name);
                        a.RecibirDanio(new InfoDanio(5f, TipoDanio.Golpe, a.transform.position + Vector3.up, Vector3.forward, null));
                        yield return new WaitForSeconds(2.5f);
                        Anotar("RESULTADO: escena en curso=" + DirectorEscenasBarrio.HayEscenaEnCurso + ", agresor hostil=" + a.EsHostil + ", alerta=" + a.Alerta);
                        yield return Captura(etiqueta + "_contra_jugador", a.transform.position);
                    }
                }
                yield return null;
            }
            yield return Captura(etiqueta + "_final", PosicionAgresor());
            Anotar("FIN escena (en curso=" + DirectorEscenasBarrio.HayEscenaEnCurso + ")");
        }

        private PandilleroAPie ultimoAgresor;

        private PandilleroAPie Agresor()
        {
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && p.Acosando && p.GetComponentInParent<GrupoAcoso>() is GrupoAcoso g && g.EsPrestado(p))
                {
                    ultimoAgresor = p;
                    return p;
                }
                if (p != null && p.Acosando && p.GetComponentInParent<GrupoAcoso>() == null && p == ultimoAgresor)
                {
                    return p;
                }
            }
            return ultimoAgresor;
        }

        private Vector3 PosicionAgresor() => Agresor() != null ? Agresor().transform.position : Jugador.Raiz.position;

        [UnityTest, Explicit("T-1633: 4 eventos de hasta 40 s en la ciudad real; se corre a propósito con -testFilter y el editor con ventana")]
        public IEnumerator Ciudad_SkullsYSnakesMolestanACivilesConElJugadorInvulnerable()
        {
            Assert.IsNotNull(Jugador, "hay jugador en Ciudad_Prologo");
            VidaAPie vida = Jugador.Raiz.GetComponentInChildren<VidaAPie>();
            Assert.IsNotNull(vida, "el jugador trae VidaAPie");
            vida.DarInvulnerabilidad(3600f);
            DirectorEscenasBarrio.Habilitado = false;

            // SKULLS: junto al primer grupo de acoso (Suburbios); el jugador a 28 m (fuera de su vista de 18 m).
            GrupoAcoso[] grupos = Object.FindObjectsByType<GrupoAcoso>(FindObjectsSortMode.None);
            Assert.Greater(grupos.Length, 0, "hay grupos de acoso");
            string[] escenasSkull = { "bully_piso_tianguis", "bully_celular", "bully_abuelo" };
            bool hechoSkulls = false;
            foreach (GrupoAcoso g in grupos)
            {
                Vector3 punto = g.transform.position + new Vector3(DistanciaJugador, 0f, 0f);
                foreach (string id in escenasSkull)
                {
                    Teletransportar(punto);
                    yield return null;
                    string prueba = DirectorEscenasBarrio.ForzarEscena(id);
                    if (!prueba.StartsWith("ok"))
                    {
                        continue;
                    }
                    yield return Correr(id, false, "skulls_sin_intervenir", punto);
                    yield return Correr(id, true, "skulls_con_intervencion", punto);
                    hechoSkulls = true;
                    break;
                }
                if (hechoSkulls)
                {
                    break;
                }
            }
            Assert.IsTrue(hechoSkulls, "se pudo armar un bullying SKULLS (ver registro)");

            // SNAKES: cerca de un pandillero de las Víboras (Neón Alto).
            PandilleroAPie snake = null;
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && p.Pandilla == IdPandilla.Viboras && p.isActiveAndEnabled && !p.EstaKO && !p.EsJefe)
                {
                    snake = p;
                    break;
                }
            }
            if (snake != null)
            {
                Vector3 punto = snake.transform.position + new Vector3(DistanciaJugador, 0f, 0f);
                yield return Correr("bully_snakes_senora", false, "snakes_sin_intervenir", punto);
                yield return Correr("bully_snakes_senora", true, "snakes_con_intervencion", punto);
            }
            else
            {
                Anotar("SNAKES: no hay pandilleros Viboras activos en la escena");
            }
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Carpeta + "/registro.txt", registro.ToString());
            Assert.IsNotNull(snake, "hay SNAKES en la ciudad");
        }
    }
}
