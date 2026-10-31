using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-418 del creador de la ciudad de pandillas: conecta los profes (EL DOC, EL INVENTOR, EL HACKER) con el director de la historia y le pasa los
    /// grupos de SNAKES de Neón Alto, que quedan apagados hasta el juego libre. Idempotente: solo cambia referencias del director y apaga los grupos.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        [MenuItem("Mecha Cyberpunk/Pandillas/T-418 Historia de la demo")]
        public static void T418()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T418Interno();
            Guardar(escena);
        }

        private static void T418Interno()
        {
            DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>(FindObjectsInactive.Include);
            if (director == null)
            {
                Debug.LogError("[T-418] No hay DirectorPandillas en la escena: corre antes T406.");
                return;
            }

            // Los grupos de SNAKES: apagados en la escena; el director los prende en Libre.
            List<GameObject> grupos = new List<GameObject>();
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.Pandilla == IdPandilla.Viboras || g.Pandilla == IdPandilla.Nova)
                {
                    g.gameObject.SetActive(false);
                    grupos.Add(g.gameObject);
                }
            }

            Dictionary<string, PersonajeApoyo> profes = new Dictionary<string, PersonajeApoyo>();
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.Datos != null)
                {
                    profes[p.Datos.Id] = p;
                }
            }

            SerializedObject so = new SerializedObject(director);
            SerializedProperty lista = so.FindProperty("gruposNeon");
            lista.arraySize = grupos.Count;
            for (int i = 0; i < grupos.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = grupos[i];
            }
            string[] ids = { DirectorPandillas.IdDoc, DirectorPandillas.IdInventor, DirectorPandillas.IdHacker };
            string[] campos = { "doc", "inventor", "hacker" };
            for (int i = 0; i < ids.Length; i++)
            {
                profes.TryGetValue(ids[i], out PersonajeApoyo p);
                if (p == null)
                {
                    Debug.LogError("[T-418] Falta el profe " + ids[i] + ": corre antes T417.");
                }
                so.FindProperty(campos[i]).objectReferenceValue = p;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform casino = BuscarTransform("Lugar_Casino");
            profes.TryGetValue(DirectorPandillas.IdHacker, out PersonajeApoyo hacker);
            Debug.Log("[T-418] Director conectado: " + grupos.Count + " grupos de SNAKES apagados; VivaCoin en "
                + (casino != null ? casino.position.ToString() : "?") + ", EL HACKER en " + (hacker != null ? hacker.transform.position.ToString() : "?")
                + " (Neón Alto empieza en x=" + LimiteZonaX + ").");
        }
    }
}
