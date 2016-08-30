using UnityEngine;
using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Kino.Tmx.Formats;

namespace Kino.Tmx {
    [System.SerializableAttribute]
    public class TmxImporter : MonoBehaviour {
        public UnityEngine.Object tmxFile;
        public string tmxFilePath;
        public Formats.Map mapFormat;
        public List<Formats.TileSet> tsxTileSetFormats = new List<Formats.TileSet>();

        public void Import() {
            if (tmxFilePath == "") {
                Debug.Log("null filePath");
                return;
            }

            this.mapFormat = null;
            this.tsxTileSetFormats.Clear();

            // do parsing tmx.
            // tmx is xml file format.
            XmlDocument xml = new XmlDocument();
            xml.Load(tmxFilePath);
            //Debug.Log(xml.OuterXml);
            
            // parse map node
            {
                XmlNode mapNode = xml.SelectSingleNode("map");
                if (mapNode != null) {
                    Formats.Map fmMap = new Formats.Map();

                    TryGainAttributeValue(mapNode.Attributes, "version", ref fmMap.version);
                    TryGainAttributeValue(mapNode.Attributes, "orientation", ref fmMap.orientation);
                    TryGainAttributeValue(mapNode.Attributes, "renderorder", ref fmMap.renderorder);
                    TryGainAttributeValue(mapNode.Attributes, "width", ref fmMap.width);
                    TryGainAttributeValue(mapNode.Attributes, "height", ref fmMap.height);
                    TryGainAttributeValue(mapNode.Attributes, "tilewidth", ref fmMap.tileWidth);
                    TryGainAttributeValue(mapNode.Attributes, "tileheight", ref fmMap.tileHeight);

                    // tileset nodes
                    {
                        foreach (XmlNode tileSetNode in mapNode.SelectNodes("tileset")) {
                            Formats.Map.TileSet fmTileSet = new Formats.Map.TileSet();

                            TryGainAttributeValue(tileSetNode.Attributes, "firstgid", ref fmTileSet.firstGid);
                            TryGainAttributeValue(tileSetNode.Attributes, "source", ref fmTileSet.source);
                            bool isTsx = fmTileSet.source != null && fmTileSet.source != "";

                            if (isTsx) {
                                string tmxFileParentDirPath = Path.GetDirectoryName(this.tmxFilePath);
                                string tsxPath = Path.Combine(tmxFileParentDirPath, fmTileSet.source);
                                XmlDocument tsx = new XmlDocument();
                                tsx.Load(tsxPath);
                                XmlNode tileSetNodeInTsx = tsx.SelectSingleNode("tileset");

                                Formats.TileSet fmTileSetForTsx = ImportTileSetFormat(tileSetNodeInTsx);
                                this.tsxTileSetFormats.Add(fmTileSetForTsx);
                                fmTileSet.orgTileSet = fmTileSetForTsx;
                            }
                            else {
                                Formats.TileSet orgTileSet = ImportTileSetFormat(tileSetNode);
                                fmTileSet.orgTileSet = orgTileSet;
                            }

                            fmMap.tileSets.Add(fmTileSet);
                        }
                    }

                    foreach (XmlNode ilayerNode in mapNode.SelectNodes("layer | objectgroup")) {
                        string ilayerNodeName = ilayerNode.Name;

                        if (ilayerNodeName == "layer") {
                            // layer nodes
                            {
                                XmlNode layerNode = ilayerNode;
                                Formats.Layer layerFormat = new Formats.Layer();

                                TryGainAttributeValue(layerNode.Attributes, "name", ref layerFormat.name);
                                TryGainAttributeValue(layerNode.Attributes, "width", ref layerFormat.width);
                                TryGainAttributeValue(layerNode.Attributes, "height", ref layerFormat.height);
                                TryGainAttributeValue(layerNode.Attributes, "visible", ref layerFormat.visible);
                                // properties nodes
                                {
                                    XmlNode propertiesNode = layerNode.SelectSingleNode("properties");
                                    if (propertiesNode != null) {
                                        Formats.Properties propertiesFormat = new Formats.Properties();

                                        foreach (XmlNode propertyNode in propertiesNode.SelectNodes("property")) {
                                            Formats.Property propertyFormat = new Formats.Property();

                                            TryGainAttributeValue(propertyNode.Attributes, "name", ref propertyFormat.name);
                                            TryGainAttributeValue(propertyNode.Attributes, "type", ref propertyFormat.type);
                                            TryGainAttributeValue(propertyNode.Attributes, "value", ref propertyFormat.value);

                                            propertiesFormat.properties.Add(propertyFormat);
                                        }

                                        layerFormat.properties = propertiesFormat;
                                    }
                                }
                                // data nodes
                                {
                                    XmlNode dataNode = layerNode.SelectSingleNode("data");
                                    if (dataNode != null) {
                                        Formats.Layer.Data dataFormat = new Formats.Layer.Data();

                                        TryGainAttributeValue(dataNode.Attributes, "encoding", ref dataFormat.encoding);

                                        foreach (XmlNode tileNode in dataNode.SelectNodes("tile")) {
                                            Formats.Layer.Data.Tile tileFormat = new Formats.Layer.Data.Tile();

                                            TryGainAttributeValue(tileNode.Attributes, "gid", ref tileFormat.gid);

                                            dataFormat.tiles.Add(tileFormat);
                                        }

                                        layerFormat.data = dataFormat;
                                    }
                                }

                                fmMap.layers.Add(layerFormat);
                                fmMap.layerOrders.Add(layerFormat);
                            }
                        }
                        else if (ilayerNodeName == "objectgroup") {
                            // object-group nodes
                            {
                                XmlNode objectGroupNode = ilayerNode;
                                Formats.ObjectGroup objectGroup = new Formats.ObjectGroup();

                                TryGainAttributeValue(objectGroupNode.Attributes, "name", ref objectGroup.name);
                                TryGainAttributeValue(objectGroupNode.Attributes, "offsetx", ref objectGroup.offsetX);
                                TryGainAttributeValue(objectGroupNode.Attributes, "offsety", ref objectGroup.offsetY);

                                // objects nodes
                                foreach (XmlNode objectNode in objectGroupNode.SelectNodes("object")) {
                                    Formats.ObjectGroup.Object obj = new Formats.ObjectGroup.Object();

                                    TryGainAttributeValue(objectNode.Attributes, "id", ref obj.id);
                                    TryGainAttributeValue(objectNode.Attributes, "name", ref obj.name);
                                    TryGainAttributeValue(objectNode.Attributes, "type", ref obj.type);
                                    TryGainAttributeValue(objectNode.Attributes, "gid", ref obj.gid);
                                    TryGainAttributeValue(objectNode.Attributes, "x", ref obj.x);
                                    TryGainAttributeValue(objectNode.Attributes, "y", ref obj.y);
                                    TryGainAttributeValue(objectNode.Attributes, "width", ref obj.width);
                                    TryGainAttributeValue(objectNode.Attributes, "height", ref obj.height);

                                    // PolyLine
                                    {
                                        XmlNode polyLine = objectNode.SelectSingleNode("polyline");
                                        if (polyLine != null) {
                                            string pointsString = "";
                                            TryGainAttributeValue(polyLine.Attributes, "points", ref pointsString);

                                            if (pointsString != "") {
                                                string[] pointStrings = pointsString.Split(' ');
                                                foreach (string pointStr in pointStrings) {
                                                    if (pointStr == "")
                                                        continue;

                                                    string[] pointStrArray = pointStr.Split(new char[] {','}, 2);
                                                    Vector2 pointVector = new Vector2((float)System.Convert.ChangeType(pointStrArray[0], typeof(float)),
                                                            - (float)System.Convert.ChangeType(pointStrArray[1], typeof(float)));

                                                    obj.polyLine.points.Add(pointVector);
                                                }
                                            }
                                        }
                                    }

                                    objectGroup.objects.Add(obj);
                                }

                                fmMap.objGroups.Add(objectGroup);
                                fmMap.layerOrders.Add(objectGroup);
                            }
                        }
                    }

                    this.mapFormat = fmMap;

                    //foreach (Formats.ILayer iLayer in fmMap.layerOrders) {
                        //Debug.Log(string.Format("layer node list: name: {0}", iLayer.Name));
                    //}
                }
            }
        }

        public void ConstructMap() {
            GameObject mapGamObj = GameObject.Find("map");
            if (mapGamObj != null)
                GameObject.DestroyImmediate(mapGamObj);

            mapGamObj = new GameObject();
            mapGamObj.name = "map";
            mapGamObj.transform.position = Vector3.zero;
            mapGamObj.transform.rotation = Quaternion.identity;

            // construct layers
            Debug.Assert(this.mapFormat.IsRenderOrder(Formats.Map.RenderOrder.RightDown));
            foreach (Formats.Layer layer in this.mapFormat.layers) {
                int layerOrder = this.mapFormat.GetLayerOrder(layer);

                GameObject layerGamObj = new GameObject();
                layerGamObj.name = layer.name;
                layerGamObj.transform.parent = mapGamObj.transform;
                layerGamObj.transform.position = Vector3.zero;

                if (layer.data != null) {
                    int tileIndex = -1;
                    int xIndex = -1;
                    int yIndex = 0;

                    foreach (Formats.Layer.Data.Tile tile in layer.data.tiles) {
                        ++ tileIndex;
                        ++ xIndex;
                        if (xIndex >= layer.width) {
                            xIndex = 0;
                            ++ yIndex;
                        }

                        Formats.TileSet srcTileSet = null;
                        Formats.Tile srcTile = null;
                        FindTile(tile.gid, out srcTileSet, out srcTile);

                        Debug.Assert(srcTileSet != null);
                        Debug.Assert(srcTile != null);

                        GameObject newTileGamObj = new GameObject();
                        newTileGamObj.name = string.Format("{0}_tile_{1}", layer.name, tileIndex);
                        newTileGamObj.transform.parent = layerGamObj.transform;
                        newTileGamObj.transform.position = new Vector3(xIndex * srcTileSet.tileWidth, - yIndex * srcTileSet.tileHeight, 0f);

                        GameObject anchor = CreateAnchorGameObject(Formats.AnchorType.LeftUp, srcTileSet.tileWidth, srcTileSet.tileHeight, newTileGamObj.transform);

                        GameObject shape = new GameObject();
                        shape.name = "shape";
                        shape.transform.parent = anchor.transform;
                        shape.transform.localPosition = Vector3.zero;

                        SpriteRenderer spRenderer = shape.AddComponent<SpriteRenderer>();
                        {
                            spRenderer.sprite = srcTile.sprite;
                            spRenderer.sortingOrder = layerOrder;
                        }
                    }
                }
            }

            // construct object-groups
            foreach (Formats.ObjectGroup objGroup in this.mapFormat.objGroups) {
                int layerOrder = this.mapFormat.GetLayerOrder(objGroup);

                GameObject objGroupGamObj = new GameObject();
                objGroupGamObj.name = objGroup.name;
                objGroupGamObj.transform.parent = mapGamObj.transform;
                objGroupGamObj.transform.localPosition = new Vector3(objGroup.offsetX, objGroup.offsetY, 0f);

                foreach (Formats.ObjectGroup.Object obj in objGroup.objects) {

                    GameObject newObjGamObj = new GameObject();
                    newObjGamObj.name = obj.name;
                    newObjGamObj.transform.parent = objGroupGamObj.transform;
                    newObjGamObj.transform.localPosition = new Vector3(obj.x, -obj.y, 0f);

                    if (obj.gid != CommonDatas.NullTileGid) {
                        GameObject anchor = CreateAnchorGameObject(Formats.AnchorType.LeftDown, obj.width, obj.height, newObjGamObj.transform);

                        // shape
                        Formats.TileSet srcTileSet = null;
                        Formats.Tile srcTile = null;
                        FindTile(obj.gid, out srcTileSet, out srcTile);

                        Debug.Assert(srcTileSet != null);
                        Debug.Assert(srcTile != null);

                        GameObject shape = new GameObject();
                        shape.name = "shape";
                        shape.transform.parent = anchor.transform;
                        shape.transform.localPosition = Vector3.zero;
                        shape.transform.localScale = new Vector3(obj.width / (float)srcTileSet.tileWidth, obj.height / (float)srcTileSet.tileHeight, 1f);

                        SpriteRenderer spRenderer = shape.AddComponent<SpriteRenderer>();
                        {
                            spRenderer.sprite = srcTile.sprite;
                            spRenderer.sortingOrder = layerOrder;
                        }

                        if (srcTile.animation != null) {
                            Animator animator = shape.AddComponent<Animator>();
                            {
                                animator.runtimeAnimatorController = srcTile.animation.animController;
                            }
                        }
                    }
                    else {
                        if (obj.polyLine.points.Count > 0) {
                            // poly collision
                            GameObject anchor = CreateAnchorGameObject(Formats.AnchorType.Center, obj.width, obj.height, newObjGamObj.transform);

                            // poly collision
                            GameObject collision = new GameObject();
                            collision.name = "collision";
                            collision.transform.parent = anchor.transform;
                            collision.transform.localPosition = Vector3.zero;

                            PolygonCollider2D collider = collision.AddComponent<PolygonCollider2D>();
                            {
                                collider.points = obj.polyLine.points.ToArray();
                            }
                        }
                        else if (obj.width != 0 && obj.height != 0) {
                            GameObject anchor = CreateAnchorGameObject(Formats.AnchorType.LeftUp, obj.width, obj.height, newObjGamObj.transform);

                            // rect collision
                            GameObject collision = new GameObject();
                            collision.name = "collision";
                            collision.transform.parent = anchor.transform;
                            collision.transform.localPosition = Vector3.zero;

                            BoxCollider2D collider = collision.AddComponent<UnityEngine.BoxCollider2D>();
                            {
                                collider.offset = Vector2.zero;
                                collider.size = new Vector2(obj.width, obj.height);
                            }
                        }
                    }
                }
            }
        }

        GameObject CreateAnchorGameObject(Formats.AnchorType anchorType, float width, float height, Transform parent = null) {
            GameObject anchor = new GameObject();
            anchor.name = "anchor";

            if (parent != null)
                anchor.transform.parent = parent;

            Vector3 localPos = Vector3.zero;
            if (anchorType == Formats.AnchorType.LeftDown) {
                localPos = new Vector3(width * 0.5f, height * 0.5f, 0f);
            }
            else if (anchorType == Formats.AnchorType.LeftUp) {
                localPos = new Vector3(width * 0.5f, - height * 0.5f, 0f);
            }
            else if (anchorType == Formats.AnchorType.RightDown) {
                localPos = new Vector3(- width * 0.5f, height * 0.5f, 0f);
            }
            else if (anchorType == Formats.AnchorType.RightUp) {
                localPos = new Vector3(- width * 0.5f, - height * 0.5f, 0f);
            }
            anchor.transform.localPosition = localPos;

            return anchor;
        }

        void FindTile(int gid, out Formats.TileSet tileSetResult, out Formats.Tile tileResut) {
            foreach (Formats.Map.TileSet tileSet in this.mapFormat.tileSets) {
                if (tileSet.firstGid > gid)
                    break;

                if (tileSet.orgTileSet == null)
                    continue;

                int lastGid = tileSet.firstGid + tileSet.orgTileSet.tileCount - 1;
                if (lastGid < gid)
                    continue;

                int tileID = gid - tileSet.firstGid;

                tileSetResult = tileSet.orgTileSet;
                tileResut = tileSet.orgTileSet.tiles[tileID];

                return;
            }

            tileSetResult = null;
            tileResut = null;
        }

        void TryGainAttributeValue<T>(XmlAttributeCollection attrCollection, string attrName, ref T valuable) {
            XmlAttribute attr = attrCollection[attrName];
            if (attr == null)
                return;

            valuable = (T)System.Convert.ChangeType(attr.Value, typeof(T));
        }


        Formats.TileSet ImportTileSetFormat(XmlNode tileSetNode) {
            Formats.TileSet fmTileSet = new Formats.TileSet();

            TryGainAttributeValue(tileSetNode.Attributes, "firstgid", ref fmTileSet.firstGid);
            TryGainAttributeValue(tileSetNode.Attributes, "source", ref fmTileSet.source);
            TryGainAttributeValue(tileSetNode.Attributes, "name", ref fmTileSet.name);
            TryGainAttributeValue(tileSetNode.Attributes, "tilewidth", ref fmTileSet.tileWidth);
            TryGainAttributeValue(tileSetNode.Attributes, "tileheight", ref fmTileSet.tileHeight);
            TryGainAttributeValue(tileSetNode.Attributes, "tilecount", ref fmTileSet.tileCount);
            TryGainAttributeValue(tileSetNode.Attributes, "columns", ref fmTileSet.columns);

            // tileOffset
            {
                XmlNode tileOffsetNode = tileSetNode.SelectSingleNode("tileoffset");
                if (tileOffsetNode != null) {
                    Formats.TileOffset tileOffset = new Formats.TileOffset();
                    TryGainAttributeValue(tileOffsetNode.Attributes, "x", ref tileOffset.x);
                    TryGainAttributeValue(tileOffsetNode.Attributes, "y", ref tileOffset.y);

                    fmTileSet.tileOffset = tileOffset;
                }
            }
            // image
            {
                XmlNode imgNode = tileSetNode.SelectSingleNode("image");
                if (imgNode != null) {
                    Formats.Image imgFormat = new Formats.Image();

                    TryGainAttributeValue(imgNode.Attributes, "source", ref imgFormat.source);
                    TryGainAttributeValue(imgNode.Attributes, "width", ref imgFormat.width);
                    TryGainAttributeValue(imgNode.Attributes, "height", ref imgFormat.height);

                    fmTileSet.image = imgFormat;
                }
            }

            // tiles
            {
                int rows = 0;
                if (fmTileSet.columns > 0)
                    rows = fmTileSet.tileCount / fmTileSet.columns;

                // create all tile info base.
                {
                    int id = -1;
                    for (int xi = 0; xi < fmTileSet.columns; ++ xi) {
                        for (int yi = 0; yi < rows; ++ yi) {
                            ++ id;

                            Formats.Tile tileFormat = new Formats.Tile();

                            tileFormat.id = id;

                            fmTileSet.tiles.Add(tileFormat);
                        }
                    }
                }

                // override tile info
                foreach (XmlNode tileNode in tileSetNode.SelectNodes("tile")) {
                    int id = 0;
                    TryGainAttributeValue(tileNode.Attributes, "id", ref id);

                    Formats.Tile tileFormat = fmTileSet.tiles[id];

                    //animation
                    {
                        XmlNode aniNode = tileNode.SelectSingleNode("animation");
                        if (aniNode != null) {
                            Formats.Animation aniFormat = new Formats.Animation();

                            foreach (XmlNode frameNode in aniNode.SelectNodes("frame")) {
                                Formats.Animation.Frame frameFormat = new Formats.Animation.Frame();

                                TryGainAttributeValue(frameNode.Attributes, "tileid", ref frameFormat.tileID);
                                TryGainAttributeValue(frameNode.Attributes, "duration", ref frameFormat.duration);

                                aniFormat.frames.Add(frameFormat);
                            }
                            
                            tileFormat.animation = aniFormat;
                        }
                    }
                }
            }

            return fmTileSet;
        }
    }
}
