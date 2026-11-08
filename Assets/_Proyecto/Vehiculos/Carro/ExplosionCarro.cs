using MechaCyberpunk.Vehiculos.Efectos;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// El estallido de un carro (T-591). T-745: los efectos (bola de fuego, onda, brasas, humo, chispas, mancha en el suelo y destello) ya no se dibujan
    /// por código: salen de <see cref="EfectosExplosion"/>, que usa los prefabs flipbook del Unity Particle Pack con materiales URP. Esta clase
    /// pone el estruendo y deja las partes a mano para pruebas. El empuje y el daño por radio los hace <see cref="DanioCarro"/>.
    /// </summary>
    public class ExplosionCarro : MonoBehaviour
    {
        public const float SegundosVida = 10f;

        public ParticleSystem Fuego { get; private set; }
        public ParticleSystem Humo { get; private set; }
        public ParticleSystem Chispas { get; private set; }
        public ParticleSystem Onda { get; private set; }
        public Light Luz { get; private set; }

        /// <summary>Los efectos creados (raíz y partes).</summary>
        public InstanciaExplosion Efectos { get; private set; }

        /// <summary>Crea la explosión en <paramref name="centro"/>; <paramref name="radio"/> es hasta donde llega la onda.</summary>
        public static ExplosionCarro Crear(Vector3 centro, float radio)
        {
            GameObject objeto = new GameObject("ExplosionCarro");
            objeto.transform.position = centro;
            ExplosionCarro explosion = objeto.AddComponent<ExplosionCarro>();
            explosion.Armar(centro, radio);
            return explosion;
        }

        private void Armar(Vector3 centro, float radio)
        {
            Efectos = EfectosExplosion.Explosion(centro, radio);
            Fuego = Efectos.Fuego;
            Humo = Efectos.Humo;
            Chispas = Efectos.Chispas;
            Onda = Efectos.Onda;
            Luz = Efectos.Luz;
            // T-620: el estruendo pasa por el gestor (interruptor VEH_EXPLOSION y limite de voces).
            Sonidos.ReproducirEn("VEH_EXPLOSION", centro);
            Destroy(gameObject, SegundosVida);
        }
    }
}
