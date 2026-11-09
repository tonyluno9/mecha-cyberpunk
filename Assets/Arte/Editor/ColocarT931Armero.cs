using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-931 (playtest 9-nov): la tienda del armero según docs/referencias-interiores/entrada-armas-mercado.png: un local chico de frente a la puerta, con vitrinas I17 de mostrador, un muro detrás del
    /// vendedor con tableros de rifles I18 y repisas, reja de tijera I19 a los lados, tambos y cajas. El mostrador real (la E) y el vendedor no se mueven. Quita la tabla con armas de cubos y el bloque
    /// magenta flotante. Sin papel picado (playtest 6-nov). Idempotente: rehace su raíz <see cref="Raiz"/> y solo apaga lo viejo.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT931Armero.Colocar
    /// </summary>
    public static class ColocarT931Armero
    {
        public const string Raiz = "T931_Armero";
        private const string Interior = "Barrio/Manzana_2/Lugar_MercadoNegro/Interior/";
        private const string I = "Assets/Arte/Interiores/Prefabs/";
        private const string Calle = "Assets/Arte/Calle/Prefabs/";
        private const string Taller = "Assets/Arte/TallerCasa/Prefabs/";
        private const string CarpetaMat = "Assets/Arte/Interiores/Materiales/";
        private const float Piso = 0.24f;
        /// <summary>Plano x del muro nuevo detrás del vendedor.</summary>
        public const float XMuro = -29.8f;
        private const float XPuerta = -25.6f;
        private const float ZMin = 21.6f;
        private const float ZMax = 30.9f;
        private const float AltoMuro = 3.7f;
        private const float ZPuertaMin = 24.25f;
        private const float ZPuertaMax = 27.75f;

        [MenuItem("Mecha Cyberpunk/T-931/Armar tienda del armero")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(Raiz));
            Transform raiz = new GameObject(Raiz).transform;
            ApagarLoViejo();
            ApagarNeonSuelto();
            Local(raiz);
            Muebles(raiz);
            ColocarS2.Guardar("t931-armero");
        }

        private static void ApagarLoViejo()
        {
            // Lo que estorba en el local o se ve de cubos: renderers apagados, los colliders y la lógica siguen.
            var caja = new Bounds(new Vector3((XMuro + XPuerta) * 0.5f, 2f, (ZMin + ZMax) * 0.5f), new Vector3(XPuerta - XMuro + 0.6f, 5f, ZMax - ZMin + 0.6f));
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => caja.Contains(r.bounds.center)))
            {
                string ruta = Ruta(r.transform);
                bool mueble = ruta.Contains("Lugar_MercadoNegro/Interior/Mobiliario/") || ruta.Contains("T546_Interiores/EntradaArmasMercado/") || ruta.Contains("Servicios/MostradorMercadoNegro/") || ruta.Contains("/Interior/Neon/");
                bool mercado = ruta.Contains("GTA_T407/Mercado/") && !ruta.Contains("/Vendedor/");
                if ((mueble || mercado) && !ruta.Contains("/SerieFocos/"))
                {
                    r.enabled = false;
                }
            }
        }

        private static void ApagarNeonSuelto()
        {
            Transform neon = ColocarS2.Buscar(Interior + "Neon");
            if (neon == null) { return; }
            foreach (Renderer r in neon.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; }
        }

        private static string Ruta(Transform t)
        {
            string s = t.name;
            for (Transform a = t.parent; a != null; a = a.parent) { s = a.name + "/" + s; }
            return s + "/";
        }

        private static void Local(Transform raiz)
        {
            Material muro = ColocarT930Casino.MatTex("T931_Muro", AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "X03_Pared.mat")?.mainTexture as Texture2D, new Vector2(3f, 1f));
            Material lateral = ColocarT930Casino.MatTex("T931_MuroLateral", AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "X03_Pared.mat")?.mainTexture as Texture2D, new Vector2(1.5f, 1f));
            Material piso = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "S2_Piso_X03.mat");
            Material techo = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "S2_Techo_X05.mat");
            float largo = ZMax - ZMin, centroZ = (ZMin + ZMax) * 0.5f, ancho = XPuerta - XMuro, centroX = (XMuro + XPuerta) * 0.5f;
            GameObject m = ColocarT930Casino.Caja(raiz, "MuroFondo", new Vector3(XMuro, Piso + AltoMuro * 0.5f, centroZ), new Vector3(0.18f, AltoMuro, largo), muro);
            m.AddComponent<BoxCollider>();
            ColocarT930Casino.Caja(raiz, "ForroIzq", new Vector3(centroX, Piso + AltoMuro * 0.5f, ZMax), new Vector3(ancho, AltoMuro, 0.06f), lateral);
            ColocarT930Casino.Caja(raiz, "ForroDer", new Vector3(centroX, Piso + AltoMuro * 0.5f, ZMin), new Vector3(ancho, AltoMuro, 0.06f), lateral);
            // El muro de la puerta visto desde dentro: ladrillo a los dos lados del vano.
            ColocarT930Casino.Caja(raiz, "ForroPuertaIzq", new Vector3(XPuerta - 0.05f, Piso + AltoMuro * 0.5f, (ZMax + ZPuertaMax) * 0.5f), new Vector3(0.05f, AltoMuro, ZMax - ZPuertaMax), lateral);
            ColocarT930Casino.Caja(raiz, "ForroPuertaDer", new Vector3(XPuerta - 0.05f, Piso + AltoMuro * 0.5f, (ZMin + ZPuertaMin) * 0.5f), new Vector3(0.05f, AltoMuro, ZPuertaMin - ZMin), lateral);
            if (piso != null) { ColocarT930Casino.Caja(raiz, "Piso", new Vector3(centroX, Piso + 0.02f, centroZ), new Vector3(ancho, 0.02f, largo), piso); }
            if (techo != null) { ColocarT930Casino.Caja(raiz, "Plafon", new Vector3(centroX, Piso + AltoMuro, centroZ), new Vector3(ancho, 0.05f, largo), techo); }
            Material tubo = ColocarT930Casino.Mat("T931_TuboLuz", new Color(1f, 0.82f, 0.55f), new Color(1f, 0.82f, 0.55f) * 3f);
            ColocarT930Casino.Caja(raiz, "TuboLuz", new Vector3(-27.9f, Piso + AltoMuro - 0.12f, centroZ), new Vector3(0.1f, 0.07f, 3.4f), tubo);
            ColocarT930Casino.Luz(raiz, "LuzTubo", new Vector3(-27.6f, Piso + AltoMuro - 0.5f, centroZ), LightType.Point, new Color(1f, 0.78f, 0.5f), 3.2f, 8f, 0f);
            ColocarT930Casino.Luz(raiz, "LuzVitrina", new Vector3(-26.5f, 1.4f, centroZ), LightType.Point, new Color(0.45f, 0.85f, 0.95f), 0.7f, 3f, 0f);
        }

        private static void Muebles(Transform raiz)
        {
            foreach (float z in new[] { 25.05f, 26.95f })
            {
                ColocarS2Interiores.Modelo(raiz, I + "I17", new Vector3(-26.95f, 0f, z), 90f, Piso, sinCollider: true);
            }
            foreach (float z in new[] { 24f, 28f })
            {
                ColocarS2Interiores.Modelo(raiz, I + "I18", new Vector3(XMuro + 0.28f, 0f, z), -90f, Piso + 0.95f, sinCollider: true);
            }
            ColocarS2Interiores.Modelo(raiz, Taller + "T08_RepisaTaller", new Vector3(XMuro + 0.25f, 0f, 26f), -90f, Piso + 2.35f, 0.8f, sinCollider: true);
            foreach (float z in new[] { 23.6f, 28.4f })
            {
                ColocarS2Interiores.Modelo(raiz, I + "I19", new Vector3(-25.82f, 0f, z), 90f, Piso, 0.45f, sinCollider: true);
            }
            ColocarS2Interiores.Modelo(raiz, Calle + "C16_Tambo", new Vector3(-28.7f, 0f, 22.5f), 0f, Piso);
            ColocarS2Interiores.Modelo(raiz, Calle + "C16_Tambo", new Vector3(-28.8f, 0f, 29.9f), 40f, Piso);
            ColocarS2Interiores.Modelo(raiz, Calle + "C17_Huacales", new Vector3(-27.9f, 0f, 30.2f), 15f, Piso);
            ColocarS2Interiores.Modelo(raiz, Calle + "C17_Huacales", new Vector3(-27.8f, 0f, 22.3f), -20f, Piso);
        }
    }
}
