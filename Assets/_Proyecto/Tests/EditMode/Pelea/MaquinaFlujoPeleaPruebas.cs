using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Flujo;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Pelea
{
    public class MaquinaFlujoPeleaPruebas
    {
        private const float SegundosPorNumero = 1f;
        private const float PausaKO = 2f;
        private const float Margen = 1e-4f;

        private MaquinaFlujoPelea maquina;
        private List<int> numeros;
        private int inicios;
        private List<ResultadoPelea> resultados;
        private List<LadoPelea> kos;

        [SetUp]
        public void SetUp()
        {
            maquina = new MaquinaFlujoPelea(SegundosPorNumero, PausaKO);
            numeros = new List<int>();
            resultados = new List<ResultadoPelea>();
            kos = new List<LadoPelea>();
            inicios = 0;
            maquina.ConteoCambio += numeros.Add;
            maquina.PeleaIniciada += () => inicios++;
            maquina.ResultadoListo += resultados.Add;
            maquina.KOOcurrido += kos.Add;
        }

        private void LlegarAPeleando()
        {
            maquina.IniciarConteo();
            maquina.Avanzar(SegundosPorNumero * 3f);
        }

        private void LlegarAResultado(LadoPelea ladoEnKO)
        {
            LlegarAPeleando();
            maquina.NotificarKO(ladoEnKO);
            maquina.Avanzar(PausaKO);
        }

        [Test]
        public void Estado_AlCrear_EsPreparandoYNoAvisaNada()
        {
            maquina.Avanzar(10f);
            Assert.AreEqual(EstadoFlujoPelea.Preparando, maquina.Estado);
            Assert.IsEmpty(numeros);
        }

        [Test]
        public void IniciarConteo_DesdePreparando_PasaAConteoYAvisaTres()
        {
            Assert.IsTrue(maquina.IniciarConteo());
            Assert.AreEqual(EstadoFlujoPelea.Conteo, maquina.Estado);
            CollectionAssert.AreEqual(new[] { 3 }, numeros);
        }

        [Test]
        public void IniciarConteo_FueraDePreparando_NoHaceNada()
        {
            maquina.IniciarConteo();
            Assert.IsFalse(maquina.IniciarConteo());
            CollectionAssert.AreEqual(new[] { 3 }, numeros);
        }

        [Test]
        public void Avanzar_PasoAPaso_AvisaTresDosUnoYCeroEnOrdenYPasaAPeleando()
        {
            maquina.IniciarConteo();
            maquina.Avanzar(SegundosPorNumero * 0.9f);
            CollectionAssert.AreEqual(new[] { 3 }, numeros);
            maquina.Avanzar(SegundosPorNumero * 0.2f);
            CollectionAssert.AreEqual(new[] { 3, 2 }, numeros);
            maquina.Avanzar(SegundosPorNumero);
            maquina.Avanzar(SegundosPorNumero);
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, numeros);
            Assert.AreEqual(EstadoFlujoPelea.Peleando, maquina.Estado);
            Assert.AreEqual(1, inicios);
        }

        [Test]
        public void Avanzar_DtGrande_NoSeSaltaNumeros()
        {
            maquina.IniciarConteo();
            maquina.Avanzar(SegundosPorNumero * 10f);
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, numeros);
            Assert.AreEqual(EstadoFlujoPelea.Peleando, maquina.Estado);
            Assert.AreEqual(1, inicios);
        }

        [Test]
        public void Avanzar_DtGrande_ElSobranteDelConteoCuentaComoPelea()
        {
            maquina.IniciarConteo();
            maquina.Avanzar(SegundosPorNumero * 3f + 0.5f);
            Assert.AreEqual(0.5f, maquina.DuracionPelea, Margen);
        }

        [Test]
        public void NotificarKO_FueraDePeleando_SeIgnora()
        {
            maquina.NotificarKO(LadoPelea.Rival);
            Assert.AreEqual(EstadoFlujoPelea.Preparando, maquina.Estado);
            maquina.IniciarConteo();
            maquina.NotificarKO(LadoPelea.Rival);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, maquina.Estado);
            Assert.IsEmpty(kos);
        }

        [Test]
        public void NotificarKO_EnPeleando_PasaAKOYGanaElOtroLado()
        {
            LlegarAPeleando();
            maquina.NotificarKO(LadoPelea.Rival);
            Assert.AreEqual(EstadoFlujoPelea.KO, maquina.Estado);
            Assert.AreEqual(LadoPelea.Jugador, maquina.Ganador);
            CollectionAssert.AreEqual(new[] { LadoPelea.Jugador }, kos);
        }

        [Test]
        public void NotificarKO_SegundoKO_SeIgnoraYGanaElDelPrimero()
        {
            LlegarAPeleando();
            maquina.NotificarKO(LadoPelea.Rival);
            maquina.NotificarKO(LadoPelea.Jugador);
            Assert.AreEqual(LadoPelea.Jugador, maquina.Ganador);
            Assert.AreEqual(1, kos.Count);
            maquina.Avanzar(PausaKO);
            Assert.AreEqual(LadoPelea.Jugador, resultados[0].Ganador);
        }

        [Test]
        public void Avanzar_DespuesDelKO_EsperaLaPausaYPasaAResultadoConGanadorYDuracion()
        {
            LlegarAPeleando();
            maquina.Avanzar(4f);
            maquina.NotificarKO(LadoPelea.Jugador);

            maquina.Avanzar(PausaKO * 0.9f);
            Assert.AreEqual(EstadoFlujoPelea.KO, maquina.Estado);
            Assert.IsEmpty(resultados);

            maquina.Avanzar(PausaKO * 0.2f);
            Assert.AreEqual(EstadoFlujoPelea.Resultado, maquina.Estado);
            Assert.AreEqual(1, resultados.Count);
            Assert.AreEqual(LadoPelea.Rival, resultados[0].Ganador);
            Assert.AreEqual(4f, resultados[0].DuracionSegundos, Margen);
        }

        [Test]
        public void Avanzar_EnResultado_NoRepiteElResultado()
        {
            LlegarAResultado(LadoPelea.Rival);
            maquina.Avanzar(10f);
            Assert.AreEqual(1, resultados.Count);
        }

        [Test]
        public void PedirRevancha_FueraDeResultado_NoHaceNada()
        {
            Assert.IsFalse(maquina.PedirRevancha());
            maquina.IniciarConteo();
            Assert.IsFalse(maquina.PedirRevancha());
            Assert.AreEqual(EstadoFlujoPelea.Conteo, maquina.Estado);
            maquina.Avanzar(SegundosPorNumero * 3f);
            Assert.IsFalse(maquina.PedirRevancha());
            Assert.AreEqual(EstadoFlujoPelea.Peleando, maquina.Estado);
            maquina.NotificarKO(LadoPelea.Rival);
            Assert.IsFalse(maquina.PedirRevancha());
            Assert.AreEqual(EstadoFlujoPelea.KO, maquina.Estado);
        }

        [Test]
        public void PedirRevancha_EnResultado_VuelveAPreparandoYPermiteOtraPelea()
        {
            LlegarAResultado(LadoPelea.Rival);
            Assert.IsTrue(maquina.PedirRevancha());
            Assert.AreEqual(EstadoFlujoPelea.Preparando, maquina.Estado);

            numeros.Clear();
            maquina.IniciarConteo();
            maquina.Avanzar(SegundosPorNumero * 3f);
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, numeros);
            maquina.NotificarKO(LadoPelea.Jugador);
            maquina.Avanzar(PausaKO);
            Assert.AreEqual(2, resultados.Count);
            Assert.AreEqual(LadoPelea.Rival, resultados[1].Ganador);
            Assert.AreEqual(0f, resultados[1].DuracionSegundos, Margen);
        }
    }
}
