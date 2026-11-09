using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Marcador temporal: se borra cuando la limpieza final quite estos objetos de Ciudad_Prologo. Los aliados ya no existen (T-930).</summary>
    public class HudAliados : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(this);
        }
    }
}
