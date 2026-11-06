using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-562: mide a qué velocidad (m/s, del clip tal como viene, a escala 1) avanza el cuerpo en un clip de locomoción In Place.
    /// El pie que está apoyado (el más bajo, a menos de 5 cm del punto más bajo del ciclo) se mueve hacia atrás respecto de la cadera
    /// justo a la velocidad a la que el cuerpo avanza; se promedia solo en esos cuadros.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.MedicionClips.Imprimir
    /// </summary>
    public static class MedicionClips
    {
        private const float Paso = 1f / 60f;
        private const float MargenApoyo = 0.05f;

        [MenuItem("Mecha Cyberpunk/Personajes/T-562 Medir clips de locomoción")]
        public static void Imprimir()
        {
            foreach (string clip in new[] { "Loco_Caminar", "Loco_Correr", "Loco_CaminarAtras", "Loco_CaminarDer", "Loco_CaminarIzq" })
            {
                Debug.Log("[Personaje] T-562: " + clip + " avanza a " + Medir(clip).ToString("F2") + " m/s.");
            }
        }

        public static float Medir(string clip)
        {
            AnimationClip c = ConstructorControladorBase.Clip(clip);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("woman")));
            try
            {
                Animator a = modelo.GetComponentInChildren<Animator>();
                a.enabled = false;
                Transform[] pies = { a.GetBoneTransform(HumanBodyBones.LeftFoot), a.GetBoneTransform(HumanBodyBones.RightFoot) };
                Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
                var rel = new List<Vector3[]>();
                float minimo = float.MaxValue;
                for (float t = 0f; t <= c.length; t += Paso)
                {
                    c.SampleAnimation(a.gameObject, t);
                    var cuadro = new Vector3[2];
                    for (int i = 0; i < 2; i++)
                    {
                        cuadro[i] = pies[i].position - cadera.position + new Vector3(0f, cadera.position.y, 0f);
                        minimo = Mathf.Min(minimo, cuadro[i].y);
                    }
                    rel.Add(cuadro);
                }
                float suma = 0f;
                int n = 0;
                for (int k = 1; k < rel.Count; k++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (rel[k][i].y > minimo + MargenApoyo || rel[k - 1][i].y > minimo + MargenApoyo)
                        {
                            continue;
                        }
                        Vector3 d = rel[k][i] - rel[k - 1][i];
                        suma += new Vector2(d.x, d.z).magnitude / Paso;
                        n++;
                    }
                }
                // El clip "tal como viene" (escala 1): la escala del Modelo (T-670) agranda el paso y VisualPersonaje la aplica al ajustar la rapidez.
                float escala = Mathf.Max(0.1f, a.transform.lossyScale.y);
                return n > 0 ? suma / n / escala : 0f;
            }
            finally
            {
                Object.DestroyImmediate(modelo);
            }
        }
    }
}
