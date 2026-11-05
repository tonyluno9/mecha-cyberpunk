using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-637: los puestos miden lo que una persona de 1.8 m espera (alto total 1.8 a 2.9 m, mostrador de ~1 m) y no se traslapan.</summary>
    public class EscalaPuestosT637Pruebas
    {
        private static readonly string[] Rutas =
        {
            "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
            "Assets/_Proyecto/Escenas/Ciudad.unity"
        };
        private const float AltoMinimo = 1.8f;
        private const float AltoMaximo = 2.9f;
        private const float MostradorMin = 0.8f;
        private const float MostradorMax = 1.1f;
        private const float Tolerancia = 0.15f;

        private static bool EsPuesto(Transform t)
        {
            string n = t.name;
            bool nombre = n.StartsWith("Puesto_") || n.StartsWith("N03a_") || n.StartsWith("N03b_");
            if (!nombre || n.Contains("Holo") || n.Contains("Luz") || n.Contains("Vapor") || n.StartsWith("Gente")) return false;
            return t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>() != null;
        }

        private static Bounds Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static IEnumerable<Transform> Puestos(Scene escena)
        {
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Where(EsPuesto);
        }

        [Test]
        public void PuestosMidenComoUnaPersonaEsperaYNoSeTraslapan([ValueSource(nameof(Rutas))] string ruta)
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
            try
            {
                Transform[] puestos = Puestos(escena).ToArray();
                Assert.GreaterOrEqual(puestos.Length, 4, ruta);
                var cajas = puestos.Select(Caja).ToArray();
                for (int i = 0; i < puestos.Length; i++)
                {
                    float alto = cajas[i].size.y;
                    Assert.That(alto, Is.InRange(AltoMinimo, AltoMaximo), puestos[i].name + " alto " + alto);
                    Transform mesa = puestos[i].Find("Mesa");
                    if (mesa != null)
                    {
                        float mostrador = Caja(mesa).max.y - puestos[i].position.y;
                        Assert.That(mostrador, Is.InRange(MostradorMin, MostradorMax), puestos[i].name + " mostrador " + mostrador);
                    }
                    for (int j = i + 1; j < puestos.Length; j++)
                    {
                        Bounds a = cajas[i];
                        Bounds b = cajas[j];
                        bool traslape = a.min.x + Tolerancia < b.max.x && b.min.x + Tolerancia < a.max.x
                            && a.min.z + Tolerancia < b.max.z && b.min.z + Tolerancia < a.max.z
                            && a.min.y < b.max.y && b.min.y < a.max.y;
                        Assert.IsFalse(traslape, puestos[i].name + " " + puestos[i].position + " traslapa con " + puestos[j].name + " " + puestos[j].position);
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void PuestosDeTacos_MidenMenosDe2Punto7()
        {
            Scene escena = EditorSceneManager.OpenScene(Rutas[0], OpenSceneMode.Additive);
            try
            {
                Transform[] tacos = Puestos(escena).Where(t => t.name.StartsWith("Puesto_Tacos")).ToArray();
                Assert.GreaterOrEqual(tacos.Length, 4);
                foreach (Transform t in tacos) Assert.Less(Caja(t).size.y, 2.7f, t.name);
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
