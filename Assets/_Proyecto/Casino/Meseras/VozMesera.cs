using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Casino.Meseras
{
    /// <summary>
    /// T-voces-personajes-23nov: la voz de las meseras robot del casino (3D, desde la mesera). Lleva la lista de meseras activas, el silencio común (una sola
    /// habla a la vez) y escucha la entrada y salida del casino (EventosLugares con IdLugar.Casino). Las reglas están en <see cref="ReglaVozMesera"/>.
    /// </summary>
    public static class VozMesera
    {
        private const float AlturaBoca = 1.5f;

        private static readonly List<RondaMesera> Meseras = new List<RondaMesera>();
        private static readonly List<Vector3> Posiciones = new List<Vector3>();
        private static float ultimaFrase = float.NegativeInfinity;
        private static float ultimaSuerte = float.NegativeInfinity;
        private static float ultimoSaludoPuerta = float.NegativeInfinity;
        private static float ultimaDespedidaPuerta = float.NegativeInfinity;

        /// <summary>El último ID que dijo una mesera (para pruebas y depuración).</summary>
        public static string UltimoId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Meseras.Clear();
            ultimaFrase = float.NegativeInfinity;
            ultimaSuerte = float.NegativeInfinity;
            ultimoSaludoPuerta = float.NegativeInfinity;
            ultimaDespedidaPuerta = float.NegativeInfinity;
            UltimoId = null;
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Suscribir()
        {
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
        }

        public static void Agregar(RondaMesera mesera)
        {
            if (mesera != null && !Meseras.Contains(mesera))
            {
                Meseras.Add(mesera);
            }
        }

        public static void Quitar(RondaMesera mesera) => Meseras.Remove(mesera);

        /// <summary>Distancia (m, en el plano) del jugador a <paramref name="punto"/>; infinito si no hay jugador o no va a pie.</summary>
        public static float DistanciaAlJugador(Vector3 punto)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null || jugador.Modo != ModoJugador.APie)
            {
                return float.PositiveInfinity;
            }
            Vector3 d = jugador.Raiz.position - punto;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>La mesera dice <paramref name="id"/> si ninguna otra acaba de hablar. True si sonó.</summary>
        public static bool Decir(RondaMesera mesera, string id)
        {
            if (mesera == null || !ReglaVozMesera.Toca(Time.time, ultimaFrase, ReglaVozMesera.SegundosEntreFrases))
            {
                return false;
            }
            ultimaFrase = Time.time;
            UltimoId = id;
            Sonidos.ReproducirEn(id, mesera.transform.position + Vector3.up * AlturaBoca);
            return true;
        }

        /// <summary>El jugador echó a andar una tragamonedas en <paramref name="maquina"/>: la mesera más cercana le desea suerte (cada tanto).</summary>
        public static bool DesearSuerte(Vector3 maquina)
        {
            if (!ReglaVozMesera.Toca(Time.time, ultimaSuerte, ReglaVozMesera.SegundosEntreSuertes))
            {
                return false;
            }
            RondaMesera mesera = MasCercana(maquina, ReglaVozMesera.DistanciaSuerte);
            if (mesera == null || !Decir(mesera, ReglaVozMesera.Suerte))
            {
                return false;
            }
            ultimaSuerte = Time.time;
            return true;
        }

        private static RondaMesera MasCercana(Vector3 punto, float maxima)
        {
            Posiciones.Clear();
            for (int i = Meseras.Count - 1; i >= 0; i--)
            {
                if (Meseras[i] == null)
                {
                    Meseras.RemoveAt(i);
                }
            }
            foreach (RondaMesera m in Meseras)
            {
                Posiciones.Add(m.transform.position);
            }
            int indice = ReglaVozMesera.MasCercana(Posiciones, punto, maxima);
            return indice >= 0 ? Meseras[indice] : null;
        }

        private static RondaMesera CercanaAlJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? MasCercana(jugador.Raiz.position, ReglaVozMesera.DistanciaPuerta) : null;
        }

        private static void AlEntrar(IdLugar lugar)
        {
            if (lugar != IdLugar.Casino || !ReglaVozMesera.Toca(Time.time, ultimoSaludoPuerta, ReglaVozMesera.SegundosEntrePuerta))
            {
                return;
            }
            if (Decir(CercanaAlJugador(), ReglaVozMesera.Saludo))
            {
                ultimoSaludoPuerta = Time.time;
            }
        }

        private static void AlSalir(IdLugar lugar)
        {
            if (lugar != IdLugar.Casino || !ReglaVozMesera.Toca(Time.time, ultimaDespedidaPuerta, ReglaVozMesera.SegundosEntrePuerta))
            {
                return;
            }
            if (Decir(CercanaAlJugador(), ReglaVozMesera.Despedida))
            {
                ultimaDespedidaPuerta = Time.time;
            }
        }
    }
}
