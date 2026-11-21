using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1553: conducta del elite de Nova (E18), sobre <see cref="ReglaEliteNova"/>. Sin arma (decisión de Tony 20-nov): vuela con el jetpack hacia el jugador
    /// (E18_Jetpack_Volar, en bucle), aterriza cerca (E18_Caida_Aterrizaje) y pelea a golpes con los puños cerrados (E18_Golpe_Der, por el estado de golpe del Animator).
    /// Se detiene (MoverPlano cero) para aterrizar y para golpear. Los clips de vuelo y aterrizaje se juegan con <see cref="ReproductorClipUnico"/> encima del Animator
    /// (como <see cref="CaidaNova"/>); al despegar se quita cualquier postura de arma. Las distancias y tiempos salen de <see cref="ReglaEliteNova.Parametros.Diseno"/>
    /// y el alcance, el daño y la velocidad de los datos del E18. Se agrega al prefab del E18 con RobotsNovaT1553.Aplicar.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ConductaEliteNova : MonoBehaviour, IConductaAPie
    {
        private const float GradosGiro = 540f;
        private const float FactorPerdida = 1.6f;
        /// <summary>Su radio de detección es el de los datos (10 m) por este factor: ve al jugador a unos 30 m y vuela hacia él.</summary>
        public const float FactorDeteccion = 3f;
        private const float AlcanceExtraImpacto = 0.6f;
        private const float AnguloImpacto = 70f;
        private const float AlturaRayoSuelo = 0.5f;
        private const float DistanciaRayoSuelo = 40f;
        private const float FraccionVueloEnBajada = 0.5f;
        // Al bajar apunta bajo el piso para seguir empujando hasta tocarlo (isGrounded).
        private const float AlturaBajada = -1f;
        private const int MaxImpactosSuelo = 8;

        private PandilleroAPie elite;
        private CharacterController controlador;
        private ReglaEliteNova regla;
        private ReproductorClipUnico reproductor;
        private readonly RaycastHit[] impactosSuelo = new RaycastHit[MaxImpactosSuelo];
        private bool avisoSinClip;

        public bool Controla => true;
        public ReglaEliteNova Regla => regla;
        /// <summary>El reproductor de los clips de vuelo y aterrizaje (null antes de Awake).</summary>
        public ReproductorClipUnico Reproductor => reproductor;
        /// <summary>Cuántos puñetazos conectaron al jugador.</summary>
        public int GolpesConectados { get; private set; }

        private void Awake()
        {
            elite = GetComponent<PandilleroAPie>();
            controlador = GetComponent<CharacterController>();
            if (elite == null || elite.Datos == null || controlador == null)
            {
                Debug.LogError("[IA] Faltan PandilleroAPie o CharacterController en ConductaEliteNova de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaEliteNova(ReglaEliteNova.Parametros.Diseno);
            reproductor = GetComponent<ReproductorClipUnico>();
            if (reproductor == null)
            {
                reproductor = gameObject.AddComponent<ReproductorClipUnico>();
            }
        }

        private void OnEnable()
        {
            if (elite != null)
            {
                elite.DanioRecibido += AlRecibirDanio;
            }
        }

        private void OnDisable()
        {
            if (elite != null)
            {
                elite.DanioRecibido -= AlRecibirDanio;
            }
            if (reproductor != null)
            {
                reproductor.Detener();
            }
        }

        private void AlRecibirDanio(PandilleroAPie p, InfoDanio info)
        {
            regla?.Interrumpir(); // un golpe o un aterrizaje a medias se corta
        }

        private void LateUpdate()
        {
            // Muerto: el clip de vuelo no puede tapar la animación de muerte.
            if (elite != null && elite.EstaKO && reproductor != null && reproductor.Reproduciendo)
            {
                reproductor.Detener();
            }
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (regla == null)
            {
                return;
            }
            QuitarPostureDeArma(p);
            Transform jugador = p.Jugador;
            bool atacable = jugador != null && p.JugadorAtacable;
            Vector3 haciaJugador = atacable ? jugador.position - p.transform.position : Vector3.zero;
            haciaJugador.y = 0f;
            float distancia = atacable ? haciaJugador.magnitude : float.MaxValue;
            Vector3 dir = haciaJugador.sqrMagnitude > 0.0001f ? haciaJugador.normalized : p.transform.forward;
            // El elite ve más lejos que un guardia (despega desde lejos, ver FactorDeteccion); una vez peleando solo pierde al jugador si se aleja mucho más.
            float radio = p.Datos.RadioDeteccion * FactorDeteccion;
            float limite = regla.Actual == ReglaEliteNova.Fase.Patrulla ? radio : radio * FactorPerdida;
            bool ve = atacable && (p.Alerta || distancia <= limite);
            var entrada = new ReglaEliteNova.Entrada { Distancia = distancia, Ve = ve, EnPiso = controlador.isGrounded, AlcanceGolpe = p.Datos.AlcanceGolpe };
            ReglaEliteNova.Salida salida = regla.Avanzar(dt, entrada);
            ReglaEliteNova.Parametros c = regla.Config;

            if (salida.EmpezoVuelo)
            {
                JugarVuelo();
            }
            if (salida.EmpezoAterrizaje)
            {
                Jugar(ConfigClipsNovaNpc.Actual != null ? ConfigClipsNovaNpc.Actual.CaidaAterrizaje : null, false);
            }
            if (salida.EmpezoGolpe)
            {
                p.AnimarGolpe(false); // E18_Golpe_Der por el estado de golpe del controlador
            }

            switch (regla.Actual)
            {
                case ReglaEliteNova.Fase.Vuela:
                    Volar(p, dir, distancia, c.VelocidadVuelo, c.AlturaVuelo, c);
                    break;
                case ReglaEliteNova.Fase.Aterriza:
                    // Sigue acercándose despacio mientras baja; sin tocar el piso no hay MoverPlano (la gravedad es la bajada propia).
                    Volar(p, dir, distancia, c.VelocidadVuelo * FraccionVueloEnBajada, AlturaBajada, c);
                    break;
                case ReglaEliteNova.Fase.Aterrizaje:
                    p.MirarHacia(dir, GradosGiro);
                    p.MoverPlano(Vector3.zero);
                    break;
                case ReglaEliteNova.Fase.Persigue:
                    DetenerClipSiSobra();
                    p.MirarHacia(dir, GradosGiro);
                    bool llego = distancia <= p.Datos.AlcanceGolpe * FraccionAlcanceDetenerse;
                    p.MoverPlano(llego ? Vector3.zero : dir * p.Datos.Velocidad);
                    break;
                case ReglaEliteNova.Fase.AvisoGolpe:
                    p.MirarHacia(dir, GradosGiro);
                    p.MoverPlano(Vector3.zero);
                    break;
                case ReglaEliteNova.Fase.Recupera:
                    p.MoverPlano(Vector3.zero);
                    break;
                default: // Patrulla
                    DetenerClipSiSobra();
                    Vector3 paso = p.PasoDePatrulla();
                    if (paso.sqrMagnitude > 0.0001f)
                    {
                        p.MirarHacia(paso, GradosGiro);
                    }
                    p.MoverPlano(paso);
                    break;
            }

            if (salida.Impacto)
            {
                Golpear(p, dir, distancia);
            }
        }

        // Se detiene al llegar a esta fracción de su alcance de golpe (así el puño siempre llega).
        private const float FraccionAlcanceDetenerse = 0.85f;

        private static void QuitarPostureDeArma(PandilleroAPie p)
        {
            VisualPersonaje visual = p.Visual;
            if (visual == null)
            {
                return;
            }
            if (visual.TipoArma != TipoArmaVisual.Ninguna)
            {
                visual.TipoArma = TipoArmaVisual.Ninguna;
            }
            if (visual.Apuntando)
            {
                visual.Apuntando = false;
            }
        }

        private void JugarVuelo()
        {
            if (TryGetComponent(out CaidaNova _))
            {
                return; // llegando de la nave: la caída ya tiene el Animator
            }
            AnimationClip clip = ConfigClipsNovaNpc.Actual != null ? ConfigClipsNovaNpc.Actual.JetpackVolar : null;
            if (clip == null && !avisoSinClip)
            {
                avisoSinClip = true;
                Debug.LogWarning("[IA] ConductaEliteNova: falta el clip E18_Jetpack_Volar en ConfigClipsNovaNpc (corre RobotsNovaT1553.Aplicar); vuela sin animación.", this);
            }
            Jugar(clip, true);
        }

        private void Jugar(AnimationClip clip, bool repetir)
        {
            VisualPersonaje visual = elite.Visual;
            if (clip == null || visual == null || visual.Animador == null || TryGetComponent(out CaidaNova _))
            {
                return;
            }
            reproductor.Reproducir(visual.Animador, clip, 1f, repetir);
        }

        private void DetenerClipSiSobra()
        {
            if (reproductor.Reproduciendo)
            {
                reproductor.Detener();
            }
        }

        // Vuelo: horizontal hacia el jugador (se detiene cerca de él) y vertical hacia la altura pedida sobre el piso.
        private void Volar(PandilleroAPie p, Vector3 dir, float distancia, float velocidad, float alturaObjetivo, ReglaEliteNova.Parametros c)
        {
            p.MirarHacia(dir, GradosGiro);
            float altura = AlturaSobreElPiso(c.AlturaVuelo);
            float vy = ReglaEliteNova.VelocidadVerticalHacia(altura, alturaObjetivo, c.VelocidadVertical);
            bool cerca = distancia <= p.Datos.AlcanceGolpe * FraccionAlcanceDetenerse;
            Vector3 plano = cerca || distancia >= float.MaxValue ? Vector3.zero : dir * velocidad;
            controlador.Move((plano + Vector3.up * vy) * Time.deltaTime);
        }

        // Metros entre sus pies y el piso (rayo hacia abajo sin contar su propio cuerpo); si no hay piso, la altura de vuelo (no sigue subiendo).
        private float AlturaSobreElPiso(float porOmision)
        {
            Vector3 origen = transform.position + Vector3.up * AlturaRayoSuelo;
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, impactosSuelo, DistanciaRayoSuelo, ~0, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (impactosSuelo[i].collider.transform.root != transform.root && impactosSuelo[i].distance < menor)
                {
                    menor = impactosSuelo[i].distance;
                }
            }
            return menor < float.MaxValue ? menor - AlturaRayoSuelo : porOmision;
        }

        private void Golpear(PandilleroAPie p, Vector3 haciaJugador, float distancia)
        {
            Vector3 frente = p.transform.forward;
            frente.y = 0f;
            if (distancia > p.Datos.AlcanceGolpe + AlcanceExtraImpacto || Vector3.Angle(frente, haciaJugador) > AnguloImpacto * 0.5f)
            {
                return;
            }
            GolpesConectados++;
            p.DaniarJugador(p.Datos.DanioGolpe, haciaJugador);
        }
    }
}
