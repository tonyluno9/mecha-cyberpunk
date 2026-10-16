using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ServicioGuardado (T-111) con dobles de IGuardable y una ruta temporal (nunca la partida real).</summary>
    public class ServicioGuardadoPruebas : InputTestFixture
    {
        private class GuardableDoble : MonoBehaviour, IGuardable
        {
            public string clave = "doble";
            public string valor = "";
            public int cargas;

            public string ClaveGuardado => clave;
            public string Guardar() => valor;

            public void Cargar(string datos)
            {
                valor = datos;
                cargas++;
            }

            private void OnEnable() => Registro<IGuardable>.Agregar(this);
            private void OnDisable() => Registro<IGuardable>.Quitar(this);
        }

        private string carpeta;
        private ServicioGuardado servicio;
        private GameObject raiz;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            carpeta = Path.Combine(Application.temporaryCachePath, "guardado_pruebas_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            raiz = new GameObject("ServicioGuardado");
            servicio = raiz.AddComponent<ServicioGuardado>();
            servicio.FijarRuta(Path.Combine(carpeta, "partida.json"));
        }

        public override void TearDown()
        {
            if (raiz != null) Object.Destroy(raiz);
            foreach (GuardableDoble doble in Object.FindObjectsByType<GuardableDoble>(FindObjectsSortMode.None))
            {
                Object.Destroy(doble.gameObject);
            }
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private static GuardableDoble Crear(string clave, string valor)
        {
            // Inactivo mientras se configura: OnEnable ya registra y puede recibir datos.
            var objeto = new GameObject("Doble_" + clave);
            objeto.SetActive(false);
            var doble = objeto.AddComponent<GuardableDoble>();
            doble.clave = clave;
            doble.valor = valor;
            objeto.SetActive(true);
            return doble;
        }

        [UnityTest]
        public IEnumerator GuardarYCargar_ConCambiosEntreMedio_DevuelveLoGuardado()
        {
            GuardableDoble a = Crear("a", "uno \"con comillas\"\ny salto");
            GuardableDoble b = Crear("b", "dos");
            yield return null;
            int guardadas = 0;
            int cargadas = 0;
            servicio.Guardada += () => guardadas++;
            servicio.Cargada += () => cargadas++;

            Assert.IsTrue(servicio.Guardar());
            a.valor = "cambiado";
            b.valor = "otro";
            Assert.IsTrue(servicio.Cargar());

            Assert.AreEqual("uno \"con comillas\"\ny salto", a.valor);
            Assert.AreEqual("dos", b.valor);
            Assert.AreEqual(1, guardadas);
            Assert.AreEqual(1, cargadas);
        }

        [UnityTest]
        public IEnumerator Cargar_PiezaQueSeRegistraDespues_RecibeSuDato()
        {
            GuardableDoble a = Crear("a", "uno");
            yield return null;
            Crear("tarde", "dato tarde");
            yield return null;
            Assert.IsTrue(servicio.Guardar());
            Object.Destroy(GameObject.Find("Doble_tarde"));
            yield return null;
            a.valor = "cambiado";

            Assert.IsTrue(servicio.Cargar());
            GuardableDoble tarde = Crear("tarde", "nuevo");
            yield return null;

            Assert.AreEqual("uno", a.valor);
            Assert.AreEqual("dato tarde", tarde.valor, "Se le entrega al registrarse");
            Assert.AreEqual(1, tarde.cargas);
        }

        [UnityTest]
        public IEnumerator Cargar_ClaveQueNoVieneEnElArchivo_NoSeToca()
        {
            GuardableDoble a = Crear("a", "uno");
            yield return null;
            Assert.IsTrue(servicio.Guardar());
            GuardableDoble extra = Crear("extra", "intacto");
            yield return null;

            Assert.IsTrue(servicio.Cargar());

            Assert.AreEqual("intacto", extra.valor);
            Assert.AreEqual(0, extra.cargas);
        }

        [UnityTest]
        public IEnumerator Guardar_ClaveRepetida_AvisaYGuardaSoloLaPrimera()
        {
            GuardableDoble primero = Crear("igual", "primero");
            GuardableDoble segundo = Crear("igual", "segundo");
            yield return null;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("repetida 'igual'"));
            Assert.IsTrue(servicio.Guardar());
            primero.valor = "x";
            segundo.valor = "y";
            Assert.IsTrue(servicio.Cargar());

            Assert.AreEqual("primero", primero.valor);
            Assert.AreEqual(0, segundo.cargas, "El segundo no se guardó, así que no se le entrega nada");
        }

        [UnityTest]
        public IEnumerator Cargar_ArchivoRoto_DevuelveFalseAvisaYNoCambiaNada()
        {
            GuardableDoble a = Crear("a", "uno");
            yield return null;
            File.WriteAllText(servicio.RutaArchivo, "{ esto no es una partida");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no se entiende"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = servicio.Cargar());

            Assert.IsFalse(ok);
            Assert.AreEqual("uno", a.valor);
            Assert.AreEqual(0, a.cargas);
        }

        [UnityTest]
        public IEnumerator Cargar_SinArchivo_DevuelveFalseYAvisa()
        {
            GuardableDoble a = Crear("a", "uno");
            yield return null;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No hay partida"));

            Assert.IsFalse(servicio.Cargar());
            Assert.AreEqual(0, a.cargas);
        }

        [UnityTest]
        public IEnumerator Guardar_DosVeces_NoDejaArchivoTemporalYConservaLoUltimo()
        {
            GuardableDoble a = Crear("a", "uno");
            yield return null;
            Assert.IsTrue(servicio.Guardar());
            a.valor = "dos";
            Assert.IsTrue(servicio.Guardar());

            Assert.AreEqual(0, Directory.GetFiles(carpeta, "*.tmp").Length);
            a.valor = "x";
            Assert.IsTrue(servicio.Cargar());
            Assert.AreEqual("dos", a.valor);
        }

        [UnityTest]
        public IEnumerator Guardar_NoSePuedeEscribir_DevuelveFalseYDejaElAnteriorIntacto()
        {
            GuardableDoble a = Crear("a", "uno");
            yield return null;
            Assert.IsTrue(servicio.Guardar());
            string antes = File.ReadAllText(servicio.RutaArchivo);
            // El .tmp es una carpeta: escribirlo falla.
            Directory.CreateDirectory(servicio.RutaArchivo + ".tmp");
            a.valor = "dos";

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("No se pudo guardar"));

            Assert.IsFalse(servicio.Guardar());
            Assert.AreEqual(antes, File.ReadAllText(servicio.RutaArchivo));
        }

        [UnityTest]
        public IEnumerator JugadorAPie_SoloEnEscenaVacia_TieneExactamenteUnServicioGuardado()
        {
            Object.Destroy(raiz);
            yield return null;
            GameObject jugador = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab"));
            yield return null;

            Assert.AreEqual(1, jugador.GetComponentsInChildren<ServicioGuardado>(true).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<ServicioGuardado>(FindObjectsSortMode.None).Length);
            Object.Destroy(jugador);
        }

        [UnityTest]
        public IEnumerator Borrar_ConPartida_DejaHayPartidaEnFalse()
        {
            Crear("a", "uno");
            yield return null;
            Assert.IsFalse(servicio.HayPartida);
            Assert.IsTrue(servicio.Guardar());
            Assert.IsTrue(servicio.HayPartida);

            servicio.Borrar();

            Assert.IsFalse(servicio.HayPartida);
        }
    }
}
