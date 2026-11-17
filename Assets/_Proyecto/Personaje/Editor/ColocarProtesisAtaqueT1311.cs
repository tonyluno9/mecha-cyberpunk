using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1311: mete PJ01_Implantes_Ataque.fbx (brazo láser IMP04 pesado a R_Forearm, hombrera de misiles IMP05 pesada a Spine02, y los vacíos IMP04_Salida e IMP05_Salida)
    /// al cuerpo de PJ01 (Visual_PJ01, que usa JugadorAPie). Mismo método que T-740 (CreadorImplantesPJ01T740): las mallas del fbx comparten los 78 huesos de PJ01, así que cada pieza
    /// entra como SkinnedMeshRenderer con los huesos del modelo del jugador (por nombre), apagada. Los vacíos de salida se copian como hijos del mismo hueso. Escala de importación
    /// igual a la de PJ01_Implantes.fbx. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ColocarProtesisAtaqueT1311.Aplicar
    /// </summary>
    public static class ColocarProtesisAtaqueT1311
    {
        public const string CarpetaImplantes = "Assets/Arte/Personajes/PJ01/Implantes/";
        public const string RutaFbx = CarpetaImplantes + "PJ01_Implantes_Ataque.fbx";
        public const string RutaFbxReferencia = CarpetaImplantes + "PJ01_Implantes.fbx";
        public const string CarpetaTexturas = CarpetaImplantes + "Texturas/";
        public const string CarpetaMateriales = CarpetaImplantes + "Materiales/";
        public const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        public const string NombreContenedor = "ImplantesAtaque";
        public const string NombreSalidaLaser = "SalidaLaser";
        public const string NombreSalidaMisiles = "SalidaMisiles";

        private const float Metalico = 0.5f;
        private const float Suavidad = 0.5f;

        private const string PiezaLaser = "IMP04_BrazoLaser";
        private const string PiezaMisiles = "IMP05_HombreraMisiles";
        private const string VacioLaser = "IMP04_Salida";
        private const string VacioMisiles = "IMP05_Salida";

        [MenuItem("Mecha Cyberpunk/T-1311/Poner prótesis de ataque en PJ01")]
        public static void Aplicar()
        {
            if (AssetDatabase.LoadMainAssetAtPath(RutaFbx) == null)
            {
                Debug.LogError("[T-1311] Falta " + RutaFbx);
                return;
            }
            Importar();
            Directory.CreateDirectory(CarpetaMateriales);
            Material matLaser = CrearMaterial("IMP04");
            Material matMisiles = CrearMaterial("IMP05");
            AssetDatabase.SaveAssets();
            PonerEnPrefab(matLaser, matMisiles);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1311] Prótesis de ataque puestas en " + RutaVisual);
        }

        /// <summary>Escala igual a la de PJ01_Implantes.fbx (userData escala=1.88, globalScale, useFileScale); sin animación ni materiales del fbx.</summary>
        private static void Importar()
        {
            AssetDatabase.Refresh();
            foreach (string codigo in new[] { "IMP04", "IMP05" })
            {
                var normal = AssetImporter.GetAtPath(CarpetaTexturas + codigo + "_Ataque_normal.jpg") as TextureImporter;
                if (normal != null && normal.textureType != TextureImporterType.NormalMap)
                {
                    normal.textureType = TextureImporterType.NormalMap;
                    normal.SaveAndReimport();
                }
            }
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx);
            var referencia = (ModelImporter)AssetImporter.GetAtPath(RutaFbxReferencia);
            imp.userData = referencia.userData;
            imp.globalScale = referencia.globalScale;
            imp.useFileScale = referencia.useFileScale;
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            Debug.Log("[T-1311] Importación ajustada: escala " + imp.globalScale + " userData '" + imp.userData + "'");
        }

        private static Material CrearMaterial(string codigo)
        {
            string ruta = CarpetaMateriales + codigo + "_Ataque.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + codigo + "_Ataque_basecolor.jpg"));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", Metalico);
            m.SetFloat("_Smoothness", Suavidad);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + codigo + "_Ataque_normal.jpg");
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            // Sin emisión en estas piezas; si se agrega, usar MaterialGlobalIlluminationFlags.RealtimeEmissive para que no se pierda _EMISSION al reimportar.
            EditorUtility.SetDirty(m);
            return m;
        }

        private static SkinnedMeshRenderer BuscarMalla(GameObject fbx, string nombre)
        {
            foreach (SkinnedMeshRenderer r in fbx.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.name == nombre) { return r; }
            }
            throw new Exception("T-1311: el FBX no trae la malla " + nombre);
        }

        private static GameObject CrearPieza(Transform contenedor, SkinnedMeshRenderer origen, Dictionary<string, Transform> huesos, Material material)
        {
            var go = new GameObject(origen.name);
            go.transform.SetParent(contenedor, false);
            SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = origen.sharedMesh;
            var bones = new Transform[origen.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                if (!huesos.TryGetValue(origen.bones[i].name, out bones[i])) { throw new Exception("T-1311: PJ01 no tiene el hueso " + origen.bones[i].name); }
            }
            smr.bones = bones;
            smr.rootBone = huesos[origen.rootBone.name];
            smr.localBounds = origen.localBounds;
            var materiales = new Material[Mathf.Max(1, origen.sharedMesh.subMeshCount)];
            for (int i = 0; i < materiales.Length; i++) { materiales[i] = material; }
            smr.sharedMaterials = materiales;
            smr.updateWhenOffscreen = false;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return go;
        }

        // Copia el vacío de salida del fbx como hijo del mismo hueso (por nombre) del cuerpo de PJ01, con su pose local.
        private static Transform CrearSalida(GameObject fbx, string vacio, string nombre, Dictionary<string, Transform> huesos)
        {
            Transform origen = null;
            foreach (Transform t in fbx.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == vacio) { origen = t; }
            }
            if (origen == null || origen.parent == null || !huesos.TryGetValue(origen.parent.name, out Transform hueso))
            {
                throw new Exception("T-1311: el FBX no trae el vacío " + vacio + " colgado de un hueso de PJ01");
            }
            Transform viejo = hueso.Find(nombre);
            if (viejo != null) { Object.DestroyImmediate(viejo.gameObject); }
            var go = new GameObject(nombre).transform;
            go.SetParent(hueso, false);
            go.localPosition = origen.localPosition;
            go.localRotation = origen.localRotation;
            Debug.Log("[T-1311] " + nombre + " en " + hueso.name + " posición local " + go.localPosition.ToString("F3"));
            return go;
        }

        private static void PonerEnPrefab(Material matLaser, Material matMisiles)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaVisual);
            try
            {
                Transform modelo = contenido.transform.Find("Giro/Modelo");
                var huesos = new Dictionary<string, Transform>();
                foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
                {
                    if (!huesos.ContainsKey(t.name)) { huesos[t.name] = t; }
                }
                Transform viejo = modelo.Find(NombreContenedor);
                if (viejo != null) { Object.DestroyImmediate(viejo.gameObject); }
                huesos.Clear();
                foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
                {
                    if (!huesos.ContainsKey(t.name)) { huesos[t.name] = t; }
                }
                var contenedor = new GameObject(NombreContenedor).transform;
                contenedor.SetParent(modelo, false);
                GameObject laser = CrearPieza(contenedor, BuscarMalla(fbx, PiezaLaser), huesos, matLaser);
                GameObject misiles = CrearPieza(contenedor, BuscarMalla(fbx, PiezaMisiles), huesos, matMisiles);
                Transform salidaLaser = CrearSalida(fbx, VacioLaser, NombreSalidaLaser, huesos);
                Transform salidaMisiles = CrearSalida(fbx, VacioMisiles, NombreSalidaMisiles, huesos);
                contenedor.gameObject.AddComponent<PiezasProtesisAtaquePJ01>().Configurar(laser, misiles, salidaLaser, salidaMisiles);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaVisual);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }
}
