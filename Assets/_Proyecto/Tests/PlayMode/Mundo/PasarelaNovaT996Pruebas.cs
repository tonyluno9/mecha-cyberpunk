using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-996: en Ciudad_Prologo cada pasarela PS01 se puede subir por sus dos escaleras (entrada libre, peldaños que el jugador a pie sube con W),
    /// se cruza el tablero y se baja por la otra hasta la banqueta, y el barandal no se atraviesa. Con T996_CAPTURAS=1 escribe docs/capturas/t996/.
    /// </summary>
    public class PasarelaNovaT996Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombrePasarela = "PS01_Pasarela";
        private const float TolTablero = 0.3f;
        private const float TolSuelo = 0.4f;
        private const float MaxSegundosSubida = 25f;
        private const float MetrosAntesDeLaEscalera = 1.5f;
        private const float RadioJugador = 0.3f;
        private const float AlturaJugador = 1.8f;
        private const float SegundosEmpuje = 3f;
        private const float MargenBorde = 0.6f;

        private Keyboard teclado;
        private GameObject jugador;
        private ControladorAPie controlador;
        private CharacterController cc;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
        }

        private IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controlador = Object.FindFirstObjectByType<ControladorAPie>();
            Assert.IsNotNull(controlador, "Ciudad_Prologo debe traer al jugador a pie");
            jugador = controlador.gameObject;
            cc = jugador.GetComponent<CharacterController>();
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            controlador.enabled = false;
            controlador.enabled = true;
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo, "El jugador debe poder moverse");
            // Tras empezar, la escena coloca al jugador en su punto de salida: hay que esperarlo antes de teletransportarlo.
            yield return Esperar(1.5f);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            Scene vacia = SceneManager.CreateScene("PasarelaNovaT996_Vacia");
            SceneManager.SetActiveScene(vacia);
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
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private sealed class Pasarela
        {
            public Transform raiz;
            public Bounds caja;
            public float suelo;
            public float yTablero;
            public Vector3 centroTablero;
            public float zPie;
            public float[] xEscaleras;
        }

        private static List<Pasarela> Pasarelas()
        {
            List<Pasarela> lista = new List<Pasarela>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x => x.name == NombrePasarela && x.gameObject.activeInHierarchy).OrderBy(x => x.position.z))
            {
                // Las rampas invisibles del pie de la escalera no cuentan para la caja del modelo.
                Collider[] cols = t.GetComponentsInChildren<Collider>().Where(c => c is MeshCollider).ToArray();
                Assert.Greater(cols.Length, 0, "La pasarela " + t.position + " no tiene colliders");
                Bounds b = cols[0].bounds;
                foreach (Collider c in cols)
                {
                    b.Encapsulate(c.bounds);
                }
                Pasarela p = new Pasarela { raiz = t, caja = b };
                p.suelo = SueloEn(new Vector3(b.min.x + 2f, 0f, b.min.z - MetrosAntesDeLaEscalera), t);
                float zTablero = b.max.z - 2.2f;
                p.yTablero = TableroEn(new Vector3(b.center.x, 0f, zTablero), t);
                p.centroTablero = new Vector3(b.center.x, p.yTablero, zTablero);
                p.zPie = b.min.z - MetrosAntesDeLaEscalera;
                p.xEscaleras = new[] { b.min.x + 2f, b.max.x - 2f };
                lista.Add(p);
            }
            Assert.GreaterOrEqual(lista.Count, 2, "Debe haber las dos pasarelas PS01 en Ciudad_Prologo");
            return lista;
        }

        private static float SueloEn(Vector3 xz, Transform excluir)
        {
            float mejor = float.NaN;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(xz.x, 3f, xz.z), Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(excluir) || h.collider.attachedRigidbody != null)
                {
                    continue;
                }
                mejor = float.IsNaN(mejor) ? h.point.y : Mathf.Max(mejor, h.point.y);
            }
            Assert.IsFalse(float.IsNaN(mejor), "Sin suelo en " + xz);
            return mejor;
        }

        private static float TableroEn(Vector3 xz, Transform pasarela)
        {
            float mejor = float.NaN;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(xz.x, 20f, xz.z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(pasarela))
                {
                    mejor = float.IsNaN(mejor) ? h.point.y : Mathf.Max(mejor, h.point.y);
                }
            }
            Assert.IsFalse(float.IsNaN(mejor), "Sin tablero en " + xz);
            return mejor;
        }

        private void Poner(Vector3 posicion, Vector3 mirar)
        {
            cc.enabled = false;
            jugador.transform.position = posicion;
            jugador.transform.rotation = Quaternion.LookRotation(new Vector3(mirar.x, 0f, mirar.z));
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        /// <summary>Gira al jugador en su sitio (el controlador manda sobre la rotación del transform, así que se reinicia su giro).</summary>
        private void Girar(Vector3 mirar)
        {
            Poner(Pos, mirar);
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        /// <summary>Camina con W hasta que se cumpla la condición o se acabe el tiempo; informa si se cumplió.</summary>
        private IEnumerator CaminarHasta(System.Func<bool> listo, float segundos, System.Action<bool> resultado)
        {
            Press(teclado.wKey);
            bool ok = false;
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
                if (listo())
                {
                    ok = true;
                    break;
                }
            }
            Release(teclado.wKey);
            yield return null;
            resultado(ok);
        }

        private Vector3 Pos => jugador.transform.position;

        // ------------------------------------------------------------------ subir, cruzar y bajar

        [UnityTest]
        public IEnumerator Escaleras_SeSubenSeCruzaElTableroYSeBajaPorLaOtra()
        {
            yield return Cargar();
            List<string> fallas = new List<string>();
            foreach (Pasarela p in Pasarelas())
            {
                for (int lado = 0; lado < 2; lado++)
                {
                    float xSube = p.xEscaleras[lado];
                    float xBaja = p.xEscaleras[1 - lado];
                    string id = p.raiz.position.ToString("F1") + (lado == 0 ? " oeste" : " este");
                    Poner(new Vector3(xSube, p.suelo + 0.05f, p.zPie), Vector3.forward);
                    yield return Esperar(0.5f);
                    bool arriba = false;
                    yield return CaminarHasta(() => Mathf.Abs(Pos.y - p.yTablero) < TolTablero, MaxSegundosSubida, r => arriba = r);
                    if (!arriba)
                    {
                        fallas.Add(id + ": no sube por la escalera (se quedó en " + Pos.ToString("F2") + ", tablero y " + p.yTablero.ToString("F2") + ")");
                        continue;
                    }
                    // Del último peldaño sigue de frente hasta el centro del tablero (el barandal de la escalera no deja girar antes).
                    bool alCentro = false;
                    yield return CaminarHasta(() => Pos.z > p.centroTablero.z - 0.5f, 10f, r => alCentro = r);
                    if (!alCentro)
                    {
                        fallas.Add(id + ": no pasa del último peldaño al tablero (" + Pos.ToString("F2") + ")");
                        continue;
                    }
                    Girar(new Vector3(Mathf.Sign(xBaja - xSube), 0f, 0f));
                    bool cruzo = false;
                    yield return CaminarHasta(() => Mathf.Abs(Pos.x - xBaja) < 1f, 15f, r => cruzo = r);
                    if (!cruzo || Mathf.Abs(Pos.y - p.yTablero) > TolTablero)
                    {
                        fallas.Add(id + ": no cruza el tablero (" + Pos.ToString("F2") + ")");
                        continue;
                    }
                    Girar(Vector3.back);
                    bool abajo = false;
                    yield return CaminarHasta(() => Pos.y < p.suelo + TolSuelo && Pos.z < p.caja.min.z, MaxSegundosSubida, r => abajo = r);
                    if (!abajo)
                    {
                        fallas.Add(id + ": no baja por la otra escalera (" + Pos.ToString("F2") + ")");
                    }
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
        }

        // ------------------------------------------------------------------ barandal

        [UnityTest]
        public IEnumerator Tablero_EmpujarDeLadoNoSacaAlJugador()
        {
            yield return Cargar();
            List<string> fallas = new List<string>();
            foreach (Pasarela p in Pasarelas())
            {
                Vector3[] direcciones = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
                foreach (Vector3 d in direcciones)
                {
                    Poner(p.centroTablero + Vector3.up * 0.05f, d);
                    yield return Esperar(0.3f);
                    Press(teclado.wKey);
                    yield return Esperar(SegundosEmpuje);
                    Release(teclado.wKey);
                    yield return null;
                    bool dentro = Pos.x > p.caja.min.x + MargenBorde && Pos.x < p.caja.max.x - MargenBorde
                        && Pos.z > p.caja.min.z && Pos.z < p.caja.max.z + MargenBorde && Mathf.Abs(Pos.y - p.yTablero) < TolTablero;
                    if (!dentro)
                    {
                        fallas.Add(p.raiz.position.ToString("F1") + ": empujando hacia " + d + " el jugador salió a " + Pos.ToString("F2"));
                    }
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
        }

        // ------------------------------------------------------------------ entrada libre

        [UnityTest]
        public IEnumerator Escaleras_ElPieDeCadaUnaEstaLibre()
        {
            yield return Cargar();
            List<string> fallas = new List<string>();
            foreach (Pasarela p in Pasarelas())
            {
                string clave = p.raiz.position.ToString("F1");
                foreach (float x in p.xEscaleras)
                {
                    for (float z = p.caja.min.z - MetrosAntesDeLaEscalera; z <= p.caja.min.z + 0.5f; z += 0.5f)
                    {
                        foreach (float dx in new[] { -1f, 0f, 1f })
                        {
                            Vector3 abajo = new Vector3(x + dx, p.suelo + 0.4f, z);
                            Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 0.8f);
                            foreach (Collider c in Physics.OverlapCapsule(abajo, arriba, RadioJugador, ~0, QueryTriggerInteraction.Ignore))
                            {
                                if (c.transform.IsChildOf(p.raiz) || c.transform.root == jugador.transform.root || c.GetComponentInParent<CharacterController>() != null)
                                {
                                    continue;
                                }
                                string nombre = c.name + " (" + c.transform.root.name + ")";
                                string prefijo = clave + " x " + x.ToString("F1") + ": " + nombre;
                                if (!fallas.Any(y => y.StartsWith(prefijo)))
                                {
                                    fallas.Add(prefijo + " tapa la entrada en " + abajo.ToString("F1"));
                                }
                            }
                        }
                    }
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
        }

        // ------------------------------------------------------------------ capturas

        [UnityTest]
        public IEnumerator Capturas_DeCadaPasarela()
        {
            if (System.Environment.GetEnvironmentVariable("T996_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t996/ con T996_CAPTURAS=1");
            }
            yield return Cargar();
            System.IO.Directory.CreateDirectory("docs/capturas/t996");
            int n = 1;
            foreach (Pasarela p in Pasarelas())
            {
                Vector3 centro = p.caja.center;
                yield return Foto(new Vector3(centro.x - 6f, p.suelo + 4f, p.caja.min.z - 20f), new Vector3(centro.x, p.suelo + 3.5f, centro.z), "pasarela-" + n++);
                yield return Foto(new Vector3(p.caja.min.x + 2f, p.yTablero + 1.7f, p.centroTablero.z - 1.5f), new Vector3(p.caja.max.x - 4f, p.yTablero + 1f, p.centroTablero.z), "pasarela-" + n++);
                yield return Foto(new Vector3(p.xEscaleras[0] - 0.5f, p.suelo + 1.7f, p.zPie - 2f), new Vector3(p.xEscaleras[0], p.suelo + 2.5f, p.caja.min.z + 2f), "pasarela-pie-" + (n / 2));
            }
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
            System.IO.File.WriteAllBytes("docs/capturas/t996/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
