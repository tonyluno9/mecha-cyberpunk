using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1003: la lógica pura que elige a quién se gira el golpe.</summary>
    public class SelectorAutoGiroPruebas
    {
        private static readonly Vector2 Origen = Vector2.zero;
        private static readonly Vector2 Frente = Vector2.up;

        [Test]
        public void Elegir_UnoAdelanteDentroDelRadio_LoElige()
        {
            Assert.AreEqual(0, SelectorAutoGiro.Elegir(Origen, Frente, new List<Vector2> { new Vector2(0f, 1.5f) }, 2.5f, 70f));
        }

        [Test]
        public void Elegir_FueraDelRadio_NoElige()
        {
            Assert.AreEqual(-1, SelectorAutoGiro.Elegir(Origen, Frente, new List<Vector2> { new Vector2(0f, 6f) }, 2.5f, 70f));
        }

        [Test]
        public void Elegir_Detras_NoElige()
        {
            Assert.AreEqual(-1, SelectorAutoGiro.Elegir(Origen, Frente, new List<Vector2> { new Vector2(0f, -1.5f) }, 2.5f, 70f));
        }

        [Test]
        public void Elegir_A50Grados_ElijeYA80No()
        {
            Vector2 a50 = Quaternion.Euler(0f, 0f, -50f) * Frente * 1.5f;
            Vector2 a80 = Quaternion.Euler(0f, 0f, -80f) * Frente * 1.5f;
            Assert.AreEqual(0, SelectorAutoGiro.Elegir(Origen, Frente, new List<Vector2> { a50 }, 2.5f, 70f));
            Assert.AreEqual(-1, SelectorAutoGiro.Elegir(Origen, Frente, new List<Vector2> { a80 }, 2.5f, 70f));
        }

        [Test]
        public void Elegir_VariosValidos_ElMasCercano()
        {
            var lista = new List<Vector2> { new Vector2(0f, 2f), new Vector2(0.5f, 1f), new Vector2(0f, -1f) };
            Assert.AreEqual(1, SelectorAutoGiro.Elegir(Origen, Frente, lista, 2.5f, 70f));
        }

        [Test]
        public void Elegir_SinCandidatos_NoElige()
        {
            Assert.AreEqual(-1, SelectorAutoGiro.Elegir(Origen, Frente, new List<Vector2>(), 2.5f, 70f));
        }

        [Test]
        public void AnguloYaw_HaciaZYHaciaX()
        {
            Assert.AreEqual(0f, SelectorAutoGiro.AnguloYaw(Vector2.up), 0.001f);
            Assert.AreEqual(90f, SelectorAutoGiro.AnguloYaw(Vector2.right), 0.001f);
        }
    }
}
