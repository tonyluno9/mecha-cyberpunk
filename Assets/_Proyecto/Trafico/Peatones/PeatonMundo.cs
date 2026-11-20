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

        // Cuerpo del peatón al morir (T-663).
        private const float AlturaCapsula = 1.8f;
        private const float RadioCapsula = 0.3f;
        private const float SegundosCuerpo = 10f;
        private const float SegundosCaida = 0.5f;
        private Quaternion rotacionInicialCaida;
        private Quaternion rotacionTirado;
        private float progresoCaida = 1f;
        private ReglaCivil regla;
        private float vida;
        private float segundosMuerto;
        private CapsuleCollider capsula;
        private Renderer[] visuales;

        /// <summary>True si ya lo mataron: queda tirado un rato y no hace nada.</summary>
        public bool Muerto { get; private set; }

        /// <summary>Se avisa al morir un peatón.</summary>
        public static event Action<PeatonMundo> Murio;

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (Muerto || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            float antes = vida;
            vida -= info.Cantidad;
            bool mato = vida <= 0f;
            Vector3? jugador = posicionJugador?.Invoke();
            if (mato)
            {
                Matar(info.Direccion);
                // T-1305: matar a un civil suma calor (con testigos, lo decide el decorador de calor) si lo hizo el jugador.
                if (config != null && ReglaCivil.EsAgresionDelJugador(info.Tipo, info.Punto, info.Direccion, jugador, info.Atacante != null))
                {
                    Registro<ICalor>.Actual?.Sumar(config.CalorPorMuerte, MotivoCalor.Pelea);
                }
            }
            else
            {
                // T-1495: reacción de daño en el visual (la recibe el puente de IA, si está) y luego huye o pelea.
                SendMessage(MensajeHerido, info.Direccion, SendMessageOptions.DontRequireReceiver);
                ReaccionarAlGolpe(info, jugador);
            }
            return new ResultadoDanio(Mathf.Min(antes, info.Cantidad), TipoBloqueo.Ninguno, mato);
        }

        // T-1305: herido sin morir, o pelea de vuelta (si el jugador está al alcance) o huye.
        private void ReaccionarAlGolpe(InfoDanio info, Vector3? jugador)
        {
            if (regla == null || config == null)
            {
                return;
            }
            float? distancia = jugador.HasValue ? DistanciaPlano(jugador.Value, transform.position) : (float?)null;
            if (regla.Herido(distancia) == EstadoCivil.Huyendo)
            {
                Vector3 origen = jugador.HasValue ? jugador.Value : info.Punto - info.Direccion;
                IniciarHuida(origen);
            }
        }

        // T-1305: en revancha se queda quieto mirando al jugador y le pega con golpes chicos; al terminar huye.
        private void AtenderRevancha()
        {
            Detenido = true;
            Vector3? jugador = posicionJugador?.Invoke();
            float? distancia = jugador.HasValue ? DistanciaPlano(jugador.Value, transform.position) : (float?)null;
            if (jugador.HasValue)
            {
                Mirar(jugador.Value - transform.position);
            }
            if (regla.Avanzar(Time.deltaTime, distancia))
            {
                GolpearAlJugador(jugador.Value);
            }
            if (regla.Estado == EstadoCivil.Huyendo)
            {
                IniciarHuida(jugador ?? transform.position - transform.forward);
            }
        }

        private void GolpearAlJugador(Vector3 posJugador)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            IRecibeDanio objetivo = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInParent<IRecibeDanio>() : null;
            if (objetivo == null)
            {
                return;
            }
            Vector3 direccion = posJugador - transform.position;
            direccion.y = 0f;
            direccion = direccion.sqrMagnitude > DesplazamientoMinimoParaDecidir ? direccion.normalized : transform.forward;
            Vector3 punto = transform.position + Vector3.up * AlturaPecho + direccion * RadioCuerpo;
            objetivo.RecibirDanio(new InfoDanio(config.DanioRevancha, TipoDanio.Golpe, punto, direccion, null));
        }

        private void IniciarHuida(Vector3 origen)
        {
            DejarDePlaticar();
            if (agachadoRestante > 0f)
            {
                agachadoRestante = 0f;
                SendMessage(MensajeLevantar, SendMessageOptions.DontRequireReceiver);
            }
            origenDelRuido = origen;
            huidaRestante = config.SegundosHuida;
            sentidoDecidido = false;
            calculo.MultiplicadorVelocidad = config.MultiplicadorHuida;
            calculo.CancelarEspera();
            SendMessage(MensajeAsustado, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>Lo mata: cae, queda tirado <see cref="SegundosCuerpo"/> s y se esconde. Cancela cualquier llamada.</summary>
        public void Matar() => Matar(null);

        /// <summary>T-1495: igual, con la dirección del golpe (decide si cae de frente, de espalda o desmayado).</summary>
        public void Matar(Vector3? direccionGolpe)
        {
            if (Muerto)
            {
                return;
            }
            Muerto = true;
            platicando = false;
            agachadoRestante = 0f;
            Calmar();
            segundosMuerto = 0f;
            if (capsula != null)
            {
                capsula.enabled = false;
            }
            // T-1495: con un visual que sabe morir (parámetro Caer) cae con su animación; si no, tirado al piso como antes.
            if (TieneMuerteAnimada())
            {
                progresoCaida = 1f;
                SendMessage(MensajeMuerte, direccionGolpe ?? -transform.forward, SendMessageOptions.DontRequireReceiver);
                Murio?.Invoke(this);
                return;
            }
            // T-1305: cae tirado al piso (90°) en SegundosCaida; la rotación se interpola en Update.
            rotacionInicialCaida = transform.rotation;
            rotacionTirado = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            progresoCaida = 0f;
            if (calculo == null)
            {
                transform.rotation = rotacionTirado; // sin preparar no corre Update: cae de golpe
                progresoCaida = 1f;
            }
            transform.position += Vector3.up * RadioCapsula;
            Murio?.Invoke(this);
        }

        /// <summary>Mensajes (SendMessage) que recibe el puente de animación del visual; Trafico no referencia a Personaje.</summary>
        public const string MensajeHerido = "AlHerirCivil";
        public const string MensajeMuerte = "AlMorirCivil";
        public const string MensajeRevivir = "AlRevivirCivil";
        /// <summary>T-1506: empieza a huir (el puente juega NPC_Asustarse una vez).</summary>
        public const string MensajeAsustado = "AlAsustarCivil";
        /// <summary>T-1516: se agacha cubriéndose la cabeza (el puente juega NPC_Agachado_CubriendoCabeza en bucle) y se levanta.</summary>
        public const string MensajeAgachar = "AlAgacharCivil";
        public const string MensajeLevantar = "AlLevantarseCivil";
        /// <summary>T-1516: platica (parámetro: 0 o 1, qué clip de plática) y deja de platicar. Se repite cada <see cref="SegundosEntreAvisos"/> s mientras dura.</summary>
        public const string MensajePlaticar = "AlPlaticarCivil";
        public const string MensajeDejarPlaticar = "AlDejarPlaticarCivil";
        public const float SegundosEntreAvisos = 1f;
        private const float ProbabilidadHuirTrasAgacharse = 0.5f;

        private System.Random azarReaccion;
        private float agachadoRestante;
        private Vector3 origenAgachado;
        private float proximoAviso;
        private bool platicando;
        private int variantePlatica;
        private Vector3 lugarPlatica;
        private Vector3 desvioPlatica;

        /// <summary>T-1516: true mientras está agachado por haber oído un disparo.</summary>
        public bool Agachado => agachadoRestante > 0f;

        /// <summary>T-1516: true mientras se queda platicando en su corro.</summary>
        public bool Platicando => platicando;

        /// <summary>T-1516: la plática que le toca (0 o 1).</summary>
        public int VariantePlatica => variantePlatica;

        /// <summary>
        /// T-1516: lo deja platicando en un corro de <paramref name="tamano"/> personas como el miembro <paramref name="miembro"/>: se queda quieto en su
        /// lugar del corro (<see cref="ReglaPlatica.Lugar"/>) mirando al centro. Un disparo o un golpe lo sacan de ahí y se comporta como los demás.
        /// </summary>
        public void Platicar(int miembro, int tamano)
        {
            platicando = true;
            variantePlatica = miembro % 2;
            lugarPlatica = ReglaPlatica.Lugar(miembro, tamano);
            desvioPlatica = lugarPlatica;
            Colocar();
        }

        private void DejarDePlaticar()
        {
            if (!platicando)
            {
                return;
            }
            platicando = false;
            SendMessage(MensajeDejarPlaticar, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>T-1516: se agacha <see cref="ConfigPeatones.SegundosAgachado"/> s y luego huye o sigue su camino.</summary>
        public void Agacharse(Vector3 origen)
        {
            if (Muerto || config == null)
            {
                return;
            }
            DejarDePlaticar();
            origenAgachado = origen;
            agachadoRestante = config.SegundosAgachado;
            proximoAviso = 0f;
            SendMessage(MensajeAgachar, SendMessageOptions.DontRequireReceiver);
        }

        private void AtenderAgachado()
        {
            Detenido = true;
            agachadoRestante -= Time.deltaTime;
            if (agachadoRestante > 0f)
            {
                proximoAviso -= Time.deltaTime;
                if (proximoAviso <= 0f)
                {
                    proximoAviso = SegundosEntreAvisos;
                    SendMessage(MensajeAgachar, SendMessageOptions.DontRequireReceiver);
                }
                return;
            }
            agachadoRestante = 0f;
            SendMessage(MensajeLevantar, SendMessageOptions.DontRequireReceiver);
            if (azarReaccion != null && azarReaccion.NextDouble() < ProbabilidadHuirTrasAgacharse)
            {
                IniciarHuida(origenAgachado);
            }
            else
            {
                Calmar();
            }
        }

        private void AtenderPlatica()
        {
            Detenido = true;
            Vector3 haciaElCentro = -lugarPlatica;
            Mirar(transform.parent != null ? transform.parent.TransformDirection(haciaElCentro) : haciaElCentro);
            proximoAviso -= Time.deltaTime;
            if (proximoAviso <= 0f)
            {
                proximoAviso = SegundosEntreAvisos;
                SendMessage(MensajePlaticar, variantePlatica, SendMessageOptions.DontRequireReceiver);
            }
        }
        public const string ParametroCaer = "KO"; // ReglasVisual.Caer: el parámetro Bool que tumba al personaje

        /// <summary>True si algún Animator del peatón tiene el parámetro de caer, "KO" (controlador base o propio, con clip de muerte).</summary>
        public bool TieneMuerteAnimada()
        {
            foreach (Animator animador in GetComponentsInChildren<Animator>(false))
            {
                if (animador == null || animador.runtimeAnimatorController == null)
                {
                    continue;
                }
                foreach (AnimatorControllerParameter p in animador.parameters)
                {
                    if (p.name == ParametroCaer && p.type == AnimatorControllerParameterType.Bool)
                    {
                        return true;
                    }
                }
            }
            return false;
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
            desvioPlatica = Vector3.zero;
            visuales = GetComponentsInChildren<Renderer>(true); // T-1409: el visual por zona se crea después del Awake
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
            SendMessage(MensajeRevivir, SendMessageOptions.DontRequireReceiver);
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
            azarReaccion = azar;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
            calculo = numeros.Crear(RutasBanqueta.Largos(ruta), azar);
            vida = numeros.VidaPeaton;
            regla = new ReglaCivil(() => (float)azar.NextDouble(), numeros.ProbabilidadRevancha, numeros.AlcanceRevancha, numeros.GolpesRevancha,
                numeros.SegundosEntreGolpesRevancha, numeros.SegundosMaximosRevancha);
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
            ActivosLista.Remove(this);
            Calmar();
        }

        /// <summary>Un disparo en <paramref name="posicion"/>: si está dentro del radio de oído, el peatón huye.</summary>
        public void AlOirDisparo(Vector3 posicion)
        {
            if (calculo == null || config == null || !isActiveAndEnabled || Muerto)
            {
                return;
            }
            if (DistanciaPlano(posicion, transform.position) > config.RadioOirDisparo)
            {
                return;
            }
            if (regla != null && regla.Estado == EstadoCivil.Revancha)
            {
                return; // T-1305: pelea y no hace caso del ruido
            }
            if (agachadoRestante > 0f)
            {
                agachadoRestante = config.SegundosAgachado; // otro tiro: sigue agachado
                origenAgachado = posicion;
                return;
            }
            // T-1516: una parte de los civiles se agacha cubriéndose antes de huir (si ya corría, sigue corriendo).
            if (huidaRestante <= 0f && azarReaccion != null && config.ProbabilidadAgacharse > 0f && azarReaccion.NextDouble() < config.ProbabilidadAgacharse)
            {
                Agacharse(posicion);
                return;
            }
            IniciarHuida(posicion);
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            if (Muerto)
            {
                if (progresoCaida < 1f)
                {
                    progresoCaida = Mathf.Min(1f, progresoCaida + Time.deltaTime / SegundosCaida);
                    transform.rotation = Quaternion.Slerp(rotacionInicialCaida, rotacionTirado, progresoCaida);
                }
                segundosMuerto += Time.deltaTime;
                if (segundosMuerto >= SegundosCuerpo)
                {
                    visuales = GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer r in visuales)
                    {
                        r.enabled = false;
                    }
                }
                return;
            }
            if (regla != null && regla.Estado == EstadoCivil.Revancha)
            {
                AtenderRevancha();
                return;
            }
            if (agachadoRestante > 0f)
            {
                AtenderAgachado();
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
            if (platicando && !huyendo)
            {
                AtenderPlatica();
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
            regla?.Calmar();
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
            desvioPlatica = Vector3.MoveTowards(desvioPlatica, platicando ? lugarPlatica : Vector3.zero, VelocidadDesvio * Time.deltaTime);
            transform.localPosition = posRuta + desvio + desvioPlatica;
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
