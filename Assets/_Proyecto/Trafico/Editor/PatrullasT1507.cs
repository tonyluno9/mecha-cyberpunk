using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1507: Patrulla.prefab (Carro_V05) y PatrullaCamioneta.prefab (Carro_V11) se rehacen en su mismo archivo (mismo GUID) con el cuerpo actual de su carro:
    /// carrocería en metros, puertas y cofre con bisagra, rin de policía. Conservan PatrullaMundo y las luces de la sirena. Correrlo otra vez deja lo mismo.
    /// Corre después de <c>CarrosRevisadosT1507.ArmarTodos</c>.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.PatrullasT1507.Rehacer
    /// </summary>
    public static class PatrullasT1507
    {
        public const string RutaPatrulla = CreadorPatrullaT707.RutaPatrulla;
        public const string RutaCamioneta = CreadorPatrullaCamionetaT1389.RutaCamioneta;
        private const float AlturaRaizCarroReal = 0.3f;
        private const float SubidaLuces = 0.25f;

        [MenuItem("Mecha Cyberpunk/T-1507/Rehacer patrullas")]
        public static void Rehacer()
        {
            AssetDatabase.Refresh();
            Debug.Log("[Trafico] T-1507: " + Rehacer(RutaPatrulla, CreadorPatrullaT707.RutaCarro));
            Debug.Log("[Trafico] T-1507: " + Rehacer(RutaCamioneta, CreadorPatrullaCamionetaT1389.RutaCarro));
            AssetDatabase.SaveAssets();
        }

        public static string Rehacer(string rutaPatrulla, string rutaCarro)
        {
            GameObject origen = AssetDatabase.LoadAssetAtPath<GameObject>(rutaCarro);
            if (origen == null || AssetDatabase.LoadAssetAtPath<GameObject>(rutaPatrulla) == null)
            {
                return "FALTA " + rutaCarro + " o " + rutaPatrulla;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPatrulla);
            try
            {
                foreach (Transform viejo in raiz.transform.Cast<Transform>().Where(t => t.name != "LuzRoja" && t.name != "LuzAzul").ToList())
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                List<Component> sobran = raiz.GetComponents<Component>().Where(c => !(c is Transform) && !(c is PatrullaMundo)).ToList();
                for (int pasada = 0; pasada < 5 && sobran.Count > 0; pasada++)
                {
                    for (int i = sobran.Count - 1; i >= 0; i--)
                    {
                        if (sobran[i] == null)
                        {
                            sobran.RemoveAt(i);
                        }
                        else if (SePuedeBorrar(sobran[i]))
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
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPatrulla);
                return rutaPatrulla + ": " + b.size.x.ToString("F2") + " x " + b.size.y.ToString("F2") + " x " + b.size.z.ToString("F2") + " m, puertas: "
                    + (raiz.GetComponent<MechaCyberpunk.Vehiculos.Carro.PuertasCarro>() != null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static bool SePuedeBorrar(Component c)
        {
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
    }
}
