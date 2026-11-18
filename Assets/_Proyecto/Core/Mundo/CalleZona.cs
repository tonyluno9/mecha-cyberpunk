using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core.Mundo
{
    /// <summary>
    /// T-1350: una calle de las zonas nuevas (Malecón, Colonia del Cerro, Autopista, Puerto) que no sale de la cuadrícula de manzanas.
    /// La pone en la escena el script de colocación de cada zona; el tráfico, la policía, los peatones y el mapa la leen.
    /// <see cref="Puntos"/> es la línea del centro de la calle en espacio de mundo, con la y del piso (la autopista va a 10 m).
    /// Los extremos se unen a la red de calles existente (o a otra CalleZona) si quedan a menos de <see cref="DistanciaUnion"/>.
    /// </summary>
    public class CalleZona : MonoBehaviour
    {
        /// <summary>Distancia (m, en planta) a la que un extremo se une con un nodo de otra calle.</summary>
        public const float DistanciaUnion = 12f;

        private static readonly List<CalleZona> activas = new List<CalleZona>();

        [Tooltip("Línea del centro de la calle, en espacio local de este objeto (m). La y es el piso.")]
        [SerializeField] private Vector3[] puntos = new Vector3[0];

        [Tooltip("Ancho de la calle de banqueta a banqueta (m).")]
        [Min(3f)] [SerializeField] private float ancho = 14f;

        [Tooltip("Banqueta a los dos lados: los peatones caminan por ella.")]
        [SerializeField] private bool banquetas = true;

        [Tooltip("Elevada o rampa: sin peatones; los carros van más rápido.")]
        [SerializeField] private bool autopista;

        [Tooltip("Zona a la que pertenece: Malecon, Cerro, Autopista, Puerto.")]
        [SerializeField] private string zona = "";

        /// <summary>Las CalleZona encendidas en las escenas cargadas.</summary>
        public static IReadOnlyList<CalleZona> Activas => activas;

        public float Ancho => ancho;
        public bool Banquetas => banquetas;
        public bool Autopista => autopista;
        public string Zona => zona;
        public int Cantidad => puntos.Length;

        /// <summary>Punto <paramref name="i"/> de la línea del centro, en mundo.</summary>
        public Vector3 Punto(int i) => transform.TransformPoint(puntos[i]);

        /// <summary>Todos los puntos en mundo.</summary>
        public List<Vector3> Puntos()
        {
            List<Vector3> lista = new List<Vector3>(puntos.Length);
            for (int i = 0; i < puntos.Length; i++)
            {
                lista.Add(Punto(i));
            }
            return lista;
        }

        /// <summary>Lo usan los scripts de colocación (editor): puntos en mundo, se guardan en local.</summary>
        public void Definir(IList<Vector3> puntosMundo, float anchoCalle, bool conBanquetas, bool esAutopista, string nombreZona)
        {
            puntos = new Vector3[puntosMundo.Count];
            for (int i = 0; i < puntosMundo.Count; i++)
            {
                puntos[i] = transform.InverseTransformPoint(puntosMundo[i]);
            }
            ancho = anchoCalle;
            banquetas = conBanquetas;
            autopista = esAutopista;
            zona = nombreZona ?? "";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            activas.Clear();
        }

        private void OnEnable()
        {
            if (!activas.Contains(this))
            {
                activas.Add(this);
            }
        }

        private void OnDisable()
        {
            activas.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = autopista ? Color.cyan : Color.yellow;
            for (int i = 1; i < puntos.Length; i++)
            {
                Gizmos.DrawLine(Punto(i - 1), Punto(i));
            }
        }
    }
}
