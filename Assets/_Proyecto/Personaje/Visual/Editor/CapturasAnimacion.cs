using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-561: hojas de capturas de los clips del controlador base, un personaje por pose sobre un piso de prueba (docs/capturas/anim-*.png).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CapturasAnimacion.Todas
    /// </summary>
    public static class CapturasAnimacion
    {
        private const float Separacion = 1.7f;

        public static void Todas()
        {
            Hoja("anim-locomocion", new (string, float)[]
            {
                ("Loco_Idle", 0.2f), ("Loco_Caminar", 0.25f), ("Loco_Correr", 0.3f), ("Loco_CaminarDer", 0.25f), ("Loco_SaltoAire", 0.5f), ("Loco_Caida", 0.4f),
            });
            Hoja("anim-pistola", new (string, float)[]
            {
                ("Pistola_Idle", 0.3f), ("Pistola_Apuntar", 0.3f), ("Pistola_Disparo", 0.2f), ("Pistola_Recargar", 0.5f),
                ("Pistola_ApuntarCaminar_Derecha", 0.3f), ("Pistola_ApuntarCaminar_Izquierda", 0.3f),
            });
            Hoja("anim-rifle", new (string, float)[]
            {
                ("Rifle_Idle", 0.3f), ("Rifle_Apuntar", 0.3f), ("Rifle_ApuntarCaminar", 0.3f), ("Rifle_DisparoCaminar", 0.2f),
                ("Rifle_Recargar", 0.5f), ("Rifle_AgachadoCaminar", 0.3f),
            });
            Hoja("anim-cobertura-punos", new (string, float)[]
            {
                ("Cobertura_Entrar", 1f), ("Cobertura_Disparo", 0.5f), ("Punos_Guardia", 0.3f), ("Punos_GolpeIzq", 0.4f),
                ("Punos_GolpeDer", 0.4f), ("Punos_Fuerte", 0.5f),
            });
            Hoja("anim-dano-muerte", new (string, float)[]
            {
                ("Dano_Frente", 0.4f), ("Dano_Espalda", 0.4f), ("Muerte_Adelante", 1f), ("Muerte_Atras", 1f),
                ("Muerte_Desmayo", 1f), ("Dano_Levantarse_BocaArriba", 0.5f),
            });
            Debug.Log("[Personaje] T-561: capturas guardadas en docs/capturas/anim-*.png");
        }

        /// <summary>Un personaje por clip, cada uno muestreado en la fracción dada del clip (0 a 1), de izquierda a derecha.</summary>
        private static void Hoja(string nombre, (string clip, float fraccion)[] poses)
        {
            CapturaPersonajes.EscenaDePrueba();
            string[] modelos = { "woman", "man", "woman2" };
            for (int i = 0; i < poses.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(modelos[i % modelos.Length]));
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Animator a = inst.GetComponentInChildren<Animator>();
                a.enabled = false;
                AnimationClip clip = ConstructorControladorBase.Clip(poses[i].clip);
                clip.SampleAnimation(a.gameObject, clip.length * poses[i].fraccion);
                // Se centra la cadera en su lugar de la hoja (el muestreo puede traer el avance del clip).
                Vector3 meta = new Vector3(i * Separacion, 0f, 0f);
                Vector3 cadera = a.GetBoneTransform(HumanBodyBones.Hips).position;
                inst.transform.position += new Vector3(meta.x - cadera.x, 0f, meta.z - cadera.z);
            }
            float centro = (poses.Length - 1) * Separacion * 0.5f;
            Camera c = CapturaPersonajes.ColocarCamara(new Vector3(centro, 0.15f, 0f), new Vector3(0f, 0f, -1f), 12.5f, 1.0f, 30f);
            CapturaPersonajes.Guardar(c, "docs/capturas/" + nombre + ".png");
        }
    }
}
