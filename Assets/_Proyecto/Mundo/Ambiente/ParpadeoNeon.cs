using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Un letrero de neón del barrio: baja su emisión de día, de noche brilla (más tenue si el local está cerrado) y, si
    /// <c>parpadea</c>, tiene apagones cortos e irregulares como neón viejo (<see cref="CalculoParpadeo"/>).
    /// Solo toca los materiales que emiten (los tubos): el marco metálico no se enciende. Sigue la franja del reloj del mundo.
    /// </summary>
    public class ParpadeoNeon : MonoBehaviour
    {
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
        private const float DiferenciaMinima = 0.001f;

        [Tooltip("Ajustes de brillo de día y del parpadeo.")]
        [SerializeField] private ConfigNeon config;

        [Tooltip("Renderers del letrero. Se atenúan sus materiales con emisión.")]
        [SerializeField] private Renderer[] renderers;

        [Tooltip("Si este letrero parpadea como neón viejo.")]
        [SerializeField] private bool parpadea;

        [Tooltip("Semilla del azar del parpadeo: misma semilla, mismo parpadeo.")]
        [SerializeField] private int semilla;

        [Tooltip("Si el local del letrero está cerrado: de noche brilla menos (ver nivel cerrado en la config).")]
        [SerializeField] private bool cerrado;

        private struct Tubo
        {
            public Renderer Renderer;
            public int Indice;
            public Color Emision;
        }

        private readonly List<Tubo> tubos = new List<Tubo>();
        private MaterialPropertyBlock bloque;
        private CalculoParpadeo calculo;
        private IRelojMundo reloj;
        private bool deNoche;
        private bool encendido = true;
        private float factor = -1f;

        /// <summary>True si este letrero parpadea.</summary>
        public bool Parpadea => parpadea;

        /// <summary>True si este letrero es de un local cerrado.</summary>
        public bool Cerrado => cerrado;

        /// <summary>Fracción del brillo de noche que tiene ahora (1 = a pleno, 0 = nada).</summary>
        public float FactorActual => factor;

        /// <summary>Suma, de todos los tubos del letrero, del canal más alto de su emisión de ahora (HDR). Sirve para medir.</summary>
        public float EmisionActual
        {
            get
            {
                float total = 0f;
                foreach (Tubo tubo in tubos)
                {
                    total += Mathf.Max(tubo.Emision.r, tubo.Emision.g, tubo.Emision.b) * Mathf.Max(factor, 0f);
                }
                return total;
            }
        }

        /// <summary>Deja listo un letrero creado por código (el creador de editor serializa los mismos datos).</summary>
        public void Configurar(ConfigNeon configNeon, Renderer[] losRenderers, bool parpadeaComoViejo, int laSemilla, bool localCerrado)
        {
            config = configNeon;
            renderers = losRenderers;
            parpadea = parpadeaComoViejo;
            semilla = laSemilla;
            cerrado = localCerrado;
        }

        private void Awake()
        {
            if (config == null || renderers == null || renderers.Length == 0)
            {
                Debug.LogError("[Mundo] ParpadeoNeon en '" + name + "' necesita su config y sus renderers.", this);
                enabled = false;
                return;
            }
            bloque = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                Material[] materiales = renderer.sharedMaterials;
                for (int i = 0; i < materiales.Length; i++)
                {
                    Material material = materiales[i];
                    if (material == null || !material.HasProperty(IdEmision))
                    {
                        continue;
                    }
                    Color emision = material.GetColor(IdEmision);
                    if (Mathf.Max(emision.r, emision.g, emision.b) > 0f)
                    {
                        tubos.Add(new Tubo { Renderer = renderer, Indice = i, Emision = emision });
                    }
                }
            }
            if (parpadea)
            {
                System.Random azar = new System.Random(semilla);
                calculo = new CalculoParpadeo(() => (float)azar.NextDouble(), config.Parametros);
            }
        }

        private void OnEnable()
        {
            reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.FranjaCambio += AlCambiarFranja;
                deNoche = reloj.EsDeNoche;
            }
            else
            {
                Registro<IRelojMundo>.Agregado += AlLlegarReloj;
            }
            Aplicar();
        }

        private void OnDisable()
        {
            Registro<IRelojMundo>.Agregado -= AlLlegarReloj;
            if (reloj != null)
            {
                reloj.FranjaCambio -= AlCambiarFranja;
                reloj = null;
            }
        }

        private void AlLlegarReloj(IRelojMundo nuevo)
        {
            Registro<IRelojMundo>.Agregado -= AlLlegarReloj;
            reloj = nuevo;
            reloj.FranjaCambio += AlCambiarFranja;
            deNoche = reloj.EsDeNoche;
            Aplicar();
        }

        private void AlCambiarFranja(FranjaDia franja)
        {
            deNoche = franja == FranjaDia.Noche;
            Aplicar();
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            bool ahora = calculo.Avanzar(Time.deltaTime);
            if (ahora != encendido)
            {
                encendido = ahora;
                Aplicar();
            }
        }

        private void Aplicar()
        {
            if (bloque == null)
            {
                return;
            }
            float nivel = !deNoche ? config.NivelDia : cerrado ? config.NivelCerrado : 1f;
            float nuevo = encendido ? nivel : nivel * config.NivelApagon;
            if (Mathf.Abs(nuevo - factor) < DiferenciaMinima)
            {
                return;
            }
            factor = nuevo;
            foreach (Tubo tubo in tubos)
            {
                tubo.Renderer.GetPropertyBlock(bloque, tubo.Indice);
                bloque.SetColor(IdEmision, tubo.Emision * factor);
                tubo.Renderer.SetPropertyBlock(bloque, tubo.Indice);
            }
        }
    }
}
