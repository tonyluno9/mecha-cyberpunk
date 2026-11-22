using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ReglaEscaladaRespuestaT1620Pruebas
    {
        private static ReglaEscaladaRespuesta Crear()
        {
            ParametrosRespuesta numeros = ParametrosRespuesta.PorOmision();
            numeros.EstrellasModoNova = 0; // T-1631: estas pruebas son de la escalada de T8; el modo "NOVA INTERVIENE" se prueba en ReglaNovaIntervieneT1631Pruebas
            return new ReglaEscaladaRespuesta(numeros, new System.Random(1));
        }

        private static void Avanzar(ReglaEscaladaRespuesta r, float t, int estrellas, int[] vivos = null)
        {
            r.Avanzar(t, estrellas, false, false, vivos ?? new int[9]);
        }

        [Test]
        public void SinEstrellas_NoGeneraNada()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 0);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Patrulla, 0f));
        }

        [Test]
        public void UnaEstrella_SoloPatrullaYNoDron()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 1);
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.Patrulla, 0f));
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Dron, 0f));
        }

        [Test]
        public void PatrullaDestruida_ReemplazoSoloTrasElCooldown()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 1);
            r.RegistrarGenerado(TipoRespuesta.Patrulla, 0f);
            Avanzar(r, 1f, 1, VivosDe(TipoRespuesta.Patrulla, 1));
            r.RegistrarPerdida(TipoRespuesta.Patrulla, 10f);
            Avanzar(r, 10f, 1);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Patrulla, 25f), "a los 15 s aún no");
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.Patrulla, 40.1f), "pasados 30 s sí");
        }

        [Test]
        public void NuncaPasaDelTopeDePatrullas()
        {
            ReglaEscaladaRespuesta r = Crear();
            for (int i = 0; i < 10; i++)
            {
                Avanzar(r, i * 100f, 5, VivosDe(TipoRespuesta.Patrulla, 3));
            }
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Patrulla, 2000f));
        }

        [Test]
        public void NivelesSeDesbloqueanEnOrden_ConElTiempo()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 5, VivosDe(TipoRespuesta.Patrulla, 1));
            Assert.AreEqual(1, r.NivelDesbloqueado, "al inicio solo el nivel 1");
            Avanzar(r, 46f, 5, VivosDe(TipoRespuesta.Patrulla, 1));
            Assert.AreEqual(2, r.NivelDesbloqueado, "pasó su tiempo: sube un solo nivel");
        }

        [Test]
        public void PerderLasEstrellas_ReiniciaTiemposYNiveles()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 5);
            Avanzar(r, 100f, 5);
            Assert.Greater(r.NivelDesbloqueado, 1);
            Avanzar(r, 101f, 0);
            Assert.AreEqual(0, r.NivelDesbloqueado);
            Avanzar(r, 102f, 1);
            Assert.AreEqual(1, r.NivelDesbloqueado);
        }

        [Test]
        public void MisionProtegida_NoDejaGenerarPoliciaPeroNovaSiSiSePermite()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Avanzar(0f, 4, true, false, new int[9]);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Patrulla, 0f));
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 100f));
            r = Crear();
            r.Avanzar(0f, 4, true, true, new int[9]);
            r.Avanzar(1f, 4, true, true, new int[9]);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Patrulla, 100f));
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 100f));
        }

        [Test]
        public void MasBajas_PermitenUnaMasAlaVez()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 1);
            Assert.AreEqual(1, r.CupoActual(TipoRespuesta.Patrulla));
            r.RegistrarPerdida(TipoRespuesta.Patrulla, 0f);
            r.RegistrarPerdida(TipoRespuesta.Patrulla, 0f);
            Assert.AreEqual(2, r.CupoActual(TipoRespuesta.Patrulla));
        }

        private static int[] VivosDe(TipoRespuesta tipo, int cuantos)
        {
            int[] v = new int[9];
            v[(int)tipo] = cuantos;
            return v;
        }
    }
}
