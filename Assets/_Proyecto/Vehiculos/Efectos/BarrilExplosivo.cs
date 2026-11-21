using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Efectos
{
    /// <summary>
    /// T-745: un barril que revienta a balazos (o por otra explosión cerca). Al reventar hace daño por área (<see cref="TipoDanio.Explosion"/>, que a pie
    /// prende al jugador con el sistema de quemadura), empuja lo que tenga cuerpo físico, deja llamas en el suelo y se esconde.
    /// </summary>
    public class BarrilExplosivo : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        public const float VidaInicial = 30f;
        public const float RadioExplosion = 4.5f;
        public const float DanioExplosion = 70f;
        public const float VelocidadEmpuje = 9f;
        public const float SegundosLlamas = 8f;
        public const string IdExplosion = LimiteSonidoExplosion.IdBarril; // T-1532: antes EXP_ONDA
        private const float SubidaEmpuje = 0.6f;
        /// <summary>T-1317: segundos entre que un barril recibe una explosión y revienta, para que la cadena se vea barril por barril.</summary>
        public const float RetrasoCadena = 0.15f;

        private float vida = VidaInicial;

        public float Vida => vida;
        public bool Exploto { get; private set; }

        /// <summary>T-1317: ya recibió una explosión y va a reventar en <see cref="RetrasoCadena"/> segundos.</summary>
        public bool Detonando { get; private set; }

        /// <summary>La última explosión visual (para pruebas).</summary>
        public InstanciaExplosion Explosion { get; private set; }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (Exploto || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            // T-1317: una explosión dentro del radio revienta el barril siempre, sin importar la caída por distancia.
            if (info.Tipo == TipoDanio.Explosion)
            {
                float restante = vida;
                vida = 0f;
                if (!Detonando)
                {
                    Detonando = true;
                    if (isActiveAndEnabled)
                    {
                        StartCoroutine(ExplotarConRetraso());
                    }
                    else
                    {
                        Explotar();
                    }
                }
                return new ResultadoDanio(restante, TipoBloqueo.Ninguno, false);
            }
            float aplicado = Mathf.Min(info.Cantidad, vida);
            vida -= aplicado;
            if (vida <= 0f)
            {
                Explotar();
            }
            return new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, Exploto);
        }

        private System.Collections.IEnumerator ExplotarConRetraso()
        {
            yield return new WaitForSeconds(RetrasoCadena);
            if (!Exploto)
            {
                Explotar();
            }
        }

        private void Explotar()
        {
            Exploto = true;
            Vector3 centro = CentroDelBarril();
            LimiteSonidoExplosion.Sonar(TipoExplosionSonido.Barril, centro); // T-1472 / T-1532: EXP_BARRIL con tope en cadena
            Explosion = EfectosExplosion.Explosion(centro, RadioExplosion);
            EfectosExplosion.FuegoSuelo(new Vector3(centro.x, EntradaSuelo(centro), centro.z), SegundosLlamas);
            // T-1532: las llamas del suelo: fwoosh y fuego chico en bucle lo que duran.
            DetalleExplosionSonido.Programar(centro, new string[0], LimiteSonidoExplosion.RetrasoDetalle, LimiteSonidoExplosion.IdFuegoChico, SegundosLlamas);
            Esconder();
            DanarAlrededor(centro, RadioExplosion, DanioExplosion, VelocidadEmpuje, transform);
        }

        private Vector3 CentroDelBarril()
        {
            Collider c = GetComponentInChildren<Collider>();
            return c != null ? c.bounds.center : transform.position + Vector3.up * 0.5f;
        }

        private static float EntradaSuelo(Vector3 centro)
        {
            return Physics.Raycast(centro, Vector3.down, out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore) ? golpe.point.y : centro.y - 0.5f;
        }

        // El barril desaparece (sus partes se apagan, no se destruye: puede ser parte de un prefab de calle).
        private void Esconder()
        {
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        /// <summary>
        /// Daño por área con caída lineal hasta <paramref name="radio"/> y empuje (cambio de velocidad) a lo que tenga Rigidbody.
        /// <paramref name="ignorar"/> y sus hijos no cuentan. Devuelve cuántos objetos recibieron daño.
        /// </summary>
        public static int DanarAlrededor(Vector3 centro, float radio, float danio, float velocidadEmpuje, Transform ignorar)
        {
            Collider[] cercanos = Physics.OverlapSphere(centro, radio, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            HashSet<IRecibeDanio> vistos = new HashSet<IRecibeDanio>();
            HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();
            int danados = 0;
            foreach (Collider c in cercanos)
            {
                if (c == null || (ignorar != null && c.transform.IsChildOf(ignorar)))
                {
                    continue;
                }
                IRecibeDanio victima = c.GetComponentInParent<IRecibeDanio>();
                // Un trigger solo cuenta si es la zona de daño de algo que recibe daño.
                if (c.isTrigger && victima == null)
                {
                    continue;
                }
                float distancia = Vector3.Distance(centro, c.ClosestPoint(centro));
                float factor = Mathf.Clamp01(1f - distancia / radio);
                if (victima != null && vistos.Add(victima))
                {
                    Vector3 direccion = (c.bounds.center - centro).normalized;
                    ResultadoDanio resultado = victima.RecibirDanio(new InfoDanio(danio * factor, TipoDanio.Explosion, c.ClosestPoint(centro), direccion, null));
                    if (resultado.DanioAplicado > 0f)
                    {
                        danados++;
                    }
                }
                Rigidbody rb = c.attachedRigidbody;
                if (!c.isTrigger && rb != null && !rb.isKinematic && empujados.Add(rb))
                {
                    rb.AddExplosionForce(velocidadEmpuje, centro, radio, SubidaEmpuje, ForceMode.VelocityChange);
                }
            }
            return danados;
        }
    }
}
