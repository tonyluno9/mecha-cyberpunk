#if !DEMO_FINAL
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Atajos para probar el juego de pandillas (T-404, D-tony-221): F6 suma 10 de respeto en la zona donde estás (como vencer a uno) y
    /// F7 te da 500 de dinero. La demo final los apaga compilando con el símbolo DEMO_FINAL.
    /// </summary>
    public class TerritorioDePrueba : MonoBehaviour
    {
        [Min(1)] [SerializeField] private int dineroF7 = 500;

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado == null)
            {
                return;
            }
            if (teclado.f6Key.wasPressedThisFrame)
            {
                IJugador jugador = Registro<IJugador>.Actual;
                ITerritorio territorio = Registro<ITerritorio>.Actual;
                if (jugador != null && jugador.Raiz != null && territorio != null)
                {
                    string zona = territorio.ZonaEn(jugador.Raiz.position);
                    territorio.RegistrarVencido(jugador.Raiz.position, territorio.Dueno(zona), false);
                }
            }
            if (teclado.f7Key.wasPressedThisFrame)
            {
                Registro<ICartera>.Actual?.Abonar(dineroF7, MotivoDinero.Otro);
            }
        }
    }
}
#endif
