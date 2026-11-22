using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625: un punto con nombre que el mapa pone para el recorrido nuevo. Ids que busca el director (todos opcionales, con respaldo):
    /// Historia_Molestia (donde los SKULLS le salen al paso, paso 3), Historia_Ranura (la grieta del muro, pasos 5 y 6) e Historia_Torre
    /// (la torre de Nova, pasos 15 y 16). La pose es la del transform: la ranura mira hacia afuera de la ciudad.
    /// </summary>
    public class MarcadorHistoria : MonoBehaviour, IPuntoHistoria
    {
        public const string IdMolestia = "Historia_Molestia";
        public const string IdRanura = "Historia_Ranura";
        public const string IdTorre = "Historia_Torre";
        public const string IdCallejonSkulls = "Historia_CallejonSkulls";

        [Tooltip("Historia_Molestia, Historia_Ranura o Historia_Torre.")]
        [SerializeField] private string id = IdMolestia;

        public string Id => id;
        public Pose Pose => new Pose(transform.position, transform.rotation);

        public void Activar(bool activo)
        {
        }

        private void OnEnable() => Registro<IPuntoHistoria>.Agregar(this);

        private void OnDisable() => Registro<IPuntoHistoria>.Quitar(this);

        /// <summary>Para crear el marcador desde código (editor).</summary>
        public void FijarId(string nuevoId) => id = nuevoId;

        /// <summary>Busca el marcador por id entre los registrados.</summary>
        public static bool TryBuscar(string idBuscado, out Pose pose)
        {
            foreach (IPuntoHistoria punto in Registro<IPuntoHistoria>.Todos)
            {
                if (punto != null && punto.Id == idBuscado)
                {
                    pose = punto.Pose;
                    return true;
                }
            }
            pose = default;
            return false;
        }
    }
}
