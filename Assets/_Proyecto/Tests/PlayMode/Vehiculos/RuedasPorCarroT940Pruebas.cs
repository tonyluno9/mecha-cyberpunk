using System;
using System.Collections;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-940: las llantas RU01 de V01a, V01b, V05 y la patrulla llenan el paso de rueda. Captura lateral por carro
    /// (docs/capturas/ui-ruedas-&lt;carro&gt;.png con CAPTURAR_UI=1) y comprueba que la llanta toca el piso y que RuedasVisualesCarro.Radio coincide.
    /// </summary>
    public class RuedasPorCarroT940Pruebas
    {
        private static readonly string[] Rutas =
        {
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01a.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01b.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab",
        };

        private static readonly string[] Nombres = { "v01a", "v01b", "v05", "patrulla" };

        [UnityTest]
        public IEnumerator Ruedas_LlenanElHuecoYTocanElPiso([ValueSource(nameof(Nombres))] string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Rutas[Array.IndexOf(Nombres, nombre)]);
            Assert.IsNotNull(prefab, nombre);
            GameObject carro = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            foreach (Rigidbody rb in carro.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }
            yield return null;
            RuedasVisualesCarro ruedas = carro.GetComponent<RuedasVisualesCarro>();
            Transform giro = carro.transform.Find("Rueda_DI/Direccion/Giro");
            Bounds llanta = Limites(giro.gameObject);
            Assert.AreEqual(ruedas.Radio, llanta.extents.y, 0.03f, "El radio de la llanta coincide con el de la física");

            if (Environment.GetEnvironmentVariable("CAPTURAR_UI") == "1")
            {
                Bounds todo = Limites(carro);
                GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piso.transform.position = new Vector3(0f, llanta.min.y - 0.5f, 0f);
                piso.transform.localScale = new Vector3(20f, 1f, 20f);
                GameObject luz = new GameObject("LuzFoto");
                luz.AddComponent<Light>().type = LightType.Directional;
                luz.transform.rotation = Quaternion.Euler(30f, -60f, 0f);
                Camera camara = new GameObject("CamFoto").AddComponent<Camera>();
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
                camara.orthographic = true;
                camara.orthographicSize = 1.1f;
                camara.transform.position = new Vector3(8f, todo.center.y, todo.center.z);
                camara.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                CapturaPruebasVehiculos.Tomar(camara, "docs/capturas/ui-ruedas-" + nombre + ".png", 1400, 600);
                UnityEngine.Object.DestroyImmediate(camara.gameObject);
                UnityEngine.Object.DestroyImmediate(luz);
                UnityEngine.Object.DestroyImmediate(piso);
            }
            UnityEngine.Object.DestroyImmediate(carro);
        }

        private static Bounds Limites(GameObject objeto)
        {
            Bounds b = default;
            bool primero = true;
            foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer)
                {
                    continue;
                }
                if (primero)
                {
                    b = r.bounds;
                    primero = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
        }
    }
}
