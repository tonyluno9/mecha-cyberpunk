using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// Adaptación del rival (T-199, D-tony-193 a D-tony-197): cuándo se ve un hábito del jugador, cómo lo recuerda el rival y los
    /// números de las tres respuestas (GUARDIA, FINTAS, RETIRADA). Los arreglos por nivel tienen 2 valores: índice 0 = nivel I,
    /// índice 1 = nivel II. No tiene vida, daño, alcance, recarga ni velocidad absoluta (regla a de D-tony-197).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos adaptación del rival", fileName = "DatosAdaptacionRival_Nuevo")]
    public class DatosAdaptacionRival : ScriptableObject
    {
        public const int Niveles = 2;

        [Header("Observación")]
        [Tooltip("Una pelea más corta que esto no enseña nada (s).")]
        [Min(0f)] [SerializeField] private float segundosMinimos = 5f;
        [Tooltip("PUÑOS: golpes lanzados por el jugador (peguen o no) que hacen falta (golpes).")]
        [Min(1)] [SerializeField] private int golpesLanzadosMinimos = 8;
        [Tooltip("PUÑOS: ritmo de golpes lanzados que hace falta (golpes por minuto).")]
        [Min(0f)] [SerializeField] private float golpesPorMinutoMinimo = 24f;
        [Tooltip("CUBRIRSE: golpes del rival que llegaron al jugador y que hacen falta (golpes).")]
        [Min(1)] [SerializeField] private int golpesRivalMinimos = 4;
        [Tooltip("CUBRIRSE: fracción de esos golpes que llegó con la guardia arriba (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionCubiertaMinima = 0.5f;
        [Tooltip("CASTIGAR: golpes del jugador que conectaron con el rival y que hacen falta (golpes).")]
        [Min(1)] [SerializeField] private int golpesConectadosMinimos = 4;
        [Tooltip("CASTIGAR: fracción de esos golpes que conectó con el rival recuperándose (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionCastigoMinima = 0.4f;

        [Header("Memoria")]
        [Tooltip("Evidencia que hace falta para el nivel I (índice 0) y el nivel II (índice 1) (peleas).")]
        [SerializeField] private int[] umbralesNivel = { 1, 3 };
        [Tooltip("Nivel más alto que puede alcanzar este rival (1 o 2).")]
        [Range(1, 2)] [SerializeField] private int nivelMaximo = 1;
        [Tooltip("Evidencia que pierde un hábito que no se vio en una pelea (peleas).")]
        [Min(0)] [SerializeField] private int olvidoPorPelea = 1;
        [Tooltip("Cuántas adaptaciones pueden estar activas a la vez (adaptaciones).")]
        [Range(1, 3)] [SerializeField] private int maximoActivas = 2;
        [Tooltip("Con esta cantidad de derrotas seguidas del jugador (contando la última) el rival ya no aprende (derrotas).")]
        [Min(1)] [SerializeField] private int derrotasSinAprender = 2;

        [Header("GUARDIA")]
        [Tooltip("Probabilidad de cubrirse ante cada golpe, nivel I y II (0 a 1).")]
        [SerializeField] private float[] probabilidadGuardia = { 0.4f, 0.65f };
        [Tooltip("Lo que tarda en reaccionar al ver empezar tu golpe, antes de subir la guardia (s).")]
        [Min(0f)] [SerializeField] private float reaccionGuardia = 0.06f;
        [Tooltip("Cuánto sostiene la guardia después de tu impacto (s).")]
        [Min(0f)] [SerializeField] private float sostenerTrasImpacto = 0.3f;

        [Header("FINTAS")]
        [Tooltip("Lo más que alarga cada aviso, nivel I y II (s).")]
        [SerializeField] private float[] extraAvisoMaximo = { 0.25f, 0.4f };
        [Tooltip("Probabilidad de amagar en lugar de golpear, nivel I y II (0 a 1).")]
        [SerializeField] private float[] probabilidadAmago = { 0.2f, 0.35f };
        [Tooltip("Hasta qué fracción del aviso de serie enseña el golpe en un amago (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionAmago = 0.6f;
        [Tooltip("Espera entre el amago y el golpe de verdad (s).")]
        [Min(0f)] [SerializeField] private float esperaTrasAmago = 0.3f;

        [Header("RETIRADA")]
        [Tooltip("Probabilidad de dar un paso atrás tras cada golpe suyo, nivel I y II (0 a 1).")]
        [SerializeField] private float[] probabilidadRetirada = { 0.45f, 0.7f };
        [Tooltip("Multiplica su velocidad y aceleración mientras se retira, nivel I y II (veces, 1 o menos).")]
        [SerializeField] private float[] factorVelocidadRetirada = { 0.8f, 0.9f };
        [Tooltip("Del impacto de su golpe a empezar a caminar hacia atrás (s).")]
        [Min(0f)] [SerializeField] private float demoraRetirada = 0.15f;
        [Tooltip("Cuánto se aleja más allá de tu alcance (m).")]
        [Min(0f)] [SerializeField] private float margenRetirada = 0.75f;

        public float SegundosMinimos => segundosMinimos;
        public int GolpesLanzadosMinimos => golpesLanzadosMinimos;
        public float GolpesPorMinutoMinimo => golpesPorMinutoMinimo;
        public int GolpesRivalMinimos => golpesRivalMinimos;
        public float FraccionCubiertaMinima => fraccionCubiertaMinima;
        public int GolpesConectadosMinimos => golpesConectadosMinimos;
        public float FraccionCastigoMinima => fraccionCastigoMinima;
        public IReadOnlyList<int> UmbralesNivel => umbralesNivel;
        public int NivelMaximo => nivelMaximo;
        public int OlvidoPorPelea => olvidoPorPelea;
        public int MaximoActivas => maximoActivas;
        public int DerrotasSinAprender => derrotasSinAprender;
        public IReadOnlyList<float> ProbabilidadGuardia => probabilidadGuardia;
        public float ReaccionGuardia => reaccionGuardia;
        public float SostenerTrasImpacto => sostenerTrasImpacto;
        public IReadOnlyList<float> ExtraAvisoMaximo => extraAvisoMaximo;
        public IReadOnlyList<float> ProbabilidadAmago => probabilidadAmago;
        public float FraccionAmago => fraccionAmago;
        public float EsperaTrasAmago => esperaTrasAmago;
        public IReadOnlyList<float> ProbabilidadRetirada => probabilidadRetirada;
        public IReadOnlyList<float> FactorVelocidadRetirada => factorVelocidadRetirada;
        public float DemoraRetirada => demoraRetirada;
        public float MargenRetirada => margenRetirada;

        private void OnValidate()
        {
            umbralesNivel = Ajustar(umbralesNivel, 1, 1, int.MaxValue);
            // El nivel II no pide menos evidencia que el I.
            umbralesNivel[1] = Mathf.Max(umbralesNivel[1], umbralesNivel[0]);
            probabilidadGuardia = Ajustar(probabilidadGuardia, 0.4f, 0f, 1f);
            extraAvisoMaximo = Ajustar(extraAvisoMaximo, 0.25f, 0f, 10f);
            probabilidadAmago = Ajustar(probabilidadAmago, 0.2f, 0f, 1f);
            probabilidadRetirada = Ajustar(probabilidadRetirada, 0.45f, 0f, 1f);
            factorVelocidadRetirada = Ajustar(factorVelocidadRetirada, 0.8f, 0.1f, 1f);
        }

        private static int[] Ajustar(int[] valores, int porDefecto, int minimo, int maximo)
        {
            int[] r = new int[Niveles];
            for (int i = 0; i < Niveles; i++)
            {
                int v = valores != null && i < valores.Length ? valores[i] : porDefecto;
                r[i] = Mathf.Clamp(v, minimo, maximo);
            }
            return r;
        }

        private static float[] Ajustar(float[] valores, float porDefecto, float minimo, float maximo)
        {
            float[] r = new float[Niveles];
            for (int i = 0; i < Niveles; i++)
            {
                float v = valores != null && i < valores.Length ? valores[i] : porDefecto;
                r[i] = Mathf.Clamp(v, minimo, maximo);
            }
            return r;
        }
    }
}
