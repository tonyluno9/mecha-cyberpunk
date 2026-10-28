using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-194: la regla pura del sistema nervioso (multiplicadores, pisos, tope y lo que no cambia).</summary>
    public class ReglaReaccionImplantesPruebas
    {
        private const float Tolerancia = 0.0001f;

        // Mecha de serie de D-tony-165: golpe 0.25, guardia 0.15, ventana 0.2.
        private static ValoresMecha DeSerie()
        {
            return new ValoresMecha
            {
                nombre = "Serie", vidaMaxima = 200f, velocidadMaxima = 6f, aceleracion = 10f, desaceleracion = 12f,
                velocidadGiroMaxima = 90f, danioGolpe = 25f, alcanceGolpe = 3f, preparacionGolpe = 0.25f, recargaGolpe = 0.8f,
                tiempoSubirGuardia = 0.15f, reduccionBloqueo = 0.5f, ventanaBloqueoPerfecto = 0.2f
            };
        }

        [Test]
        public void Aplicar_Sinapsis1_AcortaGolpeYGuardiaYAlargaLaVentana()
        {
            ValoresMecha v = ReglaReaccionImplantes.Aplicar(DeSerie(), 0.85f);
            Assert.AreEqual(0.2125f, v.preparacionGolpe, Tolerancia);
            Assert.AreEqual(0.1275f, v.tiempoSubirGuardia, Tolerancia);
            Assert.AreEqual(0.2f / 0.85f, v.ventanaBloqueoPerfecto, Tolerancia);
        }

        [Test]
        public void Aplicar_Sinapsis2_AcortaMasYRespetaElTope()
        {
            ValoresMecha v = ReglaReaccionImplantes.Aplicar(DeSerie(), 0.7f);
            Assert.AreEqual(0.175f, v.preparacionGolpe, Tolerancia);
            Assert.AreEqual(0.105f, v.tiempoSubirGuardia, Tolerancia);
            Assert.AreEqual(0.2f / 0.7f, v.ventanaBloqueoPerfecto, Tolerancia);
            Assert.LessOrEqual(v.ventanaBloqueoPerfecto, ReglaReaccionImplantes.VentanaMaxima);
        }

        [Test]
        public void Aplicar_MultiplicadorExtremo_RespetaPisosYTope()
        {
            ValoresMecha v = ReglaReaccionImplantes.Aplicar(DeSerie(), 0.1f);
            Assert.AreEqual(ReglaReaccionImplantes.PreparacionMinima, v.preparacionGolpe, Tolerancia);
            Assert.AreEqual(ReglaReaccionImplantes.GuardiaMinima, v.tiempoSubirGuardia, Tolerancia);
            Assert.AreEqual(ReglaReaccionImplantes.VentanaMaxima, v.ventanaBloqueoPerfecto, Tolerancia);
        }

        [Test]
        public void Aplicar_ValoresYaPasadosDelPisoOTope_NoLosEmpeora()
        {
            ValoresMecha v = DeSerie();
            v.preparacionGolpe = 0.1f;
            v.tiempoSubirGuardia = 0.05f;
            v.ventanaBloqueoPerfecto = 0.4f;
            ValoresMecha r = ReglaReaccionImplantes.Aplicar(v, 0.7f);
            Assert.AreEqual(0.1f, r.preparacionGolpe, Tolerancia, "no sube al piso");
            Assert.AreEqual(0.05f, r.tiempoSubirGuardia, Tolerancia);
            Assert.AreEqual(0.4f, r.ventanaBloqueoPerfecto, Tolerancia, "no baja al tope");
        }

        [TestCase(1f)]
        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void Aplicar_MultiplicadorNeutroOInvalido_NoCambiaNada(float multiplicador)
        {
            ValoresMecha v = DeSerie();
            ValoresMecha r = ReglaReaccionImplantes.Aplicar(v, multiplicador);
            Assert.AreEqual(v.preparacionGolpe, r.preparacionGolpe);
            Assert.AreEqual(v.tiempoSubirGuardia, r.tiempoSubirGuardia);
            Assert.AreEqual(v.ventanaBloqueoPerfecto, r.ventanaBloqueoPerfecto);
        }

        [TestCase(0.85f, 0f, 0f, 0f, 0f)]
        [TestCase(0.7f, 60f, 30f, 1.5f, 0.1f)]
        [TestCase(0.7f, -30f, -2f, -1f, 0.3f)]
        public void Aplicar_CualquierImplanteYPieza_VidaDanioYDemasQuedanIguales(float m, float vida, float danio, float velocidad, float recarga)
        {
            ValoresMecha v = DeSerie();
            v.vidaMaxima += vida;
            v.danioGolpe += danio;
            v.velocidadMaxima += velocidad;
            v.recargaGolpe += recarga;
            ValoresMecha r = ReglaReaccionImplantes.Aplicar(v, m);
            Assert.AreEqual(v.vidaMaxima, r.vidaMaxima);
            Assert.AreEqual(v.danioGolpe, r.danioGolpe);
            Assert.AreEqual(v.velocidadMaxima, r.velocidadMaxima);
            Assert.AreEqual(v.aceleracion, r.aceleracion);
            Assert.AreEqual(v.desaceleracion, r.desaceleracion);
            Assert.AreEqual(v.velocidadGiroMaxima, r.velocidadGiroMaxima);
            Assert.AreEqual(v.alcanceGolpe, r.alcanceGolpe);
            Assert.AreEqual(v.recargaGolpe, r.recargaGolpe);
            Assert.AreEqual(v.reduccionBloqueo, r.reduccionBloqueo);
            Assert.AreEqual(v.nombre, r.nombre);
        }
    }
}
