using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>
    /// El LED "SINAPSIS" del tablero (T-194, D-tony-167): un LED turquesa prendido con SISTEMA NERVIOSO instalado y un segundo LED con el
    /// nivel 2. Solo prende y apaga objetos (no cambia materiales en ejecución, D-tony-122).
    /// </summary>
    public class LedSinapsis : MonoBehaviour
    {
        [Tooltip("LED que se prende con el nivel 1 o más.")]
        [SerializeField] private GameObject led1;
        [Tooltip("LED que se prende con el nivel 2.")]
        [SerializeField] private GameObject led2;

        private IImplantes escuchados;

        /// <summary>Cuántos LEDs están prendidos (0, 1 o 2).</summary>
        public int Prendidos => (led1 != null && led1.activeSelf ? 1 : 0) + (led2 != null && led2.activeSelf ? 1 : 0);

        private void Awake()
        {
            if (led1 == null || led2 == null)
            {
                Debug.LogError("[Mecha] Faltan los LEDs en LedSinapsis de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IImplantes>.Agregado += AlAgregar;
            Registro<IImplantes>.Quitado += AlQuitar;
            Escuchar(Registro<IImplantes>.Actual);
        }

        private void OnDisable()
        {
            Registro<IImplantes>.Agregado -= AlAgregar;
            Registro<IImplantes>.Quitado -= AlQuitar;
            if (escuchados != null)
            {
                escuchados.Cambio -= Pintar;
                escuchados = null;
            }
        }

        private void AlAgregar(IImplantes nuevos) => Escuchar(nuevos);

        private void AlQuitar(IImplantes quitados)
        {
            if (quitados == escuchados)
            {
                Escuchar(Registro<IImplantes>.Actual);
            }
        }

        private void Escuchar(IImplantes implantes)
        {
            if (escuchados != null)
            {
                escuchados.Cambio -= Pintar;
            }
            escuchados = implantes;
            if (escuchados != null)
            {
                escuchados.Cambio += Pintar;
            }
            Pintar();
        }

        private void Pintar()
        {
            int nivel = escuchados != null ? escuchados.Nivel(EspacioImplante.SistemaNervioso) : 0;
            led1.SetActive(nivel >= 1);
            led2.SetActive(nivel >= 2);
        }
    }
}
