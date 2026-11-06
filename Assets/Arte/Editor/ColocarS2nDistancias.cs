using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-801 (noche 6-nov, sesión 2): arreglos del mundo para las pruebas de distancias de la ciudad. Idempotente.
    /// (La estación T02 de la cochera de Barrio.prefab conserva su collider: T-583 exige paredes sólidas y su centro es el Estacionamiento
    /// del prefab; conflicto anotado para decidir. En Ciudad_Prologo el Estacionamiento ya se movió con T-550 y está libre.)
    /// En Ciudad_Prologo, el poste de la guarida "cama" (el buró H11) quedaba en el pasillo pegado al vano de la puerta del cuarto: desde el
    /// pasillo la E ofrecía el buró en vez de la puerta. Ahora va dentro del cuarto, junto a la cama, lejos de la puerta.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nDistancias.Colocar
    /// </summary>
    public static class ColocarS2nDistancias
    {
                private const string RutaPosteCama = "GTA_T448/Guarida_cama";
        private const string PuntoCama = "Barrio/PuntosAparicion/Casa_Cama";
        /// <summary>Lejos del centro del vano de la puerta del cuarto (m) para que la E de la puerta no lo prefiera.</summary>
        private const float LejosDeLaPuerta = 1.8f;
        /// <summary>Lejos del punto de la cama (m): al lado, no encima.</summary>
        private const float LejosDeLaCama = 1.2f;
        private const float PasoBusqueda = 0.1f;
        private static readonly Vector3 MitadBuro = new Vector3(0.3f, 0.35f, 0.3f);

        [MenuItem("Mecha Cyberpunk/S2n/3 Distancias (buró de la guarida)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            PosteCama();
            ColocarS2.Guardar("s2n-distancias");
        }

        /// <summary>Mueve el poste "cama" de la guarida (con su buró) al lugar libre del cuarto más cercano a la cama, lejos de la puerta.</summary>
        private static void PosteCama()
        {
            Physics.SyncTransforms();
            Transform poste = ColocarS2.Buscar(RutaPosteCama);
            Transform cama = ColocarS2.Buscar(PuntoCama);
            if (poste == null || cama == null)
            {
                Debug.LogWarning("[Arte] T-801: falta " + RutaPosteCama + " o " + PuntoCama);
                return;
            }
            Vector3 puerta = new Vector3((ColocarS2nPuertas.VanoX0 + ColocarS2nPuertas.VanoX1) * 0.5f, ColocarS2nPuertas.Suelo, ColocarS2nPuertas.PasilloZ1);
            Bounds cuarto = new Bounds(ColocarS2nPuertas.CentroCuarto, new Vector3(3f, 0f, 4f));
            Vector3 mejor = poste.position;
            float mejorD = float.MaxValue;
            for (float x = cuarto.min.x + MitadBuro.x; x <= cuarto.max.x - MitadBuro.x; x += PasoBusqueda)
            {
                for (float z = cuarto.min.z + MitadBuro.z; z <= cuarto.max.z - MitadBuro.z; z += PasoBusqueda)
                {
                    Vector3 p = new Vector3(x, ColocarS2nPuertas.Suelo, z);
                    float aCama = Vector3.Distance(Plano(p), Plano(cama.position));
                    if (Vector3.Distance(Plano(p), Plano(puerta)) < LejosDeLaPuerta || aCama < LejosDeLaCama || aCama >= mejorD)
                    {
                        continue;
                    }
                    Vector3 centro = p + Vector3.up * (MitadBuro.y + 0.05f);
                    bool choca = Physics.OverlapBox(centro, MitadBuro, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(poste));
                    if (!choca)
                    {
                        mejor = p;
                        mejorD = aCama;
                    }
                }
            }
            if (mejorD == float.MaxValue)
            {
                Debug.LogWarning("[Arte] T-801: no hay lugar libre para el buró en el cuarto; se queda en " + poste.position);
                return;
            }
            // Mira hacia el centro del cuarto (el frente del buró).
            Vector3 hacia = Plano(ColocarS2nPuertas.CentroCuarto - mejor);
            poste.SetPositionAndRotation(mejor, Quaternion.LookRotation(hacia.sqrMagnitude > 0.01f ? hacia.normalized : Vector3.forward));
            ColocarS2.Informe.AppendLine("Poste de la guarida 'cama' (buró) en " + mejor + ", a " + mejorD.ToString("F2") + " m de la cama");
            ColocarS2.Foto(ColocarS2nPuertas.CentroCuarto + new Vector3(0.4f, 1.6f, -1.6f), mejor + Vector3.up * 0.4f, "docs/capturas/s2n-casa-buro-guarida.png", 75f);
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
