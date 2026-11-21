using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: la sirena de la patrulla que se lleva al jugador arrestado: un punto que se aleja en línea recta mientras suena el bucle POL_SIRENA_LOOP en 3D,
    /// hasta que se acaba el tiempo y se apaga solo (LEEME 20-nov: arrestado = MEL_GOLPE + POL_SIRENA_LOOP alejándose + esposas).
    /// </summary>
    public class SirenaQueSeAleja : MonoBehaviour
    {
        private Vector3 direccion;
        private float velocidad;
        private float restante;
        private int bucle = Sonidos.SinBucle;

        /// <summary>Pone la sirena en <paramref name="desde"/> y la aleja hacia <paramref name="hacia"/> (plano horizontal) durante <paramref name="segundos"/>.</summary>
        public static SirenaQueSeAleja Iniciar(Vector3 desde, Vector3 hacia, float metrosPorSegundo, float segundos)
        {
            GameObject go = new GameObject("SirenaQueSeAleja");
            go.transform.position = desde;
            SirenaQueSeAleja s = go.AddComponent<SirenaQueSeAleja>();
            hacia.y = 0f;
            s.direccion = hacia.sqrMagnitude > 0.0001f ? hacia.normalized : Vector3.forward;
            s.velocidad = metrosPorSegundo;
            s.restante = segundos;
            s.bucle = Sonidos.ReproducirBucle(ReglaSonidoEventos.SirenaArresto, go.transform);
            return s;
        }

        /// <summary>True si el bucle de la sirena está sonando (para las pruebas).</summary>
        public bool Sonando => bucle != Sonidos.SinBucle;

        private void Update()
        {
            restante -= Time.unscaledDeltaTime;
            transform.position += direccion * (velocidad * Time.unscaledDeltaTime);
            if (restante <= 0f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (bucle != Sonidos.SinBucle) Sonidos.DetenerBucle(bucle);
            bucle = Sonidos.SinBucle;
        }
    }
}
