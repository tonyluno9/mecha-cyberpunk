using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Carros falsos de la autopista elevada (T-098): cada hijo recorre el eje X del padre a su velocidad y vuelve a empezar al salir del
    /// rango. Solo es decorado lejano, sin colliders ni lógica de juego.
    /// </summary>
    public class TraficoAutopista : MonoBehaviour
    {
        [Tooltip("Carros que se mueven; su signo de velocidad lo decide 'velocidades'.")]
        [SerializeField] private Transform[] carros;

        [Tooltip("Velocidad de cada carro en X local (m/s, con signo), una por carro.")]
        [SerializeField] private float[] velocidades;

        [Tooltip("Mitad del recorrido (m): al pasar de ±este valor el carro reaparece en el otro extremo.")]
        [SerializeField, Min(1f)] private float mitadRecorrido = 160f;

        private void Awake()
        {
            if (carros == null || velocidades == null || carros.Length != velocidades.Length)
            {
                Debug.LogError("[Mundo] TraficoAutopista en '" + name + "' necesita un carro y una velocidad por cada uno.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            for (int i = 0; i < carros.Length; i++)
            {
                Vector3 posicion = carros[i].localPosition;
                posicion.x += velocidades[i] * Time.deltaTime;
                if (posicion.x > mitadRecorrido)
                {
                    posicion.x -= 2f * mitadRecorrido;
                }
                else if (posicion.x < -mitadRecorrido)
                {
                    posicion.x += 2f * mitadRecorrido;
                }
                carros[i].localPosition = posicion;
            }
        }
    }
}
