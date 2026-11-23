using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1495: puente entre el peatón de la ciudad (Trafico, que no referencia a Personaje) y su <see cref="VisualPersonaje"/>. El peatón avisa con
    /// SendMessage (AlHerirCivil, AlMorirCivil, AlRevivirCivil) y aquí se pone la reacción de daño y la animación de muerte (MuerteAdelante, MuerteAtras,
    /// MuerteDesmayo del controlador base, o el estado Caer de los controladores propios). Va en la raíz del prefab Peaton.
    /// </summary>
    public class AnimacionCivilMundo : MonoBehaviour
    {
        [Header("T-1521: guarda contra el civil vivo acostado")]
        [Tooltip("Cabeza sobre la planta (m) por debajo de la cual un civil vivo se considera acostado.")]
        [Min(0.1f)] [SerializeField] private float alturaMinimaCabeza = 0.5f;
        [Tooltip("Segundos seguidos con la cabeza baja, sin estar tumbado a propósito, antes de levantarlo.")]
        [Min(0.5f)] [SerializeField] private float segundosBajo = 2.5f;
        [Tooltip("Segundos seguidos tumbado (KO) sin que se haya avisado su muerte antes de levantarlo. Sacar a un conductor del carro tarda menos de 2 s.")]
        [Min(1f)] [SerializeField] private float segundosTumbado = 8f;
        [Tooltip("Cada cuántos segundos revisa la postura (s).")]
        [Min(0.1f)] [SerializeField] private float segundosEntreRevisiones = 0.5f;
        [Tooltip("Cuánto debe verse la raíz derecha para revisarlo (1 = vertical, valor mínimo de transform.up.y).")]
        [Range(0.5f, 1f)] [SerializeField] private float minimoRaizDerecha = 0.9f;

        private VisualPersonaje visual;
        private bool muerto;
        private ReglaPosturaCivil postura;
        private Transform cabeza;
        private float proximaRevision;
        // T40: cuerpo muerto que nadie oculta (el peatón que lo ocultaba ya no corre) se esconde pasado este tiempo, para que no quede un NPC tirado para siempre.
        private const float SegundosCuerpoMaximo = 20f;
        private float muertoDesde;
        private System.Collections.Generic.List<Renderer> ocultos;

        /// <summary>True desde que le avisaron su muerte hasta que vuelve a la vida.</summary>
        public bool MuerteAvisada => muerto;

        public VisualPersonaje Visual => visual != null ? visual : (visual = GetComponentInChildren<VisualPersonaje>(false));

        /// <summary>
        /// T-1533: recibe daño (de bala, golpe o atropello, herido o muerto): suena su voz de dolor, de hombre o de mujer según el visual que trae el peatón
        /// (<see cref="ReglaSonidoNpc"/>); los robots sueltan chispas.
        /// </summary>
        public void AlDolerCivil(Vector3 punto)
        {
            PeatonVariado peaton = GetComponent<PeatonVariado>();
            string nombre = peaton != null ? peaton.NombreVisual : name;
            Vector3 donde = punto != Vector3.zero ? punto : transform.position + Vector3.up;
            Sonidos.ReproducirEn(ReglaSonidoNpc.IdDolor(nombre, GetInstanceID()), donde);
        }

        /// <summary>Recibe un golpe sin morir: reacción de daño según la dirección (del atacante al personaje).</summary>
        public void AlHerirCivil(Vector3 direccion)
        {
            if (Visual != null)
            {
                Visual.Herir(Plano(direccion));
                // T-1561: herido que no murió y se queda quieto: pide ayuda con señas o se queda herido de pie (si corre, sigue corriendo).
                if (isActiveAndEnabled && !muerto)
                {
                    StartCoroutine(HeridoTrasReaccion());
                }
            }
        }

        /// <summary>Segundos que dura la reacción de daño antes de pasar a herido (s).</summary>
        private const float SegundosReaccionHerida = 0.7f;
        /// <summary>Velocidad (m/s) por debajo de la cual un civil herido se considera quieto.</summary>
        private const float VelocidadQuietoHerido = 0.3f;
        private bool heridoEnCurso;

        private System.Collections.IEnumerator HeridoTrasReaccion()
        {
            if (heridoEnCurso)
            {
                yield break;
            }
            heridoEnCurso = true;
            yield return new WaitForSeconds(SegundosReaccionHerida);
            ConfigClipsNpcT1561 config = ConfigClipsNpcT1561.Actual;
            VisualPersonaje v = Visual;
            if (config != null && v != null && !muerto && !v.Caido && v.Velocidad < VelocidadQuietoHerido)
            {
                AnimationClip clip = ReglasTransicionesT1561.HeridoPideAyuda(GetInstanceID()) ? config.HeridoPedirAyuda : config.HeridoIdle;
                if (clip != null && JugarEnBucle(clip))
                {
                    yield return new WaitForSeconds(ReglasTransicionesT1561.SegundosPedirAyuda);
                    DetenerSiJuega(clip);
                }
            }
            heridoEnCurso = false;
        }

        /// <summary>Mensaje (SendMessage) para que un civil que vio al jugador matar a alguien llame a la policía (NPC_LlamarPolicia). Quien detecte la muerte vista lo manda; hoy nadie (ver T-1561).</summary>
        public const string MensajeLlamarPolicia = "AlLlamarPoliciaCivil";

        /// <summary>T-1561: llama a la policía (NPC_LlamarPolicia en bucle, mezclado desde lo que hacía). Se corta con <see cref="AlDejarDeLlamarPoliciaCivil"/>.</summary>
        public void AlLlamarPoliciaCivil()
        {
            ConfigClipsNpcT1561 config = ConfigClipsNpcT1561.Actual;
            if (config == null || config.LlamarPolicia == null || Visual == null || Visual.Animador == null || Visual.Caido || muerto)
            {
                return;
            }
            Reproductor().Reproducir(Visual.Animador, config.LlamarPolicia, 1f, true, ReglasTransicionesT1561.SegundosMezclaNpc);
        }

        public void AlDejarDeLlamarPoliciaCivil()
        {
            ConfigClipsNpcT1561 config = ConfigClipsNpcT1561.Actual;
            if (config != null)
            {
                DetenerSiJuega(config.LlamarPolicia);
            }
        }

        /// <summary>Muere: elige la caída según la dirección del golpe y la reproduce.</summary>
        public void AlMorirCivil(Vector3 direccion)
        {
            muerto = true;
            muertoDesde = Time.time;
            if (Visual != null && accionPlaticaPuesta)
            {
                EstadosNpcT1560.PonerAccion(Visual.Animador, AccionNpcT1560.Ninguna);
                accionPlaticaPuesta = false;
            }
            RestaurarFrente();
            if (Visual != null)
            {
                ReproductorClipUnico bucle = Visual.GetComponent<ReproductorClipUnico>();
                if (bucle != null && bucle.Repite)
                {
                    bucle.Detener(); // T-1516: la muerte animada vuelve a mandar
                }
                Visual.RegistrarGolpe(Plano(direccion));
                Visual.Caido = true;
            }
        }

        /// <summary>T-1506: empieza a huir: juega NPC_Asustarse una vez (brinco atrás con las manos a la cara) y sigue corriendo. Sin clip configurado, no hace nada.</summary>
        public void AlAsustarCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (Visual == null || Visual.Animador == null || config == null || config.Asustarse == null || Visual.Caido)
            {
                return;
            }
            ReproductorClipUnico reproductor = Reproductor();
            if (reproductor.Repite)
            {
                // T-1561: de platicar, discutir, celular o agachado a asustarse con una mezcla corta (0.2 s), no de golpe.
                reproductor.Reproducir(Visual.Animador, config.Asustarse, 1f, false, ReglasTransicionesT1561.SegundosMezclaNpc);
                return;
            }
            if (!reproductor.Reproduciendo)
            {
                reproductor.Reproducir(Visual.Animador, config.Asustarse);
            }
        }

        private ReproductorClipUnico Reproductor()
        {
            ReproductorClipUnico reproductor = Visual.GetComponent<ReproductorClipUnico>();
            return reproductor != null ? reproductor : Visual.gameObject.AddComponent<ReproductorClipUnico>();
        }

        /// <summary>T-1516: juega <paramref name="clip"/> en bucle (si ya lo juega, no lo reinicia).</summary>
        private bool JugarEnBucle(AnimationClip clip)
        {
            if (Visual == null || Visual.Animador == null || clip == null || Visual.Caido)
            {
                return false;
            }
            ReproductorClipUnico reproductor = Reproductor();
            if (reproductor.ClipActual == clip)
            {
                return true;
            }
            return reproductor.Reproducir(Visual.Animador, clip, 1f, true);
        }

        private void DetenerSiJuega(params AnimationClip[] clips)
        {
            ReproductorClipUnico reproductor = Visual != null ? Visual.GetComponent<ReproductorClipUnico>() : null;
            if (reproductor == null || !reproductor.Repite)
            {
                return;
            }
            foreach (AnimationClip clip in clips)
            {
                if (clip != null && reproductor.ClipActual == clip)
                {
                    reproductor.Detener();
                    return;
                }
            }
        }

        /// <summary>T-1516: el civil se queda platicando (variante 0 o 1): clip de plática en bucle.</summary>
        public void AlPlaticarCivil(int variante)
        {
            VisualPersonaje v = Visual;
            if (v == null || v.Animador == null || v.Caido || muerto)
            {
                return;
            }
            if (Time.time < proximoCambioPlatica && (accionPlaticaPuesta || Reproductor().Repite))
            {
                return;
            }
            proximoCambioPlatica = Time.time + Mathf.Lerp(SegundosMinimosEntrePlaticas, SegundosMaximosEntrePlaticas, Mathf.Abs(Mathf.Sin(GetInstanceID() * 12.9898f + contadorPlatica * 78.233f)));
            int indice = ((variante * 3 + contadorPlatica) % AccionesPlatica.Length + AccionesPlatica.Length) % AccionesPlatica.Length;
            contadorPlatica++;
            ReproductorClipUnico reproductor = Reproductor();
            if (reproductor.Repite)
            {
                reproductor.Detener();
            }
            if (EstadosNpcT1560.PonerAccion(v.Animador, AccionesPlatica[indice]))
            {
                bool primera = !accionPlaticaPuesta;
                accionPlaticaPuesta = true;
                if (!primera && Mathf.Abs(Mathf.Sin(GetInstanceID() * 3.7f + contadorPlatica)) < ProbabilidadSenalar)
                {
                    EstadosNpcT1560.Gestear(v, contadorPlatica % 2 == 0 ? GestoNpcT1560.Senalar : GestoNpcT1560.Senalar2);
                }
            }
            else
            {
                ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
                if (config != null)
                {
                    JugarEnBucle((variante + contadorPlatica) % 2 == 0 ? config.Platicar1 : config.Platicar2);
                }
            }
            if (isActiveAndEnabled)
            {
                StartCoroutine(CorregirFrente(++medicionFrente));
            }
        }

        // T-1622: variedad de plática (Platicar A-D y Discutir 1-3 del controlador base; sin AccionNpc usa los dos clips de siempre) y giro de frente.
        private static readonly AccionNpcT1560[] AccionesPlatica =
        {
            AccionNpcT1560.PlaticarA, AccionNpcT1560.PlaticarB, AccionNpcT1560.PlaticarC, AccionNpcT1560.PlaticarD,
            AccionNpcT1560.Discutir1, AccionNpcT1560.Discutir2, AccionNpcT1560.Discutir3,
        };
        private const float SegundosMinimosEntrePlaticas = 7f;
        private const float SegundosMaximosEntrePlaticas = 14f;
        private const float ProbabilidadSenalar = 0.3f;
        private const float SegundosAntesDeMedirFrente = 0.7f;
        private const float SegundosEntreGritos = 2.5f;
        private float proximoCambioPlatica;
        private int contadorPlatica;
        private bool accionPlaticaPuesta;
        private float correccionFrente;
        private int medicionFrente;
        private float proximoGrito;

        // Espera a que la pose se asiente y gira el visual lo que el cuerpo se desvíe de la raíz (que ya mira a su pareja): arregla "platican de espaldas".
        private System.Collections.IEnumerator CorregirFrente(int cual)
        {
            yield return new WaitForSeconds(SegundosAntesDeMedirFrente);
            VisualPersonaje v = Visual;
            if (cual != medicionFrente || v == null || v.Animador == null || muerto || v.Caido)
            {
                yield break;
            }
            if (FrenteCuerpo.Medir(v.Animador, transform, out float grados) && Mathf.Abs(grados) >= FrenteCuerpo.DesvioMinimo)
            {
                v.transform.Rotate(0f, -grados, 0f, Space.World);
                correccionFrente -= grados;
            }
        }

        private void RestaurarFrente()
        {
            medicionFrente++;
            if (Mathf.Abs(correccionFrente) > 0.01f && Visual != null)
            {
                Visual.transform.Rotate(0f, -correccionFrente, 0f, Space.World);
            }
            correccionFrente = 0f;
        }

        public void AlDejarPlaticarCivil()
        {
            RestaurarFrente();
            if (accionPlaticaPuesta)
            {
                accionPlaticaPuesta = false;
                if (Visual != null)
                {
                    EstadosNpcT1560.PonerAccion(Visual.Animador, AccionNpcT1560.Ninguna);
                }
            }
            proximoCambioPlatica = 0f;
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                DetenerSiJuega(config.Platicar1, config.Platicar2);
            }
        }

        /// <summary>T-1622: grita de miedo (no hay voz de miedo en el catálogo: usa la voz de dolor de hombre o mujer; no más de un grito cada 2.5 s).</summary>
        public void AlGritarCivil()
        {
            if (Time.time < proximoGrito || muerto)
            {
                return;
            }
            proximoGrito = Time.time + SegundosEntreGritos;
            PeatonVariado peaton = GetComponent<PeatonVariado>();
            string nombre = peaton != null ? peaton.NombreVisual : name;
            Sonidos.ReproducirEn(ReglaSonidoNpc.IdDolor(nombre, GetInstanceID()), transform.position + Vector3.up * 1.5f);
        }

        /// <summary>T-1516: se agacha cubriéndose la cabeza (bucle) hasta que se levanta.</summary>
        public void AlAgacharCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                JugarEnBucle(config.Agachado);
            }
        }

        public void AlLevantarseCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                DetenerSiJuega(config.Agachado);
            }
        }

        /// <summary>El peatón vuelve a la vida (el pool lo reutiliza).</summary>
        public void AlRevivirCivil()
        {
            muerto = false;
            MostrarCuerpo();
            RestaurarFrente();
            accionPlaticaPuesta = false;
            postura?.Reiniciar();
            if (Visual != null)
            {
                Reproductor().Detener();
                Visual.Caido = false;
            }
        }

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + segundosEntreRevisiones;
            if (muerto && ocultos == null && Time.time - muertoDesde >= SegundosCuerpoMaximo)
            {
                OcultarCuerpo();
            }
            RevisarPostura(segundosEntreRevisiones);
        }

        private void OcultarCuerpo()
        {
            ocultos = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(false))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    ocultos.Add(r);
                }
            }
        }

        private void MostrarCuerpo()
        {
            if (ocultos == null)
            {
                return;
            }
            foreach (Renderer r in ocultos)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }
            ocultos = null;
        }

        /// <summary>T40: depuración. True si el cuerpo está oculto por llevar muerto más de <see cref="SegundosCuerpoMaximo"/> s.</summary>
        public bool CuerpoOculto => ocultos != null;

        /// <summary>T40: depuración. Lista los civiles activos que están acostados (cabeza a menos de 0.5 m, o KO, o muertos) y por qué, o "ninguno".</summary>
        public static string DepurarAcostados()
        {
            var sb = new System.Text.StringBuilder();
            foreach (AnimacionCivilMundo c in FindObjectsByType<AnimacionCivilMundo>(FindObjectsSortMode.None))
            {
                VisualPersonaje v = c.Visual;
                if (v == null)
                {
                    continue;
                }
                bool impulso = c.GetComponent<ImpulsoNpc>() != null;
                if (c.muerto || v.Caido || impulso || c.transform.up.y < 0.7f)
                {
                    sb.AppendLine(c.name + " en " + c.transform.position.ToString("F1") + ": muerto=" + c.muerto + " Caido=" + v.Caido + " enImpulso=" + impulso
                        + " ocultoPorTiempo=" + c.CuerpoOculto + " raizDerecha=" + c.transform.up.y.ToString("F2"));
                }
            }
            return sb.Length == 0 ? "ninguno" : sb.ToString();
        }

        /// <summary>
        /// T-1521: si un civil vivo lleva un rato con la cabeza a ras de piso o tumbado sin que nadie lo haya matado, suelta el clip, quita el KO y reinicia el Animator
        /// (queda de pie en Idle). Cubre un clip de caída o de agachado que no se soltó, un KO que nadie quitó y un Animator trabado.
        /// </summary>
        public void RevisarPostura(float dt)
        {
            VisualPersonaje v = Visual;
            if (v == null || v.Animador == null)
            {
                return;
            }
            if (cabeza == null || !cabeza.IsChildOf(v.Animador.transform))
            {
                cabeza = v.Animador.avatar != null && v.Animador.isHuman ? v.Animador.GetBoneTransform(HumanBodyBones.Head) : null;
            }
            if (cabeza == null)
            {
                return;
            }
            if (postura == null)
            {
                postura = new ReglaPosturaCivil(alturaMinimaCabeza, segundosBajo, segundosTumbado);
            }
            ReproductorClipUnico reproductor = v.GetComponent<ReproductorClipUnico>();
            bool enBucle = reproductor != null && reproductor.Repite;
            float alturaCabeza = cabeza.position.y - transform.position.y;
            bool derecha = transform.up.y >= minimoRaizDerecha;
            if (postura.Avanzar(dt, alturaCabeza, muerto, v.Caido, enBucle, derecha) != ReglaPosturaCivil.Accion.Levantar)
            {
                return;
            }
            Debug.LogWarning("[IA] T-1521: " + name + " estaba vivo y acostado (cabeza a " + alturaCabeza.ToString("F2") + " m, KO " + v.Caido + "): se levanta.", this);
            Reproductor().Detener();
            v.Caido = false;
            v.Animador.Rebind();
            ReactivarPaseo();
        }

        // T40: si una escena del barrio o un impulso lo dejó sin su paseo (PeatonMundo apagado) y se quedó tirado, al levantarlo vuelve a caminar.
        private void ReactivarPaseo()
        {
            if (GetComponent<ImpulsoNpc>() != null)
            {
                return;
            }
            foreach (MonoBehaviour m in GetComponents<MonoBehaviour>())
            {
                if (m != null && !m.enabled && m.GetType().Name == "PeatonMundo")
                {
                    m.enabled = true;
                }
            }
        }

        private Vector3 Plano(Vector3 direccion)
        {
            direccion.y = 0f;
            return direccion.sqrMagnitude < 1e-6f ? transform.forward : direccion.normalized;
        }
    }
}
