using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>
    /// Quién vio el delito (T-661, T-933). Envuelve al <see cref="ICalor"/> real con <see cref="CalorConTestigos"/>: un delito solo suma calor si lo ve la
    /// policía (una patrulla o un policía a pie con línea de vista, ver <see cref="ITestigoPolicia"/>). Con la policía ya encima (nivel mayor que 0) o en
    /// misiones, el calor suma sin preguntar. Las cámaras y los civiles que llamaban ya no cuentan.
    /// </summary>
    public class TestigosDelito : MonoBehaviour
    {
        private ConfigPersecucion config;
        private CalorConTestigos decorador;

        /// <summary>El envoltorio que está en el registro de calor (null si no hay calor).</summary>
        public CalorConTestigos Calor => decorador;

        /// <summary>Cuántas veces se negó un delito por falta de testigos (para pruebas y registros).</summary>
        public int DelitosSinTestigos { get; private set; }

        public void Configurar(ConfigPersecucion numeros) => config = numeros;

        private void OnEnable()
        {
            Registro<ICalor>.Agregado += AlAgregarCalor;
            Registro<ICalor>.Quitado += AlQuitarCalor;
            ICalor actual = Registro<ICalor>.Actual;
            if (actual != null)
            {
                Envolver(actual);
            }
        }

        private void OnDisable()
        {
            Registro<ICalor>.Agregado -= AlAgregarCalor;
            Registro<ICalor>.Quitado -= AlQuitarCalor;
            if (decorador != null)
            {
                Registro<ICalor>.Quitar(decorador);
                decorador.Soltar();
                decorador = null;
            }
        }

        private void AlAgregarCalor(ICalor nuevo) => Envolver(nuevo);

        private void AlQuitarCalor(ICalor quitado)
        {
            if (decorador != null && ReferenceEquals(decorador.Interno, quitado))
            {
                CalorConTestigos viejo = decorador;
                decorador = null;
                Registro<ICalor>.Quitar(viejo);
                viejo.Soltar();
            }
        }

        private void Envolver(ICalor real)
        {
            if (real is CalorConTestigos || (decorador != null && ReferenceEquals(decorador.Interno, real)))
            {
                return;
            }
            if (decorador != null)
            {
                CalorConTestigos viejo = decorador;
                decorador = null;
                Registro<ICalor>.Quitar(viejo);
                viejo.Soltar();
            }
            decorador = new CalorConTestigos(real, this);
            Registro<ICalor>.Agregar(decorador);
        }

        /// <summary>Decide qué pasa con un delito de <paramref name="cantidad"/> puntos que acaba de ocurrir.</summary>
        public ResultadoTestigos Evaluar(float cantidad, MotivoCalor motivo)
        {
            ResultadoTestigos resultado = EvaluarInterno(motivo);
            DelitoEvaluado?.Invoke(cantidad, motivo, resultado); // T-729: para saber de dónde sube la búsqueda (pruebas y diagnóstico)
            return resultado;
        }

        /// <summary>T-729: avisa cada delito evaluado (calor que se quería sumar, motivo y qué pasó). El que lo llama está en la pila de quien escucha.</summary>
        public static event System.Action<float, MotivoCalor, ResultadoTestigos> DelitoEvaluado;

        private ResultadoTestigos EvaluarInterno(MotivoCalor motivo)
        {
            Vector3? jugador = PosicionDelJugador();
            if (decorador == null || config == null || !jugador.HasValue || motivo == MotivoCalor.Mision || decorador.Nivel > 0)
            {
                return ResultadoTestigos.Pasa;
            }
            if (VeUnPolicia(jugador.Value))
            {
                return ResultadoTestigos.Pasa;
            }
            SacarConductoresDeCarrosRobados();
            DelitosSinTestigos++;
            return ResultadoTestigos.Nada;
        }

        /// <summary>True si una patrulla o un policía a pie ve a quien está en <paramref name="punto"/>.</summary>
        public bool VeUnPolicia(Vector3 punto)
        {
            foreach (ITestigoPolicia testigo in Registro<ITestigoPolicia>.Todos)
            {
                if (testigo != null && testigo.VeElPunto(punto))
                {
                    return true;
                }
            }
            return false;
        }

        // T-705: el conductor del carro que le están robando sale del carro aunque nadie más lo vea.
        private static void SacarConductoresDeCarrosRobados()
        {
            for (int i = 0; i < CarroTrafico.Activos.Count; i++)
            {
                if (CarroTrafico.Activos[i].EstaSiendoRobado)
                {
                    CarroTrafico.Activos[i].SacarConductorRobado();
                }
            }
        }

        private void Update()
        {
            if (decorador != null)
            {
                decorador.Mantener();
            }
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
