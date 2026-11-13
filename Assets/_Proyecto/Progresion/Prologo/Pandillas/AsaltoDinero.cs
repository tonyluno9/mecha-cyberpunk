using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>
    /// T-728 (rehecho sobre T-717): la historia del dinero. Al salir de casa por primera vez hay una cinemática corta: un asaltante (SKULL) asalta a una persona de afuera
    /// (NO a ti: no te quitan dinero), corre a su carro (un carro del pool del tráfico, por el punto de historia <c>Historia_Asalto</c> que pone <c>AsaltoMundo</c> de Trafico) y
    /// huye. Tú quedas quieto durante la cinemática. Después el objetivo es "RÓBATE UN CARRO" y, ya en uno, "PERSÍGUELOS" (el marcador se mueve con el carro). Si le ponchas una
    /// llanta (disparo a las ruedas o choque), llega al final de su ruta o se atora, el carro se detiene: el asaltante baja y pelea. Al vencerlo la víctima te da una parte de lo
    /// robado. Enter salta la cinemática. Nunca deja la historia trabada: sin carro o sin tráfico en la escena, el asalto se cancela y la historia sigue.
    /// Lo mueve <see cref="DirectorPandillas"/>; las reglas puras están en <see cref="ReglaAsalto"/>.
    /// </summary>
    public class AsaltoDinero : MonoBehaviour
    {
        public const string TextoPerseguir = "PERSÍGUELOS";
        public const string TextoRobarCarro = "RÓBATE UN CARRO";
        public const string TextoMaletin = "RECOGE EL MALETÍN";
        public const string TextoMaletinEnCarro = "BÁJATE Y RECOGE EL MALETÍN";
        public const string PrefijoRobo = "ASALTAN A UNA PERSONA: ";
        public const string AvisoMaletin = "MALETÍN RECUPERADO · +$";
        public const string SubtituloRobo = "—¡La lana! ¡Toda, mija!";
        public const string SubtituloHuida = "—¡Gracias por la lana!";
        public const string TeclaSaltar = "ENTER: SALTAR";
        public const string NombreAsaltante = "Asaltante";
        public const string NombreVictima = "VictimaAsalto";
        public const string RecursoVictima = "VictimaAsalto";
        public const string IdEscena = "Historia_Asalto_Escena";
        public const string RecursoAsaltante = "SkullRefuerzo";
        public const int OrdenCanvas = 30;

        private const float DistanciaAparicion = 8f;
        private const float DistanciaRobo = 1.8f;
        private const float RapidezCaminar = 3.5f;
        private const float RapidezCorrer = 6.5f;
        private const float DistanciaAlCarro = 2f;
        private const float SegundosMaximosEsperandoCarro = 20f;
        private const float SegundosEntreObjetivos = 0.4f;
        private const float SeparacionPuertaCarro = 2.4f;
        private const float AlturaSobreSuelo = 0.05f;
        private const float SeparacionMaletin = 1.9f;
        private const float AtrasMaletin = 1.4f;
        private const float DistanciaMaletinJugador = 1.5f;
        private const float AltoBarras = 0.12f;
        private const float DistanciaCamara = 6.5f;
        private const float AlturaCamara = 2.4f;

        private readonly ReglaAsalto regla = new ReglaAsalto();
        private PantallaHistoriaPandillas pantalla;
        private GameObject prefabAsaltante;
        private GameObject asaltante;
        private GameObject victima;
        private PuntoEscena puntoEscena;
        private PandilleroAPie pandillero;
        private MaletinAsalto maletin;
        private Camera camaraCinematica;
        private Canvas canvasBarras;
        private ControladorAPie controlador;
        private Coroutine cinematica;
        private float proximoObjetivo;
        private bool saltar;
        private bool hudOculto;

        public FaseAsalto Fase => regla.Fase;
        public int DineroRobado => regla.DineroRobado;
        public bool DineroEnManosDelLadron => regla.DineroEnManosDelLadron;
        public GameObject Asaltante => asaltante;

        /// <summary>T-987: el maletín que sueltan al detenerse el carro (null antes y después).</summary>
        public MaletinAsalto Maletin => maletin;

        /// <summary>La persona de afuera a la que asaltan (null fuera del asalto).</summary>
        public GameObject Victima => victima;

        /// <summary>El objetivo que toca ahora según la fase y si vas en un vehículo (para el HUD y las pruebas).</summary>
        public string ObjetivoActual => ReglaAsalto.TextoObjetivo(regla.Fase, JugadorEnVehiculo());

        public bool CinematicaVisible => canvasBarras != null && canvasBarras.gameObject.activeSelf;

        /// <summary>Avisa una sola vez cuando el asalto terminó (dinero recuperado, o cancelado por falta de carro): la historia sigue.</summary>
        public event Action Terminado;

        public void Configurar(PantallaHistoriaPandillas pantallaHistoria, GameObject prefab = null)
        {
            pantalla = pantallaHistoria;
            if (prefab != null)
            {
                prefabAsaltante = prefab;
            }
        }

        // ---------- Arranque y cancelación ----------

        /// <summary>Empieza el asalto desde cero. <paramref name="reponer"/> es dinero que un guardado viejo (T-717) le había quitado al jugador a medio asalto: vuelve a la cartera.</summary>
        public void Iniciar(int reponer = 0)
        {
            regla.Cancelar();
            Limpiar();
            ICartera cartera = Registro<ICartera>.Actual;
            if (reponer > 0 && cartera != null)
            {
                cartera.Abonar(reponer, MotivoDinero.Otro);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            IPuntoHistoria punto = BuscarPunto(AsaltoMundoId.Principal);
            if (jugador == null || jugador.Raiz == null || punto == null)
            {
                Debug.LogWarning("[Progresion] AsaltoDinero: falta " + (punto == null ? "el carro del asaltante (Trafico)" : "el jugador") + "; no hay asalto y la historia sigue.", this);
                regla.Iniciar();
                Terminar();
                return;
            }
            if (prefabAsaltante == null)
            {
                prefabAsaltante = Resources.Load<GameObject>(RecursoAsaltante);
            }
            if (prefabAsaltante == null)
            {
                Debug.LogWarning("[Progresion] AsaltoDinero: falta el prefab " + RecursoAsaltante + "; no hay asalto y la historia sigue.", this);
                regla.Iniciar();
                Terminar();
                return;
            }
            regla.Iniciar();
            controlador = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            PonerEscena(jugador);
            punto.Activar(true); // el carro sale hacia donde asaltan a la víctima
            PonerObjetivo(PosicionObjetivoPersecucion(jugador)); // "RÓBATE UN CARRO" desde el principio (el HUD sigue oculto durante la cinemática)
            cinematica = StartCoroutine(Cinematica(jugador, punto));
        }

        /// <summary>Quita el asalto del mundo (otro paso de la historia): suelta al jugador. No hay dinero tuyo que reponer.</summary>
        public void Cancelar()
        {
            if (regla.Fase == FaseAsalto.Inactivo)
            {
                return;
            }
            regla.Cancelar();
            Limpiar();
        }

        private void OnDisable() => Limpiar();

        private void Limpiar()
        {
            if (cinematica != null)
            {
                StopCoroutine(cinematica);
                cinematica = null;
            }
            LiberarJugador();
            QuitarCinematica();
            QuitarMaletin();
            if (asaltante != null)
            {
                Destroy(asaltante);
                asaltante = null;
                pandillero = null;
            }
            QuitarVictima();
        }

        private void QuitarMaletin()
        {
            if (maletin != null)
            {
                maletin.Recogido -= AlRecogerMaletin;
                Destroy(maletin.gameObject);
                maletin = null;
            }
        }

        private void QuitarVictima()
        {
            if (puntoEscena != null)
            {
                Registro<IPuntoHistoria>.Quitar(puntoEscena);
                puntoEscena = null;
            }
            if (victima != null)
            {
                Destroy(victima);
                victima = null;
            }
        }

        private void Terminar()
        {
            Limpiar();
            Terminado?.Invoke();
        }

        // ---------- La cinemática ----------

        private IEnumerator Cinematica(IJugador jugador, IPuntoHistoria punto)
        {
            Congelar(jugador);
            PonerCinematica(jugador);

            // 1. El asaltante camina hacia la víctima (una persona de afuera; tú solo miras).
            while (!saltar && Distancia(asaltante.transform.position, victima.transform.position) > DistanciaRobo)
            {
                Mover(asaltante.transform, victima.transform.position, RapidezCaminar);
                SeguirConLaCamara(jugador, victima.transform.position);
                LeerSalto();
                yield return null;
            }

            // 2. El robo: se encaran un momento.
            Robar();
            Encarar(victima.transform, asaltante.transform.position);
            Encarar(asaltante.transform, victima.transform.position);
            float fin = Time.unscaledTime + 1.6f;
            while (!saltar && Time.unscaledTime < fin)
            {
                SeguirConLaCamara(jugador, victima.transform.position);
                LeerSalto();
                yield return null;
            }

            // 3. Corre al carro (espera a que llegue si hace falta).
            float limite = Time.unscaledTime + SegundosMaximosEsperandoCarro;
            pantalla?.MostrarSubtitulo(SubtituloHuida, 3f);
            while (true)
            {
                IPuntoHistoria listo = BuscarPunto(AsaltoMundoId.Listo);
                if (listo != null)
                {
                    Vector3 puerta = listo.Pose.position + listo.Pose.rotation * Vector3.right * 1.2f;
                    if (saltar || Distancia(asaltante.transform.position, puerta) <= DistanciaAlCarro)
                    {
                        break;
                    }
                    Mover(asaltante.transform, puerta, RapidezCorrer);
                    SeguirConLaCamara(jugador, asaltante.transform.position);
                }
                else if (Time.unscaledTime > limite)
                {
                    Debug.LogWarning("[Progresion] AsaltoDinero: el carro del asaltante no llegó a tiempo; se cancela el asalto y la historia sigue.", this);
                    cinematica = null;
                    CancelarPorFalloYSeguir();
                    yield break;
                }
                else
                {
                    SeguirConLaCamara(jugador, asaltante.transform.position);
                }
                LeerSalto();
                yield return null;
            }

            // 4. Sube y el carro huye.
            asaltante.SetActive(false);
            regla.Abordar();
            LiberarJugador();
            QuitarCinematica();
            cinematica = null;
            BuscarPunto(AsaltoMundoId.Principal)?.Activar(false);
            QuitarVictima(); // la víctima ya no hace falta en la escena
            PonerObjetivo(PosicionObjetivoPersecucion(jugador));
        }

        private void Robar()
        {
            int robado = regla.Robar();
            if (robado <= 0)
            {
                return;
            }
            pantalla?.MostrarSubtitulo(SubtituloRobo, 3f);
            pantalla?.MostrarAyuda(PrefijoRobo + "$" + robado, 4f);
        }

        private void CancelarPorFalloYSeguir()
        {
            regla.Cancelar();
            Terminar();
        }

        private void LeerSalto()
        {
            if (regla.PuedeSaltar && MechaCyberpunk.UI.Hud.HudCinematica.SaltarPresionado)
            {
                saltar = true;
                // El salto hace el robo ya, para que lo que sigue sea igual.
                Robar();
            }
        }

        // ---------- Persecución y pelea ----------

        private void Update()
        {
            if (regla.Fase == FaseAsalto.Persecucion)
            {
                SeguirElCarro();
            }
            else if (regla.Fase == FaseAsalto.Maletin)
            {
                VigilarMaletin();
            }
        }

        private void SeguirElCarro()
        {
            IPuntoHistoria principal = BuscarPunto(AsaltoMundoId.Principal);
            if (BuscarPunto(AsaltoMundoId.Detenido) != null)
            {
                BajarAlAsaltante(principal != null ? principal.Pose : default);
                return;
            }
            if (Time.time >= proximoObjetivo && principal != null)
            {
                IJugador jugador = Registro<IJugador>.Actual;
                PonerObjetivo(PosicionObjetivoPersecucion(jugador));
            }
        }

        private static bool JugadorEnVehiculo()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Modo == ModoJugador.Carro;
        }

        /// <summary>
        /// Adonde apunta el marcador en la persecución: al carro de los asaltantes si ya vas en un vehículo; si no, al carro que se puede robar más cercano (si no hay,
        /// al carro de los asaltantes para no dejar el marcador sin sitio).
        /// </summary>
        private static Vector3 PosicionObjetivoPersecucion(IJugador jugador)
        {
            IPuntoHistoria principal = BuscarPunto(AsaltoMundoId.Principal);
            Vector3 deLosAsaltantes = principal != null ? principal.Pose.position : (jugador != null && jugador.Raiz != null ? jugador.Raiz.position : default);
            if (jugador == null || jugador.Raiz == null || jugador.Modo == ModoJugador.Carro)
            {
                return deLosAsaltantes;
            }
            Vector3? cercano = null;
            float mejor = float.MaxValue;
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable == null || montable.Modo != ModoJugador.Carro || !montable.PuedeSubir || montable.Ocupado)
                {
                    continue;
                }
                Vector3 donde = montable.PuntoSubida;
                if (principal != null && Distancia(donde, deLosAsaltantes) < 4f)
                {
                    continue; // el carro de los asaltantes no cuenta como carro por robar
                }
                float d = Distancia(donde, jugador.Raiz.position);
                if (d < mejor)
                {
                    mejor = d;
                    cercano = donde;
                }
            }
            return cercano ?? deLosAsaltantes;
        }

        private void BajarAlAsaltante(Pose carro)
        {
            regla.CarroDetenido();
            Vector3 puerta = carro.position + carro.rotation * Vector3.right * SeparacionPuertaCarro;
            puerta.y = Suelo(puerta, carro.position.y) + AlturaSobreSuelo;
            asaltante.transform.position = puerta;
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 hacia = jugador != null && jugador.Raiz != null ? jugador.Raiz.position - puerta : carro.rotation * Vector3.forward;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.01f)
            {
                asaltante.transform.rotation = Quaternion.LookRotation(hacia);
            }
            asaltante.SetActive(true);
            if (pandillero != null)
            {
                pandillero.enabled = true;
                pandillero.Alerta = true;
            }
            PonerMaletin(carro);
        }

        // ---------- El maletín (T-987) ----------

        // Lo suelta junto a la puerta del carro; si no hay piso ahí, junto al jugador. Nunca queda bajo el piso.
        private void PonerMaletin(Pose carro)
        {
            QuitarMaletin();
            Vector3 donde = carro.position + carro.rotation * (Vector3.right * SeparacionMaletin + Vector3.back * AtrasMaletin);
            if (!MaletinAsalto.BuscarPiso(donde, asaltante != null ? asaltante.transform : null, out Vector3 piso))
            {
                piso = PisoJuntoAlJugador();
            }
            CrearMaletin(piso, carro.rotation.eulerAngles.y);
        }

        private void CrearMaletin(Vector3 piso, float giro)
        {
            maletin = MaletinAsalto.Crear(piso, giro);
            maletin.Recogido += AlRecogerMaletin;
            PonerObjetivo(PosicionMaletin());
        }

        private Vector3 PisoJuntoAlJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return maletin != null ? maletin.transform.position : Vector3.zero;
            }
            Vector3 al = jugador.Raiz.position + jugador.Raiz.forward * DistanciaMaletinJugador;
            if (MaletinAsalto.BuscarPiso(al, jugador.Raiz, out Vector3 piso))
            {
                return piso;
            }
            return jugador.Raiz.position;
        }

        private Vector3 PosicionMaletin() => maletin != null ? maletin.transform.position + Vector3.up * 0.4f : Vector3.zero;

        // Cada cuadro: si el maletín se perdió (destruido o cayó fuera del mundo) se vuelve a poner junto al jugador; el marcador sigue y cambia de texto al subir o bajar de un carro.
        private void VigilarMaletin()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (maletin == null || (jugador != null && jugador.Raiz != null && maletin.EstaPerdido(jugador.Raiz.position)))
            {
                QuitarMaletin();
                CrearMaletin(PisoJuntoAlJugador(), 0f);
                return;
            }
            if (Time.time >= proximoObjetivo)
            {
                PonerObjetivo(PosicionMaletin());
            }
        }

        private void AlRecogerMaletin()
        {
            int premio = regla.MaletinRecogido();
            if (premio <= 0)
            {
                return; // ya se cobró: nunca se paga dos veces
            }
            if (maletin != null)
            {
                maletin.Recogido -= AlRecogerMaletin;
                Destroy(maletin.gameObject);
                maletin = null;
            }
            // El aviso del maletín va primero en la cola; el del dinero sale después como "+$10,000 · PREMIO".
            pantalla?.MostrarAyuda(AvisoMaletin + premio.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), 4f);
            Registro<ICartera>.Actual?.Abonar(premio, MotivoDinero.Premio);
            StartCoroutine(GestoRecoger());
            QuitarVictima();
            QuitarObjetivo();
            // El asaltante sigue ahí peleando: ya no es necesario vencerlo.
            asaltante = null;
            pandillero = null;
            Terminado?.Invoke();
        }

        // El gesto se juega un cuadro después de usar el interactuable: el jugador pone su gesto genérico justo tras Usar y este lo reemplaza.
        private IEnumerator GestoRecoger()
        {
            yield return null;
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                jugador.Raiz.GetComponent<MechaCyberpunk.Personaje.Jugador.GestosJugador>()?.Gestear(MechaCyberpunk.Personaje.Visual.GestoVisual.Recoger);
            }
        }

        // ---------- Mundo ----------

        /// <summary>
        /// Arma la escena a unos metros del jugador: la víctima (una persona de afuera) parada y el asaltante unos metros más allá, de frente a ella. Registra el punto
        /// <c>Historia_Asalto_Escena</c> para que el carro de Trafico llegue ahí.
        /// </summary>
        private void PonerEscena(IJugador jugador)
        {
            Transform t = jugador.Raiz;
            Vector3[] direcciones = { t.forward, t.right, -t.right, -t.forward };
            Vector3 puntoVictima = t.position + t.forward * DistanciaAparicion;
            Vector3 haciaFuera = t.forward;
            foreach (Vector3 d in direcciones)
            {
                Vector3 candidato = t.position + d * DistanciaAparicion;
                Vector3 atras = candidato + d * DistanciaAsaltanteAtras;
                if (SueloLibre(candidato, out Vector3 sueloVictima) && SueloLibre(atras, out _))
                {
                    puntoVictima = sueloVictima;
                    haciaFuera = d;
                    break;
                }
            }
            SueloLibre(puntoVictima + haciaFuera * DistanciaAsaltanteAtras, out Vector3 puntoAsaltante);
            if (Distancia(puntoAsaltante, puntoVictima) < 1f)
            {
                puntoAsaltante = puntoVictima + haciaFuera * DistanciaAsaltanteAtras;
            }

            if (victima == null)
            {
                GameObject prefabVictima = Resources.Load<GameObject>(RecursoVictima);
                Quaternion giroVictima = Quaternion.LookRotation(Plano(puntoAsaltante - puntoVictima, haciaFuera));
                victima = prefabVictima != null ? Instantiate(prefabVictima, puntoVictima, giroVictima) : new GameObject(NombreVictima);
                victima.transform.SetPositionAndRotation(puntoVictima, giroVictima);
                victima.name = NombreVictima;
            }
            puntoEscena = new PuntoEscena(new Pose(puntoVictima, victima.transform.rotation));
            Registro<IPuntoHistoria>.Agregar(puntoEscena);

            Quaternion giro = Quaternion.LookRotation(Plano(puntoVictima - puntoAsaltante, -haciaFuera));
            asaltante = Instantiate(prefabAsaltante, puntoAsaltante, giro);
            asaltante.name = NombreAsaltante;
            pandillero = asaltante.GetComponent<PandilleroAPie>();
            if (pandillero != null)
            {
                pandillero.enabled = false; // en la cinemática no pelea; sigue quieto hasta que se detenga el carro
            }
        }

        private const float DistanciaAsaltanteAtras = 5f;

        // Un punto del suelo bajo "candidato" sin nada sólido encima (para pararse). False si no hay suelo o hay un muro.
        private static bool SueloLibre(Vector3 candidato, out Vector3 suelo)
        {
            suelo = candidato;
            if (Physics.Raycast(candidato + Vector3.up * 3f, Vector3.down, out RaycastHit golpe, 8f, ~0, QueryTriggerInteraction.Ignore)
                && !Physics.CheckSphere(golpe.point + Vector3.up * 1f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                suelo = golpe.point + Vector3.up * AlturaSobreSuelo;
                return true;
            }
            return false;
        }

        private static Vector3 Plano(Vector3 v, Vector3 respaldo)
        {
            v.y = 0f;
            if (v.sqrMagnitude > 0.01f)
            {
                return v;
            }
            respaldo.y = 0f;
            return respaldo.sqrMagnitude > 0.01f ? respaldo : Vector3.forward;
        }

        private static void Encarar(Transform quien, Vector3 haciaDonde)
        {
            Vector3 d = haciaDonde - quien.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
            {
                quien.rotation = Quaternion.LookRotation(d);
            }
        }

        private sealed class PuntoEscena : IPuntoHistoria
        {
            public PuntoEscena(Pose pose) => Pose = pose;
            public string Id => IdEscena;
            public Pose Pose { get; }
            public void Activar(bool activo) { }
        }

        private static void Mover(Transform t, Vector3 destino, float rapidez)
        {
            Vector3 hacia = destino - t.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 0.0001f)
            {
                return;
            }
            Vector3 siguiente = t.position + hacia.normalized * (rapidez * Time.deltaTime);
            // El suelo se busca desde arriba del personaje sin contar su propio collider (si no, se subiría sobre su propia cabeza).
            siguiente.y = SueloSin(siguiente + Vector3.up * 2.5f, t, t.position.y - AlturaSobreSuelo) + AlturaSobreSuelo;
            t.position = siguiente;
            t.rotation = Quaternion.LookRotation(hacia.normalized);
        }

        private static readonly RaycastHit[] golpes = new RaycastHit[16];

        private static float SueloSin(Vector3 arriba, Transform ignorar, float respaldo)
        {
            int n = Physics.RaycastNonAlloc(arriba, Vector3.down, golpes, 7f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            float y = respaldo;
            for (int i = 0; i < n; i++)
            {
                if (golpes[i].collider.transform.root == ignorar.root || golpes[i].distance >= mejor)
                {
                    continue;
                }
                mejor = golpes[i].distance;
                y = golpes[i].point.y;
            }
            return y;
        }

        private static float Suelo(Vector3 punto, float respaldo)
        {
            if (Physics.Raycast(punto + Vector3.up * 2f, Vector3.down, out RaycastHit golpe, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point.y;
            }
            return respaldo - 0.3f;
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.magnitude;
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

        // ---------- Objetivo ----------

        private void PonerObjetivo(Vector3 posicion)
        {
            proximoObjetivo = Time.time + SegundosEntreObjetivos;
            string texto = ReglaAsalto.TextoObjetivo(regla.Fase, JugadorEnVehiculo());
            if (texto.Length == 0)
            {
                return;
            }
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(texto, posicion));
        }

        private static void QuitarObjetivo() => (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();

        // ---------- Cámara, barras y control ----------

        private void Congelar(IJugador jugador)
        {
            saltar = false;
            if (controlador != null)
            {
                controlador.ActivarControl(false);
            }
        }

        private void LiberarJugador()
        {
            if (controlador != null)
            {
                controlador.ActivarControl(true);
            }
        }

        private void PonerCinematica(IJugador jugador)
        {
            if (jugador.Camara != null)
            {
                GameObject objeto = new GameObject("CamaraAsalto");
                camaraCinematica = objeto.AddComponent<Camera>();
                camaraCinematica.CopyFrom(jugador.Camara);
                camaraCinematica.depth = jugador.Camara.depth + 5f;
                camaraCinematica.targetTexture = null;
                SeguirConLaCamara(jugador, victima != null ? victima.transform.position : asaltante.transform.position, true);
            }
            EstiloCyberfix e = KitUI.Estilo;
            canvasBarras = KitUI.CrearCanvas(transform, "CanvasAsalto", OrdenCanvas);
            Image arriba = KitUI.CrearImagen(canvasBarras.transform, "BarraArriba", Color.black);
            arriba.rectTransform.anchorMin = new Vector2(0f, 1f);
            arriba.rectTransform.anchorMax = new Vector2(1f, 1f);
            arriba.rectTransform.pivot = new Vector2(0.5f, 1f);
            arriba.rectTransform.anchoredPosition = Vector2.zero;
            arriba.rectTransform.sizeDelta = new Vector2(0f, 1080f * AltoBarras);
            Image abajo = KitUI.CrearImagen(canvasBarras.transform, "BarraAbajo", Color.black);
            abajo.rectTransform.anchorMin = new Vector2(0f, 0f);
            abajo.rectTransform.anchorMax = new Vector2(1f, 0f);
            abajo.rectTransform.pivot = new Vector2(0.5f, 0f);
            abajo.rectTransform.anchoredPosition = Vector2.zero;
            abajo.rectTransform.sizeDelta = new Vector2(0f, 1080f * AltoBarras);
            Text tecla = KitUI.CrearTexto(abajo.transform, "TeclaSaltar", RolTexto.Ayuda, TextAnchor.MiddleRight);
            tecla.text = MechaCyberpunk.UI.Hud.HudCinematica.TextoSaltar();
            // T-726: en la cinemática el HUD se oculta y solo queda este letrero.
            MechaCyberpunk.UI.Hud.HudCinematica.Entrar();
            hudOculto = true;
            tecla.color = e.Turquesa;
            tecla.rectTransform.offsetMax = new Vector2(-60f, 0f);
        }

        private void SeguirConLaCamara(IJugador jugador, Vector3 haciaDonde, bool deGolpe = false)
        {
            if (camaraCinematica == null)
            {
                return;
            }
            Vector3 jugadorPos = jugador.Raiz.position;
            Vector3 centro = haciaDonde + Vector3.up * 1.4f; // la escena (víctima y asaltante) al centro; el jugador solo mira
            Vector3 eje = haciaDonde - jugadorPos;
            eje.y = 0f;
            Vector3 lado = eje.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, eje.normalized) : jugador.Raiz.right;
            Vector3 atras = eje.sqrMagnitude > 0.01f ? -eje.normalized : -jugador.Raiz.forward;
            Vector3 posicion = PosicionLibre(centro, new[] { lado, -lado, atras, -atras }, DistanciaCamara, AlturaCamara - 1.4f);
            camaraCinematica.transform.position = deGolpe ? posicion : Vector3.Lerp(camaraCinematica.transform.position, posicion, 0.2f);
            camaraCinematica.transform.rotation = Quaternion.LookRotation((centro - camaraCinematica.transform.position).normalized, Vector3.up);
        }

        /// <summary>
        /// Un punto a <paramref name="distancia"/> m de <paramref name="centro"/> en la primera de las <paramref name="direcciones"/> desde donde se ve el centro sin pared de por medio
        /// (si ninguna sirve, se acerca). Así la cámara de la cinemática no queda dentro de una casa.
        /// </summary>
        public static Vector3 PosicionLibre(Vector3 centro, Vector3[] direcciones, float distancia, float altura)
        {
            for (float d = distancia; d >= 1.5f; d *= 0.7f)
            {
                foreach (Vector3 direccion in direcciones)
                {
                    Vector3 punto = centro + direccion * d + Vector3.up * altura;
                    if (!Physics.Linecast(centro, punto, ~0, QueryTriggerInteraction.Ignore) && !Physics.CheckSphere(punto, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        return punto;
                    }
                }
            }
            return centro + Vector3.up * altura;
        }

        private void QuitarCinematica()
        {
            if (hudOculto)
            {
                hudOculto = false;
                MechaCyberpunk.UI.Hud.HudCinematica.Salir();
            }
            if (camaraCinematica != null)
            {
                Destroy(camaraCinematica.gameObject);
                camaraCinematica = null;
            }
            if (canvasBarras != null)
            {
                Destroy(canvasBarras.gameObject);
                canvasBarras = null;
            }
        }
    }

    /// <summary>Los ids de los puntos de historia del carro del asaltante (los pone <c>AsaltoMundo</c> en Trafico; el mismo texto, sin referencia entre ensamblados).</summary>
    public static class AsaltoMundoId
    {
        public const string Principal = "Historia_Asalto";
        public const string Listo = "Historia_Asalto_Listo";
        public const string Detenido = "Historia_Asalto_Detenido";
    }
}
