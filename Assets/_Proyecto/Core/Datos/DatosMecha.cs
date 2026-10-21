using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Estadísticas de un mecha. Se balancean en el asset, no en el código.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del mecha", fileName = "DatosMecha_Nuevo")]
    public class DatosMecha : ScriptableObject
    {
        [Tooltip("Nombre en pantallas (texto).")]
        [SerializeField] private string nombre = "Mecha";

        [Tooltip("Vida al empezar la pelea (puntos).")]
        [Min(1f)] [SerializeField] private float vidaMaxima = 100f;

        [Tooltip("Velocidad al caminar (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadMaxima = 4f;

        [Tooltip("Qué tan rápido arranca (m/s²).")]
        [Min(0.1f)] [SerializeField] private float aceleracion = 6f;

        [Tooltip("Qué tan rápido frena (m/s²).")]
        [Min(0.1f)] [SerializeField] private float desaceleracion = 10f;

        [Tooltip("Giro máximo del cuerpo (°/s).")]
        [Min(1f)] [SerializeField] private float velocidadGiroMaxima = 90f;

        [Tooltip("Daño de un golpe (puntos).")]
        [Min(0.1f)] [SerializeField] private float danioGolpe = 10f;

        [Tooltip("Alcance del golpe, medido desde el centro del mecha hacia adelante (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceGolpe = 3f;

        [Tooltip("Del clic (o la decisión del rival) al impacto (s). En el rival es el aviso visible.")]
        [Min(0.01f)] [SerializeField] private float preparacionGolpe = 0.25f;

        [Tooltip("Del impacto a poder golpear otra vez (s).")]
        [Min(0.01f)] [SerializeField] private float recargaGolpe = 0.8f;

        [Tooltip("Del clic derecho a que la guardia quede arriba (s).")]
        [Min(0.01f)] [SerializeField] private float tiempoSubirGuardia = 0.15f;

        [Tooltip("Parte del daño que quita un bloqueo normal (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float reduccionBloqueo = 0.7f;

        [Tooltip("Si la guardia quedó arriba hace menos que esto, no entra daño (s).")]
        [Min(0.01f)] [SerializeField] private float ventanaBloqueoPerfecto = 0.2f;

        public string Nombre => nombre;
        public float VidaMaxima => vidaMaxima;
        public float VelocidadMaxima => velocidadMaxima;
        public float Aceleracion => aceleracion;
        public float Desaceleracion => desaceleracion;
        public float VelocidadGiroMaxima => velocidadGiroMaxima;
        public float DanioGolpe => danioGolpe;
        public float AlcanceGolpe => alcanceGolpe;
        public float PreparacionGolpe => preparacionGolpe;
        public float RecargaGolpe => recargaGolpe;
        public float TiempoSubirGuardia => tiempoSubirGuardia;
        public float ReduccionBloqueo => reduccionBloqueo;
        public float VentanaBloqueoPerfecto => ventanaBloqueoPerfecto;

        /// <summary>Todos los valores actuales de este mecha, como una copia de solo lectura.</summary>
        public ValoresMecha Valores => new ValoresMecha
        {
            nombre = nombre,
            vidaMaxima = vidaMaxima,
            velocidadMaxima = velocidadMaxima,
            aceleracion = aceleracion,
            desaceleracion = desaceleracion,
            velocidadGiroMaxima = velocidadGiroMaxima,
            danioGolpe = danioGolpe,
            alcanceGolpe = alcanceGolpe,
            preparacionGolpe = preparacionGolpe,
            recargaGolpe = recargaGolpe,
            tiempoSubirGuardia = tiempoSubirGuardia,
            reduccionBloqueo = reduccionBloqueo,
            ventanaBloqueoPerfecto = ventanaBloqueoPerfecto
        };

        /// <summary>
        /// Crea un DatosMecha en memoria con los valores dados, sin tocar ningún asset. Cada valor se limita igual
        /// que en el inspector (fuera de rango queda en el límite; NaN usa el valor por defecto; un nombre vacío usa
        /// el nombre por defecto). El objeto se llama nombre + " (ejecución)" y nunca se guarda como asset
        /// (HideFlags.DontSave). Quien lo crea lo destruye con Object.Destroy cuando ya no lo usa. No reemplaza a los
        /// assets de los rivales.
        /// </summary>
        public static DatosMecha CrearEnEjecucion(ValoresMecha valores)
        {
            DatosMecha datos = CreateInstance<DatosMecha>();
            ValoresMecha d = datos.Valores;
            datos.nombre = string.IsNullOrEmpty(valores.nombre) ? d.nombre : valores.nombre;
            datos.vidaMaxima = Limitar(valores.vidaMaxima, 1f, float.PositiveInfinity, d.vidaMaxima);
            datos.velocidadMaxima = Limitar(valores.velocidadMaxima, 0.1f, float.PositiveInfinity, d.velocidadMaxima);
            datos.aceleracion = Limitar(valores.aceleracion, 0.1f, float.PositiveInfinity, d.aceleracion);
            datos.desaceleracion = Limitar(valores.desaceleracion, 0.1f, float.PositiveInfinity, d.desaceleracion);
            datos.velocidadGiroMaxima = Limitar(valores.velocidadGiroMaxima, 1f, float.PositiveInfinity, d.velocidadGiroMaxima);
            datos.danioGolpe = Limitar(valores.danioGolpe, 0.1f, float.PositiveInfinity, d.danioGolpe);
            datos.alcanceGolpe = Limitar(valores.alcanceGolpe, 0.1f, float.PositiveInfinity, d.alcanceGolpe);
            datos.preparacionGolpe = Limitar(valores.preparacionGolpe, 0.01f, float.PositiveInfinity, d.preparacionGolpe);
            datos.recargaGolpe = Limitar(valores.recargaGolpe, 0.01f, float.PositiveInfinity, d.recargaGolpe);
            datos.tiempoSubirGuardia = Limitar(valores.tiempoSubirGuardia, 0.01f, float.PositiveInfinity, d.tiempoSubirGuardia);
            datos.reduccionBloqueo = Limitar(valores.reduccionBloqueo, 0f, 1f, d.reduccionBloqueo);
            datos.ventanaBloqueoPerfecto = Limitar(valores.ventanaBloqueoPerfecto, 0.01f, float.PositiveInfinity, d.ventanaBloqueoPerfecto);
            datos.name = datos.nombre + " (ejecución)";
            datos.hideFlags = HideFlags.DontSave;
            return datos;
        }

        private static float Limitar(float valor, float minimo, float maximo, float porDefecto)
        {
            if (float.IsNaN(valor)) return porDefecto;
            return Mathf.Clamp(valor, minimo, maximo);
        }
    }
}
