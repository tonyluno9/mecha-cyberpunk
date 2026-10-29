using System.Collections.Generic;
using MechaCyberpunk.IA.Aliados;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-405: niveles por vencidos, números por nivel y a quién ataca el aliado.</summary>
    public class ReglaAliadoPruebas
    {
        [Test]
        public void NivelesYObjetivo()
        {
            Assert.AreEqual(1, ReglaAliado.Nivel(2));
            Assert.AreEqual(2, ReglaAliado.Nivel(3));
            Assert.AreEqual(2, ReglaAliado.Nivel(7));
            Assert.AreEqual(3, ReglaAliado.Nivel(8));
            Assert.AreEqual(80f, ReglaAliado.Vida(1));
            Assert.AreEqual(140f, ReglaAliado.Vida(3));
            Assert.AreEqual(8f, ReglaAliado.Danio(2));

            Vector3 jugador = Vector3.zero;
            List<Vector3> rivales = new List<Vector3> { new Vector3(0f, 0f, 15f), new Vector3(0f, 0f, 10f) };
            Assert.AreEqual(1, ReglaAliado.ElegirObjetivo(jugador, rivales, -1), "El más cercano (10 m).");
            Assert.AreEqual(0, ReglaAliado.ElegirObjetivo(jugador, rivales, 0), "El que te disparó (15 m).");
            rivales.Add(new Vector3(0f, 0f, 25f));
            Assert.AreEqual(-1, ReglaAliado.ElegirObjetivo(jugador, new List<Vector3> { new Vector3(0f, 0f, 25f) }, 0), "Fuera de 20 m no ataca.");
        }
    }
}
