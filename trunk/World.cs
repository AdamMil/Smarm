using System;
using System.Collections;
using System.IO;

namespace Smarm
{

class Polygon
{ public Polygon() { }
  public Polygon(List list) { Load(list); }
  
  void Load(List list)
  {
  }
}

class World : IDisposable
{ ~World() { Dispose(); }

  public bool ChangedSinceSave { get { return changed; } }

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
        { int z = (int)(double)list.First;
          if(z>layers.Length)
          { Layer[] narr = new Layer[z];
            Array.Copy(narr, layers, layers.Length);
            layers = narr;
          }
          layers[z] = new Layer(list);
        }
        else if(list.Name=="polygon") polygons.Add(new Polygon(list));
      }
      changed=false;
    }
    finally { fs.Close(); }
  }

  void Clear()
  { if(layers!=null)
    { foreach(Layer layer in layers) if(layer!=null) layer.Dispose();
      layers=null;
      changed=true;
    }
  }
  
  public void Dispose()
  { Clear();
    GC.SuppressFinalize(this);
  }

  ArrayList polygons = new ArrayList();
  Layer[] layers;
  bool changed;
}

} // namespace Smarm