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
        public void RapidezJugador_TroteYCorrerRapido_ConLosClipsNuevos_EntreLosLimites()
        {
            // T-1550 (decisión de Tony del 20-nov (T-1550)): Shift trota a 4.53 (PJ01_Trote) y la velocidad rápida corre a 6.25 (PJ01_CorrerRapido, antes PJ01_Sprint a 7.25 con el sprint a 7.5).
            float trote = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadPJ01Trote, ReglasAnimacion.VelocidadPJ01Trote, ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador);
            Assert.AreEqual(1f, trote, 1e-3f, "A 4.53 m/s el trote va a su velocidad");
            float rapido = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadRapida, ReglasAnimacion.VelocidadPJ01CorrerRapido, ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador);
            Assert.AreEqual(1f, rapido, 1e-3f, "A 6.25 m/s la carrera rápida va a su velocidad");
            float sprint = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadRapida * 1.08f, ReglasAnimacion.VelocidadPJ01CorrerRapido, ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador);
            Assert.That(sprint, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador), "Un poco más rápido (8 %) aún cabe en los límites");
            float lado = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadPJ01Trote, ReglasAnimacion.VelocidadPJ01TroteLado, ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador);
            Assert.That(lado, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador), "De lado a 4.5 m/s el clip de 4.0 va a 1.125");
        }

        [Test]
        public void MezclaCaminarTrote_InterpolaLaVelocidadDelClip_ConElPesoDelArbol()
        {
            Assert.AreEqual(1.6f, ReglasAnimacion.VelocidadClipMezcla(1.6f, 1.6f, 4.5f, 1.6f, 4.5f), 1e-4f);
            Assert.AreEqual(4.5f, ReglasAnimacion.VelocidadClipMezcla(4.5f, 1.6f, 4.5f, 1.6f, 4.5f), 1e-4f);
            Assert.AreEqual(3.05f, ReglasAnimacion.VelocidadClipMezcla(3.05f, 1.6f, 4.5f, 1.6f, 4.5f), 1e-3f, "A mitad del camino el clip avanza lo que el cuerpo: rapidez 1");
            Assert.AreEqual(1.6f, ReglasAnimacion.VelocidadClipMezcla(0.5f, 1.6f, 4.5f, 1.6f, 4.5f), 1e-4f, "Abajo del umbral lento se queda en el clip lento");
            Assert.AreEqual(4.5f, ReglasAnimacion.VelocidadClipMezcla(6f, 1.6f, 4.5f, 1.6f, 4.5f), 1e-4f);
            Assert.AreEqual(2.52f, ReglasAnimacion.VelocidadClipMezcla(3.05f, 1.6f, 4.5f, 1.04f, 4f), 1e-2f, "Hacia atrás mezcla 1.04 y 4.0");
        }

        [Test]
        public void AterrizajeFuerte_SoloDesdeLaVelocidadDeLlegada()
        {
            Assert.IsFalse(ReglasAnimacion.EsAterrizajeFuerte(-5f, 5f));
            Assert.IsFalse(ReglasAnimacion.EsAterrizajeFuerte(0f, 5f));
            Assert.IsTrue(ReglasAnimacion.EsAterrizajeFuerte(-ReglasAnimacion.VelocidadAterrizajeFuerte, 3f));
            Assert.IsTrue(ReglasAnimacion.EsAterrizajeFuerte(-14f, 3f));
            Assert.IsFalse(ReglasAnimacion.EsAterrizajeFuerte(-14f, 0.3f), "Sin caída real no cuenta");
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
        public void OverrideJugador_MapeaLosClipsNuevos_YLosNpcQuedanComoAntes()
        {
            Assert.AreEqual("PJ01_Caminar", Override(RutaOverrideJugador, "Loco_Caminar"));
            Assert.AreEqual("PJ01_Trote", Override(RutaOverrideJugador, ConstructorControladorBase.RolTrote));
            Assert.AreEqual("PJ01_CorrerRapido", Override(RutaOverrideJugador, "Loco_Correr")); // T-1550, decisión de Tony del 20-nov (T-1550)
            Assert.AreEqual("PJ01_Trote_Atras", Override(RutaOverrideJugador, ConstructorControladorBase.RolTroteAtras));
            Assert.AreEqual("PJ01_Trote_Derecha", Override(RutaOverrideJugador, ConstructorControladorBase.RolTroteDer));
            Assert.AreEqual("PJ01_Trote_Izquierda", Override(RutaOverrideJugador, ConstructorControladorBase.RolTroteIzq));
            Assert.AreEqual("PJ01_Salto_Despegue", Override(RutaOverrideJugador, "Loco_SaltoInicio"));
            Assert.AreEqual("PJ01_Salto_Aire", Override(RutaOverrideJugador, "Loco_SaltoAire"));
            Assert.AreEqual("PJ01_Salto_Aterrizaje", Override(RutaOverrideJugador, "Loco_SaltoAterrizaje"));
            Assert.AreEqual("NPC_Caminar_Hombre", Override(RutaOverrideNpc, "Loco_Caminar")); // T-1506: los NPC caminan con su clip
            foreach ((string b, string p) in ConstructorControladorBase.ClipsPorPapel["NPC"])
            {
                if (b == "Loco_Caminar") Assert.AreEqual("NPC_Caminar_Hombre", p);
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
