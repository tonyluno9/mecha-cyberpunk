namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: cómo dispara un arma a pie. Bala: rayo desde la cámara (pistola, escopeta, fusil, sniper, subfusil, revólver). Proyectil: una granada que
    /// vuela en arco y explota al chocar (lanzagranadas). Lanzada: se arroja con la mano en arco y explota (granada). Rayo: se carga manteniendo el disparo
    /// y suelta un rayo que daña todo lo que toca (rifle de plasma).
    /// </summary>
    public enum TipoDisparoArma
    {
        Bala,
        Proyectil,
        Lanzada,
        Rayo
    }
}
