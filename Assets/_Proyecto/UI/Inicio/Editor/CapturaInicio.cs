using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MechaCyberpunk.UI.Inicio.Editor
{
    /// <summary>
    /// Toma una foto de la pantalla de inicio en Play (para revisarla con la Unity CLI): pone los canvas Overlay en Screen Space Camera,
    /// renderiza la cámara principal a 1920x1080 con el posprocesado puesto y guarda el PNG. Se llama con
    /// <c>unity-vivo.sh eval 'MechaCyberpunk.UI.Inicio.Editor.CapturaInicio.Tomar("ruta.png"); return 1;'</c>.
    /// </summary>
    public static class CapturaInicio
    {
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const float Plano = 0.35f;

        public static string Tomar(string ruta)
        {
            Camera camara = Camera.main;
            if (camara == null)
            {
                return "sin camara";
            }
            List<Canvas> cambiados = new List<Canvas>();
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = camara;
                    c.planeDistance = Plano + Mathf.Max(0, 32100 - c.sortingOrder) * 0.00001f;
                    cambiados.Add(c);
                }
            }
            RenderTexture anterior = camara.targetTexture;
            RenderTexture activa = RenderTexture.active;
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            Texture2D foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            try
            {
                camara.targetTexture = rt;
                camara.aspect = (float)Ancho / Alto;
                Canvas.ForceUpdateCanvases();
                camara.Render();
                RenderTexture.active = rt;
                foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
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
                camara.targetTexture = anterior;
                camara.ResetAspect();
                RenderTexture.active = activa;
                foreach (Canvas c in cambiados)
                {
                    if (c != null)
                    {
                        c.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                }
                Object.Destroy(rt);
                Object.Destroy(foto);
            }
            return ruta;
        }
    }
}
