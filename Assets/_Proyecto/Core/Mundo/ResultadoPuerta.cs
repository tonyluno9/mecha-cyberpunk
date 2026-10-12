namespace MechaCyberpunk.Core
{
    /// <summary>Respuesta de <see cref="IPuerta.IntentarAbrir"/>.</summary>
    public enum ResultadoPuerta
    {
        /// <summary>La puerta estaba cerrada y ahora se abrió.</summary>
        Abierta,
        /// <summary>La puerta ya estaba abierta; no cambió nada.</summary>
        YaAbierta,
        /// <summary>No se abrió porque el lugar está cerrado por su horario.</summary>
        CerradaPorHorario,
        /// <summary>No se abrió por un cierre que no depende de la hora (por ejemplo, cerrado en la demo).</summary>
        Bloqueada
    }
}
