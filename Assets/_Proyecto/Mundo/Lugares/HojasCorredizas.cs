using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// T-910: las dos hojas de cristal de una puerta corrediza (PC01) siguen a la <see cref="HojaPuerta"/> de la puerta: al abrir se deslizan cada
    /// una hacia su lado, sin girar, y vuelven al cerrar. La HojaPuerta sigue mandando (collider, E, cierre sola); aquí solo se mueve lo visible.
    /// </summary>
    public class HojasCorredizas : MonoBehaviour
    {
        [SerializeField] private HojaPuerta hoja;
        [SerializeField] private Transform izquierda;
        [SerializeField] private Transform derecha;
        [Tooltip("Cuánto se desliza cada hoja al abrir del todo, en el espacio de su padre (m).")]
        [SerializeField] private Vector3 deslizamientoIzquierda = new Vector3(-0.75f, 0f, 0f);
        [SerializeField] private Vector3 deslizamientoDerecha = new Vector3(0.75f, 0f, 0f);

        private Vector3 cerradaIzquierda;
        private Vector3 cerradaDerecha;

        public Transform Izquierda => izquierda;
        public Transform Derecha => derecha;
        public Vector3 DeslizamientoIzquierda => deslizamientoIzquierda;

        private void Awake()
        {
            if (hoja == null || izquierda == null || derecha == null)
            {
                Debug.LogError("[Mundo] HojasCorredizas en '" + name + "' necesita la hoja de la puerta y las dos hojas de cristal.", this);
                enabled = false;
                return;
            }
            cerradaIzquierda = izquierda.localPosition;
            cerradaDerecha = derecha.localPosition;
        }

        private void LateUpdate()
        {
            float p = hoja.ProgresoApertura;
            izquierda.localPosition = cerradaIzquierda + deslizamientoIzquierda * p;
            derecha.localPosition = cerradaDerecha + deslizamientoDerecha * p;
        }
    }
}
