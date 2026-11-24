using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T94/T99: la fase 0 del modo libre (SKULLS) termina a 1 min o a 10 SKULLS vencidos (lo que pase primero) y gotea grupos dentro de los topes.</summary>
    public class ReglaFaseSkullsPruebas
    {
        private static ParametrosRespuesta Numeros()
        {
            ParametrosRespuesta p = ParametrosRespuesta.PorOmision();
            p.SegundosFaseSkulls = 60f;
            p.SkullsFaseSkulls = 10;
            p.SegundosPrimerGrupoSkulls = 4f;
            p.SegundosEntreGruposSkulls = 12f;
            p.SkullsPorGrupoMin = 2;
            p.SkullsPorGrupoMax = 3;
            p.SkullsVivosFaseSkulls = 6;
            p.FraccionSkullsArmados = 0.2f;
            return p;
        }

        private static void Matar(ReglaFaseSkulls r, int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                r.RegistrarMuerte();
            }
        }

        [Test]
        public void PorOmision_UnMinutoODiezSkulls_VeintePorCientoArmados()
        {
            ParametrosRespuesta p = new ParametrosRespuesta();
            Assert.AreEqual(60f, p.SegundosFaseSkulls);
            Assert.AreEqual(10, p.SkullsFaseSkulls);
            Assert.AreEqual(0.2f, p.FraccionSkullsArmados, 1e-5f);
        }

        [Test]
        public void Terminada_UnMinutoSinBajas_Termina()
        {
            ReglaFaseSkulls r = new ReglaFaseSkulls(Numeros());
            Assert.IsFalse(r.Avanzar(59f));
            Assert.IsFalse(r.Terminada);
            Assert.IsTrue(r.Avanzar(1f));
        }

        [Test]
        public void Terminada_DiezSkullsAntesDelMinuto_TerminaSinEsperar()
        {
            ReglaFaseSkulls r = new ReglaFaseSkulls(Numeros());
            Assert.IsFalse(r.Avanzar(5f));
            Matar(r, 9);
            Assert.IsFalse(r.Terminada);
            r.RegistrarMuerte();
            Assert.IsTrue(r.Terminada, "sin mínimo de 2 min (T99)");
            Assert.AreEqual(10, r.Muertes);
            r.RegistrarMuerte();
            Assert.AreEqual(10, r.Muertes, "las de sobra ya no cuentan");
        }

        [Test]
        public void CuantosGenerar_GoteoRespetaPrimerGrupoIntervaloYTopes()
        {
            ReglaFaseSkulls r = new ReglaFaseSkulls(Numeros());
            r.Avanzar(3f);
            Assert.AreEqual(0, r.CuantosGenerar(0, 24, 0f), "antes del primer grupo");
            r.Avanzar(1f);
            Assert.AreEqual(2, r.CuantosGenerar(0, 24, 0f));
            Assert.AreEqual(3, r.CuantosGenerar(0, 24, 0.99f));
            Assert.AreEqual(1, r.CuantosGenerar(5, 24, 0.99f), "tope de vivos del goteo");
            Assert.AreEqual(1, r.CuantosGenerar(0, 1, 0.99f), "tope de enemigos del modo libre (PC 24 / VR 12)");
            Assert.AreEqual(0, r.CuantosGenerar(0, 0, 0.99f));
            r.RegistrarGrupo();
            r.Avanzar(11f);
            Assert.AreEqual(0, r.CuantosGenerar(0, 24, 0f), "entre grupos");
            r.Avanzar(1f);
            Assert.AreEqual(2, r.CuantosGenerar(0, 24, 0f));
            r.Avanzar(60f);
            Assert.AreEqual(0, r.CuantosGenerar(0, 24, 0f), "terminada: ya no salen");
        }

        [Test]
        public void EsArmado_VeintePorCiento()
        {
            ReglaFaseSkulls r = new ReglaFaseSkulls(Numeros());
            Assert.IsTrue(r.EsArmado(0.19f));
            Assert.IsFalse(r.EsArmado(0.2f));
            Assert.IsFalse(r.EsArmado(0.9f));
        }

        [Test]
        public void ReglaEscalada_VivosExtraLibre_CuentanParaElTope()
        {
            ParametrosRespuesta p = Numeros();
            ReglaEscaladaRespuesta e = new ReglaEscaladaRespuesta(p);
            e.PonerModoLibre(new FaseModoLibre("SKULLS", 0f, 1, false, new TipoRespuesta[0], 0, null));
            e.VivosExtraLibre = 5;
            Assert.AreEqual(5, e.VivosLibre);
            Assert.IsFalse(e.PuedeGenerar(TipoRespuesta.PoliciaAPie, 0f), "fase 0: sin tipos, no llega la policía");
        }
    }
}
