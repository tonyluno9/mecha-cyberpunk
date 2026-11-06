using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-590: luces del carro con la tecla L y faros automáticos de noche (reloj del mundo falso, carro real, entrada simulada).</summary>
    public class LucesCarroPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaCaptura = "docs/capturas/T-590-luces.png";

        private class RelojFalso : IRelojMundo
        {
            public bool Noche;
            public float Hora => Noche ? 23f : 12f;
            public int Dia => 1;
            public FranjaDia Franja => Noche ? FranjaDia.Noche : FranjaDia.Dia;
            public bool EsDeNoche => Noche;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 19f;
            public bool Pausado => false;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }
        }

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private CarroViejo carro;
        private LucesCarro luces;
        private RelojFalso reloj;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            MapasDePruebaVehiculos.Reiniciar(teclado);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            reloj = new RelojFalso();
            Registro<IRelojMundo>.Agregar(reloj);
        }

        public override void TearDown()
        {
            Registro<IRelojMundo>.Quitar(reloj);
            if (carroObjeto != null)
            {
                Object.DestroyImmediate(carroObjeto);
            }
            Object.DestroyImmediate(piso);
            MapasDePruebaVehiculos.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDePruebaVehiculos.DespuesDelTearDown();
        }

        private void CrearCarro(string ruta = RutaPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta el prefab " + ruta);
            carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            luces = carroObjeto.GetComponent<LucesCarro>();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private int LucesPrendidas() => luces.Luces.Count(l => l.enabled && l.intensity > 0.1f);

        [UnityTest]
        public IEnumerator TeclaL_PrendeYApagaLasLuces_DelCarroQueSeManeja()
        {
            CrearCarro();
            yield return Esperar(0.2f);
            Assert.IsNotNull(luces, "El carro trae sus luces");
            Assert.AreEqual(4, luces.Luces.Length, "2 faros y 2 calaveras");
            Assert.AreEqual(2, luces.Luces.Count(l => l.type == LightType.Spot), "Los faros son reflectores");
            Assert.AreEqual(0, LucesPrendidas(), "Estacionado de día: apagadas");

            Press(teclado.lKey);
            yield return null;
            Release(teclado.lKey);
            yield return Esperar(0.2f);
            Assert.AreEqual(0, LucesPrendidas(), "Sin conductor la tecla L no hace nada");

            Assert.IsTrue(carro.IntentarSubir(null));
            yield return null;
            Press(teclado.lKey);
            yield return null;
            Release(teclado.lKey);
            yield return Esperar(1.5f);
            Assert.IsTrue(luces.Encendidas);
            Assert.AreEqual(4, LucesPrendidas(), "Con L se prenden los 4");
            Assert.AreEqual(1f, luces.Brillo, 0.01f);
            Assert.Greater(luces.Luces[0].intensity, 1f);

            Press(teclado.lKey);
            yield return null;
            Release(teclado.lKey);
            yield return Esperar(1.5f);
            Assert.IsFalse(luces.Encendidas);
            Assert.AreEqual(0, LucesPrendidas(), "Con L otra vez se apagan");
        }

        [UnityTest]
        public IEnumerator DeNoche_ElTraficoLlevaFarosPrendidos_YElEstacionadoNo()
        {
            reloj.Noche = true;
            CrearCarro();
            CarroViejo parqueado = carro;
            LucesCarro lucesParqueado = luces;
            yield return Esperar(1.5f);
            Assert.AreEqual(0, lucesParqueado.Luces.Count(l => l.enabled), "Estacionado y vacío de noche: apagadas");

            // Tráfico: cuerpo cinemático.
            parqueado.GetComponent<Rigidbody>().isKinematic = true;
            yield return Esperar(0.4f);
            Assert.Greater(luces.Brillo, 0f, "Empieza a prender");
            Assert.Less(luces.Brillo, 1f, "pero no de golpe");
            yield return Esperar(1.5f);
            Assert.AreEqual(1f, luces.Brillo, 0.01f);
            Assert.AreEqual(4, LucesPrendidas(), "De noche el tráfico lleva faros y calaveras");

            reloj.Noche = false;
            yield return Esperar(1.5f);
            Assert.AreEqual(0, LucesPrendidas(), "De día se apagan");
        }

        [UnityTest]
        public IEnumerator Manual_GanaAlAutomatico_YAlBajarseVuelveAlAutomatico()
        {
            reloj.Noche = true;
            CrearCarro();
            Assert.IsTrue(carro.IntentarSubir(null));
            yield return Esperar(1.5f);
            Assert.IsTrue(luces.Encendidas, "De noche, al subir las luces ya están prendidas");

            Press(teclado.lKey);
            yield return null;
            Release(teclado.lKey);
            yield return Esperar(1.5f);
            Assert.IsFalse(luces.Encendidas, "L las apaga aunque sea de noche");

            carro.PermitirBajarYa();
            Assert.IsTrue(carro.IntentarBajar(out Pose _));
            yield return Esperar(1.5f);
            Assert.IsFalse(luces.Encendidas, "Sin conductor y sin tráfico, apagadas");
            Assert.IsTrue(carro.IntentarSubir(null));
            yield return Esperar(1.5f);
            Assert.IsTrue(luces.Encendidas, "Otro viaje de noche: automáticas de nuevo");
        }

        [UnityTest]
        public IEnumerator CarroExplotado_NoPrendeLasLuces()
        {
            CrearCarro();
            Assert.IsTrue(carro.IntentarSubir(null));
            Press(teclado.lKey);
            yield return null;
            Release(teclado.lKey);
            yield return Esperar(1.2f);
            Assert.IsTrue(luces.Encendidas);
            DanioCarro danio = carroObjeto.GetComponent<DanioCarro>();
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 2f, TipoDanio.Disparo, carroObjeto.transform.position, Vector3.forward, null));
            yield return Esperar(1.5f);
            Assert.IsTrue(danio.Explotado);
            Assert.AreEqual(0, LucesPrendidas(), "Al explotar se apagan");
            luces.Alternar();
            yield return Esperar(0.3f);
            Assert.IsFalse(luces.Encendidas, "y la tecla ya no las prende");
        }

        [UnityTest]
        public IEnumerator Captura_FarosPrendidosDeNoche()
        {
            reloj.Noche = true;
            RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.09f);
            CrearCarro();
            carroObjeto.transform.position = new Vector3(0f, 0.3f, 0f);
            Assert.IsTrue(carro.IntentarSubir(null));
            yield return Esperar(1.5f);
            GameObject camObj = new GameObject("CamCaptura");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
            cam.transform.position = new Vector3(-4.5f, 2.2f, 7.5f);
            cam.transform.LookAt(new Vector3(0f, 0.8f, 1f));
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = new Vector3(0f, 2f, 16f);
            pared.transform.localScale = new Vector3(14f, 4f, 0.5f);
            yield return Esperar(0.3f);
            CapturaPruebasVehiculos.Tomar(cam, RutaCaptura);
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(pared);
            Assert.IsTrue(System.IO.File.Exists(RutaCaptura));
        }

        [UnityTest]
        public IEnumerator T659_LasLucesNoTienenCuadrosFalsos()
        {
            CrearCarro("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V02.prefab");
            yield return Esperar(0.2f);
            Assert.AreEqual(4, luces.Luces.Length);
            foreach (Light luz in luces.Luces)
            {
                Assert.AreEqual(0, luz.GetComponentsInChildren<Renderer>(true).Length, "La luz " + luz.name + " no lleva lentes ni cuadros");
            }
            foreach (Transform t in carroObjeto.GetComponentsInChildren<Transform>(true))
            {
                Assert.IsFalse(t.name.Contains("Lente"), "Sobra un lente: " + t.name);
            }
            foreach (MeshFilter mf in carroObjeto.GetComponentsInChildren<MeshFilter>(true))
            {
                Renderer dibujo = mf.GetComponent<Renderer>();
                if (dibujo == null || !dibujo.enabled)
                {
                    continue;
                }
                string malla = mf.sharedMesh != null ? mf.sharedMesh.name : "";
                Assert.IsFalse(malla == "Cube" || malla == "Quad", "Sobra una primitiva (" + malla + ") en " + mf.name + " que simula un faro");
            }
        }

        // Las luces de fuego y de explosión (daño) existen aparte; aquí solo cuentan faros y calaveras.
        private int CuantasLucesDeFaros()
        {
            return carroObjeto.GetComponentsInChildren<Light>(true).Count(l => l.name.StartsWith("Faro") || l.name.StartsWith("Calavera"));
        }

        [UnityTest]
        public IEnumerator T660_LaMotoNoLlevaLuces()
        {
            reloj.Noche = true;
            CrearCarro("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V07.prefab");
            yield return Esperar(0.3f);
            Assert.IsNull(carroObjeto.GetComponent<LucesCarro>(), "La moto no lleva LucesCarro");
            Assert.AreEqual(0, CuantasLucesDeFaros(), "Ni faros ni calaveras");
            Assert.IsTrue(carro.IntentarSubir(null));
            yield return Esperar(1.5f);
            Assert.AreEqual(0, CuantasLucesDeFaros(), "Tampoco de noche y con conductor");
        }

        [UnityTest]
        public IEnumerator Captura_S1_LucesDelCarroDeNoche()
        {
            reloj.Noche = true;
            RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.09f);
            CrearCarro("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V02.prefab");
            carroObjeto.transform.position = new Vector3(0f, 0.3f, 0f);
            Assert.IsTrue(carro.IntentarSubir(null));
            yield return Esperar(1.5f);
            GameObject camObj = new GameObject("CamCaptura");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
            cam.transform.position = new Vector3(-4f, 1.6f, 6.5f);
            cam.transform.LookAt(new Vector3(0f, 0.7f, 1.5f));
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = new Vector3(0f, 2f, 14f);
            pared.transform.localScale = new Vector3(14f, 4f, 0.5f);
            yield return Esperar(0.3f);
            CapturaPruebasVehiculos.Tomar(cam, "docs/capturas/s1-luces-carro-noche.png");
            cam.transform.position = new Vector3(3.5f, 1.4f, -6.5f);
            cam.transform.LookAt(new Vector3(0f, 0.8f, -1.5f));
            yield return Esperar(0.2f);
            CapturaPruebasVehiculos.Tomar(cam, "docs/capturas/s1-luces-carro-noche-atras.png");
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(pared);
        }
    }
}
