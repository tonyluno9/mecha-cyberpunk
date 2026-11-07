using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-695: el humo de daño de la moto sale del motor, al centro de la moto, no del aire delante de la rueda. El carro lo sigue sacando del cofre.</summary>
    public class MotoHumoT695Pruebas
    {
        private GameObject piso;
        private GameObject vehiculo;

        [SetUp]
        public void Preparar()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(100f, 1f, 100f);
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(vehiculo);
            Object.DestroyImmediate(piso);
        }

        public static readonly string[] Motos = { "Carro_V07", "Carro_V08", "Carro_C19" };

        private static Bounds Unir(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        private IEnumerator Crear(string nombre)
        {
            vehiculo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/" + nombre + ".prefab"), new Vector3(0f, 0.2f, 0f), Quaternion.identity);
            yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator Moto_ElHumoSaleDelCentroDeLaMoto([ValueSource(nameof(Motos))] string nombre)
        {
            yield return Crear(nombre);
            DanioCarro danio = vehiculo.GetComponent<DanioCarro>();
            Assert.IsNotNull(danio);
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 0.5f, TipoDanio.Disparo, vehiculo.transform.position, Vector3.forward, null));
            Assert.AreEqual(EstadoCarro.Humo, danio.Estado);
            yield return new WaitForSeconds(0.5f);

            Bounds moto = default;
            bool hay = false;
            foreach (Renderer r in vehiculo.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                moto = hay ? Unir(moto, r.bounds) : r.bounds;
                hay = true;
            }
            Vector3 origen = danio.Humo.transform.position;
            Assert.IsTrue(moto.Contains(origen), "El humo nace dentro del cuerpo de la moto, no en el aire");
            Vector3 local = vehiculo.transform.InverseTransformPoint(origen);
            Assert.Less(Mathf.Abs(local.x), 0.15f, "Al centro de la moto");
            Assert.Less(Mathf.Abs(origen.z - moto.center.z), 0.3f, "A media longitud de la moto, no delante de la rueda");
            Assert.Less(origen.y, moto.max.y - 0.2f, "Abajo, donde está el motor");
            Assert.IsTrue(danio.Humo.isPlaying && danio.Humo.emission.rateOverTime.constant > 0f, "Humea");
            Assert.AreEqual(origen, danio.Fuego.transform.position, "El fuego sale del mismo punto");
        }

        [UnityTest]
        public IEnumerator Carro_ElHumoSigueSaliendoDelCofre()
        {
            yield return Crear("Carro_V02");
            DanioCarro danio = vehiculo.GetComponent<DanioCarro>();
            Vector3 local = vehiculo.transform.InverseTransformPoint(danio.Humo.transform.position);
            Assert.AreEqual(1.4f, local.z, 0.01f, "Al frente del carro");
            Assert.AreEqual(0.9f, local.y, 0.01f);
        }
    }
}
