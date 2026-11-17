using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>
    /// T-1018: las prótesis de ataque que están en el inventario se ven en el cuerpo: el brazo láser en el antebrazo derecho y la hombrera de misiles en el hombro izquierdo.
    /// El modelo (<see cref="DatosProtesisAtaque.Modelo"/>) cuelga del hueso del Animator Humanoid, así que sigue la animación. Al comprarla aparece sin reiniciar; sin ella no hay modelo activo.
    /// Del modelo salen los puntos de disparo: <see cref="PuntaLaser"/> (el rayo) y <see cref="SalidaMisiles"/> (los misiles) los usa <see cref="ProtesisAtaqueAPie"/>.
    /// Lo agrega <see cref="HabilidadesProtesis"/> junto a <see cref="ProtesisAtaqueAPie"/>.
    /// </summary>
    public class ProtesisVisiblesAPie : MonoBehaviour
    {
        private const string NombrePunta = "Punta";

        private Animator animador;
        private GameObject laser;
        private GameObject misiles;
        private Transform puntaLaser;
        private Transform salidaMisiles;
        private bool buscoPiezas;

        /// <summary>El modelo del brazo láser si está puesto y activo; si no, null.</summary>
        public GameObject ModeloLaser => laser != null && laser.activeSelf ? laser : null;

        /// <summary>El modelo de la hombrera de misiles si está puesto y activo; si no, null.</summary>
        public GameObject ModeloMisiles => misiles != null && misiles.activeSelf ? misiles : null;

        /// <summary>De dónde sale el rayo (la punta del brazo láser), o null si no hay brazo puesto.</summary>
        public Transform PuntaLaser => ModeloLaser != null ? puntaLaser : null;

        /// <summary>De dónde salen los misiles (la hombrera), o null si no hay hombrera puesta.</summary>
        public Transform SalidaMisiles => ModeloMisiles != null ? salidaMisiles : null;

        private void Update()
        {
            CatalogoProtesisAtaque catalogo = CatalogoProtesisAtaque.Actual;
            if (catalogo == null)
            {
                return;
            }
            if (animador == null)
            {
                animador = GetComponentInChildren<Animator>(true);
                if (animador == null || !animador.isHuman)
                {
                    return;
                }
            }
            if (!buscoPiezas)
            {
                // T-1311: si el cuerpo de PJ01 trae las piezas de PJ01_Implantes_Ataque.fbx se usan esas (y sus vacíos de salida) en vez del prefab de T-1018.
                buscoPiezas = true;
                PiezasProtesisAtaquePJ01 piezas = animador.GetComponentInChildren<PiezasProtesisAtaquePJ01>(true);
                if (piezas != null && piezas.Laser != null && piezas.Misiles != null)
                {
                    laser = piezas.Laser;
                    puntaLaser = piezas.SalidaLaser;
                    misiles = piezas.Misiles;
                    salidaMisiles = piezas.SalidaMisiles;
                }
            }
            var inventario = Registro<MechaCyberpunk.Economia.ServicioInventario>.Actual;
            Actualizar(catalogo, inventario, HabilidadProtesis.Laser, HumanBodyBones.RightLowerArm, ref laser, ref puntaLaser);
            Actualizar(catalogo, inventario, HabilidadProtesis.Misiles, HumanBodyBones.Chest, ref misiles, ref salidaMisiles);
        }

        private void Actualizar(CatalogoProtesisAtaque catalogo, MechaCyberpunk.Economia.ServicioInventario inventario, HabilidadProtesis habilidad, HumanBodyBones hueso, ref GameObject instancia, ref Transform punta)
        {
            bool tiene = catalogo.Tiene(habilidad, inventario);
            if (instancia == null)
            {
                DatosProtesisAtaque datos = catalogo.De(habilidad);
                Transform huesoT = animador.GetBoneTransform(hueso);
                if (!tiene || datos == null || datos.Modelo == null || huesoT == null)
                {
                    return;
                }
                instancia = Instantiate(datos.Modelo, huesoT, false);
                instancia.name = datos.Modelo.name;
                punta = instancia.transform.Find(NombrePunta);
                foreach (Collider c in instancia.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(c);
                }
            }
            if (instancia.activeSelf != tiene)
            {
                instancia.SetActive(tiene);
            }
        }
    }
}
