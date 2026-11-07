using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-533: seguridad de Nova en Neón Alto. Patrulla su tramo de banqueta (<see cref="GenteDeBanqueta"/> en modo Caminar), te vigila
    /// (se voltea a verte desde <see cref="radioVigila"/> m sin esperar a que te quedes quieto) y reacciona si sacas un arma cerca o si oye
    /// un disparo: se detiene, mira hacia allá, grita por el megáfono y llama a la policía (sube el calor).
    /// </summary>
    [RequireComponent(typeof(GenteDeBanqueta))]
    public class GuardiaNova : MonoBehaviour
    {
        [Tooltip("Desde aquí te vigila: se voltea a verte (m).")]
        [Min(1f)] [SerializeField] private float radioVigila = 9f;
        [Tooltip("Si sacas un arma a esta distancia o menos, reacciona (m).")]
        [Min(1f)] [SerializeField] private float radioArma = 15f;
        [Tooltip("Si oye un disparo a esta distancia o menos, reacciona (m).")]
        [Min(1f)] [SerializeField] private float radioDisparo = 40f;
        [Tooltip("Calor que suma al reaccionar (puntos; 25 es el nivel 1).")]
        [Min(0f)] [SerializeField] private float calorAlReaccionar = 30f;
        [Tooltip("Tiempo que se queda alerta mirando hacia el ruido (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAlerta = 12f;

        [Header("Presumir y amenazar (T-624)")]
        [Tooltip("Cada cuántos segundos presume (Nova_Flexing) si estás a la vista y no hay alerta (s).")]
        [Min(1f)] [SerializeField] private float segundosEntrePresumir = 25f;
        [Tooltip("En alerta, a esta distancia o menos amenaza con golpes (Nova_Punch y Nova_Barrido) (m).")]
        [Min(0.5f)] [SerializeField] private float radioGolpe = 2.2f;
        [Tooltip("Segundos entre un golpe y el siguiente (s).")]
        [Min(0.2f)] [SerializeField] private float segundosEntreGolpes = 1.5f;
        [Header("Daño de verdad (T-715)")]
        [Tooltip("Segundos de aviso (luz roja y brazo atrás) antes de que el golpe conecte (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAviso = 0.6f;
        [Tooltip("Daño del puñetazo.")]
        [Min(0f)] [SerializeField] private float danioGolpe = 12f;
        [Tooltip("Daño de la barrida (cada tercer golpe).")]
        [Min(0f)] [SerializeField] private float danioBarrida = 20f;
        [Tooltip("El golpe solo conecta si el jugador sigue a esta distancia o menos al llegar el impacto (m): alejarse a tiempo lo esquiva.")]
        [Min(0.5f)] [SerializeField] private float alcanceImpacto = 2.6f;
        [Tooltip("Ángulo frente al guardia donde conecta el puñetazo (grados totales); la barrida abre más.")]
        [Range(30f, 360f)] [SerializeField] private float anguloImpacto = 110f;

        private const float VariacionPresumir = 0.25f;
        private VisualPersonaje visual;
        private float proximoPresumir;
        private int golpesDados;
        private GenteDeBanqueta gente;
        private float finAlerta;
        private float proximaLlamada;
        private ReglaGolpeGuardia reglaGolpe;
        private Light luzAviso;
        private int impactosDados;
        private float danioHecho;

        /// <summary>True mientras está alerta (sacaste un arma o hubo disparos cerca).</summary>
        public bool Alerta => Time.time < finAlerta;
        public float RadioVigila => radioVigila;

        /// <summary>Cuántos golpes de amenaza ha lanzado (cada tercero es la barrida).</summary>
        public int GolpesDados => golpesDados;

        /// <summary>True mientras avisa el golpe (luz roja): todavía no hace daño (T-715).</summary>
        public bool AvisandoGolpe => reglaGolpe != null && reglaGolpe.Actual == ReglaGolpeGuardia.Fase.Aviso;

        /// <summary>Cuántos golpes conectaron al jugador y el daño total que hicieron (T-715).</summary>
        public int Impactos => impactosDados;
        public float DanioHecho => danioHecho;

        /// <summary>La luz roja del aviso (null si todavía no avisó nunca).</summary>
        public Light LuzAviso => luzAviso;

        /// <summary>Para las pruebas y los creadores: tiempos de presumir y de golpear.</summary>
        public void ConfigurarGestos(float entrePresumir, float entreGolpes)
        {
            segundosEntrePresumir = Mathf.Max(1f, entrePresumir);
            segundosEntreGolpes = Mathf.Max(0.2f, entreGolpes);
            reglaGolpe = null;
            proximoPresumir = Time.time + segundosEntrePresumir;
        }

        private void Awake()
        {
            gente = GetComponent<GenteDeBanqueta>();
            gente.ConfigurarMirada(radioVigila, 0f);
            visual = GetComponent<VisualPersonaje>();
            proximoPresumir = Time.time + segundosEntrePresumir * Random.Range(0.2f, 1f);
        }

        private void OnEnable()
        {
            EventosRuido.Disparo += AlOirDisparo;
            EventosRuido.ArmaSacada += AlVerArma;
        }

        private void OnDisable()
        {
            EventosRuido.Disparo -= AlOirDisparo;
            reglaGolpe?.Reiniciar();
            if (luzAviso != null)
            {
                luzAviso.enabled = false;
            }
            EventosRuido.ArmaSacada -= AlVerArma;
            if (gente != null)
            {
                gente.MirarA = null;
            }
        }

        private void AlOirDisparo(Vector3 donde) => Reaccionar(donde, radioDisparo);

        private void AlVerArma(Vector3 donde) => Reaccionar(donde, radioArma);

        private void Reaccionar(Vector3 donde, float radio)
        {
            Vector3 d = donde - transform.position;
            d.y = 0f;
            if (d.magnitude > radio)
            {
                return;
            }
            finAlerta = Time.time + segundosAlerta;
            gente.MirarA = donde;
            if (Time.time >= proximaLlamada)
            {
                // Una sola llamada por alerta (no suma calor en cada disparo de la misma balacera).
                proximaLlamada = finAlerta;
                Sonidos.ReproducirEn("CAL_PATRULLA_MEGAFONO", transform.position);
                Registro<ICalor>.Actual?.Sumar(calorAlReaccionar, MotivoCalor.Otro);
            }
        }

        private void Update()
        {
            if (!Alerta && gente.MirarA.HasValue)
            {
                gente.MirarA = null;
            }
            AmenazarOPresumir();
        }
    
        // Alerta y el jugador cerca: golpes de amenaza (el tercero es la barrida). Sin alerta y a la vista: presume de vez en cuando.
        private void AmenazarOPresumir()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (visual == null || jugador == null || jugador.Raiz == null || jugador.Modo != ModoJugador.APie)
            {
                return;
            }
            Vector3 d = jugador.Raiz.position - transform.position;
            d.y = 0f;
            float distancia = d.magnitude;
            // T-715: el golpe tiene aviso (luz roja y brazo atrás), luego conecta y hace daño de verdad; alejarte a tiempo lo esquiva.
            if (reglaGolpe == null)
            {
                reglaGolpe = new ReglaGolpeGuardia(segundosAviso, Mathf.Max(0.1f, segundosEntreGolpes - segundosAviso));
            }
            ReglaGolpeGuardia.Salida golpe = reglaGolpe.Avanzar(Time.deltaTime, Alerta, distancia <= radioGolpe);
            if (golpe.InicioAviso)
            {
                golpesDados++;
                gente.MirarA = jugador.Raiz.position;
                visual.Golpear(reglaGolpe.Fuerte);
                Sonidos.ReproducirEn("ARM_LASER_AVISO", transform.position + Vector3.up * AlturaAviso);
            }
            ActualizarLuzAviso();
            if (golpe.Impacto)
            {
                Pegar(jugador, golpe.Fuerte);
            }
            if (Alerta)
            {
                return;
            }
            if (distancia <= radioVigila && Time.time >= proximoPresumir)
            {
                proximoPresumir = Time.time + segundosEntrePresumir * (1f + Random.Range(-VariacionPresumir, VariacionPresumir));
                visual.Presumir();
            }
        }

        private const float AlturaAviso = 1.5f;

        // Luz roja sobre el pecho del guardia que sube mientras avisa y se apaga al golpear o al cancelarse.
        private void ActualizarLuzAviso()
        {
            bool avisando = AvisandoGolpe;
            if (luzAviso == null)
            {
                if (!avisando)
                {
                    return;
                }
                GameObject objeto = new GameObject("LuzAvisoGolpe");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = Vector3.up * AlturaAviso;
                luzAviso = objeto.AddComponent<Light>();
                luzAviso.type = LightType.Point;
                luzAviso.color = new Color(1f, 0.1f, 0.05f);
                luzAviso.range = 4f;
                luzAviso.shadows = LightShadows.None;
            }
            luzAviso.enabled = avisando;
            if (avisando)
            {
                luzAviso.intensity = 2f + 10f * reglaGolpe.ProgresoAviso;
            }
        }

        // El golpe llegó: conecta si el jugador sigue al alcance (y al frente, salvo la barrida, que pega alrededor).
        private void Pegar(IJugador jugador, bool fuerte)
        {
            IRecibeDanio objetivo = jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<IRecibeDanio>() : null;
            if (objetivo == null)
            {
                return;
            }
            Vector3 haciaJugador = jugador.Raiz.position - transform.position;
            haciaJugador.y = 0f;
            float alcance = alcanceImpacto + (fuerte ? 0.5f : 0f);
            if (haciaJugador.magnitude > alcance || (!fuerte && Vector3.Angle(transform.forward, haciaJugador) > anguloImpacto * 0.5f))
            {
                return;
            }
            Vector3 punto = jugador.Raiz.position + Vector3.up * 1.2f;
            ResultadoDanio resultado = objetivo.RecibirDanio(new InfoDanio(fuerte ? danioBarrida : danioGolpe, TipoDanio.Golpe, punto, haciaJugador.normalized, null));
            impactosDados++;
            danioHecho += resultado.DanioAplicado;
        }
    }
}
