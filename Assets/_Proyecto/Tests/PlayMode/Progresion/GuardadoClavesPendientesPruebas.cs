using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-164: guardar en una escena sin mecha o inventario no borra lo que se había leído de esas claves.</summary>
    public class GuardadoClavesPendientesPruebas
    {
        private class GuardableDoble : MonoBehaviour, IGuardable
        {
            public string clave = "doble";
            public string valor = "";

            public string ClaveGuardado => clave;
            public string Guardar() => valor;
            public void Cargar(string datos) => valor = datos;

            private void OnEnable() => Registro<IGuardable>.Agregar(this);
            private void OnDisable() => Registro<IGuardable>.Quitar(this);
        }

        private string carpeta;
        private string ruta;
        private ServicioGuardado servicio;
        private GameObject raiz;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            carpeta = Path.Combine(Application.temporaryCachePath, "pendientes_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ruta = Path.Combine(carpeta, "partida.json");
            raiz = new GameObject("ServicioGuardado");
            servicio = raiz.AddComponent<ServicioGuardado>();
            servicio.FijarRuta(ruta);
        }

        [TearDown]
        public void Limpiar()
        {
            if (raiz != null) Object.Destroy(raiz);
            foreach (GuardableDoble doble in Object.FindObjectsByType<GuardableDoble>(FindObjectsSortMode.None))
            {
                Object.Destroy(doble.gameObject);
            }
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
        }

        private static GuardableDoble Crear(string clave, string valor)
        {
            var objeto = new GameObject("Doble_" + clave);
            objeto.SetActive(false);
            var doble = objeto.AddComponent<GuardableDoble>();
            doble.clave = clave;
            doble.valor = valor;
            objeto.SetActive(true);
            return doble;
        }

        private Dictionary<string, string> LeerArchivo()
        {
            Assert.IsTrue(ArchivoPartida.TryLeer(File.ReadAllText(ruta), out List<PiezaGuardada> piezas, out _));
            var resultado = new Dictionary<string, string>();
            foreach (PiezaGuardada pieza in piezas) resultado[pieza.clave] = pieza.datos;
            return resultado;
        }

        [UnityTest]
        public IEnumerator Guardar_TrasQuitarMechaEInventario_ElArchivoLosConserva()
        {
            GuardableDoble mecha = Crear("mecha", "{\"piezas\":1}");
            GuardableDoble inventario = Crear("inventario", "tres cosas");
            GuardableDoble perfil = Crear("perfil", "Ana");
            yield return null;
            Assert.IsTrue(servicio.Guardar());

            // Como al cambiar a una escena que no los tiene.
            Object.Destroy(mecha.gameObject);
            Object.Destroy(inventario.gameObject);
            yield return null;
            Assert.IsTrue(servicio.Cargar());
            perfil.valor = "Ana2";
            Assert.IsTrue(servicio.Guardar());

            Dictionary<string, string> archivo = LeerArchivo();
            Assert.AreEqual("{\"piezas\":1}", archivo["mecha"]);
            Assert.AreEqual("tres cosas", archivo["inventario"]);
            Assert.AreEqual("Ana2", archivo["perfil"], "el registrado gana sobre lo leído");
        }

        [UnityTest]
        public IEnumerator Registrar_MechaNuevoTrasGuardarSinEl_RecibeLoGuardado()
        {
            GuardableDoble mecha = Crear("mecha", "original");
            Crear("inventario", "inv");
            yield return null;
            Assert.IsTrue(servicio.Guardar());
            Object.Destroy(mecha.gameObject);
            Object.Destroy(GameObject.Find("Doble_inventario"));
            yield return null;
            Assert.IsTrue(servicio.Cargar());
            Assert.IsTrue(servicio.Guardar());

            GuardableDoble nuevo = Crear("mecha", "vacio");
            yield return null;

            Assert.AreEqual("original", nuevo.valor);
            Assert.AreEqual("original", LeerArchivo()["mecha"]);
        }

        [UnityTest]
        public IEnumerator Borrar_VaciaLasPendientes_NoSeReescribenAlGuardar()
        {
            GuardableDoble mecha = Crear("mecha", "original");
            Crear("otro", "x");
            yield return null;
            Assert.IsTrue(servicio.Guardar());
            Object.Destroy(mecha.gameObject);
            yield return null;
            Assert.IsTrue(servicio.Cargar());

            servicio.Borrar();
            Assert.IsTrue(servicio.Guardar());

            Assert.IsFalse(LeerArchivo().ContainsKey("mecha"));
        }
    }
}
