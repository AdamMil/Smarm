/*
Smarm is an editor for the game Swarm, which was written by Jim Crawford. 
http://www.adammil.net
Copyright (C) 2003-2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Smarm
{

class World : IDisposable
{ public World() { Clear(); }
  ~World() { Dispose(); }

  public const int NoLayer=-2, AllLayers=-1;

  public Color BackColor { get { return backColor; } set { backColor=value; } }
  public bool ChangedSinceSave { get { return changed; } set { changed=value; } }

  public int Height
  { get
    { int height=0;
      foreach(Layer layer in layers) if(layer.Height>height) height=layer.Height;
      return height;
    }
  }

  public int Width
  { get
    { int width=0;
      foreach(Layer layer in layers) if(layer.Width>width) width=layer.Width;
      return width;
    }
  }

  public Layer[] Layers { get { return layers; } }
  public IList Polygons { get { return polygons; } }

  public void AddLayer() { AddLayer(new Layer()); }
  public void AddLayer(Layer layer)
  { Layer[] narr = new Layer[layers.Length+1];
    Array.Copy(layers, narr, layers.Length);
    narr[layers.Length] = layer;
    layers = narr;
  }

  public void Clear()
  { if(layers==null)
    { layers = new Layer[8];
      for(int i=0; i<layers.Length; i++) layers[i] = new Layer();
    }
    else foreach(Layer layer in layers) layer.Dispose();
    polygons.Clear();
    backColor=Color.Black;
    changed=false;
  }

  public void Compile(string directory)
  { string path = directory;
    path.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';

    if(!Directory.Exists(path)) Directory.CreateDirectory(path);
    else foreach(string fn in Directory.GetFiles(path, "clayer*.png")) File.Delete(fn);

    StreamWriter writer = new StreamWriter(path+"definition");
    writer.WriteLine("(world");
    writer.WriteLine("  (bgcolor {0} {1} {2})", backColor.R, backColor.G, backColor.B);
    for(int i=0; i<layers.Length; i++) layers[i].Save(path, writer, null, i, true);
    foreach(Polygon poly in polygons) poly.Save(writer);
    writer.Write(')');
    writer.Close();
    changed = false;
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
    FileStream fs = File.Open(path+"definition", FileMode.Open, FileAccess.Read);
    ZipFile zip = new ZipFile(path+"images.zip");

    try
    { Clear();
      List level = new List(fs);
      if(level.Contains("bgcolor")) backColor = level["bgcolor"].ToColor();
      foreach(List list in level)
      { if(list.Name=="layer")
        { Layer layer = new Layer(list, zip);
          int z = list.GetInt(0);
          if(z+1>layers.Length) AddLayer(layer);
          else layers[z] = layer;
        }
        else if(list.Name=="polygon") polygons.Add(new Polygon(list));
      }
    }
    finally { fs.Close(); zip.Close(); }
  }

  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom)
  { foreach(Layer layer in layers) layer.Render(dest, sx, sy, drect, zoom, false, null);
  }
  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom, int layer, Object hilite)
  { for(int i=0; i<layers.Length; i++)
      layers[i].Render(dest, sx, sy, drect, zoom, layer==AllLayers || layer==i, hilite);
  }

  public void Save(string directory)
  { string path = directory;
    path.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';

    if(!Directory.Exists(path)) Directory.CreateDirectory(path);
    ZipOutputStream zip = new ZipOutputStream(File.Open(path+"images.zip", FileMode.Create));
    zip.SetLevel(5);

    StreamWriter writer = new StreamWriter(path+"definition");
    writer.WriteLine("(smarm-world");
    writer.WriteLine("  (bgcolor {0} {1} {2})", backColor.R, backColor.G, backColor.B);
    for(int i=0; i<layers.Length; i++) layers[i].Save(path, writer, zip, i, false);
    foreach(Polygon poly in polygons) poly.Save(writer);
    writer.Write(')');
    writer.Close();
    zip.Finish();
    zip.Close();
    changed = false;
  }

  ArrayList polygons = new ArrayList();
  Layer[] layers;
  Color backColor;
  bool changed;
}

} // namespace Smarm