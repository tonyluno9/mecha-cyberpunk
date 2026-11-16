#if UNITY_EDITOR
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1110: arma el recinto del jefe final de Nova: el jefe (prefab con <see cref="ConductaJefeFinalNova"/>), la zona (<see cref="RecintoJefe"/>), la barrera de
    /// energía en la entrada, una luz fría y el enlace con los guardias pesados. Solo del editor: lo usan el colocador de la ciudad y las pruebas.
    /// La entrada está en <c>entrada</c> y el jefe espera en <c>centroJefe</c>, de cara a la entrada; la zona empieza <see cref="MargenEntrada"/> metros adentro.
    /// </summary>
    public static class ConstructorRecintoJefe
    {
        public const float MargenEntrada = 4f;
        public const float AltoBarrera = 6f;
        public const float GrosorBarrera = 0.6f;
        private const float AltoRayo = 3f;
        private const float LargoRayo = 8f;

        /// <summary>Devuelve el recinto; su jefe es <c>recinto.Jefe</c>.</summary>
        public static RecintoJefe Crear(Transform padre, string nombre, Vector3 entrada, Vector3 centroJefe, float anchoZona, float largoZona,
            GameObject prefabJefe, DatosPandilleroAPie datosJefe, PuestoGuardiasPesados guardias, Material materialBarrera)
        {
            Vector3 mira = centroJefe - entrada;
            mira.y = 0f;
            mira = Mathf.Abs(mira.x) >= Mathf.Abs(mira.z) ? new Vector3(Mathf.Sign(mira.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(mira.z)); // la zona sigue el eje de la calle
            Quaternion giro = Quaternion.LookRotation(mira, Vector3.up);
            GameObject raiz = new GameObject(nombre);
            raiz.SetActive(false); // sin Awake hasta tener todo configurado
            if (padre != null)
            {
                raiz.transform.SetParent(padre, false);
            }
            Vector3 centroZona = Suelo(entrada + mira * (MargenEntrada + largoZona * 0.5f));
            raiz.transform.SetPositionAndRotation(centroZona, giro);
            BoxCollider zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.size = new Vector3(anchoZona, 4f, largoZona);
            zona.center = Vector3.up * 2f;

            GameObject objetoJefe = (GameObject)PrefabUtility.InstantiatePrefab(prefabJefe, raiz.transform);
            objetoJefe.name = "Jefe";
            objetoJefe.transform.SetPositionAndRotation(Suelo(centroJefe), Quaternion.LookRotation(-mira, Vector3.up));
            SerializedObject soJefe = new SerializedObject(objetoJefe.GetComponent<PandilleroAPie>());
            soJefe.FindProperty("datos").objectReferenceValue = datosJefe;
            soJefe.ApplyModifiedPropertiesWithoutUndo();

            GameObject barrera = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrera.name = "BarreraEnergia";
            barrera.transform.SetParent(raiz.transform, false);
            barrera.transform.SetPositionAndRotation(Suelo(entrada) + Vector3.up * (AltoBarrera * 0.5f), giro);
            barrera.transform.localScale = new Vector3(anchoZona, AltoBarrera, GrosorBarrera);
            if (materialBarrera != null)
            {
                barrera.GetComponent<Renderer>().sharedMaterial = materialBarrera;
            }
            barrera.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            barrera.SetActive(false);

            GameObject luzObjeto = new GameObject("LuzFria");
            luzObjeto.transform.SetParent(raiz.transform, false);
            luzObjeto.transform.position = centroZona + Vector3.up * 6f;
            Light luz = luzObjeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(0.55f, 0.8f, 1f);
            luz.intensity = 12f;
            luz.range = Mathf.Max(anchoZona, largoZona) * 0.9f;
            luz.shadows = LightShadows.None;

            RecintoJefe recinto = raiz.AddComponent<RecintoJefe>();
            SerializedObject so = new SerializedObject(recinto);
            so.FindProperty("jefe").objectReferenceValue = objetoJefe.GetComponent<ConductaJefeFinalNova>();
            so.FindProperty("barrera").objectReferenceValue = barrera;
            so.FindProperty("guardias").objectReferenceValue = guardias;
            so.FindProperty("luz").objectReferenceValue = luz;
            so.ApplyModifiedPropertiesWithoutUndo();
            raiz.SetActive(true);
            return recinto;
        }

        /// <summary>Solo el jefe, sin zona ni barrera (para probar su pelea). Devuelve su conducta; su raíz es <c>conducta.transform.parent</c>.</summary>
        public static ConductaJefeFinalNova CrearJefe(Vector3 posicion, Vector3 mira, GameObject prefabJefe, DatosPandilleroAPie datosJefe)
        {
            mira.y = 0f;
            mira = mira.sqrMagnitude < 0.0001f ? Vector3.forward : mira.normalized;
            GameObject raiz = new GameObject("Puesto_JefeFinal_Prueba");
            raiz.SetActive(false);
            GameObject objetoJefe = (GameObject)PrefabUtility.InstantiatePrefab(prefabJefe, raiz.transform);
            objetoJefe.name = "Jefe";
            objetoJefe.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(mira, Vector3.up));
            SerializedObject so = new SerializedObject(objetoJefe.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = datosJefe;
            so.ApplyModifiedPropertiesWithoutUndo();
            raiz.SetActive(true);
            return objetoJefe.GetComponent<ConductaJefeFinalNova>();
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            return Physics.Raycast(punto + Vector3.up * AltoRayo, Vector3.down, out RaycastHit h, LargoRayo, ~0, QueryTriggerInteraction.Ignore) ? h.point : punto;
        }
    }
}
#endif
