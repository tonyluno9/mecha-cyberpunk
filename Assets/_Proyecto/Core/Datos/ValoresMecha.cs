using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Juego de valores de un mecha, los mismos que tiene <see cref="DatosMecha"/>. Es un dato simple: no hace
    /// cálculos ni sabe de piezas. Sirve para leer los valores de un DatosMecha (<see cref="DatosMecha.Valores"/>)
    /// y para crear uno nuevo en ejecución (<see cref="DatosMecha.CrearEnEjecucion"/>).
    /// </summary>
    [Serializable]
    public struct ValoresMecha
    {
        /// <summary>Nombre en pantallas (texto).</summary>
        public string nombre;

        /// <summary>Vida al empezar la pelea (puntos). Mínimo 1.</summary>
        public float vidaMaxima;

        /// <summary>Velocidad al caminar (m/s). Mínimo 0.1.</summary>
        public float velocidadMaxima;

        /// <summary>Qué tan rápido arranca (m/s²). Mínimo 0.1.</summary>
        public float aceleracion;

        /// <summary>Qué tan rápido frena (m/s²). Mínimo 0.1.</summary>
        public float desaceleracion;

        /// <summary>Giro máximo del cuerpo (°/s). Mínimo 1.</summary>
        public float velocidadGiroMaxima;

        /// <summary>Daño de un golpe (puntos). Mínimo 0.1.</summary>
        public float danioGolpe;

        /// <summary>Alcance del golpe, medido desde el centro del mecha hacia adelante (m). Mínimo 0.1.</summary>
        public float alcanceGolpe;

        /// <summary>Del clic (o la decisión del rival) al impacto (s). Mínimo 0.01.</summary>
        public float preparacionGolpe;

        /// <summary>Del impacto a poder golpear otra vez (s). Mínimo 0.01.</summary>
        public float recargaGolpe;

        /// <summary>Del clic derecho a que la guardia quede arriba (s). Mínimo 0.01.</summary>
        public float tiempoSubirGuardia;

        /// <summary>Parte del daño que quita un bloqueo normal (de 0 a 1).</summary>
        public float reduccionBloqueo;

        /// <summary>Si la guardia quedó arriba hace menos que esto, no entra daño (s). Mínimo 0.01.</summary>
        public float ventanaBloqueoPerfecto;
    }
}
