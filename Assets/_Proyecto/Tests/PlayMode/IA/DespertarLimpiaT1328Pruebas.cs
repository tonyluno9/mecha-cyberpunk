using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1328: al despertar con el Doc (EventosJugador.Desperto) los perseguidores sueltan al jugador: el pandillero que lo perseguía vuelve a Esperando
    /// sin objetivo, la policía se va y las estrellas quedan en 0 (el doctor borra el calor antes de publicar el aviso).
    /// </summary>
    public class DespertarLimpiaT1328Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroAPie.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";

        private GameObject piso;
        private GameObject jugador;
        private GameObject pandillero;
        private GameObject policia;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosJugador.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(pandillero);
            Object.DestroyImmediate(policia);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosJugador.LimpiarSuscriptores();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Despertar_PandilleroQuePersegia_SeCalmaYSinObjetivo()
        {
            pandillero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero), new Vector3(0f, 0.05f, 6f), Quaternion.identity);
            PandilleroAPie p = pandillero.GetComponent<PandilleroAPie>();
            float fin = Time.time + 5f;
            while (!p.EnPelea && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(p.EnPelea, "El pandillero debería perseguir al jugador cerca antes de despertar.");

            EventosJugador.PublicarDesperto();

            Assert.AreEqual(EstadoPandillero.Esperando, p.Estado);
            Assert.IsFalse(p.EnPelea);
            Assert.IsNull(p.ObjetivoActual, "Ya no apunta a nadie.");
        }

        [UnityTest]
        public IEnumerator Despertar_PoliciaSeVa()
        {
            policia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), new Vector3(0f, 0.05f, 8f), Quaternion.identity);
            yield return null;
            Assert.IsTrue(policia != null);

            EventosJugador.PublicarDesperto();
            yield return null;

            Assert.IsTrue(policia == null, "La policía se despawnea al despertar.");
        }
    }
}
