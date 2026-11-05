using System;
using System.Collections.Generic;
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
        private const float DistanciaMinimaParaMirar = 0.00000001f; // T-534: antes 0.001 (a 60 fps o más el paso de un cuadro no llegaba y no giraba)
        private const float DesplazamientoMinimoParaDecidir = 0.00000001f;
        // T-534: si algo sólido le tapa el paso (muro, puesto, carro), se da la vuelta en lugar de atravesarlo o quedarse pegado.
        private const float AlturaPecho = 1.1f;
        private const float RadioCuerpo = 0.25f;
        private const float DistanciaVer = 0.7f;
        private const float SegundosEntreVueltas = 1.5f;
        // T-631: no se atraviesan entre sí. Cada quien camina por su derecha (los que van en sentidos opuestos se cruzan sin chocar) y el que
        // alcanza a otro que va delante en el mismo sentido se frena; si se traba más de SegundosParaDestrabar, da la vuelta.
        private const float DesvioLateral = 0.3f;
        private const float VelocidadDesvio = 1f;
        private const float DistanciaSeparacion = 0.9f;
        private const float AnchoSeparacion = 0.5f;
        private const float SegundosParaDestrabar = 3f;
        private static readonly List<PeatonMundo> ActivosLista = new List<PeatonMundo>();
        private Vector3 desvio;
        private Vector3 posRuta;
        private float segundosFrenado;
        private float proximaVuelta;
        private Vector3 haciaDonde;

        /// <summary>Vueltas que dio por encontrarse algo en el camino (T-534).</summary>
        public int Vueltas { get; private set; }

        /// <summary>Lo frenó otro peatón que va delante (T-631).</summary>
        public bool Frenado { get; private set; }

        public static IReadOnlyList<PeatonMundo> Activos => ActivosLista;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => ActivosLista.Clear();

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
            ActivosLista.Add(this);
        }

        private void OnDisable()
        {
            EventosRuido.Disparo -= AlOirDisparo;
            ActivosLista.Remove(this);
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
            if (Time.time >= proximaVuelta && Tapado())
            {
                proximaVuelta = Time.time + SegundosEntreVueltas;
                calculo.InvertirSentido();
                haciaDonde = -haciaDonde;
                Vueltas++;
            }
            Frenado = HayPeatonAdelante();
            if (Frenado)
            {
                segundosFrenado += Time.deltaTime;
                if (segundosFrenado > SegundosParaDestrabar)
                {
                    segundosFrenado = 0f;
                    calculo.InvertirSentido();
                    haciaDonde = -haciaDonde;
                    Vueltas++;
                }
                Colocar();
                return;
            }
            segundosFrenado = 0f;
            Vector3 antes = posRuta;
            calculo.Avanzar(Time.deltaTime);
            Colocar();
            Vector3 desplazamiento = transform.parent != null ? transform.parent.TransformVector(posRuta - antes) : posRuta - antes;
            if (desplazamiento.sqrMagnitude > DesplazamientoMinimoParaDecidir)
            {
                haciaDonde = desplazamiento.normalized;
            }
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

        // Otro peatón a menos de DistanciaSeparacion, delante y en mi carril, que camina en el mismo sentido que yo.
        private bool HayPeatonAdelante()
        {
            Vector3 adelante = haciaDonde;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.01f)
            {
                return false;
            }
            adelante.Normalize();
            foreach (PeatonMundo otro in ActivosLista)
            {
                if (otro == this)
                {
                    continue;
                }
                Vector3 hacia = otro.transform.position - transform.position;
                hacia.y = 0f;
                float delante = Vector3.Dot(hacia, adelante);
                if (delante <= 0f || delante > DistanciaSeparacion)
                {
                    continue;
                }
                float lado = (hacia - adelante * delante).magnitude;
                Vector3 suyo = otro.haciaDonde;
                suyo.y = 0f;
                if (lado < AnchoSeparacion && Vector3.Dot(suyo, adelante) >= 0f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool Tapado()
        {
            Vector3 adelante = haciaDonde;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.01f)
            {
                return false;
            }
            Vector3 origen = transform.position + Vector3.up * AlturaPecho - adelante.normalized * RadioCuerpo;
            foreach (RaycastHit h in Physics.SphereCastAll(origen, RadioCuerpo, adelante.normalized, DistanciaVer + RadioCuerpo, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!h.collider.transform.IsChildOf(transform) && h.distance > 0f && h.collider.GetComponentInParent<PeatonMundo>() == null)
                {
                    return true;
                }
            }
            return false;
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
            posRuta = RutasBanqueta.Posicion(puntos, calculo);
            Vector3 mundo = haciaDonde;
            mundo.y = 0f;
            if (mundo.sqrMagnitude > 0.01f && transform.parent != null)
            {
                // Camina por su derecha, en espacio local del padre.
                Vector3 derecha = transform.parent.InverseTransformDirection(Vector3.Cross(Vector3.up, mundo.normalized));
                derecha.y = 0f;
                desvio = Vector3.MoveTowards(desvio, derecha.normalized * DesvioLateral, VelocidadDesvio * Time.deltaTime);
            }
            transform.localPosition = posRuta + desvio;
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
