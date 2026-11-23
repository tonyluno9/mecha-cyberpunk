using MechaCyberpunk.Trafico.Peatones;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>Los números de los peatones del barrio (T-330): velocidad, esperas en las esquinas y cuántos hay de día y de noche.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de peatones", fileName = "ConfigPeatones_Nueva")]
    public class ConfigPeatones : ScriptableObject
    {
        [Min(0.1f)] [Tooltip("Velocidad al caminar (m/s).")]
        [SerializeField] private float velocidad = 1.62f;
        [Min(0f)] [Tooltip("Espera mínima en una esquina (s).")]
        [SerializeField] private float esperaMinima = 1f;
        [Min(0f)] [Tooltip("Espera máxima en una esquina (s).")]
        [SerializeField] private float esperaMaxima = 5f;
        [Min(0)] [Tooltip("Cuántos peatones hay de día (personas).")]
        [SerializeField] private int peatonesDeDia = 12;
        [Min(0)] [Tooltip("Cuántos peatones hay de noche (personas).")]
        [SerializeField] private int peatonesDeNoche = 24;
        [Min(0.5f)] [Tooltip("A qué distancia del jugador se detiene un peatón (m).")]
        [SerializeField] private float distanciaParada = 2f;
        [Min(0.5f)] [Tooltip("Cuánto se aleja del borde de la manzana hacia adentro el centro de la banqueta (m).")]
        [SerializeField] private float margenBanqueta = 2.4f;

        [Min(0f)] [Tooltip("Cuánto se acortan las rutas que terminan junto a los cierres del barrio (m).")]
        [SerializeField] private float recorteCierres = 15f;

        [Header("Huida (T-443)")]
        [Min(1f)] [Tooltip("A qué distancia de un disparo los peatones lo oyen y salen corriendo (m).")]
        [SerializeField] private float radioOirDisparo = 45f;
        [Min(0.5f)] [Tooltip("Cuánto tiempo corre un peatón después de oír un disparo (s).")]
        [SerializeField] private float segundosHuida = 6f;
        [Min(1f)] [Tooltip("Cuántas veces su velocidad normal corre al huir (multiplicador).")]
        [SerializeField] private float multiplicadorHuida = 2.8f;

        [Header("Vida y llamada (T-663)")]
        [Min(1f)] [Tooltip("Vida de un peatón (puntos); a 0 cae y ya no llama a la policía.")]
        [SerializeField] private float vidaPeaton = 40f;

        [Header("Civiles que se defienden y matar a uno (T-1305)")]
        [Range(0f, 1f)] [Tooltip("Probabilidad de que un civil herido (que no murió) te pegue de vuelta en vez de huir (fracción de 0 a 1).")]
        [SerializeField] private float probabilidadRevancha = 0.3f;
        [Min(0.5f)] [Tooltip("A qué distancia del jugador puede pegar un civil y desde dónde decide pelear (m).")]
        [SerializeField] private float alcanceRevancha = 1.5f;
        [Min(0f)] [Tooltip("Daño de cada golpe de un civil al jugador (puntos).")]
        [SerializeField] private float danioRevancha = 5f;
        [Min(1)] [Tooltip("Cuántos golpes da un civil antes de huir (golpes).")]
        [SerializeField] private int golpesRevancha = 2;
        [Min(0.1f)] [Tooltip("Tiempo entre un golpe del civil y el siguiente (s).")]
        [SerializeField] private float segundosEntreGolpesRevancha = 0.7f;
        [Min(0.5f)] [Tooltip("Tiempo máximo que un civil insiste en pelear antes de huir (s).")]
        [SerializeField] private float segundosMaximosRevancha = 3f;
        [Min(0f)] [Tooltip("Calor que suma matar a un civil (puntos, motivo Pelea; solo cuenta si hay testigos).")]
        [SerializeField] private float calorPorMuerte = 25f;

        [Header("Platicar y agacharse (T-1516)")]
        [Min(0)] [Tooltip("Uno de cada cuántos peatones se queda platicando en pareja o trío (0 = nadie platica).")]
        [SerializeField] private int unoDeCadaPlatica;
        [Range(0f, 1f)] [Tooltip("Probabilidad de que un civil que oye un disparo se agache cubriéndose la cabeza antes de huir (0 = siempre huye de inmediato).")]
        [SerializeField] private float probabilidadAgacharse;
        [Min(0.5f)] [Tooltip("Cuánto se queda agachado después de oír un disparo (s).")]
        [SerializeField] private float segundosAgachado = 3f;

        public int UnoDeCadaPlatica => unoDeCadaPlatica;
        public float ProbabilidadAgacharse => probabilidadAgacharse;
        public float SegundosAgachado => segundosAgachado;

        /// <summary>T-1516: para el creador de editor y las pruebas.</summary>
        public void ConfigurarReaccion(int unoDeCadaQuePlatica, float probabilidadDeAgacharse, float segundosDeAgachado)
        {
            unoDeCadaPlatica = Mathf.Max(0, unoDeCadaQuePlatica);
            probabilidadAgacharse = Mathf.Clamp01(probabilidadDeAgacharse);
            segundosAgachado = Mathf.Max(0.5f, segundosDeAgachado);
        }

        public float VidaPeaton => vidaPeaton;
        public float ProbabilidadRevancha => probabilidadRevancha;
        public float AlcanceRevancha => alcanceRevancha;
        public float DanioRevancha => danioRevancha;
        public int GolpesRevancha => golpesRevancha;
        public float SegundosEntreGolpesRevancha => segundosEntreGolpesRevancha;
        public float SegundosMaximosRevancha => segundosMaximosRevancha;
        public float CalorPorMuerte => calorPorMuerte;
        public float RadioOirDisparo => radioOirDisparo;
        public float SegundosHuida => segundosHuida;
        public float MultiplicadorHuida => multiplicadorHuida;
        public float RecorteCierres => recorteCierres;
        public float DistanciaParada => distanciaParada;
        public float MargenBanqueta => margenBanqueta;
        /// <summary>El mayor de los dos conteos: cuántos peatones se preparan en total.</summary>
        public int Maximo => Mathf.Max(peatonesDeDia, peatonesDeNoche);
        public float Velocidad => velocidad;
        public float EsperaMinima => esperaMinima;
        public float EsperaMaxima => Mathf.Max(esperaMinima, esperaMaxima);
        public int PeatonesDeDia => peatonesDeDia;
        public int PeatonesDeNoche => peatonesDeNoche;

        /// <summary>Cuántos peatones debe haber ahora (personas).</summary>
        public int Cantidad(bool esDeNoche) => esDeNoche ? peatonesDeNoche : peatonesDeDia;

        /// <summary>Arma las reglas de un peatón con estos números y su ruta.</summary>
        public CalculoPeaton Crear(System.Collections.Generic.IReadOnlyList<float> largosSegmentos, System.Random azar)
        {
            return new CalculoPeaton(largosSegmentos, velocidad, esperaMinima, EsperaMaxima, azar);
        }
    }
}
