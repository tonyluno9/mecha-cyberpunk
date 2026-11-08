using System.Linq;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-743: las motos V07, V08 y C19 con dos llantas RU02 (Ruedas_T743/Rueda_Del y Rueda_Tras, cada una Direccion/Giro/Modelo), sin puertas, y con
    /// todo lo demás de antes: colliders, cámara, asiento, puntos del conductor y el caballete que ManejoMoto quita al manejar.
    /// </summary>
    public class MotosPorPiezasT743Pruebas
    {
        private static readonly string[] Codigos = { "V07", "V08", "C19" };

        private static GameObject Moto(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, "Falta Carro_" + codigo);
            return prefab;
        }

        private static Bounds LimitesEn(Transform objeto, Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
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
        public void Prefab_TieneDosLlantasConSusPivotes([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Moto(codigo).transform;
            foreach (string nombre in new[] { "Rueda_Del", "Rueda_Tras" })
            {
                Assert.IsNotNull(raiz.Find("Visual_T511/Ruedas_T743/" + nombre + "/Direccion/Giro/Modelo"), nombre);
            }
            Assert.AreEqual(2, raiz.Find("Visual_T511/Ruedas_T743").childCount, "Solo dos llantas");
            Assert.IsNull(raiz.GetComponent<PuertasCarro>(), "Las motos no llevan puertas");
        }

        [Test]
        public void Llantas_PivoteEnSuCentro_EjeAnchoDeLaMotoYTamanoDeMoto([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Moto(codigo).transform;
            foreach (string nombre in new[] { "Rueda_Del", "Rueda_Tras" })
            {
                Transform giro = raiz.Find("Visual_T511/Ruedas_T743/" + nombre + "/Direccion/Giro");
                Bounds b = LimitesEn(giro, giro);
                Assert.AreEqual(0f, b.center.x, 0.005f, nombre + ": centrada en el eje");
                Assert.AreEqual(0f, b.center.y, 0.005f, nombre + ": centrada en Y");
                Assert.AreEqual(0f, b.center.z, 0.005f, nombre + ": centrada en Z");
                Assert.Less(b.size.x, b.size.y * 0.6f, nombre + ": el eje de la llanta es X (angosta de lado a lado)");
                Assert.AreEqual(b.size.y, b.size.z, 0.03f, nombre + ": redonda");
                Assert.Greater(b.size.y, 0.5f, nombre + ": tamaño de llanta de moto");
                Assert.Less(b.size.y, 0.85f, nombre + ": tamaño de llanta de moto");
            }
        }

        [Test]
        public void Llantas_TocanElPisoYLaDelanteraVaAdelante([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Moto(codigo).transform;
            Transform del = raiz.Find("Visual_T511/Ruedas_T743/Rueda_Del");
            Transform tras = raiz.Find("Visual_T511/Ruedas_T743/Rueda_Tras");
            Assert.Greater(del.position.z - tras.position.z, 1.0f, "La delantera está adelante (+Z) de la trasera");
            Assert.AreEqual(0f, del.position.x, 0.05f);
            Assert.AreEqual(0f, tras.position.x, 0.05f);
            Bounds ruedas = LimitesEn(raiz.Find("Visual_T511/Ruedas_T743"), raiz);
            Assert.AreEqual(0f, ruedas.min.y, 0.04f, "Las llantas tocan el piso");
            Bounds todo = LimitesEn(raiz.Find("Visual_T511"), raiz);
            Assert.AreEqual(0f, todo.min.y, 0.04f, "Nada se hunde ni flota");
        }

        [Test]
        public void Componente_ApuntaALaDelanteraParaDoblarYALasDosParaRodar([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject moto = Moto(codigo);
            RuedasVisualesCarro ruedas = moto.GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas);
            SerializedObject so = new SerializedObject(ruedas);
            Assert.AreEqual(1, so.FindProperty("direccionDelantera").arraySize);
            Assert.AreEqual(2, so.FindProperty("giro").arraySize);
            Assert.AreSame(moto.transform.Find("Visual_T511/Ruedas_T743/Rueda_Del/Direccion"), so.FindProperty("direccionDelantera").GetArrayElementAtIndex(0).objectReferenceValue);
            Assert.AreSame(moto.transform.Find("Visual_T511/Ruedas_T743/Rueda_Del/Direccion/Giro"), so.FindProperty("giro").GetArrayElementAtIndex(0).objectReferenceValue);
            Assert.AreSame(moto.transform.Find("Visual_T511/Ruedas_T743/Rueda_Tras/Direccion/Giro"), so.FindProperty("giro").GetArrayElementAtIndex(1).objectReferenceValue);
            Assert.Greater(so.FindProperty("distanciaEjes").floatValue, 1f);
            Assert.Greater(so.FindProperty("radio").floatValue, 0.25f);
        }

        [Test]
        public void ManejoMoto_ConservaElCaballeteYLaMallaDelCuerpo([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject moto = Moto(codigo);
            ManejoMoto manejo = moto.GetComponent<ManejoMoto>();
            Assert.IsNotNull(manejo);
            SerializedObject so = new SerializedObject(manejo);
            Mesh completa = (Mesh)so.FindProperty("mallaCompleta").objectReferenceValue;
            Mesh sin = (Mesh)so.FindProperty("mallaSinCaballete").objectReferenceValue;
            MeshFilter filtro = (MeshFilter)so.FindProperty("filtroModelo").objectReferenceValue;
            Assert.AreSame(moto.transform.Find("Visual_T511/Visual").GetComponent<MeshFilter>(), filtro, "El cuerpo sigue en Visual_T511/Visual");
            Assert.AreSame(completa, filtro.sharedMesh);
            Assert.IsNotNull(sin, "Hay malla sin caballete");
            Assert.Less(sin.triangles.Length, completa.triangles.Length, "La malla sin caballete tiene menos triángulos");
            Assert.AreEqual(0f, so.FindProperty("bajarAlOcultar").floatValue, 0.0001f, "Las llantas ya tocan el piso: el visual no baja");
        }

        [Test]
        public void Cuerpo_SinLlantasHorneadas([Values("V07", "V08")] string codigo)
        {
            Mesh original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/" + codigo + "/" + codigo + ".fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh cuerpo = Moto(codigo).transform.Find("Visual_T511/Visual").GetComponent<MeshFilter>().sharedMesh;
            Assert.Less(cuerpo.triangles.Length, original.triangles.Length - 3000, "Se quitaron las llantas del modelo de una malla");
        }

        [Test]
        public void C19_UsaElCuerpoNuevoSinRuedas()
        {
            Mesh nuevo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Piezas/C19/C19_Cuerpo.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
            GameObject moto = Moto("C19");
            Mesh cuerpo = moto.transform.Find("Visual_T511/Visual").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreSame(nuevo, cuerpo, "C19CUERPO");
            Bounds b = LimitesEn(moto.transform.Find("Visual_T511/Visual"), moto.transform);
            Assert.AreEqual(2.0f, b.size.z, 0.05f, "Mismo largo de siempre");
        }

        [Test]
        public void Prefab_ConservaFisicaCollidersCamaraYPuntosDelConductor([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Moto(codigo).transform;
            Assert.IsNotNull(raiz.GetComponent<CarroViejo>());
            Assert.AreEqual(2, raiz.GetComponentsInChildren<BoxCollider>(true).Count(c => !c.isTrigger), "Los dos cubos de choque de siempre");
            Assert.AreEqual(0, raiz.GetComponentsInChildren<MeshCollider>(true).Length);
            Assert.IsNotNull(raiz.Find("CamaraCarro"));
            Assert.IsNotNull(raiz.Find("Asiento"));
            Assert.IsNotNull(raiz.Find("PuntoSubida"));
            foreach (string punto in new[] { "Punto_Asiento", "Punto_ManubrioIzq", "Punto_ManubrioDer", "Punto_EstriboIzq", "Punto_EstriboDer" })
            {
                Assert.IsNotNull(raiz.Find("Visual_T511/" + punto), punto);
            }
            foreach (string rueda in new[] { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" })
            {
                Assert.IsNotNull(raiz.Find(rueda), "Las anclas de la suspensión siguen: " + rueda);
            }
        }

        [Test]
        public void Trafico_LaMotoDeLaCalleHeredaLasLlantas()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V08.prefab");
            Assert.IsNotNull(prefab.GetComponent<CarroTrafico>());
            Assert.IsNotNull(prefab.GetComponent<RuedasVisualesCarro>());
            Assert.IsNotNull(prefab.transform.Find("Visual_T511/Ruedas_T743/Rueda_Del/Direccion/Giro/Modelo"));
        }

        [Test]
        public void LosCarros_NoSeTocaron([Values("V02", "V03", "V04", "V06", "V09", "V10")] string codigo)
        {
            Assert.IsNull(Moto(codigo).transform.Find("Visual_T511/Ruedas_T743"));
        }
    }
}
