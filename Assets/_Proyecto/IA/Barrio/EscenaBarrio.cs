using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T12 y T15): el guion de una escena del barrio. Las líneas salen con subtítulo aunque no haya audio; Tony graba las voces
    /// (docs/dialogos-barrio.md) y solo las asigna en <see cref="LineaBarrio"/>. Los assets viven en IA/Resources/Barrio/.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Escena del barrio", fileName = "EscenaBarrio_Nueva")]
    public class EscenaBarrio : ScriptableObject
    {
        [Tooltip("Identificador corto sin espacios (se usa para forzar la escena).")]
        [SerializeField] private string id = string.Empty;
        [SerializeField] private TipoEscenaBarrio tipo;
        [Tooltip("Pandilla que abusa (solo bullying): Calaveras (SKULLS) o Viboras (SNAKES).")]
        [SerializeField] private IdPandilla pandilla = IdPandilla.Calaveras;
        [Tooltip("Cuántos pandilleros participan (1 o 2). Si solo hay uno libre, hace los dos papeles.")]
        [Range(1, 2)] [SerializeField] private int agresores = 2;
        [Tooltip("Quién es la víctima o qué pasa, para la documentación.")]
        [SerializeField] private string descripcion = string.Empty;
        [Tooltip("Probabilidad relativa de salir entre las demás.")]
        [Min(0.1f)] [SerializeField] private float peso = 1f;
        [Tooltip("Bullying: los pandilleros se ríen y regresan a su lugar al terminar.")]
        [SerializeField] private bool rieAlFinal = true;
        [SerializeField] private LineaBarrio[] lineas = Array.Empty<LineaBarrio>();

        public string Id => id;
        public TipoEscenaBarrio Tipo => tipo;
        public IdPandilla Pandilla => pandilla;
        public int Agresores => agresores;
        public string Descripcion => descripcion;
        public float Peso => peso;
        public bool RieAlFinal => rieAlFinal;
        public LineaBarrio[] Lineas => lineas;

        /// <summary>Ruta del audio que Tony debe grabar para la línea <paramref name="indice"/> (1 = primera).</summary>
        public string ArchivoAudioEsperado(int indice) => "Assets/Arte/Audio/19_Voces/Barrio/" + id + "_" + indice.ToString("00") + ".wav";

        /// <summary>Lo usa el creador de assets del editor.</summary>
        public void Definir(string nuevoId, TipoEscenaBarrio nuevoTipo, IdPandilla nuevaPandilla, int nuevosAgresores, string nuevaDescripcion, bool nuevoRie, LineaBarrio[] nuevasLineas)
        {
            id = nuevoId;
            tipo = nuevoTipo;
            pandilla = nuevaPandilla;
            agresores = Mathf.Clamp(nuevosAgresores, 1, 2);
            descripcion = nuevaDescripcion;
            rieAlFinal = nuevoRie;
            lineas = nuevasLineas ?? Array.Empty<LineaBarrio>();
        }
    }
}
