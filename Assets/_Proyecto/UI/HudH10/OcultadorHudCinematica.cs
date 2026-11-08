using System.Collections.Generic;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-726: mientras dura una cinemática (<see cref="HudCinematica"/>) apaga los lienzos del HUD (HudAPie con HudH10, avisos y marcadores; el minimapa;
    /// el marcador del objetivo) cada cuadro, después de que los demás scripts hayan decidido, y al terminar los vuelve a encender.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public class OcultadorHudCinematica : MonoBehaviour
    {
        private readonly List<Canvas> ocultos = new List<Canvas>();

        /// <summary>Cuántos lienzos del HUD tiene apagados ahora.</summary>
        public int Ocultos => ocultos.Count;

        private void LateUpdate()
        {
            Canvas[] lienzos = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lienzos.Length; i++)
            {
                Canvas c = lienzos[i];
                if (c != null && c.enabled && EsDelHud(c))
                {
                    ocultos.Add(c);
                    c.enabled = false;
                }
            }
            for (int i = 0; i < ocultos.Count; i++)
            {
                if (ocultos[i] != null && ocultos[i].enabled)
                {
                    ocultos[i].enabled = false;
                }
            }
        }

        /// <summary>Vuelve a encender lo que apagó.</summary>
        public void Restaurar()
        {
            for (int i = 0; i < ocultos.Count; i++)
            {
                if (ocultos[i] != null)
                {
                    ocultos[i].enabled = true;
                }
            }
            ocultos.Clear();
        }

        private void OnDestroy() => HudCinematica.AlDestruirseElOcultador(this);

        private static bool EsDelHud(Canvas lienzo)
        {
            return lienzo.GetComponentInParent<HudAPie>() != null
                || lienzo.GetComponentInParent<Minimapa>() != null
                || lienzo.GetComponentInParent<MarcadorObjetivo>() != null;
        }
    }
}
