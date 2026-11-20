using System;
using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>T-1498: una variante de dron de combate. Un número en 0 o menos significa "usa el de ConfigPersecucion".</summary>
    [Serializable]
    public class VarianteDronCombate
    {
        [SerializeField] private string nombre = "DR01";
        [Tooltip("Modelo (prefab Dron_*) que se pone en lugar del visual del dron de la policía. Vacío: se queda el visual de siempre.")]
        [SerializeField] private GameObject modelo;
        [Min(0)] [SerializeField] private int estrellasMinimas;
        [SerializeField] private float vida;
        [SerializeField] private float danio;
        [SerializeField] private float segundosEntreTiros;
        [Range(0f, 1f)] [SerializeField] private float acierto;
        [Tooltip("Altura de vuelo sobre el jugador (m). Más baja que la de la policía para que se vean desde la calle.")]
        [SerializeField] private float altura;
        [SerializeField] private float escala = 1f;
        [SerializeField] private float radioImpacto;

        public string Nombre => nombre;
        public GameObject Modelo => modelo;
        public int EstrellasMinimas => estrellasMinimas;
        public float Vida => vida;
        public float Danio => danio;
        public float SegundosEntreTiros => segundosEntreTiros;
        public float Acierto => acierto;
        public float Altura => altura;
        public float Escala => escala;
        public float RadioImpacto => radioImpacto;

        public VarianteDronCombate() { }

        public VarianteDronCombate(string nombre, GameObject modelo, int estrellasMinimas, float vida, float danio, float segundosEntreTiros, float acierto, float altura, float escala, float radioImpacto)
        {
            this.nombre = nombre;
            this.modelo = modelo;
            this.estrellasMinimas = estrellasMinimas;
            this.vida = vida;
            this.danio = danio;
            this.segundosEntreTiros = segundosEntreTiros;
            this.acierto = acierto;
            this.altura = altura;
            this.escala = escala;
            this.radioImpacto = radioImpacto;
        }
    }

    /// <summary>
    /// T-1498: las variantes de dron de combate de la persecución por estrellas (DR04 explorador, DR02 pesado, E13 de asalto) además del DR01 de la policía.
    /// Vive en Trafico/Resources/ConfigDronesCombate (lo crea CrearDronesCombateT1498); sin él, todos los drones son el DR01 de siempre.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Trafico/Config drones de combate", fileName = "ConfigDronesCombate")]
    public class ConfigDronesCombate : ScriptableObject
    {
        public const string Recurso = "ConfigDronesCombate";

        [SerializeField] private VarianteDronCombate[] variantes = new VarianteDronCombate[0];

        public IReadOnlyList<VarianteDronCombate> Variantes => variantes;

        /// <summary>Para el editor y las pruebas.</summary>
        public void Configurar(VarianteDronCombate[] nuevas) => variantes = nuevas ?? new VarianteDronCombate[0];

        /// <summary>La variante del dron número <paramref name="indice"/> con <paramref name="estrellas"/>; null si no hay ninguna elegible.</summary>
        public VarianteDronCombate Elegir(int estrellas, int indice)
        {
            int[] minimos = new int[variantes.Length];
            for (int i = 0; i < variantes.Length; i++)
            {
                minimos[i] = variantes[i] != null ? variantes[i].EstrellasMinimas : int.MaxValue;
            }
            int elegida = ReglaVarianteDron.Elegir(minimos, estrellas, indice);
            return elegida < 0 ? null : variantes[elegida];
        }
    }
}
