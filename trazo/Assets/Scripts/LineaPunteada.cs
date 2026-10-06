using System.Collections.Generic;
using UnityEngine;

// Una raya punteada semitransparente entre dos puntos (- - - - -), siempre de frente a ti.
// Une tu dedo índice con el muñeco que controlas: así se entiende que eres tú quien lo mueve.
public class LineaPunteada
{
    const float Raya = 0.012f;    // largo de cada rayita (metros)
    const float Hueco = 0.016f;   // espacio entre rayitas
    const float Ancho = 0.003f;
    static readonly Color ColorLinea = new Color(0.2f, 0.5f, 0.95f, 0.6f);

    readonly GameObject go;
    readonly Mesh malla;
    readonly Material material;
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();

    // Los puntos van en el mundo: el objeto queda en el origen, sin girar.
    public LineaPunteada()
    {
        go = new GameObject("LineaPunteada");
        malla = new Mesh { name = "LineaPunteada" };
        malla.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        var control = ControlManos.Instancia;
        // Transparente (el mismo material de los íconos de sonido); si no está, uno normal.
        Material baseMat = control != null ? (control.materialIconos != null ? control.materialIconos : control.materialNodo) : null;
        if (baseMat != null)
        {
            material = new Material(baseMat);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", ColorLinea);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", ColorLinea);
            r.sharedMaterial = material;
        }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        go.SetActive(false);
    }

    // De "a" a "b", de frente a "ojo" (la cabeza).
    public void Poner(Vector3 a, Vector3 b, Vector3 ojo)
    {
        if (go == null)
            return;
        Vector3 d = b - a;
        float largo = d.magnitude;
        if (largo < 0.01f)
        {
            Ocultar();
            return;
        }
        if (!go.activeSelf)
            go.SetActive(true);
        Vector3 dir = d / largo;
        verts.Clear();
        tris.Clear();
        for (float s = 0f; s < largo; s += Raya + Hueco)
        {
            Vector3 p0 = a + dir * s;
            Vector3 p1 = a + dir * Mathf.Min(largo, s + Raya);
            Vector3 medio = (p0 + p1) * 0.5f;
            Vector3 lado = Vector3.Cross(dir, ojo - medio);
            if (lado.sqrMagnitude < 1e-10f)
                lado = Vector3.Cross(dir, Vector3.up);
            lado = lado.normalized * (Ancho * 0.5f);
            int k = verts.Count;
            verts.Add(p0 - lado);
            verts.Add(p0 + lado);
            verts.Add(p1 + lado);
            verts.Add(p1 - lado);
            tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
            tris.Add(k); tris.Add(k + 2); tris.Add(k + 3);
            tris.Add(k); tris.Add(k + 2); tris.Add(k + 1); // por los dos lados
            tris.Add(k); tris.Add(k + 3); tris.Add(k + 2);
        }
        malla.Clear();
        malla.SetVertices(verts);
        malla.SetTriangles(tris, 0);
        malla.RecalculateBounds();
    }

    public void Ocultar()
    {
        if (go != null && go.activeSelf)
            go.SetActive(false);
    }

    public void Destruir()
    {
        if (go != null)
            Object.Destroy(go);
        if (malla != null)
            Object.Destroy(malla);
        if (material != null)
            Object.Destroy(material);
    }
}
