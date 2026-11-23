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

        [Header("Verte, mirarte y acercarse (T-1659)")]
        [Tooltip("Te ven de lejos y te miran a esta distancia (m). Con 0 no aplica.")]
        [Min(0f)] [SerializeField] private float distanciaDeteccion = 20f;
        [Tooltip("Segundos seguidos que debes estar cerca para que UNO se acerque (s).")]
        [Min(0f)] [UnityEngine.Serialization.FormerlySerializedAs("segundosAdvertencia")] [SerializeField] private float segundosCerca = 20f;
        [Tooltip("A esta distancia o menos cuenta como estar cerca (m).")]
        [Min(0f)] [UnityEngine.Serialization.FormerlySerializedAs("distanciaQuedarse")] [SerializeField] private float distanciaCerca = 8f;
        [Tooltip("Una vez que uno se acerca, si te alejas más que esto se acaba todo y regresan a su lugar (m).")]
        [Min(0f)] [SerializeField] private float distanciaDejarDeSeguir = 14f;
        [Tooltip("Mínimo que dura la retirada antes de poder mirarte otra vez (s).")]
        [Min(0f)] [SerializeField] private float segundosRetirada = 3f;
        [Tooltip("El que se acerca llega hasta esta distancia y empieza a empujar (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaAcercamiento = 2.2f;
        [Tooltip("Velocidad con la que el que se acerca camina hacia ti (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadCaminar = 1.2f;

        [Header("Frases y gestos (los guardias aún usan las frases; los de calle ya no hablan al confrontar)")]
        [Tooltip("Una frase cada tanto, una sola voz a la vez en el grupo (s).")]
        [Min(1f)] [SerializeField] private float segundosEntreFrases = 5f;
        [Tooltip("Lo que se quedan quietos al empujar (s).")]
        [Min(0.2f)] [SerializeField] private float segundosGesto = 2.2f;
        [Tooltip("Si saca el arma (sin apuntar ni disparar) mientras te mira. Los SNAKES, sí.")]
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
        public float SegundosCerca => segundosCerca;
        public float DistanciaCerca => distanciaCerca;
        public float VelocidadCaminar => velocidadCaminar;
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
