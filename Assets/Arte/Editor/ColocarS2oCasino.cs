using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-884 (playtest 7-nov, sesión 2): el casino VivaCoin siempre abierto, con puerta automática de cristal como de tienda (se desliza de lado
    /// al acercarte: <see cref="PuertaAutomatica"/> + <see cref="HojaPuerta"/> corrediza) y adentro los modelos de Tony I11 mesas de
    /// blackjack, I12 sillas, I13 candiles, I14 barra, I15 bancos e I16 arco dorado. Las mesas de cartas de cubos se apagan (las cambian las
    /// I11). Las meseras R01 no van: el zip de Tony trae el modelo sin esqueleto (falta Mixamo). Idempotente: rehace su raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oCasino.Colocar
    /// </summary>
    public static class ColocarS2oCasino
    {
        public const string Raiz = "S2o_Casino";
        public const string RutaLugar = "Barrio/Manzana_3/Lugar_Casino";
        public const string RutaHoja = RutaLugar + "/Puerta/Hoja";
        public const string RutaMobiliario = RutaLugar + "/Interior/Mobiliario";
        public const string RutaMaterialCristal = "Assets/Arte/Mundo/S2o/S2o_CristalPuerta.mat";
        private const string I = "Assets/Arte/Interiores/Prefabs/";
        public const float Piso = 0.2f;
        public const float Techo = 5.5f;
        public const float GrosorCristal = 0.08f;
        public const float RadioAutomatica = 4f;
        /// <summary>Las mesas de blackjack, entre las columnas del salón (x 67 y 74, z 40.5 y 53.5) y frente a las tragamonedas.</summary>
        public static readonly Vector3[] Mesas = { new Vector3(70.5f, 0f, 43.5f), new Vector3(70.5f, 0f, 50.5f), new Vector3(77.5f, 0f, 43.5f), new Vector3(77.5f, 0f, 50.5f) };
        public static readonly Vector3 Arco = new Vector3(62.4f, 0f, 47f);
        public static readonly Vector3 Barra = new Vector3(65f, 0f, 56.6f);

        [MenuItem("Mecha Cyberpunk/S2o/5 Casino: puerta automática e I11-I16")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Puerta();
            Object.DestroyImmediate(GameObject.Find(Raiz));
            Transform raiz = new GameObject(Raiz).transform;
            Muebles(raiz);
            ColocarS2.Guardar("s2o-casino");
        }

        private static void Puerta()
        {
            Transform hoja = ColocarS2.Buscar(RutaHoja);
            if (hoja == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaHoja);
                return;
            }
            // Cristal: la misma hoja (un cubo de 1 m escalado), delgada y transparente. El ancho del vano sale del dintel (la Puerta está girada:
            // el eje local de la hoja que va a lo ancho se busca contra la dirección larga del dintel), y se desliza a lo ancho hacia la fachada.
            Transform dintel = hoja.parent.GetComponentsInChildren<Transform>(true).First(t => t.name == "Dintel");
            Bounds bd = dintel.GetComponent<Renderer>().bounds;
            Vector3 dirLargo = bd.size.z >= bd.size.x ? Vector3.forward : Vector3.right;
            float ancho = Mathf.Max(bd.size.x, bd.size.z);
            bool anchoEnX = Mathf.Abs(Vector3.Dot(hoja.TransformDirection(Vector3.right), dirLargo)) > 0.7f;
            Vector3 escala = hoja.localScale;
            hoja.localScale = anchoEnX ? new Vector3(ancho, escala.y, GrosorCristal) : new Vector3(GrosorCristal, escala.y, ancho);
            hoja.GetComponent<MeshRenderer>().sharedMaterial = Cristal();
            hoja.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Vector3 enPadre = hoja.parent.InverseTransformVector(dirLargo * (ancho - 0.1f));
            HojaPuerta hp = hoja.GetComponent<HojaPuerta>();
            SerializedObject so = new SerializedObject(hp);
            so.FindProperty("corrediza").boolValue = true;
            so.FindProperty("conBisagra").boolValue = false;
            so.FindProperty("desplazamientoCorredizo").vector3Value = enPadre;
            so.ApplyModifiedPropertiesWithoutUndo();

            ColocarT910.PuertaCristal(hoja, ancho, dirLargo);
            Transform lugar = ColocarS2.Buscar(RutaLugar);
            PuertaAutomatica auto = lugar.GetComponent<PuertaAutomatica>() ?? lugar.gameObject.AddComponent<PuertaAutomatica>();
            SerializedObject sa = new SerializedObject(auto);
            sa.FindProperty("lugar").intValue = (int)IdLugar.Casino;
            sa.FindProperty("radio").floatValue = RadioAutomatica;
            sa.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Informe.AppendLine("Puerta del casino: cristal corredizo de " + ancho.ToString("F2") + " m, se abre sola a " + RadioAutomatica + " m");
        }

        public static Material Cristal()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCristal);
            if (m == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RutaMaterialCristal));
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, RutaMaterialCristal);
            }
            // URP Lit transparente (como lo deja su inspector): azul verdoso muy claro, liso, que deja ver el salón.
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.62f, 0.86f, 0.95f, 0.22f));
            m.SetFloat("_Smoothness", 0.95f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Muebles(Transform raiz)
        {
            // Las mesas de cartas de cubos (MesaCartas, su fieltro y su borde) se apagan: van las I11.
            Transform mob = ColocarS2.Buscar(RutaMobiliario);
            int apagadas = 0;
            if (mob != null)
            {
                foreach (Transform t in mob.Cast<Transform>().Where(t => t.name == "MesaCartas" || t.name == "FieltroCartas" || t.name == "BordeCartas"))
                {
                    t.gameObject.SetActive(false);
                    apagadas++;
                }
            }
            ColocarS2Interiores.Modelo(raiz, I + "I16", Arco, 90f, Piso, sinCollider: true);
            foreach (Vector3 c in Mesas)
            {
                // El lado curvo de la mesa (los jugadores) hacia la entrada (-x); el crupier del lado de las tragamonedas.
                GameObject mesa = ColocarS2Interiores.Modelo(raiz, I + "I11", c, GiroMesa, Piso);
                for (int k = -1; k <= 1; k++)
                {
                    Vector3 silla = c + new Vector3(-1.45f + Mathf.Abs(k) * 0.35f, 0f, k * 0.85f);
                    ColocarS2Interiores.Modelo(raiz, I + "I12", silla, 90f - k * 25f + GiroSilla, Piso);
                }
                ColocarS2Interiores.Modelo(raiz, I + "I13", c, 0f, Techo, colgar: true);
            }
            ColocarS2Interiores.Modelo(raiz, I + "I14", Barra, GiroBarra, Piso);
            for (int k = -1; k <= 1; k++)
            {
                ColocarS2Interiores.Modelo(raiz, I + "I15", Barra + new Vector3(k * 1.2f, 0f, -1.35f), 0f, Piso);
            }
            Light luz = new GameObject("Luz_Barra").AddComponent<Light>();
            luz.transform.SetParent(raiz, false);
            luz.transform.position = Barra + new Vector3(0f, 3.2f, -0.8f);
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.72f, 0.42f);
            luz.intensity = 5f;
            luz.range = 7f;
            ColocarS2.Informe.AppendLine("Casino: arco I16, " + Mesas.Length + " mesas I11 con 3 sillas I12 y candil I13, barra I14 con 3 bancos I15; mesas de cubos apagadas: " + apagadas);
        }

        /// <summary>Giros (°) de los modelos para que miren como deben (se ajustaron con las fotos de dentro).</summary>
        public static float GiroMesa = 90f;
        public static float GiroSilla = 180f; // El modelo I12 tiene el frente en -Z: con 0 las sillas le daban la espalda a la mesa.
        public static float GiroBarra = 0f;

        public static void Fotos(string cuando)
        {
            ColocarS2cLook.Foto(new Vector3(50f, 2f, 47f), new Vector3(60f, 2f, 47f), "docs/capturas/s2o-casino-puerta-" + cuando + ".png", 60f);
            ColocarS2cLook.Foto(new Vector3(62.5f, 1.8f, 47f), new Vector3(80f, 1.2f, 47f), "docs/capturas/s2o-casino-entrada-" + cuando + ".png", 75f);
            ColocarS2cLook.Foto(new Vector3(82f, 3.5f, 58f), new Vector3(66f, 1f, 40f), "docs/capturas/s2o-casino-salon-" + cuando + ".png", 75f);
        }
    }
}
