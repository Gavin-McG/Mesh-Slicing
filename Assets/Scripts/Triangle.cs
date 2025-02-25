
public struct Triangle
{
    public int p1, p2, p3;
    public int subMesh;

    public Triangle(int p1, int p2, int p3, int subMesh)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
        this.subMesh = subMesh;
    }

    public void RotateCW()
    {
        int t = p1;
        p1 = p3;
        p3 = p2;
        p2 = t;
    }

    public void RotateCCW()
    {
        int t = p1;
        p1 = p2;
        p2 = p3;
        p3 = t;
    }

    public override string ToString()
    {
        return "(" + p1 + ", " + p2 + ", " + p3 + ")";
    }
}
