using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Efectos
{
    /// <summary>
    /// T-1532: suena un grupo de IDs en 3D pasado un retraso (los escombros y cristales que siguen a una explosión grande) y, si se pide, un bucle de
    /// fuego un rato. Se crea con <see cref="Programar"/> y se destruye solo.
    /// </summary>
    public sealed class DetalleExplosionSonido : MonoBehaviour
    {
        private string[] ids;
        private float cuando;
        private string idFuego;
        private float segundosFuego;
        private int bucle = Sonidos.SinBucle;
        private float finFuego;
        private bool sonaron;

        /// <summary>Suena <paramref name="ids"/> en <paramref name="donde"/> tras <paramref name="retraso"/> segundos.</summary>
        public static DetalleExplosionSonido Programar(Vector3 donde, string[] ids, float retraso, string idFuego = null, float segundosFuego = 0f)
        {
            GameObject objeto = new GameObject("DetalleExplosionSonido");
            objeto.transform.position = donde;
            DetalleExplosionSonido d = objeto.AddComponent<DetalleExplosionSonido>();
            d.ids = ids ?? new string[0];
            d.cuando = Time.time + Mathf.Max(0f, retraso);
            d.idFuego = idFuego;
            d.segundosFuego = segundosFuego;
            return d;
        }

        private void Update()
        {
            if (!sonaron && Time.time >= cuando)
            {
                sonaron = true;
                for (int i = 0; i < ids.Length; i++)
                {
                    Sonidos.ReproducirEn(ids[i], transform.position);
                }
                if (!string.IsNullOrEmpty(idFuego) && segundosFuego > 0f)
                {
                    Sonidos.ReproducirEn(LimiteSonidoExplosion.IdFuegoEncender, transform.position);
                    bucle = Sonidos.ReproducirBucle(idFuego, transform);
                    finFuego = Time.time + segundosFuego;
                }
            }
            if (sonaron && (bucle == Sonidos.SinBucle || Time.time >= finFuego))
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucle);
                bucle = Sonidos.SinBucle;
            }
        }
    }
}
