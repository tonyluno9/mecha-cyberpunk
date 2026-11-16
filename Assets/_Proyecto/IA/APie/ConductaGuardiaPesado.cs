using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: conducta del guardia pesado de Nova (E12). Lleva el turno del <see cref="PandilleroAPie"/> y traduce <see cref="ReglaGuardiaPesado"/> a
    /// movimiento y daño. De guardia en su puesto mirando a la calle; solo pelea si lo provocan (le pegan, o <see cref="Provocar"/>, que usará el recinto del
    /// jefe final). Gira a 90 grados por segundo y solo camina hacia adelante, así que rodearlo funciona. Si el jugador se aleja más de
    /// <see cref="DistanciaVuelta"/> o deja de ser atacable, vuelve a su puesto. Al romperse su <see cref="EscudoFrontal"/> queda aturdido y castigable.
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

        public bool Controla => true;
        public ReglaGuardiaPesado Regla => regla;
        public EscudoFrontal Escudo => escudo;
        public bool Provocado => provocado;
        public Vector3 Puesto => puesto;

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
        }

        private void OnEnable()
        {
            if (escudo != null)
            {
                escudo.Roto += AlRomperse;
                escudo.Golpeado += Provocar;
            }
        }

        private void OnDisable()
        {
            if (escudo != null)
            {
                escudo.Roto -= AlRomperse;
                escudo.Golpeado -= Provocar;
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

        /// <summary>Deja de pelear y vuelve a su puesto.</summary>
        public void VolverAlPuesto()
        {
            provocado = false;
        }

        private void AlRomperse()
        {
            regla?.Aturdir();
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
            if (provocado && (!atacable || distancia > DistanciaVuelta))
            {
                provocado = false;
            }
            bool enPelea = provocado && atacable;
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
            if (salida.Impacto)
            {
                Golpear(p, salida.Tipo, dir, distancia);
            }
            else if (salida.EmpezoAviso && antes != regla.Actual)
            {
                // el aviso ya está visible con el brillo; nada más que hacer este cuadro
            }
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
