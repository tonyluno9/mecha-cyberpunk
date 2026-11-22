using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1507 (patrullas desde T-1510): los carros y motos revisados en Blender vienen en metros: sin escala vieja (x4.5), largo real, llantas en el piso,
    /// rin de policía en V05, V06 y V11, patrullas con puertas y V22 sin mallas faltantes.
    /// </summary>
    public class CarrosRevisadosT1507Pruebas
    {
        private static readonly string[] Carros = CarrosRevisadosT1507.Carros;
        private static readonly string[] Motos = { "C19", "V07", "V08" };
        private static readonly string[] Policias = CarrosRevisadosT1507.Policia;
        private static readonly string[] Patrullas =
        {
            "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/PatrullaCamioneta.prefab",
        };
        private const float LargoMinimoCarro = 4.2f;
        private const float LargoMaximoCarro = 4.75f;
        private const float MargenPiso = 0.06f;
        private const float MargenEscala = 0.01f;

        private static GameObject Cargar(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta " + ruta);
            return prefab;
        }

        private static GameObject Carro(string codigo) => Cargar(CarrosRevisadosT1507.RutaPrefab(codigo));

        private static Bounds LimitesEn(Transform objeto, Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 m = marco.worldToLocalMatrix * f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 p = m.MultiplyPoint3x4(v);
                    if (primero) { b = new Bounds(p, Vector3.zero); primero = false; }
                    else { b.Encapsulate(p); }
                }
            }
            return b;
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void Carros_SinEscalaVieja_VisualYLlantasEnUno([ValueSource(nameof(Carros))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            Assert.AreEqual(1f, raiz.Find("Visual_T511").lossyScale.x, MargenEscala, codigo + ": Visual_T511 sin escala");
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                Transform modelo = raiz.Find(nombre + "/Direccion/Giro/Modelo");
                Assert.IsNotNull(modelo, codigo + " " + nombre);
                Assert.AreEqual(1f, modelo.lossyScale.x, MargenEscala, codigo + " " + nombre + ": llanta sin escala");
            }
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void Carros_MidenLoRealDeLosViejos([ValueSource(nameof(Carros))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            Bounds b = LimitesEn(raiz.Find("Visual_T511/Carroceria"), raiz);
            Assert.That(b.size.z, Is.InRange(LargoMinimoCarro, LargoMaximoCarro), codigo + ": largo " + b.size.z.ToString("F2"));
            Assert.That(b.size.x, Is.InRange(1.8f, 2.7f), codigo + ": ancho " + b.size.x.ToString("F2"));
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void Carros_LasCuatroLlantasTocanElPiso([ValueSource(nameof(Carros))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                Bounds b = LimitesEn(raiz.Find(nombre + "/Direccion/Giro/Modelo"), raiz);
                Assert.AreEqual(0f, b.min.y, MargenPiso, codigo + " " + nombre + ": la llanta toca y = 0 (ni hundida ni flotando)");
            }
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void Carros_ConPuertasYCofre([ValueSource(nameof(Carros))] string codigo)
        {
            GameObject prefab = Carro(codigo);
            Assert.IsNotNull(prefab.GetComponent<PuertasCarro>(), codigo + ": PuertasCarro");
            Transform visual = prefab.transform.Find("Visual_T511");
            Assert.IsNotNull(visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Puerta_I"), codigo + ": Puerta_I");
            Assert.IsNotNull(visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Puerta_D"), codigo + ": Puerta_D");
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void Motos_MidenDosMetros_SinEscalaVieja([ValueSource(nameof(Motos))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            Transform visual = raiz.Find("Visual_T511/Visual");
            Assert.IsNotNull(visual, codigo + " Visual");
            Assert.AreEqual(1f, visual.lossyScale.x, MargenEscala, codigo + ": sin escala");
            Bounds b = LimitesEn(raiz.Find("Visual_T511"), raiz);
            Assert.That(b.size.z, Is.InRange(1.9f, 2.3f), codigo + ": largo " + b.size.z.ToString("F2"));
            Assert.AreEqual(0f, b.min.y, MargenPiso, codigo + ": ruedas en el piso");
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void Policia_LlantasConRinDePolicia([ValueSource(nameof(Policias))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                MeshRenderer dibujo = raiz.Find(nombre + "/Direccion/Giro/Modelo").GetComponent<MeshRenderer>();
                string[] materiales = dibujo.sharedMaterials.Select(m => m != null ? m.name : "(nulo)").ToArray();
                CollectionAssert.AreEquivalent(new[] { "Llanta_Hule", "Rin_AceroNegro", "Rin_Cromo" }, materiales, codigo + " " + nombre);
            }
        }

        [Test]
        public void IndicesRin_MallaSinTresSubmallas_Nulo()
        {
            Assert.IsNull(CarrosRevisadosT1507.IndicesRin(new Mesh()), "una malla de una submalla no es un rin");
        }

        [Test]
        public void Patrullas_ConPuertasYLlantasEnElPiso([ValueSource(nameof(Patrullas))] string ruta)
        {
            GameObject patrulla = Cargar(ruta);
            Assert.IsNotNull(patrulla.GetComponent<MechaCyberpunk.Trafico.Persecucion.PatrullaMundo>(), "PatrullaMundo sigue en la raíz");
            Assert.IsNotNull(patrulla.GetComponent<PuertasCarro>(), "PuertasCarro");
            Transform[] todos = patrulla.GetComponentsInChildren<Transform>(true);
            Assert.IsTrue(todos.Any(t => t.name == "Puerta_I"), "Puerta_I");
            Assert.IsTrue(todos.Any(t => t.name == "Puerta_D"), "Puerta_D");
            Assert.AreEqual(1f, patrulla.transform.Find("Visual_T511").lossyScale.x, MargenEscala, "sin escala vieja");
            Assert.IsNotNull(patrulla.transform.Find("LuzRoja"), "Sirena roja");
            Assert.IsNotNull(patrulla.transform.Find("LuzAzul"), "Sirena azul");
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                // T-1510: las patrullas usan el cuerpo de PA01/PA02 (su llanta lleva el rin en la textura); el rin de policía de V05/V06/V11 se prueba en Policia_LlantasConRinDePolicia.
                Bounds b = LimitesEn(patrulla.transform.Find(nombre + "/Direccion/Giro/Modelo"), patrulla.transform);
                Assert.AreEqual(0f, b.min.y, MargenPiso, nombre + ": llanta en el piso");
            }
        }

        [Test]
        [Ignore("T-1644: los carros civiles vuelven al modelo original de Tripo (sin piezas de Blender)")]
        public void V22_SinMallasFaltantesYConParabrisas()
        {
            Transform raiz = Carro("V22").transform;
            foreach (MeshFilter f in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.IsNotNull(f.sharedMesh, "malla faltante en " + f.name);
            }
            Assert.IsTrue(raiz.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Cristal_Parabrisas"), "Cristal_Parabrisas");
        }
    }
}
