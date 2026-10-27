using System.Collections;
using MechaCyberpunk.Casino.Datos;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Casino.Tragamonedas
{
    /// <summary>
    /// La tragamonedas del casino (T-301): un <see cref="IInteractuable"/> que el jugador a pie usa con E. Cobra la apuesta de la
    /// <see cref="ICartera"/>, gira los tres carretes con una animación corta, muestra los símbolos y abona el premio. Paga en dinero directo
    /// (las fichas llegan en T-302). Los números salen de <see cref="DatosTragamonedas"/>; el azar y la cartera se pueden inyectar.
    /// </summary>
    public class TragamonedasMundo : MonoBehaviour, IInteractuable
    {
        [SerializeField] private DatosTragamonedas datos;
        [Tooltip("Los tres carretes, de izquierda a derecha.")]
        [SerializeField] private Text[] carretes = new Text[CalculoTragamonedas.Carretes];
        [Tooltip("Renglón que muestra el resultado del último giro.")]
        [SerializeField] private Text marcador;
        [Tooltip("Luz de neón; parpadea más rápido mientras gira.")]
        [SerializeField] private Light luzNeon;
        [Tooltip("Dónde se para el jugador (m, relativo a la máquina).")]
        [SerializeField] private Vector3 puntoDeUso = new Vector3(0f, 0f, 0.7f);
        [Min(0.1f)] [Tooltip("Duración del giro hasta que para el primer carrete (s).")]
        [SerializeField] private float segundosPrimerCarrete = 0.6f;
        [Min(0.05f)] [Tooltip("Lo que tarda cada carrete siguiente en parar (s).")]
        [SerializeField] private float segundosEntreCarretes = 0.3f;
        [Min(0.01f)] [Tooltip("Cada cuánto cambia el símbolo de un carrete que gira (s).")]
        [SerializeField] private float segundosPorCambio = 0.07f;
        [SerializeField] private string textoJugar = "Jugar · ${0}";
        [SerializeField] private string textoSinDinero = "Sin dinero · ${0}";
        [SerializeField] private string textoGirando = "Girando…";
        [SerializeField] private string textoGanaste = "GANASTE ${0}";
        [SerializeField] private string textoPerdiste = "SUERTE LA PRÓXIMA";
        [SerializeField] private string textoListo = "APUESTA ${0}";
        [Min(0f)] [SerializeField] private float intensidadNeonQuieto = 1.5f;
        [Min(0f)] [SerializeField] private float intensidadNeonGirando = 4f;
        [Min(0.1f)] [SerializeField] private float frecuenciaNeonGirando = 12f;

        private CalculoTragamonedas calculo;
        private ICartera carteraInyectada;
        private System.Random azar;
        private bool registrado;
        private bool girando;
        private int[] simbolosEnCurso;
        private int premioEnCurso;
        private bool premioPagado = true;

        public Vector3 Posicion => transform.TransformPoint(puntoDeUso);

        public bool PuedeUsar => !girando && Cartera != null && Cartera.PuedePagar(Apuesta);

        public string Texto
        {
            get
            {
                if (girando)
                {
                    return textoGirando;
                }
                bool alcanza = Cartera != null && Cartera.PuedePagar(Apuesta);
                return string.Format(alcanza ? textoJugar : textoSinDinero, Apuesta);
            }
        }

        /// <summary>True desde que se cobra la apuesta hasta que paran los tres carretes y se abona el premio.</summary>
        public bool Girando => girando;

        /// <summary>Los símbolos (índices) del último giro terminado, o null si todavía no hubo.</summary>
        public int[] UltimoResultado { get; private set; }

        /// <summary>Lo que pagó el último giro terminado.</summary>
        public int UltimoPremio { get; private set; }

        /// <summary>Cuántos giros se han cobrado en esta máquina.</summary>
        public int GirosCobrados { get; private set; }

        public int Apuesta => datos != null ? datos.Apuesta : 0;

        private ICartera Cartera => carteraInyectada ?? Registro<ICartera>.Actual;

        /// <summary>Para pruebas y escenas sin servicio de economía: usa esta cartera (y este azar) en vez de los de siempre.</summary>
        public void Inyectar(ICartera cartera, System.Random nuevoAzar)
        {
            carteraInyectada = cartera;
            if (nuevoAzar != null)
            {
                azar = nuevoAzar;
            }
        }

        private void Awake()
        {
            if (datos == null || carretes == null || carretes.Length != CalculoTragamonedas.Carretes || System.Array.IndexOf(carretes, null) >= 0)
            {
                Debug.LogError("[Casino] Faltan referencias en TragamonedasMundo de " + name, this);
                enabled = false;
                return;
            }
            calculo = datos.Crear();
            azar = azar ?? new System.Random();
            MostrarReposo();
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (girando)
            {
                // Si la apagan a media vuelta, el giro ya cobrado se termina en el acto para no perder el premio.
                StopAllCoroutines();
                TerminarGiro();
            }
        }

        public void Usar()
        {
            if (!enabled || !PuedeUsar)
            {
                return;
            }
            if (!Cartera.IntentarPagar(Apuesta, MotivoDinero.Casino))
            {
                return;
            }
            GirosCobrados++;
            simbolosEnCurso = calculo.Girar(azar);
            premioEnCurso = calculo.Pago(simbolosEnCurso, Apuesta);
            premioPagado = false;
            girando = true;
            StartCoroutine(Girar());
        }

        private IEnumerator Girar()
        {
            if (marcador != null)
            {
                marcador.text = string.Empty;
            }
            int detenidos = 0;
            float transcurrido = 0f;
            float proximoCambio = 0f;
            while (detenidos < CalculoTragamonedas.Carretes)
            {
                float paraEn = segundosPrimerCarrete + detenidos * segundosEntreCarretes;
                if (transcurrido >= paraEn)
                {
                    carretes[detenidos].text = NombreSimbolo(simbolosEnCurso[detenidos]);
                    detenidos++;
                    continue;
                }
                if (transcurrido >= proximoCambio)
                {
                    proximoCambio = transcurrido + segundosPorCambio;
                    for (int i = detenidos; i < CalculoTragamonedas.Carretes; i++)
                    {
                        carretes[i].text = NombreSimbolo(azar.Next(calculo.CantidadSimbolos));
                    }
                }
                if (luzNeon != null)
                {
                    luzNeon.intensity = Mathf.Lerp(intensidadNeonQuieto, intensidadNeonGirando, Mathf.PingPong(transcurrido * frecuenciaNeonGirando, 1f));
                }
                transcurrido += Time.deltaTime;
                yield return null;
            }
            TerminarGiro();
        }

        private void TerminarGiro()
        {
            for (int i = 0; i < CalculoTragamonedas.Carretes; i++)
            {
                carretes[i].text = NombreSimbolo(simbolosEnCurso[i]);
            }
            if (!premioPagado)
            {
                premioPagado = true;
                if (premioEnCurso > 0 && Cartera != null)
                {
                    Cartera.Abonar(premioEnCurso, MotivoDinero.Casino);
                }
            }
            UltimoResultado = (int[])simbolosEnCurso.Clone();
            UltimoPremio = premioEnCurso;
            if (marcador != null)
            {
                marcador.text = premioEnCurso > 0 ? string.Format(textoGanaste, premioEnCurso) : textoPerdiste;
            }
            if (luzNeon != null)
            {
                luzNeon.intensity = intensidadNeonQuieto;
            }
            girando = false;
        }

        private void MostrarReposo()
        {
            for (int i = 0; i < carretes.Length; i++)
            {
                carretes[i].text = NombreSimbolo(i % calculo.CantidadSimbolos);
            }
            if (marcador != null)
            {
                marcador.text = string.Format(textoListo, Apuesta);
            }
            if (luzNeon != null)
            {
                luzNeon.intensity = intensidadNeonQuieto;
            }
        }

        private string NombreSimbolo(int indice)
        {
            return datos.Simbolos[Mathf.Clamp(indice, 0, datos.Simbolos.Count - 1)].nombre;
        }
    }
}
