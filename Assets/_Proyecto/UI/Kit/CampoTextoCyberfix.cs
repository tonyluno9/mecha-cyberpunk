using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Fila del kit que se escribe (T-413): elegida (con el teclado o el ratón encima), agrega en mayúsculas lo que llega por
    /// <c>Keyboard.onTextInput</c>; solo letras (con Ñ y acentos) y un espacio entre palabras, hasta <see cref="LargoMaximo"/>.
    /// Backspace borra. El valor sale a la derecha en turquesa con un cursor "_" que parpadea; vacía muestra el texto de
    /// <see cref="Vacio"/> en gris. Mientras está elegida el menú solo navega con flechas y Enter (<see cref="CapturaTeclado"/>).
    /// </summary>
    public class CampoTextoCyberfix : BotonCyberfix
    {
        private const string Cursor = "_";
        private const float SegundosParpadeo = 0.5f;

        [Tooltip("Cuántos caracteres caben como máximo.")]
        [Min(1)] [SerializeField] private int largoMaximo = 16;
        [Tooltip("Lo que se ve en gris mientras está vacío.")]
        [SerializeField] private string vacio = string.Empty;

        private readonly StringBuilder contenido = new StringBuilder();
        private Keyboard escuchado;
        private float reloj;

        public string Valor => contenido.ToString();
        public int LargoMaximo => largoMaximo;
        public string Vacio => vacio;

        public override bool CapturaTeclado => true;

        public event Action<string> CambioValor;

        public void Configurar(int nuevoLargo, string nuevoVacio)
        {
            largoMaximo = Mathf.Max(1, nuevoLargo);
            vacio = nuevoVacio ?? string.Empty;
            Refrescar();
        }

        /// <summary>Pone el valor pasando por la misma regla que el teclado.</summary>
        public void Fijar(string valor)
        {
            contenido.Clear();
            if (valor != null)
            {
                foreach (char c in valor)
                {
                    Agregar(c);
                }
            }
            Refrescar();
            CambioValor?.Invoke(Valor);
        }

        /// <summary>Escribe una letra como si llegara del teclado (no hace falta que esté elegida). True si entró.</summary>
        public bool Escribir(char letra)
        {
            if (!Agregar(letra))
            {
                return false;
            }
            reloj = 0f;
            Refrescar();
            CambioValor?.Invoke(Valor);
            return true;
        }

        private bool Agregar(char letra)
        {
            if (contenido.Length >= largoMaximo)
            {
                return false;
            }
            if (char.IsLetter(letra))
            {
                contenido.Append(char.ToUpperInvariant(letra));
                return true;
            }
            if (letra == ' ' && contenido.Length > 0 && contenido[contenido.Length - 1] != ' ')
            {
                contenido.Append(' ');
                return true;
            }
            return false;
        }

        protected override void OnDisable()
        {
            Escuchar(null);
            base.OnDisable();
        }

        private void LateUpdate()
        {
            Keyboard teclado = EstaElegido && IsInteractable() ? Keyboard.current : null;
            if (teclado != escuchado)
            {
                Escuchar(teclado);
            }
            if (teclado == null)
            {
                return;
            }
            if (teclado.backspaceKey.wasPressedThisFrame && contenido.Length > 0)
            {
                contenido.Length--;
                reloj = 0f;
                CambioValor?.Invoke(Valor);
            }
            reloj += Time.unscaledDeltaTime;
            Refrescar();
        }

        private void Escuchar(Keyboard teclado)
        {
            if (escuchado != null)
            {
                escuchado.onTextInput -= AlEscribir;
            }
            escuchado = teclado;
            if (escuchado != null)
            {
                escuchado.onTextInput += AlEscribir;
            }
        }

        private void AlEscribir(char letra)
        {
            if (EstaElegido && IsInteractable())
            {
                Escribir(letra);
            }
        }

        protected override string TextoDelValor()
        {
            bool cursor = EstaElegido && ((int)(reloj / SegundosParpadeo) % 2 == 0);
            if (contenido.Length == 0 && !EstaElegido)
            {
                return vacio;
            }
            return contenido + (cursor ? Cursor : " ");
        }
    }
}
