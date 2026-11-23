using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Datos;
using MechaCyberpunk.UI.Tiendas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1529: arma los datos de la tiendita de la esquina (edificio ED02): crea DatosTienda_Tiendita con las mismas ofertas de consumibles de las
    /// máquinas, liga en DatosExpendedoras_Ciudad el edificio ED02, un mostrador (Prop_Barra) y un tendero (Visual_NP10_Elotero_a), pone la lista de sitios
    /// candidatos y apaga la venta de las máquinas (quedan de decorado). Idempotente. No toca escenas: el colocador pone la tiendita al cargar la ciudad.
    /// Batchmode:
    /// -executeMethod MechaCyberpunk.UI.Editor.CreadorTienditaT1529.Diagnostico   (solo informa)
    /// -executeMethod MechaCyberpunk.UI.Editor.CreadorTienditaT1529.Aplicar
    /// </summary>
    public static class CreadorTienditaT1529
    {
        private const string RutaDatos = "Assets/_Proyecto/UI/Datos/Resources/DatosExpendedoras_Ciudad.asset";
        private const string RutaTiendaMaquinas = "Assets/_Proyecto/Economia/Datos/DatosTienda_Expendedora.asset";
        private const string RutaTienda = "Assets/_Proyecto/Economia/Datos/DatosTienda_Tiendita.asset";
        private const string RutaEdificio = "Assets/Arte/Zonas/ED/ED02_TienditaEsquina_a.prefab";
        private const string RutaMostrador = "Assets/_Proyecto/Mundo/Prefabs/Kit/Prop_Barra.prefab";
        private const string RutaTendero = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_NP10_Elotero_a.prefab";
        public const string Marcador = "Marcador_Tiendita";

        // Sitios candidatos (el primero libre gana). El marcador manda si Alan lo pone en la escena; si no, se miden desde la entrada del Bar, a lo largo de
        // la calle de las expendedoras del tianguis y de Nova y 6 m hacia el fondo de la banqueta (el frente mira a +Z, a la calle).
        private static readonly (string nota, Vector3 desplazamiento)[] Sitios =
        {
            ("esquina del tianguis, hacia el oeste", new Vector3(-48f, 0f, -6f)),
            ("esquina del tianguis, hacia el este", new Vector3(-24f, 0f, -6f)),
            ("calle de Nova, hacia el oeste", new Vector3(52f, 0f, -6f)),
            ("calle de Nova, hacia el este", new Vector3(76f, 0f, -6f)),
        };

        [MenuItem("Mecha Cyberpunk/T-1529/Diagnóstico tiendita")]
        public static void Diagnostico()
        {
            Debug.Log("[T-1529] edificio ED02: " + (AssetDatabase.LoadAssetAtPath<GameObject>(RutaEdificio) != null ? "ok" : "FALTA " + RutaEdificio));
            Debug.Log("[T-1529] mostrador: " + (AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostrador) != null ? "ok" : "FALTA " + RutaMostrador));
            Debug.Log("[T-1529] tendero: " + (AssetDatabase.LoadAssetAtPath<GameObject>(RutaTendero) != null ? "ok" : "FALTA " + RutaTendero));
            var datos = AssetDatabase.LoadAssetAtPath<DatosExpendedoras>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[T-1529] Falta " + RutaDatos);
                return;
            }
            Debug.Log("[T-1529] maquinasVenden=" + datos.MaquinasVenden + " tiendita=" + (datos.Tiendita != null) + " mostrador=" + (datos.Mostrador != null)
                + " tendero=" + (datos.Tendero != null) + " tienda=" + (datos.TiendaTiendita != null) + " sitios=" + datos.SitiosTiendita.Count);
        }

        [MenuItem("Mecha Cyberpunk/T-1529/Aplicar tiendita")]
        public static void Aplicar()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosExpendedoras>(RutaDatos);
            var maquinas = AssetDatabase.LoadAssetAtPath<DatosTienda>(RutaTiendaMaquinas);
            var edificio = AssetDatabase.LoadAssetAtPath<GameObject>(RutaEdificio);
            var mostrador = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostrador);
            var tendero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTendero);
            if (datos == null || maquinas == null || edificio == null || mostrador == null || tendero == null)
            {
                Debug.LogError("[T-1529] Falta un insumo (datos, tienda de las máquinas, ED02, Prop_Barra o Visual_NP10_Elotero_a): corre Diagnostico.");
                return;
            }

            var tienda = AssetDatabase.LoadAssetAtPath<DatosTienda>(RutaTienda);
            if (tienda == null)
            {
                tienda = ScriptableObject.CreateInstance<DatosTienda>();
                AssetDatabase.CreateAsset(tienda, RutaTienda);
            }
            var origen = new SerializedObject(maquinas);
            var soT = new SerializedObject(tienda);
            soT.FindProperty("lugar").enumValueIndex = (int)IdLugar.Calle;
            soT.FindProperty("nombre").stringValue = "Tiendita";
            SerializedProperty ofertasOrigen = origen.FindProperty("ofertas");
            SerializedProperty ofertas = soT.FindProperty("ofertas");
            ofertas.arraySize = ofertasOrigen.arraySize;
            for (int i = 0; i < ofertasOrigen.arraySize; i++)
            {
                ofertas.GetArrayElementAtIndex(i).objectReferenceValue = ofertasOrigen.GetArrayElementAtIndex(i).objectReferenceValue;
            }
            soT.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tienda);

            var so = new SerializedObject(datos);
            so.FindProperty("maquinasVenden").boolValue = true; // T-1662 (T44): las máquinas vuelven a vender (el paso del hambre de la historia)
            so.FindProperty("tiendita").objectReferenceValue = edificio;
            so.FindProperty("mostrador").objectReferenceValue = mostrador;
            so.FindProperty("tendero").objectReferenceValue = tendero;
            so.FindProperty("tiendaTiendita").objectReferenceValue = tienda;
            SerializedProperty sitios = so.FindProperty("sitiosTiendita");
            sitios.arraySize = Sitios.Length;
            for (int i = 0; i < Sitios.Length; i++)
            {
                SerializedProperty e = sitios.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("escena").stringValue = "Ciudad_Prologo";
                e.FindPropertyRelative("nota").stringValue = Sitios[i].nota;
                e.FindPropertyRelative("marcador").stringValue = Marcador;
                e.FindPropertyRelative("lugar").enumValueIndex = (int)IdLugar.Bar;
                e.FindPropertyRelative("desplazamiento").vector3Value = Sitios[i].desplazamiento;
                e.FindPropertyRelative("posicion").vector3Value = Vector3.zero;
                e.FindPropertyRelative("giro").floatValue = 180f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[T-1529] Tiendita lista: " + Sitios.Length + " sitios, " + ofertasOrigen.arraySize + " consumibles; las máquinas ya no venden.");
        }
    }
}
