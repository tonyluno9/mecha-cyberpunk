using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lectura de la reputación del jugador con el barrio y con las corporaciones.
    /// Solo lectura: la implementa Tony (<c>ServicioReputacion</c>, Progresion/, en <c>ServiciosProgresion.prefab</c>, T-182,
    /// clave de guardado <c>reputacion</c>) y se busca con <c>Registro&lt;IReputacion&gt;</c>. Solo Tony la cambia; Alan solo lee
    /// (ejemplos: grafitis con el alias del jugador cuando el barrio está en nivel 1 o más, vecinos que saludan en nivel 2,
    /// anuncios que difaman cuando las corporaciones están en -1 o menos). Sin <see cref="IReputacion"/> registrado, quien
    /// lea debe actuar como valor 0 y nivel 0.
    /// </summary>
    public interface IReputacion
    {
        /// <summary>Valor mínimo de una reputación (-100).</summary>
        int Minimo { get; }

        /// <summary>Valor máximo de una reputación (100).</summary>
        int Maximo { get; }

        /// <summary>Valor de la reputación, de <see cref="Minimo"/> a <see cref="Maximo"/>. Empieza en 0.</summary>
        int Valor(TipoReputacion tipo);

        /// <summary>Nivel de la reputación, de -2 a 2 (0 es neutral). Los umbrales son datos de Tony.</summary>
        int Nivel(TipoReputacion tipo);

        /// <summary>Nombre del nivel para pantallas ("JUSTICIERO", "ENEMIGO"). Nunca null.</summary>
        string NombreNivel(TipoReputacion tipo);

        /// <summary>Se avisa tras cualquier cambio de valor, también al cargar una partida.</summary>
        event Action Cambio;

        /// <summary>
        /// Se avisa con el nivel nuevo, solo cuando el nivel cambia. No se avisa al cargar una partida
        /// (para eso está <see cref="Cambio"/>).
        /// </summary>
        event Action<TipoReputacion, int> NivelCambio;
    }
}
