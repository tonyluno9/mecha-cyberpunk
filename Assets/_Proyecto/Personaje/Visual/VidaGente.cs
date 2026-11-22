using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1332: la gente de banqueta (<see cref="GenteDeBanqueta"/>) recibe daño de balas, golpes y explosiones. Al llegar la vida a 0 muere:
    /// deja de platicar o caminar, cae hacia atrás y queda tirada (como el peatón de T-663). Se agrega sola en <see cref="GenteDeBanqueta"/>
    /// con un collider de cápsula (el visual de fondo no trae ninguno). Sin clip de muerte para estos modelos todavía: la caída es de código.
    /// </summary>
    [DisallowMultipleComponent]
    public class VidaGente : MonoBehaviour, IRecibeDanio
    {
        private const float AlturaCapsula = 1.8f;
        private const float RadioCapsula = 0.3f;
        private const float SegundosCaida = 0.5f;
        private const float GradosCaida = 90f;
        private const float ElevacionTirado = 0.15f;

        [Min(1f)] [SerializeField] private float vidaMaxima = 40f;

        private float vida;
        private bool iniciada;
        private float progreso = 1f;
        private Quaternion rotacionInicial;
        private Quaternion rotacionFinal;
        private Vector3 posicionInicial;
        private Vector3 posicionFinal;

        /// <summary>True si ya murió: queda tirada y no hace nada más.</summary>
        public bool Muerta { get; private set; }

        public float Vida
        {
            get
            {
                Iniciar();
                return vida;
            }
        }

        public float VidaMaxima => vidaMaxima;

        /// <summary>Se avisa al morir una persona de banqueta.</summary>
        public static event Action<VidaGente> Murio;

        private void Awake() => Iniciar();

        private void Iniciar()
        {
            if (iniciada)
            {
                return;
            }
            iniciada = true;
            vida = vidaMaxima;
            if (GetComponentInChildren<Collider>(true) == null)
            {
                CapsuleCollider capsula = gameObject.AddComponent<CapsuleCollider>();
                capsula.height = AlturaCapsula;
                capsula.radius = RadioCapsula;
                capsula.center = new Vector3(0f, AlturaCapsula / 2f, 0f);
            }
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            Iniciar();
            if (Muerta || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            float antes = vida;
            vida = Mathf.Max(0f, vida - info.Cantidad);
            Sonidos.ReproducirEn(ReglaSonidoNpc.IdDolor(name, GetInstanceID()), transform.position + Vector3.up); // T-1533: NPC_H_DOLOR o NPC_M_DOLOR
            bool mato = vida <= 0f;
            if (mato)
            {
                Matar();
            }
            return new ResultadoDanio(Mathf.Min(antes, info.Cantidad), TipoBloqueo.Ninguno, mato);
        }

        /// <summary>Muere ya: se apaga su conducta y cae tirada.</summary>
        public void Matar()
        {
            if (Muerta)
            {
                return;
            }
            Iniciar();
            Muerta = true;
            vida = 0f;
            GenteDeBanqueta gente = GetComponent<GenteDeBanqueta>();
            if (gente != null)
            {
                gente.enabled = false; // OnDisable suelta el clip de platicar
            }
            GuardiaNova guardia = GetComponent<GuardiaNova>();
            if (guardia != null)
            {
                guardia.enabled = false;
            }
            Collider capsula = GetComponent<Collider>();
            if (capsula != null)
            {
                capsula.enabled = false;
            }
            rotacionInicial = transform.rotation;
            rotacionFinal = Quaternion.AngleAxis(-GradosCaida, transform.right) * rotacionInicial;
            posicionInicial = transform.position;
            posicionFinal = posicionInicial + Vector3.up * ElevacionTirado;
            progreso = 0f;
            MechaCyberpunk.Core.EventosRuido.PublicarMuerte(transform.position); // T-1622
            Murio?.Invoke(this);
        }

        private void Update()
        {
            if (!Muerta || progreso >= 1f)
            {
                return;
            }
            progreso = Mathf.Min(1f, progreso + Time.deltaTime / SegundosCaida);
            transform.SetPositionAndRotation(Vector3.Lerp(posicionInicial, posicionFinal, progreso), Quaternion.Slerp(rotacionInicial, rotacionFinal, progreso));
        }
    }
}
