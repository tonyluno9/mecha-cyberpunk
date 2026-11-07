using System.Linq;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-862 (playtest 6-nov tarde): el taller del mecánico con un solo letrero bien puesto y su mecánico. Se apaga la pantalla LED (CY02) que
    /// quedaba a media puerta del portón y el letrero de hotel (L11) que salía atravesado arriba del taller; el letrero del kit (C15a) sobre el
    /// dintel dice "EL CANEPA" grande y abajo "TALLER MECÁNICO". Canepa deja el modelo de woman2 y usa el del Mecánico de Tony
    /// (Assets/Arte/Personajes/Mecanico, hecho Visual_Mecanico con CreadorVisualTripo: Idle, caminar y correr prestados de woman2), dentro del
    /// taller. Idempotente (rehace su raíz S2t_Taller).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tTaller.Colocar
    /// </summary>
    public static class ColocarS2tTaller
    {
        public const string NombreRaiz = "S2t_Taller";
        public const string RutaCanepa = "GTA_T417/Apoyo_CANEPA";
        public const string RutaLetrero = "S2n_TallerMecanico/Fachada/Letrero_Taller";
        public const string RutaTextoViejo = "S2n_TallerMecanico/Fachada/Letrero_Taller_Texto";
        public const string RutaVisualMecanico = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_Mecanico.prefab";
        /// <summary>Frente del portón (x) y su tramo en z (de ColocarS2nPuertas).</summary>
        public static readonly Bounds FrentePorton = new Bounds(new Vector3(-38f, 3f, -71.7f), new Vector3(4f, 8f, 9f));
        public static readonly Bounds ArribaDelTaller = new Bounds(new Vector3(-37.5f, 9f, -70f), new Vector3(4f, 6f, 12f));
        /// <summary>Escala del modelo como la de los otros NPC de Tripo de la ciudad (woman2 de Canepa).</summary>
        public const float EscalaModelo = 1.29f;
        public static readonly Color Naranja = new Color(1f, 0.62f, 0.2f);

        [MenuItem("Mecha Cyberpunk/S2t/10 Taller del mecanico")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;

            foreach (Transform t in ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && t.parent != null
                && ((t.name.StartsWith("CY02_PantallaVertical") && FrentePorton.Contains(t.position)) || (t.name == "L11" && ArribaDelTaller.Contains(t.position)))).ToArray())
            {
                t.gameObject.SetActive(false);
                ColocarS2.Informe.AppendLine("apagado " + t.name + " en " + t.position.ToString("F1"));
            }

            Letrero(raiz);
            Mecanico();
            ColocarS2.Guardar("s2t-taller");
        }

        private static void Letrero(Transform raiz)
        {
            Transform viejo = ColocarS2.Buscar(RutaTextoViejo);
            if (viejo != null)
            {
                viejo.gameObject.SetActive(false);
            }
            Transform letrero = ColocarS2.Buscar(RutaLetrero);
            if (letrero == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaLetrero);
                return;
            }
            Bounds b = ArteT250.Limites(letrero.gameObject);
            float x = b.min.x - 0.06f;
            ColocarS2t.TextoMundo(raiz, "Letrero_ElCanepa", "EL CANEPA", new Vector3(x, b.center.y + b.size.y * 0.14f, b.center.z), new Vector2(b.size.z * 0.8f, b.size.y * 0.42f), Vector3.left, Naranja);
            ColocarS2t.TextoMundo(raiz, "Letrero_TallerMecanico", "TALLER MECÁNICO", new Vector3(x, b.center.y - b.size.y * 0.2f, b.center.z), new Vector2(b.size.z * 0.7f, b.size.y * 0.2f), Vector3.left, Color.white);
            ColocarS2.Informe.AppendLine("letrero: EL CANEPA / TALLER MECÁNICO sobre el C15a");
        }

        private static void Mecanico()
        {
            Transform canepa = ColocarS2.Buscar(RutaCanepa);
            if (canepa == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaCanepa);
                return;
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualMecanico);
            if (prefab == null)
            {
                CreadorVisualTripo.Crear("Mecanico");
                AssetDatabase.SaveAssets();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualMecanico);
            }
            if (prefab == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaVisualMecanico + " (CreadorVisualTripo.Crear(\"Mecanico\") no lo hizo)");
                return;
            }
            Transform anterior = canepa.Cast<Transform>().FirstOrDefault(t => t.name == "Visual_Mecanico");
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior.gameObject);
            }
            Transform visualViejo = canepa.Find("Visual");
            GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canepa);
            nuevo.name = "Visual_Mecanico";
            nuevo.transform.localPosition = visualViejo != null ? visualViejo.localPosition : Vector3.zero;
            nuevo.transform.localRotation = visualViejo != null ? visualViejo.localRotation : Quaternion.identity;
            Transform modelo = nuevo.transform.Find("Giro/Modelo");
            if (modelo != null)
            {
                modelo.localScale = Vector3.one * EscalaModelo;
            }
            if (visualViejo != null)
            {
                visualViejo.gameObject.SetActive(false);
            }
            ColocarS2.Informe.AppendLine("Canepa con el modelo del Mecánico en " + canepa.position.ToString("F1"));
        }

        public static void Fotos()
        {
            ColocarS2.Abrir();
            ColocarS2t.FotoHora(13f, new Vector3(-48f, 2f, -71f), new Vector3(-37f, 4f, -71.2f), ColocarS2t.CarpetaCapturas + "s2t-taller-fachada-dia.png", 65f);
            ColocarS2t.FotoHora(22f, new Vector3(-48f, 2f, -71f), new Vector3(-37f, 4f, -71.2f), ColocarS2t.CarpetaCapturas + "s2t-taller-fachada-noche.png", 65f);
            ColocarS2t.FotoHora(22f, new Vector3(-38.2f, 1.6f, -72.5f), new Vector3(-35.4f, 1.1f, -70.6f), ColocarS2t.CarpetaCapturas + "s2t-taller-mecanico.png", 55f);
        }
    }
}
