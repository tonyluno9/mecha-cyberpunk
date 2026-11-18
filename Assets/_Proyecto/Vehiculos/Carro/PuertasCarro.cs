using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-1344: puertas y cofre que se abren. Cada pieza cuelga de una bisagra vacía (puerta izquierda, derecha, cofre) que se gira
    /// sobre su borde; el carro puede no tener alguna (carros viejos) y todo tolera nulos. Se abre sola al subir o bajar el jugador
    /// (puerta del lado de <see cref="IMontable.PuntoSubida"/>) y el cofre al prenderse el carro (estado Fuego).
    /// Tras explotar, <see cref="DanioCarro"/> suelta las piezas y la bisagra queda sin hijo: seguir girándola no hace nada.
    /// </summary>
    public sealed class PuertasCarro : MonoBehaviour
    {
        private const float SegundosAbiertaAlSubir = 1.6f;
        private const float SegundosAbiertaAlBajar = 1.2f;
        private const int Izquierda = 0;
        private const int Derecha = 1;
        private const int Cofre = 2;

        [SerializeField] private Transform bisagraPuertaIzquierda;
        [SerializeField] private Transform bisagraPuertaDerecha;
        [SerializeField] private Transform bisagraCofre;
        [SerializeField, Tooltip("Grados que abre cada puerta.")] private float anguloPuerta = 70f;
        [SerializeField, Tooltip("Grados que abre el cofre.")] private float anguloCofre = 55f;
        [SerializeField, Tooltip("Rapidez de apertura y cierre.")] private float gradosPorSegundo = 260f;

        private readonly Quaternion[] cerrada = new Quaternion[3];
        private readonly float[] apertura = new float[3];
        private readonly bool[] objetivoAbierto = new bool[3];
        private readonly float[] cerrarEn = new float[3] { -1f, -1f, -1f };
        private IMontable montable;
        private DanioCarro danio;

        /// <summary>Grados que abre la puerta (para pruebas y editor).</summary>
        public float AnguloPuerta => anguloPuerta;

        /// <summary>Grados que abre el cofre.</summary>
        public float AnguloCofre => anguloCofre;

        /// <summary>Rotación de una bisagra abierta: la cerrada girada <paramref name="grados"/> sobre <paramref name="eje"/> local.</summary>
        public static Quaternion RotacionAbierta(Quaternion cerrada, float grados, Vector3 eje)
        {
            return cerrada * Quaternion.AngleAxis(grados, eje);
        }

        private Transform Bisagra(int i)
        {
            return i == Izquierda ? bisagraPuertaIzquierda : i == Derecha ? bisagraPuertaDerecha : bisagraCofre;
        }

        private float AnguloMaximo(int i)
        {
            return i == Cofre ? anguloCofre : anguloPuerta;
        }

        // Izquierda: +yaw; derecha: -yaw; cofre: -X (levanta el borde delantero).
        private Quaternion Rotacion(int i, float grados)
        {
            switch (i)
            {
                case Izquierda: return RotacionAbierta(cerrada[i], grados, Vector3.up);
                case Derecha: return RotacionAbierta(cerrada[i], -grados, Vector3.up);
                default: return RotacionAbierta(cerrada[i], -grados, Vector3.right);
            }
        }

        private void Awake()
        {
            for (int i = 0; i < 3; i++)
            {
                Transform b = Bisagra(i);
                cerrada[i] = b != null ? b.localRotation : Quaternion.identity;
            }
        }

        private void OnEnable()
        {
            montable = GetComponent<IMontable>();
            if (montable != null)
            {
                montable.OcupadoCambio += AlCambiarOcupado;
            }
            danio = GetComponent<DanioCarro>();
            if (danio != null)
            {
                danio.EstadoCambio += AlCambiarEstado;
            }
        }

        private void OnDisable()
        {
            if (montable != null)
            {
                montable.OcupadoCambio -= AlCambiarOcupado;
                montable = null;
            }
            if (danio != null)
            {
                danio.EstadoCambio -= AlCambiarEstado;
                danio = null;
            }
        }

        // Subir (true) o bajar (false): se abre la puerta del conductor y se cierra poco después.
        private void AlCambiarOcupado(bool ocupado)
        {
            int lado = LadoIzquierdoMasCercano(montable.PuntoSubida) ? Izquierda : Derecha;
            Abrir(lado);
            cerrarEn[lado] = Time.time + (ocupado ? SegundosAbiertaAlSubir : SegundosAbiertaAlBajar);
        }

        private void AlCambiarEstado(EstadoCarro estado)
        {
            if (estado == EstadoCarro.Fuego)
            {
                AbrirCofre();
            }
        }

        /// <summary>True si el punto (mundo) queda del lado izquierdo del carro (x local menor que 0).</summary>
        public bool LadoIzquierdoMasCercano(Vector3 puntoMundo)
        {
            return transform.InverseTransformPoint(puntoMundo).x < 0f;
        }

        private void Abrir(int i)
        {
            objetivoAbierto[i] = true;
            cerrarEn[i] = -1f;
        }

        private void Cerrar(int i)
        {
            objetivoAbierto[i] = false;
            cerrarEn[i] = -1f;
        }

        public void AbrirPuerta(bool izquierda) { Abrir(izquierda ? Izquierda : Derecha); }

        public void CerrarPuerta(bool izquierda) { Cerrar(izquierda ? Izquierda : Derecha); }

        public void AbrirCofre() { Abrir(Cofre); }

        public void CerrarCofre() { Cerrar(Cofre); }

        /// <summary>True si la puerta se mandó abrir (aunque todavía esté girando).</summary>
        public bool PuertaAbierta(bool izquierda) { return objetivoAbierto[izquierda ? Izquierda : Derecha]; }

        /// <summary>True si el cofre se mandó abrir.</summary>
        public bool CofreAbierto => objetivoAbierto[Cofre];

        /// <summary>Qué tan abierta está la puerta ahora, de 0 (cerrada) a 1 (abierta del todo).</summary>
        public float AperturaPuerta(bool izquierda) { return apertura[izquierda ? Izquierda : Derecha]; }

        /// <summary>Qué tan abierto está el cofre, de 0 a 1.</summary>
        public float AperturaCofre => apertura[Cofre];

        private void Update()
        {
            for (int i = 0; i < 3; i++)
            {
                if (cerrarEn[i] >= 0f && Time.time >= cerrarEn[i])
                {
                    Cerrar(i);
                }
                float meta = objetivoAbierto[i] ? 1f : 0f;
                if (Mathf.Approximately(apertura[i], meta))
                {
                    continue;
                }
                float maximo = Mathf.Max(0.01f, AnguloMaximo(i));
                apertura[i] = Mathf.MoveTowards(apertura[i], meta, gradosPorSegundo / maximo * Time.deltaTime);
                Transform b = Bisagra(i);
                if (b != null)
                {
                    b.localRotation = Rotacion(i, apertura[i] * maximo);
                }
            }
        }
    }
}
