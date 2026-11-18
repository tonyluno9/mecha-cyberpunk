using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
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
        private GrafoCalles calles;
        private readonly Vector2[] centrosDelBarrio;   // T-1350: con centros, el grafo se rehace al aparecer calles de zona
        private int zonasVistas;

        public RutaCallesBarrio(GrafoCalles grafo) => calles = grafo;

        private RutaCallesBarrio(Vector2[] centros)
        {
            centrosDelBarrio = centros;
            calles = GrafoCalles.DeManzanas(centros, LadoManzana, 0f);
        }

        /// <summary>Las calles del barrio (con las de las zonas nuevas si ya están en la escena), para pruebas.</summary>
        public GrafoCalles Calles => Actualizar();

        // T-1350: este trazador se registra antes de cargar la escena, cuando aún no hay CalleZona: el grafo se arma de nuevo si cambia su número.
        private GrafoCalles Actualizar()
        {
            int zonas = CalleZona.Activas.Count;
            if (centrosDelBarrio != null && zonas != zonasVistas)
            {
                zonasVistas = zonas;
                calles = GrafoCalles.DeManzanas(centrosDelBarrio, LadoManzana, 0f);
                CallesDeZona.Agregar(calles);
            }
            return calles;
        }

        /// <summary>El barrio de 3 x 2 manzanas que dibuja el mapa (de oeste a este y de norte a sur).</summary>
        public static RutaCallesBarrio DelBarrio()
        {
            Vector2[] centros =
            {
                new Vector2(-PasoManzana, PasoManzana * 0.5f), new Vector2(0f, PasoManzana * 0.5f), new Vector2(PasoManzana, PasoManzana * 0.5f),
                new Vector2(-PasoManzana, -PasoManzana * 0.5f), new Vector2(0f, -PasoManzana * 0.5f), new Vector2(PasoManzana, -PasoManzana * 0.5f),
            };
            return new RutaCallesBarrio(centros);
        }

        public List<Vector3> Calcular(Vector3 origen, Vector3 destino) => CalculoRutaCalles.Ruta(Actualizar(), origen, destino);

        // Después de que Play vacía los registros (SubsystemRegistration) y antes de cargar la escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar() => Registro<IRutaCalles>.Agregar(DelBarrio());
    }
}
