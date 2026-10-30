using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>T-415: limpiar el nombre de tu pandilla y la paleta de 4 colores (lejos del magenta de SKULLS, el verde de SNAKES y el rojo de enemigos).</summary>
    public class ColoresPandillaPruebas
    {
        private const float DistanciaMinima = 0.3f;

        private static float Distancia(Color a, Color b) => new Vector3(a.r - b.r, a.g - b.g, a.b - b.b).magnitude;

        [Test]
        public void LimpiarYPaleta()
        {
            Assert.AreEqual("LOS NEÓN", ColoresPandilla.Limpiar("  los   neón "));
            Assert.AreEqual(ColoresPandilla.NombrePredeterminado, ColoresPandilla.Limpiar("ab"));
            Assert.AreEqual(ColoresPandilla.NombrePredeterminado, ColoresPandilla.Limpiar(null));
            string largo = ColoresPandilla.Limpiar("LA PANDILLA MÁS LARGA DEL MUNDO");
            Assert.LessOrEqual(largo.Length, ColoresPandilla.LargoMaximo);
            Assert.AreEqual("LA PANDILLA MÁS", largo);
            string raro = ColoresPandilla.Limpiar("x$%9y");
            foreach (char c in raro)
            {
                Assert.IsTrue(char.IsLetter(c) || c == ' ', "Solo letras y espacios: " + raro);
            }
            Assert.AreEqual("PEÑA NIETO", ColoresPandilla.Limpiar("peña$ 7nieto"));

            Assert.AreEqual(4, ColoresPandilla.Cantidad);
            string[] nombres = { "NARANJA", "TURQUESA", "AMARILLO", "AZUL" };
            string[] hex = { "FF801A", "1AE6E6", "FFD91A", "3380FF" };
            Color magenta = new Color(1f, 0.1f, 0.8f);
            Color verde = new Color(0.45f, 1f, 0.1f);
            Color rojo = new Color32(0xFF, 0x33, 0x33, 0xFF);
            for (int i = 0; i < ColoresPandilla.Cantidad; i++)
            {
                Assert.AreEqual(nombres[i], ColoresPandilla.Nombre(i));
                Assert.AreEqual(hex[i], ColorUtility.ToHtmlStringRGB(ColoresPandilla.Color(i)));
                Assert.Greater(Distancia(ColoresPandilla.Color(i), magenta), DistanciaMinima, nombres[i] + " vs magenta");
                Assert.Greater(Distancia(ColoresPandilla.Color(i), verde), DistanciaMinima, nombres[i] + " vs verde");
                Assert.Greater(Distancia(ColoresPandilla.Color(i), rojo), DistanciaMinima, nombres[i] + " vs rojo");
            }
            Assert.AreEqual(ColoresPandilla.Color(ColoresPandilla.Predeterminado), ColoresPandilla.Color(-1));
            Assert.AreEqual(ColoresPandilla.Color(ColoresPandilla.Predeterminado), ColoresPandilla.Color(4));
            Assert.AreEqual("NARANJA", ColoresPandilla.Nombre(99));
        }
    }
}
