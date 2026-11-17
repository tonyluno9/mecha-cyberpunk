using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1328: el indicador de dirección de daño. Un jugador de prueba mira a +Z; una bala que viaja hacia +X llega desde -X, o sea desde la izquierda:
    /// la marca sale a la izquierda y se desvanece en ~1 s. Una bala que viaja hacia -Z viene de enfrente.
    /// </summary>
    public class IndicadorDanioT1328Pruebas
    {
        private sealed class JugadorFalso : MonoBehaviour, IJugador
        {
            public Transform Raiz => transform;
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
            private void OnEnable() => Registro<IJugador>.Agregar(this);
            private void OnDisable() => Registro<IJugador>.Quitar(this);
        }

        private GameObject jugador;
        private GameObject hud;
        private IndicadorDanio indicador;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            jugador = new GameObject("JugadorFalso");
            jugador.AddComponent<JugadorFalso>();
            hud = new GameObject("HudPrueba");
            indicador = hud.AddComponent<IndicadorDanio>();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(hud);
            Object.DestroyImmediate(jugador);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
        }

        private void Golpear(Vector3 direccionDelGolpe)
        {
            InfoDanio info = new InfoDanio(10f, TipoDanio.Disparo, jugador.transform.position + Vector3.up, direccionDelGolpe, null);
            EventosPelea.PublicarImpacto(null, info, new ResultadoDanio(10f, TipoBloqueo.Ninguno, false));
        }

        [UnityTest]
        public IEnumerator DanioDesdeLaIzquierda_MarcaALaIzquierda_YSeDesvanece()
        {
            yield return null;
            Golpear(Vector3.right);

            Assert.AreEqual(1, indicador.MarcasVisibles);
            Assert.AreEqual(-90f, indicador.UltimoAngulo, 0.5f);
            Assert.Less(indicador.PosicionUltimaMarca.Value.x, -1f, "la marca está a la izquierda del centro");
            Assert.AreEqual(0f, indicador.PosicionUltimaMarca.Value.y, 1f);

            float fin = Time.unscaledTime + IndicadorDanio.Duracion + 0.5f;
            while (Time.unscaledTime < fin)
            {
                yield return null;
            }
            Assert.AreEqual(0, indicador.MarcasVisibles, "la marca se desvaneció");
        }

        [UnityTest]
        public IEnumerator DanioDeEnfrente_MarcaArriba()
        {
            yield return null;
            Golpear(Vector3.back);

            Assert.AreEqual(0f, indicador.UltimoAngulo, 0.5f);
            Assert.Greater(indicador.PosicionUltimaMarca.Value.y, 1f);
        }

        [UnityTest]
        public IEnumerator DanioLejosDelJugador_NoMarca()
        {
            yield return null;
            InfoDanio info = new InfoDanio(10f, TipoDanio.Disparo, jugador.transform.position + Vector3.right * 20f, Vector3.right, null);
            EventosPelea.PublicarImpacto(null, info, new ResultadoDanio(10f, TipoBloqueo.Ninguno, false));
            Assert.AreEqual(0, indicador.MarcasVisibles);
        }
    }
}
