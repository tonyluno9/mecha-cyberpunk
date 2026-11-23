using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-78 (T-1721): los candados del mundo en el inicio guiado de la historia. Mientras estén puestos el mundo no responde al jugador:
    /// sin policía ni calor (<see cref="Policia"/>, lo lee <see cref="MisionProtegida.Activa"/>), sin pandilleros hostiles (<see cref="Pandillas"/>),
    /// sin NOVA (<see cref="Nova"/>), sin subirse a carros ajenos (<see cref="RoboCarros"/>) y sin tiendas sueltas (<see cref="Tiendas"/>, salvo
    /// <see cref="TiendaAbierta"/>). true = bloqueado. Los pone el recorrido de la historia según el paso (Progresion/Historia/Recorrido/GestorCandados);
    /// quien niega algo al jugador llama a <see cref="Negar"/> para que salga un aviso suave. Estático y sin guardado: se recalcula con el paso al cargar.
    /// </summary>
    public static class CandadosMundo
    {
        public const string TiendaDoc = "doc";
        public const string TiendaTino = "tino";
        public const string AvisoAunNo = "AÚN NO";

        public static bool Policia { get; private set; }
        public static bool Pandillas { get; private set; }
        public static bool Nova { get; private set; }
        public static bool RoboCarros { get; private set; }
        public static bool Tiendas { get; private set; }

        /// <summary>La única tienda con servicio mientras <see cref="Tiendas"/> está puesto: <see cref="TiendaDoc"/>, <see cref="TiendaTino"/> o vacío.</summary>
        public static string TiendaAbierta { get; private set; } = string.Empty;

        /// <summary>True si algún candado está puesto.</summary>
        public static bool Alguno => Policia || Pandillas || Nova || RoboCarros || Tiendas;

        /// <summary>Se avisa cada vez que cambia algún candado.</summary>
        public static event Action Cambio;

        /// <summary>Se avisa cuando algo le niega al jugador una acción por un candado (texto suave para el HUD).</summary>
        public static event Action<string> Negado;

        public static void Fijar(bool policia, bool pandillas, bool nova, bool roboCarros, bool tiendas, string tiendaAbierta = "")
        {
            tiendaAbierta = tiendaAbierta ?? string.Empty;
            bool igual = Policia == policia && Pandillas == pandillas && Nova == nova && RoboCarros == roboCarros && Tiendas == tiendas
                && TiendaAbierta == tiendaAbierta;
            Policia = policia;
            Pandillas = pandillas;
            Nova = nova;
            RoboCarros = roboCarros;
            Tiendas = tiendas;
            TiendaAbierta = tiendaAbierta;
            if (!igual)
            {
                Cambio?.Invoke();
            }
        }

        /// <summary>Quita todos los candados (mundo libre).</summary>
        public static void Liberar() => Fijar(false, false, false, false, false);

        /// <summary>True si la tienda <paramref name="id"/> puede dar servicio ahora.</summary>
        public static bool TiendaPermitida(string id) => !Tiendas || (!string.IsNullOrEmpty(id) && id == TiendaAbierta);

        /// <summary>Un candado le niega algo al jugador: sale el aviso suave (por omisión "AÚN NO").</summary>
        public static void Negar(string texto = AvisoAunNo) => Negado?.Invoke(string.IsNullOrEmpty(texto) ? AvisoAunNo : texto);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Policia = false;
            Pandillas = false;
            Nova = false;
            RoboCarros = false;
            Tiendas = false;
            TiendaAbierta = string.Empty;
            Cambio = null;
            Negado = null;
        }
    }
}
