using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-932 (playtest 9-nov): un carro intacto no deja fuego ni humo al avanzar (ningún sistema de partículas, hijos incluidos, emite) y, en estado
    /// Fuego, las llamas van en espacio local del carro (sin estela).
    /// </summary>
    public class FuegoCarroT932Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const int CuadrosDeMarcha = 60;
        private const float SegundosDeMarcha = 2f;
        private const float PasoPorCuadro = 0.05f;

        private GameObject piso;
        private GameObject carroObjeto;
        private DanioCarro danio;

        [SetUp]
        public void Preparar()
        {
            EventosMontaje.LimpiarSuscriptores();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaPrefab);
            carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            danio = carroObjeto.GetComponent<DanioCarro>();
            Rigidbody cuerpo = carroObjeto.GetComponent<Rigidbody>();
            if (cuerpo != null)
            {
                cuerpo.isKinematic = true;
            }
        }

        [TearDown]
        public void Limpiar()
        {
            EventosMontaje.LimpiarSuscriptores();
            Object.DestroyImmediate(carroObjeto);
            Object.DestroyImmediate(piso);
        }

        private static int Particulas(Transform raiz)
        {
            int total = 0;
            foreach (ParticleSystem sistema in raiz.GetComponentsInChildren<ParticleSystem>(true))
            {
                total += sistema.particleCount;
            }
            return total;
        }

        // Mueve el carro un poco cada cuadro durante SegundosDeMarcha segundos de juego (o al menos CuadrosDeMarcha cuadros).
        private IEnumerator Avanzar(int cuadros, System.Action cadaCuadro = null)
        {
            float fin = Time.time + SegundosDeMarcha;
            for (int i = 0; i < cuadros || Time.time < fin; i++)
            {
                carroObjeto.transform.position += Vector3.forward * PasoPorCuadro;
                yield return null;
                cadaCuadro?.Invoke();
            }
        }

        [UnityTest]
        public IEnumerator CarroIntacto_Avanzando_NingunSistemaEmite()
        {
            yield return null;
            Assert.AreEqual(EstadoCarro.Sano, danio.Estado);
            Transform fuego = carroObjeto.transform.Find("FuegoPack");
            Transform humo = carroObjeto.transform.Find("HumoPack");
            Assert.IsNotNull(fuego, "el carro trae su fuego del pack (apagado)");
            Assert.IsNotNull(humo, "el carro trae su humo del pack (apagado)");
            foreach (ParticleSystem sistema in fuego.GetComponentsInChildren<ParticleSystem>(true))
            {
                Assert.AreEqual(0f, sistema.emission.rateOverTime.constant, 0.0001f, sistema.name + ": sin emisión por tiempo");
                Assert.AreEqual(0f, sistema.emission.rateOverDistance.constant, 0.0001f, sistema.name + ": sin emisión por distancia");
            }
            int maximoFuego = 0;
            int maximoHumo = 0;
            yield return Avanzar(CuadrosDeMarcha, () =>
            {
                maximoFuego = Mathf.Max(maximoFuego, Particulas(fuego));
                maximoHumo = Mathf.Max(maximoHumo, Particulas(humo));
            });
            Assert.AreEqual(0, maximoFuego, "ningún sistema del fuego (hijos incluidos) saca partículas con el carro intacto en marcha");
            Assert.AreEqual(0, maximoHumo, "ningún sistema del humo saca partículas con el carro intacto en marcha");
        }

        [UnityTest]
        public IEnumerator CarroEnFuego_LasLlamasVanEnEspacioLocal_Y_SalenParticulas()
        {
            yield return null;
            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 0.85f, TipoDanio.Disparo, carroObjeto.transform.position, Vector3.forward, null));
            Assert.AreEqual(EstadoCarro.Fuego, danio.Estado);
            Transform fuego = carroObjeto.transform.Find("FuegoPack");
            foreach (ParticleSystem sistema in fuego.GetComponentsInChildren<ParticleSystem>(true))
            {
                Assert.AreEqual(ParticleSystemSimulationSpace.Local, sistema.main.simulationSpace, sistema.name + ": llamas en espacio local del carro");
            }
            int maximo = 0;
            yield return Avanzar(CuadrosDeMarcha, () => maximo = Mathf.Max(maximo, Particulas(fuego)));
            Assert.Greater(maximo, 0, "con el carro en llamas sí sale fuego");
            // Sin estela: las partículas viajan con el carro, así que quedan cerca de él aunque haya avanzado muchos metros.
            ParticleSystem principal = danio.Fuego;
            ParticleSystem.Particle[] particulas = new ParticleSystem.Particle[principal.main.maxParticles];
            int n = principal.GetParticles(particulas);
            for (int i = 0; i < n; i++)
            {
                Assert.Less(particulas[i].position.magnitude, 6f, "las llamas siguen al carro, no se quedan atrás");
            }
        }
    }
}
