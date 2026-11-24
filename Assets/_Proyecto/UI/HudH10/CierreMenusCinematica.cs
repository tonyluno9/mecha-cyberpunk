using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Inventario;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T90: al empezar CUALQUIER cinemática se cierran los menús y paneles abiertos: el banco de Tino (COMPRAR y MEJORAR, con su guía), los
    /// mostradores y el banco de trabajo sueltos, la clínica del Doc, las máquinas expendedoras, las dos ruedas, la ventana de aviso del tutorial,
    /// el mapa y el inventario. Al terminar la cinemática no se vuelven a abrir (el HUD sí vuelve: lo devuelven la cinemática y OcultadorHudMenus).
    /// Escucha <see cref="CinematicaEnCurso.Cambio"/> (Core), que <c>CinematicaPendiente</c> fija como lo primero al bloquear, antes de guardar
    /// el control del jugador, el cursor, las acciones prendidas y los lienzos: así cada menú devuelve el control y el cursor en el acto y la
    /// cinemática los guarda ya libres (al terminar el jugador queda con control y el cursor como en juego). Sirve igual en PC y VR (los paneles
    /// de MenusVR son los mismos menús en World Space).
    /// </summary>
    public static class CierreMenusCinematica
    {
        /// <summary>Cuántas veces cerró menús (pruebas y depuración).</summary>
        public static int Veces { get; private set; }

        /// <summary>Cuántos menús cerró la última vez.</summary>
        public static int UltimosCerrados { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Veces = 0;
            UltimosCerrados = 0;
        }

        // Después de SubsystemRegistration (ahí CinematicaEnCurso vacía su evento).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Suscribir()
        {
            CinematicaEnCurso.Cambio -= AlCambiar;
            CinematicaEnCurso.Cambio += AlCambiar;
        }

        private static void AlCambiar(bool activa)
        {
            if (activa)
            {
                CerrarTodo();
            }
        }

        /// <summary>Cierra todo lo abierto. Devuelve cuántos menús cerró.</summary>
        public static int CerrarTodo()
        {
            int cerrados = 0;
            // Primero los dueños (devuelven el control); luego las pantallas que quedaran visibles sin dueño conocido.
            if (PestanasBancoTino.CerrarPorCinematica())
            {
                cerrados++;
            }
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                cerrados += m.CerrarPorCinematica() ? 1 : 0;
            }
            foreach (Expendedora e in Object.FindObjectsByType<Expendedora>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                cerrados += e.CerrarPorCinematica() ? 1 : 0;
            }
            foreach (BancoArmas b in Object.FindObjectsByType<BancoArmas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                cerrados += b.CerrarPorCinematica() ? 1 : 0;
            }
            foreach (MostradorClinica c in Object.FindObjectsByType<MostradorClinica>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                cerrados += c.CerrarPorCinematica() ? 1 : 0;
            }
            foreach (PantallaTienda p in Object.FindObjectsByType<PantallaTienda>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.Visible)
                {
                    p.Ocultar();
                    cerrados++;
                }
            }
            foreach (PantallaMejoraArmas p in Object.FindObjectsByType<PantallaMejoraArmas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.Visible)
                {
                    p.OcultarSinLetrero();
                    cerrados++;
                }
            }
            foreach (PantallaClinica p in Object.FindObjectsByType<PantallaClinica>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.Visible)
                {
                    p.Ocultar();
                    cerrados++;
                }
            }
            if (RuedaArmasObjetos.Actual != null && RuedaArmasObjetos.Actual.CerrarSinElegir())
            {
                cerrados++;
            }
            if (RuedaHabilidadesExo.Actual != null && RuedaHabilidadesExo.Actual.CerrarSinElegir())
            {
                cerrados++;
            }
            foreach (PantallaMapa p in Object.FindObjectsByType<PantallaMapa>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.Abierto)
                {
                    p.Cerrar();
                    cerrados++;
                }
            }
            foreach (PantallaInventario p in Object.FindObjectsByType<PantallaInventario>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.Abierta)
                {
                    p.Cerrar();
                    cerrados++;
                }
            }
            if (VentanaAviso.CerrarTodas())
            {
                cerrados++;
            }
            Veces++;
            UltimosCerrados = cerrados;
            if (cerrados > 0)
            {
                Debug.Log("[UI] Cinemática: se cerraron " + cerrados + " menús abiertos.");
            }
            return cerrados;
        }
    }
}
