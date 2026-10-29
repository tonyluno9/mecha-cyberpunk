namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Algo que se conserva en la partida guardada. Lo implementa quien posee el dato (D-027) y se agrega a
    /// <c>Registro&lt;IGuardable&gt;</c> en <c>OnEnable</c> (se quita en <c>OnDisable</c>).
    /// El guardado de Tony (<c>ServicioGuardado</c>, Progresion/, T-111) junta el texto de todos al guardar y, al cargar,
    /// le devuelve a cada uno el suyo por <see cref="ClaveGuardado"/>, también a los que se registren después de cargar.
    /// Una clave que no está en el archivo no se toca. Claves en uso: <c>cartera</c>, <c>renta</c>, <c>mecha</c>,
    /// <c>perfil</c>, <c>historia</c>, <c>inventario</c> (Tony, T-142), <c>ligas</c> (Tony, T-154), <c>reputacion</c> y <c>favores</c> (Tony, T-182),
    /// <c>misiones</c> (Tony, T-184), <c>implantes</c> (Tony, T-191), <c>adaptacion</c> (Tony, T-200) y <c>reloj</c> (Alan).
    /// </summary>
    public interface IGuardable
    {
        /// <summary>
        /// Nombre del dato en el archivo de guardado. Único y estable entre versiones del juego; solo minúsculas,
        /// números y <c>_</c>. Si dos registrados comparten clave, el guardado usa el primero que se registró y
        /// avisa con un warning.
        /// </summary>
        string ClaveGuardado { get; }

        /// <summary>
        /// El estado actual como texto (recomendado: <c>JsonUtility.ToJson</c> de una clase <c>[Serializable]</c>
        /// propia). Nunca null. No cambia nada.
        /// </summary>
        string Guardar();

        /// <summary>
        /// Restablece el estado desde un texto que dio <see cref="Guardar"/> (de esta versión). No simula el tiempo:
        /// no dispara eventos de avance ni de cobro (el reloj NO avisa <c>DiaCambio</c> ni <c>HoraEnteraCambio</c> al
        /// cargar), pero sí los de "cambió" para que pantallas y luces se pongan al día (<c>Cambio</c>,
        /// <c>FranjaCambio</c>). Si el texto no se entiende, no cambia nada, avisa con <c>Debug.LogWarning</c> y
        /// nunca lanza excepciones.
        /// </summary>
        void Cargar(string datos);
    }
}
