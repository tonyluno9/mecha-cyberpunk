using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// Lógica pura: de una lista de puntos candidatos elige el que el jugador quiere usar.
    /// Mide en el plano horizontal (se ignora la altura). Lo reutiliza cualquier cosa interactuable (puertas, carros).
    /// </summary>
    public static class SelectorInteraccion
    {
        private const float ToleranciaDistancia = 0.001f;
        private const float DistanciaMinima = 0.0001f;

        /// <summary>
        /// Devuelve el índice del mejor punto o -1. Es el más cercano dentro del alcance (m) y del ángulo máximo (°)
        /// respecto a la dirección de la vista; a igual distancia (0.001 m), el más centrado.
        /// </summary>
        public static int Elegir(Vector3 posicion, Vector3 direccionVista, IReadOnlyList<Vector3> puntos, float alcance, float anguloMaximo)
        {
            if (puntos == null || puntos.Count == 0) return -1;

            Vector2 vista = new Vector2(direccionVista.x, direccionVista.z);
            bool hayVista = vista.sqrMagnitude > DistanciaMinima;
            if (hayVista) vista.Normalize();

            int mejor = -1;
            float mejorDistancia = 0f;
            float mejorAngulo = 0f;
            for (int i = 0; i < puntos.Count; i++)
            {
                Vector2 hacia = new Vector2(puntos[i].x - posicion.x, puntos[i].z - posicion.z);
                float distancia = hacia.magnitude;
                if (distancia > alcance) continue;

                // Encima del jugador: cuenta como centrado.
                float angulo = 0f;
                if (distancia > DistanciaMinima && hayVista) angulo = Vector2.Angle(vista, hacia);
                if (angulo > anguloMaximo) continue;

                bool mas = mejor < 0
                    || distancia < mejorDistancia - ToleranciaDistancia
                    || (Mathf.Abs(distancia - mejorDistancia) <= ToleranciaDistancia && angulo < mejorAngulo);
                if (mas)
                {
                    mejor = i;
                    mejorDistancia = distancia;
                    mejorAngulo = angulo;
                }
            }
            return mejor;
        }
    }
}
