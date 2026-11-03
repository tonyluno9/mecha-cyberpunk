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

        private GenteDeBanqueta gente;
        private float finAlerta;
        private float proximaLlamada;

        /// <summary>True mientras está alerta (sacaste un arma o hubo disparos cerca).</summary>
        public bool Alerta => Time.time < finAlerta;
        public float RadioVigila => radioVigila;

        private void Awake()
        {
            gente = GetComponent<GenteDeBanqueta>();
            gente.ConfigurarMirada(radioVigila, 0f);
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
        }
    }
}
