using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>Letrero de un local: nombre y horario, y si está ABIERTO o CERRADO. Se actualiza solo al cambiar el estado.</summary>
    public class LetreroLugar : MonoBehaviour
    {
        private const string TextoAbierto = "ABIERTO";
        private const string TextoCerrado = "CERRADO";
        private const string TextoProximamente = "CERRADO · PRÓXIMAMENTE";

        [SerializeField] private LugarMundo lugar;
        [Tooltip("Texto con el nombre y, debajo, el horario.")]
        [SerializeField] private Text textoNombre;
        [Tooltip("Texto con el estado: ABIERTO o CERRADO.")]
        [SerializeField] private Text textoEstado;
        [SerializeField] private Color colorAbierto = new Color(0.1f, 0.9f, 0.85f);
        [SerializeField] private Color colorCerrado = new Color(1f, 0.15f, 0.65f);

        public string TextoDeEstado => textoEstado.text;
        public string TextoDeNombre => textoNombre.text;

        private void Awake()
        {
            if (lugar == null || textoNombre == null || textoEstado == null)
            {
                Debug.LogError("[Mundo] LetreroLugar en '" + name + "' necesita su lugar y sus dos textos.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            lugar.EstadoCambio += Actualizar;
        }

        private void OnDisable()
        {
            lugar.EstadoCambio -= Actualizar;
        }

        private void Start()
        {
            Actualizar(lugar.EstaAbierto);
        }

        private void Actualizar(bool abierto)
        {
            textoNombre.text = lugar.Nombre + "\n" + lugar.Horario.Texto();
            textoEstado.text = abierto ? TextoAbierto : (lugar.CerradoEnDemo ? TextoProximamente : TextoCerrado);
            textoEstado.color = abierto ? colorAbierto : colorCerrado;
        }
    }
}
