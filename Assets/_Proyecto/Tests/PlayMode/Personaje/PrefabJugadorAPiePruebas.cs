using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>JugadorAPie.prefab solo, en una escena vacía: trae lo suyo, su HUD y sus servicios, y no deja nada al irse.</summary>
    public class PrefabJugadorAPiePruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        private GameObject jugador;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (jugador != null)
            {
                Object.Destroy(jugador);
            }
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Prefab_Solo_TraeUnJugadorUnObjetivosYUnHud()
        {
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            yield return null;

            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Exactamente un IJugador");
            Assert.AreEqual(1, Registro<IObjetivos>.Cantidad, "Exactamente un IObjetivos");
            HudAPie[] huds = Object.FindObjectsByType<HudAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, huds.Length, "Exactamente un HUD");
            Assert.IsTrue(huds[0].transform.IsChildOf(jugador.transform), "El HUD viene dentro del prefab del jugador");
            Assert.IsNotNull(jugador.GetComponent<AparicionJugador>(), "Trae su componente de aparición");
        }

        [UnityTest]
        public IEnumerator Prefab_AlDestruirlo_LosRegistrosQuedanVacios()
        {
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            yield return null;
            Assert.AreEqual(1, Registro<IJugador>.Cantidad);

            Object.Destroy(jugador);
            yield return null;

            Assert.AreEqual(0, Registro<IJugador>.Cantidad, "Sin jugador");
            Assert.AreEqual(0, Registro<IObjetivos>.Cantidad, "Sin servicio de objetivos");
            Assert.AreEqual(0, Object.FindObjectsByType<HudAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "Sin HUD");
        }

        [UnityTest]
        public IEnumerator Prefab_DosInstanciasSucesivas_NoAcumulanRegistros()
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject copia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
                yield return null;
                Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un jugador en la vuelta " + i);
                Assert.AreEqual(1, Registro<IObjetivos>.Cantidad, "Un servicio en la vuelta " + i);
                Object.Destroy(copia);
                yield return null;
                Assert.AreEqual(0, Registro<IJugador>.Cantidad);
                Assert.AreEqual(0, Registro<IObjetivos>.Cantidad);
            }
        }
    }
}
