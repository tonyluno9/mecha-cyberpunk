using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-1484: rapidez del clip por dirección, IK solo de desniveles, inclinación en giros, giro suave y el trote en Caminar del jugador.</summary>
    public class LocomocionT1484Pruebas
    {
        private const string RutaOverrideJugador = "Assets/Arte/Personajes/Animaciones/Overrides/Override_Jugador.overrideController";
        private const string RutaOverrideNpc = "Assets/Arte/Personajes/Animaciones/Overrides/Override_NPC.overrideController";

        [Test]
        public void ClipDireccional_AdelanteAtrasYLado_DaLaVelocidadDeCadaClip()
        {
            Assert.AreEqual(4.25f, ReglasAnimacion.VelocidadClipDireccional(0f, 1f, 4.25f, 1.04f, 1.61f), 1e-4f);
            Assert.AreEqual(1.04f, ReglasAnimacion.VelocidadClipDireccional(0f, -1f, 4.25f, 1.04f, 1.61f), 1e-4f);
            Assert.AreEqual(1.61f, ReglasAnimacion.VelocidadClipDireccional(1f, 0f, 4.25f, 1.04f, 1.61f), 1e-4f);
            Assert.AreEqual(1.61f, ReglasAnimacion.VelocidadClipDireccional(-1f, 0f, 4.25f, 1.04f, 1.61f), 1e-4f);
            float diagonal = ReglasAnimacion.VelocidadClipDireccional(0.707f, 0.707f, 4.25f, 1.04f, 1.61f);
            Assert.Greater(diagonal, 1.61f);
            Assert.Less(diagonal, 4.25f);
        }

        [Test]
        public void ClipDireccional_SinDireccionOSinClipsDeLado_UsaElDeAdelante()
        {
            Assert.AreEqual(4.25f, ReglasAnimacion.VelocidadClipDireccional(0f, 0f, 4.25f, 1.04f, 1.61f), 1e-4f);
            Assert.AreEqual(2f, ReglasAnimacion.VelocidadClipDireccional(1f, 0f, 2f, 0f, 0f), 1e-4f, "Los NPC sin velocidades por dirección quedan como antes");
        }

        [Test]
        public void RapidezJugador_A4y5ConElTrote_CasiNormal_YCorriendoNoPasaDe1y6()
        {
            float caminar = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadAndar, ReglasAnimacion.VelocidadPJ01Correr, ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador);
            Assert.AreEqual(4.5f / 4.25f, caminar, 1e-3f);
            float correr = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadCarrera, ReglasAnimacion.VelocidadPJ01Correr, ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador);
            Assert.AreEqual(ReglasAnimacion.RapidezMaximaJugador, correr, 1e-3f);
            Assert.Less(Mathf.Abs(correr * ReglasAnimacion.VelocidadPJ01Correr - 7.5f) / 7.5f, 0.1f, "Corriendo los pies patinan menos de 10 %");
        }

        [Test]
        public void Desnivel_EnPlanoEsCero_EnEscalonSeRecorta()
        {
            Assert.AreEqual(0f, ReglasAnimacion.Desnivel(0.02f, 0f, 0.35f), 1e-5f, "La piel del CharacterController no hunde los pies");
            Assert.AreEqual(0.2f, ReglasAnimacion.Desnivel(0.2f, 0f, 0.35f), 1e-5f);
            Assert.AreEqual(-0.35f, ReglasAnimacion.Desnivel(-0.6f, 0f, 0.35f), 1e-5f);
        }

        [Test]
        public void BajadaCadera_SoloBajaConElPieMasBajo()
        {
            Assert.AreEqual(-0.18f, ReglasAnimacion.BajadaCadera(0.1f, -0.18f), 1e-5f);
            Assert.AreEqual(0f, ReglasAnimacion.BajadaCadera(0.1f, 0.2f), 1e-5f, "Si los dos pies suben la cadera no se mueve");
        }

        [Test]
        public void Inclinacion_HaciaDentroDeLaCurva_ConTopeYNadaQuieto()
        {
            Assert.AreEqual(0f, ReglasAnimacion.InclinacionEnGiro(0f, 180f), 1e-5f);
            float derecha = ReglasAnimacion.InclinacionEnGiro(4.5f, 90f);
            Assert.Greater(derecha, 1f, "Girando a la derecha se inclina a la derecha");
            Assert.Less(ReglasAnimacion.InclinacionEnGiro(4.5f, -90f), -1f);
            Assert.AreEqual(ReglasAnimacion.InclinacionMaxima, ReglasAnimacion.InclinacionEnGiro(7.5f, 720f), 1e-4f);
        }

        [Test]
        public void GirarSuave_LlegaSinPasarseNiExcederElMaximo()
        {
            float angulo = 0f;
            const float dt = 1f / 60f;
            for (int i = 0; i < 60; i++)
            {
                float siguiente = ReglasAnimacion.GirarSuave(angulo, 90f, dt, 720f, 14f);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(angulo, siguiente)), 720f * dt + 1e-3f);
                Assert.LessOrEqual(siguiente, 90f + 1e-3f, "No se pasa del objetivo");
                angulo = siguiente;
            }
            Assert.AreEqual(90f, angulo, 1e-3f, "En un segundo llega");
            float cruce = ReglasAnimacion.GirarSuave(170f, -170f, dt, 720f, 14f);
            Assert.Greater(cruce, 170f, "De 170 a -170 cruza 180 por el lado corto (sube)");
        }

        [Test]
        public void OverrideJugador_CaminarEsElTrote_YLosNpcSiguenCaminando()
        {
            Assert.AreEqual("PJ01_Correr_SinImplante", Override(RutaOverrideJugador, "Loco_Caminar"));
            Assert.AreEqual("PJ01_Correr_ConImplante", Override(RutaOverrideJugador, "Loco_Caminar_ConImplante"));
            Assert.AreEqual("PJ01_Caminar_SinImplante", Override(RutaOverrideNpc, "Loco_Caminar"));
            foreach ((string b, string p) in ConstructorControladorBase.ClipsPorPapel["NPC"])
            {
                if (b == "Loco_Caminar") Assert.AreEqual("PJ01_Caminar_SinImplante", p);
            }
        }

        private static string Override(string ruta, string clipBase)
        {
            AnimatorOverrideController o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
            Assert.IsNotNull(o, ruta);
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            foreach (KeyValuePair<AnimationClip, AnimationClip> par in pares)
            {
                if (par.Key != null && par.Key.name == clipBase)
                {
                    return par.Value != null ? par.Value.name : par.Key.name;
                }
            }
            Assert.Fail(ruta + " no tiene " + clipBase);
            return null;
        }
    }
}
