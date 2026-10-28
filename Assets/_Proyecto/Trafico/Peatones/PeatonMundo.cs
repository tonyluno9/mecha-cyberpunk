using System;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// Un peatón del barrio (T-331): camina su ruta con <see cref="CalculoPeaton"/> y se detiene mientras el jugador esté a menos de
    /// <see cref="ConfigPeatones.DistanciaParada"/>, mirándolo. Sin colliders: solo mueve su transform sobre la banqueta.
    /// </summary>
    public class PeatonMundo : MonoBehaviour
    {
        private const float VelocidadGiroGradosPorSegundo = 360f;
        private const float DistanciaMinimaParaMirar = 0.001f;

        private Vector3[] puntos;
        private CalculoPeaton calculo;
        private ConfigPeatones config;
        private Func<Vector3?> posicionJugador;

        public bool Detenido { get; private set; }

        /// <summary>Lo prepara el grupo: ruta (al menos 2 puntos, en espacio local del padre), números, azar y cómo saber dónde está el jugador (null si no hay).</summary>
        public void Preparar(Vector3[] ruta, ConfigPeatones numeros, System.Random azar, Func<Vector3?> dondeEstaElJugador, float segundosPrevios)
        {
            puntos = ruta;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
            calculo = numeros.Crear(RutasBanqueta.Largos(ruta), azar);
            calculo.Avanzar(segundosPrevios);
            Colocar();
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            Vector3? jugador = posicionJugador?.Invoke();
            if (jugador.HasValue && DistanciaPlano(jugador.Value, transform.position) < config.DistanciaParada)
            {
                Detenido = true;
                Mirar(jugador.Value - transform.position);
                return;
            }
            Detenido = false;
            Vector3 antes = transform.position;
            calculo.Avanzar(Time.deltaTime);
            Colocar();
            Mirar(transform.position - antes);
        }

        private void Colocar()
        {
            transform.localPosition = RutasBanqueta.Posicion(puntos, calculo);
        }

        private void Mirar(Vector3 direccion)
        {
            direccion.y = 0f;
            if (direccion.sqrMagnitude < DistanciaMinimaParaMirar)
            {
                return;
            }
            Quaternion meta = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, VelocidadGiroGradosPorSegundo * Time.deltaTime);
        }

        private static float DistanciaPlano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
