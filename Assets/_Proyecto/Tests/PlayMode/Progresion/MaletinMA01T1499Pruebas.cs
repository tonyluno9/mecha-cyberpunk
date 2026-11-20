using System.Collections;
using System.Linq;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1499: el maletín del atraco sale con el modelo MA01 (0.45 m, con collider) y cae al piso; no con primitivas.</summary>
    public class MaletinMA01T1499Pruebas
    {
        private GameObject piso;
        private MaletinAsalto maletin;

        [TearDown]
        public void Limpiar()
        {
            if (maletin != null)
            {
                Object.Destroy(maletin.gameObject);
            }
            if (piso != null)
            {
                Object.Destroy(piso);
            }
        }

        [UnityTest]
        public IEnumerator Crear_UsaElModeloMA01_ConCollider_YCaeAlPiso()
        {
            Assert.IsNotNull(Resources.Load<GameObject>(MaletinAsalto.RecursoModelo), "falta el prefab del maletín (correr ImportarMaletinT1499.Importar)");
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(20f, 1f, 20f);
            yield return null;
            Assert.IsTrue(MaletinAsalto.BuscarPiso(Vector3.zero, null, out Vector3 suelo), "hay piso");

            maletin = MaletinAsalto.Crear(suelo, 30f);
            Transform modelo = maletin.transform.Find("Modelo");
            Assert.IsNotNull(modelo, "el modelo MA01 cuelga del maletín");
            Assert.IsNull(maletin.transform.Find("Caja"), "ya no es la caja de primitivas");
            Assert.IsNotNull(modelo.GetComponentInChildren<MeshRenderer>(), "tiene malla");
            BoxCollider caja = modelo.GetComponentInChildren<BoxCollider>();
            Assert.IsNotNull(caja, "tiene su collider");
            Assert.IsFalse(caja.isTrigger);

            yield return new WaitForSeconds(1f);
            Physics.SyncTransforms();
            Bounds b = modelo.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            // El maletín cae girado 30 grados: la caja de ejes del mundo (renderer.bounds) lo infla (0.45*cos30 + 0.33*sin30 = 0.55),
            // así que el largo se mide en local, con la caja del collider (la raíz no tiene escala).
            Vector3 medidaLocal = Vector3.Scale(caja.size, caja.transform.lossyScale);
            Assert.AreEqual(0.45f, Mathf.Max(medidaLocal.x, medidaLocal.z), 0.03f, "mide ~45 cm de largo");
            Assert.Less(b.size.y, 0.3f, "va acostado");
            Assert.AreEqual(suelo.y, b.min.y, 0.03f, "descansa en el piso, sin flotar");
            Assert.IsTrue(maletin.PuedeUsar);
        }
    }
}
