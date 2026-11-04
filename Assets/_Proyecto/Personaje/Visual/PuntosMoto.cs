using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Los puntos de una moto donde se apoya el conductor (T-562): asiento, manos en el manubrio, pies en los estribos y, si hay, el caballete
    /// (se esconde al manejar). Va en la moto; <see cref="PoseMoto"/> del conductor lo usa. No tiene lógica de manejo.
    /// </summary>
    public class PuntosMoto : MonoBehaviour
    {
        [SerializeField] private Transform asiento;
        [SerializeField] private Transform manubrioIzquierdo;
        [SerializeField] private Transform manubrioDerecho;
        [SerializeField] private Transform estriboIzquierdo;
        [SerializeField] private Transform estriboDerecho;
        [Tooltip("Objeto de la moto que se esconde mientras alguien maneja (el caballete). Opcional.")]
        [SerializeField] private GameObject caballete;

        public Transform Asiento => asiento;
        public Transform ManubrioIzquierdo => manubrioIzquierdo;
        public Transform ManubrioDerecho => manubrioDerecho;
        public Transform EstriboIzquierdo => estriboIzquierdo;
        public Transform EstriboDerecho => estriboDerecho;
        public GameObject Caballete => caballete;

        public bool Completo => asiento != null && manubrioIzquierdo != null && manubrioDerecho != null
            && estriboIzquierdo != null && estriboDerecho != null;

        /// <summary>
        /// Crea los puntos que falten como hijos de la moto, en medidas de una moto normal (frente = +Z de la moto). Para el código de editor
        /// que prepara el prefab de la moto; los ajusta una persona si el modelo es de otro tamaño.
        /// </summary>
        public void CrearPuntosPorDefecto()
        {
            asiento = asiento != null ? asiento : Punto("Punto_Asiento", new Vector3(0f, 0.8f, -0.1f));
            manubrioIzquierdo = manubrioIzquierdo != null ? manubrioIzquierdo : Punto("Punto_ManubrioIzq", new Vector3(-0.3f, 1f, 0.35f));
            manubrioDerecho = manubrioDerecho != null ? manubrioDerecho : Punto("Punto_ManubrioDer", new Vector3(0.3f, 1f, 0.35f));
            estriboIzquierdo = estriboIzquierdo != null ? estriboIzquierdo : Punto("Punto_EstriboIzq", new Vector3(-0.25f, 0.3f, -0.05f));
            estriboDerecho = estriboDerecho != null ? estriboDerecho : Punto("Punto_EstriboDer", new Vector3(0.25f, 0.3f, -0.05f));
        }

        private Transform Punto(string nombre, Vector3 local)
        {
            Transform hijo = transform.Find(nombre);
            if (hijo == null)
            {
                hijo = new GameObject(nombre).transform;
                hijo.SetParent(transform, false);
            }
            hijo.localPosition = local;
            return hijo;
        }
    }
}
