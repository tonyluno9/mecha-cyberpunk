using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-621: los 27 clips de los zips "Carros e interaccion" y "Enemigos y jefe" están importados como Humanoid, con su bucle.</summary>
    public class AnimacionesAnim2Pruebas
    {
        const string Raiz = "Assets/Arte/Personajes/Animaciones/";

        // grupo, nombre, bucle
        public static readonly object[][] Esperados =
        {
            new object[] { "Carro", "Carro_Entrar", false }, new object[] { "Carro", "Carro_Salir", false },
            new object[] { "Carro", "Carro_Manejar_Bucle", true }, new object[] { "Carro", "Carro_Pitar", false },
            new object[] { "Interaccion", "Inter_AbrirPuerta", false }, new object[] { "Interaccion", "Inter_Boton", false },
            new object[] { "Interaccion", "Inter_Recoger", false }, new object[] { "Interaccion", "Inter_Telefono", false },
            new object[] { "Interaccion", "Inter_VerTelefono_Bucle", true },
            new object[] { "Skull", "Skull_Idle", true }, new object[] { "Skull", "Skull_Caminar", true }, new object[] { "Skull", "Skull_Burla", false },
            new object[] { "Jefe", "Jefe_Idle", true }, new object[] { "Jefe", "Jefe_Caminar", true }, new object[] { "Jefe", "Jefe_Ataque", false },
            new object[] { "Jefe", "Jefe_Burla", false }, new object[] { "Jefe", "Jefe_Muerte", false },
            new object[] { "Nova", "Nova_Idle", true }, new object[] { "Nova", "Nova_Caminar", true }, new object[] { "Nova", "Nova_Correr", true },
            new object[] { "Nova", "Nova_Punch", false }, new object[] { "Nova", "Nova_Barrido", false }, new object[] { "Nova", "Nova_Flexing", false },
            new object[] { "Nova", "Nova_Muerte", false },
        };

        [Test]
        public void SonVeinticuatroClips()
        {
            Assert.AreEqual(24, Esperados.Length);
        }

        [Test]
        public void ClipsExistenSonHumanoidYTienenElBucleCorrecto()
        {
            foreach (var e in Esperados)
            {
                string ruta = Raiz + e[0] + "/" + e[1] + ".fbx";
                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                Assert.IsNotNull(imp, "Falta " + ruta);
                Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, ruta);
                var clips = imp.clipAnimations;
                Assert.AreEqual(1, clips.Length, ruta);
                Assert.AreEqual((string)e[1], clips[0].name, ruta);
                Assert.AreEqual((bool)e[2], clips[0].loopTime, "bucle " + ruta);
                AnimationClip clip = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview__")) clip = c;
                Assert.IsNotNull(clip, "clip " + ruta);
                Assert.Greater(clip.length, 0.2f, ruta);
            }
        }
    }
}
