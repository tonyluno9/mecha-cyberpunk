using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// La <see cref="IRutaCalles"/> del barrio (T-669): traza rutas por el <see cref="GrafoCalles"/> de las 6 manzanas (80 m de lado, paso de 94 m),
    /// el mismo grafo que usan las patrullas. Se registra sola al arrancar el juego; el mapa de UI la usa sin referenciar a Trafico.
    /// </summary>
    public sealed class RutaCallesBarrio : IRutaCalles
    {
        public const float LadoManzana = 80f;
        public const float PasoManzana = 94f;
        private readonly GrafoCalles calles;

        public RutaCallesBarrio(GrafoCalles grafo) => calles = grafo;

        /// <summary>Las calles del barrio, para pruebas.</summary>
        public GrafoCalles Calles => calles;

        /// <summary>El barrio de 3 x 2 manzanas que dibuja el mapa (de oeste a este y de norte a sur).</summary>
        public static RutaCallesBarrio DelBarrio()
        {
            Vector2[] centros =
            {
                new Vector2(-PasoManzana, PasoManzana * 0.5f), new Vector2(0f, PasoManzana * 0.5f), new Vector2(PasoManzana, PasoManzana * 0.5f),
                new Vector2(-PasoManzana, -PasoManzana * 0.5f), new Vector2(0f, -PasoManzana * 0.5f), new Vector2(PasoManzana, -PasoManzana * 0.5f),
            };
            return new RutaCallesBarrio(GrafoCalles.DeManzanas(centros, LadoManzana, 0f));
        }

        public List<Vector3> Calcular(Vector3 origen, Vector3 destino) => CalculoRutaCalles.Ruta(calles, origen, destino);

        // Después de que Play vacía los registros (SubsystemRegistration) y antes de cargar la escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar() => Registro<IRutaCalles>.Agregar(DelBarrio());
    }
}
