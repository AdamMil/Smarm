using System;
using System.Collections;
using System.Drawing;
using System.IO;

namespace Smarm
{

class Polygon
{ public Polygon() { points = new Point[0]; }
  public Polygon(List list) { Load(list); }
  
  public Point[] Points { get { return points; } }
  public string  Type   { get { return type; } }

  public void AddPoint(Point pt)
  { Point[] narr = new Point[points.Length+1];
    Array.Copy(points, narr, points.Length);
    narr[points.Length] = pt;
    points = narr;
  }

  public void RemoveLastPoint()
  { Point[] narr = new Point[points.Length-1];
    Array.Copy(points, narr, narr.Length);
    points = narr;
  }

  void Load(List list)
  { type = list["type"].GetString(0);

    List pts = list["points"];
    points = new Point[pts.Length];
    for(int i=0; i<pts.Length; i++)
    { List point = pts.GetList(i);
      points[i] = new Point(point.GetInt(0), point.GetInt(1));
    }
  }

  Point[] points;
  string  type;
}

class World : IDisposable
{ ~World() { Dispose(); }

  public bool ChangedSinceSave { get { return changed; } }

  public int Height
  { get
    { int height=0;
      foreach(Layer layer in layers) if(layer!=null && layer.Height>height) height=layer.Height;
      return height;
    }
  }

  public int Width
  { get
    { int width=0;
      foreach(Layer layer in layers) if(layer!=null && layer.Width>width) width=layer.Width;
      return width;
    }
  }

  public IList Polygons { get { return polygons; } }

  public void Clear()
  { foreach(Layer layer in layers) if(layer!=null) layer.Dispose();
    layers=new Layer[0];
    changed=false;
  }

  public void Dispose()
  { Clear();
    GC.SuppressFinalize(this);
  }

  public void Load(string directory)
  { if(!Directory.Exists(directory))
      throw new DirectoryNotFoundException(string.Format("Directory '{0}' not found", directory));

    string path = directory;
    path.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';
    FileStream fs = File.Open(path+"definition", FileMode.Open);

    try
    { Clear();
      foreach(List list in new List(fs))
      { if(list.Name=="layer")
        { int z = list.GetInt(0);
          if(z+1>layers.Length)
          { Layer[] narr = new Layer[z+1];
            Array.Copy(layers, narr, layers.Length);
            layers = narr;
          }
          layers[z] = new Layer(list, path);
        }
        else if(list.Name=="polygon") polygons.Add(new Polygon(list));
      }
    }
    finally { fs.Close(); }
  }

  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect)
  { foreach(Layer layer in layers) if(layer!=null) layer.Render(dest, sx, sy, drect);
  }

  ArrayList polygons = new ArrayList();
  Layer[] layers = new Layer[0];
  bool changed;
}

} // namespace Smarm