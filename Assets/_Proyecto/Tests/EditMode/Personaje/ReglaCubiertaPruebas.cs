using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    public class ReglaCubiertaPruebas
    {
        [Test]
        public void Protege_TiradorDelOtroLado_SiYSoloSiEstaDetras()
        {
            Vector3 normal = Vector3.back; // la cubierta está en +Z, el jugador queda en -Z de ella
            Assert.IsTrue(ReglaCubierta.Protege(Vector3.forward * 10f, normal, 0.3f));
            Assert.IsFalse(ReglaCubierta.Protege(Vector3.back * 10f, normal, 0.3f));
            Assert.IsFalse(ReglaCubierta.Protege(Vector3.right * 10f, normal, 0.3f), "De lado no protege");
        }

        [Test]
        public void DebeSalir_SoloAlAlejarseDeLaCubierta()
        {
            Assert.IsTrue(ReglaCubierta.DebeSalir(Vector3.back, Vector3.back, 0.6f));
            Assert.IsFalse(ReglaCubierta.DebeSalir(Vector3.right, Vector3.back, 0.6f));
            Assert.IsFalse(ReglaCubierta.DebeSalir(Vector3.zero, Vector3.back, 0.6f));
        }

        [Test]
        public void Rodada_InvulnerableUnosCuadrosYEnfriamientoImpideOtra()
        {
            var rodada = new RodadaAPie();
            Assert.IsTrue(rodada.Iniciar(0.45f, 0.3f, 1.2f));
            Assert.IsTrue(rodada.Invulnerable);
            rodada.Avanzar(0.35f);
            Assert.IsFalse(rodada.Invulnerable);
            Assert.IsTrue(rodada.Activa);
            rodada.Avanzar(0.2f);
            Assert.IsFalse(rodada.Activa);
            Assert.IsFalse(rodada.Iniciar(0.45f, 0.3f, 1.2f), "En enfriamiento");
            rodada.Avanzar(0.8f);
            Assert.IsTrue(rodada.Iniciar(0.45f, 0.3f, 1.2f));
        }
    }
}
