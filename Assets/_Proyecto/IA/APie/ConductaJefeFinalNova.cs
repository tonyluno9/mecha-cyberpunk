using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1110: conducta del jefe final de Nova (E14). Lleva el turno del <see cref="PandilleroAPie"/> y traduce <see cref="ReglaJefeFinalNova"/> a movimiento,
    /// aros en el piso (cañonazos y onda de choque) y daño. Solo pelea mientras el recinto lo activa (<see cref="Activar"/>); si no, espera en su punto.
    /// Los gritos "¡Escudo activado!" y "¡Sobrecarga!" marcan las fases; el brillo del aviso es rojo en Asedio, cian en Escudo y violeta en Furia.
    /// Al vencerlo registra el jefe vencido de Nova (<see cref="IdPandilla.Nova"/>, como <see cref="JefePandilla"/>) y avisa con <see cref="Derrotado"/>.
    /// </summary>
    [RequireComponent(typeof(EscudoEnergia))]
    [DefaultExecutionOrder(-100)]
    public class ConductaJefeFinalNova : MonoBehaviour, IConductaAPie
    {
        private const float GradosGiroCamina = 270f;
        private const float GradosGiroAtaque = 160f;
        private const float DistanciaParada = 1.8f;
        private const float DistanciaCasa = 0.8f;
        private const float DistanciaDelanteAro = 3f;
        private const float VelocidadMinimaJugador = 0.5f;
        private const float AlturaAro = 0.08f;
        private const int PuntosAro = 48;
        private const float AnchoAro = 0.16f;
        private const float SegundosDestello = 0.25f;
        private const string GritoEscudo = "¡Escudo activado!";
        private const string GritoSobrecarga = "¡Sobrecarga!";
        private const string GritoFinal = "Nova... no cae... así.";
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly Color ColorAsedio = new Color(1f, 0.2f, 0.1f);
        private static readonly Color ColorEscudo = new Color(0.2f, 0.9f, 1f);
        private static readonly Color ColorFuria = new Color(0.7f, 0.2f, 1f);
        private static readonly Color ColorOnda = new Color(0.3f, 1f, 1f);

        [Tooltip("Material transparente emisivo aditivo de los aros del piso (cañonazos y onda).")]
        [SerializeField] private Material materialAro;

        private sealed class Aro
        {
            public Vector3 Centro;
            public float Aparece;
            public float Destello = -1f;
            public LineRenderer Exterior;
            public LineRenderer Interior;
        }

        private PandilleroAPie pandillero;
        private EscudoEnergia escudo;
        private ReglaJefeFinalNova regla;
        private readonly List<Aro> aros = new List<Aro>();
        private readonly List<Aro> destellos = new List<Aro>();
        private LineRenderer onda;
        private bool ondaGolpeo;
        private bool activo;
        private bool vencidoRegistrado;
        private Vector3 posicionJugadorAnterior;
        private Vector3 velocidadJugador;
        private bool tengoPosicion;
        private MaterialPropertyBlock bloque;
        private bool agachado;
        private string gritoPendiente;
        private float limiteGritoPendiente;
        private const float SegundosGritoPendiente = 6f;

        public bool Controla => true;
        public ReglaJefeFinalNova Regla => regla;
        public EscudoEnergia Escudo => escudo;
        /// <summary>True mientras el recinto tiene la pelea cerrada.</summary>
        public bool Activo => activo;
        /// <summary>Aros de cañonazo que esperan explotar.</summary>
        public int ArosPendientes => aros.Count;
        public bool OndaVisible => onda != null && onda.gameObject.activeSelf;
        public bool Vencido => vencidoRegistrado;

        /// <summary>El jefe cayó (una sola vez).</summary>
        public event Action Derrotado;

        private void Awake()
        {
            pandillero = GetComponent<PandilleroAPie>();
            escudo = GetComponent<EscudoEnergia>();
            if (pandillero == null || escudo == null)
            {
                Debug.LogError("[IA] Faltan PandilleroAPie o EscudoEnergia en ConductaJefeFinalNova de " + name, this);
                enabled = false;
                return;
            }
            bloque = new MaterialPropertyBlock();
            regla = new ReglaJefeFinalNova(ReglaJefeFinalNova.Parametros.Diseno);
            pandillero.MarcarComoJefe();
            pandillero.Vencido += AlVencer;
        }

        private void OnEnable()
        {
            if (escudo != null)
            {
                escudo.Roto += AlRomperseElEscudo;
            }
        }

        private void OnDisable()
        {
            if (escudo != null)
            {
                escudo.Roto -= AlRomperseElEscudo;
            }
        }

        private void OnDestroy()
        {
            if (pandillero != null)
            {
                pandillero.Vencido -= AlVencer;
            }
            LimpiarAros();
            if (onda != null)
            {
                Destroy(onda.gameObject);
            }
        }

        /// <summary>Empieza la pelea (el recinto se cerró).</summary>
        public void Activar()
        {
            if (!activo && pandillero != null)
            {
                VocesFaccion.Decir(FaccionVoz.JefeNova, pandillero.transform, EventoVoz.InicioCombate, true); // T-voces-23nov: su frase al empezar
            }
            activo = true;
        }

        /// <summary>Termina la pelea: el jefe espera en su punto.</summary>
        public void Desactivar()
        {
            activo = false;
        }

        /// <summary>Deja todo como al inicio: vida llena, en su punto, escudo y fase de Asedio.</summary>
        public void Reiniciar()
        {
            activo = false;
            vencidoRegistrado = false;
            regla?.Reiniciar();
            LimpiarAros();
            OcultarOnda();
            if (pandillero != null)
            {
                pandillero.SetCastigable(false);
                pandillero.Reaparecer();
            }
        }

        private void AlRomperseElEscudo()
        {
            LimpiarAros();
            OcultarOnda();
            Gritar(GritoSobrecarga);
            if (pandillero != null)
            {
                VocesFaccion.Decir(FaccionVoz.JefeNova, pandillero.transform, EventoVoz.Escudo, true); // T-voces-23nov: otra frase al romperse su escudo
            }
        }

        private void AlVencer(PandilleroAPie vencido)
        {
            if (vencidoRegistrado)
            {
                return;
            }
            vencidoRegistrado = true;
            activo = false;
            LimpiarAros();
            OcultarOnda();
            Registro<ITerritorio>.Actual?.RegistrarVencido(vencido.transform.position, IdPandilla.Nova, true);
            Gritar(GritoFinal);
            VocesFaccion.Decir(FaccionVoz.JefeNova, vencido.transform, EventoVoz.Muerte, true); // T-voces-23nov
            Debug.Log("[IA] Jefe final de Nova vencido.");
            Derrotado?.Invoke();
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (regla == null)
            {
                return;
            }
            Transform jugador = p.Jugador;
            bool atacable = jugador != null && p.JugadorAtacable;
            ReintentarGrito();
            Vector3 haciaJugador = atacable ? jugador.position - p.transform.position : Vector3.zero;
            haciaJugador.y = 0f;
            float distancia = atacable ? haciaJugador.magnitude : float.MaxValue;
            ActualizarVelocidadJugador(atacable ? jugador : null, dt);
            bool enPelea = activo && atacable;
            ReglaJefeFinalNova.Salida salida = regla.Avanzar(dt, new ReglaJefeFinalNova.Entrada { Distancia = distancia, VidaFraccion = p.VidaFraccion, EnPelea = enPelea });
            if (salida.EscudoActivado)
            {
                Gritar(GritoEscudo);
            }
            p.SetCastigable(regla.Castigable);
            if (!enPelea)
            {
                LimpiarAros();
                OcultarOnda();
                Agachar(p, false);
                p.MostrarAviso(0f, ColorAsedio);
                VolverACasa(p);
                return;
            }

            Vector3 dir = distancia > 0.0001f ? haciaJugador / distancia : p.transform.forward;
            Color color = ColorDeFase();
            switch (regla.Actual)
            {
                case ReglaJefeFinalNova.Accion.Camina:
                    Agachar(p, false);
                    p.MostrarAviso(0f, color);
                    p.MirarHacia(dir, GradosGiroCamina);
                    Vector3 frente = p.transform.forward;
                    frente.y = 0f;
                    p.MoverPlano(distancia > DistanciaParada ? frente.normalized * (p.Datos.Velocidad * regla.FactorVelocidad) : Vector3.zero);
                    break;
                case ReglaJefeFinalNova.Accion.AvisoPunetazo:
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(dir, GradosGiroAtaque);
                    p.MostrarAviso(regla.ProgresoAviso, color);
                    break;
                case ReglaJefeFinalNova.Accion.AvisoCanon:
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(dir, GradosGiroAtaque);
                    p.MostrarAviso(regla.ProgresoAviso, color);
                    break;
                case ReglaJefeFinalNova.Accion.AvisoOnda:
                    Agachar(p, true);
                    p.MoverPlano(Vector3.zero);
                    p.MostrarAviso(regla.ProgresoAviso, ColorEscudo);
                    break;
                case ReglaJefeFinalNova.Accion.Onda:
                    Agachar(p, false);
                    p.MoverPlano(Vector3.zero);
                    p.MostrarAviso(0f, color);
                    break;
                default: // Recupera, Aturdido
                    Agachar(p, false);
                    p.MoverPlano(Vector3.zero);
                    p.MostrarAviso(0f, color);
                    break;
            }

            if (salida.ImpactoPunetazo)
            {
                Punetazo(p, dir, distancia);
            }
            if (salida.NuevosAros != null)
            {
                foreach (ReglaJefeFinalNova.LugarAro lugar in salida.NuevosAros)
                {
                    CrearAro(PosicionAro(lugar, jugador, dir));
                }
            }
            for (int i = 0; i < salida.Explotan; i++)
            {
                ExplotarAro(p, jugador);
            }
            if (salida.EmpezoOnda)
            {
                ondaGolpeo = false;
                MostrarOnda(p.transform.position);
            }
            ActualizarOnda(p, jugador, distancia, dir);
            ActualizarAros();
        }

        // Un grito importante no se pierde por el enfriamiento del pandillero: si no salió, se reintenta unos segundos.
        private void Gritar(string texto)
        {
            pandillero.Gritar(texto);
            gritoPendiente = pandillero.GritoActual == texto ? null : texto;
            limiteGritoPendiente = Time.time + SegundosGritoPendiente;
        }

        private void ReintentarGrito()
        {
            if (gritoPendiente == null)
            {
                return;
            }
            if (Time.time > limiteGritoPendiente)
            {
                gritoPendiente = null;
                return;
            }
            pandillero.Gritar(gritoPendiente);
            if (pandillero.GritoActual == gritoPendiente)
            {
                gritoPendiente = null;
            }
        }

        // ------------------------------------------------------------------ daño

        private void Punetazo(PandilleroAPie p, Vector3 haciaJugador, float distancia)
        {
            ReglaJefeFinalNova.Parametros c = regla.Config;
            p.AnimarGolpe(true);
            VocesFaccion.Decir(FaccionVoz.JefeNova, p.transform, EventoVoz.Ataque); // T-voces-23nov: el canal de una voz evita que grite en cada golpe
            if (distancia > c.RadioPunetazo)
            {
                return;
            }
            Vector3 frente = p.transform.forward;
            frente.y = 0f;
            if (Vector3.Angle(frente, haciaJugador) <= c.ArcoPunetazo * 0.5f)
            {
                p.DaniarJugador(c.DanioPunetazo, haciaJugador);
            }
        }

        private void ExplotarAro(PandilleroAPie p, Transform jugador)
        {
            if (aros.Count == 0)
            {
                return;
            }
            Aro aro = aros[0];
            aros.RemoveAt(0);
            p.AnimarGolpe(true);
            if (jugador != null && p.JugadorAtacable)
            {
                Vector3 d = jugador.position - aro.Centro;
                d.y = 0f;
                if (d.magnitude <= regla.Config.RadioCanon)
                {
                    p.DaniarJugador(regla.Config.DanioCanon, d.sqrMagnitude > 0.0001f ? d.normalized : p.transform.forward);
                }
            }
            aro.Destello = Time.time + SegundosDestello;
            Colorear(aro.Exterior, Color.white);
            Colorear(aro.Interior, Color.white);
            DibujarCirculo(aro.Exterior, aro.Centro, regla.Config.RadioCanon);
            DibujarCirculo(aro.Interior, aro.Centro, regla.Config.RadioCanon);
            destellos.Add(aro);
        }

        private void ActualizarOnda(PandilleroAPie p, Transform jugador, float distancia, Vector3 dir)
        {
            if (regla.Actual != ReglaJefeFinalNova.Accion.Onda)
            {
                OcultarOnda();
                return;
            }
            float radio = regla.RadioOnda;
            DibujarCirculo(onda, p.transform.position, radio);
            if (!ondaGolpeo && jugador != null && p.JugadorAtacable)
            {
                Vector3 d = jugador.position - p.transform.position;
                d.y = 0f;
                if (Mathf.Abs(d.magnitude - radio) <= regla.Config.BandaOnda)
                {
                    ondaGolpeo = true;
                    p.DaniarJugador(regla.Config.DanioOnda, dir);
                }
            }
        }

        // ------------------------------------------------------------------ visuales

        private Vector3 PosicionAro(ReglaJefeFinalNova.LugarAro lugar, Transform jugador, Vector3 haciaJugador)
        {
            Vector3 centro = jugador != null ? jugador.position : transform.position;
            Vector3 mov = velocidadJugador.magnitude > VelocidadMinimaJugador ? velocidadJugador.normalized : haciaJugador;
            switch (lugar)
            {
                case ReglaJefeFinalNova.LugarAro.DelanteDelJugador: return centro + mov * DistanciaDelanteAro;
                case ReglaJefeFinalNova.LugarAro.DetrasDelJugador: return centro - mov * DistanciaDelanteAro;
                default: return centro;
            }
        }

        private void CrearAro(Vector3 centro)
        {
            Aro aro = new Aro { Centro = centro, Aparece = Time.time };
            aro.Exterior = CrearLinea("AroCanon");
            aro.Interior = CrearLinea("AroCanonInterior");
            Colorear(aro.Exterior, ColorAsedio);
            Colorear(aro.Interior, ColorAsedio);
            DibujarCirculo(aro.Exterior, centro, regla.Config.RadioCanon);
            DibujarCirculo(aro.Interior, centro, 0.05f);
            aros.Add(aro);
        }

        private void ActualizarAros()
        {
            float duracion = regla.DuracionAvisoCanon;
            foreach (Aro aro in aros)
            {
                float progreso = Mathf.Clamp01((Time.time - aro.Aparece) / duracion);
                DibujarCirculo(aro.Interior, aro.Centro, Mathf.Max(0.05f, regla.Config.RadioCanon * progreso));
            }
            for (int i = destellos.Count - 1; i >= 0; i--)
            {
                if (Time.time >= destellos[i].Destello)
                {
                    DestruirAro(destellos[i]);
                    destellos.RemoveAt(i);
                }
            }
        }

        private void LimpiarAros()
        {
            foreach (Aro a in aros)
            {
                DestruirAro(a);
            }
            aros.Clear();
            foreach (Aro a in destellos)
            {
                DestruirAro(a);
            }
            destellos.Clear();
        }

        private static void DestruirAro(Aro a)
        {
            if (a.Exterior != null)
            {
                Destroy(a.Exterior.gameObject);
            }
            if (a.Interior != null)
            {
                Destroy(a.Interior.gameObject);
            }
        }

        private void MostrarOnda(Vector3 centro)
        {
            if (onda == null)
            {
                onda = CrearLinea("OndaDeChoque");
                Colorear(onda, ColorOnda);
            }
            onda.gameObject.SetActive(true);
            DibujarCirculo(onda, centro, 0.05f);
        }

        private void OcultarOnda()
        {
            if (onda != null && onda.gameObject.activeSelf)
            {
                onda.gameObject.SetActive(false);
            }
        }

        private LineRenderer CrearLinea(string nombre)
        {
            GameObject go = new GameObject(nombre);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.loop = true;
            l.useWorldSpace = true;
            l.positionCount = PuntosAro;
            l.widthMultiplier = AnchoAro;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            if (materialAro != null)
            {
                l.sharedMaterial = materialAro;
            }
            return l;
        }

        private void Colorear(LineRenderer l, Color color)
        {
            if (l == null)
            {
                return;
            }
            l.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            l.SetPropertyBlock(bloque);
        }

        private static void DibujarCirculo(LineRenderer l, Vector3 centro, float radio)
        {
            if (l == null)
            {
                return;
            }
            for (int i = 0; i < PuntosAro; i++)
            {
                float a = i * Mathf.PI * 2f / PuntosAro;
                l.SetPosition(i, new Vector3(centro.x + Mathf.Cos(a) * radio, centro.y + AlturaAro, centro.z + Mathf.Sin(a) * radio));
            }
        }

        private Color ColorDeFase()
        {
            switch (regla.Fase)
            {
                case ReglaJefeFinalNova.FaseJefe.Escudo: return ColorEscudo;
                case ReglaJefeFinalNova.FaseJefe.Furia: return ColorFuria;
                default: return ColorAsedio;
            }
        }

        // ------------------------------------------------------------------ movimiento

        private void ActualizarVelocidadJugador(Transform jugador, float dt)
        {
            if (jugador == null || dt <= 0f)
            {
                tengoPosicion = false;
                velocidadJugador = Vector3.zero;
                return;
            }
            if (tengoPosicion)
            {
                Vector3 v = (jugador.position - posicionJugadorAnterior) / dt;
                v.y = 0f;
                velocidadJugador = Vector3.Lerp(velocidadJugador, v, 0.5f);
            }
            posicionJugadorAnterior = jugador.position;
            tengoPosicion = true;
        }

        private void Agachar(PandilleroAPie p, bool valor)
        {
            if (agachado == valor || p.Visual == null)
            {
                return;
            }
            agachado = valor;
            p.Visual.Agachado = valor;
        }

        private void VolverACasa(PandilleroAPie p)
        {
            Vector3 aCasa = p.Hogar - p.transform.position;
            aCasa.y = 0f;
            if (aCasa.magnitude > DistanciaCasa)
            {
                p.MirarHacia(aCasa, GradosGiroCamina);
                p.MoverPlano(aCasa.normalized * p.Datos.Velocidad);
            }
            else
            {
                p.MoverPlano(Vector3.zero);
            }
        }
    }
}
