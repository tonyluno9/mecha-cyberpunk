using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Las pantallas modales abiertas, de abajo arriba (T-410): mapa, tienda, ranuras, continuará, opciones y pausa se apilan al abrirse y
    /// se quitan al cerrarse. Esc lo atiende solo la de arriba; la pausa se abre con Esc únicamente si la pila está vacía y nadie se cerró
    /// en este mismo cuadro (así el Esc que cierra el mapa no abre también la pausa).
    /// </summary>
    public static class PilaPantallas
    {
        private static readonly List<Object> pila = new List<Object>();
        private static int cuadroCambio = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            pila.Clear();
            cuadroCambio = -1;
        }

        public static int Cantidad
        {
            get
            {
                QuitarDestruidas();
                return pila.Count;
            }
        }

        public static bool Vacia => Cantidad == 0;

        /// <summary>La pantalla de arriba, o null.</summary>
        public static Object Arriba => Cantidad > 0 ? pila[pila.Count - 1] : null;

        /// <summary>True si algo se abrió o se cerró en este cuadro (la tecla que lo hizo ya se usó).</summary>
        public static bool CambioEsteCuadro => cuadroCambio == Time.frameCount;

        public static bool EsArriba(Object pantalla) => pantalla != null && Arriba == pantalla;

        public static bool Contiene(Object pantalla) => pantalla != null && pila.Contains(pantalla);

        /// <summary>La pone arriba (si ya estaba, la sube).</summary>
        public static void Abrir(Object pantalla)
        {
            if (pantalla == null)
            {
                return;
            }
            pila.Remove(pantalla);
            pila.Add(pantalla);
            cuadroCambio = Time.frameCount;
        }

        public static void Cerrar(Object pantalla)
        {
            if (pila.Remove(pantalla))
            {
                cuadroCambio = Time.frameCount;
            }
        }

        // Una pantalla destruida sin cerrarse (cambio de escena) no debe tapar Esc para siempre.
        private static void QuitarDestruidas()
        {
            for (int i = pila.Count - 1; i >= 0; i--)
            {
                if (pila[i] == null)
                {
                    pila.RemoveAt(i);
                }
            }
        }
    }
}
