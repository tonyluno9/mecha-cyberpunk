using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1101: en Ciudad_Prologo, TODA escalera trepable (PuntoTrepar de tipo Escalera) lleva al jugador real, con la tecla W simulada,
    /// de 1,5 m delante de ella hasta un techo con piso en menos de 8 s. Las escaleras nuevas (raíz T1101_Escaleras) tienen modelo visible,
    /// se reparten por las zonas de casas, y en 3 casas nuevas el jugador camina 2 s por el techo sin atravesarlo.
    /// </summary>
    public class EscalerasTechosT1101Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RaizNuevas = "T1101_Escaleras";
        private const int NuevasMinimas = 12;
        private const float MaxSegundos = 8f;
        private const float MargenAltura = 0.4f;
        private const float DistanciaPartida = 1.5f;
        private const float DistanciaPartidaBarda = 0.7f;
        private const float CaminataTecho = 2f;
        private const int CasasConCaminata = 3;
        private const float PasoSobreTecho = 0.35f;

        private Keyboard teclado;
        private GameObject jugador;
        private ControladorAPie controlador;
        private TreparAPie trepar;
        private CharacterController cc;

        [UnitySetUp]
        public IEnumerator DescargarCiudadPrevia()
        {
            yield return DescargarCiudades("T1101_VaciaAntes");
        }

        [UnityTearDown]
        public IEnumerator DescargarCiudadAlTerminar()
        {
            yield return DescargarCiudades("T1101_VaciaDespues");
        }

        private static IEnumerator DescargarCiudades(string nombreVacia)
        {
            bool hayCiudad = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                hayCiudad |= SceneManager.GetSceneAt(i).path.Contains("Ciudad");
            }
            if (!hayCiudad)
            {
                yield break;
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene(nombreVacia));
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path.Contains("Ciudad"))
                {
                    yield return SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado != null ? new InputDevice[] { teclado } : new InputDevice[0]);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private IEnumerator Cargar()
        {
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            trepar = Object.FindFirstObjectByType<TreparAPie>();
            Assert.IsNotNull(trepar, "Ciudad_Prologo debe traer al jugador a pie con TreparAPie");
            jugador = trepar.gameObject;
            controlador = jugador.GetComponent<ControladorAPie>();
            cc = jugador.GetComponent<CharacterController>();
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            // Las pandillas de la calle dispararían al jugador mientras sube y lo dejarían KO: la prueba mide las escaleras, no el combate.
            foreach (GrupoAcoso g in Object.FindObjectsByType<GrupoAcoso>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                g.gameObject.SetActive(false);
            }
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo, "El jugador debe poder moverse en la escena");
            int estables = 0;
            int intentos = 0;
            while (estables < 10 && intentos++ < 1000)
            {
                yield return null;
                estables = Time.deltaTime < 0.05f ? estables + 1 : 0;
            }
        }

        private void Poner(Vector3 posicion, Vector3 mirar)
        {
            cc.enabled = false;
            jugador.transform.position = posicion;
            jugador.transform.rotation = Quaternion.LookRotation(new Vector3(mirar.x, 0f, mirar.z));
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static List<PuntoTrepar> Escaleras()
        {
            return Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(p => p.Tipo == TipoTrepar.Escalera)
                .OrderBy(p => p.transform.position.x).ThenBy(p => p.transform.position.z).ToList();
        }

        private static bool EsNueva(PuntoTrepar p)
        {
            return p.transform.parent != null && p.transform.parent.name == RaizNuevas;
        }

        private static string Zona(PuntoTrepar p)
        {
            string n = p.name.StartsWith("Escalera_") ? p.name.Substring("Escalera_".Length) : p.name;
            int i = n.LastIndexOf('_');
            return i > 0 ? n.Substring(0, i) : n;
        }

        private static Vector3 Partida(PuntoTrepar p)
        {
            bool enBarda = p.transform.parent != null && p.transform.parent.name.StartsWith("Ruta_");
            return p.PieAbajo - p.Frente * (enBarda ? DistanciaPartidaBarda : DistanciaPartida) + Vector3.up * 0.05f;
        }

        /// <summary>Sube y devuelve null si llegó, o el motivo del fallo.</summary>
        private IEnumerator Subir(PuntoTrepar p, List<string> fallas, System.Action<float> alTerminar = null)
        {
            Poner(Partida(p), p.Frente);
            // TreparAPie recuerda el punto cercano y un teletransporte no manda OnTriggerExit: se reinicia el componente para que no se
            // enganche a la escalera anterior (en el juego no hay teletransporte junto a una escalera).
            trepar.enabled = false;
            yield return null;
            trepar.enabled = true;
            yield return Esperar(0.5f);
            float yInicio = p.transform.position.y;
            float yMaxima = jugador.transform.position.y;
            Press(teclado.wKey);
            bool trepo = false;
            float inicio = Time.time;
            float fin = inicio + MaxSegundos;
            while (Time.time < fin)
            {
                trepo |= trepar.Trepando;
                yMaxima = Mathf.Max(yMaxima, jugador.transform.position.y);
                if (trepo && !trepar.Trepando && jugador.transform.position.y > yInicio + p.AlturaSubida - MargenAltura)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            float segundos = Time.time - inicio;
            yield return Esperar(0.5f);
            float y = jugador.transform.position.y;
            bool piso = Physics.RaycastAll(jugador.transform.position + Vector3.up * 0.3f, Vector3.down, 0.6f, ~0, QueryTriggerInteraction.Ignore)
                .Any(h => !h.collider.transform.IsChildOf(jugador.transform));
            string motivo = null;
            if (!trepo)
            {
                motivo = "no se engancha (el jugador quedó en " + jugador.transform.position.ToString("F2") + ")";
            }
            else if (segundos > MaxSegundos - 0.01f)
            {
                motivo = "tarda más de " + MaxSegundos + " s (estado " + trepar.Estado + ", pos " + jugador.transform.position.ToString("F2") + ", y máxima " + (yMaxima - yInicio).ToString("F2") + " de " + p.AlturaSubida.ToString("F2") + ")";
            }
            else if (y < yInicio + p.AlturaSubida - MargenAltura)
            {
                motivo = "no llega al techo: sube " + (y - yInicio).ToString("F2") + " de " + p.AlturaSubida.ToString("F2");
            }
            else if (!piso)
            {
                motivo = "llega sin piso bajo los pies (techo sin collider) en " + jugador.transform.position.ToString("F2");
            }
            if (motivo != null)
            {
                fallas.Add(p.name + " (" + (p.transform.parent != null ? p.transform.parent.name : "") + ") en " + p.transform.position.ToString("F1") + ": " + motivo);
            }
            alTerminar?.Invoke(y);
        }

        [UnityTest]
        public IEnumerator Escaleras_Todas_LlevanAUnTechoConPiso()
        {
            yield return Cargar();
            List<PuntoTrepar> lista = Escaleras();
            string solo = System.Environment.GetEnvironmentVariable("T1101_SOLO");
            List<string> fallas = new List<string>();
            foreach (PuntoTrepar p in lista.Where(x => string.IsNullOrEmpty(solo) || x.name.Contains(solo)))
            {
                yield return Subir(p, fallas);
            }
            Assert.IsEmpty(fallas, fallas.Count + " de " + lista.Count + " escaleras fallan:\n" + string.Join("\n", fallas));
            Assert.GreaterOrEqual(lista.Count, 17 + NuevasMinimas, "Escaleras en la escena (17 viejas + nuevas)");
        }

        [UnityTest]
        public IEnumerator EscalerasNuevas_Reparto_TienenModeloYZonas()
        {
            yield return Cargar();
            List<PuntoTrepar> nuevas = Escaleras().Where(EsNueva).ToList();
            Assert.GreaterOrEqual(nuevas.Count, NuevasMinimas, "Escaleras nuevas");
            string[] zonas = nuevas.Select(Zona).Distinct().ToArray();
            foreach (string z in new[] { "S2c_Suburbios", "S2c_CalleDorada", "S2c_Mercado" })
            {
                Assert.IsTrue(zonas.Contains(z), "Debe haber escaleras nuevas en " + z + " (hay: " + string.Join(", ", zonas) + ")");
            }
            foreach (PuntoTrepar p in nuevas)
            {
                MeshRenderer mr = p.GetComponentInChildren<MeshRenderer>();
                Assert.IsNotNull(mr, p.name + ": sin modelo visible");
                Assert.IsTrue(mr.enabled && mr.bounds.size.y >= p.AlturaSubida, p.name + ": el modelo debe cubrir la altura de la subida");
                Assert.IsNotNull(mr.sharedMaterial, p.name + ": sin material");
                // 4 m de cualquier puerta o interactuable: el centro de la escalera no está a menos de ese radio de una puerta.
                foreach (var puerta in MechaCyberpunk.Core.Registro<MechaCyberpunk.Core.IPuerta>.Todos)
                {
                    Vector3 d = puerta.Posicion - p.transform.position;
                    d.y = 0f;
                    Assert.GreaterOrEqual(d.magnitude, 4f, p.name + ": a menos de 4 m de una puerta");
                }
            }
        }

        [UnityTest]
        public IEnumerator EscalerasNuevas_CaminarDosSegundosPorElTecho_NoAtraviesaElPiso()
        {
            yield return Cargar();
            List<PuntoTrepar> nuevas = Escaleras().Where(EsNueva).ToList();
            Assert.GreaterOrEqual(nuevas.Count, CasasConCaminata, "Escaleras nuevas");
            List<string> fallas = new List<string>();
            foreach (PuntoTrepar p in nuevas.Take(CasasConCaminata))
            {
                float yTecho = 0f;
                yield return Subir(p, fallas, y => yTecho = y);
                float minimo = jugador.transform.position.y;
                Vector3 inicio = jugador.transform.position;
                Press(teclado.wKey);
                float fin = Time.time + CaminataTecho;
                while (Time.time < fin)
                {
                    minimo = Mathf.Min(minimo, jugador.transform.position.y);
                    yield return null;
                }
                Release(teclado.wKey);
                if (minimo < yTecho - PasoSobreTecho)
                {
                    fallas.Add(p.name + ": cayó a " + minimo.ToString("F2") + " desde el techo a " + yTecho.ToString("F2"));
                }
                if ((jugador.transform.position - inicio).magnitude < 0.5f)
                {
                    // Quedó contra una pared o pretil: aceptable, pero debe haberse movido o no atravesar nada.
                    Assert.GreaterOrEqual(jugador.transform.position.y, yTecho - PasoSobreTecho, p.name + ": quieto pero hundido");
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
        }

        /// <summary>
        /// T-1203: en TODOS los techos con escalera, después de subir el jugador camina con W, A y D (1,2 s cada una) y luego se queda quieto:
        /// no vuelve a engancharse a la escalera, no lo regresa nada a la orilla (sin saltos de más de 0,4 m en un cuadro) y quieto no se desliza.
        /// </summary>
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Techos_CaminarDespuesDeSubir_NoRegresaNiEmpuja()
        {
            yield return Cargar();
            List<PuntoTrepar> lista = Escaleras();
            string solo = System.Environment.GetEnvironmentVariable("T1101_SOLO");
            List<string> fallas = new List<string>();
            List<string> subida = new List<string>();
            int revisados = 0;
            foreach (PuntoTrepar p in lista.Where(x => string.IsNullOrEmpty(solo) || x.name.Contains(solo)))
            {
                int antes = subida.Count;
                yield return Subir(p, subida);
                if (subida.Count > antes)
                {
                    continue;
                }
                revisados++;
                string nombre = p.name + " (" + (p.transform.parent != null ? p.transform.parent.name : "") + ")";
                float yTecho = jugador.transform.position.y;
                foreach (ButtonControl tecla in new[] { teclado.wKey, teclado.aKey, teclado.dKey })
                {
                    Vector3 previa = jugador.transform.position;
                    Press(tecla);
                    float fin = Time.time + 1.2f;
                    string falla = null;
                    while (Time.time < fin && falla == null)
                    {
                        yield return null;
                        Vector3 ahora = jugador.transform.position;
                        Vector3 salto = ahora - previa;
                        salto.y = 0f;
                        if (trepar.Trepando)
                        {
                            falla = "con " + tecla.name + " se vuelve a enganchar a la escalera en " + ahora.ToString("F2");
                        }
                        else if (salto.magnitude > 0.4f)
                        {
                            falla = "con " + tecla.name + " salta " + salto.magnitude.ToString("F2") + " m en un cuadro, de " + previa.ToString("F2") + " a " + ahora.ToString("F2");
                        }
                        previa = ahora;
                    }
                    Release(tecla);
                    if (falla != null)
                    {
                        fallas.Add(nombre + ": " + falla);
                        break;
                    }
                    yield return Esperar(0.3f);
                    if (jugador.transform.position.y < yTecho - 1f)
                    {
                        break; // se bajó del techo caminando por la orilla: es válido y ya no hay techo que revisar
                    }
                }
                if (jugador.transform.position.y >= yTecho - 1f && controlador.EnPiso)
                {
                    Vector3 quieto = jugador.transform.position;
                    yield return Esperar(1f);
                    float deriva = Vector3.Distance(quieto, jugador.transform.position);
                    if (deriva > 0.1f)
                    {
                        fallas.Add(nombre + ": quieto se desliza " + deriva.ToString("F2") + " m desde " + quieto.ToString("F2"));
                    }
                }
            }
            Debug.Log("[Personaje] T-1203: techos revisados " + revisados + " de " + lista.Count + (subida.Count > 0 ? "; no subió: " + string.Join(" | ", subida) : ""));
            Assert.IsEmpty(fallas, fallas.Count + " de " + revisados + " techos fallan:\n" + string.Join("\n", fallas));
            Assert.IsEmpty(subida, "Escaleras que no subieron:\n" + string.Join("\n", subida));
        }

        // ------------------------------------------------------------------ capturas

        [UnityTest]
        public IEnumerator Capturas_EscalerasNuevasYTechos()
        {
            if (System.Environment.GetEnvironmentVariable("T1101_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1101/ con T1101_CAPTURAS=1");
            }
            yield return Cargar();
            System.IO.Directory.CreateDirectory("docs/capturas/t-1101");
            List<PuntoTrepar> nuevas = Escaleras().Where(EsNueva).ToList();
            string[] zonas = { "S2c_Suburbios", "S2c_CalleDorada", "S2c_Mercado", "S2c_Mercado" };
            int n = 1;
            foreach (string z in zonas)
            {
                PuntoTrepar p = nuevas.Where(x => Zona(x) == z).ElementAtOrDefault(n > 3 ? 1 : 0);
                if (p == null)
                {
                    continue;
                }
                Vector3 desde = p.PieAbajo - p.Frente * 3f + Vector3.up * 1.7f;
                yield return Foto(desde, p.transform.position + Vector3.up * 3.2f, "escalera-" + n++ + "-" + z);
            }
            // Dos vistas desde un callejón hacia la parte trasera de la manzana (a 8 m de la pared, a 1,7 m de altura).
            for (int k = 0; k < 2 && k < nuevas.Count; k++)
            {
                PuntoTrepar p = nuevas[(k * 4 + 1) % nuevas.Count];
                Vector3 desde = p.PieAbajo - p.Frente * 8f + Vector3.up * 1.7f;
                yield return Foto(desde, p.transform.position + Vector3.up * 4f, "callejon-" + (k + 1));
            }
            // Cinco vistas desde techos distintos después de subir de verdad; cada una mira a un lado distinto.
            List<string> fallas = new List<string>();
            for (int k = 0; k < 5 && k < nuevas.Count; k++)
            {
                PuntoTrepar p = nuevas[(k * 3) % nuevas.Count];
                yield return Subir(p, fallas);
                Vector3 ojos = jugador.transform.position + Vector3.up * 1.7f;
                Vector3 dir = Quaternion.Euler(0f, 90f * (k % 4), 0f) * p.Frente;
                // La cámara de la foto queda en la cabeza del jugador: se ve por dentro su propio modelo (esquirlas negras). Se oculta mientras dura la foto.
                Renderer[] propios = jugador.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                foreach (Renderer r in propios)
                {
                    r.enabled = false;
                }
                yield return Foto(ojos, ojos + dir * 6f - Vector3.up * 0.8f, "techo-" + (k + 1));
                foreach (Renderer r in propios)
                {
                    r.enabled = true;
                }
            }
            Assert.IsEmpty(fallas, string.Join(", ", fallas));
        }

        private static IEnumerator Foto(Vector3 desde, Vector3 hacia, string nombre)
        {
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 70f;
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/t-1101/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
