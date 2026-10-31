using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// Un peatón del barrio (T-331): camina su ruta con <see cref="CalculoPeaton"/> y se detiene mientras el jugador esté a menos de
    /// <see cref="ConfigPeatones.DistanciaParada"/>, mirándolo. Sin colliders: solo mueve su transform sobre la banqueta.
    /// T-443: si oye un disparo a menos de <see cref="ConfigPeatones.RadioOirDisparo"/> (<see cref="EventosRuido"/>) sale corriendo por
    /// su ruta, en el sentido que se aleja del disparo, durante <see cref="ConfigPeatones.SegundosHuida"/>; mientras corre no se detiene por el jugador.
    /// </summary>
    public class PeatonMundo : MonoBehaviour
    {
        private const float VelocidadGiroGradosPorSegundo = 360f;
        private const float DistanciaMinimaParaMirar = 0.001f;
        private const float DesplazamientoMinimoParaDecidir = 0.00000001f;

        private Vector3[] puntos;
        private CalculoPeaton calculo;
        private ConfigPeatones config;
        private Func<Vector3?> posicionJugador;
        private float huidaRestante;
        private bool sentidoDecidido = true;
        private Vector3 origenDelRuido;

        public bool Detenido { get; private set; }

        /// <summary>True mientras corre por haber oído un disparo.</summary>
        public bool Huyendo => huidaRestante > 0f;

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

        private void OnEnable()
        {
            EventosRuido.Disparo += AlOirDisparo;
        }

        private void OnDisable()
        {
            EventosRuido.Disparo -= AlOirDisparo;
            Calmar();
        }

        /// <summary>Un disparo en <paramref name="posicion"/>: si está dentro del radio de oído, el peatón huye.</summary>
        public void AlOirDisparo(Vector3 posicion)
        {
            if (calculo == null || config == null || !isActiveAndEnabled)
            {
                return;
            }
            if (DistanciaPlano(posicion, transform.position) > config.RadioOirDisparo)
            {
                return;
            }
            origenDelRuido = posicion;
            huidaRestante = config.SegundosHuida;
            sentidoDecidido = false;
            calculo.MultiplicadorVelocidad = config.MultiplicadorHuida;
            calculo.CancelarEspera();
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            bool huyendo = huidaRestante > 0f;
            if (huyendo)
            {
                huidaRestante -= Time.deltaTime;
                if (huidaRestante <= 0f)
                {
                    Calmar();
                    huyendo = false;
                }
            }
            Vector3? jugador = posicionJugador?.Invoke();
            if (!huyendo && jugador.HasValue && DistanciaPlano(jugador.Value, transform.position) < config.DistanciaParada)
            {
                Detenido = true;
                Mirar(jugador.Value - transform.position);
                return;
            }
            Detenido = false;
            Vector3 antes = transform.position;
            calculo.Avanzar(Time.deltaTime);
            Colocar();
            Vector3 desplazamiento = transform.position - antes;
            if (huyendo && !sentidoDecidido && desplazamiento.sqrMagnitude > DesplazamientoMinimoParaDecidir)
            {
                DecidirSentido(desplazamiento);
            }
            Mirar(desplazamiento);
        }

        // Si el primer paso lo acerca al disparo, da la vuelta (queda donde estaba) y sigue hacia el otro lado de la ruta.
        private void DecidirSentido(Vector3 desplazamiento)
        {
            sentidoDecidido = true;
            Vector3 haciaElRuido = origenDelRuido - transform.position;
            desplazamiento.y = 0f;
            haciaElRuido.y = 0f;
            if (Vector3.Dot(desplazamiento, haciaElRuido) > 0f)
            {
                calculo.InvertirSentido();
                Colocar();
            }
        }

        private void Calmar()
        {
            huidaRestante = 0f;
            sentidoDecidido = true;
            if (calculo != null)
            {
                calculo.MultiplicadorVelocidad = 1f;
            }
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
