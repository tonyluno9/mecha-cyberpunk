using System.Collections;
using System.Linq;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>Pruebas jugables de la cabina greybox (T-011): prefab real, piso creado en la prueba, entrada simulada.</summary>
    public class CabinaPruebas : InputTestFixture
    {
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaCentro = 3f;
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private ControladorMecha controlador;
        private Transform cabina;
        private Camera camara;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            mecha = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            controlador = mecha.GetComponent<ControladorMecha>();
            cabina = mecha.transform.Find("Cabina");
            camara = mecha.GetComponentInChildren<Camera>();
        }

        public override void TearDown()
        {
            Object.Destroy(mecha);
            Object.Destroy(piso);
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private Renderer Pieza(string nombre)
        {
            Transform t = cabina.Find(nombre);
            Assert.IsNotNull(t, "Falta " + nombre);
            return t.GetComponent<Renderer>();
        }

        private bool EnVista(Renderer render)
        {
            Plane[] planos = GeometryUtility.CalculateFrustumPlanes(camara);
            return GeometryUtility.TestPlanesAABB(planos, render.bounds);
        }

        private Transform Puno(string brazo)
        {
            return cabina.Find(brazo + "/Puno");
        }

        [Test]
        public void Cabina_Estructura_BajoElCuerpoYConLosNombresDeLaTarea()
        {
            Assert.IsNotNull(cabina, "Cabina debe colgar de la raíz del mecha");
            Assert.IsFalse(cabina.IsChildOf(camara.transform), "Cabina no debe colgar de la cámara");
            string[] nombres = { "PosteIzquierdo", "PosteDerecho", "VigaSuperior", "Techo", "Tablero", "Cristal",
                "AnclaPantallaVida", "MarcoPantallaVida", "NeonViga", "NeonTablero",
                "BrazoIzquierdo/Brazo", "BrazoIzquierdo/Antebrazo", "BrazoIzquierdo/Puno",
                "BrazoDerecho/Brazo", "BrazoDerecho/Antebrazo", "BrazoDerecho/Puno" };
            foreach (string nombre in nombres)
            {
                Assert.IsNotNull(cabina.Find(nombre), "Falta " + nombre);
            }
            Assert.AreEqual(0, cabina.GetComponentsInChildren<Collider>().Length, "La cabina no debe estorbar a la física");
            Assert.That(cabina.GetComponentsInChildren<Renderer>().All(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off),
                "Las piezas no proyectan sombras");
            Assert.LessOrEqual(camara.nearClipPlane, 0.05f);
        }

        [Test]
        public void Cabina_AnclaPantallaVida_EstaEnElCentroDelTablero()
        {
            Transform ancla = cabina.Find("AnclaPantallaVida");
            Vector3 local = camara.transform.InverseTransformPoint(ancla.position);
            Assert.AreEqual(0f, local.x, 0.01f);
            Assert.Less(local.y, 0f, "Debajo de la vista");
            Assert.Greater(local.z, camara.nearClipPlane);
        }

        [Test]
        public void Cabina_BrazosEnReposo_VisiblesAbajoUnoACadaLado()
        {
            Transform izquierdo = Puno("BrazoIzquierdo");
            Transform derecho = Puno("BrazoDerecho");
            Vector3 vi = camara.WorldToViewportPoint(izquierdo.position);
            Vector3 vd = camara.WorldToViewportPoint(derecho.position);
            Assert.That(vi.z, Is.GreaterThan(0f));
            Assert.That(vi.x, Is.InRange(0.02f, 0.5f), "Izquierdo en la mitad izquierda");
            Assert.That(vd.x, Is.InRange(0.5f, 0.98f), "Derecho en la mitad derecha");
            Assert.That(vi.y, Is.InRange(0.02f, 0.5f), "Parte baja de la vista");
            Assert.That(vd.y, Is.InRange(0.02f, 0.5f), "Parte baja de la vista");
            Assert.IsTrue(EnVista(izquierdo.GetComponent<Renderer>()));
            Assert.IsTrue(EnVista(derecho.GetComponent<Renderer>()));
        }

        [Test]
        public void Cabina_CentroDeLaVista_EstaDespejadoSalvoElCristal()
        {
            Ray rayo = new Ray(camara.transform.position, camara.transform.forward);
            foreach (Renderer r in cabina.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "Cristal")
                {
                    continue;
                }
                bool golpea = r.bounds.IntersectRay(rayo, out float distancia) && distancia < DistanciaCentro;
                Assert.IsFalse(golpea, r.name + " tapa el centro de la vista");
            }
        }

        [Test]
        public void Cabina_Piezas_NoCruzanElPlanoCercano()
        {
            foreach (Renderer r in cabina.GetComponentsInChildren<Renderer>())
            {
                float distancia = Mathf.Sqrt(r.bounds.SqrDistance(camara.transform.position));
                Assert.Greater(distancia, camara.nearClipPlane * 2f, r.name + " está pegada a la cámara");
            }
        }

        [Test]
        public void Cabina_Materiales_CristalTransparenteYNeonEmisivo()
        {
            Material cristal = Pieza("Cristal").sharedMaterial;
            Assert.AreEqual(1f, cristal.GetFloat("_Surface"), "Cristal transparente");
            Assert.GreaterOrEqual(cristal.renderQueue, 3000);
            Color tinte = cristal.GetColor("_BaseColor");
            Assert.That(tinte.a, Is.InRange(0.02f, 0.3f), "Tinte leve");
            Assert.Greater(tinte.b, tinte.r, "Tinte azulado");

            Material neonViga = Pieza("NeonViga").sharedMaterial;
            Material neonTablero = Pieza("NeonTablero").sharedMaterial;
            Material estructura = Pieza("Techo").sharedMaterial;
            Assert.IsTrue(neonViga.IsKeywordEnabled("_EMISSION"));
            Assert.IsTrue(neonTablero.IsKeywordEnabled("_EMISSION"));
            Assert.Greater(neonViga.GetColor("_EmissionColor").maxColorComponent, 1f);
            Assert.Greater(neonTablero.GetColor("_EmissionColor").maxColorComponent, 1f);
            Assert.AreEqual(0f, estructura.GetColor("_EmissionColor").maxColorComponent, 0.001f, "La estructura no brilla");
            Assert.AreNotEqual(neonViga.GetColor("_EmissionColor"), neonTablero.GetColor("_EmissionColor"), "Turquesa y magenta");
        }

        [UnityTest]
        public IEnumerator Cabina_MirarArriba_VeElTechoYLaCabinaNoSeInclina()
        {
            yield return Calentar();
            Quaternion rotacionInicial = cabina.localRotation;
            Set(raton.delta, new Vector2(0f, 100000f));
            yield return null;
            yield return null;
            Assert.AreEqual(-35f, controlador.InclinacionVertical, 0.01f);
            Assert.AreEqual(0f, Quaternion.Angle(rotacionInicial, cabina.localRotation), 0.001f, "La cabina no se inclina");
            Assert.AreEqual(0f, Quaternion.Angle(mecha.transform.rotation, cabina.rotation), 0.01f, "Orientada como el cuerpo");
            Assert.IsTrue(EnVista(Pieza("Techo")), "Mirando arriba se ve el techo");
            Assert.IsFalse(EnVista(Pieza("Tablero")), "Mirando arriba no se ve el tablero");
        }

        [UnityTest]
        public IEnumerator Cabina_MirarAbajo_VeElTablero()
        {
            yield return Calentar();
            Set(raton.delta, new Vector2(0f, -100000f));
            yield return null;
            yield return null;
            Assert.AreEqual(35f, controlador.InclinacionVertical, 0.01f);
            Assert.IsTrue(EnVista(Pieza("Tablero")), "Mirando abajo se ve el tablero");
            Assert.IsTrue(EnVista(Pieza("MarcoPantallaVida")), "Y el marco de la pantalla de vida");
        }

        [UnityTest]
        public IEnumerator Cabina_Caminar_LosBrazosSeMuevenConElMecha()
        {
            yield return Calentar();
            Transform puno = Puno("BrazoDerecho");
            Vector3 localInicial = mecha.transform.InverseTransformPoint(puno.position);
            Vector3 mundoInicial = puno.position;
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(puno.position.z - mundoInicial.z, 1f, "Avanza con el mecha");
            Vector3 localFinal = mecha.transform.InverseTransformPoint(puno.position);
            Assert.AreEqual(0f, Vector3.Distance(localInicial, localFinal), 0.001f, "Quieto respecto al cuerpo");
        }

        [UnityTest]
        public IEnumerator Cabina_Girar_LosBrazosGiranConElMecha()
        {
            yield return Calentar();
            Transform puno = Puno("BrazoIzquierdo");
            float inicio = mecha.transform.eulerAngles.y;
            Set(raton.delta, new Vector2(300f, 0f));
            yield return Esperar(1f);
            float giro = Mathf.DeltaAngle(inicio, mecha.transform.eulerAngles.y);
            Assert.Greater(giro, 10f);
            Assert.AreEqual(0f, Vector3.Angle(mecha.transform.forward, puno.forward), 0.01f, "El brazo apunta donde el mecha");
            Vector3 vista = camara.WorldToViewportPoint(puno.position);
            Assert.That(vista.x, Is.InRange(0.02f, 0.5f), "Sigue a la izquierda en la vista");
        }
    }
}
