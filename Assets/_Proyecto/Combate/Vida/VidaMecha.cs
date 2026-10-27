using System;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Vida
{
    /// <summary>
    /// Vida de un mecha: envuelve un <see cref="ModeloVida"/> y cumple el contrato de <see cref="IRecibeDanio"/>.
    /// Si el objeto tiene <see cref="GuardiaMecha"/>, <see cref="RecibirDanio"/> aplica el bloqueo.
    /// </summary>
    public class VidaMecha : MonoBehaviour, IVida, IRecibeDanio
    {
        [SerializeField] private DatosMecha datos;

        private ModeloVida modelo;
        private IPeleador peleador;
        private GuardiaMecha guardia;
        private bool buscoGuardia;
        private float multiplicadorDanioRecibido = 1f;
        private float factorEscudo = 1f;

        public DatosMecha Datos => datos;
        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maxima => modelo != null ? modelo.Maxima : 0f;
        public bool EstaKO => modelo != null && modelo.EstaKO;

        /// <summary>Multiplica el daño que entra, antes del bloqueo (1 = normal). Nunca baja de 0.</summary>
        public float MultiplicadorDanioRecibido
        {
            get => multiplicadorDanioRecibido;
            set => multiplicadorDanioRecibido = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Factor del escudo de una habilidad (1 = sin escudo). Se multiplica aparte de <see cref="MultiplicadorDanioRecibido"/>
        /// (la piedad del rival usa ese). Lo pone <c>LanzadorHabilidades</c>. Nunca baja de 0.
        /// </summary>
        public float FactorEscudo
        {
            get => factorEscudo;
            set => factorEscudo = Mathf.Max(0f, value);
        }

        public event Action Cambio;
        /// <summary>Se dispara cuando <see cref="CambiarDatos"/> o <see cref="Configurar"/> cambian los datos.</summary>
        public event Action DatosCambiados;
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
            DatosCambiados?.Invoke();
        }

        /// <summary>
        /// Cambia los datos sin restaurar: conserva los puntos de daño (<see cref="ReglaCambioDatos"/>) y, si estaba en KO, sigue en KO.
        /// Avisa <see cref="Cambio"/> y <see cref="DatosCambiados"/>. Null no hace nada (warning).
        /// </summary>
        public void CambiarDatos(DatosMecha nuevosDatos)
        {
            if (nuevosDatos == null)
            {
                Debug.LogWarning("[Combate] CambiarDatos recibió datos nulos en " + name);
                return;
            }
            if (modelo == null)
            {
                Configurar(nuevosDatos);
                return;
            }
            float nueva = ReglaCambioDatos.NuevaVida(modelo.Actual, modelo.Maxima, nuevosDatos.VidaMaxima, modelo.EstaKO);
            SoltarModelo();
            datos = nuevosDatos;
            // El modelo nuevo se arma antes de escuchar sus eventos: así no avisa un KO ni un cambio que ya ocurrieron.
            ModeloVida creado = new ModeloVida(datos.VidaMaxima);
            float resta = creado.Maxima - nueva;
            if (resta > 0f)
            {
                creado.Aplicar(resta);
            }
            modelo = creado;
            modelo.Cambio += AlCambiar;
            modelo.KO += AlLlegarAKO;
            Cambio?.Invoke();
            DatosCambiados?.Invoke();
        }

        /// <summary>Vuelve a la vida máxima y quita el KO.</summary>
        public void Restaurar()
        {
            if (modelo != null)
            {
                modelo.Restaurar();
            }
        }

        /// <summary>
        /// Deja la vida en <paramref name="valor"/> (limitado a (0, Maxima]) y quita el KO. Avisa <see cref="Cambio"/>
        /// pero no publica impacto ni KO. Con valor &lt;= 0 no hace nada y avisa un warning.
        /// </summary>
        public void FijarVida(float valor)
        {
            if (modelo == null)
            {
                return;
            }
            if (valor <= 0f || float.IsNaN(valor))
            {
                Debug.LogWarning("[Combate] FijarVida pide una vida mayor que 0 en " + name + ": " + valor);
                return;
            }
            float objetivo = Mathf.Min(valor, modelo.Maxima);
            // Restaurar() avisa Cambio y Aplicar() también; quien escuche ve el estado final al último aviso.
            modelo.Restaurar();
            float resta = modelo.Maxima - objetivo;
            if (resta > 0f)
            {
                modelo.Aplicar(resta);
            }
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            return Procesar(info, false);
        }

        /// <summary>Recibe el daño sin aplicar la guardia (el martillo rompe guardia). Lo demás es igual a <see cref="RecibirDanio"/>.</summary>
        public ResultadoDanio RecibirDanioSinGuardia(InfoDanio info)
        {
            return Procesar(info, true);
        }

        private ResultadoDanio Procesar(InfoDanio info, bool ignorarGuardia)
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

            if (!buscoGuardia)
            {
                guardia = GetComponent<GuardiaMecha>();
                buscoGuardia = true;
            }
            float cantidad = info.Cantidad * multiplicadorDanioRecibido * factorEscudo;
            TipoBloqueo bloqueo = TipoBloqueo.Ninguno;
            if (guardia != null && !ignorarGuardia)
            {
                (cantidad, bloqueo) = CalculoBloqueo.Calcular(cantidad, guardia.EstaArriba, guardia.SegundosArriba,
                    datos.VentanaBloqueoPerfecto, datos.ReduccionBloqueo);
                if (bloqueo != TipoBloqueo.Ninguno)
                {
                    Debug.Log(bloqueo == TipoBloqueo.Perfecto ? "[Combate] Bloqueo perfecto" : "[Combate] Bloqueo normal");
                }
            }

            float aplicado = cantidad > 0f ? modelo.Aplicar(cantidad) : 0f;
            ResultadoDanio resultado = new ResultadoDanio(aplicado, bloqueo, modelo.EstaKO);
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
