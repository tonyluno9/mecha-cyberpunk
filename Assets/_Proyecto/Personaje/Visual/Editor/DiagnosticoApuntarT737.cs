using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-737: mide el avatar Humanoid de PJ01 contra el de los clips mixamorig (pose de referencia, direcciones de brazos y columna, pose con músculos en cero y la pose
    /// de un clip). Escribe Logs/diagnostico-t737.txt. Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.DiagnosticoApuntarT737.Medir
    /// </summary>
    public static class DiagnosticoApuntarT737
    {
        private static readonly HumanBodyBones[] Huesos =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
        };

        public static void Medir()
        {
            var sb = new StringBuilder();
            Describir(sb, "PJ01", "Assets/Arte/Personajes/PJ01/PJ01.fbx");
            Describir(sb, "Pistola_Apuntar(mixamo)", "Assets/Arte/Personajes/Animaciones/Pistola/Pistola_Apuntar.fbx");
            Describir(sb, "Loco_Idle(mixamo)", "Assets/Arte/Personajes/Animaciones/Locomocion/Loco_Idle.fbx");
            System.IO.File.WriteAllText("Logs/diagnostico-t737.txt", sb.ToString());
            Debug.Log("T-737 diagnostico escrito");
        }

        private static void Describir(StringBuilder sb, string titulo, string ruta)
        {
            sb.AppendLine("=== " + titulo + " " + ruta);
            var imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            if (imp != null)
            {
                var d = imp.humanDescription;
                sb.AppendLine("importer: type=" + imp.animationType + " avatarSetup=" + imp.avatarSetup + " scale=" + imp.globalScale + " armTwist=" + d.upperArmTwist + " foreArmTwist=" + d.lowerArmTwist +
                    " legTwist=" + d.upperLegTwist + " lowerLegTwist=" + d.lowerLegTwist + " armStretch=" + d.armStretch + " legStretch=" + d.legStretch + " feetSpacing=" + d.feetSpacing +
                    " translationDoF=" + d.hasTranslationDoF + " clips=" + imp.defaultClipAnimations.Length + " sourceAvatar=" + (imp.sourceAvatar != null ? AssetDatabase.GetAssetPath(imp.sourceAvatar) : "-"));
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            GameObject go = (GameObject)Object.Instantiate(prefab);
            try
            {
                Animator a = go.GetComponent<Animator>();
                if (a == null) { a = go.AddComponent<Animator>(); }
                if (a.avatar == null) { foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta)) { if (o is Avatar av) { a.avatar = av; } } }
                sb.AppendLine("avatar isHuman=" + a.isHuman + " valid=" + a.avatar.isValid);
                sb.AppendLine("-- pose de importacion (mundo):");
                Volcar(sb, a);
                var handler = new HumanPoseHandler(a.avatar, a.transform);
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                float[] musculosImport = (float[])pose.muscles.Clone();
                var cero = new HumanPose { bodyPosition = pose.bodyPosition, bodyRotation = pose.bodyRotation, muscles = new float[pose.muscles.Length] };
                handler.SetHumanPose(ref cero);
                sb.AppendLine("-- musculos en cero (mundo):");
                Volcar(sb, a);
                float max = 0f; int quien = 0;
                for (int i = 0; i < musculosImport.Length; i++) { if (Mathf.Abs(musculosImport[i]) > max) { max = Mathf.Abs(musculosImport[i]); quien = i; } }
                sb.AppendLine("pose de importacion en musculos: maximo " + max.ToString("F3") + " en " + HumanTrait.MuscleName[quien]);
                for (int i = 0; i < musculosImport.Length; i++) { if (Mathf.Abs(musculosImport[i]) > 0.15f) { sb.AppendLine("   musculo " + HumanTrait.MuscleName[i] + " = " + musculosImport[i].ToString("F2")); } }
                handler.Dispose();
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void Volcar(StringBuilder sb, Animator a)
        {
            Transform cad = a.GetBoneTransform(HumanBodyBones.Hips);
            Quaternion giroCuerpo = Quaternion.Inverse(a.transform.rotation);
            foreach (HumanBodyBones h in Huesos)
            {
                Transform t = a.GetBoneTransform(h);
                if (t == null) { sb.AppendLine("  " + h + ": SIN HUESO"); continue; }
                Vector3 hijoDir = Vector3.zero;
                Transform sig = Siguiente(a, h);
                if (sig != null) { hijoDir = giroCuerpo * (sig.position - t.position).normalized; }
                sb.AppendLine("  " + h + " [" + t.name + "] alturaSobreCadera=" + (t.position.y - cad.position.y).ToString("F3") + " dirAlSiguiente=" + hijoDir.ToString("F2") +
                    " euler(mundo rel)=" + (giroCuerpo * t.rotation).eulerAngles.ToString("F0"));
            }
        }

        private static Transform Siguiente(Animator a, HumanBodyBones h)
        {
            switch (h)
            {
                case HumanBodyBones.LeftUpperArm: return a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                case HumanBodyBones.LeftLowerArm: return a.GetBoneTransform(HumanBodyBones.LeftHand);
                case HumanBodyBones.RightUpperArm: return a.GetBoneTransform(HumanBodyBones.RightLowerArm);
                case HumanBodyBones.RightLowerArm: return a.GetBoneTransform(HumanBodyBones.RightHand);
                case HumanBodyBones.LeftShoulder: return a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                case HumanBodyBones.RightShoulder: return a.GetBoneTransform(HumanBodyBones.RightUpperArm);
                case HumanBodyBones.Hips: return a.GetBoneTransform(HumanBodyBones.Spine);
                case HumanBodyBones.Spine: return a.GetBoneTransform(HumanBodyBones.Chest);
                case HumanBodyBones.Chest: return a.GetBoneTransform(HumanBodyBones.UpperChest) ?? a.GetBoneTransform(HumanBodyBones.Neck) ?? a.GetBoneTransform(HumanBodyBones.Head);
                case HumanBodyBones.UpperChest: return a.GetBoneTransform(HumanBodyBones.Neck) ?? a.GetBoneTransform(HumanBodyBones.Head);
                case HumanBodyBones.Neck: return a.GetBoneTransform(HumanBodyBones.Head);
                case HumanBodyBones.LeftUpperLeg: return a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                case HumanBodyBones.LeftLowerLeg: return a.GetBoneTransform(HumanBodyBones.LeftFoot);
            }
            return null;
        }
    }
}
