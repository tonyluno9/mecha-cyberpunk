using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1397: hace girar cada descendiente cuyo nombre empieza con "Rotor_" sobre su Y local (el pivote del modelo está en el centro de giro).
    /// Solo cambia el visual: no toca colisión ni IA del dron.
    /// </summary>
    public class GiroRotores : MonoBehaviour
    {
        public const string PrefijoRotor = "Rotor_";

        [SerializeField, Min(0f), Tooltip("Velocidad de giro de cada rotor en °/s.")] private float velocidad = 1800f;
        [SerializeField, Tooltip("Si es true, los rotores de índice impar giran al revés (pares contrarrotantes).")] private bool alternarSentido = true;

        private Transform[] rotores;

        public float Velocidad => velocidad;
        public int CantidadRotores => rotores != null ? rotores.Length : 0;

        private void Awake()
        {
            Buscar();
        }

        /// <summary>Busca los rotores entre los descendientes (también los apagados).</summary>
        public void Buscar()
        {
            List<Transform> lista = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t != transform && t.name.StartsWith(PrefijoRotor))
                {
                    lista.Add(t);
                }
            }
            rotores = lista.ToArray();
        }

        private void Update()
        {
            Girar(Time.deltaTime);
        }

        /// <summary>Gira todos los rotores lo que toca en <paramref name="dt"/> segundos.</summary>
        public void Girar(float dt)
        {
            if (rotores == null)
            {
                Buscar();
            }
            for (int i = 0; i < rotores.Length; i++)
            {
                rotores[i].Rotate(0f, CalculoGiroRotores.GradosEnPaso(velocidad, dt, i, alternarSentido), 0f, Space.Self);
            }
        }
    }
}
