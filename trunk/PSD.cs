/*
Smarm is an editor for the game Swarm, which was written by Jim Crawford. 
http://www.adammil.net/
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
using System.Drawing;
using System.IO;
using GameLib.IO;
using GameLib.Video;

namespace Smarm
{

class PSDLayer
{ public PSDLayer(Stream stream)
  { int y=IOH.ReadBE4(stream), x=IOH.ReadBE4(stream), y2=IOH.ReadBE4(stream), x2=IOH.ReadBE4(stream);
    Bounds = new Rectangle(x, y, x2-x, y2-y);

    Channels = IOH.ReadBE2(stream);
    if(Channels<3 || Channels>4) throw new NotSupportedException("Unsupported number of channels: "+Channels);
    IOH.Skip(stream, Channels*6); // FIXME: assumes [A]RGB

    if(IOH.ReadString(stream, 4)!="8BIM") throw new ArgumentException("Unknown blend signature");
    { string sval = IOH.ReadString(stream, 4);
      if(sval!="norm")
        throw new NotSupportedException(string.Format("Unsupported blend mode '{0}' for layer", sval));
    }
    int opacity = IOH.Read1(stream);
    if(opacity != 255)
      throw new NotSupportedException(string.Format("Unsupported opacity level {0} for layer", opacity));
    IOH.Skip(stream, 3); // misc stuff
    IOH.Skip(stream, IOH.ReadBE4(stream)); // extra layer data
  }

  public Rectangle Bounds;
  public Surface Surface;
  public int Channels;
}

class PSDImage
{ public PSDLayer[] Layers;
  public int Width, Height, Channels;
}

class PSD
{ public static PSDImage ReadPSD(Stream stream)
  { PSDImage img = new PSDImage();

    if(IOH.ReadString(stream, 4) != "8BPS") throw new ArgumentException("Not a photoshop file");
    int value = IOH.ReadBE2U(stream);
    if(value != 1) throw new NotSupportedException("Unsupported PSD version number: "+value);
    IOH.Skip(stream, 6);
    img.Channels = IOH.ReadBE2U(stream);
    if(img.Channels<3 || img.Channels>4)
      throw new NotSupportedException("Unsupported number of channels: "+img.Channels);
    img.Height = IOH.ReadBE4(stream);
    img.Width  = IOH.ReadBE4(stream);
    value = IOH.ReadBE2U(stream);
    if(value != 8) throw new NotSupportedException("Unsupported channel depth: "+value);
    value = IOH.ReadBE2U(stream);
    if(value != 3) throw new NotSupportedException("Unsupported color mode: "+value);

    IOH.Skip(stream, IOH.ReadBE4(stream)); // skip color block
    IOH.Skip(stream, IOH.ReadBE4(stream)); // skip image resources

    int miscLen = IOH.ReadBE4(stream);
    if(miscLen!=0) // length of miscellaneous info section
    { IOH.Skip(stream, 4);
      int numLayers = Math.Abs(IOH.ReadBE2(stream));
      if(numLayers==0) { IOH.Skip(stream, miscLen-6); goto noLayers; }

      img.Layers = new PSDLayer[numLayers];
      for(int i=0; i<numLayers; i++) img.Layers[i] = new PSDLayer(stream);
      for(int i=0; i<numLayers; i++)
        img.Layers[i].Surface = ReadImageData(stream, img.Width, img.Height, img.Layers[i].Bounds,
                                              img.Layers[i].Channels, true);
      return img;
    }
    noLayers:
    img.Layers = new PSDLayer[1];
    img.Layers[0].Bounds = new Rectangle(0, 0, img.Width, img.Height);
    img.Layers[0].Channels = img.Channels;
    img.Layers[0].Surface = ReadImageData(stream, img.Width, img.Height, img.Layers[0].Bounds, img.Channels, false);
    return img;
  }

  public static void WritePSD(Stream stream, Surface[] layers, Color bgColor)
  { IOH.WriteString(stream, "8BPS"); // signature
    IOH.WriteBE2(stream, 1); // version
    for(int i=0; i<6; i++) stream.WriteByte(0); // reserved
    IOH.WriteBE2(stream, 3); // channels
    IOH.WriteBE4(stream, layers[0].Height);
    IOH.WriteBE4(stream, layers[0].Width);
    IOH.WriteBE2(stream, 8); // bit depth (per channel)
    IOH.WriteBE2(stream, 3); // color mode (3=RGB)

    IOH.WriteBE4(stream, 0); // color data section
    IOH.WriteBE4(stream, 0); // psd resources section
    
    if(layers.Length>1)
    { int pos = (int)stream.Position;
      IOH.WriteBE4(stream, 0); // size of the miscellaneous info section (to be filled later)
      IOH.WriteBE4(stream, 0); // size of the layer section (to be filled later)
      IOH.WriteBE2(stream, (short)-layers.Length); // number of layers (yes, it's negative for a reason)

      for(int layer=0; layer<layers.Length; layer++)
      { Surface surf = layers[layer];
        IOH.WriteBE4(stream, 0); // layer top
        IOH.WriteBE4(stream, 0); // layer left
        IOH.WriteBE4(stream, surf.Height); // layer bottom
        IOH.WriteBE4(stream, surf.Width);  // layer right
        IOH.WriteBE2(stream, surf.UsingAlpha ? (short)4 : (short)3); // number of channels
        for(int i=surf.UsingAlpha ? -1 : 0; i<3; i++) // channel information
        { IOH.WriteBE2(stream, (short)i); // channel ID
          IOH.WriteBE4(stream, surf.Width*surf.Height+2); // data length
        }
        IOH.WriteString(stream, "8BIM"); // blend mode signature
        IOH.WriteString(stream, "norm"); // blend mode
        stream.WriteByte(255); // opacity (255=opaque)
        stream.WriteByte(0);   // clipping (0=base)
        stream.WriteByte(0);   // flags
        stream.WriteByte(0);   // reserved
        int extraLen = 8 + (7+layer.ToString().Length+3)/4*4;
        IOH.WriteBE4(stream, extraLen); // size of the extra layer infomation
        IOH.WriteBE4(stream, 0); // layer mask
        IOH.WriteBE4(stream, 0); // layer blending size
        string name = "Layer "+(layer+1);
        stream.WriteByte((byte)name.Length);
        IOH.WriteString(stream, name); // layer name
        if(((name.Length+1)&3) != 0) for(int i=4-((name.Length+1)&3); i>0; i--) stream.WriteByte(0); // name padding
      }

      foreach(Surface layer in layers) WriteImageData(stream, layer, true);
      IOH.WriteBE4(stream, 0); // global layer mask section
      int dist = (int)stream.Position - pos - 4;

      // save the flat image
      Surface flat = new Surface(layers[0].Width, layers[0].Height, 32);
      flat.Fill(bgColor);
      foreach(Surface layer in layers) layer.Blit(flat);
      WriteImageData(stream, flat, false);

      stream.Position = pos;
      IOH.WriteBE4(stream, dist); // miscellaneous info section size
      IOH.WriteBE4(stream, dist-8); // layer section size
    }
    else
    { IOH.WriteBE4(stream, 0); // misc info section
      WriteImageData(stream, layers[0], false);
    }
  }
  
  static void InsertFile(Stream stream, string file)
  { FileStream ins = File.Open(App.SmarmPath+file, FileMode.Open, FileAccess.Read);
    byte[] data = new byte[1024];
    int read;
    while(true)
    { read = ins.Read(data, 0, 1024);
      if(read==0) break;
      else stream.Write(data, 0, read);
    }
    ins.Close();
  }

  static unsafe Surface ReadImageData(Stream stream, int imgWidth, int imgHeight, Rectangle area, int chans, bool layer)
  { Surface surface = new Surface(imgWidth, imgHeight, 32, chans==3 ? SurfaceFlag.None : SurfaceFlag.SrcAlpha);
    byte[] linebuf=null;
    int[]  lengths=null;
    bool   compressed=false;
    int    maxlen, yi=0, yinc = (int)surface.Pitch-area.Width*4;

    surface.Lock();
    try
    { for(int chan=0; chan<chans; chan++)
      { if(layer || chan==0)
        { int value = IOH.ReadBE2(stream);
          if(value!=0 && value!=1) throw new NotSupportedException("Unsupported compression type: "+value);
          compressed = value==1;
        }

        // FIXME: assumes [A]RGB
        byte* dest = (byte*)surface.Data + area.Y*surface.Pitch + area.X*4;
        switch(chan+(chans==3?1:0))
        { case 0: dest += MaskToOffset(surface.Format.AlphaMask); break;
          case 1: dest += MaskToOffset(surface.Format.RedMask); break;
          case 2: dest += MaskToOffset(surface.Format.GreenMask); break;
          case 3: dest += MaskToOffset(surface.Format.BlueMask); break;
        }
        if(compressed)
        { if(layer || chan==0)
          { if(lengths==null) lengths = new int[layer ? area.Height : area.Height*chans];
            maxlen = 0;
            for(int y=0; y<lengths.Length; y++)
            { lengths[y] = IOH.ReadBE2U(stream);
              if(lengths[y]>maxlen) maxlen=lengths[y];
            }
            linebuf = new byte[maxlen];
          }
          fixed(byte* lbptr=linebuf)
            for(int yend=yi+area.Height; yi<yend; yi++)
            { byte* src = lbptr;
              int  f;
              byte b;
              IOH.Read(stream, linebuf, lengths[yi]);
              for(int i=0; i<area.Width;)
              { f=*src++;
                if(f>=128)
                { if(f==128) continue;
                  f=257-f;
                  b=*src++;
                  i += f;
                  do { *dest=b; dest+=4; } while(--f != 0);
                }
                else
                { i += ++f;
                  do { *dest=*src++; dest+=4; } while(--f != 0);
                }
              }
              dest += yinc;
              //if(layer && (area.Width&1)!=0) src++; // TODO: test this with an actual odd-width layer
            }
          if(layer) yi=0;
        }
        else
        { int length = area.Width*area.Height;
          byte[] data = new byte[length];
          IOH.Read(stream, data);
          fixed(byte* sdata=data) // FIXME: handle pad byte on odd-width rows
          { byte* src=sdata;
            do { *dest=*src++; dest+=4; } while(--length != 0);
          }
        }
      }
    }
    finally { surface.Unlock(); }
    return surface;
  }
  
  static unsafe void WriteImageData(Stream stream, Surface surface, bool layer)
  { surface.Lock();
    try
    { byte[] data = new byte[1024];
      for(int chan=0, chans=surface.UsingAlpha ? 4 : 3; chan<chans; chan++)
      { if(layer || chan==0) IOH.WriteBE2(stream, 0); // no compression
        byte* src = (byte*)surface.Data;
        switch(chan+(surface.UsingAlpha?0:1))
        { case 0: src += MaskToOffset(surface.Format.AlphaMask); break;
          case 1: src += MaskToOffset(surface.Format.RedMask); break;
          case 2: src += MaskToOffset(surface.Format.GreenMask); break;
          case 3: src += MaskToOffset(surface.Format.BlueMask); break;
        }
        int length = surface.Width*surface.Height, blen=0;
        fixed(byte* ddata=data)
        { byte* dest=ddata;
          do
          { *dest++=*src;
            src+=4;
            if(++blen==data.Length)
            { stream.Write(data, 0, data.Length);
              blen = 0;
              dest = ddata;
            }
          } while(--length != 0);
          if(blen>0) stream.Write(data, 0, blen);
        }
      }
    }
    finally { surface.Unlock(); }
  }

  static int MaskToOffset(uint mask)
  { int i=0;
    while(mask!=255)
    { if(mask==0) throw new NotSupportedException("unsupported color mask");
      mask >>=8;
      i++;
    }
    return i;
  }
}

} // namespace Smarm