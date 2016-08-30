#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kino.Tmx {
    [CustomEditor(typeof(TmxImporter))]
    public class TmxImporterEditor : Editor {
        TmxImporter tmxImporter;

        void OnEnable() {
            tmxImporter = target as TmxImporter;
        }

        public override void OnInspectorGUI() {
            if (tmxImporter.tmxFile != null) {
                string filePath = AssetDatabase.GetAssetPath(tmxImporter.tmxFile);
                string fileExtension = Path.GetExtension(filePath);
                //Debug.Log(string.Format("fileExtension: {0}", fileExtension));

                if (fileExtension == ".tmx") {
                    tmxImporter.tmxFilePath = filePath;
                }
                else {
                    tmxImporter.tmxFile = null;
                    tmxImporter.tmxFilePath = "";
                }
            }

            if (GUILayout.Button("Import")) {
                tmxImporter.Import();
                ConstructUnityData();
                SetTileSprites();
                SetAnimations();
            }

            if (GUILayout.Button("Construct")) {
                tmxImporter.ConstructMap();
            }

            DrawDefaultInspector();
        }

        void ConstructUnityData() {
            // construct unity data
            if (tmxImporter.mapFormat != null) {
                List<Texture2D> slicedTextures = new List<Texture2D>();

                // constructed sprite
                foreach (Formats.Map.TileSet tileSet in tmxImporter.mapFormat.tileSets) {
                    if (tileSet.orgTileSet == null)
                        continue;

                    Formats.TileSet tileSetFormat = tileSet.orgTileSet;

                    if (tileSetFormat.image != null) {
                        // set sourceAsset
                        //tileSetFormat.image.sourceAsset;
                        if (tileSetFormat.image.source == "") {
                            Debug.LogError("image.source is empty");
                        }
                        else {
                            string tmxFileParentDirPath = Path.GetDirectoryName(tmxImporter.tmxFilePath);
                            string tileSetImgPath = Path.Combine(tmxFileParentDirPath, tileSetFormat.image.source);
                            string tileSetImgDirPath = Path.GetDirectoryName(tileSetImgPath);
                            string tileSetImgNameWithoutExtension = Path.GetFileNameWithoutExtension(tileSetImgPath);
                            string tileSetImgExtension = Path.GetExtension(tileSetImgPath);
                            //Debug.Log(">>>" + tmxFileParentDirPath);
                            //Debug.Log(tileSetImgPath);
                            //Debug.Log(tileSetImgDirPath);
                            //Debug.Log(tileSetImgNameWithoutExtension);
                            //Debug.Log(tileSetImgExtension + "<<<");
                            
                            Texture2D sourceAsset = AssetDatabase.LoadAssetAtPath(tileSetImgPath, typeof(Texture2D)) as Texture2D;
                            Debug.Assert(sourceAsset != null);
                            {
                                int copyIndex = 0;
                                while (slicedTextures.Contains(sourceAsset)) {
                                    string sourceAssetPath = AssetDatabase.GetAssetPath(sourceAsset);
                                    string copySourceAssetPath = string.Format("{0}/{1}_copy_{2}{3}", 
                                            tileSetImgDirPath, 
                                            tileSetImgNameWithoutExtension, 
                                            copyIndex, 
                                            tileSetImgExtension);

                                    //Debug.Log("--- " + sourceAssetPath);
                                    //Debug.Log(copySourceAssetPath + " ---");

                                    sourceAsset = AssetDatabase.LoadAssetAtPath(copySourceAssetPath, typeof(Texture2D)) as Texture2D;
                                    if (sourceAsset != null)
                                        continue;

                                    AssetDatabase.CopyAsset(sourceAssetPath, copySourceAssetPath);
                                    AssetDatabase.ImportAsset(copySourceAssetPath);
                                    ++ copyIndex;
                                    sourceAsset = AssetDatabase.LoadAssetAtPath(copySourceAssetPath, typeof(Texture2D)) as Texture2D;
                                }
                            }

                            tileSetFormat.image.sourceAsset = sourceAsset;
                            Debug.Assert(tileSetFormat.image.sourceAsset != null);
                            slicedTextures.Add(sourceAsset);

                            // construct sprites
                            {
                                string assetPath = AssetDatabase.GetAssetPath(tileSetFormat.image.sourceAsset);
                                //Debug.Log("assetPath : " + assetPath);

                                TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                                textureImporter.isReadable = true;
                                textureImporter.textureType = TextureImporterType.Sprite;
                                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                                textureImporter.spritePixelsPerUnit = 1f;
                                
                                // make spritesSheet data
                                List<SpriteMetaData> newSpritesSheet = new List<SpriteMetaData>();

                                {
                                    int rows = tileSetFormat.tileCount / tileSetFormat.columns;
                                    int spriteIndex = 0;
                                    for (int y = 0; y < rows; ++ y) {
                                        for (int x = 0; x < tileSetFormat.columns; ++ x, ++ spriteIndex) {
                                            SpriteMetaData spriteMetaData = new SpriteMetaData();

                                            spriteMetaData.name = string.Format("{0}_{1}", tileSetFormat.name, spriteIndex);
                                            // pivot is center
                                            spriteMetaData.pivot = new Vector2(0.5f, 0.5f);
                                            spriteMetaData.rect = new Rect(x * tileSetFormat.tileWidth, tileSetFormat.image.height - (y + 1) * tileSetFormat.tileHeight, tileSetFormat.tileWidth, tileSetFormat.tileHeight);

                                            newSpritesSheet.Add(spriteMetaData);
                                        }
                                    }
                                }

                                textureImporter.spritesheet = newSpritesSheet.ToArray();

                                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                            }
                        }
                    }
                }

                // construct ani
                foreach (Formats.Map.TileSet tileSet in tmxImporter.mapFormat.tileSets) {
                    if (tileSet.orgTileSet == null)
                        continue;

                    Formats.TileSet tileSetFormat = tileSet.orgTileSet;
                    string imgAssetPath = AssetDatabase.GetAssetPath(tileSetFormat.image.sourceAsset);
                    string imgAssetParentDirPath = Path.GetDirectoryName(imgAssetPath);
                    string imgAssetNameWithoutExtension = Path.GetFileNameWithoutExtension(imgAssetPath);
                    string imgAssetExtension = Path.GetExtension(imgAssetPath);

                    foreach (Formats.Tile tile in tileSetFormat.tiles) {
                        if (tile.animation == null)
                            continue;

                        if (tile.animation.frames.Count == 0)
                            continue;

                        string animatorControllerDirPath = string.Format("{0}/{1}Ani/Tile_{2}", imgAssetParentDirPath, imgAssetNameWithoutExtension, tile.id);
                        if (!System.IO.Directory.Exists(animatorControllerDirPath)) {
                            System.IO.Directory.CreateDirectory(animatorControllerDirPath);
                        }

                        // create animation
                        string animationClipPath = string.Format("{0}/Animation.anim", animatorControllerDirPath);
                        AnimationClip animationClip = AssetDatabase.LoadAssetAtPath(animationClipPath, typeof(AnimationClip)) as AnimationClip;
                        if (animationClip == null) {
                            animationClip = new AnimationClip();

                            AnimationClipSettings animClipSettings = AnimationUtility.GetAnimationClipSettings(animationClip);
                            animClipSettings.loopTime = true;
                            AnimationUtility.SetAnimationClipSettings(animationClip, animClipSettings);

                            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(imgAssetPath).OfType<Sprite>().ToArray();

                            EditorCurveBinding[] curveBindings = AnimationUtility.GetObjectReferenceCurveBindings(animationClip);
                            if (curveBindings.Count() == 0) {
                                EditorCurveBinding curveBinding = new EditorCurveBinding();
                                curveBinding.type = typeof(SpriteRenderer);
                                curveBinding.propertyName = "m_Sprite";

                                List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();

                                int firstTileID = -1;
                                float nextTime = 0f;
                                foreach (Formats.Animation.Frame frame in tile.animation.frames) {
                                    ObjectReferenceKeyframe objRefKeyFrame = new ObjectReferenceKeyframe();
                                    objRefKeyFrame.time = nextTime;
                                    nextTime += frame.duration * 0.001f;
                                    objRefKeyFrame.value = sprites[frame.tileID];

                                    keyframes.Add(objRefKeyFrame);

                                    if (firstTileID == -1)
                                        firstTileID = frame.tileID;
                                }

                                // last frame
                                {
                                    ObjectReferenceKeyframe objRefKeyFrame = new ObjectReferenceKeyframe();
                                    objRefKeyFrame.time = nextTime;
                                    objRefKeyFrame.value = sprites[firstTileID];

                                    keyframes.Add(objRefKeyFrame);
                                }

                                Debug.Assert(animationClip != null);
                                Debug.Assert(curveBinding != null);
                                Debug.Assert(keyframes != null);
                                Debug.Assert(keyframes.ToArray() != null);

                                AnimationUtility.SetObjectReferenceCurve(animationClip, curveBinding, keyframes.ToArray());
                            }

                            AssetDatabase.CreateAsset(animationClip, animationClipPath);
                            AssetDatabase.ImportAsset(animationClipPath);
                        }

                        // create animation controller
                        string animatorControllerPath = string.Format("{0}/AnimatorController.controller", animatorControllerDirPath);
                        AnimatorController animatorController = AssetDatabase.LoadAssetAtPath(animatorControllerPath, typeof(AnimatorController)) as AnimatorController;
                        if (animatorController == null) {
                            animatorController = AnimatorController.CreateAnimatorControllerAtPath(animatorControllerPath);
                            AnimatorStateMachine rootStateMachine = animatorController.layers[0].stateMachine;

                            UnityEditor.Animations.AnimatorState entryState = rootStateMachine.AddState("Entry");
                            entryState.motion = animationClip;
                        }
                        
                        //foreach (Formats.Animation.Frame frame in tile.animation.frames) {

                        //}
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        void SetTileSprites() {
            foreach (Formats.Map.TileSet mapTileSet in tmxImporter.mapFormat.tileSets) {
                if (mapTileSet.orgTileSet == null)
                    continue;

                Formats.TileSet orgTileSet = mapTileSet.orgTileSet;
                string imgAssetPath = AssetDatabase.GetAssetPath(orgTileSet.image.sourceAsset);
                //Debug.Log("imgAssetPath: " + imgAssetPath);
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(imgAssetPath).OfType<Sprite>().ToArray();

                foreach (Formats.Tile orgTile in orgTileSet.tiles) {
                    orgTile.sprite = sprites[orgTile.id];
                }
            }
        }

        void SetAnimations() {
            foreach (Formats.TileSet tileSet in tmxImporter.tsxTileSetFormats) {
                foreach (Formats.Tile tile in tileSet.tiles) {
                    if (tile.animation == null)
                        continue;

                    string spriteImgNoExtensionName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(tile.sprite));
                    string dirPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(tile.sprite));
                    string animControllerDirPath = string.Format("{0}/{1}Ani/Tile_{2}", dirPath, spriteImgNoExtensionName, tile.id);
                    const string animControllerName = "AnimatorController.controller";
                    string animControllerPath = string.Format("{0}/{1}", animControllerDirPath, animControllerName);
                    
                    RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath(animControllerPath, typeof(AnimatorController)) as AnimatorController;

                    tile.animation.animController = animController;
                }
            }
        }
    }
}
#endif
