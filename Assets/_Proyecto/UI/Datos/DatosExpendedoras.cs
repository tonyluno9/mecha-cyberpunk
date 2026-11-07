using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;

namespace MechaCyberpunk.UI.Datos
{
    /// <summary>
    /// Dónde van las máquinas expendedoras (T-685) y con qué. <see cref="ColocadorExpendedoras"/> las pone en tiempo de
    /// ejecución al cargar la escena, así las escenas de la ciudad no se editan. Cada posición es de calle (banqueta libre).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de expendedoras", fileName = "DatosExpendedoras_Nuevo")]
    public class DatosExpendedoras : ScriptableObject
    {
        [Serializable]
        public struct Posicion
        {
            [Tooltip("Nombre de la escena donde va (texto, sin .unity).")]
            public string escena;
            [Tooltip("Para qué sirve este lugar (texto), por ejemplo 'frente a la casa'.")]
            public string nota;
            [Tooltip("Posición en el mundo (m). La y es la altura de la banqueta.")]
            public Vector3 posicion;
            [Tooltip("Giro en y (°): giro del objeto; el frente del modelo CY06 mira a -Z, así que es el giro de la calle más 180°.")]
            public float giro;
        }

        [Tooltip("Modelo de la máquina (Assets/Arte).")]
        [SerializeField] private GameObject modelo;

        [Tooltip("Pantalla de tienda que se abre al comprar (prefab).")]
        [SerializeField] private PantallaTienda pantalla;

        [Tooltip("Qué vende.")]
        [SerializeField] private DatosTienda tienda;

        [Tooltip("Catálogo con los objetos que vende (para ligar oferta y objeto).")]
        [SerializeField] private CatalogoObjetos catalogo;

        [Tooltip("Lugares donde se coloca una máquina.")]
        [SerializeField] private List<Posicion> posiciones = new List<Posicion>();

        public GameObject Modelo => modelo;
        public PantallaTienda Pantalla => pantalla;
        public DatosTienda Tienda => tienda;
        public CatalogoObjetos Catalogo => catalogo;
        public IReadOnlyList<Posicion> Posiciones => posiciones;
    }
}
