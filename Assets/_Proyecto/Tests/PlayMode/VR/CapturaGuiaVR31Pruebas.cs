using System.Collections;
using System.IO;
using MechaCyberpunk.VR.Guia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR31: abre la guía de controles con el simulador XR y guarda lo que dibuja una cámara frente al panel
    /// (páginas de botones, sacar pistola y recargar) en docs/capturas/_tareas/VR31/.
    /// </summary>
    public class CapturaGuiaVR31Pruebas : BaseJugadorVR
    {
        private const string Carpeta = "docs/capturas/_tareas/VR31";

        [UnityTest]
        public IEnumerator Guia_SeCapturaPorPaginas()
        {
            PlayerPrefs.SetInt(GuiaControlesVR.ClaveVista, 1);
            yield return ArmarVR();
            GuiaControlesVR guia = GuiaControlesVR.Abrir();
            Assert.IsNotNull(guia, "La guía se abre en VR.");
            yield return null;
            yield return null;

            Transform panel = guia.Canvas.transform;
            var objeto = new GameObject("CamaraCapturaVR31");
            extras.Add(objeto);
            Camera camara = objeto.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            camara.fieldOfView = 60f;
            camara.nearClipPlane = 0.05f;
            camara.stereoTargetEye = StereoTargetEyeMask.None;
            float alto = GuiaControlesVR.AltoPx * GuiaControlesVR.MetrosPorPx;
            float distancia = alto * 0.6f / Mathf.Tan(30f * Mathf.Deg2Rad);
            objeto.transform.position = panel.position - panel.forward * distancia;
            objeto.transform.rotation = Quaternion.LookRotation(panel.forward, panel.up);

            Directory.CreateDirectory(Carpeta);
            int[] paginas = { 0 };
            foreach (int p in paginas)
            {
                guia.IrA(p);
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                camara.targetTexture = rt;
                camara.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                camara.targetTexture = null;
                File.WriteAllBytes(Path.Combine(Carpeta, "guia_pagina_" + p + ".png"), tex.EncodeToPNG());
                Object.Destroy(tex);
                rt.Release();
                Object.Destroy(rt);
            }
            Assert.IsTrue(File.Exists(Path.Combine(Carpeta, "guia_pagina_0.png")));
            guia.Cerrar();
        }
    }
}
