using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>Fotos de T-510 a 1920x1080: la hoja de todos los visuales sobre un piso, y cada personaje nuevo en su lugar de Ciudad_Prologo.</summary>
    public static class CapturasT510
    {
        public const string Carpeta = "docs/capturas/";

        public static void Hoja() => Hoja("Idle");

        public static void HojaCaminar() => Hoja("Caminar");

        private static void Hoja(string estado)
        {
            CapturaPersonajes.EscenaDePrueba();
            string[] nombres = { "man", "woman", "woman2", "SkullHombre", "Doctor", "Inventor", "Hacker", "RobotHombre", "GuardiaNova" };
            for (int i = 0; i < nombres.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(nombres[i]));
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = new Vector3(i * 1.5f, 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Animator a = inst.GetComponentInChildren<Animator>();
                // En el editor el Animator no avanza solo: se muestrea el clip (los clips Humanoid de woman2 sirven a todos).
                a.enabled = false;
                if (CreadorVisualTripo.ClipDeModelo("woman2", estado, out AnimationClip clip))
                {
                    clip.SampleAnimation(a.gameObject, clip.length * (0.1f + 0.13f * i));
                }
            }
            Camera c = CapturaPersonajes.ColocarCamara(new Vector3(6f, 0.9f, 0f), new Vector3(0f, 0.15f, -1f), 12f, 1.1f, 34f);
            CapturaPersonajes.Guardar(c, Carpeta + "T-510-nuevos-" + estado.ToLowerInvariant() + ".png");
        }

        /// <summary>man con las cinco texturas (SKULLS natural, SNAKES verde, NOVA azul, ámbar y rojo) sobre el piso de prueba.</summary>
        public static void HojaMan()
        {
            CapturaPersonajes.EscenaDePrueba();
            string[] colores = { "natural", "verde", "azul", "ambar", "rojo" };
            for (int i = 0; i < colores.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("man"));
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = new Vector3(i * 1.4f, 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, 180f + (i - 2) * 10f, 0f);
                Material m = new Material(CreadorVisualTripo.AsegurarMaterialBase("man"));
                if (colores[i] != "natural")
                {
                    m.SetTexture("_BaseMap", ConectarVisualesT510.TexturaMan(colores[i]));
                }
                inst.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = m;
                Animator a = inst.GetComponentInChildren<Animator>();
                a.enabled = false;
                if (CreadorVisualTripo.ClipDeModelo("woman2", "Idle", out AnimationClip clip))
                {
                    clip.SampleAnimation(a.gameObject, 0.01f);
                }
            }
            Camera c = CapturaPersonajes.ColocarCamara(new Vector3(2.8f, 0.9f, 0f), new Vector3(0f, 0.12f, -1f), 6.2f, 1.1f, 34f);
            CapturaPersonajes.Guardar(c, Carpeta + "T-510-man-colores.png");
        }

        /// <summary>Todas las fotos de los personajes nuevos en su lugar de Ciudad_Prologo (en el editor el Animator no avanza: se muestrea un clip).</summary>
        public static void Escena()
        {
            Abrir();
            // (archivo, ruta "padre/hijo" o nombre, distancia, altura de cámara, apertura, giro de cámara en grados respecto a donde mira, clip)
            var tomas = new (string archivo, string objetivo, float dist, float alt, float fov, float giro, string clip)[]
            {
                ("T-510-man-skulls", "Pandilla_Calaveras_1/Pandillero_1", 3.4f, 1.5f, 38f, 20f, "Idle"),
                                ("T-510-man-nova", "Pandilla_Nova_4/Pandillero_1", 3.4f, 1.5f, 38f, 20f, "Idle"),
                ("T-510-skullhombre", "PuntoJefe_tianguis/Jefe", 4f, 1.6f, 38f, 20f, "Idle"),
                ("T-510-doctor", "Apoyo_JOEL", 3f, 2.6f, 40f, 150f, "AcuerdoCompleto"),
                ("T-510-inventor", "Apoyo_TINO", 3.4f, 1.4f, 38f, 15f, "Idle"),
                ("T-510-hacker", "Apoyo_SERGIO", 3.4f, 1.4f, 38f, 15f, "Idle"),
                ("T-510-vendedor", "Vendedor", 3.2f, 1.4f, 38f, 20f, "Idle"),
                ("T-510-guardia", "T510_Guardias/Guardia_1", 4f, 1.5f, 38f, 25f, "Idle"),
                ("T-510-banqueta", "@caminante", 6f, 1.9f, 50f, 65f, "Caminar"),
            };
            // Los colores de pandillas, vendedor y jefe los pone VarianteVisual al despertar (Awake): en el editor se aplican a mano (no se guarda).
            foreach (VarianteVisual v in Object.FindObjectsByType<VarianteVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                v.Aplicar();
            }
            foreach (var t in tomas)
            {
                Transform objetivo = t.objetivo == "@caminante" ? PrimerCaminante() : Buscar(t.objetivo);
                if (objetivo == null)
                {
                    Debug.LogError("[Personaje] CapturasT510: no existe " + t.objetivo);
                    continue;
                }
                // Algunos viven apagados hasta que el juego los despierta: se encienden solo para la foto (la escena no se guarda).
                for (Transform p = objetivo; p != null; p = p.parent)
                {
                    p.gameObject.SetActive(true);
                }
                PosarTodos(objetivo, t.clip);
                Vector3 mira = objetivo.forward;
                mira.y = 0f;
                if (t.archivo.EndsWith("banqueta"))
                {
                    Transform visual = objetivo.GetComponentInChildren<VisualPersonaje>() != null ? objetivo.GetComponentInChildren<VisualPersonaje>().transform : objetivo;
                    mira = visual.forward;
                }
                Vector3 pecho = objetivo.position + Vector3.up * (t.archivo.EndsWith("banqueta") ? 0.9f : t.archivo.Contains("skullhombre") ? 1.15f : 1.0f);
                // Primero el ángulo pedido y, si hay una pared en medio, otros ángulos y cada vez más cerca.
                Camera c = null;
                for (float k = 1f; k >= 0.45f && c == null; k -= 0.18f)
                {
                    foreach (float extra in new[] { 0f, 40f, -40f, 80f, -80f, 130f, -130f, 180f })
                    {
                        Vector3 d = Quaternion.Euler(0f, t.giro + extra, 0f) * mira.normalized;
                        Vector3 posicion = objetivo.position + d * t.dist * k + Vector3.up * t.alt;
                        if (!Physics.Linecast(posicion, pecho, out RaycastHit golpe, ~0, QueryTriggerInteraction.Ignore) || golpe.distance > Vector3.Distance(posicion, pecho) - 0.3f)
                        {
                            c = CapturaPersonajes.ColocarCamara(objetivo.position + Vector3.up * 0.1f, d, t.dist * k, t.alt - 0.1f, t.fov);
                            break;
                        }
                    }
                }
                if (c == null)
                {
                    c = CapturaPersonajes.ColocarCamara(objetivo.position + Vector3.up * 0.1f, mira, t.dist, t.alt - 0.1f, t.fov);
                }
                c.transform.LookAt(pecho);
                // Luz de relleno junto a la cámara: la escena es de noche y en el editor no hay faroles encendidos.
                GameObject luz = new GameObject("LuzCaptura");
                Light l = luz.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 12f;
                l.intensity = 6f;
                l.shadows = LightShadows.None;
                luz.transform.position = c.transform.position + Vector3.up * 0.5f;
                CapturaPersonajes.Guardar(c, Carpeta + t.archivo + ".png");
                Object.DestroyImmediate(luz);
            }
        }

        private static Transform PrimerCaminante()
        {
            foreach (GenteDeBanqueta g in Object.FindObjectsByType<GenteDeBanqueta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.Modo == ModoGente.Caminar)
                {
                    return g.transform.parent;
                }
            }
            return null;
        }

        private static Transform Buscar(string ruta)
        {
            string[] partes = ruta.Split('/');
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name != partes[partes.Length - 1])
                {
                    continue;
                }
                bool bien = true;
                Transform p = t.parent;
                for (int i = partes.Length - 2; i >= 0 && bien; i--)
                {
                    bien = p != null && p.name == partes[i];
                    p = p != null ? p.parent : null;
                }
                if (bien)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>Muestrea el clip de woman2 en cada Animator del objeto y de sus compañeros de grupo (la pareja que platica o camina).</summary>
        private static void PosarTodos(Transform objetivo, string clipNombre)
        {
            if (!CreadorVisualTripo.ClipDeModelo("woman2", clipNombre, out AnimationClip clip))
            {
                return;
            }
            Transform raiz = objetivo.parent != null && objetivo.name.StartsWith("Gente_") ? objetivo.parent : objetivo;
            int i = 0;
            foreach (Animator a in raiz.GetComponentsInChildren<Animator>(true))
            {
                a.enabled = false;
                clip.SampleAnimation(a.gameObject, clip.length * (0.15f + 0.07f * (i++ % 9)));
            }
        }

        public static void ListarCaminantes()
        {
            Abrir();
            var sb = new System.Text.StringBuilder();
            foreach (GenteDeBanqueta g in Object.FindObjectsByType<GenteDeBanqueta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sb.AppendLine(g.Modo + " " + g.transform.parent.name + " pos=" + g.transform.position + " local=" + g.transform.localPosition + " A=" + g.PuntoA + " B=" + g.PuntoB + " ctrl=" + g.GetComponentInChildren<Animator>(true).runtimeAnimatorController.name);
            }
            System.IO.File.WriteAllText("Logs/caminantes.txt", sb.ToString());
        }

        public static void Abrir()
        {
            EditorSceneManager.OpenScene(ConectarVisualesT520.RutaEscena, OpenSceneMode.Single);
        }
    }
}
