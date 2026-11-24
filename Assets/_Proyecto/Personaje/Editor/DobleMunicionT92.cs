using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T92: el doble de balas por arma. Pone en los assets los valores de las tablas de <see cref="CreadorArmas"/> (cargador de cada
    /// DatosArma) y <see cref="CreadorMunicionArmas"/> (paquete, iniciales y tope de cada DatosMunicionArma, y el texto de su oferta), y el
    /// cargador de DatosPistola_Base. Escribe valores absolutos, así que es idempotente: correrlo dos veces deja lo mismo.
    /// No regenera catálogos, ofertas ni prefabs. Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.DobleMunicionT92.Aplicar
    /// </summary>
    public static class DobleMunicionT92
    {
        private const string CarpetaMunicion = "Assets/_Proyecto/Economia/Datos";
        private const string RutaPistolaBase = "Assets/_Proyecto/Personaje/Datos/DatosPistola_Base.asset";
        public const int CargadorPistolaBase = 16;

        [MenuItem("Mecha Cyberpunk/T92 doble de munición")]
        public static void Aplicar()
        {
            int cambios = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:DatosArma", new[] { CreadorArmas.CarpetaArmas }))
            {
                DatosArma arma = AssetDatabase.LoadAssetAtPath<DatosArma>(AssetDatabase.GUIDToAssetPath(guid));
                int cargador = arma != null ? CreadorArmas.CargadorDe(arma.Arma, arma.Nivel) : -1;
                if (cargador > 0)
                {
                    cambios += Poner(arma, "cargador", cargador);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:DatosMunicionArma", new[] { CarpetaMunicion }))
            {
                DatosMunicionArma balas = AssetDatabase.LoadAssetAtPath<DatosMunicionArma>(AssetDatabase.GUIDToAssetPath(guid));
                if (balas == null || !CreadorMunicionArmas.ValoresDe(balas.Arma, out int porPaquete, out int iniciales, out int maximo))
                {
                    continue;
                }
                cambios += Poner(balas, "balasPorPaquete", porPaquete);
                cambios += Poner(balas, "balasIniciales", iniciales);
                cambios += Poner(balas, "maximo", maximo);
                DatosOferta oferta = balas.Oferta;
                if (oferta != null)
                {
                    var so = new SerializedObject(oferta);
                    SerializedProperty descripcion = so.FindProperty("descripcion");
                    string texto = CreadorMunicionArmas.DescripcionPaquete(porPaquete);
                    if (descripcion != null && descripcion.stringValue != texto)
                    {
                        descripcion.stringValue = texto;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(oferta);
                        cambios++;
                    }
                }
            }

            DatosPistola pistola = AssetDatabase.LoadAssetAtPath<DatosPistola>(RutaPistolaBase);
            if (pistola != null)
            {
                cambios += Poner(pistola, "balasPorCargador", CargadorPistolaBase);
            }
            else
            {
                Debug.LogError("[Personaje] T92: falta " + RutaPistolaBase);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T92: munición al doble; " + cambios + " valores cambiados (0 = ya estaba aplicado).");
        }

        private static int Poner(Object destino, string campo, int valor)
        {
            var so = new SerializedObject(destino);
            SerializedProperty p = so.FindProperty(campo);
            if (p == null)
            {
                Debug.LogError("[Personaje] T92: " + destino.name + " no tiene el campo " + campo);
                return 0;
            }
            if (p.intValue == valor)
            {
                return 0;
            }
            p.intValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destino);
            return 1;
        }
    }
}
