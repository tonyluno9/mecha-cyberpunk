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

        private const float VariacionPresumir = 0.25f;
        private VisualPersonaje visual;
        private float proximoPresumir;
        private float proximoGolpe;
        private int golpesDados;
        private GenteDeBanqueta gente;
        private float finAlerta;
        private float proximaLlamada;

        /// <summary>True mientras está alerta (sacaste un arma o hubo disparos cerca).</summary>
        public bool Alerta => Time.time < finAlerta;
        public float RadioVigila => radioVigila;

        /// <summary>Cuántos golpes de amenaza ha lanzado (cada tercero es la barrida).</summary>
        public int GolpesDados => golpesDados;

        /// <summary>Para las pruebas y los creadores: tiempos de presumir y de golpear.</summary>
        public void ConfigurarGestos(float entrePresumir, float entreGolpes)
        {
            segundosEntrePresumir = Mathf.Max(1f, entrePresumir);
            segundosEntreGolpes = Mathf.Max(0.2f, entreGolpes);
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
            if (Alerta)
            {
                if (distancia <= radioGolpe && Time.time >= proximoGolpe)
                {
                    proximoGolpe = Time.time + segundosEntreGolpes;
                    golpesDados++;
                    visual.Golpear(golpesDados % 3 == 0);
                }
                return;
            }
            if (distancia <= radioVigila && Time.time >= proximoPresumir)
            {
                proximoPresumir = Time.time + segundosEntrePresumir * (1f + Random.Range(-VariacionPresumir, VariacionPresumir));
                visual.Presumir();
            }
        }
    }
}
