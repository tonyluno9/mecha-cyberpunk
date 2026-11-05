namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// Etiqueta de voz de un ID pedido (T-597): "JUG_DANIO@F" es la voz femenina de JUG_DANIO. Pura. Si el catálogo trae JUG_DANIO_F el gestor
    /// usa ese; si no, el ID normal con el tono que da <see cref="TonoDeGenero"/>.
    /// </summary>
    public static class ReglaVozGenero
    {
        public const char Separador = '@';
        public const float TonoFemenino = 1.2f;
        public const float TonoMasculino = 0.95f;

        /// <summary>Separa "ID@F" en el ID y la etiqueta ("F" o "M"; vacío si no hay).</summary>
        public static string Separar(string pedido, out string etiqueta)
        {
            etiqueta = string.Empty;
            if (string.IsNullOrEmpty(pedido)) return pedido;
            int i = pedido.IndexOf(Separador);
            if (i < 0) return pedido;
            etiqueta = pedido.Substring(i + 1);
            return pedido.Substring(0, i);
        }

        /// <summary>El ID del clip propio del género: JUG_DANIO + F = JUG_DANIO_F.</summary>
        public static string IdDelGenero(string id, string etiqueta) => id + "_" + etiqueta;

        /// <summary>Tono que se le da al clip normal (sin versión propia) para que parezca de ese género.</summary>
        public static float TonoDeGenero(string etiqueta)
        {
            if (etiqueta == "F") return TonoFemenino;
            if (etiqueta == "M") return TonoMasculino;
            return 1f;
        }
    }
}
