using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1475: grupo de motor por tipo, tono con la velocidad, arranque y apagado una vez, choque y alarma.</summary>
    public class SonidosVehiculosT1475Pruebas
    {
        private sealed class SonidosDoble : ISonidos
        {
            public readonly List<string> Sonaron = new List<string>();
            public readonly List<string> Bucles = new List<string>();
            public readonly List<int> Detenidos = new List<int>();
            public readonly List<float> Tonos = new List<float>();
            public void Reproducir(string id) => Sonaron.Add(id);
            public void ReproducirEn(string id, Vector3 posicion) => Sonaron.Add(id);
            public int ReproducirBucle(string id, Transform sigue) { Bucles.Add(id); return Bucles.Count; }
            public void DetenerBucle(int bucle) => Detenidos.Add(bucle);
            public void FijarTono(int bucle, float tono) => Tonos.Add(tono);
            public float Volumen(SeccionSonido seccion) => 1f;
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private SonidosDoble sonidos;
        private GameObject carro;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            sonidos = new SonidosDoble();
            Registro<ISonidos>.Agregar(sonidos);
            carro = new GameObject("CarroPrueba");
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(carro);
            Registros.LimpiarTodos();
        }

        [Test]
        public void Grupo_PorTipo_DaLosIdsDeCadaUno()
        {
            GrupoSonidoVehiculo n = GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Normal);
            Assert.AreEqual("VEH_NORMAL_ARRANQUE", n.Arranque);
            Assert.AreEqual("VEH_NORMAL_RALENTI", n.Ralenti);
            Assert.AreEqual("VEH_NORMAL_APAGAR", n.Apagar);
            Assert.AreEqual("VEH_CLAXON_NORMAL", n.Claxon);
            GrupoSonidoVehiculo m = GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Moto);
            Assert.AreEqual("MOTO_ARRANQUE", m.Arranque);
            Assert.AreEqual("MOTO_RALENTI", m.Ralenti);
            Assert.AreEqual("MOTO_APAGAR", m.Apagar);
            GrupoSonidoVehiculo p = GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Pesado);
            Assert.AreEqual("VEH_PESADO_ARRANQUE", p.Arranque);
            Assert.AreEqual("VEH_PESADO_RALENTI", p.Ralenti);
            Assert.AreEqual("VEH_PESADO_APAGAR", p.Apagar);
            Assert.AreEqual("VEH_CLAXON_CAMION", p.Claxon);
        }

        [Test]
        public void Grupo_MotoGrandeYSuperdeportivo_ReciclanConOtroTono()
        {
            Assert.AreEqual("MOTO_RALENTI", GrupoSonidoVehiculo.Para(TipoVehiculoSonido.MotoGrande).Ralenti);
            Assert.Less(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.MotoGrande).FactorTono, GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Moto).FactorTono);
            Assert.AreEqual("VEH_NORMAL_RALENTI", GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Superdeportivo).Ralenti);
            Assert.Greater(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Superdeportivo).FactorTono, 1f);
            Assert.AreEqual("VEH_ELECTRICO_ARRANQUE", GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Electrico).Arranque);
        }

        [Test]
        public void Detectar_PorMotoMasaYNombre_ElegeElTipo()
        {
            Assert.AreEqual(TipoVehiculoSonido.Moto, GrupoSonidoVehiculo.Detectar(true, 230f, "Moto"));
            Assert.AreEqual(TipoVehiculoSonido.MotoGrande, GrupoSonidoVehiculo.Detectar(true, 400f, "Moto"));
            Assert.AreEqual(TipoVehiculoSonido.Pesado, GrupoSonidoVehiculo.Detectar(false, 5000f, ""));
            Assert.AreEqual(TipoVehiculoSonido.Pesado, GrupoSonidoVehiculo.Detectar(false, 1100f, "Camión de basura"));
            Assert.AreEqual(TipoVehiculoSonido.Superdeportivo, GrupoSonidoVehiculo.Detectar(false, 1100f, "Carro deportivo"));
            Assert.AreEqual(TipoVehiculoSonido.Normal, GrupoSonidoVehiculo.Detectar(false, 1100f, "Sedán viejo"));
        }

        [Test]
        public void Tono_ConLaVelocidad_EsMonotonoYAcotado()
        {
            float anterior = -1f;
            for (float v = -5f; v <= 80f; v += 1f)
            {
                float t = ReglasSonidoVehiculoT1475.TonoObjetivo(v, 40f, 1f);
                Assert.GreaterOrEqual(t, anterior);
                Assert.GreaterOrEqual(t, ReglasSonidoVehiculoT1475.TonoReposo - 1e-5f);
                Assert.LessOrEqual(t, ReglasSonidoVehiculoT1475.TonoMaximo + 1e-5f);
                anterior = t;
            }
            Assert.AreEqual(0.8f, ReglasSonidoVehiculoT1475.TonoObjetivo(0f, 40f, 1f), 1e-5f);
            Assert.AreEqual(1.8f, ReglasSonidoVehiculoT1475.TonoObjetivo(40f, 40f, 1f), 1e-5f);
        }

        [Test]
        public void Suavizar_NoSaltaAlObjetivo()
        {
            float t = ReglasSonidoVehiculoT1475.Suavizar(0.8f, 1.8f, 0.02f, SonidoMotorCarro.TasaSuavizado);
            Assert.Greater(t, 0.8f);
            Assert.Less(t, 1.0f);
        }

        [Test]
        public void Motor_Encender_ArranqueUnaVezYLuegoElBucleDeRalenti()
        {
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Normal), 40f);
            motor.Encender(carro.transform, true);
            motor.Encender(carro.transform, true); // segunda vez: nada
            CollectionAssert.AreEqual(new[] { "VEH_NORMAL_ARRANQUE" }, sonidos.Sonaron);
            Assert.AreEqual(0, sonidos.Bucles.Count);
            motor.Actualizar(0f, 0f, 0.3f);
            Assert.AreEqual(0, sonidos.Bucles.Count);
            motor.Actualizar(0f, 0f, 0.6f);
            motor.Actualizar(0f, 0f, 0.1f);
            CollectionAssert.AreEqual(new[] { "VEH_NORMAL_RALENTI" }, sonidos.Bucles);
        }

        [Test]
        public void Motor_Tono_SoloSeManda_SiCambia()
        {
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Normal), 40f);
            motor.Encender(carro.transform, false);
            motor.Actualizar(0f, 0f, 1f);
            int tras = sonidos.Tonos.Count;
            for (int i = 0; i < 20; i++)
            {
                motor.Actualizar(0f, 0f, 0.02f);
            }
            Assert.AreEqual(tras, sonidos.Tonos.Count, "En reposo el tono no cambia: no se llama de nuevo.");
            for (int i = 0; i < 100; i++)
            {
                motor.Actualizar(40f, 0f, 0.02f);
            }
            for (int i = 1; i < sonidos.Tonos.Count; i++)
            {
                Assert.GreaterOrEqual(sonidos.Tonos[i], sonidos.Tonos[i - 1] - 1e-5f);
            }
            Assert.Greater(motor.TonoActual, 1.5f);
        }

        [Test]
        public void Motor_Apagar_DetieneElBucleYSuenaApagarUnaVez()
        {
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Pesado), 30f);
            motor.Encender(carro.transform, true);
            motor.Actualizar(0f, 0f, 1f);
            motor.Apagar(true);
            motor.Apagar(true);
            Assert.AreEqual(1, sonidos.Detenidos.Count);
            CollectionAssert.AreEqual(new[] { "VEH_PESADO_ARRANQUE", "VEH_PESADO_APAGAR" }, sonidos.Sonaron);
            Assert.IsFalse(motor.Encendido);
        }

        [Test]
        public void Aceleron_PisandoAFondoDesdeLento_ConTiempoMinimo()
        {
            Assert.IsTrue(ReglasSonidoVehiculoT1475.Aceleron(0f, 1f, 1f, 40f, 10f));
            Assert.IsFalse(ReglasSonidoVehiculoT1475.Aceleron(1f, 1f, 1f, 40f, 10f), "El pedal ya estaba a fondo.");
            Assert.IsFalse(ReglasSonidoVehiculoT1475.Aceleron(0f, 1f, 30f, 40f, 10f), "Va rápido.");
            Assert.IsFalse(ReglasSonidoVehiculoT1475.Aceleron(0f, 1f, 1f, 40f, 0.5f), "Muy pronto.");
            SonidoMotorCarro motor = new SonidoMotorCarro(GrupoSonidoVehiculo.Para(TipoVehiculoSonido.Normal), 40f);
            motor.Encender(carro.transform, false);
            motor.Actualizar(0f, 0f, 1f);
            motor.Actualizar(0f, 1f, 0.02f);
            motor.Actualizar(0f, 0f, 0.02f);
            motor.Actualizar(0f, 1f, 0.02f);
            CollectionAssert.AreEqual(new[] { "VEH_ACELERON" }, sonidos.Sonaron);
        }

        [Test]
        public void Choque_SegunLaFuerza_LeveFuerteYCristal()
        {
            Assert.IsEmpty(ReglasSonidoVehiculoT1475.IdsChoque(2f, 4f, 10f));
            CollectionAssert.AreEqual(new[] { "VEH_CHOQUE" }, ReglasSonidoVehiculoT1475.IdsChoque(6f, 4f, 10f));
            CollectionAssert.AreEqual(new[] { "VEH_CHOQUE_FUERTE" }, ReglasSonidoVehiculoT1475.IdsChoque(12f, 4f, 10f));
            CollectionAssert.AreEqual(new[] { "VEH_CHOQUE_FUERTE", "VEH_CRISTAL_ROMPER" }, ReglasSonidoVehiculoT1475.IdsChoque(20f, 4f, 10f));
        }

        [Test]
        public void Tope_GolpeVerticalYRapidez_Suena()
        {
            Assert.IsTrue(ReglasSonidoVehiculoT1475.Tope(0f, 3f, 10f, 5f));
            Assert.IsFalse(ReglasSonidoVehiculoT1475.Tope(0f, 1f, 10f, 5f));
            Assert.IsFalse(ReglasSonidoVehiculoT1475.Tope(0f, 3f, 2f, 5f));
            Assert.IsFalse(ReglasSonidoVehiculoT1475.Tope(0f, 3f, 10f, 0.2f));
        }

        [Test]
        public void Alarma_AlRobar_SuenaUnaVozYSeCortaSola()
        {
            AlarmaCarro alarma = new AlarmaCarro();
            alarma.Iniciar(carro.transform);
            alarma.Iniciar(carro.transform);
            CollectionAssert.AreEqual(new[] { "VEH_ALARMA" }, sonidos.Bucles);
            alarma.Actualizar(AlarmaCarro.SegundosAlarma - 1f);
            Assert.IsTrue(alarma.Suena);
            alarma.Actualizar(2f);
            Assert.IsFalse(alarma.Suena);
            Assert.AreEqual(1, sonidos.Detenidos.Count);
        }
    }
}
