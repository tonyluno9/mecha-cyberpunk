using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: hace sonar los bucles de clima 2D (lluvia fuerte, tormenta, viento, lluvia dentro del carro) según <see cref="ClimaSonoro"/> y dónde está el jugador
    /// (<see cref="ReglaClimaSonido"/>). Con el clima en cero, que es como está hoy el juego, no suena nada. La lluvia sobre lámina la maneja <see cref="SonidosMundo"/>.
    /// </summary>
    public class SonidosClima : MonoBehaviour
    {
        private readonly Dictionary<string, int> bucles = new Dictionary<string, int>();
        private readonly List<IdLugar> lugares = new List<IdLugar>();
        private float proximaRevision;

        /// <summary>Los IDs de clima que suenan ahora (para las pruebas).</summary>
        public IEnumerable<string> Sonando => bucles.Keys;

        private void OnEnable()
        {
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
        }

        private void OnDisable()
        {
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
            Aplicar(new List<string>());
        }

        private void AlEntrar(IdLugar lugar)
        {
            lugares.Remove(lugar);
            if (lugar != IdLugar.Calle && lugar != IdLugar.Ninguno) lugares.Add(lugar);
        }

        private void AlSalir(IdLugar lugar) => lugares.Remove(lugar);

        private void Update()
        {
            if (Time.unscaledTime < proximaRevision) return;
            proximaRevision = Time.unscaledTime + TablaEmisoresMundo.SegundosRevision;
            IJugador jugador = Registro<IJugador>.Actual;
            bool enCarro = jugador != null && jugador.Modo == ModoJugador.Carro;
            Aplicar(ReglaClimaSonido.Bucles(ClimaSonoro.Lluvia, ClimaSonoro.Tormenta, ClimaSonoro.Viento, enCarro, lugares.Count > 0));
            // T-1535: dentro de un carro la lluvia de afuera baja (la de dentro del carro la pone Vehiculos).
            if (Registro<ISonidos>.Actual is GestorSonidos gestor)
            {
                foreach (int bucle in bucles.Values) gestor.FijarFactorBucle(bucle, ReglaClimaSonido.Factor(enCarro));
            }
        }

        /// <summary>Deja sonando exactamente estos bucles: arranca los que faltan y apaga los que sobran.</summary>
        public void Aplicar(List<string> deseados)
        {
            List<string> sobran = new List<string>();
            foreach (KeyValuePair<string, int> par in bucles)
            {
                if (!deseados.Contains(par.Key)) sobran.Add(par.Key);
            }
            foreach (string id in sobran)
            {
                Sonidos.DetenerBucle(bucles[id]);
                bucles.Remove(id);
            }
            foreach (string id in deseados)
            {
                if (bucles.ContainsKey(id)) continue;
                int bucle = Sonidos.ReproducirBucle(id, null);
                if (bucle != Sonidos.SinBucle) bucles[id] = bucle;
            }
        }
    }
}
