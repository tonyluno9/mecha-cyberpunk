using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// T-1651 (T26 y T27): cómo se confronta cada facción con el jugador. Un asset por facción en IA/Resources/Confrontacion/
    /// (Config_Skulls, Config_Snakes, Config_Guardias, Config_Policia, Config_Nova). Los crea AplicarConfrontacionT1651. Reglas en docs/confrontacion.md.
    /// Skulls, Snakes y Guardias los usa el código de IA/APie; los de Policia y Nova solo guardan los valores que esas facciones ya usan (Trafico y RespuestaNova), para verlos juntos.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/IA/Config confrontacion", fileName = "ConfigConfrontacion_Nueva")]
    public class ConfigConfrontacion : ScriptableObject
    {
        public const string CarpetaRecursos = "Confrontacion/";
        public const string RecursoSkulls = CarpetaRecursos + "Config_Skulls";
        public const string RecursoSnakes = CarpetaRecursos + "Config_Snakes";
        public const string RecursoGuardias = CarpetaRecursos + "Config_Guardias";
        public const string RecursoPolicia = CarpetaRecursos + "Config_Policia";
        public const string RecursoNova = CarpetaRecursos + "Config_Nova";

        [Header("Facción")]
        [Tooltip("Nombre que sale en el subtítulo de sus frases.")]
        [SerializeField] private string nombreHablante = "SKULL";
        [Tooltip("Color del nombre en el subtítulo.")]
        [SerializeField] private Color colorHablante = new Color(1f, 0.25f, 0.8f);
        [Tooltip("Cuándo pasa de advertir a pelear de verdad.")]
        [SerializeField] private CuandoPelea cuandoPelea = CuandoPelea.SoloSiElJugadorAtaca;

        [Header("Detección y advertencia")]
        [Tooltip("Te detectan a esta distancia y empiezan a advertir (m). Con 0 no aplica.")]
        [Min(0f)] [SerializeField] private float distanciaDeteccion = 12f;
        [Tooltip("Cuánto advierten antes de pasar a empujar si te quedas (s).")]
        [Min(0f)] [SerializeField] private float segundosAdvertencia = 20f;
        [Tooltip("Si al acabar la advertencia sigues a esta distancia o menos, cuenta como quedarte (m). Más lejos: se retiran.")]
        [Min(0f)] [SerializeField] private float distanciaQuedarse = 10f;
        [Tooltip("Si te alejas más que esto dejan de seguirte y regresan a su lugar (m).")]
        [Min(0f)] [SerializeField] private float distanciaDejarDeSeguir = 22f;
        [Tooltip("Mínimo que dura la retirada antes de poder advertirte otra vez (s).")]
        [Min(0f)] [SerializeField] private float segundosRetirada = 3f;
        [Tooltip("Durante la advertencia se acercan hasta esta distancia y ahí se quedan (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaAcercamiento = 5f;

        [Header("Frases y gestos")]
        [Tooltip("Una frase cada tanto, una sola voz a la vez en el grupo (s).")]
        [Min(1f)] [SerializeField] private float segundosEntreFrases = 5f;
        [Tooltip("Lo que se quedan quietos al provocar (animación de burla) o al empujar (s).")]
        [Min(0.2f)] [SerializeField] private float segundosGesto = 2.2f;
        [Tooltip("Si saca el arma (sin apuntar ni disparar) mientras advierte. Los SNAKES, sí.")]
        [SerializeField] private bool sacaArmaEnAdvertencia;

        [Header("Empujones (tras la advertencia)")]
        [Tooltip("Cada cuánto empuja uno del grupo (s).")]
        [Min(0.5f)] [SerializeField] private float segundosEntreEmpujones = 4f;
        [Tooltip("Cada cuántos empujones te tiran al suelo (1 = siempre).")]
        [Min(1)] [SerializeField] private int empujonesParaTirar = 2;
        [Tooltip("Tras tantos empujones se aburren y se retiran (0 = nunca).")]
        [Min(0)] [SerializeField] private int empujonesMaximos = 4;
        [Tooltip("Daño de cada empujón (puntos).")]
        [Min(0f)] [SerializeField] private float danioEmpujon = 1f;

        public string NombreHablante => nombreHablante;
        public Color ColorHablante => colorHablante;
        public CuandoPelea CuandoPelea => cuandoPelea;
        public float DistanciaDeteccion => distanciaDeteccion;
        public float SegundosAdvertencia => segundosAdvertencia;
        public float DistanciaQuedarse => distanciaQuedarse;
        public float DistanciaDejarDeSeguir => distanciaDejarDeSeguir;
        public float SegundosRetirada => segundosRetirada;
        public float DistanciaAcercamiento => distanciaAcercamiento;
        public float SegundosEntreFrases => segundosEntreFrases;
        public float SegundosGesto => segundosGesto;
        public bool SacaArmaEnAdvertencia => sacaArmaEnAdvertencia;
        public float SegundosEntreEmpujones => segundosEntreEmpujones;
        public int EmpujonesParaTirar => empujonesParaTirar;
        public int EmpujonesMaximos => empujonesMaximos;
        public float DanioEmpujon => danioEmpujon;
    }
}
