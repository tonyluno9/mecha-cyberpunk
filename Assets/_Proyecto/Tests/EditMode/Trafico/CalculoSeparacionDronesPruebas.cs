using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-938: cada dron tiene su punto en el círculo del jugador y los drones se empujan para no atravesarse.</summary>
    public class CalculoSeparacionDronesPruebas
    {
        private const float Minima = 3f;

        [Test]
        public void PuntoEnCirculo_CadaIndiceTieneSuPuntoPropio_ARadioYAlturaPedidos()
        {
            Vector3 centro = new Vector3(10f, 0f, -5f);
            List<Vector3> puntos = new List<Vector3>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = CalculoSeparacionDrones.PuntoEnCirculo(centro, i, 4, 4f, 7f);
                Assert.AreEqual(7f, p.y - centro.y, 0.001f, "A la altura pedida");
                Assert.AreEqual(4f, Vector2.Distance(new Vector2(p.x, p.z), new Vector2(centro.x, centro.z)), 0.001f, "Al radio pedido");
                puntos.Add(p);
            }
            for (int i = 0; i < puntos.Count; i++)
            {
                for (int j = i + 1; j < puntos.Count; j++)
                {
                    Assert.Greater(Vector3.Distance(puntos[i], puntos[j]), Minima, "Dos índices distintos no comparten punto");
                }
            }
        }

        [Test]
        public void PuntoEnCirculo_ConDosDrones_EstanEnLadosOpuestos()
        {
            Vector3 a = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, 0, 2, 5f, 0f);
            Vector3 b = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, 1, 2, 5f, 0f);
            Assert.AreEqual(10f, Vector3.Distance(a, b), 0.001f);
        }

        [Test]
        public void PuntoEnCirculo_ElAnguloBaseGiraElCirculo()
        {
            Vector3 sin = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, 0, 3, 10f, 0f, 0f);
            Vector3 con = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, 0, 3, 10f, 0f, 90f);
            Assert.AreEqual(10f, sin.x, 0.001f);
            Assert.AreEqual(10f, con.z, 0.001f);
        }

        [Test]
        public void Repulsion_SinNadieCerca_NoMueve()
        {
            Vector3 empuje = CalculoSeparacionDrones.Repulsion(Vector3.zero, new[] { new Vector3(10f, 0f, 0f) }, Minima, Vector3.right);
            Assert.AreEqual(Vector3.zero, empuje);
        }

        [Test]
        public void Repulsion_UnoMuyCerca_EmpujaHaciaElLadoContrario()
        {
            Vector3 empuje = CalculoSeparacionDrones.Repulsion(Vector3.zero, new[] { new Vector3(1f, 0f, 0f) }, Minima, Vector3.right);
            Assert.Less(empuje.x, 0f, "Se aparta del otro");
            Assert.AreEqual(1f, empuje.magnitude, 0.001f, "La mitad de lo que falta (3 - 1) / 2");
        }

        [Test]
        public void Repulsion_ElMismoPunto_UsaLaDireccionDeRespaldo()
        {
            Vector3 empuje = CalculoSeparacionDrones.Repulsion(Vector3.zero, new[] { Vector3.zero }, Minima, Vector3.forward);
            Assert.Greater(empuje.z, 0f);
        }

        [Test]
        public void Repulsion_AplicadaPorTodos_NuncaDejaDosDronesAMenosDeLaMinima()
        {
            Vector3[] drones =
            {
                new Vector3(0f, 7f, 0f), new Vector3(0.3f, 7f, 0.1f), new Vector3(0f, 7.2f, 0.4f), new Vector3(-0.2f, 7f, -0.1f)
            };
            Vector3[] direcciones = new Vector3[drones.Length];
            for (int i = 0; i < drones.Length; i++)
            {
                direcciones[i] = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, i, drones.Length, 1f, 0f);
            }
            for (int vuelta = 0; vuelta < 60; vuelta++)
            {
                Vector3[] empujes = new Vector3[drones.Length];
                for (int i = 0; i < drones.Length; i++)
                {
                    List<Vector3> otros = new List<Vector3>();
                    for (int j = 0; j < drones.Length; j++)
                    {
                        if (j != i)
                        {
                            otros.Add(drones[j]);
                        }
                    }
                    empujes[i] = CalculoSeparacionDrones.Repulsion(drones[i], otros, Minima, direcciones[i]);
                }
                for (int i = 0; i < drones.Length; i++)
                {
                    drones[i] += empujes[i];
                }
            }
            for (int i = 0; i < drones.Length; i++)
            {
                for (int j = i + 1; j < drones.Length; j++)
                {
                    Assert.GreaterOrEqual(Vector3.Distance(drones[i], drones[j]), Minima - 0.05f, "Drones " + i + " y " + j + " separados");
                }
            }
        }
    }
}
