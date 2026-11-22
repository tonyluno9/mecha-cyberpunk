using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1628: lógica pura de colgarse de un borde (ReglaColgarse) y el estado Colgado de ReglaTrepar.</summary>
    public class ReglaColgarseT1628Pruebas
    {
        [Test]
        public void DebeColgarse_BordeEnRango_True()
        {
            Assert.IsTrue(ReglaColgarse.DebeColgarse(true, true, 2f, 1.8f, 2.3f));
        }

        [Test]
        public void DebeColgarse_BordeBajoODesactivadoOSinVisual_False()
        {
            Assert.IsFalse(ReglaColgarse.DebeColgarse(true, true, 1.2f, 1.8f, 2.3f));
            Assert.IsFalse(ReglaColgarse.DebeColgarse(false, true, 2f, 1.8f, 2.3f));
            Assert.IsFalse(ReglaColgarse.DebeColgarse(true, false, 2f, 1.8f, 2.3f));
        }

        [Test]
        public void PosicionColgado_QuedaBajoElBordeYAntesDeLaPared()
        {
            Vector3 destino = new Vector3(0f, 3.05f, 1f);
            Vector3 p = ReglaColgarse.PosicionColgado(destino, Vector3.forward, 0.3f, 0.05f, 0.2f, 2f);
            Assert.AreEqual(3.05f - 0.05f - 2f, p.y, 1e-4f);
            Assert.AreEqual(1f - (2f * 0.35f + 0.2f), p.z, 1e-4f);
            Assert.AreEqual(0f, p.x, 1e-4f);
        }

        [Test]
        public void Decidir_AntesDelTiempoMinimo_Nada()
        {
            Assert.AreEqual(AccionColgado.Nada, ReglaColgarse.Decidir(0.5f, 0.35f, 0.5f, true, false, false));
        }

        [Test]
        public void Decidir_DespuesDelTiempo_SaltoSubeYAtrasSuelta()
        {
            Assert.AreEqual(AccionColgado.Subir, ReglaColgarse.Decidir(1f, 0.35f, 0.5f, true, false, false));
            Assert.AreEqual(AccionColgado.Subir, ReglaColgarse.Decidir(1f, 0.35f, 0.5f, false, true, false));
            Assert.AreEqual(AccionColgado.Soltar, ReglaColgarse.Decidir(1f, 0.35f, 0.5f, false, false, true));
            Assert.AreEqual(AccionColgado.Nada, ReglaColgarse.Decidir(1f, 0.35f, 0.5f, false, false, false));
        }

        [Test]
        public void ReglaTrepar_IniciarColgado_EstadoColgadoYSoltarLoLibera()
        {
            ReglaTrepar regla = new ReglaTrepar();
            regla.IniciarColgado();
            Assert.AreEqual(EstadoTrepar.Colgado, regla.Estado);
            regla.Soltar();
            Assert.AreEqual(EstadoTrepar.Libre, regla.Estado);
        }
    }
}
