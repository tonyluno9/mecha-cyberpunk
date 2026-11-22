using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: conducta del guardia pesado de Nova (E12). Lleva el turno del <see cref="PandilleroAPie"/> y traduce <see cref="ReglaGuardiaPesado"/> a
    /// movimiento y daño. De guardia en su puesto mirando a la calle; solo pelea si lo provocan (le pegan, o <see cref="Provocar"/>, que usará el recinto del
    /// jefe final). Gira a 90 grados por segundo y solo camina hacia adelante, así que rodearlo funciona. Si el jugador se aleja más de
    /// <see cref="DistanciaVuelta"/> o deja de ser atacable, vuelve a su puesto. Al romperse su <see cref="EscudoFrontal"/> queda aturdido y castigable.
    /// T-1553 (solo el E16, con las casillas del inspector que prende RobotsNovaT1553.Aplicar): fase de disparo del cañón (<see cref="ReglaCanonE16"/>: se detiene, gira hacia
    /// el jugador, avisa, juega E16_Disparar_Canon y dispara con el brazo extendido; nunca se mueve mientras dispara) y caminar con el escudo arriba
    /// (<see cref="ReglaEscudoArriba"/>: al acercarse o al recibir disparos).
    /// </summary>
    [RequireComponent(typeof(EscudoFrontal))]
    [DefaultExecutionOrder(-100)]
    public class ConductaGuardiaPesado : MonoBehaviour, IConductaAPie
    {
        public const float DistanciaVuelta = 30f;
        private const float DistanciaPuesto = 0.5f;
        private const float DistanciaMinima = 1.4f;
        private const float AnguloCaminar = 90f;
        private static readonly Color ColorAvisoEmpujon = new Color(1f, 0.6f, 0.1f);
        private static readonly Color ColorAvisoMazazo = new Color(1f, 0.15f, 0.1f);

        private PandilleroAPie pandillero;
        private EscudoFrontal escudo;
        private ReglaGuardiaPesado regla;
        private Vector3 puesto;
        private Vector3 miraPuesto;
        private bool puestoFijado;
        private bool provocado;
        private float mantenerHasta = -1f;
        private const float SegundosMantenerPorDisparo = 60f;
        private const float CercaniaDisparoJugador = 3f;

        // T-1553: cañón del E16. La boca está a unos 2.6 m del piso en un robot de 3.5 m; el pecho del jugador a 1.3 m.
        private const float AlturaBocaCanon = 2.6f;
        private const float AlturaPechoObjetivo = 1.3f;
        private const float MargenLineaDisparo = 0.3f;
        private const int MaxImpactosLinea = 16;
        private static readonly Color ColorAvisoCanon = new Color(0.2f, 0.7f, 1f);

        [Header("Robot E16 (T-1553)")]
        [Tooltip("True en el E16: se detiene a disparar el cañón cuando el jugador está en rango (ReglaCanonE16).")]
        [SerializeField] private bool disparaCanon;
        [Tooltip("True en el E16: camina con el escudo arriba (E16_CaminarEscudo) al acercarse o al recibir disparos (ReglaEscudoArriba).")]
        [SerializeField] private bool caminaConEscudo;

        private ReglaCanonE16 canon;
        private ReglaEscudoArriba escudoArriba;
        private readonly RaycastHit[] impactosLinea = new RaycastHit[MaxImpactosLinea];
        private int disparosCanonHechos;
        private int disparosCanonConectados;

        public bool Controla => true;
        public ReglaGuardiaPesado Regla => regla;
        public EscudoFrontal Escudo => escudo;
        public bool Provocado => provocado;
        public Vector3 Puesto => puesto;

        /// <summary>T-1553: la regla del cañón (null si este guardia no dispara).</summary>
        public ReglaCanonE16 Canon => disparaCanon ? canon : null;
        /// <summary>T-1553: la regla del escudo arriba (null si este guardia no camina con escudo).</summary>
        public ReglaEscudoArriba EscudoArribaRegla => caminaConEscudo ? escudoArriba : null;
        public bool DisparaCanon => disparaCanon;
        public bool CaminaConEscudo => caminaConEscudo;
        public int DisparosCanon => disparosCanonHechos;
        public int DisparosCanonConectados => disparosCanonConectados;

        /// <summary>T-1553: para los creadores y las pruebas: enciende el cañón y el escudo (el E16 los trae del prefab).</summary>
        public void ConfigurarRobot(bool conCanon, bool conEscudo)
        {
            disparaCanon = conCanon;
            caminaConEscudo = conEscudo;
        }

        private void Awake()
        {
            pandillero = GetComponent<PandilleroAPie>();
            escudo = GetComponent<EscudoFrontal>();
            if (pandillero == null || escudo == null)
            {
                Debug.LogError("[IA] Faltan PandilleroAPie o EscudoFrontal en ConductaGuardiaPesado de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaGuardiaPesado(ReglaGuardiaPesado.Parametros.Diseno);
            canon = new ReglaCanonE16(ReglaCanonE16.Parametros.Diseno);
            escudoArriba = new ReglaEscudoArriba(ReglaEscudoArriba.Parametros.Diseno);
        }

        private void OnEnable()
        {
            if (escudo != null)
            {
                escudo.Roto += AlRomperse;
                escudo.Golpeado += ProvocarPorDisparo;
            }
            if (pandillero != null)
            {
                pandillero.DanioRecibido += AlRecibirDanio;
            }
            EventosRuido.Disparo += AlOirDisparo;
        }

        private void OnDisable()
        {
            if (escudo != null)
            {
                escudo.Roto -= AlRomperse;
                escudo.Golpeado -= ProvocarPorDisparo;
            }
            if (pandillero != null)
            {
                pandillero.DanioRecibido -= AlRecibirDanio;
            }
            EventosRuido.Disparo -= AlOirDisparo;
        }

        /// <summary>
        /// T-1494: un impacto (o un disparo del jugador oído) desde cualquier distancia: pelea y va por el jugador; no vuelve a su puesto por la
        /// distancia hasta pasados <see cref="SegundosMantenerPorDisparo"/> s (sí si el jugador deja de ser atacable).
        /// </summary>
        public void ProvocarPorDisparo()
        {
            escudoArriba?.RegistrarImpacto(); // T-1553: el E16 se cubre con el escudo al recibir disparos
            provocado = true;
            mantenerHasta = Time.time + SegundosMantenerPorDisparo;
        }

        private void AlRecibirDanio(PandilleroAPie p, InfoDanio info)
        {
            ProvocarPorDisparo();
            // T-1561: un golpe fuerte lo hace tambalear (GestoTipo BloquearTambaleo = 19; el estado E16_Bloquear_Tambaleo lo agrega T-1560). Solo el E16: el E15 (ConductaEliteNova) no bloquea.
            if (p != null && p.Visual != null && !p.EstaKO && ReglasTransicionesT1561.GolpeFuerteParaRobot(info.Cantidad, p.Maxima))
            {
                p.Visual.BloquearGolpeFuerte();
            }
        }

        private void AlOirDisparo(Vector3 donde)
        {
            if (pandillero == null || pandillero.EstaKO || pandillero.Jugador == null)
            {
                return;
            }
            Vector3 jugador = pandillero.Jugador.position;
            float aDisparo = new Vector2(donde.x - jugador.x, donde.z - jugador.z).magnitude;
            float aMi = new Vector2(donde.x - transform.position.x, donde.z - transform.position.z).magnitude;
            if (aDisparo <= CercaniaDisparoJugador && aMi <= pandillero.Datos.RadioOido)
            {
                ProvocarPorDisparo();
            }
        }

        private void Start()
        {
            if (!puestoFijado)
            {
                FijarPuesto(transform.position, transform.forward);
            }
        }

        /// <summary>Pone el puesto de guardia (donde espera y hacia dónde mira).</summary>
        public void FijarPuesto(Vector3 punto, Vector3 mira)
        {
            puesto = punto;
            mira.y = 0f;
            miraPuesto = mira.sqrMagnitude < 0.0001f ? Vector3.forward : mira.normalized;
            puestoFijado = true;
        }

        /// <summary>Lo provocan: pelea contra el jugador hasta que se aleje más de <see cref="DistanciaVuelta"/>.</summary>
        public void Provocar()
        {
            provocado = true;
        }

        /// <summary>Lo deja como al inicio: vida y escudo llenos, en su puesto y de guardia (lo apagado se prende).</summary>
        public void Reiniciar()
        {
            provocado = false;
            mantenerHasta = -1f;
            regla?.Reiniciar();
            canon?.Reiniciar();
            escudoArriba?.Reiniciar();
            PonerEscudoVisual(false);
            escudo?.Reiniciar();
            gameObject.SetActive(true);
            if (pandillero != null)
            {
                pandillero.SetCastigable(false);
                pandillero.Reaparecer();
            }
        }

        /// <summary>Deja de pelear y vuelve a su puesto.</summary>
        public void VolverAlPuesto()
        {
            provocado = false;
        }

        private void AlRomperse()
        {
            regla?.Aturdir();
            canon?.Cancelar(); // T-1553: aturdido no dispara
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (regla == null)
            {
                return;
            }
            if (!puestoFijado)
            {
                FijarPuesto(transform.position, transform.forward);
            }
            Transform jugador = p.Jugador;
            bool atacable = jugador != null && p.JugadorAtacable;
            Vector3 haciaJugador = atacable ? jugador.position - p.transform.position : Vector3.zero;
            haciaJugador.y = 0f;
            float distancia = atacable ? haciaJugador.magnitude : float.MaxValue;
            if (provocado && (!atacable || (distancia > DistanciaVuelta && Time.time >= mantenerHasta)))
            {
                provocado = false;
            }
            bool enPelea = provocado && atacable;
            if (disparaCanon && TurnoCanon(p, dt, enPelea, distancia, haciaJugador))
            {
                return; // T-1553: mientras avisa y dispara el cañón no camina ni golpea
            }
            ReglaGuardiaPesado.Fase antes = regla.Actual;
            ReglaGuardiaPesado.Salida salida = regla.Avanzar(dt, new ReglaGuardiaPesado.Entrada { Distancia = distancia, EnPelea = enPelea });
            p.SetCastigable(regla.Castigable);
            Vector3 dir = distancia > 0.0001f && distancia < float.MaxValue ? haciaJugador / distancia : p.transform.forward;
            float giro = regla.Config.GradosGiro;
            switch (regla.Actual)
            {
                case ReglaGuardiaPesado.Fase.Guardia:
                    p.MostrarAviso(0f, ColorAvisoEmpujon);
                    VolverAPuesto(p);
                    break;
                case ReglaGuardiaPesado.Fase.Persigue:
                    p.MostrarAviso(0f, ColorAvisoEmpujon);
                    p.MirarHacia(dir, giro);
                    Vector3 frente = p.transform.forward;
                    frente.y = 0f;
                    bool puedeCaminar = Vector3.Angle(frente, dir) < AnguloCaminar && distancia > DistanciaMinima;
                    p.MoverPlano(puedeCaminar ? frente.normalized * p.Datos.Velocidad : Vector3.zero);
                    break;
                case ReglaGuardiaPesado.Fase.AvisoEmpujon:
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(dir, giro);
                    p.MostrarAviso(regla.ProgresoAviso, ColorAvisoEmpujon);
                    break;
                case ReglaGuardiaPesado.Fase.AvisoMazazo:
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(dir, giro);
                    p.MostrarAviso(regla.ProgresoAviso, ColorAvisoMazazo);
                    break;
                default: // Recupera, Aturdido
                    p.MoverPlano(Vector3.zero);
                    p.MostrarAviso(0f, ColorAvisoEmpujon);
                    break;
            }
            if (caminaConEscudo)
            {
                bool bloqueado = regla.Actual != ReglaGuardiaPesado.Fase.Persigue && regla.Actual != ReglaGuardiaPesado.Fase.Guardia;
                PonerEscudoVisual(escudoArriba.Avanzar(dt, distancia, enPelea, bloqueado) && regla.Actual == ReglaGuardiaPesado.Fase.Persigue);
            }
            if (salida.Impacto)
            {
                Golpear(p, salida.Tipo, dir, distancia);
            }
            else if (salida.EmpezoAviso && antes != regla.Actual)
            {
                // el aviso ya está visible con el brillo; nada más que hacer este cuadro
            }
        }

        private void PonerEscudoVisual(bool arriba)
        {
            VisualPersonaje visual = pandillero != null ? pandillero.Visual : null;
            if (visual != null && (arriba || visual.EscudoArriba))
            {
                visual.EscudoArriba = arriba;
            }
        }

        /// <summary>
        /// T-1553: un cuadro de la fase del cañón. Devuelve true si el cañón lleva el turno (aviso o disparo): el robot queda quieto (MoverPlano cero), gira hacia el jugador
        /// solo durante el aviso (la mira se fija al empezar el clip, así que moverse de lado esquiva el tiro) y el escudo baja.
        /// </summary>
        private bool TurnoCanon(PandilleroAPie p, float dt, bool enPelea, float distancia, Vector3 haciaJugador)
        {
            if (canon.Bloqueando && (!enPelea || regla.Actual == ReglaGuardiaPesado.Fase.Aturdido))
            {
                canon.Cancelar();
                p.MostrarAviso(0f, ColorAvisoCanon);
            }
            Vector3 dir = distancia > 0.0001f && distancia < float.MaxValue ? haciaJugador / distancia : p.transform.forward;
            var entrada = new ReglaCanonE16.Entrada { Distancia = distancia, EnPelea = enPelea, PuedeIniciar = regla.Actual == ReglaGuardiaPesado.Fase.Persigue };
            ReglaCanonE16.Salida salida = canon.Avanzar(dt, entrada);
            if (!canon.Bloqueando && !salida.Disparo)
            {
                return false;
            }
            p.MoverPlano(Vector3.zero);
            PonerEscudoVisual(false);
            if (canon.Actual == ReglaCanonE16.Fase.Aviso)
            {
                p.MirarHacia(dir, regla.Config.GradosGiro);
                p.MostrarAviso(canon.ProgresoAviso, ColorAvisoCanon);
            }
            else
            {
                p.MostrarAviso(0f, ColorAvisoCanon);
            }
            if (salida.EmpezoClip && p.Visual != null)
            {
                // El estado PistolaDisparo del controlador solo se entra con TipoArma = Pistola (Any State con trigger Disparar + TipoArma 1): sin esto E16_Disparar_Canon no juega.
                p.Visual.TipoArma = TipoArmaVisual.Pistola;
                p.Visual.Disparar();
            }
            if (salida.Disparo)
            {
                DispararCanon(p, dir, distancia);
            }
            return canon.Bloqueando;
        }

        /// <summary>T-1553: el tiro del cañón: ruido, y daño si el jugador sigue en la mira (<see cref="ReglaCanonE16.Parametros.ToleranciaGrados"/>) y no hay un muro en medio.</summary>
        private void DispararCanon(PandilleroAPie p, Vector3 haciaJugador, float distancia)
        {
            ReglaCanonE16.Parametros c = canon.Config;
            Vector3 origen = p.transform.position + Vector3.up * AlturaBocaCanon;
            disparosCanonHechos++;
            EventosRuido.PublicarDisparoConRadio(origen, 40f); // T-1622: el cañón se oye como un rifle
            EventosRuido.PublicarDisparo(origen);
            Vector3 frente = p.transform.forward;
            frente.y = 0f;
            if (distancia > c.DistanciaMaxima || Vector3.Angle(frente, haciaJugador) > c.ToleranciaGrados || p.Jugador == null)
            {
                return;
            }
            Vector3 pecho = p.Jugador.position + Vector3.up * AlturaPechoObjetivo;
            if (!LineaLibre(origen, pecho, p.Jugador))
            {
                return;
            }
            disparosCanonConectados++;
            p.DaniarJugador(c.Danio, (pecho - origen).normalized);
        }

        // Un muro (o cualquier cosa que no sea el propio robot ni el jugador) entre la boca y el pecho bloquea el tiro.
        private bool LineaLibre(Vector3 origen, Vector3 destino, Transform jugador)
        {
            Vector3 hacia = destino - origen;
            float largo = hacia.magnitude;
            if (largo < 0.01f)
            {
                return true;
            }
            int n = Physics.RaycastNonAlloc(origen, hacia / largo, impactosLinea, largo, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform raiz = impactosLinea[i].collider.transform.root;
                if (raiz == transform.root || raiz == jugador.root)
                {
                    continue;
                }
                if (impactosLinea[i].distance < largo - MargenLineaDisparo)
                {
                    return false;
                }
            }
            return true;
        }

        private void Golpear(PandilleroAPie p, ReglaGuardiaPesado.Ataque tipo, Vector3 haciaJugador, float distancia)
        {
            ReglaGuardiaPesado.Parametros c = regla.Config;
            bool mazazo = tipo == ReglaGuardiaPesado.Ataque.Mazazo;
            p.AnimarGolpe(mazazo);
            float radio = mazazo ? c.RadioMazazo : c.RadioEmpujon;
            float arco = mazazo ? c.ArcoMazazo : c.ArcoEmpujon;
            if (distancia > radio)
            {
                return;
            }
            Vector3 frente = p.transform.forward;
            frente.y = 0f;
            if (Vector3.Angle(frente, haciaJugador) <= arco * 0.5f)
            {
                p.DaniarJugador(mazazo ? c.DanioMazazo : c.DanioEmpujon, haciaJugador);
            }
        }

        private void VolverAPuesto(PandilleroAPie p)
        {
            Vector3 aPuesto = puesto - p.transform.position;
            aPuesto.y = 0f;
            if (aPuesto.magnitude > DistanciaPuesto)
            {
                p.MirarHacia(aPuesto, regla.Config.GradosGiro * 2f);
                p.MoverPlano(aPuesto.normalized * p.Datos.Velocidad);
            }
            else
            {
                p.MirarHacia(miraPuesto, regla.Config.GradosGiro);
                p.MoverPlano(Vector3.zero);
            }
        }
    }
}
