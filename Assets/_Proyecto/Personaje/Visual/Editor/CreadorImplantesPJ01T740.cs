using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-740 (A9): pone en el prefab Visual_PJ01 las piezas de PJ01_Implantes.fbx. El FBX trae las piezas ya pesadas a los mismos 78 huesos de PJ01 (IMP01 piernas, IMP02 brazos, IMP03 visor de cabeza,
    /// niveles 1 y 2, más PJ01_Malla que es el cuerpo y no se usa), así que cada una entra como SkinnedMeshRenderer con los huesos del modelo del jugador (por nombre) y se muestra con
    /// ImplantesVisiblesPJ01 según IImplantes. Textura de cada pieza: la de su zip de Tripo. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorImplantesPJ01T740.Crear
    /// </summary>
    public static class CreadorImplantesPJ01T740
    {
        public const string RutaFbx = "Assets/Arte/Personajes/PJ01/Implantes/PJ01_Implantes.fbx";
        public const string CarpetaTexturas = "Assets/Arte/Personajes/PJ01/Implantes/Texturas/";
        public const string CarpetaMateriales = "Assets/Arte/Personajes/PJ01/Implantes/Materiales/";
        public const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        public const string NombreContenedor = "Implantes";

        public static readonly (string Nombre, EspacioImplante Espacio, int Nivel)[] Piezas =
        {
            ("IMP01_LVL1", EspacioImplante.Piernas, 1), ("IMP01_LVL2", EspacioImplante.Piernas, 2),
            ("IMP02_LVL1", EspacioImplante.Brazos, 1), ("IMP02_LVL2", EspacioImplante.Brazos, 2),
            ("IMP03_LVL1", EspacioImplante.Ojos, 1), ("IMP03_LVL2", EspacioImplante.Ojos, 2),
        };

        [MenuItem("Mecha Cyberpunk/T-740/Poner implantes visibles en PJ01")]
        public static void Crear()
        {
            Importar();
            var materiales = new Dictionary<string, Material>();
            foreach (var p in Piezas) { materiales[p.Nombre] = CrearMaterial(p.Nombre); }
            AssetDatabase.SaveAssets();
            PonerEnPrefab(materiales);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Mismo Scale Factor que PJ01 (1.88: el FBX mide 0.98 m y en el juego 1.84): sin eso las piezas salen a la mitad de tamaño y quedan dentro del cuerpo.</summary>
        public static void Importar()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx);
            var pj = (ModelImporter)AssetImporter.GetAtPath(CreadorPJ01T732.RutaFbx);
            // El postprocesador de personajes de Tripo (PostprocesadorAssetsT250) toma la escala del userData ("escala=1.88"), igual que PJ01.
            imp.userData = pj.userData;
            imp.globalScale = pj.globalScale;
            imp.useFileScale = pj.useFileScale;
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
        }

        private static Material CrearMaterial(string nombre)
        {
            string rutaTex = CarpetaTexturas + nombre + ".jpg";
            Directory.CreateDirectory(CarpetaMateriales);
            string ruta = CarpetaMateriales + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.35f);
            m.SetFloat("_Metallic", 0.3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void PonerEnPrefab(Dictionary<string, Material> materiales)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaVisual);
            try
            {
                Transform modelo = contenido.transform.Find("Giro/Modelo");
                var huesos = new Dictionary<string, Transform>();
                foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true)) { huesos[t.name] = t; }
                Transform viejo = modelo.Find(NombreContenedor);
                if (viejo != null) { Object.DestroyImmediate(viejo.gameObject); }
                var contenedor = new GameObject(NombreContenedor).transform;
                contenedor.SetParent(modelo, false);
                var lista = new List<ImplantesVisiblesPJ01.Pieza>();
                foreach (var p in Piezas)
                {
                    SkinnedMeshRenderer origen = null;
                    foreach (SkinnedMeshRenderer r in fbx.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { if (r.name == p.Nombre) { origen = r; } }
                    if (origen == null) { throw new System.Exception("T-740: el FBX no trae " + p.Nombre); }
                    var go = new GameObject(p.Nombre);
                    go.transform.SetParent(contenedor, false);
                    SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
                    smr.sharedMesh = origen.sharedMesh;
                    var bones = new Transform[origen.bones.Length];
                    for (int i = 0; i < bones.Length; i++)
                    {
                        if (!huesos.TryGetValue(origen.bones[i].name, out bones[i])) { throw new System.Exception("T-740: PJ01 no tiene el hueso " + origen.bones[i].name); }
                    }
                    smr.bones = bones;
                    smr.rootBone = huesos[origen.rootBone.name];
                    smr.localBounds = origen.localBounds;
                    smr.sharedMaterial = materiales[p.Nombre];
                    smr.updateWhenOffscreen = false;
                    go.SetActive(false);
                    lista.Add(new ImplantesVisiblesPJ01.Pieza { espacio = p.Espacio, nivel = p.Nivel, objeto = go });
                }
                ImplantesVisiblesPJ01 visibles = contenedor.gameObject.AddComponent<ImplantesVisiblesPJ01>();
                visibles.Configurar(lista.ToArray());
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaVisual);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }
}
