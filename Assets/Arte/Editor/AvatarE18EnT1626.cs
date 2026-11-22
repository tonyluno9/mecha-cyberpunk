using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1626 (T1): los brazos del avatar del E18 quedaban ~20° abajo de la pose T desde que Blender corrigió su rig. Hace por código lo mismo que
    /// Rig → Configure → Pose → Enforce T-Pose → Apply: arma el esqueleto de la HumanDescription (la pose de referencia del avatar), gira brazo,
    /// antebrazo y mano de cada lado hasta la horizontal hacia afuera, guarda esas rotaciones en la descripción y reimporta con Create From This Model.
    /// El FBX no se toca: solo su importación.
    /// </summary>
    public static class AvatarE18EnT1626
    {
        private const string Etiqueta = "[T-1626] ";

        [MenuItem("Mecha Cyberpunk/T-1626/Avatar del E18 en T")]
        public static void Aplicar()
        {
            var sb = new StringBuilder(Etiqueta + "Avatar del E18 en T\n");
            sb.Append(ForzarT());
            Debug.Log(sb.ToString());
        }

        /// <summary>Fuerza la pose T en la descripción del avatar del E18 y reimporta. Devuelve el informe.</summary>
        public static string ForzarT()
        {
            string ruta = ClipsRobotsNovaT1553.RutaModeloE18;
            var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                return "  falta " + ruta + "\n";
            }
            var sb = new StringBuilder();
            HumanDescription d = importador.humanDescription;
            if (d.human == null || d.human.Length == 0 || d.skeleton == null || d.skeleton.Length == 0)
            {
                // Sin mapeo guardado: que Unity lo genere primero desde el modelo y luego se corrige la pose.
                importador.animationType = ModelImporterAnimationType.Human;
                importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importador.autoGenerateAvatarMappingIfUnspecified = true;
                importador.SaveAndReimport();
                d = importador.humanDescription;
            }
            if (d.human == null || d.human.Length == 0 || d.skeleton == null || d.skeleton.Length == 0)
            {
                return "  no se pudo generar el mapeo humanoide del E18\n";
            }
            (float i, float r)? antes = MedirDescripcion(ruta, d);
            sb.AppendLine("  brazos en la pose del avatar antes (L/R): " + Formato(antes));
            SkeletonBone[] esqueleto = Enderezar(ruta, d);
            if (esqueleto == null)
            {
                return sb.AppendLine("  no se encontraron los huesos de los brazos").ToString();
            }
            d.skeleton = esqueleto;
            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importador.humanDescription = d;
            importador.SaveAndReimport();
            HumanDescription nueva = (AssetImporter.GetAtPath(ruta) as ModelImporter).humanDescription;
            (float i, float r)? despues = MedirDescripcion(ruta, nueva);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Avatar>().FirstOrDefault();
            sb.AppendLine("  brazos en la pose del avatar después (L/R): " + Formato(despues) + (BrazosEnT(despues) ? " (en T)" : " (FUERA DE T)"));
            sb.AppendLine("  avatar: " + (avatar == null ? "NO HAY" : (avatar.isValid && avatar.isHuman ? "válido y humanoide" : "INVÁLIDO")));
            return sb.ToString();
        }

        /// <summary>Elevación (grados sobre la horizontal) de brazo izquierdo y derecho en la pose de referencia de <paramref name="d"/>.</summary>
        public static (float, float)? MedirDescripcion(string rutaModelo, HumanDescription d)
        {
            GameObject instancia = Instanciar(rutaModelo, d, out Dictionary<string, Transform> porNombre);
            if (instancia == null)
            {
                return null;
            }
            try
            {
                float i = Elevacion(instancia.transform, Hueso(d, porNombre, "LeftUpperArm"), Hueso(d, porNombre, "LeftLowerArm"));
                float r = Elevacion(instancia.transform, Hueso(d, porNombre, "RightUpperArm"), Hueso(d, porNombre, "RightLowerArm"));
                return float.IsNaN(i) || float.IsNaN(r) ? ((float, float)?)null : (i, r);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        /// <summary>Medida de los brazos del avatar actual del E18 (lo que usa la prueba).</summary>
        public static (float, float)? MedirAvatarE18()
        {
            var importador = AssetImporter.GetAtPath(ClipsRobotsNovaT1553.RutaModeloE18) as ModelImporter;
            return importador == null ? null : MedirDescripcion(ClipsRobotsNovaT1553.RutaModeloE18, importador.humanDescription);
        }

        public static bool BrazosEnT((float, float)? b) =>
            b.HasValue && Mathf.Abs(b.Value.Item1) <= RobotsNovaT1553.ToleranciaBrazoT && Mathf.Abs(b.Value.Item2) <= RobotsNovaT1553.ToleranciaBrazoT;

        private static SkeletonBone[] Enderezar(string rutaModelo, HumanDescription d)
        {
            GameObject instancia = Instanciar(rutaModelo, d, out Dictionary<string, Transform> porNombre);
            if (instancia == null)
            {
                return null;
            }
            try
            {
                Transform raiz = instancia.transform;
                foreach (string lado in new[] { "Left", "Right" })
                {
                    Transform brazo = Hueso(d, porNombre, lado + "UpperArm");
                    Transform codo = Hueso(d, porNombre, lado + "LowerArm");
                    Transform mano = Hueso(d, porNombre, lado + "Hand");
                    if (brazo == null || codo == null || mano == null)
                    {
                        return null;
                    }
                    Vector3 afuera = raiz.right * (lado == "Left" ? -1f : 1f);
                    Alinear(brazo, codo.position, afuera);
                    Alinear(codo, mano.position, afuera);
                    Transform dedo = mano.childCount > 0 ? mano.GetChild(0) : null;
                    if (dedo != null)
                    {
                        Alinear(mano, dedo.position, afuera);
                    }
                }
                var resultado = new SkeletonBone[d.skeleton.Length];
                for (int k = 0; k < d.skeleton.Length; k++)
                {
                    SkeletonBone b = d.skeleton[k];
                    if (porNombre.TryGetValue(b.name, out Transform t) && t != raiz)
                    {
                        b.rotation = t.localRotation;
                    }
                    resultado[k] = b;
                }
                return resultado;
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        // Gira el hueso para que la dirección hacia su hijo quede en "objetivo" (horizontal hacia afuera).
        private static void Alinear(Transform hueso, Vector3 posicionHijo, Vector3 objetivo)
        {
            Vector3 actual = (posicionHijo - hueso.position).normalized;
            if (actual.sqrMagnitude < 0.0001f)
            {
                return;
            }
            hueso.rotation = Quaternion.FromToRotation(actual, objetivo.normalized) * hueso.rotation;
        }

        private static GameObject Instanciar(string rutaModelo, HumanDescription d, out Dictionary<string, Transform> porNombre)
        {
            porNombre = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
            if (prefab == null)
            {
                return null;
            }
            GameObject instancia = Object.Instantiate(prefab);
            instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            porNombre = new Dictionary<string, Transform>();
            foreach (Transform t in instancia.GetComponentsInChildren<Transform>(true))
            {
                porNombre[t.name] = t;
            }
            porNombre[d.skeleton.Length > 0 ? d.skeleton[0].name : instancia.name] = instancia.transform;
            // La pose guardada en la descripción es la de referencia del avatar.
            for (int k = 1; k < d.skeleton.Length; k++)
            {
                if (porNombre.TryGetValue(d.skeleton[k].name, out Transform t))
                {
                    t.localPosition = d.skeleton[k].position;
                    t.localRotation = d.skeleton[k].rotation;
                }
            }
            return instancia;
        }

        private static Transform Hueso(HumanDescription d, Dictionary<string, Transform> porNombre, string humano)
        {
            foreach (HumanBone h in d.human)
            {
                if (h.humanName == humano)
                {
                    return porNombre.TryGetValue(h.boneName, out Transform t) ? t : null;
                }
            }
            return null;
        }

        private static float Elevacion(Transform raiz, Transform brazo, Transform codo)
        {
            if (brazo == null || codo == null)
            {
                return float.NaN;
            }
            Vector3 dir = raiz.InverseTransformDirection((codo.position - brazo.position).normalized);
            return Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static string Formato((float, float)? b) => b.HasValue ? b.Value.Item1.ToString("F1") + " / " + b.Value.Item2.ToString("F1") : "no se pudo medir";
    }
}
