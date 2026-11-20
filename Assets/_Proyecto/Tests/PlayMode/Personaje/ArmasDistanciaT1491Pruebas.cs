using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1491: el subfusil a 60 m y el francotirador a 150 m le bajan vida a un blanco; los trazos y el marcador de la granada se ven.</summary>
    public class ArmasDistanciaT1491Pruebas : BaseJugadorAPie
    {
        private class Blanco : MonoBehaviour, IRecibeDanio
        {
            public float Total;
            public int Golpes;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Total += info.Cantidad;
                Golpes++;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private Blanco PonerBlanco(float z)
        {
            // Un panel ancho: la dispersión del arma no lo falla.
            GameObject g = Caja(new Vector3(0f, 3f, z), new Vector3(14f, 8f, 0.5f));
            g.name = "BlancoT1491";
            return g.AddComponent<Blanco>();
        }

        private IEnumerator Preparar(ArmaAPie arma)
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
            yield return null;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(arma), arma + " se equipa");
            yield return Esperar(0.3f);
        }

        private IEnumerator Disparos(int cuantos, float entre)
        {
            for (int i = 0; i < cuantos; i++)
            {
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return Esperar(entre);
            }
        }

        [UnityTest]
        public IEnumerator Subfusil_ABlancoA60m_BajaVida()
        {
            Blanco blanco = PonerBlanco(60f);
            yield return Preparar(ArmaAPie.Subfusil);
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            yield return Disparos(4, 0.15f);
            Release(raton.rightButton);
            Debug.Log("[T1491] subfusil 60 m: " + blanco.Golpes + " impactos, " + blanco.Total + " de daño");
            Assert.Greater(blanco.Golpes, 0, "El subfusil alcanza a 60 m");
            Assert.Greater(blanco.Total, 0f);
            Assert.Less(blanco.Total / blanco.Golpes, jugador.GetComponent<PistolaAPie>().DatosActuales.Danio, "Con la caída hace menos que el daño base");
        }

        [UnityTest]
        public IEnumerator Francotirador_ABlancoA150m_HaceDanioCompleto()
        {
            Blanco blanco = PonerBlanco(150f);
            yield return Preparar(ArmaAPie.Sniper);
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            yield return Disparos(1, 0.2f);
            Release(raton.rightButton);
            float esperado = jugador.GetComponent<PistolaAPie>().DatosActuales.Danio;
            Debug.Log("[T1491] sniper 150 m: " + blanco.Golpes + " impactos, " + blanco.Total + " de daño");
            Assert.AreEqual(1, blanco.Golpes, "El francotirador alcanza a 150 m");
            Assert.GreaterOrEqual(blanco.Total, esperado - 0.01f, "Sin caída de daño");
        }

        [UnityTest]
        public IEnumerator Disparar_DibujaUnTrazo()
        {
            PonerBlanco(30f);
            yield return Preparar(ArmaAPie.Sniper);
            TrazosBala trazos = jugador.GetComponent<TrazosBala>();
            Assert.IsNotNull(trazos, "El jugador lleva TrazosBala");
            int antes = trazos.Dibujados;
            yield return Disparos(1, 0.01f);
            Assert.Greater(trazos.Dibujados, antes, "El tiro dibuja un trazo");
            Assert.Greater(trazos.Vivos, 0, "Está vivo justo después del tiro");
            yield return Esperar(0.3f);
            Assert.AreEqual(0, trazos.Vivos, "Se desvanece rápido");
        }

        [UnityTest]
        public IEnumerator Granada_AlApuntar_MuestraMarcadorDeCaida()
        {
            Caja(new Vector3(0f, -0.25f, 12f), new Vector3(40f, 0.5f, 40f)); // un piso donde cae
            yield return Preparar(ArmaAPie.Granada);
            ArmasEspecialesAPie especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            Assert.IsFalse(especiales.MarcadorVisible, "Sin apuntar no hay marcador");
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            bool arco = especiales.ArcoVisible;
            bool marcador = especiales.MarcadorVisible;
            Release(raton.rightButton);
            yield return Esperar(0.2f);
            Assert.IsTrue(arco, "Se ve el arco");
            Assert.IsTrue(marcador, "Se ve donde caerá");
            Assert.IsFalse(especiales.MarcadorVisible, "Al dejar de apuntar se apaga");
        }
    }
}
