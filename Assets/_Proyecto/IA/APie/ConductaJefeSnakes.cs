using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1108: conducta del jefe SNAKES (E11). Lleva el turno del <see cref="PandilleroAPie"/> y traduce <see cref="ReglaJefeSnakes"/> a movimiento y daño:
    /// acecha de lado, tajos en arco de 90 grados, estocada en línea recta fijada al empezar su aviso (si el jugador se aparta, falla) y, al 50% de
    /// vida, llama a los refuerzos. Pelea solo cuando el jugador está a menos de <see cref="RadioAviso"/> (y lo suelta a más de <see cref="RadioSuelta"/>).
    /// Al vencerlo registra el jefe vencido de la pandilla Viboras, como <see cref="JefePandilla"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ConductaJefeSnakes : MonoBehaviour, IConductaAPie
    {
        public const float RadioAviso = 25f;
        public const float RadioSuelta = 40f;
        public const float DanioTajo = 10f;
        public const float ArcoTajo = 90f;
        public const float DanioEstocada = 22f;
        public const float RadioEstocada = 1.2f;
        public const float VelocidadEstocada = 12f;
        private const float GradosGiro = 540f;
        private const float FactorStrafe = 0.8f;
        private const float FactorRadial = 0.6f;
        private const float DistanciaCasa = 1f;
        private static readonly Color ColorAvisoTajos = new Color(0.45f, 1f, 0.1f);
        private static readonly Color ColorAvisoEstocada = new Color(0.1f, 1f, 0.35f);

        [Tooltip("Los SNAKES de refuerzo: un grupo apagado que se activa una vez al 50% de vida. Puede quedar vacío.")]
        [SerializeField] private PandillaAPie refuerzos;
        [Tooltip("Los 2 escoltas SNAKES. Quedan siempre (de día también), no solo la fracción de día de su pandilla. Puede quedar vacío.")]
        [SerializeField] private PandillaAPie escoltas;

        private PandilleroAPie pandillero;
        private ReglaJefeSnakes regla;
        private bool enganchado;
        private Vector3 direccionEstocada;
        private bool estocadaConecto;
        private bool vencidoRegistrado;

        public bool Controla => true;
        public ReglaJefeSnakes Regla => regla;
        public PandillaAPie Refuerzos => refuerzos;
        public PandillaAPie Escoltas => escoltas;
        /// <summary>True mientras pelea contra el jugador.</summary>
        public bool Enganchado => enganchado;

        private void Awake()
        {
            pandillero = GetComponent<PandilleroAPie>();
            if (pandillero == null)
            {
                Debug.LogError("[IA] Falta el PandilleroAPie en ConductaJefeSnakes de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaJefeSnakes(ReglaJefeSnakes.Parametros.Diseno, () => Random.value);
            if (escoltas != null)
            {
                // El orden de ejecución deja esto antes del OnEnable de la pandilla: los dos escoltas están siempre, de día y de noche.
                escoltas.ModoMision = true;
            }
            if (refuerzos != null)
            {
                refuerzos.ModoMision = true; // al llamarlos salen los dos aunque sea de día
            }
            pandillero.MarcarComoJefe();
            pandillero.Vencido += AlVencer;
        }

        /// <summary>Para pruebas: cambia el azar de la regla (antes del primer Update).</summary>
        public void InyectarAzar(System.Func<float> azar)
        {
            regla = new ReglaJefeSnakes(ReglaJefeSnakes.Parametros.Diseno, azar);
        }

        private void OnDestroy()
        {
            if (pandillero != null)
            {
                pandillero.Vencido -= AlVencer;
            }
        }

        private void AlVencer(PandilleroAPie vencido)
        {
            if (vencidoRegistrado)
            {
                return;
            }
            vencidoRegistrado = true;
            Registro<ITerritorio>.Actual?.RegistrarVencido(vencido.transform.position, IdPandilla.Viboras, true);
            vencido.Gritar("¡Ssss... esto no ha terminado!");
            Debug.Log("[IA] Jefe SNAKE vencido.");
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (regla == null)
            {
                return;
            }
            Transform jugador = p.Jugador;
            bool atacable = jugador != null && p.JugadorAtacable;
            Vector3 haciaJugador = atacable ? jugador.position - p.transform.position : Vector3.zero;
            haciaJugador.y = 0f;
            float distancia = atacable ? haciaJugador.magnitude : float.MaxValue;
            if (!enganchado && distancia <= RadioAviso)
            {
                enganchado = true;
            }
            else if (enganchado && distancia > RadioSuelta)
            {
                enganchado = false;
            }
            bool enPelea = enganchado && atacable;
            ReglaJefeSnakes.Salida salida = regla.Avanzar(dt, new ReglaJefeSnakes.Entrada { Distancia = distancia, VidaFraccion = p.VidaFraccion, EnPelea = enPelea });
            if (salida.LlamarRefuerzos)
            {
                LlamarRefuerzos(p);
            }
            if (!enPelea)
            {
                p.MostrarAviso(0f, ColorAvisoTajos);
                p.SetCastigable(false);
                VolverACasa(p);
                return;
            }

            Vector3 dir = distancia > 0.0001f ? haciaJugador / distancia : p.transform.forward;
            p.SetCastigable(regla.Castigable);
            switch (regla.Actual)
            {
                case ReglaJefeSnakes.Fase.Acecho:
                    p.MostrarAviso(0f, ColorAvisoTajos);
                    p.MirarHacia(dir, GradosGiro);
                    p.MoverPlano(VelocidadAcecho(p, dir, distancia));
                    break;
                case ReglaJefeSnakes.Fase.AvisoTajos:
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(dir, GradosGiro);
                    p.MostrarAviso(regla.ProgresoAviso, ColorAvisoTajos);
                    break;
                case ReglaJefeSnakes.Fase.Tajos:
                case ReglaJefeSnakes.Fase.RecuperaTajos:
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(dir, GradosGiro);
                    p.MostrarAviso(0f, ColorAvisoTajos);
                    break;
                case ReglaJefeSnakes.Fase.AvisoEstocada:
                    if (salida.EmpezoAvisoEstocada)
                    {
                        direccionEstocada = dir;
                        p.Gritar("¡Sssss!");
                    }
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(direccionEstocada, GradosGiro);
                    p.MostrarAviso(regla.ProgresoAviso, ColorAvisoEstocada);
                    break;
                case ReglaJefeSnakes.Fase.Estocada:
                    p.MostrarAviso(0f, ColorAvisoEstocada);
                    if (salida.EmpezoEstocada)
                    {
                        estocadaConecto = false;
                        p.AnimarGolpe(true);
                    }
                    p.MirarHacia(direccionEstocada, GradosGiro);
                    p.MoverPlano(direccionEstocada * VelocidadEstocada);
                    if (!estocadaConecto && distancia <= RadioEstocada)
                    {
                        estocadaConecto = true;
                        p.DaniarJugador(DanioEstocada, direccionEstocada);
                    }
                    break;
                default: // RecuperaEstocada: quieto y castigable
                    p.MoverPlano(Vector3.zero);
                    p.MostrarAviso(0f, ColorAvisoTajos);
                    break;
            }
            if (salida.Tajo)
            {
                Tajar(p, dir, distancia);
            }
        }

        // Rodea al jugador de lado y corrige el radio hacia el que pide la regla.
        private Vector3 VelocidadAcecho(PandilleroAPie p, Vector3 haciaJugador, float distancia)
        {
            Vector3 radial = -haciaJugador; // del jugador hacia el jefe
            Vector3 tangente = Vector3.Cross(Vector3.up, radial) * regla.Sentido;
            float error = Mathf.Clamp(regla.RadioDeseado(distancia) - distancia, -1f, 1f);
            Vector3 velocidad = tangente * FactorStrafe + radial * (error * FactorRadial);
            return velocidad.sqrMagnitude > 1f ? velocidad.normalized * p.Datos.Velocidad : velocidad * p.Datos.Velocidad;
        }

        private void Tajar(PandilleroAPie p, Vector3 haciaJugador, float distancia)
        {
            p.AnimarGolpe(false);
            if (distancia > ReglaJefeSnakes.Parametros.Diseno.AlcanceTajos)
            {
                return;
            }
            Vector3 frente = p.transform.forward;
            frente.y = 0f;
            if (Vector3.Angle(frente, haciaJugador) <= ArcoTajo * 0.5f)
            {
                p.DaniarJugador(DanioTajo, haciaJugador);
            }
        }

        private void VolverACasa(PandilleroAPie p)
        {
            Vector3 aCasa = p.Hogar - p.transform.position;
            aCasa.y = 0f;
            if (aCasa.magnitude > DistanciaCasa)
            {
                p.MirarHacia(aCasa, GradosGiro);
                p.MoverPlano(aCasa.normalized * p.Datos.Velocidad * FactorStrafe);
            }
            else
            {
                p.MoverPlano(Vector3.zero);
            }
        }

        private void LlamarRefuerzos(PandilleroAPie p)
        {
            p.Gritar("¡A mí, víboras!");
            if (refuerzos == null)
            {
                return;
            }
            if (!refuerzos.gameObject.activeSelf)
            {
                refuerzos.gameObject.SetActive(true);
            }
            PandilleroAPie voz = null;
            foreach (PandilleroAPie r in refuerzos.Pandilleros)
            {
                if (r != null && r.isActiveAndEnabled && !r.EstaKO)
                {
                    r.Alerta = true;
                    voz = voz != null ? voz : r;
                }
            }
            if (voz != null)
            {
                refuerzos.Avisar(voz, "¡Ya vamos, jefe!", true);
            }
        }
    }
}
