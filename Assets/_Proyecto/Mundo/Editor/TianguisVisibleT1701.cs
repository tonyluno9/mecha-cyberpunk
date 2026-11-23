using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1701 (T66): los SKULLS de la misión del Tianguis (DirectorPandillas.gruposTianguis) a la vista, en el centro de la calle del Tianguis (la calle E-O de z = 0,
    /// banquetas en z = +-9, la de ColocarSesionBT1606), sobre el asfalto y alcanzables. La lista del director traía 2 entradas vacías (los grupos 3 y 4 no existían en la
    /// escena): con ellas la misión no podía completarse nunca al vencerlos a todos. Aquí se rehacen como copias de un solo SKULL (3 + 2 + 1 + 1 = los 7 de "VENCE A LOS SKULLS").
    /// Cada pandillero queda en un punto de piso (raycast al suelo a nivel de calle) donde su cuerpo no toca nada sólido (ni puestos). Escribe docs/capturas/_tareas/T66/puntos.md.
    /// Se usa reflexión por nombre de tipo para no depender de las áreas de IA y Progresión. Batchmode:
    /// -executeMethod MechaCyberpunk.Mundo.Editor.TianguisVisibleT1701.Aplicar
    /// </summary>
    public static class TianguisVisibleT1701
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string Informe = "docs/capturas/_tareas/T66/puntos.md";
        private const float AlturaCalle = 0.6f;
        private const float RadioCuerpo = 0.45f;
        private const float Separacion = 1.8f;
        private const float RadioBusqueda = 3f;

        // Centros de los 4 grupos sobre el eje de la calle, de oeste a este. La calle es ancha (z -6 a 6 de asfalto): van en el centro.
        private static readonly Vector3[] Centros =
        {
            new Vector3(-26f, 0f, -1f),
            new Vector3(-9f, 0f, 1f),
            new Vector3(8f, 0f, -1f),
            new Vector3(24f, 0f, 1f),
        };

        [MenuItem("Mecha Cyberpunk/T-1701/Tianguis visible (SKULLS en la calle)")]
        public static void Aplicar()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            MonoBehaviour director = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m != null && m.GetType().Name == "DirectorPandillas");
            if (director == null)
            {
                Debug.LogError("[T-1701] No hay DirectorPandillas en la escena.");
                return;
            }
            SerializedObject so = new SerializedObject(director);
            SerializedProperty prop = so.FindProperty("gruposTianguis");
            List<Transform> grupos = new List<Transform>();
            for (int i = 0; i < prop.arraySize; i++)
            {
                Object o = prop.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o is GameObject go)
                {
                    grupos.Add(go.transform);
                }
            }
            if (grupos.Count == 0)
            {
                Debug.LogError("[T-1701] El director no tiene grupos del Tianguis.");
                return;
            }
            StringBuilder sb = new StringBuilder("# T66: puntos de los SKULLS del Tianguis\n\nLista del director antes: " + prop.arraySize + " entradas, " + grupos.Count + " con grupo.\n\n## Antes\n\n");
            Describir(sb, grupos);

            // Los grupos 3 y 4 faltaban: copias de un solo SKULL del grupo con menos pandilleros que tenga 2 (o del segundo).
            Transform modelo = grupos.Count > 1 ? grupos[1] : grupos[0];
            Transform raiz = modelo.parent;
            foreach (string nombre in new[] { "Pandilla_Calaveras_Tianguis3", "Pandilla_Calaveras_Tianguis4" })
            {
                Transform viejo = raiz != null ? raiz.Find(nombre) : null;
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }
            grupos = grupos.Where(g => !g.name.StartsWith("Pandilla_Calaveras_Tianguis")).Take(2).ToList();
            foreach (string nombre in new[] { "Pandilla_Calaveras_Tianguis3", "Pandilla_Calaveras_Tianguis4" })
            {
                GameObject copia = Object.Instantiate(modelo.gameObject, raiz);
                copia.name = nombre;
                foreach (Transform p in Pandilleros(copia.transform).Skip(1))
                {
                    Object.DestroyImmediate(p.gameObject);
                }
                grupos.Add(copia.transform);
            }

            StringBuilder fallos = new StringBuilder();
            for (int i = 0; i < grupos.Count; i++)
            {
                Mover(grupos[i], Centros[i % Centros.Length], fallos);
            }
            prop.arraySize = grupos.Count;
            for (int i = 0; i < grupos.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = grupos[i].gameObject;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            sb.AppendLine("\n## Después (lista del director: " + grupos.Count + " grupos, " + grupos.Sum(g => Pandilleros(g).Count()) + " SKULLS)\n");
            Describir(sb, grupos);
            if (fallos.Length > 0)
            {
                sb.AppendLine("\n## Sin punto libre\n\n" + fallos);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Informe));
            File.WriteAllText(Informe, sb.ToString());
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[T-1701] Tianguis visible: " + grupos.Count + " grupos. " + (fallos.Length > 0 ? "HAY PUNTOS SIN PISO LIBRE" : "Todos en piso libre."));
        }

        private static List<Transform> Pandilleros(Transform grupo)
        {
            return grupo.GetComponentsInChildren<MonoBehaviour>(true).Where(m => m != null && m.GetType().Name == "PandilleroAPie").Select(m => m.transform).ToList();
        }

        private static void Describir(StringBuilder sb, List<Transform> grupos)
        {
            sb.AppendLine("| Grupo | Pandillero | Posición | Piso (collider) |\n| --- | --- | --- | --- |");
            foreach (Transform g in grupos)
            {
                sb.AppendLine("| " + g.name + " | (grupo) | " + g.position.ToString("F2") + " | |");
                foreach (Transform p in Pandilleros(g))
                {
                    string piso = Physics.Raycast(p.position + Vector3.up * 2f, Vector3.down, out RaycastHit h, 6f, ~0, QueryTriggerInteraction.Ignore) ? h.collider.name : "sin piso";
                    sb.AppendLine("| " + g.name + " | " + p.name + " | " + p.position.ToString("F2") + " | " + piso + " |");
                }
            }
        }

        // Pone el grupo en 'centro' y a cada pandillero en fila sobre X (la calle corre en X), cada uno en el piso libre más cercano.
        private static void Mover(Transform grupo, Vector3 centro, StringBuilder fallos)
        {
            List<Transform> lista = Pandilleros(grupo);
            Vector3 c = Libre(centro, lista, out bool ok) ?? centro;
            grupo.position = c;
            Physics.SyncTransforms();
            for (int i = 0; i < lista.Count; i++)
            {
                float desvio = (i - (lista.Count - 1) * 0.5f) * Separacion;
                Vector3? p = Libre(c + Vector3.right * desvio, lista, out ok);
                if (p == null)
                {
                    fallos.AppendLine("- " + grupo.name + "/" + lista[i].name + " cerca de " + (c + Vector3.right * desvio));
                    continue;
                }
                lista[i].position = p.Value;
                Physics.SyncTransforms();
            }
        }

        private static Vector3? Libre(Vector3 deseado, List<Transform> propios, out bool ok)
        {
            ok = true;
            for (float r = 0f; r <= RadioBusqueda; r += 0.5f)
            {
                int pasos = r == 0f ? 1 : Mathf.CeilToInt(2f * Mathf.PI * r / 0.5f);
                for (int k = 0; k < pasos; k++)
                {
                    float a = k * 2f * Mathf.PI / pasos;
                    Vector3 q = deseado + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (!Physics.Raycast(new Vector3(q.x, 10f, q.z), Vector3.down, out RaycastHit piso, 20f, ~0, QueryTriggerInteraction.Ignore) || piso.point.y > AlturaCalle)
                    {
                        continue;
                    }
                    Vector3 pie = piso.point;
                    bool choca = Physics.OverlapCapsule(pie + Vector3.up * (RadioCuerpo + 0.1f), pie + Vector3.up * 1.6f, RadioCuerpo, ~0, QueryTriggerInteraction.Ignore)
                        .Any(col => !propios.Any(t => col.transform.IsChildOf(t)));
                    if (!choca)
                    {
                        return pie;
                    }
                }
            }
            ok = false;
            return null;
        }
    }
}
