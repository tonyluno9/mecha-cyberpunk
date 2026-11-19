using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1389b: los personajes rigeados en Blender (E15 a E20 y NP07 a NP14) están importados, con su prefab, y metidos en peatones, Nova y SKULLS.</summary>
    public class PersonajesNuevosT1389bPruebas
    {
        private static readonly string[] Todos = PersonajesNuevosT1389b.Variantes;
        private static readonly string[] Humanoides = PersonajesNuevosT1389b.Variantes.Where(v => v != PersonajesNuevosT1389b.PerroVariante).ToArray();
        private static readonly string[] Skulls = PersonajesNuevosT1389b.Skulls;

        private static IEnumerable<string> RutasDe(Object config, string propiedad)
        {
            SerializedProperty lista = new SerializedObject(config).FindProperty(propiedad);
            for (int i = 0; i < lista.arraySize; i++)
            {
                Object o = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null) { yield return AssetDatabase.GetAssetPath(o); }
            }
        }

        [TestCaseSource(nameof(Todos))]
        public void Visual_DeCadaVariante_Existe(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(variante));
            Assert.IsNotNull(prefab, "Visual_" + variante);
            Assert.IsNotNull(prefab.GetComponent<VisualPersonaje>());
            Assert.IsNotNull(prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController, "controlador");
        }

        [TestCaseSource(nameof(Humanoides))]
        public void Visual_DeCadaHumanoide_TieneAvatarHumanoideValido(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(variante));
            Assert.IsNotNull(prefab, "Visual_" + variante);
            Avatar avatar = prefab.GetComponentInChildren<Animator>(true).avatar;
            Assert.IsNotNull(avatar, "avatar");
            Assert.IsTrue(avatar.isValid && avatar.isHuman, variante + ": Avatar Humanoid válido");
        }

        [Test]
        public void Perro_EsGenericConSusClips()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(PersonajesNuevosT1389b.PerroVariante));
            Assert.IsNotNull(prefab);
            Animator a = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsFalse(a.avatar != null && a.avatar.isHuman, "el perro no es Humanoid");
            var nombres = a.runtimeAnimatorController.animationClips.Select(c => c.name).ToList();
            CollectionAssert.Contains(nombres, "Perro_Idle");
            CollectionAssert.Contains(nombres, "Perro_Caminar");
        }

        [Test]
        public void Peaton_IncluyeATodosLosNP07aNP14()
        {
            GameObject peaton = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPeaton);
            PeatonVariado variado = peaton.GetComponent<PeatonVariado>();
            Assert.IsNotNull(variado);
            Assert.Greater(variado.ProbabilidadVariante, 0f);
            var rutas = variado.Variantes.Select(AssetDatabase.GetAssetPath).ToList();
            foreach (string v in PersonajesNuevosT1389b.Peatones)
            {
                CollectionAssert.Contains(rutas, PersonajesNuevosT1389b.RutaVisual(v), v);
            }
            for (int n = 7; n <= 14; n++)
            {
                string codigo = "NP" + n.ToString("00") + "_";
                Assert.IsTrue(rutas.Any(r => r.Contains("/Visual_" + codigo)), "falta un " + codigo);
            }
        }

        [Test]
        public void PeatonVariado_ConProbabilidadUno_ElegirVariante_CreaElVisualElegido()
        {
            GameObject visual = new GameObject("VisualPrueba");
            GameObject raiz = new GameObject("PeatonPrueba");
            try
            {
                PeatonVariado p = raiz.AddComponent<PeatonVariado>();
                p.ConfigurarVariantes(new[] { visual }, 1f);
                p.ElegirVariante(0f, 0.99f);
                Assert.IsNotNull(p.VarianteElegida);
                Assert.AreEqual(raiz.transform, p.VarianteElegida.transform.parent);
            }
            finally
            {
                Object.DestroyImmediate(visual);
                Object.DestroyImmediate(raiz);
            }
        }

        [Test]
        public void Nova_IncluyeCentinelaEliteRobotPesadoYPerro()
        {
            Object config = AssetDatabase.LoadAssetAtPath<Object>(PersonajesNuevosT1389b.RutaConfigNova);
            Assert.IsNotNull(config, "ConfigRespuestaNova");
            var guardias = RutasDe(config, "guardiasExtra").ToList();
            foreach (string v in PersonajesNuevosT1389b.GuardiasNova)
            {
                CollectionAssert.Contains(guardias, PersonajesNuevosT1389b.RutaPrefabEnemigo(v), v);
            }
            var pesados = RutasDe(config, "pesadosExtra").ToList();
            foreach (string v in PersonajesNuevosT1389b.PesadosNova)
            {
                CollectionAssert.Contains(pesados, PersonajesNuevosT1389b.RutaPrefabEnemigo(v), v);
            }
            CollectionAssert.Contains(RutasDe(config, "perros").ToList(), PersonajesNuevosT1389b.RutaPrefabEnemigo(PersonajesNuevosT1389b.PerroVariante));
        }

        [TestCaseSource(nameof(Skulls))]
        public void Skulls_CadaVarianteTienePrefabDeEnemigoConSuVisual(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPrefabEnemigo(variante));
            Assert.IsNotNull(prefab, variante);
            Assert.IsNotNull(prefab.GetComponent("PandilleroAPie"));
            Transform modelo = prefab.transform.Find("Modelo");
            Assert.IsNotNull(modelo);
            Avatar avatar = modelo.GetComponentInChildren<Animator>(true).avatar;
            StringAssert.Contains("/" + PersonajesNuevosT1389b.CodigoDe(variante) + "/", AssetDatabase.GetAssetPath(avatar));
        }
    }
}
