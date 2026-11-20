using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using Unity.Cinemachine;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1487: lo que se siente al conectar un golpe a pie: hitstop (congela los Animator del atacante y del golpeado con tiempo real, sin tocar
    /// Time.timeScale), impulso de cámara (<see cref="CinemachineImpulseSource"/> en el jugador), sonido de impacto y empuje del golpeado.
    /// Lo llama <see cref="GolpeAPie"/>; el listener del impulso lo pone la cámara.
    /// </summary>
    public class SensacionGolpe : MonoBehaviour
    {
        private const string IdGolpeFinal = "MEL_GOLPE";
        private const float VelocidadCongelada = 0f;

        private readonly TemporizadorHitstop temporizador = new TemporizadorHitstop();
        private readonly List<Animator> congelados = new List<Animator>();
        private readonly List<float> velocidadesOriginales = new List<float>();
        private ConfigGolpeAPie config;
        private CinemachineImpulseSource impulso;
        private bool impulsoPreparado;

        /// <summary>True mientras la animación está congelada (GolpeAPie no avanza su ciclo en ese tiempo).</summary>
        public bool EnHitstop => temporizador.Activo;

        public void Configurar(ConfigGolpeAPie nueva)
        {
            config = nueva;
            PrepararImpulso();
        }

        private void PrepararImpulso()
        {
            if (impulso == null)
            {
                impulso = GetComponent<CinemachineImpulseSource>();
                if (impulso == null)
                {
                    impulso = gameObject.AddComponent<CinemachineImpulseSource>();
                }
            }
            if (!impulsoPreparado)
            {
                TemblorCamara.Preparar(impulso); // T-1485: canal y forma del impulso
                impulsoPreparado = true;
            }
        }

        /// <summary>Un golpe conectó: aplica todo el feedback. <paramref name="objetivo"/> puede ser null (sin cuerpo propio).</summary>
        public void Conectar(Component objetivo, Vector3 direccion, bool final)
        {
            if (config == null)
            {
                return;
            }
            Congelar(config.Hitstop(final), objetivo);
            PrepararImpulso();
            TemblorCamara.Generar(impulso, config.ImpulsoCamara(final), direccion);
            Sonidos.ReproducirEn(ReglaSonidosJugador.GolpeCarne, transform.position);
            if (final)
            {
                Sonidos.ReproducirEn(IdGolpeFinal, transform.position);
            }
            Empujar(objetivo, direccion, final);
        }

        private void Empujar(Component objetivo, Vector3 direccion, bool final)
        {
            if (objetivo == null)
            {
                return;
            }
            Vector3 plano = new Vector3(direccion.x, 0f, direccion.z);
            if (plano.sqrMagnitude < 1e-6f)
            {
                return;
            }
            plano.Normalize();
            if (objetivo.GetComponentInParent<Rigidbody>() != null)
            {
                return; // los cuerpos con física ya los empuja GolpeAPie (BRAZOS)
            }
            CharacterController cc = objetivo.GetComponentInParent<CharacterController>();
            if (cc == null)
            {
                return;
            }
            EmpujeReceptorGolpe receptor = cc.GetComponent<EmpujeReceptorGolpe>();
            if (receptor == null)
            {
                receptor = cc.gameObject.AddComponent<EmpujeReceptorGolpe>();
            }
            receptor.Empujar(plano * config.Empuje(final), config.SegundosEmpuje);
        }

        private void Congelar(float segundos, Component objetivo)
        {
            if (segundos <= 0f)
            {
                return;
            }
            temporizador.Iniciar(segundos);
            Agregar(GetComponentsInChildren<Animator>());
            if (objetivo != null)
            {
                Animator[] delObjetivo = objetivo.transform.root.GetComponentsInChildren<Animator>();
                Agregar(delObjetivo);
            }
        }

        private void Agregar(Animator[] animadores)
        {
            for (int i = 0; i < animadores.Length; i++)
            {
                Animator a = animadores[i];
                if (a == null || congelados.Contains(a))
                {
                    continue;
                }
                congelados.Add(a);
                velocidadesOriginales.Add(a.speed);
                a.speed = VelocidadCongelada;
            }
        }

        private void Update()
        {
            if (temporizador.Activo && temporizador.Avanzar(Time.unscaledDeltaTime))
            {
                Descongelar();
            }
        }

        private void Descongelar()
        {
            for (int i = 0; i < congelados.Count; i++)
            {
                if (congelados[i] != null)
                {
                    congelados[i].speed = velocidadesOriginales[i];
                }
            }
            congelados.Clear();
            velocidadesOriginales.Clear();
        }

        private void OnDisable()
        {
            temporizador.Cancelar();
            Descongelar();
        }
    }
}
