using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Un punto con nombre que el mundo pone para la historia: los planos de cámara del flashback y los pasos de la ruta del
    /// prólogo. La implementa Alan en sus escenas (Flashback.unity y Ciudad.unity) y se agrega a
    /// <c>Registro&lt;IPuntoHistoria&gt;</c> en <c>OnEnable</c> (se quita en <c>OnDisable</c>). Los ids que Tony busca están en sus
    /// datos (<c>DatosFlashback</c> y <c>DatosPrologo</c>, T-131 y T-132) y en docs/revision-tony.md; si falta uno, Tony usa un
    /// respaldo y avisa con un warning, sin error.
    /// </summary>
    public interface IPuntoHistoria
    {
        /// <summary>
        /// Identificador único en la escena. Prefijo <c>Flashback_</c> para planos de cámara y <c>Prologo_</c> para la ruta del
        /// prólogo, por ejemplo "Flashback_Plano_1", "Prologo_Carro", "Prologo_Esquina_1".
        /// </summary>
        string Id { get; }

        /// <summary>Para un plano: posición y rotación de la cámara. Para la ruta: un punto en el piso.</summary>
        Pose Pose { get; }

        /// <summary>
        /// Tony lo llama con true cuando empieza el plano que usa este punto y con false cuando termina; Alan puede prender ahí
        /// una animación o luces. Para puntos de ruta no se llama. Nunca debe lanzar.
        /// </summary>
        void Activar(bool activo);
    }
}
