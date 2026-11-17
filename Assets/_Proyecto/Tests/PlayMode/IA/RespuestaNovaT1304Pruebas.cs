using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1304: con 4 estrellas llega Nova (guardias robot y un guardia pesado) cerca del jugador y va por él; con 5, más y el jefe final; bajo 4 se retira.
    /// </summary>
    public class RespuestaNovaT1304Pruebas
    {
        private sealed class JugadorQuieto : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) => Raiz.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            public void Poner(float valor)
            {
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                NivelCambio?.Invoke(Nivel);
            }
            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        private sealed class ZonaDoble : IZonaBusqueda
        {
            public bool Activa => true;
            public Vector3 Centro => Vector3.zero;
            public float Radio => 50f;
            public bool Visto => true;
        }

        private sealed class CuerpoJugador : MonoBehaviour, IRecibeDanio
        {
            public float Recibido;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Recibido += info.Cantidad;
                return new ResultadoDanio(0f, TipoBloqueo.Ninguno, false); // no muere: la prueba mide que lo atacan
            }
        }

        private GameObject piso;
        private GameObject jugador;
        private CalorDoble calor;
        private ZonaDoble zona;
        private RespuestaNova respuesta;
        private bool habiaActual;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            jugador = new GameObject("JugadorNova");
            jugador.AddComponent<CapsuleCollider>().center = Vector3.up;
            jugador.AddComponent<CuerpoJugador>();
            Registro<IJugador>.Agregar(new JugadorQuieto { Raiz = jugador.transform });
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            zona = new ZonaDoble();
            Registro<IZonaBusqueda>.Agregar(zona);
            habiaActual = RespuestaNova.Actual != null;
            if (habiaActual)
            {
                RespuestaNova.Actual.enabled = false; // la de la partida no estorba: esta prueba usa la suya
            }
            ConfigRespuestaNova config = Resources.Load<ConfigRespuestaNova>(ConfigRespuestaNova.Recurso);
            Assert.IsNotNull(config, "IA/Resources/ConfigRespuestaNova");
            Assert.IsNotNull(config.Guardia, "el guardia de Nova");
            Assert.IsNotNull(config.GuardiaPesado, "el guardia pesado");
            Assert.IsNotNull(config.JefeFinal, "el jefe final");
            respuesta = new GameObject("RespuestaNovaPrueba").AddComponent<RespuestaNova>();
            respuesta.Configurar(config);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = 1f;
            Object.DestroyImmediate(respuesta.gameObject);
            Object.DestroyImmediate(piso);
            Object.DestroyImmediate(jugador);
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            if (habiaActual && RespuestaNova.Actual != null)
            {
                RespuestaNova.Actual.enabled = true;
            }
            Registros.LimpiarTodos();
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
        public IEnumerator TresEstrellas_NoVieneNova()
        {
            calor.Poner(55f); // 3 estrellas
            yield return Esperar(1.5f);
            Assert.AreEqual(3, EstrellasBusqueda.De(calor));
            Assert.AreEqual(0, respuesta.GuardiasVivos + respuesta.PesadosVivos);
            Assert.IsFalse(respuesta.HayJefe);
        }

        [UnityTest]
        public IEnumerator CuatroEstrellas_LlegaNova_CercaYVaPorElJugador_CincoTraeAlJefe_YBajoCuatroSeRetira()
        {
            calor.Poner(75f); // 4 estrellas
            yield return Esperar(1.5f);
            Assert.AreEqual(4, respuesta.Estrellas);
            Assert.AreEqual(3, respuesta.GuardiasVivos, "con 4 estrellas: 3 guardias robot de Nova");
            Assert.AreEqual(1, respuesta.PesadosVivos, "y un guardia pesado");
            Assert.IsFalse(respuesta.HayJefe, "el jefe final solo con 5");
            foreach (PandilleroAPie p in respuesta.Guardias.Concat(respuesta.Pesados))
            {
                float d = Vector3.Distance(Plano(p.transform.position), Plano(jugador.transform.position));
                Assert.That(d, Is.InRange(21.5f, 34.5f), p.name + " aparece a 22-34 m");
                Assert.IsTrue(p.Alerta, p.name + " está en alerta contra el jugador");
            }
            float antes = respuesta.Guardias.Average(p => Vector3.Distance(Plano(p.transform.position), Plano(jugador.transform.position)));
            Time.timeScale = 3f;
            yield return Esperar(12f);
            Time.timeScale = 1f;
            float despues = respuesta.Guardias.Where(p => p != null).Average(p => Vector3.Distance(Plano(p.transform.position), Plano(jugador.transform.position)));
            Debug.Log("[T-1304] distancia media de los guardias: " + antes + " -> " + despues + "; daño al jugador " + jugador.GetComponent<CuerpoJugador>().Recibido);
            Assert.Less(despues, antes - 5f, "Nova va por el jugador");

            calor.Poner(95f); // 5 estrellas
            yield return Esperar(1.5f);
            Assert.AreEqual(5, respuesta.Estrellas);
            Assert.IsTrue(respuesta.HayJefe, "con 5 estrellas llega el jefe final");

            calor.Poner(30f); // 2 estrellas
            yield return Esperar(8f);
            Assert.AreEqual(0, respuesta.GuardiasVivos + respuesta.PesadosVivos, "bajo 4 estrellas Nova se retira");
            Assert.IsFalse(respuesta.HayJefe);
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
