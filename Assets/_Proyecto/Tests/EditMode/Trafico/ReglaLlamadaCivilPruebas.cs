using MechaCyberpunk.Trafico.Testigos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    public class ReglaLlamadaCivilPruebas
    {
        [Test]
        public void Llamada_TerminaSoloTrasLosSegundos()
        {
            ReglaLlamadaCivil regla = new ReglaLlamadaCivil(4f);
            Assert.AreEqual(EstadoLlamada.Libre, regla.Estado);
            Assert.IsTrue(regla.Iniciar());
            Assert.IsFalse(regla.Avanzar(3f));
            Assert.AreEqual(1f, regla.Restante, 0.0001f);
            Assert.IsTrue(regla.Avanzar(1f));
            Assert.AreEqual(EstadoLlamada.Llamo, regla.Estado);
            Assert.IsFalse(regla.Avanzar(1f), "Solo avisa una vez");
        }

        [Test]
        public void Llamada_Cancelada_NuncaLlama()
        {
            ReglaLlamadaCivil regla = new ReglaLlamadaCivil(2f);
            regla.Iniciar();
            regla.Avanzar(1f);
            regla.Cancelar();
            Assert.AreEqual(EstadoLlamada.Cancelada, regla.Estado);
            Assert.IsFalse(regla.Avanzar(5f));
            Assert.IsFalse(regla.Iniciar(), "Uno cancelado no vuelve a empezar");
        }

        [Test]
        public void Llamada_YaTerminada_NoSeCancela()
        {
            ReglaLlamadaCivil regla = new ReglaLlamadaCivil(1f);
            regla.Iniciar();
            regla.Avanzar(2f);
            regla.Cancelar();
            Assert.AreEqual(EstadoLlamada.Llamo, regla.Estado);
        }

        [Test]
        public void VistaCamara_DentroDelConoYAlcance_Ve()
        {
            Assert.IsTrue(CalculoVistaCamara.EnCono(Vector3.zero, Vector3.forward, 40f, 45f, new Vector3(10f, 0f, 30f)));
        }

        [Test]
        public void VistaCamara_FueraDelAnguloOLejos_NoVe()
        {
            Assert.IsFalse(CalculoVistaCamara.EnCono(Vector3.zero, Vector3.forward, 40f, 45f, new Vector3(30f, 0f, 5f)), "Fuera del ángulo");
            Assert.IsFalse(CalculoVistaCamara.EnCono(Vector3.zero, Vector3.forward, 40f, 45f, new Vector3(0f, 0f, 50f)), "Más lejos que el alcance");
            Assert.IsFalse(CalculoVistaCamara.EnCono(Vector3.zero, Vector3.forward, 40f, 45f, new Vector3(0f, 0f, -10f)), "Detrás");
        }
    }
}
