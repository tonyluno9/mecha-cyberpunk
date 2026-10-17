using MechaCyberpunk.UI;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-134: cuándo se acaba una ayuda de controles, qué se recuerda en la sesión y los textos de ConfigAyudas.</summary>
    public class ProgresoAyudaPruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";
        private const float Dt = 0.5f;

        [SetUp]
        public void Preparar() => AyudasVistas.Olvidar();

        [TearDown]
        public void Limpiar() => AyudasVistas.Olvidar();

        [Test]
        public void Terminada_SinHacerNada_SoloAlPasarElTiempoMaximo()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(12f, 3f);
            for (int i = 0; i < 23; i++)
            {
                progreso.Avanzar(Dt, false);
            }
            Assert.IsFalse(progreso.Terminada, "11.5 s de 12 s");
            progreso.Avanzar(Dt, false);
            Assert.IsTrue(progreso.Terminada, "12 s");
        }

        [Test]
        public void Terminada_ConActividadSuficiente_AntesDelTiempoMaximo()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(12f, 3f);
            for (int i = 0; i < 5; i++)
            {
                progreso.Avanzar(Dt, true);
            }
            Assert.IsFalse(progreso.Terminada, "2.5 s caminando");
            progreso.Avanzar(Dt, true);
            Assert.IsTrue(progreso.Terminada, "3 s caminando");
            Assert.Less(progreso.Transcurrido, 12f);
        }

        [Test]
        public void Terminada_ActividadPartida_SumaLosTramos()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(12f, 3f);
            for (int i = 0; i < 3; i++)
            {
                progreso.Avanzar(Dt, true);
            }
            for (int i = 0; i < 4; i++)
            {
                progreso.Avanzar(Dt, false);
            }
            Assert.IsFalse(progreso.Terminada);
            for (int i = 0; i < 3; i++)
            {
                progreso.Avanzar(Dt, true);
            }
            Assert.IsTrue(progreso.Terminada, "1.5 s + 1.5 s");
        }

        [Test]
        public void Terminada_PeleaConGolpesYGuardia_PideLasDosCosas()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(20f, 0f, 3, 1);
            progreso.RegistrarGolpe();
            progreso.RegistrarGolpe();
            progreso.RegistrarGolpe();
            progreso.Avanzar(Dt, false);
            Assert.IsFalse(progreso.Terminada, "Faltó subir la guardia");
            progreso.RegistrarGuardia();
            Assert.IsTrue(progreso.Terminada);
        }

        [Test]
        public void Terminada_PeleaSoloConGuardia_NoBasta()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(20f, 0f, 3, 1);
            progreso.RegistrarGuardia();
            progreso.RegistrarGolpe();
            progreso.RegistrarGolpe();
            Assert.IsFalse(progreso.Terminada, "Solo 2 golpes");
        }

        [Test]
        public void Terminada_PeleaSinHacerNada_A20Segundos()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(20f, 0f, 3, 1);
            progreso.Avanzar(19.9f, false);
            Assert.IsFalse(progreso.Terminada);
            progreso.Avanzar(0.2f, false);
            Assert.IsTrue(progreso.Terminada);
        }

        [Test]
        public void Terminada_SinRequisitos_NoSeCumpleAlInstante()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(12f);
            Assert.IsFalse(progreso.Terminada, "Sin requisitos solo cuenta el tiempo");
            progreso.Avanzar(12f, false);
            Assert.IsTrue(progreso.Terminada);
        }

        [Test]
        public void Completar_DaLaAyudaPorCumplida()
        {
            ProgresoAyuda progreso = new ProgresoAyuda(12f);
            progreso.Completar();
            Assert.IsTrue(progreso.Terminada);
        }

        [Test]
        public void AyudasVistas_MarcarYOlvidar_RecuerdaPorContexto()
        {
            Assert.IsFalse(AyudasVistas.YaVista(ContextoAyuda.APie));
            AyudasVistas.Marcar(ContextoAyuda.APie);
            Assert.IsTrue(AyudasVistas.YaVista(ContextoAyuda.APie));
            Assert.IsFalse(AyudasVistas.YaVista(ContextoAyuda.Carro), "Cada contexto es independiente");
            AyudasVistas.Olvidar();
            Assert.IsFalse(AyudasVistas.YaVista(ContextoAyuda.APie));
        }

        [Test]
        public void ConfigAyudas_Base_TieneLasLineasYLosTiemposDeLaTarea()
        {
            ConfigAyudas config = AssetDatabase.LoadAssetAtPath<ConfigAyudas>(RutaConfig);
            Assert.IsNotNull(config, "Falta ConfigAyudas_Base.asset");
            Assert.AreEqual("WASD caminar · SHIFT correr · ESPACIO saltar · E usar", config.Texto(ContextoAyuda.APie));
            Assert.AreEqual("FLECHAS manejar · E bajar (detenido)", config.Texto(ContextoAyuda.Carro));
            Assert.AreEqual("CLIC IZQ golpe · CLIC DER bloquear · justo antes del golpe = bloqueo perfecto · WASD mover",
                config.Texto(ContextoAyuda.MechaPelea));
            Assert.AreEqual("WASD mover · RATÓN mirar · E bajar (quieto)", config.Texto(ContextoAyuda.MechaCalle));
            Assert.AreEqual("M · llamar a tu mecha", config.Texto(ContextoAyuda.Llamar));
            Assert.AreEqual(12f, config.SegundosMaximos);
            Assert.AreEqual(20f, config.SegundosMaximosPelea);
            Assert.AreEqual(3f, config.SegundosCaminando);
            Assert.AreEqual(3f, config.SegundosManejando);
            Assert.AreEqual(3, config.GolpesNecesarios);
            Assert.AreEqual(1, config.GuardiasNecesarias);
        }
    }
}
