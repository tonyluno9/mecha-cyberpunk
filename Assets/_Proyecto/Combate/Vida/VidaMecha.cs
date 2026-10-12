using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Vida
{
    /// <summary>
    /// Vida de un mecha: envuelve un <see cref="ModeloVida"/> y cumple el contrato de <see cref="IRecibeDanio"/>.
    /// T-012 agrega el bloqueo dentro de <see cref="RecibirDanio"/>.
    /// </summary>
    public class VidaMecha : MonoBehaviour, IVida, IRecibeDanio
    {
        [SerializeField] private DatosMecha datos;

        private ModeloVida modelo;
        private IPeleador peleador;

        public DatosMecha Datos => datos;
        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maxima => modelo != null ? modelo.Maxima : 0f;
        public bool EstaKO => modelo != null && modelo.EstaKO;

        public event Action Cambio;
        public event Action KO;

        private void Awake()
        {
            if (datos != null && modelo == null)
            {
                CrearModelo();
                // Las pantallas que ya se activaron antes de este Awake leyeron una vida vacía.
                Cambio?.Invoke();
            }
        }

        private void OnDestroy()
        {
            SoltarModelo();
        }

        /// <summary>Cambia los datos y reinicia la vida al máximo. Lo usan las pruebas y el muñeco de T-012.</summary>
        public void Configurar(DatosMecha nuevosDatos)
        {
            datos = nuevosDatos;
            SoltarModelo();
            if (datos != null)
            {
                CrearModelo();
            }
            Cambio?.Invoke();
        }

        /// <summary>Vuelve a la vida máxima y quita el KO.</summary>
        public void Restaurar()
        {
            if (modelo != null)
            {
                modelo.Restaurar();
            }
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (modelo == null)
            {
                Debug.LogError("[Combate] VidaMecha sin DatosMecha en " + name, this);
                return ResultadoDanio.Ignorado;
            }
            if (modelo.EstaKO || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }

            float aplicado = modelo.Aplicar(info.Cantidad);
            ResultadoDanio resultado = new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, modelo.EstaKO);
            if (peleador == null)
            {
                peleador = GetComponent<IPeleador>();
            }
            EventosPelea.PublicarImpacto(peleador, info, resultado);
            return resultado;
        }

        private void CrearModelo()
        {
            modelo = new ModeloVida(datos.VidaMaxima);
            modelo.Cambio += AlCambiar;
            modelo.KO += AlLlegarAKO;
        }

        private void SoltarModelo()
        {
            if (modelo != null)
            {
                modelo.Cambio -= AlCambiar;
                modelo.KO -= AlLlegarAKO;
                modelo = null;
            }
        }

        private void AlCambiar() => Cambio?.Invoke();

        private void AlLlegarAKO()
        {
            Debug.Log("[Combate] KO: " + datos.Nombre);
            KO?.Invoke();
        }
    }
}
