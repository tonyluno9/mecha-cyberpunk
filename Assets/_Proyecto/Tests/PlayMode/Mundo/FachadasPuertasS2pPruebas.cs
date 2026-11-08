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
    /// <summary>
    /// T-890 en la ciudad del juego: la reja de la casa (P11/PU03) y la puerta del consultorio (P12/PU04) cierran el paso, abren y dejan
    /// pasar a una cápsula del tamaño del jugador por el vano de la fachada nueva. Las casas del kit llevan su puerta cerrada, sin chocar.
    /// </summary>
    public class FachadasPuertasS2pPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaHojaCasa = "Barrio/Manzana_4/Lugar_Casa/Puerta/Hoja";
        private const string RutaPuertaDoc = "S2p_Fachada_P12/Puerta";
        private const float RadioJugador = 0.35f;
        private const float AlturaJugador = 1.8f;
        private const float Holgura = 0.05f;
        private const float SegundosParaAbrir = 1.2f;
        private const float DistanciaDeCruce = 2.4f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Physics.SyncTransforms();
        }

        private static Transform Buscar(string ruta)
        {
            int i = ruta.IndexOf('/');
            GameObject raiz = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == ruta.Substring(0, i));
            Assert.IsNotNull(raiz, "Falta " + ruta);
            Transform t = raiz.transform.Find(ruta.Substring(i + 1));
            Assert.IsNotNull(t, "Falta " + ruta);
            return t;
        }

        /// <summary>Lo que estorba a una cápsula del jugador que cruza desde 'calle' hacia 'dentro' (sin triggers ni personajes).</summary>
        private static string[] Estorbos(Vector3 calle, Vector3 haciaDentro)
        {
            Physics.SyncTransforms();
            Vector3 pies = calle + Vector3.up * (RadioJugador + Holgura);
            Vector3 cabeza = calle + Vector3.up * (AlturaJugador - RadioJugador);
            return Physics.CapsuleCastAll(pies, cabeza, RadioJugador, haciaDentro, DistanciaDeCruce, ~0, QueryTriggerInteraction.Ignore)
                .Select(h => h.collider)
                .Where(c => c.GetComponentInParent<CharacterController>() == null && c.GetComponentInParent<Animator>() == null)
                .Select(c => c.name)
                .ToArray();
        }

        private static IEnumerator ComprobarPaso(HojaPuerta hoja, Vector3 calle, Vector3 haciaDentro)
        {
            Assert.IsNotEmpty(Estorbos(calle, haciaDentro), "cerrada debe estorbar el paso");
            hoja.Abrir();
            yield return new WaitForSeconds(SegundosParaAbrir);
            Assert.IsTrue(hoja.EstaAbierta);
            CollectionAssert.IsEmpty(Estorbos(calle, haciaDentro), "abierta debe dejar pasar al jugador por el vano");
            Assert.Greater(Mathf.Abs(hoja.AnguloActual), 30f, "la hoja gira sobre su bisagra");
        }

        [UnityTest]
        public IEnumerator RejaDeLaCasa_Abierta_DejaPasarPorElVano()
        {
            HojaPuerta hoja = Buscar(RutaHojaCasa).GetComponent<HojaPuerta>();
            Assert.IsNotNull(hoja.transform.Find("Modelo"), "la hoja lleva la reja PU03");
            yield return ComprobarPaso(hoja, new Vector3(-56.2f, 0.22f, -77f), Vector3.left);
        }

        [UnityTest]
        public IEnumerator PuertaDelDoc_AbreACualquierHora_YDejaPasar()
        {
            Transform t = Buscar(RutaPuertaDoc);
            PuertaSinHorario puerta = t.GetComponent<PuertaSinHorario>();
            Assert.IsNotNull(puerta);
            Assert.AreEqual(IdLugar.Clinica, puerta.Lugar);
            Assert.IsTrue(Registro<IPuerta>.Todos.Contains(puerta), "E la encuentra en el registro de puertas");
            HojaPuerta hoja = t.GetComponentInChildren<HojaPuerta>();
            Vector3 calle = new Vector3(t.position.x, 0.22f, -9.2f);
            Assert.IsNotEmpty(Estorbos(calle, Vector3.back), "cerrada debe estorbar el paso");
            Assert.AreEqual(ResultadoPuerta.Abierta, puerta.IntentarAbrir());
            yield return new WaitForSeconds(SegundosParaAbrir);
            Assert.IsTrue(puerta.EstaAbierta);
            CollectionAssert.IsEmpty(Estorbos(calle, Vector3.back), "abierta debe dejar pasar al jugador por el vano");
            Assert.Greater(Mathf.Abs(hoja.AnguloActual), 30f);
        }

        [UnityTest]
        public IEnumerator CasasDelKit_LlevanPuertaCerradaSinChocar()
        {
            yield return null;
            Transform[] puertas = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => t.name == "Puerta" && t.parent != null && (t.parent.name == "C01_Fachada" || t.parent.name == "C02_Fachada"))
                .ToArray();
            Assert.Greater(puertas.Length, 20, "las plantas bajas del kit llevan puerta");
            Assert.IsFalse(puertas.Any(p => p.GetComponentsInChildren<Collider>().Any(c => c.enabled)), "la puerta de adorno no choca");
        }
    }
}
