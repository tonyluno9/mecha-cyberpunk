using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>T-973: la mesera R01 de Blender lleva la charola pesada al 100 % a la mano izquierda (ya no se dobla con los dedos).</summary>
    public class MeseraR01T973Pruebas
    {
        private const string Ruta = "Assets/Arte/Personajes/MeseraRobot/MeseraRobot.fbx";

        [Test]
        public void Avatar_EsHumanoYValido()
        {
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(Ruta);
            Assert.IsNotNull(avatar, "El FBX tiene su propio avatar");
            Assert.IsTrue(avatar.isValid, "El avatar es válido");
            Assert.IsTrue(avatar.isHuman, "El avatar es humanoide");
        }

        [Test]
        public void Charola_SigueALaMano()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Ruta);
            Assert.IsNotNull(fbx);
            SkinnedMeshRenderer smr = fbx.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(smr, "Hay SkinnedMeshRenderer");
            Mesh malla = smr.sharedMesh;
            Assert.IsNotNull(malla);
            Transform[] huesos = smr.bones;
            var porVertice = malla.GetBonesPerVertex();
            var pesos = malla.GetAllBoneWeights();
            int idx = 0, enLaMano = 0;
            for (int v = 0; v < porVertice.Length; v++)
            {
                int n = porVertice[v];
                int mejor = -1; float pesoMax = 0f;
                for (int k = 0; k < n; k++)
                {
                    BoneWeight1 w = pesos[idx + k];
                    if (w.weight > pesoMax) { pesoMax = w.weight; mejor = w.boneIndex; }
                }
                idx += n;
                if (mejor >= 0 && pesoMax > 0.999f && huesos[mejor].name == "mixamorig:LeftHand") { enLaMano++; }
            }
            TestContext.Out.WriteLine("verticesMano=" + enLaMano + " de " + porVertice.Length);
            Assert.Greater(enLaMano, 1500, "Los vértices de la charola van al 100 % a LeftHand (" + enLaMano + ")");
        }

        [Test]
        public void Malla_ConservaElMaterial()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Ruta);
            SkinnedMeshRenderer smr = fbx.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.AreEqual(1, smr.sharedMaterials.Length);
            Assert.IsNotNull(smr.sharedMaterial);
            Assert.AreEqual("tripo_mat_3a993998", smr.sharedMaterial.name);
            Assert.AreNotEqual("Hidden/InternalErrorShader", smr.sharedMaterial.shader.name, "El material no es rosa");
        }
    }
}
