using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1333: el caminar del jugador usa los clips de Blender con bucle y la rapidez de la animación sigue la velocidad real (sin patinar).</summary>
    public class CaminarT1333Pruebas
    {
        private const string Carpeta = "Assets/Arte/Personajes/PJ01/Clips/";
        private static readonly string[] Clips = { "PJ01_Caminar_SinImplante", "PJ01_Caminar_ConImplante", "PJ01_Correr_SinImplante", "PJ01_Correr_ConImplante" };

        [TestCaseSource(nameof(Clips))]
        public void Clip_ImportaHumanoid_ConBucle_YRaizHorneada(string nombre)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(Carpeta + nombre + ".fbx");
            Assert.IsNotNull(imp, nombre);
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType);
            Assert.AreEqual(1, imp.clipAnimations.Length, nombre);
            Assert.IsTrue(imp.clipAnimations[0].loopTime, nombre + " va en bucle");
            Assert.IsTrue(imp.clipAnimations[0].lockRootRotation, nombre + " hornea el giro de la raíz");
            Assert.IsTrue(imp.clipAnimations[0].lockRootHeightY, nombre + " hornea la altura de la raíz");
            Assert.IsFalse(imp.clipAnimations[0].mirror, nombre + " sin espejo");
        }

        [Test]
        public void Controlador_CaminarYCorrer_UsanLosClipsDeBlenderDeRonda2()
        {
            Assert.AreEqual("PJ01_Caminar_SinImplante", ConstructorControladorBase.Clip("Loco_Caminar").name);
            Assert.AreEqual("PJ01_Correr_SinImplante", ConstructorControladorBase.Clip("Loco_Correr").name);
            Assert.AreEqual("PJ01_Caminar_ConImplante", ConstructorControladorBase.Clip("Loco_Caminar_ConImplante").name);
        }

        [Test]
        public void ConstantesDeVelocidad_SonLasMedidasDeLosClips_Mas_MenosDiezPorCiento()
        {
            float caminar = MedicionClips.Medir("Loco_Caminar_SinImplante");
            float correr = MedicionClips.Medir("Loco_Correr_SinImplante");
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminarSinImplante, caminar, caminar * 0.1f, "Caminar medido " + caminar);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCorrerSinImplante, correr, correr * 0.1f, "Correr medido " + correr);
        }

        [TestCase("woman")]
        [TestCase("PJ01")]
        public void VisualesDelJugador_TraenLaVelocidadMedidaDelClipNuevo(string modelo)
        {
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(modelo)).GetComponent<VisualPersonaje>();
            var so = new SerializedObject(visual);
            float caminar = MedicionClips.Medir("Loco_Caminar_SinImplante");
            float correr = MedicionClips.Medir("Loco_Correr_SinImplante");
            Assert.AreEqual(caminar, so.FindProperty("velocidadClipCaminar").floatValue, caminar * 0.1f, "Caminar de Visual_" + modelo + " (0.997 es el clip viejo)");
            Assert.AreEqual(correr, so.FindProperty("velocidadClipCorrer").floatValue, correr * 0.1f, "Correr de Visual_" + modelo + " (2.51 es el clip viejo)");
        }

        [Test]
        public void ClipsDeTony_DejaLaRapidezSiguiendoLaVelocidadReal()
        {
            var go = new GameObject("VisualT1333");
            try
            {
                var visual = go.AddComponent<VisualPersonaje>();
                AnimacionJugador.ClipsDeTony(visual);
                var so = new SerializedObject(visual);
                Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminarSinImplante, so.FindProperty("velocidadClipCaminar").floatValue, 0.001f);
                Assert.AreEqual(ReglasAnimacion.VelocidadClipCorrerSinImplante, so.FindProperty("velocidadClipCorrer").floatValue, 0.001f);
                float rapidezCaminar = ReglasAnimacion.EscalaVelocidad(ReglasAnimacion.VelocidadAndar, ReglasAnimacion.VelocidadClipCaminarSinImplante, 0.6f, ReglasAnimacion.RapidezMaximaCaminar);
                Assert.Greater(rapidezCaminar, 1f, "Caminar a la velocidad del jugador acelera el clip");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UmbralesDelControlador_CaminarEntreAndarYCorrer()
        {
            Assert.Less(ReglasVisual.UmbralCaminar, ReglasAnimacion.VelocidadAndar);
            Assert.Less(ReglasAnimacion.VelocidadAndar, ReglasAnimacion.UmbralCorrer);
            Assert.Less(ReglasAnimacion.UmbralCorrer, ReglasAnimacion.VelocidadCarrera);
        }
    }
}
