using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-622: el modelo del jugador sube, maneja y baja de un carro con los clips de Mixamo. T-656: al subir primero se acomoda en el punto
    /// de la puerta más cercana (pegado al carro), luego juega Carro_Entrar y se desliza al asiento; sentado juega Carro_Manejar (visible por la ventana; la cabeza solo se esconde en primera persona); al bajar juega Carro_Salir del asiento a la puerta y de ahí al piso. Con H suena el claxon (Honking Horn en los brazos).
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

        // T-694: la cabeza del conductor queda a esta fracción de la ventana (0 = línea de la carrocería, 1 = caja más alta, que incluye rejillas y luces): el techo visible está cerca de 0.45.
        private const float FraccionVentanaCabeza = 0.22f;
        private const float MinimoCabezaSobreCinturon = 0.2f;
        private const float MinimoCabezaBajoTecho = 0.25f;
        private const float MaximaSubidaAsiento = 1f;

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
        private IMontable montableActual;
        private bool modoMoto;
        private bool leerDelPadrePrevio = true;
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
        // T-963: avance del cuerpo entre puerta y asiento según la cadera del clip (null = ventana fija de antes).
        private float[] avanceClip;
        // T-976: dónde estaba el modelo justo antes de que la raíz del jugador salte a la salida (se repone en TrasColocar).
        private bool reponerVisual;
        private Vector3 visualAntesDeColocar;
        private Quaternion giroAntesDeColocar;
        private const int MuestrasAvance = 32;

        public FaseCarro Fase { get; private set; }

        /// <summary>True si el modelo va en una moto (T-696).</summary>
        public bool EnMoto => modoMoto;

        /// <summary>True mientras el modelo es visible dentro del carro (entrando, manejando o saliendo).</summary>
        public bool ModeloVisible => Fase != FaseCarro.Fuera;

        /// <summary>Posición local del modelo sentado, respecto de la raíz del jugador (que sigue al asiento).</summary>
        public Vector3 AsientoLocal => asientoLocal;

        private void OnEnable() => claxon.Enable();

        private void OnDisable()
        {
            claxon.Disable();
        }

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
            montableActual = montable;
            asientoLocal = CalcularAsientoLocal();
            asientoLocal.y += SubidaAlAsiento(montable.Asiento, asientoLocal);
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
            avanceClip = AvanceDelClip("Carro_Entrar", true);
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
            montableActual = montable;
            // T-695: sobre la moto el cuerpo va quieto. Sin esto el Animator lee la rapidez de la moto y juega Correr (saltitos y tambaleo del conductor).
            leerDelPadrePrevio = visual.LeerDelPadre;
            visual.LeerDelPadre = false;
            visual.Velocidad = 0f;
            visual.EnSuelo = true;
            // T-963: con los clips de Blender el cuerpo lo mueve PoseMoto con MotoSubir y MotoManejar (que va quieto); sin ellos se congela la pose como antes (T-695).
            visual.Congelado = !poseMoto.ConClips;
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
            reponerVisual = true;
            visualAntesDeColocar = raizVisual.position;
            giroAntesDeColocar = raizVisual.rotation;
            if (modoMoto)
            {
                visual.Congelado = false;
                poseMoto.Bajar();
                duracion = poseMoto.SegundosBajar + SegundosMinimosMoto;
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
            avanceClip = AvanceDelClip("Carro_Salir", false);
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Salir") / ReglasAnimacion.VelocidadEstado("CarroSalir"));
            transcurrido = 0f;
            Fase = FaseCarro.Saliendo;
            RestaurarCabeza();
            ApagarIK(false);
        }

        /// <summary>
        /// T-976: llamar justo después de colocar la raíz del jugador en la salida. El modelo es hijo de la raíz y saltaría con ella hasta el LateUpdate; en ese cuadro el
        /// Animator calcularía el IK con el cuerpo lejos de la moto (pies y cadera de un cuadro chueco). Se deja donde estaba.
        /// </summary>
        public void TrasColocar()
        {
            if (!reponerVisual || raizVisual == null)
            {
                return;
            }
            reponerVisual = false;
            raizVisual.SetPositionAndRotation(visualAntesDeColocar, giroAntesDeColocar);
        }

        /// <summary>
        /// T-976: dónde conviene que quede la raíz del jugador al bajar de la moto: donde termina el clip de bajar, a la altura de <paramref name="salida"/> y mirando
        /// como la moto. False si no va en moto con clips (se usa la salida del vehículo tal cual).
        /// </summary>
        public bool SalidaMoto(Pose salida, out Pose pose)
        {
            pose = salida;
            if (!modoMoto || poseMoto == null || !poseMoto.PoseFinalBajar(out Vector3 posicion, out Quaternion giro))
            {
                return false;
            }
            posicion.y = salida.position.y;
            pose = new Pose(posicion, giro);
            return true;
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
                // T-694: la cabeza se ve por la ventana (tercera persona); solo se esconde si la cámara está metida en ella (primera persona).
                OcultarCabezaSiLaCamaraEstaDentro();
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
            float t = avanceClip != null
                ? ReglasEntradaCarro.AvanceEn(avanceClip, u / duracion)
                : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(InicioDeslizarEntrada * duracion, FinDeslizarEntrada * duracion, u));
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
            if (avanceClip != null)
            {
                // T-963: del asiento a la puerta y de ahí a la raíz, al ritmo con el que el clip saca la cadera del carro.
                float p = ReglasEntradaCarro.AvanceEn(avanceClip, transcurrido / duracion);
                float tramo1 = Vector3.Distance(asiento, puerta);
                float largo = tramo1 + Vector3.Distance(puerta, fin);
                float recorrido = p * largo;
                if (largo < 1e-3f || recorrido <= tramo1)
                {
                    float g = tramo1 > 1e-3f ? recorrido / tramo1 : 1f;
                    raizVisual.SetPositionAndRotation(Vector3.Lerp(asiento, puerta, g), Quaternion.Slerp(giroAsiento, haciaFuera, g));
                }
                else
                {
                    float g = (recorrido - tramo1) / Mathf.Max(1e-3f, largo - tramo1);
                    raizVisual.SetPositionAndRotation(Vector3.Lerp(puerta, fin, g), Quaternion.Slerp(haciaFuera, transform.rotation, g));
                }
                return;
            }
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
            bool eraMoto = modoMoto;
            modoMoto = false;
            if (apuntado != null)
            {
                apuntado.enabled = apuntadoPrevio;
                apuntado = null;
            }
            Fase = FaseCarro.Fuera;
            raizVisual.localPosition = posicionOriginal;
            raizVisual.localRotation = rotacionOriginal;
            if (eraMoto)
            {
                // T-976: después de devolver el visual a su sitio, para que empiece a leer el giro desde ahí.
                visual.LeerDelPadre = leerDelPadrePrevio;
                visual.Congelado = false;
            }
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
                MuestrearSinMover(manejar, 0f);
                cadera = transform.InverseTransformPoint(huesoCadera.position);
            }
            raizVisual.localPosition = respaldo;
            raizVisual.localRotation = giro;
            return new Vector3(-cadera.x, -cadera.y - CaidaCadera, -cadera.z - RetrasoCadera);
        }

        // T-694: cuánto hay que subir al modelo sentado (m) para que su cabeza quede a media ventana del carro: por encima de la línea de la carrocería
        // y por debajo del techo. La línea sale de la caja "Carroceria" y el techo de la más alta de las cajas de choque del carro.
        private float SubidaAlAsiento(Transform asiento, Vector3 asientoLocalModelo)
        {
            Transform carro = asiento.GetComponentInParent<Rigidbody>() != null ? asiento.GetComponentInParent<Rigidbody>().transform : asiento.parent;
            Transform hueso = animador.GetBoneTransform(HumanBodyBones.Head);
            AnimationClip manejar = BuscarClip("Carro_Manejar_Bucle");
            if (carro == null || hueso == null || manejar == null)
            {
                return 0f;
            }
            float cinturon = float.NaN;
            float techo = float.NegativeInfinity;
            foreach (BoxCollider caja in carro.GetComponentsInChildren<BoxCollider>(true))
            {
                if (caja.isTrigger)
                {
                    continue;
                }
                for (int i = 0; i < 2; i++)
                {
                    Vector3 esquina = caja.center + new Vector3(0f, (i == 0 ? 0.5f : -0.5f) * caja.size.y, 0f);
                    float y = carro.InverseTransformPoint(caja.transform.TransformPoint(esquina)).y;
                    techo = Mathf.Max(techo, y);
                    if (caja.name == "Carroceria" && i == 0)
                    {
                        cinturon = y;
                    }
                }
            }
            if (float.IsNaN(cinturon) || techo <= cinturon)
            {
                return 0f;
            }
            // Altura de la cabeza en el clip, con el modelo en el origen.
            Vector3 respaldo = raizVisual.localPosition;
            Quaternion giro = raizVisual.localRotation;
            raizVisual.localPosition = Vector3.zero;
            raizVisual.localRotation = Quaternion.identity;
            MuestrearSinMover(manejar, 0f);
            float cabezaSobreModelo = transform.InverseTransformPoint(hueso.position).y;
            raizVisual.localPosition = respaldo;
            raizVisual.localRotation = giro;
            float cabezaAhora = carro.InverseTransformPoint(asiento.position).y + asientoLocalModelo.y + cabezaSobreModelo;
            float cabezaDeseada = Mathf.Min(Mathf.Max(Mathf.Lerp(cinturon, techo, FraccionVentanaCabeza), cinturon + MinimoCabezaSobreCinturon), techo - MinimoCabezaBajoTecho);
            return Mathf.Clamp(cabezaDeseada - cabezaAhora, 0f, MaximaSubidaAsiento);
        }

        /// <summary>Pone la pose del clip en ese tiempo sin que el objeto del Animator se quede movido por el avance del clip.</summary>
        private void MuestrearSinMover(AnimationClip clip, float segundos)
        {
            Transform objetoAnimador = animador.transform;
            Vector3 posicion = objetoAnimador.localPosition;
            Quaternion giro = objetoAnimador.localRotation;
            clip.SampleAnimation(animador.gameObject, segundos);
            objetoAnimador.localPosition = posicion;
            objetoAnimador.localRotation = giro;
        }

        /// <summary>
        /// T-963: muestrea la cadera del clip (en el plano, sobre el propio modelo en el origen) y devuelve su avance hacia el asiento (al subir) o hacia fuera (al bajar).
        /// Null si el clip casi no mueve la cadera (entonces se usa la ventana fija de antes).
        /// </summary>
        private float[] AvanceDelClip(string nombre, bool haciaElAsiento)
        {
            AnimationClip clip = BuscarClip(nombre);
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (clip == null || cadera == null)
            {
                return null;
            }
            Vector3 respaldo = raizVisual.localPosition;
            Quaternion giro = raizVisual.localRotation;
            Transform objetoAnimador = animador.transform;
            Vector3 respaldoAnimador = objetoAnimador.localPosition;
            Quaternion giroAnimador = objetoAnimador.localRotation;
            raizVisual.localPosition = Vector3.zero;
            raizVisual.localRotation = Quaternion.identity;
            var puntos = new Vector3[MuestrasAvance];
            for (int i = 0; i < MuestrasAvance; i++)
            {
                clip.SampleAnimation(animador.gameObject, clip.length * i / (MuestrasAvance - 1));
                Vector3 p = transform.InverseTransformPoint(cadera.position);
                puntos[i] = new Vector3(p.x, 0f, p.z);
            }
            // El muestreo puede mover el objeto del Animator con el avance del clip: se deja como estaba.
            objetoAnimador.localPosition = respaldoAnimador;
            objetoAnimador.localRotation = giroAnimador;
            raizVisual.localPosition = respaldo;
            raizVisual.localRotation = giro;
            Vector3 sitio = haciaElAsiento ? puntos[MuestrasAvance - 1] : puntos[0];
            var distancias = new float[MuestrasAvance];
            for (int i = 0; i < MuestrasAvance; i++)
            {
                distancias[i] = Vector3.Distance(puntos[i], sitio);
            }
            return ReglasEntradaCarro.AvanceDesdeDistancias(distancias, haciaElAsiento);
        }

        /// <summary>
        /// El clip que de verdad suena en el estado cuyo clip base es <paramref name="nombre"/> (el de Mixamo del controlador base). T-963: el jugador lo cambia por el de
        /// Blender (PJ01_Carro_*) en su override, así que primero se pregunta al override; si no hay, se busca por nombre.
        /// </summary>
        private AnimationClip BuscarClip(string nombre)
        {
            if (animador.runtimeAnimatorController is AnimatorOverrideController cambio)
            {
                AnimationClip propio = cambio[nombre];
                if (propio != null)
                {
                    return propio;
                }
            }
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
            Camera camara = montableActual != null ? montableActual.Camara : null;
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
