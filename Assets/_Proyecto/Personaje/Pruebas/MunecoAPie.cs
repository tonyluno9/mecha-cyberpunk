using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Muñeco de prueba de tamaño humano: tiene vida, parpadea al recibir daño y se restaura solo unos segundos después
    /// del KO. No es IPeleador, así que publica el impacto con objetivo null. Solo para escenas de prueba.
    /// </summary>
    public class MunecoAPie : MonoBehaviour, IVida, IRecibeDanio
    {
        private static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Vida máxima del muñeco (puntos).")]
        [Min(1f)] [SerializeField] private float vidaMaxima = 100f;
        [Tooltip("Segundos después del KO en que el muñeco se restaura solo (s).")]
        [Min(0.1f)] [SerializeField] private float segundosParaRestaurar = 3f;
        [Tooltip("Duración del parpadeo al recibir daño (s).")]
        [Min(0.01f)] [SerializeField] private float duracionParpadeo = 0.12f;
        [SerializeField] private Renderer visual;
        [SerializeField] private Color colorNormal = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color colorGolpe = Color.red;
        [SerializeField] private Color colorKO = new Color(0.2f, 0.2f, 0.2f);

        private ModeloVida modelo;
        private MaterialPropertyBlock bloque;
        private float finParpadeo = -1f;
        private float restauracion = -1f;

        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maxima => modelo != null ? modelo.Maxima : 0f;
        public bool EstaKO => modelo != null && modelo.EstaKO;

        public event Action Cambio;
        public event Action KO;

        private void Awake()
        {
            modelo = new ModeloVida(vidaMaxima);
            modelo.Cambio += AlCambiar;
            modelo.KO += AlLlegarAKO;
            bloque = new MaterialPropertyBlock();
            Pintar(colorNormal);
        }

        private void OnDestroy()
        {
            modelo.Cambio -= AlCambiar;
            modelo.KO -= AlLlegarAKO;
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (modelo.EstaKO || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            float aplicado = modelo.Aplicar(info.Cantidad);
            ResultadoDanio resultado = new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, modelo.EstaKO);
            if (!modelo.EstaKO)
            {
                finParpadeo = Time.time + duracionParpadeo;
                Pintar(colorGolpe);
            }
            EventosPelea.PublicarImpacto(null, info, resultado);
            return resultado;
        }

        public void Restaurar()
        {
            restauracion = -1f;
            finParpadeo = -1f;
            modelo.Restaurar();
            Pintar(colorNormal);
        }

        private void Update()
        {
            if (finParpadeo >= 0f && Time.time >= finParpadeo)
            {
                finParpadeo = -1f;
                Pintar(modelo.EstaKO ? colorKO : colorNormal);
            }
            if (restauracion >= 0f && Time.time >= restauracion)
            {
                Restaurar();
            }
        }

        private void AlCambiar() => Cambio?.Invoke();

        private void AlLlegarAKO()
        {
            restauracion = Time.time + segundosParaRestaurar;
            Pintar(colorKO);
            KO?.Invoke();
        }

        private void Pintar(Color color)
        {
            if (visual == null)
            {
                return;
            }
            visual.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, color);
            visual.SetPropertyBlock(bloque);
        }
    }
}
