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
using GameLib.Video;
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
    int right = rect.Right, bottom = rect.Bottom;
    bool objects=false, polys=false;
    rect.X = Expand(rect.X, Layer.PartWidth/4, -1);
    rect.Y = Expand(rect.Y, Layer.PartHeight/4, -1);
    rect.Width = Expand(right, Layer.PartWidth/4, 1) - rect.X;
    rect.Height = Expand(bottom, Layer.PartHeight/4, 1) - rect.Y;
    
    if(rect.X<0 || rect.Y<0)
    { int xo=-Math.Min(rect.X, 0), yo=-Math.Min(rect.Y, 0);
      Shift(xo, yo);
      rect.Offset(xo, yo);
    }

    Surface surface = new Surface(rect.Width*4, rect.Height*4, 32, SurfaceFlag.SrcAlpha);
    string file = Path.GetTempPath();
    if(file[file.Length-1] != '\\') file += '\\';
    for(int i=0; i<10000; i++) if(!File.Exists(file+"smarm"+i+".psd")) { file += "smarm"+i+".psd"; break; } // race condition

    PSDCodec codec = new PSDCodec();
    PSDImage image = new PSDImage();
    image.Channels = 4;
    image.Width  = rect.Width*4;
    image.Height = rect.Height*4;
    image.Layers = new PSDLayer[layers.Length+3];
    for(int i=0; i<image.Layers.Length; i++) image.Layers[i] = new PSDLayer(surface);
    image.Layers[0].Name = "Background";
    for(int i=0; i<layers.Length; i++)
    { image.Layers[i+1].Name   = "Layer "+i;
      if(layers[i].Width==0) image.Layers[i+1].Size = new Size(0, 0);
      if(!objects)
        foreach(Object obj in layers[i].Objects) if(obj.Bounds.IntersectsWith(rect)) { objects=true; break; }
    }
    image.Layers[layers.Length+1].Name = "Polygons";
    foreach(Polygon poly in polygons) if(poly.Bounds.IntersectsWith(rect)) { polys=true; break; }
    if(!polys) image.Layers[layers.Length+1].Size = new Size(0, 0);
    image.Layers[layers.Length+2].Name = "Objects";
    if(!objects) image.Layers[layers.Length+2].Size = new Size(0, 0);
    image.Layers[layers.Length+1].Opacity = 84;
    codec.StartWriting(image, file);

    surface.Fill(backColor);
    codec.WriteLayer(surface);

    foreach(Layer layer in layers)
    { if(layer.Width==0) codec.WriteLayer(null);
      else
      { surface.Fill(0);
        layer.Render(surface, rect.X*4, rect.Y*4, surface.Bounds, ZoomMode.Full, false, null, false);
        codec.WriteLayer(surface);
      }
    }

    if(polys)
    { surface.Fill(0);
      foreach(Polygon poly in polygons)
        if(poly.Points.Length>3 && poly.Bounds.IntersectsWith(rect))
        { Point[] points = (Point[])poly.Points.Clone();
          for(int i=0; i<points.Length; i++) points[i] = PolyOffset(points[i], rect.Location);
          Primitives.FilledPolygon(surface, points, poly.Color);
        }
      codec.WriteLayer(surface);
    }
    else codec.WriteLayer(null);
    
    if(objects)
    { surface.Fill(0);
      foreach(Layer layer in layers)
        foreach(Object obj in layer.Objects)
          if(obj.Bounds.IntersectsWith(rect))
          { Rectangle r = obj.Bounds;
            r.Location = PolyOffset(r.Location, rect.Location);
            r.Width *= 4; r.Height *= 4;
            Primitives.Box(surface, r, Color.White);
            if(objectFont!=null) objectFont.Render(surface, obj.Name, r, ContentAlignment.MiddleCenter);
          }
      codec.WriteLayer(surface);
    }
    else codec.WriteLayer(null);
    
    surface.Fill(0);
    codec.WriteFlattened(surface);
    codec.FinishWriting();
    
    try
    { surface.Dispose();
      bool fs = App.Fullscreen;
      App.Fullscreen = false;
      System.Diagnostics.Process proc = System.Diagnostics.Process.Start(App.EditorPath, file);
      proc.WaitForExit();
      App.Fullscreen = fs;
      surface = new Surface(rect.Width*4, rect.Height*4, 32, SurfaceFlag.SrcAlpha);
      
      image = codec.StartReading(file);
      codec.SkipLayer(); // skip the background
      for(int i=0; i<layers.Length; i++)
        if(layers[i].Width>0 || image.Layers[i+1].Width>0)
        { PSDLayer layer = codec.ReadNextLayer();
          surface.Fill(0);
          layer.Surface.UsingAlpha = false;
          layer.Surface.Blit(surface, layer.Location);
          layer.Surface.Dispose();
          layers[i].InsertSurface(surface, rect.X*4, rect.Y*4);
          changed = true;
        }
        else codec.SkipLayer();
      codec.FinishReading();
    }
    finally { File.Delete(file); }
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
  { foreach(Layer layer in layers) layer.Render(dest, sx, sy, drect, zoom, false, null, true);
  }
  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom, int layer, Object hilite)
  { for(int i=0; i<layers.Length; i++)
      layers[i].Render(dest, sx, sy, drect, zoom, layer==AllLayers || layer==i, hilite, true);
  }

  public void Save(string directory)
  { string path = directory.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';

    if(!Directory.Exists(path)) Directory.CreateDirectory(path);
    ZipOutputStream zip = new ZipOutputStream(File.Open(path+"_images.zip", FileMode.Create));
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
    
    if(this.zip!=null) this.zip.Close();
    if(File.Exists(path+"images.zip")) File.Delete(path+"images.zip");
    File.Move(path+"_images.zip", path+"images.zip");

    this.zip = new ZipFile(path+"images.zip");
    basePath = path;
    tempPath = false;
  }
  
  void Clear(bool disposing)
  { if(zip!=null) { zip.Close(); zip=null; }
    if(tempPath) Directory.Delete(basePath, true);

    if(!disposing)
    { basePath = Path.GetTempFileName().Replace('\\', '/');
      File.Delete(basePath);
      Directory.CreateDirectory(basePath);
      if(basePath[basePath.Length-1]!='/') basePath += '/';
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
  { if(sign<0)
    { return value - (value<0 ? (block + value%block) : value%block);
    }
    else
    { return value + (value<0 ? -value%block : (block - value%block));
    }
  }

  Point PolyOffset(Point p, Point amt) { p.X = (p.X-amt.X)*4; p.Y = (p.Y-amt.Y)*4; return p; }
  
  void Shift(int xo, int yo)
  { foreach(Layer layer in layers) layer.Shift(xo, yo);
    foreach(Polygon poly in polygons)
      for(int i=0; i<poly.Points.Length; i++)
      { Point p = poly.Points[i];
        p.Offset(xo, yo);
        poly.Points[i] = p;
      }
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