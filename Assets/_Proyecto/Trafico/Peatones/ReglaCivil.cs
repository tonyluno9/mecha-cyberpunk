using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// Lógica pura de un civil agredido (T-1305): al recibir un golpe o bala que no lo mata, una fracción decide pelear (<see cref="EstadoCivil.Revancha"/>)
    /// si el jugador está al alcance, y el resto huye. En revancha da unos golpes espaciados y luego huye. Sin MonoBehaviour: el tiempo y el azar se inyectan.
    /// </summary>
    public sealed class ReglaCivil
    {
        /// <summary>Altura de los ojos del jugador para saber si una bala salió de él (m).</summary>
        public const float AlturaOjosJugador = 1.4f;
        /// <summary>Distancia máxima desde la que un golpe cuerpo a cuerpo se atribuye al jugador (m).</summary>
        public const float AlcanceGolpeJugador = 3f;
        /// <summary>Distancia máxima desde la que una bala se atribuye al jugador (m).</summary>
        public const float AlcanceBalaJugador = 150f;
        /// <summary>Qué tan alineada debe ir la bala con la línea jugador-impacto para atribuirla al jugador (coseno, de 0 a 1).</summary>
        public const float AlineacionBala = 0.9f;
        private const float DistanciaMinima = 0.0001f;

        private readonly Func<float> azar;
        private readonly float probabilidadRevancha;
        private readonly float alcance;
        private readonly int golpesMaximos;
        private readonly float segundosEntreGolpes;
        private readonly float segundosMaximos;
        private int golpesRestantes;
        private float espera;
        private float tiempoEnRevancha;

        public ReglaCivil(Func<float> azarUnitario, float probabilidadDeRevancha, float alcanceDeRevancha, int golpes, float segundosEntreGolpe, float segundosMaximosDeRevancha)
        {
            azar = azarUnitario;
            probabilidadRevancha = probabilidadDeRevancha;
            alcance = alcanceDeRevancha;
            golpesMaximos = golpes;
            segundosEntreGolpes = segundosEntreGolpe;
            segundosMaximos = segundosMaximosDeRevancha;
        }

        public EstadoCivil Estado { get; private set; }

        /// <summary>Un golpe o bala no mortal. <paramref name="distanciaJugador"/> es null si no se sabe dónde está el jugador. Devuelve el estado resultante.</summary>
        public EstadoCivil Herido(float? distanciaJugador)
        {
            if (Estado == EstadoCivil.Revancha)
            {
                return Estado;
            }
            bool alAlcance = distanciaJugador.HasValue && distanciaJugador.Value <= alcance;
            if (alAlcance && azar() < probabilidadRevancha)
            {
                Estado = EstadoCivil.Revancha;
                golpesRestantes = golpesMaximos;
                espera = 0f;
                tiempoEnRevancha = 0f;
            }
            else
            {
                Estado = EstadoCivil.Huyendo;
            }
            return Estado;
        }

        /// <summary>
        /// Avanza la revancha. Devuelve true si en este cuadro debe dar un golpe (jugador a menos del alcance y pasó la espera).
        /// Al dar el último golpe o agotarse el tiempo, el estado pasa a <see cref="EstadoCivil.Huyendo"/>.
        /// </summary>
        public bool Avanzar(float dt, float? distanciaJugador)
        {
            if (Estado != EstadoCivil.Revancha)
            {
                return false;
            }
            tiempoEnRevancha += dt;
            espera -= dt;
            bool golpea = false;
            if (espera <= 0f && distanciaJugador.HasValue && distanciaJugador.Value <= alcance)
            {
                golpea = true;
                golpesRestantes--;
                espera = segundosEntreGolpes;
            }
            if (golpesRestantes <= 0 || tiempoEnRevancha >= segundosMaximos)
            {
                Estado = EstadoCivil.Huyendo;
            }
            return golpea;
        }

        /// <summary>Vuelve a <see cref="EstadoCivil.Normal"/> (terminó de huir o murió).</summary>
        public void Calmar()
        {
            Estado = EstadoCivil.Normal;
        }

        /// <summary>
        /// True si el daño recibido probablemente lo causó el jugador (T-1305): sin atacante conocido, un golpe con el jugador pegado,
        /// o una bala cuya trayectoria viene de la posición del jugador. Sirve para sumar calor solo por muertes del jugador.
        /// </summary>
        public static bool EsAgresionDelJugador(TipoDanio tipo, Vector3 punto, Vector3 direccion, Vector3? jugador, bool hayAtacante)
        {
            if (hayAtacante || !jugador.HasValue)
            {
                return false;
            }
            Vector3 ojos = jugador.Value + Vector3.up * AlturaOjosJugador;
            Vector3 haciaJugador = ojos - punto;
            float distancia = haciaJugador.magnitude;
            if (tipo == TipoDanio.Golpe)
            {
                return distancia <= AlcanceGolpeJugador;
            }
            if (tipo != TipoDanio.Disparo || distancia > AlcanceBalaJugador || distancia < DistanciaMinima || direccion.sqrMagnitude < DistanciaMinima)
            {
                return false;
            }
            return Vector3.Dot(haciaJugador / distancia, -direccion.normalized) >= AlineacionBala;
        }
    }
}
