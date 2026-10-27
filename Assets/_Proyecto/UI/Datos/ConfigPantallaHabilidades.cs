using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores y textos de la pantalla de habilidades del tablero (T-177, D-tony-83, D-tony-122 y D-tony-138).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de habilidades", fileName = "ConfigPantallaHabilidades_Nueva")]
    public class ConfigPantallaHabilidades : ScriptableObject
    {
        [Header("Textos")]
        [Tooltip("{0} = tecla, {1} = habilidad con nivel, {2} = munición.")]
        [SerializeField] private string formatoFila = "{0} · {1} · ×{2}";
        [Tooltip("Fila sin dato de munición. {0} = tecla, {1} = habilidad con nivel.")]
        [SerializeField] private string formatoFilaSinMunicion = "{0} · {1}";
        [Tooltip("Fila de una ranura sin habilidad. {0} = tecla.")]
        [SerializeField] private string formatoFilaVacia = "{0} ·";
        [SerializeField] private string textoVacia = "—";
        [SerializeField] private string textoListo = "LISTO";
        [SerializeField] private string textoSinMunicion = "SIN MUNICIÓN";
        [SerializeField] private string textoSoloBrazos = "SOLO BRAZOS";
        [Tooltip("Segundos que faltan para que la habilidad vuelva. {0} = segundos enteros.")]
        [SerializeField] private string formatoSegundos = "{0} s";

        [Header("Colores")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.03f, 0.07f, 0.95f);
        [SerializeField] private Color colorMarco = new Color(0.12f, 0.2f, 0.3f, 1f);
        [Tooltip("Marco y franja del cristal con el escudo activo.")]
        [SerializeField] private Color colorEscudo = new Color(0f, 1f, 0.9f, 1f);
        [SerializeField] private Color colorListo = new Color(0.3f, 1f, 0.6f, 1f);
        [SerializeField] private Color colorEnfriando = new Color(1f, 0.2f, 0.75f, 1f);
        [SerializeField] private Color colorSinMunicion = new Color(1f, 0.6f, 0.25f, 1f);
        [SerializeField] private Color colorGris = new Color(0.5f, 0.52f, 0.58f, 1f);
        [SerializeField] private Color colorFondoBarra = new Color(0.08f, 0.1f, 0.16f, 1f);
        [SerializeField] private Color colorTextoBarra = new Color(1f, 1f, 1f, 1f);

        public string FormatoFila => formatoFila;
        public string FormatoFilaSinMunicion => formatoFilaSinMunicion;
        public string FormatoFilaVacia => formatoFilaVacia;
        public string TextoVacia => textoVacia;
        public string TextoListo => textoListo;
        public string TextoSinMunicion => textoSinMunicion;
        public string TextoSoloBrazos => textoSoloBrazos;
        public string FormatoSegundos => formatoSegundos;
        public Color ColorFondo => colorFondo;
        public Color ColorMarco => colorMarco;
        public Color ColorEscudo => colorEscudo;
        public Color ColorListo => colorListo;
        public Color ColorEnfriando => colorEnfriando;
        public Color ColorSinMunicion => colorSinMunicion;
        public Color ColorGris => colorGris;
        public Color ColorFondoBarra => colorFondoBarra;
        public Color ColorTextoBarra => colorTextoBarra;
    }
}
