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

        public AjusteSensacion() { }

        public AjusteSensacion(float patada, float desvio, float destello, int particulas)
        {
            this.patada = patada;
            this.desvio = desvio;
            this.destello = destello;
            this.particulas = particulas;
        }
    }

    /// <summary>Parámetros de la sensación de golpe y disparo (T-420). El asset va en Resources/DatosSensacion; sin él se usan estos valores.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de sensación de disparo", fileName = "DatosSensacion")]
    public class DatosSensacion : ScriptableObject
    {
        [Tooltip("En orden: Puños, Pistola, Escopeta, Fusil, Sniper.")]
        [SerializeField] private AjusteSensacion[] armas =
        {
            new AjusteSensacion(0.9f, 0.5f, 0f, 0),
            new AjusteSensacion(1.0f, 0.25f, 0.14f, 6),
            new AjusteSensacion(3.2f, 0.5f, 0.26f, 4),
            new AjusteSensacion(0.6f, 0.35f, 0.16f, 5),
            new AjusteSensacion(4.5f, 0.3f, 0.22f, 10),
        };

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

        public AjusteSensacion Ajuste(ArmaAPie arma)
        {
            int i = (int)arma;
            return armas != null && i >= 0 && i < armas.Length && armas[i] != null ? armas[i] : new AjusteSensacion();
        }
    }
}
