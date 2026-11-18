using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1330: la puerta del consultorio del Doc está siempre abierta, se cierra con el jugador dentro y se vuelve a abrir al salir.</summary>
    public class PuertaDocT1330Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float Espera = 1.5f;
        private const float EsperaCierreSolo = 8f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        private static PuertaSinHorario Puerta()
        {
            GameObject raiz = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "S2p_Fachada_P12");
            Assert.IsNotNull(raiz, "Falta S2p_Fachada_P12");
            PuertaSinHorario p = raiz.transform.Find("Puerta").GetComponent<PuertaSinHorario>();
            Assert.IsNotNull(p);
            return p;
        }

        [UnityTest]
        public IEnumerator Fuera_QuedaAbierta_AunqueNadieEsteEnElUmbral()
        {
            PuertaSinHorario puerta = Puerta();
            EventosLugares.PublicarSalio(IdLugar.Clinica);
            yield return new WaitForSeconds(Espera);
            Assert.IsTrue(puerta.EstaAbierta, "abierta con el jugador fuera");
            yield return new WaitForSeconds(EsperaCierreSolo);
            Assert.IsTrue(puerta.EstaAbierta, "no se cierra sola");
        }

        [UnityTest]
        public IEnumerator Dentro_SeCierra_YAlSalirVuelveAAbrirse()
        {
            PuertaSinHorario puerta = Puerta();
            EventosLugares.PublicarSalio(IdLugar.Clinica);
            yield return new WaitForSeconds(Espera);
            Assert.IsTrue(puerta.EstaAbierta);

            EventosLugares.PublicarEntro(IdLugar.Clinica);
            yield return new WaitForSeconds(Espera);
            Assert.IsTrue(puerta.JugadorDentro);
            Assert.IsFalse(puerta.EstaAbierta, "cerrada con el jugador dentro");

            EventosLugares.PublicarSalio(IdLugar.Clinica);
            yield return new WaitForSeconds(Espera);
            Assert.IsTrue(puerta.EstaAbierta, "se abre otra vez al salir");
        }

        [UnityTest]
        public IEnumerator Dentro_ConEAbreParaSalir()
        {
            PuertaSinHorario puerta = Puerta();
            EventosLugares.PublicarEntro(IdLugar.Clinica);
            yield return new WaitForSeconds(Espera);
            Assert.IsFalse(puerta.EstaAbierta);
            Assert.AreEqual(ResultadoPuerta.Abierta, puerta.IntentarAbrir(), "E abre para poder salir");
            Assert.IsTrue(puerta.EstaAbierta);
            EventosLugares.PublicarSalio(IdLugar.Clinica);
        }
    }
}
