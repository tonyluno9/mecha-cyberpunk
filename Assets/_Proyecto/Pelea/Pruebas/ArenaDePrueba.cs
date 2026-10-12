using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Pruebas
{
    /// <summary>
    /// Arena de prueba de Tony: cumple <see cref="IArenaPelea"/> con puntos de inicio y una pantalla que se arman en el prefab.
    /// Sirve de ejemplo del contrato para la jaula de Alan. Se registra en OnEnable y se quita en OnDisable.
    /// </summary>
    public class ArenaDePrueba : MonoBehaviour, IArenaPelea
    {
        [Tooltip("Nombre para mostrar de la arena.")]
        [SerializeField] private string nombre = "Arena de prueba";
        [Tooltip("Punto donde empieza el jugador (su +Z mira al rival).")]
        [SerializeField] private Transform inicioJugador;
        [Tooltip("Punto donde empieza el rival (su +Z mira al jugador).")]
        [SerializeField] private Transform inicioRival;
        [Tooltip("Centro de la cara visible de la pantalla; su +Z apunta desde la jaula hacia la pantalla.")]
        [SerializeField] private Transform pantallaArena;
        [Tooltip("Ancho y alto de la zona útil de la pantalla (m).")]
        [SerializeField] private Vector2 tamanoPantalla = new Vector2(8f, 4.5f);

        public string Nombre => nombre;
        public Pose InicioJugador => new Pose(inicioJugador.position, inicioJugador.rotation);
        public Pose InicioRival => new Pose(inicioRival.position, inicioRival.rotation);
        public Vector3 Centro => transform.position;
        public Transform PantallaArena => pantallaArena;
        public Vector2 TamanoPantalla => tamanoPantalla;

        private void Awake()
        {
            if (inicioJugador == null || inicioRival == null || pantallaArena == null)
            {
                Debug.LogError("[Pelea] Faltan referencias en ArenaDePrueba de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IArenaPelea>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IArenaPelea>.Quitar(this);
        }
    }
}
