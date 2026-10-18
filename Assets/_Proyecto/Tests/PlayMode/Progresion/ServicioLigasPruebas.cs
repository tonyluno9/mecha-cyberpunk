using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ServicioLigas (T-154) desde ServiciosProgresion.prefab, con ruta de guardado temporal.</summary>
    public class ServicioLigasPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";

        private string carpeta;
        private string rutaAnterior;
        private GameObject raiz;
        private ServicioLigas ligas;
        private ServicioGuardado guardado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            rutaAnterior = ServicioGuardado.RutaForzada;
            carpeta = Path.Combine(Application.temporaryCachePath, "ligas_pruebas_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "existe ServiciosProgresion.prefab");
            raiz = Object.Instantiate(prefab);
            ligas = raiz.GetComponent<ServicioLigas>();
            guardado = raiz.GetComponent<ServicioGuardado>();
            Assert.IsNotNull(ligas, "el prefab trae ServicioLigas");
            Assert.IsNotNull(guardado, "el prefab trae ServicioGuardado");
        }

        public override void TearDown()
        {
            if (raiz != null) Object.Destroy(raiz);
            ServicioGuardado.RutaForzada = rutaAnterior;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Prefab_RegistraElServicioConSusNombres()
        {
            yield return null;
            Assert.AreSame(ligas, Registro<IProgresoLigas>.Actual);
            Assert.AreSame(ligas, Registro<ServicioLigas>.Actual);
            IGuardable encontrado = null;
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == "ligas") encontrado = g;
            }
            Assert.AreSame(ligas, encontrado);
            Assert.AreEqual(4, ligas.CantidadLigas);
            Assert.AreEqual("SÓTANO", ligas.NombreLiga(1));
            Assert.AreEqual("CALLE", Registro<IProgresoLigas>.Actual.NombreLiga(2));
            Assert.AreEqual("", ligas.NombreLiga(0));
            Assert.AreEqual("", ligas.NombreLiga(5));
            Assert.AreEqual(0.5f, ligas.Datos.FraccionPremioRevancha, 0.0001f);
        }

        private void VencerHastaLiga2Siguiente3()
        {
            for (int i = 1; i <= 5; i++) ligas.RegistrarVictoria(1, i);
            ligas.RegistrarVictoria(2, 1);
            ligas.RegistrarVictoria(2, 2);
        }

        [UnityTest]
        public IEnumerator GuardarYCargar_Liga2Siguiente3_AvisaCambioPeroNoCampeon()
        {
            yield return null;
            int campeones = 0;
            ligas.CampeonVencido += _ => campeones++;
            VencerHastaLiga2Siguiente3();
            Assert.AreEqual(2, ligas.LigaActual);
            Assert.AreEqual(3, ligas.Siguiente);
            Assert.AreEqual(1, campeones);
            Assert.IsTrue(guardado.Guardar());
            ligas.Reiniciar();
            Assert.AreEqual(1, ligas.LigaActual);

            int cambios = 0;
            ligas.Cambio += () => cambios++;
            Assert.IsTrue(guardado.Cargar());
            Assert.AreEqual(2, ligas.LigaActual);
            Assert.AreEqual(3, ligas.Siguiente);
            Assert.AreEqual(1, ligas.CampeonesVencidos);
            Assert.GreaterOrEqual(cambios, 1);
            Assert.AreEqual(1, campeones, "cargar no avisa CampeonVencido");
        }

        [UnityTest]
        public IEnumerator Cargar_ConTextoRoto_NoCambiaNadaYAvisaConWarning()
        {
            yield return null;
            VencerHastaLiga2Siguiente3();
            int cambios = 0;
            ligas.Cambio += () => cambios++;
            string[] rotos = { "no es json", "{\"liga\":9,\"siguiente\":1,\"campeones\":0}", "{\"liga\":2,\"siguiente\":99,\"campeones\":1}", "" };
            foreach (string roto in rotos)
            {
                LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] Las ligas"));
                ligas.Cargar(roto);
            }
            Assert.AreEqual(2, ligas.LigaActual);
            Assert.AreEqual(3, ligas.Siguiente);
            Assert.AreEqual(1, ligas.CampeonesVencidos);
            Assert.AreEqual(0, cambios);
        }

        [UnityTest]
        public IEnumerator Pruebas_NoTocanLaPartidaRealDelHumano()
        {
            yield return null;
            string real = Path.Combine(Application.persistentDataPath, "partida.json");
            bool existia = File.Exists(real);
            System.DateTime antes = existia ? File.GetLastWriteTimeUtc(real) : default;
            VencerHastaLiga2Siguiente3();
            Assert.IsTrue(guardado.Guardar());
            StringAssert.StartsWith(carpeta, guardado.RutaArchivo);
            Assert.AreEqual(existia, File.Exists(real));
            if (existia) Assert.AreEqual(antes, File.GetLastWriteTimeUtc(real));
        }

        [UnityTest]
        public IEnumerator TeclaL_RegistraLaVictoriaDelSiguienteRival()
        {
            yield return null;
            var objeto = new GameObject("LigasDePrueba");
            objeto.AddComponent<LigasDePrueba>();
            yield return null;
            var teclado = InputSystem.AddDevice<Keyboard>();
            Press(teclado.lKey);
            yield return null;
            Release(teclado.lKey);
            yield return null;
            Assert.AreEqual(2, ligas.Siguiente);
            for (int i = 0; i < 4; i++)
            {
                Press(teclado.lKey);
                yield return null;
                Release(teclado.lKey);
                yield return null;
            }
            Assert.AreEqual(2, ligas.LigaActual);
            Assert.AreEqual(1, ligas.Siguiente);
            Assert.AreEqual(1, ligas.CampeonesVencidos);
            Object.Destroy(objeto);
        }
    }
}
