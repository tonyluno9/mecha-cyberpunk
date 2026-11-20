using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1487: empuje del golpe, asistencia de objetivo (3 m, 70°) y valores por defecto de la config.</summary>
    public class EmpujeLinealT1487Pruebas
    {
        [Test]
        public void Avanzar_EmpujeCompleto_DesplazaVelocidadPorSegundosEntreDos()
        {
            EmpujeLineal e = new EmpujeLineal();
            e.Iniciar(new Vector3(0f, 0f, 4f), 0.5f);
            Vector3 total = Vector3.zero;
            for (int i = 0; i < 100; i++)
            {
                total += e.Avanzar(0.01f);
            }
            Assert.AreEqual(1f, total.z, 1e-3f);
            Assert.IsFalse(e.Activo);
        }

        [Test]
        public void Iniciar_ConUnoMasFuerteEnCurso_NoLoReemplaza()
        {
            EmpujeLineal e = new EmpujeLineal();
            e.Iniciar(Vector3.forward * 6f, 0.25f);
            e.Iniciar(Vector3.forward * 2f, 0.25f);
            Assert.AreEqual(6f, e.VelocidadActual().z, 1e-4f);
        }

        [Test]
        public void Config_PorDefecto_CumpleLoPedido()
        {
            ConfigGolpeAPie c = ConfigGolpeAPie.PorDefecto;
            Assert.AreEqual(3f, c.AlcanceAsistencia, 1e-4f);
            Assert.AreEqual(70f, c.AnguloAsistencia, 1e-4f);
            Assert.That(c.Hitstop(false), Is.InRange(0.05f, 0.08f));
            Assert.That(c.Hitstop(true), Is.InRange(0.05f, 0.08f));
            Assert.Greater(c.Empuje(true), c.Empuje(false));
            Assert.Greater(c.ImpulsoCamara(true), c.ImpulsoCamara(false));
        }

        [Test]
        public void Asistencia_ConValoresPorDefecto_ElijeElMasCercanoDentroDelCono()
        {
            ConfigGolpeAPie c = ConfigGolpeAPie.PorDefecto;
            // 0: fuera de alcance (3.5 m); 1: detrás; 2: a 68° y 2.7 m; 3: de frente a 2.55 m.
            Vector2[] candidatos = { new Vector2(0f, 3.5f), new Vector2(0f, -2f), new Vector2(2.5f, 1f), new Vector2(0.5f, 2.5f) };
            int i = SelectorAutoGiro.Elegir(Vector2.zero, Vector2.up, candidatos, c.AlcanceAsistencia, c.AnguloAsistencia);
            Assert.AreEqual(3, i);
            Assert.AreEqual(2, SelectorAutoGiro.Elegir(Vector2.zero, Vector2.up, new[] { candidatos[0], candidatos[1], candidatos[2] }, c.AlcanceAsistencia, c.AnguloAsistencia));
        }

        [Test]
        public void Asistencia_Acercamiento_NoPasaDeLaDistanciaDeParada()
        {
            ConfigGolpeAPie c = ConfigGolpeAPie.PorDefecto;
            Assert.AreEqual(c.AcercamientoMaximo, SelectorAutoGiro.Acercamiento(3f, c.DistanciaParada, c.AcercamientoMaximo), 1e-4f);
            Assert.AreEqual(0.5f, SelectorAutoGiro.Acercamiento(1.5f, c.DistanciaParada, c.AcercamientoMaximo), 1e-4f);
            Assert.AreEqual(0f, SelectorAutoGiro.Acercamiento(0.8f, c.DistanciaParada, c.AcercamientoMaximo), 1e-4f);
        }
    }
}
