using System.IO;
using System.Linq;
using MechaCyberpunk.Casino.Meseras;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-930 (playtest 9-nov): el casino VivaCoin armado según docs/referencias-interiores/casino-interior.png: techo con vigas, tubos y focos, alfombra de grecas, muros X04, hileras
    /// de tragamonedas con pantallas encendidas, arco al fondo y candil central. Sin papel picado (playtest 6-nov). Cambia la cajera de cubos por una R01 tras un mostrador I14 y la mesa
    /// de cubos del Hacker por un escritorio I02. Idempotente: rehace su raíz <see cref="Raiz"/> y solo apaga cosas viejas (no las borra).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT930Casino.Colocar
    /// </summary>
    public static class ColocarT930Casino
    {
        public const string Raiz = "T930_Casino";
        private const string Interior = "Barrio/Manzana_3/Lugar_Casino/Interior/";
        private const string CarpetaMat = "Assets/Arte/Interiores/Materiales/";
        private const string CarpetaTex = "Assets/Arte/Interiores/Texturas/";
        private const string I = "Assets/Arte/Interiores/Prefabs/";
        private const float Piso = 0.2f;
        private const float Techo = 5.7f;
        private const string Nombre = "MeseraRobot";
        /// <summary>Textura X05 en gris, que se tiñe con el color de cada material: vigas, focos, latón y cordón no quedan como color liso.</summary>
        private const int TexturaBase = 5;
        private const float BrilloAlfombra = 0.18f;

        private static readonly Color Dorado = new Color(1f, 0.74f, 0.4f);
        private static readonly Color Magenta = new Color(0.95f, 0.25f, 0.8f);

        [MenuItem("Mecha Cyberpunk/T-930/Armar casino")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(Raiz));
            Transform raiz = new GameObject(Raiz).transform;
            Revestir();
            ApagarLoViejo();
            Techumbre(raiz);
            Tragamonedas(raiz);
            ColocarS2Interiores.Modelo(raiz, I + "I16", new Vector3(82.6f, 0f, 47f), -90f, Piso, sinCollider: true);
            Caja(raiz, "FondoArco", new Vector3(83.0f, 2.2f, 47f), new Vector3(0.05f, 4f, 2.6f), Mat("T930_Oscuro", new Color(0.03f, 0.02f, 0.05f), Color.black));
            ColocarS2Interiores.Modelo(raiz, I + "I13", new Vector3(72f, 0f, 47f), 0f, Techo - 0.3f, 1.7f, colgar: true);
            Cajera(raiz);
            Hacker(raiz);
            Meseras();
            Entrada(raiz);
            ColocarS2.Guardar("t930-casino");
        }

        private static void Revestir()
        {
            Material alfombra = MatTex("T930_Alfombra", TexturaAlfombra(), new Vector2(9.2f, 11.7f));
            // Brilla un poco para que de noche no quede negra (la prueba de T-546).
            alfombra.EnableKeyword("_EMISSION");
            alfombra.SetTexture("_EmissionMap", alfombra.GetTexture("_BaseMap"));
            alfombra.SetTextureScale("_EmissionMap", new Vector2(9.2f, 11.7f));
            alfombra.SetColor("_EmissionColor", Color.white * BrilloAlfombra);
            EditorUtility.SetDirty(alfombra);
            Renderer a = ColocarS2.Buscar("T546_Interiores/Casino/AlfombraEstampada")?.GetComponent<Renderer>();
            if (a != null) { a.sharedMaterial = alfombra; }
            Texture2D x04 = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "X04_Pared.mat")?.mainTexture as Texture2D;
            // Cada muro con el mosaico de su largo (la textura mide unos 5.5 m a lo ancho): con el 1x1 de un cubo se estiraba.
            foreach ((string n, float largo) in new[] { ("MuroAtras", 28f), ("MuroDer", 22f), ("MuroFrenteDer", 12.5f), ("MuroFrenteIzq", 12.5f) })
            {
                Renderer r = ColocarS2.Buscar(Interior + "Revestimiento/" + n)?.GetComponent<Renderer>();
                if (r != null && x04 != null) { r.sharedMaterial = MatTex("T930_Muro_" + n, x04, new Vector2(Mathf.Round(largo / 5.5f), 1f)); }
            }
            Renderer t = ColocarS2.Buscar(Interior + "Revestimiento/Techo")?.GetComponent<Renderer>();
            Material techo = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "S2_Techo_X05.mat");
            if (t != null && techo != null) { t.sharedMaterial = techo; }
        }

        private static void ApagarLoViejo()
        {
            foreach (string n in new[] { "Neon/TiraCentro", "Mobiliario/Prop_Letrero", "Neon/LetreroCajera", "Mobiliario/Cajera/Letrero" }) { Apagar(n); }
            Transform interior = ColocarS2.Buscar(Interior.TrimEnd('/'));
            foreach (Transform t in interior.GetComponentsInChildren<Transform>(true).Where(t => t.name == "TiraTecho" || t.name == "SillonAsiento" || t.name == "SillonRespaldo" || t.name == "FileteAlfombra"
                || (t.name == "Prop_Mesa" && t.parent.name == "Mobiliario")))
            {
                t.gameObject.SetActive(false);
            }
            Apagar("T546_Interiores/Casino/Prop_Mesa", false);
            Apagar("T546_Interiores/Casino/Prop_Barra", false);
        }

        private static void Apagar(string ruta, bool enInterior = true)
        {
            Transform t = ColocarS2.Buscar(enInterior ? Interior + ruta : ruta);
            if (t != null) { t.gameObject.SetActive(false); } else { ColocarS2.Informe.AppendLine("  no está " + ruta); }
        }

        private static void Techumbre(Transform raiz)
        {
            Material viga = Mat("T930_Viga", new Color(0.13f, 0.07f, 0.05f), Color.black);
            Material tubo = Mat("T930_Tubo", new Color(0.32f, 0.29f, 0.25f), Color.black);
            Material foco = Mat("T930_FocoCuerpo", new Color(0.08f, 0.07f, 0.07f), Color.black);
            Material luzFoco = Mat("T930_FocoLuz", Dorado, Dorado * 3.2f);
            Material luzMagenta = Mat("T930_FocoMagenta", Magenta, Magenta * 3.2f);
            for (int i = 0; i < 6; i++)
            {
                float z = 35.5f + i * 4.8f;
                Caja(raiz, "Viga", new Vector3(72f, Techo - 0.25f, z), new Vector3(22f, 0.4f, 0.32f), viga);
            }
            foreach (float z in new[] { 36.5f, 39.4f, 54.6f, 57.5f })
            {
                Cilindro(raiz, "Tubo", new Vector3(72f, Techo - 0.55f, z), new Vector3(0.22f, 11f, 0.22f), Quaternion.Euler(0f, 0f, 90f), tubo);
            }
            bool magenta = false;
            for (float x = 64f; x <= 80f; x += 4f)
            {
                foreach (float z in new[] { 37.5f, 56.5f })
                {
                    Foco(raiz, new Vector3(x, Techo - 0.75f, z), foco, magenta ? luzMagenta : luzFoco, magenta ? Magenta : Dorado);
                    magenta = !magenta;
                }
            }
            foreach (Vector3 p in new[] { new Vector3(67f, 4.6f, 44f), new Vector3(77f, 4.6f, 44f), new Vector3(67f, 4.6f, 52f), new Vector3(77f, 4.6f, 52f) })
            {
                Luz(raiz, "Relleno", p, LightType.Point, Dorado, 7f, 16f, 0f);
            }
        }

        private static void Foco(Transform raiz, Vector3 p, Material cuerpo, Material brillo, Color color)
        {
            Quaternion g = Quaternion.Euler(18f, 0f, 0f);
            Cilindro(raiz, "Foco", p, new Vector3(0.28f, 0.2f, 0.28f), g, cuerpo);
            Cilindro(raiz, "FocoLuz", p - g * Vector3.up * 0.19f, new Vector3(0.24f, 0.015f, 0.24f), g, brillo);
            Light l = Luz(raiz, "LuzFoco", p - Vector3.up * 0.3f, LightType.Spot, color, 16f, 12f, 62f);
            l.transform.rotation = Quaternion.LookRotation(g * Vector3.down);
        }

        private static void Tragamonedas(Transform raiz)
        {
            Transform adorno = ColocarS2.Buscar("T546_Interiores/Casino/Tragamonedas_Adorno");
            if (adorno == null) { ColocarS2.Informe.AppendLine("  FALTA Tragamonedas_Adorno"); return; }
            Material[] pantallas = { MatTex("T930_Pantalla1", TexturaPantalla(0), Vector2.one, true), MatTex("T930_Pantalla2", TexturaPantalla(1), Vector2.one, true) };
            int k = 0;
            void Isla(float x0, int n, float z0, float frente)
            {
                for (int i = 0; i < n; i++)
                {
                    GameObject g = Object.Instantiate(adorno.gameObject, raiz);
                    g.name = "Tragamonedas_Adorno";
                    g.transform.SetPositionAndRotation(new Vector3(x0 + i * 1.12f, Piso, z0), Quaternion.Euler(0f, frente, 0f));
                }
            }
            Isla(69.8f, 10, 37.45f, 180f);
            Isla(69.8f, 10, 38.55f, 0f);
            Isla(75.6f, 5, 57.1f, 180f);
            Isla(75.6f, 5, 58.0f, 0f);
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.name == "Pantalla" && r.transform.parent.name == "Tragamonedas_Adorno"))
            {
                r.sharedMaterial = pantallas[k++ % 2];
            }
            ColocarS2.Informe.AppendLine("Casino: " + k + " pantallas de tragamonedas encendidas");
        }

        private static void Cajera(Transform raiz)
        {
            Transform c = ColocarS2.Buscar(Interior + "Mobiliario/Cajera");
            if (c == null) { ColocarS2.Informe.AppendLine("  FALTA Cajera"); return; }
            foreach (string n in new[] { "Cuerpo", "Cabeza", "Mostrador", "TapaMostrador" })
            {
                Renderer r = c.Find(n)?.GetComponent<Renderer>();
                if (r != null) { r.enabled = false; }
            }
            ColocarS2Interiores.Modelo(raiz, I + "I14", new Vector3(72f, 0f, 58.9f), 0f, Piso, 0.9f, sinCollider: true);
            GameObject m = Robot(raiz, "Cajera_R01", new Vector3(72f, Piso, 59.25f), Quaternion.Euler(0f, 180f, 0f));
            m.AddComponent<BrazoCharola>();
        }

        private static GameObject Robot(Transform raiz, string nombre, Vector3 p, Quaternion giro)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualTripo.RutaModelo(Nombre));
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz);
            m.name = nombre;
            m.transform.SetPositionAndRotation(p, giro);
            Material material = CreadorVisualTripo.AsegurarMaterialBase(Nombre);
            foreach (Renderer r in m.GetComponentsInChildren<Renderer>()) { r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray(); }
            Animator a = m.GetComponent<Animator>() ?? m.AddComponent<Animator>();
            a.avatar = AssetDatabase.LoadAllAssetsAtPath(CreadorVisualTripo.RutaModelo(Nombre)).OfType<Avatar>().First();
            a.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CreadorVisualPersonaje.RutaControlador(Nombre));
            a.applyRootMotion = false;
            return m;
        }

        private static void Hacker(Transform raiz)
        {
            Apagar("GTA_T417/Apoyo_SERGIO/Set_Hacker", false);
            ColocarS2Interiores.Modelo(raiz, I + "I02", new Vector3(80.1f, 0f, 40.5f), -90f, Piso, sinCollider: false);
            ColocarS2Interiores.Modelo(raiz, I + "I12", new Vector3(81.4f, 0f, 40.5f), -90f, Piso, sinCollider: true);
            Luz(raiz, "LuzHacker", new Vector3(80.6f, 3.2f, 40.5f), LightType.Point, new Color(0.3f, 0.95f, 0.9f), 5f, 6f, 0f);
        }

        private static void Meseras()
        {
            GameObject raiz = GameObject.Find(ColocarS2pMeseras.Raiz);
            if (raiz == null) { ColocarS2.Informe.AppendLine("  FALTA " + ColocarS2pMeseras.Raiz); return; }
            foreach (Animator a in raiz.GetComponentsInChildren<Animator>())
            {
                if (a.GetComponent<BrazoCharola>() == null) { a.gameObject.AddComponent<BrazoCharola>(); }
            }
        }

        private static void Entrada(Transform raiz)
        {
            Material alfombra = Mat("T930_AlfombraCalle", new Color(0.42f, 0.04f, 0.09f), Color.black);
            Material laton = Mat("T930_Laton", new Color(0.8f, 0.58f, 0.2f), Color.black);
            Material cordon = Mat("T930_Cordon", new Color(0.55f, 0.04f, 0.1f), new Color(0.2f, 0f, 0.03f));
            Caja(raiz, "AlfombraEntrada", new Vector3(57.7f, Piso + 0.03f, 47f), new Vector3(5f, 0.03f, 3.2f), alfombra);
            foreach (float z in new[] { 45.3f, 48.7f })
            {
                foreach (float x in new[] { 55.7f, 57.7f, 59.5f })
                {
                    Cilindro(raiz, "PosteLaton", new Vector3(x, Piso + 0.5f, z), new Vector3(0.09f, 0.5f, 0.09f), Quaternion.identity, laton);
                    Cilindro(raiz, "BasePoste", new Vector3(x, Piso + 0.02f, z), new Vector3(0.34f, 0.02f, 0.34f), Quaternion.identity, laton);
                }
                Cilindro(raiz, "Cordon", new Vector3(56.7f, Piso + 0.9f, z), new Vector3(0.05f, 1f, 0.05f), Quaternion.Euler(0f, 0f, 90f), cordon);
                Cilindro(raiz, "Cordon", new Vector3(58.6f, Piso + 0.9f, z), new Vector3(0.05f, 0.9f, 0.05f), Quaternion.Euler(0f, 0f, 90f), cordon);
            }
        }

        private static GameObject Caja(Transform p, string n, Vector3 pos, Vector3 esc, Material m) => Primitiva(PrimitiveType.Cube, p, n, pos, esc, Quaternion.identity, m);
        private static GameObject Cilindro(Transform p, string n, Vector3 pos, Vector3 esc, Quaternion g, Material m) => Primitiva(PrimitiveType.Cylinder, p, n, pos, esc, g, m);

        private static GameObject Primitiva(PrimitiveType t, Transform p, string n, Vector3 pos, Vector3 esc, Quaternion g, Material m)
        {
            GameObject o = GameObject.CreatePrimitive(t);
            o.name = n;
            o.transform.SetParent(p, false);
            o.transform.SetPositionAndRotation(pos, g);
            o.transform.localScale = esc;
            o.GetComponent<Renderer>().sharedMaterial = m;
            o.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }

        private static Light Luz(Transform p, string n, Vector3 pos, LightType tipo, Color c, float intensidad, float alcance, float angulo)
        {
            Light l = new GameObject(n).AddComponent<Light>();
            l.transform.SetParent(p, false);
            l.transform.position = pos;
            l.type = tipo;
            l.color = c;
            l.intensity = intensidad;
            l.range = alcance;
            if (tipo == LightType.Spot) { l.spotAngle = angulo; }
            return l;
        }

        private static Material Mat(string n, Color b, Color e)
        {
            string ruta = CarpetaMat + n + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", b);
            if (m.GetTexture("_BaseMap") == null) { m.SetTexture("_BaseMap", ColocarS2Interiores.TexturaGris(TexturaBase)); }
            if (e.maxColorComponent > 0.001f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", e); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material MatTex(string n, Texture2D t, Vector2 mosaico, bool emite = false)
        {
            Material m = Mat(n, Color.white, emite ? Color.white * 1.6f : Color.black);
            m.SetTexture("_BaseMap", t);
            m.SetTextureScale("_BaseMap", mosaico);
            if (emite) { m.SetTexture("_EmissionMap", t); }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D GuardarTex(Texture2D t, string nombre)
        {
            string ruta = CarpetaTex + nombre + ".png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static readonly string[] Greca = { "11111111", "10000001", "10111101", "10100101", "10101101", "10100001", "10111111", "10000000" };

        /// <summary>Alfombra de grecas: franjas de greca dorada y de rombos sobre vino, como la referencia.</summary>
        private static Texture2D TexturaAlfombra()
        {
            const int T = 1024, Celda = 16, Modulo = T / 4;
            Color vino = new Color(0.33f, 0.05f, 0.08f), oro = new Color(0.85f, 0.55f, 0.13f), naranja = new Color(0.72f, 0.28f, 0.07f), fondo = new Color(0.46f, 0.08f, 0.1f);
            var tex = new Texture2D(T, T, TextureFormat.RGB24, true);
            for (int y = 0; y < T; y++)
            {
                for (int x = 0; x < T; x++)
                {
                    bool banda = y / Modulo % 2 == 0;
                    Color c = fondo;
                    if (banda)
                    {
                        int cx = (x / Celda) % 16, cy = (y % Modulo) / Celda;
                        int gx = cx < 8 ? cx : 15 - cx;
                        if (cy < 8 && Greca[cy][gx] == '1') { c = oro; }
                        else if (cy >= 9 && cy < 16 && ((cx + cy) % 4 == 0)) { c = naranja; }
                        else { c = vino; }
                    }
                    else
                    {
                        int mx = x % Modulo - Modulo / 2, my = y % Modulo - Modulo / 2;
                        int d = Mathf.Abs(mx) + Mathf.Abs(my);
                        if (d < 44) { c = oro; } else if (d < 62) { c = vino; } else if (d < 80) { c = naranja; } else if (d < 96) { c = vino; } else { c = fondo; }
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return GuardarTex(tex, "T930_AlfombraGrecas");
        }

        private static readonly string[] Siete = { "11111", "00001", "00010", "00100", "01000", "01000", "01000" };

        /// <summary>Pantalla de tragamonedas: marco dorado, tres carretes con sietes y una barra de premio.</summary>
        private static Texture2D TexturaPantalla(int v)
        {
            const int W = 256, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            Color fondo = v == 0 ? new Color(0.12f, 0.03f, 0.32f) : new Color(0.02f, 0.2f, 0.3f), oro = new Color(1f, 0.75f, 0.2f), crema = new Color(0.97f, 0.93f, 0.8f);
            Color[] sietes = { new Color(0.95f, 0.1f, 0.3f), oro, v == 0 ? new Color(0.1f, 0.9f, 0.85f) : new Color(0.9f, 0.2f, 0.85f) };
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Color c = fondo;
                    bool borde = x < 5 || y < 5 || x >= W - 5 || y >= H - 5;
                    if (borde) { c = oro; }
                    else if (y > 98 && y < 112 && (x / 12) % 2 == 0) { c = sietes[v]; }
                    else
                    {
                        int r = (x - 12) / 80;
                        if (x >= 12 && r < 3 && y >= 16 && y < 92)
                        {
                            int rx = (x - 12) % 80;
                            if (rx > 70) { c = fondo; }
                            else
                            {
                                c = crema;
                                int px = (rx - 15) / 8, py = (92 - y - 8) / 8;
                                if (px >= 0 && px < 5 && py >= 0 && py < 7 && Siete[py][px] == '1') { c = sietes[r]; }
                            }
                        }
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return GuardarTex(tex, "T930_PantallaTragamonedas" + (v + 1));
        }
    }
}
