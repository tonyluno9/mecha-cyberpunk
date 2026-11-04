using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-562: pies sin patinar, variación de idle de NPC, temporizador de mirada, levantarse, pose de moto y cobertura.</summary>
    public class SinClipT562Pruebas
    {
        private const float VelocidadCaminarJugador = 4.5f;
        private const float VelocidadCorrerJugador = 7.5f;

        [Test]
        public void Locomocion_LosClipsMedidosCoincidenConLasConstantes()
        {
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminar, MedicionClips.Medir("Loco_Caminar"), ReglasAnimacion.VelocidadClipCaminar * 0.1f);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCorrer, MedicionClips.Medir("Loco_Correr"), ReglasAnimacion.VelocidadClipCorrer * 0.1f);
        }

        [Test]
        public void Locomocion_ElJugadorACuatroYMedioYaCorre_SinTopeDeRapidez()
        {
            Assert.Greater(VelocidadCaminarJugador, ReglasAnimacion.UmbralCorrer, "a 4.5 m/s ya mezcla hacia correr");
            foreach (float v in new[] { VelocidadCaminarJugador, VelocidadCorrerJugador })
            {
                float sinTope = v / ReglasAnimacion.VelocidadClipCorrer;
                float escala = ReglasAnimacion.EscalaVelocidad(v, ReglasAnimacion.VelocidadClipCorrer, 0.6f, ReglasAnimacion.RapidezMaximaLocomocion);
                Assert.AreEqual(sinTope, escala, 0.001f, "la escala no topa a " + v + " m/s");
            }
        }

        [Test]
        public void Locomocion_CaminarNoSeAceleraMasDeLoPermitidoAntesDeCorrer()
        {
            float escala = ReglasAnimacion.UmbralCorrer / ReglasAnimacion.VelocidadClipCaminar;
            Assert.LessOrEqual(escala, ReglasAnimacion.RapidezMaximaCaminar + 0.01f);
            Assert.LessOrEqual(ReglasAnimacion.UmbralCorrer, ReglasAnimacion.VelocidadClipCorrer + 0.1f, "correr no arranca más lento que su clip");
        }

        [Test]
        public void Prefabs_ConservanLaRapidezMaximaNueva()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("woman"));
            var so = new SerializedObject(prefab.GetComponent<VisualPersonaje>());
            Assert.AreEqual(ReglasAnimacion.RapidezMaximaLocomocion, so.FindProperty("rapidezMaxima").floatValue, 0.001f);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminar, so.FindProperty("velocidadClipCaminar").floatValue, 0.001f);
        }

        [Test]
        public void Cobertura_ElDatoYaNoAplanaElModelo()
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("Assets/_Proyecto/Personaje/Datos/Resources/DatosCubierta.asset"));
            Assert.AreEqual(1f, so.FindProperty("escalaAgachado").floatValue, 0.0001f);
        }

        [Test]
        public void Idle_DesfaseYRapidez_SonDeterministasYEstanEnRango()
        {
            for (int s = 1; s < 200; s++)
            {
                float d = ReglasAnimacion.DesfaseIdle(s);
                float r = ReglasAnimacion.RapidezIdle(s);
                Assert.That(d, Is.InRange(0f, 0.9999f));
                Assert.That(r, Is.InRange(ReglasAnimacion.RapidezIdleMinima, ReglasAnimacion.RapidezIdleMaxima));
                Assert.AreEqual(d, ReglasAnimacion.DesfaseIdle(s));
                Assert.AreEqual(r, ReglasAnimacion.RapidezIdle(s));
            }
        }

        [Test]
        public void Idle_VariosNPC_NoRespiranAlUnisono()
        {
            var desfases = new HashSet<float>();
            var rapideces = new HashSet<float>();
            for (int s = 1; s <= 30; s++)
            {
                desfases.Add(ReglasAnimacion.DesfaseIdle(s));
                rapideces.Add(ReglasAnimacion.RapidezIdle(s));
            }
            Assert.Greater(desfases.Count, 25);
            Assert.Greater(rapideces.Count, 25);
        }

        [Test]
        public void Mirada_VoltearTardaUnSegundoEnRango_YSalirReiniciaElTemporizador()
        {
            float t = 0f;
            t = ReglasAnimacion.AvanzarMirada(t, true, 0.6f);
            Assert.IsFalse(ReglasAnimacion.MiradaLista(t));
            t = ReglasAnimacion.AvanzarMirada(t, true, 0.5f);
            Assert.IsTrue(ReglasAnimacion.MiradaLista(t));
            t = ReglasAnimacion.AvanzarMirada(t, false, 0.1f);
            Assert.AreEqual(0f, t);
            Assert.IsFalse(ReglasAnimacion.MiradaLista(t));
        }

        [Test]
        public void Levantarse_SoloDespuesDelRetardoYSinKO()
        {
            Assert.IsFalse(ReglasAnimacion.DebeLevantarse(true, 5f));
            Assert.IsFalse(ReglasAnimacion.DebeLevantarse(false, ReglasAnimacion.RetardoLevantarse - 0.1f));
            Assert.IsTrue(ReglasAnimacion.DebeLevantarse(false, ReglasAnimacion.RetardoLevantarse));
        }

        [Test]
        public void Moto_InclinacionCreceConLaVelocidadYElGiro()
        {
            Assert.AreEqual(0f, ReglasMoto.Inclinacion(1f, 0f));
            Assert.AreEqual(ReglasMoto.InclinacionMaxima, ReglasMoto.Inclinacion(1f, 50f), 0.001f);
            Assert.AreEqual(-ReglasMoto.InclinacionMaxima, ReglasMoto.Inclinacion(-3f, 50f), 0.001f);
            Assert.Less(ReglasMoto.Inclinacion(1f, 3f), ReglasMoto.Inclinacion(1f, 9f));
        }

        [Test]
        public void Moto_SubirYBajarAvanzanEnElTiempoCorto()
        {
            float a = ReglasMoto.Avance(0f, true, ReglasMoto.SegundosSubir / 2f);
            Assert.AreEqual(0.5f, a, 0.001f);
            a = ReglasMoto.Avance(a, true, 1f);
            Assert.AreEqual(1f, a);
            a = ReglasMoto.Avance(a, false, ReglasMoto.SegundosSubir);
            Assert.AreEqual(0f, a);
            Assert.AreEqual(0.5f, ReglasMoto.Suave(0.5f), 0.001f);
        }

        [Test]
        public void PuntosMoto_CrearPuntosPorDefecto_DejaLaMotoCompletaYNoDuplica()
        {
            var moto = new GameObject("Moto");
            try
            {
                PuntosMoto puntos = moto.AddComponent<PuntosMoto>();
                Assert.IsFalse(puntos.Completo);
                puntos.CrearPuntosPorDefecto();
                Assert.IsTrue(puntos.Completo);
                Assert.AreEqual(5, moto.transform.childCount);
                puntos.CrearPuntosPorDefecto();
                Assert.AreEqual(5, moto.transform.childCount);
                Assert.Less(puntos.EstriboIzquierdo.localPosition.x, 0f);
                Assert.Greater(puntos.ManubrioDerecho.localPosition.z, puntos.Asiento.localPosition.z);
            }
            finally
            {
                Object.DestroyImmediate(moto);
            }
        }

        [Test]
        public void Prefabs_LosNPCTienenVariacionYElJugadorNo()
        {
            foreach ((string modelo, string papel, bool mira) in ConectarControladorBase.Reparto)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(modelo));
                Assert.AreEqual(modelo != "woman", prefab.GetComponent<VariacionNPC>() != null, modelo);
            }
        }
    }
}
