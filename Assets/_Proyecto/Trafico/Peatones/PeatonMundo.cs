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
    public class PeatonMundo : MonoBehaviour, IRecibeDanio
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

        // T-663: un civil que vio un delito se queda parado con el teléfono (icono encima) hasta terminar la llamada; si lo matan, cae y no llama.
        private const float AlturaCapsula = 1.8f;
        private const float RadioCapsula = 0.3f;
        private const float SegundosCuerpo = 10f;
        private const float MargenIcono = 0.35f;
        private float vida;
        private float segundosMuerto;
        private IndicadorLlamada indicador;
        private CapsuleCollider capsula;
        private Renderer[] visuales;

        // T-705: mientras llama saca el teléfono (Inter_Telefono) y lo vuelve a sacar cada vez que termina el gesto. El gesto lo juega VisualPersonaje
        // (otro ensamblado que Trafico no referencia): se le manda el mensaje "Telefonear" con BroadcastMessage y se mira el Animator del modelo activo.
        private const float SegundosEntreTelefono = 0.4f;
        private const string EstadoTelefono = "Telefono";
        private Animator animadorActivo;
        private float proximoTelefono;

        /// <summary>T-705: cuántas veces sacó el teléfono al llamar (para pruebas).</summary>
        public int GestosTelefono { get; private set; }

        /// <summary>T-705: True mientras está tirado en el piso (el conductor al que le roban el carro): no saca el teléfono.</summary>
        public bool Tumbado { get; set; }

        /// <summary>
        /// T-705: si es mayor que 0, la distancia al jugador (m) a la que este testigo pierde de vista al jugador y cancela su llamada, en lugar
        /// del radio general de <see cref="ConfigPersecucion.RadioAbandonoCivil"/>. El conductor al que le roban el carro usa uno corto.
        /// </summary>
        public float RadioAbandonoPropio { get; set; }

        private void MantenerTelefono()
        {
            if (!Llamando || Muerto || Tumbado || Time.time < proximoTelefono)
            {
                return;
            }
            // El prefab trae dos modelos (persona y robot, solo uno activo): el Animator es el del modelo activo.
            if (animadorActivo == null || !animadorActivo.isActiveAndEnabled)
            {
                animadorActivo = GetComponentInChildren<Animator>(false);
            }
            if (animadorActivo == null || animadorActivo.runtimeAnimatorController == null)
            {
                return;
            }
            if (animadorActivo.GetCurrentAnimatorStateInfo(0).IsName(EstadoTelefono)
                || (animadorActivo.IsInTransition(0) && animadorActivo.GetNextAnimatorStateInfo(0).IsName(EstadoTelefono)))
            {
                return;
            }
            proximoTelefono = Time.time + SegundosEntreTelefono;
            GestosTelefono++;
            BroadcastMessage("Telefonear", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>True mientras está llamando a la policía (parado, con el teléfono).</summary>
        public bool Llamando { get; private set; }

        /// <summary>True si ya lo mataron: queda tirado un rato y no hace nada.</summary>
        public bool Muerto { get; private set; }

        /// <summary>Se avisa al morir un peatón.</summary>
        public static event Action<PeatonMundo> Murio;

        /// <summary>Empieza a llamar: deja de huir y de caminar y muestra el icono del teléfono.</summary>
        public void IniciarLlamada()
        {
            if (Muerto)
            {
                return;
            }
            Llamando = true;
            proximoTelefono = 0f;
            Calmar();
            if (indicador == null)
            {
                indicador = IndicadorLlamada.Crear(transform, MargenIcono);
            }
            indicador.gameObject.SetActive(true);
        }

        /// <summary>Termina o cancela la llamada: quita el icono y sigue su camino.</summary>
        public void TerminarLlamada()
        {
            Llamando = false;
            if (indicador != null)
            {
                indicador.gameObject.SetActive(false);
            }
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (Muerto || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            float antes = vida;
            vida -= info.Cantidad;
            bool mato = vida <= 0f;
            if (mato)
            {
                Matar();
            }
            return new ResultadoDanio(Mathf.Min(antes, info.Cantidad), TipoBloqueo.Ninguno, mato);
        }

        /// <summary>Lo mata: cae, queda tirado <see cref="SegundosCuerpo"/> s y se esconde. Cancela cualquier llamada.</summary>
        public void Matar()
        {
            if (Muerto)
            {
                return;
            }
            Muerto = true;
            TerminarLlamada();
            Calmar();
            segundosMuerto = 0f;
            if (capsula != null)
            {
                capsula.enabled = false;
            }
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            transform.position += Vector3.up * RadioCapsula;
            Murio?.Invoke(this);
        }

        private void Awake()
        {
            visuales = GetComponentsInChildren<Renderer>(true);
            capsula = GetComponent<CapsuleCollider>();
            if (capsula == null)
            {
                capsula = gameObject.AddComponent<CapsuleCollider>();
                capsula.height = AlturaCapsula;
                capsula.radius = RadioCapsula;
                capsula.center = new Vector3(0f, AlturaCapsula / 2f, 0f);
            }
        }

        private void RestablecerSiMuerto()
        {
            if (!Muerto)
            {
                return;
            }
            Muerto = false;
            vida = config != null ? config.VidaPeaton : vida;
            if (capsula != null)
            {
                capsula.enabled = true;
            }
            foreach (Renderer r in visuales)
            {
                r.enabled = true;
            }
            transform.rotation = Quaternion.identity;
        }

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
            vida = numeros.VidaPeaton;
            calculo.Avanzar(segundosPrevios);
            Colocar();
        }

        private void OnEnable()
        {
            EventosRuido.Disparo += AlOirDisparo;
            ActivosLista.Add(this);
            RestablecerSiMuerto();
        }

        private void OnDisable()
        {
            EventosRuido.Disparo -= AlOirDisparo;
            TerminarLlamada();
            ActivosLista.Remove(this);
            Calmar();
        }

        /// <summary>Un disparo en <paramref name="posicion"/>: si está dentro del radio de oído, el peatón huye.</summary>
        public void AlOirDisparo(Vector3 posicion)
        {
            if (calculo == null || config == null || !isActiveAndEnabled || Llamando || Muerto)
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
            MantenerTelefono();
            if (calculo == null)
            {
                return;
            }
            if (Muerto)
            {
                segundosMuerto += Time.deltaTime;
                if (segundosMuerto >= SegundosCuerpo)
                {
                    foreach (Renderer r in visuales)
                    {
                        r.enabled = false;
                    }
                }
                return;
            }
            if (Llamando)
            {
                Detenido = true;
                Vector3? aQuien = posicionJugador?.Invoke();
                if (aQuien.HasValue)
                {
                    Mirar(aQuien.Value - transform.position);
                }
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
