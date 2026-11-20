using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1491: daño por distancia, trazos de bala y valores de alcance de las armas del jugador.</summary>
    public class ArmasAlcanceT1491Pruebas
    {
        private static DatosArma Catalogo(string clave)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DatosArma>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_" + clave + "_0.asset");
        }

        [Test]
        public void Multiplicador_SinCaida_HaceDanioCompletoHastaElAlcance()
        {
            Assert.AreEqual(1f, ReglaDanioDistancia.Multiplicador(300f, 300f, 1f, 1f), 0.0001f);
            Assert.AreEqual(1f, ReglaDanioDistancia.Multiplicador(0f, 300f, 1f, 1f), 0.0001f);
        }

        [Test]
        public void Multiplicador_ConCaida_BajaEnLineaRectaHastaElMinimo()
        {
            Assert.AreEqual(1f, ReglaDanioDistancia.Multiplicador(28f, 70f, 0.4f, 0.5f), 0.0001f);
            Assert.AreEqual(0.75f, ReglaDanioDistancia.Multiplicador(49f, 70f, 0.4f, 0.5f), 0.0001f);
            Assert.AreEqual(0.5f, ReglaDanioDistancia.Multiplicador(70f, 70f, 0.4f, 0.5f), 0.0001f);
        }

        [Test]
        public void Multiplicador_MasAllaDelAlcance_NoHaceDanio()
        {
            Assert.AreEqual(0f, ReglaDanioDistancia.Multiplicador(80f, 70f, 0.4f, 0.5f), 0.0001f);
        }

        [Test]
        public void Trazo_ColorYFrecuencia_PorArma()
        {
            Assert.AreEqual(3, ReglaTrazoBala.CadaCuantos(ArmaAPie.Subfusil));
            Assert.AreEqual(2, ReglaTrazoBala.CadaCuantos(ArmaAPie.Fusil));
            Assert.AreEqual(1, ReglaTrazoBala.CadaCuantos(ArmaAPie.Sniper));
            Assert.AreNotEqual(ReglaTrazoBala.ColorDe(ArmaAPie.Sniper), ReglaTrazoBala.ColorDe(ArmaAPie.Subfusil));
        }

        [Test]
        public void Trazo_Extremos_ViajaEnCincoCentesimasYSeDesvanece()
        {
            ReglaTrazoBala.Extremos(100f, 0f, out float cola0, out float cabeza0);
            Assert.AreEqual(0f, cabeza0, 0.0001f);
            Assert.AreEqual(0f, cola0, 0.0001f);
            ReglaTrazoBala.Extremos(100f, ReglaTrazoBala.SegundosViaje, out float cola, out float cabeza);
            Assert.AreEqual(100f, cabeza, 0.0001f);
            Assert.LessOrEqual(cabeza - cola, ReglaTrazoBala.LargoMaximo + 0.001f, "Es corto");
            Assert.AreEqual(1f, ReglaTrazoBala.Opacidad(ReglaTrazoBala.SegundosViaje), 0.0001f);
            Assert.AreEqual(0f, ReglaTrazoBala.Opacidad(ReglaTrazoBala.SegundosViaje + ReglaTrazoBala.SegundosDesvanecer), 0.0001f);
        }

        [Test]
        public void Armas_Alcances_SonSensatos()
        {
            Assert.GreaterOrEqual(Catalogo("sniper").Alcance, 250f, "Francotirador");
            Assert.AreEqual(1f, Catalogo("sniper").InicioCaida, 0.0001f, "El francotirador no pierde daño");
            Assert.GreaterOrEqual(Catalogo("fusil").Alcance, 140f, "Fusil");
            Assert.GreaterOrEqual(Catalogo("subfusil").Alcance, 60f, "Subfusil");
            Assert.LessOrEqual(Catalogo("subfusil").Alcance, 80f);
            Assert.Less(Catalogo("subfusil").InicioCaida, 1f, "El subfusil sí pierde daño");
            Assert.LessOrEqual(Catalogo("escopeta").Alcance, 30f, "Escopeta corta");
            Assert.LessOrEqual(Catalogo("sniper").Dispersion, 2f, "El francotirador sin apuntar no se abre 8 grados");
        }
    }
}
