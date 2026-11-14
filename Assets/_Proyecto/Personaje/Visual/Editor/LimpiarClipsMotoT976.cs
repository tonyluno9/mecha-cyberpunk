using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-976: PJ01_Moto_Subir y PJ01_Moto_Bajar (Blender, BITACORA 02: "pendiente el final de la moto") traen un cuadro malo cuando la pierna derecha pasa sobre el asiento
    /// (Subir al 40 %, Bajar al 60 %): el pie salta ~25 cm de lado y regresa al cuadro siguiente. Esto hace una copia .anim de cada clip, cuadro por cuadro, en la que los cuadros
    /// que se salen de la línea de sus vecinos se cambian por el promedio de esos vecinos. No se anima nada nuevo: el resto del clip queda igual.
    /// La copia se llama igual que el clip (así PoseMoto y el controlador no cambian) y ConstructorControladorBase.Clip la prefiere a la del FBX.
    /// Se puede repetir. Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.LimpiarClipsMotoT976.Aplicar
    /// </summary>
    public static class LimpiarClipsMotoT976
    {
        public const string CarpetaLimpios = ConstructorControladorBase.CarpetaClipsPJ01 + "/Limpios";
        public static readonly string[] Clips = { "PJ01_Moto_Subir", "PJ01_Moto_Bajar" };

        /// <summary>Cambio de un músculo de un cuadro al siguiente (normalizado, 1 = todo su rango) que ya no es movimiento sino cambio de rama. Lo más rápido de
        /// verdad en estos clips cambia menos de 0.4 por cuadro.</summary>
        public const float UmbralSalto = 0.6f;

        [MenuItem("Mecha Cyberpunk/T-976/Limpiar clips de la moto")]
        public static void Aplicar()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaLimpios))
            {
                AssetDatabase.CreateFolder(ConstructorControladorBase.CarpetaClipsPJ01, "Limpios");
            }
            foreach (string nombre in Clips)
            {
                AnimationClip original = ClipDelFbx(nombre);
                if (original == null)
                {
                    Debug.LogError("[Personaje] T-976: falta el clip " + nombre);
                    continue;
                }
                Limpiar(original, CarpetaLimpios + "/" + nombre + ".anim");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ConstructorControladorBase.Construir();
        }

        private static AnimationClip ClipDelFbx(string nombre)
        {
            string ruta = ConstructorControladorBase.CarpetaClipsPJ01 + "/" + nombre + ".fbx";
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is AnimationClip c && c.name == nombre)
                {
                    return c;
                }
            }
            return null;
        }

        private static void Limpiar(AnimationClip original, string ruta)
        {
            float fps = original.frameRate > 0f ? original.frameRate : 30f;
            int cuadros = Mathf.RoundToInt(original.length * fps) + 1;
            EditorCurveBinding[] enlaces = AnimationUtility.GetCurveBindings(original);
            var valores = new float[enlaces.Length][];
            for (int c = 0; c < enlaces.Length; c++)
            {
                AnimationCurve curva = AnimationUtility.GetEditorCurve(original, enlaces[c]);
                valores[c] = new float[cuadros];
                for (int k = 0; k < cuadros; k++)
                {
                    valores[c][k] = curva.Evaluate(Mathf.Min(original.length, k / fps));
                }
            }

            // Por pierna: entre dos cuadros seguidos algún músculo cambia de golpe (el giro del muslo pasa a la otra "rama" del humanoide). Cada cuadro es una
            // postura buena, pero lo que Unity interpola entre los dos no (el pie sale volando): en ese paso la pierna salta de un cuadro al otro sin mezclar.
            var escalon = new bool[enlaces.Length][];
            var cambios = new List<string>();
            foreach (string lado in new[] { "Right", "Left" })
            {
                for (int k = 0; k < cuadros - 1; k++)
                {
                    bool salta = false;
                    for (int c = 0; c < enlaces.Length && !salta; c++)
                    {
                        salta = EsMusculo(enlaces[c]) && DeLaPierna(enlaces[c], lado) && Mathf.Abs(valores[c][k + 1] - valores[c][k]) > UmbralSalto;
                    }
                    if (!salta)
                    {
                        continue;
                    }
                    for (int c = 0; c < enlaces.Length; c++)
                    {
                        if (DeLaPierna(enlaces[c], lado))
                        {
                            escalon[c] = escalon[c] ?? new bool[cuadros];
                            escalon[c][k] = true;
                        }
                    }
                    cambios.Add(lado + " " + k + "-" + (k + 1));
                }
            }

            var limpio = new AnimationClip { frameRate = fps };
            for (int c = 0; c < enlaces.Length; c++)
            {
                var claves = new Keyframe[cuadros];
                for (int k = 0; k < cuadros; k++)
                {
                    claves[k] = new Keyframe(Mathf.Min(original.length, k / fps), valores[c][k]);
                }
                var curva = new AnimationCurve(claves);
                for (int k = 0; k < cuadros; k++)
                {
                    bool saleEnEscalon = escalon[c] != null && escalon[c][k];
                    bool entraEnEscalon = escalon[c] != null && k > 0 && escalon[c][k - 1];
                    AnimationUtility.SetKeyLeftTangentMode(curva, k, entraEnEscalon ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curva, k, saleEnEscalon ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(limpio, enlaces[c], curva);
            }
            AnimationUtility.SetAnimationClipSettings(limpio, AnimationUtility.GetAnimationClipSettings(original));
            limpio.name = original.name;

            AnimationClip viejo = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
            if (viejo != null)
            {
                EditorUtility.CopySerialized(limpio, viejo);
                viejo.name = original.name;
                EditorUtility.SetDirty(viejo);
            }
            else
            {
                AssetDatabase.CreateAsset(limpio, ruta);
            }
            Debug.Log("[Personaje] T-976: " + original.name + " (" + cuadros + " cuadros) limpio en " + ruta + "; pasos sin mezcla: "
                + (cambios.Count == 0 ? "ninguno" : string.Join(", ", cambios)) + ".");
        }

        // Curvas de una pierna: sus músculos y su meta de IK (RightFootT.x, RightFootQ.w...).
        private static bool DeLaPierna(EditorCurveBinding enlace, string lado)
        {
            string n = enlace.propertyName;
            return enlace.type == typeof(Animator) && (n.StartsWith(lado + " Upper Leg") || n.StartsWith(lado + " Lower Leg") || n.StartsWith(lado + " Foot")
                || n.StartsWith(lado + " Toes") || n.StartsWith(lado + "Foot"));
        }

        // Los músculos del humanoide (no la raíz ni las metas de IK, que se mueven en metros).
        private static bool EsMusculo(EditorCurveBinding enlace)
        {
            return enlace.type == typeof(Animator) && !enlace.propertyName.StartsWith("Root") && !enlace.propertyName.StartsWith("Motion")
                && !enlace.propertyName.Contains("T.") && !enlace.propertyName.Contains("Q.");
        }

        /// <summary>La copia limpia de <paramref name="nombre"/>, si existe.</summary>
        public static AnimationClip Limpio(string nombre)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(CarpetaLimpios + "/" + nombre + ".anim");
        }
    }
}
