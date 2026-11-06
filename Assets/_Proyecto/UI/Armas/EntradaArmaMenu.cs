using System;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// Un arma en el banco de Tino (T-814): su nombre, el ícono holográfico de la lista (H01-H04) y cómo se ve en el holograma. Con
    /// <see cref="Modelo"/> se gira el modelo 3D (W01 de la pistola); sin él se gira la <see cref="Imagen"/> en un quad.
    /// </summary>
    [Serializable]
    public class EntradaArmaMenu
    {
        [SerializeField] private ArmaAPie arma = ArmaAPie.Pistola;
        [Tooltip("Nombre en la lista y el título (PISTOLA).")]
        [SerializeField] private string nombre = "PISTOLA";
        [Tooltip("Ícono holográfico de la lista (H01-H04, fondo negro: se dibuja aditivo).")]
        [SerializeField] private Texture2D icono;
        [Tooltip("Modelo 3D del arma (prefab con sus piezas). Vacío: se usa la imagen.")]
        [SerializeField] private GameObject modelo;
        [Tooltip("Imagen holográfica para el holograma cuando no hay modelo 3D. Vacía: el ícono.")]
        [SerializeField] private Texture2D imagen;
        [Tooltip("Nombre del objeto del modelo que es el cargador (se separa al elegirlo).")]
        [SerializeField] private string piezaCargador = "Bateria";
        [Tooltip("Nombre del objeto del modelo que es el cañón (se separa al elegirlo).")]
        [SerializeField] private string piezaCanon = "Corredera";
        [Tooltip("Nombre del objeto del modelo que es la mira. Vacío: el banco arma una mira holográfica encima del cañón.")]
        [SerializeField] private string piezaMira = "";

        public ArmaAPie Arma => arma;
        public string Nombre => nombre;
        public Texture2D Icono => icono;
        public GameObject Modelo => modelo;
        public Texture2D Imagen => imagen != null ? imagen : icono;

        /// <summary>El nombre del objeto del modelo que es esa pieza (vacío si no hay).</summary>
        public string NombrePieza(PiezaArma pieza)
        {
            switch (pieza)
            {
                case PiezaArma.Cargador: return piezaCargador;
                case PiezaArma.Canon: return piezaCanon;
                default: return piezaMira;
            }
        }

        public static EntradaArmaMenu Crear(ArmaAPie arma, string nombre, Texture2D icono, GameObject modelo, Texture2D imagen)
        {
            return new EntradaArmaMenu { arma = arma, nombre = nombre, icono = icono, modelo = modelo, imagen = imagen };
        }
    }
}
