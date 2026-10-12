using MechaCyberpunk.Personaje.Interaccion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    public class SelectorInteraccionPruebas
    {
        private const float Alcance = 2.5f;
        private const float Angulo = 60f;
        private static readonly Vector3 Origen = Vector3.zero;
        private static readonly Vector3 Adelante = Vector3.forward;

        private static int Elegir(params Vector3[] puntos)
        {
            return SelectorInteraccion.Elegir(Origen, Adelante, puntos, Alcance, Angulo);
        }

        [Test]
        public void Elegir_ListaVacia_DevuelveMenosUno()
        {
            Assert.AreEqual(-1, Elegir());
        }

        [Test]
        public void Elegir_ListaNull_DevuelveMenosUno()
        {
            Assert.AreEqual(-1, SelectorInteraccion.Elegir(Origen, Adelante, null, Alcance, Angulo));
        }

        [Test]
        public void Elegir_UnoDeFrenteEnAlcance_DevuelveSuIndice()
        {
            Assert.AreEqual(0, Elegir(new Vector3(0f, 1.2f, 1.5f)));
        }

        [Test]
        public void Elegir_DosEnAlcance_ElijeElMasCercano()
        {
            Assert.AreEqual(1, Elegir(new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 1f)));
        }

        [Test]
        public void Elegir_MasLejosQueElAlcance_Descarta()
        {
            Assert.AreEqual(-1, Elegir(new Vector3(0f, 0f, 2.6f)));
        }

        [Test]
        public void Elegir_JustoEnElAlcance_Elige()
        {
            Assert.AreEqual(0, Elegir(new Vector3(0f, 0f, 2.5f)));
        }

        [Test]
        public void Elegir_DetrasDelJugador_Descarta()
        {
            Assert.AreEqual(-1, Elegir(new Vector3(0f, 0f, -1.5f)));
        }

        [Test]
        public void Elegir_FueraDelAnguloPorPoco_Descarta()
        {
            // 61 grados a la derecha de la vista, a 1.5 m.
            Vector3 punto = Quaternion.Euler(0f, 61f, 0f) * Vector3.forward * 1.5f;
            Assert.AreEqual(-1, Elegir(punto));
        }

        [Test]
        public void Elegir_DentroDelAnguloPorPoco_Elige()
        {
            Vector3 punto = Quaternion.Euler(0f, -59f, 0f) * Vector3.forward * 1.5f;
            Assert.AreEqual(0, Elegir(punto));
        }

        [Test]
        public void Elegir_CercanoDeEspaldasYLejanoDeFrente_ElijeElDeFrente()
        {
            Assert.AreEqual(1, Elegir(new Vector3(0f, 0f, -0.5f), new Vector3(0f, 0f, 2f)));
        }

        [Test]
        public void Elegir_MismaDistanciaDistintoAngulo_ElijeElMasCentrado()
        {
            Vector3 lateral = Quaternion.Euler(0f, 40f, 0f) * Vector3.forward * 2f;
            Vector3 centrado = Quaternion.Euler(0f, -10f, 0f) * Vector3.forward * 2f;
            Assert.AreEqual(1, Elegir(lateral, centrado));
            Assert.AreEqual(0, Elegir(centrado, lateral));
        }

        [Test]
        public void Elegir_IgnoraLaAltura()
        {
            Assert.AreEqual(0, Elegir(new Vector3(0f, 30f, 1.5f)));
        }

        [Test]
        public void Elegir_VistaMiraArriba_UsaElPlanoHorizontal()
        {
            Vector3 mirandoArriba = new Vector3(0f, 0.8f, 0.6f);
            int indice = SelectorInteraccion.Elegir(Origen, mirandoArriba, new[] { new Vector3(0f, 1f, 1.5f) }, Alcance, Angulo);
            Assert.AreEqual(0, indice);
        }

        [Test]
        public void Elegir_PosicionDelJugadorNoEsElOrigen_MideDesdeElJugador()
        {
            Vector3 jugador = new Vector3(10f, 0f, 10f);
            int indice = SelectorInteraccion.Elegir(jugador, Adelante, new[] { new Vector3(10f, 0f, 11.5f), new Vector3(0f, 0f, 1.5f) }, Alcance, Angulo);
            Assert.AreEqual(0, indice);
        }
    }
}
