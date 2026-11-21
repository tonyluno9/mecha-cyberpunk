using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1552 (decisión de Tony, 20-nov): las alturas de los personajes. La tabla (factores y alturas) y la lógica de Aplicar son puras; lo que deja Aplicar (escala del Modelo,
    /// etiqueta, cápsulas, Tino en la escena) se lee de los prefabs y la escena ya generados con AlturasT1552.Aplicar (corre antes, con -executeMethod).
    /// </summary>
    public class AlturasT1552Pruebas
    {
        private const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        private static string[] Prefabs => EscalasHumanos.Prefabs;

        private static string[] PrefabsConCambio => EscalasHumanos.Prefabs.Where(v => Mathf.Abs(EscalasHumanos.Factor(v) - 1f) > 0.001f).ToArray();

        // ---------- La tabla ----------

        [TestCase("Visual_NP12_Abuela_a", 1.64f, 1.155f)]
        [TestCase("Visual_woman", 1.84f, 1.022f)]
        [TestCase("Visual_woman2", 1.84f, 1.064f)]
        [TestCase("Visual_MeseraRobot", 1.84f, 1.115f)]
        [TestCase("Visual_NP09_Corredor_a", 1.84f, 1.143f)]
        [TestCase("Visual_NP09_Corredor_b", 1.85f, 1.135f)]
        [TestCase("Visual_NP14_Oficinista_a", 1.84f, 1.157f)]
        [TestCase("Visual_NP14_Oficinista_b", 1.86f, 1.170f)]
        [TestCase("Visual_NP10_Elotero_a", 1.84f, 1.219f)]
        [TestCase("Visual_Inventor", 1.84f, 0.939f)]
        [TestCase("Visual_Mecanico", 1.85f, 1.028f)]
        [TestCase("Visual_AL10", 1.86f, 1.039f)]
        [TestCase("Visual_AL11", 1.86f, 1.039f)]
        [TestCase("Visual_Policia", 1.86f, 1f)]
        [TestCase("Visual_NP13_Repartidor_a", 1.86f, 1.232f)]
        [TestCase("Visual_NP08_Turista_a", 1.86f, 1.192f)]
        [TestCase("Visual_NP08_Turista_b", 1.86f, 1.170f)]
        [TestCase("Visual_NP07_Pescador_a", 1.87f, 1.199f)]
        [TestCase("Visual_NP07_Pescador_b", 1.87f, 1.206f)]
        [TestCase("Visual_SkullHombre", 1.87f, 1.081f)]
        [TestCase("Visual_Doctor", 1.88f, 0.959f)]
        [TestCase("Visual_man", 1.88f, 1.068f)]
        [TestCase("Visual_E19_PandilleroSkulls_a_2", 1.89f, 1.181f)]
        [TestCase("Visual_E19_PandilleroSkulls_b", 1.89f, 1.196f)]
        [TestCase("Visual_E10_PandilleroSnakes", 1.89f, 1.167f)]
        [TestCase("Visual_Hacker", 1.90f, 1.056f)]
        [TestCase("Visual_NP11_Estibador_a", 1.90f, 1.180f)]
        [TestCase("Visual_NP11_Estibador_b", 1.90f, 1.145f)]
        [TestCase("Visual_E15_CentinelaNova_a", 1.94f, 0.819f)]
        [TestCase("Visual_E15_CentinelaNova_b", 1.94f, 0.819f)]
        [TestCase("Visual_GuardiaNova", 1.94f, 0.789f)]
        [TestCase("Visual_RobotHombre", 1.94f, 1.078f)]
        [TestCase("Visual_E20_MatonSkulls_a", 1.96f, 1.153f)]
        [TestCase("Visual_E20_MatonSkulls_b", 1.96f, 1.174f)]
        [TestCase("Visual_E11_JefeSnakes", 1.98f, 1.112f)]
        [TestCase("Visual_E12_GuardiaPesadoNova", 2.04f, 1.153f)]
        [TestCase("Visual_E18_EliteNova_a", 2.14f, 1.109f)]
        [TestCase("Visual_E14_JefeFinalNova", 3.14f, 1.661f)]
        [TestCase("Visual_E16_RobotPesadoNova_a", 3.14f, 1.075f)]
        [TestCase("Visual_E16_RobotPesadoNova_b", 3.14f, 1.040f)]
        public void Tabla_AlturaNuevaYFactor_SonLosDeTony(string visual, float nueva, float factor)
        {
            Assert.AreEqual(nueva, EscalasHumanos.Objetivo(visual), 0.0001f, visual + ": altura nueva");
            Assert.AreEqual(factor, EscalasHumanos.Factor(visual), 0.0015f, visual + ": factor = nueva / hoy (la tabla de Tony da 3 decimales)");
        }

        [Test]
        public void Tabla_JugadorYPerro_NoEstanYSuFactorEsUno()
        {
            foreach (string visual in new[] { "Visual_PJ01", "Visual_E17_PerroRobotNova_a", "Visual_E17_PerroRobotNova_esc" })
            {
                Assert.AreEqual(0f, EscalasHumanos.Objetivo(visual), visual + " no se toca");
                Assert.AreEqual(1f, EscalasHumanos.Factor(visual), 0.0001f, visual);
            }
            Assert.AreEqual(1.84f, EscalasHumanos.AlturaJugador, 0.0001f, "PJ01 no se baja");
        }

        [Test]
        public void Tabla_TodosLosHumanosMidenMinimo184_ExceptoLaAbuela()
        {
            string[] robotsYGrandes = { "Visual_E15_CentinelaNova_a", "Visual_E15_CentinelaNova_b", "Visual_GuardiaNova", "Visual_RobotHombre", "Visual_E18_EliteNova_a",
                "Visual_E14_JefeFinalNova", "Visual_E16_RobotPesadoNova_a", "Visual_E16_RobotPesadoNova_b" };
            foreach (string visual in Prefabs)
            {
                if (visual == "Visual_NP12_Abuela_a")
                {
                    Assert.AreEqual(1.64f, EscalasHumanos.Objetivo(visual), 0.0001f);
                    continue;
                }
                Assert.GreaterOrEqual(EscalasHumanos.Objetivo(visual), 1.84f - 0.0001f, visual);
                if (robotsYGrandes.Contains(visual))
                {
                    Assert.GreaterOrEqual(EscalasHumanos.Objetivo(visual), 1.94f - 0.0001f, visual + ": robots humanoides 1.94 como mínimo");
                }
            }
        }

        [Test]
        public void Tabla_Robots_LaAlturaDeCadaGrupo()
        {
            foreach (string visual in new[] { "Visual_E15_CentinelaNova_a", "Visual_E15_CentinelaNova_b", "Visual_GuardiaNova", "Visual_RobotHombre" })
            {
                Assert.AreEqual(1.94f, EscalasHumanos.Objetivo(visual), 0.0001f, visual + ": robot humanoide");
            }
            Assert.AreEqual(2.14f, EscalasHumanos.Objetivo("Visual_E18_EliteNova_a"), 0.0001f, "soldado azul");
            foreach (string visual in new[] { "Visual_E14_JefeFinalNova", "Visual_E16_RobotPesadoNova_a", "Visual_E16_RobotPesadoNova_b" })
            {
                Assert.AreEqual(3.14f, EscalasHumanos.Objetivo(visual), 0.0001f, visual + ": robot grande");
            }
        }

        [Test]
        public void Tabla_EscalasNovaYHumanos_NoSeContradicen()
        {
            Assert.AreEqual(EscalasHumanos.Objetivo("Visual_E15_CentinelaNova_a"), EscalasNova.AlturaCentinelaE15, 0.0001f);
            Assert.AreEqual(EscalasHumanos.Objetivo("Visual_E16_RobotPesadoNova_a"), EscalasNova.AlturaRobotPesadoE16, 0.0001f);
            Assert.AreEqual(EscalasHumanos.Objetivo("Visual_E18_EliteNova_a"), EscalasNova.AlturaEliteE18, 0.0001f);
            Assert.AreEqual(EscalasHumanos.Objetivo("Visual_E14_JefeFinalNova"), EscalasNova.AlturaJefeFinalE14, 0.0001f);
        }

        [Test]
        public void Tabla_CadaVisualExisteYLleva_GiroModelo()
        {
            foreach (string visual in Prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + visual + ".prefab");
                Assert.IsNotNull(prefab, visual + ": no existe el prefab");
                Assert.IsNotNull(prefab.transform.Find("Giro/Modelo"), visual + ": sin Giro/Modelo");
            }
        }

        // ---------- Decidir: la escala que deja Aplicar, repetible ----------

        [Test]
        public void Decidir_PrimeraVez_MultiplicaLaEscalaDeHoyPorElFactor()
        {
            AlturasT1552.Decision d = AlturasT1552.Decidir("Visual_NP10_Elotero_a", 1.2f, 0f, 0f, 0f, 1.51f);
            Assert.IsTrue(d.Cambia);
            Assert.AreEqual(1.2f * 1.84f / 1.51f, d.Escala, 0.0001f);
            Assert.AreEqual(1.2f, d.Base, 0.0001f);
        }

        [Test]
        public void Decidir_SegundaVez_ConLaEtiquetaDePrimera_NoVuelveAMultiplicar()
        {
            AlturasT1552.Decision primera = AlturasT1552.Decidir("Visual_NP10_Elotero_a", 1.2f, 0f, 0f, 0f, 1.51f);
            AlturasT1552.Decision segunda = AlturasT1552.Decidir("Visual_NP10_Elotero_a", primera.Escala, primera.Base, primera.Escala, 0f, 1.84f);
            Assert.IsFalse(segunda.Cambia, "ya aplicado");
            Assert.AreEqual(primera.Escala, segunda.Escala, 0.0001f);
            Assert.AreEqual(1.2f, segunda.Base, 0.0001f, "conserva la base de hoy");
        }

        [Test]
        public void Decidir_SinEtiquetaPeroYaMideLaAlturaNueva_NoMultiplica()
        {
            AlturasT1552.Decision d = AlturasT1552.Decidir("Visual_E15_CentinelaNova_a", 1.0f, 0f, 0f, 0f, 1.95f);
            Assert.IsFalse(d.Cambia, "el prefab se rehízo con la altura nueva: no se multiplica otra vez");
            Assert.AreEqual(1.0f, d.Escala, 0.0001f);
        }

        [Test]
        public void Decidir_ElPrefabSeRehizo_TomaLaEscalaNuevaComoBase()
        {
            AlturasT1552.Decision d = AlturasT1552.Decidir("Visual_NP12_Abuela_a", 1.0f, 1.3f, 1.5f, 0f, 1.42f);
            Assert.IsTrue(d.Cambia, "la escala ya no es la de la etiqueta: alguien rehízo el prefab");
            Assert.AreEqual(1.0f, d.Base, 0.0001f);
            Assert.AreEqual(1.64f / 1.42f, d.Escala, 0.0001f);
        }

        [Test]
        public void Decidir_Tino_UsaLaEscalaDeLaInstancia_YBaja()
        {
            AlturasT1552.Decision d = AlturasT1552.Decidir("Visual_Inventor", 1.4949f, 0f, 0f, 1.67f, 1.75f);
            Assert.IsTrue(d.Cambia);
            Assert.AreEqual(1.67f * 1.84f / 1.96f, d.Escala, 0.0001f, "1.67 de la instancia por 0.939");
            Assert.Less(d.Escala, 1.67f, "Tino baja");
        }

        [Test]
        public void Decidir_SinCambio_PoliciaYFueraDeLaTabla_NoTocaNada()
        {
            Assert.IsFalse(AlturasT1552.Decidir("Visual_Policia", 1.3f, 0f, 0f, 0f, 1.86f).Cambia, "el policía no cambia");
            Assert.IsFalse(AlturasT1552.Decidir("Visual_PJ01", 1.3f, 0f, 0f, 0f, 1.84f).Cambia, "PJ01 no está en la tabla");
            Assert.IsFalse(AlturasT1552.Decidir("Visual_E17_PerroRobotNova_a", 1.3f, 0f, 0f, 0f, 1.2f).Cambia, "el perro no está en la tabla");
        }

        [Test]
        public void Etiqueta_SeEscribeYSeLee()
        {
            string etiqueta = AlturasT1552.CrearEtiqueta(1.4949f, 1.568f);
            Assert.IsTrue(AlturasT1552.LeerEtiqueta(new[] { "Otra", etiqueta }, out float baseEsc, out float fin));
            Assert.AreEqual(1.4949f, baseEsc, 0.0001f);
            Assert.AreEqual(1.568f, fin, 0.0001f);
            Assert.IsFalse(AlturasT1552.LeerEtiqueta(new[] { "Otra", "T1552_x" }, out _, out _));
            Assert.IsFalse(AlturasT1552.LeerEtiqueta(null, out _, out _));
        }

        // ---------- Cápsulas ----------

        [Test]
        public void Capsula_RobotsGrandes_SeguenLos314()
        {
            AlturasT1552.MedidasCapsula m = AlturasT1552.CalcularCapsula(3.14f, true, 2.3f, 0.5f, 0.3f, 1f);
            Assert.AreEqual(EscalasNova.AlturaCapsula(3.14f), m.Alto, 0.0001f);
            Assert.That(m.Alto, Is.InRange(3.0f, 3.14f), "la cápsula del robot de 3.14 m mide casi lo mismo");
            Assert.AreEqual(m.Alto * 0.5f, m.CentroY, 0.0001f, "parte del piso");
            Assert.AreEqual(EscalasNova.RadioMaximo, m.Radio, 0.0001f);
            Assert.AreEqual(EscalasNova.Factor(3.14f), m.EscalaCuerpo, 0.0001f);
            Assert.Less(m.Paso, m.Alto * 0.5f);
        }

        [Test]
        public void Capsula_HumanoGrande_SoloCrece()
        {
            AlturasT1552.MedidasCapsula crece = AlturasT1552.CalcularCapsula(2.04f, false, 1.8f, 0.3f, 0.3f, 1f);
            Assert.AreEqual(EscalasNova.AlturaCapsula(2.04f), crece.Alto, 0.0001f);
            Assert.AreEqual(0.3f, crece.Radio, 0.0001f);
            Assert.AreEqual(1f, crece.EscalaCuerpo, 0.0001f);
            AlturasT1552.MedidasCapsula igual = AlturasT1552.CalcularCapsula(1.84f, false, 1.8f, 0.3f, 0.3f, 1f);
            Assert.AreEqual(1.8f, igual.Alto, 0.0001f, "con 1.84 m el 97 % (1.78) no baja la cápsula de 1.8");
        }

        [Test]
        public void Capsula_EsRepetible()
        {
            AlturasT1552.MedidasCapsula a = AlturasT1552.CalcularCapsula(3.14f, true, 2.3f, 0.5f, 0.3f, 1f);
            AlturasT1552.MedidasCapsula b = AlturasT1552.CalcularCapsula(3.14f, true, a.Alto, a.Radio, a.Paso, a.EscalaCuerpo);
            Assert.AreEqual(a.Alto, b.Alto, 0.0001f);
            Assert.AreEqual(a.Paso, b.Paso, 0.0001f);
            Assert.AreEqual(a.EscalaCuerpo, b.EscalaCuerpo, 0.0001f);
        }

        [Test]
        public void Capsula_LaListaIncluyeE14YE16()
        {
            string[] visuales = AlturasT1552.FilasCapsula().Select(f => f.Visual).ToArray();
            CollectionAssert.Contains(visuales, "Visual_E14_JefeFinalNova");
            CollectionAssert.Contains(visuales, "Visual_E16_RobotPesadoNova_a");
            foreach (AlturasT1552.FilaCapsula fila in AlturasT1552.FilasCapsula())
            {
                Assert.Greater(EscalasHumanos.Objetivo(fila.Visual), 0f, fila.Visual + " tiene que estar en la tabla");
            }
        }

        [Test]
        public void Agente_AlturaDeRespuestaNova_CubreLosRobotsGrandes()
        {
            System.Type tipo = System.Type.GetType("MechaCyberpunk.IA.APie.RespuestaNova, MechaCyberpunk.IA");
            Assert.IsNotNull(tipo, "RespuestaNova");
            System.Reflection.FieldInfo campo = tipo.GetField("AlturaAgente");
            Assert.IsNotNull(campo, "AlturaAgente (pública desde T-1552)");
            float altura = (float)campo.GetRawConstantValue();
            Assert.GreaterOrEqual(altura, EscalasHumanos.AlturaRobotGrande, "cabe el robot de 3.14 m");
            Assert.LessOrEqual(altura, EscalasHumanos.AlturaRobotGrande + 0.5f, "con un margen razonable");
        }

        // ---------- Lo que deja Aplicar (lee los assets ya generados) ----------

        [TestCaseSource(nameof(PrefabsConCambio))]
        public void Aplicado_ElModelo_QuedaEnLaEscalaDeLaEtiqueta(string visual)
        {
            string ruta = CarpetaVisuales + visual + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsTrue(AlturasT1552.LeerEtiqueta(AssetDatabase.GetLabels(prefab), out float baseEsc, out float fin), visual + ": sin etiqueta T1552 (corre AlturasT1552.Aplicar)");
            Transform modelo = prefab.transform.Find("Giro/Modelo");
            Assert.AreEqual(fin, modelo.localScale.y, fin * 0.002f, visual + ": escala del Modelo contra la etiqueta");
            Assert.AreEqual(modelo.localScale.x, modelo.localScale.y, 0.0001f, visual + ": uniforme");
            Assert.AreEqual(Vector3.one, prefab.transform.localScale, visual + ": la raíz no se escala");
            if (!Mathf.Approximately(baseEsc, fin))
            {
                Assert.AreEqual(EscalasHumanos.Factor(visual), fin / baseEsc, 0.002f, visual + ": final / base = factor de la tabla");
            }
        }

        [Test]
        public void Aplicado_CapsulasDeLosAgentes_SiguenLaAlturaNueva()
        {
            foreach (AlturasT1552.FilaCapsula fila in AlturasT1552.FilasCapsula())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fila.Ruta);
                if (prefab == null)
                {
                    continue; // p. ej. E16_b sin prefab de enemigo
                }
                CharacterController cc = prefab.GetComponent<CharacterController>();
                float altura = EscalasHumanos.Objetivo(fila.Visual);
                if (fila.Completa)
                {
                    Assert.AreEqual(EscalasNova.AlturaCapsula(altura), cc.height, 0.01f, prefab.name + ": alto de la cápsula");
                    Assert.AreEqual(EscalasNova.RadioCapsula(altura), cc.radius, 0.01f, prefab.name + ": radio");
                }
                else
                {
                    Assert.GreaterOrEqual(cc.height, EscalasNova.AlturaCapsula(altura) - 0.01f, prefab.name + ": la cápsula no puede quedar más baja que el humano");
                }
                Assert.AreEqual(cc.height * 0.5f, cc.center.y, 0.01f, prefab.name + ": la cápsula parte del piso");
                Assert.IsFalse(prefab.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "NavMeshAgent"), prefab.name + ": usa CharacterController, no NavMeshAgent");
            }
        }

        [Test]
        public void Aplicado_JefeFinalE14_TieneCapsulaYCuerpoDe314()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AlturasT1552.CarpetaIA + "JefeFinalNova_E14.prefab");
            Assert.IsNotNull(prefab);
            CharacterController cc = prefab.GetComponent<CharacterController>();
            Assert.AreEqual(EscalasNova.AlturaCapsula(3.14f), cc.height, 0.01f);
            Component pandillero = prefab.GetComponents<Component>().First(c => c != null && c.GetType().Name == "PandilleroAPie");
            Assert.AreEqual(EscalasNova.Factor(3.14f), new SerializedObject(pandillero).FindProperty("escalaCuerpo").floatValue, 0.001f);
        }

        [Test]
        [Category("Lento")]
        public void Aplicado_TinoEnLaEscena_YaNoTraeEscalaPisada()
        {
            EditorSceneManager.OpenScene(AlturasT1552.RutaEscena, OpenSceneMode.Single);
            int revisados = 0;
            foreach (VisualPersonaje visual in Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
                if (fuente == null || fuente.name != AlturasT1552.VisualTino)
                {
                    continue;
                }
                revisados++;
                Transform modelo = visual.transform.Find("Giro/Modelo");
                Assert.AreEqual(1f, visual.transform.localScale.y, 0.001f, visual.name + ": la raíz de la instancia queda en 1");
                Assert.AreEqual(fuente.transform.Find("Giro/Modelo").localScale.y, modelo.localScale.y, 0.001f, visual.name + ": el Modelo es el del prefab (1.67 ya no manda)");
            }
            Assert.Greater(revisados, 0, "Tino tiene que estar en Ciudad_Prologo");
        }
    }
}
