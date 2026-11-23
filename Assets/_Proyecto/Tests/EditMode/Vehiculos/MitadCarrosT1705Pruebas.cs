using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1705 (T54): la regla de qué carros estacionados se apagan, con datos puros.</summary>
    public class MitadCarrosT1705Pruebas
    {
        private static CarroEstacionadoDato C(int id, string modelo, float x, float z, bool intocable = false) => new CarroEstacionadoDato(id, modelo, x, z, intocable);

        [Test]
        public void Duplicados_ElMismoModeloPegadoSeQuita_UnoDistintoNo()
        {
            var carros = new List<CarroEstacionadoDato> { C(0, "V02", 500f, 500f, true), C(1, "V02", 503f, 500f), C(2, "V03", 504f, 500f, true), C(3, "V02", 520f, 500f) };
            HashSet<int> quitar = ReglaMitadCarrosT1705.Quitar(carros, 0f, 0f);
            Assert.IsTrue(quitar.Contains(1), "V02 a 3 m del V02");
            Assert.IsFalse(quitar.Contains(0));
            Assert.IsFalse(quitar.Contains(2), "otro modelo");
        }

        [Test]
        public void CercaDeLaCasa_QuedanSoloTres_YSeQuitanLosDemas()
        {
            var carros = Enumerable.Range(0, 8).Select(i => C(i, "V0" + (i % 4 + 1), -53f + i * 4f, -76f)).ToList();
            HashSet<int> quitar = ReglaMitadCarrosT1705.Quitar(carros, -53f, -76.5f);
            int cerca = carros.Count(c => !quitar.Contains(c.Id) && System.Math.Abs(c.X + 53f) < 35f);
            Assert.AreEqual(ReglaMitadCarrosT1705.MaximoCerca, cerca);
        }

        [Test]
        public void MitadPorCelda_SeQuitaLaMitadEnCadaCelda_YLaDeUnCocheSeQueda()
        {
            var carros = new List<CarroEstacionadoDato>();
            for (int i = 0; i < 4; i++) carros.Add(C(i, "V0" + (i + 1), 1000f + i * 8f, 1000f)); // celda llena: 4 carros distintos
            carros.Add(C(10, "V02", 1500f, 1500f)); // celda con uno
            HashSet<int> quitar = ReglaMitadCarrosT1705.Quitar(carros, 0f, 0f);
            Assert.AreEqual(2, carros.Take(4).Count(c => quitar.Contains(c.Id)), "mitad de 4");
            Assert.IsFalse(quitar.Contains(10), "el solitario se queda: reparto por toda la ciudad");
        }

        [Test]
        public void Neon_SeQuitaAlFinal_YLosIntocablesNunca()
        {
            var carros = new List<CarroEstacionadoDato> { C(0, "V20", 2000f, 2000f), C(1, "V02", 2010f, 2000f), C(2, "V03", 2020f, 2000f, true), C(3, "V21", 2030f, 2000f) };
            HashSet<int> quitar = ReglaMitadCarrosT1705.Quitar(carros, 0f, 0f);
            Assert.IsFalse(quitar.Contains(2), "intocable");
            Assert.IsFalse(quitar.Contains(0) && quitar.Contains(3), "no salen los dos neón");
            Assert.IsTrue(quitar.Contains(1), "el básico sale antes que el neón");
        }

        [Test]
        public void Total_NuncaQuedaMasDeLaMitad()
        {
            var carros = new List<CarroEstacionadoDato>();
            for (int i = 0; i < 40; i++) carros.Add(C(i, "V0" + (i % 5 + 1), 3000f + (i % 8) * 50f, 3000f + (i / 8) * 50f));
            HashSet<int> quitar = ReglaMitadCarrosT1705.Quitar(carros, 0f, 0f);
            Assert.LessOrEqual(carros.Count - quitar.Count, 20);
        }
    }
}
