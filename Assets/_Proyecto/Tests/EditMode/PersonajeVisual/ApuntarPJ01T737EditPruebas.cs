using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-737: el avatar Humanoid de PJ01 esta bien armado (huesos, pose de referencia en T, sin Translation DoF) y el jugador no lleva correcciones de IK
    /// encima del clip al apuntar. La medida contra el clip puro esta en PlayMode (ApuntarPJ01T737Pruebas).
    /// </summary>
    public class ApuntarPJ01T737EditPruebas
    {
        private const string RutaPJ01 = "Assets/Arte/Personajes/PJ01/PJ01.fbx";
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        private const string RutaControlador = "Assets/Arte/Personajes/Animaciones/ControladorBase.controller";

        private static GameObject Instancia(out Animator animador)
        {
            GameObject go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPJ01));
            animador = go.GetComponent<Animator>();
            if (animador == null) { animador = go.AddComponent<Animator>(); }
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaPJ01)) { if (o is Avatar a) { animador.avatar = a; } }
            return go;
        }

        [Test]
        public void Avatar_PJ01_EsHumanoide_ConLosHuesosDelTorsoYLosBrazosMapeados()
        {
            GameObject go = Instancia(out Animator animador);
            try
            {
                Assert.IsTrue(animador.avatar.isValid && animador.isHuman);
                string[] esperado =
                {
                    "Hip=Hips", "Waist=Spine", "Spine01=Chest", "Spine02=UpperChest", "Head=Head",
                    "L_Clavicle=LeftShoulder", "L_Upperarm=LeftUpperArm", "L_Forearm=LeftLowerArm", "L_Hand=LeftHand",
                    "R_Clavicle=RightShoulder", "R_Upperarm=RightUpperArm", "R_Forearm=RightLowerArm", "R_Hand=RightHand",
                };
                foreach (string par in esperado)
                {
                    string[] p = par.Split('=');
                    Transform hueso = animador.GetBoneTransform((HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones), p[1]));
                    Assert.IsNotNull(hueso, p[1] + " sin hueso");
                    Assert.AreEqual(p[0], hueso.name, "El hueso humanoide " + p[1]);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Importador_PJ01_SinTranslationDoF_ConLosTwistPorDefecto_YSinClipsPropios()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaPJ01);
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType);
            HumanDescription d = imp.humanDescription;
            Assert.IsFalse(d.hasTranslationDoF, "Translation DoF apagado");
            Assert.AreEqual(0.5f, d.upperArmTwist, 0.001f, "Upper Arm Twist");
            Assert.AreEqual(0.5f, d.lowerArmTwist, 0.001f, "Forearm Twist");
            Assert.AreEqual(0f, d.feetSpacing, 0.001f, "Feet Spacing");
        }

        [Test]
        public void Avatar_PJ01_LaPoseDeReferenciaEsUnaTConLosBrazosHorizontales()
        {
            GameObject go = Instancia(out Animator animador);
            try
            {
                foreach (var (superior, inferior, signo) in new[] { (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 1f), (HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, -1f) })
                {
                    Vector3 dir = (animador.GetBoneTransform(inferior).position - animador.GetBoneTransform(superior).position).normalized;
                    Assert.Greater(dir.x * signo, 0.95f, superior + " apunta hacia afuera (T)");
                    Assert.Less(Mathf.Abs(dir.y), 0.3f, superior + " casi horizontal (T)");
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Controlador_LaCapaArriba_LlevaCuerpoCabezaYBrazos_YLaDanoEsAditiva()
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            AnimatorControllerLayer arriba = null, dano = null;
            foreach (AnimatorControllerLayer capa in c.layers)
            {
                if (capa.name == ReglasAnimacion.CapaArriba) { arriba = capa; }
                if (capa.name == ReglasAnimacion.CapaDano) { dano = capa; }
            }
            Assert.IsNotNull(arriba);
            Assert.IsNotNull(dano);
            foreach (AvatarMaskBodyPart parte in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm })
            {
                Assert.IsTrue(arriba.avatarMask.GetHumanoidBodyPartActive(parte), "La capa Arriba lleva " + parte);
            }
            Assert.IsFalse(arriba.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), "Las piernas son de la capa base");
            Assert.AreEqual(AnimatorLayerBlendingMode.Additive, dano.blendingMode);
        }

        [Test]
        public void Jugador_AlConfigurarLosClips_ApuntadoIKNoEscribeNingunHueso()
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual));
            try
            {
                var visual = go.GetComponent<VisualPersonaje>();
                var ik = go.GetComponentInChildren<ApuntadoIK>(true);
                Assert.IsNotNull(ik);
                AnimacionJugador.ClipsDeTony(visual);
                Assert.IsTrue(ik.ClipPuro, "El jugador apunta con el clip puro: sin torso girado, sin mano derecha corregida, sin mano izquierda por IK");
                Assert.IsFalse(go.GetComponentInChildren<IKPiesYMirada>(true).Pies, "Sin IK de pies encima del clip");
                Assert.IsNotNull(go.GetComponentInChildren<AgarrePistolaPJ01>(true), "T-738: PJ01 lleva la compensacion del agarre de la pistola (no toca huesos)");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
