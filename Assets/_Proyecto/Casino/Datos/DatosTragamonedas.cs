using System;
using System.Collections.Generic;
using MechaCyberpunk.Casino.Tragamonedas;
using UnityEngine;

namespace MechaCyberpunk.Casino.Datos
{
    /// <summary>Un símbolo de la tragamonedas: su nombre, qué tan seguido sale y cuánto paga con tres iguales.</summary>
    [Serializable]
    public struct SimboloTragamonedas
    {
        [Tooltip("Nombre del símbolo, como se muestra en el carrete.")]
        public string nombre;
        [Min(1)] [Tooltip("Peso al sacar el símbolo; la probabilidad es peso entre la suma de pesos (puntos).")]
        public int peso;
        [Min(0)] [Tooltip("Veces la apuesta que paga con tres iguales (×).")]
        public int multiplicadorTres;
    }

    /// <summary>Los números de la tragamonedas del casino (T-300): símbolos, pagos y apuesta. Sin números en el código.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de tragamonedas", fileName = "DatosTragamonedas_Nuevo")]
    public class DatosTragamonedas : ScriptableObject
    {
        [Tooltip("Símbolos de los carretes.")]
        [SerializeField] private List<SimboloTragamonedas> simbolos = new List<SimboloTragamonedas>();
        [Tooltip("Índice del símbolo que paga con exactamente dos iguales (las cerezas); -1 si ninguno.")]
        [SerializeField] private int simboloDosIguales = -1;
        [Min(0)] [Tooltip("Veces la apuesta que paga con dos iguales del símbolo parcial (×).")]
        [SerializeField] private int multiplicadorDosIguales = 1;
        [Min(1)] [Tooltip("Apuesta de cada giro (dinero o fichas).")]
        [SerializeField] private int apuesta = 50;

        public IReadOnlyList<SimboloTragamonedas> Simbolos => simbolos;
        public int SimboloDosIguales => simboloDosIguales;
        public int MultiplicadorDosIguales => multiplicadorDosIguales;
        public int Apuesta => apuesta;

        /// <summary>Arma las reglas con estos datos.</summary>
        public CalculoTragamonedas Crear()
        {
            int[] pesos = new int[simbolos.Count];
            int[] multiplicadores = new int[simbolos.Count];
            for (int i = 0; i < simbolos.Count; i++)
            {
                pesos[i] = simbolos[i].peso;
                multiplicadores[i] = simbolos[i].multiplicadorTres;
            }
            return new CalculoTragamonedas(pesos, multiplicadores, simboloDosIguales, multiplicadorDosIguales);
        }

        /// <summary>Para crear los datos desde código (editor y pruebas).</summary>
        public void Fijar(IEnumerable<SimboloTragamonedas> nuevosSimbolos, int simboloParcial, int multiplicadorParcial, int nuevaApuesta)
        {
            simbolos = new List<SimboloTragamonedas>(nuevosSimbolos);
            simboloDosIguales = simboloParcial;
            multiplicadorDosIguales = multiplicadorParcial;
            apuesta = nuevaApuesta;
        }
    }
}
