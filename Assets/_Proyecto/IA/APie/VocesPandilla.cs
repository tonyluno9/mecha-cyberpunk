using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1623: las voces de los SKULLS (IDs SKU_* del catálogo de sonidos). Un solo canal para toda la ciudad: no hablan dos pandilleros a la vez
    /// (<see cref="ReglaVocesPandilla"/>). La variante la elige el catálogo (al azar, sin repetir la anterior). Mientras suena una voz el gestor baja un poco
    /// la música y el ambiente. Otras áreas (acoso, escenas del barrio) la usan con <see cref="Decir"/>.
    /// </summary>
    public static class VocesPandilla
    {
        public const string Ambiente = "SKU_AMBIENTE";
        public const string Provocar = "SKU_PROVOCAR";
        public const string UltimoAviso = "SKU_ULTIMO_AVISO";
        public const string Insulto = "SKU_INSULTO";
        public const string RisaBurla = "SKU_RISA_BURLA";
        public const string GritoAlerta = "SKU_GRITO_ALERTA";
        public const string Cobertura = "SKU_COBERTURA";
        public const string Recargando = "SKU_RECARGANDO";
        public const string Dolor = "SKU_DOLOR";
        public const string Quemado = "SKU_QUEMADO";
        public const string Huir = "SKU_HUIR";
        public const string Ganamos = "SKU_GANAMOS";
        public const string LoPerdimos = "SKU_LO_PERDIMOS";

        /// <summary>Más lejos que esto del jugador (m) la voz no se pide: no se oiría (el catálogo la corta a 25 m) y ocuparía la cola.</summary>
        public const float DistanciaMaxima = 28f;

        private const float AlturaBoca = 1.6f;

        /// <summary>
        /// T-1655 (T32): interruptor general de las voces y los subtítulos de los SKULLS. En false (por ahora, hasta que Tony diga qué voz va)
        /// no suena ninguna voz SKU_* ni sale ningún subtítulo ni grito de un SKULL. Para volver a prenderlas, ponlo en true.
        /// </summary>
        public static bool VocesSkullsActivas { get; set; } = false;

        private static readonly ReglaVocesPandilla Cola = new ReglaVocesPandilla();

        /// <summary>El último ID que sonó (para pruebas y depuración).</summary>
        public static string UltimoId { get; private set; }

        /// <summary>Quién lo dijo.</summary>
        public static Transform UltimoHablante { get; private set; }

        /// <summary>Cuántas voces han sonado desde que arrancó el juego.</summary>
        public static int Sonadas { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Cola.Reiniciar();
            UltimoId = null;
            UltimoHablante = null;
            Sonadas = 0;
        }

        /// <summary>
        /// Pide que <paramref name="hablante"/> diga una voz de SKULLS (<paramref name="idSku"/>, por ejemplo "SKU_INSULTO"). Devuelve true si sonó.
        /// Respeta la cola: si ya habla otro, no suena (con <paramref name="prioridad"/> puede cortar a una voz normal). Sale de la posición del hablante.
        /// </summary>
        public static bool Decir(Transform hablante, string idSku, bool prioridad = false)
        {
            if (!VocesSkullsActivas || hablante == null || string.IsNullOrEmpty(idSku))
            {
                return false;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null && (jugador.Raiz.position - hablante.position).sqrMagnitude > DistanciaMaxima * DistanciaMaxima)
            {
                return false;
            }
            if (!Cola.Intentar(Time.time, idSku, idSku == Ambiente, prioridad))
            {
                return false;
            }
            Reproducir(hablante, idSku);
            return true;
        }

        /// <summary>Depuración: suena ya, sin cola ni distancia (para comprobar el ID en Play).</summary>
        public static void Forzar(Transform hablante, string idSku)
        {
            if (!VocesSkullsActivas || hablante == null || string.IsNullOrEmpty(idSku))
            {
                return;
            }
            Cola.Intentar(Time.time, idSku, false, true);
            Reproducir(hablante, idSku);
        }

        /// <summary>Depuración: el SKULL vivo más cercano al jugador (o a la cámara) dice <paramref name="idSku"/> ya. Devuelve su nombre, o null si no hay.</summary>
        public static string ForzarMasCercano(string idSku)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 origen = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            PandilleroAPie mejor = null;
            float menor = float.MaxValue;
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p == null || !p.isActiveAndEnabled || p.EstaKO || !p.EsSkull)
                {
                    continue;
                }
                float d = (p.transform.position - origen).sqrMagnitude;
                if (d < menor)
                {
                    menor = d;
                    mejor = p;
                }
            }
            if (mejor == null)
            {
                return null;
            }
            Forzar(mejor.transform, idSku);
            return mejor.name;
        }

        /// <summary>Depuración: hace sonar un momento por su nombre ("ambiente", "ultimo_aviso", "lo_perdimos"...) con el SKULL más cercano.</summary>
        public static string ForzarMomento(string momento)
        {
            return ForzarMasCercano("SKU_" + (momento ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_'));
        }

        private static void Reproducir(Transform hablante, string idSku)
        {
            UltimoId = idSku;
            UltimoHablante = hablante;
            Sonadas++;
            Debug.Log("[VozSkulls] " + idSku + " " + hablante.name);
            Sonidos.ReproducirEn(idSku, hablante.position + Vector3.up * AlturaBoca);
        }
    }
}
