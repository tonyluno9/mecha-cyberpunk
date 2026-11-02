using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-520: conecta los visuales de Tripo (woman, man, woman2) al juego en lugar del muñeco de prueba de T-400: prefabs del jugador, aliado,
    /// pandillero y peatón, y los personajes de la escena Ciudad_Prologo (vecina, vendedor, reclutables, apoyos). Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Conectar
    /// </summary>
    public static class ConectarVisualesT520
    {
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string RutaAliado = "Assets/_Proyecto/IA/Prefabs/Aliado.prefab";
        public const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        public const string RutaPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaCreacion = "Assets/_Proyecto/UI/Titulo/PantallaCreacionPandilla.prefab";
        public const string RutaDatosCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        public const string RutaDatosJefeCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeCalaveras.asset";
        public const string CarpetaMateriales = "Assets/Arte/Personajes/Materiales";
        public const float EscalaJefe = 1.15f;
        private const float MezclaApoyo = 0.75f;
        private const float AlturaDesdeLaQueBuscaPiso = 0.4f;

        public static readonly Color ColorCuerpoSkulls = new Color(0.78f, 0.72f, 0.82f);
        public static readonly Color ColorSkulls = new Color(0.13f, 0.12f, 0.16f);
        public static readonly Color ColorApagadoVecina = new Color(0.66f, 0.6f, 0.52f);
        public static readonly Color ColorApagadoVendedor = new Color(0.52f, 0.62f, 0.62f);
        public static readonly Color[] ColoresPeatones =
        {
            new Color(0.55f, 0.65f, 0.95f), new Color(0.95f, 0.55f, 0.55f), new Color(0.6f, 0.9f, 0.65f), new Color(0.95f, 0.85f, 0.5f),
        };

        /// <summary>Todo T-520 en orden: visuales de Tripo, visual de prueba con los nombres nuevos y conexión al juego.</summary>
        [MenuItem("Mecha Cyberpunk/Personajes/T-520 Todo")]
        public static void Todo()
        {
            CreadorVisualTripo.CrearTodos();
            CreadorVisualPrueba.Crear();
            Conectar();
        }

        [MenuItem("Mecha Cyberpunk/Personajes/T-520 Conectar visuales")]
        public static void Conectar()
        {
            ConectarJugador();
            ConectarEnPrefab(RutaAliado, "Visual_woman2", "Modelo");
            ConectarEnPrefab(RutaPandillero, "Visual_man", "Modelo");
            ConectarPeaton();
            AjustarDatosSkulls();
            ConectarCreacion();
            ConectarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-520: visuales conectados.");
        }

        public static GameObject Visual(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(nombre));

        // ---------- Materiales ----------

        public static Material Variante(string cuerpo, string nombre, Color color, Color emision = default, Texture textura = null)
        {
            string ruta = CarpetaMateriales + "/" + cuerpo + "_" + nombre + ".mat";
            Directory.CreateDirectory(CarpetaMateriales);
            Material baseMat = CreadorVisualTripo.AsegurarMaterialBase(cuerpo);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(baseMat);
                AssetDatabase.CreateAsset(m, ruta);
            }
            else
            {
                m.CopyPropertiesFromMaterial(baseMat);
            }
            m.SetColor("_BaseColor", color);
            if (textura != null)
            {
                m.SetTexture("_BaseMap", textura);
            }
            if (emision.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", emision);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Franja()
        {
            string ruta = CarpetaMateriales + "/Franja_Magenta.mat";
            Directory.CreateDirectory(CarpetaMateriales);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", Color.black);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", new Color(1.7f, 0.05f, 1.3f));
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- Prefabs ----------

        private static void ConectarJugador()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                Transform viejo = contenido.transform.Find("ModeloJugador");
                if (viejo != null && viejo.GetComponent<VisualPersonaje>() != null)
                {
                    return; // ya conectado
                }
                GameObject nuevo = Reemplazar(contenido, viejo, Visual("woman"), "ModeloJugador", true);
                SerializedObject so = new SerializedObject(contenido.GetComponent("AnimacionJugador"));
                so.FindProperty("animador").objectReferenceValue = nuevo.GetComponentInChildren<Animator>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void ConectarEnPrefab(string ruta, string visual, string nombreHijo)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform viejo = contenido.transform.Find(nombreHijo);
                if (viejo != null && viejo.GetComponent<VisualPersonaje>() != null)
                {
                    return;
                }
                Reemplazar(contenido, viejo, Visual(visual.Replace("Visual_", string.Empty)), nombreHijo, false);
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void ConectarPeaton()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPeaton);
            try
            {
                if (contenido.GetComponentInChildren<VisualPersonaje>(true) != null)
                {
                    return;
                }
                for (int i = contenido.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(contenido.transform.GetChild(i).gameObject);
                }
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(Visual("woman2"), contenido.transform);
                visual.name = "Visual";
                var materiales = new List<Material>();
                for (int i = 0; i < ColoresPeatones.Length; i++)
                {
                    materiales.Add(Variante("woman2", "Peaton" + (i + 1), ColoresPeatones[i]));
                }
                visual.AddComponent<VarianteVisual>().Configurar(materiales.ToArray(), -1, null);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPeaton);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>SKULLS: cuerpo negro (el color del juego multiplica la textura); las franjas magenta las pone VarianteVisual.</summary>
        private static void AjustarDatosSkulls()
        {
            foreach (string ruta in new[] { RutaDatosCalaveras, RutaDatosJefeCalaveras })
            {
                Object datos = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                if (datos == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(datos);
                so.FindProperty("colorNormal").colorValue = ColorSkulls;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
            }
        }

        private static void ConectarCreacion()
        {
            if (!File.Exists(RutaCreacion))
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaCreacion);
            try
            {
                Component pantalla = contenido.GetComponent("PantallaCreacionPandilla");
                SerializedObject so = new SerializedObject(pantalla);
                SerializedProperty lista = so.FindProperty("modelos");
                lista.arraySize = 2;
                lista.GetArrayElementAtIndex(0).objectReferenceValue = Visual("woman");
                lista.GetArrayElementAtIndex(1).objectReferenceValue = Visual("man");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaCreacion);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // ---------- Escena ----------

        public static void ConectarEscena()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Material franja = Franja();

            // El jugador elige entre mujer y hombre.
            foreach (Component c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c != null && c.GetType().Name == "ModeloSegunPerfil")
                {
                    SerializedObject so = new SerializedObject(c);
                    SerializedProperty lista = so.FindProperty("modelos");
                    lista.arraySize = 2;
                    lista.GetArrayElementAtIndex(0).objectReferenceValue = Visual("woman");
                    lista.GetArrayElementAtIndex(1).objectReferenceValue = Visual("man");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.gameObject.scene != escena)
                {
                    continue;
                }
                string nombre = t.name;
                Component pandillero = t.GetComponent("PandilleroAPie");
                if (pandillero != null)
                {
                    VestirPandillero(t, pandillero, franja);
                }
                else if (nombre == "DonaLupe")
                {
                    ReemplazarModeloSuelto(t, "ModeloHumano", "woman2", Variante("woman2", "Vecina", ColorApagadoVecina));
                }
                else if (nombre.StartsWith("Reclutable_"))
                {
                    VestirReclutable(t);
                }
                else if (nombre == "Vendedor")
                {
                    ColocarSobreElPiso(t, "woman2", Variante("woman2", "Vendedor", ColorApagadoVendedor), new[] { "Cuerpo", "Piernas", "Cabeza" });
                }
                else if (t.GetComponent("PersonajeApoyo") != null)
                {
                    VestirApoyo(t);
                }
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        private static void VestirPandillero(Transform raiz, Component pandillero, Material franja)
        {
            SerializedObject so = new SerializedObject(pandillero);
            Object datos = so.FindProperty("datos").objectReferenceValue;
            string datosNombre = datos != null ? datos.name : string.Empty;
            bool jefe = raiz.name == "Jefe" || datosNombre.StartsWith("DatosPandilleroAPie_Jefe");
            bool skulls = datosNombre.Contains("Calaveras");
            if (jefe)
            {
                raiz.localScale = Vector3.one * EscalaJefe;
            }
            Transform modelo = raiz.Find("Modelo");
            if (modelo == null)
            {
                return;
            }
            VarianteVisual v = modelo.GetComponent<VarianteVisual>();
            if (v == null)
            {
                v = modelo.gameObject.AddComponent<VarianteVisual>();
            }
            // T-510: man ya tiene su textura (antes salía negro). SKULLS: la textura natural (negro con magenta) un poco más oscura y con franjas;
            // SNAKES y NOVA: la misma textura con el magenta cambiado a verde o azul.
            if (skulls)
            {
                Material cuerpo = Variante("man", "Skulls", ColorCuerpoSkulls);
                v.Configurar(new[] { cuerpo }, 0, franja);
            }
            else if (datosNombre.Contains("Viboras"))
            {
                v.Configurar(new[] { Variante("man", "Viboras", Color.white, default, ConectarVisualesT510.TexturaMan("verde")) }, 0, null);
            }
            else if (datosNombre.Contains("Nova"))
            {
                v.Configurar(new[] { Variante("man", "Nova", Color.white, default, ConectarVisualesT510.TexturaMan("azul")) }, 0, null);
            }
            EditorUtility.SetDirty(v);
        }

        private static void VestirReclutable(Transform raiz)
        {
            string cuerpo = raiz.name.Contains("CHATO") ? "woman2" : raiz.name.Contains("GÜERA") ? "woman" : "man";
            ReemplazarModeloSuelto(raiz, "Modelo", cuerpo, null);
        }

        /// <summary>Cuerpo y color de cada personaje de apoyo: DOC, INVENTOR y HACKER con su modelo (T-510), DIRECTOR violeta y MECÁNICO amarillo apagado.</summary>
        public static readonly (string id, string cuerpo, Color color)[] ApoyosDeLaDemo =
        {
            ("doc", "Doctor", Color.white),
            ("inventor", "Inventor", Color.white),
            ("hacker", "Hacker", Color.white),
            ("director", "woman", new Color(0.8f, 0.62f, 1f)),
            ("mecanico", "woman2", new Color(0.9f, 0.85f, 0.4f)),
        };

        private static void VestirApoyo(Transform raiz)
        {
            Component apoyo = raiz.GetComponent("PersonajeApoyo");
            Object datos = new SerializedObject(apoyo).FindProperty("datos").objectReferenceValue;
            string id = datos != null ? new SerializedObject(datos).FindProperty("id").stringValue : string.Empty;
            foreach ((string idApoyo, string cuerpo, Color color) in ApoyosDeLaDemo)
            {
                if (idApoyo == id)
                {
                    // T-510: DOC, INVENTOR y HACKER tienen su propio modelo (con su textura, sin tinte); los demás, el color propio mezclado con blanco.
                    ColocarSobreElPiso(raiz, cuerpo, Variante(cuerpo, "Apoyo_" + id, Color.Lerp(Color.white, color, MezclaApoyo)), new[] { "Cuerpo", "Visor" });
                }
            }
        }

        /// <summary>Quita las piezas de cubo, pone el visual de <paramref name="cuerpo"/> con el material y lo apoya en el piso.</summary>
        public static void ColocarSobreElPiso(Transform raiz, string cuerpo, Material material, string[] quitar)
        {
            if (raiz.GetComponentInChildren<VisualPersonaje>(true) != null)
            {
                return;
            }
            foreach (string n in quitar)
            {
                Transform t = raiz.Find(n);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(Visual(cuerpo), raiz);
            visual.name = "Visual";
            visual.transform.localRotation = Quaternion.identity;
            Vector3 origen = raiz.position + Vector3.up * AlturaDesdeLaQueBuscaPiso; // sobre el piso y bajo el mostrador
            Vector3 pies = raiz.position;
            if (Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, 6f, ~0, QueryTriggerInteraction.Ignore) && !golpe.collider.transform.IsChildOf(raiz))
            {
                pies = golpe.point;
            }
            visual.transform.position = pies;
            visual.AddComponent<VarianteVisual>().Configurar(new[] { material }, 0, null);
        }

        private static void ReemplazarModeloSuelto(Transform raiz, string nombreViejo, string cuerpo, Material material)
        {
            Transform viejo = raiz.Find(nombreViejo);
            if (viejo == null || viejo.GetComponent<VisualPersonaje>() != null)
            {
                return;
            }
            GameObject nuevo = Reemplazar(raiz.gameObject, viejo, Visual(cuerpo), nombreViejo == "Modelo" ? "Modelo" : "Visual", false);
            if (material != null)
            {
                nuevo.AddComponent<VarianteVisual>().Configurar(new[] { material }, 0, null);
            }
        }

        // ---------- Reemplazo conservando las referencias ----------

        /// <summary>
        /// Instancia <paramref name="visual"/> en lugar de <paramref name="viejo"/> (misma posición local), pasa las armas de la mano si se pide,
        /// y reapunta las referencias serializadas del resto de la jerarquía que miraban al modelo viejo: Animator al nuevo, el GameObject raíz al nuevo
        /// y cualquier otra (piezas de cubo, renderers) se vacía.
        /// </summary>
        public static GameObject Reemplazar(GameObject raiz, Transform viejo, GameObject visual, string nombre, bool pasarArmas)
        {
            Transform padre = viejo != null ? viejo.parent : raiz.transform;
            GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(visual, padre);
            nuevo.name = nombre;
            Animator animadorNuevo = nuevo.GetComponentInChildren<Animator>(true);
            if (viejo != null)
            {
                nuevo.transform.localPosition = viejo.localPosition;
                nuevo.transform.localRotation = viejo.localRotation;
                if (pasarArmas)
                {
                    MechaCyberpunk.Personaje.Apariencia.ModeloSegunPerfil.PasarArmas(viejo, nuevo.transform);
                }
                Reapuntar(raiz, viejo, nuevo, animadorNuevo);
                Object.DestroyImmediate(viejo.gameObject);
            }
            return nuevo;
        }

        private static void Reapuntar(GameObject raiz, Transform viejo, GameObject nuevo, Animator animadorNuevo)
        {
            foreach (Component c in raiz.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c.transform.IsChildOf(viejo) || c.transform.IsChildOf(nuevo.transform))
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                bool cambio = false;
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null)
                    {
                        continue;
                    }
                    Transform objetivo = p.objectReferenceValue is Component comp ? comp.transform
                        : p.objectReferenceValue is GameObject go ? go.transform : null;
                    if (objetivo == null || !objetivo.IsChildOf(viejo))
                    {
                        continue;
                    }
                    if (p.objectReferenceValue is Animator)
                    {
                        p.objectReferenceValue = animadorNuevo;
                    }
                    else if (p.objectReferenceValue is GameObject g && g == viejo.gameObject)
                    {
                        p.objectReferenceValue = nuevo;
                    }
                    else
                    {
                        Debug.Log("[Personaje] T-520: " + c.GetType().Name + "." + p.propertyPath + " apuntaba a " + objetivo.name + " del modelo viejo: queda vacío.");
                        p.objectReferenceValue = null;
                    }
                    cambio = true;
                }
                if (cambio)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
