using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.UI.Datos;
using MechaCyberpunk.UI.Tiendas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea los datos de las máquinas expendedoras (T-685): los 5 consumibles, sus ofertas, la tienda, la entrada en el catálogo de
    /// objetos y la lista de posiciones en Resources. Se puede correr otra vez: actualiza sin duplicar.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorExpendedoras.Crear
    /// </summary>
    public static class CreadorExpendedoras
    {
        private const string CarpetaEconomia = "Assets/_Proyecto/Economia/Datos";
        private const string CarpetaUI = "Assets/_Proyecto/UI/Datos";
        private const string RutaCatalogo = CarpetaEconomia + "/CatalogoObjetos_Base.asset";
        private const string RutaModelo = "Assets/Arte/Calle/Prefabs/CY06_Expendedora.prefab";
        private const string RutaPantalla = "Assets/_Proyecto/UI/Prefabs/PantallaTienda.prefab";

        private struct Item
        {
            public string id, nombre, descripcion;
            public int precio, maximo;
            public float curacion, segundosCuracion, segundosSinDanio, escudo, factor, segundosVelocidad;
        }

        private static readonly Item[] Items =
        {
            new Item { id = "bebida_energetica", nombre = "Bebida energética", descripcion = "Cura 25 y 5 s sin recibir daño", precio = 60, maximo = 5,
                curacion = 25f, segundosSinDanio = 5f },
            new Item { id = "papitas", nombre = "Papitas", descripcion = "Curan poco: 15", precio = 15, maximo = 8, curacion = 15f },
            new Item { id = "venda", nombre = "Venda", descripcion = "Cura 70 en 4 s", precio = 90, maximo = 4, curacion = 70f, segundosCuracion = 4f },
            new Item { id = "cafe", nombre = "Café", descripcion = "Corres 35% más rápido por 20 s", precio = 40, maximo = 5, factor = 1.35f, segundosVelocidad = 20f },
            new Item { id = "bateria_escudo", nombre = "Batería de escudo", descripcion = "Recarga 40 de escudo", precio = 75, maximo = 3, escudo = 40f },
        };

        // El frente del modelo CY06 mira a -Z local: el giro es el de la calle más 180°.
        // T-719: ya no llevan coordenadas fijas: cada una se mide desde un lugar de Core (la entrada del lugar más un desplazamiento) y, si la sesión 2 pone en la ciudad un marcador con
        // el nombre de la columna "marcador", la máquina va ahí. Los desplazamientos reproducen las posiciones medidas en Ciudad_Prologo (banqueta libre, y = 0.2).
        private static readonly (string nota, string marcador, IdLugar lugar, Vector3 desplazamiento, float giro)[] Lugares =
        {
            ("frente a la casa", "Marcador_Expendedora_Casa", IdLugar.Casa, new Vector3(0f, 0f, 6f), 270f),
            ("entrada del tianguis", "Marcador_Expendedora_Tianguis", IdLugar.Bar, new Vector3(-36f, 0f, 0.49f), 180f),
            ("calle de Nova", "Marcador_Expendedora_Nova", IdLugar.Bar, new Vector3(64f, 0f, -0.01f), 180f),
        };

        [MenuItem("Mecha Cyberpunk/Crear T-685 expendedoras")]
        public static void Crear()
        {
            Directory.CreateDirectory(CarpetaUI + "/Resources");
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(RutaCatalogo);
            var ofertas = new DatosOferta[Items.Length];
            var consumibles = new DatosConsumible[Items.Length];
            for (int i = 0; i < Items.Length; i++)
            {
                Item it = Items[i];
                ofertas[i] = CargarOCrear<DatosOferta>(CarpetaEconomia + "/DatosOferta_Expendedora_" + it.id + ".asset");
                var so = new SerializedObject(ofertas[i]);
                so.FindProperty("id").stringValue = "expendedora_" + it.id;
                so.FindProperty("nombre").stringValue = it.nombre;
                so.FindProperty("descripcion").stringValue = it.descripcion;
                so.FindProperty("precio").intValue = it.precio;
                so.FindProperty("tipo").enumValueIndex = (int)TipoOferta.Consumible;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ofertas[i]);

                consumibles[i] = CargarOCrear<DatosConsumible>(CarpetaEconomia + "/DatosObjeto_" + it.id + ".asset");
                so = new SerializedObject(consumibles[i]);
                so.FindProperty("id").stringValue = it.id;
                so.FindProperty("nombre").stringValue = it.nombre;
                so.FindProperty("tipo").enumValueIndex = (int)TipoObjeto.Consumible;
                so.FindProperty("valorVenta").intValue = 0;
                so.FindProperty("maximo").intValue = it.maximo;
                so.FindProperty("oferta").objectReferenceValue = ofertas[i];
                so.FindProperty("descripcion").stringValue = it.descripcion;
                so.FindProperty("curacion").floatValue = it.curacion;
                so.FindProperty("segundosCuracion").floatValue = it.segundosCuracion;
                so.FindProperty("segundosSinDanio").floatValue = it.segundosSinDanio;
                so.FindProperty("escudo").floatValue = it.escudo;
                so.FindProperty("factorVelocidad").floatValue = it.factor > 0f ? it.factor : 1f;
                so.FindProperty("segundosVelocidad").floatValue = it.segundosVelocidad;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(consumibles[i]);
            }

            // Catálogo: se agregan los consumibles que falten (no se tocan los demás objetos).
            var soCat = new SerializedObject(catalogo);
            SerializedProperty lista = soCat.FindProperty("objetos");
            foreach (DatosConsumible c in consumibles)
            {
                bool esta = false;
                for (int i = 0; i < lista.arraySize; i++)
                {
                    if (lista.GetArrayElementAtIndex(i).objectReferenceValue == c) { esta = true; break; }
                }
                if (!esta)
                {
                    lista.arraySize++;
                    lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = c;
                }
            }
            soCat.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalogo);

            var tienda = CargarOCrear<DatosTienda>(CarpetaEconomia + "/DatosTienda_Expendedora.asset");
            var soT = new SerializedObject(tienda);
            soT.FindProperty("lugar").enumValueIndex = (int)IdLugar.Calle;
            soT.FindProperty("nombre").stringValue = "Expendedora";
            SerializedProperty lo = soT.FindProperty("ofertas");
            lo.arraySize = ofertas.Length;
            for (int i = 0; i < ofertas.Length; i++)
            {
                lo.GetArrayElementAtIndex(i).objectReferenceValue = ofertas[i];
            }
            soT.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tienda);

            var datos = CargarOCrear<DatosExpendedoras>(CarpetaUI + "/Resources/" + ColocadorExpendedoras.NombreDatos + ".asset");
            var soD = new SerializedObject(datos);
            soD.FindProperty("modelo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            soD.FindProperty("pantalla").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantalla).GetComponent<PantallaTienda>();
            soD.FindProperty("tienda").objectReferenceValue = tienda;
            soD.FindProperty("catalogo").objectReferenceValue = catalogo;
            SerializedProperty pos = soD.FindProperty("posiciones");
            pos.arraySize = Lugares.Length;
            for (int i = 0; i < Lugares.Length; i++)
            {
                SerializedProperty e = pos.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("escena").stringValue = "Ciudad_Prologo";
                e.FindPropertyRelative("nota").stringValue = Lugares[i].nota;
                e.FindPropertyRelative("marcador").stringValue = Lugares[i].marcador;
                e.FindPropertyRelative("lugar").enumValueIndex = (int)Lugares[i].lugar;
                e.FindPropertyRelative("desplazamiento").vector3Value = Lugares[i].desplazamiento;
                e.FindPropertyRelative("posicion").vector3Value = Vector3.zero; // sin coordenadas fijas
                e.FindPropertyRelative("giro").floatValue = Lugares[i].giro;
            }
            soD.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI] Expendedoras creadas: " + Items.Length + " objetos y " + Lugares.Length + " lugares.");
        }

        private static T CargarOCrear<T>(string ruta) where T : ScriptableObject
        {
            var existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente != null)
            {
                return existente;
            }
            var nuevo = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(nuevo, ruta);
            return nuevo;
        }
    }
}
