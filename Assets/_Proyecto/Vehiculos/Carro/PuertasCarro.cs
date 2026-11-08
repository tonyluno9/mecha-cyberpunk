using DG.Tweening;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-742: las puertas delanteras del carro armado por piezas (Puerta_Izq y Puerta_Der, cada una con su pivote en la bisagra). La del lado por donde
    /// sube o baja el jugador se abre y se cierra con DOTween. La animación del jugador (AnimacionCarro, Personaje) avisa con SendMessage a la raíz del
    /// carro: <c>AbrirPuertaDelLado(x)</c> con la X local de la puerta que usa (menos de 0 es la izquierda) y <c>CerrarPuertas()</c>. Sin vidrio: se ve al conductor.
    /// </summary>
    public class PuertasCarro : MonoBehaviour
    {
        /// <summary>La puerta que se abre: a la izquierda (X local negativa) o a la derecha.</summary>
        public enum Lado
        {
            Izquierda,
            Derecha,
        }

        [Header("Puertas")]
        [Tooltip("Pivote de la puerta izquierda (en la bisagra delantera). Gira sobre su eje Y local.")]
        [SerializeField] private Transform puertaIzquierda;
        [Tooltip("Pivote de la puerta derecha (en la bisagra delantera). Gira sobre su eje Y local.")]
        [SerializeField] private Transform puertaDerecha;

        [Header("Movimiento")]
        [Tooltip("Cuánto se abre la puerta (°).")]
        [SerializeField, Range(10f, 100f)] private float anguloApertura = 65f;
        [Tooltip("Lo que tarda en abrir (s).")]
        [SerializeField, Min(0.05f)] private float segundosAbrir = 0.35f;
        [Tooltip("Lo que tarda en cerrar (s).")]
        [SerializeField, Min(0.05f)] private float segundosCerrar = 0.3f;
        [Tooltip("Si la X local de la puerta que usa el jugador está más cerca de 0 que esto (m), no se abre ninguna (sube por atrás o por el techo).")]
        [SerializeField, Min(0f)] private float zonaMuertaLado = 0.3f;

        private Tween tweenIzquierda;
        private Tween tweenDerecha;
        private bool izquierdaAbierta;
        private bool derechaAbierta;

        /// <summary>True si la puerta de ese lado está abierta o abriéndose.</summary>
        public bool EstaAbierta(Lado lado) => lado == Lado.Izquierda ? izquierdaAbierta : derechaAbierta;

        /// <summary>Cuánto está abierta la puerta (° desde cerrada).</summary>
        public float AperturaActual(Lado lado)
        {
            Transform puerta = lado == Lado.Izquierda ? puertaIzquierda : puertaDerecha;
            return puerta != null ? Mathf.Abs(Mathf.DeltaAngle(0f, puerta.localEulerAngles.y)) : 0f;
        }

        /// <summary>Cuántos grados se abre una puerta del todo.</summary>
        public float AnguloApertura => anguloApertura;

        private void Awake()
        {
            if (puertaIzquierda == null || puertaDerecha == null)
            {
                Debug.LogError($"[Vehiculos] {name}: faltan los pivotes de las puertas.", this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            tweenIzquierda?.Kill();
            tweenDerecha?.Kill();
        }

        /// <summary>Mensaje de AnimacionCarro: abre la puerta que está del lado de <paramref name="xLocal"/> (X local del punto por donde sube o baja).</summary>
        public void AbrirPuertaDelLado(float xLocal)
        {
            if (Mathf.Abs(xLocal) < zonaMuertaLado)
            {
                return;
            }
            Abrir(xLocal < 0f ? Lado.Izquierda : Lado.Derecha);
        }

        /// <summary>Mensaje de AnimacionCarro: cierra las dos puertas.</summary>
        public void CerrarPuertas()
        {
            Cerrar(Lado.Izquierda);
            Cerrar(Lado.Derecha);
        }

        /// <summary>Abre la puerta de ese lado.</summary>
        public void Abrir(Lado lado)
        {
            Mover(lado, true);
        }

        /// <summary>Cierra la puerta de ese lado.</summary>
        public void Cerrar(Lado lado)
        {
            Mover(lado, false);
        }

        private void Mover(Lado lado, bool abrir)
        {
            if (!enabled)
            {
                return;
            }
            bool izquierda = lado == Lado.Izquierda;
            if ((izquierda ? izquierdaAbierta : derechaAbierta) == abrir)
            {
                return;
            }
            Transform puerta = izquierda ? puertaIzquierda : puertaDerecha;
            // La izquierda gira hacia +Y y la derecha hacia -Y (en el marco del modelo): la parte de atrás de la puerta sale hacia afuera.
            float destino = abrir ? (izquierda ? anguloApertura : -anguloApertura) : 0f;
            Tween anterior = izquierda ? tweenIzquierda : tweenDerecha;
            anterior?.Kill();
            Tween nuevo = puerta.DOLocalRotate(new Vector3(0f, destino, 0f), abrir ? segundosAbrir : segundosCerrar)
                .SetEase(abrir ? Ease.OutCubic : Ease.InOutQuad)
                .SetLink(gameObject);
            if (izquierda)
            {
                tweenIzquierda = nuevo;
                izquierdaAbierta = abrir;
            }
            else
            {
                tweenDerecha = nuevo;
                derechaAbierta = abrir;
            }
        }

        /// <summary>Cierra las dos puertas al instante, sin animar (para armar o reiniciar el carro).</summary>
        public void CerrarDeGolpe()
        {
            tweenIzquierda?.Kill();
            tweenDerecha?.Kill();
            izquierdaAbierta = false;
            derechaAbierta = false;
            if (puertaIzquierda != null)
            {
                puertaIzquierda.localRotation = Quaternion.identity;
            }
            if (puertaDerecha != null)
            {
                puertaDerecha.localRotation = Quaternion.identity;
            }
        }
    }
}
