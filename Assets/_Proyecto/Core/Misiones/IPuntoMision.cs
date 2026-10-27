using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Un punto del mundo que usan las misiones: un encargo, un destino, un botín, una pelea o un escondite.
    /// Lo implementa quien arma la escena (Alan en Ciudad.unity; Tony con <c>PuntoMisionDePrueba</c> en sus escenas), se
    /// agrega a <c>Registro&lt;IPuntoMision&gt;</c> en <c>OnEnable</c> y se quita en <c>OnDisable</c>. Los ids que busca Tony
    /// están en sus datos de misiones (T-183) y en docs/revision-tony.md; si falta uno, esa misión no se ofrece y se avisa
    /// con un warning, sin error. Tony pone el interactuable (E) encima del punto; el punto no tiene que ser un
    /// <c>IInteractuable</c>. Nada de esto es obligatorio para quien arma la escena.
    /// </summary>
    public interface IPuntoMision
    {
        /// <summary>
        /// Id único en la escena, con prefijo <c>Mision_</c> (por ejemplo "Mision_Encargo_DonaLupe").
        /// </summary>
        string Id { get; }

        /// <summary>Para qué sirve el punto.</summary>
        TipoPuntoMision Tipo { get; }

        /// <summary>
        /// Un punto en el piso. +Z apunta hacia donde se para la pelea o hacia donde mira el vecino.
        /// </summary>
        Pose Pose { get; }

        /// <summary>
        /// Tony lo llama con true mientras el punto es parte del paso actual o tiene una misión que ofrecer, y con false al
        /// dejar de serlo. Quien lo implementa puede prender ahí un vecino, una luz o una puerta. Nunca debe lanzar.
        /// </summary>
        void Activar(bool activo);
    }
}
