using System.IO;
using System.Collections.Generic;
using MechaCyberpunk.UI.Hud;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// Ayudante de capturas (T-414, lo pedía T-408): pone los canvas Overlay en Screen Space Camera, renderiza la cámara a un tamaño fijo
    /// (1920x1080 o 2560x1440), guarda el PNG y deja todo como estaba.
    /// </summary>
    public static class CapturaUI
    {
        public const int Ancho = 1920;
        public const int Alto = 1080;
        public const int Ancho2K = 2560;
        public const int Alto2K = 1440;
        private const float DistanciaPlano = 0.01f;

        public static void Capturar(string ruta, Camera camara) => Capturar(ruta, camara, Ancho, Alto);

        public static void Capturar2K(string ruta, Camera camara) => Capturar(ruta, camara, Ancho2K, Alto2K);

        public static void Capturar(string ruta, Camera camara, int ancho, int alto)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<Canvas> cambiados = new List<Canvas>();
            foreach (Canvas c in canvases)
            {
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = camara;
                    // T-408: pegado al plano cercano, para que lo que está a menos de 1 m de la cámara no tape la UI.
                    c.planeDistance = camara.nearClipPlane + DistanciaPlano;
                    cambiados.Add(c);
                }
            }
            RenderTexture anteriorObjetivo = camara.targetTexture;
            RenderTexture activa = RenderTexture.active;
            RenderTexture rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            try
            {
                camara.targetTexture = rt;
                // T-408: la cámara toma la proporción de la foto y los marcadores del objetivo se recalculan con ella.
                camara.aspect = (float)ancho / alto;
                Canvas.ForceUpdateCanvases();
                foreach (MarcadorObjetivo marcador in Object.FindObjectsByType<MarcadorObjetivo>(FindObjectsSortMode.None))
                {
                    marcador.Actualizar();
                }
                Canvas.ForceUpdateCanvases();
                camara.Render();
                RenderTexture.active = rt;
                foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
                foto.Apply();
                string carpeta = Path.GetDirectoryName(ruta);
                if (!string.IsNullOrEmpty(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }
                File.WriteAllBytes(ruta, foto.EncodeToPNG());
            }
            finally
            {
                camara.targetTexture = anteriorObjetivo;
                camara.ResetAspect();
                RenderTexture.active = activa;
                foreach (Canvas c in cambiados)
                {
                    if (c != null)
                    {
                        c.worldCamera = null;
                        c.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                }
                Object.Destroy(foto);
                rt.Release();
                Object.Destroy(rt);
            }
        }
    }
}
