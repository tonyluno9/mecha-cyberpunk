using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Doble de un punto de misión de Alan, solo para escenas y pruebas de Tony (T-184). Su pose es la de su transform. Un cilindro
    /// de color por tipo (gris apagado, color vivo con <see cref="Activar"/>(true)). Cuenta las llamadas a <see cref="Activar"/>.
    /// </summary>
    public class PuntoMisionDePrueba : MonoBehaviour, IPuntoMision
    {
        private const float FactorApagado = 0.35f;

        [Tooltip("Identificador del punto, con prefijo Mision_ (texto), por ejemplo Mision_Encargo_DonaLupe.")]
        [SerializeField] private string id = "Mision_Encargo_Prueba";

        [Tooltip("Para qué sirve el punto.")]
        [SerializeField] private TipoPuntoMision tipo = TipoPuntoMision.Destino;

        [Tooltip("Cilindro que se enciende (Renderer).")]
        [SerializeField] private Renderer cilindro;

        private Material material;

        public string Id => id;
        public TipoPuntoMision Tipo => tipo;
        public Pose Pose => new Pose(transform.position, transform.rotation);

        /// <summary>True mientras el punto está encendido.</summary>
        public bool Activo { get; private set; }

        /// <summary>Cuántas veces se llamó a Activar.</summary>
        public int LlamadasActivar { get; private set; }

        /// <summary>Color con el que se enciende cada tipo de punto.</summary>
        public static Color ColorDeTipo(TipoPuntoMision tipo)
        {
            switch (tipo)
            {
                case TipoPuntoMision.Encargo: return new Color(0.95f, 0.8f, 0.2f);
                case TipoPuntoMision.Destino: return new Color(0.2f, 0.85f, 0.4f);
                case TipoPuntoMision.Botin: return new Color(0.2f, 0.7f, 0.95f);
                case TipoPuntoMision.Pelea: return new Color(0.95f, 0.2f, 0.25f);
                default: return new Color(0.8f, 0.3f, 0.95f);
            }
        }

        private void Awake()
        {
            if (cilindro == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;
            material = new Material(shader) { name = "PuntoMision " + id };
            cilindro.sharedMaterial = material;
            Pintar();
        }

        private void OnEnable() => Registro<IPuntoMision>.Agregar(this);

        private void OnDisable() => Registro<IPuntoMision>.Quitar(this);

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        public void Activar(bool activo)
        {
            LlamadasActivar++;
            Activo = activo;
            Pintar();
        }

        private void Pintar()
        {
            if (material != null) material.SetColor("_BaseColor", Activo ? ColorDeTipo(tipo) : ColorDeTipo(tipo) * FactorApagado);
        }
    }
}
