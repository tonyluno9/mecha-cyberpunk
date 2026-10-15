using System;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Reloj del mundo de prueba de Tony (T-107, D-tony-51): cumple <see cref="IRelojMundo"/> mientras el de Alan no esté en main.
    /// Tecla J = AvanzarHasta(8). Solo va en escenas y pruebas de Tony; no reemplaza al reloj de Alan.
    /// </summary>
    public class RelojDePrueba : MonoBehaviour, IRelojMundo, IGuardable
    {
        public const string Clave = "reloj";

        [Serializable]
        private class DatosGuardados
        {
            public int dia;
            public float hora;
        }

        [Tooltip("Hora con la que empieza el día 1 (h).")]
        [Range(0f, 23.99f)] [SerializeField] private float horaInicial = 21f;
        [Tooltip("Hora a la que empieza el día (h).")]
        [Range(0f, 24f)] [SerializeField] private float horaAmanecer = 7f;
        [Tooltip("Hora a la que empieza la noche (h).")]
        [Range(0f, 24f)] [SerializeField] private float horaAnochecer = 19f;
        [Tooltip("Cuánto dura un día de juego en tiempo real (minutos).")]
        [Min(0.01f)] [SerializeField] private float minutosPorDia = 20f;
        [Tooltip("Hora a la que salta la tecla de saltar (h).")]
        [Range(0f, 23.99f)] [SerializeField] private float horaSalto = 8f;
        [SerializeField] private InputAction saltar = new InputAction("SaltarReloj", InputActionType.Button, "<Keyboard>/j");

        private ModeloRelojDePrueba modelo;

        public float Hora => modelo.Hora;
        public int Dia => modelo.Dia;
        public FranjaDia Franja => modelo.Franja;
        public bool EsDeNoche => modelo.EsDeNoche;
        public float HoraAmanecer => modelo.HoraAmanecer;
        public float HoraAnochecer => modelo.HoraAnochecer;
        public bool Pausado => modelo.Pausado;
        public string ClaveGuardado => Clave;

        public event Action<int> HoraEnteraCambio;
        public event Action<FranjaDia> FranjaCambio;
        public event Action<int> DiaCambio;

        private void Awake()
        {
            modelo = new ModeloRelojDePrueba(horaInicial, 1, horaAmanecer, horaAnochecer);
            modelo.HoraEnteraCambio += h => HoraEnteraCambio?.Invoke(h);
            modelo.FranjaCambio += f => FranjaCambio?.Invoke(f);
            modelo.DiaCambio += d => DiaCambio?.Invoke(d);
        }

        private void OnEnable()
        {
            Registro<IRelojMundo>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            saltar.performed += AlSaltar;
            saltar.Enable();
        }

        private void OnDisable()
        {
            Registro<IRelojMundo>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            saltar.performed -= AlSaltar;
            saltar.Disable();
        }

        private void Update()
        {
            if (modelo.Pausado) return;
            modelo.Avanzar(Time.deltaTime / (minutosPorDia * 60f) * 24f);
        }

        public void Pausar(bool pausado) => modelo.Pausar(pausado);

        public void AvanzarHasta(float hora) => modelo.AvanzarHasta(hora);

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { dia = Dia, hora = Hora });

        public void Cargar(string texto)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null)
            {
                Debug.LogWarning("[Progresion] El reloj de prueba no entendió los datos guardados; no se cambia nada.");
                return;
            }
            modelo.Restablecer(leidos.dia, leidos.hora);
        }

        // Si ya marca la hora del salto, AvanzarHasta no haría nada: se salta un día entero.
        private void AlSaltar(InputAction.CallbackContext contexto)
        {
            if (Mathf.Abs(Hora - horaSalto) < 0.001f) modelo.Avanzar(24f);
            else modelo.AvanzarHasta(horaSalto);
        }
    }
}
