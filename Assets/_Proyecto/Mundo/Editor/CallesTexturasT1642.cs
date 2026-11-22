using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1642 (T22): charcos, asfalto, banquetas y primitivos sin textura en toda Ciudad_Prologo.
    /// Batchmode (Unity cerrado, una corrida por metodo):
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.CallesTexturasT1642.Diagnostico | FotosAntes | Aplicar | Fotos
    /// </summary>
    public static class CallesTexturasT1642
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaLog = "Logs/t1642.txt";
        public const string CarpetaFotos = "docs/capturas/_tareas/T22";
        public const string Etiqueta = "[T-1642] ";

        // Las mismas palabras que deciden el paso mojado (Personaje: ReglaSuperficiePaso.PalabrasMojado) y el decal de agua (EfectosPieJugador).
        public static readonly string[] PalabrasMojado = { "mojad", "charco", "wet", "lluvia", "agua", "water", "puddle" };
        private static readonly string[] PalabrasMetal =
        {
            "metal", "rejilla", "acero", "lamina", "fierro", "hierro", "carro", "contenedor", "container", "pu08", "reja", "steel", "grate", "techobodega", "techo_bodega"
        };
        private static readonly string[] PalabrasLoseta = { "loseta", "azulejo", "baldosa", "mosaico", "ceramica", "cerámica", "marmol", "mármol", "tilefloor", "piso_tile" };
        public static readonly string[] PalabrasAguaDecal = { "charco", "puddle", "mojad", "wet" };
        public const float GrosorMaximoDecal = 0.15f;
        public const float LadoMaximoDecal = 40f;

        // ------------------------------------------------------------------ reglas

        public static bool Contiene(string texto, string[] palabras)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            foreach (string p in palabras)
            {
                if (texto.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        /// <summary>El texto que junta ControladorAPie.IdPasoDelPiso para un collider de piso.</summary>
        public static string NombrePiso(Collider c)
        {
            Renderer r = c.GetComponent<Renderer>();
            return (c.sharedMaterial != null ? c.sharedMaterial.name : "") + "|"
                + (r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "") + "|"
                + c.name + "|" + c.transform.root.name;
        }

        /// <summary>True si pisar este collider (en la calle) suena a paso mojado por sus nombres.</summary>
        public static bool SuenaMojado(Collider c)
        {
            string n = NombrePiso(c);
            if (Contiene(n, PalabrasLoseta) && !Contiene(n, PalabrasMojado)) return false;
            if (Contiene(n, PalabrasMetal)) return false;
            return Contiene(n, PalabrasMojado);
        }

        /// <summary>True si EfectosPieJugador lo toma como charco (chapoteo al pisarlo).</summary>
        public static bool EsCharcoQueSuena(MeshRenderer r)
        {
            if (r == null) return false;
            if (AguaT1503.EsCharco(r)) return true;
            Material m = r.sharedMaterial;
            Vector3 tam = r.bounds.size;
            return m != null && tam.y <= GrosorMaximoDecal && tam.x < LadoMaximoDecal && tam.z < LadoMaximoDecal && Contiene(m.name, PalabrasAguaDecal);
        }

        /// <summary>Un charco se ve si su renderer esta prendido, tiene material con shader valido y no es transparente del todo.</summary>
        public static bool CharcoSeVe(MeshRenderer r)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) return false;
            Material m = r.sharedMaterial;
            if (m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader") return false;
            if (r.forceRenderingOff) return false;
            return m.shader.name == AguaT1503.NombreShader; // el agua de T-1503 es la que se ve; los decals "sutiles" de T508 casi no se notan
        }

        /// <summary>Material que no necesita textura base: emisivo, vidrio/transparente, efecto, agua, o shader que no es Lit/SimpleLit/Unlit de URP.</summary>
        public static bool ExentoDeTextura(Material m)
        {
            if (m == null) return false;
            string s = m.shader != null ? m.shader.name : "";
            if (s != "Universal Render Pipeline/Lit" && s != "Universal Render Pipeline/Simple Lit" && s != "Universal Render Pipeline/Unlit"
                && s != "Universal Render Pipeline/Baked Lit" && s != "Standard")
            {
                return true; // shaders propios (agua, neon, cielo...), particulas, sprites, texto
            }
            if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) return true; // transparente: vidrio, haz, humo
            if (m.IsKeywordEnabled("_EMISSION") || (m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0 && m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.05f) return true;
            if (s == "Universal Render Pipeline/Unlit") return true; // unlit liso = luz, letrero, efecto
            if (m.name == "Greybox_Apariencia") return true; // vista previa del personaje en la clinica: Personaje la pinta en tiempo de ejecucion
            string n = m.name.ToLowerInvariant();
            string[] exentos = { "vidrio", "cristal", "glass", "neon", "luz", "emis", "led", "foco", "pantalla", "humo", "haz", "agua", "charco", "sombra", "cielo", "fuego", "chispa", "grieta" };
            return exentos.Any(n.Contains);
        }

        public static bool SinTextura(Material m)
        {
            if (m == null) return true;
            if (m.name == "Default-Material" || m.name == "Lit" || m.name == "Default-Diffuse") return true;
            if (m.HasProperty("_BaseMap")) return m.GetTexture("_BaseMap") == null;
            if (m.HasProperty("_MainTex")) return m.GetTexture("_MainTex") == null;
            return false;
        }

        public static bool RendererCuenta(Renderer r)
        {
            return r != null && r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer) && !(r is TrailRenderer) && !(r is LineRenderer)
                && !(r is SpriteRenderer) && !(r is SkinnedMeshRenderer);
        }

        /// <summary>Renderers activos con algun material liso que deberia llevar textura.</summary>
        public static List<(Renderer r, Material m)> SinTexturaEnEscena(Scene escena)
        {
            var lista = new List<(Renderer, Material)>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!RendererCuenta(r)) continue;
                    if (r.GetComponentInParent<Canvas>() != null) continue;
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null && SinTextura(m) && !ExentoDeTextura(m))
                        {
                            lista.Add((r, m));
                        }
                    }
                }
            }
            return lista;
        }

        /// <summary>Colliders de piso (planos, abajo) cuyo nombre hace sonar el paso mojado.</summary>
        public static List<Collider> PisosMojados(Scene escena)
        {
            var lista = new List<Collider>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>(false))
                {
                    if (!c.enabled || c.isTrigger || c.bounds.size.y > 1f) continue; // solo pisos (el cuerpo de una casa VerdeAgua no es piso: su azotea lo tapa)
                    if (SuenaMojado(c)) lista.Add(c);
                }
            }
            return lista;
        }

        public static List<MeshRenderer> CharcosQueSuenan(Scene escena)
        {
            var lista = new List<MeshRenderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (EsCharcoQueSuena(r)) lista.Add(r);
                }
            }
            return lista;
        }

        // ------------------------------------------------------------------ diagnostico

        [MenuItem("Mecha Cyberpunk/T-1642/Diagnostico")]
        public static void Diagnostico()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, Informe(escena));
            Debug.Log(Etiqueta + "diagnostico en " + RutaLog);
        }

        public static string Informe(Scene escena)
        {
            var sb = new StringBuilder();
            sb.AppendLine("T-1642 diagnostico " + DateTime.Now);
            sb.AppendLine("RESUMEN: " + Resumen(escena));

            var sin = SinTexturaEnEscena(escena);
            sb.AppendLine("=== SIN TEXTURA: " + sin.Count + " (renderer, material) ===");
            foreach (var g in sin.GroupBy(x => x.m).OrderByDescending(g => g.Count()))
            {
                Material m = g.Key;
                var ej = g.First().r;
                sb.AppendLine(g.Count() + "x mat=" + m.name + " shader=" + m.shader.name + " color=" + (m.HasProperty("_BaseColor") ? ColorTxt(m.GetColor("_BaseColor")) : "?")
                    + " ruta=" + AssetDatabase.GetAssetPath(m) + " ej=" + Ruta(ej.transform) + " tam=" + ej.bounds.size.ToString("0.0"));
                foreach (var pr in g.GroupBy(x => Primera(x.r.transform)).OrderByDescending(p => p.Count()).Take(6))
                {
                    sb.AppendLine("     " + pr.Count() + " en " + pr.Key);
                }
            }

            var pisos = PisosMojados(escena);
            sb.AppendLine("=== PISOS QUE SUENAN MOJADO POR NOMBRE: " + pisos.Count + " ===");
            foreach (Collider c in pisos.Take(400))
            {
                sb.AppendLine(Ruta(c.transform) + " pos=" + c.bounds.center.ToString("0.0") + " tam=" + c.bounds.size.ToString("0.0") + " nombre=" + NombrePiso(c)
                    + " tipo=" + c.GetType().Name);
            }

            var charcos = CharcosQueSuenan(escena);
            sb.AppendLine("=== CHARCOS QUE SUENAN: " + charcos.Count + " (no se ven: " + charcos.Count(r => !CharcoSeVe(r)) + ") ===");
            foreach (var g in charcos.GroupBy(r => Primera(r.transform)).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine(g.Count() + " en " + g.Key + " mats=" + string.Join(",", g.Select(r => r.sharedMaterial != null ? r.sharedMaterial.name : "null").Distinct().Take(5)));
            }
            foreach (MeshRenderer r in charcos.Where(r => !CharcoSeVe(r)).Take(100))
            {
                sb.AppendLine("  NO SE VE " + Ruta(r.transform) + " en=" + r.enabled + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.name : "null"));
            }
            foreach (MeshRenderer r in charcos.Take(400))
            {
                sb.AppendLine("  charco " + Ruta(r.transform) + " pos=" + r.bounds.center.ToString("0.0") + " tam=" + r.bounds.size.ToString("0.0") + " capa=" + r.gameObject.layer
                    + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.name : "null"));
            }
            Physics.SyncTransforms();
            sb.AppendLine("=== CHARCOS: piso bajo cada uno (rayo desde 3 m, sin capa Water) ===");
            foreach (MeshRenderer r in charcos)
            {
                Vector3 c = r.bounds.center;
                string piso = "SIN PISO";
                if (Physics.Raycast(new Vector3(c.x, c.y + 3f, c.z), Vector3.down, out RaycastHit h, 6f, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore))
                {
                    piso = "piso=" + h.point.y.ToString("0.00") + " dif=" + (h.point.y - c.y).ToString("0.00") + " de " + Ruta(h.collider.transform);
                }
                sb.AppendLine("  " + Ruta(r.transform) + " y=" + c.y.ToString("0.00") + " " + piso + " comps=" + string.Join(",", r.GetComponents<Component>().Select(x => x.GetType().Name)));
            }
            sb.AppendLine("=== Barrio/Calles ===");
            GameObject calles = GameObject.Find("Barrio/Calles");
            if (calles != null)
            {
                foreach (Renderer r in calles.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name.StartsWith("Charco")) continue;
                    Material m = r.sharedMaterial;
                    sb.AppendLine("  " + Ruta(r.transform) + " act=" + r.gameObject.activeInHierarchy + " centro=" + r.bounds.center.ToString("0.00") + " tam=" + r.bounds.size.ToString("0.0")
                        + " mat=" + (m != null ? m.name + " tiling=" + (m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap").ToString("0.0") : "") : "null")
                        + " malla=" + (r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null ? r.GetComponent<MeshFilter>().sharedMesh.name : "?"));
                }
            }
            sb.AppendLine("=== Renderers con nombre/material de calle o banqueta (todos) ===");
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    Material m = r.sharedMaterial;
                    string n = r.name + "|" + (m != null ? m.name : "");
                    if (!Contiene(n, new[] { "asfalt", "calle", "arroyo", "banquet", "acera", "bordillo", "guarnic", "road", "street", "sidewalk", "concreto", "piso", "suelo" })) continue;
                    if (r.bounds.size.x * r.bounds.size.z < 4f) continue;
                    string tex = m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "-";
                    sb.AppendLine("  " + Ruta(r.transform) + " centro=" + r.bounds.center.ToString("0.00") + " tam=" + r.bounds.size.ToString("0.0") + " mat=" + (m != null ? m.name : "null")
                        + " tex=" + tex + " tiling=" + (m != null && m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap").ToString("0.00") : "") + " malla="
                        + (r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null ? r.GetComponent<MeshFilter>().sharedMesh.name : "?"));
                }
            }
            sb.AppendLine("=== OTROS QUE PARECEN AGUA PERO NO SUENAN ===");
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (EsCharcoQueSuena(r) && r.gameObject.activeInHierarchy) continue;
                    Material m = r.sharedMaterial;
                    bool pareceAgua = (m != null && (m.shader.name == AguaT1503.NombreShader || Contiene(m.name, PalabrasAguaDecal))) || Contiene(r.name, PalabrasAguaDecal);
                    if (!pareceAgua) continue;
                    sb.AppendLine(Ruta(r.transform) + " activo=" + r.gameObject.activeInHierarchy + " en=" + r.enabled + " capa=" + r.gameObject.layer + " pos=" + r.bounds.center.ToString("0.0")
                        + " tam=" + r.bounds.size.ToString("0.00") + " mat=" + (m != null ? m.name : "null"));
                }
            }

            sb.AppendLine("=== CALLES Y BANQUETAS (renderers planos grandes abajo) ===");
            var suelos = new List<Renderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!RendererCuenta(r)) continue;
                    Bounds b = r.bounds;
                    if (b.size.y > 0.8f || b.size.x * b.size.z < 6f || b.max.y > 1.2f || b.min.y < -1.5f) continue;
                    suelos.Add(r);
                }
            }
            foreach (var g in suelos.GroupBy(r => r.sharedMaterial).OrderByDescending(g => g.Count()))
            {
                Material m = g.Key;
                string tex = m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "-";
                Vector2 esc = m != null && m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap") : Vector2.one;
                sb.AppendLine(g.Count() + "x mat=" + (m != null ? m.name : "null") + " tex=" + tex + " tiling=" + esc.ToString("0.00") + " ruta=" + (m != null ? AssetDatabase.GetAssetPath(m) : ""));
                foreach (Renderer r in g.Take(8))
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    sb.AppendLine("     " + Ruta(r.transform) + " centro=" + r.bounds.center.ToString("0.0") + " tam=" + r.bounds.size.ToString("0.0") + " malla=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "?")
                        + " esc=" + r.transform.lossyScale.ToString("0.0") + " col=" + (r.GetComponent<Collider>() != null));
                }
            }

            sb.AppendLine("=== CASAS S2c *_Cuerpo_N ===");
            var cuerpos = new List<Renderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("_Cuerpo_")) cuerpos.Add(r);
                }
            }
            sb.AppendLine("cuerpos=" + cuerpos.Count);
            foreach (var g in cuerpos.SelectMany(r => r.sharedMaterials).Where(m => m != null).GroupBy(m => m).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine(g.Count() + "x " + g.Key.name + " sinTex=" + SinTextura(g.Key) + " exento=" + ExentoDeTextura(g.Key) + " shader=" + g.Key.shader.name);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ aplicar

        public const string NombreRaizCharcos = "T1642_Charcos";
        public const string NombreRaizBordillos = "T1642_Bordillos";
        public const string RutaMatBordillo = "Assets/_Proyecto/Mundo/Materiales/T1642_Bordillo.mat";
        private const string RutaMatT09 = "Assets/Arte/Materiales/Calle18nov/M_T09_ConcretoManchado.mat";
        private const float SobrePiso = 0.012f; // m: el charco queda justo encima del piso visible
        private const float AltoBordillo = 0.24f; // m: 4 cm arriba de la banqueta (0.2)
        private const float FondoBordillo = 0.25f;
        private const float MetrosPorRepeticionBordillo = 0.5f;

        /// <summary>Material sin textura -> textura que le toca por su tipo (se le pone la textura y se conserva su color).</summary>
        public static readonly (string material, string textura)[] TexturasPorTipo =
        {
            ("Assets/_Proyecto/Casino/Materiales/Tragamonedas_Cuerpo.mat", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("Assets/_Proyecto/Casino/Materiales/Tragamonedas_Corona.mat", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("Assets/Arte/Mundo/S2c/S2c_Azotea.mat", "Assets/Arte/Exteriores/Texturas/T506_Revoque.png"),
            ("Assets/Arte/Armas/W01_Pistola/Mira_Metal.mat", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("Assets/Arte/Armas/W01_Pistola/Mira_Acento.mat", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("Assets/Arte/Mundo/S2c/T1102_Plastico.mat", "Assets/Arte/Exteriores/Texturas/T506_Lona.png"),
            ("Assets/Arte/Mundo/S2c/T1102_PlasticoRojo.mat", "Assets/Arte/Exteriores/Texturas/T506_Lona.png"),
            ("Assets/Arte/Mundo/S2b/S2b_TablonGreybox.mat", "Assets/Arte/Exteriores/Texturas/T506_Madera.png"),
        };

        /// <summary>Charcos nuevos donde no habia (callejon de la casa y frente a la casa): x, z.</summary>
        public static readonly Vector2[] CharcosNuevos =
        {
            new Vector2(-62f, -58.2f), new Vector2(-71.5f, -60.2f), new Vector2(-80f, -58.6f), // callejon (T-1641)
            new Vector2(-51f, -80f), new Vector2(-50.5f, -69.5f),                              // calle de la casa
        };

        [MenuItem("Mecha Cyberpunk/T-1642/Aplicar")]
        public static void Aplicar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var sb = new StringBuilder();
            sb.AppendLine("T-1642 Aplicar " + DateTime.Now);
            sb.AppendLine("ANTES: " + Resumen(escena));

            Shader agua = Shader.Find(AguaT1503.NombreShader);
            Material[] aguas = Enumerable.Range(0, 4)
                .Select(i => AssetDatabase.LoadAssetAtPath<Material>(AguaT1503.CarpetaMateriales + "/" + AguaT1503.PrefijoMaterial + "S2c_Charco_" + i + ".mat")).ToArray();
            if (agua == null || aguas.Any(m => m == null))
            {
                throw new Exception("T-1642: falta el shader o los materiales de agua de T-1503");
            }

            // 1) Charcos nuevos (se rehacen).
            foreach (GameObject viejo in escena.GetRootGameObjects().Where(g => g.name == NombreRaizCharcos).ToArray())
            {
                Object.DestroyImmediate(viejo);
            }
            Physics.SyncTransforms();
            var raizCharcos = new GameObject(NombreRaizCharcos);
            for (int i = 0; i < CharcosNuevos.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Charco_" + i;
                q.transform.SetParent(raizCharcos.transform, false);
                q.transform.SetPositionAndRotation(new Vector3(CharcosNuevos[i].x, 1f, CharcosNuevos[i].y), Quaternion.Euler(90f, 37f * i, 0f));
                q.transform.localScale = new Vector3(1.6f + 0.3f * (i % 3), 2.2f + 0.4f * (i % 2), 1f);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.GetComponent<MeshRenderer>().sharedMaterial = aguas[i % aguas.Length];
            }

            // 2) Todos los charcos que suenan: al sistema de agua de T-1503 (capa Water, CharcoVisible) y encima del piso visible.
            int convertidos = 0, subidos = 0;
            foreach (MeshRenderer r in CharcosQueSuenan(escena))
            {
                if (!AguaT1503.EsCharco(r) || r.sharedMaterial == null || r.sharedMaterial.shader != agua)
                {
                    r.gameObject.layer = ReflejoCharcos.CapaCharcos;
                    if (!r.name.StartsWith(AguaT1503.PrefijoCharco, StringComparison.Ordinal)) r.name = AguaT1503.PrefijoCharco + "_" + r.name;
                    r.sharedMaterial = aguas[Mathf.Abs(r.name.GetHashCode()) % aguas.Length];
                    convertidos++;
                }
                if (r.GetComponent<CharcoVisible>() == null) r.gameObject.AddComponent<CharcoVisible>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                float piso = AlturaPisoVisible(escena, r.bounds.center, r);
                Vector3 p = r.transform.position;
                if (!float.IsNaN(piso) && Mathf.Abs(p.y - (piso + SobrePiso)) > 0.004f)
                {
                    r.transform.position = new Vector3(p.x, piso + SobrePiso, p.z);
                    subidos++;
                }
                EditorUtility.SetDirty(r.gameObject);
            }
            sb.AppendLine("charcos nuevos=" + CharcosNuevos.Length + " convertidos al agua T-1503=" + convertidos + " movidos al piso visible=" + subidos);

            // 3) Materiales lisos: textura segun su tipo.
            foreach (var (rutaMat, rutaTex) in TexturasPorTipo)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
                if (m == null || t == null)
                {
                    sb.AppendLine("FALTA " + rutaMat + " o " + rutaTex);
                    continue;
                }
                if (m.GetTexture("_BaseMap") == null)
                {
                    m.SetTexture("_BaseMap", t);
                    if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
                    EditorUtility.SetDirty(m);
                    sb.AppendLine("textura " + t.name + " -> " + m.name);
                }
            }

            // 4) Bordillos de concreto en las orillas de las banquetas de cubo de las manzanas (el lado del cubo estiraba la textura 32x32).
            foreach (GameObject viejo in escena.GetRootGameObjects().Where(g => g.name == NombreRaizBordillos).ToArray())
            {
                Object.DestroyImmediate(viejo);
            }
            Material bordillo = ObtenerBordillo();
            var raizBordillos = new GameObject(NombreRaizBordillos);
            int nb = 0;
            for (int k = 1; k <= 6; k++)
            {
                GameObject ban = GameObject.Find("Barrio/Manzana_" + k + "/Banqueta");
                if (ban == null || !ban.activeInHierarchy) continue;
                Bounds b = ban.GetComponent<Renderer>().bounds;
                nb += Bordillo(raizBordillos.transform, bordillo, "M" + k + "_Norte", new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.max.z), Vector3.forward);
                nb += Bordillo(raizBordillos.transform, bordillo, "M" + k + "_Sur", new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.min.z), Vector3.back);
                nb += Bordillo(raizBordillos.transform, bordillo, "M" + k + "_Este", new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.max.z), Vector3.right);
                nb += Bordillo(raizBordillos.transform, bordillo, "M" + k + "_Oeste", new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), Vector3.left);
            }
            sb.AppendLine("bordillos=" + nb);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            sb.AppendLine("DESPUES: " + Resumen(escena));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1642-aplicar.txt", sb.ToString());
            Debug.Log(Etiqueta + sb);
        }

        // Huecos del bordillo: la boca del callejon de la casa (T-1641) en la orilla este de la Manzana_4.
        private static readonly Rect[] HuecosBordillo = { new Rect(-54.6f, -62f, 1.2f, 6f) };

        private static int Bordillo(Transform padre, Material mat, string nombre, Vector3 a, Vector3 b, Vector3 fuera)
        {
            // Tramos [t0, t1] a lo largo del lado, quitando los huecos que lo cruzan.
            var tramos = new List<(float, float)> { (0f, 1f) };
            bool alongZ = Mathf.Abs(a.x - b.x) < 0.01f;
            foreach (Rect h in HuecosBordillo)
            {
                float fijo = alongZ ? a.x : a.z;
                bool toca = alongZ ? (fijo >= h.xMin && fijo <= h.xMax) : (fijo >= h.yMin && fijo <= h.yMax);
                if (!toca) continue;
                float hIni = alongZ ? Mathf.InverseLerp(a.z, b.z, h.yMin) : Mathf.InverseLerp(a.x, b.x, h.xMin);
                float hFin = alongZ ? Mathf.InverseLerp(a.z, b.z, h.yMax) : Mathf.InverseLerp(a.x, b.x, h.xMax);
                if (hIni > hFin) (hIni, hFin) = (hFin, hIni);
                var nuevos = new List<(float, float)>();
                foreach (var (t0, t1) in tramos)
                {
                    if (hFin <= t0 || hIni >= t1)
                    {
                        nuevos.Add((t0, t1));
                        continue;
                    }
                    if (hIni > t0) nuevos.Add((t0, hIni));
                    if (hFin < t1) nuevos.Add((hFin, t1));
                }
                tramos = nuevos;
            }
            int n = 0;
            foreach (var (t0, t1) in tramos)
            {
                Vector3 p0 = Vector3.Lerp(a, b, t0), p1 = Vector3.Lerp(a, b, t1);
                float largo = Vector3.Distance(p0, p1);
                if (largo < 0.3f) continue;
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "Bordillo_" + nombre + "_" + n;
                c.transform.SetParent(padre, false);
                c.transform.position = (p0 + p1) * 0.5f + Vector3.up * (AltoBordillo * 0.5f) - fuera * (FondoBordillo * 0.5f - 0.02f);
                c.transform.rotation = Quaternion.FromToRotation(Vector3.right, (p1 - p0).normalized);
                c.transform.localScale = new Vector3(largo, AltoBordillo, FondoBordillo);
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.GetComponent<Renderer>().sharedMaterial = mat;
                n++;
            }
            return n;
        }

        private static Material ObtenerBordillo()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMatBordillo);
            if (m == null)
            {
                m = new Material(AssetDatabase.LoadAssetAtPath<Material>(RutaMatT09));
                AssetDatabase.CreateAsset(m, RutaMatBordillo);
            }
            // Cubo de ~80 m de largo y 0.24 m de alto: una repeticion cada 0.5 m a lo largo y una por lo alto (sin estirar).
            var tiling = new Vector2(80f / MetrosPorRepeticionBordillo, 1f);
            m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_BumpMap")) m.SetTextureScale("_BumpMap", tiling);
            m.SetColor("_BaseColor", new Color(0.85f, 0.83f, 0.8f, 1f));
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Altura del piso visible bajo un punto: lo mas alto entre el collider de abajo y las losas planas sin collider que lo tapan.</summary>
        public static float AlturaPisoVisible(Scene escena, Vector3 punto, Renderer ignorar)
        {
            float piso = float.NaN;
            if (Physics.Raycast(new Vector3(punto.x, punto.y + 0.5f, punto.z), Vector3.down, out RaycastHit h, 1.5f, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore))
            {
                piso = h.point.y;
            }
            float refY = float.IsNaN(piso) ? punto.y : piso;
            foreach (Renderer r in LosasPlanas(escena))
            {
                if (r == ignorar) continue;
                Bounds b = r.bounds;
                if (punto.x < b.min.x || punto.x > b.max.x || punto.z < b.min.z || punto.z > b.max.z) continue;
                if (b.max.y <= refY + 0.002f || b.max.y > refY + 0.4f) continue;
                piso = float.IsNaN(piso) ? b.max.y : Mathf.Max(piso, b.max.y);
                refY = piso;
            }
            return piso;
        }

        private static List<Renderer> losasCache;
        private static Scene losasEscena;

        private static List<Renderer> LosasPlanas(Scene escena)
        {
            if (losasCache != null && losasEscena == escena) return losasCache;
            losasEscena = escena;
            losasCache = new List<Renderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled || EsCharcoQueSuena(r) || r.gameObject.layer == ReflejoCharcos.CapaCharcos) continue;
                    Bounds b = r.bounds;
                    Material m = r.sharedMaterial;
                    if (b.size.y > 0.12f || b.size.x * b.size.z < 6f || b.max.y > 1.5f) continue;
                    if (m == null || m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) continue; // decals transparentes no tapan
                    losasCache.Add(r);
                }
            }
            return losasCache;
        }

        /// <summary>Numeros para antes/despues.</summary>
        public static string Resumen(Scene escena)
        {
            losasCache = null;
            Physics.SyncTransforms();
            var charcos = CharcosQueSuenan(escena);
            int noSeVen = charcos.Count(r => !CharcoSeVe(r));
            int tapados = charcos.Count(r => EstaTapado(escena, r));
            return "sinTextura=" + SinTexturaEnEscena(escena).Count + " pisosMojadosSinCharco=" + PisosMojados(escena).Count + " charcosQueSuenan=" + charcos.Count
                + " noSeVen(sutiles o apagados)=" + noSeVen + " tapadosPorUnaLosa=" + tapados;
        }

        /// <summary>True si una losa plana visible tapa el charco (el charco quedo abajo del piso que se ve).</summary>
        public static bool EstaTapado(Scene escena, MeshRenderer charco)
        {
            Vector3 c = charco.bounds.center;
            foreach (Renderer r in LosasPlanas(escena))
            {
                Bounds b = r.bounds;
                if (c.x < b.min.x || c.x > b.max.x || c.z < b.min.z || c.z > b.max.z) continue;
                if (b.max.y > c.y + 0.003f && b.max.y < c.y + 0.4f) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ fotos

        [MenuItem("Mecha Cyberpunk/T-1642/Fotos antes")]
        public static void FotosAntes() => TomarFotos("antes");

        /// <summary>Fotos de despues. En una corrida de Unity APARTE de Aplicar (en la misma salen negras).</summary>
        [MenuItem("Mecha Cyberpunk/T-1642/Fotos")]
        public static void Fotos() => TomarFotos("despues");

        private static void TomarFotos(string suf)
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory(CarpetaFotos);
            Foto("1_casa_" + suf, new Vector3(-50f, 1.7f, -86f), new Vector3(-45f, 0f, -66f), 70f);
            Foto("2_callejon_" + suf, new Vector3(-55f, 1.7f, -59f), new Vector3(-75f, 0.3f, -59f), 70f);
            Foto("3_dorada_" + suf, new Vector3(-60f, 1.7f, -43f), new Vector3(-80f, 0.2f, -43f), 70f);
            Foto("4_principal_" + suf, new Vector3(-62f, 1.7f, -2f), new Vector3(-38f, 0f, -1f), 70f);
            Foto("5_suburbios_" + suf, new Vector3(-110.5f, 1.7f, -50f), new Vector3(-110f, 0.2f, -28f), 70f);
            Foto("6_oriente_" + suf, new Vector3(-30f, 1.7f, -43f), new Vector3(-12f, 0.2f, -44f), 70f);
            Foto("7_neonalto_" + suf, new Vector3(47f, 1.7f, -5f), new Vector3(46f, 0f, 15f), 70f);
            Foto("8_nova_" + suf, new Vector3(70f, 1.7f, -43f), new Vector3(85f, 0.2f, -46f), 70f);
            Foto("9_bordillo_" + suf, new Vector3(-46.5f, 1.4f, -80f), new Vector3(-40.2f, 0.2f, -71f), 65f);
            Foto("10_aerea_casa_" + suf, new Vector3(-50f, 30f, -95f), new Vector3(-50f, 0f, -70f), 60f);
        }

        private static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            const int Ancho = 1280;
            const int Alto = 720;
            var go = new GameObject("CamFotosT1642");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 800f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            var datos = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = (float)Ancho / Alto;
            // El reflejo de los charcos (en juego lo pinta ReflejoCharcos cada cuadro; aqui a mano, como AereasT1414).
            ReflejoCharcos reflejo = Object.FindFirstObjectByType<ReflejoCharcos>();
            if (reflejo != null && reflejo.Actualizar(cam))
            {
                reflejo.Camara.Render();
            }
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            if (reflejo != null)
            {
                reflejo.Soltar();
            }
            Debug.Log(Etiqueta + "foto " + nombre + (reflejo != null ? " (con reflejo)" : " (sin reflejo)"));
        }

        // ------------------------------------------------------------------ utilidades

        private static string ColorTxt(Color c) => "(" + c.r.ToString("0.00") + "," + c.g.ToString("0.00") + "," + c.b.ToString("0.00") + "," + c.a.ToString("0.00") + ")";

        private static string Primera(Transform t)
        {
            Transform raiz = t.root;
            Transform hijo = t;
            while (hijo.parent != null && hijo.parent != raiz) hijo = hijo.parent;
            return hijo == raiz ? raiz.name : raiz.name + "/" + hijo.name;
        }

        public static string Ruta(Transform t)
        {
            string r = t.name;
            int n = 0;
            for (Transform p = t.parent; p != null && n < 4; p = p.parent, n++)
            {
                r = p.name + "/" + r;
            }
            return r;
        }
    }
}
