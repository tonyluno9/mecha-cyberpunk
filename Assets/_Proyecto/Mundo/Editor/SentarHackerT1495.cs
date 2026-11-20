using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1495: SERGIO (el hacker, Apoyo_SERGIO) sentado de verdad en la silla I12 más cercana al escritorio I02 del casino (T930_Casino) en Ciudad_Prologo.
    /// No hay clip de sentado en el proyecto: sigue la pose por huesos de PoseSentado (teclea con vaivén de manos). Mueve la raíz del apoyo para que
    /// su visual quede sobre la silla (la zona de interacción lo acompaña), lo gira hacia el escritorio y sube la cadera al asiento de la silla.
    /// Se puede repetir. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.SentarHackerT1495.Aplicar
    /// </summary>
    public static class SentarHackerT1495
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizCasino = "T930_Casino";
        public const string NombreHacker = "Apoyo_SERGIO";
        public const string NombreSilla = "I12";
        public const string NombreEscritorio = "I02";
        /// <summary>Altura del asiento del modelo I12 sobre su base (m, sin escala).</summary>
        public const float AsientoModelo = 0.62f;
        /// <summary>Cuánto queda la cadera sobre el asiento (m).</summary>
        public const float CaderaSobreAsiento = 0.12f;

        [MenuItem("Mecha Cyberpunk/Mundo/T-1495 Sentar al hacker")]
        public static void Aplicar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject casino = GameObject.Find(RaizCasino);
            GameObject sergio = GameObject.Find(NombreHacker);
            if (casino == null || sergio == null)
            {
                throw new System.Exception("T-1495: falta " + (casino == null ? RaizCasino : NombreHacker) + " en " + RutaEscena);
            }
            Transform escritorio = casino.transform.Cast<Transform>().FirstOrDefault(t => t.name == NombreEscritorio);
            Transform[] sillas = casino.transform.Cast<Transform>().Where(t => t.name == NombreSilla).ToArray();
            if (escritorio == null || sillas.Length == 0)
            {
                throw new System.Exception("T-1495: falta el escritorio " + NombreEscritorio + " o la silla " + NombreSilla + " en " + RaizCasino);
            }
            Transform silla = sillas.OrderBy(s => Plano(s.position - escritorio.position).sqrMagnitude).First();
            Transform visual = sergio.transform.Find("Visual");
            if (visual == null)
            {
                throw new System.Exception("T-1495: Apoyo_SERGIO no tiene hijo Visual");
            }

            // La raíz se mueve lo que haga falta para que el visual quede sobre la silla (misma altura que su base).
            Vector3 destino = silla.position;
            sergio.transform.position += destino - visual.position;
            // El frente del visual es el de la mesa (PoseSentado pone las piernas hacia visual.forward).
            Vector3 haciaMesa = Plano(escritorio.position - silla.position);
            if (haciaMesa.sqrMagnitude > 1e-6f)
            {
                visual.rotation = Quaternion.LookRotation(haciaMesa.normalized, Vector3.up);
            }
            // La cadera va un poco sobre el asiento de esta silla (con su escala).
            float altura = AsientoModelo * silla.lossyScale.y + CaderaSobreAsiento;
            bool pose = false;
            foreach (MonoBehaviour m in visual.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (m != null && m.GetType().Name == "PoseSentado")
                {
                    SerializedObject so = new SerializedObject(m);
                    so.FindProperty("alturaCadera").floatValue = altura;
                    so.ApplyModifiedProperties();
                    pose = true;
                }
            }
            if (!pose)
            {
                throw new System.Exception("T-1495: el visual de SERGIO no tiene PoseSentado");
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[Mundo] T-1495: SERGIO sentado en la silla " + silla.position + " (cadera a " + altura.ToString("F2") + " m).");
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
