using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1202: un policía sentado dentro de la patrulla mientras maneja (se ve por la ventana) y que al llegar se baja con el clip Carro_Salir del
    /// jugador: del asiento a la puerta y de la puerta al piso. Es solo la figura (sin IA, sin colliders): al tocar el piso la patrulla pone en su
    /// lugar al policía de verdad. Se crea con <see cref="Crear"/> desde el prefab del policía sin que corra ningún script suyo.
    /// </summary>
    public class OcupantePatrulla : MonoBehaviour
    {
        // Del clip de bajar, la parte que va del asiento a la puerta (el resto va de la puerta al piso), como el jugador (AnimacionCarro).
        private const float FinTramoAsientoPuerta = 0.5f;
        private const float FinTramoPuertaPiso = 0.85f;
        // La cadera del clip sentado queda un poco por debajo y detrás del punto del asiento (m), igual que con el jugador.
        private const float CaidaCadera = 0.05f;
        private const float RetrasoCadera = 0.05f;
        // Igual que el jugador (ReglasAnimacion.VelocidadEstado("CarroSalir")): el clip de 5.8 s se juega 3 veces más rápido.
        private const float VelocidadSalir = 3f;

        private Animator animador;
        private PlayableGraph grafo;
        private AnimationClip clipSalir;
        private Vector3 asientoLocal;
        private Vector3 puertaLocal;
        private Vector3 pisoMundo;
        private Quaternion giroFinal;
        private float inicioBajada;
        private float duracion;
        private Action<OcupantePatrulla> alTocarPiso;

        /// <summary>True desde que empezó a bajarse.</summary>
        public bool Bajando { get; private set; }

        /// <summary>Dónde quedó la figura (sus pies) y hacia dónde mira; ahí va el policía de verdad.</summary>
        public Pose PoseFinal => new Pose(pisoMundo, giroFinal);

        /// <summary>
        /// Crea la figura del policía desde <paramref name="prefab"/>, sin sus scripts, colliders ni cuerpo rígido, como hija de <paramref name="carro"/>
        /// y sentada con la cadera en <paramref name="asientoLocalCarro"/> (en el espacio del carro) jugando <paramref name="manejar"/> en bucle.
        /// </summary>
        public static OcupantePatrulla Crear(GameObject prefab, Transform carro, Vector3 asientoLocalCarro, AnimationClip manejar, AnimationClip salir)
        {
            // Se instancia bajo un padre apagado: así ningún Awake del policía corre y se pueden quitar sus scripts antes de encenderlo.
            GameObject apagado = new GameObject("OcupanteApagado");
            apagado.SetActive(false);
            GameObject figura = Object.Instantiate(prefab, apagado.transform);
            figura.name = "PolicíaSentado";
            QuitarComponentes(figura);
            OcupantePatrulla ocupante = figura.AddComponent<OcupantePatrulla>();
            figura.transform.SetParent(carro, false);
            figura.transform.localRotation = Quaternion.identity;
            figura.transform.localPosition = asientoLocalCarro;
            Object.Destroy(apagado);
            ocupante.Iniciar(manejar, salir, asientoLocalCarro);
            return ocupante;
        }

        private static void QuitarComponentes(GameObject figura)
        {
            // Primero los scripts (pueden pedir el CharacterController con RequireComponent) y luego la física.
            MonoBehaviour[] scripts = figura.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = scripts.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(scripts[i]);
            }
            foreach (Collider c in figura.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            foreach (Rigidbody r in figura.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(r);
            }
        }

        private void Iniciar(AnimationClip manejar, AnimationClip salir, Vector3 asientoLocalCarro)
        {
            animador = GetComponentInChildren<Animator>();
            clipSalir = salir;
            if (animador == null || manejar == null)
            {
                return;
            }
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // La cadera del clip sentado cae en el asiento: se mide con la pose del clip sobre el propio modelo.
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (cadera != null)
            {
                Transform objetoAnimador = animador.transform;
                Vector3 p = objetoAnimador.localPosition;
                Quaternion g = objetoAnimador.localRotation;
                manejar.SampleAnimation(animador.gameObject, 0f);
                objetoAnimador.localPosition = p;
                objetoAnimador.localRotation = g;
                Vector3 caderaLocal = transform.InverseTransformPoint(cadera.position);
                asientoLocal = asientoLocalCarro - new Vector3(caderaLocal.x, caderaLocal.y + CaidaCadera, caderaLocal.z + RetrasoCadera);
                transform.localPosition = asientoLocal;
            }
            Reproducir(manejar);
        }

        private void Reproducir(AnimationClip clip)
        {
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
            AnimationPlayableUtilities.PlayClip(animador, clip, out grafo);
        }

        /// <summary>
        /// Se baja: del asiento a <paramref name="puertaLocalCarro"/> (en el espacio del carro, junto a la puerta) y de ahí a <paramref name="piso"/>,
        /// terminando de cara a <paramref name="giro"/>. Al tocar el piso llama a <paramref name="alTerminar"/>.
        /// </summary>
        public void Bajar(Vector3 puertaLocalCarro, Vector3 piso, Quaternion giro, Action<OcupantePatrulla> alTerminar)
        {
            if (Bajando)
            {
                return;
            }
            Bajando = true;
            puertaLocal = puertaLocalCarro;
            pisoMundo = piso;
            giroFinal = giro;
            alTocarPiso = alTerminar;
            inicioBajada = Time.time;
            duracion = clipSalir != null ? Mathf.Max(0.5f, clipSalir.length / VelocidadSalir) : 0.5f;
            if (animador != null && clipSalir != null)
            {
                Reproducir(clipSalir);
                Playable clip = grafo.GetRootPlayable(0);
                clip.SetDuration(clipSalir.length);
                clip.SetSpeed(VelocidadSalir);
            }
        }

        private void LateUpdate()
        {
            if (!Bajando || transform.parent == null)
            {
                return;
            }
            Transform carro = transform.parent;
            float t = Time.time - inicioBajada;
            Vector3 asiento = carro.TransformPoint(asientoLocal);
            Vector3 puerta = carro.TransformPoint(puertaLocal);
            Vector3 fuera = Vector3.ProjectOnPlane(puerta - asiento, Vector3.up);
            Quaternion haciaFuera = fuera.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(fuera, Vector3.up) : carro.rotation;
            if (t < FinTramoAsientoPuerta * duracion)
            {
                float f = Mathf.SmoothStep(0f, 1f, t / (FinTramoAsientoPuerta * duracion));
                transform.SetPositionAndRotation(Vector3.Lerp(asiento, puerta, f), Quaternion.Slerp(carro.rotation, haciaFuera, f));
                return;
            }
            float f2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FinTramoAsientoPuerta * duracion, FinTramoPuertaPiso * duracion, t));
            transform.SetPositionAndRotation(Vector3.Lerp(puerta, pisoMundo, f2), Quaternion.Slerp(haciaFuera, giroFinal, f2));
            if (t >= duracion)
            {
                Action<OcupantePatrulla> aviso = alTocarPiso;
                alTocarPiso = null;
                aviso?.Invoke(this);
            }
        }

        private void OnDestroy()
        {
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
        }
    }
}
