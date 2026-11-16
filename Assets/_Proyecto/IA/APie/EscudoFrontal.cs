using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: el escudo del guardia pesado de Nova (E12) como filtro de daño del <see cref="PandilleroAPie"/>. Traduce <see cref="ReglaEscudoFrontal"/>:
    /// de frente reduce 85% (TipoBloqueo.Normal), aguanta 150 de daño bruto y al romperse avisa con <see cref="Roto"/>. El escudo que se ve es el del propio
    /// modelo E12 (antebrazo izquierdo); este componente solo pone la lógica y, al bloquear, una ráfaga de chispas amarillo-blancas a la altura del escudo
    /// y un destello de luz de 0.15 s, más un sonido metálico.
    /// </summary>
    public class EscudoFrontal : MonoBehaviour, IFiltroDanioAPie
    {
        private const string SonidoMetal = "ARM_BALA_IMPACTO_METAL";
        private const float SegundosChispa = 0.15f;
        private const int ChispasPorGolpe = 24;
        private const float AdelanteEscudo = 0.25f;
        private const float AlturaPechoPorDefecto = 1.3f;
        private static readonly Color ColorChispa = new Color(1f, 0.92f, 0.55f, 1f);

        [Tooltip("Material aditivo de las chispas del bloqueo.")]
        [SerializeField] private Material materialChispa;

        private ReglaEscudoFrontal regla;
        private Transform huesoEscudo;
        private Light destello;
        private ParticleSystem chispas;
        private float finDestello = -1f;

        public ReglaEscudoFrontal Regla => regla ?? (regla = new ReglaEscudoFrontal());
        public bool Activo => Regla.Activo;
        public ParticleSystem Chispas => chispas;

        /// <summary>Se rompió el escudo (el aguante llegó a 0).</summary>
        public event Action Roto;

        /// <summary>Llegó un golpe o bala (de frente o no): la conducta lo toma como provocación.</summary>
        public event Action Golpeado;

        private void Start()
        {
            Animator animador = GetComponentInChildren<Animator>(true);
            huesoEscudo = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.LeftLowerArm) : null;
        }

        private void Update()
        {
            Regla.Avanzar(Time.deltaTime);
            if (destello != null && destello.enabled && Time.time >= finDestello)
            {
                destello.enabled = false;
            }
        }

        public float Filtrar(PandilleroAPie p, InfoDanio info, out TipoBloqueo bloqueo)
        {
            ReglaEscudoFrontal.Resultado r = Regla.Filtrar(info.Cantidad, info.Direccion, p.transform.forward);
            bloqueo = r.Bloqueo;
            Golpeado?.Invoke();
            if (r.Bloqueo != TipoBloqueo.Ninguno)
            {
                Chispar(PuntoEscudo(p));
                Sonidos.ReproducirEn(SonidoMetal, p.transform.position);
            }
            if (r.SeRompio)
            {
                Roto?.Invoke();
            }
            return r.Entra;
        }

        /// <summary>Para pruebas y reaparición: el escudo vuelve completo.</summary>
        public void Reiniciar()
        {
            Regla.Reiniciar();
        }

        // La altura del escudo del modelo: el antebrazo izquierdo, un poco hacia el frente.
        private Vector3 PuntoEscudo(PandilleroAPie p)
        {
            Vector3 centro = huesoEscudo != null ? huesoEscudo.position : p.transform.position + Vector3.up * AlturaPechoPorDefecto;
            return centro + p.transform.forward * AdelanteEscudo;
        }

        private void Chispar(Vector3 punto)
        {
            if (chispas == null)
            {
                CrearEfectos();
            }
            chispas.transform.position = punto;
            chispas.Emit(ChispasPorGolpe);
            destello.transform.position = punto;
            destello.enabled = true;
            finDestello = Time.time + SegundosChispa;
        }

        private void CrearEfectos()
        {
            GameObject go = new GameObject("ChispasEscudo");
            go.transform.SetParent(transform, false);
            chispas = go.AddComponent<ParticleSystem>();
            chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = chispas.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = SegundosChispa;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = ColorChispa;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emision = chispas.emission;
            emision.enabled = false; // las ráfagas salen con Emit
            ParticleSystem.ShapeModule forma = chispas.shape;
            forma.shapeType = ParticleSystemShapeType.Sphere;
            forma.radius = 0.08f;
            ParticleSystemRenderer render = chispas.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Stretch;
            render.lengthScale = 2.5f;
            render.velocityScale = 0.1f;
            if (materialChispa != null)
            {
                render.sharedMaterial = materialChispa;
            }
            GameObject luz = new GameObject("DestelloEscudo");
            luz.transform.SetParent(transform, false);
            destello = luz.AddComponent<Light>();
            destello.type = LightType.Point;
            destello.color = ColorChispa;
            destello.range = 5f;
            destello.intensity = 14f;
            destello.shadows = LightShadows.None;
            destello.enabled = false;
        }
    }
}
