using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Carro estacionado que se puede robar (T-401). Va junto a un <see cref="CarroViejo"/>. La primera vez que el jugador sube
    /// suena la cerradura forzada, suma calor una sola vez y el carro queda como suyo. T-696: ya no hay luces de alarma ni sonido de carro robado.
    /// </summary>
    [RequireComponent(typeof(CarroViejo))]
    public class CarroEstacionado : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        [SerializeField] private float calorAlRobar = 30f;
        [Tooltip("Color de la carrocería (alfa 0 deja el original).")]
        [SerializeField] private Color tinte = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private bool esTuyo;

        private CarroViejo carro;

        /// <summary>True si ya es del jugador (no vuelve a sumar calor).</summary>
        public bool EsTuyo => esTuyo;

        /// <summary>Color de la carrocería; se aplica con un MaterialPropertyBlock.</summary>
        public void PonerTinte(Color color)
        {
            tinte = color;
            AplicarTinte();
        }

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            AplicarTinte();
        }

        private void OnEnable()
        {
            carro.OcupadoCambio += AlCambiarOcupado;
        }

        private void OnDisable()
        {
            if (carro != null)
            {
                carro.OcupadoCambio -= AlCambiarOcupado;
            }
        }

        private void AplicarTinte()
        {
            if (tinte.a <= 0f)
            {
                return;
            }
            foreach (Renderer renderizador in GetComponentsInChildren<Renderer>(true))
            {
                if (renderizador.name != "Carroceria")
                {
                    continue;
                }
                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                renderizador.GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, tinte);
                bloque.SetColor(IdColor, tinte);
                renderizador.SetPropertyBlock(bloque);
            }
        }

        private void AlCambiarOcupado(bool ocupado)
        {
            // T-450: la puerta suena al subir y al bajar; la primera vez (robo) también la cerradura forzada.
            Sonidos.ReproducirEn(ocupado ? "VEH_PUERTA_ABRIR" : "VEH_PUERTA_CERRAR", transform.position);
            if (!ocupado || esTuyo)
            {
                return;
            }
            Sonidos.ReproducirEn("VEH_FORZAR_PUERTA", transform.position);
            esTuyo = true;
            ICalor calor = Registro<ICalor>.Actual;
            if (calor != null)
            {
                calor.Sumar(calorAlRobar, MotivoCalor.Otro);
            }
        }
    }
}
