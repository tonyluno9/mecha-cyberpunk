using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-731 (A1): los clips de pistola, daño y agachado de Tony importan como Humanoid, con su bucle y duración.</summary>
    public class ClipsPJ01Pruebas
    {
        const string Raiz = "Assets/Arte/Personajes/Animaciones/";

        // grupo, nombre, bucle
        static readonly object[][] Esperados =
        {
            new object[] { "Pistola", "Pistola_Disparo", false }, new object[] { "Pistola", "Pistola_Correr", true },
            new object[] { "Pistola", "Pistola_ApuntarCaminar_Derecha", true }, new object[] { "Pistola", "Pistola_ApuntarCaminar_Izquierda", true },
            new object[] { "Pistola", "Pistola_ApuntarLateral_Derecha", true }, new object[] { "Pistola", "Pistola_ApuntarLateral_Izquierda", true },
            new object[] { "Pistola", "Pistola_ApuntarAtras", true }, new object[] { "Pistola", "Pistola_AgachadoIdle", true },
            new object[] { "Pistola", "Pistola_AgachadoCaminar", true }, new object[] { "Pistola", "Pistola_Sacar", false },
            new object[] { "Pistola", "Pistola_Guardar", false },
            new object[] { "Dano", "Pistola_Dano_Frente", false }, new object[] { "Dano", "Pistola_Dano_Espalda", false },
            new object[] { "Dano", "Pistola_Dano_Fuerte", false },
            new object[] { "Locomocion", "Loco_AgachadoIdle", true }, new object[] { "Locomocion", "Loco_AgachadoCaminar", true },
            new object[] { "Locomocion", "Loco_AgachadoLateral_Derecha", true }, new object[] { "Locomocion", "Loco_AgachadoLateral_Izquierda", true },
            new object[] { "Locomocion", "Loco_AgachadoAtras", true }, new object[] { "Locomocion", "Loco_Agacharse", false },
            new object[] { "Locomocion", "Loco_Pararse", false }, new object[] { "Locomocion", "Loco_Rodar", false },
        };

        [Test]
        public void SonVeintidosClips()
        {
            Assert.AreEqual(22, Esperados.Length);
        }

        [Test]
        public void CadaClipEsHumanoidConSuBucleYDuracion()
        {
            foreach (var e in Esperados)
            {
                string ruta = Raiz + e[0] + "/" + e[1] + ".fbx";
                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                Assert.IsNotNull(imp, "Falta " + ruta);
                Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, ruta);
                Assert.AreEqual(ModelImporterAvatarSetup.CreateFromThisModel, imp.avatarSetup, ruta);
                var clips = imp.clipAnimations;
                Assert.AreEqual(1, clips.Length, ruta);
                Assert.AreEqual((string)e[1], clips[0].name, ruta);
                Assert.AreEqual((bool)e[2], clips[0].loopTime, "bucle " + ruta);
                AnimationClip clip = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview__")) clip = c;
                Assert.IsNotNull(clip, "clip " + ruta);
                Assert.IsTrue(clip.isHumanMotion, "humanoid " + ruta);
                Assert.Greater(clip.length, 0f, "duración " + ruta);
            }
        }
    }
}
