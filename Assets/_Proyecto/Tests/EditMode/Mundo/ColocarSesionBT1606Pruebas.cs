using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1606: lo que pidio la sesion B esta en la escena. Pide haber corrido ColocarSesionBT1606.Aplicar. Busca por nombre para no referenciar Personaje, IA ni Progresion.</summary>
    public class ColocarSesionBT1606Pruebas
    {
        private static List<Transform> Todos()
        {
            EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            List<Transform> lista = new List<Transform>();
            foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                lista.AddRange(r.GetComponentsInChildren<Transform>(true));
            }
            return lista;
        }

        private static void ConPiso(Transform t)
        {
            Assert.IsTrue(Physics.Raycast(t.position + Vector3.up * 0.5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore), t.name + " sin piso abajo en " + t.position);
        }

        [Test]
        public void Escena_TienePuntosDeHistoriaYCamilla()
        {
            List<Transform> todos = Todos();
            foreach (string nombre in new[] { "Clinica_Camilla", "Historia_Molestia", "Historia_Ranura", "Historia_Torre" })
            {
                Transform t = todos.FirstOrDefault(x => x.name.Contains(nombre) && x.gameObject.activeInHierarchy);
                Assert.IsNotNull(t, "falta " + nombre);
                ConPiso(t);
            }
        }

        [Test]
        public void Escena_TieneBancasSentables()
        {
            List<Transform> bancas = Todos().Where(x => x.name.Contains("Banca_Sentable") && x.gameObject.activeInHierarchy).ToList();
            Assert.GreaterOrEqual(bancas.Count, 3, "bancas sentables");
            foreach (Transform b in bancas)
            {
                ConPiso(b);
            }
        }

        [Test]
        public void Escena_TieneMarcadoresDeEscenaBarrio()
        {
            List<Transform> marcadores = Todos().Where(x => x.gameObject.activeInHierarchy
                && x.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "MarcadorEscenaBarrio")).ToList();
            Assert.GreaterOrEqual(marcadores.Count, 5, "marcadores");
            foreach (Transform m in marcadores)
            {
                ConPiso(m);
            }
        }
    }
}
