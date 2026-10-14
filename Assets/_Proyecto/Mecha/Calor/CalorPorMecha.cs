using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Calor
{
    /// <summary>
    /// Usar el mecha en la calle da calor policial (T-067, D-tony-31): suma al llamarlo y por segundo mientras el jugador va dentro. Busca el <see cref="ICalor"/> en el registro cada vez; sin él no hace nada. Un mecha en arranque Directo (arena)
    /// nunca da calor.
    /// </summary>
    [RequireComponent(typeof(MontajeMecha))]
    public class CalorPorMecha : MonoBehaviour
    {
        [SerializeField] private ConfigCalorMecha config;

        private MontajeMecha montaje;
        private LlamadoMecha llamado;

        private void Awake()
        {
            montaje = GetComponent<MontajeMecha>();
            llamado = GetComponent<LlamadoMecha>();
            if (config == null)
            {
                Debug.LogError("[Mecha] Falta la config en CalorPorMecha de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (llamado != null)
            {
                llamado.LlamadoIntentado += AlIntentarLlamar;
            }
        }

        private void OnDisable()
        {
            if (llamado != null)
            {
                llamado.LlamadoIntentado -= AlIntentarLlamar;
            }
        }

        private void Update()
        {
            ICalor calor = Registro<ICalor>.Actual;
            if (calor == null || llamado == null)
            {
                return;
            }
            bool alMando = montaje.Ocupado;
            float cantidad = ReglaCalorMecha.CalorPorFrame(montaje.Estacionado, alMando, llamado.Estado, config.CalorPorSegundo, Time.deltaTime);
            if (cantidad > 0f)
            {
                calor.Sumar(cantidad, MotivoCalor.MechaEnCalle);
            }
        }

        private void AlIntentarLlamar(ResultadoLlamado resultado)
        {
            float cantidad = ReglaCalorMecha.CalorAlLlamar(resultado, config.CalorAlLlamar);
            if (cantidad > 0f)
            {
                Registro<ICalor>.Actual?.Sumar(cantidad, MotivoCalor.MechaEnCalle);
            }
        }
    }
}
