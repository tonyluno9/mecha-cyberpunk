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
    /// T-717: la historia del dinero. Al salir de casa por primera vez hay una cinemática corta: un asaltante (SKULL) te quita todo el dinero, corre a su carro
    /// (un carro del pool del tráfico, por el punto de historia <c>Historia_Asalto</c> que pone <c>AsaltoMundo</c> de Trafico) y huye. Sigues el carro con el objetivo y el
    /// marcador moviéndose con él. Si le ponchas una llanta (disparo a las ruedas o choque), llega al final de su ruta o se atora, el carro se detiene: el asaltante baja
    /// y pelea. Al vencerlo, el dinero vuelve a tu cartera. Enter salta la cinemática (el robo y la huida pasan igual). Nunca deja la historia trabada: sin carro o sin
    /// tráfico en la escena, el asalto se cancela, el dinero vuelve y la historia sigue. Lo mueve <see cref="DirectorPandillas"/>; las reglas puras están en <see cref="ReglaAsalto"/>.
    /// </summary>
    public class AsaltoDinero : MonoBehaviour
    {
        public const string TextoPerseguir = "PERSIGUE AL ASALTANTE";
        public const string TextoRecuperar = "RECUPERA TU DINERO";
        public const string PrefijoRobo = "TE ROBARON ";
        public const string PrefijoRecuperado = "RECUPERASTE ";
        public const string SubtituloRobo = "—¡La lana! ¡Toda, mijo!";
        public const string SubtituloHuida = "—¡Gracias por la lana!";
        public const string TeclaSaltar = "ENTER: SALTAR";
        public const string NombreAsaltante = "Asaltante";
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
        private const float AltoBarras = 0.12f;
        private const float DistanciaCamara = 6.5f;
        private const float AlturaCamara = 2.4f;

        private readonly ReglaAsalto regla = new ReglaAsalto();
        private PantallaHistoriaPandillas pantalla;
        private GameObject prefabAsaltante;
        private GameObject asaltante;
        private PandilleroAPie pandillero;
        private Camera camaraCinematica;
        private Canvas canvasBarras;
        private ControladorAPie controlador;
        private Coroutine cinematica;
        private float proximoObjetivo;
        private bool saltar;

        public FaseAsalto Fase => regla.Fase;
        public int DineroRobado => regla.DineroRobado;
        public bool DineroEnManosDelLadron => regla.DineroEnManosDelLadron;
        public GameObject Asaltante => asaltante;
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

        /// <summary>Empieza el asalto desde cero. <paramref name="reponer"/> es dinero que se quitó en una partida guardada a medio asalto: vuelve a la cartera antes de empezar.</summary>
        public void Iniciar(int reponer = 0)
        {
            reponer += regla.Cancelar(); // si ya había un asalto a medias, el dinero que tenía el ladrón vuelve antes de empezar de nuevo
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
                regla.Iniciar(0);
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
                regla.Iniciar(0);
                Terminar();
                return;
            }
            regla.Iniciar(cartera != null ? cartera.Dinero : 0);
            controlador = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            punto.Activar(true); // el carro sale hacia ti
            PonerAsaltante(jugador);
            PonerObjetivo(asaltante.transform.position);
            cinematica = StartCoroutine(Cinematica(jugador, punto));
        }

        /// <summary>Quita el asalto del mundo (otro paso de la historia): suelta al jugador y repone el dinero si te lo habían quitado.</summary>
        public void Cancelar()
        {
            if (regla.Fase == FaseAsalto.Inactivo)
            {
                return;
            }
            int devuelto = regla.Cancelar();
            if (devuelto > 0)
            {
                Registro<ICartera>.Actual?.Abonar(devuelto, MotivoDinero.Otro);
            }
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
            if (asaltante != null)
            {
                Destroy(asaltante);
                asaltante = null;
                pandillero = null;
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
            ICartera cartera = Registro<ICartera>.Actual;
            Vector3 mirada = asaltante.transform.position;

            // 1. El asaltante camina hacia ti.
            while (!saltar && Distancia(asaltante.transform.position, jugador.Raiz.position) > DistanciaRobo)
            {
                Mover(asaltante.transform, jugador.Raiz.position, RapidezCaminar);
                SeguirConLaCamara(jugador, asaltante.transform.position);
                PonerObjetivo(asaltante.transform.position);
                LeerSalto();
                yield return null;
            }

            // 2. El robo.
            Robar(cartera);
            float fin = Time.unscaledTime + 1.6f;
            while (!saltar && Time.unscaledTime < fin)
            {
                SeguirConLaCamara(jugador, asaltante.transform.position);
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
                PonerObjetivo(asaltante.transform.position);
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
            PonerObjetivo(BuscarPunto(AsaltoMundoId.Principal) != null ? BuscarPunto(AsaltoMundoId.Principal).Pose.position : jugador.Raiz.position);
        }

        private void Robar(ICartera cartera)
        {
            int cobrado = regla.Robar();
            if (cobrado > 0 && cartera != null)
            {
                cartera.IntentarPagar(cobrado, MotivoDinero.Otro);
            }
            pantalla?.MostrarSubtitulo(SubtituloRobo, 3f);
            pantalla?.MostrarAyuda(PrefijoRobo + "$" + regla.DineroRobado, 4f);
        }

        private void CancelarPorFalloYSeguir()
        {
            int devuelto = regla.Cancelar();
            if (devuelto > 0)
            {
                Registro<ICartera>.Actual?.Abonar(devuelto, MotivoDinero.Otro);
            }
            Terminar();
        }

        private void LeerSalto()
        {
            Keyboard teclado = Keyboard.current;
            if (regla.PuedeSaltar && teclado != null && teclado.enterKey.wasPressedThisFrame)
            {
                saltar = true;
                // El salto hace el robo ya, para que lo que sigue sea igual.
                Robar(Registro<ICartera>.Actual);
            }
        }

        // ---------- Persecución y pelea ----------

        private void Update()
        {
            if (regla.Fase == FaseAsalto.Persecucion)
            {
                SeguirElCarro();
            }
            else if (regla.Fase == FaseAsalto.Enfrentando)
            {
                SeguirAlAsaltante();
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
                PonerObjetivo(principal.Pose.position);
            }
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
                pandillero.Vencido += AlVencerAsaltante;
            }
            PonerObjetivo(puerta);
        }

        private void SeguirAlAsaltante()
        {
            if (asaltante == null)
            {
                AlVencerAsaltante(null); // se perdió (cayó del mundo, se destruyó): no se traba la historia
                return;
            }
            if (Time.time >= proximoObjetivo)
            {
                PonerObjetivo(asaltante.transform.position);
            }
        }

        private void AlVencerAsaltante(PandilleroAPie vencido)
        {
            if (pandillero != null)
            {
                pandillero.Vencido -= AlVencerAsaltante;
            }
            int devuelto = regla.AsaltanteVencido();
            if (regla.Fase != FaseAsalto.Terminado)
            {
                return;
            }
            if (devuelto > 0)
            {
                Registro<ICartera>.Actual?.Abonar(devuelto, MotivoDinero.Otro);
                pantalla?.MostrarAyuda(PrefijoRecuperado + "$" + devuelto, 4f);
            }
            QuitarObjetivo();
            // El asaltante vencido se queda en el piso (cayó); solo se avisa que la historia sigue.
            asaltante = null;
            pandillero = null;
            Terminado?.Invoke();
        }

        // ---------- Mundo ----------

        private void PonerAsaltante(IJugador jugador)
        {
            Transform t = jugador.Raiz;
            Vector3[] direcciones = { t.forward, t.right, -t.right, -t.forward };
            Vector3 punto = t.position + t.forward * DistanciaAparicion;
            foreach (Vector3 d in direcciones)
            {
                Vector3 candidato = t.position + d * DistanciaAparicion;
                if (Physics.Raycast(candidato + Vector3.up * 3f, Vector3.down, out RaycastHit golpe, 8f, ~0, QueryTriggerInteraction.Ignore)
                    && !Physics.CheckSphere(golpe.point + Vector3.up * 1f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    punto = golpe.point + Vector3.up * AlturaSobreSuelo;
                    break;
                }
            }
            Vector3 hacia = t.position - punto;
            hacia.y = 0f;
            Quaternion giro = hacia.sqrMagnitude > 0.01f ? Quaternion.LookRotation(hacia) : Quaternion.identity;
            asaltante = Instantiate(prefabAsaltante, punto, giro);
            asaltante.name = NombreAsaltante;
            pandillero = asaltante.GetComponent<PandilleroAPie>();
            if (pandillero != null)
            {
                pandillero.enabled = false; // en la cinemática no pelea; sigue quieto hasta que se detenga el carro
            }
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
            string texto = ReglaAsalto.TextoObjetivo(regla.Fase);
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
                SeguirConLaCamara(jugador, asaltante.transform.position, true);
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
            tecla.text = TeclaSaltar;
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
            Vector3 centro = (jugadorPos + haciaDonde) * 0.5f + Vector3.up * 1.4f;
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
