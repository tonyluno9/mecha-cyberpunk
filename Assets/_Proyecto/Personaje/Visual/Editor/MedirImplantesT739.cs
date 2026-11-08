using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-739: mide los clips de caminar y correr con y sin implante: escala del avatar (humanScale, altura de la cadera), duración, bucle, cuánto se diferencian de Loco_Caminar y Loco_Correr
    /// sobre PJ01 y velocidad con la que avanza el pie apoyado (MedicionClips). Escribe Logs/medicion-t739.txt.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.MedirImplantesT739.Medir
    /// </summary>
    public static class MedirImplantesT739
    {
        private const string Carpeta = "Assets/Arte/Personajes/Animaciones/Locomocion/";
        private const string RutaPJ01 = "Assets/Arte/Personajes/PJ01/PJ01.fbx";

        public static void Medir()
        {
            var sb = new StringBuilder();
            string[] nombres = { "Loco_Caminar", "Loco_Caminar_SinImplante", "Loco_Caminar_ConImplante", "Loco_Correr", "Loco_Correr_SinImplante", "Loco_Correr_ConImplante" };
            foreach (string n in nombres)
            {
                string ruta = Carpeta + n + ".fbx";
                AnimationClip clip = Clip(ruta);
                Avatar av = null;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta)) { if (o is Avatar a) { av = a; } }
                sb.AppendLine(n + ": duracion=" + clip.length.ToString("F3") + " loop=" + clip.isLooping);
                GameObject go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
                Transform hips = null;
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) { if (t.name.EndsWith(":Hips")) { hips = t; } }
                Animator an = go.GetComponent<Animator>();
                sb.AppendLine("   avatar.isHuman=" + (av != null && av.isHuman) + " cadera(local)=" + (hips != null ? hips.localPosition.ToString("F3") : "-") + " lossyScaleRaiz=" + go.transform.lossyScale.x);
                Object.DestroyImmediate(go);
            }
            foreach (string n in nombres) { sb.AppendLine("velocidad " + n + " = " + MedicionClips.Medir(n).ToString("F3") + " m/s"); }
            foreach ((string a, string b) in new[] { ("Loco_Caminar", "Loco_Caminar_ConImplante"), ("Loco_Caminar_SinImplante", "Loco_Caminar_ConImplante"), ("Loco_Correr_SinImplante", "Loco_Correr_ConImplante"), ("Loco_Correr", "Loco_Correr_ConImplante"), ("Loco_Caminar", "Loco_Caminar_SinImplante"), ("Loco_Correr", "Loco_Correr_SinImplante") })
            {
                sb.AppendLine("diferencia en PJ01 " + a + " contra " + b + ": " + Diferencia(Clip(Carpeta + a + ".fbx"), Clip(Carpeta + b + ".fbx")));
            }
            System.IO.File.WriteAllText("Logs/medicion-t739.txt", sb.ToString());
        }

        private static AnimationClip Clip(string ruta)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta)) { if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { return c; } }
            return null;
        }

        // Maximo giro local (grados) de los huesos humanoides entre dos clips en 8 momentos del ciclo, sobre PJ01.
        private static string Diferencia(AnimationClip a, AnimationClip b)
        {
            GameObject go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPJ01));
            try
            {
                Animator an = go.GetComponent<Animator>();
                if (an == null) { an = go.AddComponent<Animator>(); }
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaPJ01)) { if (o is Avatar av) { an.avatar = av; } }
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                float max = 0f; string donde = "";
                for (int i = 0; i < 8; i++)
                {
                    float f = i / 8f;
                    var rot = new Quaternion[(int)HumanBodyBones.LastBone];
                    Evaluar(an, a, f * a.length);
                    for (int h = 0; h < rot.Length; h++) { Transform t = an.GetBoneTransform((HumanBodyBones)h); if (t != null) { rot[h] = t.localRotation; } }
                    Evaluar(an, b, f * b.length);
                    for (int h = 0; h < rot.Length; h++)
                    {
                        Transform t = an.GetBoneTransform((HumanBodyBones)h);
                        if (t == null) { continue; }
                        float g = Quaternion.Angle(rot[h], t.localRotation);
                        if (g > max) { max = g; donde = ((HumanBodyBones)h).ToString() + "@" + f.ToString("F2"); }
                    }
                }
                return "max " + max.ToString("F2") + " grados (" + donde + ") duraciones " + a.length.ToString("F3") + "/" + b.length.ToString("F3");
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void Evaluar(Animator an, AnimationClip clip, float tiempo)
        {
            PlayableGraph g = PlayableGraph.Create("T739");
            g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput salida = AnimationPlayableOutput.Create(g, "s", an);
            AnimationClipPlayable p = AnimationClipPlayable.Create(g, clip);
            p.SetTime(tiempo);
            salida.SetSourcePlayable(p);
            g.Evaluate(0f);
            g.Destroy();
        }
    }
}
