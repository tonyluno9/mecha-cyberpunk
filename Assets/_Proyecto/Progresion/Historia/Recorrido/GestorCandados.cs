using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-78 (T-1721): qué candados del mundo (<see cref="CandadosMundo"/>) lleva cada paso de la historia, y el orden real de los pasos
    /// (<see cref="PasoRecorrido.UsarProtesis"/> va entre Suero y Armas aunque su número sea 21). Lógica pura y probable.
    /// Todo bloqueado de Recuerdo a la chica; la tienda que sí sirve es la del paso (el Doc en Dinero y Suero, Tino en Armas).
    /// Mundo libre desde GuerraSkulls (cuando sale la ventana "YA ERES LIBRE...").
    /// </summary>
    public static class GestorCandados
    {
        public const string TextoLibre = "YA ERES LIBRE DE RECORRER CYBER CITY";
        public const string AvisoDoc = "VE CON EL DOC";
        public const string AvisoSinArma = "COMPRA UN ARMA";
        public const string AvisoSinMejora = "PONLE UNA MEJORA";

        /// <summary>A esta distancia (m) del Doc, alejándose, sale el aviso suave de "VE CON EL DOC".</summary>
        public const float MetrosLejosDelDoc = 70f;

        public readonly struct Candados
        {
            public readonly bool Policia;
            public readonly bool Pandillas;
            public readonly bool Nova;
            public readonly bool RoboCarros;
            public readonly bool Tiendas;
            public readonly string TiendaAbierta;

            public Candados(bool todo, string tiendaAbierta, bool roboCarros)
            {
                Policia = todo;
                Pandillas = todo;
                Nova = todo;
                RoboCarros = roboCarros;
                Tiendas = todo;
                TiendaAbierta = tiendaAbierta;
            }

            public bool Alguno => Policia || Pandillas || Nova || RoboCarros || Tiendas;
        }

        /// <summary>Los candados que lleva el paso.</summary>
        public static Candados CandadosPara(PasoRecorrido paso)
        {
            switch (paso)
            {
                case PasoRecorrido.Recuerdo:
                case PasoRecorrido.Despierta:
                case PasoRecorrido.Molestan:
                case PasoRecorrido.Maletin:
                case PasoRecorrido.Ranura:
                case PasoRecorrido.Decision:
                case PasoRecorrido.PeleaSkulls: // VR24: la pelea del inicio, como el resto del inicio guiado
                    return new Candados(true, string.Empty, true); // empujón y cinemáticas: tampoco carros
                // T-83: desde que decide quedarse (el dinero) ya puede usar carros; lo demás sigue bloqueado hasta el mundo libre.
                case PasoRecorrido.UsarProtesis:
                case PasoRecorrido.Chica:
                    return new Candados(true, string.Empty, false);
                case PasoRecorrido.Dinero:
                case PasoRecorrido.Suero:
                    return new Candados(true, CandadosMundo.TiendaDoc, false);
                case PasoRecorrido.Armas:
                case PasoRecorrido.Inventor:
                    return new Candados(true, CandadosMundo.TiendaTino, false);
                default:
                    return new Candados(false, string.Empty, false); // Ninguno (recorrido viejo), GuerraSkulls en adelante y Libre
            }
        }

        /// <summary>Pone en <see cref="CandadosMundo"/> los candados del paso.</summary>
        public static void Aplicar(PasoRecorrido paso)
        {
            Candados c = CandadosPara(paso);
            CandadosMundo.Fijar(c.Policia, c.Pandillas, c.Nova, c.RoboCarros, c.Tiendas, c.TiendaAbierta);
        }

        /// <summary>El paso que sigue en la historia (F8 y pruebas). UsarProtesis queda entre Suero y Armas.</summary>
        public static PasoRecorrido Siguiente(PasoRecorrido paso)
        {
            switch (paso)
            {
                case PasoRecorrido.Suero: return PasoRecorrido.UsarProtesis;
                case PasoRecorrido.UsarProtesis: return PasoRecorrido.Armas;
                // VR24: ya no se camina a la ranura; tras el maletín va C03 y luego la pelea con los 3 SKULLS.
                case PasoRecorrido.Maletin: return PasoRecorrido.Decision;
                case PasoRecorrido.Ranura: return PasoRecorrido.Decision;
                case PasoRecorrido.Decision: return PasoRecorrido.PeleaSkulls;
                case PasoRecorrido.PeleaSkulls: return PasoRecorrido.Dinero;
                default: return paso + 1;
            }
        }

        /// <summary>
        /// VR16: true si el paso no se juega con visor. VR24: ahora es igual en PC y en VR (<paramref name="vr"/> ya no cuenta): ver <see cref="SeSalta"/>.
        /// </summary>
        public static bool SeSaltaEnVR(PasoRecorrido paso, bool vr) => SeSalta(paso);

        /// <summary>
        /// VR24: los pasos que ya no se juegan (en PC ni en VR): "Lo molestan" (el empujón) se da por hecho y sigue al Maletín; "Ranura" (caminar a la
        /// ranura del muro) se quitó y una partida guardada ahí sigue en <see cref="PasoRecorrido.Decision"/>.
        /// </summary>
        public static bool SeSalta(PasoRecorrido paso) => paso == PasoRecorrido.Molestan || paso == PasoRecorrido.Ranura;

        /// <summary>VR24: el paso al que se va en vez de <paramref name="paso"/> si este ya no se juega (<see cref="SeSalta"/>); si se juega, el mismo.</summary>
        public static PasoRecorrido Reemplazo(PasoRecorrido paso)
        {
            switch (paso)
            {
                case PasoRecorrido.Molestan: return PasoRecorrido.Maletin;
                case PasoRecorrido.Ranura: return PasoRecorrido.Decision;
                default: return paso;
            }
        }

        /// <summary>
        /// T-historia-libre-23nov: los pasos de la historia vieja que quedan desconectados (no borrados) desde el modo libre: del Inventor al Fin.
        /// Una partida guardada en uno de ellos sigue en <see cref="PasoRecorrido.Libre"/>.
        /// </summary>
        public static bool DesconectadoPorModoLibre(PasoRecorrido paso) => paso >= PasoRecorrido.Inventor && paso <= PasoRecorrido.Fin;

        /// <summary>True si la distancia al Doc ya es grande y el jugador sigue alejándose (aviso suave, sin paredes).</summary>
        public static bool DebeAvisarLejosDelDoc(float distancia, float distanciaAntes) => distancia > MetrosLejosDelDoc && distancia > distanciaAntes + 0.1f;

        /// <summary>El paso UsarProtesis termina cuando las piernas están activas y el jugador corre de verdad.</summary>
        public static bool UsoPiernasCompleto(bool piernasActivas, bool pidiendoCorrer, float rapidez) => piernasActivas && pidiendoCorrer && rapidez > 0.5f;

        /// <summary>Lo que falta en el banco de Tino en el paso Armas: vacío si ya hay arma y mejora (se puede salir).</summary>
        public static string FaltaEnElBanco(bool tieneArma, bool tieneMejora)
        {
            if (!tieneArma)
            {
                return AvisoSinArma;
            }
            return tieneMejora ? string.Empty : AvisoSinMejora;
        }
    }
}
