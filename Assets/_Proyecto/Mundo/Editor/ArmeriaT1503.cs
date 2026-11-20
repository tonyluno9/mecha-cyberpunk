using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1503 (playtest 19-nov): arregla la armería de Ciudad_Prologo. Tres pasos, cada uno se puede repetir:
    /// <see cref="VoltearGabinetes"/> (los racks de rifles I18 y el gabinete de herramientas miraban a la pared; las vitrinas I17 miran a la puerta),
    /// <see cref="ArreglarTexturas"/> (materiales perdidos o rosas y muros con la textura estirada) y <see cref="ArreglarCalle"/> (piezas repetidas,
    /// estorbando el paso a la puerta, flotando o hundidas en el piso). El frente de un modelo es su +Z (así se orientaron al colocarlos).
    /// Batchmode, en este orden: ...ArmeriaT1503.VoltearGabinetes, ...ArmeriaT1503.ArreglarTexturas, ...ArmeriaT1503.ArreglarCalle
    /// </summary>
    public static class ArmeriaT1503
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizTienda = "T931_Armero";
        public const string RaizEntrada = "EntradaArmasMercado";
        public const string RaizCalle = "CalleArmeria";
        public const string RutaMaterialRespaldo = "Assets/_Proyecto/Mundo/Materiales/T1503_Armeria_Respaldo.mat";
        public const string CarpetaMateriales = "Assets/_Proyecto/Mundo/Materiales";

        /// <summary>Centro del local del armero (x entre el muro -29.8 y la puerta -25.6; z entre 21.6 y 30.9).</summary>
        public static readonly Vector3 CentroLocal = new Vector3(-27.7f, 0f, 26.25f);
        /// <summary>Punto de la puerta de la armería: las vitrinas de mostrador miran hacia allá (hacia el cliente).</summary>
        public static readonly Vector3 PuntoPuerta = new Vector3(-25.6f, 0f, 26.25f);
        /// <summary>Paso libre del callejón a la puerta (igual que ColocarT1320Calles.PasoArmeria).</summary>
        public static readonly Bounds PasoPuerta = new Bounds(new Vector3(-22.9f, 1f, 18f), new Vector3(2.4f, 2f, 16f));

        private const float UmbralAjustePiso = 0.1f;
        private const float MaximoAjustePiso = 1f;
        private const float MetrosPorRepeticion = 2f;

        /// <summary>True si <paramref name="t"/> es un gabinete o rack de armas del local (los que se deben orientar).</summary>
        public static bool EsGabinete(Transform t) => t.name == "I18" || t.name == "I17" || t.name.StartsWith("T04_GabineteHerramientas", System.StringComparison.Ordinal);

        /// <summary>Punto hacia el que debe mirar el frente de un gabinete: las vitrinas I17 a la puerta, los racks y gabinetes de pared al centro del local.</summary>
        public static Vector3 PuntoDeFrente(Transform t) => t.name == "I17" ? PuntoPuerta : CentroLocal;

        /// <summary>Lógica pura: true si el frente (+Z) de <paramref name="t"/> mira hacia <paramref name="punto"/> (en el plano horizontal).</summary>
        public static bool MiraHacia(Transform t, Vector3 punto)
        {
            Vector3 frente = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            Vector3 hacia = Vector3.ProjectOnPlane(punto - t.position, Vector3.up);
            return Vector3.Dot(frente, hacia) > 0f;
        }

        // ------------------------------------------------------------ gabinetes

        [MenuItem("Mecha Cyberpunk/Mundo/T-1503 Armeria: voltear gabinetes")]
        public static void VoltearGabinetes()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int girados = 0;
            foreach (Transform t in Gabinetes())
            {
                if (!MiraHacia(t, PuntoDeFrente(t)))
                {
                    Bounds b = Limites(t);
                    t.RotateAround(b.center, Vector3.up, 180f);
                    EditorUtility.SetDirty(t);
                    girados++;
                }
            }
            Guardar();
            Debug.Log("[Mundo] T-1503: " + girados + " gabinetes girados.");
        }

        /// <summary>Los gabinetes de la tienda y de la entrada de armas de la escena abierta.</summary>
        public static IEnumerable<Transform> Gabinetes()
        {
            foreach (string raiz in new[] { RaizTienda, RaizEntrada })
            {
                GameObject r = GameObject.Find(raiz);
                if (r == null)
                {
                    continue;
                }
                foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                {
                    if (EsGabinete(t) && t.GetComponentInChildren<Renderer>(true) != null)
                    {
                        yield return t;
                    }
                }
            }
        }

        // ------------------------------------------------------------ texturas

        [MenuItem("Mecha Cyberpunk/Mundo/T-1503 Armeria: arreglar texturas")]
        public static void ArreglarTexturas()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Material respaldo = MaterialRespaldo();
            int cambiados = 0;
            foreach (Renderer r in RenderersArmeria())
            {
                Material[] mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (EstaRoto(mats[i]))
                    {
                        mats[i] = respaldo;
                        cambio = true;
                    }
                }
                if (cambio)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                    cambiados++;
                }
            }
            int muros = 0;
            GameObject tienda = GameObject.Find(RaizTienda);
            if (tienda != null)
            {
                foreach (Transform t in tienda.transform)
                {
                    if ((t.name == "MuroFondo" || t.name.StartsWith("Forro", System.StringComparison.Ordinal)) && Reescalar(t))
                    {
                        muros++;
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Guardar();
            Debug.Log("[Mundo] T-1503: " + cambiados + " renderers con material arreglado, " + muros + " muros con la textura a escala.");
        }

        /// <summary>True si el material falta, no tiene shader, usa el shader de error (rosa) o no se puede dibujar.</summary>
        public static bool EstaRoto(Material m) => m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported;

        private static Material MaterialRespaldo()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialRespaldo);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, RutaMaterialRespaldo);
            }
            m.SetColor("_BaseColor", new Color(0.32f, 0.29f, 0.27f));
            m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // Una caja con material compartido estira la textura en las paredes de distinto tamaño: se le da su propio material con repetición según su tamaño.
        private static bool Reescalar(Transform t)
        {
            MeshRenderer r = t.GetComponent<MeshRenderer>();
            if (r == null || r.sharedMaterial == null || EstaRoto(r.sharedMaterial))
            {
                return false;
            }
            Vector3 e = t.lossyScale;
            float ancho = Mathf.Max(e.x, e.z);
            string ruta = CarpetaMateriales + "/T1503_Muro_" + t.name + ".mat";
            Material copia = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (copia == null)
            {
                copia = new Material(r.sharedMaterial);
                AssetDatabase.CreateAsset(copia, ruta);
            }
            copia.mainTextureScale = new Vector2(Mathf.Max(1f, ancho / MetrosPorRepeticion), Mathf.Max(1f, e.y / MetrosPorRepeticion));
            EditorUtility.SetDirty(copia);
            r.sharedMaterial = copia;
            EditorUtility.SetDirty(r);
            return true;
        }

        private static IEnumerable<Renderer> RenderersArmeria()
        {
            foreach (string ruta in new[] { RaizCalle, RaizTienda, RaizEntrada, "Barrio/Manzana_2/Lugar_MercadoNegro/Interior" })
            {
                Transform raiz = BuscarRuta(ruta);
                if (raiz == null)
                {
                    continue;
                }
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    yield return r;
                }
            }
        }

        // ------------------------------------------------------------ calle

        [MenuItem("Mecha Cyberpunk/Mundo/T-1503 Armeria: arreglar calle")]
        public static void ArreglarCalle()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject calle = GameObject.Find(RaizCalle);
            if (calle == null)
            {
                throw new System.Exception("T-1503: falta " + RaizCalle + " en " + RutaEscena);
            }
            Physics.SyncTransforms();
            int repetidos = 0, estorbos = 0, ajustados = 0;
            HashSet<string> vistos = new HashSet<string>();
            foreach (Transform t in calle.transform.Cast<Transform>().ToList())
            {
                if (!t.gameObject.activeSelf || t.GetComponentInChildren<Renderer>(true) == null)
                {
                    continue;
                }
                Vector3 p = t.position;
                string clave = t.name + "|" + Mathf.Round(p.x * 10f) + "|" + Mathf.Round(p.y * 10f) + "|" + Mathf.Round(p.z * 10f);
                if (!vistos.Add(clave))
                {
                    t.gameObject.SetActive(false);
                    repetidos++;
                    continue;
                }
                Bounds b = Limites(t);
                if (b.size.x < 4f && b.size.z < 4f && b.max.y < 4f && PasoPuerta.Intersects(b) && t.GetComponentInChildren<Light>(true) == null)
                {
                    t.gameObject.SetActive(false);
                    estorbos++;
                    continue;
                }
                float ajuste = AjusteAlPiso(t, b);
                if (Mathf.Abs(ajuste) >= UmbralAjustePiso && Mathf.Abs(ajuste) <= MaximoAjustePiso)
                {
                    t.position += Vector3.up * ajuste;
                    EditorUtility.SetDirty(t);
                    ajustados++;
                }
            }
            Guardar();
            Debug.Log("[Mundo] T-1503: calle de la armería: " + repetidos + " repetidos apagados, " + estorbos + " estorbos del paso apagados, " + ajustados + " piezas asentadas en el piso.");
        }

        // Cuánto hay que subir (+) o bajar (-) la pieza para que su base toque el piso que tiene debajo; 0 si no hay piso o es cosa colgada.
        private static float AjusteAlPiso(Transform t, Bounds b)
        {
            RaycastHit[] golpes = Physics.RaycastAll(new Vector3(b.center.x, b.max.y + 0.5f, b.center.z), Vector3.down, b.size.y + 3f, ~(1 << 4), QueryTriggerInteraction.Ignore);
            float mejor = float.NegativeInfinity;
            foreach (RaycastHit h in golpes)
            {
                if (h.collider.transform.IsChildOf(t) || h.normal.y < 0.9f || h.point.y > b.min.y + MaximoAjustePiso)
                {
                    continue;
                }
                mejor = Mathf.Max(mejor, h.point.y);
            }
            return float.IsNegativeInfinity(mejor) ? 0f : mejor - b.min.y;
        }

        // ------------------------------------------------------------ utilidades

        public static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(t.position, Vector3.zero);
            bool primero = true;
            foreach (Renderer r in rs)
            {
                if (primero)
                {
                    b = r.bounds;
                    primero = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        private static Transform BuscarRuta(string ruta)
        {
            int i = ruta.IndexOf('/');
            GameObject r = GameObject.Find(i < 0 ? ruta : ruta.Substring(0, i));
            return r == null ? null : i < 0 ? r.transform : r.transform.Find(ruta.Substring(i + 1));
        }

        private static void Guardar()
        {
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }
    }
}
