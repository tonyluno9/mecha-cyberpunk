using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-977: prende en el CatalogoSonidos los sonidos que regresan (docs/sonidos-sugerencias.md, en su orden), deja apagados los molestos
    /// y pone la mezcla final de las secciones. Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.RegresarSonidosT977.Aplicar
    /// </summary>
    public static class RegresarSonidosT977
    {
        public const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        [MenuItem("Mecha Cyberpunk/T-977/Regresar sonidos y mezcla final")]
        public static void Aplicar()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[Progresion] T-977: falta " + RutaCatalogo);
                return;
            }
            foreach (string id in ReglaMezclaSonidos.IdsRegresados)
            {
                catalogo.FijarEncendido(id, true);
            }
            foreach (string id in ReglaMezclaSonidos.IdsMolestos)
            {
                catalogo.FijarEncendido(id, false);
            }
            foreach (string id in SilenciarSilbatoT729.Silbatos)
            {
                catalogo.FijarEncendido(id, false);
            }
            catalogo.SincronizarInterruptores();
            catalogo.FijarMezclaFinal();
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-977: encendidos " + string.Join(", ", ReglaMezclaSonidos.IdsRegresados));
        }
    }
}
