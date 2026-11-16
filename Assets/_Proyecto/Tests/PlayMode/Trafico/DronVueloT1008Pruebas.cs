using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1008: un dron de policía vuela de A a B con inercia: no pasa de largo más de lo permitido y su rotación no da saltos entre cuadros.</summary>
    public class DronVueloT1008Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const float DistanciaInicial = 40f;
        private const float PasoDeLargoMaximo = 2f;
        private const float GradosMaximosPorCuadro = 15f;
        private const float SegundosDeVuelo = 14f;
        private const float CuadrosDeAsentamiento = 3;

        private GameObject jugador;
        private GameObject dron;

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(dron);
            Object.DestroyImmediate(jugador);
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator DronVaDeAaB_SinPasarseDeLargo_YSinSaltosDeRotacion()
        {
            Registros.LimpiarTodos();
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = Vector3.zero;
            dron = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron));
            dron.transform.position = new Vector3(0f, config.AlturaDron, -DistanciaInicial);
            DronMundo comp = dron.GetComponent<DronMundo>();
            comp.Preparar(config.Crear(), config, () => jugador.transform.position);
            comp.AsignarOrbita(0, 1);
            // La meta del dron 0 es el punto del círculo a su altura sobre el jugador; se queda a la distancia de seguimiento de él.
            Vector3 meta = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, 0, 1, config.RadioOrbitaDron, config.AlturaDron);
            Vector3 sentido = (meta - dron.transform.position).normalized;
            meta -= sentido * config.Crear().DistanciaSeguimiento; // se detiene a la distancia de seguimiento de su punto
            Quaternion anterior = dron.transform.rotation;
            float mayorGiro = 0f;
            float pasoDeLargo = float.MinValue;
            float mayorRapidez = 0f;
            Vector3 posAnterior = dron.transform.position;
            int cuadro = 0;
            float fin = Time.time + SegundosDeVuelo;
            while (Time.time < fin)
            {
                yield return null;
                cuadro++;
                Transform t = dron.transform;
                if (cuadro > CuadrosDeAsentamiento)
                {
                    mayorGiro = Mathf.Max(mayorGiro, Quaternion.Angle(anterior, t.rotation));
                }
                mayorRapidez = Mathf.Max(mayorRapidez, Vector3.ProjectOnPlane(t.position - posAnterior, Vector3.up).magnitude / Mathf.Max(Time.deltaTime, 0.0001f));
                pasoDeLargo = Mathf.Max(pasoDeLargo, Vector3.Dot(t.position - meta, sentido));
                anterior = t.rotation;
                posAnterior = t.position;
            }
            float restante = Vector3.Distance(dron.transform.position, meta);
            Debug.Log("[T-1008] pasoDeLargo " + pasoDeLargo + " m, mayor giro " + mayorGiro + " grados/cuadro, mayor rapidez " + mayorRapidez + " m/s, restante " + restante);
            Assert.LessOrEqual(pasoDeLargo, PasoDeLargoMaximo, "No pasa de largo de su punto (m)");
            Assert.LessOrEqual(mayorGiro, GradosMaximosPorCuadro, "La rotación no da saltos (grados por cuadro)");
            Assert.LessOrEqual(restante, 1f, "Llegó y se quedó flotando sobre su punto (m)");
            Assert.LessOrEqual(mayorRapidez, config.ParametrosVuelo.VelocidadMaxima * 1.2f, "No rebasa su velocidad máxima");
        }
    }
}
