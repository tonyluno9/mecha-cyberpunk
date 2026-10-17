using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// Un punto con nombre para la historia (T-092): los pasos de la ruta del prólogo en Ciudad y los planos de cámara del flashback.
    /// Su pose es la del transform. Si tiene un objeto asignado, lo prende con <c>Activar(true)</c> y lo apaga con <c>Activar(false)</c>
    /// (por ejemplo unas luces del plano). Se registra en <c>Registro&lt;IPuntoHistoria&gt;</c>.
    /// </summary>
    public class PuntoHistoriaMundo : MonoBehaviour, IPuntoHistoria
    {
        [Tooltip("Identificador único en la escena: Prologo_Carro, Prologo_Esquina_1, Flashback_Plano_1...")]
        [SerializeField] private string id;

        [Tooltip("Objeto que se prende mientras el punto está activo (opcional).")]
        [SerializeField] private GameObject objetoDelPlano;

        public string Id => id;
        public Pose Pose => new Pose(transform.position, transform.rotation);

        public void Activar(bool activo)
        {
            if (objetoDelPlano != null)
            {
                objetoDelPlano.SetActive(activo);
            }
        }

        private void OnEnable()
        {
            Registro<IPuntoHistoria>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPuntoHistoria>.Quitar(this);
        }
    }
}
