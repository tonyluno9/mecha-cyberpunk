namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1324: lógica pura de apuntar con balas. Ninguna arma deja apuntar con el cargador vacío (primero recarga).</summary>
    public static class ReglaApuntarConBalas
    {
        /// <summary>True si apuntar queda bloqueado: sin balas en el cargador y sin un rayo de plasma ya en marcha.</summary>
        public static bool Bloquea(int balasEnCargador, bool rayoEnMarcha)
        {
            return balasEnCargador <= 0 && !rayoEnMarcha;
        }
    }
}
