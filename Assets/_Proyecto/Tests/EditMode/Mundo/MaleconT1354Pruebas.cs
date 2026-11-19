using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1354: la curva de la avenida del Malecón, sus puntos de unión y el espaciado de palmeras y faroles (cuentas puras).</summary>
    public class MaleconT1354Pruebas
    {
        [Test]
        public void Avenida_RadioMinimo_NoBajaDe60()
        {
            Assert.GreaterOrEqual(ColocarT1354Curva.RadioMinimo(ColocarT1354Curva.Avenida()), ColocarT1354Curva.RadioMinimoAvenida);
        }

        [Test]
        public void Avenida_PasaExactoPorU1U2U3YU5()
        {
            List<Vector3> linea = ColocarT1354Curva.Avenida();
            foreach (Vector3 u in new[] { ColocarT1354Curva.U1, ColocarT1354Curva.U2, ColocarT1354Curva.U3Inicio, ColocarT1354Curva.U5 })
            {
                Assert.IsTrue(ColocarT1354Curva.Pasa(linea, u, 0.01f), "no pasa por " + u);
            }
        }

        [Test]
        public void Avenida_EsPlana()
        {
            List<Vector3> linea = ColocarT1354Curva.Avenida();
            Assert.AreEqual(0f, ColocarT1354Curva.PendienteMaxima(linea), 1e-6f);
            Assert.IsTrue(linea.All(p => p.y == 0f));
        }

        [Test]
        public void Avenida_VaDeOesteAEsteSinRetroceder()
        {
            List<Vector3> linea = ColocarT1354Curva.Avenida();
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                Assert.Greater(linea[i + 1].x, linea[i].x);
            }
        }

        [Test]
        public void U1YU2_SonRectasYTerminanEnLaAvenida()
        {
            foreach (Vector3 u in new[] { ColocarT1354Curva.U1, ColocarT1354Curva.U2 })
            {
                List<Vector3> linea = ColocarT1354Curva.Union(u);
                Assert.AreEqual(ColocarT1354Curva.ZCorredorSur, linea[0].z, 1e-4f);
                Assert.IsTrue(ColocarT1354Curva.Pasa(linea, u, 0.01f));
                Assert.IsTrue(linea.All(p => Mathf.Abs(p.x - u.x) < 1e-4f));
            }
        }

        [Test]
        public void U3_PasaPorLosPuntosDelPlanoYNoSeCierraDemasiado()
        {
            List<Vector3> linea = ColocarT1354Curva.U3();
            Assert.IsTrue(ColocarT1354Curva.Pasa(linea, ColocarT1354Curva.U3Inicio, 0.01f));
            Assert.IsTrue(ColocarT1354Curva.Pasa(linea, ColocarT1354Curva.U3Medio, 0.01f));
            Assert.IsTrue(ColocarT1354Curva.Pasa(linea, ColocarT1354Curva.U3Fin, 0.01f));
            Assert.GreaterOrEqual(ColocarT1354Curva.RadioMinimo(linea), 15f);
        }

        [Test]
        public void Alternados_EmpiezaPorPalmeraYAlternaCada12m()
        {
            List<(float distancia, bool palmera)> lista = ColocarT1354Curva.Alternados(120f, 12f, 6f);
            Assert.AreEqual(10, lista.Count);
            for (int i = 0; i < lista.Count; i++)
            {
                Assert.AreEqual(6f + 12f * i, lista[i].distancia, 1e-3f);
                Assert.AreEqual(i % 2 == 0, lista[i].palmera);
            }
        }

        [Test]
        public void Alternados_EnElBordeDelMalecon_NoSeAmontonan()
        {
            float largo = ColocarT1354Curva.Longitud(ColocarT1354Curva.BordeMar(-330f, 175f));
            List<(float distancia, bool palmera)> lista = ColocarT1354Curva.Alternados(largo, ColocarT1354Malecon.PasoPaseo, 6f);
            Assert.Greater(lista.Count, 30);
            for (int i = 0; i + 1 < lista.Count; i++)
            {
                Assert.AreEqual(ColocarT1354Malecon.PasoPaseo, lista[i + 1].distancia - lista[i].distancia, 0.01f);
                Assert.AreNotEqual(lista[i].palmera, lista[i + 1].palmera);
            }
        }

        [Test]
        public void PuntoEn_DevuelveElPuntoALaDistanciaPedida()
        {
            List<Vector3> linea = new List<Vector3> { Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) };
            Vector3 p = ColocarT1354Curva.PuntoEn(linea, 15f, out Vector3 t);
            Assert.AreEqual(10f, p.x, 1e-4f);
            Assert.AreEqual(5f, p.z, 1e-4f);
            Assert.AreEqual(1f, t.z, 1e-4f);
            Assert.AreEqual(1f, ColocarT1354Curva.HaciaElMar(Vector3.forward).x, 1e-4f);
        }

        [Test]
        public void Orilla_DejaUnaPlayaDe30m()
        {
            Assert.AreEqual(30f, ColocarT1354Curva.LateralOrilla(-226f) - ColocarT1354Curva.LateralMuro, 1e-4f);
        }

        // ---------------------------------------------------------------- T-1363: volumen local de luz y relleno de T530

        [Test]
        public void Volumen_CubreLaAvenidaYElPaseo()
        {
            Bounds caja = ColocarT1354Curva.CajaVolumen();
            foreach (Vector3 p in ColocarT1354Curva.Avenida())
            {
                Assert.IsTrue(caja.Contains(new Vector3(p.x, 1f, p.z)), "la avenida queda fuera en " + p);
                Assert.AreEqual(1f, ColocarT1354Curva.PesoVolumen(new Vector3(p.x, 1f, p.z)), 1e-4f);
            }
            foreach (Vector3 p in ColocarT1354Curva.BordeMar(-300f, 160f))
            {
                Vector3 paseo = new Vector3(p.x, 1f, p.z - 12f);
                Assert.AreEqual(1f, ColocarT1354Curva.PesoVolumen(paseo), 1e-4f, "el paseo queda fuera en " + paseo);
            }
        }

        [Test]
        public void Volumen_NoAlcanzaLosBarriosViejos()
        {
            Assert.LessOrEqual(ColocarT1354Curva.VolumenZNorte + ColocarT1354Curva.VolumenMezcla, ColocarT1354Curva.ZBarriosViejos + 1e-4f);
            for (float x = -400f; x <= 400f; x += 10f)
            {
                for (float z = -189.9f; z <= 120f; z += 10f)
                {
                    Assert.AreEqual(0f, ColocarT1354Curva.PesoVolumen(new Vector3(x, 1f, z)), 1e-6f, "la luz del Malecón llega a " + x + ", " + z);
                }
            }
        }

        [Test]
        public void Volumen_MezclaDe15mYPrioridadAlta()
        {
            Assert.AreEqual(15f, ColocarT1354Curva.VolumenMezcla);
            Assert.GreaterOrEqual(ColocarT1354Curva.VolumenPrioridad, 10f);
            // A medio camino de la mezcla pesa la mitad.
            Assert.AreEqual(0.5f, ColocarT1354Curva.PesoVolumen(new Vector3(-100f, 1f, ColocarT1354Curva.VolumenZNorte + 7.5f)), 1e-4f);
        }

        [Test]
        public void RellenoT530_SoloSeApagaDetrasDeLasFachadasYCercaDeLaAvenida()
        {
            // Bloque del último tramo del corredor sur, a ~35 m de la avenida: se apaga.
            Assert.IsTrue(ColocarT1354Curva.RellenoT530Apagable(new Vector3(-70f, 15f, -190f)));
            Assert.IsTrue(ColocarT1354Curva.RellenoT530Apagable(new Vector3(60f, 15f, -200f)));
            // Más al norte (la ciudad y el tramo medio del corredor): se queda.
            Assert.IsFalse(ColocarT1354Curva.RellenoT530Apagable(new Vector3(-70f, 15f, -150f)));
            Assert.IsFalse(ColocarT1354Curva.RellenoT530Apagable(new Vector3(-70f, 15f, -170f)));
            // Al sur de la avenida (otro lado): no es relleno de T530.
            Assert.IsFalse(ColocarT1354Curva.RellenoT530Apagable(new Vector3(-70f, 15f, -240f)));
        }
    }
}
