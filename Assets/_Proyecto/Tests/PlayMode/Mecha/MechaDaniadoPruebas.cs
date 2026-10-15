using System.Collections;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-104: con vida baja el mecha camina más lento, gira peor y la alarma roja parpadea. Prefab real, entrada simulada.</summary>
    public class MechaDaniadoPruebas : InputTestFixture
    {
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const float VidaBajaDePrueba = 25f;
        private const float FactorVelocidad = 0.7f;
        private const float FactorAceleracion = 0.6f;
        private const float FactorGiro = 0.75f;
        private const float SegundosCaminando = 3f;
        private const float VentanaMedicion = 1f;
        private const float ToleranciaVelocidad = 0.1f;
        private const float ToleranciaGiro = 0.05f;
        private const float SegundosAceleracion = 0.3f;
        private const float ToleranciaAceleracion = 0.1f;
        private const float SegundosGiro = 1f;
        private const float RatonGigante = 100000f;
        private const float SegundosAlarma = 2f;
        private const int MinimoApagadosEnAlarma = 3;
        private const float DanioMortal = 100000f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private ControladorMecha controlador;
        private VidaMecha vida;
        private AlarmaCabina alarma;
        private DatosMecha datos;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            mecha = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            controlador = mecha.GetComponent<ControladorMecha>();
            vida = mecha.GetComponent<VidaMecha>();
            alarma = mecha.GetComponentInChildren<AlarmaCabina>();
            datos = vida.Datos;
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

        // Los primeros frames de PlayMode son lentos (deltaTime enorme): se espera a que los frames se estabilicen.
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

        // Mantiene W y mide la velocidad real hacia adelante (desplazamiento / tiempo) en el último segundo.
        private IEnumerator MedirVelocidadAdelante(System.Action<float> resultado)
        {
            Press(teclado.wKey);
            float t = 0f;
            float inicioVentana = 0f;
            float zInicioVentana = 0f;
            bool ventanaAbierta = false;
            while (t < SegundosCaminando)
            {
                yield return null;
                t += Time.deltaTime;
                if (!ventanaAbierta && t >= SegundosCaminando - VentanaMedicion)
                {
                    ventanaAbierta = true;
                    inicioVentana = t;
                    zInicioVentana = mecha.transform.position.z;
                }
            }
            float velocidad = (mecha.transform.position.z - zInicioVentana) / (t - inicioVentana);
            Release(teclado.wKey);
            resultado(velocidad);
        }

        // Aceleración medida: velocidad alcanzada en los primeros SegundosAceleracion, dividida entre ese tiempo (m/s²).
        private IEnumerator MedirAceleracion(System.Action<float> resultado)
        {
            Press(teclado.wKey);
            float t = 0f;
            while (t < SegundosAceleracion)
            {
                yield return null;
                t += Time.deltaTime;
            }
            float aceleracion = controlador.VelocidadLocal.y / t;
            Release(teclado.wKey);
            yield return Esperar(2f);
            resultado(aceleracion);
        }

        // Giro máximo medido: grados girados / segundos con el ratón empujando sin parar (°/s).
        private IEnumerator MedirGiro(System.Action<float> resultado)
        {
            float anterior = mecha.transform.eulerAngles.y;
            Set(raton.delta, new Vector2(RatonGigante, 0f));
            yield return null;
            anterior = mecha.transform.eulerAngles.y;
            float grados = 0f;
            float t = 0f;
            while (t < SegundosGiro)
            {
                yield return null;
                float ahora = mecha.transform.eulerAngles.y;
                grados += Mathf.DeltaAngle(anterior, ahora);
                anterior = ahora;
                t += Time.deltaTime;
            }
            resultado(grados / t);
        }

        [UnityTest]
        public IEnumerator Velocidad_ConVidaMaxima_EsLaVelocidadMaximaDeLosDatos()
        {
            yield return Calentar();
            float medida = 0f;
            yield return MedirVelocidadAdelante(v => medida = v);
            Assert.AreEqual(datos.VelocidadMaxima, medida, ToleranciaVelocidad);
        }

        [UnityTest]
        public IEnumerator Velocidad_ConVidaBaja_EsSetentaPorCientoYAceleraMenos()
        {
            yield return Calentar();
            float aceleracionNormal = 0f;
            yield return MedirAceleracion(a => aceleracionNormal = a);

            vida.FijarVida(VidaBajaDePrueba);
            float aceleracionBaja = 0f;
            yield return MedirAceleracion(a => aceleracionBaja = a);
            float medida = 0f;
            yield return MedirVelocidadAdelante(v => medida = v);

            Assert.AreEqual(datos.VelocidadMaxima * FactorVelocidad, medida, ToleranciaVelocidad);
            Assert.Less(aceleracionBaja, aceleracionNormal, "Con vida baja arranca más lento");
            Assert.AreEqual(FactorAceleracion, aceleracionBaja / aceleracionNormal, ToleranciaAceleracion);
        }

        [UnityTest]
        public IEnumerator Giro_ConVidaBaja_EsSetentaYCincoPorCientoDelMaximo()
        {
            yield return Calentar();
            vida.FijarVida(VidaBajaDePrueba);
            float medido = 0f;
            yield return MedirGiro(g => medido = g);
            Assert.AreEqual(datos.VelocidadGiroMaxima * FactorGiro, medido, datos.VelocidadGiroMaxima * ToleranciaGiro);
        }

        [UnityTest]
        public IEnumerator Giro_ConVidaMaxima_EsElMaximoDeLosDatos()
        {
            yield return Calentar();
            float medido = 0f;
            yield return MedirGiro(g => medido = g);
            Assert.AreEqual(datos.VelocidadGiroMaxima, medido, datos.VelocidadGiroMaxima * ToleranciaGiro);
        }

        [UnityTest]
        public IEnumerator Velocidad_SinConfigDeDanio_SeMueveComoAntes()
        {
            yield return Calentar();
            SerializedObject so = new SerializedObject(controlador);
            so.FindProperty("configDanio").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            vida.FijarVida(VidaBajaDePrueba);
            float medida = 0f;
            yield return MedirVelocidadAdelante(v => medida = v);
            Assert.AreEqual(datos.VelocidadMaxima, medida, ToleranciaVelocidad);
        }

        [UnityTest]
        public IEnumerator Alarma_ConVidaBaja_Parpadea()
        {
            yield return Calentar();
            Assert.IsNotNull(alarma, "El prefab lleva AlarmaCabina");
            vida.FijarVida(VidaBajaDePrueba);
            bool anterior = alarma.Prendida;
            int apagados = 0;
            bool vioPrendida = false;
            float t = 0f;
            while (t < SegundosAlarma)
            {
                yield return null;
                t += Time.deltaTime;
                vioPrendida |= alarma.Prendida;
                if (anterior && !alarma.Prendida)
                {
                    apagados++;
                }
                anterior = alarma.Prendida;
            }
            Assert.IsTrue(vioPrendida, "La alarma se prende");
            Assert.GreaterOrEqual(apagados, MinimoApagadosEnAlarma);
        }

        [UnityTest]
        public IEnumerator Alarma_ConVidaAlta_EstaApagadaTodoElTiempo()
        {
            yield return Calentar();
            Assert.IsNotNull(alarma);
            float t = 0f;
            while (t < SegundosAlarma)
            {
                yield return null;
                t += Time.deltaTime;
                Assert.IsFalse(alarma.Prendida);
            }
        }

        [UnityTest]
        public IEnumerator Alarma_AlReparar_SeApaga()
        {
            yield return Calentar();
            vida.FijarVida(VidaBajaDePrueba);
            yield return Esperar(0.7f);
            mecha.GetComponent<MechaPersistente>().Reparar();
            Assert.IsFalse(alarma.Prendida, "Apagada al instante");
            float t = 0f;
            while (t < SegundosAlarma)
            {
                yield return null;
                t += Time.deltaTime;
                Assert.IsFalse(alarma.Prendida);
            }
        }

        [UnityTest]
        public IEnumerator Alarma_EnKO_QuedaPrendidaFija()
        {
            yield return Calentar();
            vida.RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO);
            float t = 0f;
            while (t < SegundosAlarma)
            {
                yield return null;
                t += Time.deltaTime;
                Assert.IsTrue(alarma.Prendida);
            }
        }
    }
}
