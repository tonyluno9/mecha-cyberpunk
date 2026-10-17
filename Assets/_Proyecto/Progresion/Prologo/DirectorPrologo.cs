using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// Encadena la noche del prólogo (T-132, parte de demo.md pasos 2 a 4): pelea de campeonato en la arena, salida a pie hacia el carro,
    /// emboscada de la pandilla, regreso a casa y a dormir. Cada paso es una etapa de <see cref="IHistoria"/> (siempre se avanza por
    /// <see cref="ServicioHistoria.Avanzar"/>) y fija su objetivo en el HUD. Un cuadro después de arrancar, para que la partida se cargue y todo
    /// se registre, lee la etapa; sin partida (<c>Ninguna</c>) usa <see cref="etapaSinPartida"/>. Con una etapa fuera de la noche no
    /// hace nada. Desde T-133 también lleva el presente: despierta al jugador en su cama (estado inicial, D-tony-58), lo guía a su primera
    /// pelea casera en el mostrador del clandestino y cierra la demo con "continuará" (D-tony-70). Las reglas están en <see cref="MaquinaPrologo"/>.
    /// </summary>
    public class DirectorPrologo : MonoBehaviour
    {
        [SerializeField] private DatosPrologo datos;
        [Tooltip("La entrada del mecha a la arena (hija de este prefab).")]
        [SerializeField] private EntradaArena entrada;
        [Tooltip("El flujo de la pelea del tutorial (hijo de este prefab, sin revancha).")]
        [SerializeField] private DirectorPelea flujoTutorial;
        [Tooltip("Etapa con la que arranca si no hay partida (etapa Ninguna). Libre = el director no hace nada.")]
        [SerializeField] private EtapaPrologo etapaSinPartida = EtapaPrologo.Libre;
        [Tooltip("Rutas de las escenas (el título al salir de la pantalla de continuará). Demo por defecto; Pruebas_Prologo usa DatosEscenas_Pruebas.")]
        [SerializeField] private DatosEscenas datosEscenas;
        [Tooltip("Subtítulos de una línea (hijo de este prefab): el aviso al despertar.")]
        [SerializeField] private PantallaSubtitulos subtitulos;
        [Tooltip("Pantalla de continuará (de PantallasHistoria, hija de este prefab).")]
        [SerializeField] private PantallaContinuara continuara;

        private MaquinaPrologo maquina;
        private ServicioHistoria historia;
        private DirectorEmboscada emboscada;
        private bool eventosEmboscada;
        private MostradorPeleas mostrador;
        private bool avisoVisible;
        private bool ganoCasera = true;

        /// <summary>True cuando el director tomó una etapa de la noche y la está llevando.</summary>
        public bool Activo => maquina != null && maquina.Activa;

        /// <summary>La etapa con la que se comporta el director (Ninguna si no está activo).</summary>
        public EtapaPrologo Etapa => maquina != null ? maquina.Etapa : EtapaPrologo.Ninguna;

        /// <summary>True una vez que leyó la etapa, haya o no actuado.</summary>
        public bool Iniciado { get; private set; }

        public EtapaPrologo EtapaSinPartida => etapaSinPartida;

        private void Awake()
        {
            if (datos == null || entrada == null || flujoTutorial == null)
            {
                Debug.LogError("[Progresion] Faltan referencias en DirectorPrologo de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            entrada.Terminada += AlTerminarTutorial;
            EventosLugares.JugadorEntro += AlEntrarLugar;
            if (continuara != null)
            {
                continuara.SeguirJugando += AlSeguirJugando;
                continuara.SalirAlTitulo += AlSalirAlTitulo;
            }
        }

        private void OnDisable()
        {
            entrada.Terminada -= AlTerminarTutorial;
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            if (continuara != null)
            {
                continuara.SeguirJugando -= AlSeguirJugando;
                continuara.SalirAlTitulo -= AlSalirAlTitulo;
            }
            if (mostrador != null)
            {
                mostrador.PeleaTerminada -= AlTerminarCasera;
                mostrador = null;
            }
            if (avisoVisible && subtitulos != null)
            {
                subtitulos.Ocultar();
                avisoVisible = false;
            }
            SoltarEmboscada();
        }

        private IEnumerator Start()
        {
            // Un cuadro: la partida se carga y los puntos se registran.
            yield return null;
            Iniciar();
        }

        private void Iniciar()
        {
            Iniciado = true;
            historia = Registro<IHistoria>.Actual as ServicioHistoria;
            EtapaPrologo guardada = historia != null ? historia.Etapa : EtapaPrologo.Ninguna;
            bool reanudar = guardada != EtapaPrologo.Ninguna;
            EtapaPrologo etapa = reanudar ? guardada : etapaSinPartida;
            if (!reanudar && !MaquinaPrologo.Maneja(etapa))
            {
                return; // sin partida y sin etapa de la noche ni del presente: el director no hace nada
            }
            if (historia == null)
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no encontró el ServicioHistoria; no hace nada.", this);
                return;
            }

            emboscada = Object.FindFirstObjectByType<DirectorEmboscada>();
            if (emboscada == null)
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no encontró la emboscada de la escena.", this);
            }
            else
            {
                emboscada.EstadoCambio += AlCambiarEstadoEmboscada;
                emboscada.Terminada += AlTerminarEmboscada;
                eventosEmboscada = true;
            }

            maquina = new MaquinaPrologo(datos.TextoIrAlCarro, datos.TextoRegresarACasa, datos.TextoDormir, datos.PasosRuta.Count,
                datos.TextoIrAlClandestino, datos.TextoHablarEncargado);
            maquina.Iniciar(etapa, reanudar);
            if (!maquina.Activa)
            {
                Habilitar(false); // una etapa fuera de la noche: la emboscada no debe saltar
                return;
            }

            if (MaquinaPrologo.EsDelPresente(maquina.Etapa))
            {
                mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
                if (mostrador == null)
                {
                    Debug.LogWarning("[Progresion] DirectorPrologo no encontró el mostrador de peleas de la escena.", this);
                }
                else
                {
                    mostrador.PeleaTerminada += AlTerminarCasera;
                }
            }

            if (maquina.Etapa == EtapaPrologo.Despertar)
            {
                Despertar();
                return;
            }
            if (reanudar)
            {
                ColocarAlReanudar();
            }
            Aplicar();
            if (maquina.Etapa == EtapaPrologo.PeleaTutorial)
            {
                EmpezarTutorial();
            }
            else if (maquina.Etapa == EtapaPrologo.Continuara)
            {
                MostrarContinuara();
            }
        }

        private void SoltarEmboscada()
        {
            if (eventosEmboscada && emboscada != null)
            {
                emboscada.EstadoCambio -= AlCambiarEstadoEmboscada;
                emboscada.Terminada -= AlTerminarEmboscada;
            }
            eventosEmboscada = false;
        }

        private void Update()
        {
            if (maquina == null || maquina.Etapa != EtapaPrologo.RegresoACasa || maquina.IndicePaso >= datos.PasosRuta.Count)
            {
                return;
            }
            // Llegar al objetivo de un paso de la ruta lleva al siguiente (el último, la casa, se cumple al entrar).
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (objetivos != null && objetivos.HayObjetivo && objetivos.Llego && objetivos.Actual.Texto == datos.TextoRegresarACasa)
            {
                Avisar(AvisoPrologo.LlegoAPaso);
            }
        }

        // ---------- Avisos ----------

        private void AlTerminarTutorial(ResultadoPelea resultado)
        {
            Avisar(AvisoPrologo.TutorialTerminado);
        }

        private void AlCambiarEstadoEmboscada(EstadoEmboscada estado)
        {
            if (estado != EstadoEmboscada.Esperando)
            {
                Avisar(AvisoPrologo.EmboscadaEmpezo);
            }
        }

        private void AlTerminarEmboscada()
        {
            Avisar(AvisoPrologo.EmboscadaTerminada);
        }

        private void AlEntrarLugar(IdLugar lugar)
        {
            if (lugar == datos.LugarFinal)
            {
                Avisar(AvisoPrologo.EntroACasa);
            }
            if (lugar == datos.LugarClandestino)
            {
                Avisar(AvisoPrologo.EntroAlClandestino);
            }
        }

        private void AlTerminarCasera(ResultadoPelea resultado)
        {
            ganoCasera = resultado.Ganador == LadoPelea.Jugador;
            if (maquina == null || !maquina.Avisar(AvisoPrologo.PeleaCaseraTerminada))
            {
                return;
            }
            Aplicar();
            Guardar();
            MostrarContinuara();
        }

        private void AlSeguirJugando()
        {
            if (continuara != null)
            {
                continuara.Ocultar();
            }
            if (maquina != null && maquina.Avisar(AvisoPrologo.SeguirJugando))
            {
                Aplicar();
                Guardar();
            }
        }

        private void AlSalirAlTitulo()
        {
            if (continuara != null)
            {
                continuara.Ocultar();
            }
            if (datosEscenas == null)
            {
                Debug.LogError("[Progresion] DirectorPrologo no tiene DatosEscenas: no puede volver al título.", this);
                return;
            }
            CambioEscena.Ir(datosEscenas.RutaPara(EtapaPrologo.CreacionPersonaje)); // CambioEscena guarda antes de cambiar
        }

        private void Avisar(AvisoPrologo aviso)
        {
            if (maquina == null || !maquina.Avisar(aviso))
            {
                return;
            }
            Aplicar();
        }

        // ---------- Efectos de la etapa ----------

        // Pone la etapa en la historia, prende o apaga la emboscada y fija el objetivo que toca.
        private void Aplicar()
        {
            historia.Avanzar(maquina.Etapa); // no retrocede: al reanudar desde Emboscada se queda en Emboscada
            Habilitar(maquina.EmboscadaHabilitada);
            FijarObjetivo();
        }

        private static void Guardar()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio != null)
            {
                servicio.Guardar();
            }
        }

        private void Habilitar(bool habilitada)
        {
            if (emboscada != null)
            {
                emboscada.Habilitada = habilitada;
            }
        }

        private void EmpezarTutorial()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null && (!reloj.EsDeNoche || reloj.Hora < datos.HoraNoche))
            {
                reloj.AvanzarHasta(datos.HoraNoche);
            }
            if (!entrada.Empezar(flujoTutorial))
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no pudo empezar la pelea del tutorial.", this);
            }
        }

        // ---------- El presente (T-133) ----------

        [System.Serializable]
        private class DineroGuardado
        {
            public int dinero;
        }

        // El estado inicial al despertar (D-tony-58): una sola vez, al empezar la etapa Despertar. El reloj va primero para que la renta
        // cuente desde el día en que se despierta.
        private void Despertar()
        {
            ColocarEn(datos.IdAparicionCama);

            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.AvanzarHasta(datos.HoraDespertar);
            }
            ServicioRenta renta = Registro<IRenta>.Actual as ServicioRenta;
            if (renta != null)
            {
                renta.Reiniciar(reloj != null ? reloj.Dia : 1);
            }
            PonerDineroInicial();
            PonerElMechaFregado();
            Registro<ICalor>.Actual?.Borrar();

            // Avanza la historia a PeleaCasera y fija el objetivo; luego guarda ya con todo el estado inicial puesto.
            Avisar(AvisoPrologo.Despertado);
            Guardar();
            EmpezarFundidoYAviso();
        }

        // Dinero = el inicial de DatosEconomia, por el camino de carga de la cartera (motivo Carga: sin aviso de premio ni de cobro).
        private void PonerDineroInicial()
        {
            ServicioCartera cartera = Registro<ICartera>.Actual as ServicioCartera;
            if (cartera == null || cartera.Datos == null)
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no encontró la cartera: el dinero no se reinicia.", this);
                return;
            }
            cartera.Cargar(JsonUtility.ToJson(new DineroGuardado { dinero = cartera.Datos.DineroInicial }));
        }

        private void PonerElMechaFregado()
        {
            MechaPersistente persistente = Object.FindFirstObjectByType<MechaPersistente>();
            if (persistente == null)
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no encontró el mecha de la historia: no se pone fregado.", this);
                return;
            }
            persistente.FijarFraccionVida(datos.FraccionVidaDespertar);
            LlamadoMecha llamado = persistente.GetComponent<LlamadoMecha>();
            if (llamado != null)
            {
                llamado.VolverAlTallerYa();
            }
        }

        private void EmpezarFundidoYAviso()
        {
            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido != null)
            {
                fundido.Fijar(1f);
                StartCoroutine(fundido.Fundir(0f, fundido.Config.SegundosEntrada));
            }
            if (subtitulos != null)
            {
                StartCoroutine(AvisarAlDespertar());
            }
        }

        private IEnumerator AvisarAlDespertar()
        {
            subtitulos.Mostrar(datos.TextoAvisoDespertar);
            avisoVisible = true;
            float transcurrido = 0f;
            while (transcurrido < datos.SegundosAvisoDespertar)
            {
                yield return null;
                transcurrido += Time.unscaledDeltaTime;
            }
            subtitulos.Ocultar();
            avisoVisible = false;
        }

        // La pantalla de fin de la demo con el alias, el dinero y el resultado. Al reanudar en Continuara el resultado no se guardó: se asume victoria.
        private void MostrarContinuara()
        {
            if (continuara == null)
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no tiene la pantalla de continuará.", this);
                return;
            }
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            continuara.Mostrar(perfil != null ? perfil.Alias : string.Empty, cartera != null ? cartera.Dinero : 0, ganoCasera);
        }

        // Partida cargada a media noche: el jugador aparece a pie donde le toca.
        private void ColocarAlReanudar()
        {
            string id = null;
            switch (maquina.Etapa)
            {
                case EtapaPrologo.SalidaClandestino:
                case EtapaPrologo.RegresoACasa:
                    id = datos.IdAparicionSalida;
                    break;
                case EtapaPrologo.Dormir:
                case EtapaPrologo.PeleaCasera:
                    id = datos.IdAparicionCama;
                    break;
            }
            ColocarEn(id);
        }

        private void ColocarEn(string id)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (id == null || jugador == null)
            {
                return;
            }
            IPuntoAparicion punto = BuscarAparicion(id);
            if (punto == null)
            {
                Debug.LogWarning("[Progresion] DirectorPrologo no encontró la aparición '" + id + "'; el jugador se queda donde está.", this);
                return;
            }
            jugador.Colocar(punto.Pose);
        }

        private void FijarObjetivo()
        {
            ServicioObjetivos servicio = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (servicio == null)
            {
                return;
            }
            ObjetivoPrologo deseado = maquina.ObjetivoActual;
            switch (deseado.Destino)
            {
                case DestinoObjetivo.Ninguno:
                    if (maquina.Etapa == EtapaPrologo.Continuara || maquina.Etapa == EtapaPrologo.Libre)
                    {
                        servicio.Quitar(); // el fin de la demo y el juego libre no llevan objetivo
                    }
                    break;
                case DestinoObjetivo.Clandestino:
                    servicio.Fijar(new Objetivo(deseado.Texto, datos.LugarClandestino));
                    break;
                case DestinoObjetivo.Mostrador:
                    servicio.Fijar(mostrador != null
                        ? new Objetivo(deseado.Texto, mostrador.Posicion)
                        : new Objetivo(deseado.Texto, datos.LugarClandestino));
                    break;
                case DestinoObjetivo.Carro:
                    if (TryPosicionCarro(out Vector3 posicionCarro))
                    {
                        servicio.Fijar(new Objetivo(deseado.Texto, posicionCarro));
                    }
                    break;
                case DestinoObjetivo.PasoRuta:
                    IPuntoHistoria paso = BuscarPunto(datos.PasosRuta[deseado.IndicePaso]);
                    if (paso == null)
                    {
                        Debug.LogWarning("[Progresion] Falta el punto de historia '" + datos.PasosRuta[deseado.IndicePaso]
                            + "': se salta ese paso de la ruta.", this);
                        Avisar(AvisoPrologo.LlegoAPaso); // sigue con el siguiente paso (o la casa)
                        break;
                    }
                    servicio.Fijar(new Objetivo(deseado.Texto, paso.Pose.position));
                    break;
                case DestinoObjetivo.Casa:
                    servicio.Fijar(new Objetivo(deseado.Texto, datos.LugarFinal));
                    break;
                case DestinoObjetivo.Cama:
                    servicio.Fijar(ObjetivoCama(deseado.Texto));
                    break;
            }
        }

        private Objetivo ObjetivoCama(string texto)
        {
            foreach (IInteractuable interactuable in Registro<IInteractuable>.Todos)
            {
                if (interactuable is CamaJugador cama)
                {
                    return new Objetivo(texto, cama.Posicion);
                }
            }
            Debug.LogWarning("[Progresion] DirectorPrologo no encontró la cama de la escena; el objetivo apunta a la casa.", this);
            return new Objetivo(texto, datos.LugarFinal);
        }

        // El carro es un punto de historia de Alan; si falta, el respaldo es el carro registrado (con aviso).
        private bool TryPosicionCarro(out Vector3 posicion)
        {
            IPuntoHistoria punto = BuscarPunto(datos.IdPuntoCarro);
            if (punto != null)
            {
                posicion = punto.Pose.position;
                return true;
            }
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable != null && montable.Modo == ModoJugador.Carro)
                {
                    Debug.LogWarning("[Progresion] Falta el punto de historia '" + datos.IdPuntoCarro + "': el objetivo apunta al carro registrado.", this);
                    posicion = montable.PuntoSubida;
                    return true;
                }
            }
            Debug.LogWarning("[Progresion] Falta el punto de historia '" + datos.IdPuntoCarro + "' y no hay carro: no se fija el objetivo.", this);
            posicion = Vector3.zero;
            return false;
        }

        private static IPuntoHistoria BuscarPunto(string id)
        {
            foreach (IPuntoHistoria punto in Registro<IPuntoHistoria>.Todos)
            {
                if (punto != null && punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private static IPuntoAparicion BuscarAparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto != null && punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }
    }
}
