using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1201: al explotar, el carro queda quieto como chatarra (no sigue rodando, no se desliza y el de tráfico no sigue su ruta), y el fuego y el
    /// humo se apagan poco a poco. Con CAPTURAR_T991=1 deja docs/capturas/t1201/.
    /// </summary>
    public class ChatarraCarroT1201Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private static void Matar(DanioCarro danio)
        {
            danio.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, danio.transform.position, Vector3.forward, null));
        }

        private static float Tasa(ParticleSystem sistema) => sistema == null ? 0f : sistema.emission.rateOverTime.constant;

        [UnityTest]
        public IEnumerator CarroEnMarcha_AlExplotar_QuedaQuietoComoChatarra_YElFuegoSeApagaPocoAPoco()
        {
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            GameObject carro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab), new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            try
            {
                GameObject objetoCamara = new GameObject("CamaraPiso");
                Camera camara = objetoCamara.AddComponent<Camera>();
                camara.targetTexture = new RenderTexture(1280, 720, 24);
                objetoCamara.transform.SetParent(piso.transform.parent, true);
                yield return new WaitForSeconds(1.5f);
                Rigidbody cuerpo = carro.GetComponent<Rigidbody>();
                DanioCarro danio = carro.GetComponent<DanioCarro>();
                Assert.IsFalse(cuerpo.isKinematic);
                cuerpo.linearVelocity = carro.transform.forward * 18f;
                yield return new WaitForFixedUpdate();
                Matar(danio);
                Assert.IsTrue(danio.Explotado);
                Vector3 alExplotar = carro.transform.position;
                objetoCamara.transform.position = alExplotar + new Vector3(9f, 4f, 9f);
                objetoCamara.transform.LookAt(alExplotar + Vector3.up);
                yield return Dibujando(camara, 3f);
                Assert.IsTrue(danio.ChatarraFija, "Tras caer queda fijo como chatarra");
                Vector3 asentado = carro.transform.position;
                float avance = Vector3.Distance(new Vector3(alExplotar.x, 0f, alExplotar.z), new Vector3(asentado.x, 0f, asentado.z));
                Assert.Less(avance, 2.5f, "A 18 m/s no sigue avanzando tras el estallido (avanzó " + avance.ToString("0.00") + " m)");
                float fuegoAntes = Tasa(danio.Fuego);
                float humoAntes = Tasa(danio.Humo);
                objetoCamara.transform.position = asentado + new Vector3(9f, 4f, 9f);
                objetoCamara.transform.LookAt(asentado + Vector3.up * 1.5f);
                yield return Dibujando(camara, 3f);
                if (Environment.GetEnvironmentVariable("CAPTURAR_T991") == "1")
                {
                    Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1201"));
                    Guardar(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-ardiendo-6s.png"));
                }
                Assert.Less(Vector3.Distance(asentado, carro.transform.position), 0.05f, "No se desliza");
                Assert.Less(Tasa(danio.Fuego), fuegoAntes, "El fuego baja con el tiempo");
                Assert.Less(Tasa(danio.Humo), humoAntes, "El humo baja con el tiempo");
                Assert.Greater(Tasa(danio.Humo), 0f, "Todavía humea");
                if (Environment.GetEnvironmentVariable("CAPTURAR_T991") == "1")
                {
                    yield return Dibujando(camara, 14f);
                    Guardar(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-ardiendo-20s.png"));
                }
                Object.Destroy(objetoCamara);
            }
            finally
            {
                Object.Destroy(carro);
                Object.Destroy(piso);
            }
        }

        [UnityTest]
        public IEnumerator CarroDeTrafico_AlExplotar_NoSigueSuRuta()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            CarroTrafico elegido = null;
            yield return EsperarHasta(() =>
            {
                foreach (CarroTrafico c in Object.FindObjectsByType<CarroTrafico>(FindObjectsSortMode.None))
                {
                    if (c.isActiveAndEnabled && c.GetComponent<DanioCarro>() != null && c.GetComponent<Rigidbody>().isKinematic)
                    {
                        Vector3 antes = c.transform.position;
                        elegido = c;
                        return true;
                    }
                }
                return false;
            }, "un carro de tráfico", 30f);
            // Que vaya andando.
            Vector3 p0 = elegido.transform.position;
            yield return EsperarHasta(() => Vector3.Distance(p0, elegido.transform.position) > 1f, "que el carro avance", 20f);
            DanioCarro danio = elegido.GetComponent<DanioCarro>();
            // En batchmode la cámara no dibuja sola y las partículas que nadie ve no avanzan: una cámara propia dibuja cada cuadro.
            GameObject objetoCamara = new GameObject("CamaraT1201");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.targetTexture = new RenderTexture(1280, 720, 24);
            Transform t = elegido.transform;
            objetoCamara.transform.position = t.position - t.forward * 8f + t.right * 5f + Vector3.up * 4f;
            objetoCamara.transform.LookAt(t.position + Vector3.up * 0.8f);
            objetoCamara.transform.SetParent(null, true);
            Matar(danio);
            Assert.IsTrue(elegido.Destruido, "El tráfico sabe que explotó");
            Assert.IsFalse(elegido.enabled, "Deja de manejar");
            yield return Dibujando(camara, 2f);
            Vector3 p1 = elegido.transform.position;
            yield return Dibujando(camara, 4f);
            Assert.Less(Vector3.Distance(p1, elegido.transform.position), 0.05f, "La chatarra no se mueve");
            Debug.Log("[Trafico] T-1201: partículas a los 6 s: fuego " + (danio.Fuego != null ? danio.Fuego.particleCount : -1) + ", humo " + (danio.Humo != null ? danio.Humo.particleCount : -1));
            int llamas = 0;
            foreach (ParticleSystem ps in danio.LlamasChatarra)
            {
                llamas += ps.particleCount;
            }
            Assert.Greater(llamas, 0, "Arden las llamas medianas sobre la chatarra");
            Assert.Greater(danio.Fuego.particleCount, 0, "Sigue ardiendo");
            Assert.Greater(danio.Humo.particleCount, 0, "Sigue humeando");

            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") == "1")
            {
                // La foto se toma con la cámara del juego: el jugador se pone a 7 m detrás de la chatarra, de cara a ella.
                IJugador j = Registro<IJugador>.Actual;
                Vector3 frente = elegido.transform.forward;
                frente.y = 0f;
                Vector3 desde = elegido.transform.position - frente.normalized * 7f + elegido.transform.right * 2f;
                desde.y = elegido.transform.position.y + 0.1f;
                j.Colocar(new Pose(desde, Quaternion.LookRotation(elegido.transform.position - desde - Vector3.up * (elegido.transform.position.y - desde.y))));
                camara.targetTexture.Release();
                Object.Destroy(objetoCamara);
                camara = Camera.main;
                camara.useOcclusionCulling = false;
                camara.targetTexture = new RenderTexture(1280, 720, 24);
                yield return Dibujando(camara, 1f);
                Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1201"));
                Guardar(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-trafico-6s.png"));
                yield return Dibujando(camara, 12f);
                Guardar(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-trafico-18s.png"));
                yield return Dibujando(camara, 12f);
                Guardar(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-trafico-30s.png"));
                camara.targetTexture = null;
            }
            if (objetoCamara != null)
            {
                Object.Destroy(objetoCamara);
            }
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator Dibujando(Camera camara, float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                camara.Render();
                yield return null;
            }
        }

        private static void Guardar(Camera camara, string ruta)
        {
            camara.Render();
            RenderTexture rt = camara.targetTexture;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
        }
    }
}
