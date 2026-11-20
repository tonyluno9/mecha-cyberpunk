using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1516: el perro robot E17 con el modelo de escala real de Blender (imageassets/blender/personajes/escala_real/E17_PerroRobotNova.fbx), el único cuyo
    /// rig calza con los clips E17_* (caminar, correr, ladrar, morder, muerte, salto de ataque). El modelo viejo (E17_PerroRobotNova_a) queda sin usar.
    /// En este orden, cada uno con -executeMethod MechaCyberpunk.Personaje.Visual.Editor.PerroEscalaRealT1516.&lt;Método&gt;:
    ///   Importar  copia el modelo, lo deja a 1.2 m de alto (userData del .meta) y reimporta los clips con su misma escala
    ///   Aplicar   controlador, visual y cambio del modelo del prefab del enemigo PerroNova_E17_PerroRobotNova_a (se puede repetir)
    /// </summary>
    public static class PerroEscalaRealT1516
    {
        public const string Variante = "E17_PerroRobotNova_esc";
        public const string VarianteVieja = "E17_PerroRobotNova_a";
        public const string RutaEnemigo = "Assets/_Proyecto/IA/Prefabs/PerroNova_E17_PerroRobotNova_a.prefab";
        private const string NombreFuente = "E17_PerroRobotNova.fbx";
        private const string CarpetaFuente = "imageassets/blender/personajes/escala_real/";
        /// <summary>Velocidades de los clips (m/s, bitácora de Blender 19-nov) a la escala real de Blender; se multiplican por la escala extra del modelo.</summary>
        public const float RapidezCaminar = 0.90f;
        public const float RapidezCorrer = 5.00f;

        public static string RutaModelo => CreadorVisualTripo.RutaModelo(Variante);

        /// <summary>Busca el .fbx fuente en la raíz del proyecto, o subiendo desde un worktree (.claude/worktrees/x) hasta la carpeta principal.</summary>
        public static string BuscarFuente()
        {
            string raiz = Path.GetFullPath(".");
            for (int i = 0; i < 4 && raiz != null; i++)
            {
                string candidata = Path.Combine(raiz, CarpetaFuente + NombreFuente);
                if (File.Exists(candidata))
                {
                    return candidata;
                }
                raiz = Path.GetDirectoryName(raiz);
            }
            return null;
        }

        [MenuItem("Mecha Cyberpunk/T-1516/Perro 1 Importar modelo a escala real")]
        public static void Importar()
        {
            string destino = Path.GetFullPath(RutaModelo);
            if (!File.Exists(destino) || new FileInfo(destino).Length < 100000)
            {
                string fuente = BuscarFuente();
                if (fuente == null)
                {
                    Debug.LogError("[Personaje] T-1516: no encuentro " + CarpetaFuente + NombreFuente);
                    return;
                }
                File.Copy(fuente, destino, true);
            }
            AssetDatabase.ImportAsset(RutaModelo, ImportAssetOptions.ForceUpdate);
            var importador = (ModelImporter)AssetImporter.GetAtPath(RutaModelo);
            importador.userData = "escala=1";
            importador.SaveAndReimport();
            float alto = CreadorVisualTripo.AltoDelModelo(RutaModelo);
            float escala = alto > 0.01f ? EscalasNova.AlturaPerroE17 / alto : 1f;
            importador = (ModelImporter)AssetImporter.GetAtPath(RutaModelo);
            importador.userData = "escala=" + escala.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
            importador.SaveAndReimport();
            ClipsNpcNovaT1506.ImportarClipsPerro();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Personaje] T-1516: perro a escala real, alto a escala 1 " + alto.ToString("F3") + " m, escala " + escala.ToString("F4") + ".");
        }

        /// <summary>Escala extra que el modelo recibe sobre la de Blender (la de su userData).</summary>
        public static float EscalaExtra()
        {
            var importador = AssetImporter.GetAtPath(RutaModelo) as ModelImporter;
            return importador != null ? PostprocesadorEscala(importador.userData) : 1f;
        }

        private static float PostprocesadorEscala(string userData)
        {
            const string prefijo = "escala=";
            if (!string.IsNullOrEmpty(userData) && userData.StartsWith(prefijo)
                && float.TryParse(userData.Substring(prefijo.Length), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float e) && e > 0f)
            {
                return e;
            }
            return 1f;
        }

        [MenuItem("Mecha Cyberpunk/T-1516/Perro 2 Aplicar visual y enemigo")]
        public static void Aplicar()
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            if (modelo == null)
            {
                Debug.LogError("[Personaje] T-1516: falta " + RutaModelo + " (corre Importar)");
                return;
            }
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(RutaModelo).OfType<Avatar>().FirstOrDefault();
            var clips = new Dictionary<ClipVisual, AnimationClip>
            {
                // No hay un "idle" de Blender: parado, el perro ladra (E17_Ladrar, en bucle, sin avanzar).
                { ClipVisual.Idle, ClipsNpcNovaT1506.CargarClip("E17_Ladrar") },
                { ClipVisual.Caminar, ClipsNpcNovaT1506.CargarClip("E17_Caminar") },
                { ClipVisual.Correr, ClipsNpcNovaT1506.CargarClip("E17_Correr") },
                { ClipVisual.Saltar, ClipsNpcNovaT1506.CargarClip("E17_SaltoAtaque") },
                { ClipVisual.Golpe, ClipsNpcNovaT1506.CargarClip("E17_Morder") },
                { ClipVisual.Caer, ClipsNpcNovaT1506.CargarClip("E17_Muerte") },
            };
            foreach (KeyValuePair<ClipVisual, AnimationClip> par in clips.ToList())
            {
                if (par.Value == null)
                {
                    Debug.LogError("[Personaje] T-1516: falta el clip del perro para " + par.Key);
                    clips.Remove(par.Key);
                }
            }
            AnimatorController controlador = ConstructorControladorVisual.Construir(CreadorVisualPersonaje.RutaControlador(Variante), clips, out _);
            string rutaVisual = CreadorVisualPersonaje.RutaPrefab(Variante);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaVisual) != null)
            {
                AssetDatabase.DeleteAsset(rutaVisual); // se rehace limpio
            }
            GameObject visual = CreadorVisualPersonaje.ArmarPrefab(Variante, modelo, avatar, controlador);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(CreadorVisualTripo.RutaMaterialBase(VarianteVieja));
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaVisual);
            try
            {
                if (material != null)
                {
                    foreach (SkinnedMeshRenderer r in contenido.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        r.sharedMaterial = material; // la textura es la misma del perro viejo
                    }
                }
                float escala = EscalaExtra();
                contenido.GetComponent<VisualPersonaje>().ConfigurarClips(RapidezCaminar * escala, RapidezCorrer * escala, RapidezCorrer * escala);
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaVisual);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            visual = AssetDatabase.LoadAssetAtPath<GameObject>(rutaVisual);
            CambiarModeloDelEnemigo(visual);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1516: perro con el modelo a escala real y sus clips E17_*, material " + (material != null ? material.name : "SIN MATERIAL"));
        }

        private static void CambiarModeloDelEnemigo(GameObject visual)
        {
            GameObject enemigo = PrefabUtility.LoadPrefabContents(RutaEnemigo);
            try
            {
                Transform viejo = enemigo.transform.Find("Modelo");
                ConectarVisualesT520.Reemplazar(enemigo, viejo, visual, "Modelo", false);
                PrefabUtility.SaveAsPrefabAsset(enemigo, RutaEnemigo);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(enemigo);
            }
        }
    }
}
