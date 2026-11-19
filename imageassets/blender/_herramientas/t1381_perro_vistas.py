import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1381_lib as L, bpy
out = sys.argv[sys.argv.index('--') + 1]
L.reset(); me, _ = L.importar("E17_PerroRobotNova_a")
mn, mx = L.bbox(me); print("BB", mn, mx)
sc = L.escena_render(500)
c = ((mn.x+mx.x)/2, (mn.y+mx.y)/2, (mn.z+mx.z)/2)
for n, d in (('x', (1, 0, 0)), ('mx', (-1, 0, 0)), ('y', (0, -1, 0)), ('top', (0, 0.001, 1))):
    L.camara(sc, c, 1.2, d); L.renderizar(sc, os.path.join(out, "perro_%s.png" % n))
