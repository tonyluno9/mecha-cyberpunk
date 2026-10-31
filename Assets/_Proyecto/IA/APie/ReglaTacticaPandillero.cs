using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Táctica del pandillero armado (T-423): rodear, cubrirse entre ráfagas, asomarse y huir con poca vida. Lógica pura sin Unity.
    /// </summary>
    public class ReglaTacticaPandillero
    {
        public enum Modo
        {
            Rodear,
            Cubrirse,
            Asomado,
            Huir
        }

        private readonly float fraccionHuida;
        private readonly float segundosCubierto;
        private readonly float segundosAsomado;
        private float temporizador;

        public ReglaTacticaPandillero(float fraccionHuida, float segundosCubierto, float segundosAsomado)
        {
            this.fraccionHuida = fraccionHuida;
            this.segundosCubierto = segundosCubierto;
            this.segundosAsomado = segundosAsomado;
        }

        public Modo Actual { get; private set; } = Modo.Rodear;

        /// <summary>True si en este modo puede pedir turno y disparar.</summary>
        public bool PuedeDisparar => Actual == Modo.Rodear || Actual == Modo.Asomado;

        public void Reiniciar()
        {
            Actual = Modo.Rodear;
            temporizador = 0f;
        }

        /// <summary>
        /// Avanza la táctica. <paramref name="disparo"/> es true en el paso en que acaba de disparar; <paramref name="hayCubierta"/> si conoce
        /// una cubierta cercana; <paramref name="acorralado"/> si el jugador lo alcanzó (entonces pelea en vez de huir).
        /// </summary>
        public Modo Avanzar(float dt, float fraccionVida, bool hayCubierta, bool acorralado, bool disparo, bool puedeHuir)
        {
            if (puedeHuir && fraccionVida < fraccionHuida && !acorralado)
            {
                Actual = Modo.Huir;
                return Actual;
            }
            if (Actual == Modo.Huir)
            {
                // Acorralado (o curado): vuelve a pelear.
                Actual = Modo.Rodear;
                temporizador = 0f;
            }
            switch (Actual)
            {
                case Modo.Rodear:
                    if (disparo && hayCubierta)
                    {
                        Actual = Modo.Cubrirse;
                        temporizador = 0f;
                    }
                    break;
                case Modo.Cubrirse:
                    temporizador += dt;
                    if (!hayCubierta)
                    {
                        Actual = Modo.Rodear;
                    }
                    else if (temporizador >= segundosCubierto)
                    {
                        Actual = Modo.Asomado;
                        temporizador = 0f;
                    }
                    break;
                case Modo.Asomado:
                    temporizador += dt;
                    if (disparo || temporizador >= segundosAsomado || !hayCubierta)
                    {
                        Actual = hayCubierta ? Modo.Cubrirse : Modo.Rodear;
                        temporizador = 0f;
                    }
                    break;
            }
            return Actual;
        }

        /// <summary>Ángulo (grados) del puesto <paramref name="indice"/> de <paramref name="total"/> en un arco centrado en <paramref name="centro"/>.</summary>
        public static float AnguloRodeo(int indice, int total, float centro, float arco)
        {
            if (total <= 1)
            {
                return centro;
            }
            return centro - arco * 0.5f + arco * Math.Max(0, Math.Min(indice, total - 1)) / (total - 1);
        }
    }
}
