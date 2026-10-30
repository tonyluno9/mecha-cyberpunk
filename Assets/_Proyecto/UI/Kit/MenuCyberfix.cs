using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Navegación de un menú del kit (T-408): ↑ ↓ o W S mueven (sin dar la vuelta), ← → o A D cambian el valor de la fila elegida,
    /// Enter o Espacio aceptan y Esc regresa (UI_CANCELAR y <see cref="Regresar"/>). Al abrir elige el primer botón activo y muestra el
    /// cursor; al cerrar el último menú abierto el cursor vuelve a quedar bloqueado. Los botones son los <see cref="BotonCyberfix"/> hijos,
    /// en el orden de la jerarquía. El ratón encima elige el mismo botón que el teclado.
    /// </summary>
    public class MenuCyberfix : MonoBehaviour
    {
        public const string SonidoCancelar = "UI_CANCELAR";

        private static int abiertos;
        private readonly List<BotonCyberfix> botones = new List<BotonCyberfix>();
        private int elegido = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => abiertos = 0;

        /// <summary>Cuántos menús del kit están abiertos.</summary>
        public static int Abiertos => abiertos;

        public IReadOnlyList<BotonCyberfix> Botones => botones;
        public BotonCyberfix Elegido => elegido >= 0 && elegido < botones.Count ? botones[elegido] : null;

        /// <summary>Esc.</summary>
        public event Action Regresar;

        private void OnEnable()
        {
            abiertos++;
            KitUI.AsegurarEventSystem();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Recargar();
        }

        private void OnDisable()
        {
            abiertos = Mathf.Max(0, abiertos - 1);
            if (abiertos == 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>Vuelve a juntar los botones (si agregaste o quitaste) y elige el primero activo.</summary>
        public void Recargar()
        {
            botones.Clear();
            GetComponentsInChildren(false, botones);
            elegido = -1;
            for (int i = 0; i < botones.Count; i++)
            {
                botones[i].MarcarElegido(false);
            }
            for (int i = 0; i < botones.Count; i++)
            {
                if (botones[i].IsInteractable())
                {
                    BotonCyberfix.Silencio = true;
                    try
                    {
                        botones[i].Elegir();
                        elegido = i;
                    }
                    finally
                    {
                        BotonCyberfix.Silencio = false;
                    }
                    break;
                }
            }
        }

        /// <summary>Lo llama el botón cuando queda elegido (por teclado o por ratón).</summary>
        internal void AlElegir(BotonCyberfix boton)
        {
            int i = botones.IndexOf(boton);
            if (i < 0)
            {
                return;
            }
            if (elegido >= 0 && elegido < botones.Count && elegido != i)
            {
                botones[elegido].MarcarElegido(false);
            }
            elegido = i;
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado == null || botones.Count == 0)
            {
                return;
            }
            if (teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame)
            {
                Mover(-1);
            }
            else if (teclado.downArrowKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame)
            {
                Mover(1);
            }
            else if (teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame)
            {
                Elegido?.Cambiar(-1);
            }
            else if (teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame)
            {
                Elegido?.Cambiar(1);
            }
            else if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame)
            {
                Elegido?.Pulsar();
            }
            else if (teclado.escapeKey.wasPressedThisFrame)
            {
                Sonidos.Reproducir(SonidoCancelar);
                Regresar?.Invoke();
            }
        }

        /// <summary>Elige el siguiente botón activo hacia arriba (-1) o abajo (+1). No da la vuelta.</summary>
        public void Mover(int direccion)
        {
            for (int i = elegido + direccion; i >= 0 && i < botones.Count; i += direccion)
            {
                if (botones[i].IsInteractable())
                {
                    botones[i].Elegir();
                    if (elegido != i)
                    {
                        AlElegir(botones[i]);
                    }
                    return;
                }
            }
        }
    }
}
