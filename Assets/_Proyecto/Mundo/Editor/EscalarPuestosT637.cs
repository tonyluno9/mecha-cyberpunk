using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-637: pone a escala real (persona de 1.8 m) los puestos de tacos del kit que ya estan en las escenas (VestirCalle los crea
    /// ya con esa escala). Idempotente. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.EscalarPuestosT637.Aplicar
    /// </summary>
    public static class EscalarPuestosT637
    {
        private static readonly string[] Escenas =
        {
            "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
            "Assets/_Proyecto/Escenas/Ciudad.unity"
        };

        private static bool EsPuesto(Transform t)
        {
            string n = t.name;
            bool nombre = n.StartsWith("Puesto_") || n.StartsWith("N03a_") || n.StartsWith("N03b_");
            if (!nombre || n.Contains("Holo") || n.Contains("Luz") || n.Contains("Vapor") || n.StartsWith("Gente")) return false;
            return t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null;
        }

        private static Bounds Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // Mueve en X el puesto decorativo (N03) de cada par que se traslapa, hasta dejar 0.3 m de aire.
        private static int SepararTraslapes()
        {
            Physics.SyncTransforms();
            int movidos = 0;
            for (int vuelta = 0; vuelta < 30; vuelta++)
            {
                var puestos = new System.Collections.Generic.List<Transform>();
                foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (EsPuesto(t)) puestos.Add(t);
                bool huboCambio = false;
                for (int i = 0; i < puestos.Count && !huboCambio; i++)
                {
                    for (int j = i + 1; j < puestos.Count && !huboCambio; j++)
                    {
                        Bounds a = Caja(puestos[i]);
                        Bounds b = Caja(puestos[j]);
                        bool traslape = a.min.x + 0.15f < b.max.x && b.min.x + 0.15f < a.max.x && a.min.z + 0.15f < b.max.z
                            && b.min.z + 0.15f < a.max.z && a.min.y < b.max.y && b.min.y < a.max.y;
                        if (!traslape) continue;
                        Transform mover = puestos[j].name.StartsWith("N03") ? puestos[j] : puestos[i].name.StartsWith("N03") ? puestos[i] : puestos[j];
                        Bounds m = mover == puestos[j] ? b : a;
                        Bounds f = mover == puestos[j] ? a : b;
                        float signo = m.center.x >= f.center.x ? 1f : -1f;
                        float paso = signo > 0 ? f.max.x - m.min.x + 0.3f : f.min.x - m.max.x - 0.3f;
                        mover.position += Vector3.right * paso;
                        Physics.SyncTransforms();
                        movidos++;
                        huboCambio = true;
                    }
                }
                if (!huboCambio) break;
            }
            return movidos;
        }

        public static void Aplicar()
        {
            foreach (string ruta in Escenas)
            {
                var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                int cambiados = 0;
                foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!t.name.StartsWith("Puesto_Tacos") || t.parent == null) continue;
                    if (t.parent.name != "PuestosTacos" && t.parent.name != "PuestosCallejon") continue;
                    if (!Mathf.Approximately(t.localScale.y, VestirCalle.EscalaPuestoTacos))
                    {
                        t.localScale = Vector3.one * VestirCalle.EscalaPuestoTacos;
                        cambiados++;
                    }
                }
                int movidos = SepararTraslapes();
                Debug.Log("[T-637] puestos separados: " + movidos);
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                Debug.Log("[T-637] " + ruta + ": " + cambiados + " puestos escalados.");
            }
        }
    }
}
