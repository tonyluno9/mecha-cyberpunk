using System.Collections;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Flashback.unity (T-090): carga sola, expone su punto de aparición a pie sobre piso libre, está cerrada por todos lados y no deja errores.</summary>
    public class FlashbackPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Flashback.unity";
        private const string IdInicio = "Flashback_Inicio";
        private const float AlturaOjos = 1.7f;
        private const float RadioCuerpo = 0.4f;
        private const float AlturaCuerpo = 1.8f;
        private const float AlcanceRayos = 20f;
        private const int CantidadRayos = 36;
        private const float GradosPorVuelta = 360f;
        private const float AlturaPrueba = 1f;
        private const int MaximoRayosPorLaPuerta = 6;     // la puerta de 1.8 m vista desde 2 m abarca unos 48°

        private GameObject camaraObjeto;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        private static IPuntoAparicion Inicio()
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == IdInicio)
                {
                    return punto;
                }
            }
            Assert.Fail("Falta el punto de aparición " + IdInicio);
            return null;
        }

        [UnityTest]
        public IEnumerator Flashback_AlCargar_PublicaElPuntoDeAparicionAPieSinErrores()
        {
            yield return Cargar();
            IPuntoAparicion inicio = Inicio();
            Assert.AreEqual(ModoJugador.APie, inicio.Modo);
            Assert.AreEqual(0, System.Array.FindAll(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), c => c.enabled).Length, "La escena no trae cámara: la pone el jugador de Tony");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Flashback_ElPuntoDeAparicion_TienePisoYEspacioLibre()
        {
            yield return Cargar();
            Pose pose = Inicio().Pose;
            Assert.IsTrue(Physics.Raycast(pose.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit suelo, 1f, ~0, QueryTriggerInteraction.Ignore), "Hay piso bajo el punto");
            Assert.AreEqual(pose.position.y, suelo.point.y, 0.1f);
            Vector3 abajo = pose.position + Vector3.up * (RadioCuerpo + 0.05f);
            Vector3 arriba = pose.position + Vector3.up * (AlturaCuerpo - RadioCuerpo);
            Assert.IsFalse(Physics.CheckCapsule(abajo, arriba, RadioCuerpo, ~0, QueryTriggerInteraction.Ignore), "El cuerpo del jugador cabe en el punto");
        }

        [UnityTest]
        public IEnumerator Flashback_36RayosDesdeElPuntoChocanConUnMuro_EnElCuartoYSalvoPorLaPuerta()
        {
            yield return Cargar();
            Vector3 origen = Inicio().Pose.position + Vector3.up * AlturaPrueba;
            int sinChoque = 0;
            for (int i = 0; i < CantidadRayos; i++)
            {
                Vector3 direccion = Quaternion.Euler(0f, i * GradosPorVuelta / CantidadRayos, 0f) * Vector3.forward;
                if (!Physics.Raycast(origen, direccion, AlcanceRayos, ~0, QueryTriggerInteraction.Ignore))
                {
                    sinChoque++;
                }
            }
            Assert.LessOrEqual(sinChoque, MaximoRayosPorLaPuerta, "Solo la puerta (" + MaximoRayosPorLaPuerta + " rayos como máximo) deja pasar rayos; sin choque: " + sinChoque);
        }

        [UnityTest]
        public IEnumerator Flashback_Captura()
        {
            yield return Cargar();
            Pose pose = Inicio().Pose;
            camaraObjeto = new GameObject("CamaraCaptura");
            Camera camara = camaraObjeto.AddComponent<Camera>();
            camaraObjeto.transform.SetPositionAndRotation(pose.position + Vector3.up * AlturaOjos, pose.rotation);
            camara.nearClipPlane = 0.1f;
            yield return null;
            CapturaPruebas.Guardar(camara, "docs/capturas/T-090-1.png");
        }
    }
}
