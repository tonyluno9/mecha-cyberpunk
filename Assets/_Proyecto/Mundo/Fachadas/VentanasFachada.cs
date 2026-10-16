using UnityEngine;

namespace MechaCyberpunk.Mundo.Fachadas
{
    /// <summary>
    /// Quad de ventanas de un edificio (T-081). Todos comparten el mismo material de la variante; este componente ajusta
    /// la repetición de la textura al tamaño del quad con un MaterialPropertyBlock (<c>_BaseMap_ST</c>), así cada ventana
    /// mide lo mismo (3 m) en todos los edificios.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class VentanasFachada : MonoBehaviour
    {
        private static readonly int IdBaseMapST = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField, Tooltip("Repetición y desplazamiento de la textura (x e y: repeticiones del mosaico de 12 m; z y w: desplazamiento).")]
        private Vector4 repeticion = new Vector4(1f, 1f, 0f, 0f);

        public Vector4 Repeticion => repeticion;

        public void Configurar(Vector4 nuevaRepeticion)
        {
            repeticion = nuevaRepeticion;
            Aplicar();
        }

        private void OnEnable()
        {
            Aplicar();
        }

        private void Aplicar()
        {
            MeshRenderer renderizador = GetComponent<MeshRenderer>();
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            renderizador.GetPropertyBlock(bloque);
            bloque.SetVector(IdBaseMapST, repeticion);
            renderizador.SetPropertyBlock(bloque);
        }
    }
}
