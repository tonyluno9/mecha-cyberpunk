using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1110: el escudo de energía del jefe final de Nova (E14) como filtro de daño. Mientras la regla lo tiene arriba (fase Escudo) absorbe el 100% del daño
    /// a la vida y devuelve <see cref="TipoBloqueo.Perfecto"/>; al agotar sus 140 puntos avisa con <see cref="Roto"/>. Dibuja una esfera cian translúcida con el borde brillante
    /// alrededor del jefe que pulsa cuando recibe un golpe y se apaga mientras el escudo está caído.
    /// </summary>
    public class EscudoEnergia : MonoBehaviour, IFiltroDanioAPie
    {
        private const string SonidoMetal = "NOV_ESCUDO_IMPACTO"; // T-1533: balas contra el escudo de Nova
        private const float SegundosPulso = 0.25f;
        private const float DiametroEsfera = 3.4f;
        private const float AlturaCentro = 1.15f;
        private const float AumentoPulso = 0.12f;
        private const float BrilloReposo = 1f;
        private const float BrilloPulso = 2.6f;
        private static readonly int IdBrillo = Shader.PropertyToID("_Brillo");

        [Tooltip("Material de la esfera del escudo (shader MechaCyberpunk/EscudoEnergia: translúcido, borde brillante).")]
        [SerializeField] private Material materialEsfera;

        private ConductaJefeFinalNova conducta;
        private Transform esfera;
        private Renderer renderizador;
        private MaterialPropertyBlock bloque;
        private float finPulso = -1f;

        /// <summary>Se agotó el escudo.</summary>
        public event Action Roto;

        public Transform Esfera => esfera;

        private void Awake()
        {
            conducta = GetComponent<ConductaJefeFinalNova>();
            bloque = new MaterialPropertyBlock();
        }

        private void Start()
        {
            CrearEsfera();
        }

        private void Update()
        {
            if (conducta == null || conducta.Regla == null || esfera == null)
            {
                return;
            }
            bool visible = conducta.Regla.EscudoActivo;
            if (esfera.gameObject.activeSelf != visible)
            {
                esfera.gameObject.SetActive(visible);
            }
            if (!visible)
            {
                return;
            }
            float t = finPulso > Time.time ? 1f - (finPulso - Time.time) / SegundosPulso : 1f;
            float escala = DiametroEsfera * (1f + AumentoPulso * (1f - t));
            esfera.localScale = new Vector3(escala, escala, escala);
            renderizador.GetPropertyBlock(bloque);
            bloque.SetFloat(IdBrillo, Mathf.Lerp(BrilloPulso, BrilloReposo, t));
            renderizador.SetPropertyBlock(bloque);
        }

        public float Filtrar(PandilleroAPie p, InfoDanio info, out TipoBloqueo bloqueo)
        {
            if (conducta == null || conducta.Regla == null)
            {
                bloqueo = TipoBloqueo.Ninguno;
                return info.Cantidad;
            }
            ReglaJefeFinalNova.ResultadoEscudo r = conducta.Regla.AbsorberDanio(info.Cantidad);
            if (!r.Absorbido)
            {
                bloqueo = TipoBloqueo.Ninguno;
                return info.Cantidad;
            }
            bloqueo = TipoBloqueo.Perfecto;
            finPulso = Time.time + SegundosPulso;
            Sonidos.ReproducirEn(SonidoMetal, p.transform.position);
            if (r.SeRompio)
            {
                Roto?.Invoke();
            }
            return 0f;
        }

        private void CrearEsfera()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EsferaEscudo";
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
            renderizador = go.GetComponent<Renderer>();
            if (materialEsfera != null)
            {
                renderizador.sharedMaterial = materialEsfera;
            }
            renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderizador.receiveShadows = false;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * AlturaCentro;
            go.transform.localScale = Vector3.one * DiametroEsfera;
            esfera = go.transform;
            esfera.gameObject.SetActive(conducta != null && conducta.Regla != null && conducta.Regla.EscudoActivo);
        }
    }
}
