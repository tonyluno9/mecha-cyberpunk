using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-809: los puentes de Neón Alto se suben. Una cápsula con las medidas del CharacterController del jugador (radio 0.35, alto 1.8,
    /// paso 0.35, pendiente 45°) camina con Move() por el camino de cada puente (banqueta, escalera, descanso sobre el pretil, cubierta) y
    /// regresa a la banqueta, sin caerse ni atorarse.
    /// </summary>
    public class PuentesS2nPruebas
    {
        private const float Dt = 1f / 60f;
        private const float Velocidad = 3f;
        private const float Gravedad = -20f;
        private const int PasosMaximosPorTramo = 2000;
        private const float Llegada = 0.25f;
        private const float AltoCubierta = 8.39f;
        private const float AltoBanqueta = 0.2f;
        private const float Tolerancia = 0.35f;

        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private GameObject caminante;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (caminante != null)
            {
                Object.DestroyImmediate(caminante);
            }
            EscenaLugares.Descargar();
        }

        [UnityTest]
        public IEnumerator Puentes_DeLaBanquetaALaCubiertaYDeRegreso_SinCaerNiAtorarse([Values("Puente_Sur", "Puente_Nova")] string nombre)
        {
            GameObject raiz = GameObject.Find("S2n_Puentes");
            Assert.IsNotNull(raiz, "La ciudad trae los puentes de T-809 (corre ColocarS2nPuentes.Colocar)");
            Transform camino = raiz.transform.Find(nombre + "/Camino");
            Assert.IsNotNull(camino, nombre + " trae su camino");
            List<Vector3> puntos = new List<Vector3>();
            for (int i = 0; i < camino.childCount; i++)
            {
                puntos.Add(camino.Find("P" + i).position);
            }

            caminante = new GameObject("Caminante_T809");
            caminante.transform.position = puntos[0] + Vector3.up * 0.05f;
            CharacterController cc = caminante.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.03f;
            Physics.SyncTransforms();
            yield return null;

            float minimo = float.MaxValue;
            List<int> ida = new List<int>();
            for (int i = 1; i < puntos.Count; i++) { ida.Add(i); }
            for (int i = puntos.Count - 2; i >= 0; i--) { ida.Add(i); }
            foreach (int i in ida)
            {
                bool llego = Caminar(cc, puntos[i], ref minimo);
                Assert.IsTrue(llego, nombre + ": se atoró camino a P" + i + " (está en " + caminante.transform.position + ", va a " + puntos[i] + ")");
                Assert.AreEqual(puntos[i].y, caminante.transform.position.y, Tolerancia, nombre + ": en P" + i + " quedó a otra altura (se cayó o flota)");
                if (i == puntos.Count - 1)
                {
                    Assert.AreEqual(AltoCubierta, caminante.transform.position.y, Tolerancia, nombre + ": arriba del puente, sobre la cubierta");
                }
            }
            Assert.AreEqual(AltoBanqueta, caminante.transform.position.y, Tolerancia, nombre + ": de regreso en la banqueta");
            Assert.GreaterOrEqual(minimo, AltoBanqueta - Tolerancia, nombre + ": nunca atravesó el piso");
        }

        // Camina en línea recta (en planta) hacia el punto con gravedad, como el jugador; true si llegó.
        private bool Caminar(CharacterController cc, Vector3 destino, ref float minimo)
        {
            float vy = 0f;
            for (int k = 0; k < PasosMaximosPorTramo; k++)
            {
                Vector3 p = caminante.transform.position;
                Vector3 plano = new Vector3(destino.x - p.x, 0f, destino.z - p.z);
                if (plano.magnitude < Llegada)
                {
                    // Un momento quieto para que asiente en el piso.
                    for (int j = 0; j < 10; j++)
                    {
                        vy = cc.isGrounded ? -1f : vy + Gravedad * Dt;
                        cc.Move(Vector3.up * vy * Dt);
                    }
                    return true;
                }
                vy = cc.isGrounded ? -1f : vy + Gravedad * Dt;
                Vector3 paso = Vector3.ClampMagnitude(plano.normalized * Velocidad * Dt, plano.magnitude) + Vector3.up * vy * Dt;
                cc.Move(paso);
                minimo = Mathf.Min(minimo, caminante.transform.position.y);
            }
            return false;
        }
    }
}
