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
                yield return new WaitForSeconds(1.5f);
                Rigidbody cuerpo = carro.GetComponent<Rigidbody>();
                DanioCarro danio = carro.GetComponent<DanioCarro>();
                Assert.IsFalse(cuerpo.isKinematic);
                cuerpo.linearVelocity = carro.transform.forward * 18f;
                yield return new WaitForFixedUpdate();
                Matar(danio);
                Assert.IsTrue(danio.Explotado);
                Vector3 alExplotar = carro.transform.position;
                yield return new WaitForSeconds(3f);
                Assert.IsTrue(danio.ChatarraFija, "Tras caer queda fijo como chatarra");
                Vector3 asentado = carro.transform.position;
                float avance = Vector3.Distance(new Vector3(alExplotar.x, 0f, alExplotar.z), new Vector3(asentado.x, 0f, asentado.z));
                Assert.Less(avance, 2.5f, "A 18 m/s no sigue avanzando tras el estallido (avanzó " + avance.ToString("0.00") + " m)");
                float fuegoAntes = Tasa(danio.Fuego);
                float humoAntes = Tasa(danio.Humo);
                yield return new WaitForSeconds(3f);
                Assert.Less(Vector3.Distance(asentado, carro.transform.position), 0.05f, "No se desliza");
                Assert.Less(Tasa(danio.Fuego), fuegoAntes, "El fuego baja con el tiempo");
                Assert.Less(Tasa(danio.Humo), humoAntes, "El humo baja con el tiempo");
                Assert.Greater(Tasa(danio.Humo), 0f, "Todavía humea");
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
            Matar(danio);
            Assert.IsTrue(elegido.Destruido, "El tráfico sabe que explotó");
            Assert.IsFalse(elegido.enabled, "Deja de manejar");
            yield return new WaitForSeconds(2f);
            Vector3 p1 = elegido.transform.position;
            yield return new WaitForSeconds(4f);
            Assert.Less(Vector3.Distance(p1, elegido.transform.position), 0.05f, "La chatarra no se mueve");

            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") == "1")
            {
                GameObject objetoCamara = new GameObject("CamaraT1201");
                Camera camara = objetoCamara.AddComponent<Camera>();
                Transform t = elegido.transform;
                objetoCamara.transform.position = t.position - t.forward * 7f + t.right * 4f + Vector3.up * 3.5f;
                objetoCamara.transform.LookAt(t.position + Vector3.up * 0.8f);
                yield return null;
                Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1201"));
                CapturaUI(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-trafico.png"));
                yield return new WaitForSeconds(12f);
                CapturaUI(camara, Path.Combine("docs", "capturas", "t1201", "chatarra-trafico-18s.png"));
                Object.Destroy(objetoCamara);
            }
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static void CapturaUI(Camera camara, string ruta)
        {
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            camara.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}
