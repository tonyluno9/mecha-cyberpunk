using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Variante de color del mismo cuerpo (T-520): cambia el material del modelo y, si tiene material de franja, le pone franjas luminosas
    /// en el pecho y los brazos (SKULLS negro con magenta). Con índice -1 elige una variante al azar al aparecer (peatones).
    /// </summary>
    public class VarianteVisual : MonoBehaviour
    {
        [Tooltip("Materiales entre los que se elige (cuerpo completo).")]
        [SerializeField] private Material[] materiales = new Material[0];
        [Tooltip("Índice de la variante fija; -1 = al azar.")]
        [Min(-1)] [SerializeField] private int indice = -1;
        [Tooltip("Material emisivo de las franjas; vacío = sin franjas.")]
        [SerializeField] private Material materialFranja;
        [Tooltip("Grosor de las franjas de los brazos (m).")]
        [Min(0.01f)] [SerializeField] private float grosorFranja = 0.14f;

        [Header("Brillo (el jefe SKULLS)")]
        [Tooltip("Color de una luz puntual sobre el pecho; con alfa 0 no hay luz.")]
        [SerializeField] private Color colorLuz = new Color(0f, 0f, 0f, 0f);
        [Tooltip("Intensidad de la luz del brillo.")]
        [Min(0f)] [SerializeField] private float intensidadLuz = 2.5f;
        [Tooltip("Alcance de la luz del brillo (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceLuz = 5f;

        private Renderer cuerpo;
        private bool franjasPuestas;
        private bool luzPuesta;

        public int IndiceAplicado { get; private set; } = -1;
        public Renderer Cuerpo => cuerpo;
        public int Cantidad => materiales.Length;

        /// <summary>Para los creadores: define las variantes y cuál va.</summary>
        public void Configurar(Material[] nuevos, int fijo, Material franja)
        {
            materiales = nuevos ?? new Material[0];
            indice = fijo;
            materialFranja = franja;
        }

        /// <summary>Para los creadores: una luz de color sobre el pecho (el brillo del jefe).</summary>
        public void ConfigurarLuz(Color color, float intensidad, float alcance)
        {
            colorLuz = color;
            intensidadLuz = intensidad;
            alcanceLuz = alcance;
        }

        public bool TieneLuz => colorLuz.a > 0f;

        private void Awake()
        {
            Aplicar();
        }

        public void Aplicar()
        {
            cuerpo = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (cuerpo == null)
            {
                Debug.LogError("[Personaje] VarianteVisual en " + name + " no encontró el modelo.", this);
                return;
            }
            if (materiales.Length > 0)
            {
                IndiceAplicado = indice >= 0 ? indice % materiales.Length : Random.Range(0, materiales.Length);
                cuerpo.sharedMaterial = materiales[IndiceAplicado];
            }
            if (materialFranja != null && !franjasPuestas)
            {
                PonerFranjas();
            }
            if (colorLuz.a > 0f && !luzPuesta)
            {
                luzPuesta = true;
                GameObject objeto = new GameObject("BrilloMagenta");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = new Vector3(0f, AlturaLuz, 0f);
                Light luz = objeto.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = colorLuz;
                luz.intensity = intensidadLuz;
                luz.range = alcanceLuz;
                luz.shadows = LightShadows.None;
            }
        }

        private const float AlturaLuz = 1.4f;

        private void PonerFranjas()
        {
            Animator animador = GetComponentInChildren<Animator>(true);
            if (animador == null || animador.avatar == null || !animador.isHuman)
            {
                return;
            }
            franjasPuestas = true;
            Transform raiz = transform;
            Transform pecho = animador.GetBoneTransform(HumanBodyBones.UpperChest);
            if (pecho == null)
            {
                pecho = animador.GetBoneTransform(HumanBodyBones.Chest);
            }
            if (pecho != null)
            {
                Cubo("FranjaPecho", pecho, pecho.position + raiz.forward * 0.13f, Quaternion.LookRotation(raiz.forward), new Vector3(0.3f, 0.05f, 0.03f));
            }
            Banda(animador, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, "FranjaBrazoIzq");
            Banda(animador, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, "FranjaBrazoDer");
        }

        private void Banda(Animator animador, HumanBodyBones desde, HumanBodyBones hasta, string nombre)
        {
            Transform a = animador.GetBoneTransform(desde);
            Transform b = animador.GetBoneTransform(hasta);
            if (a == null || b == null)
            {
                return;
            }
            Vector3 eje = (b.position - a.position).normalized;
            Cubo(nombre, a, Vector3.Lerp(a.position, b.position, 0.55f), Quaternion.FromToRotation(Vector3.up, eje), new Vector3(grosorFranja, 0.05f, grosorFranja));
        }

        private void Cubo(string nombre, Transform padre, Vector3 posicion, Quaternion giro, Vector3 tamano)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nombre;
            Collider colision = cubo.GetComponent<Collider>();
            if (Application.isPlaying) { Destroy(colision); } else { DestroyImmediate(colision); }
            cubo.transform.SetParent(padre, true);
            cubo.transform.SetPositionAndRotation(posicion, giro);
            cubo.transform.localScale = tamano;
            Renderer r = cubo.GetComponent<Renderer>();
            r.sharedMaterial = materialFranja;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
