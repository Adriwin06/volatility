namespace Volatility;

public enum GeometryPrimitiveType : byte
{
    PointList,      // Renders the vertices as a collection of isolated points. This value is unsupported for indexed primitives. 
    LineList,       // Renders the vertices as a list of isolated straight line segments. Calls using this primitive type fail if the count is less than two or is odd. 
    LineStrip,      // Renders the vertices as a single polyline. Calls using this primitive type fail if the count is less than two. 
    LineLoop,       
    TriangleList,   // Renders the specified vertices as a sequence of isolated triangles. Each group of three vertices defines a separate triangle. Back-face culling is affected by the current winding-order render state.
    TriangleStrip,  // Renders the vertices as a triangle strip. The backface-culling flag is automatically flipped on even-numbered triangles. 
    TriangleFan,
    QuadList,
    QuadStrip,
    Polygon,
    RectList,
}
public enum CellPrimitiveType : byte
{
    CELL_GCM_PRIMITIVE_POINTS = 1,
    CELL_GCM_PRIMITIVE_LINE_STRIP = 2,
    CELL_GCM_PRIMITIVE_LINE_LOOP = 3,
    CELL_GCM_PRIMITIVE_LINES = 4,
    CELL_GCM_PRIMITIVE_TRIANGLES = 5,
    CELL_GCM_PRIMITIVE_TRIANGLE_STRIP = 6,
    CELL_GCM_PRIMITIVE_TRIANGLE_FAN = 7,
    CELL_GCM_PRIMITIVE_QUADS = 8,
    CELL_GCM_PRIMITIVE_QUAD_STRIP = 9,
    CELL_GCM_PRIMITIVE_POLYGON = 10,
}

public enum D3DPRIMITIVETYPE : UInt32
{
    D3DPT_POINTLIST = 1,
    D3DPT_LINELIST = 2,
    D3DPT_LINESTRIP = 3,
    D3DPT_TRIANGLELIST = 4,
    D3DPT_TRIANGLEFAN = 5,
    D3DPT_TRIANGLESTRIP = 6,
    D3DPT_RECTLIST = 8,
    D3DPT_QUADLIST = 13,
}

public enum D3D11_PRIMITIVE_TOPOLOGY : UInt32
{
    UNDEFINED = 0,
    POINTLIST = 1,
    LINELIST = 2,
    LINESTRIP = 3,
    TRIANGLELIST = 4,
    TRIANGLESTRIP = 5,
    LINELIST_ADJ = 10,
    LINESTRIP_ADJ = 11,
    TRIANGLELIST_ADJ = 12,
    TRIANGLESTRIP_ADJ = 13,
    CONTROL_POINT_PATCHLIST_1 = 33,
    CONTROL_POINT_PATCHLIST_2 = 34,
    CONTROL_POINT_PATCHLIST_3 = 35,
    CONTROL_POINT_PATCHLIST_4 = 36,
    CONTROL_POINT_PATCHLIST_5 = 37,
    CONTROL_POINT_PATCHLIST_6 = 38,
    CONTROL_POINT_PATCHLIST_7 = 39,
    CONTROL_POINT_PATCHLIST_8 = 40,
    CONTROL_POINT_PATCHLIST_9 = 41,
    CONTROL_POINT_PATCHLIST_10 = 42,
    CONTROL_POINT_PATCHLIST_11 = 43,
    CONTROL_POINT_PATCHLIST_12 = 44,
    CONTROL_POINT_PATCHLIST_13 = 45,
    CONTROL_POINT_PATCHLIST_14 = 46,
    CONTROL_POINT_PATCHLIST_15 = 47,
    CONTROL_POINT_PATCHLIST_16 = 48,
    CONTROL_POINT_PATCHLIST_17 = 49,
    CONTROL_POINT_PATCHLIST_18 = 50,
    CONTROL_POINT_PATCHLIST_19 = 51,
    CONTROL_POINT_PATCHLIST_20 = 52,
    CONTROL_POINT_PATCHLIST_21 = 53,
    CONTROL_POINT_PATCHLIST_22 = 54,
    CONTROL_POINT_PATCHLIST_23 = 55,
    CONTROL_POINT_PATCHLIST_24 = 56,
    CONTROL_POINT_PATCHLIST_25 = 57,
    CONTROL_POINT_PATCHLIST_26 = 58,
    CONTROL_POINT_PATCHLIST_27 = 59,
    CONTROL_POINT_PATCHLIST_28 = 60,
    CONTROL_POINT_PATCHLIST_29 = 61,
    CONTROL_POINT_PATCHLIST_30 = 62,
    CONTROL_POINT_PATCHLIST_31 = 63,
    CONTROL_POINT_PATCHLIST_32 = 64
}


public static class GeometryPrimitiveTypeConverter
{
    public static GeometryPrimitiveType ToKind(this CellPrimitiveType v) => v switch
    {
        CellPrimitiveType.CELL_GCM_PRIMITIVE_POINTS => GeometryPrimitiveType.PointList,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_LINES => GeometryPrimitiveType.LineList,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_LINE_STRIP => GeometryPrimitiveType.LineStrip,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_LINE_LOOP => GeometryPrimitiveType.LineLoop,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_TRIANGLES => GeometryPrimitiveType.TriangleList,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_TRIANGLE_STRIP => GeometryPrimitiveType.TriangleStrip,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_TRIANGLE_FAN => GeometryPrimitiveType.TriangleFan,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_QUADS => GeometryPrimitiveType.QuadList,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_QUAD_STRIP => GeometryPrimitiveType.QuadStrip,
        CellPrimitiveType.CELL_GCM_PRIMITIVE_POLYGON => GeometryPrimitiveType.Polygon,
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };

    public static GeometryPrimitiveType ToKind(this D3DPRIMITIVETYPE v) => v switch
    {
        D3DPRIMITIVETYPE.D3DPT_POINTLIST => GeometryPrimitiveType.PointList,
        D3DPRIMITIVETYPE.D3DPT_LINELIST => GeometryPrimitiveType.LineList,
        D3DPRIMITIVETYPE.D3DPT_LINESTRIP => GeometryPrimitiveType.LineStrip,
        D3DPRIMITIVETYPE.D3DPT_TRIANGLELIST => GeometryPrimitiveType.TriangleList,
        D3DPRIMITIVETYPE.D3DPT_TRIANGLESTRIP => GeometryPrimitiveType.TriangleStrip,
        D3DPRIMITIVETYPE.D3DPT_TRIANGLEFAN => GeometryPrimitiveType.TriangleFan,
        D3DPRIMITIVETYPE.D3DPT_RECTLIST => GeometryPrimitiveType.RectList,
        D3DPRIMITIVETYPE.D3DPT_QUADLIST => GeometryPrimitiveType.QuadList,
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };

    public static GeometryPrimitiveType ToKind(this D3D11_PRIMITIVE_TOPOLOGY v) => v switch
    {
        D3D11_PRIMITIVE_TOPOLOGY.POINTLIST => GeometryPrimitiveType.PointList,
        D3D11_PRIMITIVE_TOPOLOGY.LINELIST => GeometryPrimitiveType.LineList,
        D3D11_PRIMITIVE_TOPOLOGY.LINESTRIP => GeometryPrimitiveType.LineStrip,
        D3D11_PRIMITIVE_TOPOLOGY.TRIANGLELIST => GeometryPrimitiveType.TriangleList,
        D3D11_PRIMITIVE_TOPOLOGY.TRIANGLESTRIP => GeometryPrimitiveType.TriangleStrip,
        _ => throw new NotSupportedException($"No Volatility equivalent to {v}"),
    };

    public static CellPrimitiveType ToCell(this GeometryPrimitiveType k) => k switch
    {
        GeometryPrimitiveType.PointList => CellPrimitiveType.CELL_GCM_PRIMITIVE_POINTS,
        GeometryPrimitiveType.LineList => CellPrimitiveType.CELL_GCM_PRIMITIVE_LINES,
        GeometryPrimitiveType.LineStrip => CellPrimitiveType.CELL_GCM_PRIMITIVE_LINE_STRIP,
        GeometryPrimitiveType.LineLoop => CellPrimitiveType.CELL_GCM_PRIMITIVE_LINE_LOOP,
        GeometryPrimitiveType.TriangleList => CellPrimitiveType.CELL_GCM_PRIMITIVE_TRIANGLES,
        GeometryPrimitiveType.TriangleStrip => CellPrimitiveType.CELL_GCM_PRIMITIVE_TRIANGLE_STRIP,
        GeometryPrimitiveType.TriangleFan => CellPrimitiveType.CELL_GCM_PRIMITIVE_TRIANGLE_FAN,
        GeometryPrimitiveType.QuadList => CellPrimitiveType.CELL_GCM_PRIMITIVE_QUADS,
        GeometryPrimitiveType.QuadStrip => CellPrimitiveType.CELL_GCM_PRIMITIVE_QUAD_STRIP,
        GeometryPrimitiveType.Polygon => CellPrimitiveType.CELL_GCM_PRIMITIVE_POLYGON,
        _ => throw new NotSupportedException($"No Cell equivalent to {k}"),
    };

    public static D3DPRIMITIVETYPE ToD3D9(this GeometryPrimitiveType k) => k switch
    {
        GeometryPrimitiveType.PointList => D3DPRIMITIVETYPE.D3DPT_POINTLIST,
        GeometryPrimitiveType.LineList => D3DPRIMITIVETYPE.D3DPT_LINELIST,
        GeometryPrimitiveType.LineStrip => D3DPRIMITIVETYPE.D3DPT_LINESTRIP,
        GeometryPrimitiveType.TriangleList => D3DPRIMITIVETYPE.D3DPT_TRIANGLELIST,
        GeometryPrimitiveType.TriangleStrip => D3DPRIMITIVETYPE.D3DPT_TRIANGLESTRIP,
        GeometryPrimitiveType.TriangleFan => D3DPRIMITIVETYPE.D3DPT_TRIANGLEFAN,
        GeometryPrimitiveType.RectList => D3DPRIMITIVETYPE.D3DPT_RECTLIST,
        GeometryPrimitiveType.QuadList => D3DPRIMITIVETYPE.D3DPT_QUADLIST,
        _ => throw new NotSupportedException($"No D3D9 equivalent to {k}"),
    };

    public static D3D11_PRIMITIVE_TOPOLOGY ToD3D11(this GeometryPrimitiveType k) => k switch
    {
        GeometryPrimitiveType.PointList => D3D11_PRIMITIVE_TOPOLOGY.POINTLIST,
        GeometryPrimitiveType.LineList => D3D11_PRIMITIVE_TOPOLOGY.LINELIST,
        GeometryPrimitiveType.LineStrip => D3D11_PRIMITIVE_TOPOLOGY.LINESTRIP,
        GeometryPrimitiveType.TriangleList => D3D11_PRIMITIVE_TOPOLOGY.TRIANGLELIST,
        GeometryPrimitiveType.TriangleStrip => D3D11_PRIMITIVE_TOPOLOGY.TRIANGLESTRIP,
        _ => throw new NotSupportedException($"No D3D11 equivalent to {k}"),
    };

    public static D3D11_PRIMITIVE_TOPOLOGY ToD3D11(this CellPrimitiveType v) => v.ToKind().ToD3D11();
    public static D3DPRIMITIVETYPE ToD3D9(this CellPrimitiveType v) => v.ToKind().ToD3D9();
    public static CellPrimitiveType ToCell(this D3DPRIMITIVETYPE v) => v.ToKind().ToCell();
    public static CellPrimitiveType ToCell(this D3D11_PRIMITIVE_TOPOLOGY v) => v.ToKind().ToCell();
}
