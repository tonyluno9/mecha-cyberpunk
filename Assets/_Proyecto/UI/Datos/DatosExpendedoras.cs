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
            [Tooltip("T-719: nombre de un objeto de la escena (un marcador) donde va la máquina. Si existe, manda: la máquina va en su posición. Vacío = no se usa.")]
            public string marcador;
            [Tooltip("T-719: lugar de Core (Casa, Bar...) de donde se mide la posición. Si está en Ninguno y no hay marcador se usa la posición fija de abajo (solo pruebas).")]
            public IdLugar lugar;
            [Tooltip("T-719: metros del mundo desde la entrada del lugar a la máquina (x al oriente, z al norte). Solo con un lugar.")]
            public Vector3 desplazamiento;
            [Tooltip("Posición fija en el mundo (m). Solo para pruebas y escenas sin lugares: las máquinas de la ciudad usan un marcador o un lugar. La y es la altura de la banqueta.")]
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

        [Header("T-1529: tiendita de la esquina (edificio ED02)")]
        [Tooltip("False: las máquinas expendedoras (y las de adorno de las escenas) ya no venden: son de decorado. Los consumibles se compran en la tiendita.")]
        [SerializeField] private bool maquinasVenden = true;

        [Tooltip("Edificio de la tiendita (ED02_TienditaEsquina_a). Vacío = no hay tiendita.")]
        [SerializeField] private GameObject tiendita;

        [Tooltip("Mostrador que va a la entrada de la tiendita (modelo existente).")]
        [SerializeField] private GameObject mostrador;

        [Tooltip("El tendero que está tras el mostrador (visual de un NPC existente).")]
        [SerializeField] private GameObject tendero;

        [Tooltip("Qué vende la tiendita (los consumibles).")]
        [SerializeField] private DatosTienda tiendaTiendita;

        [Tooltip("Sitios candidatos de la tiendita, en orden: se usa el primero que esté libre (sin otros colliders). Con 'marcador' manda el objeto de la escena con ese nombre; 'giro' es el de la fachada.")]
        [SerializeField] private List<Posicion> sitiosTiendita = new List<Posicion>();

        public bool MaquinasVenden => maquinasVenden;
        public GameObject Tiendita => tiendita;
        public GameObject Mostrador => mostrador;
        public GameObject Tendero => tendero;
        public DatosTienda TiendaTiendita => tiendaTiendita;
        public IReadOnlyList<Posicion> SitiosTiendita => sitiosTiendita;

        public GameObject Modelo => modelo;
        public PantallaTienda Pantalla => pantalla;
        public DatosTienda Tienda => tienda;
        public CatalogoObjetos Catalogo => catalogo;
        public IReadOnlyList<Posicion> Posiciones => posiciones;
    }
}
