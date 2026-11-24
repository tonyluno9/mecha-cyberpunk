namespace MechaCyberpunk.IA.APie
{
    /// <summary>T-voces-personajes-23nov: de quién es la voz grabada de un enemigo a pie (cada uno con su canal de una sola voz).</summary>
    public enum FaccionVoz
    {
        /// <summary>Sin voz grabada (civiles, perros, robots, otras pandillas): siguen con lo de antes.</summary>
        Ninguna,
        /// <summary>Pandillero común de los SKULLS (las voces de T-1623, por <see cref="VocesPandilla"/>).</summary>
        Skulls,
        /// <summary>El pandillero grandote de los SKULLS con el brazo robótico (E20_MatonSkulls).</summary>
        Maton,
        /// <summary>Jefe de los SKULLS.</summary>
        JefeSkulls,
        /// <summary>Policía a pie.</summary>
        Policia,
        /// <summary>Agente de Nova con armadura y casco (guardia, élite, guardia pesado).</summary>
        Nova,
        /// <summary>Jefe final de Nova.</summary>
        JefeNova,
    }
}
