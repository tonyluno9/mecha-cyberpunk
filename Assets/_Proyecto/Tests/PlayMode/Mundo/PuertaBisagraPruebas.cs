using System.Collections;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-804: hoja de puerta con bisagra. Arma una puerta de 1 m en un muro (sin escena), la abre desde un lado, mide que gire ~95° alejándose de
    /// quien abre, que un CharacterController cruce el vano y que se cierre sola (y vuelva a chocar) cuando el umbral queda libre.
    /// </summary>
    public class PuertaBisagraPruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigPuertas_Mundo.asset";
        private const float AnchoHoja = 1f;
        private const float AltoHoja = 2.1f;
        private const float ToleranciaGrados = 2f;

        private GameObject raiz;
        private ConfigPuertas config;

        [SetUp]
        public void Armar()
        {
            config = AssetDatabase.LoadAssetAtPath<ConfigPuertas>(RutaConfig);
            Assert.IsNotNull(config);
        }

        [TearDown]
        public void Limpiar()
        {
            if (raiz != null)
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // Muro en z = 0 (de x -3 a 3) con un vano de x -0.5 a 0.5; la hoja cerrada llena el vano y su bisagra está en x = 0.5.
        private HojaPuerta CrearPuerta()
        {
            raiz = new GameObject("PruebaBisagra");
            Muro("Muro_Izq", new Vector3(-1.75f, 1.5f, 0f), new Vector3(2.5f, 3f, 0.2f));
            Muro("Muro_Der", new Vector3(1.75f, 1.5f, 0f), new Vector3(2.5f, 3f, 0.2f));
            Muro("Piso", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 8f));
            GameObject puerta = new GameObject("Puerta");
            puerta.transform.SetParent(raiz.transform, false);
            Transform umbral = new GameObject("Umbral").transform;
            umbral.SetParent(puerta.transform, false);
            umbral.localPosition = Vector3.up * 1.1f;
            GameObject hoja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoja.name = "Hoja";
            hoja.SetActive(false);
            hoja.transform.SetParent(puerta.transform, false);
            hoja.transform.localPosition = new Vector3(0f, AltoHoja * 0.5f, 0f);
            hoja.transform.localScale = new Vector3(AnchoHoja, AltoHoja, 0.06f);
            HojaPuerta hp = hoja.AddComponent<HojaPuerta>();
            SerializedObject so = new SerializedObject(hp);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("colliderHoja").objectReferenceValue = hoja.GetComponent<Collider>();
            so.FindProperty("umbral").objectReferenceValue = umbral;
            so.FindProperty("tamanoUmbral").vector3Value = new Vector3(1.2f, 2.2f, 1.6f);
            so.FindProperty("conBisagra").boolValue = true;
            so.FindProperty("bisagraLocal").vector3Value = new Vector3(0.5f, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            hoja.SetActive(true);
            return hp;
        }

        private void Muro(string nombre, Vector3 centro, Vector3 tam)
        {
            GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m.name = nombre;
            m.transform.SetParent(raiz.transform, false);
            m.transform.position = centro;
            m.transform.localScale = tam;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Bisagra_AbrirDesdeUnLado_GiraLejosDeQuienAbreYSeCierraSola()
        {
            HojaPuerta hoja = CrearPuerta();
            yield return null;
            Collider col = hoja.GetComponent<Collider>();
            Vector3 bisagra = hoja.BisagraMundo;
            Assert.AreEqual(0.5f, bisagra.x, 0.01f, "La bisagra está en la orilla +X de la hoja");
            Assert.IsTrue(col.enabled, "Cerrada, la hoja choca");

            Vector3 quien = new Vector3(0f, 0f, -1.2f);   // abre desde el lado -Z
            hoja.AbrirDesde(quien);
            Assert.IsTrue(hoja.EstaAbierta);
            Assert.IsFalse(col.enabled, "Abierta, la hoja deja pasar");
            yield return Esperar(config.DuracionBisagra + 0.2f);
            Assert.AreEqual(config.GradosBisagra, Mathf.Abs(hoja.AnguloActual), ToleranciaGrados, "Gira los grados del config");
            Assert.That(config.GradosBisagra, Is.InRange(85f, 110f), "Unos 95°");
            Assert.Greater(hoja.transform.position.z, 0.3f, "La hoja se abrió hacia el otro lado (+Z), lejos de quien abrió");
            Assert.AreEqual(bisagra.x, hoja.BisagraMundo.x, 0.001f, "La bisagra no se movió");
            Assert.AreEqual(0.5f, Vector3.Distance(new Vector3(hoja.transform.position.x, 0f, hoja.transform.position.z), new Vector3(bisagra.x, 0f, bisagra.z)), 0.02f, "La hoja gira alrededor de su orilla");

            // Nadie en el umbral: se cierra sola y vuelve a chocar.
            yield return Esperar(config.SegundosParaCerrar + config.DuracionBisagra + 0.5f);
            Assert.IsFalse(hoja.EstaAbierta, "Se cerró sola");
            Assert.IsTrue(col.enabled);
            Assert.AreEqual(0f, hoja.AnguloActual, 0.5f, "Volvió a su sitio");
            Assert.AreEqual(0f, hoja.transform.position.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator Bisagra_AbrirDesdeElOtroLado_GiraAlReves()
        {
            HojaPuerta hoja = CrearPuerta();
            yield return null;
            hoja.AbrirDesde(new Vector3(0f, 0f, 1.2f));
            yield return Esperar(config.DuracionBisagra + 0.2f);
            Assert.Less(hoja.transform.position.z, -0.3f, "Desde +Z abre hacia -Z");
        }

        // Un cuerpo del tamaño del jugador (cápsula de 0.3 m de radio, de 0.4 a 1.6 m) barrido de un lado del muro al otro por el vano.
        private static bool VanoLibre(out string choque)
        {
            Vector3 a = new Vector3(0f, 0.4f, -1.5f);
            Vector3 b = new Vector3(0f, 1.6f, -1.5f);
            bool golpe = Physics.CapsuleCast(a, b, 0.3f, Vector3.forward, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore);
            choque = golpe ? h.collider.name : "";
            return !golpe;
        }

        [UnityTest]
        public IEnumerator Bisagra_Abierta_UnCuerpoCruzaElVano_YNoSeCierraConAlguienEnElUmbral()
        {
            HojaPuerta hoja = CrearPuerta();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(VanoLibre(out string choque), "Cerrada, la hoja tapa el vano");
            Assert.AreEqual("Hoja", choque);

            // Se abre desde un lado y un cuerpo del tamaño del jugador cruza el vano de lado a lado.
            hoja.AbrirDesde(new Vector3(0f, 0f, -0.5f));
            yield return Esperar(config.DuracionBisagra + 0.1f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(VanoLibre(out choque), "Abierta, un cuerpo cruza el vano de lado a lado (chocó con " + choque + ")");

            // Alguien parado en el umbral (un CharacterController, como el jugador): la hoja no se le cierra encima.
            GameObject cuerpo = new GameObject("Cuerpo");
            cuerpo.transform.SetParent(raiz.transform, false);
            cuerpo.transform.position = new Vector3(0f, 0.05f, -0.5f);
            CharacterController cc = cuerpo.AddComponent<CharacterController>();
            cc.radius = 0.3f;
            cc.height = 1.8f;
            cc.center = Vector3.up * 0.9f;
            yield return new WaitForFixedUpdate();
            Assert.Greater(hoja.OcupantesEnUmbral, 0, "El cuerpo cuenta como ocupante del umbral");
            yield return Esperar(config.SegundosParaCerrar + 1f);
            Assert.IsTrue(hoja.EstaAbierta, "Con alguien en el umbral no se cierra sola");

            // Se va: se cierra sola.
            cuerpo.SetActive(false);
            yield return Esperar(config.SegundosParaCerrar + config.DuracionBisagra + 0.5f);
            Assert.IsFalse(hoja.EstaAbierta, "Sin nadie en el umbral se cierra sola");
        }

        [Test]
        public void SinBisagra_ElComportamientoDeSiempre_NoGira()
        {
            GameObject go = new GameObject("HojaSinBisagra");
            go.SetActive(false); // sin Awake: solo se mira el valor por omisión
            HojaPuerta hoja = go.AddComponent<HojaPuerta>();
            Assert.IsFalse(hoja.ConBisagra, "Por omisión la hoja sube al dintel como antes");
            Assert.AreEqual(0f, hoja.AnguloActual);
            Object.DestroyImmediate(go);
        }
    }
}
