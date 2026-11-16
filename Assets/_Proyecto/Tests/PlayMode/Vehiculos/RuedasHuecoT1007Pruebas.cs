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
    /// T-1007: con el carro en el mundo, las cuatro llantas miden lo mismo que el radio de RuedasVisualesCarro y su parte baja toca el piso (y = 0).
    /// Captura de lado de cada carro y moto en docs/capturas/t1007/ (con CAPTURAR_S1=1).
    /// </summary>
    public class RuedasHuecoT1007Pruebas
    {
        private const float Tolerancia = 0.015f;
        private const float AlturaCamara = 1.1f;
        private const float DistanciaCamara = 8f;
        private const float TamanoOrtografico = 1.1f;
        private const int Ancho = 1400;
        private const int Alto = 600;

        private static readonly string[] Rutas =
        {
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01a.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01b.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V01a.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V01b.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V05.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V07.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_C19.prefab",
        };

        [UnityTest]
        public IEnumerator Llantas_TocanElPiso_YMidenElRadioDeLasRuedas([ValueSource(nameof(Rutas))] string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            GameObject carro = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            foreach (Rigidbody rb in carro.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }
            yield return null;
            RuedasVisualesCarro ruedas = carro.GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, ruta);
            bool esMoto = ruta.Contains("V07") || ruta.Contains("V08") || ruta.Contains("C19");
            Transform[] llantas = esMoto
                ? new[] { carro.transform.Find("Visual_T511/Ruedas_T743/Rueda_Del"), carro.transform.Find("Visual_T511/Ruedas_T743/Rueda_Tras") }
                : new[] { carro.transform.Find("Rueda_DI/Direccion/Giro"), carro.transform.Find("Rueda_DD/Direccion/Giro"), carro.transform.Find("Rueda_TI/Direccion/Giro"), carro.transform.Find("Rueda_TD/Direccion/Giro") };
            foreach (Transform llanta in llantas)
            {
                Assert.IsNotNull(llanta, ruta);
                Bounds b = Limites(llanta.gameObject);
                Assert.AreEqual(0f, b.min.y, esMoto ? 0.05f : Tolerancia + 0.005f, ruta + " " + llanta.name + ": la llanta toca el piso");
                if (!esMoto)
                {
                    Assert.AreEqual(ruedas.Radio, b.extents.y, Tolerancia, ruta + " " + llanta.name + ": radio");
                }
            }
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return Capturar(carro, System.IO.Path.GetFileNameWithoutExtension(ruta));
            }
            UnityEngine.Object.DestroyImmediate(carro);
        }

        private static IEnumerator Capturar(GameObject carro, string nombre)
        {
            Bounds todo = Limites(carro);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(20f, 1f, 20f);
            GameObject luz = new GameObject("LuzFoto");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(30f, -60f, 0f);
            Camera camara = new GameObject("CamFoto").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            camara.orthographic = true;
            camara.orthographicSize = TamanoOrtografico;
            camara.transform.position = new Vector3(DistanciaCamara, Mathf.Max(AlturaCamara * 0.5f, todo.center.y), todo.center.z);
            camara.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            CapturaPruebasVehiculos.Tomar(camara, "docs/capturas/t1007/" + nombre + ".png", Ancho, Alto);
            UnityEngine.Object.DestroyImmediate(camara.gameObject);
            UnityEngine.Object.DestroyImmediate(luz);
            UnityEngine.Object.DestroyImmediate(piso);
            yield return null;
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
                if (primero) { b = r.bounds; primero = false; }
                else { b.Encapsulate(r.bounds); }
            }
            return b;
        }
    }
}
