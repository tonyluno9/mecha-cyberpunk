using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-indicador-24nov: qué orilla destella según de dónde vino el daño.</summary>
    public class ReglaIndicadorDanioOrillaPruebas
    {
        private const float Arriba = 40f;
        private const float Frente = 35f;
        private const float Atras = 50f;

        // direccionDelGolpe viaja del atacante al jugador: un atacante a la izquierda dispara hacia +x.
        private static OrillaDanio Orilla(Vector3 direccionDelGolpe, bool puedeSerArriba = true)
        {
            return ReglaIndicadorDanio.Orilla(Vector3.forward, direccionDelGolpe, puedeSerArriba, Arriba, Frente, Atras);
        }

        [Test]
        public void Orilla_AtacanteALaIzquierda_Izquierda()
        {
            Assert.AreEqual(OrillaDanio.Izquierda, Orilla(Vector3.right));
        }

        [Test]
        public void Orilla_AtacanteALaDerecha_Derecha()
        {
            Assert.AreEqual(OrillaDanio.Derecha, Orilla(Vector3.left));
        }

        [Test]
        public void Orilla_AtacanteAtras_Atras()
        {
            Assert.AreEqual(OrillaDanio.Atras, Orilla(Vector3.forward));
            Assert.AreEqual(OrillaDanio.Atras, Orilla(new Vector3(0.5f, 0f, 1f)), "Atrás un poco de lado sigue siendo atrás");
        }

        [Test]
        public void Orilla_AtacanteEnfrente_Frente()
        {
            Assert.AreEqual(OrillaDanio.Frente, Orilla(Vector3.back));
        }

        [Test]
        public void Orilla_DisparoDesdeUnaAzotea_Arriba()
        {
            Assert.AreEqual(OrillaDanio.Arriba, Orilla(new Vector3(0f, -1f, -0.5f)));
        }

        [Test]
        public void Orilla_GolpeOCaidaHaciaAbajo_NoEsArriba()
        {
            Assert.AreEqual(OrillaDanio.Ninguna, Orilla(Vector3.down, false), "Una caída no destella");
            Assert.AreEqual(OrillaDanio.Izquierda, Orilla(new Vector3(1f, -1.5f, 0f), false), "Un golpe desde arriba-izquierda marca la izquierda");
        }

        [Test]
        public void Orilla_SinDireccion_Ninguna()
        {
            Assert.AreEqual(OrillaDanio.Ninguna, Orilla(Vector3.zero));
        }

        [Test]
        public void SeAleja_GolpeDelJugadorAUnEnemigoCercano_True()
        {
            Assert.IsTrue(ReglaIndicadorDanio.SeAlejaDelJugador(new Vector3(0f, 1f, 1.2f), Vector3.zero, Vector3.forward));
        }

        [Test]
        public void SeAleja_BalaQueLlegaAlJugador_False()
        {
            Assert.IsFalse(ReglaIndicadorDanio.SeAlejaDelJugador(new Vector3(0.2f, 1.2f, 0.1f), Vector3.zero, Vector3.right), "Impacto en el cuerpo");
            Assert.IsFalse(ReglaIndicadorDanio.SeAlejaDelJugador(new Vector3(0f, 0f, 2f), Vector3.zero, Vector3.back), "Explosión que viene hacia él");
        }

        [Test]
        public void Alfa_SeDesvaneceEnUnSegundo()
        {
            Assert.AreEqual(1f, ReglaIndicadorDanio.Alfa(0f, 1f), 1e-4f);
            Assert.AreEqual(0f, ReglaIndicadorDanio.Alfa(1f, 1f), 1e-4f);
        }
    }
}
