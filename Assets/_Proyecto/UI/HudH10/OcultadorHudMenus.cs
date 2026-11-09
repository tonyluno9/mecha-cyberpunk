using System.Collections.Generic;
using MechaCyberpunk.UI.Kit;
using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Mientras haya una pantalla de menú abierta (<see cref="PilaPantallas"/>) apaga los lienzos del HUD para que no se crucen con los títulos,
    /// y al cerrarlas todas los vuelve a encender. Corre antes que <see cref="OcultadorHudCinematica"/>: si hay una cinemática, esa decide al final.
    /// </summary>
    [DefaultExecutionOrder(31999)]
    public class OcultadorHudMenus : MonoBehaviour
    {
        private readonly List<Canvas> ocultos = new List<Canvas>();
        private bool ocultando;

        /// <summary>Cuántos lienzos del HUD tiene apagados ahora.</summary>
        public int Ocultos => ocultos.Count;

        private void LateUpdate()
        {
            bool hayMenu = !PilaPantallas.Vacia;
            if (hayMenu && !ocultando)
            {
                Reunir();
                ocultando = true;
            }
            else if (!hayMenu && ocultando)
            {
                Restaurar();
                ocultando = false;
            }
            for (int i = 0; ocultando && i < ocultos.Count; i++)
            {
                if (ocultos[i] != null)
                {
                    ocultos[i].enabled = false;
                }
            }
        }

        private void OnDisable()
        {
            if (ocultando)
            {
                Restaurar();
                ocultando = false;
            }
        }

        private void Reunir()
        {
            ocultos.Clear();
            foreach (Canvas lienzo in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (lienzo.enabled && OcultadorHudCinematica.EsDelHud(lienzo))
                {
                    ocultos.Add(lienzo);
                }
            }
        }

        private void Restaurar()
        {
            foreach (Canvas lienzo in ocultos)
            {
                if (lienzo != null)
                {
                    lienzo.enabled = true;
                }
            }
            ocultos.Clear();
        }
    }
}
