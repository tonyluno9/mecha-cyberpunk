namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>Los consejos que salen en la pantalla de carga (texto de interfaz, no de balance).</summary>
    public static class ConsejosCarga
    {
        public static readonly string[] Textos =
        {
            "Pulsa E junto a puertas, mostradores y personas para interactuar.",
            "Mantén SHIFT para correr y ESPACIO para saltar.",
            "Apunta con el botón derecho del ratón antes de disparar.",
            "Recarga con R antes de entrar a una pelea larga.",
            "Abre el mapa con M y marca tu destino para seguir el GPS.",
            "Saca el teléfono con T para ver tus contactos.",
            "ESC abre la pausa: ahí guardas la partida y ajustas las opciones.",
            "Mantén TAB para elegir un arma o usar un objeto; la rueda del ratón también cambia de arma.",
            "Con implantes, las teclas 1, 2 y 3 activan las habilidades de piernas, brazos y cabeza.",
            "Lo que pasa en la calle se nota: la policía se fija en lo que haces.",
            "Cada pandilla manda en su zona. Mira los colores del mapa.",
        };

        public static string Obtener(int indice)
        {
            return Textos.Length == 0 ? string.Empty : Textos[((indice % Textos.Length) + Textos.Length) % Textos.Length];
        }
    }
}
