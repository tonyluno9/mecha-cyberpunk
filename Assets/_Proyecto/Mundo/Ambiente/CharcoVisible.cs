using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Va en cada charco que refleja: cuenta cuántos se ven ahora. <see cref="ReflejoCharcos"/> solo pinta su cámara si hay alguno a la vista
    /// (la cámara del reflejo no ve la capa de los charcos, así que solo cuentan las demás cámaras). Ahorra un render de toda la escena donde
    /// no hay charcos y no despierta animaciones que nadie ve.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class CharcoVisible : MonoBehaviour
    {
        private static int visibles;
        private bool visto;

        /// <summary>Charcos que alguna cámara está viendo en este momento.</summary>
        public static int Visibles => visibles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => visibles = 0;

        private void OnBecameVisible()
        {
            if (!visto)
            {
                visto = true;
                visibles++;
            }
        }

        private void OnBecameInvisible() => Olvidar();

        private void OnDisable() => Olvidar();

        private void Olvidar()
        {
            if (visto)
            {
                visto = false;
                visibles = Mathf.Max(0, visibles - 1);
            }
        }
    }
}
