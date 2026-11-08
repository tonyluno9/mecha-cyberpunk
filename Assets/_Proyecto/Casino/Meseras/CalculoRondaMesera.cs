using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Casino.Meseras
{
    /// <summary>
    /// T-892: la ronda de una mesera, sin MonoBehaviour: va y vuelve por sus puntos y se detiene un momento en cada uno. Recibe el tiempo como parámetro.
    /// </summary>
    public sealed class CalculoRondaMesera
    {
        private readonly IReadOnlyList<Vector3> puntos;
        private readonly float velocidad;
        private readonly float pausa;
        private int destino;
        private int sentido = 1;
        private float esperaRestante;

        public Vector3 Posicion { get; private set; }

        /// <summary>Hacia dónde mira (sin componente vertical): se conserva durante la pausa.</summary>
        public Vector3 Direccion { get; private set; } = Vector3.forward;

        public bool EstaCaminando => esperaRestante <= 0f;

        /// <param name="puntos">Al menos dos puntos, en orden.</param>
        /// <param name="velocidad">m/s.</param>
        /// <param name="pausa">Segundos parada en cada punto.</param>
        /// <param name="esperaInicial">Segundos antes de arrancar (para que no vayan todas al mismo paso).</param>
        public CalculoRondaMesera(IReadOnlyList<Vector3> puntos, float velocidad, float pausa, float esperaInicial)
        {
            this.puntos = puntos;
            this.velocidad = velocidad;
            this.pausa = pausa;
            esperaRestante = esperaInicial;
            Posicion = puntos[0];
            destino = 1;
            Mirar(puntos[1] - puntos[0]);
        }

        public void Avanzar(float dt)
        {
            if (esperaRestante > 0f)
            {
                esperaRestante -= dt;
                return;
            }
            Vector3 meta = puntos[destino];
            Vector3 falta = meta - Posicion;
            float paso = velocidad * dt;
            if (falta.magnitude > paso)
            {
                Posicion += falta.normalized * paso;
                Mirar(falta);
                return;
            }
            Posicion = meta;
            esperaRestante = pausa;
            if (destino + sentido < 0 || destino + sentido >= puntos.Count)
            {
                sentido = -sentido;
            }
            destino += sentido;
        }

        private void Mirar(Vector3 d)
        {
            d.y = 0f;
            if (d.sqrMagnitude > Mathf.Epsilon)
            {
                Direccion = d.normalized;
            }
        }
    }
}
