using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>
    /// Escudo de energía del jugador a pie (T-684). Lo crea <see cref="VidaAPie"/> con los datos del personaje; absorbe
    /// el daño que le pasa la vida, se recarga solo y se guarda como <see cref="IGuardable"/> (clave "escudo": solo las
    /// mejoras, el escudo arranca lleno).
    /// </summary>
    public class EscudoJugador : MonoBehaviour, IEscudo, IGuardable
    {
        public const string Clave = "escudo";

        [Serializable]
        private class DatosGuardados
        {
            public float bonoMaximo;
            public float bonoRecarga;
        }

        private ModeloEscudo modelo;

        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maximo => modelo != null ? modelo.Maximo : 0f;
        public float RecargaPorSegundo => modelo != null ? modelo.RecargaPorSegundo : 0f;
        public float BonoMaximo => modelo != null ? modelo.BonoMaximo : 0f;
        public float BonoRecarga => modelo != null ? modelo.BonoRecarga : 0f;
        public string ClaveGuardado => Clave;

        public event Action Cambio;
        public event Action<float> Absorbio;

        /// <summary>Crea el modelo con los datos base. Lo llama la vida del jugador al nacer.</summary>
        public void Iniciar(float maximo, float recargaPorSegundo, float segundosSinDanio)
        {
            if (modelo != null)
            {
                modelo.Cambio -= AlCambiar;
                modelo.Absorbio -= AlAbsorber;
            }
            modelo = new ModeloEscudo(maximo, recargaPorSegundo, segundosSinDanio);
            modelo.Cambio += AlCambiar;
            modelo.Absorbio += AlAbsorber;
            if (isActiveAndEnabled)
            {
                Registrar();
            }
        }

        private void OnEnable()
        {
            if (modelo != null)
            {
                Registrar();
            }
        }

        private void OnDisable()
        {
            Registro<IEscudo>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        private void Registrar()
        {
            Registro<IEscudo>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<IEscudo>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void Update() => modelo?.Avanzar(Time.deltaTime);

        /// <summary>Absorbe lo que puede y devuelve el daño que pasa a la vida.</summary>
        public float Absorber(float cantidad) => modelo != null ? modelo.Absorber(cantidad) : cantidad;

        public void Restaurar() => modelo?.Restaurar();

        public void MejorarMaximo(float puntos) => modelo?.MejorarMaximo(puntos);

        public void MejorarRecarga(float puntosPorSegundo) => modelo?.MejorarRecarga(puntosPorSegundo);

        public void Recuperar(float puntos) => modelo?.Recuperar(puntos);

        private void AlCambiar() => Cambio?.Invoke();

        private void AlAbsorber(float puntos) => Absorbio?.Invoke(puntos);

        public string Guardar()
        {
            return JsonUtility.ToJson(new DatosGuardados { bonoMaximo = BonoMaximo, bonoRecarga = BonoRecarga });
        }

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
            if (leidos == null || modelo == null)
            {
                Debug.LogWarning("[Personaje] El escudo no entendió los datos guardados; no se cambia nada.");
                return;
            }
            modelo.FijarBonos(leidos.bonoMaximo, leidos.bonoRecarga);
        }
    }
}
