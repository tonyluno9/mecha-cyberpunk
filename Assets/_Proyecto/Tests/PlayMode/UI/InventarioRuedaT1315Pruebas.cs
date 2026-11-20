using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Inventario;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.UI.Tiendas;
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
    /// T-1315 en Ciudad_Prologo con la entrada simulada: en el inventario (I) se quita un arma de la rueda con Enter y la rueda (Tab) ya no la muestra; el HUD
    /// dice la tecla real de consumir ([B] y el objeto) y B consume; el inventario muestra los implantes con su descripción; cada máquina expendedora de la
    /// escena deja comprar. Con CAPTURAR_UI=1 guarda docs/capturas/t-1315/*.png.
    /// </summary>
    public class InventarioRuedaT1315Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_UI";
        private const string CarpetaCapturas = "docs/capturas/t-1315/";
        private const float SegundosCiudad = 30f;
        private const float SegundosEntrada = 0.8f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            EquipoRueda.Reiniciar();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            EquipoRueda.Reiniciar();
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
                    && Registro<ServicioInventario>.Actual != null && Registro<ReservaBalas>.Actual != null && Registro<ServicioImplantes>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);
        }

        private static void Foto(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar(CarpetaCapturas + nombre + ".png", Registro<IJugador>.Actual.Camara);
            }
        }

        private static DatosConsumible ConsumibleConBebida()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DatosConsumible"))
            {
                DatosConsumible c = AssetDatabase.LoadAssetAtPath<DatosConsumible>(AssetDatabase.GUIDToAssetPath(guid));
                if (c != null && c.SegundosSinDanio > 0f)
                {
                    return c;
                }
            }
            return null;
        }

        private static bool ControlActivo()
        {
            IJugador j = Registro<IJugador>.Actual;
            ControladorAPie c = j != null && j.Raiz != null ? j.Raiz.GetComponentInChildren<ControladorAPie>() : null;
            return c != null && c.ControlActivo;
        }

        [UnityTest]
        public IEnumerator I_Enter_QuitaUnArmaDeLaRueda_YLaRuedaYaNoLaMuestra()
        {
            yield return CargarCiudad();
            PantallaInventario pantalla = Object.FindFirstObjectByType<PantallaInventario>(FindObjectsInactive.Include);
            RuedaArmasObjetos rueda = Object.FindFirstObjectByType<RuedaArmasObjetos>(FindObjectsInactive.Include);
            Assert.IsNotNull(pantalla);
            Assert.IsNotNull(rueda);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            arsenal.DarMejora(ArmaAPie.Escopeta);
            arsenal.DarMejora(ArmaAPie.Fusil);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosConsumible bebida = ConsumibleConBebida();
            Assert.IsNotNull(bebida, "Hay una bebida");
            inventario.Agregar(bebida, 2);

            // Rueda abierta antes de quitar nada: el fusil sale.
            Press(raton.middleButton);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(RuedaArmasObjetos.Abierta, "Tab abre la rueda");
            Assert.IsTrue(rueda.CasillaVisible((int)ArmaAPie.Fusil), "El fusil va en la rueda por omisión");
            Release(raton.middleButton);
            yield return new WaitForSecondsRealtime(0.3f);

            // I abre; E pasa a ARMAS; se elige el fusil y Enter lo quita.
            yield return Pulsar(teclado.iKey);
            Assert.IsTrue(pantalla.Abierta);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Cursor libre para el clic");
            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(CategoriaInventario.Armas, pantalla.Categoria);
            int posicion = -1;
            for (int i = 0; i < pantalla.Visibles.Count; i++)
            {
                if (pantalla.Visibles[i].Id == "arma_Fusil")
                {
                    posicion = i;
                }
            }
            Assert.GreaterOrEqual(posicion, 0, "El fusil sale en ARMAS");
            pantalla.Elegir(posicion);
            Assert.IsTrue(pantalla.Elegida.Equipable);
            Assert.IsTrue(pantalla.Elegida.Equipada);
            Assert.AreEqual(PantallaInventario.TextoQuitar, pantalla.TextoBotonEquipo);
            Assert.AreEqual(PantallaInventario.TextoMarcaRueda, pantalla.MarcaDe(posicion), "La casilla lleva su marca de rueda");
            yield return new WaitForSecondsRealtime(SegundosEntrada);
            Foto("inventario-armas");
            yield return Pulsar(teclado.enterKey);
            Assert.IsFalse(pantalla.Elegida.Equipada, "Enter quita el fusil de la rueda");
            Assert.AreEqual(string.Empty, pantalla.MarcaDe(posicion));
            Assert.AreEqual(PantallaInventario.TextoEquipar, pantalla.TextoBotonEquipo);
            Assert.IsFalse(EquipoRueda.Equipado("arma_Fusil"));
            yield return Pulsar(teclado.iKey);
            Assert.IsFalse(pantalla.Abierta);

            Press(raton.middleButton);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(RuedaArmasObjetos.Abierta);
            Assert.IsFalse(rueda.CasillaVisible((int)ArmaAPie.Fusil), "La rueda ya no muestra el fusil");
            Assert.IsTrue(rueda.CasillaVisible((int)ArmaAPie.Escopeta), "Lo demás sigue en la rueda");
            Release(raton.middleButton);
            yield return new WaitForSecondsRealtime(0.3f);

            // Volver a ponerlo: F lo equipa y la rueda lo vuelve a mostrar.
            yield return Pulsar(teclado.iKey);
            yield return Pulsar(teclado.eKey);
            pantalla.Elegir(posicion);
            yield return Pulsar(teclado.fKey);
            Assert.IsTrue(pantalla.Elegida.Equipada, "F lo vuelve a equipar");
            yield return Pulsar(teclado.iKey);
            Press(raton.middleButton);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(rueda.CasillaVisible((int)ArmaAPie.Fusil));
            Release(raton.middleButton);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        [UnityTest]
        public IEnumerator Inventario_MuestraImplantes_ConSuDescripcion_DeSoloLectura()
        {
            yield return CargarCiudad();
            PantallaInventario pantalla = Object.FindFirstObjectByType<PantallaInventario>(FindObjectsInactive.Include);
            ServicioImplantes servicio = Registro<ServicioImplantes>.Actual;
            DatosImplante datos = null;
            foreach (string guid in AssetDatabase.FindAssets("t:DatosImplante"))
            {
                DatosImplante d = AssetDatabase.LoadAssetAtPath<DatosImplante>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && !string.IsNullOrEmpty(d.Descripcion) && d.Nivel == 1)
                {
                    datos = d;
                    break;
                }
            }
            Assert.IsNotNull(datos, "Hay un implante con descripción");
            servicio.Regalar(datos);
            yield return Pulsar(teclado.iKey);
            Assert.IsTrue(pantalla.Abierta);
            for (int i = 0; i < 4; i++)
            {
                yield return Pulsar(teclado.eKey);
            }
            Assert.AreEqual(CategoriaInventario.Implantes, pantalla.Categoria);
            Assert.GreaterOrEqual(pantalla.Visibles.Count, 1, "El implante instalado sale");
            EntradaInventario entrada = pantalla.Elegida;
            Assert.AreEqual(datos.Nombre.ToUpperInvariant(), entrada.Nombre);
            Assert.AreEqual(datos.Descripcion, entrada.Descripcion, "Dice para qué sirve");
            Assert.IsFalse(entrada.Equipable, "Los implantes no van en la rueda");
            Assert.AreEqual(PantallaInventario.TextoSoloLectura, pantalla.TextoBotonEquipo);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(entrada.Id, pantalla.Elegida.Id, "Enter no cambia nada en un implante");
            yield return new WaitForSecondsRealtime(SegundosEntrada);
            Foto("inventario-implantes");
        }

        [UnityTest]
        public IEnumerator Hud_DiceLaTeclaDeConsumir_YLaTeclaConsume()
        {
            yield return CargarCiudad();
            HudConsumibles hud = Object.FindFirstObjectByType<HudConsumibles>();
            Assert.IsNotNull(hud, "El HUD a pie trae la línea de consumo");
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosConsumible bebida = ConsumibleConBebida();
            inventario.Agregar(bebida, 2);
            yield return new WaitForSecondsRealtime(0.5f);
            string tecla = TeclasHud.Texto(AccionConsumirEquipado.Accion, "?").ToUpperInvariant();
            Assert.AreEqual("E", tecla);
            Assert.IsNotNull(hud.ObjetoEquipado, "Hay un objeto equipado para consumir");
            StringAssert.StartsWith("[" + tecla + "] ", hud.TextoConsumo.text);
            StringAssert.Contains(hud.ObjetoEquipado.Nombre.ToUpperInvariant(), hud.TextoConsumo.text);
            Foto("hud-consumir");

            DatosConsumible antes = hud.ObjetoEquipado;
            int cantidadAntes = inventario.Cantidad(antes);
            Assert.IsTrue(ControlActivo(), "El jugador tiene el control");
            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(cantidadAntes - 1, inventario.Cantidad(antes), "E consume una pieza del objeto equipado");

            // Lo que se quita de la rueda ya no es el objeto de la tecla.
            EquipoRueda.Poner(antes.Id, false);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreNotEqual(antes, hud.ObjetoEquipado, "Quitado de la rueda, B ya no lo consume");
        }

        [UnityTest]
        public IEnumerator Ciudad_TodaMaquinaExpendedora_DejaComprar()
        {
            yield return CargarCiudad();
            yield return new WaitForSecondsRealtime(1f);
            Expendedora[] maquinas = Object.FindObjectsByType<Expendedora>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.GreaterOrEqual(maquinas.Length, 3, "Las tres del colocador, más las de adorno");
            foreach (Expendedora m in maquinas)
            {
                Assert.IsNotNull(m.Tienda, m.name + " tiene su tienda");
                Assert.Greater(m.EnVenta.Count, 0, m.name + " tiene algo que vender");
                Assert.IsTrue(m.PuedeUsar, m.name + " se puede usar");
                Assert.IsNotNull(m.GetComponentInChildren<Collider>(), m.name + " tiene collider para apuntarle");
            }
            // Ninguna máquina de la escena (por nombre) se quedó de adorno.
            int sinComponente = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name.StartsWith(ColocadorExpendedoras.PrefijoDecorada, StringComparison.Ordinal) && t.GetComponentInParent<Expendedora>(true) == null
                    && t.GetComponentInChildren<Expendedora>(true) == null)
                {
                    sinComponente++;
                    Debug.LogWarning("Expendedora de adorno: " + t.name);
                }
            }
            Assert.AreEqual(0, sinComponente, "Toda CY06_Expendedora de la escena vende");
            Debug.Log("[UI] T-1315 expendedoras vivas: " + maquinas.Length);
        }
    }
}
