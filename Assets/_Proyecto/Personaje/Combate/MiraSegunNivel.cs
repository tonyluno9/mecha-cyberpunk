using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-971: la mira de la pistola en la mano solo se ve con la mejora Mira comprada (la pistola en nivel III). Va en la raíz del prefab de
    /// agarre y prende o apaga el objeto <see cref="mira"/>. Busca el <see cref="ArmasAPie"/> del padre y el <see cref="ArsenalJugador"/> que
    /// usa; sin ArmasAPie en los padres (la pistola en el piso o de un NPC) la mira se queda apagada. El arsenal no avisa cuando se compra
    /// una mejora, así que se revisa el nivel cada <see cref="Intervalo"/> segundos y se guarda en un entero.
    /// </summary>
    public class MiraSegunNivel : MonoBehaviour
    {
        /// <summary>Nivel de la pistola que da la mira (la regla del banco: cargador I, cañón II, mira III).</summary>
        public const int NivelConMira = 3;
        private const float Intervalo = 0.25f;

        [Tooltip("El objeto Mira que se prende o se apaga (hijo de la corredera).")]
        [SerializeField] private GameObject mira;

        private ArmasAPie armas;
        private ArsenalJugador arsenal;
        private int nivel = -1;
        private float siguiente;
        private bool buscado;

        public GameObject Mira => mira;
        /// <summary>El último nivel de la pistola que se leyó (-1 si no hay arsenal).</summary>
        public int Nivel => nivel;

        private void Awake() => BuscarArmas();

        // La pistola de la mano está apagada hasta que se saca: si Awake aún no corrió, se busca la primera vez que se revisa.
        private void BuscarArmas()
        {
            if (!buscado)
            {
                buscado = true;
                armas = GetComponentInParent<ArmasAPie>(true);
            }
        }

        private void OnEnable() => Revisar();

        private void Update()
        {
            if (armas == null || Time.unscaledTime < siguiente)
            {
                return;
            }
            siguiente = Time.unscaledTime + Intervalo;
            Revisar();
        }

        /// <summary>Lee el nivel de la pistola y prende o apaga la mira.</summary>
        public void Revisar()
        {
            if (mira == null)
            {
                return;
            }
            BuscarArmas();
            if (armas == null)
            {
                nivel = -1;
            }
            else
            {
                arsenal = arsenal != null ? arsenal : Registro<ArsenalJugador>.Actual;
                nivel = arsenal != null ? arsenal.Nivel(ArmaAPie.Pistola) : -1;
            }
            bool visible = nivel >= NivelConMira;
            if (mira.activeSelf != visible)
            {
                mira.SetActive(visible);
            }
        }
    }
}
