using System;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Sensación de un arma al usarla (T-420): patada de cámara, tamaño del destello y chispas. Índice = valor de ArmaAPie.</summary>
    [Serializable]
    public class AjusteSensacion
    {
        [Tooltip("Patada de cámara hacia arriba en cada tiro (°). En los puños, sacudón vertical al conectar.")]
        [Min(0f)] public float patada = 0.8f;
        [Tooltip("Desvío lateral máximo de la patada (°, signo al azar).")]
        [Min(0f)] public float desvio = 0.2f;
        [Tooltip("Tamaño del destello del cañón (m). 0 = sin destello.")]
        [Min(0f)] public float destello = 0.3f;
        [Tooltip("Chispas o gotas por impacto (por perdigón en la escopeta).")]
        [Min(0)] public int particulas = 6;

        [Header("T-1486: peso del disparo")]
        [Tooltip("Cuánto retrocede el arma hacia atrás en cada tiro (m).")]
        [Min(0f)] public float retrocesoArma = 0.08f;
        [Tooltip("Cuánto tarda el arma en ir atrás y volver (s).")]
        [Min(0.02f)] public float duracionRetrocesoArma = 0.12f;
        [Tooltip("Cuánto tarda la patada de cámara en volver a cero (s). 0 = usar la general.")]
        [Min(0f)] public float recuperacion;
        [Tooltip("Fuerza del temblor de cámara por impulso (ver TemblorCamara). 0 = sin temblor.")]
        [Min(0f)] public float impulso = 0.25f;
        [Tooltip("Casquillos expulsados por tiro (0 = el arma no los suelta).")]
        [Min(0)] public int casquillos = 1;
        [Tooltip("Dispersión (°) que se suma a cada tiro seguido.")]
        [Min(0f)] public float floracion;
        [Tooltip("Tope de la dispersión acumulada (°).")]
        [Min(0f)] public float topeFloracion;
        [Tooltip("Cuánta dispersión acumulada se recupera por segundo (°/s).")]
        [Min(0f)] public float recuperacionFloracion = 8f;

        public AjusteSensacion() { }

        public AjusteSensacion(float patada, float desvio, float destello, int particulas)
        {
            this.patada = patada;
            this.desvio = desvio;
            this.destello = destello;
            this.particulas = particulas;
        }

        /// <summary>T-1486: con todo el peso del disparo.</summary>
        public AjusteSensacion(float patada, float desvio, float destello, int particulas, float retrocesoArma, float duracionRetrocesoArma,
            float recuperacion, float impulso, int casquillos, float floracion, float topeFloracion, float recuperacionFloracion)
            : this(patada, desvio, destello, particulas)
        {
            this.retrocesoArma = retrocesoArma;
            this.duracionRetrocesoArma = duracionRetrocesoArma;
            this.recuperacion = recuperacion;
            this.impulso = impulso;
            this.casquillos = casquillos;
            this.floracion = floracion;
            this.topeFloracion = topeFloracion;
            this.recuperacionFloracion = recuperacionFloracion;
        }
    }

    /// <summary>Parámetros de la sensación de golpe y disparo (T-420). El asset va en Resources/DatosSensacion; sin él se usan estos valores.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de sensación de disparo", fileName = "DatosSensacion")]
    public class DatosSensacion : ScriptableObject
    {
        [Tooltip("En orden del enum ArmaAPie: Puños, Pistola, Escopeta, Fusil, Sniper, Subfusil, Revólver, Lanzagranadas, Granada, Rifle de plasma.")]
        [SerializeField] private AjusteSensacion[] armas = AjustesPorArma();

        [Tooltip("Cuánto tarda la patada en volver a cero (s).")]
        [Min(0.02f)] [SerializeField] private float duracionPatada = 0.18f;
        [Tooltip("Tope de patada acumulada en ráfaga (°).")]
        [Min(0f)] [SerializeField] private float topePatada = 5f;
        [Tooltip("Cuánto dura el destello del cañón (s). Unos 2 o 3 cuadros.")]
        [Min(0.01f)] [SerializeField] private float duracionDestello = 0.04f;
        [Tooltip("Cuánto dura una partícula de chispa o sangre (s).")]
        [Min(0.05f)] [SerializeField] private float vidaParticula = 0.35f;
        [Tooltip("Cuánto dura la marca en la retícula al pegar (s).")]
        [Min(0.02f)] [SerializeField] private float duracionHitmarker = 0.15f;
        [Tooltip("Cuánto dura la marca de muerte (s).")]
        [Min(0.02f)] [SerializeField] private float duracionHitmarkerMuerte = 0.35f;
        [Tooltip("Material sin luz con textura y color por vértice (Sprites/Default). Sin él se busca ese shader por nombre.")]
        [SerializeField] private Material materialEfecto;
        [Tooltip("T-676: textura del destello del cañón (imageassets/vfx/F01_Destello.png cuando se importe). Sin ella se dibuja una chispa procedural. El menú Mecha Cyberpunk > Ajustar T-676 destello F01 la asigna sola si ya está en el proyecto.")]
        [SerializeField] private Texture2D texturaDestello;

        public float DuracionPatada => duracionPatada;
        public float TopePatada => topePatada;
        public float DuracionDestello => duracionDestello;
        public float VidaParticula => vidaParticula;
        public float DuracionHitmarker => duracionHitmarker;
        public float DuracionHitmarkerMuerte => duracionHitmarkerMuerte;
        public Material MaterialEfecto => materialEfecto;
        public Texture2D TexturaDestello => texturaDestello;

        /// <summary>
        /// T-1486: el peso de cada arma (patada, retroceso visual, temblor, casquillos, floración). Es el valor de fábrica y lo que escribe el método
        /// de editor AjustarSensacionT1486. Índice = valor de <see cref="ArmaAPie"/>.
        /// </summary>
        public static AjusteSensacion[] AjustesPorArma()
        {
            return new[]
            {
                new AjusteSensacion(0.9f, 0.5f, 0f, 0, 0f, 0.12f, 0f, 0f, 0, 0f, 0f, 8f),            // Puños
                new AjusteSensacion(1.0f, 0.25f, 0.26f, 6, 0.07f, 0.12f, 0f, 0.25f, 1, 0.3f, 1.5f, 6f), // Pistola
                new AjusteSensacion(3.2f, 0.5f, 0.5f, 4, 0.16f, 0.26f, 0.3f, 0.6f, 1, 0f, 0f, 8f),   // Escopeta
                new AjusteSensacion(0.6f, 0.35f, 0.3f, 5, 0.045f, 0.07f, 0.1f, 0.3f, 1, 0.15f, 2.5f, 5f), // Fusil
                new AjusteSensacion(4.5f, 0.3f, 0.4f, 10, 0.2f, 0.4f, 0.45f, 0.6f, 1, 0f, 0f, 8f),   // Sniper
                new AjusteSensacion(0.5f, 0.3f, 0.22f, 4, 0.035f, 0.06f, 0.09f, 0.22f, 1, 0.12f, 2f, 5f), // Subfusil
                new AjusteSensacion(2.6f, 0.4f, 0.4f, 8, 0.14f, 0.2f, 0.28f, 0.5f, 0, 0f, 0f, 8f),   // Revólver (los casquillos salen al recargar)
                new AjusteSensacion(3.0f, 0.3f, 0.3f, 0, 0.15f, 0.3f, 0.3f, 0.6f, 0, 0f, 0f, 8f),    // Lanzagranadas
                new AjusteSensacion(0f, 0f, 0f, 0, 0f, 0.12f, 0f, 0f, 0, 0f, 0f, 8f),               // Granada
                new AjusteSensacion(0.7f, 0.2f, 0.3f, 5, 0.05f, 0.1f, 0.12f, 0.3f, 0, 0f, 0f, 8f),  // Rifle de plasma
            };
        }

        public AjusteSensacion Ajuste(ArmaAPie arma)
        {
            int i = (int)arma;
            return armas != null && i >= 0 && i < armas.Length && armas[i] != null ? armas[i] : new AjusteSensacion();
        }
    }
}
