using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1553: los robots de Nova tras la revisión de Tony del 20-nov. Los clips E16_CaminarEscudo y E18_* viven en la carpeta Clips de su personaje (con su .meta: mismo GUID),
    /// los cuatro con rig propio se importan Humanoid con la escala del personaje, el Avatar del E18 queda con los brazos en T, la W10 del E15 apunta al frente con
    /// E15_Caminar y E15_Correr, y el E18 usa sus clips con velocidades 1.62 y 4.53 m/s. Necesita haber corrido RobotsNovaT1553.Aplicar.
    /// </summary>
    public class RobotsNovaT1553Pruebas
    {
        private static ModelImporter Importador(string nombre)
        {
            var importador = AssetImporter.GetAtPath(ClipsRobotsNovaT1553.RutaClip(nombre)) as ModelImporter;
            Assert.IsNotNull(importador, nombre + " no está en " + ClipsRobotsNovaT1553.RutaClip(nombre));
            return importador;
        }

        [Test]
        public void ClipsDeRobot_EstanEnLaCarpetaDeSuPersonaje_YYaNoEnPJ01()
        {
            foreach (string nombre in ClipsRobotsNovaT1553.ClipsDeRobot)
            {
                string ruta = ClipsRobotsNovaT1553.RutaClip(nombre);
                string esperada = nombre.StartsWith("E16_") ? "Assets/Arte/Personajes/E16_RobotPesadoNova/Clips/" : "Assets/Arte/Personajes/E18_EliteNova/Clips/";
                StringAssert.StartsWith(esperada, ruta, nombre);
                Assert.IsTrue(File.Exists(ruta), ruta);
                Assert.IsTrue(File.Exists(ruta + ".meta"), "el .meta viaja con el .fbx: " + ruta);
                Assert.IsFalse(File.Exists(ImportadorClipsPJ01.Carpeta + nombre + ".fbx"), nombre + " ya no está en PJ01/Clips");
            }
            Assert.IsEmpty(Directory.GetFiles(ImportadorClipsPJ01.Carpeta.TrimEnd('/'), "E18_*.fbx"), "ningún E18_* queda en PJ01/Clips");
            Assert.IsEmpty(Directory.GetFiles(ImportadorClipsPJ01.Carpeta.TrimEnd('/'), "E16_CaminarEscudo*.fbx"));
        }

        [Test]
        public void CarpetasClips_TienenSuMeta()
        {
            Assert.IsTrue(File.Exists(ClipsRobotsNovaT1553.CarpetaE16.TrimEnd('/') + ".meta"));
            Assert.IsTrue(File.Exists(ClipsRobotsNovaT1553.CarpetaE18.TrimEnd('/') + ".meta"));
        }

        [Test]
        public void ConstructorControladorBase_EncuentraLosClipsEnSuNuevaCarpeta()
        {
            foreach (string nombre in ClipsRobotsNovaT1553.ClipsDeRobot)
            {
                AnimationClip clip = ConstructorControladorBase.Clip(nombre);
                Assert.IsNotNull(clip, nombre);
                Assert.AreEqual(ClipsRobotsNovaT1553.RutaClip(nombre), AssetDatabase.GetAssetPath(clip), nombre);
            }
        }

        [TestCase("E16_CaminarEscudo")]
        [TestCase("E18_Caminar")]
        [TestCase("E18_Correr")]
        [TestCase("E18_Golpe_Der")]
        public void ClipsConRigPropio_SonHumanoid_ConElAvatarYLaEscalaDeSuPersonaje(string nombre)
        {
            ModelImporter importador = Importador(nombre);
            Assert.IsTrue(ClipsRobotsNovaT1553.TieneRigPropio(nombre));
            Assert.AreEqual(ModelImporterAnimationType.Human, importador.animationType, nombre);
            Assert.AreEqual(ModelImporterMaterialImportMode.None, importador.materialImportMode, nombre + ": sin materiales");
            var modelo = (ModelImporter)AssetImporter.GetAtPath(ClipsRobotsNovaT1553.RutaModeloDe(nombre));
            Assert.AreEqual(modelo.globalScale, importador.globalScale, 1e-4f, nombre + ": la escala de su personaje");
            // "Humanoid con el Avatar del personaje" (Tony, 20-nov): Copy From Other Avatar con el del modelo.
            Assert.AreEqual(ModelImporterAvatarSetup.CopyFromOther, importador.avatarSetup, nombre + ": avatar copiado del personaje (corre RobotsNovaT1553.Aplicar)");
            Assert.IsNotNull(importador.sourceAvatar, nombre);
            Assert.AreEqual(ClipsRobotsNovaT1553.RutaModeloDe(nombre), AssetDatabase.GetAssetPath(importador.sourceAvatar), nombre + ": el avatar es el de su personaje");
            Assert.IsTrue(importador.sourceAvatar.isValid && importador.sourceAvatar.isHuman, nombre + ": avatar Humanoid válido");
            Assert.AreEqual(1, importador.clipAnimations.Length, nombre);
            Assert.AreEqual(nombre, importador.clipAnimations[0].name, nombre);
        }

        [TestCase("E16_CaminarEscudo", true)]
        [TestCase("E18_Caminar", true)]
        [TestCase("E18_Correr", true)]
        [TestCase("E18_Golpe_Der", false)]
        [TestCase("E18_Jetpack_Volar", true)]
        [TestCase("E18_Caida_Aire", true)]
        [TestCase("E18_Caida_Aterrizaje", false)]
        public void ClipsDeRobot_Bucle(string nombre, bool bucle)
        {
            Assert.AreEqual(bucle, Importador(nombre).clipAnimations[0].loopTime, nombre);
        }

        [Test]
        public void ClipsDeRobot_ConRigPJ01_SiguenHumanoidConLaEscalaDePJ01()
        {
            foreach (string nombre in ClipsRobotsNovaT1553.ClipsDeRobot.Where(n => !ClipsRobotsNovaT1553.TieneRigPropio(n)))
            {
                ModelImporter importador = Importador(nombre);
                Assert.AreEqual(ModelImporterAnimationType.Human, importador.animationType, nombre);
                Assert.AreEqual(ImportadorClipsPJ01.EscalaClips, importador.globalScale, 1e-3f, nombre);
            }
        }

        [Test]
        public void Postprocesador_ReconoceLasDosCarpetas_YNingunaOtra()
        {
            Assert.IsTrue(ClipsRobotsNovaT1553.EsClipDeRobot("Assets/Arte/Personajes/E18_EliteNova/Clips/E18_Caminar.fbx"));
            Assert.IsTrue(ClipsRobotsNovaT1553.EsClipDeRobot("Assets/Arte/Personajes/E16_RobotPesadoNova/Clips/E16_CaminarEscudo.fbx"));
            Assert.IsFalse(ClipsRobotsNovaT1553.EsClipDeRobot("Assets/Arte/Personajes/E18_EliteNova/E18_EliteNova_a.fbx"), "el modelo del personaje no es un clip");
            Assert.IsFalse(ClipsRobotsNovaT1553.EsClipDeRobot("Assets/Arte/Personajes/PJ01/Clips/PJ01_Caminar.fbx"));
            Assert.IsFalse(ClipsRobotsNovaT1553.EsClipDeRobot("Assets/Arte/Personajes/E18_EliteNova/Clips/E18_Caminar.fbm/x.jpg"));
        }

        [Test]
        public void ClipsNpcNovaT1506_ImportaLosCuatroClipsNuevos_YLaTablaDeVisualesTraeLasVelocidadesDelE18()
        {
            foreach (string nombre in new[] { "E16_CaminarEscudo", "E18_Caminar", "E18_Correr", "E18_Golpe_Der" })
            {
                CollectionAssert.Contains(ClipsNpcNovaT1506.ClipsHumanoides, nombre);
            }
            (string Visual, string Override, float Caminar, float Correr) fila = ClipsNpcNovaT1506.Visuales.First(v => v.Visual == "E18_EliteNova_a");
            Assert.AreEqual("E18", fila.Override);
            Assert.AreEqual(1.62f, fila.Caminar, 1e-4f, "E18_Caminar a 1.62 m/s");
            Assert.AreEqual(4.53f, fila.Correr, 1e-4f, "E18_Correr a 4.53 m/s");
        }

        [Test]
        public void Overrides_ApuntanALosClipsDeLaNuevaCarpeta()
        {
            foreach ((string nombre, string clipBase, string esperado) in new[] { ("E16", "Loco_Caminar_ConImplante", "E16_CaminarEscudo"), ("E18", "Loco_Caminar", "E18_Caminar"), ("E18", "Punos_GolpeDer", "E18_Golpe_Der") })
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ClipsNpcNovaT1506.RutaOverrideExtra(nombre));
                Assert.IsNotNull(o, nombre);
                var pares = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                AnimationClip clip = pares.First(p => p.Key != null && p.Key.name == clipBase).Value;
                Assert.IsNotNull(clip, nombre + " " + clipBase);
                Assert.AreEqual(esperado, clip.name);
                Assert.AreEqual(ClipsRobotsNovaT1553.RutaClip(esperado), AssetDatabase.GetAssetPath(clip));
            }
        }

        [Test]
        public void Config_TieneElClipDeVueloDelE18_EnBucle()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.JetpackVolar, "corre RobotsNovaT1553.Aplicar");
            Assert.AreEqual("E18_Jetpack_Volar", config.JetpackVolar.name);
            Assert.IsTrue(config.JetpackVolar.isLooping, "volar se repite");
            Assert.AreEqual("E18_Caida_Aterrizaje", config.CaidaAterrizaje.name, "aterriza con el clip de caída");
        }

        [Test]
        public void E18_Avatar_TieneLosBrazosEnT_TrasElArregloDelRig()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ClipsRobotsNovaT1553.RutaModeloE18).OfType<Avatar>().FirstOrDefault();
            Assert.IsNotNull(avatar);
            Assert.IsTrue(avatar.isValid && avatar.isHuman, "avatar humanoide válido");
            (float izquierdo, float derecho)? brazos = RobotsNovaT1553.MedirBrazos(ClipsRobotsNovaT1553.RutaModeloE18);
            Assert.IsTrue(brazos.HasValue, "no se pudo medir los brazos del modelo");
            Assert.IsTrue(RobotsNovaT1553.BrazosEnT(brazos), "brazos a " + brazos.Value.izquierdo.ToString("F1") + " y " + brazos.Value.derecho.ToString("F1")
                + " grados de la horizontal (máximo " + RobotsNovaT1553.ToleranciaBrazoT + "): rehacer el avatar con RobotsNovaT1553.Aplicar o Rig, Configure, Enforce T-Pose");
        }

        [Test]
        public void E15_W10_ApuntaAlFrente_ConE15CaminarYE15Correr()
        {
            var medidas = RobotsNovaT1553.MedirArmaE15(RobotsNovaT1553.RutaPrefabE15);
            Assert.AreEqual(RobotsNovaT1553.ClipsArmaE15.Length, medidas.Count, "se midieron E15_Caminar y E15_Correr");
            foreach (RobotsNovaT1553.MedidaArma m in medidas)
            {
                Assert.LessOrEqual(m.GiroMaximo, RobotsNovaT1553.ToleranciaGiroArma, m.Clip + ": giro de la boca respecto al frente");
                Assert.LessOrEqual(m.CabeceoMaximo, RobotsNovaT1553.ToleranciaCabeceoArma, m.Clip + ": cabeceo de la boca");
            }
        }

        [Test]
        public void EscalaNovaT1497_ComponeElGiroExtraDeLaW10_SoloParaElE15()
        {
            Assert.AreEqual(Vector3.zero, EscalaNovaT1497.GiroExtraArma("E16_RobotPesadoNova_a"));
            Assert.AreEqual(Vector3.zero, EscalaNovaT1497.GiroExtraArma(null));
            Assert.AreEqual(Vector3.zero, EscalaNovaT1497.GiroExtraArma("E18_EliteNova_a"));
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            Assert.IsNotNull(config);
            Assert.AreEqual(config.GiroArmaE15, EscalaNovaT1497.GiroExtraArma("E15_CentinelaNova_a"));
            Assert.AreEqual(config.GiroArmaE15, EscalaNovaT1497.GiroExtraArma("E15_CentinelaNova_b"));
        }
    }
}
