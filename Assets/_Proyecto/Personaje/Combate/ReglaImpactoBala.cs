using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1324: busca el primer collider que para una bala (hitscan), sin buffer limitado y saltando al propio jugador y a su carro.</summary>
    public static class ReglaImpactoBala
    {
        private const int MaxSaltos = 24;
        private const float Avance = 0.02f;

        /// <summary>T-1602: capa de lo delgado o hueco (cables, toldos, lonas, rejas, ramas): sigue siendo sólida para jugador y carros, pero no detiene balas.</summary>
        public const string NombreCapaNoDetieneBalas = "NoDetieneBalas";

        /// <summary>T-1602: máscara de las balas: todo menos la capa NoDetieneBalas (todo, si la capa no existe en el proyecto).</summary>
        public static int MascaraBala
        {
            get
            {
                int capa = LayerMask.NameToLayer(NombreCapaNoDetieneBalas);
                return capa < 0 ? ~0 : ~(1 << capa);
            }
        }

        /// <summary>El primer collider no trigger del rayo que no sea de <paramref name="raizPropia"/> ni de <paramref name="raizExtra"/> (puede ser null).</summary>
        public static bool BuscarPrimero(Vector3 origen, Vector3 direccion, float alcance, Transform raizPropia, Transform raizExtra, out RaycastHit mejor)
            => BuscarPrimero(origen, direccion, alcance, raizPropia, raizExtra, MascaraBala, out mejor);

        /// <summary>T-1602: igual, con la máscara de capas dada (en el juego, <see cref="MascaraBala"/>).</summary>
        public static bool BuscarPrimero(Vector3 origen, Vector3 direccion, float alcance, Transform raizPropia, Transform raizExtra, int mascara, out RaycastHit mejor)
        {
            mejor = default;
            float recorrido = 0f;
            Vector3 desde = origen;
            for (int i = 0; i < MaxSaltos && recorrido < alcance; i++)
            {
                if (!Physics.Raycast(desde, direccion, out RaycastHit impacto, alcance - recorrido, mascara, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }
                Transform raiz = impacto.collider.transform.root;
                if (raiz != raizPropia && (raizExtra == null || raiz != raizExtra))
                {
                    impacto.distance += recorrido;
                    mejor = impacto;
                    return true;
                }
                float paso = impacto.distance + Avance;
                recorrido += paso;
                desde += direccion * paso;
            }
            return false;
        }
    }
}
