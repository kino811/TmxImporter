using System.Collections.Generic;
using UnityEngine;

namespace Kino.Tmx.Formats {
    // refer to : http://doc.mapeditor.org/reference/tmx-map-format/
    
    enum AnchorType {
        LeftDown,
        LeftUp,
        RightDown,
        RightUp,
        Center,
    }
    
    static class CommonDatas {
        public const int NullTileGid = 0;
    }

    public class Property {
        public enum Type {
            String, // default
            Int,
            Float,
            Bool,
            Color,
            File,
        }

        public static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>() {
            {"string", Type.String},
            {"int", Type.Int},
            {"float", Type.Float},
            {"bool", Type.Bool},
            {"color", Type.Color},
            {"file", Type.File},
        };

        public string name;
        public string type;
        public string value;
    }

    public class Properties {
        public List<Property> properties = new List<Property>();
    }

    /*Can contain: tileset, layer, objectgroup, imagelayer*/
    [System.SerializableAttribute]
    public class Map {
        public enum Orientation {
            Orthogonal,
            Isometric,
            Staggered,
            Hexagonal,
        }

        public static readonly string[] OrientationStrings = {
            "orthogonal",
            "isometric",
            "staggered",
            "hexagonal",
        };

        public enum RenderOrder {
            RightDown,
            RightUp,
            LeftDown,
            LeftUp,
        }

        public static readonly string[] RenderOrderStrings = {
            "right-down",
            "right-up",
            "left-down",
            "left-up",
        };

        [System.SerializableAttribute]
        public class TileSet {
            public int firstGid;
            public string source;
            public Formats.TileSet orgTileSet;
        }

        public string version = "1.0";
        public string orientation = "orthogonal";
        public string renderorder = "right-down";
        public int width;
        public int height;
        public int tileWidth;
        public int tileHeight;
        public List<TileSet> tileSets = new List<TileSet>();
        public List<Layer> layers = new List<Layer>();
        public List<ObjectGroup> objGroups = new List<ObjectGroup>();
        public List<ILayer> layerOrders = new List<ILayer>();

        public bool IsRenderOrder(RenderOrder renderOrder) {
            string renderOrderStr = RenderOrderStrings[(int)renderOrder];

            return this.renderorder == renderOrderStr;
        }

        public int GetLayerOrder(Formats.ILayer layer) {
            const int defaultOrder = 0;

            int order = defaultOrder;

            for (int i = 0; i < this.layerOrders.Count; ++ i) {
                ILayer iLayer = this.layerOrders[i];

                if (iLayer == layer) {
                    order += i;
                    break;
                }
            }

            return order;
        }
    }

    /*Can contain: properties (since 0.8), terraintypes (since 0.9)*/
    [System.SerializableAttribute]
    public class TileSet {
        public int firstGid;
        /*source: If this tileset is stored in an external TSX (Tile Set XML) file, this attribute refers to that file. That TSX file has the same structure as the <tileset> element described here. (There is the firstgid attribute missing and this source attribute is also not there. These two attributes are kept in the TMX map, since they are map specific.)*/
        public string source = "";
        public string name = "";
        public int tileWidth;
        public int tileHeight;
        public int tileCount;
        public int columns;
        public TileOffset tileOffset;
        public Image image;
        public List<Tile> tiles = new List<Tile>();
    }

    [System.SerializableAttribute]
    public class TileOffset {
        public int x;
        public int y;
    }

    /*Can contain: data (since 0.9)*/
    [System.SerializableAttribute]
    public class Image {
        public string source = "";
        public int width;
        public int height;
        public Texture2D sourceAsset;
    }

    /*class TerrainTypes {
        public List<Terrain> = new List<Terrain>();
    }*/

    /*class Terrain {
    }*/

    [System.SerializableAttribute]
    public class Tile {
        public int id;
        //public int[4] terrain = new int[] {0, 0, 0, 0};
        public Animation animation;
        public Sprite sprite;
    }

    [System.SerializableAttribute]
    public class Animation {
        [System.SerializableAttribute]
        public class Frame {
            public int tileID;
            public int duration = 100; // milliseconds
        }

        public List<Frame> frames = new List<Frame>();
        public RuntimeAnimatorController animController;
    }

    public interface ILayer {
        string Name {get;}
    }

    [System.SerializableAttribute]
    public class Layer : ILayer {
        [System.SerializableAttribute]
        public class Data {
            public enum Encoding {
                Csv,
            }

            public static readonly string[] EncodingStrings = {
                "csv",
            };

            [System.SerializableAttribute]
            public class Tile {
                public int gid = CommonDatas.NullTileGid;
            }

            public string encoding;
            public List<Tile> tiles = new List<Tile>();
        }

        public string name;
        public int width;
        public int height;
        public bool visible = true;
        public Properties properties;
        public Data data;

        public string Name {get {return name;}}
    }

    [System.SerializableAttribute]
    public class ObjectGroup : ILayer {
        [System.SerializableAttribute]
        public class Object {
            [System.SerializableAttribute]
            public class PolyLine {
                public List<Vector2> points = new List<Vector2>();
            }

            public int id;
            public string name;
            public string type;
            public int gid = CommonDatas.NullTileGid;
            public float x;
            public float y;
            public int width;
            public int height;
            public bool visible = true;
            public PolyLine polyLine = new PolyLine();
        }

        public string name;
        public int offsetX;
        public int offsetY;
        public List<Object> objects = new List<Object>();

        public string Name {get {return name;}}
    }

    public class ObjectTypes {
        public class ObjectType {
            public string name;
            public string color;
            public List<Property> properties = new List<Property>();
        }

        public List<ObjectType> objectTypes;
    }
}
