using MechaCyberpunk.Combate.Lectura;
using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Textos, colores y tiempos de la pantalla de adaptación de la cabina (T-203, D-tony-198): lo que el rival aprendió del jugador,
    /// el parpadeo cuando lo usa y el aviso de fin de pelea con la pista.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de adaptación", fileName = "ConfigPantallaAdaptacion_Nueva")]
    public class ConfigPantallaAdaptacion : ScriptableObject
    {
        private const int TiposAdaptacion = 3;

        [Header("Qué rival se lee")]
        [Tooltip("Distancia máxima al rival que se lee (m).")]
        [Min(1f)] [SerializeField] private float distanciaMaxima = 60f;
        [Tooltip("Cada cuánto se vuelve a elegir al rival más cercano (s).")]
        [Min(0.05f)] [SerializeField] private float segundosReeleccion = 0.5f;

        [Header("Tiempos")]
        [Tooltip("Cuánto parpadea la fila cuando el rival usa su adaptación (s).")]
        [Min(0.1f)] [SerializeField] private float segundosParpadeo = 0.6f;
        [Tooltip("Cuántos cambios de color hace el parpadeo por segundo (veces).")]
        [Min(1f)] [SerializeField] private float parpadeosPorSegundo = 6f;
        [Tooltip("Cuánto se queda el aviso de fin de pelea si no empieza otra (s).")]
        [Min(1f)] [SerializeField] private float segundosAvisoFin = 8f;

        [Header("Textos")]
        [SerializeField] private string titulo = "TE TIENE ESTUDIADO";
        [Tooltip("Una fila activa. {0} = nombre, {1} = nivel en romano, {2} = hábito.")]
        [SerializeField] private string formatoFila = "{0} {1} · {2}";
        [Tooltip("Línea de fin de pelea cuando subió un nivel. {0} = nombre, {1} = nivel nuevo en romano.")]
        [SerializeField] private string formatoAprendio = "APRENDIÓ: {0} {1}";
        [Tooltip("Línea de fin de pelea cuando bajó un nivel. {0} = nombre.")]
        [SerializeField] private string formatoOlvido = "OLVIDÓ: {0}";
        [Tooltip("Niveles en romanos: el primero es el nivel 1.")]
        [SerializeField] private string[] niveles = { "I", "II", "III" };
        [Tooltip("Nombre por TipoAdaptacion (Guardia, Fintas, Retirada).")]
        [SerializeField] private string[] nombres = { "GUARDIA", "FINTAS", "RETIRADA" };
        [Tooltip("Hábito del jugador que leyó, por TipoAdaptacion.")]
        [SerializeField] private string[] habitos = { "TIRAS MUCHOS PUÑOS", "TE CUBRES MUCHO", "LE PEGAS CUANDO FALLA" };
        [Tooltip("Pista para cambiar de estilo, por TipoAdaptacion.")]
        [SerializeField] private string[] pistas = { "PÉGALE JUSTO DESPUÉS DE SU GOLPE", "NO TE ESCONDAS: ATACA TÚ", "PÉGALE RÁPIDO O ATACA PRIMERO" };

        [Header("Colores (nunca rojo puro, ni el naranja del aviso de OJOS, ni su turquesa)")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.03f, 0.08f, 0.95f);
        [Tooltip("Título y filas (azul neón).")]
        [SerializeField] private Color colorBase = new Color(0.25f, 0.6f, 1f, 1f);
        [Tooltip("La fila que parpadea cuando el rival usa la adaptación (magenta).")]
        [SerializeField] private Color colorParpadeo = new Color(1f, 0.25f, 0.85f, 1f);
        [Tooltip("Líneas de fin de pelea.")]
        [SerializeField] private Color colorFin = new Color(0.85f, 0.93f, 1f, 1f);
        [Tooltip("La pista del final.")]
        [SerializeField] private Color colorPista = new Color(0.75f, 0.55f, 1f, 1f);

        public float DistanciaMaxima => distanciaMaxima;
        public float SegundosReeleccion => segundosReeleccion;
        public float SegundosParpadeo => segundosParpadeo;
        public float ParpadeosPorSegundo => parpadeosPorSegundo;
        public float SegundosAvisoFin => segundosAvisoFin;
        public string Titulo => titulo;
        public string FormatoFila => formatoFila;
        public string FormatoAprendio => formatoAprendio;
        public string FormatoOlvido => formatoOlvido;
        public Color ColorFondo => colorFondo;
        public Color ColorBase => colorBase;
        public Color ColorParpadeo => colorParpadeo;
        public Color ColorFin => colorFin;
        public Color ColorPista => colorPista;

        public string Nombre(TipoAdaptacion tipo) => Elegir(nombres, (int)tipo, tipo.ToString().ToUpperInvariant());
        public string Habito(TipoAdaptacion tipo) => Elegir(habitos, (int)tipo, string.Empty);
        public string Pista(TipoAdaptacion tipo) => Elegir(pistas, (int)tipo, string.Empty);

        /// <summary>El nivel en romano (1 = "I"); si no hay texto para ese nivel, el número.</summary>
        public string Nivel(int nivel) => Elegir(niveles, nivel - 1, nivel.ToString());

        private static string Elegir(string[] lista, int indice, string respaldo)
        {
            return lista != null && indice >= 0 && indice < lista.Length && !string.IsNullOrEmpty(lista[indice]) ? lista[indice] : respaldo;
        }

        private void OnValidate()
        {
            Ajustar(ref nombres);
            Ajustar(ref habitos);
            Ajustar(ref pistas);
        }

        private static void Ajustar(ref string[] lista)
        {
            if (lista == null || lista.Length != TiposAdaptacion)
            {
                System.Array.Resize(ref lista, TiposAdaptacion);
            }
        }
    }
}
