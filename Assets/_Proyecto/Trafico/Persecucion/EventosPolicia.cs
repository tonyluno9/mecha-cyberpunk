using System;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>Avisos de la persecución policial (T-665). Se vacían al entrar a Play.</summary>
    public static class EventosPolicia
    {
        /// <summary>Una patrulla cerca te arrestó: el calor se borra y la policía se retira.</summary>
        public static event Action JugadorArrestado;

        /// <summary>Escapaste: saliste de su vista y del círculo de búsqueda el tiempo suficiente.</summary>
        public static event Action JugadorEscapo;

        public static void PublicarArresto() => JugadorArrestado?.Invoke();

        public static void PublicarEscape() => JugadorEscapo?.Invoke();

        public static void LimpiarSuscriptores()
        {
            JugadorArrestado = null;
            JugadorEscapo = null;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarSuscriptores();
    }
}
