using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1204: config de animación de escalera y fórmula del tiempo del clip.</summary>
    public class ConfigAnimacionEscaleraPruebas
    {
        private const float Metros = 0.9f;
        private const float Largo = 1.2f;

        [Test]
        public void HayClips_SinClips_EsFalso()
        {
            ConfigAnimacionEscalera config = ScriptableObject.CreateInstance<ConfigAnimacionEscalera>();
            Assert.IsFalse(config.HayClips);
            Assert.IsNull(config.Subir);
            Assert.IsNull(config.Bajar);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void HayClips_ConUnClip_EsVerdadero()
        {
            ConfigAnimacionEscalera config = ScriptableObject.CreateInstance<ConfigAnimacionEscalera>();
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(config);
            so.FindProperty("subir").objectReferenceValue = new AnimationClip();
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(config.HayClips);
            Object.DestroyImmediate(config.Subir);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void TiempoClip_Progreso_DaFraccionDelClip()
        {
            Assert.AreEqual(0f, AnimacionEscalera.TiempoClip(0f, Metros, Largo), 1e-4f);
            Assert.AreEqual(Largo * 0.5f, AnimacionEscalera.TiempoClip(Metros * 0.5f, Metros, Largo), 1e-4f);
            Assert.AreEqual(Largo * 0.5f, AnimacionEscalera.TiempoClip(Metros * 1.5f, Metros, Largo), 1e-4f);
        }
    }
}
