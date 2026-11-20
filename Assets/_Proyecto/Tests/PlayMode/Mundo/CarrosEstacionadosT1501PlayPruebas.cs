using System.Collections;
using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1501: tras 3 s de juego ningún carro estacionado queda con las llantas bajo el piso.</summary>
    public class CarrosEstacionadosT1501PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float ToleranciaSuspension = 0.10f;   // m: la suspensión comprime el carro un poco en reposo

        [UnityTest]
        public IEnumerator CarrosEstacionados_TrasTresSegundosNingunoBajoElPiso()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(3f);

            CarroEstacionado[] carros = Object.FindObjectsByType<CarroEstacionado>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.Greater(carros.Length, 0, "debe haber carros estacionados");
            int medidos = 0;
            foreach (CarroEstacionado carro in carros)
            {
                foreach (string nombre in new[] { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" })
                {
                    Transform rueda = carro.transform.Find(nombre);
                    if (rueda == null)
                    {
                        continue;
                    }
                    // Fondo exacto por vértices: el AABB de un Renderer con la llanta rodada sale inflado (hasta 1.4 radios) y parecía hundida.
                    MeshFilter[] mallas = rueda.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null).ToArray();
                    if (mallas.Length == 0)
                    {
                        continue;
                    }
                    // Llanta como cilindro (centro de la malla menos su radio): las mallas no son legibles y el AABB girado se infla.
                    float fondoLlanta = float.MaxValue;
                    Vector3 centroLlanta = Vector3.zero;
                    foreach (MeshFilter f in mallas)
                    {
                        Bounds local = f.sharedMesh.bounds;
                        Vector3 e = Vector3.Scale(local.extents, f.transform.lossyScale);
                        float radio = Mathf.Max(Mathf.Abs(e.x), Mathf.Max(Mathf.Abs(e.y), Mathf.Abs(e.z)));
                        Vector3 c = f.transform.TransformPoint(local.center);
                        if (c.y - radio < fondoLlanta) { fondoLlanta = c.y - radio; centroLlanta = c; }
                    }
                    Bounds b = new Bounds(new Vector3(centroLlanta.x, fondoLlanta, centroLlanta.z), Vector3.zero);
                    Vector3 origen = new Vector3(b.center.x, b.min.y + 1f, b.center.z);
                    RaycastHit[] hits = Physics.RaycastAll(origen, Vector3.down, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                        .Where(h => !h.collider.transform.IsChildOf(carro.transform)).OrderBy(h => h.distance).ToArray();
                    if (hits.Length == 0)
                    {
                        continue;
                    }
                    medidos++;
                    Assert.GreaterOrEqual(b.min.y, hits[0].point.y - ToleranciaSuspension,
                        carro.name + "/" + nombre + " quedó bajo el piso (llanta " + b.min.y.ToString("F3") + ", piso " + hits[0].point.y.ToString("F3") + ")");
                }
            }
            Assert.Greater(medidos, 0, "se midió al menos una llanta");
        }
    }
}
