using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-858 (playtest 6-nov tarde): los callejones que no tenían salida llegan a otra calle (mapa de paso a pie desde la casa).</summary>
    public class SalidasS2tPruebas
    {
        [Test]
        public void Callejones_LleganAOtraCalleDesdeLaCasa()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            var (_, zona, _, _) = MapaPasoS2t.Calcular();
            foreach (Vector3 p in new[]
            {
                new Vector3(-100f, 0f, -61.5f), // el callejón de la casa, pasando donde estaba la reja
                new Vector3(-128f, 0f, -61.5f), // y ya en la calle de la orilla oeste
                new Vector3(-22f, 0f, 70f),     // el del mercado, donde estaban los edificios 13 y 14
            })
            {
                (int i, int j) = MapaPasoS2t.Indice(p);
                bool alcanzado = false;
                for (int di = -2; di <= 2 && !alcanzado; di++)
                {
                    for (int dj = -2; dj <= 2 && !alcanzado; dj++)
                    {
                        alcanzado = zona[Mathf.Clamp(i + di, 0, zona.GetLength(0) - 1), Mathf.Clamp(j + dj, 0, zona.GetLength(1) - 1)] == 1;
                    }
                }
                Assert.IsTrue(alcanzado, "se llega a pie desde la casa a " + p);
            }
        }
    }
}
