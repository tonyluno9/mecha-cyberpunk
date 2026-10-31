using MechaCyberpunk.Progresion.Prologo.Encargos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-429 del creador de la ciudad de pandillas: el ServicioNegocios con el asset DatosNegocios_Base y un punto con poste de neón por negocio
    /// (TIENDITA en SUBURBIOS, PUESTO y FONDA en EL TIANGUIS, CLUB y ARCADE en NEÓN ALTO, TALLER en LA REFINERÍA). Idempotente: rehace solo la raíz GTA_T429.
    /// Va después de T426 (usa el tablero para ubicar la tiendita).
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT429 = "GTA_T429";
        private const string RutaDatosNegocios = "Assets/_Proyecto/Progresion/Datos/DatosNegocios_Base.asset";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-429 Negocios")]
        public static void T429()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T429Interno();
            Guardar(escena);
        }

        private static void T429Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT429);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }

            // id, nombre, zona, precio, ingreso base del nivel 1, centro de búsqueda y límites X/Z de la zona.
            string[] ids = { "tiendita", "puesto", "fonda", "club", "arcade", "taller" };
            string[] nombres = { "TIENDITA DE LA ESQUINA", "PUESTO DEL TIANGUIS", "FONDA DEL TIANGUIS", "CLUB NEÓN", "ARCADE NEÓN", "TALLER DE LA REFINERÍA" };
            string[] zonas = { "fosa", "tianguis", "tianguis", "neon", "neon", "refineria" };
            int[] precios = { 300, 400, 400, 600, 500, 700 };
            int[] ingresos = { 35, 45, 45, 70, 55, 80 };
            Transform tablero = null;
            DirectorEncargos encargos = Object.FindFirstObjectByType<DirectorEncargos>(FindObjectsInactive.Include);
            if (encargos != null)
            {
                tablero = new SerializedObject(encargos).FindProperty("tablero").objectReferenceValue as Transform;
            }
            Vector3 baseTiendita = tablero != null ? tablero.position + new Vector3(-10f, 0f, 14f) : new Vector3(-90f, 0f, 0f);
            Vector3[] bases =
            {
                baseTiendita, new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, -35f),
                new Vector3(100f, 0f, 60f), new Vector3(145f, 0f, 25f), new Vector3(130f, 0f, -60f)
            };
            float[] xMin = { -ExtensionZona, -LimiteZonaX + 6f, -LimiteZonaX + 6f, LimiteZonaX + 6f, LimiteZonaX + 6f, LimiteZonaX + 6f };
            float[] xMax = { -LimiteZonaX - 6f, LimiteZonaX - 6f, LimiteZonaX - 6f, 400f, 400f, 400f };
            float[] zMin = { -400f, -400f, -400f, 6f, 6f, -400f };
            float[] zMax = { 400f, 400f, 400f, 400f, 400f, -6f };

            DatosNegocios datos = CrearAsset<DatosNegocios>(RutaDatosNegocios);
            SerializedObject soDatos = new SerializedObject(datos);
            SerializedProperty lista = soDatos.FindProperty("negocios");
            lista.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                SerializedProperty e = lista.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = ids[i];
                e.FindPropertyRelative("nombre").stringValue = nombres[i];
                e.FindPropertyRelative("zona").stringValue = zonas[i];
                e.FindPropertyRelative("precio").intValue = precios[i];
                e.FindPropertyRelative("ingresoBase").intValue = ingresos[i];
            }
            soDatos.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);

            System.IO.Directory.CreateDirectory(CarpetaMateriales);
            Material poste = MaterialT407("T429_PosteNegocio", new Color(0.1f, 0.1f, 0.12f), Color.black);
            Material letrero = MaterialT407("T429_LetreroNegocio", new Color(0.1f, 0.9f, 0.9f), new Color(0.1f, 0.9f, 0.9f) * 3f);

            GameObject raiz = new GameObject(RaizT429);
            ServicioNegocios servicio = raiz.AddComponent<ServicioNegocios>();
            SerializedObject soServicio = new SerializedObject(servicio);
            soServicio.FindProperty("datos").objectReferenceValue = datos;
            soServicio.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < ids.Length; i++)
            {
                GameObject punto = new GameObject("Negocio_" + ids[i]);
                punto.transform.SetParent(raiz.transform, false);
                punto.transform.position = PuntoLibreT427(bases[i], xMin[i], xMax[i], zMin[i], zMax[i]);
                Marcar(punto.transform, PrimitiveType.Cube, "Poste", new Vector3(0.35f, 2.4f, 0.35f), 1.2f, poste);
                Marcar(punto.transform, PrimitiveType.Cube, "Letrero", new Vector3(1.4f, 0.6f, 0.12f), 2.6f, letrero);
                NegocioPunto np = punto.AddComponent<NegocioPunto>();
                SerializedObject so = new SerializedObject(np);
                so.FindProperty("servicio").objectReferenceValue = servicio;
                so.FindProperty("idNegocio").stringValue = ids[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[T-429] " + nombres[i] + " en " + punto.transform.position);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Marcar(Transform padre, PrimitiveType tipo, string nombre, Vector3 escala, float altura, Material material)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nombre;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(padre, false);
            g.transform.localPosition = new Vector3(0f, altura, 0f);
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
