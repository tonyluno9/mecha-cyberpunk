using MechaCyberpunk.UI.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1662 (T44): las máquinas expendedoras vuelven a vender consumibles (DatosExpendedoras_Ciudad.maquinasVenden = true). La tiendita sigue existiendo.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.UI.Editor.ActivarMaquinasT1662.Aplicar
    /// </summary>
    public static class ActivarMaquinasT1662
    {
        private const string RutaDatos = "Assets/_Proyecto/UI/Datos/Resources/DatosExpendedoras_Ciudad.asset";

        [MenuItem("Mecha Cyberpunk/T-1662/Activar venta en las máquinas")]
        public static void Aplicar()
        {
            DatosExpendedoras datos = AssetDatabase.LoadAssetAtPath<DatosExpendedoras>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[T-1662] No está " + RutaDatos);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("maquinasVenden").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1662] maquinasVenden=" + datos.MaquinasVenden + " (tienda con " + (datos.Tienda != null ? datos.Tienda.Ofertas.Count : 0) + " ofertas)");
        }
    }
}
