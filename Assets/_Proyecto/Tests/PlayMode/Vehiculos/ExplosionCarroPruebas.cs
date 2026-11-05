using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-591: al reventar, el carro suelta fuego, humo, chispas, onda, destello de luz, estruendo y empuja lo cercano.</summary>
    public class ExplosionCarroPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaCaptura = "docs/capturas/T-591-explosion.png";

        private sealed class SonidosFalsos : ISonidos
        {
            public readonly List<string> Ids = new List<string>();
            public void Reproducir(string id) { Ids.Add(id); }
            public void ReproducirEn(string id, Vector3 posicion) { Ids.Add(id); }
            public int ReproducirBucle(string id, Transform sigue) { return Sonidos.SinBucle; }
            public void DetenerBucle(int bucle) { }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) { return 1f; }
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private GameObject piso;
        private GameObject carroObjeto;
        private DanioCarro danio;
        private SonidosFalsos sonidos;
        private readonly List<GameObject> extras = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            EventosMontaje.LimpiarSuscriptores();
            sonidos = new SonidosFalsos();
            Registro<ISonidos>.Agregar(sonidos);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            danio = carroObjeto.GetComponent<DanioCarro>();
        }

        [TearDown]
        public void Limpiar()
        {
            Registro<ISonidos>.Quitar(sonidos);
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            foreach (ExplosionCarro e in Object.FindObjectsByType<ExplosionCarro>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(e.gameObject);
            }
            if (carroObjeto != null)
            {
                Object.DestroyImmediate(carroObjeto);
            }
            Object.DestroyImmediate(piso);
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
        public IEnumerator Reventar_CreaFuegoHumoChispasOndaLuzYSonido()
        {
            yield return Esperar(0.3f);
            Assert.IsNull(danio.UltimaExplosion, "Antes de reventar no hay explosión");
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 2f, TipoDanio.Disparo, carroObjeto.transform.position, Vector3.forward, null));
            yield return null;
            yield return null;
            ExplosionCarro e = danio.UltimaExplosion;
            Assert.IsNotNull(e, "Al reventar se crea la explosión");
            Assert.Greater(e.Fuego.particleCount, 0, "fuego");
            Assert.Greater(e.Humo.particleCount, 0, "humo");
            Assert.Greater(e.Chispas.particleCount, 0, "chispas");
            Assert.Greater(e.Onda.particleCount, 0, "onda expansiva");
            Assert.IsTrue(e.Luz.enabled);
            Assert.Greater(e.Luz.intensity, 2f, "la luz destella fuerte");
            Assert.IsNotNull(e.Fuente.clip, "estruendo");
            Assert.Greater(e.Fuente.clip.length, 0.5f);
            Assert.Contains("VEH_CHOQUE_FUERTE", sonidos.Ids, "y suena por el gestor");
            yield return Esperar(0.6f);
            Assert.Less(e.Luz.intensity, 1f, "el destello se apaga");
        }

        [UnityTest]
        public IEnumerator Reventar_EmpujaRigidbodiesCercanos_NoLosLejanos()
        {
            yield return Esperar(0.3f);
            Rigidbody cerca = CrearCaja(new Vector3(3f, 0.5f, 0f));
            Rigidbody lejos = CrearCaja(new Vector3(60f, 0.5f, 0f));
            yield return Esperar(0.2f);
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 2f, TipoDanio.Disparo, carroObjeto.transform.position, Vector3.forward, null));
            yield return null;
            yield return null;
            yield return Esperar(0.3f);
            Assert.Greater(cerca.transform.position.x, 4f, "La onda aleja la caja cercana del carro (x=" + cerca.transform.position.x + ")");
            Assert.AreEqual(60f, lejos.transform.position.x, 0.05f, "La lejana ni se entera");
        }

        private Rigidbody CrearCaja(Vector3 posicion)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.transform.position = posicion;
            Rigidbody rb = caja.AddComponent<Rigidbody>();
            extras.Add(caja);
            return rb;
        }

        [UnityTest]
        public IEnumerator Captura_Explosion()
        {
            yield return Esperar(0.3f);
            RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.2f);
            GameObject camObj = new GameObject("CamCaptura");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            cam.transform.position = new Vector3(-6f, 3f, -8f);
            cam.transform.LookAt(new Vector3(0f, 1.6f, 0f));
            extras.Add(camObj);
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 2f, TipoDanio.Disparo, carroObjeto.transform.position, Vector3.forward, null));
            yield return Esperar(0.4f);
            CapturaPruebasVehiculos.Tomar(cam, RutaCaptura);
            Assert.IsTrue(System.IO.File.Exists(RutaCaptura));
        }
    }
}
