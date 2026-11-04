using MechaCyberpunk.Progresion.Prologo.Clinica;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-586: el texto del doctor sin deuda y el punto de despertar (Clinica_Camilla) dentro del consultorio de I+D+I, junto al Doc
    /// (antes quedaba en el cuarto viejo, a 80 m). Idempotente. Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.AjustarT586.Ajustar
    /// </summary>
    public static class AjustarT586
    {
        private const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Base.asset";
        private const string RutaDatosPandillas = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Pandillas.asset";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaPantallas = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string NombreCamilla = "Clinica_Camilla";
        private const string TextoSinFondos = "No traes todo, mijo. Hoy te lo perdono.";

        // Dentro del consultorio, a ~1.2 m del Doc (-100.3, -13.4) y de frente a él; sin choque con los muebles (medido en vivo).
        private static readonly Vector3 PosicionDespertar = new Vector3(-100f, 0.24f, -12.2f);
        private const float GiroDespertar = 194f;

        [MenuItem("Mecha Cyberpunk/Ajustar T-586 despertar en I+D+I")]
        public static void Ajustar()
        {
            foreach (string ruta in new[] { RutaDatos, RutaDatosPandillas })
            {
                DatosDoctor datos = AssetDatabase.LoadAssetAtPath<DatosDoctor>(ruta);
                SerializedObject so = new SerializedObject(datos);
                so.FindProperty("textoDeuda").stringValue = TextoSinFondos;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
            }
            AssetDatabase.SaveAssets();

            // El doctor sin fundido propio (la ciudad lo trae dentro del DirectorPrologo apagado): le damos las pantallas de historia.
            GameObject contenido = PrefabUtility.LoadPrefabContents(CreadorDoctorClinica.RutaPrefab);
            SerializedObject soDoctor = new SerializedObject(contenido.GetComponent<DoctorClinica>());
            soDoctor.FindProperty("prefabPantallas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallas);
            soDoctor.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contenido, CreadorDoctorClinica.RutaPrefab);
            PrefabUtility.UnloadPrefabContents(contenido);

            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Transform camilla = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == NombreCamilla)
                    {
                        camilla = t;
                    }
                }
            }
            if (camilla == null)
            {
                Debug.LogError("[Progresion] T-586: no encuentro " + NombreCamilla + " en " + RutaEscena);
                return;
            }
            camilla.SetPositionAndRotation(PosicionDespertar, Quaternion.Euler(0f, GiroDespertar, 0f));
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Progresion] T-586: " + NombreCamilla + " en " + PosicionDespertar);
        }
    }
}
