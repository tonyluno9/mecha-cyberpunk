using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Inventario;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-904 en Ciudad_Prologo con la entrada simulada: la tecla I abre el inventario (cursor libre, el tiempo sigue), muestra lo que el jugador trae de
    /// verdad (armas del arsenal con su filo de rareza, balas de la reserva, objetos del inventario) con los íconos de Resources/Menus, las flechas y Q E
    /// mueven la elección y la categoría, y I o Esc lo cierran sin pausar. No abre con el mapa abierto. Con CAPTURAR_UI=1 guarda docs/capturas/ui-inventario.png.
    /// </summary>
    public class InventarioPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_UI";
        private const float SegundosCiudad = 30f;
        private const float SegundosEntrada = 0.8f;
        private const int Categorias = 6;
        private const int BalasDeRegalo = 20;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            CatalogoDestinosMapa.Limpiar();
            yield return null;
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        private IEnumerator CargarCiudad()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + SegundosCiudad;
            while (Time.realtimeSinceStartup < fin)
            {
                DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>();
                if (director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null
                    && Registro<ServicioInventario>.Actual != null && Registro<ReservaBalas>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);
        }

        [UnityTest]
        public IEnumerator I_AbreElInventario_ConLoQueElJugadorTraeDeVerdad_YICierra()
        {
            yield return CargarCiudad();
            PantallaInventario pantalla = Object.FindFirstObjectByType<PantallaInventario>(FindObjectsInactive.Include);
            Assert.IsNotNull(pantalla, "El mapa trae el inventario de la ciudad");
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ReservaBalas reserva = Registro<ReservaBalas>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;

            // Lo que trae: armas con nivel (el arsenal), balas de reserva y un par de objetos del inventario.
            arsenal.DarMejora(ArmaAPie.Escopeta);
            arsenal.DarMejora(ArmaAPie.Fusil);
            arsenal.DarMejora(ArmaAPie.Fusil);
            reserva.Agregar(ArmaAPie.Escopeta, BalasDeRegalo);
            int objetosDados = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:DatosObjeto"))
            {
                DatosObjeto objeto = AssetDatabase.LoadAssetAtPath<DatosObjeto>(AssetDatabase.GUIDToAssetPath(guid));
                if (objeto != null && !(objeto is DatosArma) && !(objeto is DatosMunicionArma) && objetosDados < 3 && inventario.Agregar(objeto, 1) > 0)
                {
                    objetosDados++;
                }
            }
            Assert.Greater(objetosDados, 0, "Hay objetos para regalar");

            Assert.IsFalse(pantalla.Abierta);
            yield return Pulsar(teclado.iKey);
            Assert.IsTrue(pantalla.Abierta, "I abre el inventario");
            Assert.AreEqual(1f, Time.timeScale, "No pausa el tiempo");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Cursor libre");
            Assert.IsTrue(PilaPantallas.EsArriba(pantalla));

            // Armas: las del arsenal, con su nivel como rareza.
            int armasTiene = 0;
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                if (arsenal.Tiene(arma))
                {
                    armasTiene++;
                }
            }
            Assert.AreEqual(armasTiene, ReglaInventarioUI.Contar(pantalla.Todas, CategoriaInventario.Armas), "Una casilla por arma que tienes");
            EntradaInventario fusil = null;
            foreach (EntradaInventario e in pantalla.Todas)
            {
                if (e.Id == "arma_Fusil")
                {
                    fusil = e;
                }
            }
            Assert.IsNotNull(fusil, "El fusil sale");
            Assert.AreEqual(arsenal.Nivel(ArmaAPie.Fusil), fusil.Rareza, "La rareza es el nivel del arma");
            // Munición: la de la reserva.
            EntradaInventario balas = null;
            foreach (EntradaInventario e in pantalla.Todas)
            {
                if (e.Id == "balas_Escopeta")
                {
                    balas = e;
                }
            }
            Assert.IsNotNull(balas, "Las balas de la escopeta salen");
            Assert.AreEqual(reserva.Cantidad(ArmaAPie.Escopeta), balas.Cantidad, "con las balas reales de la reserva");
            // Objetos: lo que hay en el inventario.
            Assert.GreaterOrEqual(ReglaInventarioUI.Contar(pantalla.Todas, CategoriaInventario.Consumibles) + ReglaInventarioUI.Contar(pantalla.Todas, CategoriaInventario.Otros), 1);
            Assert.AreEqual(Moneda.Texto(Registro<ICartera>.Actual.Dinero), pantalla.Dinero, "Cabecera con el dinero real");

            // Íconos de Resources/Menus y filo de rareza.
            Assert.AreEqual(4, pantalla.IconosDeCategoria, "Los cuatro íconos de categoría (Cat_*) salen");
            Assert.IsTrue(pantalla.TieneIcono(0), "La primera casilla lleva su ícono");
            Assert.AreEqual(TrazoH10.Rareza(pantalla.Elegida.Rareza).r, pantalla.FiloDe(0).r, 0.01f, "El filo de la casilla es el de su rareza");
            Assert.AreEqual(pantalla.Elegida.Nombre, pantalla.DetalleNombre, "El detalle es de la casilla elegida");
            yield return new WaitForSecondsRealtime(SegundosEntrada);
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar("docs/capturas/ui-inventario.png", Registro<IJugador>.Actual.Camara);
            }

            // Flechas mueven la casilla; E pasa a ARMAS y Q regresa.
            yield return Pulsar(teclado.rightArrowKey);
            Assert.AreEqual(1, pantalla.IndiceElegido);
            yield return Pulsar(teclado.leftArrowKey);
            Assert.AreEqual(0, pantalla.IndiceElegido);
            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(CategoriaInventario.Armas, pantalla.Categoria);
            Assert.AreEqual(armasTiene, pantalla.Visibles.Count, "ARMAS solo muestra armas");
            foreach (EntradaInventario e in pantalla.Visibles)
            {
                Assert.AreEqual(CategoriaInventario.Armas, e.Categoria);
            }
            yield return Pulsar(teclado.qKey);
            Assert.AreEqual(CategoriaInventario.Todo, pantalla.Categoria);

            // I lo cierra; también Esc, y ninguno de los dos pausa.
            yield return Pulsar(teclado.iKey);
            Assert.IsFalse(pantalla.Abierta, "I lo cierra");
            Assert.IsTrue(PilaPantallas.Vacia);
            yield return Pulsar(teclado.iKey);
            Assert.IsTrue(pantalla.Abierta);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pantalla.Abierta, "Esc lo cierra");
            PantallaPausa pausa = Object.FindFirstObjectByType<PantallaPausa>();
            Assert.IsFalse(pausa.Abierta, "El Esc que cierra el inventario no pausa");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator I_NoAbreConElMapaAbierto_NiEnPausa()
        {
            yield return CargarCiudad();
            PantallaInventario pantalla = Object.FindFirstObjectByType<PantallaInventario>(FindObjectsInactive.Include);
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto);
            yield return Pulsar(teclado.iKey);
            Assert.IsFalse(pantalla.Abierta, "Con el mapa abierto la I no abre el inventario");
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(mapa.Abierto);

            PantallaPausa pausa = Object.FindFirstObjectByType<PantallaPausa>();
            yield return Pulsar(teclado.escapeKey);
            Assert.IsTrue(pausa.Abierta);
            yield return Pulsar(teclado.iKey);
            Assert.IsFalse(pantalla.Abierta, "En pausa tampoco");
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pausa.Abierta);
        }
    }
}
