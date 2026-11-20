using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1494: conducta del perro robot de Nova (E17), sobre <see cref="ReglaPerroNova"/>. Patrulla alrededor de su punto; al detectar al jugador (o al recibir
    /// daño, o si Nova lo azuza) corre hacia él, y al llegar muerde con un pequeño impulso hacia adelante y daño de cuerpo a cuerpo. Sin animaciones nuevas:
    /// usa la velocidad del Animator que ya tenga y siempre se orienta hacia donde corre. Se agrega sola a un <see cref="PandilleroAPie"/> con datos de perro robot.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ConductaPerroNova : MonoBehaviour, IConductaAPie
    {
        private const float GradosGiroCorrer = 720f;
        private const float MultiplicadorImpulso = 1.5f;
        private const float MargenMordida = 0.6f;

        private PandilleroAPie perro;
        private ReglaPerroNova regla;

        public bool Controla => true;
        public ReglaPerroNova Regla => regla;

        private void Awake()
        {
            perro = GetComponent<PandilleroAPie>();
            if (perro == null || perro.Datos == null)
            {
                Debug.LogError("[IA] Falta PandilleroAPie en ConductaPerroNova de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaPerroNova(perro.Datos.RadioDeteccion, perro.Datos.AlcanceGolpe);
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (regla == null)
            {
                return;
            }
            Transform jugador = p.Jugador;
            bool atacable = jugador != null && p.JugadorAtacable;
            Vector3 haciaJugador = atacable ? jugador.position - p.transform.position : Vector3.zero;
            haciaJugador.y = 0f;
            float distancia = atacable ? haciaJugador.magnitude : float.MaxValue;
            Vector3 dir = haciaJugador.sqrMagnitude > 0.0001f ? haciaJugador.normalized : p.transform.forward;
            ReglaPerroNova.Salida salida = regla.Avanzar(dt, new ReglaPerroNova.Entrada(distancia, p.Alerta, atacable));
            float velocidad = p.Datos.Velocidad;
            switch (regla.Actual)
            {
                case ReglaPerroNova.Fase.Persigue:
                    p.MirarHacia(dir, GradosGiroCorrer);
                    p.MoverPlano(dir * velocidad);
                    break;
                case ReglaPerroNova.Fase.Aviso:
                    p.MirarHacia(dir, GradosGiroCorrer);
                    p.MoverPlano(Vector3.zero);
                    break;
                case ReglaPerroNova.Fase.Impulso:
                    p.MirarHacia(dir, GradosGiroCorrer);
                    p.MoverPlano(dir * velocidad * MultiplicadorImpulso);
                    break;
                case ReglaPerroNova.Fase.Recupera:
                    p.MoverPlano(Vector3.zero);
                    break;
                default:
                    Vector3 paso = p.PasoDePatrulla();
                    if (paso.sqrMagnitude > 0.0001f)
                    {
                        p.MirarHacia(paso, GradosGiroCorrer);
                    }
                    p.MoverPlano(paso);
                    break;
            }
            if (salida.Mordida)
            {
                p.AnimarGolpe(false);
                if (atacable && distancia <= p.Datos.AlcanceGolpe + MargenMordida)
                {
                    p.DaniarJugador(p.Datos.DanioGolpe, dir);
                }
            }
        }
    }
}
