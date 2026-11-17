using System.Collections.Generic;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1309: da a cada arma su propio sonido de disparo en el <see cref="CatalogoSonidos"/> (las armas piden un ID por código, ver PistolaAPie.IdDisparo):
    /// subfusil ARM_SUB_DISPARO, revólver ARM_REV_DISPARO (el mismo clip de siempre, ARM_SNI_DISPARO_01), lanzagranadas ARM_LGR_DISPARO, rifle de plasma
    /// ARM_PLA_DISPARO y su chispeo de carga ARM_PLA_CARGA; el sniper se queda con ARM_SNI_DISPARO_02. Importa los WAV nuevos (sin .meta todavía) y prende
    /// sus interruptores. Se puede correr varias veces.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.SonidosArmasT1309.Aplicar
    /// </summary>
    public static class SonidosArmasT1309
    {
        public const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        public const string CarpetaArm = "Assets/Arte/Audio/ARM/";

        /// <summary>ID -> archivo (sin carpeta) de su clip final. Un solo clip por ID: el disparo de cada arma suena siempre igual.</summary>
        private static readonly (string id, string archivo)[] Asignaciones =
        {
            ("ARM_SUB_DISPARO", "ARM_SUB_DISPARO_01.wav"),
            ("ARM_REV_DISPARO", "ARM_REV_DISPARO_01.wav"),
            ("ARM_LGR_DISPARO", "ARM_LGR_DISPARO_01.wav"),
            ("ARM_PLA_DISPARO", "ARM_PLA_DISPARO_01.wav"),
            ("ARM_PLA_CARGA", "ARM_PLA_CARGA_01.wav"),
            ("ARM_SNI_DISPARO", "ARM_SNI_DISPARO_02.wav"),
        };

        [MenuItem("Mecha Cyberpunk/T-1309 Asignar sonidos de armas")]
        public static void Aplicar()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            if (catalogo == null)
            {
                throw new System.InvalidOperationException("Falta " + RutaCatalogo);
            }
            List<CatalogoSonidos.Entrada> entradas = new List<CatalogoSonidos.Entrada>(catalogo.Entradas);
            foreach ((string id, string archivo) in Asignaciones)
            {
                string ruta = CarpetaArm + archivo;
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                if (clip == null)
                {
                    throw new System.InvalidOperationException("No se pudo cargar el clip " + ruta);
                }
                CatalogoSonidos.Entrada entrada = entradas.Find(e => e != null && e.id == id);
                if (entrada == null)
                {
                    entrada = new CatalogoSonidos.Entrada { id = id };
                    entradas.Add(entrada);
                }
                entrada.finales = new[] { clip };
                catalogo.FijarEncendido(id, true);
            }
            catalogo.FijarEncendido("CAL_DRON_ZUMBIDO", true); // T-1309b: el motor de los drones (su clip ya estaba en el catálogo)
            entradas.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            catalogo.Fijar(entradas);
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Personaje] T-1309: sonidos de armas asignados (" + Asignaciones.Length + " IDs en el catalogo).");
        }
    }
}
