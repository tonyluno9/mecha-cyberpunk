using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Pruebas
{
    /// <summary>
    /// Herramienta de prueba: cada cierto tiempo se pinta de naranja durante su preparación y luego golpea al objetivo
    /// si está dentro de su alcance. Solo va en Pruebas_Tony.
    /// </summary>
    public class AtacanteDePrueba : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        [Tooltip("Estadísticas del atacante (DatosMecha_AtacantePrueba).")]
        [SerializeField] private DatosMecha datos;
        [Tooltip("Objeto con un IRecibeDanio a quien golpea (el mecha del jugador).")]
        [SerializeField] private GameObject objetivo;
        [Tooltip("Segundos entre el fin de un golpe y el aviso del siguiente (s).")]
        [Min(0.5f)] [SerializeField] private float intervalo = 5f;
        [Tooltip("Cubo que se pinta.")]
        [SerializeField] private Renderer visual;
        [SerializeField] private Color colorReposo = new Color(0.4f, 0.4f, 0.5f);
        [Tooltip("Color durante la preparación del golpe.")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.45f, 0.05f);

        private CicloGolpe ciclo;
        private IRecibeDanio receptor;
        private MaterialPropertyBlock bloque;
        private float espera;

        /// <summary>Si el cubo está en su aviso naranja (preparando el golpe).</summary>
        public bool EnAviso => ciclo != null && ciclo.Fase == FaseGolpe.Preparando;
        public FaseGolpe Fase => ciclo != null ? ciclo.Fase : FaseGolpe.Listo;

        private void Awake()
        {
            if (datos == null || objetivo == null || visual == null)
            {
                Debug.LogError("[Combate] Faltan referencias en AtacanteDePrueba de " + name, this);
                enabled = false;
                return;
            }
            receptor = objetivo.GetComponent<IRecibeDanio>();
            if (receptor == null)
            {
                Debug.LogError("[Combate] El objetivo de AtacanteDePrueba no tiene IRecibeDanio", this);
                enabled = false;
                return;
            }
            ciclo = new CicloGolpe(datos.PreparacionGolpe, datos.RecargaGolpe);
            bloque = new MaterialPropertyBlock();
            Pintar(colorReposo);
        }

        private void Update()
        {
            if (ciclo.Fase == FaseGolpe.Listo)
            {
                espera += Time.deltaTime;
                if (espera >= intervalo)
                {
                    espera = 0f;
                    ciclo.IntentarGolpear();
                    Pintar(colorAviso);
                }
            }

            if (ciclo.Avanzar(Time.deltaTime))
            {
                Pintar(colorReposo);
                Golpear();
            }
        }

        private void Golpear()
        {
            Vector3 hacia = objetivo.transform.position - transform.position;
            hacia.y = 0f;
            if (hacia.magnitude > datos.AlcanceGolpe)
            {
                return;
            }
            receptor.RecibirDanio(new InfoDanio(datos.DanioGolpe, TipoDanio.Golpe, objetivo.transform.position,
                hacia.normalized, null));
        }

        private void Pintar(Color color)
        {
            visual.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            visual.SetPropertyBlock(bloque);
        }
    }
}
