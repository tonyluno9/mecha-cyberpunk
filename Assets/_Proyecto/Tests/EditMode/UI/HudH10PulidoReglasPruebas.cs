using System.Collections.Generic;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-701: las reglas del pulido del HUD (nombre de zona que se desvanece, calles del minimapa sacadas de las manzanas, íconos sin encimar).</summary>
    public class HudH10PulidoReglasPruebas
    {
        private static CalculoCallesMapa.Huella Caja(float x0, float z0, float x1, float z1) =>
            new CalculoCallesMapa.Huella(new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1));

        // ---- Nombre de zona ----

        [Test]
        public void Titulo_ElTextoLlevaLaCiudadYLaZonaEnMayusculas()
        {
            Assert.AreEqual("MEGALOPOLIS · SUBURBIOS", ReglaTituloZonaH10.Texto("Suburbios"));
            Assert.IsTrue(ReglaTituloZonaH10.EsTituloDeZona("MEGALOPOLIS · SUBURBIOS"));
            Assert.IsFalse(ReglaTituloZonaH10.EsTituloDeZona("ENCARGO: ENTREGA"));
            Assert.IsFalse(ReglaTituloZonaH10.EsTituloDeZona(string.Empty));
        }

        [Test]
        public void Titulo_SeVeLlenoYAlFinalSeDesvanece()
        {
            float duracion = ReglaTituloZonaH10.SegundosVisible;
            Assert.AreEqual(0f, ReglaTituloZonaH10.Alfa(-1f, duracion), 0.001f, "Antes de salir no se ve");
            Assert.AreEqual(1f, ReglaTituloZonaH10.Alfa(0f, duracion), 0.001f);
            Assert.AreEqual(1f, ReglaTituloZonaH10.Alfa(duracion - ReglaTituloZonaH10.SegundosDesvanecer, duracion), 0.001f, "Lleno hasta que empieza a desvanecerse");
            float medio = ReglaTituloZonaH10.Alfa(duracion - ReglaTituloZonaH10.SegundosDesvanecer * 0.5f, duracion);
            Assert.AreEqual(0.5f, medio, 0.001f, "Se va apagando");
            Assert.AreEqual(0f, ReglaTituloZonaH10.Alfa(duracion, duracion), 0.001f, "Al terminar ya no se ve");
        }

        // ---- Calles del minimapa ----

        [Test]
        public void Calles_DosManzanasSeparadasDejanLineasALosLadosDeLaCalle()
        {
            // Dos manzanas de 30 x 30 con una calle de 12 m entre ellas (x de 30 a 42).
            List<CalculoCallesMapa.Tramo> tramos = CalculoCallesMapa.ContornosDeManzanas(new[] { Caja(0f, 0f, 30f, 30f), Caja(42f, 0f, 72f, 30f) });
            bool bordeIzquierdoDeLaCalle = false;
            bool bordeDerechoDeLaCalle = false;
            foreach (CalculoCallesMapa.Tramo t in tramos)
            {
                bool vertical = Mathf.Abs(t.A.x - t.B.x) < 0.01f;
                bordeIzquierdoDeLaCalle |= vertical && Mathf.Abs(t.A.x - 30f) < 1.6f && t.Largo > 25f;
                bordeDerechoDeLaCalle |= vertical && Mathf.Abs(t.A.x - 42f) < 1.6f && t.Largo > 25f;
            }
            Assert.IsTrue(bordeIzquierdoDeLaCalle, "Línea del lado de la primera manzana");
            Assert.IsTrue(bordeDerechoDeLaCalle, "Línea del lado de la segunda manzana");
        }

        [Test]
        public void Calles_DosHuellasQueSeTocanSeFundenYNoDejanLineaEntreEllas()
        {
            List<CalculoCallesMapa.Tramo> tramos = CalculoCallesMapa.ContornosDeManzanas(new[] { Caja(0f, 0f, 30f, 30f), Caja(30f, 0f, 60f, 30f) });
            foreach (CalculoCallesMapa.Tramo t in tramos)
            {
                bool vertical = Mathf.Abs(t.A.x - t.B.x) < 0.01f;
                Assert.IsFalse(vertical && Mathf.Abs(t.A.x - 30f) < 1f, "No hay línea en la unión (x = " + t.A.x + ")");
            }
            Assert.Greater(tramos.Count, 0);
        }

        [Test]
        public void Calles_SinEdificiosNoHayLineas()
        {
            Assert.AreEqual(0, CalculoCallesMapa.ContornosDeManzanas(new List<CalculoCallesMapa.Huella>()).Count);
        }

        [Test]
        public void Calles_UnaHuellaGiradaSeLlena()
        {
            // Un rombo: el centro está dentro y una esquina del cuadro que lo rodea, fuera.
            CalculoCallesMapa.Huella rombo = new CalculoCallesMapa.Huella(new Vector2(20f, 0f), new Vector2(40f, 20f), new Vector2(20f, 40f), new Vector2(0f, 20f));
            Assert.IsTrue(rombo.Contiene(new Vector2(20f, 20f)));
            Assert.IsFalse(rombo.Contiene(new Vector2(2f, 2f)));
        }

        [Test]
        public void Calles_ElTramoMasCercanoSeMideAlSegmento()
        {
            CalculoCallesMapa.Tramo t = new CalculoCallesMapa.Tramo(new Vector2(0f, 0f), new Vector2(10f, 0f));
            Assert.AreEqual(3f, CalculoCallesMapa.DistanciaATramo(new Vector2(5f, 3f), t), 0.001f);
            Assert.AreEqual(5f, CalculoCallesMapa.DistanciaATramo(new Vector2(-3f, 4f), t), 0.001f, "Más allá del extremo cuenta hasta el extremo");
        }

        // ---- Íconos sin encimar ----

        [Test]
        public void Iconos_SeEscondenLosQueSeEncimanYLosQueTapanAlJugador()
        {
            Vector2[] posiciones = { new Vector2(40f, 0f), new Vector2(45f, 3f), new Vector2(-60f, 50f), new Vector2(6f, 4f) };
            bool[] visibles = { true, true, true, true };
            bool[] quedan = CalculoCallesMapa.SinEncimar(posiciones, visibles, 20f, 30f);
            Assert.IsTrue(quedan[0], "El primero se queda");
            Assert.IsFalse(quedan[1], "El que queda encima del primero se esconde");
            Assert.IsTrue(quedan[2], "El lejano se queda");
            Assert.IsFalse(quedan[3], "El que queda sobre la flecha del jugador se esconde");
        }

        [Test]
        public void Iconos_LosQueNoSeVenNoCuentanParaEncimar()
        {
            Vector2[] posiciones = { new Vector2(40f, 0f), new Vector2(42f, 0f) };
            bool[] visibles = { false, true };
            bool[] quedan = CalculoCallesMapa.SinEncimar(posiciones, visibles, 20f, 10f);
            Assert.IsFalse(quedan[0]);
            Assert.IsTrue(quedan[1], "Si el primero no se ve, no estorba");
        }
    }
}
