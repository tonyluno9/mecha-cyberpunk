using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Core.Mundo;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1360: el barrido detecta cada tipo de bug en escenas mínimas armadas con cubos de prueba (datos, no contenido).</summary>
    public class BarridoZonasT1360Pruebas
    {
        private GameObject raiz;
        private Transform detalles;
        private Material material;

        [SetUp]
        public void SetUp()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f));
            }
            raiz = new GameObject("ZonaPrueba");
            detalles = new GameObject("Detalles").transform;
            detalles.SetParent(raiz.transform, false);
            Cubo("Suelo", raiz.transform, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(raiz);
            Object.DestroyImmediate(material);
        }

        private GameObject Cubo(string nombre, Transform padre, Vector3 pos, Vector3 escala)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            g.transform.SetParent(padre, false);
            g.transform.position = pos;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = material;
            return g;
        }

        private List<BarridoZonasT1360.Hallazgo> Barrer()
        {
            return BarridoZonasT1360.BarrerZona(raiz).ToList();
        }

        [Test]
        public void Barrer_CuboApoyado_NoDaHallazgos()
        {
            Cubo("Caja", detalles, new Vector3(0f, 0.5f, 0f), Vector3.one);
            List<BarridoZonasT1360.Hallazgo> r = Barrer();
            Assert.AreEqual(0, r.Count, string.Join("; ", r.Select(h => h.tipo + " " + h.ruta + " " + h.detalle)));
        }

        [Test]
        public void Barrer_CuboFlotando_EsFlota()
        {
            Cubo("Caja", detalles, new Vector3(0f, 1f, 0f), Vector3.one);
            BarridoZonasT1360.Hallazgo h = Barrer().FirstOrDefault(x => x.tipo == BarridoZonasT1360.TipoFlota);
            Assert.IsNotNull(h);
            Assert.AreEqual(0.5f, h.valor, 0.05f);
        }

        [Test]
        public void Barrer_CuboEnterrado_EsEnterrado()
        {
            Cubo("Caja", detalles, new Vector3(0f, 0f, 0f), Vector3.one);
            BarridoZonasT1360.Hallazgo h = Barrer().FirstOrDefault(x => x.tipo == BarridoZonasT1360.TipoEnterrado);
            Assert.IsNotNull(h);
            Assert.AreEqual(0.5f, h.valor, 0.05f);
        }

        [Test]
        public void Barrer_DosCubosMetidosUnoEnOtro_SonEncimados()
        {
            Cubo("CajaA", detalles, new Vector3(0f, 0.5f, 0f), Vector3.one);
            Cubo("CajaB", detalles, new Vector3(0.5f, 0.5f, 0f), Vector3.one);
            BarridoZonasT1360.Hallazgo h = Barrer().FirstOrDefault(x => x.tipo == BarridoZonasT1360.TipoEncimado);
            Assert.IsNotNull(h);
            Assert.AreEqual(0.5f, h.valor, 0.05f);
        }

        [Test]
        public void Barrer_ColliderSinRenderer_EsMuroInvisible()
        {
            GameObject muro = new GameObject("Muro");
            muro.transform.SetParent(detalles, false);
            muro.transform.position = new Vector3(5f, 1f, 5f);
            muro.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 2f);
            Assert.IsTrue(Barrer().Any(x => x.tipo == BarridoZonasT1360.TipoMuroInvisible));
        }

        [Test]
        public void Barrer_RendererSinCollider_EsSinCollider()
        {
            GameObject caja = Cubo("Caja", detalles, new Vector3(5f, 0.5f, 5f), new Vector3(2f, 1f, 1f));
            Object.DestroyImmediate(caja.GetComponent<Collider>());
            Assert.IsTrue(Barrer().Any(x => x.tipo == BarridoZonasT1360.TipoSinCollider));
        }

        [Test]
        public void Barrer_MaterialNulo_EsTexturaRota()
        {
            GameObject caja = Cubo("Caja", detalles, new Vector3(0f, 0.5f, 0f), Vector3.one);
            caja.GetComponent<Renderer>().sharedMaterial = null;
            Assert.IsTrue(Barrer().Any(x => x.tipo == BarridoZonasT1360.TipoTexturaRota));
        }

        [Test]
        public void Barrer_PisoConEscalonBajoCalleZona_EsEscalon()
        {
            Object.DestroyImmediate(raiz.transform.Find("Suelo").gameObject);
            Cubo("SueloA", raiz.transform, new Vector3(-5f, -0.5f, 0f), new Vector3(10f, 1f, 10f));
            Cubo("SueloB", raiz.transform, new Vector3(5f, 0f, 0f), new Vector3(10f, 1f, 10f));
            GameObject calle = new GameObject("CalleZona_Prueba");
            calle.transform.SetParent(raiz.transform, false);
            calle.AddComponent<CalleZona>().Definir(new[] { new Vector3(-8f, 0f, 0f), new Vector3(8f, 0f, 0f) }, 6f, true, false, "Prueba");
            BarridoZonasT1360.Hallazgo h = Barrer().FirstOrDefault(x => x.tipo == BarridoZonasT1360.TipoEscalon);
            Assert.IsNotNull(h);
            Assert.AreEqual(0.5f, h.valor, 0.05f);
        }

        [Test]
        public void Muestrear_LineaDe10m_DaUnaMuestraCada25cm()
        {
            List<Vector3> m = BarridoZonasT1360.Muestrear(new[] { Vector3.zero, new Vector3(10f, 0f, 0f) }, 0.25f, 2f);
            Assert.AreEqual(41, m.Count);
            Assert.AreEqual(-2f, m[0].z, 0.001f);
            Assert.AreEqual(10f, m[m.Count - 1].x, 0.001f);
        }

        [Test]
        public void Saltable_HuecoYDesnivel_SigueLasReglas()
        {
            Assert.IsTrue(BarridoZonasT1360.Saltable(1.5f, 2f));
            Assert.IsTrue(BarridoZonasT1360.Saltable(2f, -4f));
            Assert.IsFalse(BarridoZonasT1360.Saltable(2.5f, 0f));
            Assert.IsFalse(BarridoZonasT1360.Saltable(1f, 2.5f));
            Assert.IsFalse(BarridoZonasT1360.Saltable(1f, -4.5f));
        }
    }
}
