using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1307: con el jugador dentro de un carro, la bala que pega en el vehículo le llega y le baja vida (una fracción, porque el carro protege).</summary>
    public class DanioEnCarroT1307Pruebas : BaseJugadorAPie
    {
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private const float DistanciaTirador = 15f;
        private const float DanioBala = 20f;
        private const float AlturaPecho = 1.3f;

        private GameObject carroObjeto;
        private GameObject tirador;
        private System.Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f;
        }

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            if (carroObjeto != null)
            {
                Object.Destroy(carroObjeto);
            }
            if (tirador != null)
            {
                Object.Destroy(tirador);
            }
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator EnCarro_UnDisparoAlVehiculo_BajaVidaDelJugador()
        {
            yield return Calentar();
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0.05f, 4f), Quaternion.identity);
            yield return null;
            IMontable carro = carroObjeto.GetComponent<IMontable>();
            Assert.IsTrue(jugadorAPie.Subir(carro), "El jugador sube al carro");
            yield return Esperar(0.3f);
            Physics.SyncTransforms();

            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float antes = vida.Actual;
            tirador = new GameObject("Tirador");
            tirador.transform.position = new Vector3(0f, AlturaPecho, jugador.transform.position.z + DistanciaTirador);
            PistolaPandillero pistola = tirador.AddComponent<PistolaPandillero>();
            Vector3 pecho = jugador.transform.position + Vector3.up * AlturaPecho;

            bool dio = pistola.Disparar(tirador.transform.position, pecho, vida, DanioBala, 1f);

            Assert.IsTrue(dio, "La bala que pega en el carro le llega al jugador");
            Assert.AreEqual(antes - DanioBala * VidaAPie.FraccionDanioDisparoEnCarro, vida.Actual, 0.01f, "En carro recibe solo una fracción");
        }
    }
}
