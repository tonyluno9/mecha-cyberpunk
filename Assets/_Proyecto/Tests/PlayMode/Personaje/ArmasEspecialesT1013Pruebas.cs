using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1013: las armas nuevas con entrada simulada: el subfusil dispara en ráfaga, el lanzagranadas lanza una granada que explota y daña en área, la
    /// granada se lanza en arco (que se ve al apuntar) y explota al acabarse la mecha, y el rifle de plasma carga 3 s manteniendo el clic y suelta un rayo
    /// de 5 s que atraviesa a la gente y daña todo lo que toca.
    /// </summary>
    public class ArmasEspecialesT1013Pruebas : BaseJugadorAPie
    {
        /// <summary>Un blanco que cuenta el daño que recibe (no se cae ni se restaura).</summary>
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

        private ArmasAPie armas;
        private ArmasEspecialesAPie especiales;

        private void Dar(ArmaAPie arma)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsNotNull(arsenal, "Hay arsenal");
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
        }

        private Blanco PonerBlanco(Vector3 donde)
        {
            GameObject g = Caja(donde, new Vector3(0.6f, 1.8f, 0.6f));
            g.name = "BlancoT1013";
            return g.AddComponent<Blanco>();
        }

        private IEnumerator Preparar(ArmaAPie arma)
        {
            yield return Calentar();
            // T-1014: las armas ya tienen modelo y el rayo y las granadas salen de su boca: sin esto, con la cámara de la prueba el personaje no se anima (fuera de pantalla) y el arma queda de lado.
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            if (visual != null && visual.Animador != null)
            {
                visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            yield return Esperar(0.3f);
            armas = jugador.GetComponent<ArmasAPie>();
            especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            Assert.IsNotNull(especiales, "El jugador lleva ArmasEspecialesAPie");
            Dar(arma);
            yield return null;
            Assert.IsTrue(armas.Equipar(arma), arma + " se equipa");
            yield return Esperar(0.3f);
        }

        private IEnumerator Clic(float segundos = 0f)
        {
            Press(raton.leftButton);
            yield return null;
            if (segundos > 0f)
            {
                yield return Esperar(segundos);
            }
            Release(raton.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Subfusil_MantenerElClic_DisparaEnRafagaYPega()
        {
            Blanco blanco = PonerBlanco(new Vector3(0.75f, 1.4f, 10f));
            yield return Preparar(ArmaAPie.Subfusil);
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            int antes = jugador.GetComponent<PistolaAPie>().Balas;
            yield return Clic(0.6f);
            int gastadas = antes - jugador.GetComponent<PistolaAPie>().Balas;
            Release(raton.rightButton);
            Debug.Log("[T1013] subfusil: " + gastadas + " balas, " + blanco.Golpes + " impactos, " + blanco.Total + " de daño");
            Assert.GreaterOrEqual(gastadas, 5, "Automático: en 0.6 s salen varias balas");
            Assert.Greater(blanco.Golpes, 2, "Las balas pegan en el blanco");
        }

        [UnityTest]
        public IEnumerator Lanzagranadas_LaGranadaVuelaEnArco_ExplotaYDanaEnArea()
        {
            Caja(new Vector3(0f, 2f, 16f), new Vector3(8f, 4f, 0.5f)); // una pared al fondo
            Blanco cerca = PonerBlanco(new Vector3(1.5f, 0.9f, 15f));
            Blanco lejos = PonerBlanco(new Vector3(12f, 0.9f, 15f));
            yield return Preparar(ArmaAPie.Lanzagranadas);
            int antes = ExplosionAPie.Cuantas;
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            Assert.IsTrue(especiales.ArcoVisible, "Al apuntar se ve el arco");
            yield return Clic();
            Assert.IsNotNull(especiales.UltimoProyectil, "Salió la granada");
            Assert.Greater(especiales.UltimoProyectil.Velocidad.y, -0.5f, "Sale hacia el blanco en arco");
            yield return Esperar(2.5f);
            Release(raton.rightButton);
            Assert.AreEqual(antes + 1, ExplosionAPie.Cuantas, "Explotó al chocar");
            Debug.Log("[T1013] lanzagranadas: explotó en " + ExplosionAPie.Ultima + "; cerca " + cerca.Total + ", lejos " + lejos.Total);
            Assert.Greater(ExplosionAPie.Ultima.z, 12f, "Explotó allá, no en la mano");
            Assert.Greater(cerca.Total, 30f, "Daña lo que está en el radio");
            Assert.AreEqual(0f, lejos.Total, "No daña lo que está fuera");
        }

        [UnityTest]
        public IEnumerator Granada_SeVeElArcoAlApuntar_SeLanzaYExplotaConLaMecha()
        {
            Blanco blanco = PonerBlanco(new Vector3(0f, 0.9f, 9f));
            yield return Preparar(ArmaAPie.Granada);
            Assert.IsFalse(especiales.ArcoVisible, "Sin apuntar no hay arco");
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            Assert.IsTrue(especiales.ArcoVisible, "Apuntando se ve el arco de la granada");
            int antes = ExplosionAPie.Cuantas;
            yield return Clic();
            ProyectilAPie granada = especiales.UltimoProyectil;
            Assert.IsNotNull(granada, "Se lanzó la granada");
            Assert.Greater(granada.Velocidad.y, 0f, "Sale hacia arriba, en arco");
            yield return Esperar(0.5f);
            Assert.AreEqual(antes, ExplosionAPie.Cuantas, "Con la mecha aún no explota");
            yield return Esperar(2.5f);
            Release(raton.rightButton);
            Assert.AreEqual(antes + 1, ExplosionAPie.Cuantas, "Explota al acabarse la mecha (o al tocar a alguien)");
            Debug.Log("[T1013] granada: explotó en " + ExplosionAPie.Ultima + "; blanco " + blanco.Total);
            Assert.Greater(ExplosionAPie.Ultima.z, 3f, "Voló lejos de la mano");
        }

        [UnityTest]
        public IEnumerator RiflePlasma_CargaTresSegundos_RayoCincoQueAtraviesaYDanaATodos()
        {
            Blanco primero = PonerBlanco(new Vector3(0.75f, 1.4f, 8f));
            Blanco segundo = PonerBlanco(new Vector3(0.75f, 1.4f, 14f));
            yield return Preparar(ArmaAPie.RiflePlasma);
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            Press(raton.leftButton);
            yield return Esperar(1.5f);
            Assert.AreEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma, "Manteniendo el clic carga");
            Assert.That(especiales.CargaPlasma, Is.InRange(0.35f, 0.7f), "A la mitad de la carga");
            Assert.AreEqual(0f, primero.Total, "Mientras carga no daña");
            yield return Esperar(2f);
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma, "A los 3 s sale el rayo");
            Release(raton.leftButton);
            yield return Esperar(2f);
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma, "Soltar no corta el rayo");
            Assert.Greater(primero.Total, 50f, "El rayo daña al primero");
            Assert.Greater(segundo.Total, 50f, "Y atraviesa al segundo");
            yield return Esperar(3.5f);
            Release(raton.rightButton);
            Debug.Log("[T1013] plasma: primero " + primero.Total + ", segundo " + segundo.Total);
            Assert.AreEqual(EstadoPlasma.Reposo, especiales.EstadoPlasma, "A los 5 s el rayo se acaba");
            float total = primero.Total;
            yield return Esperar(0.5f);
            Assert.AreEqual(total, primero.Total, 0.001f, "Sin rayo ya no daña");
        }

        [UnityTest]
        public IEnumerator RiflePlasma_SoltarAntesDeCargar_NoDispara()
        {
            Blanco blanco = PonerBlanco(new Vector3(0.75f, 1.4f, 8f));
            yield return Preparar(ArmaAPie.RiflePlasma);
            yield return Clic(1.2f);
            yield return Esperar(0.3f);
            Assert.AreEqual(EstadoPlasma.Reposo, especiales.EstadoPlasma, "Soltar a la mitad cancela la carga");
            Assert.AreEqual(0f, blanco.Total, "Sin carga completa no hay rayo");
        }

        [UnityTest]
        public IEnumerator RiflePlasma_ConMiraLaser_SoloApuntando_YSeApagaConElRayo()
        {
            PonerBlanco(new Vector3(0.75f, 1.4f, 8f));
            yield return Preparar(ArmaAPie.RiflePlasma);
            Assert.IsFalse(especiales.LaserVisible, "Sin apuntar no hay mira");
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            Assert.IsTrue(especiales.LaserVisible, "T-1337: apuntando con el rifle de plasma se ve la mira");
            Assert.IsNotNull(GameObject.Find("MiraLaserPlasma"), "La línea existe");
            LineRenderer laser = GameObject.Find("MiraLaserPlasma").GetComponent<LineRenderer>();
            Assert.Greater(Vector3.Distance(laser.GetPosition(0), laser.GetPosition(1)), 1f, "Va de la boca al punto de mira");
            Press(raton.leftButton);
            yield return Esperar(3.5f);
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma);
            Assert.IsFalse(especiales.LaserVisible, "Con el rayo fuera la mira se apaga");
            Release(raton.leftButton);
            yield return Esperar(5.5f);
            Release(raton.rightButton);
            yield return Esperar(0.3f);
            Assert.IsFalse(especiales.LaserVisible, "Sin apuntar se apaga");
        }
    }
}
