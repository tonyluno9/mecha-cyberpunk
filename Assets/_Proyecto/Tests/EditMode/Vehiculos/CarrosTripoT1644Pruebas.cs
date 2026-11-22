using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1644 (T24): los carros civiles y las motos llevan el modelo original de Tripo (una malla, sin piezas de Blender), con su física, sus cajas de choque a la medida, LucesCarro y
    /// lo necesario para robarlos; la patrulla buena (PA01, Patrulla.prefab) se queda con sus puertas. Corren después de CarrosTripoT1644.Aplicar.
    /// </summary>
    public class CarrosTripoT1644Pruebas
    {
        private static readonly string[] Carros = CarrosTripoT1644.Carros.ToArray();
        private static readonly string[] Motos = CarrosTripoT1644.Motos.ToArray();
        private const float LargoMinimo = 3.5f;

        private static GameObject Prefab(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarrosTripoT1644.RutaPrefab(codigo));
            Assert.IsNotNull(prefab, "falta el prefab de " + codigo);
            return prefab;
        }

        private static Mesh MallaDelFbx(string codigo)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CarrosTripoT1644.De(codigo).RutaFbx);
            Assert.IsNotNull(fbx, codigo + ": falta el FBX de Tripo " + CarrosTripoT1644.De(codigo).RutaFbx);
            return fbx.GetComponentsInChildren<MeshFilter>(true).First(m => m.sharedMesh != null).sharedMesh;
        }

        [Test]
        public void Carro_VisualEsLaMallaOriginalDeTripo([ValueSource(nameof(Carros))] string codigo)
        {
            Transform modelo = Prefab(codigo).transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/" + CarrosTripoT1644.NombreModelo);
            Assert.IsNotNull(modelo, codigo + ": falta " + CarrosTripoT1644.NombreModelo);
            MeshFilter filtro = modelo.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null);
            Assert.IsNotNull(filtro, codigo + ": sin malla");
            Assert.AreSame(MallaDelFbx(codigo), filtro.sharedMesh, codigo + ": la malla no es la del FBX de Tripo");
            MeshRenderer dibujo = filtro.GetComponent<MeshRenderer>();
            Assert.IsNotNull(dibujo, codigo + ": sin renderer");
            Assert.IsTrue(dibujo.enabled, codigo + ": renderer apagado");
            Assert.IsTrue(dibujo.sharedMaterials.All(m => m != null), codigo + ": material faltante");
            StringAssert.StartsWith("Universal Render Pipeline", dibujo.sharedMaterials[0].shader.name, codigo + ": material URP");
        }

        [Test]
        public void Carro_SinPiezasDeBlender([ValueSource(nameof(Carros))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Transform[] todos = prefab.GetComponentsInChildren<Transform>(true);
            Assert.IsFalse(todos.Any(t => t.name.StartsWith("Cristal_") || t.name.StartsWith("Puerta_") || t.name.StartsWith("Bisagra_") || t.name == "Cofre" || t.name.StartsWith("Fija_")),
                codigo + ": quedan piezas de Blender");
            Assert.IsFalse(todos.Any(t => t.name == "Modelo" && t.parent != null && t.parent.name == "Giro"), codigo + ": quedan llantas por piezas");
        }

        [Test]
        public void Carro_ConFisicaManejoYRobo([ValueSource(nameof(Carros))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Rigidbody cuerpo = prefab.GetComponent<Rigidbody>();
            Assert.IsNotNull(cuerpo, codigo + ": Rigidbody");
            Assert.Greater(cuerpo.mass, 100f, codigo + ": masa");
            CarroViejo carro = prefab.GetComponent<CarroViejo>();
            Assert.IsNotNull(carro, codigo + ": CarroViejo (manejo, daño y explosión)");
            Assert.IsNotNull(prefab.GetComponent<CarroEstacionado>(), codigo + ": CarroEstacionado (robo, alarma y calor)");
            Assert.IsNotNull(prefab.GetComponent<PuertasCarro>(), codigo + ": PuertasCarro tolera no tener puertas");
            SerializedObject so = new SerializedObject(carro);
            Assert.IsNotNull(so.FindProperty("asiento").objectReferenceValue, codigo + ": asiento");
            Assert.IsNotNull(so.FindProperty("puntoSubida").objectReferenceValue, codigo + ": punto de subida");
            foreach (string nombre in new[] { "ruedaDelanteraIzquierda", "ruedaDelanteraDerecha", "ruedaTraseraIzquierda", "ruedaTraseraDerecha" })
            {
                Assert.IsNotNull(so.FindProperty(nombre).objectReferenceValue, codigo + ": ancla " + nombre);
            }
        }

        [Test]
        public void Carro_CajaDeChoqueAjustadaALaMalla([ValueSource(nameof(Carros))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Transform carroceria = prefab.transform.Find("Carroceria");
            Assert.IsNotNull(carroceria, codigo + ": caja Carroceria");
            BoxCollider caja = carroceria.GetComponent<BoxCollider>();
            Assert.IsNotNull(caja, codigo + ": BoxCollider");
            float largo = caja.size.z * carroceria.lossyScale.z;
            Assert.That(largo, Is.InRange(LargoMinimo, CarrosTripoT1644.LargoMaximoDe(codigo)), codigo + ": largo de la caja " + largo.ToString("F2"));

            Bounds malla = LimitesMalla(prefab.transform.Find(CreadorCarrosPiezasT742.NombreVisual));
            float escala = prefab.transform.lossyScale.z;
            Assert.AreEqual(malla.size.z * escala, largo / 0.98f, 0.35f, codigo + ": la caja mide lo que la malla");
            Assert.AreEqual(0f, malla.min.y * prefab.transform.lossyScale.y, 0.05f, codigo + ": el fondo de la malla va en y = 0");
        }

        [Test]
        public void Carro_ConLucesDeFarosYCalaveras([ValueSource(nameof(Carros))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Assert.IsNotNull(prefab.GetComponent<LucesCarro>(), codigo + ": LucesCarro (faros delanteros y calaveras, de noche como la patrulla buena)");
            Assert.IsNull(prefab.GetComponent<ManejoMoto>(), codigo + ": no es moto");
        }

        [Test]
        public void Carro_MismaFisicaQueLaPatrullaBuena([ValueSource(nameof(Carros))] string codigo)
        {
            Rigidbody patrulla = Prefab(CarrosTripoT1644.PatrullaBuena).GetComponent<Rigidbody>();
            Rigidbody carro = Prefab(codigo).GetComponent<Rigidbody>();
            Assert.AreEqual(patrulla.angularDamping, carro.angularDamping, 0.001f, codigo + ": amortiguamiento angular");
            Assert.AreEqual(patrulla.collisionDetectionMode, carro.collisionDetectionMode, codigo + ": detección de colisión");
            Assert.AreEqual(patrulla.interpolation, carro.interpolation, codigo + ": interpolación");
        }

        [Test]
        public void Moto_LlevaLaMallaOriginalDeTripoSinLlantasRU02([ValueSource(nameof(Motos))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Transform visual = prefab.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/Visual");
            Assert.IsNotNull(visual, codigo + ": Visual");
            Assert.AreSame(MallaDelFbx(codigo), visual.GetComponent<MeshFilter>().sharedMesh, codigo + ": malla del FBX de Tripo");
            Assert.IsNull(prefab.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/" + CreadorMotosPiezasT743.NombreRuedas), codigo + ": sin llantas RU02");
            ManejoMoto manejo = prefab.GetComponent<ManejoMoto>();
            Assert.IsNotNull(manejo, codigo + ": ManejoMoto");
            Assert.AreSame(MallaDelFbx(codigo), new SerializedObject(manejo).FindProperty("mallaCompleta").objectReferenceValue, codigo + ": malla completa");
            Assert.IsNotNull(prefab.GetComponent<Rigidbody>(), codigo + ": Rigidbody");
            Assert.IsNotNull(prefab.GetComponent<CarroEstacionado>(), codigo + ": se puede robar");
        }

        [Test]
        public void PatrullaBuena_SigueConSusPuertasYSuTorreta()
        {
            GameObject pa01 = Prefab(CarrosTripoT1644.PatrullaBuena);
            Transform[] todos = pa01.GetComponentsInChildren<Transform>(true);
            Assert.IsTrue(todos.Any(t => t.name == "Puerta_I"), "Puerta_I de PA01");
            Assert.IsTrue(todos.Any(t => t.name == "Puerta_D"), "Puerta_D de PA01");
            Assert.IsNull(pa01.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/" + CarrosTripoT1644.NombreModelo), "PA01 no lleva el modelo de Tripo");
            GameObject patrulla = AssetDatabase.LoadAssetAtPath<GameObject>(CarrosTripoT1644.RutaPatrullaBuena);
            Assert.IsNotNull(patrulla, "Patrulla.prefab");
            Transform[] piezas = patrulla.GetComponentsInChildren<Transform>(true);
            Assert.IsTrue(piezas.Any(t => t.name == "Puerta_I") && piezas.Any(t => t.name == "Puerta_D"), "Patrulla.prefab con sus puertas");
            Assert.IsNotNull(patrulla.transform.Find("LuzRoja"), "sirena roja");
            Assert.IsNotNull(patrulla.transform.Find("LuzAzul"), "sirena azul");
            foreach (string intacto in new[] { "PA02", "V11" })
            {
                Assert.IsNull(Prefab(intacto).transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/" + CarrosTripoT1644.NombreModelo), intacto + " no se toca");
            }
        }

        private static Bounds LimitesMalla(Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in marco.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 m = marco.root.worldToLocalMatrix * f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 p = m.MultiplyPoint3x4(v);
                    if (primero)
                    {
                        b = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(p);
                    }
                }
            }
            return b;
        }
    }
}
