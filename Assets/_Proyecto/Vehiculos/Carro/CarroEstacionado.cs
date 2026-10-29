using System.Collections;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Carro estacionado que se puede robar (T-401). Va junto a un <see cref="CarroViejo"/>. La primera vez que el jugador sube
    /// suena la alarma (luces que parpadean y un sonido si hay clip), suma calor una sola vez y el carro queda como suyo.
    /// </summary>
    [RequireComponent(typeof(CarroViejo))]
    public class CarroEstacionado : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        [SerializeField] private float segundosAlarma = 3f;
        [SerializeField] private float calorAlRobar = 30f;
        [SerializeField] private float parpadeoSegundos = 0.25f;
        [SerializeField] private AudioClip sonidoAlarma;
        [Tooltip("Color de la carrocería (alfa 0 deja el original).")]
        [SerializeField] private Color tinte = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private bool esTuyo;

        private CarroViejo carro;
        private Light[] luces;
        private AudioSource fuente;

        /// <summary>True si ya es del jugador (no vuelve a sumar calor).</summary>
        public bool EsTuyo => esTuyo;

        /// <summary>True mientras suena la alarma.</summary>
        public bool AlarmaSonando { get; private set; }

        /// <summary>Color de la carrocería; se aplica con un MaterialPropertyBlock.</summary>
        public void PonerTinte(Color color)
        {
            tinte = color;
            AplicarTinte();
        }

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            AplicarTinte();
        }

        private void OnEnable()
        {
            carro.OcupadoCambio += AlCambiarOcupado;
        }

        private void OnDisable()
        {
            if (carro != null)
            {
                carro.OcupadoCambio -= AlCambiarOcupado;
            }
        }

        private void AplicarTinte()
        {
            if (tinte.a <= 0f)
            {
                return;
            }
            foreach (Renderer renderizador in GetComponentsInChildren<Renderer>(true))
            {
                if (renderizador.name != "Carroceria")
                {
                    continue;
                }
                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                renderizador.GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, tinte);
                bloque.SetColor(IdColor, tinte);
                renderizador.SetPropertyBlock(bloque);
            }
        }

        private void AlCambiarOcupado(bool ocupado)
        {
            if (!ocupado || esTuyo)
            {
                return;
            }
            esTuyo = true;
            ICalor calor = Registro<ICalor>.Actual;
            if (calor != null)
            {
                calor.Sumar(calorAlRobar, MotivoCalor.Otro);
            }
            StartCoroutine(Alarma());
        }

        private IEnumerator Alarma()
        {
            AlarmaSonando = true;
            if (luces == null)
            {
                luces = new Light[2];
                for (int i = 0; i < luces.Length; i++)
                {
                    GameObject objeto = new GameObject("LuzAlarma_" + i);
                    objeto.transform.SetParent(transform, false);
                    objeto.transform.localPosition = new Vector3(i == 0 ? -0.8f : 0.8f, 0.9f, i == 0 ? 2f : -2f);
                    Light luz = objeto.AddComponent<Light>();
                    luz.type = LightType.Point;
                    luz.color = new Color(1f, 0.55f, 0.1f);
                    luz.range = 6f;
                    luz.intensity = 4f;
                    luz.enabled = false;
                    luces[i] = luz;
                }
            }
            if (sonidoAlarma != null)
            {
                if (fuente == null)
                {
                    fuente = gameObject.AddComponent<AudioSource>();
                    fuente.spatialBlend = 1f;
                    fuente.loop = true;
                }
                fuente.clip = sonidoAlarma;
                fuente.Play();
            }
            float fin = Time.time + segundosAlarma;
            bool alterna = false;
            while (Time.time < fin)
            {
                alterna = !alterna;
                luces[0].enabled = alterna;
                luces[1].enabled = !alterna;
                yield return new WaitForSeconds(parpadeoSegundos);
            }
            foreach (Light luz in luces)
            {
                luz.enabled = false;
            }
            if (fuente != null)
            {
                fuente.Stop();
            }
            AlarmaSonando = false;
        }
    }
}
