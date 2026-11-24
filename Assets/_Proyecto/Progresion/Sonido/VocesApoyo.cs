using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-voces-personajes-23nov: las voces grabadas de Canepa (el mecánico) y de Tino (el inventor), en 3D desde donde están parados.
    /// Canepa saluda al entrar a su taller y se despide al salir (EventosLugares con IdLugar.Taller) y dice REPARAR al terminar su trabajo (pintar el carro).
    /// Tino saluda cuando el jugador se le acerca y se despide al alejarse o al cerrar su banco, y habla al abrir el banco, al comprar y cuando no alcanza.
    /// Los dos se registran desde su <c>PersonajeApoyo</c>; sin registrar se quedan callados. Un enfriamiento evita repetir saludo y despedida.
    /// </summary>
    public static class VocesApoyo
    {
        public const string CanepaSaludo = "CANEPA_VOZ_SALUDO";
        public const string CanepaReparar = "CANEPA_VOZ_REPARAR";
        public const string CanepaDespedida = "CANEPA_VOZ_DESPEDIDA";

        public const string TinoSaludo = "TINO_VOZ_SALUDO";
        public const string TinoBanco = "TINO_VOZ_BANCO";
        public const string TinoCompra = "TINO_VOZ_COMPRA";
        public const string TinoSinDinero = "TINO_VOZ_SIN_DINERO";
        public const string TinoDespedida = "TINO_VOZ_DESPEDIDA";

        /// <summary>El id de <c>DatosPersonajeApoyo</c> de cada uno.</summary>
        public const string IdCanepa = "mecanico";
        public const string IdTino = "inventor";

        /// <summary>Segundos mínimos entre dos saludos (o dos despedidas) del mismo personaje.</summary>
        public const float SegundosEntrePuerta = 20f;

        /// <summary>A menos de esto (m) de Tino, saluda; pasado <see cref="DistanciaDespedidaTino"/>, se despide (con margen para no repetir en el borde).</summary>
        public const float DistanciaSaludoTino = 5f;
        public const float DistanciaDespedidaTino = 9f;

        private const float AlturaBoca = 1.6f;

        private static Transform canepa;
        private static Transform tino;
        private static float ultimoSaludoCanepa;
        private static float ultimaDespedidaCanepa;
        private static float ultimoSaludoTino;
        private static float ultimaDespedidaTino;

        /// <summary>El último ID que pidieron (para pruebas y depuración).</summary>
        public static string UltimoId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            canepa = null;
            tino = null;
            ultimoSaludoCanepa = float.NegativeInfinity;
            ultimaDespedidaCanepa = float.NegativeInfinity;
            ultimoSaludoTino = float.NegativeInfinity;
            ultimaDespedidaTino = float.NegativeInfinity;
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

        /// <summary>Si toca saludar o despedirse: pasó el enfriamiento desde la última vez.</summary>
        public static bool TocaPuerta(float ahora, float ultimo) => ahora - ultimo >= SegundosEntrePuerta;

        /// <summary>
        /// Cercanía con margen: +1 si el jugador acaba de llegar (estaba lejos y ya está a menos de <see cref="DistanciaSaludoTino"/>), -1 si se acaba de
        /// ir (estaba cerca y ya pasó <see cref="DistanciaDespedidaTino"/>), 0 si nada cambió.
        /// </summary>
        public static int CambioCercania(bool estabaCerca, float distancia)
        {
            if (!estabaCerca && distancia <= DistanciaSaludoTino) return 1;
            if (estabaCerca && distancia >= DistanciaDespedidaTino) return -1;
            return 0;
        }

        /// <summary>El personaje de apoyo <paramref name="idApoyo"/> habla desde <paramref name="donde"/> (solo Canepa y Tino tienen voz aquí).</summary>
        public static void Registrar(string idApoyo, Transform donde)
        {
            if (idApoyo == IdCanepa) canepa = donde;
            else if (idApoyo == IdTino) tino = donde;
        }

        public static void Quitar(string idApoyo, Transform donde)
        {
            if (idApoyo == IdCanepa && canepa == donde) canepa = null;
            else if (idApoyo == IdTino && tino == donde) tino = null;
        }

        private static void AlEntrar(IdLugar lugar)
        {
            if (lugar != IdLugar.Taller || canepa == null || !TocaPuerta(Time.time, ultimoSaludoCanepa)) return;
            ultimoSaludoCanepa = Time.time;
            Decir(canepa, CanepaSaludo);
        }

        private static void AlSalir(IdLugar lugar)
        {
            if (lugar != IdLugar.Taller || canepa == null || !TocaPuerta(Time.time, ultimaDespedidaCanepa)) return;
            ultimaDespedidaCanepa = Time.time;
            Decir(canepa, CanepaDespedida);
        }

        /// <summary>Canepa terminó su trabajo (pintó el carro y cobró).</summary>
        public static void CanepaTermino() => Decir(canepa, CanepaReparar);

        /// <summary>El jugador llegó con Tino (<paramref name="llego"/>) o se fue.</summary>
        public static void TinoCercania(bool llego)
        {
            if (llego)
            {
                if (!TocaPuerta(Time.time, ultimoSaludoTino)) return;
                ultimoSaludoTino = Time.time;
                Decir(tino, TinoSaludo);
            }
            else
            {
                TinoSeDespide();
            }
        }

        /// <summary>Se abrió el banco de Tino (COMPRAR y MEJORAR).</summary>
        public static void TinoAbrioBanco() => Decir(tino, TinoBanco);

        /// <summary>Se compró algo en el banco de Tino.</summary>
        public static void TinoCompro() => Decir(tino, TinoCompra);

        /// <summary>No alcanzó el dinero en el banco de Tino.</summary>
        public static void TinoSinDineroAlcanza() => Decir(tino, TinoSinDinero);

        /// <summary>Se cerró el banco de Tino o el jugador se alejó: se despide (una vez por enfriamiento).</summary>
        public static void TinoSeDespide()
        {
            if (!TocaPuerta(Time.time, ultimaDespedidaTino)) return;
            ultimaDespedidaTino = Time.time;
            Decir(tino, TinoDespedida);
        }

        private static void Decir(Transform quien, string id)
        {
            if (quien == null) return;
            UltimoId = id;
            Sonidos.ReproducirEn(id, quien.position + Vector3.up * AlturaBoca);
        }
    }
}
