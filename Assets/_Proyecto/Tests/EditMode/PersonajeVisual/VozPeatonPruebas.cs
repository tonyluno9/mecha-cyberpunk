using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-voces-personajes-23nov: la voz de los peatones. Cada peatón tiene UNA voz (1 a 3) estable por su semilla, cada momento pide su ID NPC_VOZn_*,
    /// habla de a una frase y respeta los enfriamientos. Solo los hombres civiles usan la voz grabada.
    /// </summary>
    public class VozPeatonPruebas
    {
        [Test]
        public void VozPara_MismaSemilla_SiempreLaMismaYEntreUnoYTres()
        {
            HashSet<int> vistas = new HashSet<int>();
            for (int semilla = -500; semilla < 500; semilla += 7)
            {
                int voz = ReglaVozPeaton.VozPara(semilla);
                Assert.That(voz, Is.InRange(1, 3), "semilla " + semilla);
                Assert.That(ReglaVozPeaton.VozPara(semilla), Is.EqualTo(voz), "estable");
                vistas.Add(voz);
            }
            Assert.That(vistas.Count, Is.EqualTo(3), "las tres voces se reparten");
        }

        [Test]
        public void Id_CadaMomento_PideSuGrupoGrabado()
        {
            Assert.That(ReglaVozPeaton.Id(1, MomentoVozPeaton.Miedo), Is.EqualTo("NPC_VOZ1_MIEDO"));
            Assert.That(ReglaVozPeaton.Id(2, MomentoVozPeaton.Enojo), Is.EqualTo("NPC_VOZ2_ENOJO"));
            Assert.That(ReglaVozPeaton.Id(3, MomentoVozPeaton.Policia), Is.EqualTo("NPC_VOZ3_POLICIA"));
            Assert.That(ReglaVozPeaton.Id(1, MomentoVozPeaton.Saludo), Is.EqualTo("NPC_VOZ1_SALUDO"));
            Assert.That(ReglaVozPeaton.Id(2, MomentoVozPeaton.Vendedor), Is.EqualTo("NPC_VOZ2_VENDEDOR"));
            Assert.That(ReglaVozPeaton.Id(3, MomentoVozPeaton.Risa), Is.EqualTo("NPC_VOZ3_RISA"));
            Assert.That(ReglaVozPeaton.Id(9, MomentoVozPeaton.Risa), Is.EqualTo("NPC_VOZ3_RISA"), "fuera de rango se acota");
        }

        [Test]
        public void Intentar_UnaFraseALaVez_YEnfriamientoPorMomento()
        {
            ReglaVozPeaton r = new ReglaVozPeaton();
            Assert.That(r.Intentar(0f, MomentoVozPeaton.Miedo), Is.True);
            Assert.That(r.Intentar(1f, MomentoVozPeaton.Policia), Is.False, "sigue hablando");
            Assert.That(r.Intentar(ReglaVozPeaton.SegundosFrase + 0.1f, MomentoVozPeaton.Miedo), Is.False, "miedo en enfriamiento");
            Assert.That(r.Intentar(ReglaVozPeaton.SegundosFrase + 0.1f, MomentoVozPeaton.Policia), Is.True, "otro momento sí");
            Assert.That(r.Intentar(20f, MomentoVozPeaton.Saludo), Is.True);
            Assert.That(r.Intentar(40f, MomentoVozPeaton.Saludo), Is.False, "saludo cada 30 s");
            Assert.That(r.Intentar(51f, MomentoVozPeaton.Saludo), Is.True);
        }

        [Test]
        public void Ocupar_ElDolorNoSePisaConOtraFrase()
        {
            ReglaVozPeaton r = new ReglaVozPeaton();
            r.Ocupar(0f, ReglaVozPeaton.SegundosDolor);
            Assert.That(r.Intentar(0.1f, MomentoVozPeaton.Miedo), Is.False);
            Assert.That(r.Intentar(ReglaVozPeaton.SegundosDolor + 0.01f, MomentoVozPeaton.Enojo), Is.True);
        }

        [Test]
        public void Ambientales_EsperanElSilencioDeTodos()
        {
            Assert.That(ReglaVozPeaton.EsAmbiental(MomentoVozPeaton.Saludo), Is.True);
            Assert.That(ReglaVozPeaton.EsAmbiental(MomentoVozPeaton.Vendedor), Is.True);
            Assert.That(ReglaVozPeaton.EsAmbiental(MomentoVozPeaton.Risa), Is.True);
            Assert.That(ReglaVozPeaton.EsAmbiental(MomentoVozPeaton.Miedo), Is.False);
            Assert.That(ReglaVozPeaton.TocaAmbientalDeTodos(1f, 0f), Is.False);
            Assert.That(ReglaVozPeaton.TocaAmbientalDeTodos(2f, 0f), Is.True);
        }

        [Test]
        public void SoloHombresCiviles_UsanLaVozGrabada()
        {
            Assert.That(ReglaVozPeaton.UsaVozGrabada(ReglaSonidoNpc.Genero("Visual_man_03", 0)), Is.True);
            Assert.That(ReglaVozPeaton.UsaVozGrabada(ReglaSonidoNpc.Genero("Visual_woman_01", 0)), Is.False);
            Assert.That(ReglaVozPeaton.UsaVozGrabada(ReglaSonidoNpc.Genero("robot", 0)), Is.False);
            Assert.That(ReglaVozPeaton.EsCivil("Vendedor_Tacos|Visual_man"), Is.True);
            Assert.That(ReglaVozPeaton.EsCivil("Guardia_Nova_02"), Is.False);
            Assert.That(ReglaVozPeaton.EsCivil("Policia_01"), Is.False);
        }
    }
}
