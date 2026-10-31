using System.Collections.Generic;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-448 del creador de la ciudad de pandillas: la guarida que sube de nivel. El ServicioGuarida con DatosGuarida_Base, tres postes dentro
    /// de la casa junto a la cama (ARMERO, GARAJE y CAMA, con letrero turquesa) y el carro del garaje, apagado hasta comprar el garaje,
    /// estacionado fuera cerca del tablero de encargos. Idempotente: rehace solo la raíz GTA_T448. Va después de T426 y T429.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT448 = "GTA_T448";
        private const string RutaDatosGuarida = "Assets/_Proyecto/Progresion/Datos/DatosGuarida_Base.asset";
        private const string RutaCarroGaraje = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-448 Guarida")]
        public static void T448()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T448Interno();
            Guardar(escena);
        }

        // Un punto del piso cerca de 'centro', a unos 'radio' metros, sin nada alto a menos de 0.45 m (para el poste).
        private static bool BuscarPuntoDeLaCasa(Vector3 centro, float radio, float anguloInicial, List<Vector3> usados, out Vector3 punto)
        {
            for (int k = 0; k < 16; k++)
            {
                float angulo = (anguloInicial + k * 22.5f) * Mathf.Deg2Rad;
                for (float r = radio; r <= radio + 1.5f; r += 0.5f)
                {
                    Vector3 candidato = PisoT426(centro + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * r);
                    bool lejosDeLosUsados = true;
                    foreach (Vector3 otro in usados)
                    {
                        lejosDeLosUsados &= Vector3.Distance(candidato, otro) > 1.4f;
                    }
                    if (lejosDeLosUsados && Mathf.Abs(candidato.y - centro.y) < 0.6f && !Physics.CheckSphere(candidato + Vector3.up * 1f, 0.45f))
                    {
                        punto = candidato;
                        return true;
                    }
                }
            }
            punto = centro;
            return false;
        }

        private static void T448Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT448);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>(FindObjectsInactive.Include);
            DirectorEncargos encargos = Object.FindFirstObjectByType<DirectorEncargos>(FindObjectsInactive.Include);
            if (cama == null)
            {
                Debug.LogError("[T-448] No hay CamaJugador en " + RutaEscena + ".");
                return;
            }

            DatosGuarida datos = CrearAsset<DatosGuarida>(RutaDatosGuarida);
            SerializedObject soDatos = new SerializedObject(datos);
            string[] ids = { DatosGuarida.IdArmero, DatosGuarida.IdGaraje, DatosGuarida.IdCama };
            string[] nombres = { "ARMERO", "GARAJE", "CAMA" };
            int[] precios = { 500, 600, 400 };
            SerializedProperty lista = soDatos.FindProperty("mejoras");
            lista.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                SerializedProperty e = lista.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = ids[i];
                e.FindPropertyRelative("nombre").stringValue = nombres[i];
                e.FindPropertyRelative("precio").intValue = precios[i];
            }
            soDatos.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);

            System.IO.Directory.CreateDirectory(CarpetaMateriales);
            Material poste = MaterialT407("T448_PosteGuarida", new Color(0.1f, 0.1f, 0.12f), Color.black);
            Material letrero = MaterialT407("T448_LetreroGuarida", new Color(0.1f, 0.9f, 0.9f), new Color(0.1f, 0.9f, 0.9f) * 3f);

            GameObject raiz = new GameObject(RaizT448);
            ServicioGuarida servicio = raiz.AddComponent<ServicioGuarida>();
            SerializedObject soServicio = new SerializedObject(servicio);
            soServicio.FindProperty("datos").objectReferenceValue = datos;
            soServicio.ApplyModifiedPropertiesWithoutUndo();

            // Tres postes alrededor de la cama, dentro de la casa.
            Vector3 centro = PisoT426(cama.transform.position);
            List<Vector3> usados = new List<Vector3>();
            float[] angulos = { 0f, 120f, 240f };
            for (int i = 0; i < ids.Length; i++)
            {
                BuscarPuntoDeLaCasa(centro, 1.7f, angulos[i], usados, out Vector3 donde);
                usados.Add(donde);
                GameObject punto = new GameObject("Guarida_" + ids[i]);
                punto.transform.SetParent(raiz.transform, false);
                punto.transform.position = donde;
                MarcarPoste(punto.transform, "Poste", new Vector3(0.2f, 1.4f, 0.2f), 0.7f, poste);
                MarcarPoste(punto.transform, "Letrero", new Vector3(0.8f, 0.3f, 0.08f), 1.5f, letrero);
                GuaridaPunto gp = punto.AddComponent<GuaridaPunto>();
                SerializedObject so = new SerializedObject(gp);
                so.FindProperty("servicio").objectReferenceValue = servicio;
                so.FindProperty("idMejora").stringValue = ids[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[T-448] " + nombres[i] + " en " + donde);
            }

            // El carro del garaje: fuera de la casa, cerca del tablero de encargos; apagado hasta que el garaje sea de nivel 1.
            Transform tablero = encargos != null ? new SerializedObject(encargos).FindProperty("tablero").objectReferenceValue as Transform : null;
            Vector3 baseGaraje = tablero != null ? tablero.position + new Vector3(6f, 0f, -10f) : centro + new Vector3(0f, 0f, -12f);
            Vector3 lugarCarro = PuntoLibreT427(baseGaraje, -ExtensionZona, -LimiteZonaX - 6f, -400f, 400f);
            GameObject garaje = new GameObject("Garaje");
            garaje.transform.SetParent(raiz.transform, false);
            garaje.transform.position = lugarCarro;
            GameObject prefabCarro = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarroGaraje);
            GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefabCarro, garaje.transform);
            carro.name = "CarroDeLaGuarida";
            carro.transform.position = lugarCarro + Vector3.up * 0.3f;
            carro.transform.rotation = Quaternion.identity;
            carro.SetActive(false);
            // GarajeCarro vive en Vehiculos; se busca por nombre para no referenciar ese ensamblado desde Progresion.
            System.Type tipoGaraje = System.Type.GetType("MechaCyberpunk.Vehiculos.Carro.GarajeCarro, MechaCyberpunk.Vehiculos");
            if (tipoGaraje == null)
            {
                Debug.LogError("[T-448] No encuentro MechaCyberpunk.Vehiculos.Carro.GarajeCarro.");
                return;
            }
            Component gc = garaje.AddComponent(tipoGaraje);
            SerializedObject soGaraje = new SerializedObject(gc);
            soGaraje.FindProperty("carro").objectReferenceValue = carro;
            soGaraje.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[T-448] Carro del garaje en " + lugarCarro);
            AssetDatabase.SaveAssets();
        }

        private static void MarcarPoste(Transform padre, string nombre, Vector3 escala, float altura, Material material)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(padre, false);
            g.transform.localPosition = new Vector3(0f, altura, 0f);
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
