using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores y tamaños del título y de la creación de personaje (T-126).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de creación", fileName = "ConfigPantallaCreacion_Nueva")]
    public class ConfigPantallaCreacion : ScriptableObject
    {
        [Tooltip("Color de la fila no elegida.")]
        [SerializeField] private Color colorNormal = new Color(0.65f, 0.7f, 0.8f, 1f);

        [Tooltip("Color de la fila elegida (neón).")]
        [SerializeField] private Color colorElegido = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color del aviso y de la confirmación.")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.3f, 0.55f, 1f);

        [Min(8)] [SerializeField] private int tamanoTitulo = 90;
        [Min(8)] [SerializeField] private int tamanoTexto = 44;
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [Tooltip("Segundos de cada medio parpadeo del cursor del campo de texto.")]
        [Min(0.05f)] [SerializeField] private float segundosParpadeo = 0.5f;

        [SerializeField] private string textoJuego = "MECHA CYBERPUNK";
        [SerializeField] private string textoContinuar = "CONTINUAR";
        [SerializeField] private string textoNuevaPartida = "NUEVA PARTIDA";
        [SerializeField] private string textoConfirmar = "Se borrará tu partida · ENTER confirmar · ESC volver";
        [SerializeField] private string textoCrea = "CREA A TU PELEADOR";
        [SerializeField] private string textoNombre = "NOMBRE";
        [SerializeField] private string textoAlias = "ALIAS";
        [SerializeField] private string textoCara = "CARA";
        [SerializeField] private string textoRopa = "ROPA";
        [SerializeField] private string textoImplante = "IMPLANTE";
        [SerializeField] private string textoEmpezar = "EMPEZAR";
        [SerializeField] private string textoAviso = "Escribe tu nombre y tu alias";
        [SerializeField] private string textoAyuda = "↑ ↓ · TAB cambiar de fila     ← → cambiar opción     ENTER confirmar";
        [SerializeField] private string textoAyudaTitulo = "↑ ↓ elegir     ENTER confirmar";

        public Color ColorNormal => colorNormal;
        public Color ColorElegido => colorElegido;
        public Color ColorAviso => colorAviso;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoTexto => tamanoTexto;
        public int TamanoAyuda => tamanoAyuda;
        public float SegundosParpadeo => segundosParpadeo;
        public string TextoJuego => textoJuego;
        public string TextoContinuar => textoContinuar;
        public string TextoNuevaPartida => textoNuevaPartida;
        public string TextoConfirmar => textoConfirmar;
        public string TextoCrea => textoCrea;
        public string TextoNombre => textoNombre;
        public string TextoAlias => textoAlias;
        public string TextoCara => textoCara;
        public string TextoRopa => textoRopa;
        public string TextoImplante => textoImplante;
        public string TextoEmpezar => textoEmpezar;
        public string TextoAviso => textoAviso;
        public string TextoAyuda => textoAyuda;
        public string TextoAyudaTitulo => textoAyudaTitulo;
    }
}
