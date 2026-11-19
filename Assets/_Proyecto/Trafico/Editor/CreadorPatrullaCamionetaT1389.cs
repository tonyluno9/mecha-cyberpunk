using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1389a: la camioneta de policía (Carro_V11) como patrulla extra de los 5 estrellas. Copia Patrulla.prefab a PatrullaCamioneta.prefab,
    /// le quita el cuerpo del V05 (conserva PatrullaMundo y las luces de la sirena) y le pone el de Carro_V11, igual que T-707. Después conecta
    /// <c>GestorPersecucion.prefabCamioneta</c> del Barrio.prefab. Correrlo otra vez rehace el prefab.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPatrullaCamionetaT1389.Crear
    /// </summary>
    public static class CreadorPatrullaCamionetaT1389
    {
        public const string RutaCamioneta = "Assets/_Proyecto/Trafico/Prefabs/PatrullaCamioneta.prefab";
        public const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V11.prefab";
        public const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const float AlturaRaizCarroReal = 0.3f;
        private const float SubidaLuces = 0.25f;

        [MenuItem("Mecha Cyberpunk/T-1389a/Patrulla camioneta V11")]
        public static string Crear()
        {
            GameObject origen = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            if (origen == null)
            {
                return "FALTA " + RutaCarro + " (arma primero Carro_V11)";
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPatrullaT707.RutaPatrulla) == null)
            {
                return "FALTA " + CreadorPatrullaT707.RutaPatrulla;
            }
            AssetDatabase.DeleteAsset(RutaCamioneta);
            if (!AssetDatabase.CopyAsset(CreadorPatrullaT707.RutaPatrulla, RutaCamioneta))
            {
                return "No se pudo copiar Patrulla.prefab";
            }
            string resumen;
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaCamioneta);
            try
            {
                // Fuera el cuerpo del V05 (todo menos las luces) y los componentes de la raíz que venían de él (menos PatrullaMundo).
                foreach (Transform viejo in raiz.transform.Cast<Transform>().Where(t => t.name != "LuzRoja" && t.name != "LuzAzul").ToList())
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                List<Component> sobran = raiz.GetComponents<Component>().Where(c => !(c is Transform) && !(c is PatrullaMundo)).ToList();
                for (int pasada = 0; pasada < 4 && sobran.Count > 0; pasada++)
                {
                    for (int i = sobran.Count - 1; i >= 0; i--)
                    {
                        if (sobran[i] == null)
                        {
                            sobran.RemoveAt(i);
                        }
                        else if (CanDestroy(sobran[i]))
                        {
                            Object.DestroyImmediate(sobran[i]);
                            sobran.RemoveAt(i);
                        }
                    }
                }

                GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(origen, raiz.scene);
                PrefabUtility.UnpackPrefabInstance(copia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (Transform hijo in copia.transform.Cast<Transform>().ToList())
                {
                    hijo.SetParent(raiz.transform, false);
                }
                List<Component> nuevos = new List<Component>();
                foreach (Component c in copia.GetComponents<Component>())
                {
                    if (c is Transform || c.GetType().Name == "DanioCarro")
                    {
                        continue;
                    }
                    if (ComponentUtility.CopyComponent(c) && ComponentUtility.PasteComponentAsNew(raiz))
                    {
                        nuevos.Add(raiz.GetComponents<Component>().Last());
                    }
                }
                foreach (Component c in nuevos)
                {
                    CreadorPatrullaT707.Reapuntar(c, copia, raiz);
                }
                Object.DestroyImmediate(copia);

                Rigidbody cuerpo = raiz.GetComponent<Rigidbody>();
                cuerpo.isKinematic = false;
                cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
                SerializedObject so = new SerializedObject(raiz.GetComponent<PatrullaMundo>());
                so.FindProperty("alturaRaiz").floatValue = AlturaRaizCarroReal;
                so.ApplyModifiedPropertiesWithoutUndo();

                Bounds b = CreadorPatrullaT707.Limites(raiz);
                foreach (string luz in new[] { "LuzRoja", "LuzAzul" })
                {
                    Transform l = raiz.transform.Find(luz);
                    if (l != null)
                    {
                        l.position = new Vector3(l.position.x, b.max.y + SubidaLuces, l.position.z);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaCamioneta);
                resumen = "PatrullaCamioneta " + b.size.x.ToString("F1") + " x " + b.size.y.ToString("F1") + " x " + b.size.z.ToString("F1") + " m, "
                    + raiz.GetComponentsInChildren<Collider>(true).Length + " colliders";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            return resumen + "; " + ConectarEnBarrio();
        }

        private static bool CanDestroy(Component c)
        {
            // Un componente del que otro depende (RequireComponent) se salta hasta la siguiente pasada.
            foreach (Component otro in c.gameObject.GetComponents<Component>())
            {
                if (otro == null || otro == c)
                {
                    continue;
                }
                foreach (RequireComponent r in (RequireComponent[])System.Attribute.GetCustomAttributes(otro.GetType(), typeof(RequireComponent), true))
                {
                    if (r.m_Type0 == c.GetType() || r.m_Type1 == c.GetType() || r.m_Type2 == c.GetType())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Pone PatrullaCamioneta en <c>GestorPersecucion.prefabCamioneta</c> del Barrio.prefab.</summary>
        public static string ConectarEnBarrio()
        {
            PatrullaMundo camioneta = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCamioneta)?.GetComponent<PatrullaMundo>();
            if (camioneta == null)
            {
                return "sin PatrullaCamioneta que conectar";
            }
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaBarrio);
            try
            {
                GestorPersecucion gestor = barrio.GetComponentInChildren<GestorPersecucion>(true);
                if (gestor == null)
                {
                    return "Barrio.prefab no tiene GestorPersecucion";
                }
                SerializedObject so = new SerializedObject(gestor);
                so.FindProperty("prefabCamioneta").objectReferenceValue = camioneta;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(barrio, RutaBarrio);
                return "prefabCamioneta conectado en Barrio.prefab";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(barrio);
            }
        }
    }
}
