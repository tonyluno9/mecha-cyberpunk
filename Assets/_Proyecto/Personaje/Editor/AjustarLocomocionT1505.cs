using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1505: integra los clips de locomoción, escaleras y salto de Blender (PJ01_Caminar, Trote, Sprint, Trote_Izquierda/Derecha/Atras, Escaleras_Subir/Bajar,
    /// Salto_Despegue/Aire/Aterrizaje y Aterrizaje_Fuerte). Se corre en este orden (todo idempotente):
    /// 1. <see cref="ImportarClips"/>: Humanoid con el avatar de PJ01, bucle por nombre y eventos de pisada (ImportadorClipsPJ01).
    /// 2. <see cref="ReconstruirControlador"/>: ConstructorControladorBase.Construir (árbol 1D de Caminar, estado AterrizajeFuerte, overrides del jugador).
    /// 3. <see cref="ConfigurarEscaleras"/>: PJ01_Escaleras_Subir/Bajar en ConfigAnimacionEscalera.
    /// <see cref="Aplicar"/> corre los tres.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarLocomocionT1505.&lt;Metodo&gt;
    /// </summary>
    public static class AjustarLocomocionT1505
    {
        public static readonly string[] Clips =
        {
            "PJ01_Caminar", "PJ01_Trote", "PJ01_Sprint", "PJ01_Trote_Izquierda", "PJ01_Trote_Derecha", "PJ01_Trote_Atras",
            "PJ01_Escaleras_Subir", "PJ01_Escaleras_Bajar",
            "PJ01_Salto_Despegue", "PJ01_Salto_Aire", "PJ01_Salto_Aterrizaje", "PJ01_Aterrizaje_Fuerte",
        };

        /// <summary>Metros de subida por vuelta del clip de escaleras: 2 escalones de 18 cm (0.30 m/s por 1.2 s en Subir, 0.32 m/s por 1.13 s en Bajar).</summary>
        public const float MetrosPorCicloEscaleras = 0.36f;

        [MenuItem("Mecha Cyberpunk/Personajes/T-1505 Aplicar todo")]
        public static void Aplicar()
        {
            ImportarClips();
            ReconstruirControlador();
            ConfigurarEscaleras();
        }

        [MenuItem("Mecha Cyberpunk/Personajes/T-1505 1 Importar clips")]
        public static void ImportarClips()
        {
            AssetDatabase.Refresh();
            foreach (string nombre in Clips)
            {
                string ruta = ImportadorClipsPJ01.Carpeta + nombre + ".fbx";
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importador == null)
                {
                    Debug.LogError("[Personaje] T-1505: falta " + ruta);
                    continue;
                }
                // Igual que ImportarClipsRonda2T1310 y ImportadorClipsPJ01.Aplicar: avatar de PJ01, bucle por nombre, eventos de pisada; sin root motion aplicado
                // (el avance queda como movimiento de raíz sin bake y el Animator no lo aplica: el juego mueve la cápsula).
                ImportadorClipsPJ01.ConfigurarModelo(importador);
                importador.SaveAndReimport();
                ImportadorClipsPJ01.CopiarDescripcion(importador);
                ImportadorClipsPJ01.ConfigurarClip(importador, ruta);
                importador.SaveAndReimport();
                Debug.Log("[Personaje] T-1505: importado " + nombre);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Mecha Cyberpunk/Personajes/T-1505 2 Reconstruir controlador")]
        public static void ReconstruirControlador()
        {
            ConstructorControladorBase.Construir();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Mecha Cyberpunk/Personajes/T-1505 3 Configurar escaleras")]
        public static void ConfigurarEscaleras()
        {
            ConfigAnimacionEscalera config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionEscalera>(ImportarAnimacionesEscalera.RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Personaje] T-1505: falta " + ImportarAnimacionesEscalera.RutaConfig);
                return;
            }
            var so = new SerializedObject(config);
            so.FindProperty("subir").objectReferenceValue = ConstructorControladorBase.Clip("PJ01_Escaleras_Subir");
            so.FindProperty("bajar").objectReferenceValue = ConstructorControladorBase.Clip("PJ01_Escaleras_Bajar");
            so.FindProperty("metrosPorCiclo").floatValue = MetrosPorCicloEscaleras;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1505: escaleras con PJ01_Escaleras_Subir/Bajar, " + MetrosPorCicloEscaleras + " m por ciclo");
        }
    }
}
