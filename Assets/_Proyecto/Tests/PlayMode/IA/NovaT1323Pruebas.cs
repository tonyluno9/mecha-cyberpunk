using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1323: los agentes de Nova caen del cielo con destello, un disparo a la cabeza les hace daño (x3), el que ya te vio sigue persiguiéndote aunque baje la
    /// búsqueda, con 5 estrellas suena el aviso especial y el guardia pesado es más grande.
    /// </summary>
    public class NovaT1323Pruebas
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
            public ResultadoDanio RecibirDanio(InfoDanio info) => new ResultadoDanio(0f, TipoBloqueo.Ninguno, false);
        }

        private GameObject piso;
        private GameObject jugador;
        private CalorDoble calor;
        private RespuestaNova respuesta;
        private ConfigRespuestaNova config;
        private bool habiaActual;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            jugador = new GameObject("JugadorNova1323");
            jugador.AddComponent<CapsuleCollider>().center = Vector3.up;
            jugador.AddComponent<CuerpoJugador>();
            Registro<IJugador>.Agregar(new JugadorQuieto { Raiz = jugador.transform });
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            Registro<IZonaBusqueda>.Agregar(new ZonaDoble());
            habiaActual = RespuestaNova.Actual != null;
            if (habiaActual)
            {
                RespuestaNova.Actual.enabled = false;
            }
            config = Resources.Load<ConfigRespuestaNova>(ConfigRespuestaNova.Recurso);
            Assert.IsNotNull(config, "IA/Resources/ConfigRespuestaNova");
            respuesta = new GameObject("RespuestaNovaPrueba1323").AddComponent<RespuestaNova>();
            respuesta.Configurar(config);
            DirectorRespuesta.DepurarForzarPuertaNova(true); // T-1660: estas pruebas son de Nova, no de su puerta
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
            foreach (DestelloLlegadaNova d in Object.FindObjectsByType<DestelloLlegadaNova>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(d.gameObject);
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
        public IEnumerator CuatroEstrellas_LlegaCayendoDesdeArribaConDestello_YAterrizaEnElPiso()
        {
            calor.Poner(75f);
            yield return Esperar(0.6f); // la respuesta revisa cada medio segundo
            Assert.Greater(respuesta.GuardiasVivos, 0, "llegó Nova");
            PandilleroAPie p = respuesta.Guardias.First(g => g != null);
            Assert.Greater(p.transform.position.y, 3f, "aparece unos metros sobre el piso, no en él");
            Assert.IsNotNull(Object.FindFirstObjectByType<DestelloLlegadaNova>(), "hay un destello de llegada");
            yield return Esperar(3f);
            Assert.Less(p.transform.position.y, 0.6f, "cayó y está parado en el piso");
        }

        [UnityTest]
        public IEnumerator DisparoALaCabeza_ElColliderDeLaCabezaLlevaAlAgente_YCuentaComoCabeza()
        {
            calor.Poner(75f);
            yield return Esperar(3.5f); // ya aterrizó
            PandilleroAPie p = respuesta.Guardias.First(g => g != null);
            ZonaCabezaNova zona = p.GetComponent<ZonaCabezaNova>();
            Assert.IsNotNull(zona, "el agente de Nova trae su zona de la cabeza");
            Assert.IsNotNull(zona.Esfera, "la esfera de la cabeza existe (el modelo es humanoide)");
            Vector3 arriba = zona.Esfera.bounds.center + Vector3.up * 1.5f;
            Assert.IsTrue(Physics.Raycast(arriba, Vector3.down, out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore), "una bala desde arriba pega");
            Assert.AreSame(zona.Esfera, golpe.collider, "lo primero que toca es la cabeza, no pasa por encima");
            IRecibeDanio objetivo = golpe.collider.GetComponentInParent<IRecibeDanio>();
            Assert.AreSame(p, objetivo, "la cabeza manda el daño al agente");
            Assert.IsTrue(ReglaZonaDanio.EsCabeza(golpe.collider, golpe.point), "cuenta como cabeza (x3)");
            float antes = p.Actual;
            objetivo.RecibirDanio(new InfoDanio(10f * ReglaZonaDanio.Multiplicador(true), TipoDanio.Disparo, golpe.point, Vector3.down, null));
            Assert.Less(p.Actual, antes, "el disparo a la cabeza hace daño");
        }

        [UnityTest]
        public IEnumerator SiTeVieron_BajarLaBusquedaNoLosQuita_PeroSinBusquedaSeVan()
        {
            calor.Poner(75f);
            yield return Esperar(1.5f);
            int antes = respuesta.GuardiasVivos;
            Assert.Greater(antes, 0);
            Assert.IsTrue(respuesta.YaVioAlJugador(respuesta.Guardias.First(g => g != null)), "aparecen a menos de la distancia de vista");
            calor.Poner(30f); // 2 estrellas
            yield return Esperar(config.SegundosParaRetirarse + 3f);
            Assert.AreEqual(antes, respuesta.GuardiasVivos, "los que ya te vieron siguen contigo");
            calor.Borrar(); // el Doc o un arresto: sin búsqueda
            yield return Esperar(config.SegundosParaRetirarse + 3f);
            Assert.AreEqual(0, respuesta.GuardiasVivos + respuesta.PesadosVivos, "sin búsqueda, Nova se va");
        }

        [UnityTest]
        public IEnumerator CincoEstrellas_SuenaElAvisoUnaVez_YBajanElPesadoYElJefe()
        {
            int avisos = 0;
            respuesta.LlegadaEspecial += () => avisos++;
            calor.Poner(75f);
            yield return Esperar(1.5f);
            Assert.AreEqual(0, avisos, "con 4 estrellas no hay aviso especial");
            calor.Poner(95f);
            yield return Esperar(1.5f);
            Assert.AreEqual(1, avisos, "al llegar a 5 estrellas suena el aviso");
            yield return Esperar(1.5f);
            Assert.AreEqual(1, avisos, "una sola vez por búsqueda");
            Assert.IsTrue(respuesta.HayJefe);
            Assert.AreEqual(config.Pesados(5), respuesta.PesadosVivos);
        }

        [UnityTest]
        public IEnumerator GuardiaPesado_TieneElModeloAgrandadoPorLaConfig()
        {
            calor.Poner(75f);
            yield return Esperar(0.5f);
            PandilleroAPie pesado = respuesta.Pesados.First(g => g != null);
            Transform modelo = pesado.transform.Find("Modelo");
            Transform modeloPrefab = config.GuardiaPesado.transform.Find("Modelo");
            Assert.IsNotNull(modelo);
            Assert.AreEqual(modeloPrefab.localScale.y * config.EscalaPesado, modelo.localScale.y, 0.001f, "el pesado se agranda por EscalaPesado");
            PandilleroAPie guardia = respuesta.Guardias.First(g => g != null);
            Assert.AreEqual(config.Guardia.transform.Find("Modelo").localScale.y, guardia.transform.Find("Modelo").localScale.y, 0.001f, "el guardia normal no cambia");
        }
    }
}
