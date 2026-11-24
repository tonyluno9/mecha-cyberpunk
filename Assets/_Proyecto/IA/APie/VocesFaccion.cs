using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-voces-personajes-23nov: las voces grabadas del matón y el jefe de los SKULLS, la policía a pie y los agentes y el jefe de Nova. Cada facción tiene
    /// un solo canal (<see cref="ReglaVocesPandilla"/>): nunca hablan dos de la misma a la vez, y la plática, la provocación y las burlas esperan además un
    /// silencio de unos segundos. La variante (_01, _02...) la elige el catálogo al azar sin repetir la anterior. Los SKULLS comunes siguen en
    /// <see cref="VocesPandilla"/> (su propio canal); aquí solo se traduce el momento a su ID.
    /// </summary>
    public static class VocesFaccion
    {
        public const string SkuMuerte = "SKU_MUERTE";

        public const string MatonProvocar = "SKU_MATON_PROVOCAR";
        public const string MatonAtaque = "SKU_MATON_ATAQUE";
        public const string MatonRisa = "SKU_MATON_RISA";
        public const string MatonDolor = "SKU_MATON_DOLOR";
        public const string MatonMuerte = "SKU_MATON_MUERTE";

        public const string JefeSkullsEnfrentamiento = "SKU_JEFE_ENFRENTAMIENTO";
        public const string JefeSkullsDolor = "SKU_JEFE_DOLOR";
        public const string JefeSkullsDerrota = "SKU_JEFE_DERROTA";

        public const string PolAlto = "POL_VOZ_ALTO";
        public const string PolRendirse = "POL_VOZ_RENDIRSE";
        public const string PolRefuerzos = "POL_VOZ_REFUERZOS";
        public const string PolOficialCaido = "POL_VOZ_OFICIAL_CAIDO";
        public const string PolLoPerdimos = "POL_VOZ_LO_PERDIMOS";
        public const string PolDolor = "POL_VOZ_DOLOR";
        public const string PolMuerte = "POL_VOZ_MUERTE";

        public const string NovAlerta = "NOV_VOZ_ALERTA";
        public const string NovOrden = "NOV_VOZ_ORDEN";
        public const string NovCubrirse = "NOV_VOZ_CUBRIRSE";
        public const string NovLoPerdimos = "NOV_VOZ_LO_PERDIMOS";
        public const string NovDolor = "NOV_VOZ_DOLOR";
        public const string NovMuerte = "NOV_VOZ_MUERTE";

        public const string JefeNovaFrase = "NOV_JEFE_FRASE";
        public const string JefeNovaAtaque = "NOV_JEFE_ATAQUE";
        public const string JefeNovaMuerte = "NOV_JEFE_MUERTE";

        /// <summary>Más lejos que esto del jugador (m) no se pide la voz: no se oiría y ocuparía el canal.</summary>
        public const float DistanciaMaxima = 40f;

        private const float AlturaBoca = 1.6f;

        private static readonly ReglaVocesPandilla[] Colas = CrearColas();

        /// <summary>El último ID que sonó por aquí (para pruebas y depuración).</summary>
        public static string UltimoId { get; private set; }

        private static ReglaVocesPandilla[] CrearColas()
        {
            ReglaVocesPandilla[] colas = new ReglaVocesPandilla[System.Enum.GetValues(typeof(FaccionVoz)).Length];
            for (int i = 0; i < colas.Length; i++) colas[i] = new ReglaVocesPandilla();
            return colas;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            foreach (ReglaVocesPandilla c in Colas) c.Reiniciar();
            UltimoId = null;
        }

        /// <summary>Si el momento cuenta como plática o burla (espera un silencio extra en el canal).</summary>
        public static bool EsAmbiental(EventoVoz evento) => evento == EventoVoz.Ambiente || evento == EventoVoz.Provocar || evento == EventoVoz.Burla;

        /// <summary>El ID de voz de esa facción en ese momento ("" si no tiene: se queda callado).</summary>
        public static string Id(FaccionVoz faccion, EventoVoz evento)
        {
            switch (faccion)
            {
                case FaccionVoz.Skulls: return IdSkulls(evento);
                case FaccionVoz.Maton:
                    switch (evento)
                    {
                        case EventoVoz.Provocar:
                        case EventoVoz.UltimoAviso:
                        case EventoVoz.Alerta:
                        case EventoVoz.InicioCombate: return MatonProvocar;
                        case EventoVoz.Ataque: return MatonAtaque;
                        case EventoVoz.Burla:
                        case EventoVoz.Ganamos: return MatonRisa;
                        case EventoVoz.Dolor:
                        case EventoVoz.Quemado: return MatonDolor;
                        case EventoVoz.Muerte: return MatonMuerte;
                    }
                    break;
                case FaccionVoz.JefeSkulls:
                    switch (evento)
                    {
                        case EventoVoz.Alerta:
                        case EventoVoz.InicioCombate: return JefeSkullsEnfrentamiento;
                        case EventoVoz.Dolor:
                        case EventoVoz.Quemado: return JefeSkullsDolor;
                        case EventoVoz.Muerte: return JefeSkullsDerrota;
                    }
                    break;
                case FaccionVoz.Policia:
                    switch (evento)
                    {
                        case EventoVoz.Alerta:
                        case EventoVoz.InicioCombate: return PolAlto;
                        case EventoVoz.UltimoAviso:
                        case EventoVoz.Burla:
                        case EventoVoz.Ataque: return PolRendirse;
                        case EventoVoz.Cobertura: return PolRefuerzos;
                        case EventoVoz.Dolor:
                        case EventoVoz.Quemado: return PolDolor;
                        case EventoVoz.Muerte: return PolMuerte;
                        case EventoVoz.LoPerdimos: return PolLoPerdimos;
                        case EventoVoz.OficialCaido: return PolOficialCaido;
                    }
                    break;
                case FaccionVoz.Nova:
                    switch (evento)
                    {
                        case EventoVoz.Alerta: return NovAlerta;
                        case EventoVoz.InicioCombate:
                        case EventoVoz.Burla: return NovOrden;
                        case EventoVoz.Escudo: return NovCubrirse;
                        case EventoVoz.Dolor:
                        case EventoVoz.Quemado: return NovDolor;
                        case EventoVoz.Muerte: return NovMuerte;
                        case EventoVoz.LoPerdimos: return NovLoPerdimos;
                    }
                    break;
                case FaccionVoz.JefeNova:
                    switch (evento)
                    {
                        case EventoVoz.InicioCombate:
                        case EventoVoz.Escudo: return JefeNovaFrase;
                        case EventoVoz.Ataque: return JefeNovaAtaque;
                        case EventoVoz.Muerte: return JefeNovaMuerte;
                    }
                    break;
            }
            return string.Empty;
        }

        private static string IdSkulls(EventoVoz evento)
        {
            switch (evento)
            {
                case EventoVoz.Ambiente: return VocesPandilla.Ambiente;
                case EventoVoz.Provocar: return VocesPandilla.Provocar;
                case EventoVoz.UltimoAviso: return VocesPandilla.UltimoAviso;
                case EventoVoz.Burla:
                case EventoVoz.Ataque: return VocesPandilla.Insulto;
                case EventoVoz.Alerta:
                case EventoVoz.InicioCombate: return VocesPandilla.GritoAlerta;
                case EventoVoz.Cobertura: return VocesPandilla.Cobertura;
                case EventoVoz.Recargando: return VocesPandilla.Recargando;
                case EventoVoz.Dolor: return VocesPandilla.Dolor;
                case EventoVoz.Quemado: return VocesPandilla.Quemado;
                case EventoVoz.Muerte: return SkuMuerte;
                case EventoVoz.Huir: return VocesPandilla.Huir;
                case EventoVoz.Ganamos: return VocesPandilla.Ganamos;
                case EventoVoz.LoPerdimos: return VocesPandilla.LoPerdimos;
                default: return string.Empty;
            }
        }

        /// <summary>El momento al que corresponde un ID de los SKULLS comunes (los puntos del código que ya pedían SKU_* sirven a todas las facciones).</summary>
        public static EventoVoz EventoDeSkull(string idSku)
        {
            switch (idSku)
            {
                case VocesPandilla.Ambiente: return EventoVoz.Ambiente;
                case VocesPandilla.Provocar: return EventoVoz.Provocar;
                case VocesPandilla.UltimoAviso: return EventoVoz.UltimoAviso;
                case VocesPandilla.Insulto:
                case VocesPandilla.RisaBurla: return EventoVoz.Burla;
                case VocesPandilla.GritoAlerta: return EventoVoz.Alerta;
                case VocesPandilla.Cobertura: return EventoVoz.Cobertura;
                case VocesPandilla.Recargando: return EventoVoz.Recargando;
                case VocesPandilla.Dolor: return EventoVoz.Dolor;
                case VocesPandilla.Quemado: return EventoVoz.Quemado;
                case SkuMuerte: return EventoVoz.Muerte;
                case VocesPandilla.Huir: return EventoVoz.Huir;
                case VocesPandilla.Ganamos: return EventoVoz.Ganamos;
                case VocesPandilla.LoPerdimos: return EventoVoz.LoPerdimos;
                default: return EventoVoz.Ambiente;
            }
        }

        /// <summary>
        /// <paramref name="hablante"/> dice la voz de su facción para <paramref name="evento"/>. Devuelve true si sonó. Respeta el canal de la facción (con
        /// <paramref name="prioridad"/> corta a una voz normal) y no se pide lejos del jugador. Los SKULLS comunes van por <see cref="VocesPandilla.Decir"/>.
        /// </summary>
        public static bool Decir(FaccionVoz faccion, Transform hablante, EventoVoz evento, bool prioridad = false)
        {
            string id = Id(faccion, evento);
            if (hablante == null || id.Length == 0)
            {
                return false;
            }
            if (faccion == FaccionVoz.Skulls)
            {
                return VocesPandilla.Decir(hablante, id, prioridad);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null && (jugador.Raiz.position - hablante.position).sqrMagnitude > DistanciaMaxima * DistanciaMaxima)
            {
                return false;
            }
            if (!Colas[(int)faccion].Intentar(Time.time, id, EsAmbiental(evento), prioridad))
            {
                return false;
            }
            UltimoId = id;
            Sonidos.ReproducirEn(id, hablante.position + Vector3.up * AlturaBoca);
            return true;
        }
    }
}
