using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1630: el semáforo de un cruce. Lleva el reloj de los dos ejes (<see cref="CalculoCruceSemaforo"/>), enciende las lentes de cada poste (rojo, ámbar, verde, con un MaterialPropertyBlock:
    /// no crea materiales) y responde a los carros con <see cref="Consultar"/>. Cada poste le habla a los carros que van hacia su <c>sentido</c> y pertenece a un eje; los postes sin
    /// modelo (solo control) ponen la línea de parada aunque no haya poste a la vista.
    /// </summary>
    public class SemaforoCruce : MonoBehaviour
    {
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private const int Lentes = 3; // rojo, ámbar, verde, en ese orden

        /// <summary>Un poste del cruce: dónde está, a qué sentido le habla, de qué eje es y sus tres lentes.</summary>
        [Serializable]
        public sealed class Poste
        {
            [SerializeField] private Transform poste;
            [Tooltip("Eje al que pertenece (0 o 1): los dos ejes se alternan.")]
            [SerializeField, Range(0, 1)] private int eje;
            [Tooltip("Hacia dónde van los carros a los que les habla (mundo, plano).")]
            [SerializeField] private Vector3 sentido = Vector3.forward;
            [Tooltip("Si es false solo se pinta (poste viejo adoptado) y no frena a nadie.")]
            [SerializeField] private bool controla = true;
            [Tooltip("Lentes en este orden: rojo, ámbar, verde. Pueden faltar.")]
            [SerializeField] private Renderer[] lentes = new Renderer[Lentes];
            [SerializeField] private Light luz;

            public Poste() { }

            public Poste(Transform poste, int eje, Vector3 sentido, bool controla, Renderer[] lentes, Light luz)
            {
                this.poste = poste;
                this.eje = Mathf.Clamp(eje, 0, CalculoCruceSemaforo.Ejes - 1);
                this.sentido = sentido;
                this.controla = controla;
                this.lentes = lentes ?? new Renderer[Lentes];
                this.luz = luz;
            }

            public Transform Transform => poste;
            public int Eje => eje;
            public Vector3 Sentido => sentido;
            public bool Controla => controla;
            public IReadOnlyList<Renderer> LentesDelPoste => lentes;
            public Light Luz => luz;
        }

        [Header("Ciclo")]
        [Tooltip("Segundos en verde por eje (s).")]
        [SerializeField, Min(0.5f)] private float segundosVerde = 8f;
        [Tooltip("Segundos en ámbar por eje (s).")]
        [SerializeField, Min(0.5f)] private float segundosAmbar = 2f;
        [Tooltip("Segundos en rojo para los dos ejes antes de cambiar de eje (s).")]
        [SerializeField, Min(0f)] private float segundosDespeje = 1.5f;
        [Tooltip("Segundos que se adelanta el reloj de este cruce (s): así no cambian todos a la vez.")]
        [SerializeField] private float desfase;

        [Header("Carros")]
        [Tooltip("Cuánto antes del poste queda la línea de parada, contando el medio carro (m).")]
        [SerializeField, Min(0f)] private float margenParada = 4.5f;
        [Tooltip("Un carro empieza a hacerle caso a este poste a esta distancia de su línea de parada (m).")]
        [SerializeField, Min(1f)] private float alcance = 40f;

        [Header("Luces")]
        [Tooltip("Color de cada lente encendida: rojo, ámbar, verde.")]
        [SerializeField] private Color[] colores = { new Color(1f, 0.12f, 0.08f), new Color(1f, 0.62f, 0.05f), new Color(0.1f, 1f, 0.3f) };
        [Tooltip("Intensidad HDR de la emisión de la lente encendida.")]
        [SerializeField, Min(0f)] private float intensidadEmision = 3f;
        [Tooltip("Cuánto se apagan las lentes que no tocan (fracción del color).")]
        [SerializeField, Range(0f, 1f)] private float nivelApagada = 0.12f;

        [SerializeField] private List<Poste> postes = new List<Poste>();

        private CalculoCruceSemaforo calculo;
        private MaterialPropertyBlock bloque;
        private float tiempo;
        private FaseCruce? forzada;
        private readonly int[] fasePintada = { -1, -1 };

        public IReadOnlyList<Poste> Postes => postes;
        public float Tiempo => tiempo;
        public float Ciclo
        {
            get
            {
                Preparar();
                return calculo.Ciclo;
            }
        }

        /// <summary>Segundos del ciclo con que arranca este cruce.</summary>
        public float Desfase => desfase;

        /// <summary>La luz que ve ahora el eje 0 o 1.</summary>
        public FaseCruce Fase(int eje)
        {
            Preparar();
            return forzada ?? calculo.FaseEn(eje, tiempo + desfase);
        }

        /// <summary>Pone el ciclo (lo usan el editor y las pruebas).</summary>
        public void Configurar(float verde, float ambar, float despeje, float desfaseSegundos)
        {
            segundosVerde = verde;
            segundosAmbar = ambar;
            segundosDespeje = despeje;
            desfase = desfaseSegundos;
            calculo = null;
        }

        /// <summary>Agrega un poste.</summary>
        public void AgregarPoste(Poste poste)
        {
            if (poste != null)
            {
                postes.Add(poste);
                fasePintada[0] = -1;
                fasePintada[1] = -1;
            }
        }

        /// <summary>Quita todos los postes.</summary>
        public void LimpiarPostes() => postes.Clear();

        /// <summary>Mueve el reloj del cruce a estos segundos desde su arranque (pruebas).</summary>
        public void FijarTiempo(float segundos) => tiempo = segundos;

        /// <summary>Obliga a los dos ejes a esta luz (pruebas y diagnóstico); null deja el ciclo normal.</summary>
        public void Forzar(FaseCruce? fase)
        {
            forzada = fase;
            Pintar();
        }

        /// <summary>
        /// Lo que le toca a un carro en <paramref name="posicion"/> que va hacia <paramref name="adelante"/>: la luz y los metros hasta la línea de parada del poste más cercano que le habla.
        /// Devuelve false si ninguno de los postes de este cruce es suyo.
        /// </summary>
        public bool Consultar(Vector3 posicion, Vector3 adelante, out FaseCruce fase, out float metros)
        {
            fase = FaseCruce.Verde;
            metros = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < postes.Count; i++)
            {
                Poste p = postes[i];
                if (p == null || !p.Controla || p.Transform == null)
                {
                    continue;
                }
                if (CalculoCruceSemaforo.Controla(posicion, adelante, p.Transform.position, p.Sentido, margenParada, alcance, out float m) && m < metros)
                {
                    metros = m;
                    fase = Fase(p.Eje);
                    hay = true;
                }
            }
            return hay;
        }

        private void OnEnable()
        {
            RegistroSemaforos.Agregar(this);
            fasePintada[0] = -1;
            fasePintada[1] = -1;
            Pintar();
        }

        private void OnDisable() => RegistroSemaforos.Quitar(this);

        private void Update()
        {
            tiempo += Time.deltaTime;
            Pintar();
        }

        private void Preparar()
        {
            if (calculo == null)
            {
                calculo = new CalculoCruceSemaforo(segundosVerde, segundosAmbar, segundosDespeje);
            }
            if (bloque == null)
            {
                bloque = new MaterialPropertyBlock();
            }
        }

        // Pinta solo cuando cambia la luz de un eje: de las tres lentes se enciende una y las otras quedan oscuras.
        private void Pintar()
        {
            Preparar();
            for (int eje = 0; eje < CalculoCruceSemaforo.Ejes; eje++)
            {
                FaseCruce fase = Fase(eje);
                int activa = fase == FaseCruce.Rojo ? 0 : fase == FaseCruce.Ambar ? 1 : 2;
                if (activa == fasePintada[eje])
                {
                    continue;
                }
                fasePintada[eje] = activa;
                for (int i = 0; i < postes.Count; i++)
                {
                    if (postes[i] != null && postes[i].Eje == eje)
                    {
                        PintarPoste(postes[i], activa);
                    }
                }
            }
        }

        private void PintarPoste(Poste poste, int activa)
        {
            IReadOnlyList<Renderer> lentes = poste.LentesDelPoste;
            for (int i = 0; i < lentes.Count && i < colores.Length; i++)
            {
                if (lentes[i] == null)
                {
                    continue;
                }
                bool encendida = i == activa;
                lentes[i].GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, encendida ? colores[i] : colores[i] * nivelApagada);
                bloque.SetColor(IdEmision, encendida ? colores[i] * intensidadEmision : Color.black);
                lentes[i].SetPropertyBlock(bloque);
            }
            if (poste.Luz != null && activa < colores.Length)
            {
                poste.Luz.color = colores[activa];
            }
        }
    }
}
