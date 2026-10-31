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
    /// T-444: el carro recibe daño (balas, golpes, choques), saca humo, se prende y explota; la explosión daña lo cercano y no lo lejano,
    /// y saca de golpe a quien lo maneja. Prefab real sobre un piso creado aquí.
    /// </summary>
    public class DanioCarroPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private sealed class Victima : MonoBehaviour, IRecibeDanio
        {
            public float Recibido;
            public TipoDanio UltimoTipo;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Recibido += info.Cantidad;
                UltimoTipo = info.Tipo;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private GameObject piso;
        private GameObject carroObjeto;
        private CarroViejo carro;
        private DanioCarro danio;
        private readonly System.Collections.Generic.List<GameObject> extras = new System.Collections.Generic.List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            EventosMontaje.LimpiarSuscriptores();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaPrefab);
            carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            danio = carroObjeto.GetComponent<DanioCarro>();
        }

        [TearDown]
        public void Limpiar()
        {
            EventosMontaje.LimpiarSuscriptores();
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            if (carroObjeto != null)
            {
                Object.DestroyImmediate(carroObjeto);
            }
            Object.DestroyImmediate(piso);
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

        private Victima CrearVictima(Vector3 posicion)
        {
            GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            objeto.name = "Victima";
            objeto.transform.position = posicion;
            Victima victima = objeto.AddComponent<Victima>();
            extras.Add(objeto);
            return victima;
        }

        private float Pegar(float cantidad) =>
            danio.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Disparo, carroObjeto.transform.position, Vector3.forward, null)).DanioAplicado;

        [UnityTest]
        public IEnumerator Carro_TieneDanioYEsInerte_YLasBalasLoDanian()
        {
            yield return Calentar();
            Assert.IsNotNull(danio, "CarroViejo agrega DanioCarro al nacer");
            Assert.IsInstanceOf<IObjetoInerte>(danio, "Un carro no sangra");
            Assert.AreEqual(EstadoCarro.Sano, danio.Estado);
            float maxima = danio.VidaMaxima;
            Assert.AreEqual(25f, Pegar(25f), 0.001f);
            Assert.AreEqual(maxima - 25f, danio.Vida, 0.001f);
        }

        [UnityTest]
        public IEnumerator Carro_ConPocaVida_SacaHumo_ConMenos_Fuego_YAlFinalExplota()
        {
            yield return Calentar();
            float maxima = danio.VidaMaxima;
            Assert.AreEqual(0f, danio.Humo.emission.rateOverTime.constant, 0.001f, "Sano no humea");
            Pegar(maxima * 0.5f);
            Assert.AreEqual(EstadoCarro.Humo, danio.Estado);
            Assert.Greater(danio.Humo.emission.rateOverTime.constant, 0f, "Con la mitad sale humo");
            Assert.AreEqual(0f, danio.Fuego.emission.rateOverTime.constant, 0.001f, "Todavía sin fuego");
            Pegar(maxima * 0.3f);
            Assert.AreEqual(EstadoCarro.Fuego, danio.Estado);
            Assert.Greater(danio.Fuego.emission.rateOverTime.constant, 0f, "Con poco queda en llamas");
            Assert.IsFalse(danio.Explotado);
            Pegar(maxima);
            Assert.AreEqual(EstadoCarro.Destruido, danio.Estado);
            Assert.IsTrue(danio.Explotado, "Sin vida explota");
            Assert.IsFalse(carro.enabled, "El armazón ya no se maneja");
            Assert.AreEqual(0f, Pegar(10f), 0.001f, "Un carro destruido ya no recibe daño");
        }

        [UnityTest]
        public IEnumerator Explosion_DanaLoCercano_NoLoLejano_YSacaALaPersona()
        {
            yield return Calentar();
            Victima cerca = CrearVictima(new Vector3(3f, 1f, 0f));
            Victima lejos = CrearVictima(new Vector3(40f, 1f, 0f));
            bool pidioSalir = false;
            EventosMontaje.SalidaForzada += montable =>
            {
                pidioSalir = ReferenceEquals(montable, carro);
                carro.IntentarBajar(out Pose _);
            };
            Assert.IsTrue(carro.IntentarSubir(null), "Alguien sube al carro");
            Assert.IsTrue(carro.Ocupado);
            Pegar(danio.VidaMaxima * 2f);
            yield return null;
            Assert.IsTrue(pidioSalir, "La explosión pide que baje quien lo maneja");
            Assert.IsFalse(carro.Ocupado, "y baja aunque vaya rápido");
            Assert.Greater(cerca.Recibido, 0f, "Quien está cerca recibe daño");
            Assert.AreEqual(TipoDanio.Explosion, cerca.UltimoTipo);
            Assert.AreEqual(0f, lejos.Recibido, 0.001f, "Quien está lejos no recibe nada");
        }

        [UnityTest]
        public IEnumerator Choque_FuerteDanaAlCarro_YLeveNo()
        {
            yield return Calentar();
            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.name = "Muro";
            muro.transform.position = new Vector3(0f, 1.5f, 7f);
            muro.transform.localScale = new Vector3(10f, 3f, 1f);
            extras.Add(muro);
            Rigidbody cuerpo = carroObjeto.GetComponent<Rigidbody>();
            float maxima = danio.VidaMaxima;
            // Lento: no cuenta.
            cuerpo.linearVelocity = new Vector3(0f, 0f, 3f);
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(maxima, danio.Vida, 0.001f, "Un roce lento no daña");
            // Rápido: le pega al muro.
            carroObjeto.transform.position = new Vector3(0f, 0.3f, 0f);
            cuerpo.linearVelocity = new Vector3(0f, 0f, 25f);
            yield return new WaitForSeconds(1f);
            Assert.Less(danio.Vida, maxima, "Un choque a toda velocidad lo daña");
        }
    }
}
