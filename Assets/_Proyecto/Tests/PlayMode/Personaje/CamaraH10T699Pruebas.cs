using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-699: la cámara en tercera persona como H10_HUD.png: personaje centrado y en la parte de abajo, ocupando ~1/4 del alto de la pantalla,
    /// cámara un poco alta mirando a lo largo de la calle. Se mide en Ciudad_Prologo frente a la casa (Casa_Puerta).
    /// Con CAPTURAR_S1=1 deja docs/capturas/s1c-T-699-calle.png. Con H10_BARRIDO="altura,distancia,fov;..." solo imprime la fracción de cada combinación.
    /// </summary>
    public class CamaraH10T699Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        public const float FraccionMinima = 0.2f;
        public const float FraccionMaxima = 0.32f;

        private static float MetrosALaCalle => float.Parse(System.Environment.GetEnvironmentVariable("H10_METROS") ?? "9", System.Globalization.CultureInfo.InvariantCulture);
        private static float GiroCalle => float.Parse(System.Environment.GetEnvironmentVariable("H10_GIRO") ?? "0", System.Globalization.CultureInfo.InvariantCulture);

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private static IEnumerator CargarYSituar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForSeconds(0.5f);
            // T-418: la historia pone Calaveras frente a la casa: se apaga el director y se quitan (por nombre).
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (m != null && m.GetType().Name == "DirectorPandillas") m.enabled = false;
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && t.name.StartsWith("CalaveraPunos")) Object.Destroy(t.gameObject);
            }
            // La historia de la demo empieza en la cama: se pasa a la puerta de la casa (la calle).
            IJugador jugador = Registro<IJugador>.Todos[0];
            foreach (IPuntoAparicion p in Registro<IPuntoAparicion>.Todos)
            {
                if (p.Id == "Casa_Puerta")
                {
                    // Un paso hacia la calle (la puerta da a un muro detrás) y mirando a lo largo de ella.
                    Vector3 enLaCalle = p.Pose.position + p.Pose.rotation * Vector3.forward * MetrosALaCalle + Vector3.up * 0.05f;
                    jugador.Colocar(new Pose(enLaCalle, Quaternion.Euler(0f, GiroCalle, 0f)));
                }
            }
            yield return new WaitForSeconds(1.2f);
            Debug.Log("[H10] jugador " + jugador.Raiz.position + " camara " + jugador.Camara.transform.position + " rot " + jugador.Camara.transform.eulerAngles);
        }

        /// <summary>Del jugador: alto y centro (viewport, 0 abajo) del personaje, de la cabeza a los pies, con las mallas animadas.</summary>
        private static void Medir(IJugador jugador, out float fraccion, out float centroX, out float pies, out float cabeza)
        {
            Camera camara = jugador.Camara;
            float minY = float.MaxValue, maxY = float.MinValue, minX = float.MaxValue, maxX = float.MinValue;
            Mesh malla = new Mesh();
            foreach (SkinnedMeshRenderer r in jugador.Raiz.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                r.BakeMesh(malla);
                Matrix4x4 m = Matrix4x4.TRS(r.transform.position, r.transform.rotation, r.transform.lossyScale);
                foreach (Vector3 v in malla.vertices)
                {
                    Vector3 p = camara.WorldToViewportPoint(m.MultiplyPoint3x4(v));
                    if (p.z <= 0f) continue;
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                }
            }
            Object.Destroy(malla);
            Assert.Less(minY, maxY, "El jugador tiene mallas visibles");
            fraccion = maxY - minY;
            centroX = (minX + maxX) * 0.5f;
            pies = minY;
            cabeza = maxY;
        }

        private static void PonerConfig(SerializedObject so, float altura, float distancia, float fov)
        {
            so.FindProperty("alturaNormal").floatValue = altura;
            so.FindProperty("distanciaNormal").floatValue = distancia;
            so.FindProperty("fovNormal").floatValue = fov;
            so.FindProperty("alturaApuntando").floatValue = altura;
            so.FindProperty("distanciaApuntando").floatValue = distancia;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityTest]
        public IEnumerator Barrido_ImprimeLaFraccionDeCadaCombinacion()
        {
            string barrido = System.Environment.GetEnvironmentVariable("H10_BARRIDO");
            if (string.IsNullOrEmpty(barrido)) Assert.Ignore("Sin H10_BARRIDO");
            yield return CargarYSituar();
            IJugador jugador = Registro<IJugador>.Todos[0];
            SerializedObject so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(RutaConfig));
            float a0 = so.FindProperty("alturaNormal").floatValue, d0 = so.FindProperty("distanciaNormal").floatValue, f0 = so.FindProperty("fovNormal").floatValue;
            foreach (string combo in barrido.Split(';'))
            {
                string[] v = combo.Split(',');
                float altura = float.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture);
                float distancia = float.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture);
                float fov = float.Parse(v[2], System.Globalization.CultureInfo.InvariantCulture);
                PonerConfig(so, altura, distancia, fov);
                yield return new WaitForSeconds(0.8f);
                Medir(jugador, out float fr, out float cx, out float pies, out float cabeza);
                Debug.Log("[H10] altura " + altura + " dist " + distancia + " fov " + fov + " -> fraccion " + fr.ToString("F3") + " centroX " + cx.ToString("F3") + " pies " + pies.ToString("F3") + " cabeza " + cabeza.ToString("F3"));
                CapturaS1.FotoCamara("s1c-T-699-barrido-" + altura + "-" + distancia + "-" + fov, jugador.Camara);
            }
            PonerConfig(so, a0, d0, f0);
        }

        [UnityTest]
        public IEnumerator Calle_PersonajeCentradoAbajoYComoUnCuartoDeLaPantalla()
        {
            yield return CargarYSituar();
            IJugador jugador = Registro<IJugador>.Todos[0];
            Camera camara = jugador.Camara;
            Medir(jugador, out float fraccion, out float centroX, out float pies, out float cabeza);
            Debug.Log("[H10] fraccion " + fraccion.ToString("F3") + " centroX " + centroX.ToString("F3") + " pies " + pies.ToString("F3") + " cabeza " + cabeza.ToString("F3"));
            Assert.That(fraccion, Is.InRange(FraccionMinima, FraccionMaxima), "El personaje ocupa ~1/4 del alto de la pantalla");
            Assert.AreEqual(0.5f, centroX, 0.04f, "El personaje queda centrado");
            Assert.Less(pies, 0.2f, "Los pies quedan en la parte de abajo");
            Assert.Less(cabeza, 0.6f, "La cabeza queda por debajo del centro: se ve la calle por delante");
            Vector3 local = jugador.Raiz.InverseTransformPoint(camara.transform.position);
            Assert.AreEqual(0f, local.x, 0.05f, "Caminando la cámara va centrada (lateral 0)");
            Assert.Greater(local.y, 2.5f, "La cámara va alta, por encima de la cabeza");
            CapturaS1.FotoCamara("s1c-T-699-calle", camara);
        }
    }
}
