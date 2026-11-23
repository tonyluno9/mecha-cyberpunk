namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10): los 19 pasos de la historia nueva (tabla 10.2 de docs/tarea-historia-en-juego.md), en orden, más el juego libre.
    /// El valor numérico es el número del paso. Se guarda por nombre, así que se pueden agregar pasos sin romper partidas.
    /// </summary>
    public enum PasoRecorrido
    {
        Ninguno = 0,
        Recuerdo = 1,
        Despierta = 2,
        Molestan = 3,
        Maletin = 4,
        Ranura = 5,
        Decision = 6,
        Dinero = 7,
        Suero = 8,
        Armas = 9,
        Inventor = 10,
        Chica = 11,
        GuerraSkulls = 12,
        GuerraSnakes = 13,
        Hacker = 14,
        Robots = 15,
        Hermano = 16,
        JefeFinal = 17,
        Puertas = 18,
        Fin = 19,
        Libre = 20,
        /// <summary>T-78: tras instalar las piernas del Doc, usarlas (Tab, PIERNAS, correr). Va entre Suero y Armas; el número es 21 para no mover los pasos guardados y los saltos de las pruebas (el orden lo da <see cref="GestorCandados.Siguiente"/>).</summary>
        UsarProtesis = 21
    }
}
