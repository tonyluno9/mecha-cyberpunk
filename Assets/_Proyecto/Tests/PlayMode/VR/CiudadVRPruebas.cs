using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR1 en la ciudad real (Ciudad_Prologo) con el simulador: el rig se arma en el jugador de la escena, el visor solo ve su cámara y se camina.</summary>
    public class CiudadVRPruebas : BaseJugadorVR
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            // La partida de Tony no se toca: guardado y opciones en una carpeta temporal.
            carpeta = Path.Combine(Path.GetTempPath(), "vr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
            FlujoPartida.TituloEnEditor = true;
        }

        public override void TearDown()
        {
            FlujoPartida.TituloEnEditor = false;
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator EnLaCiudadElRigSeArmaYSeCamina()
        {
            // La escena trae errores propios que no son de VR (p. ej. [Arena] JaulaClandestino): aquí solo se mide el rig.
            LogAssert.ignoreFailingMessages = true;
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            PonerMano(false, new Vector3(-0.2f, 1.2f, 0.3f), Quaternion.identity);
            PonerMano(true, new Vector3(0.2f, 1.2f, 0.3f), Quaternion.identity);
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            float fin = Time.realtimeSinceStartup + 60f;
            while ((AdaptadorVR.Actual == null || !AdaptadorVR.Actual.Armado) && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            AdaptadorVR a = AdaptadorVR.Actual;
            Assert.IsNotNull(a, "En la ciudad el jugador a pie debe llevar el rig VR.");
            Assert.IsTrue(a.Armado);
            yield return Segundos(3f);

            Assert.AreEqual(StereoTargetEyeMask.Both, a.Camara.stereoTargetEye);
            foreach (Camera c in Camera.allCameras)
            {
                if (c != a.Camara && c.targetTexture == null)
                {
                    Assert.AreEqual(StereoTargetEyeMask.None, c.stereoTargetEye, c.name + " no debe verse en el visor.");
                }
            }

            int cuadros = 0;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 3f)
            {
                cuadros++;
                yield return null;
            }
            Debug.Log("[VR] Ciudad con rig VR (batchmode, sin visor): " + (cuadros / 3f).ToString("0") + " cuadros/s.");

            Vector3 inicio = a.transform.position;
            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(1.5f);
            Stick(false, Vector2.zero);
            float avance = Vector3.Distance(inicio, a.transform.position);
            Debug.Log("[VR] En la ciudad el stick avanzó " + avance.ToString("0.00") + " m en 1.5 s (control activo: " + a.Controlador.ControlActivo + ").");
            if (a.Controlador.ControlActivo)
            {
                Assert.Greater(avance, 1f, "Con el control activo el stick debe caminar en la ciudad.");
            }
        }
    }
}
