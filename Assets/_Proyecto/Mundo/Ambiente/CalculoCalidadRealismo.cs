namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Lógica pura de la calidad del realismo (T14). Bajo apaga todo lo pesado; Medio deja halos y partículas (lo barato) sin SSAO ni sombras de luces
    /// adicionales; Alto lo prende todo. La reflexión planar de los charcos ya la decide Opciones (CalidadVisual), no se repite aquí.
    /// </summary>
    public static class CalculoCalidadRealismo
    {
        /// <summary>Llave de PlayerPrefs donde Opciones (ServicioOpciones) deja el nivel (0 Bajo, 1 Medio, 2 Alto).</summary>
        public const string LlaveNivel = "T1605_CalidadRealismo";

        public static AjustesRealismo Ajustes(NivelCalidadRealismo nivel)
        {
            switch (nivel)
            {
                case NivelCalidadRealismo.Bajo: return new AjustesRealismo(false, false, false, false);
                case NivelCalidadRealismo.Alto: return new AjustesRealismo(true, true, true, true);
                default: return new AjustesRealismo(false, true, true, false);
            }
        }

        /// <summary>Convierte el entero guardado a nivel; lo desconocido es Medio.</summary>
        public static NivelCalidadRealismo Nivel(int valor)
        {
            return valor == 0 ? NivelCalidadRealismo.Bajo : valor == 2 ? NivelCalidadRealismo.Alto : NivelCalidadRealismo.Medio;
        }
    }
}
