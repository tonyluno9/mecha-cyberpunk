using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: el escudo del guardia pesado de Nova (E12) como filtro de daño del <see cref="PandilleroAPie"/>. Traduce <see cref="ReglaEscudoFrontal"/>:
    /// de frente reduce 85% (TipoBloqueo.Normal, con un flash de chispa y sonido metálico), aguanta 150 de daño bruto y al romperse avisa con <see cref="Roto"/>.
    /// Dibuja una placa metálica azul en el antebrazo izquierdo (hueso LeftLowerArm); se esconde mientras el escudo está caído.
    /// </summary>
    public class EscudoFrontal : MonoBehaviour, IFiltroDanioAPie
    {
        private const string SonidoMetal = "ARM_BALA_IMPACTO_METAL";
        private const float SegundosChispa = 0.12f;
        private static readonly Vector3 TamanoPlaca = new Vector3(0.5f, 0.7f, 0.06f);
        private const float AdelantePlaca = 0.2f;

        [Tooltip("Material metálico azul de la placa del escudo.")]
        [SerializeField] private Material materialPlaca;

        private ReglaEscudoFrontal regla;
        private PandilleroAPie pandillero;
        private Transform placa;
        private Light chispa;
        private float finChispa;
        private bool placaVisible = true;

        public ReglaEscudoFrontal Regla => regla ?? (regla = new ReglaEscudoFrontal());
        public bool Activo => Regla.Activo;
        public Transform Placa => placa;

        /// <summary>Se rompió el escudo (el aguante llegó a 0).</summary>
        public event Action Roto;

        /// <summary>Llegó un golpe o bala (de frente o no): la conducta lo toma como provocación.</summary>
        public event Action Golpeado;

        private void Awake()
        {
            pandillero = GetComponent<PandilleroAPie>();
        }

        private void Start()
        {
            CrearPlaca();
        }

        private void Update()
        {
            Regla.Avanzar(Time.deltaTime);
            if (placa != null && placaVisible != Regla.Activo)
            {
                placaVisible = Regla.Activo;
                placa.gameObject.SetActive(placaVisible);
            }
            if (chispa != null && chispa.enabled && Time.time >= finChispa)
            {
                chispa.enabled = false;
            }
        }

        public float Filtrar(PandilleroAPie p, InfoDanio info, out TipoBloqueo bloqueo)
        {
            ReglaEscudoFrontal.Resultado r = Regla.Filtrar(info.Cantidad, info.Direccion, p.transform.forward);
            bloqueo = r.Bloqueo;
            Golpeado?.Invoke();
            if (r.Bloqueo != TipoBloqueo.Ninguno)
            {
                Chispar(info.Punto != Vector3.zero ? info.Punto : p.transform.position + Vector3.up * 1.2f + p.transform.forward * 0.5f);
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

        private void Chispar(Vector3 punto)
        {
            if (chispa == null)
            {
                GameObject go = new GameObject("ChispaEscudo");
                go.transform.SetParent(transform, false);
                chispa = go.AddComponent<Light>();
                chispa.type = LightType.Point;
                chispa.color = new Color(1f, 0.85f, 0.5f);
                chispa.range = 4f;
                chispa.intensity = 8f;
                chispa.shadows = LightShadows.None;
            }
            chispa.transform.position = punto;
            chispa.enabled = true;
            finChispa = Time.time + SegundosChispa;
        }

        private void CrearPlaca()
        {
            Animator animador = GetComponentInChildren<Animator>(true);
            Transform hueso = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.LeftLowerArm) : null;
            Transform padre = hueso != null ? hueso : transform;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "PlacaEscudo";
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col); // la placa no estorba a las balas ni al jugador
            }
            if (materialPlaca != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = materialPlaca;
            }
            go.transform.SetParent(padre, false);
            Vector3 centro = hueso != null ? hueso.position : transform.position + Vector3.up * 1.2f;
            go.transform.position = centro + transform.forward * AdelantePlaca;
            go.transform.rotation = transform.rotation;
            Vector3 escala = padre.lossyScale;
            go.transform.localScale = new Vector3(TamanoPlaca.x / Mathf.Max(escala.x, 0.0001f), TamanoPlaca.y / Mathf.Max(escala.y, 0.0001f), TamanoPlaca.z / Mathf.Max(escala.z, 0.0001f));
            placa = go.transform;
            placaVisible = true;
            placa.gameObject.SetActive(Regla.Activo);
        }
    }
}
