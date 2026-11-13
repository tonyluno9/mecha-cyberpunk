using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>S2: lógica pura de trepar escaleras (ReglaTrepar) y de agarrarse de bordes (CalculoAgarre).</summary>
    public class TreparPruebas
    {
        private const float AlturaEscalera = 3f;
        private const float VelocidadEscalera = 2f;
        private const float Minima = 0.5f;
        private const float Maxima = 2.3f;
        private const float NormalMinima = 0.7f;
        private const float SegundosBase = 0.35f;
        private const float SegundosMetro = 0.2f;

        private static ResultadoAgarre Evaluar(float altura, bool espacio = true, bool apoyo = true, float normalY = 1f)
        {
            return CalculoAgarre.Evaluar(altura, espacio, apoyo, normalY, Minima, Maxima, NormalMinima, SegundosBase, SegundosMetro);
        }

        private static ReglaTrepar Escalera(float progresoInicial = 0f)
        {
            ReglaTrepar regla = new ReglaTrepar();
            regla.IniciarEscalera(AlturaEscalera, VelocidadEscalera, progresoInicial);
            return regla;
        }

        [Test]
        public void Escalera_ConW_SubeALaVelocidad()
        {
            ReglaTrepar regla = Escalera();
            regla.AvanzarEscalera(0.5f, 1f, false, false);
            Assert.AreEqual(EstadoTrepar.EnEscalera, regla.Estado);
            Assert.AreEqual(1f, regla.Progreso, 0.001f);
        }

        [Test]
        public void Escalera_ConS_Baja()
        {
            ReglaTrepar regla = Escalera(2f);
            regla.AvanzarEscalera(0.5f, -1f, false, false);
            Assert.AreEqual(1f, regla.Progreso, 0.001f);
        }

        [Test]
        public void Escalera_ConSEnElSueloYAbajo_Suelta()
        {
            ReglaTrepar regla = Escalera();
            AccionTrepar accion = regla.AvanzarEscalera(0.1f, -1f, true, false);
            Assert.AreEqual(AccionTrepar.Solto, accion);
            Assert.AreEqual(EstadoTrepar.Libre, regla.Estado);
        }

        [Test]
        public void Escalera_ConSEnElAire_NoSuelta()
        {
            ReglaTrepar regla = Escalera(1f);
            AccionTrepar accion = regla.AvanzarEscalera(0.1f, -1f, false, false);
            Assert.AreEqual(AccionTrepar.Nada, accion);
            Assert.AreEqual(EstadoTrepar.EnEscalera, regla.Estado);
        }

        [Test]
        public void Escalera_ConSalto_SueltaEnCualquierAltura()
        {
            ReglaTrepar regla = Escalera(1.5f);
            AccionTrepar accion = regla.AvanzarEscalera(0.1f, 1f, false, true);
            Assert.AreEqual(AccionTrepar.Solto, accion);
            Assert.AreEqual(EstadoTrepar.Libre, regla.Estado);
        }

        [Test]
        public void Escalera_AlLlegarALaCima_PideAgarreYNoPasaDeLaAltura()
        {
            ReglaTrepar regla = Escalera(2.9f);
            AccionTrepar accion = regla.AvanzarEscalera(1f, 1f, false, false);
            Assert.AreEqual(AccionTrepar.LlegoCima, accion);
            Assert.AreEqual(AlturaEscalera, regla.Progreso, 0.001f);
        }

        [Test]
        public void Agarre_TerminaTrasSusSegundosYVuelveALibre()
        {
            ReglaTrepar regla = Escalera(AlturaEscalera);
            regla.IniciarAgarre(0.4f);
            Assert.AreEqual(EstadoTrepar.Agarre, regla.Estado);
            Assert.IsFalse(regla.AvanzarAgarre(0.2f));
            Assert.AreEqual(0.5f, regla.ProgresoAgarre, 0.001f);
            Assert.IsTrue(regla.AvanzarAgarre(0.25f));
            Assert.AreEqual(EstadoTrepar.Libre, regla.Estado);
        }

        [Test]
        public void Evaluar_BordeEnRango_Valido()
        {
            Assert.IsTrue(Evaluar(1.6f).Valido);
        }

        [Test]
        public void Evaluar_BordeMuyBajo_NoValido()
        {
            Assert.IsFalse(Evaluar(Minima - 0.01f).Valido);
        }

        [Test]
        public void Evaluar_BordeMuyAlto_NoValido()
        {
            Assert.IsFalse(Evaluar(Maxima + 0.01f).Valido);
        }

        [Test]
        public void Evaluar_ExtremosDelRango_SonValidos()
        {
            Assert.IsTrue(Evaluar(Minima).Valido);
            Assert.IsTrue(Evaluar(Maxima).Valido);
        }

        [Test]
        public void Evaluar_SinEspacioArriba_NoValido()
        {
            Assert.IsFalse(Evaluar(1.6f, espacio: false).Valido);
        }

        [Test]
        public void Evaluar_SinApoyo_NoValido()
        {
            Assert.IsFalse(Evaluar(1f, apoyo: false).Valido);
        }

        [Test]
        public void Evaluar_SuperficieInclinada_NoValido()
        {
            Assert.IsFalse(Evaluar(1.6f, normalY: 0.5f).Valido);
        }

        [Test]
        public void Evaluar_DuracionCreceConLaAltura()
        {
            ResultadoAgarre bajo = Evaluar(0.6f);
            ResultadoAgarre alto = Evaluar(2f);
            Assert.AreEqual(SegundosBase + SegundosMetro * 0.6f, bajo.Segundos, 0.001f);
            Assert.AreEqual(SegundosBase + SegundosMetro * 2f, alto.Segundos, 0.001f);
            Assert.Greater(alto.Segundos, bajo.Segundos);
        }

        [Test]
        public void Posicion_EnLasDosFases_SubeYLuegoAvanza()
        {
            Vector3 inicio = new Vector3(0f, 0f, 0f);
            Vector3 final = new Vector3(0f, 1.6f, 0.8f);
            Vector3 finDeSubida = CalculoAgarre.Posicion(inicio, final, CalculoAgarre.FraccionSubida);
            Assert.AreEqual(1.6f, finDeSubida.y, 0.001f);
            Assert.AreEqual(0f, finDeSubida.z, 0.001f);
            Vector3 mitad = CalculoAgarre.Posicion(inicio, final, CalculoAgarre.FraccionSubida / 2f);
            Assert.AreEqual(0f, mitad.z, 0.001f);
            Assert.Less(mitad.y, 1.6f);
            Assert.Greater(mitad.y, 0f);
            Assert.AreEqual(final, CalculoAgarre.Posicion(inicio, final, 1f));
            Assert.AreEqual(inicio, CalculoAgarre.Posicion(inicio, final, 0f));
        }
    }
}
