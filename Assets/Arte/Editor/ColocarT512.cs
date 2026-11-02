using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-512 (paso 4 del plan de personajes y carros): iluminación por zona comparando con docs/referencias-ciudad/.
    /// (1) Callejón y pasillo de El Mercado con más luz sin perder lo oscuro: lámparas N01 en los muros con luz cálida, luces de neón
    /// magenta y la luz naranja del portón más fuerte. (2) El Tianguis con más puestos (N03a, N03b, C12, C13) en hileras a las dos
    /// orillas de la calle principal, sin tapar carros, postes ni el carril central. (3) Azoteas menos oscuras: una luz de luna
    /// (rebote frío, sin sombra) sobre cada techo. (4) Lluvia ligera también de día (ConfigLluvia: lluviaDeDia y fraccionDeDia).
    /// Las luces nuevas solo se prenden de noche (LuzNocturna). Idempotente: todo cuelga de <see cref="NombreRaiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT512.Colocar
    /// </summary>
    public static class ColocarT512
    {
        public const string NombreRaiz = "T512_LuzZonas";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaConfigLluvia = "Assets/_Proyecto/Mundo/Datos/ConfigLluvia_Mundo.asset";
        private const string TipoLuzNocturna = "MechaCyberpunk.Mundo.Reloj.LuzNocturna, MechaCyberpunk.Mundo";
        private const string CarpetaCalle = "Assets/Arte/Calle/Prefabs/";

        public static readonly Color Calido = new Color(1f, 0.72f, 0.42f);
        public static readonly Color Magenta = new Color(1f, 0.2f, 0.75f);
        public static readonly Color Luna = new Color(0.6f, 0.66f, 1f);

        // Callejón de la emboscada: x -87 a -59, muros en z -54.1 y -63.9, ruta en z -59.
        private const float ZRutaCallejon = -59f;
        private static readonly float[] LamparasCallejon = { -84f, -78f, -74.5f, -64.5f, -61f };
        // Pasillo de El Mercado: x -25 a -19 (muros), z 5 a 40.
        private const float XCentroMercado = -22f;
        private static readonly float[] LamparasMercado = { 8f, 14f, 20f, 32f, 38f };
        // El Tianguis: calle principal x -37 a 37; la banqueta empieza en |z| = 7; carril central libre |z| < 3.6.
        private const float XTianguisMin = -36f;
        private const float XTianguisMax = 36f;
        private const float ZBanqueta = 7f;
        private const float ZCarrilLibre = 3.6f;
        private const float PasoPuestos = 1.2f;
        private const float HolguraPuestos = 0.35f;
        private const float DistanciaACarro = 9f;
        private static readonly string[] Puestos = { "N03a", "N03b", "C12_PuestoTacos", "C13_PuestoCiber" };
        // Azoteas: techos a más de 7 m en la ciudad.
        private const float AlturaMinimaTecho = 7f;
        private const float PasoBusquedaTechos = 5f;
        private const float AlturaLuzLuna = 3f;
        private const float IntensidadLuna = 18f;

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-512/Luz por zona")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Physics.SyncTransforms();
            Transform raiz = new GameObject(NombreRaiz).transform;

            Callejon(Grupo(raiz, "Callejon"));
            Mercado(Grupo(raiz, "Mercado"));
            Tianguis(Grupo(raiz, "Tianguis"));
            Azoteas(Grupo(raiz, "Azoteas"));
            LluviaDeDia();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t512-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-512 listo:\n" + Informe);
        }

        private static Transform Grupo(Transform raiz, string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(raiz, false);
            return g;
        }

        /// <summary>Luz puntual sin sombra que solo se prende de noche.</summary>
        public static Light LuzDeNoche(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float rango)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = rango;
            l.shadows = LightShadows.None;
            Type tipo = Type.GetType(TipoLuzNocturna);
            if (tipo != null)
            {
                SerializedObject so = new SerializedObject(go.AddComponent(tipo));
                so.FindProperty("luz").objectReferenceValue = l;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return l;
        }

        private static GameObject Instanciar(string codigo, Transform padre, Vector3 pos, float giro, bool sinCollider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCalle + codigo + ".prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_T512";
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            if (sinCollider)
            {
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            }
            return go;
        }

        private static Vector3 Tam(string codigo)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCalle + codigo + ".prefab").GetComponent<BoxCollider>().size;
        }

        private static float Giro(Vector3 hacia)
        {
            return Quaternion.LookRotation(hacia, Vector3.up).eulerAngles.y;
        }

        /// <summary>Muro más cercano desde 'desde' en la dirección dada (hasta 8 m); si no hay, devuelve el valor por defecto.</summary>
        private static float Muro(Vector3 desde, Vector3 dir, float porDefecto, bool enX)
        {
            if (Physics.Raycast(desde, dir, out RaycastHit golpe, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return enX ? golpe.point.x : golpe.point.z;
            }
            return porDefecto;
        }

        // ------------------------------------------------------------------ callejón

        private static void Callejon(Transform g)
        {
            Vector3 tam = Tam("N01");
            int n = 0;
            for (int i = 0; i < LamparasCallejon.Length; i++)
            {
                float x = LamparasCallejon[i];
                int lado = i % 2 == 0 ? 1 : -1;
                float zMuro = Muro(new Vector3(x, 1.5f, ZRutaCallejon), new Vector3(0f, 0f, lado), lado > 0 ? -54.1f : -63.9f, false);
                float z = zMuro - lado * (tam.z * 0.5f + 0.05f);
                GameObject lampara = Instanciar("N01", g, new Vector3(x, 2.1f, z), Giro(new Vector3(0f, 0f, -lado)), true);
                LuzDeNoche(lampara.transform, "Luz", new Vector3(x, 2.6f, z - lado * 0.6f), Calido, 12f, 9f);
                n++;
            }
            // Neón magenta en los muros (como la referencia): dos manchas de color que dejan ver los objetos.
            LuzDeNoche(g, "Neon_Magenta_0", new Vector3(-81f, 3.2f, -55.2f), Magenta, 7f, 8f);
            LuzDeNoche(g, "Neon_Magenta_1", new Vector3(-67f, 3.2f, -62.8f), Magenta, 7f, 8f);
            Informe.AppendLine("Callejón: lámparas N01 con luz cálida " + n + ", neón magenta 2");
        }

        // ------------------------------------------------------------------ El Mercado

        private static void Mercado(Transform g)
        {
            Vector3 tam = Tam("N01");
            int n = 0;
            for (int i = 0; i < LamparasMercado.Length; i++)
            {
                float z = LamparasMercado[i];
                int lado = i % 2 == 0 ? 1 : -1;
                float xMuro = Muro(new Vector3(XCentroMercado, 1.5f, z), new Vector3(lado, 0f, 0f), XCentroMercado + lado * 3f, true);
                float x = xMuro - lado * (tam.z * 0.5f + 0.05f);
                GameObject lampara = Instanciar("N01", g, new Vector3(x, 2.3f, z), Giro(new Vector3(-lado, 0f, 0f)), true);
                LuzDeNoche(lampara.transform, "Luz", new Vector3(x - lado * 0.6f, 2.8f, z), Calido, 8f, 7f);
                n++;
            }
            LuzDeNoche(g, "Neon_Magenta_0", new Vector3(-20f, 3.4f, 11f), Magenta, 6f, 7f);
            LuzDeNoche(g, "Neon_Magenta_1", new Vector3(-24f, 3.4f, 35f), Magenta, 6f, 7f);

            // Luz naranja del portón (T-504) más fuerte: es la que marca la entrada en la referencia.
            int porton = 0;
            Transform mercado = GameObject.Find("T504_Mercado")?.transform;
            if (mercado != null)
            {
                foreach (Light l in mercado.GetComponentsInChildren<Light>(true))
                {
                    // Valores fijos (no multiplicar: el colocador se puede repetir). T-504: adentro 6 / 7 m, derrame 5 / 8 m.
                    if (l.name == "Luz_Adentro" || l.name == "Luz_Derrame")
                    {
                        l.intensity = l.name == "Luz_Adentro" ? 10f : 8f;
                        l.range = l.name == "Luz_Adentro" ? 9f : 10f;
                        EditorUtility.SetDirty(l);
                        porton++;
                    }
                }
            }
            Informe.AppendLine("El Mercado: lámparas N01 con luz cálida " + n + ", neón magenta 2, luces del portón más fuertes " + porton);
        }

        // ------------------------------------------------------------------ El Tianguis

        private static void Tianguis(Transform g)
        {
            List<Vector3> carros = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.name.StartsWith("CarroRobable_")).Select(r => r.transform.position).ToList();
            int n = 0;
            int modelo = 0;
            foreach (int lado in new[] { 1, -1 })
            {
                float x = XTianguisMin;
                while (x <= XTianguisMax)
                {
                    string codigo = Puestos[modelo % Puestos.Length];
                    Vector3 tam = Tam(codigo);
                    // De frente a la calle, con la espalda en la banqueta: el largo corre a lo largo de la calle.
                    float profundo = tam.z;
                    float largo = tam.x;
                    float z = lado * (ZBanqueta - profundo * 0.5f - 0.1f);
                    Vector3 centro = new Vector3(x + largo * 0.5f, 0f, z);
                    bool libre = lado * z - profundo * 0.5f >= ZCarrilLibre
                        && !carros.Any(c => Mathf.Sign(c.z) == lado && Mathf.Abs(c.x - centro.x) < DistanciaACarro)
                        && Libre(centro, new Vector3(largo, tam.y, profundo));
                    if (libre)
                    {
                        Instanciar(codigo, g, new Vector3(centro.x, PisoEn(centro), centro.z), Giro(new Vector3(0f, 0f, -lado)), false);
                        Physics.SyncTransforms();
                        n++;
                        modelo++;
                        x += largo + HolguraPuestos;
                    }
                    else
                    {
                        x += PasoPuestos;
                    }
                }
            }
            Informe.AppendLine("El Tianguis: puestos nuevos en hileras " + n);
        }

        private static float PisoEn(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, 5f, p.z), Vector3.down, out RaycastHit golpe, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return golpe.point.y;
            }
            return 0f;
        }

        private static bool Libre(Vector3 centro, Vector3 tam)
        {
            Vector3 medio = new Vector3(tam.x * 0.5f + HolguraPuestos, tam.y * 0.5f, tam.z * 0.5f + 0.05f);
            Vector3 c = new Vector3(centro.x, PisoEn(centro) + medio.y + 0.15f, centro.z);
            foreach (Collider otro in Physics.OverlapBox(c, medio, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (otro.name != "Asfalto" && otro.name != "Banqueta")
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------ azoteas

        private static void Azoteas(Transform g)
        {
            HashSet<Collider> techos = new HashSet<Collider>();
            for (float x = -110f; x <= 110f; x += PasoBusquedaTechos)
            {
                for (float z = -95f; z <= 95f; z += PasoBusquedaTechos)
                {
                    if (Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out RaycastHit golpe, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                        && golpe.point.y > AlturaMinimaTecho && golpe.normal.y > 0.9f)
                    {
                        techos.Add(golpe.collider);
                    }
                }
            }
            int n = 0;
            foreach (Collider techo in techos)
            {
                Bounds b = techo.bounds;
                float lado = Mathf.Max(b.size.x, b.size.z);
                if (lado < 3f || lado > 40f)
                {
                    continue;
                }
                Vector3 pos = new Vector3(b.center.x, b.max.y + AlturaLuzLuna, b.center.z);
                LuzDeNoche(g, "Luna_" + techo.name + "_" + n, pos, Luna, IntensidadLuna, Mathf.Clamp(lado * 1.05f + 5f, 12f, 29f));
                n++;
            }
            Informe.AppendLine("Azoteas: luces de luna sobre techos " + n + " (de " + techos.Count + " techos)");
        }

        // ------------------------------------------------------------------ lluvia de día

        private static void LluviaDeDia()
        {
            Object config = AssetDatabase.LoadAssetAtPath<Object>(RutaConfigLluvia);
            if (config == null)
            {
                Informe.AppendLine("Lluvia de día: falta " + RutaConfigLluvia);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("lluviaDeDia").boolValue = true;
            so.FindProperty("fraccionDeDia").floatValue = 0.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            Informe.AppendLine("Lluvia de día: ligera (30 % de las gotas)");
        }
    }
}
