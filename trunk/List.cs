using System;
using System.Collections;
using System.IO;

namespace Smarm
{

class List : IEnumerable
{ public List() { name=string.Empty; }
  public List(string name) { this.name=name; }
  public List(Stream stream) { name=string.Empty; Read(new StreamReader(stream)); }

  public object this[int index] { get { return items[index]; } set { items[index]=value; } }

  public List this[string name]
  { get
    { foreach(object o in items) if(o is List) { List list=(List)o; if(list.Name==name) return list; }
      return null;
    }
  }

  public int Length { get { return items.Count; } }

  public string Name
  { get { return name; }
    set { if(value==null) throw new ArgumentNullException("Name"); name=value; }
  }

  public void Add(object o) { if(o==null) throw new ArgumentNullException(); items.Add(o); }

  public void Clear() { Name=string.Empty; items.Clear(); }
  
  public double GetFloat(int index) { return (double)items[index]; }
  public int GetInt(int index) { return (int)(double)items[index]; }
  public List GetList(int index) { return (List)items[index]; }
  public string GetString(int index) { return (string)items[index]; }

  public void Load(Stream stream)
  { Clear();
    Read(new StreamReader(stream));
  }

  public void Save(Stream stream)
  { StreamWriter sw = new StreamWriter(stream);
    Write(sw, 0);
    sw.Flush();
  }
  
  public override string ToString()
  { StringWriter s = new StringWriter();
    Write(s, 0);
    return s.ToString();
  }

  public IEnumerator GetEnumerator() { return items.GetEnumerator(); }

  #region Internals
  void Read(TextReader stream) // .NET needs higher-level text IO
  { int read = SkipWS(stream);
    if(read!='(')
      throw new ArgumentException(string.Format("Expected '(' [got {0}] near {1}", read, stream.ReadLine()));

    stream.Read();
    read=SkipWS(stream); // skip to name

    // read name
    if(char.IsLetter((char)read))
      do
      { name += (char)read;
        stream.Read();
      } while((read=stream.Peek())!=-1 && !char.IsWhiteSpace((char)read) && read!='(' && read!=')');
    if(read==-1) throw new EndOfStreamException();
    
    while(true)
    { read = SkipWS(stream);
      if(read=='(')
      { List list = new List();
        list.Read(stream);
        Add(list);
      }
      else if(read=='-' || char.IsDigit((char)read))
      { string value=string.Empty;
        while(true)
        { read=stream.Peek();
          if(read==-1) throw new EndOfStreamException();
          if(read!='-' && read!='.' && !char.IsDigit((char)read)) break;
          value += (char)stream.Read();
        }
        Add(double.Parse(value));
      }
      else
      { stream.Read();
        if(read=='\"' || read=='\'')
        { string value=string.Empty;
          int delim=read;
          while(true)
          { read=stream.Read();
            if(read==-1) throw new EndOfStreamException("Unterminated string");
            if(read==delim) break;
            value += (char)read;
          }
          Add(value);
        }
        else if(read==')') break;
      }
    }
  }

  void Write(TextWriter stream, int level)
  { stream.Write('(');
    stream.Write(name);
    foreach(object o in items)
    { stream.Write(' ');
      if(o is List)
      { if(level==0) stream.Write("\n  ");
        ((List)o).Write(stream, level+1);
      }
      else if(o is string)
      { string s = (string)o;
        char delim = s.IndexOf('\"')==-1 ? '\"' : '\'';
        stream.Write(delim);
        stream.Write(s);
        stream.Write(delim);
      }
      else stream.Write(o.ToString());
    }
    stream.Write(')');
  }
  
  int SkipWS(TextReader stream)
  { int read;
    while(char.IsWhiteSpace((char)(read=stream.Peek()))) stream.Read();
    if(read==-1) throw new EndOfStreamException();
    return read;
  }
  #endregion

  string    name;
  ArrayList items = new ArrayList(2);
}

} // namespace Smarm