using System;
using System.Collections;
using System.Drawing;
using GameLib.Video;

namespace Smarm
{

class Layer : IDisposable
{ public Layer() { }
  public Layer(List list) { Load(list); }

  ~Layer() { Dispose(); }
  public void Dispose()
  { for(int x=0; x<width; x++)
      for(int y=0; y<height; y++)
        if(surfaces[x, y]!=null) { surfaces[x, y].Dispose(); surfaces[x, y]=null; }
    GC.SuppressFinalize(this);
  }

  public int Width  { get { return width*PartWidth;   } }
  public int Height { get { return height*PartHeight; } }

  void Load(List list)
  {
  }

  const int PartWidth=128, PartHeight=64;

  ArrayList objects = new ArrayList();
  Surface[,] surfaces = new Surface[32, 32];
  int width, height;
}

} // namespace Smarm