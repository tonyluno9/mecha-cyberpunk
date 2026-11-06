using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// Presentación de los huecos de bala (T-808): materiales de concreto (D01) y metal (D02), cuántos caben, cuánto duran y cómo se reconoce un
    /// carro. Va en Personaje/Datos/Resources/ConfigHuecosBala.asset: <see cref="Combate.HuecosBala"/> la carga solo al arrancar.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config huecos de bala", fileName = "ConfigHuecosBala")]
    public class ConfigHuecosBala : ScriptableObject
    {
        [Tooltip("Hueco en pared, piso o mueble (D01).")]
        [SerializeField] private Material materialConcreto;
        [Tooltip("Hueco en un carro u otra cosa de metal (D02).")]
        [SerializeField] private Material materialMetal;
        [Tooltip("Huecos a la vez; el más viejo se reusa (huecos).")]
        [Min(1)] [SerializeField] private int maximoHuecos = 48;
        [Tooltip("Lo que se ve entero antes de apagarse (s).")]
        [Min(0f)] [SerializeField] private float segundosVisible = 14f;
        [Tooltip("Lo que tarda en apagarse (s).")]
        [Min(0.01f)] [SerializeField] private float segundosApagado = 2f;
        [Tooltip("Lado del hueco en concreto (m).")]
        [Min(0.01f)] [SerializeField] private float tamanoConcreto = 0.24f;
        [Tooltip("Lado del hueco en metal (m).")]
        [Min(0.01f)] [SerializeField] private float tamanoMetal = 0.2f;
        [Tooltip("Separación de la superficie para que no parpadee (m).")]
        [Min(0f)] [SerializeField] private float separacion = 0.006f;
        [Tooltip("Distancia del rayo que busca la superficie en el punto del impacto (m).")]
        [Min(0.01f)] [SerializeField] private float sondeo = 0.15f;
        [Tooltip("Si el nombre del objeto (o de sus padres) o de su material lleva una de estas palabras, es metal (sin distinguir mayúsculas).")]
        [SerializeField] private string[] palabrasMetal = { "carro", "metal", "patrulla", "auto", "vehiculo", "taxi", "van", "pickup", "coche", "camion" };

        public Material MaterialConcreto => materialConcreto;
        public Material MaterialMetal => materialMetal;
        public int MaximoHuecos => maximoHuecos;
        public float SegundosVisible => segundosVisible;
        public float SegundosApagado => segundosApagado;
        public float TamanoConcreto => tamanoConcreto;
        public float TamanoMetal => tamanoMetal;
        public float Separacion => separacion;
        public float Sondeo => sondeo;
        public string[] PalabrasMetal => palabrasMetal;
    }
}
