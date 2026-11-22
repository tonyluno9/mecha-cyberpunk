namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T15): lo que pasa al decir una línea.
    /// Empujar: un agresor empuja a la víctima. Tumbar: la víctima cae. Quitar: un agresor le quita algo (gesto de recoger).
    /// Patear: un agresor patea (golpe). Suplicar: la víctima se asusta con las manos arriba. Levantar: la víctima se levanta.
    /// Burla y Telefono los hace quien habla.
    /// </summary>
    public enum GestoBarrio
    {
        Ninguno = 0,
        Burla = 1,
        Empujar = 2,
        Tumbar = 3,
        Quitar = 4,
        Patear = 5,
        Suplicar = 6,
        Levantar = 7,
        Telefono = 8
    }
}
