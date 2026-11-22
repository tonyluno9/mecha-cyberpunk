using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>Tema del menú y volumen por AudioMixer (pedido de Tony: música del menú y control de volumen).</summary>
    public class MusicaMenuYMezclaPruebas
    {
        private const string RutaClip = "Assets/Arte/Audio/Musica/MUS_MENU_PRINCIPAL.wav";
        private const string RutaMezclador = "Assets/_Proyecto/Progresion/Sonido/Resources/MezclaCyberCity.mixer";

        [Test]
        public void Decibeles_CeroEsSilencio_UnoEsCero_YLaMitadSeisMenos()
        {
            Assert.AreEqual(MezclaAudio.Silencio, MezclaAudio.Decibeles(0f), 1e-4f, "0 en el deslizador = silencio de verdad");
            Assert.AreEqual(0f, MezclaAudio.Decibeles(1f), 1e-4f);
            Assert.AreEqual(-6.02f, MezclaAudio.Decibeles(0.5f), 0.01f, "escala de decibeles");
            Assert.Less(MezclaAudio.Decibeles(0.05f), MezclaAudio.Decibeles(0.1f));
        }

        [Test]
        public void Secciones_VanASuGrupo_AmbienteEInterfazPorEfectos()
        {
            Assert.AreEqual("Master", MezclaAudio.NombreGrupo(SeccionSonido.General));
            Assert.AreEqual("Musica", MezclaAudio.NombreGrupo(SeccionSonido.Musica));
            Assert.AreEqual("Voces", MezclaAudio.NombreGrupo(SeccionSonido.Voces));
            Assert.AreEqual("Efectos", MezclaAudio.NombreGrupo(SeccionSonido.Efectos));
            Assert.AreEqual("Efectos", MezclaAudio.NombreGrupo(SeccionSonido.Ambiente));
            Assert.AreEqual("Efectos", MezclaAudio.NombreGrupo(SeccionSonido.Interfaz));
            Assert.IsNull(MezclaAudio.Parametro(SeccionSonido.Ambiente), "el ambiente sigue al deslizador de efectos");
        }

        [Test]
        public void Mezclador_TieneLosCuatroGrupos_YSusVolumenesExpuestos()
        {
            AudioMixer m = AssetDatabase.LoadAssetAtPath<AudioMixer>(RutaMezclador);
            Assert.IsNotNull(m, "corre CrearMezclaAudio.Aplicar");
            foreach (string g in new[] { "Master", "Musica", "Efectos", "Voces" })
            {
                Assert.IsTrue(m.FindMatchingGroups(g).Any(x => x.name == g), "grupo " + g);
            }
            foreach (string p in new[] { MezclaAudio.ParamGeneral, MezclaAudio.ParamMusica, MezclaAudio.ParamEfectos, MezclaAudio.ParamVoces })
            {
                Assert.IsTrue(m.GetFloat(p, out _), p + " expuesto");
            }
            Assert.AreSame(m, Resources.Load<AudioMixer>(MezclaAudio.Recurso), "se carga de Resources en el juego");
        }

        [Test]
        public void TemaDelMenu_ImportadoComoMusica_YEnElCatalogo()
        {
            var imp = (AudioImporter)AssetImporter.GetAtPath(RutaClip);
            Assert.IsNotNull(imp, "MUS_MENU_PRINCIPAL.wav en Assets/Arte/Audio/Musica");
            Assert.IsFalse(imp.forceToMono, "estéreo");
            Assert.AreEqual(AudioClipLoadType.Streaming, imp.defaultSampleSettings.loadType);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaClip);
            Assert.AreEqual(2, clip.channels);
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>("Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset");
            var entrada = catalogo.Entradas.FirstOrDefault(e => e != null && e.id == MusicaMenu.Id);
            Assert.IsNotNull(entrada, "MUS_MENU_PRINCIPAL en el catálogo");
            Assert.AreSame(clip, entrada.finales[0]);
            Assert.IsTrue(catalogo.Encendido(MusicaMenu.Id));
            Assert.AreEqual(SeccionSonido.Musica, ReglaCatalogoSonidos.SeccionDe(MusicaMenu.Id), "grupo Música");
        }

        [Test]
        public void Fundidos_YCruceDelBucle()
        {
            Assert.AreEqual(0.5f, ReglaMusicaMenu.Fundir(0f, 1f, 0.75f, 1.5f), 1e-4f, "entra en 1.5 s");
            Assert.AreEqual(1f, ReglaMusicaMenu.Fundir(0.9f, 1f, 1f, 1.5f), 1e-4f, "no se pasa");
            Assert.AreEqual(0f, ReglaMusicaMenu.Fundir(0.3f, 0f, 1f, 1.6f), 1e-4f);
            Assert.IsFalse(ReglaMusicaMenu.TocaCruce(30f, 40.42f, 2.5f));
            Assert.IsTrue(ReglaMusicaMenu.TocaCruce(38f, 40.42f, 2.5f), "el cruce empieza 2.5 s antes del final");
            Assert.Less(MusicaMenu.SegundosSalida, 2.2f, "se apaga antes que el mínimo de la pantalla de carga");
        }

        [Test]
        public void Opciones_LaMusicaYaNoArrancaApagada_YLasViejasEnCeroSeSuben()
        {
            Assert.Greater(ReglaOpciones.VolumenInicial(SeccionSonido.Musica), 0f);
            var vieja = new ValoresOpciones { volumenes = new[] { 0.8f, 0.8f, 0.8f, 0.8f, 0f, 0.8f }, versionVolumen = 0 };
            ReglaOpciones.Recortar(vieja);
            Assert.AreEqual(ReglaOpciones.VolumenPorDefecto, vieja.volumenes[(int)SeccionSonido.Musica], 1e-4f, "archivo viejo: la música en 0 era el defecto viejo");
            Assert.AreEqual(ReglaOpciones.VersionVolumen, vieja.versionVolumen);
            var nueva = new ValoresOpciones { volumenes = new[] { 0.8f, 0.8f, 0.8f, 0.8f, 0f, 0.8f }, versionVolumen = ReglaOpciones.VersionVolumen };
            ReglaOpciones.Recortar(nueva);
            Assert.AreEqual(0f, nueva.volumenes[(int)SeccionSonido.Musica], 1e-4f, "si la bajaste a 0 tú, se respeta");
        }
    }
}
