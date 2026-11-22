using System.Collections;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// Bug de CONTINUAR: el jugador aparecía en el vacío y caía. Una posición guardada sin piso, dentro de algo o fuera del mapa no se
    /// usa; se busca el punto seguro más cercano o se queda en el inicio; y si cae bajo el mapa, la red de seguridad lo regresa.
    /// </summary>
    public class PosicionSeguraJugadorPruebas : BaseJugadorAPie
    {
        private static string Guardada(Vector3 p) =>
            "{\"escena\":\"" + SceneManager.GetActiveScene().path + "\",\"posicion\":{\"x\":" + p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"y\":" + p.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"z\":" + p.z.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "},\"giro\":0,\"arma\":\"Punos\"}";

        [UnityTest]
        public IEnumerator Cargar_PosicionValida_LaUsa()
        {
            yield return Calentar();
            jugadorAPie.Cargar(Guardada(new Vector3(10f, 0f, 10f)));
            yield return null;
            Assert.AreEqual(ReglaPosicionSegura.Motivo.Valida, jugadorAPie.UltimaCargaMotivo);
            Assert.AreEqual(10f, jugador.transform.position.x, 0.05f);
            Assert.AreEqual(10f, jugador.transform.position.z, 0.05f);
        }

        [UnityTest]
        public IEnumerator Cargar_SinPisoDebajo_NoSeMueve_YNoCae()
        {
            yield return Calentar();
            Vector3 inicio = jugador.transform.position;
            jugadorAPie.Cargar(Guardada(new Vector3(400f, 3f, 0f)));
            yield return Esperar(1f);
            Assert.AreEqual(ReglaPosicionSegura.Motivo.SinPiso, jugadorAPie.UltimaCargaMotivo, "fuera del piso (como las zonas que se quitaron)");
            Assert.AreEqual(inicio.x, jugador.transform.position.x, 0.2f, "se queda en el inicio");
            Assert.Greater(jugador.transform.position.y, -1f, "no cae");
        }

        [UnityTest]
        public IEnumerator Cargar_DentroDeUnaCaja_VaAlPuntoSeguroMasCercano()
        {
            yield return Calentar();
            Caja(new Vector3(20f, 1.5f, 0f), new Vector3(3f, 3f, 3f));
            yield return null;
            jugadorAPie.Cargar(Guardada(new Vector3(20f, 0f, 0f)));
            yield return null;
            Assert.IsTrue(jugadorAPie.UltimaCargaCorregida, "la guardada estaba ocupada");
            Vector3 p = jugador.transform.position;
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(20f, 0f));
            Assert.Greater(d, 1.5f, "fuera de la caja");
            Assert.Less(d, 5f, "cerca de donde estaba");
            Assert.AreEqual(0f, p.y, 0.15f, "sobre el piso");
        }

        [UnityTest]
        public IEnumerator RedDeSeguridad_SiCaeBajoElMapa_RegresaAlUltimoPuntoSeguro()
        {
            yield return Calentar();
            yield return Esperar(1f);
            RedSeguridadJugador red = jugador.GetComponent<RedSeguridadJugador>();
            Assert.IsNotNull(red, "JugadorAPie agrega la red de seguridad");
            Assert.IsTrue(red.HaySeguro);
            jugadorAPie.Colocar(new Pose(new Vector3(500f, red.AlturaMinimaMapa - 5f, 0f), Quaternion.identity));
            yield return null;
            yield return null;
            Assert.AreEqual(1, red.Rescates);
            Assert.Greater(jugador.transform.position.y, -1f, "regresó arriba");
            Assert.Less(Mathf.Abs(jugador.transform.position.x), 50f, "al último punto seguro");
        }
    }
}
