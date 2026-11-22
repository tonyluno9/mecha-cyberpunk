using System.Collections;
using System.IO;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T24 / T-1644: foto de noche de carros civiles con el modelo de Tripo (V01a, V02, V03, V09, V12, V18, V20 y la moto V08) junto a la patrulla buena PA01, con faros y calaveras
    /// prendidos (L) y una cápsula de 1.8 m de referencia. Hojas de 3 x 3 (un cuadro por carro): de noche de frente y de atrás, y de día. Explicit: solo corre si se pide por nombre.
    /// </summary>
    public class CarrosTripoT1644Captura
    {
        private const string Carpeta = "docs/capturas/_tareas/T24";
        private static readonly string[] Codigos = { "PA01", "V01a", "V02", "V03", "V09", "V12", "V20", "V18", "V08" };
        private const float Separacion = 25f;
        private const float AlturaPersona = 1.8f;

        [UnityTest, Explicit("Genera docs/capturas/_tareas/T24/carros-noche-frente.png, carros-noche-atras.png y carros-dia.png")]
        public IEnumerator Fila_DeNoche_ConFarosYCalaveras()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.2f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 120f);
            GameObject luna = new GameObject("Luna");
            Light luzLuna = luna.AddComponent<Light>();
            luzLuna.type = LightType.Directional;
            luzLuna.intensity = 0.25f;
            luzLuna.color = new Color(0.6f, 0.7f, 1f);
            luna.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            for (int i = 0; i < Codigos.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + Codigos[i] + ".prefab");
                Assert.IsNotNull(prefab, Codigos[i]);
                float x = (i - (Codigos.Length - 1) * 0.5f) * Separacion;
                GameObject carro = Object.Instantiate(prefab, new Vector3(x, 0f, 0f), Quaternion.identity);
                GameObject referencia = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                referencia.transform.localScale = new Vector3(0.5f, AlturaPersona * 0.5f, 0.5f);
                referencia.transform.position = new Vector3(x + 3.5f, AlturaPersona * 0.5f, 0f);
                foreach (Rigidbody rb in carro.GetComponentsInChildren<Rigidbody>())
                {
                    rb.isKinematic = true;
                }
            }
            yield return null;
            foreach (LucesCarro luces in Object.FindObjectsByType<LucesCarro>(FindObjectsSortMode.None))
            {
                luces.Alternar();
            }
            yield return new WaitForSeconds(1.5f);

            GameObject camObjeto = new GameObject("Cam");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            cam.fieldOfView = 50f;
            Directory.CreateDirectory(Carpeta);
            Hoja(cam, new Vector3(-4.5f, 1.6f, 6.5f), Carpeta + "/carros-noche-frente.png");
            Hoja(cam, new Vector3(4.5f, 1.6f, -6.5f), Carpeta + "/carros-noche-atras.png");

            // De día, casi a ras del piso y de lado-frente: se ve si alguno flota, se hunde o está de reversa.
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            luzLuna.intensity = 1.2f;
            luzLuna.color = Color.white;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            Hoja(cam, new Vector3(-6f, 1.2f, 5f), Carpeta + "/carros-dia.png");
            yield return null;
        }

        // Una hoja de 3 x 3: cada carro visto desde su posición + desplazamiento (el V18 de más lejos).
        private static void Hoja(Camera cam, Vector3 desplazamiento, string ruta)
        {
            const int Ancho = 640;
            const int Alto = 400;
            Texture2D hoja = new Texture2D(Ancho * 3, Alto * 3, TextureFormat.RGB24, false);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            for (int i = 0; i < Codigos.Length; i++)
            {
                Vector3 centro = new Vector3((i - (Codigos.Length - 1) * 0.5f) * Separacion, 0.9f, 0f);
                float lejos = Codigos[i] == "V18" ? 1.9f : Codigos[i] == "V08" ? 0.6f : 1f;
                cam.transform.position = centro + desplazamiento * lejos;
                cam.transform.LookAt(centro);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0f, 0f, Ancho, Alto), (i % 3) * Ancho, (2 - i / 3) * Alto);
            }
            hoja.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(ruta, hoja.EncodeToPNG());
            Object.Destroy(hoja);
            Object.Destroy(rt);
        }
    }
}
