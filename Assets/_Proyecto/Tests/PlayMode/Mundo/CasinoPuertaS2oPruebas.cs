using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-884 (playtest 7-nov): en la ciudad del juego, la puerta del casino es de cristal y automática, como de tienda: con el jugador a pie
    /// frente a ella se desliza de lado sola (sin E) y se vuelve a cerrar cuando se va.
    /// </summary>
    public class CasinoPuertaS2oPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float SegundosParaAbrir = 1.5f;
        private const float SegundosParaCerrar = 6f;
        private const float MetrosDeslizados = 1.5f;
        private const float MetrosFrente = 2.5f;
        private const float MetrosLejos = 30f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Casino_ConElJugadorEnfrente_ElCristalSeDeslizaSolo()
        {
            IPuerta puerta = null;
            foreach (IPuerta p in Registro<IPuerta>.Todos)
            {
                if (p.Lugar == IdLugar.Casino)
                {
                    puerta = p;
                }
            }
            Assert.IsNotNull(puerta, "la puerta del casino");
            HojaPuerta hoja = ((Component)puerta).GetComponentInChildren<HojaPuerta>();
            Assert.IsTrue(hoja.Corrediza, "se desliza de lado, no sube al dintel");
            Assert.Less(hoja.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").a, 0.5f, "es de cristal");
            Vector3 cerrada = hoja.transform.position;

            IJugador jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "hay jugador");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            // La fachada del casino da al poniente (la calle está en -x).
            Vector3 haciaCalle = Vector3.left;
            Mover(jugador, puerta.Posicion + haciaCalle * MetrosFrente);
            yield return new WaitForSeconds(SegundosParaAbrir);
            Assert.IsTrue(puerta.EstaAbierta, "se abrió sola, sin E");
            Vector3 movimiento = hoja.transform.position - cerrada;
            Assert.Less(Mathf.Abs(movimiento.y), 0.05f, "no sube");
            Assert.Greater(new Vector2(movimiento.x, movimiento.z).magnitude, MetrosDeslizados, "se hizo a un lado");

            Mover(jugador, puerta.Posicion + haciaCalle * MetrosLejos);
            yield return new WaitForSeconds(SegundosParaCerrar);
            Assert.IsFalse(puerta.EstaAbierta, "se cierra sola cuando te vas");
        }

        private static void Mover(IJugador jugador, Vector3 a)
        {
            CharacterController cc = jugador.Raiz.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
            jugador.Raiz.position = new Vector3(a.x, a.y + 0.1f, a.z);
            if (cc != null)
            {
                cc.enabled = true;
            }
        }
    }
}
