using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1553: el E16 camina con el escudo arriba al acercarse al jugador o al recibir disparos (decisión de Tony 20-nov), con histéresis.</summary>
    public class ReglaEscudoArribaPruebas
    {
        private const float Dt = 0.02f;

        private static ReglaEscudoArriba Crear() => new ReglaEscudoArriba(ReglaEscudoArriba.Parametros.Diseno);

        private static void Pasar(ReglaEscudoArriba r, float segundos, float distancia, bool enPelea = true, bool bloqueado = false)
        {
            for (float t = 0f; t < segundos; t += Dt)
            {
                r.Avanzar(Dt, distancia, enPelea, bloqueado);
            }
        }

        [Test]
        public void SinPelea_NuncaLevantaElEscudo()
        {
            ReglaEscudoArriba r = Crear();
            Pasar(r, 3f, 2f, enPelea: false);
            Assert.IsFalse(r.EscudoArriba);
        }

        [Test]
        public void PeleandoYLejos_CaminaSinEscudo()
        {
            ReglaEscudoArriba r = Crear();
            Pasar(r, 3f, r.Config.DistanciaEscudo + 5f);
            Assert.IsFalse(r.EscudoArriba);
        }

        [Test]
        public void AlAcercarse_LevantaElEscudo()
        {
            ReglaEscudoArriba r = Crear();
            Pasar(r, 1f, r.Config.DistanciaEscudo + 5f);
            Pasar(r, Dt * 2f, r.Config.DistanciaEscudo - 1f);
            Assert.IsTrue(r.EscudoArriba);
        }

        [Test]
        public void AlRecibirUnDisparo_LevantaElEscudoAunqueEsteLejos_YLoBajaPasadosLosSegundos()
        {
            ReglaEscudoArriba r = Crear();
            float lejos = r.Config.DistanciaEscudo * 3f;
            Pasar(r, 1f, lejos);
            Assert.IsFalse(r.EscudoArriba);
            r.RegistrarImpacto();
            Pasar(r, Dt * 2f, lejos);
            Assert.IsTrue(r.EscudoArriba, "recibió un disparo");
            Pasar(r, r.Config.SegundosTrasImpacto - 0.5f, lejos);
            Assert.IsTrue(r.EscudoArriba, "sigue cubierto mientras dura la cobertura");
            Pasar(r, 1f, lejos);
            Assert.IsFalse(r.EscudoArriba, "sin más disparos lo baja");
        }

        [Test]
        public void Histeresis_UnaVezArriba_NoParpadeaEnElBorde()
        {
            ReglaEscudoArriba r = Crear();
            float d = r.Config.DistanciaEscudo;
            Pasar(r, Dt * 2f, d - 0.5f);
            Assert.IsTrue(r.EscudoArriba);
            Pasar(r, 0.5f, d + 0.5f);
            Assert.IsTrue(r.EscudoArriba, "apenas pasó el borde: sigue arriba");
            Pasar(r, 0.5f, d * r.Config.FactorHisteresis + 0.5f);
            Assert.IsFalse(r.EscudoArriba, "ya muy lejos: baja");
        }

        [Test]
        public void Bloqueado_PorDisparoOGolpe_BajaElEscudo()
        {
            ReglaEscudoArriba r = Crear();
            Pasar(r, Dt * 2f, 3f);
            Assert.IsTrue(r.EscudoArriba);
            Pasar(r, Dt * 2f, 3f, bloqueado: true);
            Assert.IsFalse(r.EscudoArriba, "dispara el cañón o golpea: sin escudo animado");
            Pasar(r, Dt * 2f, 3f);
            Assert.IsTrue(r.EscudoArriba, "al terminar vuelve");
        }

        [Test]
        public void Reiniciar_BajaElEscudoYOlvidaLosImpactos()
        {
            ReglaEscudoArriba r = Crear();
            r.RegistrarImpacto();
            Pasar(r, Dt * 2f, 100f);
            Assert.IsTrue(r.EscudoArriba);
            r.Reiniciar();
            Assert.IsFalse(r.EscudoArriba);
            Pasar(r, Dt * 2f, 100f);
            Assert.IsFalse(r.EscudoArriba, "el impacto anterior se olvidó");
        }
    }
}
