using System;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEngine;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// RETIRADA del rival (T-202, D-tony-196): si aprendió RETIRADA, después de cada golpe suyo (pegue o no) a veces da un paso atrás hasta
    /// quedar fuera del alcance de tu golpe, así que el contragolpe tardío se queda corto. Aplica <see cref="ReglaRetirada"/> con
    /// <see cref="RivalBasico.IniciarRetirada"/>. Va dentro de los controlados del <c>PeleadorMecha</c>.
    /// </summary>
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(AdaptacionRival))]
    public class RetiradaRival : MonoBehaviour
    {
        private RivalBasico rival;
        private AdaptacionRival adaptacion;
        private ReglaRetirada regla;
        private Func<float> azar = () => UnityEngine.Random.value;
        private bool pendiente;
        private float faltaParaEmpezar;
        private PedidoRetirada pedido;

        /// <summary>Cambia el azar (0 a 1) de la retirada. Solo para pruebas; null vuelve al azar normal.</summary>
        public void FijarAzar(Func<float> nuevo)
        {
            azar = nuevo ?? (() => UnityEngine.Random.value);
        }

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            adaptacion = GetComponent<AdaptacionRival>();
            if (adaptacion.Datos == null || rival.Datos == null || rival.Datos.Mecha == null)
            {
                Debug.LogError("[IA] Faltan referencias en RetiradaRival de " + name, this);
                enabled = false;
                return;
            }
            DatosAdaptacionRival datos = adaptacion.Datos;
            regla = new ReglaRetirada(datos.ProbabilidadRetirada, datos.FactorVelocidadRetirada, datos.DemoraRetirada, datos.MargenRetirada,
                () => azar());
        }

        private void OnEnable()
        {
            rival.Impacto += AlImpactar;
        }

        private void OnDisable()
        {
            rival.Impacto -= AlImpactar;
            pendiente = false;
        }

        private void AlImpactar()
        {
            pendiente = false;
            GameObject objetivo = rival.Objetivo;
            VidaMecha vida = objetivo != null ? objetivo.GetComponent<VidaMecha>() : null;
            if (vida == null || vida.Datos == null || vida.EstaKO)
            {
                return;
            }
            if (regla.Decidir(adaptacion.Nivel(TipoAdaptacion.Retirada), vida.Datos.AlcanceGolpe, rival.Datos.Mecha.RecargaGolpe, out pedido))
            {
                pendiente = true;
                faltaParaEmpezar = pedido.Demora;
            }
        }

        private void Update()
        {
            if (!pendiente)
            {
                return;
            }
            faltaParaEmpezar -= Time.deltaTime;
            if (faltaParaEmpezar > 0f)
            {
                return;
            }
            pendiente = false;
            rival.IniciarRetirada(pedido.Distancia, pedido.SegundosMaximos, pedido.FactorVelocidad);
            if (rival.Retirandose)
            {
                adaptacion.AvisarUso(TipoAdaptacion.Retirada);
            }
        }
    }
}
