using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// El puño en primera persona se ve cromado con implante de BRAZOS, y con una franja magenta en el nivel 2 (T-192).
    /// Usa MaterialPropertyBlock (D-tony-122). Nada más del cuerpo se ve en primera persona (D-tony-63).
    /// </summary>
    public class PunoImplantes : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdMetalico = Shader.PropertyToID("_Metallic");
        private static readonly int IdBrillo = Shader.PropertyToID("_Smoothness");

        [SerializeField] private Renderer puno;
        [Tooltip("Franja que se prende con BRAZOS nivel 2.")]
        [SerializeField] private GameObject franja;
        [SerializeField] private Color colorFranja = new Color(0.9f, 0.1f, 0.6f);
        [SerializeField] private Color colorCromo = new Color(0.8f, 0.82f, 0.88f);

        private readonly SeguidorImplantes seguidor = new SeguidorImplantes();
        private MaterialPropertyBlock bloque;

        public Renderer Puno => puno;
        public GameObject Franja => franja;
        public int NivelMostrado { get; private set; }

        private void OnEnable()
        {
            seguidor.Cambio += Refrescar;
            seguidor.Activar();
            Refrescar();
        }

        private void OnDisable()
        {
            seguidor.Cambio -= Refrescar;
            seguidor.Desactivar();
        }

        private void Refrescar() => Aplicar(seguidor.Nivel(EspacioImplante.Brazos));

        /// <summary>Muestra el puño con el nivel de BRAZOS dado (0 = normal). También lo usan las capturas del editor.</summary>
        public void Aplicar(int nivel)
        {
            NivelMostrado = nivel;
            if (puno != null)
            {
                bloque ??= new MaterialPropertyBlock();
                puno.GetPropertyBlock(bloque);
                if (nivel > 0)
                {
                    bloque.SetColor(IdColorBase, colorCromo);
                    bloque.SetFloat(IdMetalico, 0.9f);
                    bloque.SetFloat(IdBrillo, 0.85f);
                    puno.SetPropertyBlock(bloque);
                }
                else
                {
                    puno.SetPropertyBlock(null);
                }
            }
            if (franja != null)
            {
                franja.SetActive(nivel >= 2);
                var render = franja.GetComponent<Renderer>();
                if (nivel >= 2 && render != null)
                {
                    bloque ??= new MaterialPropertyBlock();
                    render.GetPropertyBlock(bloque);
                    bloque.SetColor(IdColorBase, colorFranja);
                    render.SetPropertyBlock(bloque);
                }
            }
        }
    }
}
