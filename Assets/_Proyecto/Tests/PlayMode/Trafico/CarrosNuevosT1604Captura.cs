using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1604: foto de Carro_V01a, Carro_V01b y Carro_V05 lado a lado con una puerta abierta. Explicit: solo corre si se pide por nombre.</summary>
    public class CarrosNuevosT1604Captura
    {
        [UnityTest, Explicit("Genera docs/capturas/_tareas/T6/carros-nuevos.png")]
        public IEnumerator TresCarros_LadoALado()
        {
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(40f, 1f, 40f);
            GameObject luz = new GameObject("Luz");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            string[] codigos = { "V01a", "V01b", "V05" };
            for (int i = 0; i < codigos.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigos[i] + ".prefab");
                Assert.IsNotNull(prefab, codigos[i]);
                GameObject carro = Object.Instantiate(prefab, new Vector3((i - 1) * 4.5f, 0.2f, 0f), Quaternion.identity);
                foreach (Rigidbody rb in carro.GetComponentsInChildren<Rigidbody>())
                {
                    rb.isKinematic = true;
                }
                if (i == 1)
                {
                    foreach (Transform t in carro.GetComponentsInChildren<Transform>())
                    {
                        if (t.name == "Bisagra_Puerta_I")
                        {
                            t.localRotation = Quaternion.Euler(0f, -65f, 0f) * t.localRotation;
                        }
                    }
                }
            }
            GameObject camObjeto = new GameObject("Cam");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            camObjeto.transform.position = new Vector3(-9f, 2.6f, 8f);
            camObjeto.transform.LookAt(new Vector3(0f, 0.8f, 0f));
            yield return null;
            RenderTexture rt = new RenderTexture(1600, 800, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D img = new Texture2D(1600, 800, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0f, 0f, 1600f, 800f), 0, 0);
            img.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory("docs/capturas/_tareas/T6");
            File.WriteAllBytes("docs/capturas/_tareas/T6/carros-nuevos.png", img.EncodeToPNG());
            Object.Destroy(img);
            Object.Destroy(rt);
        }
    }
}
