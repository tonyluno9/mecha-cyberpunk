using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-707: la patrulla de la persecución es un carro de verdad. Rehace Trafico/Prefabs/Patrulla.prefab con el cuerpo, los colliders, el asiento, la cámara y el
    /// resto de Vehiculos/Prefabs/Carro_V05.prefab (la patrulla manejable), en cuerpo cinemático como los carros del tráfico; conserva en la raíz el
    /// <see cref="PatrullaMundo"/> (y su referencia desde Barrio.prefab) y las luces de la sirena, que suben al techo del modelo.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPatrullaT707.Crear
    /// </summary>
    public static class CreadorPatrullaT707
    {
        public const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        public const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab";
        private const float AlturaRaizCarroReal = 0.3f;
        private const float SubidaLuces = 0.25f;

        [MenuItem("Mecha Cyberpunk/T-707/Rehacer patrulla como carro")]
        public static void Crear()
        {
            GameObject origen = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            if (origen == null)
            {
                Debug.LogError("[Trafico] T-707: falta " + RutaCarro);
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPatrulla);
            try
            {
                if (raiz.GetComponent<Rigidbody>() != null)
                {
                    Debug.Log("[Trafico] T-707: la patrulla ya es un carro; no se rehace.");
                    return;
                }
                GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(origen, raiz.scene);
                PrefabUtility.UnpackPrefabInstance(copia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // Se queda lo que es de la persecución (luces de la sirena); lo demás era el cuerpo de cubos sin colliders.
                List<Transform> viejos = raiz.transform.Cast<Transform>().Where(t => t.name != "LuzRoja" && t.name != "LuzAzul").ToList();
                foreach (Transform viejo in viejos)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                foreach (Transform hijo in copia.transform.Cast<Transform>().ToList())
                {
                    hijo.SetParent(raiz.transform, false);
                }

                // Los componentes de la raíz del carro pasan a la raíz de la patrulla, con sus referencias apuntando a la nueva raíz.
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
                    Reapuntar(c, copia, raiz);
                }
                Object.DestroyImmediate(copia);

                Rigidbody cuerpo = raiz.GetComponent<Rigidbody>();
                cuerpo.isKinematic = false; // T-986: maneja con la física del carro
                cuerpo.interpolation = RigidbodyInterpolation.Interpolate;

                PatrullaMundo patrulla = raiz.GetComponent<PatrullaMundo>();
                SerializedObject so = new SerializedObject(patrulla);
                so.FindProperty("alturaRaiz").floatValue = AlturaRaizCarroReal;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Las luces de la sirena van sobre el techo del modelo.
                Bounds b = Limites(raiz);
                foreach (string luz in new[] { "LuzRoja", "LuzAzul" })
                {
                    Transform l = raiz.transform.Find(luz);
                    if (l != null)
                    {
                        Vector3 mundo = l.position;
                        l.position = new Vector3(mundo.x, b.max.y + SubidaLuces, mundo.z);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPatrulla);
                Debug.Log("[Trafico] T-707: Patrulla.prefab es un carro (" + b.size.x.ToString("F1") + " x " + b.size.y.ToString("F1") + " x " + b.size.z.ToString("F1")
                    + " m) con " + raiz.GetComponentsInChildren<Collider>(true).Length + " colliders.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        // Toda referencia a un componente de la raíz vieja pasa al componente del mismo tipo de la raíz nueva.
        private static void Reapuntar(Component componente, GameObject viejaRaiz, GameObject nuevaRaiz)
        {
            SerializedObject so = new SerializedObject(componente);
            SerializedProperty p = so.GetIterator();
            bool entrar = true;
            while (p.NextVisible(entrar))
            {
                entrar = true;
                if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null)
                {
                    continue;
                }
                Object objetivo = p.objectReferenceValue;
                if (objetivo is Component otro && otro.gameObject == viejaRaiz)
                {
                    Component equivalente = nuevaRaiz.GetComponent(otro.GetType());
                    if (equivalente != null)
                    {
                        p.objectReferenceValue = equivalente;
                    }
                }
                else if (objetivo is GameObject go && go == viejaRaiz)
                {
                    p.objectReferenceValue = nuevaRaiz;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds Limites(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.zero);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
