using System.Linq;
using System.IO;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-594: el piano repetitivo (ZON_ENTRAR_*) ya no suena y el sonido de texto es un tic corto y bajito.</summary>
    public class SonidoUiT594Pruebas
    {
        private static string[] CodigoDelJuego()
        {
            return Directory.GetFiles("Assets/_Proyecto", "*.cs", SearchOption.AllDirectories)
                .Where(r => !r.Replace("\\", "/").Contains("/Tests/")).ToArray();
        }

        [Test]
        public void ElPiano_NingunCodigoPideZonEntrar()
        {
            foreach (string ruta in CodigoDelJuego())
            {
                StringAssert.DoesNotContain("ZON_ENTRAR", File.ReadAllText(ruta), ruta + " pide el piano de entrar a la zona");
            }
        }

        [Test]
        public void ElMensajeDelTelefonoYaNoUsaElTimbre()
        {
            string codigo = File.ReadAllText("Assets/_Proyecto/Progresion/Mensajes/ServicioMensajes.cs");
            StringAssert.Contains("\"UI_TEXTO\"", codigo);
            StringAssert.DoesNotContain("UI_TELEFONO", codigo);
        }

        [TestCase("Assets/_Proyecto/UI/HudAPie/ColaAvisos.cs")]
        [TestCase("Assets/_Proyecto/UI/HudAPie/AvisoMensaje.cs")]
        [TestCase("Assets/_Proyecto/UI/Historia/PantallaSubtitulos.cs")]
        public void LosAvisosYLosDialogosTocanElSonidoDeTexto(string ruta)
        {
            StringAssert.Contains("SonidoTexto", File.ReadAllText(ruta));
        }

        [Test]
        public void ElSonidoDeTextoEsCortoBajitoYSinRepetirseRapido()
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para("UI_TEXTO");
            Assert.LessOrEqual(p.Volumen, 0.5f);
            Assert.Greater(p.SegundosMinimos, 0.05f);
            Assert.Greater(p.VariacionTono, 0f);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Arte/Audio/Provisional/UI/UI_TEXTO.wav");
            Assert.IsNotNull(clip, "El provisional generado por codigo existe.");
            Assert.Less(clip.length, 0.15f, "Es un tic, no una melodia.");
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>("Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset");
            Assert.IsNotNull(catalogo.Elegir("UI_TEXTO"), "El catalogo tiene el ID.");
        }
    }
}
