using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-1515: cobertura (quieto, asomarse, regresar), daño de lado, moto, escalones de concreto y velocidades de la tabla de Blender.</summary>
    public class PendientesPJ01T1515Pruebas
    {
        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static AnimatorState Estado(int capa, string nombre) => Base().layers[capa].stateMachine.states.First(s => s.state.name == nombre).state;

        private static bool Hay(AnimatorState desde, string hacia, string parametro)
        {
            return desde.transitions.Any(t => t.destinationState != null && t.destinationState.name == hacia && t.conditions.Any(c => c.parameter == parametro));
        }

        private static ModelImporterClipAnimation ClipImportado(string clip)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(ConstructorControladorBase.CarpetaClipsPJ01 + "/" + clip + ".fbx");
            return imp.clipAnimations[0];
        }

        private static List<KeyValuePair<AnimationClip, AnimationClip>> OverridesJugador()
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            return pares;
        }

        // ---------- Cobertura ----------

        [TestCase("CoberturaMantener", "PJ01_Cobertura_Quieto")]
        [TestCase("CoberturaAsomarse", "PJ01_Cobertura_Asomarse")]
        [TestCase("CoberturaRegresar", "PJ01_Cobertura_Regresar")]
        public void Cobertura_TieneSusEstadosConLosClipsDeBlender(string estado, string clip)
        {
            Assert.AreEqual(clip, Estado(0, estado).motion.name);
        }

        [Test]
        public void Cobertura_AlApuntarSeAsoma_YAlSoltarRegresaAQuieto()
        {
            Assert.IsTrue(Hay(Estado(0, "CoberturaMantener"), "CoberturaAsomarse", ReglasVisual.Apuntando));
            Assert.IsTrue(Hay(Estado(0, "CoberturaAsomarse"), "CoberturaRegresar", ReglasVisual.Apuntando));
            Assert.IsTrue(Estado(0, "CoberturaRegresar").transitions.Any(t => t.destinationState.name == "CoberturaMantener" && t.hasExitTime));
        }

        [Test]
        public void Cobertura_SoltarLaCubiertaSaleDesdeAsomarseYRegresar()
        {
            foreach (string estado in new[] { "CoberturaAsomarse", "CoberturaRegresar" })
            {
                Assert.IsTrue(Hay(Estado(0, estado), "Idle", ReglasVisual.Cubierto), estado);
            }
        }

        [TestCase("PJ01_Cobertura_Quieto", true)]
        [TestCase("PJ01_Cobertura_Asomarse", false)]
        [TestCase("PJ01_Cobertura_Regresar", false)]
        public void Cobertura_ImportadosConElBucleDeLaTabla(string clip, bool bucle)
        {
            Assert.AreEqual(bucle, ClipImportado(clip).loopTime, clip);
        }

        // ---------- Daño de lado ----------

        [Test]
        public void Dano_TieneLosEstadosDeIzquierdaYDerecha_ConSusClips()
        {
            Assert.AreEqual("PJ01_Dano_Izquierda", Estado(2, "DanoIzquierda").motion.name);
            Assert.AreEqual("PJ01_Dano_Derecha", Estado(2, "DanoDerecha").motion.name);
            CollectionAssert.AreEquivalent(ReglasAnimacion.EstadosDano, Base().layers[2].stateMachine.states.Select(s => s.state.name));
        }

        [TestCase("DanoIzquierda", (int)DanoVisual.Izquierda)]
        [TestCase("DanoDerecha", (int)DanoVisual.Derecha)]
        public void Dano_EntraConElDanoTipoQueLeToca(string estado, int tipo)
        {
            AnimatorCondition c = Base().layers[2].stateMachine.anyStateTransitions
                .First(t => t.destinationState.name == estado).conditions.First(x => x.parameter == ReglasAnimacion.DanoTipo);
            Assert.AreEqual(tipo, (int)c.threshold);
        }

        [TestCase("PJ01_Dano_Izquierda")]
        [TestCase("PJ01_Dano_Derecha")]
        public void Dano_LosClipsLateralesSonAditivosConElPrimerCuadroDeReferencia(string clip)
        {
            ModelImporterClipAnimation c = ClipImportado(clip);
            Assert.IsTrue(c.hasAdditiveReferencePose);
            Assert.AreEqual(c.firstFrame, c.additiveReferencePoseFrame, 0.001f);
        }

        [Test]
        public void LadoDelGolpe_UnaBalaQueVaALaDerechaLePegaPorLaIzquierda()
        {
            Assert.AreEqual(-1, ReglasAnimacion.LadoDelGolpe(Vector3.right, Vector3.forward));
            Assert.AreEqual(1, ReglasAnimacion.LadoDelGolpe(Vector3.left, Vector3.forward));
            Assert.AreEqual(0, ReglasAnimacion.LadoDelGolpe(Vector3.back, Vector3.forward));
            Assert.AreEqual(0, ReglasAnimacion.LadoDelGolpe(Vector3.forward, Vector3.forward));
            Assert.AreEqual(0, ReglasAnimacion.LadoDelGolpe(Vector3.zero, Vector3.forward));
        }

        [Test]
        public void LadoDelGolpe_SigueElFrenteDelJugador()
        {
            // Mirando a la derecha (+X), una bala que va hacia -Z (su derecha) le pega por la izquierda.
            Assert.AreEqual(-1, ReglasAnimacion.LadoDelGolpe(Vector3.back, Vector3.right));
        }

        [Test]
        public void ElegirDano_ConLateral_DiceIzquierdaDerechaYSigueIgualEnLoDemas()
        {
            Assert.AreEqual(DanoVisual.Izquierda, ReglasAnimacion.ElegirDano(Vector3.right, Vector3.forward, false, true));
            Assert.AreEqual(DanoVisual.Derecha, ReglasAnimacion.ElegirDano(Vector3.left, Vector3.forward, false, true));
            Assert.AreEqual(DanoVisual.Frente, ReglasAnimacion.ElegirDano(Vector3.back, Vector3.forward, false, true));
            Assert.AreEqual(DanoVisual.Espalda, ReglasAnimacion.ElegirDano(Vector3.forward, Vector3.forward, false, true));
            Assert.AreEqual(DanoVisual.Fuerte, ReglasAnimacion.ElegirDano(Vector3.right, Vector3.forward, true, true));
            Assert.AreEqual(DanoVisual.Frente, ReglasAnimacion.ElegirDano(Vector3.right, Vector3.forward, false, false), "sin lateral (NPC) el golpe de lado sigue siendo de frente");
        }

        // ---------- Moto y carro ----------

        [Test]
        public void Moto_ManejarUsaElClipNuevoDeBlender_YPoseMotoLoBusca()
        {
            Assert.AreEqual("PJ01_Manejar_Moto", Estado(0, "MotoManejar").motion.name);
            Assert.AreEqual("PJ01_Manejar_Moto", PoseMoto.ClipManejar);
        }

        [Test]
        public void Carro_ManejarDelJugadorEsElClipNuevoDeBlender()
        {
            Assert.AreEqual("PJ01_Manejar_Carro", OverridesJugador().First(p => p.Key.name == "Carro_Manejar_Bucle").Value.name);
        }

        // ---------- Escalones ----------

        [TestCase("EscalonesSubir", "PJ01_Escaleras_Subir")]
        [TestCase("EscalonesBajar", "PJ01_Escaleras_Bajar")]
        public void Escalones_TienenSuEstadoConRapidezPorParametro(string estado, string clip)
        {
            AnimatorState s = Estado(0, estado);
            Assert.AreEqual(clip, s.motion.name);
            Assert.IsTrue(s.speedParameterActive);
            Assert.AreEqual(ReglasAnimacion.RapidezEscalones, s.speedParameter);
        }

        [TestCase("Caminar", "EscalonesSubir", 1)]
        [TestCase("Caminar", "EscalonesBajar", 2)]
        [TestCase("Correr", "EscalonesSubir", 1)]
        public void Escalones_SeEntraDesdeLaLocomocionConElParametroEscalones(string desde, string hacia, int valor)
        {
            AnimatorStateTransition t = Estado(0, desde).transitions.First(x => x.destinationState != null && x.destinationState.name == hacia);
            Assert.AreEqual(valor, (int)t.conditions.First(c => c.parameter == ReglasAnimacion.Escalones).threshold);
        }

        [Test]
        public void Escalones_LaEscaleraDeManoSigueConSusClips()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionEscalera>("Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset");
            Assert.IsNotNull(config);
            Assert.AreEqual("PJ01_Escalera_Subir", config.Subir.name);
            Assert.AreEqual("PJ01_Escalera_Bajar", config.Bajar.name);
        }

        [Test]
        public void Escalones_ParametrosEnElControlador()
        {
            Assert.IsTrue(Base().parameters.Any(p => p.name == ReglasAnimacion.Escalones && p.type == AnimatorControllerParameterType.Int));
            Assert.IsTrue(Base().parameters.Any(p => p.name == ReglasAnimacion.RapidezEscalones && p.type == AnimatorControllerParameterType.Float));
        }

        [Test]
        public void ReglaEscalones_ClasificaSubirBajarRampaYMuro()
        {
            Assert.AreEqual(1, ReglaEscalones.Clasificar(0f, 0.18f, 1f, 1f));
            Assert.AreEqual(-1, ReglaEscalones.Clasificar(0.36f, 0.18f, 1f, 1f));
            Assert.AreEqual(0, ReglaEscalones.Clasificar(0f, 0.02f, 1f, 1f), "piso plano");
            Assert.AreEqual(0, ReglaEscalones.Clasificar(0f, 0.18f, 1f, 0.9f), "rampa: normal inclinada");
            Assert.AreEqual(0, ReglaEscalones.Clasificar(0f, 0.8f, 1f, 1f), "muro o repisa alta");
        }

        [Test]
        public void SeguidorEscalones_EntraTrasUnRatoYSaleTrasOtro()
        {
            var s = new SeguidorEscalones();
            Assert.AreEqual(0, s.Avanzar(1, 0.1f), "un cuadro suelto no basta");
            Assert.AreEqual(1, s.Avanzar(1, 0.15f));
            Assert.AreEqual(1, s.Avanzar(0, 0.2f), "aún no sale");
            Assert.AreEqual(0, s.Avanzar(0, 0.2f));
            Assert.AreEqual(0, s.Avanzar(1, 0.1f));
            Assert.AreEqual(0, s.Avanzar(0, 0.05f));
            Assert.AreEqual(0, s.Avanzar(1, 0.1f), "la banqueta suelta reinicia la cuenta");
        }

        [Test]
        public void RapidezEscalones_SigueLaVelocidadYTieneTope()
        {
            Assert.AreEqual(ReglasAnimacion.RapidezMinimaEscalones, ReglasAnimacion.RapidezEscalonesPara(0.1f), 0.001f);
            Assert.AreEqual(ReglasAnimacion.RapidezMaximaEscalones, ReglasAnimacion.RapidezEscalonesPara(9f), 0.001f);
            Assert.AreEqual(2f, ReglasAnimacion.RapidezEscalonesPara(2f * ReglasAnimacion.VelocidadClipEscalones), 0.001f);
        }

        // ---------- Velocidades de la tabla de Blender (BITACORA 19-nov A) ----------

        [Test]
        public void Velocidades_IgualanLaTablaDeBlender()
        {
            // T-1550 (decisión de Tony del 20-nov (T-1550)): caminar 1.62, trote 4.53 y la carrera rápida 6.25 (reemplaza al sprint de 7.25).
            Assert.AreEqual(1.62f, ReglasAnimacion.VelocidadPJ01Caminar, 0.001f);
            Assert.AreEqual(4.53f, ReglasAnimacion.VelocidadPJ01Trote, 0.001f);
            Assert.AreEqual(6.25f, ReglasAnimacion.VelocidadPJ01CorrerRapido, 0.001f);
            Assert.AreEqual(1.57f, ReglasAnimacion.VelocidadPJ01CaminarApuntandoDerecha, 0.001f);
            Assert.AreEqual(4f, ReglasAnimacion.VelocidadPJ01TroteLado, 0.001f);
            Assert.AreEqual(4f, ReglasAnimacion.VelocidadPJ01TroteAtras, 0.001f);
        }

        [Test]
        public void Velocidades_ElOverrideDelJugadorUsaLosClipsDeLaTabla()
        {
            var pares = OverridesJugador();
            string Clip(string baseNombre) => pares.First(p => p.Key.name == baseNombre).Value.name;
            Assert.AreEqual("PJ01_Caminar", Clip("Loco_Caminar"));
            Assert.AreEqual("PJ01_Trote", Clip(ConstructorControladorBase.RolTrote));
            Assert.AreEqual("PJ01_CorrerRapido", Clip("Loco_Correr")); // T-1550, decisión de Tony del 20-nov (T-1550)
            Assert.AreEqual("PJ01_Trote_Atras", Clip(ConstructorControladorBase.RolTroteAtras));
            Assert.AreEqual("PJ01_Trote_Derecha", Clip(ConstructorControladorBase.RolTroteDer));
            Assert.AreEqual("PJ01_Trote_Izquierda", Clip(ConstructorControladorBase.RolTroteIzq));
        }

        [Test]
        public void Velocidades_LosUmbralesDelArbolDeCaminarSon1_6y4_5()
        {
            var umbrales = new List<float>();
            void Recorrer(BlendTree a)
            {
                if (a.blendType == BlendTreeType.Simple1D && a.blendParameter == ReglasVisual.Velocidad)
                {
                    umbrales.AddRange(a.children.Select(h => h.threshold));
                }
                foreach (ChildMotion h in a.children)
                {
                    if (h.motion is BlendTree hijo) { Recorrer(hijo); }
                }
            }
            Recorrer((BlendTree)Estado(0, "Caminar").motion);
            Assert.IsNotEmpty(umbrales, "hay un árbol 1D por Velocidad");
            Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Caminar, umbrales.Min(), 0.001f);
            Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Trote, umbrales.Max(), 0.001f);
        }
    }
}
