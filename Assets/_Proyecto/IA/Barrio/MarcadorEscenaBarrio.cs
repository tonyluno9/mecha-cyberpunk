using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T15): marcador opcional que se coloca en el barrio (tianguis, esquina, Neón Alto) para decir "aquí pasan escenas". El director lo usa solo
    /// para preferir civiles dentro de su radio; sin marcadores elige civiles y pandilleros cercanos al jugador. <see cref="IdEscena"/> vacío = cualquier escena.
    /// </summary>
    public class MarcadorEscenaBarrio : MonoBehaviour
    {
        private static readonly List<MarcadorEscenaBarrio> ActivosLista = new List<MarcadorEscenaBarrio>();

        [Tooltip("ID de la escena que prefiere este lugar (vacío = cualquiera).")]
        [SerializeField] private string idEscena = string.Empty;
        [Tooltip("Radio donde se buscan los civiles de la escena (m).")]
        [Min(1f)] [SerializeField] private float radio = 10f;

        public static IReadOnlyList<MarcadorEscenaBarrio> Activos => ActivosLista;
        public string IdEscena => idEscena;
        public float Radio => radio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => ActivosLista.Clear();

        private void OnEnable() => ActivosLista.Add(this);

        private void OnDisable() => ActivosLista.Remove(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, radio);
        }

        /// <summary>Lo usa el creador del prefab.</summary>
        public void Definir(string id, float nuevoRadio)
        {
            idEscena = id;
            radio = Mathf.Max(1f, nuevoRadio);
        }
    }
}
