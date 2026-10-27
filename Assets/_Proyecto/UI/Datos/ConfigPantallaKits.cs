using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores y textos de la pantalla de kits de reparación de la cabina (T-148).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de kits", fileName = "ConfigPantallaKits_Nueva")]
    public class ConfigPantallaKits : ScriptableObject
    {
        [Tooltip("Texto con kits disponibles. {0} = cuántos kits quedan.")]
        [SerializeField] private string textoKits = "KITS {0} · G";

        [Tooltip("Texto mientras se repara.")]
        [SerializeField] private string textoReparando = "REPARANDO…";

        [Tooltip("Texto con 0 kits.")]
        [SerializeField] private string textoSinKits = "SIN KITS";

        [Tooltip("Texto al llegar al tope de kits de la pelea.")]
        [SerializeField] private string textoLimite = "LÍMITE DE KITS";

        [SerializeField] private Color colorNormal = new Color(0.3f, 1f, 0.6f, 1f);
        [SerializeField] private Color colorReparando = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color colorAgotado = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 1f);

        public string TextoKits => textoKits;
        public string TextoReparando => textoReparando;
        public string TextoSinKits => textoSinKits;
        public string TextoLimite => textoLimite;
        public Color ColorNormal => colorNormal;
        public Color ColorReparando => colorReparando;
        public Color ColorAgotado => colorAgotado;
        public Color ColorFondo => colorFondo;
    }
}
