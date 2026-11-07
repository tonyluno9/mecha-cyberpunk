using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Capturas de cerca del jugador para la sesión S1 (docs/capturas/s1-*.png). Solo se guardan con CAPTURAR_S1=1.</summary>
    public static class CapturaS1
    {
        public static bool Activa => System.Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1";

        /// <summary>Foto de lo que ve una cámara del juego (por ejemplo la del jugador). Deja la cámara como estaba.</summary>
        public static void FotoCamara(string nombre, Camera camara, int ancho = 1280, int alto = 720)
        {
            if (!Activa || camara == null) return;
            RenderTexture previa = camara.targetTexture;
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            camara.targetTexture = previa;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(foto);
        }

        /// <summary>Foto de un canvas de pantalla (HUD o menú) sobre fondo oscuro. Lo deja como estaba.</summary>
        public static void FotoCanvas(string nombre, Canvas canvas, int ancho = 1280, int alto = 720)
        {
            if (!Activa || canvas == null) return;
            GameObject go = new GameObject("CamCanvasS1");
            Camera c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.12f, 0.14f, 0.2f);
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            c.targetTexture = rt;
            RenderMode modo = canvas.renderMode;
            Camera previa = canvas.worldCamera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = c;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            canvas.renderMode = modo;
            canvas.worldCamera = previa;
            Object.Destroy(go);
            Object.Destroy(rt);
            Object.Destroy(foto);
        }

        /// <summary>Foto de cerca de un punto del mundo, desde una dirección en coordenadas locales del jugador a <paramref name="distancia"/> metros.</summary>
        public static void FotoMira(string nombre, Transform jugador, Vector3 objetivo, Vector3 direccionLocal, float distancia, float fov = 30f, int tamano = 600)
        {
            if (!Activa) return;
            Vector3 camara = objetivo + jugador.TransformDirection(direccionLocal.normalized) * distancia;
            Foto(nombre, jugador, jugador.InverseTransformPoint(camara), jugador.InverseTransformPoint(objetivo), fov, tamano, tamano);
        }

        /// <summary>Foto del objetivo desde un punto en coordenadas locales del jugador (x derecha, y arriba, z adelante), mirando a <paramref name="mira"/> (también local).</summary>
        public static void Foto(string nombre, Transform jugador, Vector3 camaraLocal, Vector3 miraLocal, float fov = 40f, int ancho = 900, int alto = 900)
        {
            if (!Activa) return;
            GameObject go = new GameObject("CamS1");
            Camera c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            c.fieldOfView = fov;
            c.nearClipPlane = 0.05f;
            go.transform.position = jugador.TransformPoint(camaraLocal);
            go.transform.rotation = Quaternion.LookRotation(jugador.TransformPoint(miraLocal) - go.transform.position, Vector3.up);
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            Object.Destroy(go);
            Object.Destroy(rt);
            Object.Destroy(foto);
        }
    }
}
