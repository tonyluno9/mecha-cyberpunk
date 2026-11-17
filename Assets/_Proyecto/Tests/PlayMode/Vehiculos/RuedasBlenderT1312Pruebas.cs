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
    /// T-1312: V02, V03, V04, V06, V09, V10 y la moto C19 con las ruedas de Blender: un vehículo que avanza hace girar sus ruedas sobre el eje X y las
    /// ruedas tocan el piso. Con CAPTURAR_T1312=1 deja una captura de lado y una de 3/4 de cada uno en docs/capturas/t-1312/.
    /// </summary>
    public class RuedasBlenderT1312Pruebas
    {
        private const float PasoMetros = 0.15f;
        private const int Cuadros = 20;
        private const float MinimoGiroGrados = 10f;
        private const float TocaPisoTolerancia = 0.03f;
        private const int Ancho = 1400;
        private const int Alto = 600;
        private const int AnchoTresCuartos = 1280;
        private const int AltoTresCuartos = 720;
        private const float TamanoOrtografico = 1.9f;

        private static readonly string[] Codigos = { "V02", "V03", "V04", "V06", "V09", "V10", "C19" };

        private static string[] RutasGiro(string codigo)
        {
            return codigo == "C19"
                ? new[] { "Visual_T511/Ruedas_T743/Rueda_Del/Direccion/Giro", "Visual_T511/Ruedas_T743/Rueda_Tras/Direccion/Giro" }
                : new[] { "Rueda_DI/Direccion/Giro", "Rueda_DD/Direccion/Giro", "Rueda_TI/Direccion/Giro", "Rueda_TD/Direccion/Giro" };
        }

        [UnityTest]
        public IEnumerator Avanzar_HaceGirarLasRuedas_YTocanElPiso([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, codigo);
            GameObject carro = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            foreach (Rigidbody rb in carro.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }
            yield return null;
            RuedasVisualesCarro ruedas = carro.GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, codigo);
            float antes = ruedas.RodadoGrados;
            for (int i = 0; i < Cuadros; i++)
            {
                carro.transform.position += carro.transform.forward * PasoMetros; // paso fijo: en batch los cuadros duran casi 0 s
                yield return null;
            }
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(antes, ruedas.RodadoGrados)), MinimoGiroGrados, codigo + ": el componente rueda");
            foreach (string ruta in RutasGiro(codigo))
            {
                Transform giro = carro.transform.Find(ruta);
                Assert.IsNotNull(giro, codigo + " " + ruta);
                Assert.Greater(Quaternion.Angle(Quaternion.identity, giro.localRotation), MinimoGiroGrados, codigo + " " + ruta + ": la rueda giró");
                Quaternion q = giro.localRotation; // gira solo sobre X: sin componentes Y ni Z en el cuaternión (los Euler pasan a Y=180 tras 90°)
                Assert.AreEqual(0f, Mathf.Abs(q.y) + Mathf.Abs(q.z), 0.01f, codigo + " " + ruta + ": gira solo sobre X");
            }
            carro.transform.position = Vector3.zero;
            yield return null;
            foreach (string ruta in RutasGiro(codigo))
            {
                Bounds b = Limites(carro.transform.Find(ruta).gameObject);
                Assert.AreEqual(0f, b.min.y, ruta.StartsWith("Visual") ? 0.05f : TocaPisoTolerancia + 0.1f, codigo + " " + ruta + ": toca el piso");
            }
            if (Environment.GetEnvironmentVariable("CAPTURAR_T1312") == "1")
            {
                yield return Capturar(carro, codigo);
            }
            UnityEngine.Object.DestroyImmediate(carro);
        }

        private static IEnumerator Capturar(GameObject carro, string codigo)
        {
            foreach (RuedasVisualesCarro r in carro.GetComponents<RuedasVisualesCarro>())
            {
                r.enabled = false;
            }
            foreach (string ruta in RutasGiro(codigo))
            {
                carro.transform.Find(ruta).localRotation = Quaternion.identity;
            }
            Bounds todo = Limites(carro);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(30f, 1f, 30f);
            GameObject luz = new GameObject("LuzFoto");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(35f, -50f, 0f);
            Camera camara = new GameObject("CamFoto").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            camara.orthographic = true;
            camara.orthographicSize = TamanoOrtografico;
            camara.transform.position = new Vector3(12f, todo.center.y, todo.center.z);
            camara.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            CapturaPruebasVehiculos.Tomar(camara, "docs/capturas/t-1312/" + codigo + "-lado.png", Ancho, Alto);
            camara.orthographic = false;
            camara.fieldOfView = 40f;
            camara.transform.position = todo.center + new Vector3(-7f, 2.8f, 8.5f);
            camara.transform.LookAt(todo.center);
            CapturaPruebasVehiculos.Tomar(camara, "docs/capturas/t-1312/" + codigo + "-3-4.png", AnchoTresCuartos, AltoTresCuartos);
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
                if (r is ParticleSystemRenderer || !r.enabled)
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
