using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1513: tres fotos desde dentro de la ciudad mirando al muro nuevo: docs/capturas/_20nov/muro_*.png.
    /// Abre Ciudad_Prologo sin guardar. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.FotosT1513.Tomar
    /// </summary>
    public static class FotosT1513
    {
        private const string Carpeta = "docs/capturas/_20nov";
        private const float AltoCamara = 9f;
        private const float DistanciaAlMuro = 28f;

        public static void Tomar()
        {
            ReemplazarEdificiosT1502.Abrir();
            Directory.CreateDirectory(Carpeta);
            var segmentos = MuroCiudadT1513.MurosViejos();
            if (segmentos.Count == 0)
            {
                Debug.LogWarning("[FotosT1513] no hay muro");
                return;
            }
            // Tramo oeste/este largo (calle x = +-135), tramo norte/sur largo y una esquina.
            var esteNorte = segmentos.Find(m => m.position.x > 100f && m.position.z > 0f);
            var norteCentro = segmentos.Find(m => Mathf.Abs(m.position.x) < 1f && m.position.z > 0f);
            Transform este = esteNorte != null ? esteNorte : segmentos[0];
            Transform norte = norteCentro != null ? norteCentro : segmentos[segmentos.Count - 1];
            Foto("muro_este", este.position, Vector3.left);
            Foto("muro_norte", norte.position, Vector3.back);
            Vector3 esquina = new Vector3(Mathf.Max(este.position.x, norte.position.x), 0f, Mathf.Max(este.position.z, norte.position.z));
            Foto("muro_esquina", esquina, new Vector3(-1f, 0f, -1f).normalized);
        }

        // La cámara se pone hacia el interior (haciaDentro) a 28 m del punto, a 9 m de alto, y mira al muro.
        private static void Foto(string nombre, Vector3 punto, Vector3 haciaDentro)
        {
            var go = new GameObject("CamFotosT1513");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 1500f;
            Vector3 objetivo = new Vector3(punto.x, 4f, punto.z);
            go.transform.position = new Vector3(punto.x, AltoCamara, punto.z) + haciaDentro * DistanciaAlMuro;
            go.transform.LookAt(objetivo);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Carpeta, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log("[FotosT1513] " + nombre + " desde " + go.transform.position);
            Object.DestroyImmediate(go);
        }
    }
}
