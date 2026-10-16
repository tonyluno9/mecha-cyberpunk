using System.Text;
using MechaCyberpunk.Progresion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Creacion
{
    /// <summary>
    /// Campo de texto de una línea (T-126). Enfocado, agrega lo que llega por <c>Keyboard.onTextInput</c> (sin caracteres de
    /// control), borra con Backspace, no pasa de <see cref="ReglaAlias.LargoMaximo"/> y muestra un cursor parpadeante.
    /// </summary>
    public class CampoTextoTeclado : MonoBehaviour
    {
        [SerializeField] private Text texto;

        private readonly StringBuilder contenido = new StringBuilder();
        private Keyboard tecladoEscuchado;
        private bool enfocado;
        private float segundosParpadeo = 0.5f;
        private float reloj;

        public string Valor => contenido.ToString();
        public bool Enfocado => enfocado;

        private void OnDisable()
        {
            Desuscribir();
        }

        private void OnEnable()
        {
            if (enfocado) Suscribir();
        }

        public void Configurar(Color color, int tamano, float parpadeo)
        {
            texto.color = color;
            texto.fontSize = tamano;
            segundosParpadeo = Mathf.Max(0.05f, parpadeo);
            Repintar();
        }

        public void Enfocar(bool valor)
        {
            enfocado = valor;
            reloj = 0f;
            if (valor && isActiveAndEnabled) Suscribir(); else Desuscribir();
            Repintar();
        }

        public void Limpiar()
        {
            contenido.Clear();
            Repintar();
        }

        private void Suscribir()
        {
            Desuscribir();
            tecladoEscuchado = Keyboard.current;
            if (tecladoEscuchado != null) tecladoEscuchado.onTextInput += AlEscribir;
        }

        private void Desuscribir()
        {
            if (tecladoEscuchado != null) tecladoEscuchado.onTextInput -= AlEscribir;
            tecladoEscuchado = null;
        }

        private void AlEscribir(char letra)
        {
            if (!enfocado || char.IsControl(letra) || contenido.Length >= ReglaAlias.LargoMaximo) return;
            contenido.Append(letra);
            reloj = 0f;
            Repintar();
        }

        private void Update()
        {
            if (!enfocado) return;
            if (Keyboard.current != tecladoEscuchado) Suscribir();
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.backspaceKey.wasPressedThisFrame && contenido.Length > 0)
            {
                contenido.Length--;
                reloj = 0f;
            }
            reloj += Time.unscaledDeltaTime;
            Repintar();
        }

        private void Repintar()
        {
            bool cursor = enfocado && ((int)(reloj / segundosParpadeo) % 2 == 0);
            texto.text = contenido + (cursor ? "_" : string.Empty);
        }
    }
}
