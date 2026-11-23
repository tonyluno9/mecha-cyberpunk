using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1486: curvas del peso del disparo, floración de la dispersión, datos por arma y sonidos de la marca.</summary>
    public class DisparoPesoT1486Pruebas
    {
        private const float Tol = 0.0001f;

        [Test]
        public void Desplazamiento_AlInicioYAlFinal_EsCero()
        {
            Assert.AreEqual(0f, ReglaRetrocesoArma.Desplazamiento(0f, 0.2f), Tol);
            Assert.AreEqual(0f, ReglaRetrocesoArma.Desplazamiento(0.2f, 0.2f), Tol);
            Assert.AreEqual(0f, ReglaRetrocesoArma.Desplazamiento(0.5f, 0.2f), Tol);
        }

        [Test]
        public void Desplazamiento_AlFinalDeLaSubida_EsElMaximo()
        {
            float duracion = 0.2f;
            Assert.AreEqual(1f, ReglaRetrocesoArma.Desplazamiento(duracion * ReglaRetrocesoArma.FraccionSubida, duracion), Tol);
        }

        [Test]
        public void Desplazamiento_TrasElMaximo_BajaSinSubirOtraVez()
        {
            float anterior = 1f;
            for (float t = 0.05f; t < 0.2f; t += 0.01f)
            {
                float v = ReglaRetrocesoArma.Desplazamiento(t, 0.2f);
                Assert.LessOrEqual(v, anterior + Tol, "t=" + t);
                anterior = v;
            }
        }

        [Test]
        public void Desplazamiento_SubeMasRapidoDeLoQueRegresa()
        {
            float duracion = 0.4f;
            float tSubida = duracion * ReglaRetrocesoArma.FraccionSubida;
            float tRegreso = duracion - tSubida;
            Assert.Less(tSubida, tRegreso);
            Assert.Greater(ReglaRetrocesoArma.Desplazamiento(tSubida * 0.5f, duracion), 0.6f);
        }

        [Test]
        public void Recuperacion_ParteDeUnoYLlegaACero()
        {
            Assert.AreEqual(1f, ReglaRetrocesoArma.Recuperacion(0f, 0.3f), Tol);
            Assert.AreEqual(0.25f, ReglaRetrocesoArma.Recuperacion(0.15f, 0.3f), Tol);
            Assert.AreEqual(0f, ReglaRetrocesoArma.Recuperacion(1f, 0.3f), Tol);
        }

        [Test]
        public void Floracion_CadaTiroSumaYRespetaElTope()
        {
            var f = new ReglaFloracionDispersion();
            for (int i = 0; i < 20; i++)
            {
                f.Disparar(0f, 0.3f, 1.5f, 6f);
            }
            Assert.AreEqual(1.5f, f.Actual(0f, 6f), Tol);
        }

        [Test]
        public void Floracion_SeRecuperaConElTiempoYNoBajaDeCero()
        {
            var f = new ReglaFloracionDispersion();
            f.Disparar(1f, 1.2f, 3f, 6f);
            Assert.AreEqual(0.6f, f.Actual(1.1f, 6f), Tol);
            Assert.AreEqual(0f, f.Actual(5f, 6f), Tol);
        }

        [Test]
        public void Floracion_Apuntando_CuentaLaMitad()
        {
            Assert.AreEqual(1f, ReglaFloracionDispersion.Aplicada(2f, true), Tol);
            Assert.AreEqual(2f, ReglaFloracionDispersion.Aplicada(2f, false), Tol);
        }

        [Test]
        public void Ajustes_TieneUnaEntradaPorArma()
        {
            Assert.AreEqual(System.Enum.GetValues(typeof(ArmaAPie)).Length, DatosSensacion.AjustesPorArma().Length);
        }

        [Test]
        public void Ajustes_LaEscopetaPesaMasQueElFusilEnTodo()
        {
            AjusteSensacion[] a = DatosSensacion.AjustesPorArma();
            AjusteSensacion escopeta = a[(int)ArmaAPie.Escopeta];
            AjusteSensacion fusil = a[(int)ArmaAPie.Fusil];
            Assert.Greater(escopeta.patada, fusil.patada);
            Assert.Greater(escopeta.retrocesoArma, fusil.retrocesoArma);
            Assert.Greater(escopeta.impulso, fusil.impulso);
            Assert.Greater(escopeta.duracionRetrocesoArma, fusil.duracionRetrocesoArma);
        }

        [Test]
        public void Ajustes_ArmasDeFuegoTienenImpulsoYLosPunosNo()
        {
            AjusteSensacion[] a = DatosSensacion.AjustesPorArma();
            Assert.AreEqual(0f, a[(int)ArmaAPie.Punos].impulso);
            foreach (ArmaAPie arma in new[] { ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper, ArmaAPie.Subfusil, ArmaAPie.Revolver })
            {
                Assert.Greater(a[(int)arma].impulso, 0f, arma.ToString());
                Assert.Greater(a[(int)arma].retrocesoArma, 0f, arma + " retroceso");
            }
        }

        [Test]
        public void Ajustes_SoloLasAutomaticasFloran()
        {
            AjusteSensacion[] a = DatosSensacion.AjustesPorArma();
            Assert.Greater(a[(int)ArmaAPie.Fusil].floracion, 0f);
            Assert.Greater(a[(int)ArmaAPie.Subfusil].floracion, 0f);
            Assert.AreEqual(0f, a[(int)ArmaAPie.Sniper].floracion);
            foreach (AjusteSensacion ajuste in a)
            {
                Assert.GreaterOrEqual(ajuste.topeFloracion, ajuste.floracion);
            }
        }

        [Test]
        public void MarcaImpacto_MatarNoSuenaYPegarSi()
        {
            // T64: sin sonido de confirmación de kill.
            Assert.IsNull(SonidosMarcaImpacto.Id(true));
            Assert.AreEqual(SonidosMarcaImpacto.Pegar, SonidosMarcaImpacto.Id(false));
        }
    }
}
