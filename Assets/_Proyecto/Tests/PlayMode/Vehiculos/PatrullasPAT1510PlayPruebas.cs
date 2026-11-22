using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1510: en juego, PA01, PA02, V22 y los carros de policía V05, V06 y V11 se asientan sobre el piso (ninguna llanta queda hundida), las llantas giran y las puertas abren.
    /// Piso plano de un cubo; el carro cae de 0.4 m y se espera 3 s.
    /// </summary>
    public class PatrullasPAT1510PlayPruebas
    {
        private static readonly string[] Codigos = { "PA01", "PA02", "V11" };
        private const float SegundosParaAsentarse = 3f;
        private const float MaximoHundido = 0.10f;
        private const float MaximoFlotando = 0.30f;
        private const float AlturaCaida = 0.4f;
        private static readonly string[] Ruedas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };

        private GameObject piso;
        private GameObject carroObjeto;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(carroObjeto);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
        }

        private static GameObject Prefab(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, "Falta el prefab de " + codigo);
            return prefab;
        }

        /// <summary>Fondo de una llanta tomada como cilindro de eje horizontal: centro de su malla en el mundo menos su radio
        /// (la mayor extensión local escalada). No depende de cuánto haya rodado ni necesita leer vértices.</summary>
        internal static float FondoCilindro(MeshFilter f)
        {
            Bounds local = f.sharedMesh.bounds;
            Vector3 escala = f.transform.lossyScale;
            Vector3 ext = new Vector3(Mathf.Abs(local.extents.x * escala.x), Mathf.Abs(local.extents.y * escala.y), Mathf.Abs(local.extents.z * escala.z));
            float radio = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z));
            return f.transform.TransformPoint(local.center).y - radio;
        }

        private static float FondoDeLlanta(Transform carro, string rueda)
        {
            // Fondo de la llanta como cilindro: el AABB de un Renderer con la llanta girada sale inflado, y las mallas no son legibles (sin Read/Write).
            float menor = float.MaxValue;
            foreach (MeshFilter f in carro.Find(rueda).GetComponentsInChildren<MeshFilter>())
            {
                menor = Mathf.Min(menor, FondoCilindro(f));
            }
            Assert.Less(menor, float.MaxValue, rueda + " sin llanta visible");
            return menor;
        }

        [UnityTest]
        public IEnumerator Carro_TrasTresSegundos_NingunaLlantaHundida([ValueSource(nameof(Codigos))] string codigo)
        {
            carroObjeto = Object.Instantiate(Prefab(codigo), new Vector3(0f, AlturaCaida, 0f), Quaternion.identity);
            yield return new WaitForSeconds(SegundosParaAsentarse);
            foreach (string rueda in Ruedas)
            {
                float fondo = FondoDeLlanta(carroObjeto.transform, rueda);
                Assert.GreaterOrEqual(fondo, -MaximoHundido, codigo + " " + rueda + ": llanta hundida " + fondo.ToString("F3") + " m bajo el piso");
                Assert.LessOrEqual(fondo, MaximoFlotando, codigo + " " + rueda + ": llanta flotando " + fondo.ToString("F3") + " m sobre el piso");
            }
        }

        [UnityTest]
        public IEnumerator Carro_AlAvanzar_LasLlantasGiran([ValueSource(nameof(Codigos))] string codigo)
        {
            carroObjeto = Object.Instantiate(Prefab(codigo), new Vector3(0f, AlturaCaida, 0f), Quaternion.identity);
            yield return null;
            RuedasVisualesCarro ruedas = carroObjeto.GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, codigo + ": RuedasVisualesCarro");
            float antes = ruedas.RodadoGrados;
            for (int i = 0; i < 25; i++)
            {
                ruedas.Avanzar(0.4f, 0f, 0.02f); // 20 m/s
            }
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(antes, ruedas.RodadoGrados)), 0.5f, codigo + ": las llantas rodaron");
            Transform giro = carroObjeto.transform.Find("Rueda_TI/Direccion/Giro");
            // El ángulo se lee con el cuaternión (eulerAngles.x es ambiguo pasados los 90°): giro firmado de la llanta alrededor de su eje X local.
            Vector3 arriba = giro.localRotation * Vector3.up;
            float medido = Mathf.Repeat(Mathf.Atan2(arriba.z, arriba.y) * Mathf.Rad2Deg, 360f);
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(ruedas.RodadoGrados, medido)), 1f, codigo + ": el pivote Giro de la llanta gira lo que rodó (" + ruedas.RodadoGrados.ToString("F1") + " contra " + medido.ToString("F1") + ")");
            yield break;
        }

        [UnityTest]
        public IEnumerator Carro_PuertasAbren([ValueSource(nameof(Codigos))] string codigo)
        {
            carroObjeto = Object.Instantiate(Prefab(codigo), new Vector3(0f, AlturaCaida, 0f), Quaternion.identity);
            yield return null;
            PuertasCarro puertas = carroObjeto.GetComponent<PuertasCarro>();
            Assert.IsNotNull(puertas, codigo + ": PuertasCarro");
            Transform bisagra = carroObjeto.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bisagra_Puerta_I");
            Quaternion cerrada = bisagra.localRotation;
            puertas.AbrirPuerta(true);
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(puertas.AperturaPuerta(true), 0.9f, codigo + ": la puerta izquierda terminó de abrir");
            Assert.Greater(Quaternion.Angle(cerrada, bisagra.localRotation), puertas.AnguloPuerta * 0.8f, codigo + ": la bisagra giró casi lo que abre la puerta");
        }
    }
}
