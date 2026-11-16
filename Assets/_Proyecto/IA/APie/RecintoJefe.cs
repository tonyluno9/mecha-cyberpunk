using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1110: el recinto del jefe final de Nova al pie de la torre. Una zona (BoxCollider trigger) que, cuando el jugador entra a pie y en pie, cierra la pelea:
    /// prende la barrera de energía que cierra la entrada, el jefe grita "Nadie entra a la torre de Nova.", se activa y provoca a los guardias pesados del
    /// puesto (<see cref="PuestoGuardiasPesados"/>). Si el jugador muere o queda a más de <see cref="DistanciaReset"/>, abre la barrera, el jefe vuelve a su punto con la
    /// vida llena y los guardias vuelven. Al vencer al jefe se abre la barrera y se apagan los guardias que queden. Revisa la zona cada cuadro (no depende
    /// de los mensajes de trigger de la física).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RecintoJefe : MonoBehaviour
    {
        public const float DistanciaReset = 40f;
        public const string GritoCierre = "Nadie entra a la torre de Nova.";

        [Tooltip("El jefe final del recinto.")]
        [SerializeField] private ConductaJefeFinalNova jefe;
        [Tooltip("La barrera de energía (con collider) que cierra la entrada. Empieza apagada.")]
        [SerializeField] private GameObject barrera;
        [Tooltip("Los guardias pesados de la torre (T-1109). Puede quedar vacío.")]
        [SerializeField] private PuestoGuardiasPesados guardias;
        [Tooltip("Luz fría del recinto. Puede quedar vacío.")]
        [SerializeField] private Light luz;

        private BoxCollider zona;
        private PandilleroAPie pandilleroJefe;
        private bool cerrado;

        public ConductaJefeFinalNova Jefe => jefe;
        public GameObject Barrera => barrera;
        public PuestoGuardiasPesados Guardias => guardias;
        /// <summary>True mientras la pelea está cerrada (barrera prendida).</summary>
        public bool Cerrado => cerrado;

        private void Awake()
        {
            zona = GetComponent<BoxCollider>();
            zona.isTrigger = true;
            if (jefe == null)
            {
                Debug.LogError("[IA] Falta el jefe en RecintoJefe de " + name, this);
                enabled = false;
                return;
            }
            pandilleroJefe = jefe.GetComponent<PandilleroAPie>();
            if (barrera != null)
            {
                barrera.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (jefe != null)
            {
                jefe.Derrotado += AlDerrotarAlJefe;
            }
        }

        private void OnDisable()
        {
            if (jefe != null)
            {
                jefe.Derrotado -= AlDerrotarAlJefe;
            }
        }

        private void Update()
        {
            Transform jugador = pandilleroJefe.Jugador;
            bool atacable = jugador != null && pandilleroJefe.JugadorAtacable;
            if (!cerrado)
            {
                if (atacable && !jefe.Vencido && !pandilleroJefe.EstaKO && DentroDeLaZona(jugador.position))
                {
                    Cerrar();
                }
                return;
            }
            if (jefe.Vencido)
            {
                return;
            }
            if (!atacable || DistanciaPlana(jugador.position, transform.position) > DistanciaReset)
            {
                Resetear();
            }
        }

        private bool DentroDeLaZona(Vector3 punto)
        {
            Vector3 local = transform.InverseTransformPoint(punto) - zona.center;
            Vector3 mitad = zona.size * 0.5f;
            return Mathf.Abs(local.x) <= mitad.x && Mathf.Abs(local.z) <= mitad.z;
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void Cerrar()
        {
            cerrado = true;
            if (barrera != null)
            {
                barrera.SetActive(true);
            }
            jefe.Activar();
            pandilleroJefe.Gritar(GritoCierre);
            guardias?.ProvocarATodos();
            Debug.Log("[IA] Recinto del jefe final de Nova cerrado.");
        }

        private void Abrir()
        {
            cerrado = false;
            if (barrera != null)
            {
                barrera.SetActive(false);
            }
        }

        private void Resetear()
        {
            Abrir();
            jefe.Reiniciar();
            guardias?.ReiniciarTodos();
            Debug.Log("[IA] Recinto del jefe final de Nova reiniciado.");
        }

        private void AlDerrotarAlJefe()
        {
            Abrir();
            guardias?.ApagarTodos();
        }
    }
}
