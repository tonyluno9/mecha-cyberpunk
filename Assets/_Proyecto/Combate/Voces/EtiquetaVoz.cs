namespace MechaCyberpunk.Combate.Voces
{
    /// <summary>
    /// Arma el ID de sonido con la etiqueta de voz (T-597): "JUG_DANIO" + femenina = "JUG_DANIO@F". El gestor de sonidos (Progresion/Sonido,
    /// ReglaVozGenero) busca primero el clip del género (JUG_DANIO_F_01.wav) y si no existe usa el normal subiendo o bajando el tono.
    /// Está aquí porque Personaje e IA no se referencian entre sí pero los dos ven Combate.
    /// </summary>
    public static class EtiquetaVoz
    {
        public const char Separador = '@';
        public const char Femenina = 'F';
        public const char Masculina = 'M';

        public static string De(string id, GeneroVoz genero)
        {
            return id + Separador + (genero == GeneroVoz.Femenina ? Femenina : Masculina);
        }

        /// <summary>La cara 0 del perfil del jugador es la mujer y la 1 el hombre (Personaje/Apariencia/ModeloSegunPerfil).</summary>
        public static GeneroVoz DeCara(int cara)
        {
            return cara == 0 ? GeneroVoz.Femenina : GeneroVoz.Masculina;
        }

        /// <summary>Género repartido por semilla estable (por ejemplo el número de instancia del NPC): con 4 sale una voz femenina de cada cuatro.</summary>
        public static GeneroVoz PorSemilla(int semilla, int porCadaFemenina)
        {
            int resto = ((semilla % porCadaFemenina) + porCadaFemenina) % porCadaFemenina;
            return resto == 0 ? GeneroVoz.Femenina : GeneroVoz.Masculina;
        }
    }
}
