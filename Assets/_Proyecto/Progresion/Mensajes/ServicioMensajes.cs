using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Los mensajes del teléfono (T-449): cada medio segundo mira cómo va el juego y, cuando le toca a un mensaje de <see cref="ReglaMensajes"/>,
    /// lo entrega (una sola vez). Guarda los últimos mensajes con su estado de leído en la clave "mensajes". Lo leen la pestaña MENSAJES del
    /// teléfono y el aviso del HUD.
    /// </summary>
    public class ServicioMensajes : MonoBehaviour, IGuardable
    {
        public const string Clave = "mensajes";
        public const int MaximoGuardados = 12;
        private const float SegundosBusqueda = 0.5f;

        [Serializable]
        public class MensajeGuardado
        {
            public string id = string.Empty;
            public bool leido;
            public int dia;
        }

        [Serializable]
        private class DatosGuardados
        {
            public MensajeGuardado[] mensajes = Array.Empty<MensajeGuardado>();
        }

        private readonly List<MensajeGuardado> recibidos = new List<MensajeGuardado>();
        private readonly HashSet<string> entregados = new HashSet<string>();
        private float proximaBusqueda;
        // La historia de la demo la lleva DirectorPandillas (otro ensamblado) y avisa aquí; sin director no hay "juego libre" y esos mensajes no llegan.
        private bool historiaTerminada;

        public string ClaveGuardado => Clave;

        /// <summary>Lo llama quien lleva la historia: true cuando ya terminó y el jugador está en el juego libre.</summary>
        public void FijarHistoriaTerminada(bool terminada) => historiaTerminada = terminada;

        /// <summary>Los mensajes recibidos, del más viejo al más nuevo.</summary>
        public IReadOnlyList<MensajeGuardado> Recibidos => recibidos;

        public int SinLeer
        {
            get
            {
                int cuenta = 0;
                foreach (MensajeGuardado m in recibidos)
                {
                    cuenta += m.leido ? 0 : 1;
                }
                return cuenta;
            }
        }

        /// <summary>Llegó un mensaje nuevo (se avisa con su id).</summary>
        public event Action<string> Recibido;

        /// <summary>Cambió algo (llegó uno o se marcaron como leídos).</summary>
        public event Action Cambio;

        private void OnEnable()
        {
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioMensajes>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioMensajes>.Quitar(this);
        }

        private void Update()
        {
            if (Time.unscaledTime < proximaBusqueda)
            {
                return;
            }
            proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
            Revisar(LeerContexto(historiaTerminada));
        }

        /// <summary>Entrega lo que le toca según el contexto. Devuelve cuántos mensajes llegaron.</summary>
        public int Revisar(ReglaMensajes.Contexto contexto)
        {
            List<string> pendientes = ReglaMensajes.Pendientes(contexto, entregados);
            foreach (string id in pendientes)
            {
                Entregar(id, contexto.Dia);
            }
            return pendientes.Count;
        }

        /// <summary>Entrega un mensaje del catálogo si no se había entregado.</summary>
        public bool Entregar(string id, int dia)
        {
            if (!ReglaMensajes.Buscar(id, out _) || !entregados.Add(id))
            {
                return false;
            }
            recibidos.Add(new MensajeGuardado { id = id, leido = false, dia = dia });
            while (recibidos.Count > MaximoGuardados)
            {
                recibidos.RemoveAt(0);
            }
            Sonidos.Reproducir("UI_TELEFONO");
            Recibido?.Invoke(id);
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Marca todos como leídos (al abrir la pestaña MENSAJES).</summary>
        public void MarcarLeidos()
        {
            bool cambio = false;
            foreach (MensajeGuardado m in recibidos)
            {
                cambio |= !m.leido;
                m.leido = true;
            }
            if (cambio)
            {
                Cambio?.Invoke();
            }
        }

        private static ReglaMensajes.Contexto LeerContexto(bool historiaTerminada)
        {
            ReglaMensajes.Contexto c = default;
            c.Libre = historiaTerminada;
            ICartera cartera = Registro<ICartera>.Actual;
            c.Dinero = cartera != null ? cartera.Dinero : 0;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (territorio != null)
            {
                foreach (string zona in territorio.Zonas)
                {
                    c.ZonasTuyas += territorio.Dueno(zona) == IdPandilla.Jugador ? 1 : 0;
                }
            }
            ICalor calor = Registro<ICalor>.Actual;
            c.NivelCalor = calor != null ? calor.Nivel : 0;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            c.Dia = reloj != null ? reloj.Dia : 0;
            return c;
        }

        public string Guardar()
        {
            return JsonUtility.ToJson(new DatosGuardados { mensajes = recibidos.ToArray() });
        }

        public void Cargar(string texto)
        {
            DatosGuardados g;
            try
            {
                g = JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (Exception)
            {
                g = null;
            }
            recibidos.Clear();
            entregados.Clear();
            if (g != null && g.mensajes != null)
            {
                foreach (MensajeGuardado m in g.mensajes)
                {
                    if (m != null && ReglaMensajes.Buscar(m.id, out _) && entregados.Add(m.id))
                    {
                        recibidos.Add(m);
                    }
                }
            }
            Cambio?.Invoke();
        }
    }
}
