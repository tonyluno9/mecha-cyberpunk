using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-993: las armas del banco de Tino llenan el panel del holograma. Se proyectan las mallas con la cámara del holograma y se mide qué
    /// fracción del ancho y del alto del panel ocupan (el panel recorta arriba y abajo la textura). Con CAPTURAR_T993=1 guarda
    /// docs/capturas/t993/&lt;arma&gt;.png (lo que ve el panel).
    /// </summary>
    public class HologramaArmaT993Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        private const string Variable = "CAPTURAR_T993";
        private const float MinimoAncho = 0.70f;
        private const float MinimoAlto = 0.65f;
        private const float Maximo = 0.95f;
        private static readonly string[] Nombres = { "pistola", "escopeta", "fusil", "francotirador" };

        private static ConfigMenuArmas Config() => AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);

        // Lo que ocupa el arma (sin el pedestal) en el panel: ancho y alto como fracción del panel.
        private static void Medir(VistaHologramaArma vista, out float ancho, out float alto)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (MeshFilter mf in vista.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.name == "Pedestal" || mf.sharedMesh == null)
                {
                    continue;
                }
                Bounds b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 e = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 p = vista.Camara.WorldToViewportPoint(mf.transform.TransformPoint(e));
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            }
            ancho = max.x - min.x;
            alto = (max.y - min.y) / vista.FraccionAltoVisible;
        }

        // La proporción real del panel de PantallaMejoraArmas, leída de la pantalla armada.
        private static float FraccionDelPanel(ConfigMenuArmas config)
        {
            GameObject go = new GameObject("PantallaPrueba");
            PantallaMejoraArmas pantalla = go.AddComponent<PantallaMejoraArmas>();
            pantalla.Configurar(config);
            pantalla.Precargar();
            float fraccion = pantalla.Vista.FraccionAltoVisible;
            Object.Destroy(go);
            return fraccion;
        }

        [UnityTest]
        public IEnumerator Holograma_CadaArma_LlenaElPanelSinSalirse([ValueSource(nameof(Nombres))] string nombre)
        {
            ConfigMenuArmas config = Config();
            float fraccion = FraccionDelPanel(config);
            Assert.Less(fraccion, 1f, "El panel recorta la textura");
            GameObject go = new GameObject("VistaPrueba");
            VistaHologramaArma vista = go.AddComponent<VistaHologramaArma>();
            vista.FijarFraccionAltoVisible(fraccion);
            vista.Mostrar(config.Armas[Array.IndexOf(Nombres, nombre)], config);
            yield return null;
            Assert.IsTrue(vista.ConModelo);
            Medir(vista, out float ancho, out float alto);
            Debug.Log("[T993] " + nombre + " ancho=" + ancho.ToString("F3") + " alto=" + alto.ToString("F3") + " distancia=" + vista.Distancia.ToString("F3"));
            Assert.IsTrue(ancho >= MinimoAncho || alto >= MinimoAlto, nombre + ": ocupa ancho " + ancho + " y alto " + alto + " del panel");
            Assert.LessOrEqual(ancho, Maximo, nombre + ": no se sale a lo ancho");
            Assert.LessOrEqual(alto, Maximo, nombre + ": no se sale a lo alto");
            if (Environment.GetEnvironmentVariable(Variable) == "1")
            {
                Guardar(vista, "docs/capturas/t993/" + nombre + ".png");
            }
            vista.Ocultar();
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Holograma_Rueda_AcercaYAlejaDesdeElEncuadreYElPedestalQuedaDebajo()
        {
            ConfigMenuArmas config = Config();
            GameObject go = new GameObject("VistaPrueba");
            VistaHologramaArma vista = go.AddComponent<VistaHologramaArma>();
            vista.FijarFraccionAltoVisible(FraccionDelPanel(config));
            vista.Mostrar(config.Armas[1], config);
            yield return null;
            float baseDist = vista.DistanciaEncuadre;
            Assert.AreEqual(baseDist, vista.Distancia, 0.0001f, "Empieza en el encuadre");
            vista.Acercar(120f);
            Assert.Less(vista.Distancia, baseDist, "La rueda hacia adelante acerca");
            vista.Acercar(-240f);
            Assert.Greater(vista.Distancia, baseDist, "La rueda hacia atrás aleja");
            vista.Acercar(100000f);
            Assert.AreEqual(baseDist * config.ZoomMinimo, vista.Distancia, 0.001f, "Tope cerca relativo al encuadre");
            vista.Acercar(-100000f);
            Assert.AreEqual(baseDist * config.ZoomMaximo, vista.Distancia, 0.001f, "Tope lejos relativo al encuadre");
            // El pedestal queda por debajo del arma, sin tapar su centro.
            Renderer pedestal = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "Pedestal")
                {
                    pedestal = r;
                }
            }
            Assert.IsNotNull(pedestal, "Hay pedestal");
            Assert.Less(pedestal.bounds.max.y, config.PosicionSet.y, "El pedestal está por debajo del centro del arma");
            vista.Ocultar();
            Object.Destroy(go);
        }

        // Guarda lo que ve el panel: la textura recortada arriba y abajo, sobre fondo oscuro.
        private static void Guardar(VistaHologramaArma vista, string ruta)
        {
            RenderTexture rt = vista.Textura;
            RenderTexture.active = rt;
            int alto = Mathf.RoundToInt(rt.height * vista.FraccionAltoVisible);
            Texture2D leida = new Texture2D(rt.width, alto, TextureFormat.RGBA32, false);
            leida.ReadPixels(new Rect(0, (rt.height - alto) / 2, rt.width, alto), 0, 0);
            RenderTexture.active = null;
            Color[] pixeles = leida.GetPixels();
            for (int i = 0; i < pixeles.Length; i++)
            {
                Color p = pixeles[i];
                pixeles[i] = new Color(0.02f + p.r, 0.05f + p.g, 0.08f + p.b, 1f);
            }
            leida.SetPixels(pixeles);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, leida.EncodeToPNG());
            Object.Destroy(leida);
        }
    }
}
