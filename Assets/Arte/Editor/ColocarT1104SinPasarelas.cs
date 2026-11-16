using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1104: quita por completo las pasarelas PS01 de Neón Alto (raíz T910_Pasarelas: cubiertas, escaleras y rampas T996). Sin escaleras la
    /// pasarela no tiene sentido. Los puentes N02 viejos siguen apagados. Vuelve a encender los props que T-996 apagó al pie de las escaleras
    /// solo si no se encima con nada ni flota. Idempotente. ColocarT910.Pasarelas termina llamando a Aplicar, así que correrlo no las deja vivas.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1104SinPasarelas.Quitar
    /// </summary>
    public static class ColocarT1104SinPasarelas
    {
        private static readonly string[] PrefijosProps = { "N07", "CY10", "C17" };
        /// <summary>Zona donde estaban las pasarelas (m): banqueta de Neón Alto.</summary>
        private static readonly Vector3 ZonaMin = new Vector3(36f, -1f, 50f);
        private static readonly Vector3 ZonaMax = new Vector3(58f, 6f, 82f);
        private const float Encogido = 0.05f;
        private const float AguantaFlotando = 0.3f;

        [MenuItem("Mecha Cyberpunk/T-1104/Quitar pasarelas")]
        public static void Quitar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Aplicar();
            ColocarS2.Guardar("t1104-sin-pasarelas");
        }

        /// <summary>Apaga T910_Pasarelas y reenciende los props que T-996 apagó (sin guardar la escena).</summary>
        public static void Aplicar()
        {
            Transform raiz = ColocarS2.Todos().FirstOrDefault(t => t.name == ColocarT910.RaizPasarelas && t.parent == null);
            if (raiz != null)
            {
                raiz.gameObject.SetActive(false);
                ColocarS2.Informe.AppendLine("T910_Pasarelas apagada");
            }
            Physics.SyncTransforms();
            Transform[] candidatos = ColocarS2.Todos()
                .Where(t => !t.gameObject.activeSelf && t.gameObject.scene.IsValid() && EsProp(t.name) && EnZona(t.position) && PadreActivo(t))
                .ToArray();
            foreach (Transform t in candidatos)
            {
                t.gameObject.SetActive(true);
                Physics.SyncTransforms();
                if (EstorbaOFlota(t))
                {
                    t.gameObject.SetActive(false);
                    ColocarS2.Informe.AppendLine("se deja apagado " + t.name + " en " + t.position.ToString("F1"));
                }
                else
                {
                    ColocarS2.Informe.AppendLine("se enciende " + t.name + " en " + t.position.ToString("F1"));
                }
            }
        }

        private static bool EsProp(string nombre)
        {
            return PrefijosProps.Any(p => nombre.StartsWith(p));
        }

        private static bool EnZona(Vector3 p)
        {
            return p.x >= ZonaMin.x && p.x <= ZonaMax.x && p.y >= ZonaMin.y && p.y <= ZonaMax.y && p.z >= ZonaMin.z && p.z <= ZonaMax.z;
        }

        private static bool PadreActivo(Transform t)
        {
            return t.parent == null || t.parent.gameObject.activeInHierarchy;
        }

        private static bool EstorbaOFlota(Transform t)
        {
            Collider[] propios = t.GetComponentsInChildren<Collider>();
            if (propios.Length == 0)
            {
                return false;
            }
            foreach (Collider c in propios)
            {
                Bounds b = c.bounds;
                Collider[] cerca = Physics.OverlapBox(b.center, b.extents - Vector3.one * Encogido, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider o in cerca)
                {
                    if (!o.transform.IsChildOf(t) && o.attachedRigidbody == null && !EsSuelo(o, b))
                    {
                        ColocarS2.Informe.AppendLine("  " + t.name + " se encima con " + o.name);
                        return true;
                    }
                }
            }
            Bounds todo = propios[0].bounds;
            foreach (Collider c in propios)
            {
                todo.Encapsulate(c.bounds);
            }
            Vector3 origen = new Vector3(todo.center.x, todo.min.y + 0.05f, todo.center.z);
            foreach (RaycastHit h in Physics.RaycastAll(origen, Vector3.down, AguantaFlotando + 0.05f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!h.collider.transform.IsChildOf(t))
                {
                    return false;
                }
            }
            ColocarS2.Informe.AppendLine("  " + t.name + " flotaría");
            return true;
        }

        /// <summary>El suelo o banqueta ancha cuyo borde de arriba apenas toca la base del prop no cuenta como encimado.</summary>
        private static bool EsSuelo(Collider otro, Bounds prop)
        {
            return otro.bounds.max.y <= prop.min.y + 0.15f;
        }
    }
}
