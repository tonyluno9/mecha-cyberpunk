using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-1505: reglas de los pasos por evento de animación, eventos y bucle de los clips nuevos, caminar de verdad y aterrizaje fuerte.</summary>
    public class PasosAnimadosT1505Pruebas
    {
        [Test]
        public void IndiceDominante_ElClipConMasPeso_YNingunoSiTodosSonCasiCero()
        {
            Assert.AreEqual(1, ReglaPasosAnimados.IndiceDominante(new[] { 0.3f, 0.7f }));
            Assert.AreEqual(0, ReglaPasosAnimados.IndiceDominante(new[] { 0.5f, 0.5f }), "Con empate gana el primero: un solo paso");
            Assert.AreEqual(-1, ReglaPasosAnimados.IndiceDominante(new[] { 0f, 0.005f }));
            Assert.AreEqual(-1, ReglaPasosAnimados.IndiceDominante(new float[0]));
        }

        [Test]
        public void DebeSonar_SoloElClipDominante_NoLosDosMezclados()
        {
            float[] pesos = { 0.4f, 0.6f, 0f };
            Assert.IsFalse(ReglaPasosAnimados.DebeSonar(0, pesos));
            Assert.IsTrue(ReglaPasosAnimados.DebeSonar(1, pesos));
            Assert.IsFalse(ReglaPasosAnimados.DebeSonar(2, pesos));
            Assert.IsFalse(ReglaPasosAnimados.DebeSonar(-1, pesos), "Un evento de un clip que ya no está activo no suena");
        }

        [Test]
        public void PesoEnTransicion_EscalaPorElAvance_ElQueSaleYElQueEntra()
        {
            Assert.AreEqual(0.8f, ReglaPasosAnimados.PesoEnTransicion(1f, false, 0.2f), 1e-5f);
            Assert.AreEqual(0.2f, ReglaPasosAnimados.PesoEnTransicion(1f, true, 0.2f), 1e-5f);
            // Idle sale (sin pisadas) y Trote entra: a mitad de la mezcla el que entra empata y el que sale pierde por orden; a 0.6 el que entra domina.
            float[] pesos = { ReglaPasosAnimados.PesoEnTransicion(1f, false, 0.6f), ReglaPasosAnimados.PesoEnTransicion(1f, true, 0.6f) };
            Assert.AreEqual(1, ReglaPasosAnimados.IndiceDominante(pesos));
        }

        [Test]
        public void TienePasos_SoloConElEventoDePaso()
        {
            Assert.IsFalse(ReglaPasosAnimados.TienePasos((AnimationEvent[])null));
            Assert.IsFalse(ReglaPasosAnimados.TienePasos(new AnimationEvent[0]));
            Assert.IsFalse(ReglaPasosAnimados.TienePasos(new[] { new AnimationEvent { functionName = "AlSacarPistola" } }));
            Assert.IsTrue(ReglaPasosAnimados.TienePasos(new[] { new AnimationEvent { functionName = ReglaPasosAnimados.FuncionPaso } }));
        }

        [Test]
        public void EventosPisadas_NombreDeFuncionIgualAlDelReceptor_YElReceptorLaTiene()
        {
            Assert.AreEqual(ReglaPasosAnimados.FuncionPaso, ImportadorClipsPJ01.FuncionPasoPJ01);
            Assert.IsNotNull(typeof(ReceptorPasosPJ01).GetMethod(ReglaPasosAnimados.FuncionPaso));
        }

        [TestCase("Caminar", 0f, 0.5f)]
        [TestCase("Trote", 0f, 0.5f)]
        [TestCase("Sprint", 0f, 0.5f)]
        [TestCase("Trote_Izquierda", 0f, 0.5f)]
        [TestCase("Trote_Derecha", 0f, 0.5f)]
        [TestCase("Trote_Atras", 0f, 0.5f)]
        public void EventosPisadas_LosClipsDeLocomocion_LlevanUnaPisadaPorPie(string clip, float izquierdo, float derecho)
        {
            AnimationEvent[] e = ImportadorClipsPJ01.EventosPisadas(clip);
            Assert.AreEqual(2, e.Length);
            Assert.AreEqual(ReglaPasosAnimados.FuncionPaso, e[0].functionName);
            Assert.AreEqual(ReglaPasosAnimados.PieIzquierdo, e[0].intParameter);
            Assert.AreEqual(ReglaPasosAnimados.PieDerecho, e[1].intParameter);
            Assert.AreEqual(Mathf.Max(ImportadorClipsPJ01.FraccionMinimaEvento, izquierdo), e[0].time, 1e-4f, "El pie izquierdo cae en el fotograma 1");
            Assert.AreEqual(derecho, e[1].time, 1e-4f, "El derecho a mitad del ciclo (bitácora: caminar 16 de 31, trote 11 de 21, sprint 10 de 19)");
        }

        [Test]
        public void EventosPisadas_OtrosClips_NoLlevan()
        {
            Assert.IsEmpty(ImportadorClipsPJ01.EventosPisadas("Loco_Idle"));
            Assert.IsEmpty(ImportadorClipsPJ01.EventosPisadas("Salto_Aire"));
            Assert.IsEmpty(ImportadorClipsPJ01.EventosPisadas("Escaleras_Subir"));
        }

        [TestCase("PJ01_Caminar", true)]
        [TestCase("PJ01_Trote", true)]
        [TestCase("PJ01_Sprint", true)]
        [TestCase("PJ01_CorrerRapido", true)] // T-1550
        [TestCase("PJ01_Trote_Izquierda", true)]
        [TestCase("PJ01_Trote_Derecha", true)]
        [TestCase("PJ01_Trote_Atras", true)]
        [TestCase("PJ01_Escaleras_Subir", true)]
        [TestCase("PJ01_Escaleras_Bajar", true)]
        [TestCase("PJ01_Salto_Aire", false)] // T-1550: decisión de Tony del 20-nov, SaltoAire ya no hace ciclo (se queda en la pose de bajada)
        [TestCase("PJ01_Salto_Despegue", false)]
        [TestCase("PJ01_Salto_Aterrizaje", false)]
        [TestCase("PJ01_Aterrizaje_Fuerte", false)]
        public void Bucle_DeLosClipsNuevos_SoloLosCiclicos(string clip, bool bucle)
        {
            Assert.AreEqual(bucle, ImportadorClipsPJ01.TieneBucle(ImportadorClipsPJ01.NombreBase(clip)));
        }

        [Test]
        public void CaminarLento_AlternaConCadaPulsacion_YBajaLaVelocidadSinPasarDeLaNormal()
        {
            Assert.IsTrue(ReglaCaminarLento.Alternar(false, true));
            Assert.IsFalse(ReglaCaminarLento.Alternar(true, true));
            Assert.IsTrue(ReglaCaminarLento.Alternar(true, false), "Sin pulsar se queda como estaba");
            // T-1550 (decisión de Tony del 20-nov (T-1550)): la velocidad de caminar de verdad es la del clip PJ01_Caminar (1.62, antes 1.6).
            Assert.AreEqual(ReglaCaminarLento.Velocidad, ReglaCaminarLento.VelocidadCaminar(true, 4.5f), 1e-4f);
            Assert.AreEqual(4.5f, ReglaCaminarLento.VelocidadCaminar(false, 4.5f), 1e-4f);
            Assert.AreEqual(1.2f, ReglaCaminarLento.VelocidadCaminar(true, 1.2f), 1e-4f, "Nunca más rápido que caminar normal");
            Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Caminar, ReglaCaminarLento.Velocidad, 1e-4f, "Es la velocidad del clip PJ01_Caminar");
            Assert.AreEqual("<Keyboard>/capsLock", ReglaCaminarLento.TeclaCaminar);
        }

        [Test]
        public void Bloqueo_ElAterrizajeFuerte_BloqueaElMovimiento()
        {
            Assert.IsTrue(ReglaBloqueoAnimacion.EstadoBloquea("AterrizajeFuerte"));
            Assert.IsTrue(ReglaBloqueoAnimacion.HashBloquea(Animator.StringToHash("AterrizajeFuerte")));
            CollectionAssert.Contains(ReglasAnimacion.EstadosBase, "AterrizajeFuerte");
            CollectionAssert.Contains(ReglasAnimacion.ParametrosTrigger, ReglasAnimacion.AterrizajeFuerte);
        }

        [TestCase("PJ01_Trote", "AlPasoPJ01")]
        [TestCase("PJ01_Sprint", "AlPasoPJ01")]
        [TestCase("PJ01_Caminar", "AlPasoPJ01")]
        [TestCase("PJ01_CorrerRapido", "AlPasoPJ01")] // T-1550 (decisión de Tony del 20-nov (T-1550))
        public void ClipImportado_LlevaLosEventosDePisada(string nombre, string funcion)
        {
            AnimationClip clip = MechaCyberpunk.Personaje.Visual.Editor.ConstructorControladorBase.Clip(nombre);
            Assert.IsNotNull(clip, nombre);
            Assert.AreEqual(2, clip.events.Length, nombre + " lleva dos pisadas");
            Assert.AreEqual(funcion, clip.events[0].functionName);
            Assert.IsTrue(clip.isLooping, nombre + " es bucle");
            Assert.IsTrue(ReglaPasosAnimados.TienePasos(clip));
        }

        [Test]
        public void ClipImportado_SaltoDespegueNoEsBucle_YSaltoAireSi()
        {
            Assert.IsFalse(MechaCyberpunk.Personaje.Visual.Editor.ConstructorControladorBase.Clip("PJ01_Salto_Despegue").isLooping);
            Assert.IsFalse(MechaCyberpunk.Personaje.Visual.Editor.ConstructorControladorBase.Clip("PJ01_Salto_Aire").isLooping); // T-1550: decisión de Tony del 20-nov, ya no hace ciclo
            Assert.IsFalse(MechaCyberpunk.Personaje.Visual.Editor.ConstructorControladorBase.Clip("PJ01_Aterrizaje_Fuerte").isLooping);
        }

        [Test]
        [Ignore("T-1506: la escalera de mano no usa PJ01_Escaleras_* (son de escalones); se queda con PJ01_Escalera_*")]
        public void Escaleras_ConfigUsaLosClipsNuevos()
        {
            var config = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigAnimacionEscalera>("Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset");
            Assert.IsNotNull(config);
            Assert.AreEqual("PJ01_Escaleras_Subir", config.Subir.name);
            Assert.AreEqual("PJ01_Escaleras_Bajar", config.Bajar.name);
            Assert.AreEqual(0.36f, config.MetrosPorCiclo, 1e-4f);
        }
    }
}
