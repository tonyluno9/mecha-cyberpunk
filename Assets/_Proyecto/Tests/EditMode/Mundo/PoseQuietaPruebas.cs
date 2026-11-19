using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    public class PoseQuietaPruebas
    {
        private GameObject go;

        [TearDown]
        public void Limpiar()
        {
            if (go != null) { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Aplicar_SinControlador_DejaAlwaysAnimateYDevuelveFalso()
        {
            go = new GameObject("Persona");
            Animator a = go.AddComponent<Animator>();
            a.cullingMode = AnimatorCullingMode.CullCompletely;
            PoseQuieta pose = go.AddComponent<PoseQuieta>();
            Assert.IsFalse(pose.Aplicar());
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, a.cullingMode);
        }

        [Test]
        public void Aplicar_SinAnimator_DevuelveFalso()
        {
            go = new GameObject("Persona");
            PoseQuieta pose = go.AddComponent<PoseQuieta>();
            Assert.IsFalse(pose.Aplicar());
        }

        [Test]
        public void Definir_FaseFueraDeRango_SeAcotaDe0A1()
        {
            go = new GameObject("Persona");
            PoseQuieta pose = go.AddComponent<PoseQuieta>();
            pose.Definir("Caminar", 3.5f, true);
            Assert.AreEqual("Caminar", pose.Estado);
            Assert.AreEqual(1f, pose.Fase, 1e-5f);
            Assert.IsTrue(pose.Congelar);
        }
    }
}
