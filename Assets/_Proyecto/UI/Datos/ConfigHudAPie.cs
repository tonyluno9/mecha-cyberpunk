using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores, textos, tamaños y márgenes del HUD a pie.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config HUD a pie", fileName = "ConfigHudAPie_Nueva")]
    public class ConfigHudAPie : ScriptableObject
    {
        [Tooltip("Color de la barra de vida (turquesa neón).")]
        [SerializeField] private Color colorVida = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de la barra de vida con vida baja (magenta neón).")]
        [SerializeField] private Color colorVidaBaja = new Color(1f, 0.1f, 0.6f, 1f);

        [Tooltip("Color del texto principal (arma, canción y mira).")]
        [SerializeField] private Color colorTexto = new Color(0.55f, 0.85f, 1f, 1f);

        [Tooltip("Color de los avisos (naranja neón): arma recargando o sin balas.")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.55f, 0.1f, 1f);

        [Tooltip("Color del hueco de la barra de vida.")]
        [SerializeField] private Color colorHueco = new Color(0.05f, 0.08f, 0.12f, 0.6f);

        [Tooltip("Debajo de esta parte de la vida máxima la barra usa el color de vida baja (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float umbralVidaBaja = 0.3f;

        [Tooltip("Texto de la canción mientras la radio no avisa ninguna.")]
        [SerializeField] private string textoCancionEspera = "RADIO · sin señal";

        [Tooltip("Tamaño de la letra del texto de vida, arma y canción.")]
        [Min(8)] [SerializeField] private int tamanoTexto = 22;

        [Tooltip("Margen del HUD al borde de la pantalla (px de referencia).")]
        [Min(0f)] [SerializeField] private float margen = 36f;

        [Tooltip("Ancho de la barra de vida (px de referencia).")]
        [Min(10f)] [SerializeField] private float anchoBarra = 260f;

        [Tooltip("Alto de la barra de vida (px de referencia).")]
        [Min(2f)] [SerializeField] private float altoBarra = 12f;

        [Tooltip("Largo de cada brazo de la cruz de la mira (px de referencia).")]
        [Min(1f)] [SerializeField] private float largoMira = 14f;

        public Color ColorVida => colorVida;
        public Color ColorVidaBaja => colorVidaBaja;
        public Color ColorTexto => colorTexto;
        public Color ColorAviso => colorAviso;
        public Color ColorHueco => colorHueco;
        public float UmbralVidaBaja => umbralVidaBaja;
        public string TextoCancionEspera => textoCancionEspera;
        public int TamanoTexto => tamanoTexto;
        public float Margen => margen;
        public float AnchoBarra => anchoBarra;
        public float AltoBarra => altoBarra;
        public float LargoMira => largoMira;
    }
}
