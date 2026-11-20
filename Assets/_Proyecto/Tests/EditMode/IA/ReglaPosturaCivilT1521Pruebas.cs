using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1521: la guarda que levanta a un civil vivo acostado en la calle.</summary>
    public class ReglaPosturaCivilT1521Pruebas
    {
        private const float CabezaBaja = 0.2f;
        private const float CabezaDePie = 1.6f;
        private const float AlturaMinima = 0.5f;
        private const float SegundosBajo = 2.5f;
        private const float SegundosTumbado = 8f;
        private const float Paso = 0.5f;

        private static ReglaPosturaCivil Nueva() => new ReglaPosturaCivil(AlturaMinima, SegundosBajo, SegundosTumbado);

        private static ReglaPosturaCivil.Accion Correr(ReglaPosturaCivil regla, float segundos, float cabeza, bool muerto, bool caido, bool bucle, bool derecha)
        {
            ReglaPosturaCivil.Accion ultima = ReglaPosturaCivil.Accion.Nada;
            for (float t = 0f; t < segundos; t += Paso)
            {
                ReglaPosturaCivil.Accion a = regla.Avanzar(Paso, cabeza, muerto, caido, bucle, derecha);
                if (a != ReglaPosturaCivil.Accion.Nada)
                {
                    ultima = a;
                }
            }
            return ultima;
        }

        [Test]
        public void CabezaBaja_VivoSinKO_SeLevantaTrasElTiempo()
        {
            ReglaPosturaCivil regla = Nueva();
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(regla, SegundosBajo - Paso, CabezaBaja, false, false, false, true), "todavía no pasa el tiempo");
            Assert.AreEqual(ReglaPosturaCivil.Accion.Levantar, regla.Avanzar(Paso, CabezaBaja, false, false, false, true));
        }

        [Test]
        public void DePie_NuncaHaceNada()
        {
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(Nueva(), 60f, CabezaDePie, false, false, false, true));
        }

        [Test]
        public void CabezaBaja_QueSeLevanta_ReiniciaElConteo()
        {
            ReglaPosturaCivil regla = Nueva();
            Correr(regla, SegundosBajo - Paso, CabezaBaja, false, false, false, true);
            regla.Avanzar(Paso, CabezaDePie, false, false, false, true);
            Assert.AreEqual(0f, regla.SegundosBajo, 0.0001f);
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(regla, SegundosBajo - Paso, CabezaBaja, false, false, false, true));
        }

        [Test]
        public void Muerto_NoSeToca()
        {
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(Nueva(), 60f, CabezaBaja, true, true, false, true));
        }

        [Test]
        public void ClipEnBucle_AgachadoOPlaticando_NoSeToca()
        {
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(Nueva(), 60f, CabezaBaja, false, false, true, true));
        }

        [Test]
        public void RaizGirada_CaidaDeRespaldoDelPeaton_NoSeToca()
        {
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(Nueva(), 60f, CabezaBaja, false, false, false, false));
        }

        [Test]
        public void TumbadoSinAvisoDeMuerte_SeLevantaAlPasarElTiempoLargo()
        {
            ReglaPosturaCivil regla = Nueva();
            Assert.AreEqual(ReglaPosturaCivil.Accion.Nada, Correr(regla, SegundosTumbado - Paso, CabezaBaja, false, true, false, true), "un conductor sacado del carro se levanta solo en menos de 2 s");
            Assert.AreEqual(ReglaPosturaCivil.Accion.Levantar, regla.Avanzar(Paso, CabezaBaja, false, true, false, true));
        }

        [Test]
        public void TrasLevantar_ElConteoQuedaEnCero()
        {
            ReglaPosturaCivil regla = Nueva();
            Correr(regla, SegundosBajo, CabezaBaja, false, false, false, true);
            Assert.AreEqual(0f, regla.SegundosBajo, 0.0001f);
            Assert.AreEqual(0f, regla.SegundosTumbado, 0.0001f);
        }
    }
}
