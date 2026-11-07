using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-719: en la ciudad las expendedoras salen de un lugar de Core (la entrada del lugar más un desplazamiento) y, si la sesión 2 pone un marcador con el nombre acordado en la
    /// escena, de ese marcador. Guardado temporal.
    /// </summary>
    public class ExpendedorasMarcadoresT719Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private string carpeta;

        [SetUp]
        public void Preparar()
        {
            carpeta = Path.Combine(Application.temporaryCachePath, "t719_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
        }

        [TearDown]
        public void Limpiar()
        {
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(raiz);
            Registros.LimpiarTodos();
            yield return null;
        }

        private static Vector3 Entrada(IdLugar id)
        {
            foreach (ILugar l in Registro<ILugar>.Todos)
            {
                if (l.Id == id) return l.PosicionEntrada;
            }
            Assert.Fail("No hay lugar " + id);
            return default;
        }

        private static GameObject Raiz() => GameObject.Find(ColocadorExpendedoras.NombreRaiz);

        [UnityTest]
        public IEnumerator Expendedoras_SalenDeLosLugares_YElMarcadorDeLaEscenaGana()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + 30f;
            while (Registro<ILugar>.Cantidad == 0 || Raiz() == null)
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "La ciudad no puso las expendedoras");
                yield return null;
            }
            // a. Salen de los lugares de Core: cada una a su desplazamiento de la entrada del lugar.
            Transform raiz = Raiz().transform;
            Expendedora[] maquinas = raiz.GetComponentsInChildren<Expendedora>();
            Assert.AreEqual(3, maquinas.Length, "Casa, tianguis y Nova");
            Vector3 casa = Entrada(IdLugar.Casa);
            Vector3 bar = Entrada(IdLugar.Bar);
            Assert.Less(Vector3.Distance(raiz.Find("Expendedora_1").position, casa + new Vector3(0f, 0f, 6f)), 0.01f, "La de la casa mide desde Lugar_Casa");
            Assert.Less(Vector3.Distance(raiz.Find("Expendedora_2").position, bar + new Vector3(-36f, 0f, 0.49f)), 0.01f, "La del tianguis mide desde Lugar_Bar");
            Assert.Less(Vector3.Distance(raiz.Find("Expendedora_3").position, bar + new Vector3(64f, 0f, -0.01f)), 0.01f, "La de Nova mide desde Lugar_Bar");

            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                Vector3 maquina = raiz.Find("Expendedora_1").position + Vector3.up * 1f;
                GameObject foto = new GameObject("CamaraFoto");
                Camera camara = foto.AddComponent<Camera>();
                camara.CopyFrom(Registro<IJugador>.Actual.Camara);
                camara.depth += 3f;
                camara.transform.position = AsaltoDinero.PosicionLibre(maquina, new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }, 5f, 1f);
                camara.transform.rotation = Quaternion.LookRotation((maquina - camara.transform.position).normalized);
                CapturaUI.Capturar("docs/capturas/s1c-T-719-expendedora-casa.png", camara);
                Object.Destroy(foto);
            }

            // b. La sesión 2 pone un marcador en la escena: la máquina va ahí sin cambiar código ni datos.
            Object.Destroy(raiz.gameObject);
            yield return null;
            GameObject marcador = new GameObject("Marcador_Expendedora_Tianguis");
            marcador.transform.position = new Vector3(-20f, 0.2f, 4f);
            Assert.IsNotNull(ColocadorExpendedoras.Colocar(SceneManager.GetActiveScene()), "Se vuelven a colocar");
            Transform nueva = Raiz().transform;
            Assert.Less(Vector3.Distance(nueva.Find("Expendedora_2").position, marcador.transform.position), 0.01f, "El marcador de la escena manda");
            Assert.Less(Vector3.Distance(nueva.Find("Expendedora_1").position, casa + new Vector3(0f, 0f, 6f)), 0.01f, "Las demás siguen en su lugar");

            // c. Si el lugar de la casa no estuviera, esa máquina no se coloca (ni cae a coordenadas fijas).
            Object.Destroy(nueva.gameObject);
            yield return null;
            Object.Destroy(marcador);
            yield return null;
            Registros.LimpiarTodos(); // sin lugares registrados
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Expendedora 'frente a la casa'.*no se coloca"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Expendedora 'entrada del tianguis'.*no se coloca"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Expendedora 'calle de Nova'.*no se coloca"));
            ColocadorExpendedoras.Colocar(SceneManager.GetActiveScene());
            Assert.AreEqual(0, Raiz() != null ? Raiz().GetComponentsInChildren<Expendedora>().Length : 0, "Sin lugares no hay máquinas en coordenadas inventadas");
        }
    }
}
