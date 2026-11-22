using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1620 (T8): el director de respuesta. Por aquí pasan TODOS los spawns de policía y NOVA (patrullas, camionetas, drones, soldados, robots, perros, jefe):
    /// antes de generar una unidad se pregunta <see cref="PuedeGenerar"/>, y al generarla se avisa con <see cref="Registrar"/>. Lleva la cuenta de vivos, bajas
    /// y retiradas, y decide con <see cref="ReglaEscaladaRespuesta"/>. Quien ya tiene un ciclo de actualización (GestorPersecucion, RespuestaNova) llama a
    /// <see cref="Avanzar"/> con las estrellas de ahora. Cada alta y baja escribe una línea <c>[Respuesta] hora tipo nivel vivos</c> en consola.
    /// Respeta <see cref="MisionProtegida"/>: con ella activa no sale policía; NOVA solo si la misión lo permite.
    /// </summary>
    public static class DirectorRespuesta
    {
        private sealed class Entrada
        {
            public TipoRespuesta Tipo;
            public GameObject Objeto;
            public Func<EstadoUnidad> Estado;
            public bool Retirada;
        }

        private static readonly List<Entrada> entradas = new List<Entrada>();
        private static readonly int[] cuenta = new int[Enum.GetValues(typeof(TipoRespuesta)).Length];
        private static readonly bool[] declarados = new bool[Enum.GetValues(typeof(TipoRespuesta)).Length];
        private static readonly bool[] existen = new bool[Enum.GetValues(typeof(TipoRespuesta)).Length];
        private static ConfigRespuesta config;
        private static ParametrosRespuesta parametros;
        private static ReglaEscaladaRespuesta regla;
        private static float ultimoAhora = float.NaN;
        private static int ultimasEstrellas = -1;
        private static bool modoNovaPrevio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            modoNovaPrevio = false;
            entradas.Clear();
            Array.Clear(declarados, 0, declarados.Length);
            Array.Clear(existen, 0, existen.Length);
            config = null;
            parametros = null;
            regla = null;
            ultimoAhora = float.NaN;
            ultimasEstrellas = -1;
        }

        /// <summary>Los números de ahora (el asset de Resources, o los de omisión). Para pruebas: <see cref="Configurar"/>.</summary>
        public static ParametrosRespuesta Parametros
        {
            get
            {
                if (parametros == null)
                {
                    config = Resources.Load<ConfigRespuesta>(ConfigRespuesta.Recurso);
                    parametros = config != null ? config.Parametros : ParametrosRespuesta.PorOmision();
                }
                return parametros;
            }
        }

        /// <summary>La regla viva (para consultas y pruebas).</summary>
        public static ReglaEscaladaRespuesta Regla
        {
            get
            {
                if (regla == null)
                {
                    regla = new ReglaEscaladaRespuesta(Parametros);
                    for (int i = 0; i < declarados.Length; i++)
                    {
                        if (declarados[i])
                        {
                            regla.Declarar((TipoRespuesta)i, existen[i]);
                        }
                    }
                }
                return regla;
            }
        }

        /// <summary>Para pruebas: usa estos números y empieza de cero.</summary>
        public static void Configurar(ParametrosRespuesta numeros)
        {
            Reiniciar();
            parametros = numeros;
        }

        /// <summary>Dice si hay con qué generar este tipo (prefab, calles). Un tipo sin existir hace que su nivel se salte.</summary>
        public static void DeclararDisponible(TipoRespuesta tipo, bool existe)
        {
            declarados[(int)tipo] = true;
            existen[(int)tipo] = existe;
            Regla.Declarar(tipo, existe);
        }

        public static int Vivos(TipoRespuesta tipo) => Regla.Vivos(tipo);

        public static int VivosTotal => Regla.VivosTotal;

        public static int NivelDesbloqueado => Regla.NivelDesbloqueado;

        /// <summary>
        /// T-1631 (T17): True mientras "NOVA INTERVIENE" (estrellas al máximo): no se genera policía, la que hay debe retirarse (quien la maneja lee esto) y llega solo Nova.
        /// </summary>
        public static bool ModoNova => Regla.ModoNova;

        /// <summary>T-1631: unidades de NOVA vivas que cuentan para el tope de modo Nova.</summary>
        public static int NovaVivos => Regla.NovaVivos;

        public static int NivelDe(TipoRespuesta tipo) => ReglaEscaladaRespuesta.NivelDe(tipo);

        /// <summary>Avanza el tiempo del director: cuenta vivos, bajas y retiradas y desbloquea niveles. Varias llamadas en el mismo instante valen una.</summary>
        public static void Avanzar(float ahora, int estrellas)
        {
            bool mision = MisionProtegida.Activa;
            bool nova = mision && MisionProtegida.NovaPermitida;
            if (nova)
            {
                estrellas = Mathf.Max(estrellas, Parametros.EstrellasNovaEnMision);
            }
            else if (mision)
            {
                estrellas = 0;
            }
            if (ahora == ultimoAhora && estrellas == ultimasEstrellas)
            {
                return;
            }
            ultimoAhora = ahora;
            ultimasEstrellas = estrellas;
            Array.Clear(cuenta, 0, cuenta.Length);
            for (int i = entradas.Count - 1; i >= 0; i--)
            {
                Entrada e = entradas[i];
                EstadoUnidad estado = e.Retirada ? EstadoUnidad.Retirada : e.Estado();
                if (estado == EstadoUnidad.Viva)
                {
                    cuenta[(int)e.Tipo]++;
                    continue;
                }
                entradas.RemoveAt(i);
                if (estado == EstadoUnidad.Muerta)
                {
                    Regla.RegistrarPerdida(e.Tipo, ahora);
                    Escribir("baja", e.Tipo, ahora);
                }
                else
                {
                    Escribir("retiro", e.Tipo, ahora);
                }
            }
            Regla.Avanzar(ahora, estrellas, mision, nova, cuenta);
            AvisarCambioDeModoNova(ahora);
        }

        // T-1631: al entrar al modo Nova se escribe en el registro y se publica el aviso (el HUD pone el texto); al salir se anota que vuelve la policía.
        private static void AvisarCambioDeModoNova(float ahora)
        {
            bool modo = Regla.ModoNova;
            if (modo == modoNovaPrevio)
            {
                return;
            }
            modoNovaPrevio = modo;
            if (Parametros.Registrar)
            {
                Debug.Log("[Respuesta] " + ahora.ToString("F1") + (modo
                    ? " NOVA INTERVIENE: deja de llegar policia, la que hay se retira (tope Nova " + Parametros.TopeNovaVivos + ")"
                    : " fin de NOVA INTERVIENE: puede volver la policia"));
            }
            if (modo)
            {
                EventosRespuesta.PublicarNovaInterviene();
            }
        }

        /// <summary>True si el director deja generar otra unidad de este tipo ahora (nivel desbloqueado, cooldown, topes, misión).</summary>
        public static bool PuedeGenerar(TipoRespuesta tipo) => Regla.PuedeGenerar(tipo, Time.time);

        /// <summary>Cuántas unidades de este tipo caben todavía (cupo y total), para quien manda varias juntas (por ejemplo una nave).</summary>
        public static int Espacio(TipoRespuesta tipo)
        {
            int total = Parametros.TopeTotal - Regla.VivosTotal;
            int cupo = Regla.CupoActual(tipo) - Regla.Vivos(tipo);
            return Mathf.Max(0, Mathf.Min(total, cupo));
        }

        /// <summary>True si este tipo aún puede estar en pie con las estrellas y la misión de ahora; si no, hay que retirarlo (primero los niveles altos).</summary>
        public static bool PermiteMantener(TipoRespuesta tipo) => Regla.PermiteMantener(tipo);

        /// <summary>Avisa que salió una unidad. <paramref name="estado"/> dice si sigue viva; sin él se usa IVida.EstaKO y el objeto destruido cuenta como retirado.</summary>
        public static void Registrar(TipoRespuesta tipo, GameObject objeto, Func<EstadoUnidad> estado = null)
        {
            if (objeto == null)
            {
                return;
            }
            for (int i = 0; i < entradas.Count; i++)
            {
                if (entradas[i].Objeto == objeto)
                {
                    return;
                }
            }
            entradas.Add(new Entrada { Tipo = tipo, Objeto = objeto, Estado = estado ?? (() => EstadoPorOmision(objeto)) });
            Regla.RegistrarGenerado(tipo, Time.time);
            Escribir("sale", tipo, Time.time);
        }

        /// <summary>Avisa que esta unidad se va a retirar a propósito (no cuenta como baja).</summary>
        public static void MarcarRetirado(GameObject objeto)
        {
            foreach (Entrada e in entradas)
            {
                if (e.Objeto == objeto)
                {
                    e.Retirada = true;
                }
            }
        }

        private static EstadoUnidad EstadoPorOmision(GameObject objeto)
        {
            if (objeto == null)
            {
                return EstadoUnidad.Retirada;
            }
            IVida vida = objeto.GetComponentInChildren<IVida>();
            return vida != null && vida.EstaKO ? EstadoUnidad.Muerta : EstadoUnidad.Viva;
        }

        private static void Escribir(string que, TipoRespuesta tipo, float ahora)
        {
            if (!Parametros.Registrar)
            {
                return;
            }
            Debug.Log("[Respuesta] " + ahora.ToString("F1") + " " + tipo + " nivel " + NivelDe(tipo) + " vivos " + Regla.Vivos(tipo) + "/" + Regla.VivosTotal + (Regla.ModoNova ? " nova " + Regla.NovaVivos + "/" + Parametros.TopeNovaVivos : string.Empty) + " (" + que + ")");
        }

        // ----- Depuración para comprobar en Play (se llaman por eval) -----

        /// <summary>Sube las estrellas de búsqueda hasta las dadas (sirve para comprobar la escalada).</summary>
        public static void DepurarSubirEstrellas(int estrellas)
        {
            EstrellasBusqueda.SubirA(Registro<ICalor>.Actual, estrellas);
        }

        /// <summary>Mata a una unidad viva de este tipo con daño enorme. Devuelve true si había una.</summary>
        public static bool DepurarMatar(TipoRespuesta tipo)
        {
            foreach (Entrada e in entradas)
            {
                if (e.Tipo != tipo || e.Retirada || e.Objeto == null || e.Estado() != EstadoUnidad.Viva)
                {
                    continue;
                }
                IRecibeDanio danio = e.Objeto.GetComponentInChildren<IRecibeDanio>();
                if (danio == null)
                {
                    continue;
                }
                const float DanioDepuracion = 100000f;
                danio.RecibirDanio(new InfoDanio(DanioDepuracion, TipoDanio.Explosion, e.Objeto.transform.position, Vector3.down, null));
                return true;
            }
            return false;
        }

        /// <summary>Una línea con el estado: nivel desbloqueado y vivos por tipo.</summary>
        public static string DepurarEstado()
        {
            string texto = "[Respuesta] estado estrellas " + Regla.Estrellas + " nivel " + Regla.NivelDesbloqueado + " total " + Regla.VivosTotal + " modoNova " + Regla.ModoNova + " (nova " + Regla.NovaVivos + ")";
            foreach (TipoRespuesta t in Enum.GetValues(typeof(TipoRespuesta)))
            {
                texto += " | " + t + " " + Regla.Vivos(t) + " (bajas " + Regla.Perdidas(t) + ")";
            }
            Debug.Log(texto);
            return texto;
        }
    }
}
