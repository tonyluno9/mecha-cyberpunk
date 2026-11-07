using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-882 (playtest 7-nov, sesión 2): charcos con reflejo nítido, borde mojado y ondas que no son un espejo plano.</summary>
    public class CharcosS2oPruebas
    {
        [Test]
        public void Reflejo_AResolucionCompleta()
        {
            ConfigCharcos config = AssetDatabase.LoadAssetAtPath<ConfigCharcos>(ColocarS2cLook.RutaConfigCharcos);
            Assert.AreEqual(1f, config.EscalaResolucion, 0.001f, "a la mitad se veía borroso");
        }

        [Test]
        public void Mascaras_TraenBordeMojadoMasAnchoQueElCharco()
        {
            for (int i = 0; i < ColocarS2oCharcos.Formas; i++)
            {
                string ruta = KitS2c.CarpetaTexturas + "/S2c_Charco_" + i + ".png";
                Texture2D t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(ruta));
                Color32[] px = t.GetPixels32();
                Object.DestroyImmediate(t);
                int charco = px.Count(p => p.r > 128);
                int mojado = px.Count(p => p.g > 128);
                Assert.Greater(charco, 0, ruta);
                Assert.Greater(mojado, charco * 1.1f, ruta + ": el borde mojado rodea al charco");
            }
        }

        [Test]
        public void Shader_TieneBordeMojadoYOndas()
        {
            string fuente = File.ReadAllText("Assets/_Proyecto/Mundo/Ambiente/Shaders/Charco.shader");
            StringAssert.Contains("_ColorBorde", fuente);
            StringAssert.Contains("mascara.g", fuente, "el borde sale del canal G");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(KitS2c.Carpeta + "/S2c_Charco_0.mat");
            Assert.Greater(m.GetFloat("_Ondas"), 0.003f, "con ondas, no espejo plano");
        }
    }
}
