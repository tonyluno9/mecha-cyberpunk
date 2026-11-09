using System.Collections;
using MechaCyberpunk.Casino.Meseras;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-930 (playtest 9-nov): las meseras R01 llevan la charola plana a la altura de la cintura, también al caminar. Fallaría si el brazo volviera a colgar con la charola de lado.</summary>
    public class MeseraCharolaT930Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float MinimoPlano = 0.985f;
        private const float AlturaMinima = 0.8f;

        [UnityTest]
        public IEnumerator Meseras_LlevanLaCharolaPlanaALaCintura()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(2f);
            BrazoCharola[] brazos = Object.FindObjectsByType<BrazoCharola>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(brazos.Length, 4, "Las tres meseras y la cajera llevan el brazo de la charola");
            foreach (BrazoCharola b in brazos)
            {
                Assert.Greater(Vector3.Dot(b.NormalCharola, Vector3.up), MinimoPlano, b.name + ": la charola va plana");
                Assert.Greater(b.Mano.position.y - b.transform.position.y, AlturaMinima, b.name + ": la charola va a la altura de la cintura");
            }
        }
    }
}
