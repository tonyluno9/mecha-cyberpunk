using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-622: el modelo del jugador sube, maneja y baja de un carro con los clips de Mixamo. T-656: al subir primero se acomoda en el punto
    /// de la puerta más cercana (pegado al carro), luego juega Carro_Entrar y se desliza al asiento; sentado juega Carro_Manejar (visible por la ventana, sin cabeza para no estorbar la cámara
    /// de primera persona); al bajar juega Carro_Salir del asiento a la puerta y de ahí al piso. Con H suena el claxon (Honking Horn en los brazos).
    /// Lo agrega JugadorAPie solo; no maneja el carro ni mueve la raíz del jugador (esa sigue al asiento).
    /// </summary>
    public class AnimacionCarro : MonoBehaviour
    {
        public enum FaseCarro
        {
            Fuera,
            Entrando,
            Dentro,
            Saliendo,
        }

        // Cuánto cae la cadera sentada respecto del punto del asiento del carro (m) y cuánto se echa para atrás.
        private const float CaidaCadera = 0.2f;
        private const float RetrasoCadera = 0.05f;
        private const float InicioDeslizarEntrada = 0.5f;
        private const float FinDeslizarEntrada = 0.9f;
        private const float FinTramoAsientoPuerta = 0.5f;
        private const float FinTramoPuertaPiso = 0.85f;
        private const float SegundosMinimos = 0.3f;
        private const float SegundosMinimosMoto = 0.05f;
        private const float DistanciaOcultarCabeza = 0.35f;
        private const float AlturaRayoPiso = 1.5f;
        private const float LargoRayoPiso = 4f;
        private const int MaxImpactosPiso = 8;

        private PoseMoto poseMoto;
        private IMontable montableMoto;
        private bool modoMoto;
        private ApuntadoIK apuntado;
        private bool apuntadoPrevio;
        private readonly RaycastHit[] impactosPiso = new RaycastHit[MaxImpactosPiso];
        private readonly InputAction claxon = new InputAction("Claxon", InputActionType.Button, "<Keyboard>/h");
        private VisualPersonaje visual;
        private Animator animador;
        private Transform raizVisual;
        private Transform cabeza;
        private IKPiesYMirada ik;
        private bool ikPrevio;
        private Vector3 posicionOriginal;
        private Quaternion rotacionOriginal;
        private Vector3 inicioLocal;
        private float inicioYaw;
        private Vector3 puertaLocal;
        private float puertaYaw;
        private float segundosAcercar;
        private bool clipIniciado;
        private Vector3 asientoLocal;
        private Transform marcoSalida;
        private Vector3 asientoSalidaLocal;
        private Vector3 puertaSalidaLocal;
        private Vector3 asientoMundo;
        private Vector3 puertaMundo;
        private Quaternion giroAsiento;
        private float duracion;
        private float transcurrido;

        public FaseCarro Fase { get; private set; }

        /// <summary>True mientras el modelo es visible dentro del carro (entrando, manejando o saliendo).</summary>
        public bool ModeloVisible => Fase != FaseCarro.Fuera;

        /// <summary>Posición local del modelo sentado, respecto de la raíz del jugador (que sigue al asiento).</summary>
        public Vector3 AsientoLocal => asientoLocal;

        private void OnEnable() => claxon.Enable();

        private void OnDisable() => claxon.Disable();

        /// <summary>Sube: el jugador estaba en <paramref name="posicionPrevia"/> mirando a <paramref name="frentePrevio"/>. Devuelve false si no hay modelo animable.</summary>
        public bool Subir(IMontable montable, Vector3 posicionPrevia, Vector3 frentePrevio)
        {
            if (!Preparar() || montable.Asiento == null)
            {
                return false;
            }
            raizVisual.gameObject.SetActive(true);
            posicionOriginal = raizVisual.localPosition;
            rotacionOriginal = raizVisual.localRotation;
            PuntosMoto puntosMoto = montable.Asiento.GetComponentInParent<PuntosMoto>();
            if (puntosMoto != null && puntosMoto.Completo)
            {
                return SubirMoto(puntosMoto, montable, posicionPrevia);
            }
            asientoLocal = CalcularAsientoLocal();
            // La raíz del jugador saltará al asiento en LateUpdate: todo se mide respecto del asiento (y el carro lo lleva consigo).
            Transform asiento = montable.Asiento;
            inicioLocal = asiento.InverseTransformPoint(posicionPrevia);
            Vector3 frente = Vector3.ProjectOnPlane(frentePrevio, Vector3.up);
            inicioYaw = frente.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(asiento.forward, frente, Vector3.up) : 0f;

            // T-656: la puerta más cercana (la del conductor o la del otro lado); el personaje se para pegado al carro, en el piso.
            Vector3 puerta = montable.PuntoSubida;
            if (PuertasMontable.TryEspejo(montable, out Vector3 opuesta))
            {
                puerta = ReglasEntradaCarro.PuertaMasCercana(posicionPrevia, puerta, opuesta);
            }
            puerta = ReglasEntradaCarro.PuntoPuerta(puerta, asiento.position);
            Rigidbody cuerpoCarro = asiento.GetComponentInParent<Rigidbody>();
            puerta = PisoBajo(puerta, cuerpoCarro != null ? cuerpoCarro.transform : (asiento.parent != null ? asiento.parent : asiento));
            puertaLocal = asiento.InverseTransformPoint(puerta);
            Vector3 haciaAsiento = asientoLocal - puertaLocal;
            puertaYaw = Mathf.Atan2(haciaAsiento.x, haciaAsiento.z) * Mathf.Rad2Deg;
            segundosAcercar = ReglasEntradaCarro.SegundosAcercar(Vector3.Distance(inicioLocal, puertaLocal));

            ApagarIK(true);
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Entrar") / ReglasAnimacion.VelocidadEstado("CarroEntrar"));
            transcurrido = 0f;
            Fase = FaseCarro.Entrando;
            clipIniciado = false;
            Colocar(0f);
            return true;
        }

        // T-660: en una moto el conductor se sienta ENCIMA (cadera en el asiento de la moto), las manos van al manubrio y los pies a los estribos por IK.
        private bool SubirMoto(PuntosMoto puntos, IMontable montable, Vector3 posicionPrevia)
        {
            poseMoto = animador.GetComponent<PoseMoto>();
            if (poseMoto == null)
            {
                poseMoto = animador.gameObject.AddComponent<PoseMoto>();
            }
            apuntado = animador.GetComponent<ApuntadoIK>();
            if (apuntado != null)
            {
                apuntadoPrevio = apuntado.enabled;
                apuntado.enabled = false;
            }
            ApagarIK(true);
            if (!poseMoto.Montar(puntos, posicionPrevia))
            {
                ApagarIK(false);
                return false;
            }
            modoMoto = true;
            montableMoto = montable;
            Fase = FaseCarro.Dentro;
            return true;
        }

        /// <summary>
        /// Baja: llamar justo antes de colocar la raíz del jugador en <paramref name="salida"/>. Juega Carro_Salir del asiento a la puerta
        /// (pegado al carro) y de ahí camina al piso, donde queda la raíz del jugador.
        /// </summary>
        public void Bajar(IMontable montable, Pose salida)
        {
            if (Fase == FaseCarro.Fuera || raizVisual == null)
            {
                return;
            }
            if (modoMoto)
            {
                poseMoto.Bajar();
                duracion = ReglasMoto.SegundosSubir + SegundosMinimosMoto;
                transcurrido = 0f;
                Fase = FaseCarro.Saliendo;
                return;
            }
            asientoMundo = raizVisual.position;
            giroAsiento = raizVisual.rotation;
            marcoSalida = montable != null ? montable.Asiento : null;
            puertaMundo = ReglasEntradaCarro.PuntoPuerta(salida.position, asientoMundo);
            if (marcoSalida != null)
            {
                asientoSalidaLocal = marcoSalida.InverseTransformPoint(asientoMundo);
                puertaSalidaLocal = marcoSalida.InverseTransformPoint(puertaMundo);
            }
            visual.Manejando = false;
            visual.Gestear(GestoVisual.CarroSalir);
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Salir") / ReglasAnimacion.VelocidadEstado("CarroSalir"));
            transcurrido = 0f;
            Fase = FaseCarro.Saliendo;
            RestaurarCabeza();
            ApagarIK(false);
        }

        /// <summary>Suena el claxon: los brazos tocan el claxon sentado al volante.</summary>
        public void Pitar()
        {
            if (Fase != FaseCarro.Dentro)
            {
                return;
            }
            if (!modoMoto)
            {
                visual.Pitar();
            }
            Sonidos.ReproducirEn("VEH_CLAXON_VIEJO", transform.position);
        }

        // El punto de la puerta se baja hasta el piso real (sin contar el carro ni al propio jugador) para que los pies no floten.
        private Vector3 PisoBajo(Vector3 punto, Transform carro)
        {
            int cantidad = Physics.RaycastNonAlloc(punto + Vector3.up * AlturaRayoPiso, Vector3.down, impactosPiso, LargoRayoPiso,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactosPiso[i];
                if (impacto.collider.transform.IsChildOf(carro) || impacto.collider.transform.IsChildOf(transform) || impacto.distance >= mejor)
                {
                    continue;
                }
                mejor = impacto.distance;
                punto.y = impacto.point.y;
            }
            return punto;
        }

        private bool Preparar()
        {
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (visual == null || visual.Animador == null || visual.Animador.runtimeAnimatorController == null)
            {
                return false;
            }
            animador = visual.Animador;
            raizVisual = visual.transform;
            cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
            ik = animador.GetComponent<IKPiesYMirada>();
            return true;
        }

        private void Update()
        {
            if (Fase == FaseCarro.Fuera || visual == null)
            {
                return;
            }
            if (Fase == FaseCarro.Dentro)
            {
                if (claxon.WasPressedThisFrame())
                {
                    Pitar();
                }
                return;
            }
            transcurrido += Time.deltaTime;
            if (Fase == FaseCarro.Entrando && !clipIniciado && transcurrido >= segundosAcercar)
            {
                // Ya está junto a la puerta: ahora sí juega el clip de subir.
                clipIniciado = true;
                visual.Manejando = true;
                visual.Gestear(GestoVisual.CarroEntrar);
            }
            float total = Fase == FaseCarro.Entrando ? segundosAcercar + duracion : duracion;
            if (transcurrido >= total)
            {
                Terminar();
            }
        }

        private void LateUpdate()
        {
            if (Fase == FaseCarro.Fuera || raizVisual == null)
            {
                return;
            }
            if (Fase == FaseCarro.Entrando)
            {
                Colocar(transcurrido);
            }
            else if (Fase == FaseCarro.Saliendo)
            {
                if (!modoMoto)
                {
                    ColocarSalida();
                }
            }
            else if (modoMoto)
            {
                // La pose de la moto la coloca PoseMoto; la cabeza solo se esconde si la cámara está metida en ella (primera persona).
                OcultarCabezaSiLaCamaraEstaDentro();
            }
            else
            {
                raizVisual.localPosition = asientoLocal;
                raizVisual.localRotation = Quaternion.identity;
                if (cabeza != null)
                {
                    cabeza.localScale = Vector3.zero;
                }
            }
        }

        // Primero llega a la puerta (pegado al carro), luego se desliza al asiento mientras juega el clip: mirando como el jugador, a la puerta, al frente.
        private void Colocar(float segundos)
        {
            if (segundos < segundosAcercar)
            {
                float f = Mathf.SmoothStep(0f, 1f, segundos / segundosAcercar);
                raizVisual.localPosition = Vector3.Lerp(inicioLocal, puertaLocal, f);
                raizVisual.localRotation = Quaternion.Euler(0f, Mathf.LerpAngle(inicioYaw, puertaYaw, f), 0f);
                return;
            }
            float u = segundos - segundosAcercar;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(InicioDeslizarEntrada * duracion, FinDeslizarEntrada * duracion, u));
            raizVisual.localPosition = Vector3.Lerp(puertaLocal, asientoLocal, t);
            raizVisual.localRotation = Quaternion.Euler(0f, Mathf.LerpAngle(puertaYaw, 0f, t), 0f);
        }

        // Del asiento a la puerta (pegado al carro) con el clip, y de la puerta al piso donde quedó la raíz del jugador.
        private void ColocarSalida()
        {
            Vector3 asiento = marcoSalida != null ? marcoSalida.TransformPoint(asientoSalidaLocal) : asientoMundo;
            Vector3 puerta = marcoSalida != null ? marcoSalida.TransformPoint(puertaSalidaLocal) : puertaMundo;
            Vector3 fin = transform.position;
            Vector3 salida = Vector3.ProjectOnPlane(puerta - asiento, Vector3.up);
            Quaternion haciaFuera = salida.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(salida, Vector3.up) : giroAsiento;
            float f1 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transcurrido / (FinTramoAsientoPuerta * duracion)));
            if (transcurrido < FinTramoAsientoPuerta * duracion)
            {
                raizVisual.SetPositionAndRotation(Vector3.Lerp(asiento, puerta, f1), Quaternion.Slerp(giroAsiento, haciaFuera, f1));
                return;
            }
            float f2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FinTramoAsientoPuerta * duracion, FinTramoPuertaPiso * duracion, transcurrido));
            raizVisual.SetPositionAndRotation(Vector3.Lerp(puerta, fin, f2), Quaternion.Slerp(haciaFuera, transform.rotation, f2));
        }

        private void Terminar()
        {
            if (Fase == FaseCarro.Entrando)
            {
                Fase = FaseCarro.Dentro;
                visual.Manejando = true;
                return;
            }
            modoMoto = false;
            if (apuntado != null)
            {
                apuntado.enabled = apuntadoPrevio;
                apuntado = null;
            }
            Fase = FaseCarro.Fuera;
            raizVisual.localPosition = posicionOriginal;
            raizVisual.localRotation = rotacionOriginal;
            RestaurarCabeza();
            ApagarIK(false);
        }

        // La cadera sentada (Carro_Manejar) cae donde dice el asiento: se mide con la pose del clip sobre el propio modelo.
        private Vector3 CalcularAsientoLocal()
        {
            Vector3 respaldo = raizVisual.localPosition;
            Quaternion giro = raizVisual.localRotation;
            raizVisual.localPosition = Vector3.zero;
            raizVisual.localRotation = Quaternion.identity;
            AnimationClip manejar = BuscarClip("Carro_Manejar_Bucle");
            Vector3 cadera = Vector3.zero;
            Transform huesoCadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (manejar != null && huesoCadera != null)
            {
                manejar.SampleAnimation(animador.gameObject, 0f);
                cadera = transform.InverseTransformPoint(huesoCadera.position);
            }
            raizVisual.localPosition = respaldo;
            raizVisual.localRotation = giro;
            return new Vector3(-cadera.x, -cadera.y - CaidaCadera, -cadera.z - RetrasoCadera);
        }

        private AnimationClip BuscarClip(string nombre)
        {
            foreach (AnimationClip c in animador.runtimeAnimatorController.animationClips)
            {
                if (c != null && c.name == nombre)
                {
                    return c;
                }
            }
            return null;
        }

        private float LargoClip(string nombre)
        {
            AnimationClip c = BuscarClip(nombre);
            return c != null ? c.length : 0f;
        }

        private void OcultarCabezaSiLaCamaraEstaDentro()
        {
            if (cabeza == null)
            {
                return;
            }
            Camera camara = montableMoto != null ? montableMoto.Camara : null;
            bool dentro = camara != null && camara.enabled && Vector3.Distance(camara.transform.position, cabeza.position) < DistanciaOcultarCabeza;
            cabeza.localScale = dentro ? Vector3.zero : Vector3.one;
        }

        private void RestaurarCabeza()
        {
            if (cabeza != null)
            {
                cabeza.localScale = Vector3.one;
            }
        }

        private void ApagarIK(bool apagar)
        {
            if (ik == null)
            {
                return;
            }
            if (apagar)
            {
                ikPrevio = ik.enabled;
                ik.enabled = false;
            }
            else
            {
                ik.enabled = ikPrevio || ik.enabled;
            }
        }
    }
}
