using System.IO;
using System.Linq;
using MechaCyberpunk.Vehiculos;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1510: PA01, PA02 y V22 partidos en piezas (FBX de Blender), sus prefabs, las patrullas con el cuerpo de PA01/PA02 y la suspensión alineada (ninguna llanta hundida).
    /// Corren después de CarrosPAT1510.ArmarTodos y PatrullasPAT1510.Rehacer.
    /// </summary>
    public class CarrosPAT1510Pruebas
    {
        private static readonly string[] Codigos = { "PA01", "PA02", "V22" };
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaCamioneta = "Assets/_Proyecto/Trafico/Prefabs/PatrullaCamioneta.prefab";
        private const float MargenLargo = 0.12f;
        private const float MargenPivote = 0.03f;
        private const float MargenAltura = 0.02f;
        private const float MargenPiso = 0.06f;

        private static float LargoReal(string codigo) => CarrosPAT1510.Carros.First(c => c.codigo == codigo).largo;

        private static GameObject Cargar(string ruta)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(g, "Falta " + ruta);
            return g;
        }

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

        private static Transform Buscar(Transform raiz, string nombre) => raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);

        [Test]
        public void Fbx_TraeCarroceriaCuatroLlantasPuertasYCristales([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject fbx = Cargar(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            foreach (string pieza in new[] { "Carroceria", "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD", "Puerta_I", "Puerta_D" })
            {
                Transform t = fbx.transform.Find(pieza);
                Assert.IsNotNull(t, codigo + ": falta " + pieza);
                Assert.IsNotNull(t.GetComponent<MeshFilter>().sharedMesh, codigo + ": " + pieza + " sin malla");
            }
            foreach (string cristal in new[] { "Cristal_Parabrisas", "Cristal_Puerta_I", "Cristal_Puerta_D" })
            {
                Assert.IsNotNull(Buscar(fbx.transform, cristal), codigo + ": falta " + cristal);
            }
        }

        [Test]
        public void Fbx_CarroceriaMideElLargoReal([ValueSource(nameof(Codigos))] string codigo)
        {
            // Largo de la caja de todas las piezas del FBX (ruedas incluidas): la dimensión mayor es el largo del carro.
            GameObject fbx = Cargar(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            Bounds b = LimitesEn(fbx.transform, fbx.transform);
            Assert.AreEqual(LargoReal(codigo), Mathf.Max(b.size.x, b.size.y, b.size.z), MargenLargo, codigo + ": largo real en metros");
        }

        [Test]
        public void Fbx_LlantasConElPivoteEnElEje([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject fbx = Cargar(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                Mesh malla = fbx.transform.Find(nombre).GetComponent<MeshFilter>().sharedMesh;
                Vector3 c = malla.bounds.center;
                Assert.Less(c.magnitude, 0.06f, codigo + " " + nombre + ": el centro de la llanta está a " + c.magnitude.ToString("F3") + " m de su pivote");
            }
        }

        [Test]
        public void Prefab_LlantasGiranSobreSuEje([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(Cargar(CarrosPAT1510.RutaPrefab(codigo)));
            try
            {
                foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
                {
                    Transform giro = instancia.transform.Find(nombre + "/Direccion/Giro");
                    Assert.IsNotNull(giro, codigo + " " + nombre);
                    Bounds antes = CreadorCarrosT511.LimitesExactos(giro.gameObject);
                    giro.localRotation = Quaternion.Euler(77f, 0f, 0f);
                    Bounds despues = CreadorCarrosT511.LimitesExactos(giro.gameObject);
                    Assert.Less(Vector3.Distance(antes.center, despues.center), MargenPivote, codigo + " " + nombre + ": la llanta se desplaza al girar (pivote fuera del eje)");
                    Assert.Less(Vector3.Distance(giro.position, antes.center), 0.05f, codigo + " " + nombre + ": el pivote Giro no está en el centro de la llanta");
                }
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        [Test]
        public void Prefab_LargoRealPuertasYCristal([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Cargar(CarrosPAT1510.RutaPrefab(codigo));
            Bounds b = LimitesEn(prefab.transform.Find("Visual_T511"), prefab.transform);
            Assert.AreEqual(LargoReal(codigo), b.size.z, MargenLargo, codigo + ": largo en Z");
            Assert.AreEqual(1f, prefab.transform.Find("Visual_T511").lossyScale.x, 0.01f, codigo + ": sin escala vieja");
            Assert.IsNotNull(prefab.GetComponent<PuertasCarro>(), codigo + ": PuertasCarro");
            foreach (string nombre in new[] { "Puerta_I", "Puerta_D", "Cristal_Parabrisas", "Bisagra_Puerta_I", "Bisagra_Puerta_D" })
            {
                Assert.IsNotNull(Buscar(prefab.transform, nombre), codigo + ": " + nombre);
            }
        }

        [Test]
        public void Prefab_BisagraDeLaPuertaEnSuBordeDelantero([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Cargar(CarrosPAT1510.RutaPrefab(codigo));
            foreach (string lado in new[] { "I", "D" })
            {
                Transform bisagra = Buscar(prefab.transform, "Bisagra_Puerta_" + lado);
                Bounds puerta = LimitesEn(bisagra.Find("Puerta_" + lado), prefab.transform);
                Vector3 h = prefab.transform.InverseTransformPoint(bisagra.position);
                Assert.AreEqual(puerta.max.z, h.z, 0.1f, codigo + " puerta " + lado + ": la bisagra va en el borde delantero");
                Assert.Greater(puerta.size.z, 0.5f, codigo + " puerta " + lado + ": tiene largo");
            }
        }

        [Test]
        public void Patrullas_UsanElCuerpoDePA01YPA02()
        {
            (string ruta, string codigo)[] pares = { (RutaPatrulla, "PA01"), (RutaCamioneta, "PA02") };
            foreach (var par in pares)
            {
                GameObject patrulla = Cargar(par.ruta);
                GameObject fbx = Cargar(CreadorCarrosBlenderT1312.RutaFbx(par.codigo));
                Mesh llantaFbx = fbx.transform.Find("Rueda_DI").GetComponent<MeshFilter>().sharedMesh;
                Mesh llantaPatrulla = patrulla.transform.Find("Rueda_DI/Direccion/Giro/Modelo").GetComponent<MeshFilter>().sharedMesh;
                Assert.AreSame(llantaFbx, llantaPatrulla, par.ruta + ": la llanta es la de " + par.codigo);
                Mesh cuerpoFbx = fbx.transform.Find("Carroceria").GetComponent<MeshFilter>().sharedMesh;
                Mesh cuerpoPatrulla = patrulla.transform.Find("Visual_T511/Carroceria").GetComponent<MeshFilter>().sharedMesh;
                Assert.AreSame(cuerpoFbx, cuerpoPatrulla, par.ruta + ": la carrocería es la de " + par.codigo);
                Assert.IsNotNull(patrulla.GetComponent<MechaCyberpunk.Trafico.Persecucion.PatrullaMundo>(), "PatrullaMundo sigue en la raíz");
                Assert.IsNotNull(patrulla.GetComponent<PuertasCarro>(), "PuertasCarro");
                Assert.IsNotNull(patrulla.transform.Find("LuzRoja"), "LuzRoja");
                Assert.IsNotNull(patrulla.transform.Find("LuzAzul"), "LuzAzul");
                Assert.IsNotNull(Buscar(patrulla.transform, "Cristal_Parabrisas"), "cristal");
            }
        }

        [Test]
        public void Patrullas_ConservanSuGuid()
        {
            // Mismo archivo, mismo GUID: Barrio.prefab las referencia.
            foreach (string ruta in new[] { RutaPatrulla, RutaCamioneta })
            {
                Assert.IsTrue(File.Exists(ruta + ".meta"), ruta + ".meta");
                Assert.IsNotEmpty(AssetDatabase.AssetPathToGUID(ruta), ruta);
            }
        }

        [Test]
        public void V22_UsaElFbxNuevoYSusRuedasSeparadas()
        {
            GameObject prefab = Cargar(CarrosPAT1510.RutaPrefab("V22"));
            GameObject fbx = Cargar(CreadorCarrosBlenderT1312.RutaFbx("V22"));
            Mesh cuerpoFbx = fbx.transform.Find("Carroceria").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreSame(cuerpoFbx, prefab.transform.Find("Visual_T511/Carroceria").GetComponent<MeshFilter>().sharedMesh, "la carrocería es la del FBX nuevo");
            foreach (MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.IsNotNull(f.sharedMesh, "malla faltante en " + f.name);
            }
        }

        // ------------------------------------------------------------------ suspensión (ninguna llanta hundida)

        private static string[] RutasDeCarros()
        {
            return Directory.GetFiles(CreadorCarrosT511.CarpetaPrefabs, "Carro_*.prefab").Select(r => r.Replace('\\', '/'))
                .Concat(new[] { RutaPatrulla, RutaCamioneta }).ToArray();
        }

        [Test]
        public void AlturaAnclaEnReposo_ConLosDatosDeSiempre_Es035()
        {
            ConfigCarro sedan = AssetDatabase.LoadAssetAtPath<ConfigCarro>("Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Sedan.asset");
            Assert.IsNotNull(sedan);
            Assert.AreEqual(sedan.RadioRueda, CarrosPAT1510.AlturaAnclaEnReposo(sedan), 0.02f, "con 0.15 m de caída el ancla queda a un radio de rueda sobre el piso");
        }

        [Test]
        public void Suspension_ConLaLlantaEnElPisoElAnclaQuedaAlturaDeReposo()
        {
            int revisados = 0;
            foreach (string ruta in RutasDeCarros())
            {
                GameObject prefab = Cargar(ruta);
                ConfigCarro config = CarrosPAT1510.ConfigDe(prefab);
                if (config == null || prefab.GetComponent<ManejoMoto>() != null)
                {
                    continue;
                }
                float altura = CarrosPAT1510.AlturaAnclaEnReposo(config);
                foreach (string rueda in CreadorCarrosPiezasT742.NombresRuedas)
                {
                    float fondo = CarrosPAT1510.FondoDeLlanta(prefab.transform, rueda);
                    if (float.IsNaN(fondo))
                    {
                        continue;
                    }
                    float ancla = prefab.transform.Find(rueda).localPosition.y;
                    Assert.AreEqual(fondo + altura, ancla, MargenAltura, ruta + " " + rueda + ": el ancla no deja la llanta sobre el piso (se hundiría o flotaría)");
                    Assert.AreEqual(0f, fondo, MargenPiso, ruta + " " + rueda + ": la llanta más baja debe quedar en y = 0 de la raíz");
                    revisados++;
                }
            }
            Assert.Greater(revisados, 20, "se revisaron las llantas de los carros");
        }

        [Test]
        public void Policia_V05V06V11_ConRinEnLasCuatroLlantas()
        {
            foreach (string codigo in CarrosRevisadosT1507.Policia)
            {
                GameObject prefab = Cargar(CarrosRevisadosT1507.RutaPrefab(codigo));
                foreach (string rueda in CreadorCarrosPiezasT742.NombresRuedas)
                {
                    MeshRenderer dibujo = prefab.transform.Find(rueda + "/Direccion/Giro/Modelo").GetComponent<MeshRenderer>();
                    Assert.IsTrue(dibujo.sharedMaterials.Any(m => m != null && m.name == "Rin_Cromo"), codigo + " " + rueda + ": rin de policía");
                }
            }
        }
    }
}
