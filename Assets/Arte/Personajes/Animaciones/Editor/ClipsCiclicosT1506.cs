namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1506: qué clips de Blender del 19-nov (NPC, Nova, PJ01 no locomoción) van en bucle y cuáles son aditivos (capa de daño).
    /// Los nombres son los del archivo sin el prefijo PJ01_ (ver ImportadorClipsPJ01.NombreBase); los de NPC, E15 a E18 y Guardia traen su prefijo completo.
    /// </summary>
    public static class ClipsCiclicosT1506
    {
        /// <summary>Clips que se repiten (bucle), según la bitácora de Blender (columna Loop).</summary>
        public static readonly string[] ConBucle =
        {
            "CaminarApuntando_Adelante", "CaminarApuntando_Atras", "CaminarApuntando_Derecha", "CaminarApuntando_Izquierda",
            "Cobertura_Quieto", "Manejar_Carro", "Manejar_Moto",
            "NPC_Caminar_Hombre", "NPC_Caminar_Mujer", "NPC_Caminar_Mayor_Baston", "NPC_Caminar_Celular", "NPC_Esperar", "NPC_Platicar_1", "NPC_Platicar_2",
            "NPC_Sentado_Banca", "NPC_Huir_Corriendo", "NPC_Agachado_CubriendoCabeza", "NPC_Mesera_Charola", "NPC_Hacker_Tecleando",
            "E15_Caminar", "E15_Correr", "E15_Disparar", "E15_Cubrirse", "E16_Caminar", "E18_Caida_Aire", "E18_Jetpack_Volar", "E18_Disparar",
            "Guardia_Caida_Aire", "Guardia_Caminar", "Guardia_Disparar",
            "E17_Caminar", "E17_Correr", "E17_Ladrar",
        };

        /// <summary>Clips de reacción al daño que se juegan en la capa aditiva (primer cuadro = pose de referencia), igual que Pistola_Dano_*.</summary>
        public static readonly string[] Aditivos =
        {
            "Dano_Frente", "Dano_Espalda", "Dano_Izquierda", "Dano_Derecha", "NPC_Balazo_Frente", "NPC_Balazo_Espalda", "E15_Impacto", "E16_Impacto",
        };

        public static bool EsCiclico(string nombreBase) => System.Array.IndexOf(ConBucle, nombreBase) >= 0;

        public static bool EsAditivo(string nombreBase) => System.Array.IndexOf(Aditivos, nombreBase) >= 0;
    }
}
