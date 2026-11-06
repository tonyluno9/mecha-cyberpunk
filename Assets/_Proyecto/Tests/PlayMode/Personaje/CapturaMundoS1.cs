using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Capturas del mundo para la sesión S1 (docs/capturas/s1-*.png): cámara suelta mirando a un punto. Solo se guardan con CAPTURAR_S1=1.</summary>
    public static class CapturaMundoS1
    {
        public static bool Activa => CapturaS1.Activa;

        /// <summary>Foto desde <paramref name="camara"/> mirando a <paramref name="mira"/>. <paramref name="noche"/> oscurece el fondo y baja el sol.</summary>
        public static void Foto(string nombre, Vector3 camara, Vector3 mira, float fov = 40f, bool noche = false, int ancho = 1000, int alto = 700)
        {
            if (!Activa) return;
            GameObject go = new GameObject("CamS1");
            Camera c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = noche ? new Color(0.02f, 0.03f, 0.06f) : new Color(0.45f, 0.55f, 0.7f);
            c.fieldOfView = fov;
            c.nearClipPlane = 0.05f;
            go.transform.position = camara;
            go.transform.rotation = Quaternion.LookRotation(mira - camara, Vector3.up);
            GameObject luz = new GameObject("LuzS1");
            Light sol = luz.AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = noche ? 0.05f : 1.1f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderTexture previa = RenderTexture.active;
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            RenderTexture.active = previa;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            c.targetTexture = null;
            Object.Destroy(go);
            Object.Destroy(luz);
            Object.Destroy(rt);
            Object.Destroy(foto);
        }
    }
}
