using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Los avisos del llamado del mecha en el HUD a pie, en Pruebas_Calle con entrada simulada (M).</summary>
    public class HudLlamadoPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float ZFueraDeZona = 90f;
        private const float AlturaJugadorFuera = 0.05f;
        private const float MargenEspera = 2f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private LlamadoMecha llamado;
        private HudAPie hud;
        private ConfigHudAPie config;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            config = AssetDatabase.LoadAssetAtPath<ConfigHudAPie>(RutaConfig);
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            llamado = Object.FindFirstObjectByType<LlamadoMecha>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(llamado, "Pruebas_Calle trae el mecha con LlamadoMecha");
            Assert.IsNotNull(hud, "Pruebas_Calle trae el HUD a pie");
            yield return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator EsperarEstado(EstadoLlamado estado, float maxSegundos)
        {
            float fin = Time.time + maxSegundos;
            while (Time.time < fin && llamado.Estado != estado)
            {
                yield return null;
            }
            Assert.AreEqual(estado, llamado.Estado);
        }

        [UnityTest]
        public IEnumerator Aviso_AlLlamar_MuestraMechaEnCaminoConCuentaAtras()
        {
            yield return CargarCalle();
            Assert.IsFalse(hud.GrupoLlamado.activeSelf, "Sin llamar no hay aviso");

            yield return Tecla(teclado.mKey);
            Assert.IsTrue(hud.GrupoLlamado.activeSelf);
            Assert.AreEqual("MECHA EN CAMINO · 6", hud.TextoLlamado.text);

            yield return Esperar(2.5f);
            Assert.AreEqual("MECHA EN CAMINO · 4", hud.TextoLlamado.text, "La cuenta atrás baja");
            yield return Esperar(2f);
            Assert.AreEqual("MECHA EN CAMINO · 2", hud.TextoLlamado.text);

            yield return EsperarEstado(EstadoLlamado.EnCalle, llamado.Config.SegundosLlegada + MargenEspera);
            yield return null;
            Assert.IsFalse(hud.GrupoLlamado.activeSelf, "Al llegar el aviso se va");
        }

        [UnityTest]
        public IEnumerator Aviso_FueraDeZona_MuestraZonaProhibidaYSeVa()
        {
            yield return CargarCalle();
            jugador.Colocar(new Pose(new Vector3(0f, AlturaJugadorFuera, ZFueraDeZona), Quaternion.identity));
            yield return null;

            yield return Tecla(teclado.mKey);
            Assert.IsTrue(hud.GrupoLlamado.activeSelf);
            Assert.AreEqual(config.TextoZonaProhibida, hud.TextoLlamado.text);
            Assert.AreEqual("AQUÍ NO PUEDE LLEGAR TU MECHA", hud.TextoLlamado.text);

            yield return Esperar(config.DuracionAvisoLlamado + 0.5f);
            Assert.IsFalse(hud.GrupoLlamado.activeSelf, "El aviso se va solo");
        }

        [UnityTest]
        public IEnumerator Aviso_AlDevolver_MuestraElRegresoYSeVa()
        {
            yield return CargarCalle();
            yield return Tecla(teclado.mKey);
            yield return EsperarEstado(EstadoLlamado.EnCalle, llamado.Config.SegundosLlegada + MargenEspera);

            yield return Tecla(teclado.mKey);
            Assert.IsTrue(hud.GrupoLlamado.activeSelf);
            Assert.AreEqual("TU MECHA REGRESA AL TALLER", hud.TextoLlamado.text);

            yield return EsperarEstado(EstadoLlamado.EnTaller, llamado.Config.SegundosRegreso + MargenEspera);
            yield return null;
            Assert.IsFalse(hud.GrupoLlamado.activeSelf, "Al llegar al taller el aviso se va");
        }
    }
}
