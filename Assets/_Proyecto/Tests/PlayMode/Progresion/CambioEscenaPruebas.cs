using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>CambioEscena (T-124): guarda antes de ir y la escena nueva carga; sin transición no se lee la partida.</summary>
    public class CambioEscenaPruebas
    {
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaAPie = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";
        private const string AliasGuardado = "EL RAYO";
        private const int FramesDeEspera = 5;
        private const int MaxFramesEscena = 600;

        private string carpeta;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            carpeta = Path.Combine(Application.temporaryCachePath, "cambio_escena_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
        }

        [TearDown]
        public void Limpiar()
        {
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            foreach (GameObject objeto in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(objeto);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static IEnumerator Esperar()
        {
            for (int i = 0; i < FramesDeEspera; i++) yield return null;
        }

        // La escena nueva puede activar al jugador unos frames después: espera a que el servicio de perfil vivo sea el de ella.
        private static IEnumerator EsperarServiciosDeLaEscenaNueva(string rutaEscena)
        {
            for (int i = 0; i < MaxFramesEscena; i++)
            {
                if (SceneManager.GetActiveScene().path != rutaEscena) { yield return null; continue; }
                var perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
                if (perfil != null && perfil.gameObject.scene == SceneManager.GetActiveScene()) break;
                yield return null;
            }
            yield return Esperar();
        }

        private static GameObject CrearServicios()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios);
            Assert.IsNotNull(prefab, "existe ServiciosProgresion.prefab");
            return Object.Instantiate(prefab);
        }

        [UnityTest]
        public IEnumerator Ir_ConAliasYEtapaFijados_LaEscenaNuevaCargaLaPartida()
        {
            CrearServicios();
            yield return null;
            var perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
            Assert.IsNotNull(perfil);
            perfil.Fijar("Ana", AliasGuardado);
            var historia = Registro<IHistoria>.Actual as ServicioHistoria;
            Assert.IsNotNull(historia, "el prefab trae ServicioHistoria");
            Assert.IsTrue(historia.Avanzar(EtapaPrologo.PeleaTutorial));

            CambioEscena.Ir(RutaAPie);
            yield return Esperar();
            yield return EsperarServiciosDeLaEscenaNueva(RutaAPie);

            Assert.AreEqual(RutaAPie, SceneManager.GetActiveScene().path);
            Assert.AreEqual(AliasGuardado, Registro<IPerfilJugador>.Actual.Alias);
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, Registro<IHistoria>.Actual.Etapa);
            Assert.IsFalse(CambioEscena.ConsumirTransicion(), "la transición ya se consumió al cargar");
        }

        [UnityTest]
        public IEnumerator PlayDirecto_SinTransicion_NoCargaLaPartida()
        {
            GameObject servicios = CrearServicios();
            yield return null;
            var perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
            perfil.Fijar("Ana", AliasGuardado);
            Assert.IsTrue(ServicioGuardado.Actual.Guardar());
            Assert.IsTrue(File.Exists(ServicioGuardado.RutaForzada));
            Object.Destroy(servicios);
            yield return null;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaAPie, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Esperar();

            Assert.AreEqual(RutaAPie, SceneManager.GetActiveScene().path);
            Assert.IsNotNull(Registro<IPerfilJugador>.Actual);
            Assert.AreNotEqual(AliasGuardado, Registro<IPerfilJugador>.Actual.Alias, "sin transición no se lee la partida");
            Assert.AreEqual(EtapaPrologo.Ninguna, Registro<IHistoria>.Actual.Etapa);
        }

        [UnityTest]
        public IEnumerator Ir_ConInterceptor_NoCargaEscenaYRecibeLaRuta()
        {
            var marca = new GameObject("MarcaDeEscena");
            string recibida = null;
            CambioEscena.Interceptar(ruta => recibida = ruta);
            Scene antes = SceneManager.GetActiveScene();

            CambioEscena.Ir(RutaAPie);
            yield return Esperar();

            Assert.AreEqual(RutaAPie, recibida);
            Assert.AreEqual(antes, SceneManager.GetActiveScene());
            Assert.IsTrue(marca != null, "la escena actual sigue ahí");
        }

        [Test]
        public void Ir_SinRuta_NoMarcaTransicion()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] CambioEscena"));
            CambioEscena.Ir(string.Empty);
            Assert.IsFalse(CambioEscena.ConsumirTransicion());
        }
    }
}
