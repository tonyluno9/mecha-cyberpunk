using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>
    /// Quién vio el delito (T-661). Envuelve al <see cref="ICalor"/> real con <see cref="CalorConTestigos"/> y decide si un delito suma calor:
    /// 1) una patrulla con línea de vista o una cámara de seguridad que te ve: suma de inmediato;
    /// 2) un civil que te ve: se asusta, se queda llamando <see cref="ConfigPersecucion.SegundosLlamadaCivil"/> s con el teléfono y, si sigue vivo
    ///    y no huyó, suma el calor acumulado (T-663); si lo matan o huye antes, nadie viene;
    /// 3) nadie: no suma. Con la policía ya encima (nivel mayor que 0) o en misiones, el calor suma sin preguntar.
    /// </summary>
    public class TestigosDelito : MonoBehaviour
    {
        private sealed class Llamada
        {
            public PeatonMundo Civil;
            public ReglaLlamadaCivil Regla;
        }

        private readonly List<Llamada> llamadas = new List<Llamada>();
        private ConfigPersecucion config;
        private CalorConTestigos decorador;
        private float pendiente;
        private MotivoCalor motivoPendiente = MotivoCalor.Otro;

        /// <summary>Cuántos civiles están llamando ahora.</summary>
        public int LlamadasEnCurso => llamadas.Count;

        /// <summary>Calor que se sumará si algún civil termina de llamar (puntos).</summary>
        public float Pendiente => pendiente;

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
            CancelarTodas();
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
                CancelarTodas();
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
            IniciarLlamadas(jugador.Value);
            if (llamadas.Count > 0)
            {
                pendiente = Mathf.Min(pendiente + cantidad, decorador.Maximo);
                motivoPendiente = motivo;
                return ResultadoTestigos.Pendiente;
            }
            DelitosSinTestigos++;
            return ResultadoTestigos.Nada;
        }

        /// <summary>True si una patrulla con línea de vista o una cámara de seguridad ve a quien está en <paramref name="punto"/>.</summary>
        public bool VeUnPolicia(Vector3 punto)
        {
            foreach (PatrullaMundo patrulla in PatrullaMundo.Activas)
            {
                if (patrulla != null && patrulla.VeElPunto(punto))
                {
                    return true;
                }
            }
            return CamaraQueVe(punto) != null;
        }

        /// <summary>La primera cámara de seguridad que ve a quien está en <paramref name="punto"/>, o null.</summary>
        public CamaraSeguridad CamaraQueVe(Vector3 punto)
        {
            foreach (CamaraSeguridad camara in CamaraSeguridad.Activas)
            {
                if (camara != null && camara.Ve(punto))
                {
                    return camara;
                }
            }
            return null;
        }

        // T-705: el conductor del carro que le están robando es testigo desde ya (el carro puede avisar del robo antes o después que este calor).
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

        private void IniciarLlamadas(Vector3 punto)
        {
            foreach (PeatonMundo civil in PeatonMundo.Activos)
            {
                if (civil == null || civil.Muerto || civil.Llamando || YaLlama(civil) || !CivilVe(civil, punto))
                {
                    continue;
                }
                Llamada llamada = new Llamada { Civil = civil, Regla = new ReglaLlamadaCivil(config.SegundosLlamadaCivil) };
                llamada.Regla.Iniciar();
                civil.IniciarLlamada();
                llamadas.Add(llamada);
            }
        }

        private bool YaLlama(PeatonMundo civil)
        {
            foreach (Llamada l in llamadas)
            {
                if (l.Civil == civil)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CivilVe(PeatonMundo civil, Vector3 punto)
        {
            Vector3 ojos = civil.transform.position + Vector3.up * 1.6f;
            Vector3 d = punto - civil.transform.position;
            d.y = 0f;
            return d.magnitude <= config.RadioVistaCivil && LineaDeVista.Libre(ojos, punto);
        }

        private void Update()
        {
            if (decorador == null)
            {
                return;
            }
            decorador.Mantener();
            ActualizarCamaras();
            AvanzarLlamadas(Time.deltaTime);
        }

        // Las cámaras que ven al jugador parpadean en rojo.
        private void ActualizarCamaras()
        {
            Vector3? jugador = PosicionDelJugador();
            foreach (CamaraSeguridad camara in CamaraSeguridad.Activas)
            {
                camara.PonerViendo(jugador.HasValue && camara.Ve(jugador.Value));
            }
        }

        private void AvanzarLlamadas(float dt)
        {
            if (llamadas.Count == 0)
            {
                return;
            }
            Vector3? jugador = PosicionDelJugador();
            bool alguienLlamo = false;
            for (int i = llamadas.Count - 1; i >= 0; i--)
            {
                Llamada l = llamadas[i];
                bool sigue = l.Civil != null && l.Civil.isActiveAndEnabled && !l.Civil.Muerto;
                if (sigue && jugador.HasValue)
                {
                    Vector3 d = jugador.Value - l.Civil.transform.position;
                    d.y = 0f;
                    float radio = l.Civil.RadioAbandonoPropio > 0f ? l.Civil.RadioAbandonoPropio : config.RadioAbandonoCivil;
                    sigue = d.magnitude <= radio;
                }
                if (!sigue)
                {
                    l.Regla.Cancelar();
                    if (l.Civil != null)
                    {
                        l.Civil.TerminarLlamada();
                    }
                    llamadas.RemoveAt(i);
                    continue;
                }
                if (l.Regla.Avanzar(dt))
                {
                    l.Civil.TerminarLlamada();
                    llamadas.RemoveAt(i);
                    alguienLlamo = true;
                }
            }
            if (alguienLlamo && pendiente > 0f)
            {
                float cantidad = pendiente;
                pendiente = 0f;
                CancelarTodas(); // con uno que avise basta; los demás ya no llaman
                decorador.SumarDirecto(cantidad, motivoPendiente);
            }
            else if (llamadas.Count == 0)
            {
                pendiente = 0f; // nadie llegó a llamar: ese delito no cuenta
            }
        }

        private void CancelarTodas()
        {
            foreach (Llamada l in llamadas)
            {
                l.Regla.Cancelar();
                if (l.Civil != null)
                {
                    l.Civil.TerminarLlamada();
                }
            }
            llamadas.Clear();
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
