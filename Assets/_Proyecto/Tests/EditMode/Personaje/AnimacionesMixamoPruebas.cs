using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class AnimacionesMixamoPruebas
    {
        const string Raiz = "Assets/Arte/Personajes/Animaciones/";

        // grupo, nombre, bucle
        static readonly object[][] Esperados =
        {
            new object[] { "Locomocion", "Loco_Idle", true }, new object[] { "Locomocion", "Loco_Caminar", true },
            new object[] { "Locomocion", "Loco_CaminarAtras", true }, new object[] { "Locomocion", "Loco_CaminarDer", true },
            new object[] { "Locomocion", "Loco_CaminarIzq", true }, new object[] { "Locomocion", "Loco_Correr", true },
            new object[] { "Locomocion", "Loco_Frenar", false }, new object[] { "Locomocion", "Loco_GirarDer", false },
            new object[] { "Locomocion", "Loco_GirarIzq", false }, new object[] { "Locomocion", "Loco_SaltoInicio", false },
            new object[] { "Locomocion", "Loco_SaltoAire", true }, new object[] { "Locomocion", "Loco_SaltoAterrizaje", false },
            new object[] { "Locomocion", "Loco_Caida", true },
            new object[] { "Pistola", "Pistola_Apuntar", true }, new object[] { "Pistola", "Pistola_ApuntarCaminar_Derecha", true },
            new object[] { "Pistola", "Pistola_ApuntarCaminar_Izquierda", true }, new object[] { "Pistola", "Pistola_Caminar", true },
            new object[] { "Pistola", "Pistola_Correr", true }, new object[] { "Pistola", "Pistola_Disparo", false },
            new object[] { "Pistola", "Pistola_Idle", true }, new object[] { "Pistola", "Pistola_Recargar", false },
            new object[] { "Pistola", "Pistola_RecargarCaminar", false },
            new object[] { "Rifle", "Rifle_Apuntar", true }, new object[] { "Rifle", "Rifle_ApuntarCaminar", true },
            new object[] { "Rifle", "Rifle_ApuntarCorrer", true }, new object[] { "Rifle", "Rifle_Caminar", true },
            new object[] { "Rifle", "Rifle_Correr", true }, new object[] { "Rifle", "Rifle_DisparoCaminar", false },
            new object[] { "Rifle", "Rifle_Idle", true }, new object[] { "Rifle", "Rifle_Recargar", false },
            new object[] { "Rifle", "Rifle_AgachadoCaminar", true },
            new object[] { "Punos", "Punos_Fuerte", false }, new object[] { "Punos", "Punos_GolpeDer", false },
            new object[] { "Punos", "Punos_GolpeIzq", false }, new object[] { "Punos", "Punos_Guardia", true },
            new object[] { "Cobertura", "Cobertura_Disparo", false }, new object[] { "Cobertura", "Cobertura_Disparo2", false },
            new object[] { "Cobertura", "Cobertura_Entrar", false },
            new object[] { "Dano", "Dano_Espalda", false }, new object[] { "Dano", "Dano_Frente", false },
            new object[] { "Dano", "Dano_Fuerte", false }, new object[] { "Dano", "Dano_Levantarse_BocaAbajo", false },
            new object[] { "Dano", "Dano_Levantarse_BocaArriba", false },
            new object[] { "Muerte", "Muerte_Adelante", false }, new object[] { "Muerte", "Muerte_Atras", false },
            new object[] { "Muerte", "Muerte_Desmayo", false },
        };

        [Test]
        public void SonCuarentaYSeisClips()
        {
            Assert.AreEqual(46, Esperados.Length);
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
                Assert.IsFalse(clips[0].lockRootPositionXZ, "En el sitio: sin bake de la posición XZ, la raíz se ignora " + ruta);
                Assert.IsTrue(clips[0].lockRootHeightY, "La altura va en la pose (muertes y levantarse) " + ruta);
                Assert.IsNotNull(BuscarClip(ruta), "clip " + ruta);
            }
        }

        static AnimationClip BuscarClip(string ruta)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            return null;
        }
    }
}
