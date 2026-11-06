using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-816: el HUD a pie usa las imágenes holográficas H01-H04 en el icono del arma y en las casillas, con el material UIHologramaHud (el fondo
    /// negro no se ve y toman el color del HUD); los puños se quedan con su icono. Con CAPTURAR_S2N_HUD=1 guarda docs/capturas/s2n-hud-arma.png.
    /// </summary>
    public class HudHologramaArmaPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaEscopeta = "Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_escopeta_0.asset";
        private const string VariableCaptura = "CAPTURAR_S2N_HUD";
        private const float SegundosEspera = 5f;

        private Keyboard teclado;
        private Mouse raton;
        private readonly HashSet<GameObject> raicesIniciales = new HashSet<GameObject>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            raicesIniciales.Clear();
            raicesIniciales.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raicesIniciales.Contains(raiz))
                {
                    Object.Destroy(raiz);
                }
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que)
        {
            float limite = Time.realtimeSinceStartup + SegundosEspera;
            while (!condicion())
            {
                if (Time.realtimeSinceStartup > limite)
                {
                    Assert.Fail("No pasó a tiempo: " + que);
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HudAPie_ArmasConHolograma_IconoYCasillasHolograficasYPunosIgual()
        {
            PiezasUI piezas = PiezasUI.Actual;
            Assert.IsNotNull(piezas);
            for (int i = 1; i <= 4; i++)
            {
                Assert.IsTrue(piezas.EsHolograma(i), "el arma " + (ArmaAPie)i + " tiene imagen holográfica");
                StringAssert.StartsWith("H0" + i, piezas.ArmaHud(i).name);
            }
            Assert.IsFalse(piezas.EsHolograma(0), "los puños no tienen holograma");
            Assert.AreSame(piezas.Arma(0), piezas.ArmaHud(0), "los puños se quedan con su icono");

            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 3f;
            GameObject jugadorGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            yield return null;
            yield return null;
            Registro<ServicioInventario>.Actual.Agregar(AssetDatabase.LoadAssetAtPath<DatosArma>(RutaEscopeta), 1);
            HudAPie hud = jugadorGo.GetComponentInChildren<HudAPie>(true);
            ArmasAPie armas = jugadorGo.GetComponent<ArmasAPie>();
            Assert.IsNotNull(hud, "el jugador trae el HUD");
            Assert.IsNotNull(armas);
            yield return EsperarHasta(() => armas.ArmaActiva == ArmaAPie.Escopeta || armas.Equipar(ArmaAPie.Escopeta), "equipar la escopeta");
            yield return EsperarHasta(() => hud.IconoArma.sprite == piezas.ArmaHud((int)armas.ArmaActiva), "el icono del arma activa");
            yield return new WaitForSecondsRealtime(0.3f);

            int activa = (int)armas.ArmaActiva;
            Assert.AreEqual((int)ArmaAPie.Escopeta, activa);
            Assert.AreSame(piezas.ArmaHud(activa), hud.IconoArma.sprite);
            Assert.IsTrue(hud.IconoArma.enabled);
            if (piezas.EsHolograma(activa))
            {
                Assert.AreSame(piezas.MaterialArmaHud(activa), hud.IconoArma.material, "el icono holográfico usa su material");
                Assert.AreEqual("MechaCyberpunk/UI/HologramaHud", hud.IconoArma.material.shader.name);
            }
            Assert.AreSame(piezas.ArmaHud(1), hud.CasillasArma[1].sprite, "la casilla de la pistola es holográfica");
            Assert.AreSame(piezas.MaterialArmaHud(1), hud.CasillasArma[1].material);
            Assert.AreSame(piezas.Arma(0), hud.CasillasArma[0].sprite, "la casilla de los puños, su icono");
            Assert.AreNotSame(piezas.MaterialArmaHud(1), hud.CasillasArma[0].material, "y su material de siempre");

            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Camera camara = jugadorGo.GetComponentInChildren<Camera>();
                CapturaUI.Capturar("docs/capturas/s2n-hud-arma.png", camara);
            }
        }
    }
}
