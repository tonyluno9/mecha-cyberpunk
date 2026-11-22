using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T12 y T15): corre una escena del barrio. Normal: publica las líneas con subtítulo (y voz si hay). Bullying: los pandilleros (puestos en
    /// <c>Acosando</c> para que su IA no los mueva) se acercan al civil, lo gritan, empujan, tiran o le quitan algo según el guion; si el jugador golpea,
    /// dispara o saca un arma cerca, dejan al civil y van contra el jugador; si no, se ríen y regresan a su lugar. Lo arranca <see cref="DirectorEscenasBarrio"/>.
    /// </summary>
    public sealed class EjecutorEscenaBarrio
    {
        public sealed class Reparto
        {
            public PandilleroAPie Agresor1;
            public PandilleroAPie Agresor2;
            public ActorCivilBarrio Victima;
            public ActorCivilBarrio Persona1;
            public ActorCivilBarrio Persona2;
            public Vector3 Centro;
        }

        // Medidas (m, s, m/s) que no se balancean: es la coreografía.
        private const float AlturaBoca = 1.7f;
        private const float DistanciaAbordaje = 1.8f;
        private const float DistanciaAbordajeSegundo = 3f; // el segundo se queda detrás del primero: sus colisionadores se estorban
        private const float VelocidadAcercarse = 2.8f;
        private const float VelocidadRegreso = 1.8f;
        private const float GradosGiroAgresor = 360f;
        private const float SegundosMaximosAcercarse = 25f; // la víctima puede estar hasta a 40 m (hay pocos civiles)
        private const float SegundosMaximosRegreso = 25f; // hasta 40 m a 1.8 m/s
        private const float DistanciaLlegadaHogar = 1.5f;
        private const float SegundosGesto = 1f;
        private const float SegundosMaximosCaido = 5f;
        private const float SegundosAlLevantar = 1.7f;
        private const float SegundosRisaFinal = 1.6f;
        private const float RadioIntervencion = 30f;
        private const float RadioArmaIntervencion = 15f;
        private const float DistanciaJugadorHostil = 60f;
        private const string GritoAlerta = "¡Órale, tú!";
        private static readonly Color ColorSkull = new Color(1f, 0.25f, 0.85f);
        private static readonly Color ColorSnake = new Color(0.65f, 1f, 0.15f);
        private static readonly Color ColorVictima = new Color(0.4f, 0.9f, 1f);
        private static readonly Color ColorVecino = new Color(1f, 0.8f, 0.2f);

        private readonly EscenaBarrio escena;
        private readonly Reparto reparto;
        private bool limpio;
        private bool intervencion;
        private bool abortar;
        private float caidoDesde = -1f;
        private Vector3 ultimaPosicion;

        public EjecutorEscenaBarrio(EscenaBarrio escena, Reparto reparto)
        {
            this.escena = escena;
            this.reparto = reparto;
            ultimaPosicion = reparto.Centro;
        }

        public EscenaBarrio Escena => escena;
        public bool EsBullying => escena.Tipo == TipoEscenaBarrio.Bullying;
        /// <summary>Cómo terminó: "completa", "jugador" (intervino) o "abortada".</summary>
        public string Motivo { get; private set; } = "completa";

        private bool Terminada => intervencion || abortar;

        /// <summary>Pide cortar la escena sin enojar a nadie (misión protegida, cinemática, otra escena forzada).</summary>
        public void Abortar()
        {
            abortar = true;
        }

        public IEnumerator Correr()
        {
            Suscribir();
            if (EsBullying)
            {
                PrepararBullying();
                yield return Acercarse();
            }
            LineaBarrio[] lineas = escena.Lineas;
            for (int i = 0; i < lineas.Length && !Terminada; i++)
            {
                yield return Esperar(lineas[i].Retraso);
                if (Terminada)
                {
                    break;
                }
                yield return DecirLinea(lineas[i]);
            }
            if (EsBullying && !Terminada)
            {
                yield return Cierre();
            }
            Limpiar();
        }

        // ---------- preparación ----------

        private void PrepararBullying()
        {
            if (reparto.Victima != null)
            {
                reparto.Victima.Sujetar();
            }
            Poner(reparto.Agresor1);
            Poner(reparto.Agresor2);
        }

        private static void Poner(PandilleroAPie p)
        {
            if (p != null)
            {
                // T-1633: si es de un GrupoAcoso (los SKULLS fijos de la ciudad) el grupo lo presta: deja de moverlo y mirar a su civil.
                p.GetComponentInParent<GrupoAcoso>()?.Prestar(p);
                p.Acosando = true;
            }
        }

        private void Suscribir()
        {
            if (reparto.Agresor1 != null)
            {
                reparto.Agresor1.DanioRecibido += AlDanio;
            }
            if (reparto.Agresor2 != null)
            {
                reparto.Agresor2.DanioRecibido += AlDanio;
            }
            if (EsBullying)
            {
                EventosRuido.Disparo += AlDisparo;
                EventosRuido.ArmaSacada += AlArma;
            }
        }

        private void AlDanio(PandilleroAPie quien, InfoDanio info)
        {
            if (info.Cantidad > 0f)
            {
                intervencion = true;
            }
        }

        private void AlDisparo(Vector3 punto)
        {
            if (Cerca(punto, RadioIntervencion))
            {
                intervencion = true;
            }
        }

        private void AlArma(Vector3 punto)
        {
            if (Cerca(punto, RadioArmaIntervencion))
            {
                intervencion = true;
            }
        }

        private bool Cerca(Vector3 punto, float radio)
        {
            Transform t = reparto.Victima != null && reparto.Victima.Transform != null ? reparto.Victima.Transform : (reparto.Agresor1 != null ? reparto.Agresor1.transform : null);
            return t != null && (t.position - punto).sqrMagnitude <= radio * radio;
        }

        // ---------- esperas y mantenimiento ----------

        private IEnumerator Esperar(float segundos)
        {
            float t = 0f;
            while (t < segundos && !Terminada)
            {
                Mantener(Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Cada cuadro: se corta si cambió el entorno, los agresores siguen mirando a la víctima y la víctima al agresor.
        private void Mantener(float dt)
        {
            if (MisionProtegida.Activa || CinematicaEnCurso.Activa || !ActoresVigentes())
            {
                abortar = true;
                return;
            }
            if (!EsBullying)
            {
                return;
            }
            Vector3 centroVictima = reparto.Victima.Transform.position;
            Atender(reparto.Agresor1, centroVictima, dt);
            Atender(reparto.Agresor2, centroVictima, dt);
            reparto.Victima.Mirar(reparto.Agresor1.transform.position, dt);
            if (reparto.Victima.Caido)
            {
                caidoDesde = caidoDesde < 0f ? Time.time : caidoDesde;
                if (Time.time - caidoDesde > SegundosMaximosCaido)
                {
                    reparto.Victima.Levantar();
                    caidoDesde = -1f;
                }
            }
            else
            {
                caidoDesde = -1f;
            }
        }

        private static void Atender(PandilleroAPie p, Vector3 mirarA, float dt)
        {
            if (p == null)
            {
                return;
            }
            p.MoverPlano(Vector3.zero);
            p.MirarHacia(mirarA - p.transform.position, GradosGiroAgresor);
        }

        private bool ActoresVigentes()
        {
            if (EsBullying)
            {
                return Vigente(reparto.Agresor1) && (reparto.Agresor2 == null || Vigente(reparto.Agresor2)) && reparto.Victima != null && reparto.Victima.Vivo;
            }
            return true;
        }

        private static bool Vigente(PandilleroAPie p) => p != null && p.isActiveAndEnabled && !p.EstaKO && p.Acosando; // sin Acosando alguien lo soltó (su grupo pasó a alerta)

        // ---------- bullying: acercarse y cierre ----------

        private IEnumerator Acercarse()
        {
            float t = 0f;
            while (!Terminada)
            {
                if (MisionProtegida.Activa || CinematicaEnCurso.Activa || !ActoresVigentes())
                {
                    abortar = true;
                    break;
                }
                Vector3 meta = reparto.Victima.Transform.position;
                bool llegaron = true;
                float dt = Time.deltaTime;
                llegaron &= IrHacia(reparto.Agresor1, meta, dt, DistanciaAbordaje);
                llegaron &= IrHacia(reparto.Agresor2, meta, dt, DistanciaAbordajeSegundo);
                reparto.Victima.Mirar(reparto.Agresor1.transform.position, dt);
                if (llegaron)
                {
                    yield break;
                }
                t += dt;
                if (t > SegundosMaximosAcercarse)
                {
                    abortar = true;
                    Debug.Log("[Barrio] escena " + escena.Id + ": los agresores no llegaron a la víctima, se cancela.");
                    yield break;
                }
                yield return null;
            }
        }

        // Camina hacia el punto y para a DistanciaAbordaje. True si ya llegó (o no existe).
        private static bool IrHacia(PandilleroAPie p, Vector3 meta, float dt, float distancia)
        {
            if (p == null)
            {
                return true;
            }
            Vector3 d = meta - p.transform.position;
            d.y = 0f;
            if (d.magnitude <= distancia)
            {
                Atender(p, meta, dt);
                return true;
            }
            p.MirarHacia(d, GradosGiroAgresor);
            p.MoverPlano(d.normalized * VelocidadAcercarse);
            return false;
        }

        private IEnumerator Cierre()
        {
            Vector3 donde = reparto.Victima.Transform.position;
            if (escena.RieAlFinal)
            {
                if (reparto.Agresor1 != null)
                {
                    VocesPandilla.Decir(reparto.Agresor1.transform, VocesPandilla.RisaBurla, true);
                    reparto.Agresor1.Visual?.Burlar();
                }
                if (reparto.Agresor2 != null && reparto.Agresor2 != reparto.Agresor1)
                {
                    reparto.Agresor2.Visual?.Burlar();
                }
                yield return Esperar(SegundosRisaFinal);
            }
            if (Terminada)
            {
                yield break;
            }
            if (reparto.Victima.Caido)
            {
                reparto.Victima.Levantar();
                yield return Esperar(SegundosAlLevantar);
            }
            if (Terminada)
            {
                yield break;
            }
            // La víctima huye (se le devuelve el paseo con un susto) y los agresores vuelven a su lugar.
            reparto.Victima.Soltar(reparto.Agresor1.transform.position);
            yield return Regresar(reparto.Agresor1, reparto.Agresor2);
            ultimaPosicion = donde;
        }

        private IEnumerator Regresar(PandilleroAPie a, PandilleroAPie b)
        {
            float t = 0f;
            while (t < SegundosMaximosRegreso && !intervencion && !abortar)
            {
                bool listos = Volver(a, Time.deltaTime);
                listos &= Volver(b, Time.deltaTime);
                if (listos)
                {
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        private static bool Volver(PandilleroAPie p, float dt)
        {
            if (p == null || !p.isActiveAndEnabled)
            {
                return true;
            }
            Vector3 d = p.Hogar - p.transform.position;
            d.y = 0f;
            if (d.magnitude <= DistanciaLlegadaHogar)
            {
                p.MoverPlano(Vector3.zero);
                return true;
            }
            p.MirarHacia(d, GradosGiroAgresor);
            p.MoverPlano(d.normalized * VelocidadRegreso);
            return false;
        }

        // ---------- una línea ----------

        private IEnumerator DecirLinea(LineaBarrio l)
        {
            Transform quien = Resolver(l.Quien, out PandilleroAPie pandillero);
            float clip = l.Audio != null ? l.Audio.length : 0f;
            float duracion = ReglaEscenasBarrio.DuracionLinea(l.Duracion, clip, l.Texto);
            Vector3 boca = quien != null ? quien.position + Vector3.up * AlturaBoca : ultimaPosicion;
            if (quien != null)
            {
                ultimaPosicion = quien.position;
            }
            if (!string.IsNullOrEmpty(l.Texto))
            {
                Subtitulos.Publicar(l.Hablante, l.Texto, boca, duracion, ColorDe(l));
            }
            if (l.Audio != null)
            {
                AudioSource.PlayClipAtPoint(l.Audio, boca, Mathf.Clamp01(Sonidos.Volumen(SeccionSonido.Voces)));
            }
            else if (!string.IsNullOrEmpty(l.IdVozSku) && pandillero != null)
            {
                VocesPandilla.Decir(pandillero.transform, l.IdVozSku, true);
            }
            Gesticular(l, quien, pandillero);
            float espera = Mathf.Max(duracion, l.Gesto != GestoBarrio.Ninguno ? SegundosGesto : 0f);
            yield return Esperar(espera);
        }

        private Transform Resolver(RolBarrio rol, out PandilleroAPie pandillero)
        {
            pandillero = null;
            switch (rol)
            {
                case RolBarrio.Agresor1:
                    pandillero = reparto.Agresor1;
                    return pandillero != null ? pandillero.transform : null;
                case RolBarrio.Agresor2:
                    pandillero = reparto.Agresor2 != null ? reparto.Agresor2 : reparto.Agresor1;
                    return pandillero != null ? pandillero.transform : null;
                case RolBarrio.Victima:
                    return reparto.Victima != null ? reparto.Victima.Transform : null;
                case RolBarrio.Persona2:
                    return reparto.Persona2 != null && reparto.Persona2.Transform != null ? reparto.Persona2.Transform : (reparto.Persona1 != null ? reparto.Persona1.Transform : null);
                default:
                    return reparto.Persona1 != null ? reparto.Persona1.Transform : null;
            }
        }

        private static Color ColorDe(LineaBarrio l)
        {
            if (l.Quien == RolBarrio.Agresor1 || l.Quien == RolBarrio.Agresor2)
            {
                return l.Hablante.StartsWith("SNAKE") ? ColorSnake : ColorSkull;
            }
            return l.Quien == RolBarrio.Victima ? ColorVictima : ColorVecino;
        }

        private void Gesticular(LineaBarrio l, Transform quien, PandilleroAPie pandillero)
        {
            PandilleroAPie agresor = pandillero != null ? pandillero : reparto.Agresor1;
            Vector3 haciaVictima = reparto.Victima != null && agresor != null && reparto.Victima.Transform != null
                ? reparto.Victima.Transform.position - agresor.transform.position : Vector3.forward;
            switch (l.Gesto)
            {
                case GestoBarrio.Burla:
                    VisualDe(quien, pandillero)?.Burlar();
                    break;
                case GestoBarrio.Telefono:
                    VisualDe(quien, pandillero)?.Telefonear();
                    break;
                case GestoBarrio.Empujar:
                    agresor?.Visual?.Golpear(false);
                    reparto.Victima?.Empujar(haciaVictima);
                    break;
                case GestoBarrio.Patear:
                    agresor?.Visual?.Golpear(true);
                    break;
                case GestoBarrio.Quitar:
                    agresor?.Visual?.Gestear(GestoVisual.Recoger);
                    reparto.Victima?.Suplicar();
                    break;
                case GestoBarrio.Tumbar:
                    reparto.Victima?.Tumbar(haciaVictima);
                    break;
                case GestoBarrio.Suplicar:
                    reparto.Victima?.Suplicar();
                    break;
                case GestoBarrio.Levantar:
                    reparto.Victima?.Levantar();
                    break;
            }
        }

        private static VisualPersonaje VisualDe(Transform quien, PandilleroAPie pandillero)
        {
            if (pandillero != null)
            {
                return pandillero.Visual;
            }
            return quien != null ? quien.GetComponentInChildren<VisualPersonaje>(false) : null;
        }

        // ---------- final ----------

        /// <summary>Suelta a todos. Idempotente. Con intervención del jugador los pandilleros van contra él; si no, vuelven a su vida normal.</summary>
        public void Limpiar()
        {
            if (limpio)
            {
                return;
            }
            limpio = true;
            Motivo = intervencion ? "jugador" : (abortar ? "abortada" : "completa");
            EventosRuido.Disparo -= AlDisparo;
            EventosRuido.ArmaSacada -= AlArma;
            Soltar(reparto.Agresor1);
            Soltar(reparto.Agresor2);
            if (reparto.Victima != null)
            {
                Vector3 desde = reparto.Agresor1 != null ? reparto.Agresor1.transform.position : reparto.Centro;
                IJugador jugador = Registro<IJugador>.Actual;
                if (intervencion && jugador != null && jugador.Raiz != null)
                {
                    desde = jugador.Raiz.position;
                }
                reparto.Victima.Soltar(desde);
            }
        }

        private void Soltar(PandilleroAPie p)
        {
            if (p == null)
            {
                return;
            }
            p.DanioRecibido -= AlDanio;
            GrupoAcoso prestador = p.GetComponentInParent<GrupoAcoso>();
            bool eraPrestado = prestador != null && prestador.EsPrestado(p);
            if (eraPrestado)
            {
                prestador.Devolver(p, intervencion); // sin intervención vuelve al acoso (Acosando sigue true); con ella su grupo pasa a alerta
            }
            if (!eraPrestado || intervencion)
            {
                p.Acosando = false;
            }
            if (!intervencion || p.EstaKO || !p.isActiveAndEnabled)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null || (jugador.Raiz.position - p.transform.position).sqrMagnitude > DistanciaJugadorHostil * DistanciaJugadorHostil)
            {
                return;
            }
            p.Alerta = true;
            p.VolverseHostil();
            p.Grupo?.Avisar(p, GritoAlerta, true);
        }
    }
}
