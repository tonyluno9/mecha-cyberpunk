using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-740 (A9): PJ01_Implantes.fbx entra a la misma escala que PJ01 y sus seis piezas quedan en Visual_PJ01 pesadas a los huesos del modelo, con su textura, ocultas y ligadas a su espacio de IImplantes.</summary>
    public class ImplantesPJ01T740EditPruebas
    {
        private static GameObject Instancia() => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorImplantesPJ01T740.RutaVisual));

        [Test]
        public void Importador_UsaLaMismaEscalaQuePJ01_ElCuerpoYLasPiezasMidenLoMismo()
        {
            var pj = (ModelImporter)AssetImporter.GetAtPath(CreadorPJ01T732.RutaFbx);
            var imp = (ModelImporter)AssetImporter.GetAtPath(CreadorImplantesPJ01T740.RutaFbx);
            Assert.AreEqual(pj.globalScale, imp.globalScale, 0.0001f);
            Assert.AreEqual(Altura(CreadorPJ01T732.RutaFbx), Altura(CreadorImplantesPJ01T740.RutaFbx), 0.02f, "La cadera mide lo mismo: si no, las piezas quedan dentro del cuerpo");
        }

        private static float Altura(string ruta)
        {
            GameObject go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            try
            {
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) { if (t.name == "Hip") { return t.position.y; } }
                return -1f;
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Prefab_TieneLasSeisPiezas_ConSusHuesos_Textura_YOcultas()
        {
            GameObject visual = Instancia();
            try
            {
                var visibles = visual.GetComponentInChildren<ImplantesVisiblesPJ01>(true);
                Assert.IsNotNull(visibles);
                Assert.AreEqual(6, visibles.Piezas.Length);
                Animator animador = visual.GetComponentInChildren<VisualPersonaje>(true).Animador;
                foreach (ImplantesVisiblesPJ01.Pieza p in visibles.Piezas)
                {
                    var r = p.objeto.GetComponent<SkinnedMeshRenderer>();
                    Assert.IsFalse(p.objeto.activeSelf, p.objeto.name + " oculta hasta que haya implante");
                    Assert.AreEqual(78, r.bones.Length, p.objeto.name + ": los 78 huesos de PJ01");
                    foreach (Transform hueso in r.bones) { Assert.IsTrue(hueso.IsChildOf(animador.transform), p.objeto.name + ": el hueso " + hueso.name + " es del modelo del jugador"); }
                    Assert.IsNotNull(r.sharedMaterial.GetTexture("_BaseMap"), p.objeto.name + " con su textura");
                    Assert.AreEqual(animador.GetBoneTransform(HumanBodyBones.Hips).root == animador.transform.root, true);
                }
                (string, EspacioImplante, int)[] esperado =
                {
                    ("IMP01_LVL1", EspacioImplante.Piernas, 1), ("IMP01_LVL2", EspacioImplante.Piernas, 2), ("IMP02_LVL1", EspacioImplante.Brazos, 1),
                    ("IMP02_LVL2", EspacioImplante.Brazos, 2), ("IMP03_LVL1", EspacioImplante.Ojos, 1), ("IMP03_LVL2", EspacioImplante.Ojos, 2),
                };
                foreach ((string nombre, EspacioImplante espacio, int nivel) in esperado)
                {
                    bool hay = false;
                    foreach (ImplantesVisiblesPJ01.Pieza p in visibles.Piezas) { hay |= p.objeto.name == nombre && p.espacio == espacio && p.nivel == nivel; }
                    Assert.IsTrue(hay, nombre + " ligada a " + espacio + " nivel " + nivel);
                }
            }
            finally { Object.DestroyImmediate(visual); }
        }
    }
}
