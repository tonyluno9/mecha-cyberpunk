using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1202: la patrulla sale con sus policías sentados dentro (figuras sin IA ni colliders, con la cabeza a la altura de la ventana) y al llegar
    /// se bajan con el clip de bajar del carro del jugador: no aparece ningún policía de verdad hasta que la figura toca el piso junto a su puerta.
    /// Con CAPTURAR_T991=1 deja docs/capturas/t1202/.
    /// </summary>
    public class PoliciasSentadosT1202Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private ConfigPersecucion config;
        private GameObject jugador;
        private Camera camara;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            extras.Add(piso);
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = new Vector3(0f, 0f, 40f);
            extras.Add(jugador);
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugador.transform });
            GameObject luz = new GameObject("Sol");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            extras.Add(luz);
            GameObject objetoCamara = new GameObject("CamaraT1202");
            camara = objetoCamara.AddComponent<Camera>();
            camara.targetTexture = new RenderTexture(1280, 720, 24);
            extras.Add(objetoCamara);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            foreach (GameObject g in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (g != null && g.name.StartsWith("Policia_"))
                {
                    Object.DestroyImmediate(g);
                }
            }
            Registros.LimpiarTodos();
        }

        private PatrullaMundo CrearPatrulla()
        {
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>(),
                new Vector3(0f, 0.6f, 0f), Quaternion.identity);
            extras.Add(patrulla.gameObject);
            GrafoCalles grafo = GrafoCalles.DeManzanas(new[] { new Vector2(-47f, -47f), new Vector2(47f, -47f), new Vector2(-47f, 47f), new Vector2(47f, 47f) }, 80f, 0.2f);
            Vector3 p = jugador.transform.position;
            patrulla.Preparar(config.Crear(), config, grafo, 0, () => p);
            patrulla.GetComponent<Rigidbody>().isKinematic = true; // quieta: la prueba mira a los policías, no la persecución
            return patrulla;
        }

        private static int PoliciasDeVerdad()
        {
            int n = 0;
            foreach (GameObject g in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                n += g.name.StartsWith("Policia_") ? 1 : 0;
            }
            return n;
        }

        private IEnumerator Dibujando(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                camara.Render();
                yield return null;
            }
        }

        private void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") != "1")
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1202"));
            camara.Render();
            RenderTexture rt = camara.targetTexture;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine("docs", "capturas", "t1202", nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
        }

        private void Mirar(Transform carro, Vector3 desdeLocal)
        {
            camara.transform.position = carro.TransformPoint(desdeLocal);
            camara.transform.LookAt(carro.position + Vector3.up * 1.1f);
        }

        [UnityTest]
        public IEnumerator Patrulla_SaleConSusPoliciasSentadosDentro_VisiblesPorLaVentana()
        {
            PatrullaMundo patrulla = CrearPatrulla();
            yield return null;
            yield return null;
            Assert.AreEqual(config.PoliciasPorPatrulla, patrulla.PoliciasDentro, "Van todos sentados dentro");
            Assert.AreEqual(0, PoliciasDeVerdad(), "Ningún policía de verdad todavía");
            Bounds carroceria = new Bounds(patrulla.transform.position, Vector3.zero);
            foreach (Collider c in patrulla.GetComponentsInChildren<Collider>())
            {
                if (!c.isTrigger)
                {
                    carroceria.Encapsulate(c.bounds);
                }
            }
            foreach (OcupantePatrulla o in patrulla.Ocupantes)
            {
                Assert.IsEmpty(o.GetComponentsInChildren<Collider>(), "La figura no choca con nada");
                foreach (MonoBehaviour m in o.GetComponentsInChildren<MonoBehaviour>())
                {
                    Assert.IsInstanceOf<OcupantePatrulla>(m, "La figura no lleva la IA del policía");
                }
                Animator a = o.GetComponentInChildren<Animator>();
                Transform cabeza = a.GetBoneTransform(HumanBodyBones.Head);
                Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
                Vector3 c = patrulla.transform.InverseTransformPoint(cadera.position);
                Assert.IsTrue(carroceria.Contains(cadera.position), "La cadera está dentro del carro: " + c.ToString("F2"));
                Assert.Less(cabeza.position.y, carroceria.max.y, "La cabeza no atraviesa el techo");
                Assert.Greater(cabeza.position.y, cadera.position.y + 0.35f, "Va sentado derecho (cabeza sobre la cadera)");
                Assert.Less(Mathf.Abs(c.x), 0.9f, "Sentado dentro, no afuera de la puerta");
            }
            Mirar(patrulla.transform, new Vector3(-4.5f, 1.6f, 0.8f));
            yield return Dibujando(0.5f);
            Capturar("sentados-lado-conductor");
            Mirar(patrulla.transform, new Vector3(2.5f, 2.2f, 5f));
            yield return Dibujando(0.3f);
            Capturar("sentados-frente");
        }

        [UnityTest]
        public IEnumerator AlLlegar_SeBajanConElClipDeBajarDelCarro_YSoloEntoncesHayPolicias()
        {
            PatrullaMundo patrulla = CrearPatrulla();
            yield return null;
            yield return null;
            Assert.AreEqual(config.PoliciasPorPatrulla, patrulla.PoliciasDentro);
            Mirar(patrulla.transform, new Vector3(-6f, 2f, 3f));
            patrulla.BajarPolicias();
            Assert.IsTrue(patrulla.PoliciasBajaron);
            Assert.AreEqual(0, patrulla.Policias.Count, "Al abrir la puerta todavía no hay policía de verdad: se está bajando");
            foreach (OcupantePatrulla o in patrulla.Ocupantes)
            {
                Assert.IsTrue(o.Bajando);
            }
            yield return Dibujando(0.8f);
            Capturar("bajandose");
            float fin = Time.time + 6f;
            while (patrulla.PoliciasDentro > 0 && Time.time < fin)
            {
                camara.Render();
                yield return null;
            }
            Assert.AreEqual(0, patrulla.PoliciasDentro, "Ya se bajaron todos");
            Assert.AreEqual(config.PoliciasPorPatrulla, patrulla.Policias.Count, "Cada figura se volvió un policía de verdad");
            foreach (GameObject policia in patrulla.Policias)
            {
                Vector3 local = patrulla.transform.InverseTransformPoint(policia.transform.position);
                Assert.Greater(Mathf.Abs(local.x), 1.2f, "Quedó afuera, junto a su puerta");
                Assert.Less(Mathf.Abs(policia.transform.position.y), 0.3f, "En el piso");
            }
            yield return Dibujando(0.3f);
            Capturar("abajo");
        }
    }
}
