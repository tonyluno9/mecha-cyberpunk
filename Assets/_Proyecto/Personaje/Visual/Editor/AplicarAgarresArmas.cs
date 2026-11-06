using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// S1 (T-650): pone cada arma del jugador (Pistola, Fusil, Escopeta, Sniper, hijas del socket ManoDer del hueso RightHand) con el agarre de
    /// <see cref="AgarresArmas"/> y le agrega el punto de la mano izquierda (AgarreIzq). Las armas de cubos se adelgazan para caber en la mano. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AplicarAgarresArmas.Aplicar
    /// </summary>
    public static class AplicarAgarresArmas
    {
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        /// <summary>Alto (m) del cuerpo de cubos de las armas largas: el de origen (0.16) no cabe en la palma.</summary>
        public const float AltoCuerpoArmaLarga = 0.09f;

        [MenuItem("Mecha Cyberpunk/Personajes/S1 Aplicar agarres de armas")]
        public static void Aplicar()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                Aplicar(contenido);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Debug.Log("[Personaje] S1: agarres de armas aplicados al jugador.");
        }

        /// <summary>Aplica los agarres a las armas que cuelgan del socket ManoDer dentro de <paramref name="raiz"/>.</summary>
        public static void Aplicar(GameObject raiz)
        {
            foreach (ApuntadoIK ik in raiz.GetComponentsInChildren<ApuntadoIK>(true))
            {
                Transform socket = ik.SocketArma;
                if (socket == null)
                {
                    continue;
                }
                foreach (Transform arma in socket)
                {
                    if (!AgarresArmas.TryObtener(arma.name, out AgarreArma agarre))
                    {
                        continue;
                    }
                    arma.localPosition = agarre.Posicion;
                    arma.localRotation = Quaternion.Euler(agarre.Rotacion);
                    Transform punto = arma.Find(AgarresArmas.NombreAgarreIzquierdo);
                    if (punto == null)
                    {
                        punto = new GameObject(AgarresArmas.NombreAgarreIzquierdo).transform;
                        punto.SetParent(arma, false);
                    }
                    punto.localPosition = agarre.PosicionIzquierda;
                    punto.localRotation = Quaternion.Euler(agarre.RotacionIzquierda);
                    Transform cuerpo = arma.Find("Cuerpo " + arma.name);
                    if (cuerpo != null && arma.name != "Pistola")
                    {
                        Vector3 e = cuerpo.localScale;
                        cuerpo.localScale = new Vector3(e.x, AltoCuerpoArmaLarga, e.z);
                    }
                }
            }
        }
    }
}
