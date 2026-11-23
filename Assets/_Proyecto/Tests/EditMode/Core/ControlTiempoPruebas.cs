using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.EditMode.Core
{
    /// <summary>T81: el tiempo del juego siempre vuelve a 1 sin importar el orden en que se piden y sueltan las pausas y cámaras lentas.</summary>
    public sealed class ControlTiempoPruebas
    {
        private float escalaGuardada;
        private float fijoGuardado;

        [SetUp]
        public void Preparar()
        {
            escalaGuardada = Time.timeScale;
            fijoGuardado = Time.fixedDeltaTime;
            ControlTiempo.SoltarTodo();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void Limpiar()
        {
            ControlTiempo.SoltarTodo();
            Time.timeScale = escalaGuardada;
            Time.fixedDeltaTime = fijoGuardado;
        }

        [Test]
        public void SinPedidosEsUno()
        {
            Assert.AreEqual(1f, ControlTiempo.Escala);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void EncadenadosSoltadosEnOtroOrdenTerminanEnUno()
        {
            object rueda = new object();
            object aviso = new object();
            object pausa = new object();
            ControlTiempo.Pedir(rueda, 0.25f);
            ControlTiempo.Pedir(aviso, 0f);
            ControlTiempo.Pedir(pausa, 0f);
            Assert.AreEqual(0f, Time.timeScale);
            ControlTiempo.Soltar(rueda);
            Assert.AreEqual(0f, Time.timeScale);
            ControlTiempo.Soltar(aviso);
            Assert.AreEqual(0f, Time.timeScale);
            ControlTiempo.Soltar(pausa);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, ControlTiempo.Cantidad);
        }

        [Test]
        public void RuedaAbiertaYAvisoQueSeCierraDespuesDejaUno()
        {
            object rueda = new object();
            object aviso = new object();
            ControlTiempo.Pedir(rueda, 0.25f);
            ControlTiempo.Pedir(aviso, 0f);
            ControlTiempo.Soltar(rueda);
            ControlTiempo.Soltar(aviso);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void GanaElMinimoYLaPausaGana()
        {
            object a = new object();
            object b = new object();
            ControlTiempo.Pedir(a, 0.5f);
            ControlTiempo.Pedir(b, 0.25f);
            Assert.AreEqual(0.25f, Time.timeScale, 0.0001f);
            ControlTiempo.Soltar(b);
            Assert.AreEqual(0.5f, Time.timeScale, 0.0001f);
            ControlTiempo.Pedir(b, 0f);
            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void PedirDosVecesElMismoDuenoNoAcumula()
        {
            object a = new object();
            ControlTiempo.Pedir(a, 0.25f);
            ControlTiempo.Pedir(a, 0.25f);
            ControlTiempo.Soltar(a);
            Assert.AreEqual(1f, Time.timeScale);
            ControlTiempo.Soltar(a);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void FixedDeltaTimeVuelveALaBase()
        {
            object a = new object();
            float baseFija = Time.fixedDeltaTime;
            ControlTiempo.Pedir(a, 0.25f);
            Assert.AreEqual(baseFija * 0.25f, Time.fixedDeltaTime, 0.00001f);
            ControlTiempo.Pedir(a, 0f);
            Assert.AreEqual(baseFija, Time.fixedDeltaTime, 0.00001f);
            ControlTiempo.Soltar(a);
            Assert.AreEqual(baseFija, Time.fixedDeltaTime, 0.00001f);
        }

        [Test]
        public void SoltarTodoReiniciaYDepurarLista()
        {
            object a = new object();
            ControlTiempo.Pedir(a, 0.25f);
            StringAssert.Contains("escala=0.25", ControlTiempo.Depurar());
            ControlTiempo.SoltarTodo();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual("escala=1 pedidos=[]", ControlTiempo.Depurar());
        }

        [Test]
        public void CalcularEsMinimoYUnoSiVacio()
        {
            Assert.AreEqual(1f, ControlTiempo.Calcular(new float[0]));
            Assert.AreEqual(0.25f, ControlTiempo.Calcular(new[] { 1f, 0.25f, 0.5f }));
            Assert.AreEqual(0f, ControlTiempo.Calcular(new[] { 0.25f, 0f }));
        }
    }
}
