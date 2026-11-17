using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1109: el patrón del guardia pesado (guardia, persecución, empujón, mazazo cada 3 ataques, recuperación y aturdimiento).</summary>
    public class ReglaGuardiaPesadoPruebas
    {
        private const float Dt = 0.01f;

        private static ReglaGuardiaPesado Crear() => new ReglaGuardiaPesado(ReglaGuardiaPesado.Parametros.Diseno);

        private static ReglaGuardiaPesado.Entrada En(float distancia, bool enPelea = true) => new ReglaGuardiaPesado.Entrada { Distancia = distancia, EnPelea = enPelea };

        // Avanza hasta que haya un impacto; devuelve el tipo y deja el tiempo transcurrido.
        private static ReglaGuardiaPesado.Ataque? HastaImpacto(ReglaGuardiaPesado r, float distancia, out float tiempo, float max = 6f)
        {
            tiempo = 0f;
            while (tiempo < max)
            {
                ReglaGuardiaPesado.Salida s = r.Avanzar(Dt, En(distancia));
                tiempo += Dt;
                if (s.Impacto)
                {
                    return s.Tipo;
                }
            }
            return null;
        }

        private static void Esperar(ReglaGuardiaPesado r, float distancia, float segundos)
        {
            for (float t = 0f; t < segundos; t += Dt)
            {
                r.Avanzar(Dt, En(distancia));
            }
        }

        [Test]
        public void SinProvocar_SeQuedaDeGuardiaAunqueElJugadorEsteCerca()
        {
            ReglaGuardiaPesado r = Crear();
            for (int i = 0; i < 500; i++)
            {
                ReglaGuardiaPesado.Salida s = r.Avanzar(Dt, En(1f, false));
                Assert.IsFalse(s.Impacto);
                Assert.IsFalse(s.EmpezoAviso);
            }
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Guardia, r.Actual);
        }

        [Test]
        public void Provocado_PersigueYAtacaAlLlegar()
        {
            ReglaGuardiaPesado r = Crear();
            r.Avanzar(Dt, En(10f));
            r.Avanzar(Dt, En(10f));
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Persigue, r.Actual);
            r.Avanzar(Dt, En(1.5f));
            Assert.AreEqual(ReglaGuardiaPesado.Fase.AvisoEmpujon, r.Actual);
        }

        [Test]
        public void Empujon_AvisoDeSieteDecimasYRecuperaUnPuntoDos()
        {
            ReglaGuardiaPesado r = Crear();
            Esperar(r, 1.5f, 0.02f);
            ReglaGuardiaPesado.Ataque? tipo = HastaImpacto(r, 1.5f, out float t);
            Assert.AreEqual(ReglaGuardiaPesado.Ataque.Empujon, tipo);
            Assert.AreEqual(0.7f, t, 0.03f);
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Recupera, r.Actual);
            Esperar(r, 10f, 1.15f);
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Recupera, r.Actual, "a 1.15 s sigue recuperándose");
            Esperar(r, 10f, 0.1f);
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Persigue, r.Actual, "a 1.25 s ya persigue");
        }

        [Test]
        public void CadaTresAtaques_UnMazazoConAvisoDeUnPuntoUno()
        {
            ReglaGuardiaPesado r = Crear();
            Esperar(r, 1.5f, 0.02f);
            List<ReglaGuardiaPesado.Ataque> ataques = new List<ReglaGuardiaPesado.Ataque>();
            List<float> avisos = new List<float>();
            for (int i = 0; i < 6; i++)
            {
                ReglaGuardiaPesado.Ataque? a = HastaImpacto(r, 1.5f, out float t, 10f);
                Assert.IsNotNull(a);
                ataques.Add(a.Value);
                avisos.Add(t);
                Esperar(r, 10f, 1.3f); // recupera y queda persiguiendo
            }
            CollectionAssert.AreEqual(new[]
            {
                ReglaGuardiaPesado.Ataque.Empujon, ReglaGuardiaPesado.Ataque.Empujon, ReglaGuardiaPesado.Ataque.Mazazo,
                ReglaGuardiaPesado.Ataque.Empujon, ReglaGuardiaPesado.Ataque.Empujon, ReglaGuardiaPesado.Ataque.Mazazo
            }, ataques);
            Assert.AreEqual(6, r.AtaquesHechos);
        }

        [Test]
        public void Mazazo_AvisoDeUnPuntoUnoSegundos()
        {
            ReglaGuardiaPesado r = Crear();
            Esperar(r, 1.5f, 0.02f);
            ReglaGuardiaPesado.Ataque? a;
            float t = 0f;
            for (int i = 0; i < 2; i++)
            {
                HastaImpacto(r, 1.5f, out t, 10f);
                Esperar(r, 10f, 1.3f);
            }
            a = HastaImpacto(r, 1.5f, out t, 10f);
            Assert.AreEqual(ReglaGuardiaPesado.Ataque.Mazazo, a);
            Assert.AreEqual(1.1f, t, 0.03f);
        }

        [Test]
        public void Aturdir_DuraUnPuntoDosYEsCastigable()
        {
            ReglaGuardiaPesado r = Crear();
            Esperar(r, 1.5f, 0.3f); // en aviso
            r.Aturdir();
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Aturdido, r.Actual);
            Assert.IsTrue(r.Castigable);
            Esperar(r, 10f, 1.15f);
            Assert.IsTrue(r.Castigable, "a 1.15 s sigue aturdido");
            Esperar(r, 10f, 0.1f);
            Assert.IsFalse(r.Castigable);
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Persigue, r.Actual);
        }

        [Test]
        public void SoloEsCastigableAturdido()
        {
            ReglaGuardiaPesado r = Crear();
            Esperar(r, 1.5f, 0.3f);
            Assert.IsFalse(r.Castigable);
            HastaImpacto(r, 1.5f, out float t);
            Assert.IsFalse(r.Castigable, "recuperándose de un ataque no es castigable");
        }

        [Test]
        public void SiPierdeLaProvocacionEnElAviso_VuelveAGuardia()
        {
            ReglaGuardiaPesado r = Crear();
            Esperar(r, 1.5f, 0.3f);
            r.Avanzar(Dt, En(40f, false));
            Assert.AreEqual(ReglaGuardiaPesado.Fase.Guardia, r.Actual);
        }

        [Test]
        public void Parametros_SonLosDelDiseno()
        {
            ReglaGuardiaPesado.Parametros p = ReglaGuardiaPesado.Parametros.Diseno;
            Assert.AreEqual(18f, p.DanioEmpujon);
            Assert.AreEqual(28f, p.DanioMazazo);
            Assert.AreEqual(2.5f, p.RadioMazazo);
            Assert.AreEqual(90f, p.GradosGiro);
            Assert.AreEqual(3, p.AtaquesPorMazazo);
        }
    }
}
