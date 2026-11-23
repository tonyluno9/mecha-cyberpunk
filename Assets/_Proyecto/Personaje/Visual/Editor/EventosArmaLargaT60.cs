using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-60: pone a PJ01_Rifle_Sacar y PJ01_Rifle_Guardar los eventos AlSacarPistola y AlGuardarPistola (los recibe ReceptorEventosArma) para que el arma larga
    /// aparezca en la mano al sacarla y desaparezca al guardarla, como la pistola. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.EventosArmaLargaT60.Aplicar
    /// </summary>
    public static class EventosArmaLargaT60
    {
        public static readonly string[] Clips = { "PJ01_Rifle_Sacar", "PJ01_Rifle_Guardar" };

        [MenuItem("Mecha Cyberpunk/Personajes/T-60 Eventos de sacar y guardar arma larga")]
        public static void Aplicar()
        {
            foreach (string nombre in Clips)
            {
                string ruta = ConstructorControladorBase.CarpetaClipsPJ01 + "/" + nombre + ".fbx";
                var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importador == null)
                {
                    Debug.LogError("[Personaje] T-60: falta " + ruta);
                    continue;
                }
                ModelImporterClipAnimation[] clips = importador.clipAnimations.Length > 0 ? importador.clipAnimations : importador.defaultClipAnimations;
                if (clips.Length == 0)
                {
                    continue;
                }
                string baseNombre = nombre.Substring(ImportadorClipsPJ01.Prefijo.Length);
                clips[0].events = ImportadorMixamo.EventosDe(baseNombre, clips[0].firstFrame, clips[0].lastFrame);
                importador.clipAnimations = clips;
                importador.SaveAndReimport();
                Debug.Log("[Personaje] T-60: eventos de arma puestos en " + nombre);
            }
        }
    }
}
