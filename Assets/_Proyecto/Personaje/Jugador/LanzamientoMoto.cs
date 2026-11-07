using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-696: un choque fuerte en moto avienta al conductor. Sale volando un trecho en la dirección en que iba (sin pasar por muros), cae con la
    /// animación de caída que ya existe (la misma de caer derrotado), se queda tirado un momento, pierde un poco de vida (menos que un disparo)
    /// y se levanta con la animación de levantarse; mientras tanto no controla. Lo agrega y lo dispara <see cref="JugadorAPie"/>.
    /// </summary>
    public class LanzamientoMoto : MonoBehaviour
    {
        /// <summary>Rapidez mínima (m/s) con la que debía ir la moto para que el choque avente al conductor.</summary>
        public const float RapidezMinima = 9f;
        /// <summary>Daño de la caída: menos que un disparo de pistola (25).</summary>
        public const float DanioCaida = 10f;
        private const float FraccionRapidez = 0.55f;
        private const float RapidezMaxima = 9f;
        private const float SegundosVuelo = 0.9f;
        private const float SegundosTirado = 1.6f;
        private const float SegundosLevantarse = 1.0f;
        private const float Gravedad = 20f;

        private CharacterController controlador;
        private ControladorAPie controladorAPie;
        private VisualPersonaje visual;
        private IRecibeDanio vida;
        private Coroutine rutina;

        /// <summary>True mientras vuela, está tirado o se levanta.</summary>
        public bool Activo => rutina != null;

        /// <summary>True si una moto que iba a esta velocidad avienta al conductor.</summary>
        public static bool Lanza(Vector3 velocidadMoto)
        {
            return new Vector3(velocidadMoto.x, 0f, velocidadMoto.z).magnitude >= RapidezMinima;
        }

        /// <summary>Rapidez (m/s) con la que sale volando el conductor de una moto que iba a esta velocidad.</summary>
        public static float RapidezDeSalida(Vector3 velocidadMoto)
        {
            return Mathf.Min(RapidezMaxima, new Vector3(velocidadMoto.x, 0f, velocidadMoto.z).magnitude * FraccionRapidez);
        }

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            controladorAPie = GetComponent<ControladorAPie>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
            vida = GetComponent<IRecibeDanio>();
        }

        /// <summary>Avienta al conductor en la dirección de <paramref name="velocidadMoto"/> (la de la moto justo antes del choque).</summary>
        public void Lanzar(Vector3 velocidadMoto)
        {
            if (rutina != null || !Lanza(velocidadMoto))
            {
                return;
            }
            Vector3 direccion = new Vector3(velocidadMoto.x, 0f, velocidadMoto.z).normalized;
            rutina = StartCoroutine(Secuencia(direccion, RapidezDeSalida(velocidadMoto)));
        }

        private IEnumerator Secuencia(Vector3 direccion, float rapidez)
        {
            if (controladorAPie != null)
            {
                controladorAPie.ActivarControl(false);
            }
            if (visual != null)
            {
                visual.Congelado = false;
                visual.Caido = true;
            }
            vida?.RecibirDanio(new InfoDanio(DanioCaida, TipoDanio.Golpe, transform.position, direccion, null));

            float vertical = 0f;
            for (float t = 0f; t < SegundosVuelo; t += Time.deltaTime)
            {
                float avance = rapidez * (1f - t / SegundosVuelo);
                vertical = controlador.isGrounded ? -1f : vertical - Gravedad * Time.deltaTime;
                if (controlador.enabled)
                {
                    controlador.Move((direccion * avance + Vector3.up * vertical) * Time.deltaTime);
                }
                yield return null;
            }
            yield return new WaitForSeconds(SegundosTirado);
            if (visual != null)
            {
                visual.Caido = false;
            }
            yield return new WaitForSeconds(SegundosLevantarse);
            if (controladorAPie != null)
            {
                controladorAPie.ActivarControl(true);
            }
            rutina = null;
        }

        private void OnDisable()
        {
            if (rutina != null)
            {
                StopCoroutine(rutina);
                rutina = null;
                if (visual != null)
                {
                    visual.Caido = false;
                }
                if (controladorAPie != null)
                {
                    controladorAPie.ActivarControl(true);
                }
            }
        }
    }
}
