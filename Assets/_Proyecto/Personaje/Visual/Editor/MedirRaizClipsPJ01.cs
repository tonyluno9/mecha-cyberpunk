using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-962: cuánto avanza el cuerpo (RootT, en metros del avatar de PJ01) y cuánto gira en cada clip de golpes, muertes y levantarse de Blender, del primer al último cuadro
    /// y en su punto más lejano. Sirve para decidir entre Bake Into Pose y root motion. Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.MedirRaizClipsPJ01.Imprimir
    /// </summary>
    public static class MedirRaizClipsPJ01
    {
        public static readonly string[] Clips =
        {
            "PJ01_Punos_Guardia", "PJ01_Punos_GolpeIzq", "PJ01_Punos_GolpeDer", "PJ01_Punos_Fuerte",
            "PJ01_Muerte_Adelante", "PJ01_Muerte_Atras", "PJ01_Muerte_Desmayo",
            "PJ01_Dano_Levantarse_BocaArriba", "PJ01_Dano_Levantarse_BocaAbajo",
            "Muerte_Adelante", "Muerte_Atras", "Dano_Levantarse_BocaArriba",
        };

        public static void Imprimir()
        {
            foreach (string nombre in Clips)
            {
                AnimationClip c = ConstructorControladorBase.Clip(nombre);
                if (c == null) { continue; }
                Debug.Log("[Personaje] T-962 raíz " + nombre + ": " + Medir(c));
            }
            foreach (string nombre in new[] { "PJ01_Punos_GolpeIzq", "PJ01_Punos_GolpeDer", "PJ01_Punos_Fuerte", "Punos_GolpeIzq", "Punos_GolpeDer", "Punos_Fuerte" })
            {
                Debug.Log("[Personaje] T-962 alcance " + nombre + ": " + Alcance(ConstructorControladorBase.Clip(nombre)));
            }
        }

        /// <summary>T-963: largo de los clips de carro y moto y la cadera (respecto de la raíz del modelo) al inicio, a la mitad y al final.</summary>
        public static void CarroMoto()
        {
            foreach (string nombre in new[] { "PJ01_Carro_Entrar", "PJ01_Carro_Manejar", "PJ01_Carro_Salir", "PJ01_Moto_Subir", "PJ01_Moto_Manejar", "PJ01_Moto_Bajar",
                "Carro_Entrar", "Carro_Manejar_Bucle", "Carro_Salir" })
            {
                AnimationClip c = ConstructorControladorBase.Clip(nombre);
                if (c == null) { continue; }
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("PJ01")));
                Animator a = inst.GetComponentInChildren<Animator>();
                a.enabled = false;
                string texto = "dura " + c.length.ToString("F2") + " s, bucle " + c.isLooping + "; cadera";
                foreach (float f in new[] { 0f, 0.5f, 1f })
                {
                    c.SampleAnimation(a.gameObject, c.length * f);
                    Vector3 p = inst.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Hips).position);
                    Vector3 pie = inst.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.LeftFoot).position);
                    Vector3 mano = inst.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.LeftHand).position);
                    texto += " @" + f.ToString("F1") + " " + p.ToString("F2") + " pieI " + pie.ToString("F2") + " manoI " + mano.ToString("F2");
                }
                Debug.Log("[Personaje] T-963 " + nombre + ": " + texto);
                Object.DestroyImmediate(inst);
            }
        }

        /// <summary>El momento (s del clip) en que una mano llega más lejos al frente de la cadera, y cuánto.</summary>
        public static string Alcance(AnimationClip c)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("PJ01")));
            try
            {
                Animator a = inst.GetComponentInChildren<Animator>();
                a.enabled = false;
                float mejor = float.MinValue;
                float cuando = 0f;
                for (float t = 0f; t <= c.length; t += 1f / 60f)
                {
                    c.SampleAnimation(a.gameObject, t);
                    Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
                    foreach (HumanBodyBones mano in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
                    {
                        Vector3 d = a.GetBoneTransform(mano).position - cadera.position;
                        float frente = Vector3.Dot(d, inst.transform.forward);
                        if (frente > mejor) { mejor = frente; cuando = t; }
                    }
                }
                return "mano más al frente " + mejor.ToString("F2") + " m a los " + cuando.ToString("F2") + " s de " + c.length.ToString("F2") + " s";
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        public static string Medir(AnimationClip c)
        {
            AnimationCurve x = Curva(c, "RootT.x");
            AnimationCurve z = Curva(c, "RootT.z");
            if (x == null || z == null) { return "sin curvas RootT"; }
            Vector2 inicio = new Vector2(x.Evaluate(0f), z.Evaluate(0f));
            Vector2 fin = new Vector2(x.Evaluate(c.length), z.Evaluate(c.length));
            float lejos = 0f;
            for (float t = 0f; t <= c.length; t += 1f / 30f)
            {
                lejos = Mathf.Max(lejos, (new Vector2(x.Evaluate(t), z.Evaluate(t)) - inicio).magnitude);
            }
            return "dura " + c.length.ToString("F2") + " s, fin-inicio XZ (" + (fin - inicio).x.ToString("F2") + ", " + (fin - inicio).y.ToString("F2") + ") m, lo más lejos "
                + lejos.ToString("F2") + " m, averageSpeed " + c.averageSpeed.ToString("F2") + ", bucle " + c.isLooping;
        }

        private static AnimationCurve Curva(AnimationClip c, string propiedad)
        {
            foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(c))
            {
                if (b.propertyName == propiedad) { return AnimationUtility.GetEditorCurve(c, b); }
            }
            return null;
        }
    }
}
