using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1543: las vistas de FueraDelMuroT1543.Vistas (aérea completa y las de A24, K32, K33, K34) en docs/capturas/_21nov/muro1543_*.png.
    /// Abre Ciudad_Prologo sin guardar. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.FotosT1543.Tomar
    /// </summary>
    public static class FotosT1543
    {
        private const string Carpeta = "docs/capturas/_21nov";
        private const int Ancho = 1280;
        private const int Alto = 720;

        public static void Tomar()
        {
            ReemplazarEdificiosT1502.Abrir();
            Directory.CreateDirectory(Carpeta);
            foreach (FueraDelMuroT1543.Vista v in FueraDelMuroT1543.Vistas(FueraDelMuroT1543.LineaDelMuro()))
            {
                Foto(v);
            }
        }

        private static void Foto(FueraDelMuroT1543.Vista v)
        {
            GameObject go = new GameObject("CamFotosT1543");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = v.Fov;
            cam.farClipPlane = 1500f;
            go.transform.position = v.Posicion;
            go.transform.LookAt(v.Objetivo);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Carpeta, "muro1543_" + v.Nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log("[T-1543] foto " + v.Nombre + " desde " + v.Posicion);
            Object.DestroyImmediate(go);
        }
    }
}
