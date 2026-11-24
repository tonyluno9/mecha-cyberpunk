namespace MechaCyberpunk.VR
{
    /// <summary>
    /// T88, lógica pura: cuándo hay que (re)poner el modo de la ventana del PC (el espejo del visor) en un solo ojo.
    /// Solo con VR real (no el simulador), con el display XR corriendo y si el modo actual no es ya el deseado.
    /// </summary>
    public static class ReglaVentanaPCVR
    {
        /// <summary>True si hay que llamar a SetPreferredMirrorBlitMode con <paramref name="modoDeseado"/>.</summary>
        public static bool DebeAplicar(bool vrActivo, bool simulado, bool displayCorriendo, int modoActual, int modoDeseado)
        {
            if (!vrActivo || simulado || !displayCorriendo)
            {
                return false;
            }
            return modoActual != modoDeseado;
        }
    }
}
