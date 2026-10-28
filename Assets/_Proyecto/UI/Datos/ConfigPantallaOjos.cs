using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores y límites de la pantalla de OJOS de la cabina (T-194, D-tony-165 y D-tony-168).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de ojos", fileName = "ConfigPantallaOjos_Nueva")]
    public class ConfigPantallaOjos : ScriptableObject
    {
        [Header("Qué rival se lee")]
        [Tooltip("Distancia máxima al rival que se lee (m).")]
        [Min(1f)] [SerializeField] private float distanciaMaxima = 40f;
        [Tooltip("Ángulo máximo entre la vista de la cabina y el rival (°).")]
        [Range(1f, 180f)] [SerializeField] private float anguloMaximo = 60f;

        [Header("Textos")]
        [SerializeField] private string textoSinObjetivo = "SIN OBJETIVO";
        [Tooltip("Vida en números (nivel 2). {0} = actual, {1} = máxima.")]
        [SerializeField] private string formatoNumeros = "{0} / {1}";
        [SerializeField] private string textoProximoGolpe = "PRÓXIMO: GOLPE";
        [SerializeField] private string textoProximoHabilidad = "PRÓXIMO: HABILIDAD";

        [Header("Colores")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.03f, 0.07f, 0.95f);
        [Tooltip("Barra de vida del rival (turquesa).")]
        [SerializeField] private Color colorBarra = new Color(0f, 1f, 0.9f, 1f);
        [SerializeField] private Color colorFondoBarra = new Color(0.08f, 0.1f, 0.16f, 1f);
        [SerializeField] private Color colorTexto = new Color(0.85f, 0.95f, 1f, 1f);
        [Tooltip("El aviso del próximo ataque (naranja, igual que el brazo del rival; nunca rojo puro).")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color colorSinObjetivo = new Color(0.5f, 0.52f, 0.58f, 1f);

        public float DistanciaMaxima => distanciaMaxima;
        public float AnguloMaximo => anguloMaximo;
        public string TextoSinObjetivo => textoSinObjetivo;
        public string FormatoNumeros => formatoNumeros;
        public string TextoProximoGolpe => textoProximoGolpe;
        public string TextoProximoHabilidad => textoProximoHabilidad;
        public Color ColorFondo => colorFondo;
        public Color ColorBarra => colorBarra;
        public Color ColorFondoBarra => colorFondoBarra;
        public Color ColorTexto => colorTexto;
        public Color ColorAviso => colorAviso;
        public Color ColorSinObjetivo => colorSinObjetivo;
    }
}
