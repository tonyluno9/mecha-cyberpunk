using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Economia
{
    /// <summary>El inventario del jugador: registro, guardado y carga con JugadorAPie.prefab (T-142).</summary>
    public class ServicioInventarioPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaCatalogo = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";

        private Keyboard teclado;
        private readonly HashSet<GameObject> raicesIniciales = new HashSet<GameObject>();
        private CatalogoObjetos catalogo;
        private DatosObjeto brazo;
        private DatosObjeto kit;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            raicesIniciales.Clear();
            raicesIniciales.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(RutaCatalogo);
            brazo = catalogo.Buscar("brazo_chatarra");
            kit = catalogo.Buscar("kit_reparacion");
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raicesIniciales.Contains(raiz)) Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static GameObject CrearJugador() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));

        private static IGuardable BuscarGuardable()
        {
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == "inventario") return g;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Jugador_EnEscenaVacia_TraeUnInventarioVacioYUnGuardable()
        {
            GameObject jugador = CrearJugador();
            yield return null;

            Assert.AreEqual(1, Registro<ServicioInventario>.Cantidad, "Un solo inventario registrado");
            Assert.AreEqual(1, jugador.GetComponentsInChildren<ServicioInventario>(true).Length);
            Assert.AreEqual(0, Registro<ServicioInventario>.Actual.Todos.Count, "Empieza vacío");
            Assert.IsNotNull(BuscarGuardable(), "Hay un IGuardable con clave inventario");
        }

        [UnityTest]
        public IEnumerator Cargar_TrasGuardar_DevuelveDosBrazosYUnKit()
        {
            CrearJugador();
            yield return null;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            IGuardable guardable = BuscarGuardable();
            inventario.Agregar(brazo, 2);
            inventario.Agregar(kit, 1);
            string texto = guardable.Guardar();

            inventario.Quitar(brazo, 2);
            inventario.Quitar(kit, 1);
            Assert.AreEqual(0, inventario.Todos.Count);

            int avisos = 0;
            inventario.Cambio += () => avisos++;
            guardable.Cargar(texto);

            Assert.AreEqual(2, inventario.Cantidad(brazo));
            Assert.AreEqual(1, inventario.Cantidad(kit));
            Assert.AreEqual(1, avisos, "Un solo aviso por la carga");
        }

        [UnityTest]
        public IEnumerator Cargar_ConUnIdDesconocido_LoIgnoraConWarning()
        {
            CrearJugador();
            yield return null;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Economia\].*objeto_que_no_existe"));
            BuscarGuardable().Cargar("{\"objetos\":[{\"id\":\"objeto_que_no_existe\",\"cantidad\":4},{\"id\":\"brazo_chatarra\",\"cantidad\":2}]}");

            Assert.AreEqual(2, inventario.Cantidad(brazo));
            Assert.AreEqual(1, inventario.Todos.Count);
        }

        [UnityTest]
        public IEnumerator Cargar_ConTextoRoto_NoCambiaNadaYAvisa()
        {
            CrearJugador();
            yield return null;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            inventario.Agregar(kit, 2);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Economia\].*inventario"));
            BuscarGuardable().Cargar("basura");

            Assert.AreEqual(2, inventario.Cantidad(kit));
        }

        [UnityTest]
        public IEnumerator Destruir_ElJugador_QuitaElInventarioDeLosRegistros()
        {
            GameObject jugador = CrearJugador();
            yield return null;

            Object.Destroy(jugador);
            yield return null;

            Assert.AreEqual(0, Registro<ServicioInventario>.Cantidad);
            Assert.IsNull(BuscarGuardable());
        }

        [UnityTest]
        public IEnumerator InventarioDePrueba_TeclasNueveYCero_AgreganBrazoYKit()
        {
            GameObject jugador = CrearJugador();
            var herramienta = new GameObject("InventarioDePrueba");
            var comp = herramienta.AddComponent<MechaCyberpunk.Economia.Pruebas.InventarioDePrueba>();
            var so = new SerializedObject(comp);
            so.FindProperty("brazo").objectReferenceValue = brazo;
            so.FindProperty("kit").objectReferenceValue = kit;
            so.ApplyModifiedPropertiesWithoutUndo();
            comp.enabled = false;
            comp.enabled = true;
            yield return null;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;

            Press(teclado.digit9Key);
            yield return null;
            Release(teclado.digit9Key);
            yield return null;
            Press(teclado.digit0Key);
            yield return null;
            Release(teclado.digit0Key);
            yield return null;

            Assert.AreEqual(1, inventario.Cantidad(brazo), "La tecla 9 agrega un brazo");
            Assert.AreEqual(1, inventario.Cantidad(kit), "La tecla 0 agrega un kit");
            Assert.IsNotNull(jugador);
        }
    }
}
