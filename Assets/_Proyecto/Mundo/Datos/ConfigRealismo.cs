using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>
    /// T14 (T-1605): presentación del realismo visual: tinte de las nubes por barrio, límites de los barrios, luna del cielo y lo que apaga el
    /// selector de calidad (SSAO, halos, partículas, sombras de luces adicionales). Los límites son los medidos en el juego (reporte 21-nov).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Mundo/Config Realismo", fileName = "ConfigRealismo_Mundo")]
    public class ConfigRealismo : ScriptableObject
    {
        [Header("Barrios (en X y Z del mundo)")]
        [Tooltip("Suburbios queda a la izquierda de esta X (m).")]
        [SerializeField] private float limiteSuburbiosX = -40f;

        [Tooltip("Neon Alto y Nova quedan a la derecha de esta X (m).")]
        [SerializeField] private float limiteEsteX = 40f;

        [Tooltip("Nova queda debajo de esta Z (m); Neon Alto arriba.")]
        [SerializeField] private float limiteNovaZ = -10f;

        [Header("Tinte de las nubes por barrio")]
        [SerializeField] private Color tinteSuburbios = new Color(1f, 0.5f, 0.16f);
        [SerializeField] private Color tinteNeonAlto = new Color(1f, 0.18f, 0.7f);
        [SerializeField] private Color tinteNova = new Color(0.3f, 0.55f, 1f);
        [SerializeField] private Color tinteMercado = new Color(1f, 0.62f, 0.3f);

        [Tooltip("Qué tan rápido cambia el tinte al cruzar de barrio (1/s; 1 = cerca de un segundo).")]
        [SerializeField, Min(0.01f)] private float velocidadTinte = 0.35f;

        [Header("Cielo")]
        [Tooltip("Radio (m) del domo del cielo; menor que el plano lejano de la cámara.")]
        [SerializeField, Min(10f)] private float radioDomo = 700f;

        [Tooltip("Hacia donde esta la luna: giro en ° desde el norte hacia el este.")]
        [SerializeField, Range(0f, 360f)] private float giroLuna = 140f;

        [Tooltip("Altura de la luna sobre el horizonte (°).")]
        [SerializeField, Range(5f, 85f)] private float alturaLuna = 38f;

        [Header("Calidad")]
        [Tooltip("Features de oclusión ambiental (SSAO) de los renderers de URP que el selector de calidad prende o apaga.")]
        [SerializeField] private ScriptableRendererFeature[] oclusionAmbiental = new ScriptableRendererFeature[0];

        [Tooltip("Los halos y las partículas de aire/vapor cuyo nombre empieza con alguno de estos textos se apagan en calidad baja.")]
        [SerializeField] private string[] prefijosParticulas = { "Halo_", "Vapor_", "Polvo_" };

        [Header("Aire fino")]
        [Tooltip("Material de las partículas de polvo cerca del jugador (AMB_HaloNiebla).")]
        [SerializeField] private Material materialPolvo;

        [Tooltip("Partículas de polvo vivas a la vez.")]
        [SerializeField, Min(0)] private int maximoPolvo = 140;

        [Tooltip("Lado (m) de la caja de polvo alrededor del jugador.")]
        [SerializeField, Min(2f)] private float ladoPolvo = 22f;

        public float LimiteSuburbiosX => limiteSuburbiosX;
        public float LimiteEsteX => limiteEsteX;
        public float LimiteNovaZ => limiteNovaZ;
        public Color TinteSuburbios => tinteSuburbios;
        public Color TinteNeonAlto => tinteNeonAlto;
        public Color TinteNova => tinteNova;
        public Color TinteMercado => tinteMercado;
        public float VelocidadTinte => velocidadTinte;
        public float RadioDomo => radioDomo;
        public float GiroLuna => giroLuna;
        public float AlturaLuna => alturaLuna;
        public ScriptableRendererFeature[] OclusionAmbiental => oclusionAmbiental;
        public string[] PrefijosParticulas => prefijosParticulas;
        public Material MaterialPolvo => materialPolvo;
        public int MaximoPolvo => maximoPolvo;
        public float LadoPolvo => ladoPolvo;

        /// <summary>Dirección (unitaria, espacio del mundo) hacia la luna según giro y altura.</summary>
        public Vector3 DireccionLuna()
        {
            Quaternion giro = Quaternion.Euler(-alturaLuna, giroLuna, 0f);
            return giro * Vector3.forward;
        }
    }
}
