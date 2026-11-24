using System.Collections;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>T88: la ventana del PC muestra un solo ojo del visor. Regla pura y el componente vivo sin visor.</summary>
    public class T88VentanaPCVRPruebas
    {
        private static readonly int Izq = (int)XRMirrorViewBlitMode.LeftEye;
        private static readonly int Lado = (int)XRMirrorViewBlitMode.SideBySide;

        [Test]
        public void DebeAplicar_VRRealCorriendoOtroModo_True()
        {
            Assert.IsTrue(ReglaVentanaPCVR.DebeAplicar(true, false, true, Lado, Izq));
            Assert.IsTrue(ReglaVentanaPCVR.DebeAplicar(true, false, true, (int)XRMirrorViewBlitMode.Default, Izq));
        }

        [Test]
        public void DebeAplicar_YaEnUnOjo_False()
        {
            Assert.IsFalse(ReglaVentanaPCVR.DebeAplicar(true, false, true, Izq, Izq));
        }

        [Test]
        public void DebeAplicar_SinVROSimuladoODisplayParado_False()
        {
            Assert.IsFalse(ReglaVentanaPCVR.DebeAplicar(false, false, true, Lado, Izq));
            Assert.IsFalse(ReglaVentanaPCVR.DebeAplicar(true, true, true, Lado, Izq));
            Assert.IsFalse(ReglaVentanaPCVR.DebeAplicar(true, false, false, Lado, Izq));
        }

        [Test]
        public void ModoDeseado_EsOjoIzquierdo()
        {
            Assert.AreEqual(Izq, VentanaPCVR.ModoDeseado);
            Assert.AreNotEqual(Lado, VentanaPCVR.ModoDeseado);
        }

        [UnityTest]
        public IEnumerator Componente_ExisteYSinVisorNoAplicaNada()
        {
            yield return null;
            VentanaPCVR ventana = VentanaPCVR.Instancia;
            if (ventana == null)
            {
                var objeto = new GameObject("VentanaPCVR_Prueba");
                ventana = objeto.AddComponent<VentanaPCVR>();
            }
            Assert.IsNotNull(VentanaPCVR.Instancia);
            int antes = ventana.VecesAplicado;
            ventana.Revisar();
            yield return null;
            // Sin visor (pruebas en batch) no hay display XR corriendo: no debe cambiar nada ni lanzar errores.
            Assert.AreEqual(antes, ventana.VecesAplicado);
        }
    }
}
