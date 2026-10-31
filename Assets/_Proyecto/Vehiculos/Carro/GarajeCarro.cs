using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// El carro del garaje de la guarida (T-448): el carro de la casa solo está cuando el garaje es de nivel 1 o más, y con los niveles 2 y 3
    /// aguanta más daño. Revisa <see cref="IGuarida"/> cada medio segundo; va en un objeto que siempre está activo y tiene el carro de hijo
    /// (el hijo es el que se prende y se apaga).
    /// </summary>
    public class GarajeCarro : MonoBehaviour
    {
        private const float SegundosRevision = 0.5f;

        [Tooltip("El carro de la casa: un hijo con CarroViejo que empieza apagado.")]
        [SerializeField] private GameObject carro;

        private int nivelAplicado = -1;
        private float factorAplicado = -1f;
        private float proximaRevision;

        public GameObject Carro => carro;
        public int NivelAplicado => nivelAplicado;

        private void Awake()
        {
            if (carro == null)
            {
                Debug.LogError("[Vehiculos] Falta el carro en GarajeCarro de " + name, this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.unscaledTime + SegundosRevision;
            IGuarida guarida = Registro<IGuarida>.Actual;
            int nivel = guarida != null ? guarida.NivelGaraje : 0;
            float factor = guarida != null ? guarida.FactorVidaCarro : 1f;
            if (nivel == nivelAplicado && Mathf.Approximately(factor, factorAplicado))
            {
                return;
            }
            Aplicar(nivel, factor);
        }

        private void Aplicar(int nivel, float factor)
        {
            nivelAplicado = nivel;
            factorAplicado = factor;
            bool hayCarro = nivel >= 1;
            if (carro.activeSelf != hayCarro)
            {
                carro.SetActive(hayCarro);
            }
            if (hayCarro)
            {
                carro.GetComponent<DanioCarro>()?.EscalarVida(factor);
            }
        }
    }
}
