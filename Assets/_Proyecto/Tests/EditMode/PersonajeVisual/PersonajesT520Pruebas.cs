using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-520: los personajes de Tripo están armados (Humanoid, 1.8 m, controlador) y conectados a los prefabs del juego.</summary>
    public class PersonajesT520Pruebas
    {
        private const float AlturaEsperada = 1.8f;
        private const float Tolerancia = 0.12f;

        private static GameObject Cargar(string ruta)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(g, ruta);
            return g;
        }

        [TestCase("woman")]
        [TestCase("man")]
        [TestCase("woman2")]
        public void Visual_DeCadaPersonaje_EsHumanoideConAnimatorYControlador(string nombre)
        {
            GameObject prefab = Cargar(CreadorVisualPersonaje.RutaPrefab(nombre));
            Assert.IsNotNull(prefab.GetComponent<VisualPersonaje>(), "VisualPersonaje en la raíz");
            Animator animador = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animador, "Animator en el modelo");
            Assert.IsNotNull(animador.avatar);
            Assert.IsTrue(animador.avatar.isValid && animador.avatar.isHuman, "Avatar Humanoid válido");
            Assert.IsFalse(animador.applyRootMotion, "Sin movimiento de raíz: lo mueve el juego");
            AnimatorController c = (animador.runtimeAnimatorController as AnimatorOverrideController)?.runtimeAnimatorController as AnimatorController;
            Assert.IsNotNull(c);
            string[] estados = c.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            foreach (string e in ReglasAnimacion.EstadosBase)
            {
                CollectionAssert.Contains(estados, e);
            }
            foreach (string parametro in new[] { "Velocidad", "EnPiso", "Apuntando", "Arma", "KO", "Golpe", "Disparo", "Recarga" })
            {
                Assert.IsTrue(c.parameters.Any(p => p.name == parametro), "Parámetro del juego: " + parametro);
            }
            ChildAnimatorState caminar = c.layers[0].stateMachine.states.First(s => s.state.name == "Caminar");
            Assert.IsNotNull(caminar.state.motion, "Caminar usa el clip del modelo");
            ChildAnimatorState correr = c.layers[0].stateMachine.states.First(s => s.state.name == "Correr");
            Assert.IsNotNull(correr.state.motion, "Correr usa el clip del modelo");
            Assert.IsTrue(c.layers[0].iKPass, "IK Pass para apuntar");
        }

        [TestCase("woman")]
        [TestCase("man")]
        [TestCase("woman2")]
        public void Visual_DeCadaPersonaje_MideAlrededorDeUnMetroOchentaYSeTumbaYApunta(string nombre)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(Cargar(CreadorVisualPersonaje.RutaPrefab(nombre)));
            try
            {
                Bounds? total = null;
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>())
                {
                    if (total.HasValue) { Bounds b = total.Value; b.Encapsulate(r.bounds); total = b; } else { total = r.bounds; }
                }
                Assert.AreEqual(AlturaEsperada, total.Value.size.y, Tolerancia, "Altura del modelo (m)");
                Assert.AreEqual(Vector3.one, instancia.transform.localScale, "La raíz no se escala");
                Assert.IsNotNull(instancia.GetComponentInChildren<ApuntadoIK>(true), "ApuntadoIK para apuntar sin animación");
                Assert.IsNotNull(instancia.GetComponentInChildren<ApuntadoIK>(true).SocketArma, "Socket de la mano para las armas");
                VisualPersonaje v = instancia.GetComponent<VisualPersonaje>();
                Assert.AreEqual(0f, v.Tumbado);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        [TestCase("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab", "woman")]
        [TestCase("Assets/_Proyecto/IA/Prefabs/Aliado.prefab", "woman2")]
        [TestCase("Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab", "man")]
        [TestCase("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab", "woman2")]
        public void PrefabsDelJuego_UsanElVisualDeTripo_SinElMunecoDePrueba(string ruta, string cuerpo)
        {
            GameObject prefab = Cargar(ruta);
            VisualPersonaje visual = prefab.GetComponentInChildren<VisualPersonaje>(true);
            Assert.IsNotNull(visual, ruta + " tiene VisualPersonaje");
            Animator animador = visual.GetComponentInChildren<Animator>(true);
            string papel = System.Array.Find(ConectarControladorBase.Reparto, r => r.Modelo == cuerpo).Papel;
            Assert.AreEqual(ConstructorControladorBase.RutaOverride(papel), AssetDatabase.GetAssetPath(animador.runtimeAnimatorController), "Override de " + cuerpo);
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreNotEqual("Cadera", t.name, "El muñeco de cubos ya no está en " + ruta);
            }
        }

        [Test]
        public void Jugador_ConservaColliderYArmasEnLaManoDelVisual()
        {
            GameObject prefab = Cargar("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab");
            CharacterController cc = prefab.GetComponent<CharacterController>();
            Assert.IsNotNull(cc);
            Assert.AreEqual(AlturaEsperada, cc.height, 0.05f, "CharacterController de 1.8 m");
            ApuntadoIK ik = prefab.GetComponentInChildren<ApuntadoIK>(true);
            Assert.IsNotNull(ik.SocketArma);
            foreach (string arma in new[] { "Pistola", "Escopeta", "Fusil", "Sniper" })
            {
                Transform t = ik.SocketArma.Find(arma);
                Assert.IsNotNull(t, arma + " cuelga de la mano del visual");
                Assert.IsNotNull(t.Find("Boca"), arma + " conserva su boca");
            }
        }

        [Test]
        public void Peaton_TieneCuatroVariantesDeColorDistintas()
        {
            GameObject prefab = Cargar("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab");
            VarianteVisual v = prefab.GetComponentInChildren<VarianteVisual>(true);
            Assert.IsNotNull(v);
            Assert.AreEqual(4, v.Cantidad);
            var colores = new System.Collections.Generic.HashSet<Color>();
            for (int i = 0; i < 4; i++)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Personajes/Materiales/woman2_Peaton" + (i + 1) + ".mat");
                Assert.IsNotNull(m);
                colores.Add(m.GetColor("_BaseColor"));
            }
            Assert.AreEqual(4, colores.Count, "Cuatro colores distintos");
        }

        [Test]
        public void Skulls_SonNegrosConFranjaMagenta()
        {
            foreach (string ruta in new[] { ConectarVisualesT520.RutaDatosCalaveras, ConectarVisualesT520.RutaDatosJefeCalaveras })
            {
                SerializedObject so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(ruta));
                Color c = so.FindProperty("colorNormal").colorValue;
                Assert.Less(c.maxColorComponent, 0.25f, "Cuerpo negro: " + ruta);
            }
            Material franja = ConectarVisualesT520.Franja();
            Color e = franja.GetColor("_EmissionColor");
            Assert.Greater(e.r, e.g * 5f, "Franja magenta (rojo y azul altos, verde bajo)");
            Assert.Greater(e.b, e.g * 5f);
            Material jefe = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Personajes/Materiales/SkullHombre_JefeSkulls.mat");
            Assert.IsNotNull(jefe);
            Assert.Greater(jefe.GetColor("_EmissionColor").r, 0f, "El jefe (SkullHombre, T-510) brilla magenta (emisión mínima; la luz hace el resto)");
        }

        [Test]
        public void ModelosDeTripo_GuardanLaEscalaAUnMetroOchenta()
        {
            foreach (string n in CreadorVisualTripo.Nombres)
            {
                ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(CreadorVisualTripo.RutaModelo(n));
                Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, n);
                Assert.IsTrue(imp.userData.StartsWith("escala="), n + " guarda su escala en userData");
                Assert.AreEqual(float.Parse(imp.userData.Substring(7), System.Globalization.CultureInfo.InvariantCulture), imp.globalScale, 0.001f, n);
            }
            ModelImporter hombre = (ModelImporter)AssetImporter.GetAtPath(CreadorVisualTripo.RutaModelo("man"));
            Assert.IsTrue(hombre.generateMeshLods, "man (92 mil triángulos) lleva Mesh LOD");
        }

        [Test]
        public void Velocidad_AlCaminarYCorrer_AjustaLaRapidezDeLaAnimacion()
        {
            GameObject prefab = Cargar(CreadorVisualPersonaje.RutaPrefab("woman"));
            SerializedObject so = new SerializedObject(prefab.GetComponent<VisualPersonaje>());
            Assert.Greater(so.FindProperty("velocidadClipCaminar").floatValue, 0.3f, "Velocidad medida del clip Caminar (m/s)");
            Assert.Greater(so.FindProperty("velocidadClipCorrer").floatValue, so.FindProperty("velocidadClipCaminar").floatValue, "Correr avanza más que Caminar");
        }

        [Test]
        public void ListaDeAnimacionesQueFaltan_PideLoQueDeVerdadFalta()
        {
            Assert.IsTrue(System.IO.File.Exists("docs/animaciones-faltan.md"));
            string texto = System.IO.File.ReadAllText("docs/animaciones-faltan.md");
            // Lo que sigue faltando (T-625).
            foreach (string archivo in new[] { "Loco_Rodar.fbx", "NPC_SentadoEscribiendo_Bucle.fbx", "NPC_Platicar1_Bucle.fbx", "NPC_RecargadoPared_Bucle.fbx", "NPC_Aterrado_Bucle.fbx", "Moto_Subir.fbx" })
            {
                StringAssert.Contains(archivo, texto);
            }
            // Cada archivo de la tabla debe faltar de verdad: no puede haber ya un FBX con ese nombre en Animaciones/.
            var existentes = new System.Collections.Generic.HashSet<string>();
            foreach (string ruta in System.IO.Directory.GetFiles("Assets/Arte/Personajes/Animaciones", "*.fbx", System.IO.SearchOption.AllDirectories))
            {
                existentes.Add(System.IO.Path.GetFileName(ruta));
            }
            foreach (string linea in texto.Split('\n'))
            {
                if (!linea.StartsWith("|") || linea.StartsWith("| ---") || linea.StartsWith("| Para qué")) continue;
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(linea, @"\w+\.fbx"))
                {
                    Assert.IsFalse(existentes.Contains(m.Value), m.Value + " ya está importado: quítalo de docs/animaciones-faltan.md");
                }
            }
        }
    }
}
