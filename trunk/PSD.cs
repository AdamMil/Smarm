using System;
using System.Drawing;
using System.IO;
using GameLib.IO;
using GameLib.Video;

namespace Smarm
{

class PSD
{ public static Surface[] ReadPSD(Stream stream)
  { int width, height, chans;

    if(IOH.ReadString(stream, 4) != "8BPS") throw new ArgumentException("Not a photoshop file");
    int value = IOH.ReadBE2U(stream);
    if(value != 1) throw new NotSupportedException("Unsupported PSD version number: "+value);
    IOH.Skip(stream, 6);
    chans = IOH.ReadBE2U(stream);
    if(chans<3 || chans>4) throw new NotSupportedException("Unsupported number of channels: "+chans);
    height = IOH.ReadBE4(stream);
    width  = IOH.ReadBE4(stream);
    value  = IOH.ReadBE2U(stream);
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

      Surface[] surfaces = new Surface[numLayers];
      Layer[] layers = new Layer[numLayers];
      for(int i=0; i<numLayers; i++) layers[i] = new Layer(stream);
      for(int i=0; i<numLayers; i++)
        surfaces[i] = ReadImageData(stream, width, height, layers[i].Bounds, layers[i].Channels, true);
      return surfaces;
    }
    noLayers:
    return new Surface[] { ReadImageData(stream, width, height, new Rectangle(0, 0, width, height), chans, false) };
  }

  public static void WritePSD(Stream stream, Surface[] layers) { }
  
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

        // assumes ARGB
        byte* dest = (byte*)surface.Data + area.Y*surface.Pitch + area.X*4;
        switch(chan+(chans==3?1:0))
        { case 0: dest += MaskToOffset(surface.Format.AlphaMask); break;
          case 1: dest += MaskToOffset(surface.Format.RedMask); break;
          case 2: dest += MaskToOffset(surface.Format.GreenMask); break;
          case 3: dest += MaskToOffset(surface.Format.BlueMask); break;
        }
        if(compressed)
        { if(layer || chan==0)
          { lengths = new int[layer ? area.Height : area.Height*chans];
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
              if(layer && (area.Width&1)!=0) src++;
            }
          if(layer) yi=0;
        }
        else
        { int length = area.Width*area.Height;
          byte[] data = new byte[length];
          IOH.Read(stream, data);
          fixed(byte* sdata=data)
          { byte* src=sdata;
            do { *dest=*src++; dest+=4; } while(--length != 0);
          }
        }
      }
    }
    finally { surface.Unlock(); }
    return surface;
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
  
  struct Layer
  { public Layer(Stream stream)
    { int y=IOH.ReadBE4(stream), x=IOH.ReadBE4(stream), y2=IOH.ReadBE4(stream), x2=IOH.ReadBE4(stream);
      if(x2-x==0 || y2-y==0) throw new NotSupportedException("Unsupported: layer with no area");
      Bounds = new Rectangle(x, y, x2-x, y2-y);

      Channels = IOH.ReadBE2(stream);
      if(Channels<3 || Channels>4) throw new NotSupportedException("Unsupported number of channels: "+Channels);
      IOH.Skip(stream, Channels*6); // assume RGB[A]

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
    public int Channels;
  }
}

} // namespace Smarm