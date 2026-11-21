using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1533: los sonidos de un enemigo de Nova o un robot (guardia, élite, centinela, pesado, perro, jefe final): pasos, roce de armadura, servos, voz de alerta,
    /// chispas al daño, muerte y apagado, escudo al cubrirse, jetpack con fade al caer del cielo y su llegada. Lo agrega solo el <see cref="PandilleroAPie"/>
    /// cuando su modelo es de Nova (<see cref="Preparar"/>). Lo que se decide (qué ID, cada cuántos metros) vive en <see cref="ReglaSonidoEnemigo"/>.
    /// Sin variante de tono (el API de sonidos no lo permite para sonidos puntuales), los pesados reciclan los pasos de Nova sin pitch bajo.
    /// </summary>
    [DisallowMultipleComponent]
    public class SonidosEnemigoAPie : MonoBehaviour
    {
        private const float AlturaOido = 1f;
        private const float DtMinimo = 0.0001f;
        private const int PasosPorServos = 5;
        private const float SegundosMinimosCarga = 2f;

        private PandilleroAPie enemigo;
        private TipoSonoroEnemigo tipo;
        private readonly AcumuladorPasos pasos = new AcumuladorPasos();
        private CaidaNova caida;
        private ConductaPerroNova conductaPerro;
        private Vector3 anterior;
        private bool tieneAnterior;
        private bool enMovimiento;
        private bool alertaAntes;
        private bool agachadoAntes;
        private bool enPeleaAntes;
        private bool avisoPerroAntes;
        private bool aterrizoAntes;
        private bool llegadaRevisada;
        private float proximaAlerta;
        private float proximasChispas;
        private float proximoServos;
        private float apagarEn = -1f;
        private int bucleJetpack = Sonidos.SinBucle;
        private float nivelJetpack;
        private bool murio;

        /// <summary>El tipo de enemigo que suena.</summary>
        public TipoSonoroEnemigo Tipo => tipo;

        /// <summary>True si es un robot: el pandillero no debe soltar su voz humana (alerta, caída).</summary>
        public bool EsRobot => ReglaSonidoEnemigo.EsRobot(tipo);

        /// <summary>El nivel (0 a 1) del fade del jetpack ahora (para las pruebas).</summary>
        public float NivelJetpack => nivelJetpack;

        /// <summary>True mientras suena el bucle del jetpack (para las pruebas).</summary>
        public bool JetpackSonando => bucleJetpack != Sonidos.SinBucle;

        /// <summary>
        /// Pone este componente en <paramref name="pandillero"/> si su modelo es de Nova (según los nombres de su controlador, avatar y datos) o es un perro
        /// robot. Devuelve el componente, o null si no es de Nova.
        /// </summary>
        public static SonidosEnemigoAPie Preparar(PandilleroAPie pandillero)
        {
            if (pandillero == null || pandillero.Datos == null)
            {
                return null;
            }
            SonidosEnemigoAPie existente = pandillero.GetComponent<SonidosEnemigoAPie>();
            if (existente != null)
            {
                return existente;
            }
            TipoSonoroEnemigo t = ReglaSonidoEnemigo.Tipo(Nombres(pandillero), pandillero.Datos.PerroRobot);
            if (t == TipoSonoroEnemigo.Ninguno)
            {
                return null;
            }
            SonidosEnemigoAPie nuevo = pandillero.gameObject.AddComponent<SonidosEnemigoAPie>();
            nuevo.enemigo = pandillero;
            nuevo.tipo = t;
            return nuevo;
        }

        // Los nombres que delatan el modelo: el prefab se renombra al aparecer (Nova_Guardia_1), pero el controlador y el avatar conservan el del FBX (E15_..., E16_...).
        private static List<string> Nombres(PandilleroAPie pandillero)
        {
            List<string> nombres = new List<string> { pandillero.Datos.name };
            foreach (Animator animador in pandillero.GetComponentsInChildren<Animator>(true))
            {
                if (animador.runtimeAnimatorController != null)
                {
                    nombres.Add(animador.runtimeAnimatorController.name);
                }
                if (animador.avatar != null)
                {
                    nombres.Add(animador.avatar.name);
                }
                nombres.Add(animador.gameObject.name);
            }
            foreach (Renderer renderizador in pandillero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                nombres.Add(renderizador.name);
            }
            return nombres;
        }

        private void Awake()
        {
            if (enemigo == null)
            {
                enemigo = GetComponent<PandilleroAPie>();
            }
            if (enemigo == null)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (enemigo == null)
            {
                return;
            }
            enemigo.KO += AlLlegarAKO;
            enemigo.DanioRecibido += AlRecibirDanio;
            tieneAnterior = false;
        }

        private void OnDisable()
        {
            if (enemigo != null)
            {
                enemigo.KO -= AlLlegarAKO;
                enemigo.DanioRecibido -= AlRecibirDanio;
            }
            PararJetpack();
        }

        private Vector3 PuntoOido => transform.position + Vector3.up * AlturaOido;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (enemigo == null || dt < DtMinimo)
            {
                return;
            }
            if (enemigo.EstaKO)
            {
                if (apagarEn >= 0f && Time.time >= apagarEn)
                {
                    apagarEn = -1f;
                    string apagado = ReglaSonidoEnemigo.IdApagado(tipo);
                    if (!string.IsNullOrEmpty(apagado))
                    {
                        Sonidos.ReproducirEn(apagado, PuntoOido);
                    }
                }
                ActualizarJetpack(false, dt);
                return;
            }
            murio = false;
            Vector3 aqui = transform.position;
            float velocidad = 0f;
            if (tieneAnterior)
            {
                Vector3 delta = aqui - anterior;
                delta.y = 0f;
                velocidad = delta.magnitude / dt;
                AvanzarPasos(delta.magnitude, velocidad);
            }
            anterior = aqui;
            tieneAnterior = true;
            RevisarAlerta();
            RevisarEscudo();
            RevisarPerro();
            RevisarCaida(dt);
            RevisarLlegadaSinCaida();
        }

        private void AvanzarPasos(float metros, float velocidad)
        {
            bool mueve = ReglaSonidoEnemigo.EnMovimiento(velocidad) && metros < 3f;
            if (!mueve)
            {
                if (enMovimiento)
                {
                    pasos.Reiniciar();
                }
                enMovimiento = false;
                return;
            }
            if (!enMovimiento)
            {
                enMovimiento = true;
                AlArrancar();
            }
            bool corre = ReglaSonidoEnemigo.Corre(velocidad);
            string id = ReglaSonidoEnemigo.IdPaso(tipo, corre);
            if (string.IsNullOrEmpty(id) || !pasos.Avanzar(metros, ReglaSonidoEnemigo.MetrosPorPaso(tipo, corre)))
            {
                return;
            }
            Sonidos.ReproducirEn(id, transform.position);
            if (ReglaSonidoEnemigo.LlevaArmaduraNova(tipo) && pasos.Pasos % ReglaSonidoEnemigo.PasosPorRoce == 0)
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdArmaduraMover, PuntoOido);
            }
            if (ReglaSonidoEnemigo.EsRobot(tipo) && tipo != TipoSonoroEnemigo.Perro && pasos.Pasos % PasosPorServos == 0)
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdServos, PuntoOido);
            }
        }

        // Al echar a andar: servos (o la hidráulica del gigante), no más seguido que cada tanto.
        private void AlArrancar()
        {
            if (!ReglaSonidoEnemigo.EsRobot(tipo) || Time.time < proximoServos)
            {
                return;
            }
            proximoServos = Time.time + ReglaSonidoEnemigo.SegundosEntreServos;
            Sonidos.ReproducirEn(ReglaSonidoEnemigo.EsGigante(tipo) ? ReglaSonidoEnemigo.IdHidraulica : ReglaSonidoEnemigo.IdServos, PuntoOido);
        }

        private void RevisarAlerta()
        {
            bool alerta = enemigo.Alerta;
            if (alerta && !alertaAntes && Time.time >= proximaAlerta)
            {
                string id = ReglaSonidoEnemigo.IdAlerta(tipo);
                if (!string.IsNullOrEmpty(id))
                {
                    proximaAlerta = Time.time + ReglaSonidoEnemigo.SegundosEntreAlertas;
                    Sonidos.ReproducirEn(id, PuntoOido);
                }
            }
            alertaAntes = alerta;
        }

        private void RevisarEscudo()
        {
            bool agachado = enemigo.EstaAgachado;
            if (agachado && !agachadoAntes && ReglaSonidoEnemigo.EscudoAlCubrirse(tipo))
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdEscudoActivar, PuntoOido);
            }
            agachadoAntes = agachado;
            bool pelea = enemigo.EnPelea;
            if (pelea && !enPeleaAntes && ReglaSonidoEnemigo.EscudoAlPelear(tipo))
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdEscudoActivar, PuntoOido);
            }
            enPeleaAntes = pelea;
        }

        // El perro gruñe cuando avisa la mordida.
        private void RevisarPerro()
        {
            if (tipo != TipoSonoroEnemigo.Perro)
            {
                return;
            }
            if (conductaPerro == null)
            {
                conductaPerro = GetComponent<ConductaPerroNova>();
            }
            bool aviso = conductaPerro != null && conductaPerro.Regla != null && conductaPerro.Regla.Actual == ReglaPerroNova.Fase.Aviso;
            if (aviso && !avisoPerroAntes)
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdGrunido, PuntoOido);
            }
            avisoPerroAntes = aviso;
        }

        // La caída del cielo: jetpack con fade mientras está en el aire; al tocar el piso se enciende (robots) o llega (gigantes).
        private void RevisarCaida(float dt)
        {
            if (caida == null)
            {
                caida = GetComponent<CaidaNova>();
            }
            bool hayCaida = caida != null;
            bool enAire = hayCaida && caida.Actual == CaidaNova.Fase.Aire;
            ActualizarJetpack(enAire && ReglaSonidoEnemigo.TieneJetpack(tipo), dt);
            bool aterrizo = hayCaida && caida.Actual != CaidaNova.Fase.Aire;
            if (aterrizo && !aterrizoAntes)
            {
                AlAterrizar();
            }
            aterrizoAntes = aterrizo || (aterrizoAntes && !hayCaida);
        }

        private void AlAterrizar()
        {
            if (ReglaSonidoEnemigo.EsGigante(tipo))
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdLlegadaPesado, transform.position);
            }
            else if (ReglaSonidoEnemigo.EsRobot(tipo))
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdEncender, PuntoOido);
            }
        }

        // Un gigante que ya estaba en la escena (sin caída) también anuncia su llegada la primera vez, una vez cargada la escena.
        private void RevisarLlegadaSinCaida()
        {
            if (llegadaRevisada)
            {
                return;
            }
            llegadaRevisada = true;
            if (caida == null && ReglaSonidoEnemigo.EsGigante(tipo) && Time.timeSinceLevelLoad > SegundosMinimosCarga)
            {
                Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdLlegadaPesado, transform.position);
            }
        }

        private void ActualizarJetpack(bool vuela, float dt)
        {
            if (vuela && bucleJetpack == Sonidos.SinBucle)
            {
                bucleJetpack = Sonidos.ReproducirBucle(ReglaSonidoEnemigo.IdJetpack, transform);
                if (bucleJetpack == Sonidos.SinBucle)
                {
                    return; // otro jetpack ya suena (un bucle a la vez) o no hay gestor
                }
                nivelJetpack = 0f;
            }
            if (bucleJetpack == Sonidos.SinBucle)
            {
                return;
            }
            nivelJetpack = ReglaSonidoEnemigo.NivelJetpack(nivelJetpack, vuela, dt);
            // El API de bucles no tiene volumen por bucle: el fade se oye como un motor que arranca (tono de 0.6 a 1) y se apaga (de 1 a 0.6).
            Sonidos.FijarTono(bucleJetpack, ReglaSonidoEnemigo.TonoJetpack(nivelJetpack));
            if (!vuela && nivelJetpack <= 0f)
            {
                PararJetpack();
            }
        }

        private void PararJetpack()
        {
            if (bucleJetpack != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleJetpack);
                bucleJetpack = Sonidos.SinBucle;
            }
            nivelJetpack = 0f;
        }

        private void AlRecibirDanio(PandilleroAPie quien, InfoDanio info)
        {
            string id = ReglaSonidoEnemigo.IdDanio(tipo);
            if (string.IsNullOrEmpty(id) || Time.time < proximasChispas)
            {
                return;
            }
            proximasChispas = Time.time + ReglaSonidoEnemigo.SegundosEntreChispas;
            Sonidos.ReproducirEn(id, info.Punto != Vector3.zero ? info.Punto : PuntoOido);
        }

        private void AlLlegarAKO()
        {
            if (murio)
            {
                return;
            }
            murio = true;
            if (!ReglaSonidoEnemigo.EsRobot(tipo))
            {
                return;
            }
            Sonidos.ReproducirEn(ReglaSonidoEnemigo.IdMuerteRobot, PuntoOido);
            apagarEn = Time.time + ReglaSonidoEnemigo.SegundosHastaApagado(tipo);
        }
    }
}
