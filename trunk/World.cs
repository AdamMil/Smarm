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

  public void AddLayer(Layer layer)
  { Layer[] narr = new Layer[layers.Length+1];
    Array.Copy(layers, narr, layers.Length);
    narr[layers.Length] = layer;
    layers = narr;
  }

  public void Clear() { Clear(false); }

  public void Compile(string directory)
  { string path = directory.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';

    if(!Directory.Exists(path)) Directory.CreateDirectory(path);
    else foreach(string fn in Directory.GetFiles(path, "layer*.png")) File.Delete(fn);

    StreamWriter writer = new StreamWriter(path+"world.lev");
    writer.WriteLine("(world");
    writer.WriteLine("  (bgcolor {0} {1} {2})", backColor.R, backColor.G, backColor.B);
    for(int i=0; i<layers.Length; i++) layers[i].Save(path, writer, null, i, true);
    foreach(Polygon poly in polygons) poly.Save(writer);
    writer.Write(')');
    writer.Close();
    changed = false;
  }

  public void Dispose()
  { Clear(true);
    GC.SuppressFinalize(this);
  }

  public void EditRect(Rectangle rect, GameLib.Fonts.Font objectFont)
  { // expand the rectangle dimensions to multiples of the block size
    rect.X = Expand(rect.X, Layer.PartWidth, -1);
    rect.Y = Expand(rect.Y, Layer.PartHeight, -1);
    rect.Width = Expand(rect.Right, Layer.PartWidth, 1) - rect.X;
    rect.Height = Expand(rect.Bottom, Layer.PartHeight, 1) - rect.Y;
  }

  public void InsertLayer(int pos)
  { Layer layer = new Layer(this);
    AddLayer(layer);
    for(int i=layers.Length-1; i>pos; i--) layers[i] = layers[i-1];
    layers[pos] = layer;
  }
  
  public void Load(string directory)
  { if(!Directory.Exists(directory))
      throw new DirectoryNotFoundException(string.Format("Directory '{0}' not found", directory));

    string path = directory.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';
    FileStream fs = File.Open(path+"definition", FileMode.Open, FileAccess.Read);

    try
    { Clear();
      zip = new ZipFile(path+"images.zip");
      List level = new List(fs);
      if(level.Contains("bgcolor")) backColor = level["bgcolor"].ToColor();
      foreach(List list in level)
      { if(list.Name=="layer")
        { Layer layer = new Layer(this, list);
          int z = list.GetInt(0);
          if(z+1>layers.Length) AddLayer(layer);
          else layers[z] = layer;
        }
        else if(list.Name=="polygon") polygons.Add(new Polygon(list));
      }
      basePath = path;
      tempPath = false;
    }
    catch(Exception e) { Clear(); throw e; }
    finally { fs.Close(); }
  }

  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom)
  { foreach(Layer layer in layers) layer.Render(dest, sx, sy, drect, zoom, false, null);
  }
  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom, int layer, Object hilite)
  { for(int i=0; i<layers.Length; i++)
      layers[i].Render(dest, sx, sy, drect, zoom, layer==AllLayers || layer==i, hilite);
  }

  public void Save(string directory)
  { string path = directory.Replace('\\', '/');
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
    
    this.zip = new ZipFile(path+"images.zip");
    basePath = path;
    tempPath = false;
  }
  
  void Clear(bool disposing)
  { if(zip!=null) { zip.Close(); zip=null; }
    if(tempPath) Directory.Delete(basePath, true);

    if(!disposing)
    { basePath = Path.GetTempFileName();
      File.Delete(basePath);
      Directory.CreateDirectory(basePath);
      tempPath = true;
    }

    if(layers!=null) foreach(Layer layer in layers) layer.Dispose();
    layers = new Layer[8];
    for(int i=0; i<layers.Length; i++) layers[i] = new Layer(this);
    polygons.Clear();
    backColor=Color.Black;
    changed=false;
    nextTile=0;
  }

  int Expand(int value, int block, int sign)
  { return value + (value<0 ? (block - value%block) : (value + value%block)) * sign;
  }

  internal int NextTile { get { return nextTile++; } }
  internal ZipFile zip;
  internal string basePath;

  ArrayList polygons = new ArrayList();
  Layer[] layers;
  Color backColor;
  int nextTile;
  bool changed, tempPath;
}

} // namespace Smarm