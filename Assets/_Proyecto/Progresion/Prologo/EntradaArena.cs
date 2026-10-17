using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// El mecha de la historia entra a la arena (T-128, D-tony-65). <see cref="Empezar"/>: con un fundido, el mecha (el que
    /// descansa en el taller y conserva su daño) va a la arena, el jugador sube y arranca la pelea del director, sin revancha.
    /// Al terminar la pelea, otro fundido deja al jugador a pie en la aparición de salida mientras el mecha vuelve solo al taller.
    /// </summary>
    public class EntradaArena : MonoBehaviour
    {
        [SerializeField] private ConfigEntradaArena config;
        [Tooltip("PantallasHistoria.prefab: se instancia una vez si la escena no trae ninguna pantalla de fundido.")]
        [SerializeField] private GameObject prefabPantallas;

        private DirectorPelea flujo;
        private JugadorAPie jugador;
        private IMontable montable;
        private LlamadoMecha llamado;
        private PantallaFundido fundido;
        private bool peleaTerminada;
        private ResultadoPelea resultado;

        /// <summary>True desde que <see cref="Empezar"/> acepta hasta que termina la salida.</summary>
        public bool EnCurso { get; private set; }

        /// <summary>Se avisa al final de la salida, con el resultado de la pelea.</summary>
        public event Action<ResultadoPelea> Terminada;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Progresion] Falta el config en EntradaArena de " + name, this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (EnCurso)
            {
                // Si se apaga a media entrada nadie la termina: se suelta para no dejar el evento colgado.
                StopAllCoroutines();
                EventosPelea.PeleaTerminada -= AlTerminarPelea;
                EnCurso = false;
            }
        }

        /// <summary>
        /// Empieza la entrada. Solo con el jugador a pie y ninguna entrada en curso; si falta la arena, el mecha o el jugador,
        /// avisa con un error y no empieza. Devuelve si empezó.
        /// </summary>
        public bool Empezar(DirectorPelea director)
        {
            if (!enabled || EnCurso)
            {
                return false;
            }
            if (director == null)
            {
                Debug.LogError("[Progresion] EntradaArena necesita un DirectorPelea.", this);
                return false;
            }
            jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null)
            {
                Debug.LogError("[Progresion] EntradaArena no encontró al jugador a pie.", this);
                return false;
            }
            if (jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            if (director.Arena == null)
            {
                Debug.LogError("[Progresion] EntradaArena: el director " + director.name + " no tiene arena.", this);
                return false;
            }
            if (director.Estado != EstadoFlujoPelea.Preparando)
            {
                return false;
            }
            montable = BuscarMecha();
            llamado = montable != null ? (montable as Component)?.GetComponent<LlamadoMecha>() : null;
            if (montable == null || llamado == null)
            {
                Debug.LogError("[Progresion] EntradaArena no encontró el mecha de la historia (montable de modo Mecha con LlamadoMecha).", this);
                return false;
            }
            if (!montable.PuedeSubir)
            {
                Debug.LogError("[Progresion] EntradaArena: el mecha de la historia no se puede subir ahora.", this);
                return false;
            }
            fundido = ObtenerFundido();
            if (fundido == null)
            {
                Debug.LogError("[Progresion] EntradaArena no pudo conseguir una pantalla de fundido.", this);
                return false;
            }

            flujo = director;
            peleaTerminada = false;
            EnCurso = true;
            EventosPelea.PeleaTerminada += AlTerminarPelea;
            StartCoroutine(Rutina());
            return true;
        }

        private static IMontable BuscarMecha()
        {
            foreach (IMontable candidato in Registro<IMontable>.Todos)
            {
                if (candidato != null && candidato.Modo == ModoJugador.Mecha)
                {
                    return candidato;
                }
            }
            return null;
        }

        private PantallaFundido ObtenerFundido()
        {
            if (PantallaFundido.Actual != null)
            {
                return PantallaFundido.Actual;
            }
            if (prefabPantallas == null)
            {
                return null;
            }
            GameObject nuevas = Instantiate(prefabPantallas);
            nuevas.name = prefabPantallas.name;
            return nuevas.GetComponentInChildren<PantallaFundido>(true);
        }

        private void AlTerminarPelea(ResultadoPelea terminada)
        {
            resultado = terminada;
            peleaTerminada = true;
        }

        private IEnumerator Rutina()
        {
            float segundosSalida = fundido.Config != null ? fundido.Config.SegundosSalida : 0f;
            float segundosEntrada = fundido.Config != null ? fundido.Config.SegundosEntrada : 0f;

            // Entrada: negro, mecha a la arena, jugador adentro, pelea y fundido de vuelta.
            yield return fundido.Fundir(1f, segundosSalida);
            llamado.LlevarA(flujo.Arena.InicioJugador);
            if (!jugador.Subir(montable))
            {
                Debug.LogError("[Progresion] EntradaArena: el jugador no pudo subir al mecha; se cancela la entrada.", this);
                llamado.VolverAlTallerYa();
                EventosPelea.PeleaTerminada -= AlTerminarPelea;
                yield return fundido.Fundir(0f, segundosEntrada);
                EnCurso = false;
                yield break;
            }
            flujo.EmpezarPelea();
            yield return fundido.Fundir(0f, segundosEntrada);

            // La pelea se juega sola; el director avisa su resultado.
            while (!peleaTerminada)
            {
                yield return null;
            }
            EventosPelea.PeleaTerminada -= AlTerminarPelea;
            yield return new WaitForSeconds(config.SegundosTrasResultado);

            // Salida: negro, jugador a pie en la salida, mecha al taller y el director listo para otra pelea.
            yield return fundido.Fundir(1f, segundosSalida);
            float limite = Time.time + config.SegundosMaximosParaBajar;
            while (!jugador.Bajar())
            {
                if (Time.time >= limite)
                {
                    Debug.LogError("[Progresion] EntradaArena: el jugador no pudo bajar del mecha; se queda dentro.", this);
                    yield return fundido.Fundir(0f, segundosEntrada);
                    EnCurso = false;
                    yield break;
                }
                yield return null;
            }
            jugador.Colocar(PoseSalida());
            llamado.VolverAlTallerYa();
            flujo.Reiniciar();
            yield return fundido.Fundir(0f, segundosEntrada);
            EnCurso = false;
            Terminada?.Invoke(resultado);
        }

        // La aparición de salida; si falta, junto a la escotilla del mecha.
        private Pose PoseSalida()
        {
            IReadOnlyList<IPuntoAparicion> puntos = Registro<IPuntoAparicion>.Todos;
            for (int i = 0; i < puntos.Count; i++)
            {
                if (puntos[i] != null && puntos[i].Id == config.IdAparicionSalida)
                {
                    return puntos[i].Pose;
                }
            }
            Debug.LogWarning("[Progresion] No hay aparición '" + config.IdAparicionSalida + "'; el jugador sale junto al mecha.", this);
            return new Pose(montable.PuntoSubida, jugador.Raiz.rotation);
        }
    }
}
