using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-voces-23nov: la voz del Doc en su clínica (2D): saluda al entrar, se despide al salir, explica al instalar un implante, pide que te sientes y
    /// te pone el suero en la silla, y te habla al revivir tras caer. Entrar y salir los escucha solo (EventosLugares con IdLugar.Clinica); lo demás lo
    /// piden la silla, el servicio de implantes y el doctor. Un enfriamiento evita que saludo y despedida se repitan al cruzar la puerta varias veces.
    /// </summary>
    public static class VocesDoc
    {
        public const string Saludo = "DOC_VOZ_SALUDO";
        public const string Sientate = "DOC_VOZ_SIENTATE";
        public const string Explicar = "DOC_VOZ_EXPLICAR";
        public const string Revivir = "DOC_VOZ_REVIVIR";
        public const string Suero = "DOC_VOZ_SUERO";
        public const string Despedida = "DOC_VOZ_DESPEDIDA";

        /// <summary>Segundos mínimos entre dos saludos (o dos despedidas) del Doc.</summary>
        public const float SegundosEntrePuerta = 20f;

        private static float ultimoSaludo = float.NegativeInfinity;
        private static float ultimaDespedida = float.NegativeInfinity;

        /// <summary>El último ID que pidió el Doc (para pruebas y depuración).</summary>
        public static string UltimoId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ultimoSaludo = float.NegativeInfinity;
            ultimaDespedida = float.NegativeInfinity;
            UltimoId = null;
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Suscribir()
        {
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
        }

        /// <summary>Si toca saludar o despedirse: pasó el enfriamiento desde la última vez.</summary>
        public static bool TocaPuerta(float ahora, float ultimo) => ahora - ultimo >= SegundosEntrePuerta;

        private static void AlEntrar(IdLugar lugar)
        {
            if (lugar != IdLugar.Clinica || !TocaPuerta(Time.time, ultimoSaludo)) return;
            ultimoSaludo = Time.time;
            Decir(Saludo);
        }

        private static void AlSalir(IdLugar lugar)
        {
            if (lugar != IdLugar.Clinica || !TocaPuerta(Time.time, ultimaDespedida)) return;
            ultimaDespedida = Time.time;
            Decir(Despedida);
        }

        /// <summary>El Doc dice <paramref name="id"/> en 2D (su voz no lleva distancia).</summary>
        public static void Decir(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            UltimoId = id;
            Sonidos.Reproducir(id);
        }
    }
}
