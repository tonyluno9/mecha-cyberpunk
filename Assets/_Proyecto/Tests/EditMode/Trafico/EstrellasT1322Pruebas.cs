using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1322: estrellas más difíciles (2 al dispararle a la policía, 3 al reventar una patrulla), disparo a un carro frente a la policía, 5 estrellas y radio.</summary>
    public class EstrellasT1322Pruebas
    {
        private sealed class CalorDePrueba : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 75f ? 3 : (Valor >= 50f ? 2 : (Valor >= 25f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            public void Sumar(float cantidad, MotivoCalor motivo)
            {
                Valor = Math.Min(Maximo, Valor + cantidad);
                ValorCambio?.Invoke(Valor);
                NivelCambio?.Invoke(Nivel);
            }
            public void Enfriar(float cantidad) => Valor = Math.Max(0f, Valor - cantidad);
            public void Borrar() => Valor = 0f;
        }

        [Test]
        public void SubirA_DosEstrellas_LlegaAlMenosADosYNoPasaDeTres()
        {
            CalorDePrueba calor = new CalorDePrueba();
            EstrellasBusqueda.SubirA(calor, 2);
            int estrellas = EstrellasBusqueda.De(calor);
            Assert.GreaterOrEqual(estrellas, 2);
            Assert.LessOrEqual(estrellas, 3);
        }

        [Test]
        public void SubirA_TresEstrellas_Llega()
        {
            CalorDePrueba calor = new CalorDePrueba();
            EstrellasBusqueda.SubirA(calor, EstrellasBusqueda.EstrellasPorReventarPatrulla);
            Assert.GreaterOrEqual(EstrellasBusqueda.De(calor), 3);
        }

        [Test]
        public void SubirA_YaConMasEstrellas_NoSumaNada()
        {
            CalorDePrueba calor = new CalorDePrueba();
            calor.Sumar(90f, MotivoCalor.Pelea);
            EstrellasBusqueda.SubirA(calor, 2);
            Assert.AreEqual(90f, calor.Valor, 0.01f);
            EstrellasBusqueda.SubirA(null, 2);
        }

        [Test]
        public void Reglas_PatrullaYPolicia_SubenMasQueAntes()
        {
            Assert.AreEqual(2, EstrellasBusqueda.EstrellasPorDispararAPatrulla);
            Assert.AreEqual(3, EstrellasBusqueda.EstrellasPorReventarPatrulla);
            Assert.Greater(EstrellasBusqueda.EstrellasPorMatarPolicia, EstrellasBusqueda.EstrellasPorDispararAPolicia);
        }

        [Test]
        public void EnMaximo_SoloConCincoEstrellas()
        {
            Assert.IsFalse(EstrellasBusqueda.EnMaximo(4));
            Assert.IsTrue(EstrellasBusqueda.EnMaximo(5));
        }

        [Test]
        public void ReglaDisparoACarro_CuentaSoloTrasUnDisparoYCerca()
        {
            Assert.IsTrue(ReglaDisparoACarro.Cuenta(0.2f, 100f, 80f, 20f));
            Assert.IsFalse(ReglaDisparoACarro.Cuenta(3f, 100f, 80f, 20f), "Sin disparo reciente es un choque");
            Assert.IsFalse(ReglaDisparoACarro.Cuenta(0.2f, 100f, 100f, 20f), "No perdió vida");
            Assert.IsFalse(ReglaDisparoACarro.Cuenta(0.2f, 100f, 80f, ReglaDisparoACarro.DistanciaMaxima + 1f), "Muy lejos");
        }

        [Test]
        public void Radio_SinIdNoSuena_ConIdSuenaUnaVezPorEnfriamiento()
        {
            Assert.IsFalse(RadioPolicia.DebeSonar("", 10f, 0f));
            Assert.IsFalse(RadioPolicia.DebeSonar(null, 10f, 0f));
            Assert.IsTrue(RadioPolicia.DebeSonar("X", 10f, 0f));
            Assert.IsFalse(RadioPolicia.DebeSonar("X", 10f, 20f));
        }

        [Test]
        public void Config_CincoEstrellasTraeMasPatrullasYDrones_YRadioConID()
        {
            ConfigPersecucion config = ScriptableObject.CreateInstance<ConfigPersecucion>();
            Assert.GreaterOrEqual(config.PatrullasExtraCincoEstrellas, 1);
            Assert.GreaterOrEqual(config.DronesExtraCincoEstrellas, 1);
            Assert.AreEqual("POL_RADIO_AVISO", config.IdSonidoRadio);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
