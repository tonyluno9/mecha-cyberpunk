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
    /// T-1304, reescrita en T-1653 para la llegada gradual de T8/T17: con 4 estrellas Nova llega de a una unidad con espera (nunca en oleada) cerca del jugador y va
    /// por él; con 5 interviene (un solo aviso); sin búsqueda se retira.
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
            // T-1653: el director de respuesta con tiempos cortos para que la llegada gradual se vea en pocos segundos.
            ParametrosRespuesta numeros = ParametrosRespuesta.PorOmision();
            numeros.RetrasoNivelNuevo = 1f;
            numeros.SegundosEntreGenerados = 1.5f;
            numeros.SegundosParaSubir = 3f;
            numeros.Registrar = false;
            numeros.Tipos = new[]
            {
                new AjusteTipoRespuesta(TipoRespuesta.NovaHumanoide, 4, 4, 1f, 1f),
                new AjusteTipoRespuesta(TipoRespuesta.RobotMediano, 1, 1, 1f, 1f),
                new AjusteTipoRespuesta(TipoRespuesta.Perro, 1, 1, 1f, 1f),
                new AjusteTipoRespuesta(TipoRespuesta.RobotGrande, 1, 1, 1f, 1f),
                new AjusteTipoRespuesta(TipoRespuesta.Jefe, 1, 1, 1f, 1f),
            };
            DirectorRespuesta.Configurar(numeros);
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
            DirectorRespuesta.Configurar(null); // vuelve a los números del asset
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

        private int NovaVivos() => respuesta.GuardiasVivos + respuesta.PesadosVivos + respuesta.PerrosVivos;

        /// <summary>
        /// T-1653 (T8/T17): con 4 estrellas Nova llega de a una unidad, con espera entre una y otra (el director va con valores cortos), y no en oleada:
        /// al principio no hay nadie, la primera aparece sola y nunca suben dos en el mismo cuadro; cada una aparece a 22-34 m y va por el jugador.
        /// </summary>
        [UnityTest]
        public IEnumerator CincoEstrellas_LlegaDeAUno_ConEspera_NoEnOleada_YVaPorElJugador()
        {
            calor.Poner(95f); // 5 estrellas: con T17 Nova solo entra con el máximo de estrellas
            float inicio = Time.time;
            int anterior = 0;
            float primera = -1f;
            float segunda = -1f;
            int saltoMaximo = 0;
            while (Time.time - inicio < 7f)
            {
                yield return null;
                int ahora = NovaVivos();
                saltoMaximo = Mathf.Max(saltoMaximo, ahora - anterior);
                if (ahora >= 1 && primera < 0f)
                {
                    primera = Time.time - inicio;
                }
                if (ahora >= 2 && segunda < 0f)
                {
                    segunda = Time.time - inicio;
                }
                anterior = ahora;
            }
            Debug.Log("[T-1653] primera unidad a los " + primera + " s, segunda a los " + segunda + " s, salto máximo por cuadro " + saltoMaximo + ", vivos " + NovaVivos());
            Assert.AreEqual(5, respuesta.Estrellas);
            Assert.GreaterOrEqual(primera, 0.9f, "no llega nadie al instante: el nivel nuevo tiene su retraso");
            Assert.LessOrEqual(saltoMaximo, 1, "nunca dos unidades en el mismo cuadro (nada de oleada)");
            Assert.GreaterOrEqual(segunda, 0f, "con espera, pero sí llega la segunda");
            Assert.GreaterOrEqual(segunda - primera, 1.0f, "entre una y otra hay espera (SegundosEntreGenerados)");
            Assert.LessOrEqual(NovaVivos(), 4, "dentro del tope");
            Assert.IsFalse(respuesta.HayJefe, "el jefe final solo con 5");
            foreach (PandilleroAPie p in respuesta.Guardias.Concat(respuesta.Pesados))
            {
                Assert.IsTrue(p.Alerta, p.name + " está en alerta contra el jugador");
            }
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(respuesta.BuscarPunto(jugador.transform.position, out Vector3 punto), "Hay dónde aparecer");
                Assert.That(Vector3.Distance(Plano(punto), Plano(jugador.transform.position)), Is.InRange(21.9f, 34.1f), "Aparecen a 22-34 m");
            }
            float antes = respuesta.Guardias.Average(p => Vector3.Distance(Plano(p.transform.position), Plano(jugador.transform.position)));
            Time.timeScale = 3f;
            yield return Esperar(8f);
            Time.timeScale = 1f;
            float despues = respuesta.Guardias.Where(p => p != null).Average(p => Vector3.Distance(Plano(p.transform.position), Plano(jugador.transform.position)));
            Assert.Less(despues, antes + 2f, "Nova no se aleja del jugador");
        }

        [UnityTest]
        public IEnumerator CincoEstrellas_NovaInterviene_SeAvisaUnaVez_Y_SinBusqueda_NovaSeRetira()
        {
            int avisos = 0;
            Action alAviso = () => avisos++;
            EventosRespuesta.NovaInterviene += alAviso;
            try
            {
                calor.Poner(95f); // 5 estrellas
                yield return Esperar(2f);
                Assert.AreEqual(5, respuesta.Estrellas);
                Assert.IsTrue(DirectorRespuesta.ModoNova, "con 5 estrellas Nova interviene");
                Assert.AreEqual(1, avisos, "el aviso de NOVA INTERVIENE sale una sola vez");
                Assert.LessOrEqual(NovaVivos(), 1, "tras 2 s solo ha llegado una unidad como mucho");
                calor.Poner(0f); // T-1323: sin búsqueda (Doc, arresto) Nova se retira
                yield return Esperar(8f);
                Assert.AreEqual(0, NovaVivos(), "sin búsqueda Nova se retira");
                Assert.IsFalse(respuesta.HayJefe);
            }
            finally
            {
                EventosRespuesta.NovaInterviene -= alAviso;
            }
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
