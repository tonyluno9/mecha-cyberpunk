using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1527: ajusta los colliders de los props de calle (postes, señales, semáforos, faroles, árboles, bolardos...) a su forma, porque colliders
    /// grandes frenaban las balas lejos de la mira (a 100 m pegaban en un poste). Diagnostico() solo informa; Aplicar() ajusta. Idempotente.
    /// Postes y faroles: una cápsula delgada. Árboles: una cápsula del tronco. Lo demás: una caja del tamaño del render.
    /// En la escena solo toca objetos sueltos (no instancias de prefab: esas se arreglan en su prefab).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CollidersPropsT1527.Diagnostico / .Aplicar
    /// </summary>
    public static class CollidersPropsT1527
    {
        public static readonly string[] CarpetasPrefabs = { "Assets/Arte/Calle", "Assets/Arte/Zonas", "Assets/_Proyecto/Mundo/Prefabs" };
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private static readonly Regex Props = new Regex("poste|farol|semaforo|senal|se.al|letrero|lampara|bolardo|hidrante|arbol|valla|transformador", RegexOptions.IgnoreCase);
        private static readonly Regex Delgados = new Regex("poste|farol|semaforo|lampara|bolardo|transformador", RegexOptions.IgnoreCase);
        private static readonly Regex Arboles = new Regex("arbol", RegexOptions.IgnoreCase);

        /// <summary>Un collider cuenta como grande si pasa del render por esta fracción más <see cref="HolguraM"/>.</summary>
        public const float ExcesoFraccion = 0.25f;
        public const float HolguraM = 0.1f;
        public const float RadioMinimoPoste = 0.06f;
        public const float RadioMaximoPoste = 0.2f;
        public const float RadioMinimoTronco = 0.1f;
        public const float RadioMaximoTronco = 0.35f;
        public const float AlturaMaximaTronco = 4f;
        public const float RelacionDelgado = 3f;

        public enum Forma { Caja, Capsula }

        private struct Cuenta
        {
            public int revisados, grandes, ajustados;
            public bool cambio;
        }

        [MenuItem("Mecha Cyberpunk/T-1527/Diagnostico colliders de props")]
        public static void Diagnostico() => Ejecutar(false);

        [MenuItem("Mecha Cyberpunk/T-1527/Aplicar colliders de props")]
        public static void Aplicar() => Ejecutar(true);

        private static void Ejecutar(bool aplicar)
        {
            Cuenta total = default;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", CarpetasPrefabs))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!Props.IsMatch(System.IO.Path.GetFileNameWithoutExtension(ruta)))
                {
                    continue;
                }
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    Cuenta c = default;
                    Procesar(raiz.transform, ruta, aplicar, ref c);
                    if (c.cambio && aplicar)
                    {
                        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    }
                    Sumar(ref total, c);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            if (System.IO.File.Exists(RutaEscena))
            {
                Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
                Cuenta c = default;
                foreach (GameObject r in escena.GetRootGameObjects())
                {
                    BuscarEnEscena(r.transform, aplicar, ref c);
                }
                if (c.cambio && aplicar)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                }
                Sumar(ref total, c);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1527] colliders de props (" + (aplicar ? "Aplicar" : "Diagnostico") + "): " + total.revisados + " props revisados, " + total.grandes + " con collider grande, " + total.ajustados + " ajustados.");
        }

        private static void Sumar(ref Cuenta total, Cuenta c)
        {
            total.revisados += c.revisados;
            total.grandes += c.grandes;
            total.ajustados += c.ajustados;
        }

        private static void BuscarEnEscena(Transform t, bool aplicar, ref Cuenta c)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(t.gameObject))
            {
                return; // viene de un prefab: se arregla en el prefab
            }
            if (Props.IsMatch(t.name) && t.GetComponentInChildren<Collider>(true) != null)
            {
                Revisar(t, "escena", aplicar, ref c);
                return;
            }
            for (int i = 0; i < t.childCount; i++)
            {
                BuscarEnEscena(t.GetChild(i), aplicar, ref c);
            }
        }

        // Busca el prop (el objeto de más arriba que coincide por nombre) y lo revisa; si la raíz no coincide, recorre los hijos.
        private static void Procesar(Transform t, string ruta, bool aplicar, ref Cuenta c)
        {
            if (Props.IsMatch(t.name))
            {
                Revisar(t, ruta, aplicar, ref c);
                return;
            }
            for (int i = 0; i < t.childCount; i++)
            {
                Procesar(t.GetChild(i), ruta, aplicar, ref c);
            }
        }

        private static void Revisar(Transform prop, string ruta, bool aplicar, ref Cuenta c)
        {
            if (!BoundsLocales(prop, prop.GetComponentsInChildren<Renderer>(), out Bounds render))
            {
                return;
            }
            List<Collider> colliders = new List<Collider>();
            foreach (Collider col in prop.GetComponentsInChildren<Collider>(true))
            {
                if (!col.isTrigger)
                {
                    colliders.Add(col);
                }
            }
            if (colliders.Count == 0)
            {
                return;
            }
            c.revisados++;
            BoundsLocales(prop, colliders.ToArray(), out Bounds collider);
            bool grande = Excede(collider.size.x, render.size.x) || Excede(collider.size.y, render.size.y) || Excede(collider.size.z, render.size.z);
            // Postes, faroles, transformadores y árboles: una caja del tamaño de todo el modelo (con el brazo o la copa) también es
            // demasiado grande para las balas. Si su forma debe ser cápsula y no es ya una sola cápsula delgada, se cambia.
            if (!grande && ElegirForma(prop.name, render.size) == Forma.Capsula)
            {
                // Ya está bien si tiene un tronco de cápsula delgada y, a lo más, cajas chicas de base (diseño de T-1340).
                bool tronco = colliders.Any(x => x is CapsuleCollider capsula && capsula.radius <= RadioMaximoTronco + 0.01f);
                bool cajasChicas = colliders.All(x => !(x is BoxCollider) || Mathf.Max(x.bounds.size.x, x.bounds.size.z) <= 1.5f);
                bool yaDelgada = tronco && cajasChicas;
                grande = !yaDelgada;
            }
            Debug.Log("[T-1527] " + ruta + " / " + prop.name + ": render " + render.size.ToString("F2") + " collider " + collider.size.ToString("F2") + (grande ? " GRANDE" : " ok"));
            if (!grande)
            {
                return;
            }
            c.grandes++;
            if (!aplicar)
            {
                return;
            }
            foreach (Collider col in colliders)
            {
                Object.DestroyImmediate(col);
            }
            Forma forma = ElegirForma(prop.name, render.size);
            if (forma == Forma.Capsula)
            {
                CapsuleCollider cap = prop.gameObject.AddComponent<CapsuleCollider>();
                float radio = Mathf.Clamp(Mathf.Min(render.size.x, render.size.z) * 0.5f, RadioMinimoPoste, RadioMaximoPoste);
                float alto = render.size.y;
                if (Arboles.IsMatch(prop.name))
                {
                    radio = Mathf.Clamp(Mathf.Min(render.size.x, render.size.z) * 0.08f, RadioMinimoTronco, RadioMaximoTronco);
                    alto = Mathf.Min(alto, AlturaMaximaTronco);
                }
                cap.direction = 1;
                cap.radius = radio;
                cap.height = Mathf.Max(alto, radio * 2f);
                // El poste o tronco suele estar en el pivote del prop; el centro de los bounds se corre hacia el brazo o la copa.
                // Se usa el centro de los vértices del primer metro de la malla (la base del poste o del tronco).
                Vector2 basePie = CentroDeLaBase(prop, render.min.y, new Vector2(render.center.x, render.center.z));
                cap.center = new Vector3(basePie.x, render.min.y + cap.height * 0.5f, basePie.y);
            }
            else
            {
                BoxCollider caja = prop.gameObject.AddComponent<BoxCollider>();
                caja.center = render.center;
                caja.size = render.size;
            }
            c.ajustados++;
            c.cambio = true;
            Debug.Log("[T-1527] " + prop.name + ": collider reemplazado por " + forma + ".");
        }

        // Centro (x, z) en el espacio local del prop de los vértices que quedan a menos de 1 m del piso del modelo.
        private static Vector2 CentroDeLaBase(Transform prop, float pisoLocal, Vector2 respaldo)
        {
            Vector2 suma = Vector2.zero;
            int n = 0;
            foreach (MeshFilter mf in prop.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null)
                {
                    continue;
                }
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    Vector3 local = prop.InverseTransformPoint(mf.transform.TransformPoint(v));
                    if (local.y <= pisoLocal + 1f)
                    {
                        suma += new Vector2(local.x, local.z);
                        n++;
                    }
                }
            }
            return n > 0 ? suma / n : respaldo;
        }

        /// <summary>Árboles: cápsula del tronco. Objetos altos y delgados con nombre de poste, farol, semáforo, lámpara o bolardo: cápsula. El resto: caja.</summary>
        public static Forma ElegirForma(string nombre, Vector3 render)
        {
            if (Arboles.IsMatch(nombre))
            {
                return Forma.Capsula;
            }
            bool delgado = render.y >= RelacionDelgado * Mathf.Max(0.01f, Mathf.Min(render.x, render.z));
            return Delgados.IsMatch(nombre) && delgado ? Forma.Capsula : Forma.Caja;
        }

        public static bool Excede(float collider, float render) => collider > render * (1f + ExcesoFraccion) + HolguraM;

        // Bounds en el espacio local del prop a partir de los bounds del mundo de cada renderer o collider.
        private static bool BoundsLocales(Transform raiz, Component[] cosas, out Bounds resultado)
        {
            resultado = default;
            bool hay = false;
            foreach (Component comp in cosas)
            {
                Bounds mundo;
                if (comp is Renderer r)
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                    {
                        continue;
                    }
                    mundo = r.bounds;
                }
                else if (comp is Collider col)
                {
                    mundo = col.bounds;
                }
                else
                {
                    continue;
                }
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = mundo.center + Vector3.Scale(mundo.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = raiz.InverseTransformPoint(esquina);
                    if (!hay)
                    {
                        resultado = new Bounds(local, Vector3.zero);
                        hay = true;
                    }
                    else
                    {
                        resultado.Encapsulate(local);
                    }
                }
            }
            return hay;
        }
    }
}
