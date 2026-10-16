using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Fachadas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-081: los edificios del barrio con locales (Fachada_Modular), ventanas y cornisa en sus lados visibles.</summary>
    public class FachadasPruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const float MitadInterior = 37f;
        private const float ToleranciaBorde = 0.05f;
        private const float DistanciaRayo = 3f;
        private const float AlturaBaja = 2f;
        private const float AlturaAlta = 7f;
        private const float HolguraRayo = 0.1f;
        private const int MinimoLadosDeCalle = 40;
        private const float MedidaVentana = 3f;
        private const float ToleranciaVentana = 0.1f;
        private const float VentanasPorMosaico = 4f;
        private const float MaximaSaliente = 0.45f;
        private const float HolguraPuerta = 0.3f;
        private static readonly int IdBaseMapST = Shader.PropertyToID("_BaseMap_ST");
        private static readonly Vector3[] Normales = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        private static readonly string[] Lados = { "Norte", "Sur", "Este", "Oeste" };

        private GameObject barrio;

        [SetUp]
        public void CrearBarrio()
        {
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(barrio);
        }

        private List<Transform> Edificios()
        {
            return barrio.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Edificio_") && t.parent.name == "Edificios").ToList();
        }

        private static float Mitad(Transform cuerpo, Vector3 normal)
        {
            return (normal.x != 0f ? cuerpo.localScale.x : cuerpo.localScale.z) / 2f;
        }

        /// <summary>Lados de edificio que dan a la calle: su cara está en el borde de la manzana.</summary>
        private IEnumerable<(Transform edificio, int indice, Vector3 centro)> LadosDeCalle()
        {
            foreach (Transform edificio in Edificios())
            {
                Transform cuerpo = edificio.Find("Cuerpo");
                Vector3 local = edificio.position - edificio.parent.parent.position;
                for (int i = 0; i < Normales.Length; i++)
                {
                    Vector3 n = Normales[i];
                    float cara = n.x != 0f ? local.x * n.x + Mitad(cuerpo, n) : local.z * n.z + Mitad(cuerpo, n);
                    if (cara >= MitadInterior - ToleranciaBorde)
                    {
                        yield return (edificio, i, edificio.position + n * Mitad(cuerpo, n));
                    }
                }
            }
        }

        private static bool RayoToca(Ray rayo, IEnumerable<Renderer> renderizadores)
        {
            foreach (Renderer r in renderizadores)
            {
                Bounds b = r.bounds;
                b.Expand(HolguraRayo);
                if (b.IntersectRay(rayo))
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void LadosDeCalle_TienenFachadaModularYVentanas_ADosYSieteMetros()
        {
            int total = 0;
            foreach (var lado in LadosDeCalle())
            {
                total++;
                Transform grupo = lado.edificio.Find("Lado_" + Lados[lado.indice]);
                Assert.IsNotNull(grupo, lado.edificio.name + " no tiene el lado " + Lados[lado.indice]);
                Vector3 normal = Normales[lado.indice];
                Vector3 origen = lado.centro + normal * DistanciaRayo;

                Ray bajo = new Ray(new Vector3(origen.x, AlturaBaja, origen.z), -normal);
                Renderer[] fachadas = grupo.GetComponentsInChildren<Renderer>().Where(r => r.name == "Fachada_Modular" || r.transform.parent.name == "Fachada_Modular").ToArray();
                Assert.IsNotEmpty(fachadas, lado.edificio.name + " " + Lados[lado.indice] + " sin Fachada_Modular");
                Assert.IsTrue(RayoToca(bajo, fachadas), lado.edificio.name + " " + Lados[lado.indice] + ": el rayo a 2 m no toca la fachada");

                Ray alto = new Ray(new Vector3(origen.x, AlturaAlta, origen.z), -normal);
                Renderer[] ventanas = grupo.GetComponentsInChildren<Renderer>().Where(r => r.name == "Ventanas").ToArray();
                Assert.AreEqual(1, ventanas.Length, lado.edificio.name + " " + Lados[lado.indice] + " debe tener un quad de ventanas");
                Assert.IsTrue(RayoToca(alto, ventanas), lado.edificio.name + " " + Lados[lado.indice] + ": el rayo a 7 m no toca las ventanas");

                Assert.IsNotNull(grupo.Find("Cornisa"), lado.edificio.name + " sin cornisa");
            }
            Assert.GreaterOrEqual(total, MinimoLadosDeCalle, "Debe haber muchos lados de edificio que dan a la calle");
        }

        [Test]
        public void Lugares_NoTienenFachadaNiVentanasEncimaDeSuPuerta()
        {
            List<Transform> lugares = barrio.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Lugar_")).ToList();
            Assert.GreaterOrEqual(lugares.Count, 5, "Faltan los lugares");
            List<Renderer> piezas = barrio.GetComponentsInChildren<Renderer>(true).Where(r => r.name == "Fachada_Modular" || r.name == "Ventanas" || r.name == "Cornisa" || r.name == "Relleno_Fachada" || r.transform.parent.name == "Fachada_Modular").ToList();
            Assert.IsNotEmpty(piezas);
            foreach (Transform lugar in lugares.Where(l => l.parent.name.StartsWith("Manzana_")))
            {
                Assert.IsEmpty(lugar.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Lado_") || t.name == "Fachada_Modular"), lugar.name + " no debe llevar fachada");
                Transform panel = lugar.Find("Puerta/Hoja") ?? lugar.Find("PanelPuerta");
                Assert.IsNotNull(panel, lugar.name + " sin hoja de puerta");
                Bounds puerta = panel.GetComponent<Renderer>().bounds;
                puerta.Expand(HolguraPuerta);
                foreach (Renderer pieza in piezas)
                {
                    Assert.IsFalse(puerta.Intersects(pieza.bounds), pieza.transform.parent.name + "/" + pieza.name + " tapa la puerta de " + lugar.name);
                }
            }
        }

        [Test]
        public void Ventanas_UnaVentanaMide3mEnAnchoYAlto()
        {
            VentanasFachada[] quads = barrio.GetComponentsInChildren<VentanasFachada>(true);
            Assert.GreaterOrEqual(quads.Length, MinimoLadosDeCalle);
            foreach (VentanasFachada quad in quads)
            {
                MeshRenderer renderizador = quad.GetComponent<MeshRenderer>();
                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                renderizador.GetPropertyBlock(bloque);
                Vector4 st = bloque.GetVector(IdBaseMapST);
                Assert.Greater(st.x, 0f, quad.transform.parent.parent.name + ": _BaseMap_ST sin aplicar");
                Vector3 escala = quad.transform.lossyScale;
                Assert.AreEqual(MedidaVentana, escala.x / st.x / VentanasPorMosaico, ToleranciaVentana, quad.transform.parent.parent.name + ": ancho de ventana");
                Assert.AreEqual(MedidaVentana, escala.y / st.y / VentanasPorMosaico, ToleranciaVentana, quad.transform.parent.parent.name + ": alto de ventana");
                Assert.IsNull(quad.GetComponent<Collider>(), "Las ventanas no llevan collider");
            }
        }

        [Test]
        public void FachadaModular_SinColliderYNoSobresaleMasDe45cm()
        {
            int piezas = 0;
            foreach (Transform grupo in barrio.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Lado_")))
            {
                Assert.IsEmpty(grupo.GetComponentsInChildren<Collider>(true), grupo.parent.name + "/" + grupo.name + " no debe tener colliders (el Cuerpo ya choca)");
                int indice = System.Array.IndexOf(Lados, grupo.name.Substring("Lado_".Length));
                Vector3 normal = Normales[indice];
                Transform cuerpo = grupo.parent.Find("Cuerpo");
                Vector3 centroLado = grupo.parent.position + normal * Mitad(cuerpo, normal);
                float pared = Vector3.Dot(centroLado, normal);
                foreach (Transform pieza in grupo.Cast<Transform>().Where(t => t.name == "Fachada_Modular"))
                {
                    piezas++;
                    Bounds b = pieza.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                    float extremo = Vector3.Dot(normal.x + normal.z > 0f ? b.max : b.min, normal);
                    Assert.LessOrEqual(extremo - pared, MaximaSaliente, grupo.parent.name + " " + grupo.name + ": la fachada sobresale demasiado");
                }
            }
            Assert.Greater(piezas, 0);
        }
    }
}
