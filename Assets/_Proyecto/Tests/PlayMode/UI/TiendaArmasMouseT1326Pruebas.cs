using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Kit;
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
    /// T-1326: en El Mercado el arma 3D se gira arrastrando con el ratón (o con Q/E), el cursor resalta las filas, un clic elige/compra y el botón SALIR cierra;
    /// el banco de Tino también tiene botón SALIR. Todo con el ratón simulado de InputTestFixture.
    /// </summary>
    public class TiendaArmasMouseT1326Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        // El centro de un rectángulo en px de pantalla (los lienzos son Screen Space Overlay).
        private static Vector2 Centro(Transform t)
        {
            RectTransform r = (RectTransform)t;
            return RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
        }

        private IEnumerator Clic(Transform objetivo)
        {
            Set(raton.position, Centro(objetivo));
            yield return null;
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        private IEnumerator AbrirMercado(Action<PantallaTienda> alAbrir)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null && Registro<ICartera>.Actual != null,
                "jugador, arsenal y cartera", 30f);
            yield return new WaitForSecondsRealtime(1f);
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(20000 - cartera.Dinero, MotivoDinero.Otro);
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                mostrador = m.name.Contains("MercadoNegro") ? m : mostrador;
            }
            Assert.IsNotNull(mostrador, "Está el mostrador de El Mercado");
            PantallaTienda pantalla = PantallaDeTiendas();
            pantalla.Mostrar(mostrador.Tienda, mostrador.EnVenta, () => pantalla.Ocultar(), mostrador.Compra, mostrador.AceptaFavorDescuento);
            yield return EsperarHasta(() => pantalla.Visible, "abrir El Mercado", 5f);
            yield return EsperarHasta(() => pantalla.Mercado != null && pantalla.Mercado.Holograma != null && pantalla.Mercado.Holograma.Textura != null, "holograma del arma", 10f);
            yield return new WaitForSecondsRealtime(0.5f);
            alAbrir(pantalla);
        }

        private static float Cambio(float antes, float despues) => Mathf.Abs(Mathf.DeltaAngle(antes, despues));

        [UnityTest]
        public IEnumerator ElMercado_ArrastrarConElRaton_GiraElArma_YLaRuedaLaAcerca()
        {
            PantallaTienda pantalla = null;
            yield return AbrirMercado(p => pantalla = p);
            VistaHologramaArma holo = pantalla.Mercado.Holograma;
            Transform area = pantalla.Mercado.AreaHolograma;
            Set(raton.position, Centro(area));
            yield return null;
            yield return null;

            // Sin apretar el botón, mover el ratón no gira el arma.
            float antes = holo.Giro;
            Set(raton.delta, new Vector2(80f, 0f));
            yield return null;
            Assert.Less(Cambio(antes, holo.Giro), 15f, "Sin apretar el botón el arma no gira con el ratón");

            // Arrastrar con el botón izquierdo sobre el holograma.
            Press(raton.leftButton);
            yield return null;
            antes = holo.Giro;
            for (int i = 0; i < 4; i++)
            {
                Set(raton.delta, new Vector2(60f, 0f));
                yield return null;
            }
            Release(raton.leftButton);
            yield return null;
            Assert.Greater(Cambio(antes, holo.Giro), 30f, "Arrastrar gira el arma");

            // Arrastrar empezando fuera del holograma no la gira. Se empieza en una esquina vacía: sobre una fila, soltar la elige y el holograma cambia de arma.
            Vector2 fuera = new Vector2(4f, 4f);
            Assert.IsFalse(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)area, fuera, null), "La esquina queda fuera del holograma");
            Set(raton.position, fuera);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            antes = holo.Giro;
            for (int i = 0; i < 3; i++)
            {
                Set(raton.delta, new Vector2(60f, 0f));
                yield return null;
            }
            Release(raton.leftButton);
            yield return null;
            Assert.Less(Cambio(antes, holo.Giro), 15f, "Arrastrar fuera del holograma no gira el arma");

            // La rueda sobre el holograma la acerca.
            Set(raton.position, Centro(area));
            yield return null;
            float distancia = holo.Distancia;
            Set(raton.scroll, new Vector2(0f, 120f));
            yield return null;
            Assert.AreNotEqual(distancia, holo.Distancia, "La rueda acerca o aleja el arma");
        }

        [UnityTest]
        public IEnumerator ElMercado_TeclasQyE_GiranElArmaSinRaton()
        {
            PantallaTienda pantalla = null;
            yield return AbrirMercado(p => pantalla = p);
            VistaHologramaArma holo = pantalla.Mercado.Holograma;
            foreach (var tecla in new[] { teclado.eKey, teclado.qKey })
            {
                float antes = holo.Giro;
                Press(tecla);
                yield return new WaitForSecondsRealtime(0.5f);
                Release(tecla);
                yield return null;
                Assert.Greater(Cambio(antes, holo.Giro), 15f, "La tecla " + tecla.displayName + " gira el arma");
            }
        }

        [UnityTest]
        public IEnumerator ElMercado_CursorResaltaLaFila_ClicEligeYCompra_ClicSalirCierra()
        {
            PantallaTienda pantalla = null;
            yield return AbrirMercado(p => pantalla = p);
            Assert.GreaterOrEqual(pantalla.CantidadFilas, 2);

            // Pasar el cursor resalta la fila.
            Transform fila1 = Buscar(pantalla.PanelLista, "Fila_1");
            HoverH10 hover = fila1.GetComponent<HoverH10>();
            float hasta = Time.realtimeSinceStartup + 0.5f;
            while (!hover.Encendido && Time.realtimeSinceStartup < hasta)
            {
                Set(raton.position, Centro(fila1) + new Vector2(Time.frameCount % 2, 0f));
                yield return null;
            }
            Assert.IsTrue(hover.Encendido, "La fila bajo el cursor se resalta");

            // Clic elige; otro clic sobre la elegida compra.
            yield return Clic(fila1);
            Assert.AreEqual(1, pantalla.IndiceElegido, "El clic elige la fila");
            ICartera cartera = Registro<ICartera>.Actual;
            int antes = cartera.Dinero;
            yield return Clic(fila1);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Less(cartera.Dinero, antes, "Otro clic sobre la fila elegida compra");

            // El botón SALIR cierra la tienda con un clic.
            Transform salir = Buscar(pantalla.transform, "BotonSalir");
            Assert.IsNotNull(salir, "Hay un botón SALIR");
            yield return Clic(salir);
            yield return EsperarHasta(() => !pantalla.Visible, "cerrar con el clic en SALIR", 5f);
        }

        [UnityTest]
        public IEnumerator BancoDeTino_BotonSalir_CierraConUnClic()
        {
            // En una escena vacía de prueba (sin la ciudad) el clic real en SALIR colgaba al editor si esta prueba corría primero; el banco vive en la ciudad.
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null, "la ciudad", 30f);
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            Assert.IsNotNull(config);
            GameObject go = new GameObject("PantallaTinoPrueba");
            PantallaMejoraArmas pantalla = go.AddComponent<PantallaMejoraArmas>();
            pantalla.Configurar(config);
            bool cerro = false;
            pantalla.Mostrar(() =>
            {
                cerro = true;
                pantalla.Ocultar();
            });
            yield return null;
            yield return null;
            Transform salir = Buscar(go.transform, "BotonSalir");
            Assert.IsNotNull(salir, "El banco de Tino tiene un botón SALIR");
            yield return Clic(salir);
            Assert.IsTrue(cerro, "El clic en SALIR cierra el banco");
            Assert.IsFalse(pantalla.Visible);
        }
    

        // T-1330 puso otra PantallaTienda dentro de MostradorClinica (y T-685 las Expendedoras tienen la suya):
        // FindFirstObjectByType podía devolver cualquiera. Las tiendas de la calle usan la que no está dentro de esos dos.
        private static PantallaTienda PantallaDeTiendas()
        {
            foreach (PantallaTienda p in Object.FindObjectsByType<PantallaTienda>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                bool ajena = false;
                for (Transform t = p.transform; t != null; t = t.parent)
                {
                    ajena |= t.name.StartsWith("MostradorClinica") || t.name == "Expendedoras";
                }
                if (!ajena)
                {
                    return p;
                }
            }
            return null;
        }
    }
}
