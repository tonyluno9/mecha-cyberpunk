using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Torreta de una patrulla: alterna una luz roja y una azul (brillo emisivo y luz puntual) siempre, de día y de noche.
    /// El ritmo lo manda <see cref="CalculoTorreta"/>; cada patrulla trae su propia fase inicial.
    /// </summary>
    public class LuzTorreta : MonoBehaviour
    {
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private ConfigTorreta config;
        [SerializeField] private Renderer visualRojo;
        [SerializeField] private Renderer visualAzul;
        [SerializeField] private Light luzRoja;
        [SerializeField] private Light luzAzul;

        [Tooltip("Segundos que esta torreta ya lleva corriendo al empezar, para que no parpadee igual que las demás (s).")]
        [SerializeField] private float faseInicial;

        private CalculoTorreta calculo;
        private MaterialPropertyBlock bloque;
        private Color emisionRoja;
        private Color emisionAzul;
        private bool aplicado;
        private ColorTorreta ultimoAplicado;

        /// <summary>Color prendido ahora.</summary>
        public ColorTorreta ColorActual => calculo != null ? calculo.ColorActual : ColorTorreta.Rojo;

        private void Awake()
        {
            if (config == null || visualRojo == null || visualAzul == null || luzRoja == null || luzAzul == null)
            {
                Debug.LogError("[Mundo] LuzTorreta '" + name + "' tiene referencias sin asignar", this);
                enabled = false;
                return;
            }
            bloque = new MaterialPropertyBlock();
            emisionRoja = LeerEmision(visualRojo);
            emisionAzul = LeerEmision(visualAzul);
            calculo = new CalculoTorreta(config.Periodo, faseInicial);
            luzRoja.intensity = config.IntensidadLuz;
            luzRoja.range = config.RangoLuz;
            luzAzul.intensity = config.IntensidadLuz;
            luzAzul.range = config.RangoLuz;
            Aplicar(calculo.ColorActual);
        }

        private void Update()
        {
            calculo.Avanzar(Time.deltaTime);
            ColorTorreta color = calculo.ColorActual;
            if (!aplicado || color != ultimoAplicado)
            {
                Aplicar(color);
            }
        }

        private static Color LeerEmision(Renderer visual)
        {
            Material material = visual.sharedMaterial;
            return material != null && material.HasProperty(IdEmision) ? material.GetColor(IdEmision) : Color.white;
        }

        private void Aplicar(ColorTorreta color)
        {
            aplicado = true;
            ultimoAplicado = color;
            bool rojo = color == ColorTorreta.Rojo;
            Poner(visualRojo, emisionRoja, rojo);
            Poner(visualAzul, emisionAzul, !rojo);
            luzRoja.enabled = rojo;
            luzAzul.enabled = !rojo;
        }

        private void Poner(Renderer visual, Color emision, bool prendido)
        {
            visual.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, prendido ? emision : emision * config.BrilloApagado);
            visual.SetPropertyBlock(bloque);
        }
    }
}
