using System.Collections.Generic;
using MechaCyberpunk.Core;
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

        [Test]
        public void Decidir_SiguiendoYModos_ElAliadoSigueSeSubeOAlcanza()
        {
            Assert.AreEqual(ReglaAliado.Accion.Esperar, ReglaAliado.Decidir(false, ModoJugador.APie, 20f), "Con espera no se mueve.");
            Assert.AreEqual(ReglaAliado.Accion.Esperar, ReglaAliado.Decidir(false, ModoJugador.Carro, 20f), "Con espera tampoco se sube.");
            Assert.AreEqual(ReglaAliado.Accion.Esperar, ReglaAliado.Decidir(true, ModoJugador.APie, 3f));
            Assert.AreEqual(ReglaAliado.Accion.Caminar, ReglaAliado.Decidir(true, ModoJugador.APie, 6f));
            Assert.AreEqual(ReglaAliado.Accion.Correr, ReglaAliado.Decidir(true, ModoJugador.APie, 15f));
            Assert.AreEqual(ReglaAliado.Accion.Alcanzar, ReglaAliado.Decidir(true, ModoJugador.APie, 60f));
            Assert.AreEqual(ReglaAliado.Accion.IrAbordo, ReglaAliado.Decidir(true, ModoJugador.Carro, 3f));
            Assert.AreEqual(ReglaAliado.Accion.IrAbordo, ReglaAliado.Decidir(true, ModoJugador.Mecha, 100f));
        }
    }
}
