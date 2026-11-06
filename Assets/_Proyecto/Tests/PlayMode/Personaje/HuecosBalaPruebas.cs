using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-808: los huecos de bala. Un impacto simulado (el gancho de <see cref="HuecosBala"/>, el mismo camino que <see cref="PistolaAPie.BalaImpacto"/>)
    /// en una pared deja un hueco de concreto alineado con la normal y pegado a la superficie; en un carro (cuerpo rígido) uno de metal que se va
    /// con el carro; en una persona ninguno. La pistola del jugador queda escuchada y el cubito MarcaImpacto (Greybox_Marca) ya no se ve.
    /// Con CAPTURAR_S2N_HUECOS=1 guarda docs/capturas/s2n-huecos-bala.png.
    /// </summary>
    public class HuecosBalaPruebas
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMarca = "Assets/_Proyecto/Personaje/Materiales/Greybox_Marca.mat";
        private const string RutaCarro = "Assets/Arte/Vehiculos/Prefabs/V01b_CarroPro.prefab";
        private const string VariableCaptura = "CAPTURAR_S2N_HUECOS";

        private readonly HashSet<GameObject> raices = new HashSet<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            raices.Clear();
            raices.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        [UnityTearDown]
        public IEnumerator Quitar()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raices.Contains(raiz))
                {
                    Object.Destroy(raiz);
                }
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static GameObject Pared()
        {
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.name = "ParedConcreto";
            pared.transform.position = new Vector3(0f, 1.5f, 5f);
            pared.transform.localScale = new Vector3(6f, 3f, 0.3f);
            return pared;
        }

        private static GameObject Carro()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            GameObject carro = prefab != null ? Object.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            carro.name = "CarroDePrueba";
            carro.transform.SetPositionAndRotation(new Vector3(2.2f, 0f, 3.4f), Quaternion.Euler(0f, 90f, 0f));
            foreach (Collider c in carro.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(c);
            }
            // Colisión con la forma del carro (sus mallas), para que los huecos queden sobre la carrocería.
            foreach (MeshFilter mf in carro.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null)
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
            if (carro.GetComponentInChildren<MeshCollider>() == null)
            {
                carro.AddComponent<BoxCollider>();
            }
            Rigidbody cuerpo = carro.GetComponent<Rigidbody>();
            if (cuerpo == null)
            {
                cuerpo = carro.AddComponent<Rigidbody>();
            }
            cuerpo.isKinematic = true;
            return carro;
        }

        private static ImpactoBala Disparo(Vector3 desde, Vector3 hacia, bool enPersona = false)
        {
            Assert.IsTrue(Physics.Raycast(desde, (hacia - desde).normalized, out RaycastHit golpe, 50f), "el disparo pega en algo");
            return new ImpactoBala(golpe.point, golpe.normal, enPersona, false);
        }

        [UnityTest]
        public IEnumerator Impactos_ParedYCarro_HuecoAlineadoDeConcretoYDeMetal()
        {
            Material marca = AssetDatabase.LoadAssetAtPath<Material>(RutaMarca);
            Assert.AreEqual("MechaCyberpunk/Personaje/Invisible", marca.shader.name, "el cubito MarcaImpacto ya no se ve");

            // El de la partida se creó al arrancar; Registros.LimpiarTodos le quitó la suscripción al registro: uno nuevo.
            if (HuecosBala.Actual != null)
            {
                Object.DestroyImmediate(HuecosBala.Actual.gameObject);
            }
            HuecosBala huecos = HuecosBala.Crear();
            Assert.IsNotNull(huecos, "hay ConfigHuecosBala en Resources: se crea solo");
            Assert.AreSame(huecos, HuecosBala.Actual);
            ConfigHuecosBala config = huecos.Config;
            Assert.IsNotNull(config.MaterialConcreto);
            Assert.IsNotNull(config.MaterialMetal);
            Assert.IsNotNull(config.MaterialConcreto.mainTexture, "con la textura del hueco");

            GameObject pared = Pared();
            GameObject carro = Carro();
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 3f;
            GameObject luz = new GameObject("Sol");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
            GameObject jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, -6f), Quaternion.identity);
            yield return null;
            yield return null;
            Physics.SyncTransforms();
            PistolaAPie pistola = jugador.GetComponent<PistolaAPie>();
            Assert.IsNotNull(pistola);
            Assert.IsTrue(huecos.Escucha(pistola), "escucha la pistola del jugador");

            // Pared: concreto, alineado con la normal y separado poquito.
            Vector3 ojo = new Vector3(-1f, 1.6f, -1f);
            ImpactoBala enPared = Disparo(ojo, new Vector3(-1.2f, 1.7f, 5f));
            int antes = huecos.Activos;
            Assert.IsTrue(huecos.SimularImpacto(enPared));
            Transform hueco = huecos.Ultimo;
            Assert.AreEqual(antes + 1, huecos.Activos);
            Assert.IsTrue(hueco.gameObject.activeInHierarchy);
            Assert.IsFalse(huecos.UltimoEsMetal, "la pared es concreto");
            Assert.AreSame(config.MaterialConcreto, hueco.GetComponent<Renderer>().sharedMaterial);
            Assert.Greater(Vector3.Dot(-hueco.forward, enPared.Normal), 0.99f, "el frente del hueco mira hacia afuera de la pared");
            float distancia = Vector3.Dot(hueco.position - enPared.Punto, enPared.Normal);
            Assert.AreEqual(config.Separacion, distancia, 0.001f, "pegado a la pared");
            Assert.AreEqual(config.TamanoConcreto, hueco.lossyScale.x, 0.001f);

            for (int i = 0; i < 5; i++)
            {
                huecos.SimularImpacto(Disparo(ojo, new Vector3(-2.2f + i * 0.35f, 1.2f + (i % 2) * 0.5f, 5f)));
            }

            // Carro: metal, y el hueco se va con el carro.
            ImpactoBala enCarro = Disparo(ojo, carro.transform.position + Vector3.up * 0.7f);
            Assert.IsTrue(huecos.SimularImpacto(enCarro));
            Transform huecoCarro = huecos.Ultimo;
            Assert.IsTrue(huecos.UltimoEsMetal, "el carro es metal");
            Assert.AreSame(config.MaterialMetal, huecoCarro.GetComponent<Renderer>().sharedMaterial);
            Assert.IsTrue(huecoCarro.IsChildOf(carro.transform), "sigue al carro");
            for (int i = 0; i < 3; i++)
            {
                huecos.SimularImpacto(Disparo(ojo, carro.transform.position + new Vector3(-0.4f + i * 0.4f, 0.55f + i * 0.12f, 0f)));
            }

            // En una persona no hay hueco.
            int activos = huecos.Activos;
            Assert.IsFalse(huecos.SimularImpacto(Disparo(ojo, new Vector3(0f, 1.5f, 5f), true)), "en una persona no");
            Assert.AreEqual(activos, huecos.Activos);

            yield return null;
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Capturar(ojo + new Vector3(0.6f, -0.1f, -1.2f), new Vector3(0.6f, 1.2f, 4.2f));
            }

            // Se apagan solos.
            float fin = Time.time + config.SegundosVisible + config.SegundosApagado + 1f;
            Time.timeScale = 20f;
            while (huecos.Activos > 0 && Time.time < fin)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(0, huecos.Activos, "los huecos se apagan con el tiempo");
        }

        private static void Capturar(Vector3 desde, Vector3 hacia)
        {
            const int Ancho = 1920;
            const int Alto = 1080;
            GameObject go = new GameObject("CamaraCaptura");
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            Camera camara = go.AddComponent<Camera>();
            camara.fieldOfView = 50f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.25f, 0.3f, 0.4f);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            Texture2D foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            RenderTexture activa = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            foto.Apply();
            RenderTexture.active = activa;
            camara.targetTexture = null;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/s2n-huecos-bala.png", foto.EncodeToPNG());
            Object.Destroy(foto);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
