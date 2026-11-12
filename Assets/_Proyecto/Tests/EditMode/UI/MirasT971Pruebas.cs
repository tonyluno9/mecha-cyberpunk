using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-971: la Mira_PuntoRojo de Blender sobre la corredera de la pistola (banco y agarre), su vidrio transparente y el banco sin piezas de primitivas.</summary>
    public class MirasT971Pruebas
    {
        private const string Carpeta = "Assets/Arte/Armas/W01_Pistola/";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";

        private static IEnumerable<string> Prefabs() => new[] { Carpeta + "W01_Pistola.prefab", Carpeta + "W01_Pistola_agarre.prefab" };

        private static Transform Buscar(Transform raiz, string nombre) => raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);

        // Límites de las mallas de t (con todos sus hijos) en el espacio local de la raíz.
        private static Bounds Limites(Transform raiz, Transform t, bool soloEste = false)
        {
            Bounds b = new Bounds();
            bool primero = true;
            List<Vector3> v = new List<Vector3>();
            IEnumerable<MeshFilter> filtros = soloEste ? new[] { t.GetComponent<MeshFilter>() } : t.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter f in filtros)
            {
                Matrix4x4 m = raiz.worldToLocalMatrix * f.transform.localToWorldMatrix;
                v.Clear();
                f.sharedMesh.GetVertices(v);
                foreach (Vector3 p in v)
                {
                    Vector3 q = m.MultiplyPoint3x4(p);
                    if (primero)
                    {
                        b = new Bounds(q, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(q);
                    }
                }
            }
            return b;
        }

        private static (Transform raiz, Transform mira, Bounds corredera, Bounds miraLimites) Cargar(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            Transform raiz = prefab.transform;
            Transform corredera = Buscar(raiz, "Corredera");
            Assert.IsNotNull(corredera, "Corredera");
            Transform mira = corredera.Find("Mira");
            Assert.IsNotNull(mira, "La Mira es hija directa de la Corredera en " + ruta);
            return (raiz, mira, Limites(raiz, corredera, true), Limites(raiz, mira));
        }

        private static Vector3 Frente(Transform raiz, Bounds corredera, Vector3 eje)
        {
            Transform boca = Buscar(raiz, "Boca");
            Vector3 haciaBoca = boca != null
                ? raiz.InverseTransformPoint(boca.position) - corredera.center
                : corredera.center - Limites(raiz, Buscar(raiz, "Bateria")).center;
            return Vector3.Dot(haciaBoca, eje) >= 0f ? eje : -eje;
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Pistola_TieneLaMiraBajoLaCorredera_Con4Materiales(string ruta)
        {
            var d = Cargar(ruta);
            MeshRenderer r = d.mira.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(r);
            Assert.GreaterOrEqual(r.sharedMaterials.Length, 4, "4 materiales");
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Mira_ApoyadaSobreLaCorredera(string ruta)
        {
            var d = Cargar(ruta);
            // Lo más alto de la corredera bajo la huella de la mira (los hierros de mira de la corredera son más altos que su lomo).
            Transform corredera = Buscar(d.raiz, "Corredera");
            MeshFilter f = corredera.GetComponent<MeshFilter>();
            Matrix4x4 m = d.raiz.worldToLocalMatrix * f.transform.localToWorldMatrix;
            List<Vector3> v = new List<Vector3>();
            f.sharedMesh.GetVertices(v);
            float tope = float.MinValue;
            foreach (Vector3 p in v)
            {
                Vector3 q = m.MultiplyPoint3x4(p);
                if (q.x >= d.miraLimites.min.x && q.x <= d.miraLimites.max.x && q.z >= d.miraLimites.min.z && q.z <= d.miraLimites.max.z)
                {
                    tope = Mathf.Max(tope, q.y);
                }
            }
            float dif = d.miraLimites.min.y - tope;
            Assert.That(dif, Is.InRange(-0.003f, 0.002f), "Base de la mira a " + dif * 1000f + " mm de la corredera bajo ella");
            Assert.LessOrEqual(d.miraLimites.min.y, d.corredera.max.y + 0.002f, "No flota sobre los hierros de mira");
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Mira_CentradaALoAncho_YEnLaMitadTrasera(string ruta)
        {
            var d = Cargar(ruta);
            bool largoEnX = d.corredera.size.x > d.corredera.size.z;
            float ancho = largoEnX ? d.miraLimites.center.z - d.corredera.center.z : d.miraLimites.center.x - d.corredera.center.x;
            Assert.Less(Mathf.Abs(ancho), 0.003f, "Centrada a lo ancho (menos de 3 mm)");
            float largo = largoEnX ? d.corredera.size.x : d.corredera.size.z;
            float baseMira = largoEnX ? d.miraLimites.size.x : d.miraLimites.size.z;
            Assert.Greater(baseMira, largo * 0.2f, "La mira es creíble frente a la corredera");
            Assert.Less(baseMira, largo * 0.6f, "La mira no es enorme frente a la corredera");
            Vector3 frente = Frente(d.raiz, d.corredera, largoEnX ? Vector3.right : Vector3.forward);
            Assert.Less(Vector3.Dot(d.miraLimites.center - d.corredera.center, frente), 0f, "El centro de la mira está en la mitad trasera");
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Mira_ElFrenteMiraHaciaLaBocaYArribaEsArriba(string ruta)
        {
            var d = Cargar(ruta);
            // El nodo raíz del FBX trae su giro (89.98° en X): sin él, Y arriba y Z al frente.
            Quaternion giroFbx = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "Mira_PuntoRojo.fbx").transform.localRotation;
            Quaternion visual = d.mira.rotation * Quaternion.Inverse(giroFbx);
            Assert.Greater(Vector3.Dot(visual * Vector3.up, d.raiz.up), 0.99f, "Y de la mira hacia arriba");
            Vector3 f = d.raiz.InverseTransformDirection(visual * Vector3.forward);
            bool largoEnX = d.corredera.size.x > d.corredera.size.z;
            Vector3 esperado = Frente(d.raiz, d.corredera, largoEnX ? Vector3.right : Vector3.forward);
            Assert.Greater(Vector3.Dot(f, esperado), 0.9f, "El frente de la mira mira hacia la boca");
        }

        [Test]
        public void MiraVidrio_EsTransparente()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "Mira_Vidrio.mat");
            Assert.IsNotNull(m);
            Assert.GreaterOrEqual(m.renderQueue, 3000);
            Assert.AreEqual(1f, m.GetFloat("_Surface"));
            Assert.Less(m.GetColor("_BaseColor").a, 0.5f);
            Assert.AreEqual(0f, m.GetFloat("_ZWrite"));
            Assert.IsTrue(m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"));
        }

        [Test]
        public void MiraPunto_EsEmisivoYNoPierdeLaEmision()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "Mira_Punto.mat");
            Assert.IsNotNull(m);
            Assert.IsTrue(m.IsKeywordEnabled("_EMISSION"));
            Assert.AreEqual(MaterialGlobalIlluminationFlags.RealtimeEmissive, m.globalIlluminationFlags);
            Assert.Greater(m.GetColor("_EmissionColor").r, 0.5f);
        }

        [Test]
        public void MiraFbx_UsaLosCuatroMaterialesRemapeados()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "Mira_PuntoRojo.fbx");
            Assert.IsNotNull(fbx);
            List<string> rutas = fbx.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Select(m => AssetDatabase.GetAssetPath(m)).Distinct().ToList();
            Assert.AreEqual(4, rutas.Count, string.Join(",", rutas));
            foreach (string ruta in rutas)
            {
                StringAssert.StartsWith(Carpeta + "Mira_", ruta);
                StringAssert.EndsWith(".mat", ruta);
            }
        }

        [Test]
        public void Banco_PistolaPiezaMira_ExisteComoMallaDelPrefab()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            EntradaArmaMenu pistola = config.Armas[0];
            string nombre = pistola.NombrePieza(PiezaArma.Mira);
            Assert.IsFalse(string.IsNullOrEmpty(nombre), "La pistola declara su pieza mira");
            // VistaHologramaArma.CopiarModelo nombra cada copia con el GameObject que tiene el MeshFilter.
            Assert.IsTrue(pistola.Modelo.GetComponentsInChildren<MeshFilter>(true).Any(f => f.name == nombre), "El prefab tiene una malla llamada " + nombre);
        }

        [Test]
        public void VistaHologramaArma_YaNoCreaPiezasDePrimitivas()
        {
            const System.Reflection.BindingFlags privado = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            System.Type t = typeof(VistaHologramaArma);
            Assert.IsNull(t.GetMethod("CrearMira", privado), "CrearMira");
            Assert.IsNull(t.GetMethod("Primitiva", privado), "Primitiva");
            string fuente = System.IO.File.ReadAllText("Assets/_Proyecto/UI/Armas/VistaHologramaArma.cs");
            foreach (string nombre in new[] { "MiraHolografica", "CargadorHolografico", "CanonHolografico" })
            {
                StringAssert.DoesNotContain(nombre, fuente);
            }
        }
    }
}
