using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-715: el golpe del guardia de Nova avisa, luego conecta, y se cancela si el jugador se va; el tercero es la barrida.</summary>
    public class ReglaGolpeGuardiaT715Pruebas
    {
        [Test]
        public void Alerta_YJugadorAlAlcance_EmpiezaElAviso_SinDanioTodavia()
        {
            ReglaGolpeGuardia r = new ReglaGolpeGuardia(0.6f, 1f);
            ReglaGolpeGuardia.Salida s = r.Avanzar(0.016f, true, true);
            Assert.IsTrue(s.InicioAviso);
            Assert.IsFalse(s.Impacto, "El aviso no hace daño");
            Assert.AreEqual(ReglaGolpeGuardia.Fase.Aviso, r.Actual);
            Assert.IsFalse(r.Avanzar(0.3f, true, true).Impacto, "A la mitad del aviso todavía no conecta");
            Assert.Greater(r.ProgresoAviso, 0.4f);
        }

        [Test]
        public void TerminadoElAviso_Conecta_YDespuesRecarga()
        {
            ReglaGolpeGuardia r = new ReglaGolpeGuardia(0.6f, 1f);
            r.Avanzar(0.016f, true, true);
            Assert.IsTrue(r.Avanzar(0.7f, true, true).Impacto);
            Assert.AreEqual(ReglaGolpeGuardia.Fase.Recarga, r.Actual);
            Assert.IsFalse(r.Avanzar(0.5f, true, true).InicioAviso, "En la recarga no empieza otro aviso");
            Assert.IsFalse(r.Avanzar(0.6f, true, true).InicioAviso, "Ese cuadro sale de la recarga; el siguiente puede empezar");
            Assert.IsTrue(r.Avanzar(0.016f, true, true).InicioAviso, "Ya libre empieza otro golpe");
        }

        [Test]
        public void SiElJugadorSeVaDuranteElAviso_ElGolpeSeCancela()
        {
            ReglaGolpeGuardia r = new ReglaGolpeGuardia(0.6f, 1f);
            r.Avanzar(0.016f, true, true);
            ReglaGolpeGuardia.Salida s = r.Avanzar(0.3f, true, false);
            Assert.IsFalse(s.Impacto);
            Assert.AreEqual(ReglaGolpeGuardia.Fase.Libre, r.Actual);
            Assert.IsFalse(r.Avanzar(1f, true, false).Impacto);
        }

        [Test]
        public void SinAlerta_NoGolpea()
        {
            ReglaGolpeGuardia r = new ReglaGolpeGuardia(0.6f, 1f);
            Assert.IsFalse(r.Avanzar(1f, false, true).InicioAviso);
            Assert.AreEqual(ReglaGolpeGuardia.Fase.Libre, r.Actual);
        }

        [Test]
        public void CadaTercerGolpe_EsLaBarrida()
        {
            ReglaGolpeGuardia r = new ReglaGolpeGuardia(0.1f, 0.1f);
            bool[] fuertes = new bool[6];
            for (int golpe = 0; golpe < 6; golpe++)
            {
                r.Avanzar(0.016f, true, true);
                fuertes[golpe] = r.Fuerte;
                Assert.IsTrue(r.Avanzar(0.2f, true, true).Impacto);
                r.Avanzar(0.2f, true, true); // recarga
            }
            CollectionAssert.AreEqual(new[] { false, false, true, false, false, true }, fuertes);
            Assert.AreEqual(6, r.Golpes);
        }
    }
}
