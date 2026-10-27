using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Los escondites del barrio (T-187): un <see cref="IInteractuable"/> por cada <see cref="IPuntoMision"/> de tipo Escondite.
    /// Con calor y un favor ESCONDITE, la E borra el calor, gasta el favor y, si la misión está en el paso Escapar, lo cumple.
    /// Con calor y sin favor solo avisa que hace falta un favor (sin E). Sin calor no se ofrece. Mientras hay calor llama
    /// <c>Activar(true)</c> a los puntos. Solo a pie. Va en ServiciosProgresion.prefab; mira los puntos nuevos cada <c>segundosRevision</c>.
    /// </summary>
    public class Escondites : MonoBehaviour
    {
        [Tooltip("Lo que dice el escondite cuando se puede usar (el HUD le pone 'E · ').")]
        [SerializeField] private string textoEsconderte = "ESCONDERTE (FAVOR)";
        [Tooltip("Lo que dice el escondite cuando hay calor pero no hay favor (sin E).")]
        [SerializeField] private string textoNecesitasFavor = "ESCONDITE · NECESITAS UN FAVOR";
        [Tooltip("Cada cuántos segundos se buscan los puntos nuevos (s).")]
        [Min(0.05f)] [SerializeField] private float segundosRevision = 0.5f;

        private sealed class Escondite : IInteractuable
        {
            private readonly Escondites dueno;
            public IPuntoMision Punto { get; }
            public bool Registrado { get; set; }

            public Escondite(Escondites dueno, IPuntoMision punto)
            {
                this.dueno = dueno;
                Punto = punto;
            }

            public Vector3 Posicion => Punto.Pose.position;
            public string Texto => dueno.TextoDe();
            public bool PuedeUsar => dueno.PuedeUsarAhora();
            public void Usar() => dueno.Esconder();
        }

        private readonly Dictionary<IPuntoMision, Escondite> puntos = new Dictionary<IPuntoMision, Escondite>();
        private readonly List<IPuntoMision> quitados = new List<IPuntoMision>();
        private float restante;
        private bool ofreciendo;

        /// <summary>Cuántos escondites se están ofreciendo ahora (hay calor).</summary>
        public int CantidadOfrecidos
        {
            get
            {
                int cuenta = 0;
                foreach (Escondite e in puntos.Values)
                {
                    if (e.Registrado) cuenta++;
                }
                return cuenta;
            }
        }

        private void OnEnable()
        {
            restante = 0f;
        }

        private void OnDisable()
        {
            foreach (Escondite e in puntos.Values)
            {
                Quitar(e);
            }
            puntos.Clear();
            ofreciendo = false;
        }

        private void Update()
        {
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                restante = segundosRevision;
                Buscar();
            }
            bool hayCalor = HayCalor();
            if (hayCalor == ofreciendo)
            {
                return;
            }
            ofreciendo = hayCalor;
            foreach (Escondite e in puntos.Values)
            {
                Ofrecer(e, hayCalor);
            }
        }

        private void Buscar()
        {
            quitados.Clear();
            foreach (IPuntoMision punto in puntos.Keys)
            {
                if (!Existe(punto))
                {
                    quitados.Add(punto);
                }
            }
            foreach (IPuntoMision punto in quitados)
            {
                Quitar(puntos[punto]);
                puntos.Remove(punto);
            }
            foreach (IPuntoMision punto in Registro<IPuntoMision>.Todos)
            {
                if (punto == null || punto.Tipo != TipoPuntoMision.Escondite || puntos.ContainsKey(punto))
                {
                    continue;
                }
                var escondite = new Escondite(this, punto);
                puntos[punto] = escondite;
                if (ofreciendo)
                {
                    Ofrecer(escondite, true);
                }
            }
        }

        private static bool Existe(IPuntoMision buscado)
        {
            foreach (IPuntoMision punto in Registro<IPuntoMision>.Todos)
            {
                if (punto == buscado)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Ofrecer(Escondite escondite, bool ofrecer)
        {
            if (ofrecer && !escondite.Registrado)
            {
                Registro<IInteractuable>.Agregar(escondite);
                escondite.Registrado = true;
                escondite.Punto.Activar(true);
            }
            else if (!ofrecer)
            {
                Quitar(escondite);
            }
        }

        private static void Quitar(Escondite escondite)
        {
            if (escondite.Registrado)
            {
                Registro<IInteractuable>.Quitar(escondite);
                escondite.Registrado = false;
                escondite.Punto.Activar(false);
            }
        }

        private static bool HayCalor()
        {
            ICalor calor = Registro<ICalor>.Actual;
            return calor != null && calor.Valor > 0f;
        }

        private static bool APie()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Modo == ModoJugador.APie;
        }

        private static bool HayFavor()
        {
            ServicioFavores favores = Registro<ServicioFavores>.Actual;
            return favores != null && favores.Cantidad(TipoFavor.Escondite) > 0;
        }

        private bool PuedeUsarAhora() => APie() && HayCalor() && HayFavor();

        private string TextoDe()
        {
            if (!HayCalor())
            {
                return string.Empty;
            }
            return HayFavor() ? textoEsconderte : textoNecesitasFavor;
        }

        private void Esconder()
        {
            if (!PuedeUsarAhora())
            {
                return;
            }
            Registro<ICalor>.Actual.Borrar();
            Registro<ServicioFavores>.Actual.Gastar(TipoFavor.Escondite);
            Registro<ServicioMisiones>.Actual?.CumplirPasoExterno(TipoPaso.Escapar);
        }
    }
}
