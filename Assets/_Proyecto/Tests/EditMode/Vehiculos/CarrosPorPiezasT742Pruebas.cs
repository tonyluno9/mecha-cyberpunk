using System.Linq;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-742: V01a, V01b y V05 por piezas (Carroceria, Puerta_Izq, Puerta_Der con pivote en la bisagra, cuatro llantas RU01 con su pivote en el centro,
    /// Asiento_Conductor y Punto_Puerta), con el mismo tamaño y los mismos colliders de antes, también en el tráfico y en la patrulla.
    /// </summary>
    public class CarrosPorPiezasT742Pruebas
    {
        private static readonly string[] Codigos = { "V01a", "V01b", "V05" };
        private static readonly string[] Ruedas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Margen = 0.05f;

        private static GameObject Carro(string codigo) => Cargar("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");

        private static GameObject Cargar(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta " + ruta);
            return prefab;
        }

        private static float Largo(string codigo)
        {
            switch (codigo)
            {
                case "V01a": return 4.2f;
                case "V01b": return 4.5f;
                default: return 4.6f;
            }
        }

        // Límites de las mallas de un objeto (vértice por vértice) en el espacio local de <marco>.
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
        public void Prefab_TieneLasPiezasConSusNombres([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            Transform visual = raiz.Find("Visual_T511");
            Assert.IsNotNull(visual, "Visual_T511");
            Assert.IsNotNull(visual.Find("Carroceria"), "Carroceria");
            Assert.IsNotNull(visual.Find("Puerta_Izq"), "Puerta_Izq");
            Assert.IsNotNull(visual.Find("Puerta_Der"), "Puerta_Der");
            foreach (string nombre in Ruedas)
            {
                Transform rueda = raiz.Find(nombre);
                Assert.IsNotNull(rueda, nombre);
                Assert.IsNotNull(rueda.Find("Direccion/Giro/Modelo"), nombre + "/Direccion/Giro/Modelo");
            }
            Assert.IsNotNull(raiz.Find("Asiento_Conductor"), "Asiento_Conductor");
            Assert.IsNotNull(raiz.Find("Punto_Puerta"), "Punto_Puerta");
            Assert.IsNull(raiz.Find("Asiento"), "Se renombró Asiento");
            Assert.IsNull(raiz.Find("PuntoSubida"), "Se renombró PuntoSubida");
        }

        [Test]
        public void Prefab_MismoTamanoDeAntes_ConLosPiesEnElPiso([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            Bounds b = LimitesEn(raiz.Find("Visual_T511/Carroceria"), raiz);
            Assert.AreEqual(Largo(codigo), b.size.z, Margen, "Largo");
            Assert.AreEqual(0f, b.min.y, Margen, "Pies en y = 0");
            Assert.AreEqual(0f, b.center.x, Margen, "Centrado a lo ancho");
            // Las llantas también tocan el piso (nada se hunde ni flota).
            Bounds todo = LimitesEn(raiz, raiz);
            Assert.AreEqual(0f, todo.min.y, Margen, "El punto más bajo del carro es el piso");
        }

        [Test]
        public void Prefab_MismosCollidersDeCubos_SinMallaDeChoque([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            foreach (string nombre in new[] { "Carroceria", "Cabina" })
            {
                Assert.IsNotNull(raiz.Find(nombre).GetComponent<BoxCollider>(), nombre + " sigue chocando");
                Assert.IsFalse(raiz.Find(nombre).GetComponent<Renderer>().enabled, nombre + " no se ve (es el cubo)");
            }
            Assert.AreEqual(0, raiz.GetComponentsInChildren<MeshCollider>(true).Length, "Las piezas visuales no chocan");
            Assert.AreEqual(2, raiz.GetComponentsInChildren<BoxCollider>(true).Count(c => !c.isTrigger), "Dos cajas de choque, como antes");
        }

        [Test]
        public void Puertas_TienenElPivoteEnLaBisagraDelanteraYSeAbrenHaciaAfuera([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform visual = Carro(codigo).transform.Find("Visual_T511");
            foreach (string nombre in new[] { "Puerta_Izq", "Puerta_Der" })
            {
                Transform pivote = visual.Find(nombre);
                Bounds b = LimitesEn(pivote, pivote);
                // En el marco del modelo el frente va a -X: la bisagra (origen del pivote) está en el borde delantero (X mínima) y abajo.
                Assert.AreEqual(0f, b.min.x, 0.002f, nombre + ": la bisagra es el borde delantero");
                Assert.AreEqual(0f, b.min.y, 0.002f, nombre + ": el origen está abajo de la puerta");
                Assert.Greater(b.size.x, 0.1f, nombre + ": tiene largo");
            }
            // Cada puerta está de su lado (izquierda: X negativa del carro, derecha: positiva).
            Transform raiz = Carro(codigo).transform;
            Assert.Less(raiz.InverseTransformPoint(visual.Find("Puerta_Izq").position).x, -0.5f);
            Assert.Greater(raiz.InverseTransformPoint(visual.Find("Puerta_Der").position).x, 0.5f);
        }

        [Test]
        public void Puertas_SinVidrio_ConMaterialesDeLasPiezas([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform visual = Carro(codigo).transform.Find("Visual_T511");
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, r.name + " tiene material");
                    StringAssert.DoesNotContain("Vidrio", m.name, "Sin vidrio, para ver al conductor");
                    Assert.IsNotNull(m.GetTexture("_BaseMap"), r.name + ": con su textura");
                }
            }
        }

        [Test]
        public void Ruedas_CadaLlantaTieneElPivoteEnSuCentroYElRadioDeLaTabla([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            float radio = raiz.GetComponent<RuedasVisualesCarro>().Radio;
            foreach (string nombre in Ruedas)
            {
                Transform giro = raiz.Find(nombre + "/Direccion/Giro");
                Bounds b = LimitesEn(giro, giro);
                Assert.AreEqual(0f, b.center.x, 0.005f, nombre + ": centrada en el eje");
                Assert.AreEqual(0f, b.center.y, 0.005f, nombre + ": centrada en Y");
                Assert.AreEqual(0f, b.center.z, 0.005f, nombre + ": centrada en Z");
                Assert.AreEqual(2f * radio, b.size.y, 0.02f, nombre + ": diámetro");
                Assert.AreEqual(2f * radio, b.size.z, 0.02f, nombre + ": diámetro");
                Assert.Less(b.size.x, b.size.y, nombre + ": el eje de la llanta es X (más angosta que alta)");
            }
        }

        [Test]
        public void Ruedas_LasCuatroLlevanElComponenteConLasDelanterasParaDoblar([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject carro = Carro(codigo);
            RuedasVisualesCarro ruedas = carro.GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas);
            SerializedObject so = new SerializedObject(ruedas);
            SerializedProperty delanteras = so.FindProperty("direccionDelantera");
            SerializedProperty giro = so.FindProperty("giro");
            Assert.AreEqual(2, delanteras.arraySize);
            Assert.AreEqual(4, giro.arraySize);
            Assert.AreSame(carro.transform.Find("Rueda_DI/Direccion"), delanteras.GetArrayElementAtIndex(0).objectReferenceValue);
            Assert.AreSame(carro.transform.Find("Rueda_DD/Direccion"), delanteras.GetArrayElementAtIndex(1).objectReferenceValue);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreSame(carro.transform.Find(Ruedas[i] + "/Direccion/Giro"), giro.GetArrayElementAtIndex(i).objectReferenceValue);
            }
        }

        [Test]
        public void Puertas_ElComponenteApuntaALosPivotes([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject carro = Carro(codigo);
            SerializedObject so = new SerializedObject(carro.GetComponent<PuertasCarro>());
            Assert.AreSame(carro.transform.Find("Visual_T511/Puerta_Izq"), so.FindProperty("puertaIzquierda").objectReferenceValue);
            Assert.AreSame(carro.transform.Find("Visual_T511/Puerta_Der"), so.FindProperty("puertaDerecha").objectReferenceValue);
        }

        [Test]
        public void Prefab_ConservaLaFisicaYElManejo([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            CarroViejo carro = raiz.GetComponent<CarroViejo>();
            Assert.IsNotNull(carro);
            SerializedObject so = new SerializedObject(carro);
            Assert.AreSame(raiz.Find("Asiento_Conductor"), so.FindProperty("asiento").objectReferenceValue, "El asiento sigue siendo el del carro");
            Assert.AreSame(raiz.Find("Punto_Puerta"), so.FindProperty("puntoSubida").objectReferenceValue, "El punto de subida sigue siendo el del carro");
            Assert.AreSame(raiz.Find("Rueda_DI"), so.FindProperty("ruedaDelanteraIzquierda").objectReferenceValue, "La suspensión usa las mismas anclas");
            Assert.AreEqual("ConfigCarro_Viejo", so.FindProperty("config").objectReferenceValue.name, "Misma config de manejo de siempre");
        }

        [Test]
        public void Trafico_LosCarrosDeLaCalleHeredanLasPiezas([Values("V01a", "V01b")] string codigo)
        {
            GameObject prefab = Cargar("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_" + codigo + ".prefab");
            Assert.IsNotNull(prefab.GetComponent<CarroTrafico>());
            Assert.IsNotNull(prefab.GetComponent<PuertasCarro>());
            Assert.IsNotNull(prefab.GetComponent<RuedasVisualesCarro>());
            Assert.IsNotNull(prefab.transform.Find("Visual_T511/Carroceria"));
            Assert.IsNotNull(prefab.transform.Find("Rueda_DI/Direccion/Giro/Modelo"));
        }

        [Test]
        public void Patrulla_ConservaSirenaRoboYGpsYLlevaLasPiezasDeV05()
        {
            GameObject patrulla = Cargar(RutaPatrulla);
            Assert.IsNotNull(patrulla.GetComponent<PatrullaMundo>(), "PatrullaMundo sigue en la raíz");
            Assert.IsNotNull(patrulla.GetComponent<CarroViejo>(), "Se puede robar (es un carro manejable)");
            Assert.IsNotNull(patrulla.GetComponent<CarroEstacionado>(), "Con alarma y calor al robarla");
            Assert.IsNotNull(patrulla.transform.Find("LuzRoja"), "Sirena roja");
            Assert.IsNotNull(patrulla.transform.Find("LuzAzul"), "Sirena azul");
            Assert.IsNotNull(patrulla.transform.Find("Visual_T511/Carroceria"));
            Assert.IsNotNull(patrulla.transform.Find("Visual_T511/Puerta_Izq"));
            Assert.IsNotNull(patrulla.transform.Find("Visual_T511/Puerta_Der"));
            Assert.IsNotNull(patrulla.GetComponent<PuertasCarro>());
            Assert.IsNotNull(patrulla.GetComponent<RuedasVisualesCarro>());
            foreach (string nombre in Ruedas)
            {
                Assert.IsNotNull(patrulla.transform.Find(nombre + "/Direccion/Giro/Modelo"), nombre);
            }
            Assert.IsNotNull(patrulla.transform.Find("Asiento_Conductor"));
            Assert.IsNotNull(patrulla.transform.Find("Punto_Puerta"));
            SerializedObject so = new SerializedObject(patrulla.GetComponent<CarroViejo>());
            Assert.AreSame(patrulla.transform.Find("Asiento_Conductor"), so.FindProperty("asiento").objectReferenceValue);
            Assert.AreSame(patrulla.transform.Find("Rueda_DI"), so.FindProperty("ruedaDelanteraIzquierda").objectReferenceValue);
        }

        [Test]
        public void Patrulla_MideLoMismoQueElCarroV05()
        {
            GameObject patrulla = Cargar(RutaPatrulla);
            Bounds b = LimitesEn(patrulla.transform.Find("Visual_T511/Carroceria"), patrulla.transform);
            Assert.AreEqual(4.6f, b.size.z, Margen);
            Assert.AreEqual(0f, b.min.y, Margen);
        }

        [Test]
        public void LosOtrosCarros_SiguenConSuModeloDeUnaMalla([Values("V02", "V03", "V04", "V06", "V08", "V09", "V10")] string codigo)
        {
            Transform raiz = Carro(codigo).transform;
            Assert.IsNull(raiz.GetComponent<PuertasCarro>(), codigo + " no lleva puertas por piezas");
            Assert.IsNull(raiz.GetComponent<RuedasVisualesCarro>(), codigo + " no lleva ruedas por piezas");
            Assert.IsNull(raiz.Find("Visual_T511/Carroceria"), codigo + " sigue con su modelo de una malla");
        }
    }
}
