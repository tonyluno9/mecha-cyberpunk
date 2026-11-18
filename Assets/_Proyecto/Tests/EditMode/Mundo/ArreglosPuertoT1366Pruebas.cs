using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Core.Trepar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1366: el prefab del Puerto ya arreglado (contenedores apoyados y compactos, escaleras, colliders, asfalto sin planos repetidos).</summary>
    public class ArreglosPuertoT1366Pruebas
    {
        private const float ToleranciaFlota = 0.05f;
        private const float VoladizoMaximo = 0.3f;
        private const float HuecoFilaMaximo = 0.3f;
        private const float PasilloMinimo = 3.5f;
        private const float AlturaMaxPrimerNivel = 2.7f;
        private const float AlturaPieEscalera = 0.5f;
        private const float AlturaCimaEscaleraMin = 2.3f;
        private const float AlturaMaxColliderPoste = 2.6f;
        private const float CoplanarTolerancia = 0.001f;

        private GameObject raiz;

        [SetUp]
        public void SetUp()
        {
            raiz = AssetDatabase.LoadAssetAtPath<GameObject>(ArreglarPuertoT1366.RutaPrefab);
            Assert.IsNotNull(raiz, "Falta " + ArreglarPuertoT1366.RutaPrefab);
        }

        private List<Transform> Bloques()
        {
            Transform padre = raiz.transform.Find("Contenedores");
            Assert.IsNotNull(padre, "Falta el grupo Contenedores");
            List<Transform> lista = padre.Cast<Transform>().Where(t => t.name.StartsWith("Bloque_")).ToList();
            Assert.Greater(lista.Count, 20, "Se esperaban los bloques de contenedores");
            return lista;
        }

        private static Bounds CajaBloque(Transform bloque)
        {
            List<ArreglarPuertoT1366.Pila> pilas = ArreglarPuertoT1366.PilasDe(bloque);
            Bounds b = ArreglarPuertoT1366.LimitesRenderers(pilas[0].Contenedores[0]);
            foreach (ArreglarPuertoT1366.Pila p in pilas)
            {
                foreach (Transform t in p.Contenedores)
                {
                    b.Encapsulate(ArreglarPuertoT1366.LimitesRenderers(t));
                }
            }
            return b;
        }

        [Test]
        public void Contenedores_Ninguno_FlotaNiSeHundeEnSuPila()
        {
            foreach (Transform bloque in Bloques())
            {
                foreach (ArreglarPuertoT1366.Pila pila in ArreglarPuertoT1366.PilasDe(bloque))
                {
                    float techo = 0f;
                    foreach (Transform t in pila.Contenedores)
                    {
                        Bounds b = ArreglarPuertoT1366.LimitesRenderers(t);
                        Assert.AreEqual(techo, b.min.y, ToleranciaFlota, bloque.name + "/" + t.name + " debe apoyarse exacto en el de abajo (o en el patio)");
                        techo = b.max.y;
                    }
                }
            }
        }

        [Test]
        public void Contenedores_DeArriba_NoQuedanEnVoladizo()
        {
            foreach (Transform bloque in Bloques())
            {
                foreach (ArreglarPuertoT1366.Pila pila in ArreglarPuertoT1366.PilasDe(bloque))
                {
                    for (int k = 1; k < pila.Contenedores.Count; k++)
                    {
                        Bounds abajo = ArreglarPuertoT1366.LimitesRenderers(pila.Contenedores[k - 1]);
                        Bounds arriba = ArreglarPuertoT1366.LimitesRenderers(pila.Contenedores[k]);
                        float largo = arriba.size.x;
                        float sobresale = Mathf.Max(0f, arriba.max.x - abajo.max.x) + Mathf.Max(0f, abajo.min.x - arriba.min.x);
                        Assert.LessOrEqual(sobresale, VoladizoMaximo * largo, bloque.name + " nivel " + k + " en voladizo en x");
                        Assert.LessOrEqual(Mathf.Abs(arriba.center.z - abajo.center.z), VoladizoMaximo * arriba.size.z, bloque.name + " nivel " + k + " en voladizo en z");
                    }
                }
            }
        }

        [Test]
        public void Contenedores_FilasDelMismoBloque_QuedanJuntas()
        {
            foreach (Transform bloque in Bloques())
            {
                foreach (IGrouping<int, ArreglarPuertoT1366.Pila> col in ArreglarPuertoT1366.PilasDe(bloque).GroupBy(p => p.Columna))
                {
                    List<ArreglarPuertoT1366.Pila> orden = col.OrderBy(p => p.Z).ToList();
                    Assert.AreEqual(4, orden.Count, bloque.name + ": 4 filas por columna");
                    for (int i = 1; i < orden.Count; i++)
                    {
                        Bounds a = ArreglarPuertoT1366.LimitesRenderers(orden[i - 1].Contenedores[0]);
                        Bounds b = ArreglarPuertoT1366.LimitesRenderers(orden[i].Contenedores[0]);
                        Assert.LessOrEqual(b.min.z - a.max.z, HuecoFilaMaximo, bloque.name + " fila " + i + " separada de la anterior");
                    }
                }
            }
        }

        [Test]
        public void Bloques_Vecinos_TienenPasilloDeAlMenos3m50()
        {
            List<Transform> bloques = Bloques();
            List<Bounds> cajas = bloques.Select(CajaBloque).ToList();
            for (int i = 0; i < cajas.Count; i++)
            {
                for (int j = i + 1; j < cajas.Count; j++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(cajas[i].min.x - cajas[j].max.x, cajas[j].min.x - cajas[i].max.x));
                    float dz = Mathf.Max(0f, Mathf.Max(cajas[i].min.z - cajas[j].max.z, cajas[j].min.z - cajas[i].max.z));
                    Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), PasilloMinimo, bloques[i].name + " y " + bloques[j].name);
                }
            }
        }

        [Test]
        public void Bloques_Todos_SeSubenDesdeElSuelo()
        {
            foreach (Transform bloque in Bloques())
            {
                PuntoTrepar suelo = bloque.GetComponentsInChildren<PuntoTrepar>(true).FirstOrDefault(p => p.name.StartsWith("Escalera_Suelo"));
                Assert.IsNotNull(suelo, bloque.name + ": sin Escalera_Suelo");
                Assert.Less(suelo.PieAbajo.y, AlturaPieEscalera, bloque.name + ": la escalera debe arrancar del suelo");
                Assert.GreaterOrEqual(suelo.Cima.y, AlturaCimaEscaleraMin, bloque.name + ": la escalera debe llegar arriba del primer contenedor");
                Bounds b = ArreglarPuertoT1366.LimitesRenderers(ArreglarPuertoT1366.PilasDe(bloque)[0].Contenedores[0]);
                Assert.LessOrEqual(b.max.y, AlturaMaxPrimerNivel, bloque.name + ": primer nivel de 2.6 m");
            }
        }

        [Test]
        public void Bloques_CadaSubidaDeFila_TieneEscaleraDeFila()
        {
            foreach (Transform bloque in Bloques())
            {
                List<float> techos = ArreglarPuertoT1366.PilasDe(bloque).Where(p => p.Columna == 0).OrderBy(p => p.Z)
                    .Select(p => ArreglarPuertoT1366.LimitesRenderers(p.Contenedores[p.Contenedores.Count - 1]).max.y).ToList();
                List<PuntoTrepar> filas = bloque.GetComponentsInChildren<PuntoTrepar>(true).Where(p => p.name.StartsWith("Escalera_Fila")).ToList();
                for (int f = 0; f + 1 < techos.Count; f++)
                {
                    if (techos[f + 1] - techos[f] > 0.5f)
                    {
                        Assert.IsTrue(filas.Any(p => Mathf.Abs(p.transform.position.y - techos[f]) < 0.8f), bloque.name + ": falta escalera de la fila " + f);
                    }
                }
            }
        }

        private void AfirmarConCollider(string prefijo, int minimo)
        {
            List<Transform> piezas = raiz.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith(prefijo) && PrefabUtility.GetNearestPrefabInstanceRoot(t) == t.gameObject).ToList();
            Assert.GreaterOrEqual(piezas.Count, minimo, "Se esperaban al menos " + minimo + " " + prefijo);
            foreach (Transform t in piezas)
            {
                Assert.IsTrue(t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && !c.isTrigger), prefijo + " en " + t.position + " sin collider activo");
            }
        }

        [Test]
        public void Gruas_Todas_TienenCollider() { AfirmarConCollider("PU01_Grua", 4); }

        [Test]
        public void Barco_TieneCollider() { AfirmarConCollider("PU02_Barco", 1); }

        [Test]
        public void Bodegas_Todas_TienenCollider()
        {
            AfirmarConCollider("PU03_Bodega", 4);
            AfirmarConCollider("PU04_Bodega", 3);
        }

        [Test]
        public void Pallets_Huacales_Tambos_Montacargas_TienenCollider()
        {
            // Los pallets de los bloques y de las puertas, 14 como mínimo en el barrido original.
            AfirmarConCollider("PU06_Pallets", 14);
            AfirmarConCollider("C17_Huacales", 7);
            AfirmarConCollider("C16_Tambo", 10);
            AfirmarConCollider("PU05_Montacargas", 4);
        }

        [Test]
        public void Barco_QuedaSeparadoDelMuelleYFlotando()
        {
            Transform barco = raiz.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith("PU02_Barco"));
            Bounds b = ArreglarPuertoT1366.LimitesRenderers(barco);
            float hueco = ArreglarPuertoT1366.MuelleSur - b.max.z;
            Assert.GreaterOrEqual(hueco, ArreglarPuertoT1366.HuecoBarcoMin - 0.01f, "Barco pegado o dentro del muelle");
            Assert.LessOrEqual(hueco, ArreglarPuertoT1366.HuecoBarcoMax + 0.01f, "Barco demasiado lejos del muelle");
            Assert.Less(b.min.y, ArreglarPuertoT1366.YMar, "La quilla debe quedar bajo el agua");
            Assert.Greater(b.max.y, ArreglarPuertoT1366.YMar, "La cubierta debe sobresalir del agua");
        }

        [Test]
        public void Gruas_Todas_PisanElMuelle()
        {
            foreach (Transform g in raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("PU01_Grua")))
            {
                Assert.AreEqual(ArreglarPuertoT1366.YMuelle, ArreglarPuertoT1366.LimitesRenderers(g).min.y, 0.02f, "Patas de la grúa en " + g.position);
            }
        }

        [Test]
        public void Tramos_DeLasCalles_NoSeEncimanEnElMismoPlano()
        {
            List<Bounds> cajas = new List<Bounds>();
            foreach (Transform calle in raiz.transform.Cast<Transform>().Where(t => t.name.StartsWith("Calle_")))
            {
                foreach (Transform t in calle.Cast<Transform>().Where(x => x.name == "Tramo"))
                {
                    cajas.Add(ArreglarPuertoT1366.LimitesRenderers(t));
                }
            }
            Assert.AreEqual(9, cajas.Count, "Se esperaban los 9 Tramo de las 4 calles");
            for (int i = 0; i < cajas.Count; i++)
            {
                for (int j = i + 1; j < cajas.Count; j++)
                {
                    float ox = Mathf.Min(cajas[i].max.x, cajas[j].max.x) - Mathf.Max(cajas[i].min.x, cajas[j].min.x);
                    float oz = Mathf.Min(cajas[i].max.z, cajas[j].max.z) - Mathf.Max(cajas[i].min.z, cajas[j].min.z);
                    if (ox > 0.01f && oz > 0.01f)
                    {
                        Assert.Greater(Mathf.Abs(cajas[i].max.y - cajas[j].max.y), CoplanarTolerancia, "Tramos " + i + " y " + j + " en el mismo plano");
                    }
                }
            }
        }

        [Test]
        public void Postes_NoTienenColliderDeColumnaAlta()
        {
            Transform postes = raiz.transform.Find("Postes");
            Assert.IsNotNull(postes);
            foreach (BoxCollider bc in postes.GetComponentsInChildren<BoxCollider>(true))
            {
                Bounds w = ArreglarPuertoT1366.CajaEnMundo(bc.transform, new Bounds(bc.center, bc.size));
                Assert.LessOrEqual(w.size.y, AlturaMaxColliderPoste, "Poste con collider alto en " + w.center);
            }
        }
    }
}
